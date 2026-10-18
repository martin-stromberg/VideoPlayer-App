# Offene Aufgaben

Erstellt am: 2026-10-18
Abbruchgrund: Maximale Iterationsanzahl erreicht

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

(Keine — review.md meldet „Vollständig umgesetzt".)

## Code-Review-Befunde

- [ ] `PlaybackProgress_SeriesAdvance_RequiresNotResolving` (VideoPlayer.Maui.Tests/AppUpdateTests_Player.cs ~Z. 708–727) ist eine strikte Teilmenge von `PlaybackProgress_WhileResolving_NoWindowEffects` (~Z. 682–704): identisches Arrange/Act, beide Asserts in den 4 des anderen enthalten — Duplikat entfernen bzw. zusammenführen.
- [ ] `PlaybackProgress_TwoTicks_MarksEpisodeWatchedInDetails` (VideoPlayer.Maui.Tests/AppUpdateTests_Player.cs ~Z. 731–749) ist ein Szenario-Duplikat von `PlaybackProgress_WatchedWindow_MarksEpisodeWatchedInDetails` (~Z. 940–958): identisches Arrange, gleicher Zwei-Takt-Act, identische Asserts — Duplikat entfernen bzw. zusammenführen.

## Fehlgeschlagene Tests

(Keine — test-results.md meldet „Keine Fehler": 1381/1381 Unit-Tests, 11/11 E2E-Tests.)
