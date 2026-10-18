# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### AppUpdateTests_Player.cs (Update-Tests)

- **Doppelter Code** — Zwei Testmethoden prüfen dasselbe Szenario mit identischem Arrange, identischem Act und identischen bzw. Teilmengen-Asserts:
  - `PlaybackProgress_SeriesAdvance_RequiresNotResolving` (Z. 708–727) ist eine strikte Teilmenge von `PlaybackProgress_WhileResolving_NoWindowEffects` (Z. 682–704): identisches Modell (`PlayingState(endWindowStreak: 1, upcoming: [32])` + `IsResolving = true`, `AutoResumeEnabled = true`), identischer Takt (`duration - 10 s`), und seine beiden Asserts (`MediaId 31`, kein `ResolvePlaybackSourceEffect`) sind in den vier Asserts des anderen Tests (`EndWindowStreak 0`, `!IsWatched`, `MediaId 31`, kein `ResolvePlaybackSourceEffect`) vollständig enthalten.
  - `PlaybackProgress_TwoTicks_MarksEpisodeWatchedInDetails` (Z. 731–749) ist das Szenario-Duplikat von `PlaybackProgress_WatchedWindow_MarksEpisodeWatchedInDetails` (Z. 940–958): identisches Arrange (`WithDetails(ShowDetails())` + `PlayingState()`), derselbe Zwei-Takt-Act innerhalb des „angesehen"-Fensters (290→280 s vs. 300→295 s Restlaufzeit) und identische Asserts (`IsWatched`, `Seasons[0].Episodes[0].IsWatched`, `!Episodes[1].IsWatched`). Obwohl die Summary „erst mit dem zweiten Fenstertakt" behauptet, assertiert der Test nichts am Zwischenstand `first` — er deckt gegenüber dem bestehenden Test nichts ab.

  Empfehlung: `PlaybackProgress_SeriesAdvance_RequiresNotResolving` entfernen (sein Prüfumfang ist komplett abgedeckt). `PlaybackProgress_TwoTicks_MarksEpisodeWatchedInDetails` in `PlaybackProgress_WatchedWindow_MarksEpisodeWatchedInDetails` aufgehen lassen — entweder entfernen oder, falls die Zwischenbehauptung gemeint war, dort den fehlenden Zwischen-Assert (`!IsWatched` nach dem ersten Fenstertakt) ergänzen (Muster: `PlaybackProgress_MarksWatchedWithinWindow`).

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
