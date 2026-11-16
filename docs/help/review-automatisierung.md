# Review-Automatisierung

Drei zeitgesteuerte GitHub-Actions-Workflows legen periodisch Issues im
Repository-Tracker an und erwähnen darin `@softwarewerkstatt`, um die
automatische Analyse und Bearbeitung anzustoßen. Die Workflows führen
die Reviews nicht selbst durch — sie beauftragen nur per Issue. Die
gemeinsame Ablauflogik liegt in der Composite Action
`.github/actions/create-review-issue/`; die drei Workflows
unterscheiden sich nur in Zeitpunkt, Label, Titelschema und
Issue-Text.

## Die drei Workflows

| Workflow-Datei | Prüfungsart | Zeitpunkt (Cron, UTC) | Label | Issue-Titel |
|----------------|-------------|-----------------------|-------|-------------|
| `scheduled-architecture-security-review.yml` | Architektur- und Sicherheitsreview | Monatlich am 1., 05:00 UTC (`0 5 1 * *`) | `review-architektur-sicherheit` | `Architektur- und Sicherheitsreview <JJJJ>-<MM>` |
| `scheduled-nuget-check.yml` | NuGet-Paketprüfung | Wöchentlich montags, 05:00 UTC (`0 5 * * 1`) | `review-nuget` | `NuGet-Prüfung <ISO-Jahr>-W<KW>` |
| `scheduled-compliance-review.yml` | Compliance- und Copyright-Review | Jährlich am 1. Januar, 05:00 UTC (`0 5 1 1 *`) | `review-compliance` | `Compliance- und Copyright-Review <JJJJ>` |

Alle drei Workflows lassen sich zusätzlich manuell über
`workflow_dispatch` auslösen (Actions-Tab → Workflow → „Run workflow").

## Ablauf eines Laufs

Der Job `create-issue` läuft auf `ubuntu-latest` und besteht aus zwei
Schritten: `actions/checkout` stellt Issue-Text und Composite Action
bereit, danach ruft der Schritt „Review-Issue erstellen" die Composite
Action `.github/actions/create-review-issue/` mit den Eingaben `label`,
`label-description`, `title-prefix`, `title-date-format`, `body-file`
und `gh-token` auf. Das Token wird als Eingabe übergeben
(`secrets.GITHUB_TOKEN`), da Composite Actions keine Secrets lesen
können. Innerhalb der Action laufen vier Schritte:

1. **Label sicherstellen:** Das dedizierte Review-Label wird per
   `gh label create --force` idempotent angelegt bzw. aktualisiert —
   beim ersten Lauf entsteht es automatisch samt Beschreibung, ein
   manuelles Anlegen ist nicht nötig.
2. **Duplikatprüfung:** `gh issue list --state open --label <label>`
   sucht nach einem bereits offenen Issue desselben Typs.
3. Existiert ein solches Issue, endet der Lauf erfolgreich ohne neue
   Erstellung — die Issue-Nummer des Altfalls steht im Lauf-Protokoll.
4. **Titel bestimmen und Issue erstellen:** Sonst wird der Zeitraum
   per `date` im konfigurierten Format (`title-date-format`) ermittelt,
   an den Titel-Präfix gehängt und das Issue per `gh issue create`
   mit Label und dem vorgegebenen Prüfungstext aus
   `.github/issue-bodies/` angelegt; die erste Body-Zeile ist stets
   `@softwarewerkstatt`.

## Duplikatlogik und blockierender Altfall

Die Erkennung „desselben Typs" läuft ausschließlich über das
Review-Label, nicht über den Titel. Konsequenz: Solange ein
Review-Issue eines Typs offen ist, legt der Workflow kein neues an —
der jeweilige Zyklus gilt als abgedeckt, bis das Altfall-Issue
geschlossen wird. Manuell angelegte Issues ohne das Label blockieren
den Zyklus nicht.

## Sonstiges

- Die Issues werden als `github-actions[bot]` angelegt
  (Standardverhalten des `GITHUB_TOKEN`).
- Die Workflows laufen mit minimalen Rechten (`contents: read` für
  den Checkout der Body-Datei, `issues: write` für Label- und
  Issue-Verwaltung); es sind keine Secrets über `GITHUB_TOKEN` hinaus
  nötig.
- Die Issue-Texte liegen als eigene Dateien unter
  `.github/issue-bodies/` (`architecture-security-review.md`,
  `nuget-check.md`, `compliance-review.md`) und bleiben so separat
  reviewbar. Fehlt eine Body-Datei, schlägt der Lauf sichtbar fehl.
- Eine `concurrency`-Gruppe je Workflow verhindert, dass ein manueller
  Dispatch und ein Cron-Lauf gleichzeitig zwischen Duplikatprüfung und
  Erstellung ein Doppel-Issue erzeugen.
- Ein manuell ausgelöster Probelauf erzeugt ein echtes Issue — nach
  der Sichtprüfung kann es einfach geschlossen werden; die
  Duplikatprüfung verhindert Folge-Issues beim Zweitlauf.

## Aktivierungshinweis

Scheduled- und Dispatch-Trigger greifen nur auf dem Default-Branch
(`main`). Die Workflows werden daher erst aktiv, wenn sie über den
Staging-Pfad (`staging` → `main`, siehe
[Staging-Branch](staging-branch.md)) im Default-Branch angekommen
sind.
