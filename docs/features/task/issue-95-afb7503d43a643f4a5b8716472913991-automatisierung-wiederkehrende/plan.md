# Umsetzungsplan: Automatisierung wiederkehrender Architektur-, Sicherheits- und Compliance-Reviews

## Übersicht

Es werden drei zeitgesteuerte GitHub-Actions-Workflows unter
`.github/workflows/` eingeführt, die periodisch GitHub-Issues erzeugen
und darin `@softwarewerkstatt` erwähnen, um die externe
Bearbeitungsautomatisierung anzustoßen: monatliches Architektur- und
Sicherheitsreview (am 1. eines Monats), wöchentliche NuGet-Prüfung und
jährliches Compliance-/Copyright-Review (am 1. Januar). Jeder Workflow
prüft vor der Erstellung über ein dediziertes Label, ob bereits ein
offenes Issue desselben Typs existiert, und überspringt die Erstellung
in diesem Fall. Betroffen sind ausschließlich `.github/` sowie
Dokumentationsdateien — kein App-Code.

## Scope-Klärung

Verbindliche fachliche Abgrenzung vor der Umsetzung — übernommen bzw.
konkretisiert aus dem Abschnitt `Fachlicher Scope` der
`requirement.md`:

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Ein Review-Zyklus je Prüfungsart — das Ereignisbündel „fälliger Review-Zeitpunkt → Label-Sicherstellung → Duplikatprüfung → Issue-Erstellung". Zum Scope gehört nur das automatische Anlegen der Beauftragungs-Issues; die inhaltliche Review-Durchführung liegt bei `@softwarewerkstatt` außerhalb dieses Features. |
| Auslösende Ereignisse | `schedule`-Cron-Trigger je Prüfungsart: monatlich `0 5 1 * *` (1. Tag, 05:00 UTC), wöchentlich `0 5 * * 1` (Montag, 05:00 UTC), jährlich `0 5 1 1 *` (1. Januar, 05:00 UTC). Zusätzlich `workflow_dispatch` für manuelle Probeläufe. |
| Erwartete Wirkung | Im Repository erscheint ein neues, offenes Issue mit aussagekräftigem Titel inkl. Zeitraum, dem exakt in Issue #95 vorgegebenen Prüfungstext mit `@softwarewerkstatt` als erster Zeile sowie dem Review-Label. Existiert bereits ein offenes Issue desselben Typs, ist die sichtbare Wirkung „kein neues Issue" (Protokollzeile im Workflow-Lauf, Lauf endet erfolgreich). |
| Umgang mit fehlenden Objekten | Review-Label nicht vorhanden → im selben Lauf erzeugen (`gh label create --force`, idempotent). Offenes Issue desselben Typs nicht vorhanden → erzeugen. Offenes Issue desselben Typs vorhanden → verwerfen (kein neues Issue; der Zyklus gilt als abgedeckt, bis das Altfall-Issue geschlossen wird). Issue-Body-Datei fehlt → Lauf schlägt fehl (sichtbarer Fehler, kein stilles Überspringen). |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Keine inhaltliche Durchführung der Reviews — die Workflows analysieren weder NuGet-Pakete noch Code, sie beauftragen nur via Issue. | Analyse und Bearbeitung obliegen der durch die `@softwarewerkstatt`-Erwähnung ausgelösten externen Automatisierung (Zweck des Features laut Issue #95). | bestätigt (2026-11-16) |
| 2 | Keine automatische Umsetzung von NuGet-Updates durch den Workflow. | Die im Issue-Text geforderte PR-Umsetzung unbedenklicher Updates ist Aufgabe von `@softwarewerkstatt`, nicht des Repositorium-Workflows. | bestätigt (2026-11-16) |
| 3 | Keine Änderungen am App-Code. | Reine CI-/Repository-Infrastruktur unter `.github/` plus Dokumentation; keine C#-Artefakte betroffen. | bestätigt (2026-11-16) |
| 4 | Keine Benachrichtigungskanäle außerhalb von GitHub (keine E-Mail-/Chat-Integration). | Issue #95 verlangt ausschließlich Issue-Erzeugung im Tracker. | bestätigt (2026-11-16) |
| 5 | Kein manueller Eingriff in den Zyklus und keine Sonderbehandlung geschlossener oder manuell angelegter Fremd-Issues. | Die Duplikaterkennung prüft ausschließlich das dedizierte Review-Label; manuell angelegte Issues ohne Label blockieren den Zyklus nicht. | bestätigt (2026-11-16) |
| 6 | Keine zusätzlichen Issue-Metadaten (Assignees, Milestones, weitere Labels über das Duplikat-Label hinaus). | Issue #95 verlangt nur Titel, vorgegebenen Body und die Erwähnung; Annahme aus `requirement.md` (Offene Frage 5). | bestätigt (2026-11-16) |
| 7 | Keine sofortige Aktivierung nach Merge nach `staging`. | Scheduled- und Dispatch-Trigger greifen nur auf dem Default-Branch; die Automatisierung wird erst aktiv, wenn die Workflows über `staging` → `main` angekommen sind. | bestätigt (2026-11-16) |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Duplikaterkennung | Dediziertes Label je Prüfungsart (`review-architektur-sicherheit`, `review-nuget`, `review-compliance`) plus `gh issue list --state open --label "<label>" --limit 1`; Label wird je Lauf vorab per `gh label create --force` idempotent sichergestellt. | Es existiert noch kein Review-Label — die Festlegung liegt beim Plan. Label-Suche ist robust gegen Titelvariationen (Zeitraum im Titel würde eine Titelsuche pro Zyklus treffen); `--force` macht den ersten Lauf ohne manuelle Label-Anlage möglich. Label-Namen ASCII-transliteriert analog zur Umlaut-Umschrift-Konvention. |
| Ablage der Issue-Bodies | Separate UTF-8-Markdown-Dateien unter `.github/issue-bodies/` (`architecture-security-review.md`, `nuget-check.md`, `compliance-review.md`), Erstellung via `gh issue create --body-file`. | Der in Issue #95 exakt vorgegebene Prüfungstext bleibt eigenständig diff- und reviewbar statt in YAML-Blöcken eingebettet; Umlaute bleiben als UTF-8 lesbar. Preis: `actions/checkout` und `contents: read` — bewusst akzeptiert. |
| Ausführungswerkzeug | `gh` CLI in `bash`-Schritten mit `GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}` (statt `actions/github-script`). | Konsistent zu den bestehenden Workflows (`gh pr create`, `gh release upload`); keine zusätzliche Action-Abhängigkeit. |
| Cron-Zeitpunkte | 05:00 UTC für alle drei Workflows; Wochentag Montag für die wöchentliche Prüfung. | GitHub Actions wertet Cron in UTC aus; 05:00 UTC entspricht ca. 06:00/07:00 Uhr deutscher Zeit — früher Morgen, bevor der Arbeitstag beginnt. Annahme aus `requirement.md` (Offene Frage 1). |
| Zusätzlicher Trigger | `workflow_dispatch` in allen drei Workflows. | Ermöglicht Probeläufe und sofortige Auslösung ohne Warten auf den Cron-Zeitpunkt; Annahme aus `requirement.md` (Offene Frage 3). Wirkung erst ab Default-Branch. |
| Laufzeitumgebung | `ubuntu-latest`, ein Job `create-issue` je Workflow. | Wie die Release-Workflows; `gh` und `bash` sind vorinstalliert. |
| Parallelitätsschutz | `concurrency`-Gruppe je Workflow (Name = Workflow-Dateiname) ohne `cancel-in-progress`. | Verhindert, dass ein manueller Dispatch und ein Cron-Lauf gleichzeitig zwischen Duplikatprüfung und Erstellung ein Doppel-Issue erzeugen. |
| Issue-Titel | Festes Schema je Prüfungsart mit dynamischem Zeitraum via `date`: `Architektur- und Sicherheitsreview %Y-%m`, `NuGet-Prüfung %G-W%V` (ISO-Kalenderwoche), `Compliance- und Copyright-Review %Y`. | Akzeptanzkriterium „aussagekräftige Titel"; der Zeitraum macht den Zyklus im Tracker sofort erkennbar. |

## Programmabläufe

### Gemeinsamer Ablauf (alle drei Workflows)

1. Der Trigger feuert (`schedule`-Cron oder `workflow_dispatch`); der
   Job `create-issue` startet auf `ubuntu-latest` mit `env:
   GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}`.
2. Schritt `Checkout`: `actions/checkout@v4` stellt die Issue-Body-Datei
   bereit.
3. Schritt „Review-Label sicherstellen": `gh label create "<label>"
   --color ededed --description "..." --force` — idempotent, legt das
   Label beim ersten Lauf an bzw. aktualisiert es.
4. Schritt „Duplikatprüfung": `gh issue list --state open --label
   "<label>" --limit 1 --json number` liefert die Trefferzahl.
5. Bei mindestens einem Treffer: Protokollzeile „Offenes Issue #N
   existiert bereits — kein neues Issue" ausgeben und der Lauf endet
   erfolgreich (kein Issue).
6. Schritt „Issue-Titel bestimmen": Zeitraum über `date` berechnen
   (Monat `%Y-%m`, ISO-Kalenderwoche `%G-W%V` bzw. Jahr `%Y`) und den
   vollständigen Titel zusammensetzen.
7. Schritt „Issue erstellen": `gh issue create --title "<titel>"
   --label "<label>" --body-file ".github/issue-bodies/<datei>.md"`;
   die URL des neuen Issues wird ins Lauf-Protokoll geschrieben.
8. Schlägt ein `gh`-Aufruf fehl (z. B. fehlende Rechte), endet der
   Schritt nicht-null und der Workflow-Lauf ist als fehlgeschlagen
   sichtbar — kein stilles Überspringen.

Beteiligte Klassen/Komponenten: Workflow-Datei unter
`.github/workflows/`, `actions/checkout@v4`, `gh` CLI (`label create`,
`issue list`, `issue create`), `.github/issue-bodies/*.md`,
GitHub-Issues- und -Labels-API.

### Monatliches Architektur- und Sicherheitsreview (`scheduled-architecture-security-review.yml`)

Gemeinsamer Ablauf mit: Cron `0 5 1 * *`, Label
`review-architektur-sicherheit`, Titel `Architektur- und
Sicherheitsreview <YYYY>-<MM>`, Body-Datei
`.github/issue-bodies/architecture-security-review.md` (Text exakt aus
Issue #95, beginnt mit `@softwarewerkstatt`).

### Wöchentliche NuGet-Prüfung (`scheduled-nuget-check.yml`)

Gemeinsamer Ablauf mit: Cron `0 5 * * 1` (Montag), Label `review-nuget`,
Titel `NuGet-Prüfung <ISO-Jahr>-W<KW>` (`date +%G-W%V`, ISO-Jahr statt
Kalenderjahr wegen Jahreswechsel), Body-Datei
`.github/issue-bodies/nuget-check.md`.

### Jährliches Compliance- und Copyright-Review (`scheduled-compliance-review.yml`)

Gemeinsamer Ablauf mit: Cron `0 5 1 1 *` (1. Januar), Label
`review-compliance`, Titel `Compliance- und Copyright-Review <YYYY>`,
Body-Datei `.github/issue-bodies/compliance-review.md`.

## Neue Klassen

Keine — das Feature erzeugt ausschließlich Workflow-, Text- und
Dokumentationsdateien (siehe „Neue Dateien").

## Neue Dateien

| Datei | Typ | Zweck |
|-------|-----|-------|
| `.github/workflows/scheduled-architecture-security-review.yml` | Workflow | Monatlicher Cron + Dispatch, Duplikatprüfung, Issue-Erstellung (Architektur/Sicherheit) |
| `.github/workflows/scheduled-nuget-check.yml` | Workflow | Wöchentlicher Cron + Dispatch, Duplikatprüfung, Issue-Erstellung (NuGet) |
| `.github/workflows/scheduled-compliance-review.yml` | Workflow | Jährlicher Cron + Dispatch, Duplikatprüfung, Issue-Erstellung (Compliance) |
| `.github/issue-bodies/architecture-security-review.md` | Issue-Vorlage | Vorgegebener Prüfungstext aus Issue #95, erste Zeile `@softwarewerkstatt` |
| `.github/issue-bodies/nuget-check.md` | Issue-Vorlage | Vorgegebener Prüfungstext aus Issue #95 |
| `.github/issue-bodies/compliance-review.md` | Issue-Vorlage | Vorgegebener Prüfungstext aus Issue #95 |
| `docs/help/review-automatisierung.md` | Dokumentation | Beschreibung der drei Workflows, Cron-Zeitpunkte, Duplikatlogik, Labels, Aktivierungshinweis (Default-Branch) |

## Änderungen an bestehenden Klassen

Keine — es sind keine C#-Klassen betroffen. Angepasst werden lediglich
Dokumentationsdateien:

### `docs/help/index.md`

- **Ergänzung:** Eintrag für `review-automatisierung.md` unter der
  Gruppe „Infrastruktur" mit einzeiliger Kurzbeschreibung.

### `README.md`

- **Ergänzung:** Kurzer Hinweis auf die drei Review-Workflows im
  Abschnitt „Branching und CI" mit Verweis auf die Hilfeseite.

### `docs/RELEASE_NOTES.md`

- **Ergänzung:** Englischer Bullet-Eintrag zur neuen
  Review-Automatisierung gemäß Projektprozess.

### `changes.log`

- **Ergänzung:** Deutscher Eintrag mit Datumsüberschrift gemäß
  Projektprozess.

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine — das Feature verarbeitet keine Nutzereingaben. Die einzigen
„Eingaben" sind feste Konstanten (Cron-Ausdrücke, Label-Namen,
Titel-Schemata) und die per Commit versionierten Issue-Body-Dateien;
sie werden durch den Review-Prozess und den Workflow-Lauf selbst
geprüft. Ein defekter Dateiname der Body-Datei führt zu einem
sichtbar fehlschlagenden Lauf statt zu einer Fehlkonfiguration im
Hintergrund.

## Konfigurationsänderungen

Keine `appsettings`- oder Konfigurationsklassen-Änderungen. Fest in den
Workflow-Dateien hinterlegte Konstanten:

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| Cron Architektur-/Sicherheitsreview | `schedule`-Cron | `0 5 1 * *` | Monatlich am 1., 05:00 UTC |
| Cron NuGet-Prüfung | `schedule`-Cron | `0 5 * * 1` | Wöchentlich montags, 05:00 UTC |
| Cron Compliance-Review | `schedule`-Cron | `0 5 1 1 *` | Jährlich am 1. Januar, 05:00 UTC |
| `permissions` | Workflow-Block | `contents: read`, `issues: write` | Checkout für Body-Datei sowie Label-/Issue-Verwaltung; minimal wie bisherige Workflows |
| `GH_TOKEN` | Env auf Job-Ebene | `${{ secrets.GITHUB_TOKEN }}` | Authentifizierung der `gh`-Aufrufe |
| Label-Namen | Konstante je Workflow | `review-architektur-sicherheit`, `review-nuget`, `review-compliance` | Duplikat-Merkmal und Issue-Markierung |

Keine Secrets über `GITHUB_TOKEN` hinaus erforderlich; Änderungen an
Cron-Zeitpunkten erfolgen per Commit.

## Seiteneffekte und Risiken

- **Externe Automatisierung:** Die Erwähnung `@softwarewerkstatt` in
  jedem erzeugten Issue löst eine externe Bearbeitungsautomatisierung
  aus — das ist der beabsichtigte Zweck des Features, kein Risiko.
- **Issue-Autor:** Die Issues werden als `github-actions[bot]` angelegt;
  das ist Standardverhalten des `GITHUB_TOKEN`.
- **Label-Bestand:** Beim ersten Lauf entstehen drei neue Labels im
  Repository (`review-architektur-sicherheit`, `review-nuget`,
  `review-compliance`) — gewollt, verändert aber den sichtbaren
  Label-Bestand.
- **Verzögerte Aktivierung:** Scheduled- und Dispatch-Trigger laufen
  nur auf dem Default-Branch — bis zur Promotion nach `main` ist der
  Cron wirkungslos; in der Hilfeseite zu dokumentieren.
- **Dispatch-Probelauf erzeugt echtes Issue:** Ein manueller Lauf legt
  ein reales Issue an; die Duplikatprüfung verhindert Folge-Issues beim
  Zweitlauf. Umgang bestätigt: Test-Issue nach Sichtprüfung schließen.
- **Bestehende Workflows unberührt:** `pr-checks.yml` validiert keine
  Workflow-YAML — Syntaxfehler in den neuen Dateien würden erst beim
  ersten Lauf sichtbar; daher ist ein lokaler YAML-/Lint-Check als
  Arbeitsschritt eingeplant.
- **Blockierender Altfall:** Ein dauerhaft offenes Review-Issue
  unterdrückt alle Folgezyklen desselben Typs — fachlich so gefordert
  (Duplikatvermeidung), in der Hilfeseite zu erläutern.

## Umsetzungsreihenfolge

1. **Issue-Body-Dateien anlegen**
   - Voraussetzungen: Die exakten Prüfungstexte aus Issue #95 liegen
     vor (im Issue eingesehen und in `requirement.md` referenziert);
     Verzeichnis `.github/issue-bodies/` wird neu angelegt.
   - Beschreibung: Drei Markdown-Dateien mit dem jeweils vorgegebenen
     Body — erste Zeile `@softwarewerkstatt`, danach der
     Prüfungsauftrag mit Aufzählung exakt wie in Issue #95.

2. **Workflow `scheduled-architecture-security-review.yml` anlegen**
   - Voraussetzungen: Body-Datei aus Schritt 1.
   - Beschreibung: `on: schedule` (`0 5 1 * *`) plus
     `workflow_dispatch`, `permissions` (`contents: read`,
     `issues: write`), `concurrency`-Gruppe, Job `create-issue` auf
     `ubuntu-latest` mit den Schritten Checkout → Label sicherstellen →
     Duplikatprüfung → Titel bestimmen → `gh issue create`.
     Kommentare deutsch im Projektstil (ASCII-Umschrift ae/oe/ue/ss).

3. **Workflow `scheduled-nuget-check.yml` anlegen**
   - Voraussetzungen: Body-Datei aus Schritt 1; Schritt 2 als
     Strukturvorlage.
   - Beschreibung: Analog Schritt 2 mit Cron `0 5 * * 1`, Label
     `review-nuget`, Titel mit ISO-Kalenderwoche (`date +%G-W%V`).

4. **Workflow `scheduled-compliance-review.yml` anlegen**
   - Voraussetzungen: Body-Datei aus Schritt 1; Schritt 2 als
     Strukturvorlage.
   - Beschreibung: Analog Schritt 2 mit Cron `0 5 1 1 *`, Label
     `review-compliance`, Titel mit Jahreszahl (`date +%Y`).

5. **YAML-/Syntax-Verifikation der Workflows**
   - Voraussetzungen: Schritte 2–4; lokal verfügbares Prüfwerkzeug
     (z. B. `actionlint`, alternativ YAML-Parse + `gh workflow view`
     nach Push).
   - Beschreibung: Alle drei Workflow-Dateien statisch prüfen; die
     `gh`-Kommandos für Label-/Issue-Abfrage lokal gegen das Repository
     trocken ausführen (nur lesende `gh issue list`-Prüfung), um
     Parameter und Label-Filter zu verifizieren.

6. **Hilfeseite `docs/help/review-automatisierung.md` und Index-Eintrag**
   - Voraussetzungen: Schritte 2–4 (Workflow-Namen und Cron-Ausdrücke
     stehen final fest).
   - Beschreibung: Deutsche Hilfeseite mit den drei Workflows,
     Zeitpunkten, Duplikatlogik (Label-basiert, Blockade durch offenes
     Altfall-Issue), Issue-Autor `github-actions[bot]` und dem Hinweis
     auf die Default-Branch-Aktivierung; Eintrag unter „Infrastruktur"
     in `docs/help/index.md`.

7. **README-, Release-Notes- und changes.log-Einträge**
   - Voraussetzungen: Schritte 1–6.
   - Beschreibung: Kurzer Hinweis in `README.md` (Abschnitt „Branching
     und CI"), englischer Bullet in `docs/RELEASE_NOTES.md`, deutscher
     Eintrag in `changes.log` — jeweils gemäß Projektprozess.

8. **Probelauf nach Erreichen des Default-Branches**
   - Voraussetzungen: Merge/Promotion der Workflows nach `main`
     (`staging`-Pfad); `gh`-Zugang zum Repository.
   - Beschreibung: Je Workflow einmal `workflow_dispatch` auslösen,
     Issue-Erstellung im Tracker prüfen (Titel, Label, Body, Erwähnung)
     und den Duplikat-Skip beim Zweitlauf nachweisen; Ergebnis als
     Verifikationsprotokoll festhalten.

## Tests

### Neue Tests

Keine automatisierten Tests — die Anforderung erzeugt kein
ausführbares Code-Artefakt (nur Workflow-YAML und Markdown). Das
Projekt besitzt keine Testinfrastruktur für Workflow-Dateien.
Stattdessen sind folgende Verifikationsmaßnahmen eingeplant:

| Verifikation | Ort | Was wird geprüft? |
|--------------|-----|---------------------|
| YAML-/Lint-Prüfung der drei Workflow-Dateien | lokal (z. B. `actionlint`) | Syntax, gültige Trigger-/Permissions-Struktur |
| Trockenlauf der Duplikat-Abfrage | lokal via `gh issue list --state open --label <label> --limit 1 --json number` | Label-Filter und Rückgabeauswertung funktionieren gegen das echte Repository |
| `workflow_dispatch`-Probelauf je Workflow | GitHub Actions (nach Erreichen von `main`) | End-to-End-Wirkung: Issue wird mit korrektem Titel, Label, Body und `@softwarewerkstatt`-Erwähnung angelegt |
| Zweitlauf desselben Workflows | GitHub Actions | Duplikatvermeidung: kein zweites Issue, Skip-Protokoll im Lauf |

### Betroffene bestehende Tests

Keine.

### E2E-Tests (primärer Funktionsnachweis)

Es sind **keine neuen E2E-Tests** erforderlich. Begründung: Die
Anforderung berührt keinen über die App-UI oder eine Nutzeraktion in
der Anwendung erreichbaren Ablauf — der einzige „Benutzerfluss" ist ein
Systemablauf auf GitHub, dessen sichtbare Wirkung (ein Issue im
Repository-Tracker) außerhalb der Anwendung liegt und von der
FlaUI-basierten Suite `VideoPlayer.E2E.Tests` grundsätzlich nicht
beobachtet werden kann. Der reale Funktionsnachweis erfolgt stattdessen
über den `workflow_dispatch`-Probelauf mit anschließender `gh issue
list`-/Tracker-Kontrolle (siehe Akzeptanzszenarien) — ein echter
Systemdurchlauf vom Trigger bis zum sichtbaren Issue.

Bestehende E2E-Tests sind nicht betroffen.

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path monatlich | Cron `0 5 1 * *` (oder `workflow_dispatch` als Ersatzauslöser) | Workflows liegen auf `main`; Label `review-architektur-sicherheit` existiert noch nicht; kein offenes Issue mit diesem Label | Lauf startet → Checkout → Label wird erzeugt → Duplikatprüfung leer → Titel mit aktuellem `YYYY-MM` → `gh issue create` | Neues offenes Issue „Architektur- und Sicherheitsreview YYYY-MM" mit Label, Body beginnt mit `@softwarewerkstatt`; Issue-URL im Lauf-Protokoll | Probelauf + Sichtprüfung Issue-Tracker; realer Cron-Lauf am Folgemonat |
| 2 | Duplikatvermeidung | Zweiter Lauf desselben Workflows | Offenes Issue mit `review-architektur-sicherheit` existiert (aus Szenario 1) | Lauf startet → Label vorhanden → Duplikatprüfung meldet Treffer → Lauf endet erfolgreich ohne `gh issue create` | Kein neues Issue; Protokollzeile nennt die Nummer des bestehenden Issues | Actions-Log des Laufes |
| 3 | Wöchentliche Prüfung | Cron `0 5 * * 1` oder Dispatch | Label `review-nuget` noch nicht vorhanden | Wie Szenario 1 mit `NuGet-Prüfung <ISO-Jahr>-W<KW>` | Neues Issue mit Kalenderwochen-Titel und NuGet-Prüftext | Probelauf + Tracker-Kontrolle |
| 4 | Jährliches Review | Cron `0 5 1 1 *` oder Dispatch | Label `review-compliance` noch nicht vorhanden | Wie Szenario 1 mit `Compliance- und Copyright-Review <YYYY>` | Neues Issue mit Jahres-Titel und Compliance-Prüftext | Probelauf + Tracker-Kontrolle |
| 5 | Fehlgeschlagener Lauf | Cron/Dispatch bei `gh`-Fehler (z. B. Rechteproblem, fehlende Body-Datei) | Workflow auf `main`; z. B. Body-Datei umbenannt | Lauf bricht im betroffenen Schritt mit Fehler ab | Workflow-Lauf rot in der Actions-Liste; kein Issue und keine halbe Aktion | Actions-Log; bewusst kein stilles Überspringen |
| 6 | Vor Default-Branch-Erreichung | Cron-Zeitpunkt tritt ein, während Workflows nur auf `staging` liegen | Workflows noch nicht auf `main` | — | Kein Lauf, kein Issue — der Cron greift erst im Default-Branch; in der Hilfeseite dokumentiert | Dokumentierter Hinweis; Beobachtung nach Merge nach `staging` |

## Offene Punkte

Keine. Alle zuvor offenen Punkte wurden mit der Anwender-Antwort „passt so.“ (2026-11-16) bestätigt — die Empfehlungen sind jeweils als Designentscheidungen im Plan umgesetzt.
