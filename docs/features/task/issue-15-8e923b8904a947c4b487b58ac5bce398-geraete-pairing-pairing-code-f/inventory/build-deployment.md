# Build und Deployment — Bestandsaufnahme

## `VideoPlayerApiToken` (MSBuild-Eigenschaft)

Datei: `VideoPlayer.ApiClient/VideoPlayer.ApiClient.csproj`

```xml
<VideoPlayerApiToken Condition="'$(VideoPlayerApiToken)' == ''">&lt;MAUI_CLIENT_API_TOKEN&gt;</VideoPlayerApiToken>
<AssemblyAttribute Include="System.Reflection.AssemblyMetadataAttribute">
  <_Parameter1>VideoPlayer.ApiClient.ApiToken</_Parameter1>
  <_Parameter2>$(VideoPlayerApiToken)</_Parameter2>
</AssemblyAttribute>
```

- Die Eigenschaft ist in der Projektdatei definiert (Default: Platzhalter `<MAUI_CLIENT_API_TOKEN>`) und wird als `AssemblyMetadata`-Attribut in die `VideoPlayer.ApiClient.dll` eingebettet — von `ClientGate.Resolve()` über den Schlüssel `VideoPlayer.ApiClient.ApiToken` gelesen.
- `InternalsVisibleTo` auf `VideoWebPlayer.Maui.Tests` ist gesetzt (ermöglicht die internen `ClientGate.Resolve(env, meta, file)`-Überladungen in Tests).
- `VideoPlayer.Maui.csproj` referenziert `VideoPlayer.ApiClient` per `ProjectReference`; die Eigenschaft kann beim Publish der App per `-p:VideoPlayerApiToken=...` durchgereicht werden.

## `scripts/iOS-Deployment.ps1`

Build-/Deployment-Skript für die iOS-App (Aktionen `build`, `simulator`, `device`, `store`, `upload`, `list`, `menu`; Pair-to-Mac per SSH-Response-File).

**Befund:** Das Skript übergibt derzeit **kein** `-p:VideoPlayerApiToken` — weder in `Invoke-Build`/`Invoke-Run`/`Invoke-DeviceViaSsh`/`Invoke-Store` noch in den Response-Datei-Eigenschaften (`Add-PropertyLine`). Vorhandene Parameter betreffen Pair-to-Mac (`ServerAddress`, `ServerUser`, `ServerPassword`, `TcpPort`, `_DotNetRootRemoteDirectory`), Codesigning (`CodesignKey`, `CodesignProvision`, `CodesignEntitlements`) und App-Store-Connect (`ApiKeyPath`, `ApiKeyId`, `ApiIssuerId` — der App-Store-API-Schlüssel, kein Client-Gate-Token). Die Anforderungsangabe „bereits über `scripts/iOS-Deployment.ps1` nutzbar" trifft so noch nicht zu: Der Publish-Pfad `-p:VideoPlayerApiToken` existiert projektseitig, müsste aber im Skript noch als Parameter durchgereicht werden.

## Legacy-Projekt `VideoWebPlayer.Maui`

- `VideoWebPlayer.Maui/Services/MauiClientConfiguration.cs`: eigener statischer Token-Auflöser (`X-API-Key`, Env `VIDEOWEBPLAYER_MAUI_API_TOKEN`, Metadata-Key `VideoWebPlayer.Maui.ApiToken`, Platzhalter) — unabhängig von `ClientGate`.
- `VideoWebPlayer.Maui/VideoWebPlayer.Maui.csproj`: eigenständige MAUI-Projektdatei des Altbestands (wird in CI weiter gebaut: `pr-checks.yml` Android/Windows, `main-merge.yml`/`staging-merge.yml` iOS).
- `VideoWebPlayer.Client/VideoWebPlayerClient.cs` + `Models/`: geteilter älterer Client ohne Pairing-DTOs und ohne Gate-Logik; vom Testprojekt referenziert.

## CI-Workflows (Test-/Build-Kontext)

Dateien: `.github/workflows/pr-checks.yml`, `main-merge.yml`, `staging-merge.yml`

- `pr-checks.yml`: Release-Builds von `VideoWebPlayer.Client`, `VideoWebPlayer.Maui.Tests`, `VideoWebPlayer.Maui` (Android + Windows), `VideoPlayer.Maui` (Android + Windows); anschließend `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --no-build --configuration Release`.
- `main-merge.yml` / `staging-merge.yml`: zusätzlich iOS-Builds (`net10.0-ios`, `iossimulator-arm64`) beider MAUI-Projekte.

## Git-Hooks

`.githooks/` enthält u. a. `enum-coverage-check.py` (Enum-Werte müssen in Testdateien vorkommen), `csproj-xmldoc-check.py`, `no-notimplemented-check.py`, `translation-check.py`, `pre-commit`, `pre-push` — für neue Enums/Fehlertexte relevant.
