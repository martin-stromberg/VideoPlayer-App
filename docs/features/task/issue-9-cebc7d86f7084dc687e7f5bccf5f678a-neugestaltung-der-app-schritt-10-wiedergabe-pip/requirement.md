# Übersetzte Anforderung: Videowiedergabe mit Picture-in-Picture und Fortschrittsverfolgung (Entwicklungsschritt 10)

## Fachliche Zusammenfassung

Die MVU-App `VideoPlayer.Maui` erhält eine eigene Wiedergabeseite
(`PlayerPage`, Shell-Route `player`), die über das `MediaElement` der
Komponente `CommunityToolkit.Maui.MediaElement` Videos wiedergibt —
sowohl Server-Streams (`api/items/{typ}/{id}/stream` mit Token als
Query-Parameter, Muster des Altbestands `DownloadManager.BuildStreamUrl`)
als auch lokale Dateien (Quellenabstraktion, die Schritt 11 um
Downloads erweitert). Verpflichtend ist Picture-in-Picture auf beiden
Zielplattformen: auf iOS über den im `AVPlayerViewController`
eingebauten PiP-Modus der Plattform-Transportsteuerung, auf Windows als
kompaktes Overlay-/Mini-Player-Fenster über den
`CompactOverlay`-Presenter des App-Fensters.

Beim Start der Wiedergabe wird eine vorhandene Fortsetzungsposition
(Weiterschauen-Stand des Servers) berücksichtigt. Während der Wiedergabe
meldet die App den Fortschritt periodisch über den bestehenden
`ProgressReportService` an den Server; bei Offline oder überholbarem
Fehlschlag landen die Meldungen in der persistenten Warteschlange
`pending_progress` und werden bei Online-Rückkehr durch den bestehenden
Synchronisationslauf nachgesendet. Erreicht die Position das Ende
abzüglich des eingestellten Zeitfensters
(`AppSettings.WatchedEndWindowSeconds`), gilt das Medium als
„angesehen". Ist die benutzerseitige Auto-Fortsetzung aktiviert
(`AppSettings.AutoResumeEnabled`) und unterschreitet die Restlaufzeit
das konfigurierte Fenster
(`AppSettings.AutoResumeRemainingSeconds`), startet automatisch die
nächste Episode (Serienkontext aus `MediaDetailsState`). Ist eine
Videoquelle nicht verfügbar, erscheint eine Fehlermeldung und die App
fällt auf die nächste Episode zurück.

## Betroffene Klassen und Komponenten

- **MVU-Kern (`VideoPlayer.Mvu`, plattformneutral, testbar):**
  - `AppModel` — neuer Zustand `Player` (`PlayerState`): Medium,
    Quelle, Position, Dauer, Pufferung, „angesehen"-Flag, nächste
    Episode, Fehlertext
  - `AppMessage` — neue Messages: Wiedergabe angefordert, Player-Seite
    geöffnet, Quelle aufgelöst/auflösbar-fehlgeschlagen,
    Positions-Update der Wiedergabe (Takt der Seite), Medium zu
    Ende/Wiedergabe fehlgeschlagen, PiP-Umschaltung angefordert,
    Zurück/Beenden der Wiedergabe
  - `AppEffect` — neue Effekte: zur Player-Seite navigieren
    (`NavigateToPlayerEffect`), Wiedergabequelle auflösen
    (`ResolvePlaybackSourceEffect`), Fortschritt melden
    (`ReportPlaybackProgressEffect`), PiP umschalten
    (`TogglePictureInPictureEffect`), auf nächste Episode zurückfallen
  - `AppUpdate` — Übergänge der Player-Messages inkl. „angesehen"-
    Schwelle, Auto-Fortsetzungs-Prüfung und Fehler-Rückfall
  - Neue Records/Enums: `PlayerState`, `PlaybackSource`
    (`PlaybackSourceKind`: `ServerStream`, `LocalFile`),
    `PlayerEpisodeRef` (nächste Episode: Id/Titel)
  - Neuer Dienst `PlaybackSourceService` — löst die Quelle eines
    Mediums auf (Stream-URL aus `AuthSessionService.ClientBaseAddress`
    + Token via `AppSecretsService.GetAuthTokenAsync`,
    Fortsetzungsposition aus der Weiterschauen-Liste des Servers bzw.
    des Caches); Schritt 11 liefert zusätzlich lokale Dateien
- **App-Rand (`VideoPlayer.Maui`):**
  - `Pages/PlayerPage.xaml(.cs)` — neue Seite mit `MediaElement`
    (`ShouldAutoPlay`, `ShouldShowPlaybackControls`), Titel-/Status-
    Anzeige, Fehleranzeige, PiP-Schalter; Positions-Takt (Timer) und
    `MediaEnded`/`MediaFailed`-Ereignisse dispatcht Messages
  - `AppShell.xaml.cs` — Route `player` registrieren (geschoben auf
    den Stack, ohne Menüeintrag)
  - `Pages/MediaDetailsPage.xaml(.cs)` — „Abspielen"-Schaltfläche je
    abspielbarem Medium (Film/Episode), dispatcht
    `PlayRequestedMessage` mit dem Episodenkontext der Detailseite
  - `Services/PictureInPictureService` — plattformbezogene
    Implementierung: Windows schaltet `AppWindow.Presenter` auf
    `CompactOverlay` (Mini-Player) und zurück; iOS nutzt den nativen
    PiP-Modus des `AVPlayerViewController` (plattformseitig über die
    Transportsteuerung)
  - `Platforms/iOS/Info.plist` — `UIBackgroundModes` `audio` für PiP
  - `MauiProgram` — `UseMauiCommunityToolkitMediaElement()` + DI
  - `VideoPlayer.Maui.csproj` — NuGet `CommunityToolkit.Maui.MediaElement`
    (stabile Version 10.0.0, bereits vom Altbestand genutzt)
- **Ablage/Server (`VideoPlayer.Storage`/`VideoPlayer.ApiClient`):**
  - keine Schema-Änderung — `pending_progress`,
    `ProgressReportService`, `AppSettings` (`AutoResumeEnabledKey`,
    `AutoResumeRemainingSecondsKey`, `WatchedEndWindowSecondsKey`)
    existieren
- **Tests (`VideoWebPlayer.Maui.Tests`):**
  - MVU-Übergangstests: Wiedergabe-Start (Route + Quellauflösung),
    Fortschritts-Takt (Meldungseffekt), „angesehen"-Schwelle,
    Auto-Fortsetzung (Schalter/Fenster/keine nächste Episode),
    Fehler-Rückfall auf nächste Episode, PiP-Umschaltung
  - Service-Tests: `PlaybackSourceService` (Stream-URL inkl.
    Token-Parameter, Fortsetzungsposition, lokale Datei)
  - Enum-Abdeckung der neuen Enum-Werte (Hook-Vorgabe)
  - Contract-Tests: Route `player`, MediaElement-Referenz, Info.plist

## Implementierungsansatz

- Wiedergabe-Start: „Abspielen" auf `MediaDetailsPage` (Film direkt,
  Episode aus der Episodenliste oder der hervorgehobenen Episode)
  dispatcht eine Player-Messages; `AppUpdate` bestimmt aus
  `model.MediaDetails.Details` den Serienkontext (sortierte Episoden
  der gewählten Staffel bzw. staffelübergreifend) und die nächste
  Episode, setzt `PlayerState` und erzeugt
  `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect`.
- Die Player-Seite bindet `MediaElement.Source` an die aufgelöste
  `PlaybackSource` (`MediaSource.FromUri` für Streams,
  `MediaSource.FromFile` für lokale Dateien) und springt nach dem
  Metadaten-Stand auf die Fortsetzungsposition (`SeekTo` beim ersten
  `MediaOpened`/State `Playing`).
- Fortschritt: Seiten-Timer (ca. 5 s, Muster `MediaBannerPlayer` des
  Altbestands) dispatcht Positions-Messages; `AppUpdate` erzeugt
  `ReportPlaybackProgressEffect`, den der Effekt-Handler an
  `ProgressReportService.ReportProgressAsync` weiterreicht (online
  sofort, offline über die Warteschlange — beides bereits vorhanden).
- „Angesehen"/Auto-Fortsetzung/Fehler-Rückfall sind reine
  Update-Logik über `model.Settings` (geladen über
  `LoadSettingsEffect` beim Start).
- PiP: iOS nativ über `AVPlayerViewController`
  (`AllowsPictureInPicturePlayback` standardmäßig aktiv; Voraussetzung
  `UIBackgroundModes=audio`); Windows über
  `AppWindow.SetPresenter(CompactOverlay)` bzw. Rückkehr zu
  `Default` — als plattformbezogener Dienst hinter einem
  `IPictureInPictureService`-Interface am App-Rand.

## Konfiguration

Benutzerseitig bereits vorhanden (Einstellungsseite, Schritt 3):
`AutoResumeEnabled` (Schalter Auto-Fortsetzung),
`AutoResumeRemainingSeconds` (Zeitfenster Auto-Fortsetzung),
`WatchedEndWindowSeconds` (Zeitfenster „angesehen"). Keine neuen
Einstellungen.

## Offene Fragen

Keine — die PiP-Machbarkeit ist in diesem Schritt früh zu verifizieren
(im Plan dokumentiert): iOS über `AVPlayerViewController` (Toolkit
nutzt ihn mit Transportsteuerung; `UIBackgroundModes=audio` ist
nachzurüsten), Windows über `AppWindow`-`CompactOverlay` (WinAppSDK,
auch für unverpackte Apps verfügbar — `WindowsPackageType=None`).
