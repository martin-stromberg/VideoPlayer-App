# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### scheduled-architecture-security-review.yml, scheduled-compliance-review.yml, scheduled-nuget-check.yml (Workflows)

- **Doppelter Code** — Die drei neuen Workflow-Dateien (~68 Zeilen) sind bis auf Cron-Zeitplan, Label-Name/-Beschreibung, Titelpräfix/Datumsformat und Body-Dateipfad nahezu identisch: gleiche Job-Struktur, gleiche vier Schritte (`gh label create --force`, Duplikatsuche via `gh issue list`, Titelermittlung, `gh issue create`) mit wortgleichen Kommentaren und identischem Bash-Code. Jede fachliche Änderung an der Ablauflogik (z. B. Duplikatkriterium, Label-Farbe, Fehlerbehandlung) müsste künftig dreifach gepflegt werden und kann auseinanderlaufen.

  Empfehlung: Die gemeinsame Ablauflogik in einen wiederverwendbaren Workflow (`on: workflow_call` mit Inputs wie `label`, `title-prefix`, `title-date-format`, `body-file`) oder eine Composite Action auslagern. Die drei Dateien behalten dann nur `schedule`/`workflow_dispatch` und den `uses:`-Aufruf mit den jeweiligen Parametern.

- **Hardcodierte Werte** — Das Label-Literal steht in jeder Datei dreimal: z. B. `scheduled-nuget-check.yml` Zeilen 33 (`gh label create "review-nuget"`), 45 (`gh issue list --label "review-nuget"`) und 68 (`gh issue create --label "review-nuget"`); analog `review-architektur-sicherheit` und `review-compliance` in den beiden anderen Dateien. Eine inkonsistente Änderung an nur einer Stelle würde die Duplikatprüfung stillschweigend außer Kraft setzen (Label wird erstellt und gesetzt, die Suche findet aber nichts bzw. umgekehrt).

  Empfehlung: Den Label-Namen einmal als Job-`env:`-Konstante definieren (z. B. `REVIEW_LABEL: review-nuget`) und in allen drei Schritten `"$REVIEW_LABEL"` verwenden.

- **Security (Quoting/Injection in `run:`-Blöcken)** — Zeile 65 (`scheduled-architecture-security-review.yml`), Zeile 65 (`scheduled-compliance-review.yml`) und Zeile 67 (`scheduled-nuget-check.yml`): `--title "${{ steps.title.outputs.title }}"` interpoliert einen GitHub-Actions-Ausdruck direkt in den Shell-Code. Der Wert ist aktuell vollständig workflow-kontrolliert (`date`-Ausgabe), sodass kein aktiver Injection-Pfad besteht; das Muster umgeht jedoch das Shell-Quoting und ist von GitHub als Anti-Pattern dokumentiert — eine spätere Änderung der Titelquelle (z. B. aus Issue-/Event-Daten) würde sofort ein Injektionsrisiko eröffnen.

  Empfehlung: Den Wert über eine `env:`-Variable am Step übergeben (`env: TITLE: ${{ steps.title.outputs.title }}`) und im `run:`-Block `--title "$TITLE"` verwenden.

## Geprüfte Dateien

Liste aller geprüften Dateien:
- `.github/workflows/scheduled-architecture-security-review.yml`
- `.github/workflows/scheduled-compliance-review.yml`
- `.github/workflows/scheduled-nuget-check.yml`
- `.github/issue-bodies/architecture-security-review.md`
- `.github/issue-bodies/compliance-review.md`
- `.github/issue-bodies/nuget-check.md`
- `docs/help/review-automatisierung.md`
- `docs/help/index.md`
- `README.md`
- `docs/RELEASE_NOTES.md`
- `changes.log`
