# Tasks: Automatisierung wiederkehrender Architektur-, Sicherheits- und Compliance-Reviews

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Issue-Vorlagen | `.github/issue-bodies/architecture-security-review.md` mit dem Prüfungstext aus Issue #95 anlegen (erste Zeile `@softwarewerkstatt`) | Offen | — |
| 2 | Issue-Vorlagen | `.github/issue-bodies/nuget-check.md` mit dem Prüfungstext aus Issue #95 anlegen (erste Zeile `@softwarewerkstatt`) | Offen | — |
| 3 | Issue-Vorlagen | `.github/issue-bodies/compliance-review.md` mit dem Prüfungstext aus Issue #95 anlegen (erste Zeile `@softwarewerkstatt`) | Offen | — |
| 4 | Workflows | `.github/workflows/scheduled-architecture-security-review.yml` anlegen: `schedule` (`0 5 1 * *`) + `workflow_dispatch`, `permissions` (`contents: read`, `issues: write`), `concurrency`-Gruppe, `GH_TOKEN`-Env | Offen | — |
| 5 | Workflows | Architektur-Workflow: Schritt „Review-Label sicherstellen" (`gh label create review-architektur-sicherheit --force`) | Offen | — |
| 6 | Workflows | Architektur-Workflow: Schritt „Duplikatprüfung" (`gh issue list --state open --label` + Skip-Protokoll bei Treffer) | Offen | — |
| 7 | Workflows | Architektur-Workflow: Schritt „Issue erstellen" (Titel `Architektur- und Sicherheitsreview %Y-%m` via `date`, `gh issue create --body-file --label`) | Offen | — |
| 8 | Workflows | `.github/workflows/scheduled-nuget-check.yml` anlegen: `schedule` (`0 5 * * 1`) + `workflow_dispatch`, gleiche Permissions-/Concurrency-Struktur | Offen | — |
| 9 | Workflows | NuGet-Workflow: Schritt „Review-Label sicherstellen" (`gh label create review-nuget --force`) | Offen | — |
| 10 | Workflows | NuGet-Workflow: Schritt „Duplikatprüfung" | Offen | — |
| 11 | Workflows | NuGet-Workflow: Schritt „Issue erstellen" (Titel `NuGet-Prüfung %G-W%V` via `date`, ISO-Kalenderwoche) | Offen | — |
| 12 | Workflows | `.github/workflows/scheduled-compliance-review.yml` anlegen: `schedule` (`0 5 1 1 *`) + `workflow_dispatch`, gleiche Permissions-/Concurrency-Struktur | Offen | — |
| 13 | Workflows | Compliance-Workflow: Schritt „Review-Label sicherstellen" (`gh label create review-compliance --force`) | Offen | — |
| 14 | Workflows | Compliance-Workflow: Schritt „Duplikatprüfung" | Offen | — |
| 15 | Workflows | Compliance-Workflow: Schritt „Issue erstellen" (Titel `Compliance- und Copyright-Review %Y` via `date`) | Offen | — |
| 16 | Verifikation | YAML-/Lint-Prüfung aller drei Workflow-Dateien (z. B. `actionlint` oder YAML-Parse) | Offen | — |
| 17 | Verifikation | Trockenlauf der Duplikat-Abfrage: `gh issue list --state open --label <label> --limit 1 --json number` lokal gegen das Repository | Offen | — |
| 18 | Dokumentation | Hilfeseite `docs/help/review-automatisierung.md` anlegen (Workflows, Cron-Zeitpunkte, Label-basierte Duplikatlogik, Default-Branch-Aktivierung) | Offen | — |
| 19 | Dokumentation | Eintrag der Hilfeseite in `docs/help/index.md` unter „Infrastruktur" | Offen | — |
| 20 | Dokumentation | Hinweis auf die Review-Workflows in `README.md` (Abschnitt „Branching und CI") | Offen | — |
| 21 | Dokumentation | Englischer Eintrag in `docs/RELEASE_NOTES.md` | Offen | — |
| 22 | Dokumentation | Deutscher Eintrag in `changes.log` mit Datumsüberschrift | Offen | — |
| 23 | Verifikation | `workflow_dispatch`-Probelauf je Workflow nach Erreichen des Default-Branches (`main`): Issue-Erstellung prüfen (Titel, Label, Body, Erwähnung) | Offen | — |
| 24 | Verifikation | Zweitlauf je Workflow: Duplikat-Skip im Actions-Log nachweisen (kein zweites Issue); Test-Issues nach Sichtprüfung schließen | Offen | — |
