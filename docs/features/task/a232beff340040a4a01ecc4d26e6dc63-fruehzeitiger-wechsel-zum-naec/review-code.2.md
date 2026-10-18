# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### AppUpdateTests_Downloads.cs / AppUpdateTests_PlaylistPlayback.cs (Update-Tests)

- **Doppelter Code** — Jeweils zwei Testmethoden prüfen dasselbe Szenario mit identischem Arrange, identischem Zwei-Takt-Act und identischen End-Asserts; die neuere Variante ergänzt lediglich Zwischen-Asserts auf den ersten Takt:
  - `PlaybackProgress_WatchedEpisode_EmitsAutoDelete` (Z. 467–491) ist eine strikte Teilmenge von `PlaybackProgress_TwoWindowTicks_EmitsAutoDelete` (Z. 495–525) — gleiches Modell, gleiche Takte (88→89 min), gleiche Asserts (`IsWatched`, `DeleteWatchedDownloadEffect` MediaId 33).
  - `PlaybackProgress_NearEnd_InPlaylist_AdvancesPlaylist` (Z. 533–556) ist eine strikte Teilmenge von `PlaybackProgress_NearEnd_TwoTicks_AdvancesPlaylist` (Z. 563–592) — gleiche Takte (97→98 s), gleiche Asserts (`IsResolving`, `AdvancePlaylistEntryEffect`).

  Empfehlung: Die Paare zusammenführen — die Zwischen-Asserts der „TwoTicks"-Varianten in die bestehenden Tests übernehmen und die neuen Duplikate entfernen (Muster dafür liefert `PlaybackProgress_MarksWatchedWithinWindow` in `AppUpdateTests_Player.cs`, das die Streak-Zwischenprüfung direkt im bestehenden Test integriert).

### PlayerPage.xaml.cs (PlayerPage)

- **Doppelter Code** — `ReportPosition` (Z. 176–203) leitet dieselben zwei Werte zweimal aus `state` ab: `state == MediaElementState.Playing` und `state is MediaElementState.Buffering or MediaElementState.Opening` stehen je einmal im `LogDebug`-Aufruf (Z. 192–193) und einmal im `PlaybackProgressMessage`-Aufruf (Z. 201–202). Divergieren die Ableitungen später, protokolliert die Diagnose andere Werte als die tatsächlich dispatchte Nachricht — genau das Protokoll, dem die Untersuchung des gestörten Streams vertrauen soll.

  Empfehlung: Vor der Log-Bedingung zwei lokale Variablen (`isPlaying`, `isBuffering`) aus `state` ableiten und an beiden Stellen verwenden.

## Geprüfte Dateien

Liste aller geprüften Dateien:
- `VideoPlayer.Mvu/AppUpdate.cs`
- `VideoPlayer.Mvu/AppMessage.cs`
- `VideoPlayer.Mvu/PlayerState.cs`
- `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`
- `VideoPlayer.Storage/AppSettings.cs`
- `VideoPlayer.Maui.Tests/AppSettingsTests.cs`
- `VideoPlayer.Maui.Tests/AppUpdateTests_Downloads.cs`
- `VideoPlayer.Maui.Tests/AppUpdateTests_Player.cs`
- `VideoPlayer.Maui.Tests/AppUpdateTests_PlaylistPlayback.cs`
- `VideoPlayer.Maui.Tests/PlayerContractTests.cs`
