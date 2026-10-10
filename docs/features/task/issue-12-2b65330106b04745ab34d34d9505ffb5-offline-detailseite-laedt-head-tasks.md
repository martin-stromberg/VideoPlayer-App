# Tasks: Offline-Detailseite lädt Header-Bild nach Reconnect nicht nach

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Logik | `WithDetailsHeaderLoad`-Gate um `HasHeaderImage`-Bedingung erweitern + XML-Doku ergänzen | Erledigt | `AppUpdateTests_DetailsHeader.Reconnect_ExistingHeaderImage_NoReload`, `MediaDetailsLoaded_ExistingHeaderImage_NoReload` |
| 2 | Logik | `EnterConnected`: `WithDetailsHeaderLoad(updated, effects)` nach `WithHeroLoad` einreihen + Kommentar | Erledigt | `AppUpdateTests_DetailsHeader.Reconnect_OfflineEpisodeDetails_EmitsHeaderEffect` |
| 3 | Tests | `Reconnect_OfflineEpisodeDetails_EmitsHeaderEffect` anlegen | Erledigt | Test selbst (bestanden) |
| 4 | Tests | `Reconnect_OfflineDetailsWithoutHeaderSource_NoHeaderEffect` anlegen | Erledigt | Test selbst (bestanden) |
| 5 | Tests | `Reconnect_ExistingHeaderImage_NoReload` anlegen | Erledigt | Test selbst (bestanden) |
| 6 | Tests | `Reconnect_NoDetails_NoHeaderEffect` anlegen | Erledigt | Test selbst (bestanden) |
| 7 | Tests | `MediaDetailsLoaded_ExistingHeaderImage_NoReload` anlegen | Erledigt | Test selbst (bestanden) |
| 8 | Verifikation | `dotnet build` + `dotnet test VideoWebPlayer.Maui.Tests` grün | Erledigt | 1037 Tests bestanden, 0 Fehler |
