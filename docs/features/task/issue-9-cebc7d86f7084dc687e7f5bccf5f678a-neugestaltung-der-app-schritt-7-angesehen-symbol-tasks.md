# Tasks: Episodenlisten — Angesehen-Symbol bei gesehenen Episoden

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `MediaEntry.WatchedAt` (`DateTime?`) mit XML-Doku ergänzen | Erledigt | `MediaEntrySerializationTests.MediaEntry_DeserializesWatchedAt` |
| 2 | Datenmodell | `EpisodeItem.WatchedAt` + abgeleitetes `IsWatched` ergänzen | Erledigt | `MediaDetailsPageContractTests.EpisodeItem_CarriesWatchedState` |
| 3 | Datenmodell | `CachedShowEpisode.WatchedAt` ergänzen | Erledigt | `MediaDetailsServiceTests.Load_WithoutClient_ShowSnapshot_MapsEpisodeWatchedAt` |
| 4 | Logik | `ShowDetailsCache.WriteAsync` schreibt `WatchedAt` mit | Erledigt | `MediaDetailsServiceTests.Load_WithoutClient_ShowSnapshot_MapsEpisodeWatchedAt` (Cache-Roundtrip) |
| 5 | Logik | `MediaDetailsService.MapSeasons` mappt `WatchedAt` (online) | Erledigt | `MediaDetailsServiceTests.Load_TvShow_MapsEpisodeWatchedAt` |
| 6 | Logik | `MediaDetailsService.MapOfflineShow` mappt `WatchedAt` (Offline-Snapshot) | Erledigt | `MediaDetailsServiceTests.Load_WithoutClient_LegacyCache_TreatsEpisodesAsUnwatched` |
| 7 | Logik | `AppUpdate`: Hilfsmethode + Markierung des `EpisodeItem` bei `IsWatched`-Übergang (Progress-Fenster und PlaybackEnded) | Erledigt | `AppUpdateTests_Player.PlaybackEnded_MarksEpisodeWatchedInDetails`, `…_AutoResume_StillMarksFinishedEpisode`, `PlaybackProgress_WatchedWindow_MarksEpisodeWatchedInDetails` |
| 8 | UI | `VpBadgeBg`/`VpBadgeBorder`-Tokens in `Colors.xaml` | Erledigt | `DesignTokensContractTests.Colors_DefinesWatchedBadgeTokens` |
| 9 | UI | Watched-Badge (Pill-`Border` + Augen-`Path`/`Ellipse`s, oben links über dem Poster, `IsWatched`-Binding) im Episoden-`DataTemplate` der `MediaDetailsPage` | Erledigt | `MediaDetailsPageContractTests.MediaDetailsPage_EpisodeWatchedBadge_BindsIsWatched`, `…_FollowsWebDesign` |
| 10 | Tests | `MediaEntrySerializationTests`: `WatchedAt`-Deserialisierung (gesetzt/fehlend) | Erledigt | `MediaEntry_DeserializesWatchedAt`, `MediaEntry_WithoutWatchedAt_StaysNull` |
| 11 | Tests | `MediaDetailsServiceTests`: WatchedAt-Mapping online + Offline-Snapshot | Erledigt | 3 neue Tests (online, Snapshot, Legacy-Cache) |
| 12 | Tests | `AppUpdateTests_Player`: Markierung bei `PlaybackEnded` und Watched-Fenster | Erledigt | 6 neue Tests |
| 13 | Tests | `MediaDetailsPageContractTests`: Badge-Markup-Vertrag | Erledigt | 3 neue Tests |
| 14 | Tests | `DesignTokensContractTests`: neue Badge-Tokens pinnen | Erledigt | `Colors_DefinesWatchedBadgeTokens` |
