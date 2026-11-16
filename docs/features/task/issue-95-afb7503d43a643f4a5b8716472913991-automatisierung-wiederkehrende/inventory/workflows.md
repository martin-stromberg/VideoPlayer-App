# Workflows, CI-Konventionen und Repository-Automatisierung

Analysiert wurden alle Dateien unter `.github/` sowie die im Repository
bereits vorhandenen Automatisierungen. Stand: Commit
`0a8edbad9031e5f726477d343dae566a6b778bf9`.

## Vorhandene Workflows (` .github/workflows/`)

Drei Workflows, alle im Kebab-Case benannt, alle ohne `schedule:`- oder
`workflow_dispatch:`-Trigger — zeitgesteuerte Ausführung existiert im
Projekt noch nicht.

### `pr-checks.yml` — „PR Checks"

Datei: `.github/workflows/pr-checks.yml`

| Aspekt | Ist-Zustand |
|--------|-------------|
| Trigger | `pull_request` auf `main` und `staging`; `push` auf `task/issue-86-**` (laufende tvOS-Verifikation) |
| Permissions | `contents: read` |
| Job `verify` | `windows-latest`: Checkout → Setup .NET `10.0.x` → `dotnet workload install maui` → `dotnet restore` → Build `VideoPlayer.Maui.Tests` (Release) → Build `VideoPlayer.Maui` Windows-TFM (Release) → `dotnet test` (`--no-build`, Release) → Lint `dotnet format whitespace --verify-no-changes` → Guard: PRs nach `main` nur von `staging` |
| Job `build-tvos` | `macos-latest`: SDK-Band-Pin `10.0.100` per `global.json`, tvOS-Manifest-Rollback `26.5.10318`, Simulator-Build `net10.0-tvos` ohne Codesignierung |

### `staging-merge.yml` — „Staging Prerelease"

Datei: `.github/workflows/staging-merge.yml`

| Aspekt | Ist-Zustand |
|--------|-------------|
| Trigger | `push` auf `staging` |
| Permissions | `contents: write`, `pull-requests: write` |
| Job `release` | `ubuntu-latest`: Semantic Release via npm (Plugins commit-analyzer, release-notes-generator, git, github), Ausführung über `.github/scripts/semantic-release-with-retry.sh`; Outputs `tag`, `version`, `released` |
| Job `build-ios` | `xcode-27`-Runner: SDK-Band-Pin `10.0.100`, iOS-Manifest-Rollback `27.1.10018`, Versionsstempel via `.github/scripts/Set-Version.ps1`, iOS-Simulator-Build, Upload `.app.zip` ans Prerelease per `gh release upload` |
| Job `package-ios` | Gate: `vars.IOS_SIGNING_ENABLED == 'true' && released == 'true'`; signierter Store-Build über Composite-Action `./.github/actions/package-ios`, Upload `release-ios.ipa` + `release-ios-dsym.zip` |
| Job `package-tvos` | Gleiches Gate-Konzept mit `vars.TVOS_SIGNING_ENABLED`, Composite-Action `./.github/actions/package-tvos`, Upload `release-tvos.ipa` + `release-tvos-dsym.zip` |
| Job `draft-pr` | Legt per `gh pr create` einen Draft-PR `staging` → `main` an |

### `main-merge.yml` — „Main Release"

Datei: `.github/workflows/main-merge.yml`

| Aspekt | Ist-Zustand |
|--------|-------------|
| Trigger | `push` auf `main` |
| Permissions | `contents: write`, `pull-requests: write` |
| Job `release` | Wie `staging-merge.yml` (Semantic Release + Retry-Skript) |
| Job `build-ios` | `macos-latest`: SDK-Band-Pin `10.0.100`, iOS-Manifest-Rollback `26.5.10318`, `Set-Version.ps1`, Build, `.app.zip`-Upload |
| Job `package-ios` / `package-tvos` | Gleiche Gates und Composite-Actions wie in `staging-merge.yml` |
| Job `back-merge` | Erzeugt per `gh pr create` einen PR `main` → `staging` (`--title 'chore: back-merge main into staging'`) |

## Unterstützende Artefakte unter `.github/`

| Pfad | Zweck |
|------|-------|
| `.github/actions/package-ios/action.yml` | Composite-Action: signierter iOS-Build + TestFlight-Upload (Fastlane), Parameter für Signatur-Secrets |
| `.github/actions/package-tvos/action.yml` | tvOS-Pendant zu `package-ios` |
| `.github/scripts/Set-Version.ps1` | Setzt Versionsangaben im MAUI-Projekt aus dem Release-Tag |
| `.github/scripts/semantic-release-with-retry.sh` | Retry-Wrapper um `semantic-release`; räumt verwaiste Remote-Tags/Notes-Refs bei Fehlschlag auf (bekanntes Problem mit `refs/notes/semantic-release-*`) |
| `.github/ISSUE_TEMPLATE/` | GitHub-Standardvorlagen `bug_report.md`, `feature_request.md`, `custom.md` (englisch, ohne Label-/Assignee-Vorbelegung) |

## Beobachtete Projekt-Konventionen in den Workflows

- **Sprache/Kommentare:** Deutsche Block-Kommentare über Jobs und
  nicht-triviale Schritte; Umlaute werden als `ae`/`oe`/`ue`
  umschrieben (z. B. „waehrend", „laeuft", „ueber"), `ß` als `ss` —
  der Workflow-Text selbst ist ASCII.
- **Dateinamen:** Kebab-Case (`main-merge.yml`, `pr-checks.yml`,
  `staging-merge.yml`); Workflow-`name:` ist englischer Titel
  („PR Checks", „Staging Prerelease", „Main Release").
- **Berechtigungen:** Minimaler `permissions`-Block auf
  Workflow-Ebene (`contents: read` für Checks; `contents: write` +
  `pull-requests: write` für Release-Workflows). `issues: write`
  kommt bisher nirgends vor.
- **GitHub-CLI:** `gh`-Aufrufe (`gh release upload`, `gh pr create`)
  laufen mit `GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}` in `bash`-Shells
  auf `ubuntu-latest`; Fehlertoleranz per `|| true` bei PR-Erstellung.
- **Gates:** Deaktivierbare Jobs werden über Repository-Variablen
  (`vars.IOS_SIGNING_ENABLED`, `vars.TVOS_SIGNING_ENABLED`) plus
  Job-`if` gesteuert — kein manueller Eingriff in Workflow-Dateien
  nötig.
- **Pinning:** SDK- und Workload-Versionen werden bewusst gepinnt
  (`global.json` im Job, Workload-Rollback-JSON) — Kommentare
  begründen jeweils die Pin-Version.

## Issue-/Label-Konventionen (Ist-Zustand via `gh`)

- Vorhandene Labels (Standard-GitHub-Set): `bug`, `documentation`,
  `duplicate`, `enhancement`, `good first issue`, `help wanted`,
  `invalid`, `question`, `wontfix`, `accessibility` sowie das
  projekteigene Label `released on @staging` (Farbe `#ededed`, ohne
  Beschreibung).
- **Keine** dedizierten Labels für Review-/Automatisierungs-Issues
  vorhanden — ein Label-basiertes Duplikat-Merkmal müsste neu
  angelegt werden.
- Offene Issues: ausschließlich Issue `#95` (diese Anforderung).
  Referenz für Review-Issues: das geschlossene Issue `#87`
  („Code-Review: Orchestrierung mehrstufiger Datenpflege …") zeigt,
  dass review-artige Issues bereits als Titel-Konvention vorkamen —
  ohne Label-Systematik.
- `gh`-Authentifizierung in dieser Umgebung: Account
  `softwarewerkstatt` (Token-Scopes u. a. `repo`, `workflow`) — also
  genau der Account, den die neuen Workflows in Issues erwähnen
  sollen.

## Lücken bezogen auf die Anforderung

- Kein `schedule:`-Trigger und kein `workflow_dispatch:` in keinem
  Workflow — Cron-Automatisierung ist ein neues Muster im Projekt.
- Kein Workflow erstellt oder durchsucht Issues
  (`issues: write`/`gh issue …` fehlen bislang; es existieren nur
  `gh pr create`/`gh release upload`).
- Keine Issue-Body-Vorlagen außerhalb der standardmäßigen
  `ISSUE_TEMPLATE/`-Verzeichnisses; keine bestehende Automation, die
  Issues anlegt oder Accounts erwähnt.
