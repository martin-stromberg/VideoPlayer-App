using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.Logging;
using VideoPlayer.Maui.Services;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;
#if WINDOWS
using Microsoft.UI.Xaml.Controls;
#endif

namespace VideoPlayer.Maui.Pages;

/// <summary>
/// Wiedergabeseite (Shell-Route „player", geschoben auf den
/// Navigationsstack): spielt die vom <see cref="PlayerState"/> aufgelöste
/// Quelle über das <see cref="MediaElement"/> ab (Server-Stream oder
/// lokale Datei), springt auf die Fortsetzungsposition und meldet den
/// Fortschritt im 5-Sekunden-Takt in den MVU-Zyklus. Der PiP-/Mini-
/// Player-Schalter erscheint nur auf Plattformen mit umschaltbarem
/// Overlay (Windows) — auf iOS stellt die native Transportsteuerung die
/// PiP-Schaltfläche bereit.
/// </summary>
public partial class PlayerPage : ContentPage
{
    private static readonly TimeSpan ProgressTickInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ResumeSeekTimeout = TimeSpan.FromSeconds(10);
    // Geschätzte Höhe der nativen iOS-Top-Leiste des
    // AVPlayerViewController (Vollbild-/PiP-Buttons) inklusive Safe-Area
    // und Sicherheitsabstand. Der Wert ist ohne Gerät nicht verifizierbar
    // und in der TestFlight-Geräteprüfung zu bestätigen bzw.
    // feinzujustieren.
    private const double IosNativeControlsClearance = 96;

    private readonly MvuHost mvuHost;
    private readonly IPictureInPictureService pictureInPicture;
    private readonly ILogger<PlayerPage> logger;
    private string? appliedLocation;
    // Gesetzt, wenn der letzte Quellenwechsel fehlschlug: erzwingt, dass
    // UpdateUi den Setz-/Leerpfad erneut durchläuft — auch wenn das Modell
    // dieselbe Adresse erneut liefert oder die Quelle leert (null == null
    // würde sonst per Frühreturn den Stop der noch laufenden Quelle
    // überspringen).
    private bool sourceApplyFailed;
    // Gemerkter Fortsetzungs-Seek der aktuellen Quelle — wird erst bei
    // Wiedergabebereitschaft angewendet (Zugriff ausschließlich auf dem
    // UI-Thread, alle Schreiber/Leser laufen in MainThread-Marshals).
    private TimeSpan? pendingResumePosition;
    private bool timerRunning;
    // volatile: wird sowohl auf dem UI-Thread als auch auf dem
    // Thread des jeweiligen Toolkit-Events gelesen/geschrieben.
    private volatile bool subscribed;

    /// <summary>Initialisiert eine neue Instanz der <see cref="PlayerPage"/>-Klasse.</summary>
    /// <param name="mvuHost">MVU-Host; liefert den Player-Zustand und nimmt die Nachrichten entgegen.</param>
    /// <param name="pictureInPicture">Plattformbezogener PiP-/Mini-Player-Dienst (Schalter-Sichtbarkeit).</param>
    /// <param name="logger">Logger für das Programmprotokoll.</param>
    public PlayerPage(MvuHost mvuHost, IPictureInPictureService pictureInPicture, ILogger<PlayerPage> logger)
    {
        InitializeComponent();
        this.mvuHost = mvuHost;
        this.pictureInPicture = pictureInPicture;
        this.logger = logger;
        BindingContext = mvuHost;
        PipButton.IsVisible = pictureInPicture.IsToggleSupported;
#if IOS
        // iOS blendet die native Top-Leiste des AVPlayerViewController mit
        // den Vollbild-/PiP-Buttons im oberen Randbereich ein.
        // OverlayChrome und PlaylistNavigationBar werden um denselben
        // Versatz unter die Leiste geschoben: das eingabedurchlässige
        // Kopfband würde die nativen Buttons sonst weiterhin visuell
        // überdecken (Scrim-Abdunklung, WatchedBadge in der oberen rechten
        // Ecke), und die interaktive Bar darf nicht eingabedurchlässig
        // sein — sie muss daher physisch darunter liegen und hält ihren
        // XAML-Abstand zum (mitverschobenen) Kopfband. Die XAML-Werte
        // bleiben der Standardpfad für Windows und andere Plattformen.
        OverlayChrome.Margin = new Thickness(0, IosNativeControlsClearance, 0, 0);
        var barMargin = PlaylistNavigationBar.Margin;
        PlaylistNavigationBar.Margin = new Thickness(
            barMargin.Left,
            barMargin.Top + IosNativeControlsClearance,
            barMargin.Right,
            barMargin.Bottom);
#endif
#if WINDOWS
        // Unter Windows blenden die Transportsteuerungen des MediaElements
        // sich nach dem Timeout nicht mehr über eigene Pointer-Ereignisse ein
        // (die Tap-/Hover-Erkennung des MediaPlayerElement greift in dieser
        // Kombination nicht). Workaround: Ein Tip auf die Videofläche ruft
        // TransportControls.Show() — Ausblenden erledigt der Auto-Timer.
        var tap = new TapGestureRecognizer();
        tap.Tapped += OnPlayerTapped;
        PlayerRoot.GestureRecognizers.Add(tap);
#endif
        UpdateUi(mvuHost.Model);
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Die Seite ist transient (pro Wiedergabe neu erzeugt) — das Abo
        // gilt nur, solange sie sichtbar ist (Muster: SettingsPage).
        subscribed = true;
        mvuHost.ModelChanged -= OnModelChanged;
        mvuHost.ModelChanged += OnModelChanged;
        _ = mvuHost.DispatchAsync(new PlayerPageOpenedMessage());
        UpdateUi(mvuHost.Model);
        StartProgressTimer();
    }

    /// <inheritdoc/>
    protected override void OnDisappearing()
    {
        // Erst abmelden, dann schließen: Eine verwaiste Seite darf den
        // eigenen Player-Reset nicht mehr auf ihrem losgelösten
        // MediaElement rendern.
        subscribed = false;
        mvuHost.ModelChanged -= OnModelChanged;
        timerRunning = false;
        // Verlassene Wiedergabe: Overlay-Fenstermodus wiederherstellen,
        // Abschluss-Fortschritt melden und den Player-Zustand leeren.
        _ = pictureInPicture.RestoreAsync();
        _ = mvuHost.DispatchAsync(new PlayerClosedMessage());
        base.OnDisappearing();
    }

    /// <summary>
    /// Fortschrittstakt (Muster des Altbestand-Players): solange die
    /// Wiedergabe läuft, wird alle 5 Sekunden die Position in den
    /// MVU-Zyklus gemeldet; Zustandswechsel (Pufferung/Pause) kommen
    /// über <c>StateChanged</c>.
    /// </summary>
    private void StartProgressTimer()
    {
        if (timerRunning)
        {
            return;
        }

        timerRunning = true;
        Dispatcher.StartTimer(ProgressTickInterval, () =>
        {
            if (!timerRunning)
            {
                return false;
            }

            try
            {
                if (VideoElement.CurrentState == MediaElementState.Playing)
                {
                    ReportPosition();
                }
            }
            catch (Exception ex)
            {
                // Wie ReportPosition: ein Fehler am nativen Element darf im
                // Dispatcher-Timer-Callback nicht unbehandelt bleiben (nur
                // protokollieren; der nächste Takt versucht es erneut).
                logger.LogWarning(ex, "Zustandsabfrage im Fortschrittstakt fehlgeschlagen.");
            }

            return true;
        });
    }

    private void ReportPosition()
    {
        try
        {
            var state = VideoElement.CurrentState;
            _ = mvuHost.DispatchAsync(new PlaybackProgressMessage(
                VideoElement.Position,
                VideoElement.Duration,
                state == MediaElementState.Playing,
                state is MediaElementState.Buffering or MediaElementState.Opening));
        }
        catch (Exception ex)
        {
            // Der Aufruf läuft aus dem Dispatcher-Timer-Callback — ein
            // Fehler am nativen Element darf nicht unbehandelt bleiben
            // (nur protokollieren; der nächste Takt versucht es erneut).
            logger.LogWarning(ex, "Positionsmeldung fehlgeschlagen.");
        }
    }

    private void OnModelChanged(object? sender, EventArgs e)
    {
        var model = mvuHost.Model;
        // Modelländerungen kommen auf dem Dispatch-Thread — Elementzugriff
        // nur gemarshalt und nur solange die Seite noch aktiv ist.
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (subscribed)
            {
                UpdateUi(model);
            }
        });
    }

    private void UpdateUi(AppModel model)
    {
        var player = model.Player;

        TitleLabel.Text = player.Title ?? string.Empty;
        PlaylistContextLabel.Text = player.PlaylistLabel;
        PlaylistContextLabel.IsVisible = player.IsPlaylistPlayback;
        WatchedBadge.IsVisible = player.IsWatched;
        ErrorPanel.IsVisible = player.HasError && !player.IsPictureInPicture;
        ErrorLabel.Text = player.Error;
        // Im Mini-Player (kompaktes Overlay) wird nur das Video gezeigt.
        OverlayChrome.IsVisible = !player.IsPictureInPicture;
        PlaylistNavigationBar.IsVisible = player.IsPlaylistPlayback && !player.IsPictureInPicture;
        PreviousButton.IsEnabled = player.CanNavigatePrevious;
        NextButton.IsEnabled = player.CanNavigateNext;
        LoadingIndicator.IsVisible = player.IsResolving || player.IsBuffering;
        LoadingIndicator.IsRunning = LoadingIndicator.IsVisible;

        var location = player.Source?.Location;
        if (!sourceApplyFailed && location == appliedLocation)
        {
            return;
        }

        sourceApplyFailed = false;
        appliedLocation = location;
        pendingResumePosition = null;
        if (location is null || player.Source is null)
        {
            try
            {
                VideoElement.Stop();
                VideoElement.Source = null;
            }
            catch (Exception ex)
            {
                FailPlaybackStart($"Zurücksetzen der Quelle fehlgeschlagen: {SanitizeForLog(ex.Message)}");
            }

            return;
        }

        if (!IsValidSourceLocation(player.Source, location))
        {
            // Die Quell-Adresse selbst bleibt unprotokolliert — sie
            // enthält bei ServerStream die access_token-Query.
            FailPlaybackStart($"Ungültige Quell-Adresse ({player.Source.Kind}).");
            return;
        }

        try
        {
            VideoElement.Source = player.Source.Kind == PlaybackSourceKind.LocalFile
                ? MediaSource.FromFile(location)
                : MediaSource.FromUri(location);
        }
        catch (Exception ex)
        {
            FailPlaybackStart($"Setzen der Quelle fehlgeschlagen: {SanitizeForLog(ex.Message)}");
        }
    }

    /// <summary>
    /// Frühvalidierung der aufgelösten Quell-Adresse vor der Übergabe an
    /// das native Element: <see cref="PlaybackSourceKind.ServerStream"/>
    /// verlangt eine absolute http/https-URL, <see cref="PlaybackSourceKind.LocalFile"/>
    /// einen nicht leeren Pfad — ungültige Werte lösen am nativen Player
    /// sonst unausweichende Ausnahmen aus.
    /// </summary>
    /// <param name="source">Die aufgelöste Quelle (nur die Art ist relevant).</param>
    /// <param name="location">Die zu prüfende Quell-Adresse (URL oder Pfad).</param>
    /// <returns><see langword="true"/>, wenn die Adresse zur Quellenart passt.</returns>
    private static bool IsValidSourceLocation(PlaybackSource source, string location)
    {
        return source.Kind switch
        {
            PlaybackSourceKind.ServerStream =>
                Uri.TryCreate(location, UriKind.Absolute, out var uri)
                && uri.Scheme is "http" or "https",
            PlaybackSourceKind.LocalFile => !string.IsNullOrWhiteSpace(location),
            _ => false
        };
    }

    /// <summary>
    /// Meldet einen Fehler des Wiedergabestarts in den MVU-Zyklus
    /// (<see cref="PlaybackFailedMessage"/> → Fehlerpanel/Episoden-Rückfall)
    /// und merkt den Fehlschlag über <see cref="sourceApplyFailed"/>, damit
    /// die nächste Modelländerung den Quellenwechsel erneut durchläuft —
    /// unabhängig davon, ob das Modell dieselbe Adresse erneut liefert
    /// oder die Quelle leert.
    /// </summary>
    /// <param name="logMessage">Diagnosemeldung für das Protokoll (bereits bereinigt).</param>
    private void FailPlaybackStart(string logMessage)
    {
        logger.LogError("Wiedergabestart fehlgeschlagen: {LogMessage}", logMessage);
        sourceApplyFailed = true;
        if (subscribed)
        {
            _ = mvuHost.DispatchAsync(new PlaybackFailedMessage());
        }
    }

    /// <summary>
    /// Bereinigt plattform- oder ausnahmegenerierte Meldungen vor dem
    /// Protokollieren: eine enthaltene Quell-URL kann die
    /// access_token-Query tragen — der Token-Wert wird maskiert. Die
    /// Maskierung teilt sich die Seite mit dem Sinkenpunkt
    /// (<see cref="AppLogService.MaskAccessToken"/>) — eine
    /// Implementierung für beide Schreiber.
    /// </summary>
    /// <param name="message">Die rohe Diagnosemeldung.</param>
    /// <returns>Die Meldung mit maskiertem access_token-Wert.</returns>
    private static string SanitizeForLog(string? message)
    {
        return message is null ? string.Empty : AppLogService.MaskAccessToken(message);
    }

    private void OnMediaOpened(object? sender, EventArgs e)
    {
        logger.LogInformation(
            "Wiedergabe geöffnet: {Title} (Fortsetzung {ResumePosition}).",
            mvuHost.Model.Player.Title,
            mvuHost.Model.Player.Source?.ResumePosition);

        // Fortsetzungsposition der Quelle lediglich merken: auf iOS feuert
        // MediaOpened noch vor AVPlayerItemStatus.ReadyToPlay (Toolkit-
        // Issue #3249) — ein sofortiger Seek wirft im async-void-Handler
        // des Toolkits eine unbehandelte Ausnahme (SIGABRT). Der Seek
        // wird daher erst über TryApplyPendingResumeSeek angestoßen,
        // sobald der Player Playing/Paused meldet.
        var resume = mvuHost.Model.Player.Source?.ResumePosition;
        if (resume.HasValue && resume.Value > TimeSpan.Zero)
        {
            var position = resume.Value;
            // Das Toolkit-Event kann auf einem Threadpool-Thread feuern —
            // das native Element darf nur auf dem UI-Thread angefasst
            // werden (sonst COMException RPC_E_WRONG_THREAD in
            // MediaManager.PlatformSeek).
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (subscribed)
                {
                    pendingResumePosition = position;
                    TryApplyPendingResumeSeek();
                }
            });
        }
    }

    /// <summary>
    /// Wendet einen gemerkten Fortsetzungs-Seek an, sobald das Element
    /// wiedergabebereit ist (<see cref="MediaElementState.Playing"/> oder
    /// <see cref="MediaElementState.Paused"/> — beide implizieren
    /// ReadyToPlay). Liegt der Bereitschaftszustand noch nicht vor,
    /// bleibt der Wunsch bis zum nächsten Zustandswechsel liegen.
    /// Läuft ausschließlich auf dem UI-Thread.
    /// </summary>
    private void TryApplyPendingResumeSeek()
    {
        if (pendingResumePosition is not { } position)
        {
            return;
        }

        MediaElementState state;
        try
        {
            state = VideoElement.CurrentState;
        }
        catch (Exception ex)
        {
            // Der Aufruf läuft aus BeginInvokeOnMainThread-Marshals — ein
            // Wurf des nativen Elements darf dem Lambda nicht entkommen.
            // Der Seek-Wunsch bleibt liegen und wird beim nächsten
            // Zustandswechsel erneut geprüft.
            logger.LogWarning(ex, "Bereitschaftsprüfung des Fortsetzungs-Seeks fehlgeschlagen.");
            return;
        }

        if (state is not (MediaElementState.Playing or MediaElementState.Paused))
        {
            return;
        }

        pendingResumePosition = null;
        _ = ApplyResumeSeekAsync(position);
    }

    /// <summary>
    /// Führt den Fortsetzungs-Seek mit Zeitlimit aus: die Toolkit-
    /// Vollendung kann im Fehlerfall ausbleiben — ohne Abbruchschwelle
    /// hinge das await ewig und blockierte die interne Seek-Semaphore
    /// für alle Folge-Seeks. Timeout oder Fehler werden als
    /// Wiedergabefehler gemeldet statt den Prozess abzubrechen.
    /// </summary>
    /// <param name="position">Zielposition des Fortsetzungs-Sprungs.</param>
    /// <returns>Eine Aufgabe, die nach dem Sprung oder Fehlschlag abschließt.</returns>
    private async Task ApplyResumeSeekAsync(TimeSpan position)
    {
        try
        {
            using var timeout = new CancellationTokenSource(ResumeSeekTimeout);
            await VideoElement.SeekTo(position, timeout.Token);
            logger.LogInformation("Fortsetzungs-Seek auf {Position} angewendet.", position);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Fortsetzungs-Seek auf {Position} fehlgeschlagen.", position);
            if (subscribed)
            {
                _ = mvuHost.DispatchAsync(new PlaybackFailedMessage());
            }
        }
    }

    private void OnMediaEnded(object? sender, EventArgs e)
    {
        logger.LogInformation("Wiedergabe beendet: {Title}.", mvuHost.Model.Player.Title);
        if (subscribed)
        {
            _ = mvuHost.DispatchAsync(new PlaybackEndedMessage());
        }
    }

    private void OnMediaFailed(object? sender, MediaFailedEventArgs e)
    {
        logger.LogError("Wiedergabe fehlgeschlagen: {LogMessage}", SanitizeForLog(e.ErrorMessage));
        if (subscribed)
        {
            _ = mvuHost.DispatchAsync(new PlaybackFailedMessage());
        }
    }

    private void OnMediaStateChanged(object? sender, MediaStateChangedEventArgs e)
    {
        // Zustandswechsel (Pufferung, Pause, Wiedergabe) sofort in den
        // MVU-Zyklus spiegeln — Meldungen bleiben per Throttling
        // positionsgebunden. Das Event kann auf einem Threadpool-Thread
        // feuern; die Positions-/Zustandsabfrage am nativen Element läuft
        // deshalb gemarshalt auf dem UI-Thread.
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (subscribed)
            {
                ReportPosition();
                // Erreicht der Player Playing/Paused, ist er bereit —
                // ein noch liegender Fortsetzungs-Seek wird angewendet.
                TryApplyPendingResumeSeek();
            }
        });
    }

    private void OnPipClicked(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new PictureInPictureRequestedMessage());
    }

    private void OnPreviousClicked(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new PlaylistPreviousRequestedMessage());
    }

    private void OnNextClicked(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new PlaylistNextRequestedMessage());
    }

    /// <summary>
    /// Windows-Workaround: Ein Tip auf die Videofläche blendet die
    /// Transportsteuerung wieder ein. Das Ausblenden übernimmt weiterhin der
    /// Auto-Timer des MediaPlayerElement.
    /// </summary>
    /// <param name="sender">Das auslösende Element.</param>
    /// <param name="e">Ereignisdaten.</param>
    private void OnPlayerTapped(object? sender, TappedEventArgs e)
    {
#if WINDOWS
        if (VideoElement.Handler?.PlatformView is Panel panel
            && panel.Children.OfType<MediaPlayerElement>().FirstOrDefault() is { } mpe)
        {
            mpe.TransportControls?.Show();
        }
#endif
    }
}
