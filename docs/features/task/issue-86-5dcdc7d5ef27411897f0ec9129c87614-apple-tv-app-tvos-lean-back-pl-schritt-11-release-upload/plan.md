# Umsetzungsplan — Schritt 11: Signiertes Release-Paket und Upload nach App Store Connect (tvOS)

## Ansatz

Der iOS-Release-Pfad wird 1:1 auf tvOS portiert: neue Composite-Action `package-tvos` (Klon von `package-ios` mit tvOS-SDK/Workload/RID/Artefaktnamen), je ein `package-tvos`-Job in `staging-merge.yml` und `main-merge.yml` hinter `vars.TVOS_SIGNING_ENABLED`, plus `scripts/tvOS-Deployment.ps1` nach dem Muster von `iOS-Deployment.ps1` (ohne Pair-to-Mac-MSBuild-Remoting — `VideoPlayer.TvOS` ist kein MAUI-Projekt; Remote-Builds laufen per SSH auf einen Mac-Repo-Checkout). Ergänzend werden zwei Store-Voraussetzungen ins tvOS-Projekt geholt: `PrivacyInfo.xcprivacy` (BundleResource) und `ITSAppUsesNonExemptEncryption=false` in der `Info.plist`. Die iOS-Pfade bleiben unverändert.

## 1. `.github/actions/package-tvos/action.yml` (neu)

Klon von `package-ios/action.yml` mit diesen Anpassungen:

- Inputs identisch (`release-version`, `release-tag`, `codesign-key`, `provisioning-profile`, `api-key-id`, `api-issuer-id`, `api-key-p8`, `certificate-p12-base64`, `certificate-p12-password`, `provisioning-profile-base64`), nur `ios-workload-version` → `tvos-workload-version` (Default `26.5.10318` — deckungsgleich mit dem `build-tvos`-Rollback in `pr-checks.yml`).
- Schritte: `actions/setup-dotnet@v6` (`10.0.1xx`) → `global.json`-Pin `10.0.100`/`latestPatch` → Workload-Rollback `{"microsoft.net.sdk.tvos": "<input>"}` + `dotnet workload install tvos --skip-manifest-update` + Entfernen zu neuer `Microsoft.tvOS.Sdk.net10.0_*`-Packs (TPV-Guard wie iOS).
- Restore: `dotnet restore VideoPlayer.TvOS/VideoPlayer.TvOS.csproj -r tvos-arm64 -p:IncludeTvOsTarget=true` (env `IncludeTvOsTarget=true`; die `IncludeAndroid/MacCatalystTarget`-Env-Schalter entfallen — tvOS-Projekt nutzt sie nicht).
- Keychain-Import + Profil-Ablage: identisch (temp. Keychain, `set-key-partition-list`, `~/Library/MobileDevice/Provisioning Profiles`).
- Publish: `dotnet publish VideoPlayer.TvOS/VideoPlayer.TvOS.csproj -f net10.0-tvos -r tvos-arm64 -c Release -p:ArchiveOnBuild=true -p:CodesignKey -p:CodesignProvision -p:Version -p:ApplicationDisplayVersion (Suffix gekürzt) -p:ApplicationVersion=<Commit-Anzahl + run_attempt - 1> -p:IncludeTvOsTarget=true --no-restore`. Kein `MauiXamlInflator` (kein XAML).
- Collect: `find VideoPlayer.TvOS/bin/Release -name '*.ipa'` → `release-tvos.ipa`; dSYMs → `release-tvos-dsym.zip` (gleiche Dedupe-/publish-Bevorzugungs-Logik).
- Upload: iTMSTransporter (gleiche Suchpfade + Standalone-Fallback), `if: inputs.api-key-p8 != ''`, `-assetFile release-tvos.ipa`.
- Keychain-Cleanup `if: always()` — identisch.

## 2. Workflows (Edit, iOS-Jobs unverändert)

`staging-merge.yml` + `main-merge.yml`: je ein `package-tvos`-Job nach dem `package-ios`-Muster:

```yaml
  package-tvos:
    needs: release
    if: vars.TVOS_SIGNING_ENABLED == 'true' && needs.release.outputs.released == 'true'
    runs-on: macos-latest
    timeout-minutes: 45
    steps:
      - uses: actions/checkout@v4
        with: { ref: ${{ needs.release.outputs.tag }}, fetch-depth: 0 }
      - uses: ./.github/actions/package-tvos
        with:
          release-version/release-tag + secrets.TVOS_* (8 Secrets, Präfix-Pendant zu IOS_*)
      - name: Upload tvOS artifact  # release-tvos.ipa + release-tvos-dsym.zip ans Release
```

Kommentar-Block analog iOS (Gate-Erklärung, Apple-Account-Vorlauf). Ohne gesetzte Variable läuft der Job nie — Workflow bleibt grün.

## 3. `scripts/tvOS-Deployment.ps1` (neu, ~600 Z.)

Struktur nach `iOS-Deployment.ps1`, tvOS-spezifisch:

- Aktionen: `build` (signiertes `.ipa` mit Codesign-Parametern bzw. unsignierter Build), `simulator` (build + `simctl boot/install/launch`, tvOS-Simulator), `store` (signierter Release-Build + Remote-Validierung + iTMSTransporter-Upload), `upload` (vorhandene `.ipa` validieren + hochladen), `list` (Signaturidentitäten, Profile, Geräte auf dem Mac), `menu` (interaktiv).
- Plattform: macOS → dotnet-Befehle lokal; Windows → SSH (`BatchMode`, `Invoke-OnMac`/`Invoke-MacCapture`-Base64-Pattern wie iOS) gegen einen Repo-Checkout auf dem Mac (`-RemoteRepoPath`, Env `VIDEOPLAYER_TVOS_MAC_REPO_PATH`, Default `~/VideoPlayer-App`); Remote führt `git fetch`+`checkout <lokaler HEAD-SHA>` vor dem Build aus — kein MSBuild-Remoting nötig/möglich. Bei `build` unter Windows wird die `.ipa` per `scp` nach `artifacts/` (gitignored) zurückkopiert.
- Env/Parameter: `VIDEOPLAYER_TVOS_CODESIGN_KEY`, `VIDEOPLAYER_TVOS_PROVISIONING_PROFILE`, `VIDEOPLAYER_TVOS_MAC_SERVER_ADDRESS`/`_USER`, `VIDEOPLAYER_TVOS_MAC_REPO_PATH`, `VIDEOPLAYER_TVOS_API_KEY_PATH`/`_ID`/`_ISSUER_ID`; `-BuildNumber`, `-NoBumpBuildNumber`, `-IpaPath`, `-NoPrompt`, `-LogDir`.
- Workload-Pin im Remote-/Lokal-Build: Rollback `26.5.10318` + `workload install tvos --skip-manifest-update` (wie CI).
- `Assert-StorePrerequisites`: `Apple Distribution`-Zertifikat (Development verworfen), `.p8` existiert/außerhalb Repo, SSH-Zugang unter Windows.
- Remote-Validierung (`Invoke-IpaValidation` angepasst): codesign `--verify --deep --strict`, `get-task-allow`-Abweisung, `PrivacyInfo.xcprivacy` im Bundle-Root, `UIDeviceFamily` enthält `3` (statt `1`), `ITSAppUsesNonExemptEncryption=false`, iTMSTransporter `-m verify` als Best-Effort-Fallback.
- Transkript-Logging (`logs/`, gitignored), Deploy-Prefs `.tvos-deploy.user.json` (gitignore-Eintrag ergänzen).
- Kein `VideoPlayerApiToken` (tvOS-Projekt nutzt die Eigenschaft nicht).

## 4. `VideoPlayer.TvOS` — Store-Voraussetzungen

- `VideoPlayer.TvOS/PrivacyInfo.xcprivacy` (neu): .NET-Mindestmanifest — Kopie des iOS-Manifests (FileTimestamp C617.1, SystemBootTime 35F9.1, DiskSpace E174.1, UserDefaults CA92.1).
- `VideoPlayer.TvOS.csproj`: `<BundleResource Include="PrivacyInfo.xcprivacy" />` unter `IncludeTvOsTarget=true`.
- `VideoPlayer.TvOS/Info.plist`: `ITSAppUsesNonExemptEncryption` = `false` ergänzen (Konsistenz mit iOS; Export-Compliance-Abfrage entfällt).
- **Nicht** im Umfang: App-Icon-/Top-Shelf-Assets und Multicast-Entitlement — dokumentierter manueller Vorlauf (siehe Doku; Store-Validierung greift erst bei aktiviertem Gate).

## 5. Tests — `VideoPlayer.Maui.Tests/TvOsDeploymentContractTests.cs` (neu)

Muster `TvOsProjectContractTests`/`IosDeploymentResponseFileTests` (Quelltext-Contract, läuft unter Windows):

- `package-tvos/action.yml`: vorhanden; enthält `microsoft.net.sdk.tvos`, `workload-rollback.json`, `dotnet workload install tvos --skip-manifest-update`, `tvos-arm64`, `ArchiveOnBuild=true`, `CodesignKey`, `iTMSTransporter`, `release-tvos.ipa`, `git rev-list --count HEAD`, `run_attempt`, `if: always()`-Cleanup.
- `staging-merge.yml`/`main-merge.yml`: `package-tvos:`-Job, `vars.TVOS_SIGNING_ENABLED == 'true'`, `needs.release.outputs.released`, `./.github/actions/package-tvos`, `secrets.TVOS_`, `release-tvos.ipa`; iOS-Gate `vars.IOS_SIGNING_ENABLED` weiterhin vorhanden (Pfade unverändert).
- `scripts/tvOS-Deployment.ps1`: vorhanden; `VIDEOPLAYER_TVOS_`, `tvos-arm64`, `iTMSTransporter`, `git rev-list --count`, ValidateSet der Aktionen.
- `VideoPlayer.TvOS.csproj`: `BundleResource` für `PrivacyInfo.xcprivacy`; `Info.plist` enthält `ITSAppUsesNonExemptEncryption`.
- `.gitignore` enthält `.tvos-deploy.user.json`.

## 6. Dokumentation

- `docs/help/tv-app/deployment.md` (neu): Gate `TVOS_SIGNING_ENABLED`, Secret-/Variablen-Liste, Ablauf (Rollback → Keychain → Publish → ipa/dSYM → iTMSTransporter → Release-Assets), Skript-Aufrufbeispiele, **manueller Vorlauf**: Bundle-ID bestätigen, tvOS-Provisioning-Profil, Apple-Distribution-Zertifikat, ASC-API-Key, App-Icon/Top-Shelf-Assets, ggf. Multicast-Entitlement.
- `docs/help/tv-app/index.md` + `installation.md`: Link/Querverweis; `beschreibung.md` Einschränkungs-Satz „Veröffentlichung noch nicht eingerichtet" anpassen.
- `docs/help/index.md`, `README.md`: tvOS-Deployment-Erwähnung (`scripts/tvOS-Deployment.ps1`, `scripts/`-Tabellenzeile).

## 7. Abschluss

- YAML-Syntax lokal via `python -c yaml.safe_load` validieren; `dotnet test VideoPlayer.Maui.Tests` lokal grün.
- Planungscommit (`plan:`) → Implementierungscommit (`feat:`) → `git push -u origin <branch>` → `env -u GH_TOKEN gh run watch <id> --repo martin-stromberg/VideoPlayer-App --exit-status`; bei Fail `gh run view --job <jobid> --log-failed` → fix → erneut. Abschluss erst bei grünem `build-tvos` (Run-ID im Ergebnis).

## Offene Punkte

Keine.
