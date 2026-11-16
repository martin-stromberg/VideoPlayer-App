# Dokumentationsstruktur (Bestand für das Akzeptanzkriterium „vollständig im Repository dokumentiert")

Stand: Commit `0a8edbad9031e5f726477d343dae566a6b778bf9`.

## Übersicht der Dokumentationsorte

| Ort | Inhalt / Konvention |
|-----|---------------------|
| `README.md` | Deutsch. Abschnitte: Funktionen, Plattformen, Voraussetzungen, „Bauen und Testen", Projektstruktur, „Secrets und Client-Gate", Developer-API, „Branching und CI", Dokumentation (Verweis auf `docs/help/`), Changelog-Verweis, Lizenz. Enthält bereits einen Abschnitt zu CI/Branching als Ankerpunkt für Automatisierungs-Hinweise. |
| `docs/help/` | Deutsche Fachdokumentation mit `index.md` als Übersicht; Themen-Dateien im Kebab-Case (z. B. `staging-branch.md`, `e2e-tests.md`, `developer-api.md`); Unterordner `api/`, `api-client/`, `tv-app/`. Der Index gruppiert in „App" und „Infrastruktur" — neue Automatisierungs-Doku würde unter „Infrastruktur" passen. |
| `docs/RELEASE_NOTES.md` | Englisch. Abschnitte „Important Notes Before Update" und „What's New" — chronologisch neuester Eintrag oben; jede größere Änderung erhält einen Bullet mit Coverage-Angaben. |
| `changes.log` | Deutsch. Änderungsprotokoll mit Datumsüberschriften (`## YYYY-MM-DD — Titel`) und Stichpunkt-Details; neuester Eintrag oben. |
| `docs/features/task/` | Prozess-Artefakte je Aufgabe (`requirement.md`, `inventory.md`, `plan.md`, `todo.md` u. a.) — werden nach Projektprozess am Ende wieder entfernt. |
| `docs/projects/{project,task}` | Projektweite bzw. aufgabenbezogene Dokumente. |
| `.github/ISSUE_TEMPLATE/` | `bug_report.md`, `feature_request.md`, `custom.md` — unveränderte GitHub-Standardvorlagen (englisch), ohne Label- oder Assignee-Vorbelegung. |
| `scripts/` | Deployment-Skripte (`iOS-Deployment.ps1`, `tvOS-Deployment.ps1`, `Retag-Esqlite3-For-Tvos.py`) — betriebsbezogen, keine Repo-Automatisierung. |
| `docs/help/staging-branch.md` | Dokumentiert den Branching-/Release-Prozess (`feature/*` → `staging` → `main`), PR-Regeln, manuelle Branch-Protection-Schritte in GitHub und Release-Builds. Referenziert `pr-checks.yml` als erforderlichen Status-Check. |

## Konventionen

- Hilfeseiten sind auf Deutsch verfasst; Umlaute werden in `docs/`
  normal als UTF-8 verwendet (anders als in den Workflow-Kommentaren,
  die ASCII mit ae/oe/ue-Umschrift nutzen).
- `index.md` in `docs/help/` führt jede neue Hilfedatei mit Titel und
  einzeiliger Kurzbeschreibung — neue Doku wird dort verlinkt.
- Release Notes und `changes.log` werden pro Änderung ergänzt
  (Schritte 12/12b/12c des Aufgabenprozesses in `todo.md`).

## Relevanz für die Anforderung

- Die Anforderung verlangt „vollständig innerhalb des Repositories
  dokumentiert" — naheliegende Orte sind eine neue Seite unter
  `docs/help/` (Infrastruktur) mit Eintrag in `docs/help/index.md`
  sowie Workflow-Kommentare im Projektstil; `README.md` (Abschnitt
  „Branching und CI"), `docs/RELEASE_NOTES.md` und `changes.log`
  folgen dem bestehenden Projektprozess.
- Hinweis aus der Anforderung: Scheduled Workflows werden erst aktiv,
  sobald sie über `staging` → `main` den Default-Branch erreichen —
  dieser Zusammenhang passt zum dokumentierten Staging-Modell in
  `docs/help/staging-branch.md`.
