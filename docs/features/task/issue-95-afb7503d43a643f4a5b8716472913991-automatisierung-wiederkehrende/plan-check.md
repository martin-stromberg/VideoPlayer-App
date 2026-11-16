# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Drei zeitgesteuerte GitHub-Actions-Workflows unter `.github/workflows/` einführen | Drei neue Workflow-Dateien (`scheduled-architecture-security-review.yml`, `scheduled-nuget-check.yml`, `scheduled-compliance-review.yml`) mit `on: schedule` + `workflow_dispatch`; Umsetzungsschritte 2–4 | YAML-/Lint-Prüfung (Schritt 5), `workflow_dispatch`-Probelauf je Workflow (Schritt 8, Akzeptanzszenarien 1/3/4) | Abgedeckt |
| Monatliches Architektur- und Sicherheitsreview am 1. eines Monats | Cron `0 5 1 * *`, Label `review-architektur-sicherheit`, Titel `Architektur- und Sicherheitsreview YYYY-MM`, Body `architecture-security-review.md` (Designentscheidungen, Programmablauf, Schritt 2) | Probelauf + Tracker-Sichtprüfung; realer Cron-Lauf am Folgemonat (Szenario 1) | Abgedeckt |
| Wöchentliche NuGet-Paketprüfung | Cron `0 5 * * 1` (Montag 05:00 UTC — Entscheidung zu Offener Frage 1), Label `review-nuget`, Titel mit ISO-Kalenderwoche `%G-W%V` (Schritt 3) | Probelauf + Tracker-Kontrolle (Szenario 3) | Abgedeckt |
| Jährliches Compliance-/Copyright-Review am 1. Januar | Cron `0 5 1 1 *`, Label `review-compliance`, Titel mit Jahreszahl `%Y` (Schritt 4) | Probelauf + Tracker-Kontrolle (Szenario 4) | Abgedeckt |
| Duplikatprüfung: offenes Issue desselben Typs → Erstellung überspringen | Label-basierte Duplikaterkennung via `gh issue list --state open --label "<label>" --limit 1`; Treffer → Protokollzeile, Lauf endet erfolgreich ohne Issue (Programmablauf Schritte 4–5; Designentscheidung) | Zweitlauf desselben Workflows (Verifikationstabelle, Szenario 2) | Abgedeckt |
| Issue mit aussagekräftigem Titel, exakt vorgegebenem Body aus Issue #95 und `@softwarewerkstatt` als erster Zeile | Titel-Schema mit dynamischem Zeitraum via `date`; drei Body-Dateien unter `.github/issue-bodies/` mit dem vorgegebenen Text, erste Zeile `@softwarewerkstatt`; Erstellung via `gh issue create --body-file` (Schritt 1, Programmablauf Schritt 7) | Sichtprüfung des erzeugten Issues beim Probelauf (Titel, Label, Body, Erwähnung — Schritt 8) | Abgedeckt |
| Identifikationsmerkmal für „desselben Typs" — beim ersten Lauf herstellen, wenn nicht vorhanden | Dediziertes Label je Prüfungsart, pro Lauf idempotent via `gh label create --force` sichergestellt (Programmablauf Schritt 3, Scope-Klärung) | Szenario 1 startet explizit im Ausgangszustand „Label existiert noch nicht" | Abgedeckt |
| Minimaler `permissions`-Block, keine Secrets über `GITHUB_TOKEN` hinaus | `contents: read` + `issues: write` je Workflow, `GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}` auf Job-Ebene (Konfigurationstabelle, Programmablauf Schritt 1) | Lint-/Syntax-Prüfung der Permissions-Struktur (Schritt 5); Fehlverhalten bei Rechteproblemen = sichtbarer Fehlerlauf (Szenario 5) | Abgedeckt |
| Workflow-Konventionen: kebab-case-Dateinamen, deutsche Kommentare mit ASCII-Umschrift (ae/oe/ue/ss) | Ausdrücklich in Umsetzungsschritt 2 gefordert; Label-Namen ASCII-transliteriert (Designentscheidung Duplikaterkennung) | YAML-/Lint-Prüfung (Schritt 5); Review-Prozess | Abgedeckt |
| Vollständige Dokumentation innerhalb des Repositories | Neue Hilfeseite `docs/help/review-automatisierung.md` + Index-Eintrag, `README.md` (Abschnitt „Branching und CI"), `docs/RELEASE_NOTES.md`, `changes.log` (Schritte 6–7) | Voraussetzung/Hinweis in den Schritten 6–7; Inhaltliche Vorgaben inkl. Default-Branch-Aktivierung benannt | Abgedeckt |
| Scope-Ausschlüsse (keine Review-Durchführung, keine NuGet-Updates, kein App-Code, keine externen Benachrichtigungen, kein manueller Eingriff, keine Zusatz-Metadaten) | Alle fünf Ausschlüsse aus `requirement.md` übernommen und um zwei konkretisierte Ausschlüsse (Metadaten, verzögerte Aktivierung) erweitert — sämtlich mit Begründung und Status `bestätigt (2026-11-16)` | — | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Nur ausfüllen, wenn Status `Plan lückenhaft` — nicht zutreffend.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Zeitgesteuerte Issue-Erstellung (Systemablauf auf GitHub, kein App-UI-Fluss) | Kein FlaUI-E2E-Test; stattdessen `workflow_dispatch`-Probelauf je Workflow mit Tracker-Sichtprüfung + Zweitlauf für Duplikatvermeidung | Nicht erforderlich mit Begründung: Die Anforderung berührt keinen über die App-UI erreichbaren Ablauf; die sichtbare Wirkung (Issue im GitHub-Tracker) kann die FlaUI-Suite `VideoPlayer.E2E.Tests` grundsätzlich nicht beobachten. Der Probelauf ist ein echter Systemdurchlauf vom Trigger bis zum sichtbaren Issue — für reine Infrastruktur-Workflows sachgerecht. |
| Fehlerfall (fehlende Body-Datei, `gh`-Fehler) | — | Nicht erforderlich mit Begründung: Fehler ist als roter Workflow-Lauf in der Actions-Liste sichtbar (Szenario 5); kein stilles Überspringen; Beobachtung erfolgt über GitHub Actions, nicht über die App-UI. |
| Bestehende E2E-Suite | Bestehende E2E-Tests nicht betroffen (kein App-Code geändert) | Nicht erforderlich mit Begründung |

## Fehlende oder unvollständige Planbestandteile

Nur ausfüllen, wenn Status `Plan lückenhaft` — nicht zutreffend.

## Hinweise

- Der Probelauf (Umsetzungsschritt 8) erzeugt reale Issues im Tracker; der Plan legt den Umgang fest (Test-Issue nach Sichtprüfung schließen, Duplikatprüfung verhindert Folge-Issues). Bei der Umsetzung sicherstellen, dass der Probelauf bewusst und zeitnah erfolgt, da ein offenes Test-Issue den nächsten regulären Zyklus unterdrückt (blockierender Altfall — im Plan benannt).
- Umsetzungsschritt 1 setzt voraus, dass die exakten Prüfungstexte aus Issue #95 eingesehen wurden — beim Umsetzen die Body-Dateien 1:1 gegen das Issue abgleichen.
- `actionlint` ist als lokales Prüfwerkzeug nur eine Empfehlung („z. B."); falls nicht verfügbar, ist die im Plan genannte Alternative (YAML-Parse + `gh workflow view` nach Push) zu nutzen.
- Betroffene bestehende Tests: korrekt mit „Keine" angegeben — es wird kein ausführbarer Code geändert; der grüne Test-Ausgangszustand (1484 Unit- + 74 E2E-Tests) ist in `inventory/tests.md` dokumentiert.
