# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

### Issue-Body-Dateien (`.github/issue-bodies/`)

- [x] `.github/issue-bodies/architecture-security-review.md` — vorhanden; Text 1:1 mit dem Prüfungstext aus Issue #95, erste Zeile `@softwarewerkstatt`
- [x] `.github/issue-bodies/nuget-check.md` — vorhanden; Text 1:1 mit Issue #95 inklusive des PR-Umsetzungsabsatzes, erste Zeile `@softwarewerkstatt`
- [x] `.github/issue-bodies/compliance-review.md` — vorhanden; Text 1:1 mit Issue #95, erste Zeile `@softwarewerkstatt`

### Workflow `.github/workflows/scheduled-architecture-security-review.yml`

- [x] Datei angelegt — vorhanden
- [x] Trigger `schedule` mit Cron `0 5 1 * *` (monatlich, 1. Tag, 05:00 UTC) — vorhanden
- [x] Trigger `workflow_dispatch` — vorhanden
- [x] `permissions`-Block `contents: read` + `issues: write` — vorhanden
- [x] `concurrency`-Gruppe `scheduled-architecture-security-review.yml` ohne `cancel-in-progress` — vorhanden
- [x] Job `create-issue` auf `ubuntu-latest` mit `env: GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}` — vorhanden
- [x] Schritt `Checkout` (`actions/checkout@v4`) — vorhanden
- [x] Schritt „Review-Label sicherstellen": `gh label create "review-architektur-sicherheit" --color ededed --description ... --force` — vorhanden, idempotent
- [x] Schritt „Duplikatprüfung": `gh issue list --state open --label "review-architektur-sicherheit" --limit 1 --json number` mit Skip-Protokollzeile und `exists`-Output — vorhanden; Folgeschritte per `if:` übersprungen
- [x] Schritt „Issue-Titel bestimmen": `Architektur- und Sicherheitsreview $(date +%Y-%m)` — vorhanden
- [x] Schritt „Issue erstellen": `gh issue create --title ... --label "review-architektur-sicherheit" --body-file ".github/issue-bodies/architecture-security-review.md"` — vorhanden
- [x] Deutsche Kommentare mit ASCII-Umschrift (ae/oe/ue/ss) — vorhanden

### Workflow `.github/workflows/scheduled-nuget-check.yml`

- [x] Datei angelegt — vorhanden
- [x] Trigger `schedule` mit Cron `0 5 * * 1` (montags, 05:00 UTC) + `workflow_dispatch` — vorhanden
- [x] `permissions`, `concurrency`-Gruppe `scheduled-nuget-check.yml`, Job `create-issue`, `GH_TOKEN`, Checkout — vorhanden (gleiche Struktur)
- [x] Schritt „Review-Label sicherstellen": `gh label create "review-nuget" ... --force` — vorhanden
- [x] Schritt „Duplikatprüfung" mit Label `review-nuget` — vorhanden
- [x] Schritt „Issue-Titel bestimmen": `NuGet-Prüfung $(date +%G-W%V)` (ISO-Jahr + ISO-Kalenderwoche) — vorhanden
- [x] Schritt „Issue erstellen" mit `--body-file ".github/issue-bodies/nuget-check.md"` — vorhanden

### Workflow `.github/workflows/scheduled-compliance-review.yml`

- [x] Datei angelegt — vorhanden
- [x] Trigger `schedule` mit Cron `0 5 1 1 *` (1. Januar, 05:00 UTC) + `workflow_dispatch` — vorhanden
- [x] `permissions`, `concurrency`-Gruppe `scheduled-compliance-review.yml`, Job `create-issue`, `GH_TOKEN`, Checkout — vorhanden (gleiche Struktur)
- [x] Schritt „Review-Label sicherstellen": `gh label create "review-compliance" ... --force` — vorhanden
- [x] Schritt „Duplikatprüfung" mit Label `review-compliance` — vorhanden
- [x] Schritt „Issue-Titel bestimmen": `Compliance- und Copyright-Review $(date +%Y)` — vorhanden
- [x] Schritt „Issue erstellen" mit `--body-file ".github/issue-bodies/compliance-review.md"` — vorhanden

### Verifikation

- [x] YAML-/Syntax-Prüfung aller drei Workflow-Dateien — `yaml.safe_load` erfolgreich für alle drei Dateien (Parse ohne Fehler)
- [x] Trockenlauf der Duplikat-Abfrage — `gh issue list --state open --label review-nuget --limit 1 --json number` gegen das Repository ausgeführt: Rückgabe `[]`, Exit-Code 0 (Label-Filter und Auswertung funktionieren)

### Dokumentation

- [x] `docs/help/review-automatisierung.md` — vorhanden; beschreibt alle drei Workflows mit Dateiname, Cron-Zeitpunkt, Label und Titel-Schema, den Ablauf (Label-Sicherstellung → Duplikatprüfung → Erstellung), die Label-basierte Duplikatlogik inkl. blockierendem Altfall, Issue-Autor `github-actions[bot]`, `concurrency`-Schutz, Probelauf-Hinweis und den Aktivierungshinweis (Default-Branch)
- [x] Eintrag in `docs/help/index.md` unter „Infrastruktur" — vorhanden (Zeile 30)
- [x] Hinweis in `README.md` im Abschnitt „Branching und CI" — vorhanden (Zeile 89) mit Verweis auf die Hilfeseite
- [x] Englischer Eintrag in `docs/RELEASE_NOTES.md` unter „What's New" — vorhanden
- [x] Deutscher Eintrag in `changes.log` mit Datumsüberschrift `2026-11-16` — vorhanden

## Hinweise

- **Planbedingt zurückgestellt (keine Implementierungslücke):** Die Verifikationsaufgaben „`workflow_dispatch`-Probelauf je Workflow" und „Zweitlauf zum Nachweis des Duplikat-Skips" (Tasks 23–24, Plan-Umsetzungsschritt 8) sind laut Plan nur nach Erreichen der Workflows auf dem Default-Branch `main` ausführbar — Scheduled- und Dispatch-Trigger greifen nicht auf dem Feature-Branch/`staging`. Sie bleiben in der Tasks-Datei bewusst `Offen` und sind nach dem Staging-Pfad (`staging` → `main`) nachzuholen.
- Die Duplikat-Abfrage nutzt ergänzend `--jq '.[0].number // empty'` zur direkten Nummernausgabe — konsistente Erweiterung des geplanten `gh issue list`-Aufrufs, keine Abweichung.
- Der Issue-Titel `NuGet-Prüfung` enthält ein echtes Umlaut-ü im `run`-Block; die ASCII-Umschrift-Konvention gilt nur für Kommentare, der Titel ist der sichtbare Issue-Text — korrekt so.
- Die drei Review-Labels existieren im Repository noch nicht; sie werden beim ersten Lauf per `--force` angelegt (so geplant).
