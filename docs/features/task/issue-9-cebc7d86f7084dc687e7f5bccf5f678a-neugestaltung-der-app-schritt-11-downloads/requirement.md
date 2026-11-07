# Übersetzte Anforderung: Download-Verwaltung und automatische Downloads (Entwicklungsschritt 11)

## Fachliche Zusammenfassung

Die MVU-App `VideoPlayer.Maui` erhält eine vollwertige Download-Verwaltung
für den Offlinebetrieb. Medien (Filme `movie`, Episoden `tvshowepisode`)
können vom Server-Stream `api/items/{typ}/{id}/stream` heruntergeladen
werden; der Bestand wird in der nicht backup-pflichtigen
`download_manifest`-Tabelle (`DownloadManifestEntry`, `CacheDatabase`)
geführt und die Dateien liegen im nicht backup-pflichtigen
`AppStorageLayout.VideosDirectory`. Die Downloads-Seite (bisher
Platzhalter, `AppPage.Downloads`, Route `downloads`) listet alle
Downloads mit Titel, Status und Fortschritt und bietet Abbrechen- und
Löschen-Aktionen. Die Verwaltung unterstützt parallele Downloads, die
automatische Wiederaufnahme abgebrochener Downloads (prozessübergreifend
über das persistente Manifest) und eine definierte Fehlerbehandlung.

Das benutzerkonfigurierbare Speicherplatzkontingent
(`AppSettings.DownloadQuotaGb`, GB, Default 5) begrenzt den belegten
Speicher; vor dem Download-Start wird geprüft und bei Überschreitung mit
Fehler abgelehnt. Optional werden als gesehen markierte Episoden
automatisch gelöscht (`AppSettings.AutoDeleteWatchedEpisodes` — Kopplung
an das „angesehen"-Ereignis der Wiedergabe aus Schritt 10). Automatische
Downloads (`AppSettings.AutoDownloadEnabled`) laden die nächsten Episoden
der Weiterschauen-Einträge vor (`AppSettings.PreloadEpisodeCount`,
Serienkontext-Mechanik aus Schritt 8/10), optional beschränkt auf
WLAN-Verbindungen (`AppSettings.WifiOnlyDownloads` —
`ConnectionProfile.WiFi` über `Connectivity` am App-Rand).

Heruntergeladene Videos sind über die vorhandene Wiedergabe abspielbar —
`PlaybackSourceService` (Schritt 10) bevorzugt bereits fertige
Manifest-Einträge (`State == "completed"` + vorhandene Datei) vor dem
Server-Stream; es muss nur das Manifest gespeist werden.

## Betroffene Klassen und Komponenten

- **MVU-Kern (`VideoPlayer.Mvu`, plattformneutral, testbar):**
  - `AppModel` — neuer Zustand `Downloads` (`DownloadsPageState`): Liste
    der `DownloadListItem`s, Lade-/Fehlerzustand
  - `AppMessage` — neue Messages: Downloads-Seite geöffnet, Download
    angefordert/abgebrochen/gelöscht, Download-Fortschritt/-Abschluss/
    -Fehler (Rückmeldungen des Download-Dienstes), Auto-Download-Lauf
    angestoßen, „Episode gesehen" (Auto-Löschung)
  - `AppEffect` — neue Effekte: Download einreihen
    (`EnqueueDownloadEffect`), Download abbrechen
    (`CancelDownloadEffect`), Download löschen (`DeleteDownloadEffect`),
    Downloads laden (`LoadDownloadsEffect`), Auto-Download-Lauf
    (`RunAutoDownloadsEffect`), gesehene Episode löschen
    (`DeleteWatchedDownloadEffect` o. ä.)
  - `AppUpdate` — Übergänge der Download-Messages inkl. Quota-Ablehnung,
    Auto-Download-Anstoß (nach Sync bzw. Continue-Watching-Ereignis) und
    Auto-Lösch-Kopplung an `PlaybackEndedMessage`/`IsWatched`
  - Neue Records/Enums: `DownloadsPageState`, `DownloadListItem`
    (Titel, Status, Fortschritt, Aktionen), `DownloadState`
    (Queued/Downloading/Completed/Failed — Werte landen als String im
    `DownloadManifestEntry.State`)
  - Neue Dienste (Service Layer, plattformneutral):
    - `DownloadManager`/`DownloadService` — Kern der Verwaltung:
      Manifest CRUD, Einreihen, Abbrechen, Löschen, Quota-Prüfung,
      Wiederaufnahme (Neustart des Downloads — der Server-Stream
      unterstützt keine Range-Fortsetzung, Altbestand lädt stets neu),
      parallele Ausführung mit konfigurierbarem Grad
    - `AutoDownloadService`/`DownloadCoordinator` — ermittelt die
      nächsten Episoden der Weiterschauen-Einträge (Serienkontext über
      `GetTvShowAsync` wie `MediaDetailsService`/`NextEpisodeQueue`) und
      reiht sie unter Beachtung von Anzahl- und WLAN-only-Einstellung ein
  - `INetworkStatusMonitor` — ggf. Erweiterung um WLAN-Erkennung
    (`ConnectionProfile.WiFi`) — die Abstraktion muss plattformneutral
    bleiben
- **App-Rand (`VideoPlayer.Maui`):**
  - `Pages/DownloadsPage.xaml(.cs)` — vollwertige Liste: Titel, Status
    (Wartend/Läuft/Fehler/Fertig), Fortschritt in %, Aktionen
    Abbrechen/Löschen; Belegung über `MvuHost.Model.Downloads`
  - `Pages/MediaDetailsPage.xaml(.cs)` — „Herunterladen"-Schaltfläche je
    abspielbarem Medium (Film/Episode) sowie je Episoden-Eintrag
  - `Services/ShellAppEffectHandler.cs` — Ausführung der neuen Effekte
    (Download-Dienst aufrufen, Fortschritts-/Abschluss-Messages in den
    MVU-Zyklus dispatcht)
  - `Services/ConnectivityNetworkStatusMonitor.cs` — WLAN-Erkennung
    (`Connectivity.Current.ConnectionProfiles` enthält
    `ConnectionProfile.WiFi`)
  - `MauiProgram` — DI-Registrierung der Download-Dienste
- **Ablage (`VideoPlayer.Storage`):**
  - `DownloadManifestEntry` — Erweiterung um Titel/Fortschritt/Restlauf-
    Informationen (mindestens `Title`, `DownloadedBytes`, `TotalBytes`,
    Fehlertext); Tabelle/Schema-Anschluss an `CacheDatabase.InitializeAsync`
  - `AppStorageLayout.VideosDirectory` — Zielverzeichnis existiert
- **Tests (`VideoWebPlayer.Maui.Tests`):**
  - MVU-Übergangstests: Einreihen/Abbrechen/Löschen, Fortschritts- und
    Abschluss-Messages, Quota-Ablehnung, Auto-Download-Anstoß (Schalter,
    Anzahl, WLAN-only), Auto-Löschung gesehener Episoden, Offline-Guards
  - Service-Tests: Download-Dienst (Manifest, Fortschritt, Fehler,
    Wiederaufnahme, Parallelität, Quota), Auto-Download-Ermittlung
    (nächste Episoden aus Serienkontext)
  - Enum-Abdeckung der neuen Enum-Werte (Hook-Vorgabe)
  - Contract-Tests: DownloadsPage-Struktur, DI-Registrierungen

## Implementierungsansatz

- Download-Auslösung: „Herunterladen" auf der Detailseite dispatcht eine
  Download-Anforderungs-Message; `AppUpdate` prüft Typ/Id und erzeugt
  einen `EnqueueDownloadEffect`; der Effekt-Handler übergibt an den
  Download-Dienst (Quota-Prüfung, Manifest-Eintrag, Start).
- Der Download-Dienst läuft im Service Layer (Singleton im DI);
  Fortschritt/Abschluss/Fehler werden als Messages in den MVU-Zyklus
  zurückgemeldet (Muster `ServerEventsService` → `DispatchMessageAsync`).
- Parallele Downloads: interne `SemaphoreSlim`-Begrenzung (z. B. 2–3
  parallele Transfers); die Abarbeitung ist persistent — jeder Auftrag
  steht als Manifest-Eintrag (`queued`/`downloading`), sodass ein
  App-Neustart aus dem Manifest fortsetzt (Resume = Neustart des
  Downloads mit erneuter Auftragsaufnahme; kein Range-Support nach
  Altbestand-Befund).
- Auto-Downloads: nach abgeschlossener Synchronisation bzw. nach einem
  Continue-Watching-Ereignis wird — bei aktiviertem Schalter und (falls
  WLAN-only) WLAN-Verbindung — der Lauf angestoßen; die Ermittlung nutzt
  die Weiterschauen-Liste (Cache) und den Serienkontext
  (`GetTvShowAsync`) für die nächsten `PreloadEpisodeCount` Episoden.
- Auto-Löschung: beim Setzen von `IsWatched` (Episoden, Schritt-10-Stelle
  in `UpdatePlaybackProgress`/`UpdatePlaybackEnded`) wird bei aktiviertem
  Schalter ein Lösch-Effekt erzeugt.
- Downloads-Seite: `DownloadsPageOpenedMessage` löst `LoadDownloadsEffect`
  aus; Fortschritts-Messages aktualisieren die Liste live.

## Konfiguration

Bereits vorhandene Einstellungen (Schritt 3, `AppSettings` +
Einstellungsseite): `AutoDownloadEnabled`, `PreloadEpisodeCount` (1–10),
`WifiOnlyDownloads`, `DownloadQuotaGb` (1–500, Standard 5 GB),
`AutoDeleteWatchedEpisodes`. Keine neuen Einstellungen erforderlich.

## Offene Fragen

- Keine — der verbindliche Kontext legt Quelle (Stream-URL), Manifest,
  Quota-Default und Einstellungen fest; der fehlende Range-Support des
  Servers ist als dokumentierte Einschränkung zu vermerken (Resume =
  Neustart des Downloads).
