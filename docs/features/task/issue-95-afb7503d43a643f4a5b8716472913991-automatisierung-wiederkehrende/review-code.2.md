# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### action.yml (create-review-issue)

- **Fehlerbehandlung** — Zeile 70: `echo "title=$TITLE_PREFIX $(date "$TITLE_DATE_FORMAT")" >> "$GITHUB_OUTPUT"` führt `date` in einer Command Substitution innerhalb eines `echo`-Kommandos aus. GitHub Actions startet `shell: bash` mit `-e`, aber `-e` greift nur auf den Exit-Status des Gesamtkommandos — schlägt `date` fehl (z. B. wegen eines ungültigen `title-date-format`), liefert `echo` trotzdem 0, der Step gilt als erfolgreich und es wird ein Issue mit Titel ohne Zeitraum angelegt. Im Schritt „Offenes Issue desselben Typs suchen" (Zeile 54) ist das Muster dagegen korrekt: dort steht `gh` in einer Zuweisung (`number=$(gh ...)`) und ein Fehler bricht den Step ab.

  Empfehlung: Das Datum vorab in eine eigene Zuweisung auslagern, damit ein `date`-Fehler den Step sichtbar abbrechen lässt: `period="$(date "$TITLE_DATE_FORMAT")"` gefolgt von `echo "title=$TITLE_PREFIX $period" >> "$GITHUB_OUTPUT"`.

- **Namenskonventionen und Einheitlichkeit** — Zeile 5 (Description): „Wird von den scheduled-*-review-Workflows ... aufgerufen" beschreibt die Aufrufer ungenau — zwei der drei Workflows passen auf das Muster `scheduled-*-review`, der dritte heißt `scheduled-nuget-check.yml`. Wer anhand der Description nach den Aufrufern sucht, übersieht den NuGet-Workflow.

  Empfehlung: Die Description auf „scheduled-*-Workflows" verallgemeinern oder die drei Aufrufer namentlich nennen.

## Geprüfte Dateien

Liste aller geprüften Dateien:
- `.github/actions/create-review-issue/action.yml`
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
