# Umsetzungsplan: Frühzeitiger Wechsel zum nächsten Video trotz Seek-Fix (zweiter Vorfall)

## Übersicht

Der positionsgetriebene Endfenster-Eintritt in `AppUpdate.UpdatePlaybackProgress` (`VideoPlayer.Mvu/AppUpdate.cs`, Z. 3792–3876) wird plausibilisiert: Ein einzelner Takt mit inkonsistenten Positions-/Dauer-Meldungen (eingefrorene Position bei gemeldetem `IsPlaying`, geschrumpfte `Duration`, Sprung ans Ende) darf weder die Auto-Fortsetzung (`AdvancePlaylistEntry`/`AdvanceToNextEpisode`) noch die „angesehen"-Markierung samt Download-Löschung (`DeleteWatchedDownloadEffect`) auslösen. Ergänzend wird der Takt per `MediaId` an das aktuell angewendete Medium gebunden und die Fensterlogik während `IsResolving` gesperrt, damit nachlaufende Takte des Vorgängermediums nach einem Wechsel die Folge-Episode nicht beeinträchtigen können. Betroffen sind der MVU-Kern (`AppUpdate`, `PlayerState`, `PlaybackProgressMessage`), der App-Rand (`PlayerPage`) sowie die Update- und Contract-Tests.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Takt-Plausibilität | Neue private Hilfsmethode `IsProgressTickPlausible(PlayerState, PlaybackProgressMessage)` in `AppUpdate` — fachlich eine *Specification* im Muster der bestehenden `IsPlaybackEndPlausible` (Z. 3973–3979) | Setzt das etablierte Plausibilitätsmuster fort; prüft die Nachricht gegen den Vorgänger-Zustand (`player.Position`/`player.Duration`), nicht nur gegen sich selbst |
| Endfenster-Bestätigung | Streak-Zähler `EndWindowStreak` (int) auf `PlayerState` + Konstante `EndWindowRequiredTicks = 2` in `AppUpdate` (Muster `PlaybackEndedToleranceSeconds`, Z. 88) | Die Fensterbedingung muss über mindestens 2 aufeinanderfolgende plausible Takte bestätigt sein; die Takt-Historie kann nur im State liegen (`PlayerState` ist die einzige Stelle mit taktübergreifendem Gedächtnis, vgl. `LastReportedPositionSeconds`) |
| Medienidentität des Takts | `PlaybackProgressMessage` wird um `MediaId` erweitert; `PlayerPage` meldet die zuletzt am `MediaElement` angewendete `MediaId` (neues Feld `appliedMediaId` parallel zu `appliedLocation`, Z. 37); ein Takt mit abweichender Id wird in `UpdatePlaybackProgress` vollständig verworfen | Einzige deterministische Abwehr nachlaufender Takte des Vorgängermediums (im Vorfall: Download der Folge-Episode 21603 wurde vor deren Quellauflösung gelöscht). `IsResolving`-Sperre + Streak allein lassen die Lücke zwischen `PlaybackSourceResolved` und dem tatsächlichen Quellenwechsel des Elements offen; die Identitätsbindung verhindert zudem falsch attributierte `ReportPlaybackProgressEffect`-Meldungen an den Server |
| Fensterlogik während `IsResolving` | Während `player.IsResolving` werden Streak-Aufbau, `IsWatched`-Setzung und beide Advance-Zweige ausgesetzt (`EndWindowStreak` wird auf 0 gehalten); der Serienzweig erhält zusätzlich explizit `!updated.IsResolving` | Playlist-Wechsel ändern die `MediaId` erst in `UpdatePlaylistNavigationCompleted` — bis dahin ist die Identitätsbindung wirkungslos, die `IsResolving`-Sperre schließt diese Phase ab. Der Serienzweig prüft `IsResolving` bislang nicht (Inventory-Befund) und wäre ohne Sperre kaskadierbar |
| `IsWatched`-/`DeleteWatchedDownloadEffect`-Kopplung | Dieselbe Plausibilitätsbedingung (Streak ≥ `EndWindowRequiredTicks`) wie die Auto-Fortsetzung | Anwender-Entscheidung: Trade-off hingenommen — ein echtes Ende markiert weiterhin `PlaybackEndedMessage`; der seltene Auslass (Schließen/Pausieren vor Ende) wird akzeptiert. Ohne Kopplung bliebe die falsche Watched-Markierung samt Download-Löschung bei Glitch-Takten auslösbar |
| Pausentakt-Behandlung | Keine Ausnahme: Pausentakte werden nicht gesondert behandelt — sie erfüllen den Fortschrittstest nicht und fallen unter „übrige Takte" → `EndWindowStreak = 0` | Anwender-Entscheidung: Watched-Markierung und Fortsetzung brauchen ≥ 2 aufeinanderfolgende Takte mit realem Fortschritt; ein am Ende pausiertes Medium markiert nur noch `PlaybackEndedMessage` |
| Streak-Fenster | Die Streak zählt plausible Takte mit `remaining <= max(WatchedEndWindowSeconds, AutoResumeRemainingSeconds)` | Bei unterschiedlich eingestellten Fenstern bleibt der Zeitpunkt des engeren Ereignisses unverändert; nur der Eintritt ins weitere Fenster beginnt die Bestätigung früher |
| Diagnose-Logging | `PlayerPage.ReportPosition` protokolliert bei `remaining <=` dem eingestellten Fenster die Takt-Werte (Position, Dauer, Zustand, gemeldete `MediaId`, `EndWindowStreak` aus `mvuHost.Model`) auf `LogDebug` | Anwender-Entscheidung: Logging wird wie vorgeschlagen ausgeliefert — einziger Eingriff zum Auslöser-Nachweis, kein weiterer Code-Eingriff und kein Einstellungs-Eingriff (`AutoResumeRemainingSeconds` bleibt unverändert). Der MVU-Kern ist rein und ohne Logger; der App-Rand ist der einzige Punkt mit Zugriff auf Elementwerte und Logger |

## Programmabläufe

### Fortschrittstakt mit Fenster-Plausibilisierung (`UpdatePlaybackProgress`)

1. Frühreturn `!player.IsActive` — unverändert.
2. **Neu — Identitäts-Guard:** `message.MediaId != player.MediaId` → `AppTransition.Unchanged(model)`; der Takt wird vollständig verworfen (kein State-Update, keine Effekte).
3. `updated = player with { Position, Duration, IsPlaying, IsBuffering }` — unverändert.
4. Fortschrittsmeldung an den Server mit 0,5-s-Throttling (`LastReportedPositionSeconds`) — unverändert.
5. **Neu — Streak-Auswertung** (nur wenn `message.Duration > TimeSpan.Zero`, sonst `EndWindowStreak = 0`):
   - `updated.IsResolving` → `EndWindowStreak = 0`, keine weitere Fensterauswertung (Schritte 6–8 entfallen).
   - `IsProgressTickPlausible(player, message)` (`message.Duration > 0`, `message.Position >= 0`, `message.Position > player.Position` — realer Fortschritt — und `player.Duration <= 0 || message.Duration >= player.Duration` — keine geschrumpfte Dauer; erste Meldung erlaubt) **und** `remaining <= max(WatchedEndWindowSeconds, AutoResumeRemainingSeconds)` → `EndWindowStreak = player.EndWindowStreak + 1`.
   - Alle übrigen Takte → `EndWindowStreak = 0`: Pausentakt (kein Fortschritt — keine Ausnahme), eingefrorene Position bei `IsPlaying`, geschrumpfte `Duration`, Rückwärtssprung z. B. Seek zurück, Dauer `<= 0`, plausibler Takt außerhalb des Fensters.
6. **Geändert — „angesehen"-Schwelle:** `!updated.IsWatched && remaining <= WatchedEndWindowSeconds && updated.EndWindowStreak >= EndWindowRequiredTicks` → `IsWatched = true`; bei `AutoDeleteWatchedEpisodes && MediaType == "tvshowepisode"` zusätzlich `DeleteWatchedDownloadEffect` (bisherige Bedingungen bleiben).
7. **Geändert — Playlist-Auto-Fortsetzung:** bestehende Bedingungen (`AutoResumeEnabled`, `message.IsPlaying`, `IsPlaylistPlayback`, `!PlaylistNextExhausted`, `!IsResolving`, `remaining <= AutoResumeRemainingSeconds`) **plus** `EndWindowStreak >= EndWindowRequiredTicks` → `AdvancePlaylistEntry` wie bisher.
8. **Geändert — Serien-Auto-Fortsetzung:** bestehende Bedingungen (`AutoResumeEnabled`, `message.IsPlaying`, `HasNextEpisode`, `remaining <= AutoResumeRemainingSeconds`) **plus** `!updated.IsResolving` und `EndWindowStreak >= EndWindowRequiredTicks` → `AdvanceToNextEpisode` wie bisher.
9. `MarkEpisodeWatchedInDetails` und Rückgabe des Übergangs — unverändert.

Beteiligte Klassen/Komponenten: `AppUpdate`, `PlayerState`, `PlaybackProgressMessage`, `AppSettings`, `AdvancePlaylistEntry`, `AdvanceToNextEpisode`, `MarkEpisodeWatchedInDetails`, `DeleteWatchedDownloadEffect`, `ReportPlaybackProgressEffect`.

### Medienwechsel und nachlaufende Takte

1. `AdvanceToNextEpisode` (Serien-Wechsel) und `UpdatePlaylistNavigationCompleted` (Playlist-Eintragswechsel) setzen beim Übernehmen der neuen Identität zusätzlich `EndWindowStreak = 0` zurück (Position/Duration werden bereits auf `TimeSpan.Zero` gesetzt).
2. `PlayerPage.UpdateUi` merkt beim Anwenden einer neuen Quelle `appliedMediaId = player.MediaId` (parallel zu `appliedLocation`); beim Leeren der Quelle (`location is null`) `appliedMediaId = 0`.
3. `PlayerPage.ReportPosition` dispatcht `PlaybackProgressMessage` mit `appliedMediaId`. Ein Takt, der nach dem Zustandswechsel noch Werte des alten Elements trägt, erreicht den MVU-Kern mit der alten Id → wird durch den Identitäts-Guard (Ablauf 1, Schritt 2) verworfen und kann die Folge-Episode weder als gesehen markieren noch deren Download löschen oder einen weiteren Wechsel kaskadieren.
4. Takte, die während `IsResolving` bei **unveränderter** `MediaId` eintreffen (Playlist-Pfad zwischen `AdvancePlaylistEntry` und `UpdatePlaylistNavigationCompleted`), aktualisieren zwar `Position`/`Duration`, bleiben aber ohne Fensterwirkung (`EndWindowStreak = 0`).

Beteiligte Klassen/Komponenten: `AppUpdate` (`AdvanceToNextEpisode`, `AdvancePlaylistEntry`, `UpdatePlaylistNavigationCompleted`, `UpdatePlaybackSourceResolved`), `PlayerPage` (`UpdateUi`, `ReportPosition`), `PlaybackProgressMessage`.

### Diagnose-Protokollierung des Fenstertakts

1. `ReportPosition` liest `mvuHost.Model.Settings` (`WatchedEndWindowSeconds`, `AutoResumeRemainingSeconds`) und `mvuHost.Model.Player.EndWindowStreak`.
2. Liegt die gemeldete Restlaufzeit innerhalb des Fensters (`remaining <= max(...)`), wird ein `LogDebug`-Eintrag mit Position, Dauer, `IsPlaying`/`IsBuffering`, `appliedMediaId` und Streak geschrieben — ohne Quell-URLs (Muster `SanitizeForLog` bleibt unberührt, Positionswerte enthalten keine Secrets).

Beteiligte Klassen/Komponenten: `PlayerPage` (`ReportPosition`), `AppModel`, `AppSettings`, `PlayerState`.

## Neue Klassen

Keine.

## Änderungen an bestehenden Klassen

### `PlayerState` (`VideoPlayer.Mvu/PlayerState.cs`, record)

- **Neue Eigenschaften:** `EndWindowStreak` (`int`, Default `0`) — Anzahl aufeinanderfolgender plausibler Fortschrittstakte innerhalb des Endfensters; Grundlage der Bestätigungsprüfung für `IsWatched` und Auto-Fortsetzung.

### `PlaybackProgressMessage` (`VideoPlayer.Mvu/AppMessage.cs`, Z. 1003–1007, record)

- **Neue Eigenschaften:** `MediaId` (`long`) — Identität des Mediums, dessen Wiedergabeelement die Werte meldet (von der Seite gemerkte, zuletzt angewendete `MediaId`); `0` = noch keine Quelle angewendet → Takt wird verworfen, da `player.MediaId > 0` bei aktiver Wiedergabe.

### `AppUpdate` (`VideoPlayer.Mvu/AppUpdate.cs`)

- **Neue Konstanten:** `EndWindowRequiredTicks = 2` (`private const int`) — erforderliche Anzahl aufeinanderfolgender plausibler Fenstertakte; bewusst Konstante, keine Einstellung (Muster `PlaybackEndedToleranceSeconds`).
- **Neue Methoden:** `IsProgressTickPlausible(PlayerState player, PlaybackProgressMessage message)` → `bool` — Takt-Plausibilität: positive Dauer, nicht-negative Position, realer Fortschritt (`message.Position > player.Position`), nicht geschrumpfte Dauer (`player.Duration <= 0 || message.Duration >= player.Duration`).
- **Geänderte Methoden:**
  - `UpdatePlaybackProgress` (Z. 3792–3876) — Identitäts-Guard auf `message.MediaId`; Streak-Auswertung inkl. `IsResolving`-Sperre (keine Pausentakt-Sonderbehandlung); `IsWatched`/`DeleteWatchedDownloadEffect` sowie beide Advance-Zweige an `EndWindowStreak >= EndWindowRequiredTicks` gekoppelt; Serienzweig zusätzlich um `!updated.IsResolving` ergänzt.
  - `AdvanceToNextEpisode` (Z. 3735–3777) — `EndWindowStreak = 0` beim Medienwechsel zurücksetzen.
  - `UpdatePlaylistNavigationCompleted` (Z. 2540–2599) — `EndWindowStreak = 0` beim Eintragswechsel zurücksetzen.

### `PlayerPage` (`VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`)

- **Neue Felder:** `appliedMediaId` (`long`) — zuletzt am `MediaElement` angewendete `MediaId` (Zugriff nur UI-Thread, analog `appliedLocation`).
- **Geänderte Methoden:**
  - `UpdateUi` (Z. 201–261) — `appliedMediaId` beim erfolgreichen Quellen-Setzen bzw. Leeren pflegen (bei `sourceApplyFailed`-Pfaden: auf `player.MediaId` setzen bleibt korrekt, da der Takt dann ohnehin nicht läuft).
  - `ReportPosition` (Z. 167–185) — `PlaybackProgressMessage` um `appliedMediaId` ergänzen; Fenster-Diagnoselog (`LogDebug`) bei `remaining <=` Fenster.

## Datenbankmigrationen

Keine.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `PlaybackProgressMessage` | `message.MediaId == player.MediaId` | Fremder/nachlaufender Takt → vollständig verworfen (`AppTransition.Unchanged`) |
| `PlaybackProgressMessage` | `IsProgressTickPlausible`: `Duration > 0`, `Position >= 0`, `Position > player.Position`, `player.Duration <= 0 || Duration >= player.Duration` | Glitch-Takt (Stall, Schrumpfen, Rückwärtssprung) → `EndWindowStreak = 0`, keine Fensterwirkung |

## Konfigurationsänderungen

Keine neuen Benutzereinstellungen. Die bestehenden Schalter `AutoResumeEnabled`, `AutoResumeRemainingSeconds`, `WatchedEndWindowSeconds`, `AutoDeleteWatchedEpisodes` (`VideoPlayer.Storage/AppSettings.cs`) bleiben unverändert wirksam — nur der Fenstereintritt wird plausibilisiert. Neu ist lediglich die interne Konstante `EndWindowRequiredTicks = 2` in `AppUpdate`.

## Seiteneffekte und Risiken

- **Verzögerter Fenstereintritt (~1 Taktintervall, ≤ 5 s):** `IsWatched`, `DeleteWatchedDownloadEffect` und beide Advance-Pfade greifen frühestens beim zweiten plausiblen Fenstertakt. Die Auto-Fortsetzung schaltet damit bis zu 5 s später um als bisher — fachlich unerheblich, da das Fenster typischerweise 300 s beträgt.
- **Kurze Medien (kürzer als ~2 Taktintervalle Restlaufzeit):** Der Tick-Pfad markiert/switched ggf. nicht mehr; der unveränderte `PlaybackEndedMessage`-Pfad (`IsPlaybackEndPlausible`, `UpdatePlaybackEnded`) übernimmt Abschluss, Watched-Markierung und Fortsetzung — legitimes Verhalten bleibt erhalten.
- **Scrub-to-End:** Ein Sprung ans Ende bestätigt sich erst ab dem zweiten laufenden Fenstertakt (~10 s); pausiert der Anwender direkt danach, greift der Takt-Pfad nicht — `PlaybackEndedMessage` bzw. gar keine Markierung beim Schließen vor Ende (hingenommener Trade-off).
- **Seek zurück aus dem Fenster:** Setzt die Streak zurück — verhindert zusätzlich ein unmittelbares erneutes Vorrücken (Verhaltensverbesserung).
- **Pausieren oder Schließen vor dem Ende:** Pausentakte setzen die Streak zurück (keine Ausnahme) — ein am Ende pausiertes Medium wird taktseitig weder als gesehen markiert noch weitergeschaltet; `IsWatched` entfällt auf `PlaybackEndedMessage`, beim Schließen vor dem Ende ganz (Anwender-Entscheidung: seltene Auslassung akzeptiert, der Schaden einer Fehlmarkierung samt Download-Löschung überwiegt).
- **Echte Stalls mit `IsPlaying = true`:** Eingefrorene Position erfüllt den Fortschrittstest nicht → Streak-Reset. Ein Stall, der als `Buffering`/`Opening` sichtbar wird, meldet `IsPlaying = false` — auch diese Takte fallen unter „übrige Takte" und setzen zurück (keine Pausentakt-Sonderbehandlung).
- **`ReportPlaybackProgressEffect`:** Unverändert (Throttling bleibt); Fremd-Takte werden jetzt vor der Meldung verworfen — der Server erhält keine falsch attributierten Fortschritte mehr.
- **`PlaybackEndedMessage`-Pfad:** Bleibt bewusst unverändert (`IsPlaybackEndPlausible` greift weiter).
- **`PlayerContractTests`:** Die geänderte `PlaybackProgressMessage`-Signatur ist ein Contract-Bruch für alle direkt konstruierenden Tests — betroffene Aufrufstellen in `AppUpdateTests_Player`, `AppUpdateTests_PlaylistPlayback`, `AppUpdateTests_Downloads` und `PlayerContractTests` sind anzupassen.
- **E2E-Auto-Advance-Tests:** Wechsel benötigt nun ≥ 2 aufeinanderfolgende Fenstertakte (~10 s statt ~5 s) — Laufzeit und Timeout-Margen der betroffenen Tests prüfen.
- **Residual-Lücke der Identitätsbindung:** Zwischen `appliedMediaId`-Zuweisung und tatsächlichem Medienwechsel des Elements können Werte des alten Mediums unter neuer Id gemeldet werden (Millisekunden-Fenster); der folgende Takt des neuen Mediums macht die Streak unplausibel — Risiko als vernachlässigbar bewertet.

## Umsetzungsreihenfolge

1. **`PlayerState.EndWindowStreak` und Konstante `EndWindowRequiredTicks` anlegen**
   - Voraussetzungen: Keine.
   - Beschreibung: Neue `init`-Eigenschaft (Default 0) auf `PlayerState`; `private const int EndWindowRequiredTicks = 2` im Konstantenblock von `AppUpdate` (Muster `PlaybackEndedToleranceSeconds`).

2. **`PlaybackProgressMessage` um `MediaId` erweitern und `PlayerPage` anbinden**
   - Voraussetzungen: Keine.
   - Beschreibung: Record-Parameter `MediaId` ergänzen; `PlayerPage`: Feld `appliedMediaId`, Pflege in `UpdateUi` beim Quellen-Setzen/Leeren, `ReportPosition` dispatcht mit `appliedMediaId` und schreibt das Fenster-Diagnoselog. Alle kompilierenden Aufrufstellen (`ReportPosition`, Tests) werden auf die neue Signatur umgestellt — `PlayerContractTests` wird im selben Schritt angepasst.

3. **Plausibilität und Streak-Logik in `UpdatePlaybackProgress` einbauen**
   - Voraussetzungen: Schritte 1 und 2 (Feld, Konstante, `MediaId` auf der Nachricht).
   - Beschreibung: `IsProgressTickPlausible` implementieren; Identitäts-Guard, `IsResolving`-Sperre und Streak-Aufbau/-Reset einbauen (alle nicht plausiblen Takte — inkl. Pausentakte — setzen zurück); `IsWatched`/`DeleteWatchedDownloadEffect` sowie beide Advance-Zweige an die Streak-Bedingung koppeln; `!updated.IsResolving` im Serienzweig ergänzen.

4. **Streak-Reset an den Medienwechselstellen**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: `EndWindowStreak = 0` in `AdvanceToNextEpisode` und `UpdatePlaylistNavigationCompleted` ergänzen (`StartPlayback`, `UpdatePlaylistPlaybackStarted`, `UpdatePlayerClosed` erzeugen neue/leere States — Default 0 greift automatisch).

5. **Bestehende Tests auf die neue Semantik umstellen**
   - Voraussetzungen: Schritte 1–4.
   - Beschreibung: Ein-Takt-Tests auf Zwei-Takt-Sequenzen umstellen (`PlayingState`-Hilfsmethode ggf. um `EndWindowStreak`-/`mediaId`-Parameter erweitern); `PlaybackProgressMessage`-Aufrufe um `mediaId` ergänzen.

6. **Neue Tests für Glitch-Takte, Streak und Identitätsbindung schreiben**
   - Voraussetzungen: Schritte 1–5.
   - Beschreibung: Siehe Abschnitt „Neue Tests".

7. **Testlauf und E2E-Timing-Prüfung**
   - Voraussetzungen: Schritte 1–6.
   - Beschreibung: `dotnet test VideoPlayer.Maui.Tests` ausführen; E2E-Tests mit Auto-Advance auf die verlängerte Wechselzeit prüfen (Build/Ausführung der E2E-Suite nur, wenn die Umgebung es zulässt — sonst Timeout-/Wartezeit-Anpassung dokumentieren).

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `PlaybackProgress_SingleWindowTick_DoesNotAdvance` | `AppUpdateTests_Player` | Ein einzelner plausibler Fenstertakt (Streak 1) löst weder Wechsel noch `IsWatched` aus |
| `PlaybackProgress_SecondWindowTick_AdvancesAndMarksWatched` | `AppUpdateTests_Player` | Zwei aufeinanderfolgende plausible Fenstertakte → `IsWatched` + `AdvanceToNextEpisode` (`ResolvePlaybackSourceEffect` für Folge-MediaId) |
| `PlaybackProgress_FrozenPosition_ResetsStreak` | `AppUpdateTests_Player` | Fenstertakt → eingefrorener Takt (`Position` unverändert, `IsPlaying = true`) → weiterer Fenstertakt: kein Wechsel (Streak wurde zurückgesetzt) |
| `PlaybackProgress_ShrunkDuration_ResetsStreak` | `AppUpdateTests_Player` | Fenstertakt → Takt mit geschrumpfter `Duration` im Fenster → Folgetakt: kein Wechsel/kein `IsWatched` |
| `PlaybackProgress_PositionJump_SingleTick_NoAdvance` | `AppUpdateTests_Player` | Einzelner Vorwärtssprung direkt ins Fenster löst nicht sofort aus; erst der zweite bestätigende Takt schaltet weiter |
| `PlaybackProgress_PausedTick_ResetsStreak` | `AppUpdateTests_Player` | Plausibler Fenstertakt → Pausentakt (`IsPlaying = false`) → weiterer Fenstertakt: kein Wechsel/kein `IsWatched` (Streak zurückgesetzt); der Pausentakt selbst löst nichts aus |
| `PlaybackProgress_WrongMediaId_IsDiscarded` | `AppUpdateTests_Player` | Takt mit abweichender `MediaId` (nachlaufender Takt des Vorgängermediums nach `AdvanceToNextEpisode`) → `AppTransition.Unchanged`, kein `IsWatched`, kein `DeleteWatchedDownloadEffect` |
| `PlaybackProgress_WhileResolving_NoWindowEffects` | `AppUpdateTests_Player` | Takt bei `IsResolving = true` setzt `EndWindowStreak = 0` und löst weder `IsWatched` noch Advance aus |
| `PlaybackProgress_SeriesAdvance_RequiresNotResolving` | `AppUpdateTests_Player` | Serienzweig schaltet bei `IsResolving` nicht weiter (kein kaskadierender `AdvanceToNextEpisode`) |
| `PlaybackProgress_TwoWindowTicks_EmitsAutoDelete` | `AppUpdateTests_Downloads` | `DeleteWatchedDownloadEffect` erst ab dem zweiten plausiblen Fenstertakt |
| `PlaybackProgress_NearEnd_TwoTicks_AdvancesPlaylist` | `AppUpdateTests_PlaylistPlayback` | Playlist-Pfad: zwei plausible Fenstertakte → `AdvancePlaylistEntryEffect`; einzelner Takt nicht |
| `PlaybackProgress_TwoTicks_MarksEpisodeWatchedInDetails` | `AppUpdateTests_Player` | `MarkEpisodeWatchedInDetails` greift mit der bestätigten Streak (`WatchedAt` auf der Detail-Episode) |
| `PlayingState`-Hilfsmethode | `AppUpdateTests_Player` | Um optionale Parameter `endWindowStreak`/`mediaId`-Übergabe erweitern |
| `PlayingInPlaylist`-Hilfsmethode | `AppUpdateTests_PlaylistPlayback` | Um `mediaId`-konsistente Takt-Erzeugung ergänzen (Fixture-Position liegt bereits im Fenster — zweiter Takt nötig) |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `PlaybackProgress_MarksWatchedWithinWindow` | Ein-Takt-Markierung → zwei plausible Fenstertakte nötig; Grenzfall-Aufrufe (300/301 s) um `mediaId` ergänzen |
| `PlaybackProgress_AutoResumeAdvancesWhenEnabled` | Advance erst ab zweitem Fenstertakt |
| `PlaybackProgress_AutoResumeDisabled_DoesNotAdvance`, `PlaybackProgress_AboveResumeWindow_DoesNotAdvance`, `PlaybackProgress_PausedWithinWindow_DoesNotAdvance`, `PlaybackProgress_NoNextEpisode_DoesNotAdvance` | `mediaId`-Parameter ergänzen; negative Erwartungen bleiben bestehen (zusätzlich Streak-Variante prüfbar) |
| `PlaybackProgress_ReportsChangedPosition`, `PlaybackProgress_WithoutPlaylist_ReportsNullPlaylistId`, `PlaybackProgress_UnchangedPosition_NoReport` | Neue `PlaybackProgressMessage`-Signatur (`mediaId`) |
| `PlaybackProgress_WatchedWindow_MarksEpisodeWatchedInDetails` | Zwei plausible Fenstertakte statt einem |
| `PlaybackProgress_WatchedEpisode_EmitsAutoDelete`, `PlaybackProgress_WatchedMovie_NoAutoDelete`, `PlaybackProgress_AutoDeleteDisabled_NoEffect` (`AppUpdateTests_Downloads`) | Streak-Bedingung + `mediaId`-Signatur |
| `PlaybackProgress_NearEnd_InPlaylist_AdvancesPlaylist`, `PlaybackProgress_NearEnd_WhileResolving_NoSecondAdvance`, `PlaybackProgress_InPlaylist_ReportsPlaylistId` (`AppUpdateTests_PlaylistPlayback`) | Streak-Bedingung + `mediaId`-Signatur |
| `PlayerPage_DispatchesPlaybackMessages` (`PlayerContractTests`) | `PlaybackProgressMessage` trägt `MediaId`; Dispatch mit `appliedMediaId` pinnen |

### E2E-Tests (Pflicht)

Kein neuer E2E-Test: Das geänderte Verhalten betrifft keine neue Benutzerinteraktion, sondern die Robustheit gegenüber inkonsistenten Positions-/Dauer-Takten eines gestörten Streams. Ein Stall-/Ausreißer-Takt-Szenario ist E2E-seitig nicht nachstellbar (`MockVideoServer` streamt ein festes Testvideo; der Fortschrittstakt kommt vom echten `MediaElement` und kann nicht gestört injiziert werden). Der legitime Auto-Advance-Happy-Path bleibt durch die bestehenden Tests abgedeckt.

Welche bestehenden E2E-Tests müssen angepasst werden?

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `PlaybackEnd_AdvancesToNextPlaylistEntry` (`PlaylistPlaybackTests`) | Wechsel benötigt ≥ 2 Fenstertakte (~10 s statt ~5 s); Wartezeit/Timeout prüfen und ggf. erhöhen |
| `TapContinueWatchingEpisode_OpensDetailsAndStartsPlaybackAndAdvancesToNextEpisode` (`ContinueWatchingPlaybackTests`) | Derselbe Timing-Effekt im Serienzweig |
| `ContinueWatchingPlaylistEntry_AdvancesByPlaylistOrder_NotSeries` (`PlaylistPlaybackTests`) | Derselbe Timing-Effekt im Playlist-Pfad |

## Offene Punkte

Keine.
