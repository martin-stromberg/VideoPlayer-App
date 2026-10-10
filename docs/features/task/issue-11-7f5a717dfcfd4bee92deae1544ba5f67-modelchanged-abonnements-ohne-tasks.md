# Tasks: ModelChanged-Abonnements ohne Abmeldung auf mehreren Pages (Lebenszyklus-Leck)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | UI | `HomePage`: `subscribed`-Feld einführen, `OnAppearing`/`OnDisappearing` anlegen, Abo aus Konstruktor umziehen, Guard in `OnModelChanged` | Offen | — |
| 2 | UI | `MediaDetailsPage`: `subscribed`-Feld, Abo-Umzug nach `OnAppearing` (vor `requested`-Guard), `OnDisappearing`, Guard in `OnModelChanged` | Offen | — |
| 3 | UI | `DownloadsPage`: `subscribed`-Feld, Abo-Umzug nach `OnAppearing`, `OnDisappearing`, Guard in `OnModelChanged` | Offen | — |
| 4 | UI | `FavoritesPage`: `subscribed`-Feld, Abo-Umzug nach `OnAppearing`, `OnDisappearing`, Guard in `OnModelChanged` | Offen | — |
| 5 | UI | `SourceOverviewPage`: `subscribed`-Feld, Abo-Umzug nach `OnAppearing`, `OnDisappearing`, Guard in `OnModelChanged` | Offen | — |
| 6 | UI | `ProfilePage`: `subscribed`-Feld, Abo-Umzug nach `OnAppearing`, `OnDisappearing`, Guard in `OnModelChanged` | Offen | — |
| 7 | Tests | `PageLifecycleContractTests` anlegen (`Read`/`MethodBody`-Helfer) | Offen | — |
| 8 | Tests | Sechs `{Page}_UnsubscribesModelChangedOnDisappearing`-Contract-Tests | Offen | — |
| 9 | Tests | Sechs `{Page}_IgnoresModelChangesAfterDisappearing`-Contract-Tests | Offen | — |
