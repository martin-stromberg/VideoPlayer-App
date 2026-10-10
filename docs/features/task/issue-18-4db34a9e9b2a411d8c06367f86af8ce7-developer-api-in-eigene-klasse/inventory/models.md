# Detail: Datenmodelle

## Generisch (portierbar in die Bibliothek)

### `DevApiRequest`
Datei: `VideoPlayer.Mvu/DevApi/DevApiRequest.cs` — `sealed record`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Method` | `string` (required) | HTTP-Methode |
| `Path` | `string` (required) | Pfad ohne Query |
| `Headers` | `IReadOnlyDictionary<string,string>` | OrdinalIgnoreCase |
| `Body` | `string` | Request-Body als Text |
| `TryGetJsonBody<T>(out T?, out string?)` | Methode | JSON-Deserialisierung (Web-Defaults, camelCase tolerant) mit deutschem Fehlertext |

### `DevApiResponse`
Datei: `VideoPlayer.Mvu/DevApi/DevApiResponse.cs` — `sealed record`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `StatusCode` | `int` (required) | HTTP-Status |
| `Body` | `string` | Antwort-Body |
| `ContentType` | `string` | Default `application/json; charset=utf-8` |
| `Json(object)` | static Factory | 200 + camelCase-JSON |
| `Error(int, string)` | static Factory | `{"error":"…"}` camelCase |

### `DevApiMetadata`
Datei: `VideoPlayer.Mvu/DevApi/DevApiMetadata.cs` — `sealed record`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Port` | `int` | Systemvergebener Port |
| `Pid` | `int` | Prozess-ID |
| `StartedUtc` | `DateTime` | Startzeitpunkt UTC |

## MAUI-/App-gebunden (bleiben in der App)

### `DevApiTreeNode`
Datei: `VideoPlayer.Maui/DevApi/DevApiTreeNode.cs` — `sealed record`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `AutomationId` | `string?` | AutomationId des Elements |
| `Type` | `string` (required) | CLR-Typname |
| `IsVisible` | `bool` | Sichtbarkeits-Flag |
| `Bounds` | `DevApiBounds?` | Layout-Position/-Größe |
| `Text` | `string?` | Anzeigetext |
| `Placeholder` | `string?` | Platzhalter eines Eingabefelds |
| `Children` | `IReadOnlyList<DevApiTreeNode>` | Unterelemente |
| `Items` | `IReadOnlyList<DevApiTreeItem>?` | CollectionView-Einträge (UIA-Lücke) |

### `DevApiBounds`
Datei: `VideoPlayer.Maui/DevApi/DevApiTreeNode.cs` — `sealed record`: `X`, `Y`, `Width`, `Height` (je `double`).

### `DevApiTreeItem`
Datei: `VideoPlayer.Maui/DevApi/DevApiTreeNode.cs` — `sealed record`: `Index` (`int`), `Type` (`string`), `Text` (`string?`), `Detail` (`string?`).

### `DevApiModelSnapshot`
Datei: `VideoPlayer.Mvu/DevApi/DevApiModelSnapshot.cs` — `sealed record`, ~30 Felder (`CurrentPage`, `ConnectionState`, `ServerHealth`, `IsStartupRunning`, `LoginError`, `IsOnline`, `ActiveServerAddress`, `KnownServers`, `DiscoveredServers`, Pairing-/Bootstrap-Flags, `HeroTitle`, Zähler …). `FromModel(AppModel)` mappt aus `AppModel`. Keine Token-/Passwort-Felder per Konstruktion.

### `DevApiKnownServer`, `DevApiDiscoveredServer`
Datei: `VideoPlayer.Mvu/DevApi/DevApiModelSnapshot.cs` — `sealed record`s mit `Address`, `DisplayName`/`Name`, `NameOrAddress`, `LastUsedAtUtc`/`Source`.

## Verbraucher-Modelle (E2E)

`VideoPlayer.E2E.Tests/Support/DevApiModels.cs` spiegelt `/model`- und `/tree`-Felder client-seitig — die JSON-Feldnamen (camelCase) sind der Vertrag.
