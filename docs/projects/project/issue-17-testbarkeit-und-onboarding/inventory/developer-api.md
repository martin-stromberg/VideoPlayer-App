# Detail: Developer-API (Schritt 2)

## Ziel (requirement Z. 28–41)

HTTP-Schnittstelle in der Windows-App zur strukturierten UI-Analyse/-Steuerung; `127.0.0.1`, Port 0 (systemvergeben), Port-Ablage in Metadatendatei im Programmverzeichnis; nur Debug-Build oder explizites Opt-in, niemals Release.

## Anzapfpunkte

| Bedarf | Naht | Stelle |
|--------|------|--------|
| Modell lesen | `MvuHost.Model` (unveränderlicher `AppModel`-Snapshot) | `VideoPlayer.Mvu/MvuHost.cs` Z. 30 |
| Änderungen beobachten | `MvuHost.ModelChanged` / `PropertyChanged` | Z. 33/36 |
| Aktionen auslösen | `MvuHost.DispatchAsync(AppMessage)` — serialisiert via `SemaphoreSlim` | Z. 15, 44–68 |
| DI-Registrierung | `MauiProgram.CreateMauiApp()` | `VideoPlayer.Maui/MauiProgram.cs` Z. 21–128 |
| Lifecycle-Start | `App.CreateWindow()` / `RunStartupAsync` | `VideoPlayer.Maui/App.xaml.cs` Z. 32–60 |
| Seitenbaum | `AppShell` (Singleton, Z. 101) → `CurrentPage`-Route via `AppRoutes` | `AppShell.xaml.cs` Z. 59–77, `AppRoutes.cs` |
| UI-Thread | `MainThread.BeginInvokeOnMainThread` / `InvokeOnMainThreadAsync` | Muster `ServerSetupPage.xaml.cs` Z. 45 |

## Sicher exponierbarer Modellauszug (`GET /model`)

Aus `AppModel.cs`: `CurrentPage` (13), `ConnectionState` (26), `ServerHealth`+`ServerHealthText` (19/188), `IsStartupRunning` (16), `IsLoginRunning`/`LoginError`/`LoginUsername` (29–38), `IsOnline`/`HasOfflineData`/`IsSyncRunning` (41–54), `ActiveServerAddress` (70), `KnownServers`/`DiscoveredServers` (64/73), `IsDiscoveryRunning`/`DiscoveryAttempted`/`IsServerCheckRunning`/`ServerSetupError` (76–88), `IsPairingRequired`/`IsPairingRunning`/`PairingError` (95–104), `StorageNotice` (211). Sensible Daten (Tokens, Passwort) stehen nicht im Modell — der Auszug ist per Konstruktion safe.

## `GET /tree`

- Logischer Baum über `IVisualTreeElement` ab `AppShell`/aktivem `Window.Page`; je Knoten `AutomationId`, Typ, `IsVisible`, Bounds (`Frame`/`Bounds` des Elements bzw. `GetLocation`/`AbsoluteBounds` auf Windows), Text (`Text`/`Placeholder`).
- **CollectionView-Items**: nicht aus dem Visual Tree, sondern aus `CollectionView.ItemsSource` + ItemTemplate-Datenkontext ableiten (löst die belegte UIA-Lücke — CollectionView-Items fehlen unter Windows im UIA-Baum).

## `POST /invoke`, `POST /set-text`, optional `POST /dispatch`

- Lookup per `AutomationId` (Element traversal); `invoke` → `Clicked`-Event-Äquivalent (`SendClicked()` auf `Button`, `SendToggled` auf `Switch`); `set-text` → `Entry.Text` setzen + `TextChanged`.
- `dispatch` → `AppMessage`-Factory (Basistyp `abstract record AppMessage`, `AppMessage.cs` Z. 7); umgeht UI-Eventkette — als Opt-in-Endpunkt markieren.

## Aktivierung

- Compile-Time bevorzugt: `#if DEBUG` plus eigene Konstante für Opt-in (`DevApi` Define via `-p:DefineConstants` oder Env-Var-gesteuertes MSBuild-Property). Laufzeit-Opt-in (Env-Var `VIDEOPLAYER_DEV_API=1`) nur als zweite Schicht — Anforderung: „niemals Release".
- Port 0: `HttpListener` kann kein `port 0`-Prefix; Optionen: `TcpListener(IPAddress.Loopback, 0)` zur Port-Ermittlung + eigenes Mini-HTTP (plattformneutral, keine Paketabhängigkeit) oder Kestrel-Minimalserver (Paket `Microsoft.AspNetCore` — Schwergewicht für eine Debug-API). Metadatendatei z. B. `devapi.json` neben der Exe (`AppContext.BaseDirectory`).

## Debug-only Vorläufer

- `#if DEBUG builder.Logging.AddDebug()` (`MauiProgram.cs` Z. 123–125).
- `[Conditional("DEBUG")] WriteDiagnostic` (`ClientGate.cs` Z. 142–167) — schreibt bereits eine Diagnosedatei ins Temp-Verzeichnis.
