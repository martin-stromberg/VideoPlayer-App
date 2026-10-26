# Unabhängiges Re-Review der E2E-Prozessdiagnose

## Status: Keine Befunde

Geprüft am: 2026-10-26. Umfang: aktueller Diff in `VideoPlayer.E2E.Tests/Support/AppInstance.cs` und `VideoPlayer.E2E.Tests/Tests/E2ETestBase.cs` gegenüber HEAD, insbesondere Nachbesserung des Kill-Race-Befunds. Das erste Review ist als nummerierte Datei archiviert.

## Geschlossener Befund

- [x] **P2 – Killversuch maskiert nativen Prozessabbruch:** geschlossen. `killAttempted`, `killRequestSucceeded` und `killError` sind getrennt. Ein Killaufruf gilt erst nach dessen Rückkehr als angenommen; ein Fixture-Kill wird nur bei zusätzlich tatsächlich beobachtetem Exitcode `-1` klassifiziert. `0xC0000005` bleibt unabhängig von Killflags ein Fehler; ein fehlgeschlagener Killversuch kann diesen nativen Fehlercode nicht mehr vom Guard ausnehmen. Unbeendete Prozesse bleiben ebenfalls Fehler. Die getrennte xUnit-Cleanup-Fehlerübergabe ist unverändert und erhält primäre Assertions.

## Unabhängige gezielte Prüfung

Die eigene temporäre Probe `.work/shutdown-review-probe/ReviewProbe.csproj` wurde gegen den aktuellen Code aktualisiert und erfolgreich ausgeführt, Exitcode 0. Sie startet ausschließlich eigene kurze Kindprozesse. Ein Kindprozess beendet sich kontrolliert mit dem bekannten Fehlercode; es wird kein Speicherzugriffsfehler ausgelöst. Ein zweiter laufender eigener Kindprozess wird über die tatsächliche private Fixture-Killmethode beendet.

| Tatsächlicher Exit | Killversuch | Killaufruf angenommen | Guard meldet Fehler |
|---|---|---|---|
| `0xC0000005` | nein | nein | ja |
| `0xC0000005` | ja | nein | ja |
| `0xC0000005` | ja | ja | ja |
| tatsächlicher Fixture-Kill, `0xFFFFFFFF` | ja | ja | nein |
| derselbe Exit, erfolgreicher Kill nicht belegt | ja | nein | ja |

Alle fünf unabhängigen Erwartungen erfüllt. Probe-Compilerhinweis nur zum transitiven xUnit-Einstiegspunktgenerator, kein Buildfehler. Die Prozessdiagnosen nennen für den kontrollierten nativen Fehlercode `forcedKill=false`, auch wenn beide Killflags gesetzt sind.

## Weitere Prüfung

Queue pro Testinstanz, getrennte Fehleraggregation, asynchrone Ausgabeerfassung, PID-/Profilzuordnung und Beschränkung von Killoperationen auf den konkret gestarteten Testprozess sind weiterhin konsistent. Kein neuer Produktcode, Paketupdate oder abgeschwächte Benutzerfluss-Assertion. Kein Eingriff in fremde Prozesse oder Systemdialoge.

Keine Fixes, Commits oder vollständige Suite durch den Reviewer. Der vollständige Windows-E2E-Lauf mit dem korrigierten Guard steht als nächster eigener Nachweis an.
