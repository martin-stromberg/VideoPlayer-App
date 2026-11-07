# Enums

## `ConnectionState` (`VideoPlayer.Mvu/ConnectionState.cs`)

`Unknown`, `Connecting`, `Connected`, `NeedsLogin`, `NeedsServerSetup`,
`Offline`. Steuert die Navigation; `IsOffline` im Model abgeleitet.
Für diese Anforderung relevant: `Connected` → `Offline` bei Netzwerk-
verlust, `Offline` → `Connecting` bei Netzwerk-Rückkehr.

## `ServerHealthStatus` (`VideoPlayer.Mvu/ServerHealthStatus.cs`)

`Unknown`, `Checking`, `Reachable`, `Unreachable`.

## `AuthAttemptStatus` (`VideoPlayer.Mvu/AuthAttempt.cs`)

`Succeeded`, `MissingCredentials`, `InvalidCredentials`,
`ServerUnreachable`, `Failed`.

## `AppPage` (`VideoPlayer.Mvu/AppPage.cs`)

`Loading`, `Home`, `Sources`, `Favorites`, `Downloads`, `Profile`,
`Settings`, `ServerSetup`, `Login`. `AppRoutes.MenuPages` enthält die
sechs Menüseiten; `Loading`, `ServerSetup`, `Login` sind Workflow-Seiten
ohne Menü-Route — relevant für die Entscheidung „bei Wiederverbindung
nicht zwangsnavigieren".

## Plattform-Enum (Extern)

`Microsoft.Maui.Networking.NetworkAccess`: `Unknown`, `None`, `Local`,
`ConstrainedInternet`, `Internet` — Grundlage der Online-Erkennung.
