# Bestandsaufnahme: Plausibilitätsprüfung des Wiedergabe-Ende-Ereignisses

Analysiert wurde die Kette vom `MediaEnded`-Ereignis des `MediaElement` über die `PlaybackEndedMessage` bis zum Abschluss-Ablauf in `AppUpdate.UpdatePlaybackEnded` (Angesehen-Markierung, End-Fortschrittsmeldung, Auto-Fortsetzung in Serien-/Einzel- und Playlist-Kontext).

## Zusammenfassung

- `PlayerPage.OnMediaEnded` (`VideoPlayer.Maui/Pages/PlayerPage.xaml.cs:417`) dispatcht `PlaybackEndedMessage` ungeprüft, sobald das `MediaElement` `MediaEnded` feuert (XAML-Verdrahtung `PlayerPage.xaml:18`). Einziger Dispatcher der Nachricht.
- `AppUpdate.UpdatePlaybackEnded` (`VideoPlayer.Mvu/AppUpdate.cs:3862`) prüft nur `PlayerState.IsActive` und löst danach den kompletten Abschluss aus: `IsWatched = true`, `ReportPlaybackProgressEffect` mit der Endposition, optional `DeleteWatchedDownloadEffect`, Auto-Fortsetzung über `AdvancePlaylistEntry` (Playlist-Kontext) bzw. `AdvanceToNextEpisode` (Serien-/Einzelkontext). Jede Positions-/Dauer-Plausibilisierung fehlt.
- `PlayerState` enthält bereits `Position` und `Duration` (letzter gemeldeter Takt, alle 5 s während `MediaElementState.Playing` plus bei jedem `StateChanged`) — die Datenbasis für den geforderten Positions-gegen-Dauer-Check ist vorhanden, ohne das `MediaElement` erneut anzufassen.
- Orientierungswert für das Toleranzfenster: `AppSettings.WatchedEndWindowSeconds` (Standard 300 s, Klärung verlangt feste Konstante, keine neue Einstellung).
- `UpdatePlaybackProgress` (Auto-Vorlauf über Restlaufzeit) ist positionsbasiert und damit von Natur aus plausibilisiert — nicht Teil der Änderung.
- `UpdatePlayerClosed` meldet beim Verlassen der Seite den Abschluss-Fortschritt — anwendergesteuert, nicht Teil der Änderung.
- Betroffene bestehende Tests (siehe `inventory/tests.md`): `PlaybackEnded_WatchedEpisode_EmitsAutoDelete` (Position 0 bei Dauer 40 min), `PlaybackEnded_WithoutPlaylist_KeepsSeriesAdvance` (Dauer/Position 0), `PlaybackEnded_Movie_DoesNotTouchEpisodeList` und `PlaybackEnded_UnknownEpisode_LeavesDetailsUnchanged` (Position 0 bei Dauer 45 min — inhaltlich weiter grün, verlieren aber ihre Testaussage, daher anzupassen).
- E2E-Infra (`VideoPlayer.E2E.Tests`, FlaUI/Windows, maui-windows-Workload installiert): `PlaybackEnd_AdvancesToNextPlaylistEntry` nutzt ein 20-s-Testvideo, das komplett innerhalb jedes denkbaren Toleranzfensters liegt — durch die Prüfung nicht gefährdet.

Test-Ausgangszustand: `dotnet test VideoWebPlayer.Maui.Tests` — 1329/1329 bestanden (Nachweis: [baseline-dotnet-test.log](inventory/test-results/baseline-dotnet-test.log), Details in [tests.md](inventory/tests.md)).

## Details

- [Logik](inventory/logic.md) — `PlayerPage`, `AppUpdate`, Effekt-Kette
- [Datenmodell](inventory/models.md) — `PlayerState`, `PlaybackEndedMessage`, `AppSettings`-Fensterwerte
- [Tests](inventory/tests.md) — Test-Ausgangszustand, betroffene und relevante Testklassen
