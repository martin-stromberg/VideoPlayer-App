# Plan-Review — Schritt 11: Signiertes Release-Paket und Upload nach App Store Connect

Status: **Vollständig umgesetzt**

## Prüfung der Planelemente

| Planelement | Umgesetzt | Nachweis |
|-------------|-----------|----------|
| 1. `.github/actions/package-tvos/action.yml` | ✓ | SDK-Band-Pin `10.0.1xx` + `global.json` `10.0.100`/`latestPatch`; Rollback `{"microsoft.net.sdk.tvos": "<tvos-workload-version>"}` (Default `26.5.10318`, deckungsgleich mit `build-tvos`) + `workload install tvos --skip-manifest-update`; TPV-Bereinigung `Microsoft.tvOS.Sdk.net10.0_*`; Restore `-r tvos-arm64 -p:IncludeTvOsTarget=true`; Keychain-Import (temp. Keychain, `set-key-partition-list`, Profil nach `~/Library/MobileDevice/Provisioning Profiles`); Publish `-f net10.0-tvos -r tvos-arm64 -p:ArchiveOnBuild=true` mit `CodesignKey`/`CodesignProvision`, `ApplicationDisplayVersion` (Suffix gekürzt), `ApplicationVersion = git rev-list --count HEAD + run_attempt - 1`; Collect `release-tvos.ipa` + `release-tvos-dsym.zip`; iTMSTransporter-Upload (`if: api-key-p8 != ''`, Suchpfade + Standalone-Fallback); Keychain-Cleanup `if: always()`. Input `release-tag` für stabile Aufrufsignatur (wie iOS). |
| 2. `package-tvos`-Job in beiden Release-Workflows | ✓ | `staging-merge.yml` + `main-merge.yml`: Job hinter `vars.TVOS_SIGNING_ENABLED == 'true' && needs.release.outputs.released == 'true'`, `macos-latest`, `timeout-minutes: 45`, `fetch-depth: 0`, alle 8 `TVOS_*`-Secrets verdrahtet, Upload `release-tvos.ipa` + `release-tvos-dsym.zip` ans Release. iOS-Jobs unverändert (Contract-Test `ReleaseWorkflows_KeepIosPathIntact`). |
| 3. `scripts/tvOS-Deployment.ps1` | ✓ | Aktionen `build`/`simulator`/`device`/`store`/`upload`/`list`/`menu`; macOS lokal, Windows per schlüsselbasiertem SSH (`Invoke-OnMac`/`Invoke-MacCapture`, BatchMode) auf Remote-Checkout (`-RemoteRepoPath`/`VIDEOPLAYER_TVOS_MAC_REPO_PATH`, `Sync-RemoteCheckout` auf lokalen HEAD-SHA); `.ipa`-Rücktransfer nach `artifacts/`; Workload-Pin `26.5.10318` wie CI; `Assert-StorePrerequisites` (Distribution-Zertifikat, `.p8` außerhalb Repo); Remote-Validierung (codesign, `get-task-allow`, `UIDeviceFamily=3`, `ITSAppUsesNonExemptEncryption`, Privacy-Manifest, `iTMSTransporter -m verify` als Fallback); Upload via iTMSTransporter-Kandidatenliste; Transcript-Logging; Deploy-Prefs `.tvos-deploy.user.json`. Kein `VideoPlayerApiToken` (tvOS nutzt die Eigenschaft nicht). |
| 4. Store-Voraussetzungen tvOS-Projekt | ✓ | `VideoPlayer.TvOS/PrivacyInfo.xcprivacy` (.NET-Mindestmanifest, deckungsgleich mit iOS) + `<BundleResource Include="PrivacyInfo.xcprivacy" />` unter `IncludeTvOsTarget=true`; `ITSAppUsesNonExemptEncryption=false` in `Info.plist`. App-Icon/Top-Shelf-Assets und Multicast-Entitlement bewusst als manueller Vorlauf dokumentiert (Deployment-Doku). |
| 5. Contract-Tests | ✓ | `VideoPlayer.Maui.Tests/TvOsDeploymentContractTests.cs` — 7 Tests: Action-Pinning, signierter Geräte-Build, Build-Nummerierung/Upload, Gate in beiden Workflows (Theory), iOS-Pfade unverändert (Theory), Skript-Merkmale, Store-Metadaten, gitignore. Alle grün. |
| 6. Dokumentation | ✓ (Lifecycle-Schritt 9) | `docs/help/tv-app/deployment.md`, `index.md`/`installation.md`/`beschreibung.md`, `docs/help/index.md`, `README.md`. |
| 7. `.gitignore` | ✓ | `.tvos-deploy.user.json` ergänzt. |
| Abschluss | ◻ ausstehend | YAML-Syntax validiert (PyYAML), `dotnet test` grün (1482/1482), PowerShell-Syntax validiert; Push + `build-tvos`-Lauf folgt. |

## Offene Aufgaben

Keine.
