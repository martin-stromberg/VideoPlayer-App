# Tests — Bestandsaufnahme

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-10-14T00:43:54+02:00 (Lauf ca. 00:44–00:46)
- Branch und Commit-ID: `task/issue-49-8320ea2ee24347a3b7ecbb210062e168-rueckmeldungen-quellen-browsin-schritt-5-wiedergabeende`, Commit `e4889c0e24d4d59863c96e0ec8c36b00a7467547`
- Uncommittete Änderungen im getesteten Stand: keine (`git status --porcelain` leer; die Lifecycle-Artefakte unter `docs/features/` wurden parallel angelegt und sind ohne Einfluss auf den Build/Testlauf)
- Testumgebung und Runtime-/SDK-Versionen: Windows, .NET SDK 10.0.401, xunit; Workloads: android, ios, maccatalyst, maui-windows
- Ermittelte Testsuiten und Quellen der Testbefehle: `VideoWebPlayer.Maui.Tests` (Unit-/Contract-Tests, Befehl aus Aufgabenstellung/Projektkonvention); `VideoPlayer.E2E.Tests` (FlaUI-UI-Automatisierung, net10.0-windows, startet die echte App über `AppFixture`)

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Ausgangslauf | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --logger "console;verbosity=normal"` | Repo-Stamm | 0 | 1329 | 0 | 0 | [baseline-dotnet-test.log](test-results/baseline-dotnet-test.log) |
| E2E | nicht ausgeführt (Ausgangslauf beschränkt auf Unit-Suite; E2E startet die installierte App per FlaUI und ist zeit-/umgebungsintensiv) | – | – | unbekannt | unbekannt | unbekannt | – |

### Nachgewiesene bestehende Testfehler

Keine — der Ausgangslauf meldet 0 Fehlschläge.

### Testlücken und Ausführungsprobleme

- `VideoPlayer.E2E.Tests` im Ausgangslauf nicht ausgeführt (kein Fehlschlagnachweis, kein Erfolgsnachweis). Relevante Bestandstests: `PlaylistPlaybackTests.PlaybackEnd_AdvancesToNextPlaylistEntry`, `ContinueWatchingPlaybackTests.TapContinueWatchingEpisode_OpensDetailsAndStartsPlaybackAndAdvancesToNextEpisode`.

## Testklassen (relevant für die Anforderung)

### `AppUpdateTests_Player` (`VideoWebPlayer.Maui.Tests/AppUpdateTests_Player.cs`)

- `PlaybackEnded_MarksWatchedAndAdvances` — Ende-Message: „angesehen", Abschlussmeldung mit Endposition, Auto-Fortsetzung (Position = Dauer − 60 s — liegt im Orientierungsfenster)
- `PlaybackEnded_WithoutNext_MarksWatchedOnly` — Ende ohne Folge-Episode (Position = Dauer − 5 s)
- `PlaybackEnded_MarksEpisodeWatchedInDetails` — Watched-Übertrag in geladene Details (Position = Dauer − 1 min)
- `PlaybackEnded_AutoResume_StillMarksFinishedEpisode` — Auto-Fortsetzung markiert die beendete Episode (Position = Dauer − 5 s)
- `PlaybackEnded_AlreadyWatchedEpisode_KeepsServerTimestamp` — bestehender Server-Zeitpunkt bleibt (Position = Dauer − 1 min)
- `PlaybackEnded_Movie_DoesNotTouchEpisodeList` — Film-Ende berührt Episodenliste nicht (`PlayingState()` → Position 0, Dauer 45 min — **Position außerhalb jedes Toleranzfensters: Test würde nur noch über den Verwerfungs-Pfad grün bleiben; zur Erhaltung der Aussage Position ans Ende setzen**)
- `PlaybackEnded_UnknownEpisode_LeavesDetailsUnchanged` — unbekannte Episode lässt Details unverändert (Position 0 — **gleiche Situation**)
- `PlaybackProgress_*` — Watched-Fenster/Auto-Fortsetzung über den positionsbasierten Takt (unverändert relevant)
- Hilfsmethode `PlayingState(mediaId, upcoming, duration, position)`: Dauer-Standard 45 min, Positions-Standard 0

### `AppUpdateTests_PlaylistPlayback` (`VideoWebPlayer.Maui.Tests/AppUpdateTests_PlaylistPlayback.cs`)

- `PlaybackEnded_InPlaylist_AdvancesPlaylist` — Ende → `AdvancePlaylistEntryEffect` (`PlayingInPlaylist()`: Dauer 100 s, Position 96 s — plausibel)
- `PlaybackEnded_InPlaylist_AutoResumeOff_NoAdvance` — ohne Auto-Fortsetzung kein Advance (Position 96 s — plausibel)
- `PlaybackEnded_WithoutPlaylist_KeepsSeriesAdvance` — Serienpfad ohne Playlist (`PlayerState` ohne `Duration`/`Position` → beide 0 — **bricht unter der Prüfung; Position/Dauer zu befüllen**)
- Hilfsmethode `PlayingInPlaylist(...)`: Dauer 100 s, Position 96 s voreingestellt

### `AppUpdateTests_Downloads` (`VideoWebPlayer.Maui.Tests/AppUpdateTests_Downloads.cs`)

- `PlaybackEnded_WatchedEpisode_EmitsAutoDelete` — Ende + `AutoDeleteWatchedEpisodes` → `DeleteWatchedDownloadEffect` (Dauer 40 min, **Position 0 → bricht unter der Prüfung; Position ans Ende setzen**)

### `PlayerContractTests` (`VideoWebPlayer.Maui.Tests/PlayerContractTests.cs`)

- `PlayerPage_UsesMediaElementWithControls` — pinnt `MediaEnded=`-Verdrahtung im XAML
- `PlayerPage_DispatchesPlaybackMessages` — pinnt Dispatch von `PlaybackEndedMessage` u. a.
- `PlayerPage_IgnoresModelChangesAfterDisappearing` — pinnt `subscribed`-Guard in `OnMediaEnded` (u. a.)

### E2E (`VideoPlayer.E2E.Tests/Tests/`)

- `PlaylistPlaybackTests.PlaybackEnd_AdvancesToNextPlaylistEntry` — Auto-Fortsetzung am Titelende mit 20-s-Testvideo (Dauer < jedes Toleranzfenster → weiterhin plausibel)
- `PlaylistPlaybackTests.StreamFailure_AdvancesToNextPlaylistEntry` — Fehler-Rückfall (nicht das Ende-Ereignis — unverändert)
- `ContinueWatchingPlaybackTests.TapContinueWatchingEpisode_...` — Fortsetzungs-Wiedergabe mit Weiterschaltung

## Hilfsmethoden

- `PlayingState(...)` in `AppUpdateTests_Player` — laufender Serien-Player (Dauer 45 min, Position 0, Quelle ServerStream)
- `PlayingInPlaylist(...)` in `AppUpdateTests_PlaylistPlayback` — laufende Playlist-Wiedergabe (Dauer 100 s, Position 96 s)
- `WithPlayer`, `WithDetails`, `ShowDetails`, `Connected` — Modell-Fixtures
