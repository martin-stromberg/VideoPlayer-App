using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using VideoPlayer.Maui.Services;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;
#if ANDROID || IOS
// ZXing.Net.Maui.Controls wird nur für die mobilen TFMs referenziert.
using ZXing.Net.Maui.Controls;
#endif

namespace VideoPlayer.Maui
{
    /// <summary>
    /// Einstiegspunkt der MAUI-App: baut die <see cref="MauiApp"/> mit
    /// Dependency-Injection-Registrierungen für MVU-Host, Effekt-Handler,
    /// lokale Ablage, Serververwaltung, Synchronisation und alle Seiten.
    /// </summary>
    public static class MauiProgram
    {
        /// <summary>
        /// Erstellt und konfiguriert die <see cref="MauiApp"/> der Anwendung
        /// (Dienste, Seiten, Schriftarten, Debug-Logging).
        /// </summary>
        /// <returns>Die konfigurierte <see cref="MauiApp"/>-Instanz.</returns>
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder.UseMauiApp<App>();

            // Videowiedergabe (MediaElement) — registriert die plattform-
            // spezifischen Player (iOS: AVPlayerViewController mit nativem
            // PiP; Windows: MediaPlayerElement).
            builder.UseMauiCommunityToolkitMediaElement(true);

#if ANDROID || IOS
            // QR-/Ticket-Onboarding: Kamera-Scan nur auf Mobilgeräten
            // (ZXing.Net.Maui.Controls 0.10.4; Windows/MacCatalyst nutzen
            // den manuellen Ticket-Einstieg der ServerSetupPage).
            builder.UseBarcodeReader();
#endif

            // MVU-Zyklus: Host hält das unveränderliche App-Model, der Effekt-Handler
            // führt Navigation und Server-Erreichbarkeitsprüfung am App-Rand aus.
            builder.Services.AddSingleton<IServerAddressProvider, ServerAddressProvider>();
            builder.Services.AddSingleton<IServerHealthProbe, ServerHealthProbe>();
            // Geräte-Pairing: öffentlicher Austausch-Client (ohne Gate-/
            // Bearer-Header und ohne Wiederholungen) plus Verwaltung der
            // gepaarten Tokens (sichere Ablage, Gate-Provider).
            builder.Services.AddSingleton<VideoPlayer.ApiClient.DevicePairingClient>(
                _ => new VideoPlayer.ApiClient.DevicePairingClient(
                    new HttpClient { Timeout = VideoPlayer.ApiClient.DevicePairingClient.RequestTimeout }));
            builder.Services.AddSingleton<DeviceTokenService>();
            builder.Services.AddSingleton<AuthSessionService>();
            builder.Services.AddSingleton<IAppEffectHandler, ShellAppEffectHandler>();
            builder.Services.AddSingleton<MvuHost>();

            // Lokale Datenhaltung: Profil-Isolation, normale und sichere Ablage,
            // Einstellungs-/Geheimnisdienste und getrennte Datenbanken.
            builder.Services.AddSingleton(_ => AppProfile.FromEnvironment());
            builder.Services.AddSingleton<ISettingsStore>(_ => new PreferencesSettingsStore(Preferences.Default));
            builder.Services.AddSingleton<ISecretStore>(sp => new SecureStorageSecretStore(
                SecureStorage.Default,
                sp.GetRequiredService<ILogger<SecureStorageSecretStore>>()));
            builder.Services.AddSingleton<AppSettingsService>();
            builder.Services.AddSingleton<AppSecretsService>();
            builder.Services.AddSingleton(sp =>
                AppStorageLayoutProvider.CreateLayout(sp.GetRequiredService<AppProfile>()));

            // Programmprotokoll (Datei-Logging im Logs-Verzeichnis des
            // Ablage-Layouts): der Startzustand wird synchron aus der
            // persistenten Einstellung gelesen; Laufzeit-Umschaltungen
            // laufen über ShellAppEffectHandler → AppLogService.SetEnabled.
            builder.Services.AddSingleton(sp =>
                new AppLogService(
                    sp.GetRequiredService<AppStorageLayout>(),
                    sp.GetRequiredService<AppSettingsService>().Load().LoggingEnabled));
            builder.Services.AddSingleton<ILoggerProvider>(
                sp => new FileLoggerProvider(sp.GetRequiredService<AppLogService>()));
            builder.Services.AddSingleton<BackupDatabase>();
            builder.Services.AddSingleton<CacheDatabase>();

            // Serververwaltung und Netzwerk-Erkennung (mDNS + UDP-Broadcast).
            builder.Services.AddSingleton<KnownServersService>();
            builder.Services.AddSingleton<VideoPlayer.ApiClient.Discovery.ServerDiscoveryService>(
                _ => VideoPlayer.ApiClient.Discovery.ServerDiscoveryService.CreateDefault());

            // Netzwerkstatus, Ereignisverbindung (SignalR) und Synchronisation.
            builder.Services.AddSingleton<INetworkStatusMonitor, ConnectivityNetworkStatusMonitor>();
            builder.Services.AddSingleton<Func<IServerEventsConnection>>(
                sp => () => new SignalRServerEventsConnection(
                    sp.GetRequiredService<ILogger<SignalRServerEventsConnection>>()));
            builder.Services.AddSingleton<ServerEventsService>();
            builder.Services.AddSingleton<PendingProgressSyncService>();
            builder.Services.AddSingleton<ProgressReportService>();
            builder.Services.AddSingleton<ServerSyncService>();

            // Startseiten-Listen aus der Cache-Ablage (Cache-First) und
            // Vorschaubilder (api/pictures/{id}) mit lokaler Ablage im
            // nicht backup-pflichtigen Previews-Verzeichnis.
            builder.Services.AddSingleton<HomeListService>();
            builder.Services.AddSingleton<OfflineContinueWatchingService>();
            builder.Services.AddSingleton<PosterImageService>();

            // Quellen-Browsing, Medien-Details und Favoriten-Umschaltung
            // (plattformneutrale Dienste auf dem angemeldeten API-Client).
            builder.Services.AddSingleton<SourceBrowserService>();
            builder.Services.AddSingleton<MediaDetailsService>();
            builder.Services.AddSingleton<FavoritesService>();
            // Playlists: Übersicht (eigene + öffentliche) und paginierte
            // Einträge — reines Online-Feature ohne lokale Kopie.
            builder.Services.AddSingleton<PlaylistService>();
            // Profil-Seite: Benutzerinfo aus Token-Claims/gespeicherter
            // Anmelde-Kennung plus aktives lokales Profil.
            builder.Services.AddSingleton<ProfileInfoService>();

            // Wiedergabe: Quellauflösung (Stream/lokale Datei) und
            // Picture-in-Picture (Windows: kompaktes Overlay des
            // App-Fensters; iOS: nativer PiP der Transportsteuerung).
            builder.Services.AddSingleton<PlaybackSourceService>();
            builder.Services.AddSingleton<IPictureInPictureService, PictureInPictureService>();
            // Folgevideo-Auflösung des Weiterschauen-Direktstarts:
            // Serienkontext ohne Detailseite (Server bzw. Cache).
            builder.Services.AddSingleton<UpcomingEpisodesService>();
            // Weiterschauen-Kontextmenü: Ausblenden/Überspringen über die
            // Server-Endpunkte bzw. lokales Entfernen aus dem Cache.
            builder.Services.AddSingleton<ContinueWatchingMutationService>();

            // Download-Verwaltung: persistente Aufträge im Download-
            // Manifest, parallele Transfers mit Quota/WLAN-Bedingung —
            // die Downloads-Seite speist sich über ItemChanged/ItemRemoved.
            // AutoDownloadService: Vorlauf der Weiterschauen-Episoden.
            builder.Services.AddSingleton<DownloadService>();
            builder.Services.AddSingleton<AutoDownloadService>();

            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddTransient<Pages.LoadingPage>();
            builder.Services.AddTransient<Pages.HomePage>();
            builder.Services.AddTransient<Pages.FavoritesPage>();
            builder.Services.AddTransient<Pages.PlaylistsPage>();
            builder.Services.AddTransient<Pages.DownloadsPage>();
            builder.Services.AddTransient<Pages.ProfilePage>();
            builder.Services.AddTransient<Pages.SettingsPage>();
            builder.Services.AddTransient<Pages.ServerSetupPage>();
            builder.Services.AddTransient<Pages.LoginPage>();
            // Detailseite (Shell-Route „mediadetails") und
            // Quellen-Browsing-Seite (Shell-Route „sourceoverview").
            builder.Services.AddTransient<Pages.MediaDetailsPage>();
            builder.Services.AddTransient<Pages.SourceOverviewPage>();
            // Playlist-Detailseite (Shell-Route „playlistdetails").
            builder.Services.AddTransient<Pages.PlaylistDetailsPage>();
            // Wiedergabeseite (Shell-Route „player").
            builder.Services.AddTransient<Pages.PlayerPage>();

            builder.ConfigureFonts(fonts =>
            {
                // Hausschrift Inter (SIL Open Font License): Regular löst
                // FontAttributes.Bold/Italic über die RIBBI-Schnitte Bold/
                // Italic auf; Medium/SemiBold stehen als eigene Familien
                // für Zwischenabstufungen zur Verfügung.
                fonts.AddFont("Inter-Regular.ttf", "Inter");
                fonts.AddFont("Inter-Italic.ttf", "InterItalic");
                fonts.AddFont("Inter-Medium.ttf", "InterMedium");
                fonts.AddFont("Inter-SemiBold.ttf", "InterSemiBold");
                fonts.AddFont("Inter-Bold.ttf", "InterBold");
                // Material Icons Round (Apache License 2.0): einheitliche
                // Glyphen für Menü-Icons und Symbol-Schaltflächen.
                fonts.AddFont("MaterialIconsRound-Regular.otf", "MaterialIcons");
            });

#if DEBUG || DEV_API
            // Developer-API (nur Debug oder explizites Opt-in via DEV_API-Define,
            // niemals Release): lokaler Mini-HTTP-Server auf 127.0.0.1 zur
            // UI-Automatisierung; Start erfolgt in App.CreateWindow.
            builder.Services.AddSingleton<DevApi.DevApiService>();
#endif

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
