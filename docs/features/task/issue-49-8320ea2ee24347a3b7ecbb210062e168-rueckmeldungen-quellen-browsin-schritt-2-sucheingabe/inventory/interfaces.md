# Bestandsaufnahme — Interfaces und Developer-API

## `IAppEffectHandler`

Datei: `VideoPlayer.Mvu/IAppEffectHandler.cs` (Implementierung:
`ShellAppEffectHandler`, `VideoPlayer.Maui/Services/`)

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `HandleAsync` | `AppEffect`, `CancellationToken` | `Task` | Führt einen Effekt am App-Rand aus |

Relevante Effekte: `LoadSourceItemsEffect(SourceId, Page, SearchText,
GenreId, Append)` → `api/items` (`search`-Parameter), `LoadSourceGenresEffect`,
`NavigateToSourceOverviewEffect`, `NavigateToMediaDetailsEffect`.

## Developer-API (DevApi)

Server: `msTools.DeveloperApi.Maui/MauiDevApiService.cs` (nur DEBUG/DEV_API)

| Route | Zweck |
|-------|-------|
| `GET /tree` | Logischer Elementbaum; `DevApiTreeNode` enthält `AutomationId`, `Type`, `IsVisible`, `Text` (liest u. a. `InputView.Text` — damit auch den Inhalt eines `SearchBar`), `Placeholder`, `Items` |
| `POST /invoke` | Clicked-Äquivalent (`Button.SendClicked`, Schalter, Menüeinträge, Commands, Tap-Gesten, Listenauswahl per `itemIndex`) — **kein** SearchBar-Submit |
| `POST /set-text` | Setzt `InputView.Text` (feuert `TextChanged` wie eine Eingabe) |

Befund: Für den Suchfeld-Zugriff aus E2E-Tests fehlt dem `MediaSearchBar`
die `AutomationId`; das Abschicken der Suche ist über die DevApi nicht
auslösbar (kein Invoke-Pfad für `SearchButtonPressed`) — FlaUI-Tastatur
(Enter auf dem fokussierten Feld) ist der naheliegende Weg.
