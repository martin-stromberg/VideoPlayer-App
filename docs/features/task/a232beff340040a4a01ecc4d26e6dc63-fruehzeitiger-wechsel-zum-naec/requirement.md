# Übersetzte Anforderung: Frühzeitiger Wechsel zum nächsten Video trotz Seek-Fix (zweiter Vorfall)

Branch: `task/a232beff340040a4a01ecc4d26e6dc63-fruehzeitiger-wechsel-zum-naec`
Quelle: Kundenrückmeldung zum Folgevorfall nach dem ersten Fix
(`e465907` — fehlschlagender Fortsetzungs-Seek löst keinen
Episodenwechsel mehr aus); Protokoll `videoplayer (6).log`
(Repository-Root), Vorfall am 2026-10-18.

## Fachliche Zusammenfassung

Fehlerbehebung im Auto-Fortsetzungspfad der Wiedergabe: Trotz des
installierten Fixes wurde die Episode „Normale Helden"
(`tvshowepisode/21600`, Quelle `ServerStream`, iOS) erneut frühzeitig
beendet und automatisch die Folge-Episode „Die Hochzeit des
Jahrhunderts" (`tvshowepisode/21603`) gestartet. Das Protokoll zeigt
diesmal **keinen** der bisherigen Auslöser: weder „Wiedergabe beendet"
(`OnMediaEnded` → `PlaybackEndedMessage`) noch „Wiedergabe
fehlgeschlagen" (`OnMediaFailed`/`FailPlaybackStart` →
`PlaybackFailedMessage`) noch einen Seek-Fehlschlag der betroffenen
Episode vor dem Wechsel. Einziger verbleibender Wechselpfad ist die
positionsgetriebene Auto-Fortsetzung in `AppUpdate.UpdatePlaybackProgress`:
sobald `Duration - Position <= AutoResumeRemainingSeconds` (Standard
300 s) bei `IsPlaying` gemeldet wird, schaltet die App ohne
Plausibilitätsprüfung über `AdvanceToNextEpisode` bzw.
`AdvancePlaylistEntry` weiter. Die Verdachtsrichtung (zu verifizieren):
Der durch `AVPlayerItemPlaybackStalledNotification` nachweisbar gestörte
Stream hat eine eingefrorene/gesprungene Position oder eine geschrumpfte
Dauer gemeldet und damit ein künstliches „Ende" erzeugt. Dieselbe
Fensterprüfung setzt `IsWatched` (`WatchedEndWindowSeconds`) und löste
die beobachtete Download-Löschung (`AutoDeleteWatchedEpisodes` →
`DeleteWatchedDownloadEffect`) aus — beide Schadwirkungen sind
abzusichern.

Erwartetes Verhalten: Auto-Fortsetzung und „angesehen"-Markierung dürfen
nur bei einem tatsächlich erreichten Wiedergabeende greifen. Einzelne
Takte mit inkonsistenten Position-/Dauer-Meldungen (Stall, Sprung,
geschrumpfte Dauer) dürfen weder den Episodenwechsel noch die
„angesehen"-Markierung/Download-Löschung auslösen — die Wiedergabe läuft
weiter. Als Muster steht die bereits vorhandene Plausibilitätsprüfung
`IsPlaybackEndPlausible` für das `PlaybackEndedMessage`-Ereignis bereit;
dem positionsgetriebenen Takt fehlt ein solches Gegenstück.

## Betroffene Klassen und Komponenten

- **MVU-Kern (`VideoPlayer.Mvu`, plattformneutral):**
  - `AppUpdate.cs` —
    - `UpdatePlaybackProgress` (~Z. 3792–3876): übernimmt
      Position/Dauer/Laufzustand aus `PlaybackProgressMessage`; setzt
      die „angesehen"-Schwelle (Z. 3821–3836,
      `WatchedEndWindowSeconds` → `IsWatched` →
      `DeleteWatchedDownloadEffect`) und prüft die Auto-Fortsetzung
      (Z. 3846–3870, `AutoResumeRemainingSeconds` →
      `AdvancePlaylistEntry`/`AdvanceToNextEpisode`). Hier liegt der zu
      korrigierende Mechanismus.
    - `IsPlaybackEndPlausible` (Z. 3973–3979) und die Konstante
      `PlaybackEndedToleranceSeconds` (Z. 88): vorhandene
      Plausibilitätsprüfung des Ende-Ereignisses — Referenzmuster für
      die neue Absicherung.
    - `UpdatePlaybackEnded` (Z. 3896–3960), `UpdatePlaybackFailed`
      (Z. 4033–4050), `AdvanceToNextEpisode` (Z. 3735–3777),
      `AdvancePlaylistEntry` (Z. 2612–2627),
      `MarkEpisodeWatchedInDetails` (Z. 3994–4031): betroffene
      Abschluss-/Wechselpfade, v. a. für den Playlist-Kontext.
  - `PlayerState.cs` — `Position`, `Duration`, `IsPlaying`,
    `IsBuffering`, `IsWatched`, `UpcomingEpisodes`/`HasNextEpisode`,
    `IsPlaylistPlayback`, `PlaylistNextExhausted`, `IsResolving`,
    `LastReportedPositionSeconds`; ggf. neue Zustandsfelder für eine
    Stabilitäts-/Monotonieprüfung der Positionsmel­dungen (Annahme).
  - `AppMessage.cs` — `PlaybackProgressMessage` (Z. 1003–1007:
    `Position`, `Duration`, `IsPlaying`, `IsBuffering`); ggf. Erweiterung
    oder zusätzliche Meldung (Annahme).
  - `AppEffect.cs` — `ReportPlaybackProgressEffect` (Z. 652),
    `DeleteWatchedDownloadEffect` (Z. 771), `AdvancePlaylistEntryEffect`
    (Z. 406).
- **App-Rand (`VideoPlayer.Maui`):**
  - `Pages/PlayerPage.xaml.cs` — `ReportPosition` (Z. 167–185) als
    Quelle der Position/Dauer-Werte (`VideoElement.Position`,
    `VideoElement.Duration`), `StartProgressTimer` (Z. 133–165) mit
    `ProgressTickInterval` = 5 s (Z. 25), `OnMediaStateChanged`
    (Z. 433–450, zusätzlicher `ReportPosition`-Aufruf bei
    Zustandswechsel), `OnMediaEnded` (Z. 415–422). Ggf. ergänzendes
    Diagnose-Logging der gemeldeten Position/Dauer beim
    Auto-Fortsetzungs-Wechsel (Annahme).
- **Einstellungen (`VideoPlayer.Storage`):**
  - `AppSettings.cs` — `AutoResumeEnabled` (Z. 21),
    `AutoResumeRemainingSeconds` (Z. 27, Default 300, Clamp 30–3600),
    `WatchedEndWindowSeconds` (Z. 36, Default 300),
    `AutoDeleteWatchedEpisodes` (Z. 42).
- **Tests:**
  - `VideoPlayer.Maui.Tests/AppUpdateTests_Player.cs` — Update-Tests des
    Fortschrittstakts / der Auto-Fortsetzung im Serienkontext.
  - `VideoPlayer.Maui.Tests/AppUpdateTests_PlaylistPlayback.cs` —
    Playlist-Pfad (`AdvancePlaylistEntry`).
  - `VideoPlayer.Maui.Tests/AppUpdateTests_Downloads.cs` —
    `AutoDeleteWatchedEpisodes`/`DeleteWatchedDownloadEffect`.
  - `VideoPlayer.Maui.Tests/PlayerContractTests.cs` — ggf. Vertrag für
    neues Diagnose-Logging (Annahme).
  - `VideoPlayer.E2E.Tests` (`PlaybackTests`,
    `ContinueWatchingPlaybackTests`, `PlaylistPlaybackTests`) —
    Regression; ein Stall-/Fehlmeldungs-Szenario ist E2E-seitig
    vermutlich nicht nachstellbar (Annahme, zu prüfen).

## Implementierungsansatz

- **Mechanismus verifizieren:** Das Protokoll enthält keine
  Positions-/Dauer-Werte des Takts — die Fehlkonstellation (Position ≈
  Dauer vs. geschrumpfte `Duration`) ist am Code nicht ablesbar.
  Ergänzendes Diagnose-Logging beim Auto-Fortsetzungs-/IsWatched-Eintritt
  (Position, Dauer, `remaining`, ggf. Verlauf der letzten Takte) ist zu
  erwägen, um den Auslöser belastbar zu belegen (Annahme).
- **Plausibilitätsabsicherung des positionsgetriebenen Wechsels** in
  `UpdatePlaybackProgress`, analog zum Vorbild `IsPlaybackEndPlausible`.
  Kandidaten für das Kriterium (Annahmen — Auswahl in der Planung):
  - Stabilität der gemeldeten `Duration` über mehrere Takte (Sprünge /
    Schrumpfen ignorieren);
  - Positions-Fortschritt/Monotonie: das Fenster muss durch reale
    Wiedergabe erreicht worden sein, nicht durch einen einzelnen
    Ausreißer-Takt;
  - Ausschluss während `IsBuffering`/Stall (die iOS-Stall-Benachrichtig-
    ungen im Protokoll deuten darauf hin, dass `IsPlaying` während eines
    Stalls weiter `true` melden kann);
  - mehrfach bestätigter Fenstereintritt statt einmaligem Treffer.
- **Beide Schadwirkungen absichern:** Die `IsWatched`-Schwelle und die
  Auto-Fortsetzung nutzen dieselbe Fensterlogik — ohne Absicherung der
  `IsWatched`-Seite bliebe die unberechtigte Download-Löschung
  (`AutoDeleteWatchedEpisodes` → `DeleteWatchedDownloadEffect`) bestehen.
- **Playlist-Pfad mitbehandeln:** `AdvancePlaylistEntry` wird über
  denselben Fenstertest ausgelöst; der Fix muss beide Kontexte
  (Serien-Fortsetzungsliste und serverseitige Playlist-Reihenfolge)
  abdecken.
- **Fatalpfade unverändert lassen:** `PlaybackEndedMessage` (bereits
  durch `IsPlaybackEndPlausible` geschützt), `PlaybackFailedMessage`
  (`OnMediaFailed`, `FailPlaybackStart`) und die fehlgeschlagene
  Quellauflösung behalten ihre Semantik.

## Konfiguration

Keine neue Benutzereinstellung erkennbar. Die bestehenden
anwendungsweiten Schalter `AutoResumeEnabled`,
`AutoResumeRemainingSeconds`, `WatchedEndWindowSeconds` und
`AutoDeleteWatchedEpisodes` (`AppSettings`, benutzerseitig in der
`SettingsPage`) bleiben unverändert — ihr jeweiliges Fenster ist weiter
wirksam, nur der Eintritt wird plausibilisiert. Neue interne
Schwellen-/Toleranzwerte sind als Konstanten im Muster von
`PlaybackEndedToleranceSeconds` vorzusehen (Annahme).

## Offene Fragen

- **Auslöser-Konstellation:** Meldete der gestörte Stream `Position ==
  Duration` oder eine geschrumpfte `Duration` — oder lag die reale
  Restlaufzeit zum Wechselzeitpunkt (13:42:48, nach ~18:24 min
  Wiedergabe ab Fortsetzung `null`) tatsächlich bereits innerhalb des
  300-s-Fensters (legitimer, aber vom Anwender als „frühzeitig"
  empfundener Abspann-Vorlauf)? Die tatsächliche Episodenlaufzeit von
  `tvshowepisode/21600` ist zu klären; ohne Diagnose-Daten bleibt die
  Unterscheidung „künstliches Ende" vs. „korrekter Fenstereintritt"
  offen.
- Soll zusätzliches Diagnose-Logging in `UpdatePlaybackProgress` bzw.
  `ReportPosition` eingeführt werden (Position/Dauer/`remaining` beim
  Fenstereintritt), um künftige Vorfälle belegen zu können?
- Soll die Fensterprüfung bei `IsBuffering`/Stream-Stall ausgesetzt
  werden, und ist der Stall-Zustand (`AVPlayerItemPlaybackStalledNotification`)
  plattformübergreifend überhaupt am `PlaybackProgressMessage`
  ablesbar (nur `IsBuffering` vorhanden)?
- Soll die Auto-Fortsetzung weiterhin rein positionsgetrieben bleiben
  (nur härter abgesichert) oder zusätzlich an das Ende-Ereignis
  `PlaybackEndedMessage` gekoppelt werden? Letzteres würde das
  konfigurierbare Vorlauf-Fenster `AutoResumeRemainingSeconds`
  faktisch entwerten — Architektur-Entscheidung mit Kunde abzustimmen.
- Gilt die Plausibilisierung gleichermaßen für `IsWatched`/
  Download-Löschung wie für den Episodenwechsel, oder soll
  `AutoDeleteWatchedEpisodes` separat abgesichert werden?
- Plattformbezug: Der Vorfall trat auf iOS auf (AVPlayer-Stall-
  Benachrichtigungen). Ist das Verhalten der Positions-/Dauer-Meldung
  bei Stalls auf Windows/Android vergleichbar, und soll der Fix
  plattformneutral im MVU-Kern greifen (Annahme: ja)?
