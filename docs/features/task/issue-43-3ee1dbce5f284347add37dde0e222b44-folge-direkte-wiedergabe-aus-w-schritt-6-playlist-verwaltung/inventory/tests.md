# Inventar — Testinfrastruktur

## Testprojekte und Befehle

| Projekt | Framework | Befehl |
|---|---|---|
| `VideoWebPlayer.Maui.Tests` | xUnit, net10.0 | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj` |
| `VideoPlayer.E2E.Tests` | xUnit + FlaUI.UIA3, Windows | `dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj -f net10.0-windows10.0.19041.0` |
| Gesamt-Build | — | `dotnet build VideoPlayer.App.sln` |

- Baseline (dieser Schritt): Build 0 Fehler/293 Warnungen; Unit **1206/1206 grün**.
- Coverage: `dotnet test --collect:"XPlat Code Coverage"` (im Test-Schritt).
- Git-Hook `enum-coverage-check` → jeder Enum-Wert braucht Vertragstest
  (`EnumCoverageTests`).

## Stubs/Helfer (Unit)

- `Stubs/StubHttpMessageHandler` (`Enqueue(status, json)`, `EnqueueJson`,
  `Requests` mit `Method`/`PathAndQuery`), `ApiClientTestFactory`,
  `InMemorySettingsStore`, `InMemorySecretStore`, `RecordingDelays`,
  `AppProfile.Create(null)`.
- Testaufbau `PlaylistServiceTests`: `AuthSessionService` mit
  `InMemory*`-Stores + `HttpClient(handler)`; `LoginAsync()` enqueuet Token.

## E2E-Infrastruktur

- `E2ETestBase`: `StartAppAsync` (eigenes Profil), `ConnectToPairingRequiredAsync`,
  `PairAsync`, `LoginAsync`, `LoginOnHomeAsync`, `ContinueWatchingIndexAsync`.
- `MockVideoServer`: Konstruktor `(pairingCode, deviceName, username, password)`;
  `BaseAddress`, `Playlists` (`List<MockPlaylist>`), `PlaybackMovieId/…`,
  `WasPathRequested`, Request-Tracking; `MockPlaylist{Id,Name,Description,
  SortMode,CoverPictureId,CoverPng,Genres,IsOwner,IsPublic,Entries}`,
  `MockPlaylistEntry` (inkl. `SortOrder`? — zu prüfen/ergänzen: `SortOrder`
  fehlt vermutlich → Erweiterung nötig), `AddContinueWatching`.
- `DevApiClient`: `GetModelAsync`, `WaitForModelAsync`, `GetTreeAsync`,
  `InvokeAsync`, `SelectItemAsync`, `LongPressAsync`, `SetTextAsync`.
- `UiDriver`: `FindByName`, `ClickByName` (UIA-Klartext, für native
  ActionSheets/Alerts), `EnterText`, `ClickButton`.
- `DevApiModelSnapshot`: Playlist-Lesefelder vorhanden; Verwaltungsfelder
  (Formular offen, Pending-Konflikte, Notices, CanEdit) sind zu ergänzen.

## E2E-Fähigkeiten/Lücken

- `/invoke`: Button, ImageButton, Switch, CheckBox, MenuItem, Commands,
  Tap-Gesten; `/set-text`: InputView; ItemIndex-Auswahl CollectionView/
  BindableLayout; `longpress` via `SendItemLongPressed`.
- `Picker` nicht setzbar → keine Picker in neuen UI-Flüssen.
- Native ActionSheet/Alert-Buttons über `UiDriver.ClickByName` (Muster:
  `ContinueWatchingContextMenuTests.WaitForName`).
