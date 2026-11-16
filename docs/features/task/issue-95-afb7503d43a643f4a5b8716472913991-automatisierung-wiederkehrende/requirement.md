# Übersetzte Anforderung: Automatisierung wiederkehrender Architektur-, Sicherheits- und Compliance-Reviews

Branch: `task/issue-95-afb7503d43a643f4a5b8716472913991-automatisierung-wiederkehrende`
Quelle: GitHub Issue #95 im Repository `martin-stromberg/VideoPlayer-App`.

## Fachliche Zusammenfassung

Es sind drei zeitgesteuerte GitHub-Actions-Workflows einzuführen, die
periodisch GitHub-Issues erzeugen und darin den Account
`@softwarewerkstatt` erwähnen, um dessen automatische Analyse und
Bearbeitung anzustoßen. Jeder Workflow deckt eine Prüfungsart ab:
monatliches Architektur- und Sicherheitsreview (am 1. eines Monats),
wöchentliche NuGet-Paketprüfung und jährliches Compliance-/Copyright-Review
(am 1. Januar). Vor der Issue-Erstellung prüft jeder Workflow, ob bereits
ein offenes Issue desselben Typs aus einem früheren Zyklus existiert, und
überspringt die Erstellung in diesem Fall (Duplikatvermeidung).

## Fachlicher Scope

- **Verarbeitungseinheit:** Ein Review-Zyklus je Prüfungsart — also das
  Ereignisbündel „fälliger Review-Zeitpunkt → Duplikatprüfung →
  Issue-Erstellung". Zum Scope gehört ausschließlich das automatische
  Anlegen der Beauftragungs-Issues; die inhaltliche Durchführung der
  Reviews liegt bei `@softwarewerkstatt` außerhalb dieses Features.
- **Auslösende Ereignisse:** Zeitgesteuerter Cron-Trigger pro Prüfungsart
  (monatlich am 1. Tag, wöchentlich, jährlich am 1. Januar).
- **Erwartete Wirkung:** Im Repository erscheint zum jeweiligen Zeitpunkt
  ein neues, offenes Issue mit aussagekräftigem Titel, dem im Issue
  vorgegebenen Prüfungstext und der Erwähnung `@softwarewerkstatt` — für
  nachgelagerte Prozesse (automatische Analyse) und im Issue-Tracker
  sichtbar. Besteht bereits ein offenes Issue desselben Typs, ist die
  sichtbare Wirkung „kein neues Issue" (Zählung/Protokoll im
  Workflow-Lauf).
- **Umgang mit fehlenden Objekten:**
  - Offenes Issue desselben Typs nicht vorhanden → erzeugen.
  - Offenes Issue desselben Typs vorhanden → verwerfen (kein neues Issue
    anlegen; der Zyklus gilt als abgedeckt, bis das Altfall-Issue
    geschlossen wird).
  - Identifikationsmerkmal für „desselben Typs" (z. B. dediziertes Label
    oder fester Titel-Bestandteil) nicht vorhanden → beim ersten Lauf
    erzeugen bzw. implizit durch den Issue-Titel herstellen.
- **Scope-Ausschlüsse:**
  - Keine Durchführung der Reviews selbst — die Workflows analysieren
    weder NuGet-Pakete noch Code; sie beauftragen nur via Issue.
  - Keine automatische Umsetzung von NuGet-Updates durch den Workflow —
    die im Issue-Text geforderte PR-Umsetzung obliegt
    `@softwarewerkstatt`.
  - Keine Änderungen am App-Code (reine CI-/Repository-Infrastruktur
    unter `.github/workflows/` plus Dokumentation).
  - Keine Benachrichtigungskanäle außerhalb von GitHub (keine
    E-Mail/Chat-Integration).
  - Kein manueller Eingriff in den Zyklus: Geschlossene oder manuell
    angelegte Fremd-Issues werden nicht speziell behandelt.

## Betroffene Klassen und Komponenten

Keine C#-Klassen betroffen; ausschließlich Workflow- und
Dokumentationsartefakte:

- Drei neue Workflow-Dateien unter `.github/workflows/` (kebab-case,
  analog `pr-checks.yml`, `staging-merge.yml`, `main-merge.yml`), z. B.:
  - `scheduled-architecture-security-review.yml` (Cron: monatlich, 1. Tag)
  - `scheduled-nuget-check.yml` (Cron: wöchentlich)
  - `scheduled-compliance-review.yml` (Cron: jährlich, 1. Januar)
- Ggf. Issue-Body-Vorlagen als Dateien im Repository (Annahme: z. B.
  unter `.github/`), falls der Issue-Text nicht inline im Workflow
  gepflegt wird.
- Dokumentation der Automatisierung innerhalb des Repositories
  (Akzeptanzkriterium „vollständig innerhalb des Repositories
  dokumentiert"), z. B. unter `docs/help/` bzw. Workflow-Kommentaren;
  ggf. Ergänzung in `README.md`/`docs/RELEASE_NOTES.md` gemäß
  Projektprozess.
- Tests: keine Unit-Tests; Verifizierung über Probelauf des Workflows
  (siehe Offene Fragen zu `workflow_dispatch`).

## Implementierungsansatz

- Je Prüfungsart ein eigener Workflow mit `on: schedule` (Cron-Ausdruck;
  GitHub Actions wertet Cron in UTC aus) und minimalem
  `permissions`-Block mit `issues: write`.
- Duplikatprüfung als erster Schritt: Abfrage offener Issues desselben
  Typs (z. B. via `gh issue list --state open` mit Label- oder
  Titel-Filter, oder `actions/github-script` mit REST-API). Nur bei
  leerem Ergebnis folgt die Issue-Erstellung.
- Issue-Erstellung via `gh issue create` (bzw. `github-script`) mit
  aussagekräftigem Titel (z. B. inkl. Zeitraum/Jahr) und dem exakt im
  Issue #95 vorgegebenen Body inkl. `@softwarewerkstatt` als erster
  Zeile.
- Berechtigungen minimal halten (`contents: read` bzw. nur `issues:
  write`), konsistent zu den bestehenden Workflows.
- Kommentare im Workflow-Stil des Projekts (bestehende Workflows
  kommentieren deutsch und umschreiben Umlaute als ae/oe/ue).
- Hinweis: Scheduled Workflows laufen nur auf dem Default-Branch —
  die Automatisierung wird erst aktiv, wenn die Workflows über den
  Staging-Pfad (`staging` → `main`) im Default-Branch angekommen sind.

## Konfiguration

- Cron-Zeitpunkte sind feste Bestandteile der Workflow-Dateien
  (`schedule`-Cron), nicht zur Laufzeit konfigurierbar — Änderungen
  erfolgen per Commit.
- Identifikationsmerkmal für die Duplikatprüfung (Label-Name bzw.
  Titel-Marker) ist als Konstante im Workflow zu hinterlegen.
- Kein Konfigurationsbedarf auf Anwendungsebene; keine Secrets über
  `GITHUB_TOKEN` hinaus erforderlich.

## Offene Fragen

1. **Wochentag/Uhrzeit der wöchentlichen NuGet-Prüfung:** Der Issue nennt
   nur „wöchentlich" — Annahme: ein fester Wochentag (z. B. Montag) in
   den frühen Morgenstunden; Uhrzeit wegen UTC-Cron ggf. auf deutsche
   Ortszeit umzurechnen.
2. **Duplikaterkennung:** Label-basiert (robust, erfordert ggf. das
   Anlegen des Labels beim ersten Lauf) oder Titelsuche (ohne
   Label-Verwaltung)? Annahme: Label-basiert mit festem Label-Namen pro
   Prüfung.
3. **Manuelle Auslösung:** Darf `workflow_dispatch` zusätzlich ergänzt
   werden, um die Workflows ohne Warten auf den Cron-Zeitpunkt testen zu
   können? Annahme: ja.
4. **Account `@softwarewerkstatt`:** Zu bestätigen, dass der Account als
   GitHub-Nutzer/-Bot existiert und die Erwähnung die gewünschte
   automatische Bearbeitung auslöst.
5. **Issue-Metadaten:** Sollen die Issues zusätzlich Assignees, Labels
   (über das Duplikat-Label hinaus) oder Meilensteine erhalten? Annahme:
   nein, nur Titel + Body wie vorgegeben.
