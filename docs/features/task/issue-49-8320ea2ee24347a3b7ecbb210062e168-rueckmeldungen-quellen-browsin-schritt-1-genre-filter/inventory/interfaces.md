# Interfaces

## `IAppEffectHandler`

Datei: `VideoPlayer.Mvu/IAppEffectHandler.cs`

Contract zwischen dem plattformneutralen MVU-Kern und dem App-Rand: alle von `AppUpdate.Update` erzeugten `AppEffect`-Datensätze (Navigation, Server-Aufrufe — darunter `LoadSourceGenresEffect`) laufen über diese Schnittstelle. Implementiert von `ShellAppEffectHandler` (`VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `HandleAsync` | `(AppEffect effect, CancellationToken cancellationToken = default)` | `Task` | Führt einen einzelnen Effekt aus (Navigation, Server-Aufrufe o. ä.) |

Keine weiteren Interfaces für diese Anforderung relevant — der Genre-Ladepfad nutzt konkrete Dienste (`SourceBrowserService`, `VideoPlayerApiClient`), kein eigenes Interface.
