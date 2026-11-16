# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

### Issue-Body-Dateien (`.github/issue-bodies/`)

- [x] `.github/issue-bodies/architecture-security-review.md` — vorhanden; Inhalt per Abgleich mit Issue #95 identisch (UTF-8), erste Zeile `@softwarewerkstatt`
- [x] `.github/issue-bodies/nuget-check.md` — vorhanden; Inhalt identisch mit Issue #95 inklusive des PR-Umsetzungsabsatzes, erste Zeile `@softwarewerkstatt`
- [x] `.github/issue-bodies/compliance-review.md` — vorhanden; Inhalt identisch mit Issue #95, erste Zeile `@softwarewerkstatt`

### Composite Action `.github/actions/create-review-issue/action.yml`

- [x] Datei angelegt — vorhanden; `runs: using: composite` (Zeile 28-29)
- [x] Input `label` — vorhanden (Zeile 9-11)
- [x] Input `label-description` — vorhanden (Zeile 12-14)
- [x] Input `title-prefix` — vorhanden (Zeile 15-17)
- [x] Input `title-date-format` — vorhanden (Zeile 18-20)
- [x] Input `body-file` — vorhanden (Zeile 21-23)
- [x] Input `gh-token` — vorhanden (Zeile 24-26); dokumentiert, dass Composite Actions keine `secrets` lesen
- [x] Schritt „Review-Label sicherstellen": `gh label create "$REVIEW_LABEL" --color ededed --description "$LABEL_DESCRIPTION" --force` — vorhanden (Zeile 33-43), idempotent
- [x] Schritt „Duplikatprüfung": `gh issue list --state open --label "$REVIEW_LABEL" --limit 1 --json number` mit Skip-Protokollzeile und `exists`-Output — vorhanden (Zeile 48-61)
- [x] Schritt „Issue-Titel bestimmen": `$TITLE_PREFIX $(date "$TITLE_DATE_FORMAT")` — vorhanden (Zeile 63-71), per `if:` übersprungen bei Treffer
- [x] Schritt „Issue erstellen": `gh issue create --title "$TITLE" --label "$REVIEW_LABEL" --body-file "$BODY_FILE"` — vorhanden (Zeile 73-85), per `if:` übersprungen bei Treffer
- [x] Expressions ausschließlich über `env:` an `run:`-Blöcke übergeben — erfüllt; alle `run:`-Blöcke referenzieren nur `"$VAR"`, keine `${{ ... }}`-Interpolation im Shell-Code
- [x] `GH_TOKEN` je `gh`-Schritt aus dem Input `gh-token` gesetzt — erfüllt (Zeile 37, 52, 79)
- [x] Deutsche Kommentare mit ASCII-Umschrift (ae/oe/ue/ss) — vorhanden

### Workflow `.github/workflows/scheduled-architecture-security-review.yml`

- [x] Datei angelegt — vorhanden
- [x] Trigger `schedule` mit Cron `0 5 1 * *` (monatlich, 1. Tag, 05:00 UTC) — vorhanden (Zeile 4-6)
- [x] Trigger `workflow_dispatch` — vorhanden (Zeile 8)
- [x] `permissions`-Block `contents: read` + `issues: write` — vorhanden (Zeile 10-12)
- [x] `concurrency`-Gruppe `scheduled-architecture-security-review.yml` ohne `cancel-in-progress` — vorhanden (Zeile 17-18)
- [x] Job `create-issue` auf `ubuntu-latest` — vorhanden (Zeile 21-22)
- [x] Schritt `Checkout` (`actions/checkout@v4`) — vorhanden (Zeile 24-25)
- [x] Schritt „Review-Issue erstellen": `uses: ./.github/actions/create-review-issue` — vorhanden (Zeile 30-31)
- [x] `with:`-Parameter — vorhanden: `label: review-architektur-sicherheit`, `label-description`, `title-prefix: Architektur- und Sicherheitsreview`, `title-date-format: '+%Y-%m'`, `body-file: .github/issue-bodies/architecture-security-review.md`, `gh-token: ${{ secrets.GITHUB_TOKEN }}` (Zeile 32-38)

### Workflow `.github/workflows/scheduled-nuget-check.yml`

- [x] Datei angelegt — vorhanden
- [x] Trigger `schedule` mit Cron `0 5 * * 1` (montags, 05:00 UTC) + `workflow_dispatch` — vorhanden (Zeile 4-8)
- [x] `permissions`, `concurrency`-Gruppe `scheduled-nuget-check.yml`, Job `create-issue` auf `ubuntu-latest`, Checkout — vorhanden (gleiche Struktur)
- [x] `uses:`-Aufruf der Composite Action — vorhanden (Zeile 30-31)
- [x] `with:`-Parameter — vorhanden: `label: review-nuget`, `title-prefix: NuGet-Prüfung`, `title-date-format: '+%G-W%V'` (ISO-Jahr + ISO-Kalenderwoche), `body-file: .github/issue-bodies/nuget-check.md`, `gh-token` (Zeile 32-40)

### Workflow `.github/workflows/scheduled-compliance-review.yml`

- [x] Datei angelegt — vorhanden
- [x] Trigger `schedule` mit Cron `0 5 1 1 *` (1. Januar, 05:00 UTC) + `workflow_dispatch` — vorhanden (Zeile 4-8)
- [x] `permissions`, `concurrency`-Gruppe `scheduled-compliance-review.yml`, Job `create-issue` auf `ubuntu-latest`, Checkout — vorhanden (gleiche Struktur)
- [x] `uses:`-Aufruf der Composite Action — vorhanden (Zeile 30-31)
- [x] `with:`-Parameter — vorhanden: `label: review-compliance`, `title-prefix: Compliance- und Copyright-Review`, `title-date-format: '+%Y'`, `body-file: .github/issue-bodies/compliance-review.md`, `gh-token` (Zeile 32-38)

### Verifikation

- [x] YAML-/Syntax-Prüfung aller drei Workflow-Dateien und der `action.yml` — `yaml.safe_load` erfolgreich für alle vier Dateien (Parse ohne Fehler)
- [x] Trockenlauf der Duplikat-Abfrage — `gh issue list --state open --label review-nuget --limit 1 --json number` gegen das Repository ausgeführt: Rückgabe `[]`, Exit-Code 0

### Dokumentation

- [x] `docs/help/review-automatisierung.md` — vorhanden; beschreibt alle drei Workflows mit Dateiname, Cron-Zeitpunkt, Label und Titel-Schema, den Ablauf (Label-Sicherstellung → Duplikatprüfung → Erstellung), die Label-basierte Duplikatlogik inkl. blockierendem Altfall, Issue-Autor `github-actions[bot]`, `concurrency`-Schutz, Probelauf-Hinweis und den Aktivierungshinweis (Default-Branch)
- [x] Eintrag in `docs/help/index.md` unter „Infrastruktur" — vorhanden (Zeile 30)
- [x] Hinweis in `README.md` im Abschnitt „Branching und CI" — vorhanden (Zeile 89) mit Verweis auf die Hilfeseite
- [x] Englischer Eintrag in `docs/RELEASE_NOTES.md` unter „What's New" — vorhanden (Zeile 12)
- [x] Deutscher Eintrag in `changes.log` mit Datumsüberschrift `2026-11-16` — vorhanden (Zeile 3)

## Hinweise

- **Planbedingt zurückgestellt (keine Implementierungslücke):** Die Verifikationsaufgaben „`workflow_dispatch`-Probelauf je Workflow" und „Zweitlauf zum Nachweis des Duplikat-Skips" (Tasks 21–22, Plan-Umsetzungsschritt 9) sind laut Plan erst ausführbar, wenn die Workflows über den Staging-Pfad (`staging` → `main`) den Default-Branch erreicht haben — Scheduled- und Dispatch-Trigger greifen nicht auf dem Feature-Branch/`staging`. Sie bleiben in der Tasks-Datei bewusst `Offen` und sind nach der Promotion nach `main` nachzuholen.
- **Struktur seit Iteration 2:** Die gemeinsame Schrittfolge (Label-Sicherstellung → Duplikatprüfung → Titelermittlung → Issue-Erstellung) liegt in der Composite Action `create-review-issue`; die drei Workflow-Dateien enthalten nur noch Trigger, Permissions, `concurrency` und den parametrisierten `uses:`-Aufruf — umgesetzt wie in der Designentscheidung „Gemeinsame Ablauflogik" des aktualisierten Plans.
- Die Duplikat-Abfrage nutzt ergänzend `--jq '.[0].number // empty'` zur direkten Nummernausgabe — konsistente Erweiterung des geplanten `gh issue list`-Aufrufs, keine Abweichung.
- Der `title-prefix` `NuGet-Prüfung` enthält ein echtes Umlaut-ü; die ASCII-Umschrift-Konvention gilt nur für Kommentare, der Titel ist der sichtbare Issue-Text — korrekt so. Die drei Issue-Body-Dateien liegen korrekt als UTF-8 vor (im Plan gefordert).
- Die drei Review-Labels existieren im Repository noch nicht; sie werden beim ersten Lauf per `--force` angelegt (so geplant).
