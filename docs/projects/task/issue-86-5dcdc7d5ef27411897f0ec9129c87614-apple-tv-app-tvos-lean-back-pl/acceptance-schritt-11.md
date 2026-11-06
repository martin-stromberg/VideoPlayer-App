# Abnahmeprüfung – Entwicklungsschritt 11

**Schritt:** Signiertes Release-Paket und automatisierter Upload nach App Store Connect
**Branch:** `task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl-schritt-11-release-upload`
**Geprüfter Stand:** `14a0ceb6ea4d7fcd74319b55b6b0d46a8d133029` (HEAD, vollständig gepusht)
**Diff-Basis:** `task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl...HEAD`

## Ergebnis

**Status: Anforderung erfüllt – externe Verifikation ausstehend**

Alle im Code prüfbaren Akzeptanzkriterien sind erfüllt. Der signierte Store-Build und der TestFlight-Upload sind plangemäß erst nach Bereitstellung der Apple-Account-Artefakte (manueller Vorlauf des Kontoinhabers) und Aktivierung des Gates `TVOS_SIGNING_ENABLED` extern verifizierbar — der Gate-Ansatz hält die Pipeline bis dahin grün und ist verifiziert.

### Einzelprüfung

| Kriterium | Ergebnis |
|---|---|
| (a) `package-tvos`-Action analog `package-ios` | ✅ `.github/actions/package-tvos/action.yml` (286 Zeilen) ist eine nahezu 1:1-Portierung: SDK-Band-Pin (`setup-dotnet` 10.0.1xx + `global.json` 10.0.100/latestPatch), Workload-Rollback `{"microsoft.net.sdk.tvos": "26.5.10318"}` + `dotnet workload install tvos --skip-manifest-update` inkl. TPV-Guard (deckungsgleich mit `build-tvos` in `pr-checks.yml:84`), `dotnet restore -r tvos-arm64 -p:IncludeTvOsTarget=true`, Keychain-Import mit Zufallspasswort + `set-key-partition-list`, signierter `dotnet publish -f net10.0-tvos -r tvos-arm64 -p:ArchiveOnBuild=true` mit `CodesignKey`/`CodesignProvision`, IPA-Sammlung `release-tvos.ipa` + `release-tvos-dsym.zip`, iTMSTransporter-Upload `-m upload -assetFile -apiKey -apiIssuer` mit `AuthKey_<id>.p8` unter `~/.appstoreconnect/private_keys` und identischer Kandidatenliste/Standalone-Fallback wie iOS |
| (b) Jobs hinter `TVOS_SIGNING_ENABLED`-Gate + `released`-Bedingung | ✅ `staging-merge.yml:181-224` und `main-merge.yml:168-206`: `if: vars.TVOS_SIGNING_ENABLED == 'true' && needs.release.outputs.released == 'true'` — ohne Gate wird der Job übersprungen, der Workflow bleibt grün (Job-`if` bricht nicht fehl). Beide Jobs `needs: release`, `macos-latest`, `fetch-depth: 0`, Secrets-Verdrahtung `TVOS_*` vollständig, Artefakt-Upload ans Release inkl. dSYM-Guard |
| (c) Build-Nummer-Konvention | ✅ `BUILD_NUMBER=$(( $(git rev-list --count HEAD) + ${{ github.run_attempt }} - 1 ))` — Commit-Anzahl plus Laufzähler, identisch zum iOS-Pfad; `CFBundleShortVersionString` aus Release-Version ohne Pre-Release-Suffix. Lokales Skript: `Get-CommitBuildNumber` (`scripts/tvOS-Deployment.ps1:476-489`) mit `-BuildNumber`-Override/`-NoBumpBuildNumber` |
| (d) iOS-Pfade unverändert | ✅ `git diff` auf `.github/actions/package-ios/action.yml` und `scripts/iOS-Deployment.ps1` ist leer; die Workflow-Diffs sind rein additiv (nur `package-tvos`-Blöcke). Contract-Test `ReleaseWorkflows_KeepIosPathIntact` sichert die iOS-Verdrahtung ab |
| (e) Lokales Skript analog iOS-Deployment (SSH vom Windows-Rechner) | ✅ `scripts/tvOS-Deployment.ps1` (990 Zeilen): Aktionen `build`/`simulator`/`device`/`store`/`upload`/`list`/`menu`; unter Windows Delegation per schlüsselbasiertem SSH (`BatchMode`, kein Passwort-Prompt) an einen Mac-Checkout (`-RemoteRepoPath`/`VIDEOPLAYER_TVOS_MAC_REPO_PATH`), Synchronisation auf lokalen HEAD (`Sync-RemoteCheckout`), IPA-Rücktransfer nach `artifacts/tvos/` (gitignored), Store-Validierung (`codesign --verify`, Dev-Profil-Abweis, `PrivacyInfo.xcprivacy`-Pflicht, `UIDeviceFamily=3`, `ITSAppUsesNonExemptEncryption=false`), `iTMSTransporter -m verify`/`upload`. Pair-to-Mac-MSBuild-Remoting entfällt begründet (tvOS-Projekt ist kein MAUI) — dokumentiert im Skript-Header und `docs/help/tv-app/deployment.md` |
| (f) Keine hartkodierten Secrets, Keychain-Cleanup `if: always()` | ✅ Sämtliche Zugangsdaten kommen aus `secrets.TVOS_*`/`vars`/Umgebungsvariablen; Keychain-Passwort ist Zufall (`openssl rand -base64 32`); `.p12`/`.mobileprovision`-Tempdateien werden entfernt; Keychain-Löschung läuft unter `if: always()` (`action.yml`, letzter Schritt). `.p8` muss lokal außerhalb des Repo liegen (Skript prüft das, `tvOS-Deployment.ps1:429-434`); `.tvos-deploy.user.json`, `artifacts/`, `logs/` sind gitignored |
| (g) `dotnet test VideoPlayer.Maui.Tests` | ✅ Lokal ausgeführt: **1514 Tests bestanden, 0 Fehler, 0 übersprungen** (net10.0, ~50 s). Lediglich vorhandene NU1903-Warnung (SignalR.Common 10.0.10), schrittunabhängig |
| (h) YAML-Validität | ✅ `staging-merge.yml`, `main-merge.yml`, `pr-checks.yml`, `package-tvos/action.yml`, `package-ios/action.yml` parsen fehlerfrei (PyYAML); zusätzlich PowerShell-AST-Parse von `tvOS-Deployment.ps1` fehlerfrei |
| (i) CI-Run `42142360871` | ✅ `gh run view`: `conclusion: success`, beide Jobs grün (`build-tvos` 9m06s, `verify` 6m23s); `headSha: 14a0ceb6ea4d7fcd74319b55b6b0d46a8d133029` == HEAD; `git log origin/<branch>..HEAD` leer — nichts Ungepushtes |

## Ausstehende externe Verifikationen

1. **Signierter tvOS-Build + TestFlight-Upload im Ist-Betrieb:** Erst nach manuellem Vorlauf des Kontoinhabers (tvOS-App-Record/Bundle-ID, tvOS-Distribution-Provisioning-Profil, Apple-Distribution-Zertifikat inkl. `.p12`, App-Store-Connect-API-Key) und dem Setzen der Secrets `TVOS_*` sowie der Variable `TVOS_SIGNING_ENABLED=true` verifizierbar. Bis dahin läuft der `package-tvos`-Job nicht an — Pipeline bleibt grün (by design, wie im Projektplan gefordert).
2. **App-Icon-/Top-Shelf-Assets:** Die Store-Verarbeitung verlangt für tvOS zwingend ein App-Icon-Set und Top-Shelf-Bild; im Projekt liegt noch kein `Assets.xcassets` vor (in `docs/help/tv-app/deployment.md` als Vorlauf-Punkt dokumentiert).
3. **Lokaler Skript-End-to-End-Lauf:** `store`/`upload` wurden unter Windows nicht gegen einen echten Mac ausgeführt (kein Zugang im Prüfumfeld); Syntax, Parameterverdrahtung und Pfadlogik sind statisch geprüft und durch `TvOsDeploymentContractTests` abgesichert.

## Hinweise

- Die Gate-Variable ist exakt analog zu `IOS_SIGNING_ENABLED` gestaltet; die Doku (`docs/help/tv-app/deployment.md`, 130 Zeilen, neu) beschreibt Gate, Vorlauf-Schritte, Secrets-Tabelle, Action-Ablauf und Skript-Nutzung vollständig.
- `package-tvos` steht — wie `package-ios` — nicht in den `needs` der Folgejobs `draft-pr`/`back-merge` (Konsistenz, kein neuer Kopplungspunkt).
- Store-relevante Metadaten ergänzt: `VideoPlayer.TvOS/PrivacyInfo.xcprivacy` (Required-Reason-APIs, als `BundleResource` im Bundle-Root, gegen ITMS-91053) und `ITSAppUsesNonExemptEncryption=false` in `Info.plist` (Export-Compliance).
- Neue Contract-Tests `VideoPlayer.Maui.Tests/TvOsDeploymentContractTests.cs` (8 Fakten/Theorien) prüfen den Release-Pfad quellbasiert — sinnvoll, da der signierte Build unter Windows nicht ausführbar ist.
- README.md und `docs/help/tv-app/*` wurden konsistent nachgezogen (Release-Pfad erwähnt, Index verlinkt).
- Die Buildnummer teilt sich mit iOS denselben Zählraum (Commit-Anzahl) — unkritisch, da App Store Connect `CFBundleVersion` pro App-Record wertet und die tvOS-App ein eigener Record ist (im Code dokumentiert).
