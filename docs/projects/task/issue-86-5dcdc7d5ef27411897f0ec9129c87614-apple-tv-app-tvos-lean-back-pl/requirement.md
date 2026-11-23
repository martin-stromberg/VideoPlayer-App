# Anforderung: Apple TV-App (tvOS) — Lean-back-Player via .NET for tvOS, Deployment nach App Store Connect

Quelle: `issue.md` (Issue #86), Aufgaben-ID `5dcdc7d5-ef27-4118-97f0-ec9129c87614`.

## Fachliche Zusammenfassung

Neben der bestehenden App (Windows/iOS/Android über `VideoPlayer.Maui`) wird eine eigenständige App für **Apple TV (tvOS)** als Lean-back-Player mit dem Kern-Funktionsumfang der bestehenden App gebaut: Serververbindung mit Pairing-Code-Flow, Startseiten-Karussells (Weiterschauen, „Neu im Programm", Favoriten), Quellen-/Genre-Browsing und Suche, Detailseiten, Wiedergabe mit Fortschritts-Tracking und Auto-Fortsetzung, rein lesender Playlist-Zugriff sowie manuelle Downloads mit Ablaufdatum und Quota. Da .NET MAUI tvOS nicht unterstützt und Xamarin.tvOS seit Mai 2024 End-of-Life ist, erfolgt die Umsetzung mit **.NET for tvOS** (`net…-tvos`, macios-Workloads) und tvOS-nativem UI-Aufbau (UIKit/SwiftUI-Bindings, Focus-Engine für die Fernbedienung, ggf. Top Shelf). Die plattformunabhängigen Schichten (`VideoPlayer.Mvu`, `VideoPlayer.Storage`, `VideoPlayer.ApiClient`, `msTools.DeveloperApi`, Domain-Logik — alle `net10.0`, keine MAUI-Abhängigkeit) sollen wiederverwendet werden; die MAUI-UI ist nicht übertragbar. Der Staging-/Release-Workflow soll analog zum iOS-Pfad eine tvOS-App bauen, signieren und als Release Candidate nach **App Store Connect** laden.

## Funktionsumfang (MVP, aus dem Issue belegt)

1. **Serververbindung:** Pairing-Code-Flow (auf TV ideal — Code-Anzeige am TV), Anmeldung, Servererkennung/-wechsel. Entspricht den bestehenden Flüssen `POST api/pairing/exchange` (Einmal-Code) und `POST api/pairing/bootstrap` (Ticket/Kurzcode) sowie `ServerDiscoveryService` (mDNS/UDP), `KnownServersService`, `AuthSessionService`, `DeviceTokenService`.
2. **Startseite:** Weiterschauen, „Neu im Programm"- und Favoriten-Karussells (`HomeListService`, `cached_lists`, Cache-First).
3. **Browsing und Suche:** Quellen-/Genre-Browsing (`SourceBrowserService`) und Suche — Eingabe per Diktat/iPhone-Tastatur (tvOS-Eingabemechanismen).
4. **Detailseiten:** Film, Serie, Staffel, Episode (`MediaDetailsService`).
5. **Wiedergabe:** Fortschritts-Tracking inkl. „angesehen"-Schwelle (`ProgressReportService`, `pending_progress`-Warteschlange, `WatchedEndWindowSeconds`, bestätigter Fenstereintritt), Auto-Fortsetzung für Serie + Playlist (`AutoResumeEnabled`, `advance`-/`next`-/`previous`-Endpunkte), Spur-/Untertitelwahl.
6. **Playlists:** nur aufrufen, Inhalte durchblättern und abspielen (`GET api/playlists`, `…/public`, `…/entries/paged`, `POST api/playlists/{id}/play`) — keine Anlage/Bearbeitung.
7. **Manuelle Downloads:** einzeln über die Detailseite anstoßen; schlanke Download-Liste mit Status, Abbrechen, Löschen; Ablaufdatum je Download (einstellbare Aufbewahrungszeit); Bereinigung bei App-Start **und App-Aktivierung** (tvOS-Apps bleiben wochenlang suspendiert — reine Kaltstart-Prüfung würde selten feuern); Reconcile-Logik (fehlende Datei → `missing_file`) ist Pflicht, da tvOS App-Daten unter Speicherdruck purgt — die Aufbewahrungszeit ist Obergrenze, keine Garantie; einfache Quota-Obergrenze (tvOS-Speicher ist geteilt).
8. **Einstellungen:** schlanker Bereich (Aufbewahrungszeit, Quota, Auto-Resume, Endfenster o. ä.).

## Betroffene Klassen und Komponenten

### Wiederverwendung (plattformneutral, `net10.0` — belegt)

- **`VideoPlayer.ApiClient`:** `VideoPlayerApiClient` (nur lesende + Wiedergabe-/Fortschritts-Endpunkte), `DevicePairingClient` (`exchange`/`bootstrap`), `PairingCrypto` (ECDH P-256/AES-256-GCM), `ClientGate` (`DeviceTokenProvider`), `Discovery` (`ServerDiscoveryService`, `UdpServerDiscovery`, `MdnsServerDiscovery`), Models (`MediaEntry`, `Playlist`, `ContinueWatching`, `RecentEntry`, `FavoriteEntry`, `SeasonalGenre` u. a.).
- **`VideoPlayer.Storage`:** `AppSettings`, `AppSettingsService`, `AppSecretsService`, `KnownServersService`, `ServerAddress`, `CacheDatabase`, `BackupDatabase`, `PendingProgressSyncService`, `AppStorageLayout`, `AppLogService` (`FileLogger`/`FileLoggerProvider`), Entities (`DownloadManifestEntry`, `KnownServerEntry`, `CachedListEntry`, `CachedFavoritesEntry`, `PendingProgressSyncEntry`, `BackupStateEntry`).
- **`VideoPlayer.Mvu`:** MVU-Kern `MvuHost`, `AppModel`, `AppUpdate`, `AppMessage`, `AppEffect`, `AppPage`, `AppTransition`, `IAppEffectHandler`; Services `AuthSessionService`, `DeviceTokenService`, `BootstrapTicketParser`, `PairingCode`, `HomeListService`, `SourceBrowserService`, `MediaDetailsService`, `FavoritesService`, `PlaylistService`, `PlaybackSourceService`, `ProgressReportService`, `ServerSyncService`, `ServerEventsService`, `DownloadService`, `OfflineContinueWatchingService`, `ContinueWatchingMutationService`, `UpcomingEpisodesService`, `ProfileInfoService`; Download-Typen `IDownloadBackend`, `HttpDownloadBackend`, `DownloadEnqueueRequest`, `DownloadListItem`, `DownloadState`, `DownloadReconcileResult`, `DownloadManifestPaths`, `DownloadsPageState`; `DevApi/DevApiModelSnapshot`.
- **`msTools.DeveloperApi`:** plattformneutraler Mini-HTTP-Server für UI-Automatisierung — Wiederverwendung unter tvOS zu prüfen.

### Neu zu erstellen (Annahmen — Namen an Projektkonventionen angelehnt)

- **Neues App-Projekt** (Arbeitsname `VideoPlayer.TvOS`, TFM `net10.0-tvos`): App-Einstieg (`UIApplicationDelegate`/Lebenszyklus), Verdrahtung von `MvuHost` und Services.
- **tvOS-UI-Controller/Views** für alle `AppPage`-Ausprägungen: Startseite mit fokussierbaren Karussells, Quellen-/Genre-Ansicht, Suche (`UISearchController` o. ä.), Detailseiten (Film/Serie/Staffel/Episode), Playlist-Übersicht/-Detail, Download-Liste, Einstellungen, Server-Setup/Pairing/Anmeldung — UIKit/SwiftUI-Bindings mit Focus-Engine (`UIFocusEnvironment`) für die Siri Remote.
- **tvOS-`IAppEffectHandler`** als Gegenstück zum MAUI-`ShellAppEffectHandler` (Navigation, Effekt-Ausführung am App-Rand).
- **tvOS-Plattform-Adapter:** `ISettingsStore` (z. B. `NSUserDefaults`), `ISecretStore` (Keychain/`SecItem`), `AppStorageLayout`-Provider, `INetworkStatusMonitor`, ggf. `IServerEventsConnection`/`IDownloadBackend`-Varianten (Hintergrund-Session).
- **Nativer Player:** `AVPlayerViewController`/`AVPlayer`-Wrapper — Fortschrittstakt (periodic time observer), Ende-Ereignis, Spur-/Untertitelwahl (`AVMediaSelectionGroup`), Fortsetzungssprung.
- **Aktivierungs-Hook:** Lebenszyklus-Trigger für den Download-Abgleich/-Bereinigungslauf bei App-Aktivierung (`WillEnterForeground`/`DidBecomeActive`), zusätzlich zu App-Start.
- **CI/Deployment:** neue Composite-Action analog `.github/actions/package-ios` (z. B. `package-tvos`), zusätzliche Jobs in `.github/workflows/staging-merge.yml`/`main-merge.yml`; ggf. lokales Skript analog `scripts/iOS-Deployment.ps1`.
- **Tests:** bestehende plattformneutrale Suiten (`VideoPlayer.Maui.Tests` prüft großteils die Mvu-Logik) laufen weiter; tvOS-spezifische Ergänzungen offen.

### Datenmodell-/Enum-Berührungspunkte

- `AppPage`-Enum als Seiteninventar (Player ist heute Route `player`, kein `AppPage`-Eintrag — tvOS-Navigation separat zu definieren).
- `AppSettings` (Teilmenge, siehe Konfiguration); `DownloadState`/`DownloadReconcileResult` inkl. `missing_file`; `PlaybackSource`/`PlaybackSourceKind` (`ServerStream`/`LocalFile`); `PlayerState` inkl. `EndWindowStreak`, Playlist-Kontextfeldern; `DownloadOrigin` (Herkunft „Cache" entfällt konzeptionell ohne Auto-Download).

## Implementierungsansatz

- **MVU-Kreislauf wiederverwenden:** `MvuHost` + `AppUpdate` bleiben der Zustandskern; die tvOS-UI dispatcht dieselben Messages und rendert aus `AppModel`. Der MAUI-spezifische `ShellAppEffectHandler` wird durch einen tvOS-`IAppEffectHandler` ersetzt — Erweiterung über die bestehende Schnittstelle, keine neue Abstraktion.
- **Bereinigung bei App-Aktivierung (neuer Erweiterungspunkt):** der bisherige Abgleich/Bereinigungslauf des `DownloadService` feuert heute bei App-Start, Online-Rückkehr und Öffnen der Downloads-Seite. Für tvOS ist ein zusätzlicher Trigger nötig: Message/Effekt aus dem Lebenszyklus (`WillEnterForeground`/`DidBecomeActive`), damit Ablauf-Prüfung und Reconcile (`missing_file`, Waisen-/Teildateien) auch nach wochenlanger Suspension laufen. *Annahme:* Erweiterung um eine Aktivierungs-Message im bestehenden Update-/Effekt-Fluss statt eines tvOS-only Sonderpfads.
- **Wiedergabe:** AVPlayer-basiert; der 5-Sekunden-Fortschrittstakt, das plausibilisierte Ende-Ereignis (Toleranzfenster) und der bestätigte Fenstereintritt (`EndWindowRequiredTicks = 2`) bleiben in `AppUpdate` — der tvOS-Player ist reiner Event-Übersetzer wie heute die `PlayerPage` (`appliedMediaId`-Medienbindung der Takte übernehmen). Auto-Fortsetzung über `UpcomingEpisodesService` (Serie) bzw. `advance`-Endpunkt (Playlist). Spur-/Untertitelwahl nativ über `AVMediaSelectionGroup` — *offene Abhängigkeit zum Stream-Format, siehe Offene Fragen.*
- **Suche/Eingabe:** bestehender Suchpfad des `SourceBrowserService`; Eingabe über tvOS-Mechanismen (Diktat, iPhone-Tastatur/Continuity Keyboard).
- **Downloads:** `DownloadService` mit Manifest (`download_manifest`) und einfacher Quota (`DownloadQuotaGb`); ohne Auto-Download entfallen Kandidaten-/Verlängerungs- und Herkunfts-Logik (`DownloadOrigin` „Cache") sowie Re-Download-Schutz — *Annahme:* bestehender Service wird für tvOS ohne Auto-Download-Pfade betrieben (`AutoDownloadService` nicht verdrahtet), nicht dupliziert.
- **Abhängigkeiten prüfen:** `sqlite-net-pcl`/`SQLitePCLRaw.bundle_green` unter `net10.0-tvos`; `Zeroconf` (mDNS) unter tvOS; Keychain- statt `SecureStorage`-Adapter; `MediaElement`/`CommunityToolkit.Maui.MediaElement` entfällt (nativer AVPlayer).
- **Deployment:** analog iOS-Pfad — Build für `net…-tvos`, Codesigning (Zertifikat + tvOS-Provisioning-Profil), Upload des Release Candidates nach App Store Connect (iTMSTransporter wie bei `package-ios`); Workload-Pinning der macios-Workloads analog dem iOS-Rollback (`workload-rollback.json`). Voraussetzung laut Issue: Bundle-ID, Provisioning-Profile und Zertifikate für tvOS im Apple-Developer-Account.
- **Akzeptanzrahmen (aus dem Issue):** MVP-Scope abgedeckt, keinerlei Schreibzugriff auf Inhalte; CI-Job baut die tvOS-App für den Release-Tag; Release-Kandidat landet automatisiert in App Store Connect.

## Konfiguration

- **Einstellungen:** bestehender `AppSettings`/`AppSettingsService` mit neuem tvOS-`ISettingsStore`-Adapter. MVP-Teilmenge auf der Einstellungsseite: `DownloadRetentionDays` (Aufbewahrungszeit), `DownloadQuotaGb` (Quota), `AutoResumeEnabled` (Auto-Fortsetzung), `WatchedEndWindowSeconds` („Als angesehen"-Endfenster) — alle belegt durch den Scope „Aufbewahrungszeit, Quota, Auto-Resume, Endfenster o. ä.".
- **Entfallene/nicht exponierte Werte (Annahme):** `AutoDownloadEnabled`, `PreloadEpisodeCount` (kein Auto-Download); `AutoDeletePlayedDownloads`/`DeleteAfterPlaybackDelayMinutes` (im Issue nicht genannt — s. Offene Fragen); `WifiOnlyDownloads` (tvOS typischerweise Festnetz/WLAN ohne Drossel-Profil — Relevanz zu prüfen); `LoggingEnabled` ohne Versand (lokale Protokolldatei möglich, kein Mail-Composer); `HomeHeroEnabled` optional.
- **Server/Profil:** `VIDEOPLAYER_SERVER_URL` als Entwicklungs-Fallback, `VIDEOPLAYER_PROFILE` für getrennte Profile — beide Konventionen bestehen bereits und bleiben sinnvoll (Annahme).
- **CI-Secrets:** tvOS-spezifische Secrets analog den iOS-Eingängen der `package-ios`-Action (Signaturidentität, Provisioning-Profil, App-Store-Connect-API-Key `.p8`/Issuer-ID, Zertifikat-Export).

## Explizite Nicht-Ziele

- **Keine Bearbeitung von Metadaten** (Filme, Serien, Episoden, Playlists) — jeglicher Schreibzugriff auf Inhalte entfällt.
- **Keine Playlist-Verwaltung** (anlegen, umbenennen, sortieren, Einträge, Genre-Editor, Öffentlich-Flags).
- **Kein Auto-Download** (kein Vorlauf, keine Kandidaten-Logik).
- **Kein Protokollversand** (tvOS hat keinen Mail-Composer).

## Offene Fragen

- **Pairing-Code-Flow „Code-Anzeige am TV":** Zeigt die TV-App selbst einen Code an, der an anderer Stelle (z. B. Server-Web-UI) eingelöst wird — das wäre ein neuer Server-Vertrag —, oder bleibt es bei der Eingabe des serverseitig erzeugten Einmal-/Kurzcodes auf dem TV (bestehende `exchange`/`bootstrap`-Endpunkte)?
- **Spur-/Untertitelwahl:** Liefert der Stream-Endpunkt `api/items/{typ}/{id}/stream` ein Format mit wählbaren Spuren (z. B. HLS mit `AVMediaSelectionGroup`)? Genügt clientseitige Auswahl im AVPlayer oder ist Server-Unterstützung nötig? (Die bestehende App hat keine Spurwahl — das ist für tvOS neuer Funktionsumfang.)
- **TFM/Workloads:** exaktes TFM (`net10.0-tvos`) und Pinning-Version der `Microsoft.tvOS.Sdk`-Workload-Manifeste für den CI-Runner (analog `microsoft.net.sdk.ios`-Rollback).
- **NuGet-Kompatibilität tvOS:** `sqlite-net-pcl`/`SQLitePCLRaw` und `Zeroconf` unter `net10.0-tvos` verifizieren; ggf. tvOS-Ersatz für die Discovery.
- **Hintergrund-Downloads unter tvOS:** Sollen Transfers in einer `NSURLSession`-Hintergrund-Konfiguration laufen (Suspension-tauglich), oder genügt Vordergrund-Transfer mit Wiederaufnahme? (Issue nennt nur „Vorabladen für schwachen Server".)
- **„Downloads nach dem Abspielen löschen":** im Issue nicht genannt — entfällt die Option auf tvOS (Teil des schlanken Einstellungsbereichs) oder gilt sie stillschweigend mit?
- **Modell-Schnittmenge:** Werden tvOS-irrelevante Modell-/Message-Anteile (QR-Scanner — kein Kamera-Zugang, Mini-Player/PiP, Auto-Download, Protokollversand) unverdrahtet belassen oder konditionalisiert?
- **Top Shelf:** Ist eine Top-Shelf-Extension („ggf." im Issue) Teil des MVP?
- **Teststrategie:** Nutzung von `msTools.DeveloperApi` unter tvOS für UI-Automatisierung — es existiert keine tvOS-UI-Testinfrastruktur im Repository.
- **App-Store-Voraussetzungen:** Sind Bundle-ID, Provisioning-Profile und Zertifikate für tvOS bereits im Apple-Developer-Account eingerichtet, oder ist das ein vorgelagerter manueller Schritt?
