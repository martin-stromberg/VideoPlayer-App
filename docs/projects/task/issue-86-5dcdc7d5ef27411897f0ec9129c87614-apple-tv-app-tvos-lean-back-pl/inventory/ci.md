# CI-/Deployment-Landschaft — Bestandsaufnahme (iOS-Pfad als tvOS-Vorlage)

Dateipfade relativ zum Repository-Root. Secrets-Namen ohne Werte.

## Workflows `.github/workflows/`

### `pr-checks.yml` — PR-Checks (Windows)
- Trigger: `pull_request` nach `main`, `staging`; Runner `windows-latest`.
- Schritte: `actions/setup-dotnet@v4` (`10.0.x`) → `dotnet workload install maui --skip-manifest-update` → `dotnet restore` → `dotnet build VideoPlayer.Maui.Tests` (Release) → `dotnet build VideoPlayer.Maui -f net10.0-windows10.0.19041.0` → `dotnet test VideoPlayer.Maui.Tests --no-build -c Release` → `dotnet format whitespace --verify-no-changes` → Branch-Guard (PRs nach `main` nur aus `staging`).

### `staging-merge.yml` — Staging-Prerelease
- Trigger: Push auf `staging`. Jobs:
  - `release` (ubuntu-latest): semantic-release via `.github/scripts/semantic-release-with-retry.sh`; Outputs `tag`, `version`, `released` (nur bei neuem Tag `true` — Upload-Duplikat-Schutz wegen `CFBundleVersion`-Kollision).
  - `build-ios` (Runner-Image `xcode-27`, Xcode-27.1-Pin): Checkout Release-Tag → setup-dotnet → `global.json`-Band-Pin (`10.0.100`, `rollForward: latestPatch`) → Workload-Rollback `{"microsoft.net.sdk.ios": "27.1.10018"}` + `dotnet workload install maui --skip-manifest-update` → `Set-Version.ps1` → `dotnet build VideoPlayer.Maui -f net10.0-ios -r iossimulator-arm64 -p:EnableCodeSigning=false` → `.app.zip` ans GitHub-Prerelease.
  - `package-ios` (macos-latest, **nur wenn** `vars.IOS_SIGNING_ENABLED == 'true'` **und** `released == 'true'`): Composite-Action `./.github/actions/package-ios` mit allen Signatur-Inputs; danach `release-ios.ipa` + `release-ios-dsym.zip` ans Release.
  - `draft-pr` (ubuntu): Draft-PR staging→main.

### `main-merge.yml` — Main-Release
- Trigger: Push auf `main`. Gleiche Struktur (`release`, `build-ios` auf `macos-latest` mit Rollback `26.5.10318`, `package-ios` gleiches Gate, `back-merge` PR main→staging).

## Composite-Action `.github/actions/package-ios/action.yml`
- Inputs: `release-version`, `release-tag`, `codesign-key`, `provisioning-profile`, `api-key-id`, `api-issuer-id`, `api-key-p8`, `certificate-p12-base64`, `certificate-p12-password`, `provisioning-profile-base64`, `ios-workload-version` (Default `26.5.10318`).
- Schritte: setup-dotnet `10.0.1xx` → `global.json`-Pin `10.0.100` → Workload-Rollback-Datei `{"microsoft.net.sdk.ios": "<ios-workload-version>"}` + `workload install maui-ios ios --skip-manifest-update` + Entfernen zu neuer `Microsoft.iOS.Sdk.net10.0_*`-Packs (TPV-Resolution) → `dotnet restore VideoPlayer.Maui -r ios-arm64` (env `IncludeAndroidTarget=false`, `IncludeMacCatalystTarget=false`) → Keychain-Import (`.p12` + `.mobileprovision`, `set-key-partition-list`) → `dotnet publish -f net10.0-ios -r ios-arm64 -p:ArchiveOnBuild=true -p:CodesignKey=… -p:CodesignProvision=… -p:MauiXamlInflator=XamlC` mit `ApplicationDisplayVersion` (Suffix gekürzt) + `ApplicationVersion` = Commit-Anzahl + `run_attempt − 1` → `.ipa`-Sammlung + dSYM-Zip → TestFlight-Upload via `iTMSTransporter -m upload -assetFile release-ios.ipa -apiKey … -apiIssuer …` (Suchpfade Transporter.app/`/usr/local/itms`, Fallback Standalone-Paket von Apple-CDN; nur wenn `api-key-p8` gesetzt) → Keychain-Cleanup (`if: always()`).

## Verwendete Secrets (iOS-Pfad)
`IOS_CODESIGN_KEY`, `IOS_PROVISIONING_PROFILE`, `IOS_API_KEY_ID`, `IOS_API_ISSUER_ID`, `IOS_API_KEY_P8`, `IOS_CERTIFICATE_P12_BASE64`, `IOS_CERTIFICATE_P12_PASSWORD`, `IOS_PROVISIONING_PROFILE_BASE64`; Repository-Variable `IOS_SIGNING_ENABLED` (Gate). `GH_TOKEN`/`GITHUB_TOKEN` für Release-Uploads.

## Hilfsskripte `.github/scripts/`
`Set-Version.ps1` (Versions-Stempel), `semantic-release-with-retry.sh` (Retry + Tag-Aufräumen).

## Lokales Skript `scripts/iOS-Deployment.ps1` (+ `iOS-Deployment.ResponseFile.ps1`)
- Aktionen: `build`, `simulator`, `device`, `store`, `upload`, `list`, `menu`; Windows→Mac per SSH/Pair-to-Mac (`Invoke-MacCapture`, schlüsselbasiertes SSH für store/upload).
- Parameter/Umgebungsvariablen: `VIDEOPLAYER_IOS_CODESIGN_KEY`, `VIDEOPLAYER_IOS_PROVISIONING_PROFILE`, `VIDEOPLAYER_IOS_MAC_SERVER_ADDRESS`/`_USER`/`_PASSWORD`, `VIDEOPLAYER_IOS_MAC_DOTNET_ROOT`, `VIDEOPLAYER_IOS_API_KEY_PATH`/`_ID`/`_ISSUER_ID`, `VIDEOWEBPLAYER_MAUI_API_TOKEN` (Build-Eigenschaft `VideoPlayerApiToken`, maskiert), `-BuildNumber` (Default = Commit-Anzahl wie CI), `-NoBumpBuildNumber`, `-NoPrompt`.
- Funktionen u. a.: `Get-RuntimeIdentifier`, `Get-SimulatorUdid`, `Get-BundleId`, `Get-RemoteIdentities`/`Get-RemoteProfiles`/`Get-RemoteDevices`, `Resolve-CodesigningInteractive`, `Assert-StorePrerequisites` (verwirft Development-Zertifikate), `Assert-TransporterAvailable`, `Get-CommitBuildNumber`, `Get-LatestIpa`.
- `scripts/iOS-Deployment.ResponseFile.ps1`: Response-File-Erzeugung + Sanitizing/Maskierung (separat testbar; `VideoPlayer.Maui.Tests/IosDeploymentResponseFileTests.cs`).

## Weitere Fakten für den tvOS-Pfad
- `VideoPlayer.Maui/VideoPlayer.Maui.csproj`: `ApplicationId` = `de.martinstromberg.videos`, TFMs via `Include*Target`-Schalter (`net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`, `net10.0-windows10.0.19041.0`), iOS-Default-Signierung `Apple Development: Created via API (466MVK852A)` + `VS: WildCard Development` (nur Geräte-Builds), `MauiPackageVersion`-Pin `10.0.90` (in `msTools.DeveloperApi.Maui` gleich), `MauiXamlInflator=SourceGen`.
- `Platforms/iOS/Info.plist`: `CFBundleIdentifier=de.martinstromberg.videos`, `LSRequiresIPhoneOS`, `NSLocalNetworkUsageDescription` (Discovery), `NSCameraUsageDescription` (QR-Scan), `UIBackgroundModes: audio`, `NSAppTransportSecurity.NSAllowsArbitraryLoads`.
- **Kein tvOS-Projekt, keine tvOS-Workload, kein `package-tvos`-Workflow vorhanden.** Installierte Workloads auf diesem Rechner: `android`, `ios` (26.5.10318/10.0.100), `maccatalyst`, `maui-windows` — kein `tvos`.
