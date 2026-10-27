# Code-Review: Episodenlisten — Angesehen-Symbol bei gesehenen Episoden

Basisbranch: `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app`
(namensbasiert, ohne Suffix `-schritt-7-angesehen-symbol`).

## Ergebnis

**Status:** Keine Befunde

## Prüfschwerpunkte

| Bereich | Befund |
|---------|--------|
| MVU-Reinheit | `IsWatched` ist eine abgeleitete Eigenschaft am `EpisodeItem` (`WatchedAt.HasValue`) — kein MAUI-Typ, kein UI-Wissen im Core; das Badge bindet direkt daran. |
| Idempotenz | `MarkEpisodeWatchedInDetails` greift nur, wenn `target.WatchedAt` noch leer ist — ein bereits vom Server gelieferter Zeitpunkt wird nicht durch `DateTime.UtcNow` überschrieben (Test `PlaybackEnded_AlreadyWatchedEpisode_KeepsServerTimestamp`). |
| Gating | Markierung nur bei `tvshowepisode` (normalisiert über `MediaTypeNames.Normalize`), vorhandenem Serienkontext (`HasSeasons`) und enthaltener Episode; Film-Wiedergabe und Fremd-Ids lassen die Ansicht unverändert (`Assert.Same`-Identität bei Nicht-Treffer — kein unnötiges Record-Kopieren). |
| Reihenfolge | Die Markierung erfolgt auf dem Model **vor** `AdvanceToNextEpisode` — die Episode wird markiert, obwohl der Player bereits die nächste Identität trägt (`PlaybackEnded_AutoResume_StillMarksFinishedEpisode`). |
| Unveränderte Gesehen-Logik | `ReportPlaybackProgressEffect`, `WatchedEndWindowSeconds`-Schwelle, `DeleteWatchedDownloadEffect` und Auto-Resume-Fenster sind unverändert; die Erweiterung ist rein additiv auf dem Model. |
| Kosten | `MarkEpisodeWatchedInDetails` läuft je Progress-Takt, bricht aber früh ab (`!player.IsWatched`, `WatchedAt is not null`) — keine nennenswerte Mehrarbeit im Hot-Path. |
| Abwärtskompatibilität | `CachedShowEpisode.WatchedAt` ist `init`-optional; JSON ohne das Feld deserialisiert zu `null` (Legacy-Test mit handgeschriebenem Alt-Cache-JSON). |
| Doppelter Code | Online- (`MapSeasons`) und Offline-Mapping (`MapOfflineShow`) folgen dem bestehenden Zweig-Muster der Datei; die eine Zeile `WatchedAt = e.WatchedAt` pro Zweig ist die etablierte Struktur, keine neue Duplikation. |
| XAML | Keine Farbliterale — ausschließlich `VpBadgeBg`/`VpBadgeBorder`/`VpPrimarySoft`/`VpText` (per Contract-Test gepinnt); `InputTransparent` verhindert Gesten-Verschlucken; Badge liegt im Poster-`Grid` statt die Listenstruktur zu verändern. |
| Stil | Deutsche XML-Doku an allen neuen Membern (`MediaEntry.WatchedAt`, `EpisodeItem.*`, `CachedShowEpisode.WatchedAt`, `MarkEpisodeWatchedInDetails` mit `param`/`returns`); kein `NotImplementedException`; kein toter oder kommentierter Code. |
| Testqualität | Neue Tests folgen Arrange-Act-Assert, prüfen fachliches Verhalten (Zeitpunkt gesetzt/andere unverändert/Identität erhalten) statt Implementierungsdetails; Contract-Tests prüfen das Badge-Markup positionsbasiert im Episoden-Template. |

## Geprüfte Randfälle

- Episode bereits vom Server als gesehen geladen → lokaler Zeitpunkt wird
  nicht überschrieben.
- `PlaybackEnded` bei Film (`movie`) → Episodenliste unberührt.
- `PlaybackEnded` bei Episode, die nicht in den geladenen Staffeln liegt
  → `Details`-Instanz bleibt identisch.
- Auto-Fortsetzung aktiv → markierte Episode + Player läuft auf Folge-
  Episode weiter.
- Legacy-Cache ohne `WatchedAt` → alle Episoden ungesehen, kein Badge.

## Geprüfte Dateien

- `VideoPlayer.ApiClient/Models/MediaEntry.cs`
- `VideoPlayer.Mvu/MediaDetailsView.cs`
- `VideoPlayer.Mvu/ShowDetailsCache.cs`
- `VideoPlayer.Mvu/MediaDetailsService.cs`
- `VideoPlayer.Mvu/AppUpdate.cs`
- `VideoPlayer.Maui/Pages/MediaDetailsPage.xaml`
- `VideoPlayer.Maui/Resources/Styles/Colors.xaml`
- `VideoWebPlayer.Maui.Tests/MediaEntrySerializationTests.cs`
- `VideoWebPlayer.Maui.Tests/MediaDetailsServiceTests.cs`
- `VideoWebPlayer.Maui.Tests/AppUpdateTests_Player.cs`
- `VideoWebPlayer.Maui.Tests/MediaDetailsPageContractTests.cs`
- `VideoWebPlayer.Maui.Tests/DesignTokensContractTests.cs`

## Nächster Schritt

Schritt 10 — Builds (Debug/Release) und volle Testsuite (bereits ausgeführt,
siehe `test-results.md`).
