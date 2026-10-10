# Tasks: Offline-Detailseite lädt Header-Bild nach Reconnect nicht nach

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Logik | `WithDetailsHeaderLoad`-Gate um `HasHeaderImage`-Bedingung erweitern + XML-Doku ergänzen | Offen | — |
| 2 | Logik | `EnterConnected`: `WithDetailsHeaderLoad(updated, effects)` nach `WithHeroLoad` einreihen + Kommentar | Offen | — |
| 3 | Tests | `Reconnect_OfflineEpisodeDetails_EmitsHeaderEffect` anlegen | Offen | — |
| 4 | Tests | `Reconnect_OfflineDetailsWithoutHeaderSource_NoHeaderEffect` anlegen | Offen | — |
| 5 | Tests | `Reconnect_ExistingHeaderImage_NoReload` anlegen | Offen | — |
| 6 | Tests | `Reconnect_NoDetails_NoHeaderEffect` anlegen | Offen | — |
| 7 | Tests | `MediaDetailsLoaded_ExistingHeaderImage_NoReload` anlegen | Offen | — |
| 8 | Verifikation | `dotnet build` + `dotnet test VideoWebPlayer.Maui.Tests` grün | Offen | — |
