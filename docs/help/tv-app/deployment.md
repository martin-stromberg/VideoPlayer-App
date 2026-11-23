← [Zurück zur Übersicht](index.md)

# Apple-TV-App (tvOS) — Signiertes Release-Paket und App-Store-Connect-Upload

Der tvOS-Release-Pfad ist ein 1:1-Pendant des iOS-Pfads: eine
wiederverwendbare Paketierungs-Action, die in beide Release-Workflows
eingebunden ist, plus ein lokales Hilfsskript für manuelle Builds.

## Aktivierungs-Gate `TVOS_SIGNING_ENABLED`

Der signierte Store-Build läuft **nur**, wenn die Repository-Variable
`TVOS_SIGNING_ENABLED=true` gesetzt ist (Repository → Settings →
Secrets and variables → Actions → Variables). Ohne die Variable läuft
der `package-tvos`-Job nie — die Workflows bleiben grün, solange die
Apple-Account-Artefakte fehlen. Zusätzlich gilt wie beim iOS-Pfad der
Duplikat-Schutz `needs.release.outputs.released == 'true'`: ohne neuen
Release-Tag wird nichts gebaut/hochgeladen (sonst
`CFBundleVersion`-Kollision in TestFlight).

## Manueller Vorlauf (Kontoinhaber)

Vor der Aktivierung müssen im Apple-Developer-Account bzw. App Store
Connect vorliegen:

1. **tvOS-App-Record** mit bestätigter Bundle-ID (Arbeitsannahme im
   Projekt: `de.martinstromberg.videos.tv`).
2. **tvOS-Provisioning-Profil** (Distribution/App Store) für diese
   Bundle-ID.
3. **Apple-Distribution-Zertifikat** inkl. Private Key (`.p12`-Export).
4. **App-Store-Connect-API-Key** (`.p8` + Key-ID + Issuer-ID).
5. **App-Icon- und Top-Shelf-Assets** — die Store-Validierung verlangt
   für tvOS-Apps ein App-Icon-Set und ein Top-Shelf-Bild; im Projekt
   liegt noch kein `Assets.xcassets` vor.
6. Optional: **Multicast-Entitlement**
   (`com.apple.developer.networking.multicast`) für die mDNS-
   Servererkennung auf Geräten — erfordert eine Genehmigung durch
   Apple; ohne das Entitlement greift die UDP-Discovery
   (`UdpServerDiscovery`).

### Repository-Secrets (Pendant zu `IOS_*`)

| Secret | Inhalt |
|--------|--------|
| `TVOS_CODESIGN_KEY` | Signaturidentität, z. B. `Apple Distribution: …` |
| `TVOS_PROVISIONING_PROFILE` | Name des tvOS-Provisioning-Profils |
| `TVOS_CERTIFICATE_P12_BASE64` | Base64 des `.p12`-Exports |
| `TVOS_CERTIFICATE_P12_PASSWORD` | Passwort des `.p12` |
| `TVOS_PROVISIONING_PROFILE_BASE64` | Base64 der `.mobileprovision` |
| `TVOS_API_KEY_ID` / `TVOS_API_ISSUER_ID` / `TVOS_API_KEY_P8` | App-Store-Connect-API-Key (ID, Issuer, `.p8`-Inhalt) |

## Ablauf der Action `.github/actions/package-tvos`

Identisch mit `package-ios`, auf tvOS portiert:

1. `actions/setup-dotnet` (`10.0.1xx`) + `global.json`-Pin
   `10.0.100`/`latestPatch` (Band-Pin, damit das Rollback im
   `10.0.100`-Manifest-Feed auflöst).
2. Workload-Rollback `{"microsoft.net.sdk.tvos": "26.5.10318"}` +
   `dotnet workload install tvos --skip-manifest-update`; zu neue
   `Microsoft.tvOS.Sdk.net10.0_*`-Packs werden entfernt (TPV-Guard).
3. `dotnet restore VideoPlayer.TvOS -r tvos-arm64
   -p:IncludeTvOsTarget=true`.
4. Signatur-Import: temporäre Keychain (Zufallspasswort), `.p12`-Import,
   Profil nach `~/Library/MobileDevice/Provisioning Profiles`
   (Cleanup-Schritt läuft `if: always()`).
5. `dotnet publish -f net10.0-tvos -r tvos-arm64
   -p:ArchiveOnBuild=true` mit `CodesignKey`/`CodesignProvision`.
   **Build-Nummerierung** (Konvention): `CFBundleVersion` =
   `git rev-list --count HEAD + run_attempt − 1` — repo-weit monoton,
   pro App-Record eigener Zählraum (die tvOS-App ist ein eigener
   App-Store-Connect-Record). `CFBundleShortVersionString` =
   Release-Version ohne Pre-Release-Suffix.
6. Artefakte: `release-tvos.ipa` + `release-tvos-dsym.zip` (Symbolisierung
   späterer Crash-Reports) — werden ans GitHub-Release gehängt.
7. Upload per `iTMSTransporter -m upload -assetFile` mit API-Key
   (`.p8` unter `~/.appstoreconnect/private_keys`) — nur wenn
   `TVOS_API_KEY_P8` gesetzt ist. Suchpfade: Transporter.app,
   `/usr/local/itms`, `ContentDeliveryServices.framework`; Fallback:
   Standalone-Paket vom Apple-CDN.

## Einbindung

- `.github/workflows/staging-merge.yml` → Job `package-tvos`
  (Prerelease-Kandidat ans Staging-Release).
- `.github/workflows/main-merge.yml` → Job `package-tvos`
  (finale Version ans Release).

## Lokales Hilfsskript `scripts/tvOS-Deployment.ps1`

Muster: `scripts/iOS-Deployment.ps1`. Da `VideoPlayer.TvOS` kein
MAUI-Projekt ist, greift das Pair-to-Mac-MSBuild-Remoting **nicht** —
das Skript delegiert unter Windows per schlüsselbasiertem SSH
(`BatchMode`) an einen Mac mit eigenem Repo-Checkout.

```powershell
# Aktionen: build | simulator | device | store | upload | list | menu
./scripts/tvOS-Deployment.ps1 -Action list                      # Identitäten/Profile/Geräte auf dem Mac
./scripts/tvOS-Deployment.ps1 -Action build                     # unsignierter Compile-Check
./scripts/tvOS-Deployment.ps1 -Action simulator                 # Build + tvOS-Simulator-Start + Screenshot
./scripts/tvOS-Deployment.ps1 -Action store                     # signierter Release-Build + Validierung + TestFlight-Upload
./scripts/tvOS-Deployment.ps1 -Action upload -IpaPath app.ipa   # vorhandene .ipa validieren + hochladen
```

Umgebungsvariablen (Pendant `VIDEOPLAYER_IOS_*` → `VIDEOPLAYER_TVOS_*`):

- `VIDEOPLAYER_TVOS_CODESIGN_KEY`, `VIDEOPLAYER_TVOS_PROVISIONING_PROFILE`
- `VIDEOPLAYER_TVOS_MAC_SERVER_ADDRESS`, `VIDEOPLAYER_TVOS_MAC_SERVER_USER`
- `VIDEOPLAYER_TVOS_MAC_REPO_PATH` — Repo-Checkout auf dem Mac
  (Standard `~/VideoPlayer-App`); Build-Aktionen synchronisieren ihn vorab
  auf den lokalen HEAD-Commit (`git fetch` + `checkout`)
- `VIDEOPLAYER_TVOS_API_KEY_PATH` (`.p8` außerhalb des Repo),
  `VIDEOPLAYER_TVOS_API_KEY_ID`, `VIDEOPLAYER_TVOS_API_ISSUER_ID`

Unter Windows wird die `.ipa` nach Remote-Builds nach
`artifacts/tvos/…` (gitignored) zurückkopiert; `-Action upload`
akzeptiert eine lokale `.ipa` (`-IpaPath`), die per `scp` auf den Mac
wandert und von dort validiert/hochgeladen wird. Die Remote-Validierung
prüft `codesign --verify`, verwirft Development-Profile
(`get-task-allow`), fordert `PrivacyInfo.xcprivacy` im Bundle-Root,
`UIDeviceFamily=3` und `ITSAppUsesNonExemptEncryption=false`; die
Buildnummer folgt derselben Commit-Anzahl-Konvention wie die CI
(`-NoBumpBuildNumber`/`-BuildNumber` als Opt-out/Override).

## Store-Metadaten im Projekt

- `VideoPlayer.TvOS/PrivacyInfo.xcprivacy` — .NET-Mindestmanifest
  (Required-Reason-APIs), als `BundleResource` im Bundle-Root
  (sonst ITMS-91053).
- `ITSAppUsesNonExemptEncryption=false` in `Info.plist` — entfällt die
  Export-Compliance-Abfrage pro Build.
