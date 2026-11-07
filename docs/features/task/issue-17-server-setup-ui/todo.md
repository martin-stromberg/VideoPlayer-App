# Aufgabenliste – Anforderungsbearbeitung

Branch: `task/issue-17-server-setup-ui`

Hinweis: Unteragenten stehen in dieser Umgebung nicht zur Verfügung — alle Lifecycle-Schritte werden gemäß Fallback-Regel in `lifecycle.md` („Wichtiger Hinweis vorab") selbst ausgeführt.

| Status | Schritt | Beschreibung | Artefakt |
|--------|---------|--------------|----------|
| [x] | 1 | Branch-Name ermitteln | – |
| [x] | 2 | Verzeichnisstruktur vorbereiten | `docs/features/task/issue-17-server-setup-ui/` |
| [x] | – | Einstiegspunkt ermitteln | – |
| [x] | 3 | Anforderung übersetzen | `requirement.md` |
| [x] | 4 | Bestandsaufnahme | `inventory.md`, `inventory/` |
| [x] | 5 | Umsetzungsplanung | `plan.md` |
| [x] | 5a | Offene Punkte prüfen und ggf. Planung wiederholen | `plan.md` (keine offenen Punkte) |
| [x] | 5b | Plan gegen Anforderung und Testbedarf prüfen | `plan-check.md` |
| [x] | 5c | Planungscommit | `f983df2` |
| [x] | 6 | Implementierung | `ServerSetupPage.xaml`/`.xaml.cs`, Contract-/E2E-Tests |
| [x] | 7 | Plan-Review | `review.md` |
| [x] | 8 | Usability-Review (bei UI-Änderungen) | `review-usability.md` |
| [x] | 9 | Code-Review | `review-code.md` |
| [x] | 10 | Tests ausführen | `test-results.md` (927/927 + 11/11 E2E) |
| [x] | – | Iteration oder Abschluss entscheiden | Abschluss: alle Kriterien erfüllt, keine Befunde |
| [x] | 11 | Folgeaufgaben dokumentieren (bei Schleifenabbruch) | nicht nötig (kein Schleifenabbruch) |
| [x] | 12 | Dokumentation erstellen | `docs/help/serververwaltung-erststart.md` |
| [x] | 12b | README aktualisieren | `README.md` |
| [x] | 12c | Release Notes aktualisieren | `docs/RELEASE_NOTES.md`, `changes.log` |
| [x] | – | Feature-Verzeichnis aufräumen | Verzeichnis bleibt als Doku erhalten (Konvention wie `issue-15-*`/`issue-17-flaui-e2e`) |
| [ ] | – | Commit durchführen | – |
