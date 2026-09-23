using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using VideoPlayer.Maui.Services;
using VideoPlayer.Mvu;

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

    private readonly MvuHost mvuHost;
    private readonly IPictureInPictureService pictureInPicture;
    private string? appliedLocation;
    // volatile: werden sowohl auf dem UI-Thread als auch auf dem
    // Thread des jeweiligen Toolkit-Events gelesen/geschrieben.
    private volatile bool resumeApplied;
    private bool timerRunning;
    private volatile bool subscribed;

    /// <summary>Initialisiert eine neue Instanz der <see cref="PlayerPage"/>-Klasse.</summary>
    /// <param name="mvuHost">MVU-Host; liefert den Player-Zustand und nimmt die Nachrichten entgegen.</param>
    /// <param name="pictureInPicture">Plattformbezogener PiP-/Mini-Player-Dienst (Schalter-Sichtbarkeit).</param>
    public PlayerPage(MvuHost mvuHost, IPictureInPictureService pictureInPicture)
    {
        InitializeComponent();
        this.mvuHost = mvuHost;
        this.pictureInPicture = pictureInPicture;
        BindingContext = mvuHost;
        PipButton.IsVisible = pictureInPicture.IsToggleSupported;
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

            if (VideoElement.CurrentState == MediaElementState.Playing)
            {
                ReportPosition();
            }

            return true;
        });
    }

    private void ReportPosition()
    {
        var state = VideoElement.CurrentState;
        _ = mvuHost.DispatchAsync(new PlaybackProgressMessage(
            VideoElement.Position,
            VideoElement.Duration,
            state == MediaElementState.Playing,
            state is MediaElementState.Buffering or MediaElementState.Opening));
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
        WatchedBadge.IsVisible = player.IsWatched;
        ErrorPanel.IsVisible = player.HasError && !player.IsPictureInPicture;
        ErrorLabel.Text = player.Error;
        // Im Mini-Player (kompaktes Overlay) wird nur das Video gezeigt.
        OverlayChrome.IsVisible = !player.IsPictureInPicture;
        LoadingIndicator.IsVisible = player.IsResolving || player.IsBuffering;
        LoadingIndicator.IsRunning = LoadingIndicator.IsVisible;

        var location = player.Source?.Location;
        if (location == appliedLocation)
        {
            return;
        }

        appliedLocation = location;
        resumeApplied = false;
        if (location is null || player.Source is null)
        {
            VideoElement.Stop();
            VideoElement.Source = null;
            return;
        }

        VideoElement.Source = player.Source.Kind == PlaybackSourceKind.LocalFile
            ? MediaSource.FromFile(location)
            : MediaSource.FromUri(location);
    }

    private void OnMediaOpened(object? sender, EventArgs e)
    {
        // Einmalig pro Quelle auf die Fortsetzungsposition springen.
        if (resumeApplied)
        {
            return;
        }

        resumeApplied = true;
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
                    _ = VideoElement.SeekTo(position);
                }
            });
        }
    }

    private void OnMediaEnded(object? sender, EventArgs e)
    {
        if (subscribed)
        {
            _ = mvuHost.DispatchAsync(new PlaybackEndedMessage());
        }
    }

    private void OnMediaFailed(object? sender, MediaFailedEventArgs e)
    {
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
            }
        });
    }

    private void OnPipClicked(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new PictureInPictureRequestedMessage());
    }
}
