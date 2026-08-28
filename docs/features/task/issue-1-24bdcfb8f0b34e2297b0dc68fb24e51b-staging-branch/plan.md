# Umsetzungsplan: Staging Branch

## Massnahmen

1. **GitHub-Workflows anlegen**
   - `.github/workflows/pr-checks.yml`
     - Wird bei PRs gegen `main` und `staging` ausgeführt.
     - Führt `dotnet build` (alle MAUI-Ziele), `dotnet test` und `dotnet format --verify-no-changes` aus.
     - Verhindert PRs gegen `main`, deren Quellbranch nicht `staging` ist.
   - `.github/workflows/staging-merge.yml`
     - Wird bei Merge auf `staging` getriggert.
     - Erzeugt einen Prerelease (`v{major}.{minor}.{patch}.{build}-RC.{counter}`) für Windows, iOS und Android via `dotnet publish`.
     - Erstellt automatisch einen Draft-PR von `staging` auf `main`.
   - `.github/workflows/main-merge.yml`
     - Wird bei Merge auf `main` getriggert.
     - Führt Semantic Release für vollständige Releases durch.
     - Erzeugt Release-Builds für Windows, iOS und Android.
     - Erstellt einen Back-Merge-PR von `main` auf `staging`.

2. **Semantic-Release-Konfiguration**
   - `.releaserc.json` mit Commit-Analyzer, Release-Notes-Generator, Git- und GitHub-Plugin.
   - Exec-Plugin, das `VersionUpdate.ps1` aufruft, um `ApplicationDisplayVersion` und `ApplicationVersion` in `VideoWebPlayer.Maui.csproj` anzupassen.

3. **Branch-Protection (manuell in GitHub anzulegen)**
   - `main`: Require PR, nur `staging` als Quellbranch erlauben (wenn unterstützt, sonst Workflow-Check), Tests und Linter grün, mindestens ein Review.
   - `staging`: Tests und Linter grün, mindestens ein Review.

4. **Dokumentation**
   - `docs/help/staging-branch.md` mit der neuen Branch-Strategie.
   - `README.md` um CI/CD-Hinweis ergänzen.

## Offene Punkte

- Keine.
