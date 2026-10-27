using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Pages;

/// <summary>
/// Startseite der App: horizontale Medienlisten „Weiterschauen",
/// „Favoriten" und „Neu im Programm" aus der lokalen Cache-Ablage
/// (Cache-First, Aktualisierung über MVU-Modell nach Synchronisation und
/// Server-Ereignissen). Die Auswahl eines Eintrags navigiert über den MVU-
/// Kreislauf zur Detailseite des Mediums.
/// </summary>
public partial class HomePage : ContentPage
{
    private readonly MvuHost mvuHost;
    private byte[]? appliedHeroBytes;
    private bool subscribed;
    private readonly TimeProvider timeProvider;
    private ITimer? expiryTimer;

    /// <summary>Initialisiert eine neue Instanz der <see cref="HomePage"/>-Klasse.</summary>
    /// <param name="mvuHost">MVU-Host; dient als <see cref="BindableObject.BindingContext"/> und Dispatch-Ziel der Auswahl-Nachrichten.</param>
    /// <param name="timeProvider">Uhr für serverseitige Ablaufgrenzen.</param>
    public HomePage(MvuHost mvuHost, TimeProvider timeProvider)
    {
        InitializeComponent();
        this.mvuHost = mvuHost;
        this.timeProvider = timeProvider;
        BindingContext = mvuHost;
        HomeRefresh.Command = new Command(() => _ = mvuHost.DispatchAsync(new HomeRefreshRequestedMessage()));
        SizeChanged += OnSizeChanged;
        UpdateBanners(mvuHost.Model);
        UpdateHero(mvuHost.Model);
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Das ModelChanged-Abo gilt nur, solange die Seite sichtbar ist
        // (Muster: SettingsPage) — verlassene Instanzen dürfen nicht
        // weiter am MvuHost hängen.
        subscribed = true;
        mvuHost.ModelChanged -= OnModelChanged;
        mvuHost.ModelChanged += OnModelChanged;
        UpdateBanners(mvuHost.Model);
        UpdateHero(mvuHost.Model);
        ScheduleExpiry(mvuHost.Model);
        _ = mvuHost.DispatchAsync(new HomeListsReloadRequestedMessage());
    }

    /// <inheritdoc/>
    protected override void OnDisappearing()
    {
        subscribed = false;
        expiryTimer?.Dispose();
        expiryTimer = null;
        mvuHost.ModelChanged -= OnModelChanged;
        base.OnDisappearing();
    }

    private void OnModelChanged(object? sender, EventArgs e)
    {
        // UI-Anpassungen auf dem Hauptthread; die Listen binden über
        // BindingContext an das MVU-Modell.
        var model = mvuHost.Model;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (subscribed)
            {
                UpdateBanners(model);
                UpdateHero(model);
                ScheduleExpiry(model);
            }
        });
    }

    private void UpdateBanners(AppModel model)
    {
        OfflineBanner.IsVisible = model.IsOffline;
        StorageNoticeBanner.IsVisible = model.StorageNotice is not null;
    }

    private void ScheduleExpiry(AppModel model)
    {
        expiryTimer?.Dispose();
        expiryTimer = null;
        if (!subscribed || model.SeasonalGenres.Count == 0)
            return;

        var delay = model.SeasonalGenres.Min(g => g.ExpiresAt) - timeProvider.GetUtcNow();
        // Revalidate bounded long timers after wall-clock corrections as well.
        delay = delay <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(1) : TimeSpan.FromTicks(Math.Min(delay.Ticks, TimeSpan.FromMinutes(1).Ticks));
        expiryTimer = timeProvider.CreateTimer(_ => MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!subscribed)
                return;
            if (mvuHost.Model.SeasonalGenres.Any(g => g.ExpiresAt <= timeProvider.GetUtcNow()))
                _ = mvuHost.DispatchAsync(new HomeListsReloadRequestedMessage());
            else
                ScheduleExpiry(mvuHost.Model);
        }), null, delay, Timeout.InfiniteTimeSpan);
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        // Responsive Höhe — App-Gegenstück zum Web-CSS
        // clamp(260px, 42vw, 520px).
        if (Width > 0)
        {
            HeroSection.HeightRequest = Math.Clamp(Width * 0.42, 260, 520);
        }
    }

    /// <summary>
    /// Übernimmt die Hero-Bilddaten des Models in die Bildebene. Die
    /// Umwandlung in eine <see cref="ImageSource"/> erfolgt nur bei
    /// tatsächlich neuen Bytes (Referenzvergleich); ohne Bild bleibt die
    /// Ebene leer — die Gradienten und Texte tragen den Kopfbereich.
    /// </summary>
    /// <param name="model">Aktuelles App-Model.</param>
    private void UpdateHero(AppModel model)
    {
        if (ReferenceEquals(appliedHeroBytes, model.HomeHero.ImageBytes))
        {
            return;
        }

        appliedHeroBytes = model.HomeHero.ImageBytes;
        HeroImage.Source = appliedHeroBytes is { Length: > 0 } bytes
            ? ImageSource.FromStream(() => new MemoryStream(bytes))
            : null;
    }

    private void OnMediaItemTapped(object? sender, HomeMediaItem item)
    {
        // Die Auswahl läuft über den MVU-Kreislauf — Update entscheidet
        // über die Detail-Navigation.
        _ = mvuHost.DispatchAsync(new MediaItemSelectedMessage(item));
    }

    private void OnContinueWatchingItemTapped(object? sender, HomeMediaItem item)
    {
        // Weiterschauen-Einträge starten abspielbare Typen direkt —
        // Update entscheidet über Wiedergabe bzw. Detail-Fallback.
        _ = mvuHost.DispatchAsync(new ContinueWatchingItemSelectedMessage(item));
    }

    private async void OnContinueWatchingItemLongPressed(object? sender, HomeMediaItem item)
    {
        // Kontextmenü nur für verwertbare Einträge (Platzhalter fehlender
        // Offline-Episoden tragen keine serverseitige Identität).
        if (!item.IsSelectable)
        {
            return;
        }

        // Kontextmenü der Weiterschauen-Liste (2-s-Long-Press) — analog
        // zur Serveranwendung: Ausblenden/Überspringen über die MVU-
        // Nachrichten an die Server-Endpunkte bzw. das lokale Entfernen.
        var choice = await DisplayActionSheetAsync(
            item.Title, "Abbrechen", null, FlowDirection.MatchParent, "Ausblenden", "Überspringen");
        switch (choice)
        {
            case "Ausblenden":
                _ = mvuHost.DispatchAsync(new ContinueWatchingHideRequestedMessage(item));
                break;
            case "Überspringen":
                _ = mvuHost.DispatchAsync(new ContinueWatchingSkipRequestedMessage(item));
                break;
        }
    }
}
