# Umsetzungsplan: Frühzeitiger Wechsel zum nächsten Video bei fehlschlagendem Fortsetzungs-Seek

## Übersicht

Fehlerbehebung im Wiedergabepfad der `PlayerPage` (`VideoPlayer.Maui`): Der catch-Zweig von `ApplyResumeSeekAsync` dispatcht derzeit bei **jeder** Ausnahme des `MediaElement.SeekTo`-Aufrufs — inklusive der `TaskCanceledException` des internen 10-s-`ResumeSeekTimeout` — ein `PlaybackFailedMessage`, das in `AppUpdate.UpdatePlaybackFailed` ungeprüft den Episoden-/Playlist-Rückfall auslöst. Der Seek-Fehlschlag wird zu einem nicht-fatalen Pfad (Warn-Log, Wiedergabe läuft weiter); der Rückfall bleibt den echten Fehlerpfaden `OnMediaFailed`, `FailPlaybackStart` und der fehlgeschlagenen Quellauflösung vorbehalten. Der MVU-Kern (`AppUpdate`, `AppMessage`) bleibt unverändert; betroffen sind `PlayerPage` und die Quelltext-Vertragstests in `VideoPlayer.Maui.Tests`.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Fehlerpfad `ApplyResumeSeekAsync` | Minimalvariante: `PlaybackFailedMessage`-Dispatch entfernen, Warn-Log beibehalten — **keine** neue `AppMessage`, keine Änderung an `UpdatePlaybackFailed` | Die Degradation ist rein lokal: Der MVU-Kern hat keinen Zustand zu ändern (kein Effekt, kein Modellwert); eine eigene `ResumeSeekFailedMessage` müsste ein Update auslösen, das nichts verändert. Zugleich bleibt die Semantik von `PlaybackFailedMessage` eindeutig „fataler Abspielfehler" — alle verbleibenden Dispatch-Stellen dürfen weiterhin den Rückfall auslösen. |
| Ausnahmebehandlung des Seeks | Alle Ausnahmen des `SeekTo`-Aufrufs als nicht-fatal behandeln (bestehender `catch (Exception)` bleibt, nur der Dispatch entfällt) | Kein Ausnahmetyp des Seek-Aufrufs belegt einen Abspielfehler — die Wiedergabe selbst läuft weiter, nur der Sprung wurde ggf. nicht angewendet. Für echte Abspielfehler ist `MediaFailed` zuständig; eine Typunterscheidung brächte keinen zusätzlichen Schutz. |
| Playlist-Kontext | Keine gesonderte Behandlung nötig | Der Seek-Fehlschlag erreicht den Playlist-Rückfall (`AdvancePlaylistEntry`) nur über denselben Dispatch-Punkt in `ApplyResumeSeekAsync` — mit dessen Entschärfung ist der Playlist-Kontext automatisch abgedeckt. |
| Wiederholungsversuch des Seeks | Kein Retry — es bleibt beim einmaligen Versuch über `pendingResumePosition` | Das Protokoll legt nahe, dass der native Sprung bereits wirksam war (Video lief „mitten im Video"); ein Retry riskiert einen sichtbaren Positions-Rücksprung und ein erneutes Timeout. |
| Geltungsbereich der Entschärfung | Plattform- und quellartenübergreifend (`ServerStream` und `LocalFile`, alle Plattformen) | Das Seek-Gate unterscheidet `PlaybackSource.Kind` nicht; ein `LocalFile`-Seek-Fehlschlag ist ebenso nicht-fatal — ein Sonderpfad für iOS/`ServerStream` wäre unnötige Komplexität. |
| Nutzerhinweis bei Seek-Fehlschlag | Stille Degradation — kein sichtbarer Hinweis, kein Episodenwechsel | Die Wiedergabe läuft für den Nutzer korrekt weiter (der native Sprung war ggf. bereits wirksam); ein Hinweis würde einen nicht wahrnehmbaren Fehler anzeigen. Das Warn-Log genügt zur Diagnose. |

## Programmabläufe

### Fortsetzungs-Seek (geänderter Fehlerpfad)

1. `OnMediaOpened` merkt `Source.ResumePosition` als `pendingResumePosition` und stößt `TryApplyPendingResumeSeek` per `MainThread.BeginInvokeOnMainThread` an (unverändert, iOS-`ReadyToPlay`-Workaround).
2. `TryApplyPendingResumeSeek` prüft `VideoElement.CurrentState`; bei `Playing`/`Paused` wird `pendingResumePosition` geleert und `ApplyResumeSeekAsync` einmalig angestoßen (unverändert — kein Wiederholungsversuch nach einem Fehlschlag).
3. `ApplyResumeSeekAsync` ruft `VideoElement.SeekTo(position, timeout.Token)` unter `CancellationTokenSource(ResumeSeekTimeout)` (10 s, unverändert) auf.
4. Erfolg: Info-Log „Fortsetzungs-Seek angewendet" (unverändert).
5. **Geändert:** Bei einer Ausnahme (Timeout-`TaskCanceledException` oder beliebige andere — plattform- und quellartenübergreifend) wird nur noch das Warn-Log geschrieben — **kein** `PlaybackFailedMessage`-Dispatch und kein sichtbarer Hinweis. Die laufende Wiedergabe bleibt unangetastet und läuft an der vom Player erreichten Position weiter (stille Degradation statt Abbruch).

Beteiligte Klassen/Komponenten: `PlayerPage`, `MediaElement` (CommunityToolkit.Maui), `pendingResumePosition`, `ResumeSeekTimeout`

### Echter Abspielfehler (unverändert — Regression)

1. `OnMediaFailed` (`MediaFailed`-Event) oder `FailPlaybackStart` (ungültige/nicht setzbare Quelle) dispatcht `PlaybackFailedMessage`.
2. `AppUpdate.UpdatePlaybackFailed` fällt bei aktivem Player zurück: Playlist (`IsPlaylistPlayback && !PlaylistNextExhausted`) → `AdvancePlaylistEntry` → `AdvancePlaylistEntryEffect` → `POST api/playlists/{id}/play/advance`; sonst → `AdvanceToNextEpisode` → `ResolvePlaybackSourceEffect` für `UpcomingEpisodes[0]`.
3. Ebenso bleibt `UpdatePlaybackSourceResolved` bei `Source = null` (nicht auflösbare Quelle) auf demselben Rückfall.

Beteiligte Klassen/Komponenten: `PlayerPage`, `AppUpdate`, `PlaybackFailedMessage`, `AdvancePlaylistEntryEffect`, `ResolvePlaybackSourceEffect`, `ShellAppEffectHandler`, `PlaylistService`

## Neue Klassen

Keine.

## Änderungen an bestehenden Klassen

### `PlayerPage` (`VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`, MAUI-Seite)

- **Geänderte Methoden:** `ApplyResumeSeekAsync` (Zeilen 399–415) — im catch-Zweig entfällt der `PlaybackFailedMessage`-Dispatch (Zeilen 410–413); das Warn-Log bleibt. Der XML-Dokumentationskommentar (Zeilen 390–396) wird angepasst: Timeout/Fehler werden nicht mehr „als Wiedergabefehler gemeldet", sondern degradieren still. Signatur und Timeout-Mechanik unverändert.
- **Unverändert:** `FailPlaybackStart` (Zeile 294) und `OnMediaFailed` (Zeile 426) bleiben die einzigen `PlaybackFailedMessage`-Dispatch-Stellen (fataler Pfad); `TryApplyPendingResumeSeek`, `OnMediaOpened`, `UpdateUi`, `ResumeSeekTimeout` unverändert.

Keine Änderungen an `AppUpdate`, `AppMessage`, `PlayerState`, `PlaybackSource`, `ShellAppEffectHandler`, `PlaybackSourceService`, `PlaylistService`.

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine.

## Konfigurationsänderungen

Keine. `ResumeSeekTimeout` bleibt unverändert bei 10 s als internes `private static readonly`-Feld der `PlayerPage` (festgelegt; die Toolkit-Vollendung bleibt bei Server-Streams strukturell aus, ein längeres Timeout würde die sichtbare Degradation nur verzögern).

## Seiteneffekte und Risiken

- **Playlist-Kontext:** Derselbe Seek-Fehlschlag löste bisher auch `AdvancePlaylistEntry` (`play/advance`) aus — dieser Seiteneffekt entfällt automatisch mit; ein Seek-Timeout verbraucht damit auch keinen Playlist-Eintrag mehr serverseitig.
- **Diagnose:** Seek-Fehlschläge bleiben ausschließlich über das Warn-Log sichtbar; der MVU-Kern erfährt nichts mehr davon (bewusst — kein Zustand betroffen).
- **Vertragstests:** `PlayerContractTests.PlayerPage_NativeAccessesAreFailureIsolated` pinnt den Ist-Zustand (`PlaybackFailedMessage` im Rumpf von `ApplyResumeSeekAsync`, Zeilen 370–373) und bricht ohne Anpassung.
- **`PlaybackFailedMessage`-Semantik:** Verbleibende Dispatch-Stellen (`FailPlaybackStart`, `OnMediaFailed`) und `UpdatePlaybackFailed` bleiben unverändert — `AppUpdateTests_Player`/`AppUpdateTests_PlaylistPlayback` dienen als Regression und bleiben grün.
- **`LocalFile`-Quellen / andere Plattformen:** Laufen durch dasselbe Seek-Gate; die Entschärfung gilt quellarten- und plattformübergreifend (festgelegt), ohne dass ein Sonderpfad nötig wird.

## Umsetzungsreihenfolge

1. **Catch-Zweig von `ApplyResumeSeekAsync` entschärfen**
   - Voraussetzungen: Keine.
   - Beschreibung: In `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs` den `PlaybackFailedMessage`-Dispatch (inkl. `subscribed`-Guard) aus dem catch-Zweig von `ApplyResumeSeekAsync` entfernen; Warn-Log beibehalten; XML-Dokumentationskommentar der Methode auf die nicht-fatale Semantik anpassen.

2. **Bestehenden Vertragstest `PlayerPage_NativeAccessesAreFailureIsolated` anpassen**
   - Voraussetzungen: Schritt 1 (der Quelltext-Vertrag prüft den geänderten Methodenrumpf).
   - Beschreibung: In `VideoPlayer.Maui.Tests/PlayerContractTests.cs` den Assert korrigieren, der `PlaybackFailedMessage` im Rumpf von `ApplyResumeSeekAsync` erwartet (Zeilen 370–373) — invertieren bzw. in den neuen Test aus Schritt 3 überführen; die übrigen Isolations-Asserts (`FailPlaybackStart`, `UpdateUi`, `ReportPosition`, Timer, Gate) bleiben.

3. **Neuen Vertragstest für den nicht-fatalen Seek-Pfad anlegen**
   - Voraussetzungen: Schritt 1; vorhandene Hilfsmethoden `PlayerPageCode()`/`MethodBody(...)` in `PlayerContractTests`.
   - Beschreibung: Neuer Test pinnnt: `ApplyResumeSeekAsync`-Rumpf enthält `try`/`catch` und das Warn-Log, aber keinen `PlaybackFailedMessage`-Dispatch; `FailPlaybackStart` und `OnMediaFailed` dispatchen weiterhin `PlaybackFailedMessage` (fatale Pfade bleiben vertraglich gesichert).

4. **Regressionslauf der betroffenen Test-Suites**
   - Voraussetzungen: Schritte 1–3.
   - Beschreibung: `VideoPlayer.Maui.Tests` ausführen (`PlayerContractTests`, `AppUpdateTests_Player`, `AppUpdateTests_PlaylistPlayback`) und die E2E-Suite `VideoPlayer.E2E.Tests` (`PlaybackTests`, `PlaylistPlaybackTests`, `ContinueWatchingPlaybackTests`) als Regression — keine funktionalen Änderungen an `AppUpdate`/Messages, alle Update-Tests müssen unverändert grün bleiben.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `PlayerPage_ResumeSeekFailure_IsNonFatal` | `PlayerContractTests` (`VideoPlayer.Maui.Tests`) | `MethodBody` von `ApplyResumeSeekAsync`: enthält `catch` + Warn-Log, enthält **kein** `PlaybackFailedMessage`; zugleich Regression: `FailPlaybackStart` und `OnMediaFailed` dispatcht weiterhin `PlaybackFailedMessage`. |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `PlayerContractTests.PlayerPage_NativeAccessesAreFailureIsolated` (Z. 370–373) | Pinnt den `PlaybackFailedMessage`-Dispatch im `ApplyResumeSeekAsync`-Rumpf — Assert muss invertiert bzw. in den neuen Test überführt werden. |

### E2E-Tests (Pflicht)

Kein neues E2E-Szenario technisch nachstellbar: `MockVideoServer` bietet keinen Schalter für einen hängenden/abbrechenden Seek, und das Toolkit-Timeout (`ResumeSeekTimeout`) ist E2E-seitig nicht reproduzierbar (Befund der Bestandsaufnahme). Das geänderte Verhalten wird über die Quelltext-Vertragstests gepinnt. Der bestehende Fortsetzungspfad bleibt abgedeckt:

| Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium |
|----------|------------------------|-------------------------------|
| Wiedergabe startet an der Fortsetzungsposition (Erfolgspfad des Fortsetzungs-Seeks) | `VideoPlayer.E2E.Tests/Tests/PlaybackTests.cs` — `PlayFromRecent_StartsPlaybackAtResumePosition` | Wiedergabe läuft mit Fortsetzungsposition fehlerfrei weiter (unverändert gültig). |

Welche bestehenden E2E-Tests müssen angepasst werden?

Keine. `PlaylistPlaybackTests.StreamFailure_AdvancesToNextPlaylistEntry` und die übrigen Rückfall-/Fortsetzungs-E2E-Tests bleiben gültig, da die fatalen Fehlerpfade unverändert sind.

## Offene Punkte

Keine.
