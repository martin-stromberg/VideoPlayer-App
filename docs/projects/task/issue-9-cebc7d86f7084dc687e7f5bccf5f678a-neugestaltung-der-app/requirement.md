# Übersetzte Anforderung: Neugestaltung der App „VideoPlayer" (Issue #9)

Quelle: `issue.md`, Abschnitt „## Anforderung". Technische Bezeichnungen sind im Original und in Backticks. Annahmen sind explizit als *(Annahme)* gekennzeichnet. Als Orientierung für Namenskonventionen dient die vorhandene Repository-Struktur (`VideoWebPlayer.Maui`, `VideoWebPlayer.Client`, `VideoWebPlayer.Maui.Tests`); `docs/features.md` bzw. `docs/features/` existieren nicht.

## Fachliche Zusammenfassung

Es wird eine neue .NET-MAUI-App als Frontend zur bestehenden Serveranwendung „VideoPlayer" (`https://github.com/martin-stromberg/VideoPlayer`) entwickelt. Die App kommuniziert ausschließlich über die öffentliche API des Servers; UI und Bedienung sollen der Webanwendung möglichst gleichen. Zielplattformen sind zunächst iOS und Windows (Android später), Architektur ist MVU (Model–View–Update), Wiedergabe über `CommunityToolkit.Maui.MediaElement` mit verpflichtender Picture-in-Picture-Unterstützung. Kernfunktionen: Multi-Server-Verwaltung mit automatischer Servererkennung, Startseite mit gecachten Listen („Weiterschauen", „Favoriten", „Neu im Programm"), automatische und manuelle Downloads für Offlinebetrieb, Fortschrittsmeldung mit Offline-Sammeln und Nachsynchronisierung, sowie eine klar getrennte Cloud-Backup-Strategie (Einstellungen sichern, Medien neu laden).

## Betroffene Klassen und Komponenten bzw. Bereiche

Da es sich um eine Neuentwicklung handelt, sind die folgenden Artefakte voraussichtlich neu zu erstellen oder bestehende Strukturen entsprechend zu ersetzen. Die Benennung folgt den im Repository beobachteten Konventionen (`*Service` + `I*Service` unter `VideoWebPlayer.Maui/Services/`, `*Page.xaml`, DTOs `*Dto`/`Dto*` unter `VideoWebPlayer.Client/Models/`, `*Event` + `NotificationEventService` unter `Services/Events/`, SQLite-Modelle unter `VideoWebPlayer.Maui/Models/`).

### API-Client-Schicht (`VideoWebPlayer.Client`)

- `VideoWebPlayerClient` / `MauiVideoWebPlayerClient`: Zugriff auf die öffentliche Server-API. Belegte Endpunkte (aus vorhandenem Client): `api/auth/login`, `api/health`, `api/Sources`, `api/items/recent`, `api/continue-watching` (+ `progress`, `hide`, `skip`), `api/favorites` (+ `toggle`, `remove`), `api/items` (+ `movie`, `tvshow`, `tvshowepisode`, `genres`, `moviecollection`, `metadata`), `api/pictures/{id}`.
- DTOs: `DtoMediaSource`, `DtoRecentEntry`, `ContinueWatchingDto`, `DtoFavoriteEntry`, `MediaEntryDto`, `DtoMovie`, `DtoTVShowEpisode`, `AuthenticationRequest`, `AuthorizationToken`.
- `MauiClientConfiguration`: Client-Gate via Header `X-API-Key` (Quelle: MSBuild-Property `MauiClientApiToken` bzw. Umgebungsvariable `VIDEOWEBPLAYER_MAUI_API_TOKEN`); Backend-Gegenstück `Jwt:ApiToken:Maui`.
- Retry-Strategie für fehlerhafte Serverantworten (definiert zu werden, s. Fehlerszenarien).

### Einstellungen, Profile, Serververwaltung

- `ISettingsService` / `SettingsService`: Persistenz via `Preferences.Default` (nicht geheim) und `SecureStorage.Default` (geheim: `AuthToken`, `Passkey`), Profil-Präfixe via `ProfileManager.GetPreferencesPrefix()`.
- Neu/erweitert: Liste bekannter Server (`KnownServers`), zuletzt verbundener Server (`LastConnectedServer` / heute `ServerAddress`), App-Einstellungen für automatische Downloads (an/aus), Anzahl vorzuladender Episoden, Auto-Fortsetzung (an/aus), WLAN-only-Downloads, Zeitfenster Auto-Fortsetzung, Zeitfenster „als angesehen", Speicherplatzkontingent, optionale Auto-Löschung gesehener Episoden.
- `IConnectionService` / `ConnectionService` mit `ConnectionState` (`Connected`, `Offline`, `NeedsServerSetup`, `NeedsLogin`): Verbindungs-Workflow inkl. `HealthCheckAsync`, automatische Verbindung zum zuletzt genutzten Server, automatische Wiederverbindung.
- Servererkennung: bestehender Mechanismus `SettingsService.DiscoverServersAsync` (mDNS via `Zeroconf`) und `DiscoverServersUdpAsync` (UDP-Broadcast `VIDEOWEBPLAYER_DISCOVERY` / Antwort `VIDEOWEBPLAYER_SERVER:` auf Port 5001) als Referenz *(Annahme: Protokoll bleibt bestehen bzw. muss serverseitig beantwortet werden)*.
- `IAuthService` / `AuthService`: Login, Credential-Speicherung, Token-Handling (`HandleUnauthorized`-Refresh im Client).

### Datenhaltung & Backup-Klassifikation

- `ClientDatabase` (Singleton, `sqlite-net-pcl`, `SQLiteAsyncConnection`, Tabellen `Downloads`/`DownloadedVideo`, `CarouselCache`/`CachedCarouselItem`): lokale Datenbank für Downloads, Caches und unsynchronisierte Fortschrittsdaten.
- Neuer Bereich: Backup-Klassifikation der Ablageorte — plattformspezifische Steuerung, welche Dateien/Verzeichnisse ins Cloud-Backup eingehen (iOS: Ausschluss via „skip backup"-Attribut *(Annahme: `NSFileManager`/`NSURL.IsExcludedFromBackupKey` bzw. Ablage in `FileSystem.CacheDirectory` statt `FileSystem.AppDataDirectory`)*; Windows: entsprechendes Äquivalent zu prüfen, s. Offene Fragen).
- Modelle: `DownloadedVideo` (inkl. `DownloadStatus`, `DownloadRetentionType`, `PlaybackPositionSeconds`), `CachedCarouselItem`; neu: Speicher für unsynchronisierte Fortschrittsmeldungen (Outbox/Queue) und Download-Manifest.

### Netzwerk & Synchronisierung

- Netzwerkstatus-Erkennung: *(Annahme: `Microsoft.Maui.Networking.Connectivity` bzw. `Connectivity.ConnectivityChanged`)* — Online/Offline-Wechsel triggern Reconnect und Synchronisierung.
- `SignalRService` + `NotificationEventService` (`IPublishNotificationEvent`, `ISubscribeNotificationEvent`, Events wie `ContinueWatchingUpdatedEvent`, `FavoritesChangedEvent`, `NewVideosScannedEvent`, `DownloadCompletedEvent`, `DownloadDeletedEvent`): Server-Ereignisse und Reconnect-Behandlung.
- Synchronisierungskomponente: vollständiger Metadaten-Sync, Nachsenden unsynchronisierter Fortschrittsdaten, Aktualisierung der Weiterschauen-Liste (Referenz: `WatchlistDownloadCoordinatorService`).

### Downloads & Offlinebetrieb

- `DownloadManager`, `DownloadQueue`: parallele Downloads, automatische Wiederaufnahme, Fehlerbehandlung, Speicherplatzkontingent, optionale Auto-Löschung gesehener Episoden.
- Automatische Downloads (Vorladen von Episoden) mit den Einstellungen Anzahl + WLAN-only.
- Cache-Dienste: `ElementCacheService` (Startseiten-Listen), Vorschaubild-Cache, Episodenlisten-Cache.

### UI / Navigation

- Shell-Navigation mit Hamburgermenü (`AppShell` mit `FlyoutBehavior="Flyout"`): Einträge „Startseite", „Quellen" (freigegebene Medienbereiche, API `api/Sources`), „Favoriten", „Downloads", „Profil", „Einstellungen".
- Pages nach Konvention `*Page.xaml`: `HomePage` (horizontale Listen „Weiterschauen", „Favoriten", „Neu im Programm" via `MediaCarousel`), Quellenübersicht (Referenz `SourceOverviewPage`), `Favoriten`-Seite, `Downloads`-Seite, `Profil`-Seite, `SettingsPage` (inkl. „Server wechseln"), `ServerSetupPage` (Erststart-Dialog mit Servereingabe + gefundenen Servern), Detailseiten (`TVShowDetailsPage`, `MovieCollectionDetailsPage`), `LoginPage`, `LoadingPage`.
- Player-Komponente: `MediaBannerPlayer` / `MediaElement` mit PiP.
- Wiedergabe-Fortschritt: Erfassung + Outbox für Offline-Meldungen (Referenz: `ReportPlaybackProgressAsync` mit Coalescing im Client).
- MVU-Architektur: State-/Model-/Update-/View-Struktur neu (die vorhandenen `*ViewModel`-Klassen folgen MVVM — Abweichung, s. Offene Fragen).

### Tests (`VideoWebPlayer.Maui.Tests`)

- xUnit-Tests (`[Fact]`, `[Theory]`); vorhandenes Muster: Contract-Tests, die Quelltexte prüfen (`MauiSettingsContractTests`), und Verhaltenstests (`ContinueWatchingEventIngressTests`).
- Zu ergänzen: Tests für Backup-Klassifikation, Wiederherstellungs-/Reconnect-Workflow, Offline-Sync der Fortschrittsdaten, Download-Verwaltung (Wiederaufnahme, Kontingent, Auto-Löschung), Serververwaltung/Erststart.

## Implementierungsansatz (grob)

1. **Grundlage**: Bestehende iOS-App wird verworfen; die Implementierung der bisherigen App wird nicht berücksichtigt. *(Annahme: Die im Repository vorhandene `VideoWebPlayer.Maui`-Struktur dient nur als Namens-/Konventionsreferenz und wird im Rahmen der Neugestaltung ersetzt bzw. neu aufgebaut — siehe Offene Fragen.)*
2. **Designvorgabe**: Zu Beginn ist ein Google-Stitch-Designentwurf bereitzustellen/anzufordern; er definiert Navigation, Layout, Farbkonzept und Interaktionsmuster. Die Umsetzung orientiert sich daran und an der Webanwendung.
3. **Architektur**: MVU (Model–View–Update) für die App-Struktur; plattformübergreifend für iOS und Windows, Android-fähig halten. `CommunityToolkit.Maui.MediaElement` als Player mit PiP.
4. **Start-Workflow**: `LoadingPage` → automatischer Verbindungsversuch zum zuletzt verbundenen Server → bei Erfolg Login (gespeicherte Credentials via `SecureStorage`) → Metadaten-Sync; bei Nichterreichbarkeit Offline-Modus (falls Offline-Daten vorhanden) oder Serverauswahl. Beim Erststart `ServerSetupPage` mit manueller Eingabe und automatischer Erkennung (Broadcast/Multicast).
5. **Serververwaltung**: Einstellungen-Eintrag „Server wechseln" → Liste bekannter Server + Hinzufügen neuer Server; Feature bewusst unauffällig; zuletzt verbundener Server wird persistiert und beim Start automatisch verwendet.
6. **Datenhaltung**: Ablageorte so strukturieren, dass backup-pflichtige Daten (Serverliste, letzter Server, Auth/Profil, Einstellungen, ggf. Favoriten, unsynchronisierte Fortschritte) von nicht-backup-pflichtigen Daten (Offline-Videos, Vorschaubilder, Startseiten-Metadaten, Episodenlisten, Download-Manifest, Temp-Cache) getrennt sind; Nicht-Backup-Daten plattformspezifisch aus dem Cloud-Backup ausschließen.
7. **Wiedergabe**: `MediaElement` mit PiP; Fortschrittsmeldung über `api/continue-watching/progress`; offline gesammelte Meldungen in lokaler Outbox zwischenspeichern und bei Online-Rückkehr nachsynchronisieren; „als angesehen" bei Erreichen des Endes mit konfigurierbarem Zeitfenster; Auto-Fortsetzung mit an/aus-Schalter und konfigurierbarem Zeitfenster.
8. **Downloads**: Verwaltung über `DownloadQueue`/`DownloadManager` — parallel, wiederaufnehmbar, mit Fehlerbehandlung, Speicherplatzkontingent und optionaler Auto-Löschung gesehener Episoden; automatische Downloads nach Einstellung (Anzahl, WLAN-only via Netzwerkstatus).
9. **Offlinebetrieb**: Startseite aus Cache („Weiterschauen" auf Basis lokaler Downloads, fehlende Episoden als Platzhalter), Downloads abspielbar, Favoriten lokal; eingeschränkt: keine neuen Inhalte, keine Quellenliste, kein Profilabruf.
10. **Netzwerk**: Online/Offline-Erkennung; bei Rückkehr online automatischer Reconnect zum Server (inkl. `SignalRService`) und automatische Synchronisierung.
11. **Fehlerbehandlung**: Server nicht erreichbar → Offline-Modus; fehlerhafte Serverantwort → definierte Retry-Strategie; abgebrochene Downloads → automatische Wiederaufnahme; nicht verfügbare Videoquelle → Fehlermeldung + Rückfall auf nächste Episode; beschädigte lokale Daten → Verhalten zu definieren.
12. **Serverbezug**: Nur öffentliche API; fehlende API-Möglichkeiten führen zu Issues im Server-Repository statt zu Workarounds in der App. Test gegen das Webprojekt im lokalen IIS (Webseite „VideoPlayer").

## Konfiguration

Benutzerspezifische App-Einstellungen (persistiert, im Cloud-Backup enthalten; Ablage nach Konvention via `ISettingsService`/`Preferences`):

- Automatische Downloads: an/aus
- Anzahl vorzuladender Episoden (Zahl)
- Auto-Fortsetzung: an/aus
- Zeitfenster für Auto-Fortsetzung nach manuellem Start (konfigurierbar)
- WLAN-only Downloads: an/aus
- Zeitfenster „als angesehen" am Videoende (konfigurierbar)
- Speicherplatzkontingent für Downloads
- Automatische Löschung gesehener Episoden: optional/an/aus

Nicht-benutzerbezogene Konfiguration / technische Werte:

- `MauiClientApiToken` (MSBuild-Property) bzw. `VIDEOWEBPLAYER_MAUI_API_TOKEN` (Umgebungsvariable) → Header `X-API-Key` (`MauiClientConfiguration`), Backend-Pendant `Jwt:ApiToken:Maui`.
- Discovery-Parameter: mDNS-Service (`_http._tcp.local.`), UDP-Port 5001, Timeouts *(aus Bestandscode; Annahme: Übernahme bzw. serverseitige Abstimmung)*.
- `VIDEOWEBPLAYER_PROFILE` (Umgebungsvariable) für Profil-Isolation (`ProfileManager`).
- Retry-Strategie-Parameter (Anzahl/Intervalle) — Ausgestaltung offen.

## Akzeptanzkriterien

1. Die App ist eine Neuentwicklung ohne Übernahme der bisherigen App-Implementierung und nutzt ausschließlich die öffentliche API des Serverprojekts.
2. UI und Bedienung entsprechen dem Google-Stitch-Designentwurf und sind der Webanwendung möglichst gleich.
3. Die App läuft auf iOS und Windows; die Struktur erlaubt die spätere Erweiterung um Android. Architektur ist MVU; Player ist `CommunityToolkit.Maui.MediaElement`; PiP ist verfügbar.
4. Backup-Trennung ist umgesetzt: Serverliste, letzter Server, Profil-/Auth-Daten (sofern zulässig), App-Einstellungen, ggf. Favoriten und unsynchronisierte Fortschritte sind im Cloud-Backup; Offline-Videos, Vorschaubilder, Startseiten-Metadaten, Episodenlisten, Download-Manifest und Temp-Cache sind ausgeschlossen.
5. Nach Wiederherstellung verbindet sich die App automatisch mit dem zuletzt genutzten Server; bei Nichterreichbarkeit wechselt sie in den Offline-Modus (falls Daten vorhanden) oder zur Serverauswahl; anschließend erfolgen vollständiger Metadaten-Sync, Nachsenden unsynchronisierter Fortschritte und Aktualisierung der Weiterschauen-Liste.
6. Online/Offline-Wechsel werden erkannt; Reconnect und Synchronisierung erfolgen automatisch bei Online-Rückkehr.
7. Mehrere Server können gespeichert werden; „Server wechseln" in den Einstellungen zeigt bekannte Server und erlaubt das Hinzufügen neuer; beim Start wird der zuletzt verbundene Server automatisch verwendet.
8. Beim Erststart erscheint ein Dialog zur Servereingabe mit automatischer Servererkennung via Broadcast/Multicast, Auswahl aus gefundenen Servern und Speicherung der erfolgreichen Verbindung.
9. Das Hamburgermenü enthält: Startseite, Quellen, Favoriten, Downloads, Profil, Einstellungen. Die Startseite zeigt horizontale Listen „Weiterschauen", „Favoriten", „Neu im Programm", alle lokal gecacht.
10. Wiedergabe: Auto-Fortsetzung mit an/aus-Schalter und konfigurierbarem Zeitfenster; „als angesehen" am Ende mit konfigurierbarem Zeitfenster; Fortschrittsmeldungen werden offline gesammelt und später synchronisiert.
11. Automatische Downloads sind aktivierbar, Anzahl und WLAN-only konfigurierbar; Downloadverwaltung unterstützt parallele Downloads, automatische Wiederaufnahme, Fehlerbehandlung, Speicherplatzkontingent und optionale Auto-Löschung gesehener Episoden.
12. Offlinebetrieb: Startseite aus Cache, Downloads abspielbar, Favoriten lokal verfügbar; keine neuen Inhalte, keine Quellenliste, kein Profilabruf; fehlende Episoden werden als Platzhalter dargestellt.
13. Fehlerszenarien greifen: Server nicht erreichbar → Offline-Modus; fehlerhafte Antworten → definierte Retry-Strategie; abgebrochene Downloads → Wiederaufnahme; nicht verfügbare Videoquelle → Fehlermeldung + nächste Episode; beschädigte lokale Daten → definiertes Verhalten.
14. Die App ist gegen die API des im lokalen IIS eingerichteten Webprojekts „VideoPlayer" testbar.
15. API-Lücken werden nicht per Workaround umgangen; Bedarf wird als Issue im Server-Repository gemeldet.

## Explizite Nicht-Ziele

- Keine Änderungen am Serverprojekt in diesem Repository; Server-Anpassungen erfolgen ausschließlich als separate Issues im Server-Repository.
- Keine Implementierung von Workarounds für unzureichende API-Gegebenheiten in der App.
- Keine Wiederverwendung/Portierung der Implementierung der bisherigen iOS-App.
- Android ist nicht Teil der ersten Ausbaustufe (nur vorgehaltene Erweiterbarkeit).
- Keine Funktionen über den genannten Umfang hinaus (z. B. keine eigenen Server-Verwaltungsfunktionen, keine Medienbearbeitung, keine Inhalte außerhalb der Server-API).

## Offene Fragen

1. **Bestandscode im Repository**: Die Anforderung verwirft die bestehende iOS-App. Ist der vorhandene `VideoWebPlayer.Maui`-Code in diesem Repository als diese alte Implementierung zu verstehen und vollständig zu ersetzen, oder darf er als Startbasis/Referenz dienen?
2. **MVU-Framework**: MVU ist gefordert, der vorhandene Code folgt MVVM (`*ViewModel`). Welches MVU-Framework bzw. welcher Ansatz ist zu verwenden (z. B. `Fabulous`, `Comet`, eigener MVU-Zyklus)?
3. **PiP auf Windows**: PiP ist Pflicht — welche konkrete PiP-Ausprägung ist auf Windows gefordert (z. B. kompaktes Overlay-Fenster), und genügt die `MediaElement`-Unterstützung?
4. **„Zeitfenster für Auto-Fortsetzung nach manuellem Start"**: Was genau ist gemeint — innerhalb welchen Zeitfensters nach manuellem Start soll die Auto-Fortsetzung greifen (z. B. nur wenn manuell gestartetes Video kürzer als X gespielt wurde)?
5. **Konfigurierbarkeit der Zeitfenster**: Sind „Zeitfenster für Auto-Fortsetzung" und „Zeitfenster als angesehen" benutzerseitige Einstellungen in der App oder technische Konstanten?
6. **Favoriten-Speicherort**: Sind Favoriten ausschließlich serverseitig (API `api/favorites` existiert) oder teilweise lokal? Davon hängt ab, ob sie ins Backup gehören.
7. **Auth-Daten im Backup**: „sofern zulässig" — sollen Passwort/Token (`SecureStorage`/Keychain) ins Backup einbezogen werden oder nur das Profil ohne Geheimnisse? Plattformabhängig ist die Keychain-Sicherung eingeschränkt.
8. **Servererkennung**: Welches Discovery-Protokoll beantwortet der Server tatsächlich (mDNS-Service-Typ, UDP-Broadcast-Port/Payload)? Fehlt ein Discovery-Endpunkt serverseitig, ist ein Issue im Server-Repository zu stellen.
9. **Cloud-Backup auf Windows**: Cloud-Backup ist ein iOS-Konzept — gibt es eine Windows-Entsprechung (z. B. Roaming) oder gilt die Backup-Strategie nur für iOS?
10. **Speicherplatzkontingent**: Welcher Standardwert/welche Einheit (MB/GB) und ist das Kontingent benutzerkonfigurierbar?
11. **„Profil"-Menüeintrag**: Was zeigt die Profil-Seite — Server-Benutzerprofil, lokale Profile (`ProfileManager`) oder beides?
12. **Beschädigte lokale Daten**: Welches Verhalten ist gewünscht (z. B. Cache verwerfen und neu synchronisieren, Neuinitialisierung der Datenbank, Nutzerdialog)?
13. **Retry-Strategie**: Parameter (Anzahl Versuche, Backoff, Abbruchbedingungen) sind zu definieren.
14. **Google-Stitch-Designentwurf**: Wer stellt den Entwurf bereit und in welcher Form liegt er vor? Er ist Voraussetzung zu Beginn der Umsetzung.
15. **„Neu im Programm"**: Entspricht dies dem Endpunkt `api/items/recent` *(Annahme: ja)*?
16. **Weiterschauen im Offlinebetrieb**: Soll „Weiterschauen" offline ausschließlich aus lokalen Downloads gebildet werden, auch wenn gecachte Server-Fortschritte vorliegen?
