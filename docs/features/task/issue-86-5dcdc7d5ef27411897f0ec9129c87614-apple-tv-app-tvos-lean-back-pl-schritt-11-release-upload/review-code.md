# Code-Review — Schritt 11: Signiertes Release-Paket und Upload nach App Store Connect

Status: **Keine Befunde**

## Geprüfte Änderungen

- `.github/actions/package-tvos/action.yml` (neu)
- `.github/workflows/staging-merge.yml`, `.github/workflows/main-merge.yml` (je ein `package-tvos`-Job ergänzt)
- `scripts/tvOS-Deployment.ps1` (neu)
- `VideoPlayer.TvOS/VideoPlayer.TvOS.csproj` (`BundleResource`), `Info.plist` (`ITSAppUsesNonExemptEncryption`), `PrivacyInfo.xcprivacy` (neu)
- `VideoPlayer.Maui.Tests/TvOsDeploymentContractTests.cs` (neu)
- `.gitignore` (`.tvos-deploy.user.json`)

## Befunde

Keine.

## Geprüfte Risikopunkte (ohne Befund)

- **Secrets im Code:** keine — alle sensiblen Werte kommen als `${{ secrets.TVOS_* }}`/Umgebungsvariablen; `.p8` wird nur nach `~/.appstoreconnect/private_keys` mit `chmod 700` geschrieben; Keychain-Passwort ist ein Laufzeit-Zufallswert; `.p12`/`.mobileprovision` werden nach Import gelöscht (`if: always()`-Cleanup).
- **Gate-Korrektheit:** `if: vars.TVOS_SIGNING_ENABLED == 'true' && needs.release.outputs.released == 'true'` — ohne die Repository-Variable läuft der Job nie (Workflow bleibt grün); der Duplikat-Schutz `released` verhindert `CFBundleVersion`-Kollisionen wie beim iOS-Pfad.
- **Signatur-Schritte gegated:** Keychain-Import nur bei `certificate-p12-base64 != ''`, Upload nur bei `api-key-p8 != ''` — partiell gesetzte Secrets erzeugen klare Teilzustände (gleiche Semantik wie iOS).
- **Buildnummer:** `git rev-list --count HEAD + run_attempt - 1` — identisch mit iOS; `fetch-depth: 0` im Caller garantiert volle Historie.
- **TFM-Schalter:** `IncludeTvOsTarget=true` per `-p:` (Restore + Publish) und env — der macOS-Default würde ohnehin greifen; explizit gegen Host-OS-Abhängigkeit.
- **PowerShell:** `Parser::ParseFile` ohne Fehler; ein anfänglich nicht geschlossener String (`CodesignEntitlements`-Zeile) und eine Pfad-Inkonsistenz zwischen `Copy-IpaBackToLocal`/`Get-LatestIpa` (`artifacts/.../<rid>`) wurden im Review behoben.
- **YAML:** alle geänderten Workflow-/Action-Dateien mit PyYAML `safe_load` validiert.
- **iOS-Pfade:** `package-ios`, `IOS_SIGNING_ENABLED`, iOS-Secrets, `build-ios` — unverändert (Contract-Tests).
- **Bash-Snippets im Skript:** `Invoke-MacCapture`/`Invoke-OnMac` normalisieren CRLF vor Base64/SSH; `trap`-Cleanup in allen Temp-Verzeichnis-Snippets; `set -e` an kritischen Stellen.
