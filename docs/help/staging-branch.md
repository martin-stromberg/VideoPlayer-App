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

Neben `release-ios.ipa` lädt der iOS-Build das Archiv
`release-ios-dsym.zip` mit den Symbol-Bundles (`*.dSYM`) des Builds
ans GitHub-Release — die `package-ios`-Action sammelt sie aus
`VideoPlayer.Maui/bin/Release` und zippt sie (kein Fund: Warnung statt
Fehlschlag, der Upload ist existenz-guarded). TestFlight-/Store-
Crash-Reports eines Builds sind damit nachträglich symbolisierbar;
das Archiv ist wie die `.ipa` an die Buildnummer des Releases
gekoppelt. Beim lokalen Build über `scripts/iOS-Deployment.ps1`
legt die Funktion `Export-DsymArchive` das Symbolarchiv als
`<ipa-name>-dsym.zip` neben der `.ipa` ab.
