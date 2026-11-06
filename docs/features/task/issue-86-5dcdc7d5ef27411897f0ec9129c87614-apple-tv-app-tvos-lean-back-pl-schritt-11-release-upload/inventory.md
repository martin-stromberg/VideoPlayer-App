# Bestandsaufnahme — Schritt 11: Signiertes Release-Paket und Upload nach App Store Connect (tvOS)

Detail-Bestandsaufnahme des iOS-Release-Pfads liegt bereits vor: `docs/projects/task/issue-86-.../inventory/ci.md` (nicht ändern — `docs/projects/` ist read-only). Hier die tvOS-relevanten Ergänzungen und Lücken.

## Vorlage: iOS-Release-Pfad (Bestand, siehe `inventory/ci.md`)

| Baustein | Ort | tvOS-Pendant |
|----------|-----|--------------|
| Composite-Action | `.github/actions/package-ios/action.yml` (287 Z.) | `.github/actions/package-tvos/action.yml` (neu) |
| Staging-Release | `.github/workflows/staging-merge.yml` — Jobs `release` → `build-ios` (xcode-27-Image, Rollback `27.1.10018`), `package-ios` (macos-latest, Gate `vars.IOS_SIGNING_ENABLED == 'true'` **und** `needs.release.outputs.released == 'true'`), `draft-pr` | neuer Job `package-tvos` mit Gate `vars.TVOS_SIGNING_ENABLED == 'true'` + `released` |
| Main-Release | `.github/workflows/main-merge.yml` — analog (`build-ios` macos-latest, Rollback `26.5.10318`) | neuer Job `package-tvos`, gleiche Bedingung |
| Lokales Skript | `scripts/iOS-Deployment.ps1` (1156 Z.) + `scripts/iOS-Deployment.ResponseFile.ps1` (Helper, rsp-Erzeugung/Sanitizing) | `scripts/tvOS-Deployment.ps1` (neu) |
| Secrets | `IOS_CODESIGN_KEY`, `IOS_PROVISIONING_PROFILE`, `IOS_API_KEY_ID`, `IOS_API_ISSUER_ID`, `IOS_API_KEY_P8`, `IOS_CERTIFICATE_P12_BASE64`, `IOS_CERTIFICATE_P12_PASSWORD`, `IOS_PROVISIONING_PROFILE_BASE64`; Variable `IOS_SIGNING_ENABLED` | gleiche Namen mit `TVOS_`-Präfix; Variable `TVOS_SIGNING_ENABLED` |

## tvOS-Bestand (aus Schritt 1)

- `VideoPlayer.TvOS/VideoPlayer.TvOS.csproj`: `net10.0-tvos` hinter `IncludeTvOsTarget`-Schalter (Default true nur unter macOS), `OutputType=Exe`, `ApplicationId=de.martinstromberg.videos.tv` (Arbeitsannahme), `RuntimeIdentifiers`: `tvossimulator-x64;tvossimulator-arm64;tvos-arm64`. Keine MAUI-Abhängigkeit → `MauiXamlInflator`, `IncludeAndroidTarget`/`IncludeMacCatalystTarget`-Env-Schalter und Pair-to-Mac-MSBuild-Remoting (`ServerAddress`/`ServerUser`/`TcpPort`/`_DotNetRootRemoteDirectory`) greifen **nicht** — Remote-Build unter Windows muss per SSH auf einen Mac mit eigenem Repo-Checkout delegiert werden.
- `pr-checks.yml` Job `build-tvos`: macos-latest, SDK-Band-Pin `10.0.100`/`latestPatch`, Rollback `{"microsoft.net.sdk.tvos": "26.5.10318"}` + `dotnet workload install tvos --skip-manifest-update`, Simulator-Build `tvossimulator-arm64` mit `EnableCodeSigning=false`.
- `VideoPlayer.TvOS/Info.plist`: `UIDeviceFamily=3`, `NSLocalNetworkUsageDescription`, `NSBonjourServices`, `NSAppTransportSecurity.NSAllowsArbitraryLoads` — **kein** `ITSAppUsesNonExemptEncryption` (iOS-Plist hat `false`; für Store-Uploads sinnvoll, Export-Compliance-Abfrage entfällt).
- **Kein `PrivacyInfo.xcprivacy` im tvOS-Projekt** — iOS trägt das .NET-Mindestmanifest unter `Platforms/iOS/Resources/`; App-Store-Verarbeitung meldet sonst ITMS-91053 (Required-Reason-APIs: FileTimestamp C617.1, SystemBootTime 35F9.1, DiskSpace E174.1, UserDefaults CA92.1). Muss als `BundleResource` ins App-Bundle.
- Kein App-Icon/`Assets.xcassets` im tvOS-Projekt — Store-Validierung verlangt später App-Icon + Top-Shelf-Bild; gehört wie Bundle-ID/Profil/Zertifikate zum manuellen Vorlauf (im Gate-Konzept abgedeckt, zu dokumentieren).
- Multicast-Entitlement (`com.apple.developer.networking.multicast`) für mDNS-Discovery auf Geräten: Account-seitige Genehmigung nötig — UDP-Discovery (`UdpServerDiscovery`) ist Fallback; kein Entitlements-Plist-Eingriff im Rahmen dieses Schritts.

## Workflow-Mechanik (aus iOS übernommen)

- `released`-Output schützt vor `CFBundleVersion`-Kollisionen (nur bei neuem Tag bauen/hochladen).
- Build-Nummer: `BUILD_NUMBER=$(( $(git rev-list --count HEAD) + github.run_attempt - 1 ))`; Display-Version = Release-Version ohne Pre-Release-Suffix. `fetch-depth: 0` Pflicht.
- Keychain-Import: temp. Keychain mit Zufallspasswort, `security import` .p12, `set-key-partition-list`, Profil nach `~/Library/MobileDevice/Provisioning Profiles` (gleiches Verzeichnis für tvOS-Profile), Cleanup-Step `if: always()`.
- iTMSTransporter-Suchpfade: Transporter.app (`/Applications`, `$HOME/Applications`), `/usr/local/itms`, `ContentDeliveryServices.framework` unter `xcode-select -p`; Fallback Standalone-Paket vom Apple-CDN. Upload: `-m upload -assetFile <ipa> -apiKey <id> -apiIssuer <issuer>`; `.p8` unter `~/.appstoreconnect/private_keys/AuthKey_<id>.p8`.
- Pack-Bereinigung nach Workload-Pin: zu neue `Microsoft.tvOS.Sdk.net10.0_*`-Packs entfernen (TPV-Resolution würde sonst das neueste Pack wählen) — analog `Microsoft.iOS.Sdk.net10.0_*`.
- `.ipa`-Sammlung: `find <proj>/bin/Release -name '*.ipa'`, Dedupe per Basename, `publish/`-Pfad bevorzugen; dSYMs → Zip.

## Lücken / zu schaffen

1. `.github/actions/package-tvos/action.yml` — iOS-Action auf `VideoPlayer.TvOS` portiert (RID `tvos-arm64`, Workload `tvos`, Sdk-Pack `Microsoft.tvOS.Sdk.net10.0_*`, Input `tvos-workload-version` Default `26.5.10318`, Output `release-tvos.ipa`/`release-tvos-dsym.zip`, `-p:IncludeTvOsTarget=true` beim Restore/Publish zur Absicherung).
2. `package-tvos`-Job in beiden Release-Workflows hinter `vars.TVOS_SIGNING_ENABLED == 'true' && needs.release.outputs.released == 'true'` (macos-latest, 45 min, `fetch-depth: 0`), Upload `release-tvos.ipa` (+ dSYM-Zip) ans Release — iOS-Jobs unverändert.
3. `scripts/tvOS-Deployment.ps1` — Aktionen `build`, `simulator`, `store`, `upload`, `list`, `menu`; macOS lokal, Windows via SSH (`VIDEOPLAYER_TVOS_MAC_*`, Remote-Repo-Checkout auf gleichem Commit); `Assert-StorePrerequisites` (Distribution-Zertifikat, .p8 außerhalb Repo), Commit-Buildnummer (`Get-CommitBuildNumber`), Remote-Validierung (codesign, Development-Profil-Abweisung, `UIDeviceFamily=3`, `ITSAppUsesNonExemptEncryption`, Privacy-Manifest), iTMSTransporter-Upload.
4. `VideoPlayer.TvOS/PrivacyInfo.xcprivacy` (.NET-Mindestmanifest wie iOS) + `BundleResource`-Eintrag im csproj; `ITSAppUsesNonExemptEncryption=false` in `Info.plist`.
5. Contract-Tests `TvOsDeploymentContractTests` (Muster `TvOsProjectContractTests`/`IosDeploymentResponseFileTests`): Action-Inhalt, Gate in beiden Workflows, iOS-Pfade unverändert, Skript-Merkmale, Privacy/Encryption-Manifest.
6. `.gitignore`: `.tvos-deploy.user.json` ergänzen (Pendant zu `.ios-deploy.user.json`).
7. Doku: `docs/help/tv-app/` Deployment-Abschnitt (Gate, Secrets-Liste, Skript-Aufruf, manueller Vorlauf Bundle-ID/Profil/Zertifikate/Icon-Assets), `README.md` tvOS-Zeile.

## Rahmen

- tvOS lokal (Windows) nicht kompilierbar → Push autorisiert, `build-tvos` muss grün sein (Run-ID nennen).
- `dotnet test VideoPlayer.Maui.Tests` grün halten; YAML lokal validieren (python/yaml vorhanden).
- Keine Secrets im Code; `docs/projects/` nicht anfassen.
