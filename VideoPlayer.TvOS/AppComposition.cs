using Foundation;
using Microsoft.Extensions.Logging;
using VideoPlayer.ApiClient;
using VideoPlayer.ApiClient.Discovery;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;
using VideoPlayer.TvOS.Services;
using VideoPlayer.TvOS.Ui;

namespace VideoPlayer.TvOS
{
    /// <summary>
    /// Kompositionswurzel der tvOS-App — Pendant zu <c>MauiProgram</c> ohne
    /// DI-Container: verdrahtet die Plattform-Adapter (<see cref="ISettingsStore"/>,
    /// <see cref="ISecretStore"/>, <see cref="AppStorageLayout"/>,
    /// <see cref="INetworkStatusMonitor"/>), die plattformneutralen Dienste,
    /// den <see cref="TvosAppEffectHandler"/> und den <see cref="MvuHost"/>
    /// manuell. Alle Objekte sind Singletons der App-Instanz.
    /// </summary>
    public sealed class AppComposition
    {
        private AppComposition()
        {
            Profile = AppProfile.FromEnvironment();
            SettingsStore = new NSUserDefaultsSettingsStore(NSUserDefaults.StandardUserDefaults);

            // Ablage-Verzeichnisse anlegen, bevor Datenbanken/Logging darauf
            // zugreifen (Muster: MauiProgram — Layout ist Singleton).
            StorageLayout = TvosStorageLayoutProvider.CreateLayout(Profile);
            Settings = new AppSettingsService(SettingsStore, Profile);
            AppLog = new AppLogService(StorageLayout, Settings.Load().LoggingEnabled);
            var loggerProvider = new FileLoggerProvider(AppLog);
            LoggerFactory = new TvosLoggerFactory(loggerProvider);

            Secrets = new AppSecretsService(
                SettingsStore,
                new KeychainSecretStore(new AppLogger<KeychainSecretStore>(LoggerFactory)),
                Profile);

            BackupDatabase = new BackupDatabase(
                StorageLayout, new AppLogger<BackupDatabase>(LoggerFactory));
            CacheDatabase = new CacheDatabase(
                StorageLayout, new AppLogger<CacheDatabase>(LoggerFactory));
            KnownServers = new KnownServersService(BackupDatabase, SettingsStore, Profile);
            PendingProgress = new PendingProgressSyncService(
                BackupDatabase, new AppLogger<PendingProgressSyncService>(LoggerFactory));

            ServerAddressProvider = new ServerAddressProvider(KnownServers);
            HealthProbe = new ServerHealthProbe(
                ServerAddressProvider,
                Secrets,
                new AppLogger<ServerHealthProbe>(LoggerFactory),
                LoggerFactory);

            Pairing = new DevicePairingClient(
                new HttpClient { Timeout = DevicePairingClient.RequestTimeout });
            DeviceTokens = new DeviceTokenService(
                Secrets, null, new AppLogger<DeviceTokenService>(LoggerFactory));
            AuthSession = new AuthSessionService(
                Secrets, DeviceTokens, null, null, LoggerFactory);

            NetworkMonitor = new TvosNetworkStatusMonitor(
                new AppLogger<TvosNetworkStatusMonitor>(LoggerFactory));
            ServerEvents = new ServerEventsService(
                AuthSession,
                () => new SignalRServerEventsConnection(
                    new AppLogger<SignalRServerEventsConnection>(LoggerFactory)),
                new AppLogger<ServerEventsService>(LoggerFactory));

            ServerSync = new ServerSyncService(
                AuthSession, PendingProgress, CacheDatabase,
                new AppLogger<ServerSyncService>(LoggerFactory));
            ProgressReports = new ProgressReportService(
                AuthSession, PendingProgress, NetworkMonitor,
                new AppLogger<ProgressReportService>(LoggerFactory));
            HomeLists = new HomeListService(
                CacheDatabase, new AppLogger<HomeListService>(LoggerFactory), TimeProvider.System);
            SourceBrowser = new SourceBrowserService(AuthSession);
            OfflineContinueWatching = new OfflineContinueWatchingService(
                CacheDatabase, BackupDatabase, HomeLists, StorageLayout,
                new AppLogger<OfflineContinueWatchingService>(LoggerFactory));
            PosterImages = new TvosPosterImageService(
                AuthSession, StorageLayout,
                new AppLogger<TvosPosterImageService>(LoggerFactory));
            ServerDiscovery = ServerDiscoveryService.CreateDefault();
        }

        /// <summary>Aktives Anwendungsprofil (<c>VIDEOPLAYER_PROFILE</c>).</summary>
        public AppProfile Profile { get; }

        /// <summary>Nicht-geheime Einstellungsablage (<c>NSUserDefaults</c>).</summary>
        public ISettingsStore SettingsStore { get; }

        /// <summary>Ablage-Verzeichnisstruktur unter <c>Library/Caches</c>.</summary>
        public AppStorageLayout StorageLayout { get; }

        /// <summary>Einstellungsdienst (lesen/schreiben der App-Einstellungen).</summary>
        public AppSettingsService Settings { get; }

        /// <summary>Datei-Protokoll-Dienst (Programmprotokoll im Logs-Verzeichnis).</summary>
        public AppLogService AppLog { get; }

        /// <summary>Logger-Fabrik für alle kategorisierten Logger.</summary>
        public ILoggerFactory LoggerFactory { get; }

        /// <summary>Sichere Ablage für Zugangsdaten und Geräte-Tokens (Schlüsselbund).</summary>
        public AppSecretsService Secrets { get; }

        /// <summary>Backup-pflichtige Datenbank (Serverliste, Fortschritts-Outbox).</summary>
        public BackupDatabase BackupDatabase { get; }

        /// <summary>Cache-Datenbank (gecachte Listen, Offline-Snapshots).</summary>
        public CacheDatabase CacheDatabase { get; }

        /// <summary>Verwaltung bekannter Server.</summary>
        public KnownServersService KnownServers { get; }

        /// <summary>Ausstehende Fortschrittsmeldungen (Offline-Warteschlange).</summary>
        public PendingProgressSyncService PendingProgress { get; }

        /// <summary>Server-Adressquelle (hinterlegter Server, Fallback-Konventionen).</summary>
        public ServerAddressProvider ServerAddressProvider { get; }

        /// <summary>Erreichbarkeitsprüfung des Servers.</summary>
        public IServerHealthProbe HealthProbe { get; }

        /// <summary>Öffentlicher Pairing-Endpunkt-Client.</summary>
        public DevicePairingClient Pairing { get; }

        /// <summary>Verwaltung der gepaarten Geräte-Tokens.</summary>
        public DeviceTokenService DeviceTokens { get; }

        /// <summary>Sitzungsdienst des Anmelde-Workflows.</summary>
        public AuthSessionService AuthSession { get; }

        /// <summary>Netzwerkstatus-Überwachung (<see cref="NWPathMonitor"/>).</summary>
        public INetworkStatusMonitor NetworkMonitor { get; }

        /// <summary>Server-Ereignisverbindung (SignalR-Hub).</summary>
        public ServerEventsService ServerEvents { get; }

        /// <summary>Vollständige/teilweise Server-Synchronisation.</summary>
        public ServerSyncService ServerSync { get; }

        /// <summary>Fortschrittsmeldungen an den Server.</summary>
        public ProgressReportService ProgressReports { get; }

        /// <summary>Lesezugriff auf die gecachten Startseiten-Listen.</summary>
        public HomeListService HomeLists { get; }

        /// <summary>Ladedienst des Quellen-Browsers (Quellen, Genres, paginierte Einträge, Suche).</summary>
        public SourceBrowserService SourceBrowser { get; }

        /// <summary>Offline-Zusammenstellung der Weiterschauen-Liste.</summary>
        public OfflineContinueWatchingService OfflineContinueWatching { get; }

        /// <summary>Poster-/Vorschaubild-Cache der Startseiten-Karussells.</summary>
        public TvosPosterImageService PosterImages { get; }

        /// <summary>Netzwerk-Erkennung (mDNS + UDP-Broadcast).</summary>
        public ServerDiscoveryService ServerDiscovery { get; }

        private MvuHost? host;
        private TvosNavigator? navigator;

        /// <summary>MVU-Host des App-Zyklus (erstellt in <see cref="CreateRuntime"/>).</summary>
        /// <exception cref="InvalidOperationException"><see cref="CreateRuntime"/> wurde noch nicht aufgerufen.</exception>
        public MvuHost Host => host
            ?? throw new InvalidOperationException("CreateRuntime wurde noch nicht aufgerufen.");

        /// <summary>Der Navigator des Seitenrahmens (erstellt in <see cref="CreateRuntime"/>).</summary>
        /// <exception cref="InvalidOperationException"><see cref="CreateRuntime"/> wurde noch nicht aufgerufen.</exception>
        public TvosNavigator Navigator => navigator
            ?? throw new InvalidOperationException("CreateRuntime wurde noch nicht aufgerufen.");

        /// <summary>Erstellt die Komposition mit den Standard-Adaptern.</summary>
        /// <returns>Die vollständig verdrahteten Dienste der App.</returns>
        public static AppComposition Create() => new();

        /// <summary>
        /// Erzeugt den Laufzeit-Teil der Komposition: Navigator,
        /// <see cref="TvosAppEffectHandler"/> und <see cref="MvuHost"/>,
        /// verbindet den Navigator mit dem Fensterrahmen und verdrahtet die
        /// modellgetriebene Darstellung (<see cref="MvuHost.ModelChanged"/>
        /// → <see cref="TvosNavigator.Render"/>).
        /// </summary>
        /// <param name="main">Der Fensterrahmen der App.</param>
        /// <returns>Der MVU-Host (bereits für <c>DispatchAsync</c> nutzbar).</returns>
        public MvuHost CreateRuntime(MainViewController main)
        {
            ArgumentNullException.ThrowIfNull(main);
            if (host is not null)
            {
                return host;
            }

            // Rückmeldung in den MVU-Zyklus immer auf dem Hauptthread —
            // UIKit-Objekte und die modellgetriebene Darstellung sind
            // hauptthread-gebunden.
            Task Dispatch(AppMessage message) =>
                MainThreadInvoker.InvokeOnMainThreadAsync(() => Host.DispatchAsync(message));

            navigator = new TvosNavigator(Dispatch, PosterImages);
            navigator.Attach(main);
            var effectHandler = new TvosAppEffectHandler(
                this,
                HealthProbe,
                AuthSession,
                Pairing,
                DeviceTokens,
                Secrets,
                NetworkMonitor,
                ServerEvents,
                navigator,
                new AppLogger<TvosAppEffectHandler>(LoggerFactory));
            host = new MvuHost(effectHandler);
            host.ModelChanged += (_, _) =>
                MainThreadInvoker.InvokeOnMainThread(() => navigator.Render(Host.Model));
            return host;
        }
    }
}
