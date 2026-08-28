# Staging Branch

## Übersicht

Das Projekt verwendet einen `staging`-Branch als Qualitätsgatter vor `main`.

```
feature/*  →  staging  →  main
```

## Pull Requests

### Gegen `staging`

- Alle Tests müssen erfolgreich sein (`dotnet test`).
- Der Linter muss grün sein (`dotnet format --verify-no-changes`).
- Nach dem Merge wird automatisch ein Draft-PR gegen `main` erstellt.
- Ein Prerelease (`v{major}.{minor}.{patch}-RC.{n}`) wird für Windows, iOS und Android erzeugt.

### Gegen `main`

- Nur der `staging`-Branch ist als Quelle erlaubt.
- Alle Tests müssen erfolgreich sein.
- Nach dem Merge wird ein Semantic-Release erzeugt sowie ein Back-Merge-PR von `main` auf `staging`.

## Manuelle Schritte in GitHub

Die Branch-Protection-Regeln müssen im Repository manuell angelegt werden:

- `main`
  - "Require a pull request before merging" aktivieren.
  - "Require status checks to pass" aktivieren und `PR Checks` voraussetzen.
  - Optional: Anzahl der erforderlichen Reviews festlegen.
- `staging`
  - "Require a pull request before merging" aktivieren.
  - "Require status checks to pass" aktivieren und `PR Checks` voraussetzen.

Der Workflow `pr-checks.yml` erzwingt, dass PRs gegen `main` ausschließlich von `staging` kommen.

## Release-Builds

Die Workflows `.github/workflows/staging-merge.yml` und `.github/workflows/main-merge.yml` erzeugen Release-Builds für Android, iOS und Windows. Für iOS sind ein gültiges Apple-Entwicklerzertifikat, Provisioning-Profile und ggf. weitere Geheimnisse erforderlich.
