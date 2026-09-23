# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| 1. Badge bei gesetztem `WatchedAt` (Pill-Form, dunkler Grund, Auge in `VpPrimarySoft`, feiner Rand, oben links über dem Poster) | Schritte 1, 2, 5: `MediaEntry.WatchedAt` → `EpisodeItem.WatchedAt`/`IsWatched` → Badge-`Border` (`RoundRectangle 999`, `VpBadgeBg`/`VpBadgeBorder`, Augen-`Path`+`Ellipse`s) über dem Poster-`Grid` | `MediaDetailsServiceTests.Load_TvShow_MapsEpisodeWatchedAt`, `MediaDetailsPageContractTests.MediaDetailsPage_EpisodeWatchedBadge_*` | Abgedeckt |
| 2. Ungesehene Episoden zeigen kein Badge | `IsWatched => WatchedAt.HasValue` als einziges Sichtbarkeitskriterium (`IsVisible`-Binding) | `MediaEntrySerializationTests` (Feld fehlt → `null`), Mapping-Test (ungefüllte Episode → `null`), `IsWatched`-Ableitung | Abgedeckt |
| 3. Status aktualisiert sich, wenn eine Episode gesehen wurde | Schritt 4: `MarkEpisodeWatchedInDetails` in `UpdatePlaybackProgress` (Schwellen-Übergang) und `UpdatePlaybackEnded`, vor `AdvanceToNextEpisode` | `AppUpdateTests_Player.PlaybackEnded_MarksEpisodeWatchedInDetails`, `PlaybackProgress_WatchedWindow_MarksEpisodeWatchedInDetails` | Abgedeckt |
| 4. Offline-Snapshot übernimmt den Stand | `CachedShowEpisode.WatchedAt`, `ShowDetailsCache.WriteAsync`, `MapOfflineShow` | `MediaDetailsServiceTests.Load_OfflineShow_MapsEpisodeWatchedAt` inkl. Alt-Cache (`null`) | Abgedeckt |
| 5. Keine Server-Änderung, Gesehen-Logik unverändert | Plan berührt nur Client-Modell, Mapping, eine View-Aktualisierung und XAML; `ReportPlaybackProgressEffect`, `WatchedEndWindowSeconds`, `AutoDeleteWatchedEpisodes` bleiben unverändert | Bestehende Player-Tests (`PlaybackProgress_*`, `PlaybackEnded_*`) müssen unverändert grün bleiben — im Baseline-Lauf dokumentiert | Abgedeckt |
| 6. DoD: Build Debug + Release, volle Testsuite grün | Umsetzungsreihenfolge endet mit Tests; Ausgangslauf 728/728 dokumentiert | `inventory/tests.md`, Abschlusslauf in `test-results.md` | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Badge-Anzeige in der Episodenliste (AK 1–3) | — | Nicht erforderlich mit Begründung: Das Repo besitzt keine UI-E2E-Infrastruktur für die MAUI-App (nur xUnit; die MAUI-Schicht ist im Testprojekt nicht instanziierbar). Der etablierte Funktionsnachweis (Schritte 5 und 6) sind MVU-Übergangstests plus Quell-Vertragstests — der Plan folgt diesem Muster und begründet es im E2E-Abschnitt |

## Hinweise

- Der Badge-Fallback-Fall (gesehene Episode als Offline-Platzhalter) ist
  unproblematisch: `IsWatched` blendet unabhängig von `IsPlaceholder` ein —
  fachlich korrekt (die Episode war gesehen), kein Eingriff nötig.
- `MediaDetailsService.LoadEpisodeAsync` ohne Serienkontext erzeugt keine
  Episodenliste — `WatchedAt`-Mapping ist dort wirkungslos und bewusst nicht
  geplant.
- `AutoDownloadService` schreibt den `ShowDetailsCache` über denselben
  `WriteAsync`-Pfad — die Cache-Erweiterung greift dort automatisch mit.
