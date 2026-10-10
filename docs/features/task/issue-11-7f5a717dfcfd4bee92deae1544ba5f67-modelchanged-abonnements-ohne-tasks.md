# Tasks: ModelChanged-Abonnements ohne Abmeldung auf mehreren Pages (Lebenszyklus-Leck)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | UI | `HomePage`: `subscribed`-Feld einführen, `OnAppearing`/`OnDisappearing` anlegen, Abo aus Konstruktor umziehen, Guard in `OnModelChanged` | Erledigt | `PageLifecycleContractTests.HomePage_UnsubscribesModelChangedOnDisappearing`, `HomePage_IgnoresModelChangesAfterDisappearing` |
| 2 | UI | `MediaDetailsPage`: `subscribed`-Feld, Abo-Umzug nach `OnAppearing` (vor `requested`-Guard), `OnDisappearing`, Guard in `OnModelChanged` | Erledigt | `PageLifecycleContractTests.MediaDetailsPage_UnsubscribesModelChangedOnDisappearing`, `MediaDetailsPage_IgnoresModelChangesAfterDisappearing` |
| 3 | UI | `DownloadsPage`: `subscribed`-Feld, Abo-Umzug nach `OnAppearing`, `OnDisappearing`, Guard in `OnModelChanged` | Erledigt | `PageLifecycleContractTests.DownloadsPage_UnsubscribesModelChangedOnDisappearing`, `DownloadsPage_IgnoresModelChangesAfterDisappearing` |
| 4 | UI | `FavoritesPage`: `subscribed`-Feld, Abo-Umzug nach `OnAppearing`, `OnDisappearing`, Guard in `OnModelChanged` | Erledigt | `PageLifecycleContractTests.FavoritesPage_UnsubscribesModelChangedOnDisappearing`, `FavoritesPage_IgnoresModelChangesAfterDisappearing` |
| 5 | UI | `SourceOverviewPage`: `subscribed`-Feld, Abo-Umzug nach `OnAppearing`, `OnDisappearing`, Guard in `OnModelChanged` | Erledigt | `PageLifecycleContractTests.SourceOverviewPage_UnsubscribesModelChangedOnDisappearing`, `SourceOverviewPage_IgnoresModelChangesAfterDisappearing` |
| 6 | UI | `ProfilePage`: `subscribed`-Feld, Abo-Umzug nach `OnAppearing`, `OnDisappearing`, Guard in `OnModelChanged` | Erledigt | `PageLifecycleContractTests.ProfilePage_UnsubscribesModelChangedOnDisappearing`, `ProfilePage_IgnoresModelChangesAfterDisappearing` |
| 7 | Tests | `PageLifecycleContractTests` anlegen (`Read`/`MethodBody`-Helfer) | Erledigt | Testklasse selbst |
| 8 | Tests | Sechs `{Page}_UnsubscribesModelChangedOnDisappearing`-Contract-Tests | Erledigt | Tests selbst (Negativprobe auf Ist-Stand fehlgeschlagen) |
| 9 | Tests | Sechs `{Page}_IgnoresModelChangesAfterDisappearing`-Contract-Tests | Erledigt | Tests selbst (Negativprobe auf Ist-Stand fehlgeschlagen) |
