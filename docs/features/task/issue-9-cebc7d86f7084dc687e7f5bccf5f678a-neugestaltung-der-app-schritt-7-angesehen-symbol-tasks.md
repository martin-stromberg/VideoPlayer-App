# Tasks: Episodenlisten — Angesehen-Symbol bei gesehenen Episoden

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `MediaEntry.WatchedAt` (`DateTime?`) mit XML-Doku ergänzen | Offen | — |
| 2 | Datenmodell | `EpisodeItem.WatchedAt` + abgeleitetes `IsWatched` ergänzen | Offen | — |
| 3 | Datenmodell | `CachedShowEpisode.WatchedAt` ergänzen | Offen | — |
| 4 | Logik | `ShowDetailsCache.WriteAsync` schreibt `WatchedAt` mit | Offen | — |
| 5 | Logik | `MediaDetailsService.MapSeasons` mappt `WatchedAt` (online) | Offen | — |
| 6 | Logik | `MediaDetailsService.MapOfflineShow` mappt `WatchedAt` (Offline-Snapshot) | Offen | — |
| 7 | Logik | `AppUpdate`: Hilfsmethode + Markierung des `EpisodeItem` bei `IsWatched`-Übergang (Progress-Fenster und PlaybackEnded) | Offen | — |
| 8 | UI | `VpBadgeBg`/`VpBadgeBorder`-Tokens in `Colors.xaml` | Offen | — |
| 9 | UI | Watched-Badge (Pill-`Border` + Augen-`Path`/`Ellipse`s, oben links über dem Poster, `IsWatched`-Binding) im Episoden-`DataTemplate` der `MediaDetailsPage` | Offen | — |
| 10 | Tests | `MediaEntrySerializationTests`: `WatchedAt`-Deserialisierung (gesetzt/fehlend) | Offen | — |
| 11 | Tests | `MediaDetailsServiceTests`: WatchedAt-Mapping online + Offline-Snapshot | Offen | — |
| 12 | Tests | `AppUpdateTests_Player`: Markierung bei `PlaybackEnded` und Watched-Fenster | Offen | — |
| 13 | Tests | `MediaDetailsPageContractTests`: Badge-Markup-Vertrag | Offen | — |
| 14 | Tests | `DesignTokensContractTests`: neue Badge-Tokens pinnen | Offen | — |
