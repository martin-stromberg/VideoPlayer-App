# Aufgabenliste – Anforderungsbearbeitung

Branch: `task/issue-49-8320ea2ee24347a3b7ecbb210062e168-rueckmeldungen-quellen-browsin-schritt-5-wiedergabeende`

| Status | Schritt | Beschreibung | Artefakt |
|--------|---------|--------------|----------|
| [x] | 1 | Branch-Name ermitteln | – |
| [x] | 2 | Verzeichnisstruktur vorbereiten | `docs/features/{branchname}/` |
| [x] | – | Einstiegspunkt ermitteln | – |
| [ ] | 3 | Anforderung übersetzen (Unteragent) | `requirement.md` |
| [ ] | 4 | Bestandsaufnahme (Unteragent) | `inventory.md`, `inventory/` |
| [ ] | 5 | Umsetzungsplanung (Unteragent) | `plan.md` |
| [ ] | 5a | Offene Punkte prüfen und ggf. Planung wiederholen | `plan.md` (aktualisiert) |
| [ ] | 5b | Plan gegen Anforderung und Testbedarf prüfen (Unteragent) | `plan-check.md` |
| [ ] | 5c | Planungscommit | – |
| [ ] | 6 | Implementierung (Unteragent) | Codeänderungen |
| [ ] | 7 | Plan-Review (Unteragent, bedingt) | `review.md` |
| [ ] | 8 | Usability-Review (Unteragent, bei UI-Änderungen) | `review-usability.md` |
| [ ] | 9 | Code-Review (Unteragent) | `review-code.md` |
| [ ] | 10 | Tests ausführen (Unteragent) | `test-results.md` |
| [ ] | – | Iteration oder Abschluss entscheiden | – |
| [ ] | 11 | Folgeaufgaben dokumentieren (bei Schleifenabbruch) | `continue.md` |
| [ ] | 12 | Dokumentation erstellen (Unteragent) | `docs/help/` |
| [ ] | 12b | README aktualisieren (Unteragent) | `README.md` |
| [ ] | 12c | Release Notes aktualisieren (Unteragent) | `docs/RELEASE_NOTES.md` |
| [ ] | – | Feature-Verzeichnis löschen | – |
| [ ] | – | Commit durchführen | – |

## Hinweis zur Ausführung

Keine Unteragenten verfügbar — alle Lifecycle-Schritte (Übersetzung, Bestandsaufnahme, Planung, Plan-Check, Implementierung, Reviews, Tests, Dokumentation) werden vom Hauptagenten selbst ausgeführt (Fallback-Variante gemäß `lifecycle.md`, „Wichtiger Hinweis vorab"). Der Anwender ist nicht erreichbar; offene Punkte werden mit dokumentierten Annahmen entschieden statt rückgefragt.

Einstiegspunkt: Schritt 3 (`requirement.md` fehlt).
