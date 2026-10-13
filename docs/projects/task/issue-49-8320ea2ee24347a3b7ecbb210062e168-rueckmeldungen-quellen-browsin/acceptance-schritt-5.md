# Abnahmeprüfung – Entwicklungsschritt 5

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

- [ ] Keine.

## Hinweise

Prüfgegenstand: Diff `task/issue-49-...-rueckmeldungen-quellen-browsin...HEAD` (10 Dateien, +243/−8) sowie die Quelldateien `VideoPlayer.Mvu/AppUpdate.cs`, `VideoPlayer.Mvu/PlayerState.cs`, `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs` und die Testdateien.

**(a) Vollständige Verwerfung unplausibler Ereignisse:** `UpdatePlaybackEnded` (`AppUpdate.cs:3893–3956`) kehrt bei `!player.IsActive || !IsPlaybackEndPlausible(player)` mit `AppTransition.Unchanged(model)` zurück — identisches Modell, leere Effektliste (`AppTransition.cs:12`). Der Guard steht vor jeglicher Effektkonstruktion und vor der Verzweigung `IsPlaylistPlayback`/`HasNextEpisode`, sodass Angesehen-Markierung, `ReportPlaybackProgressEffect`, `DeleteWatchedDownloadEffect` und Auto-Vorlauf in beiden Kontexten (Serien-/Einzel- und Playlist-Wiedergabe) vollständig unterbleiben. Die einzige Dispatch-Quelle ist `PlayerPage.OnMediaEnded` (`PlayerPage.xaml.cs:417–424`), die unverändert `PlaybackEndedMessage` sendet — es gibt keinen zweiten Auslöser der Kette.

**(b) Tatsächliches Ende unverändert:** Im plausiblen Zweig läuft der bisherige Abschluss identisch: `IsWatched = true`, `Position = Duration` als `finalPosition` mit `ReportPlaybackProgressEffect`, optionale Download-Löschung, `MarkEpisodeWatchedInDetails`, Auto-Fortsetzung über `AdvancePlaylistEntry` (Playlist, mit `PlaylistNextExhausted`/`IsResolving`-Guards) bzw. `AdvanceToNextEpisode` (Serienkontext). Die Änderung `finalPosition = player.Duration` (vorher `Duration > 0 ? Duration : Position`) ist unter dem Guard äquivalent, da `IsPlaybackEndPlausible` `Duration > 0` bereits erzwingt.

**(c) Toleranz 300 s:** `PlaybackEndedToleranceSeconds = 300` (`AppUpdate.cs:88`, private const — kein neues konfigurierbares Verhalten, wie in der Projektklärung gefordert). Regel: `Duration > 0 && Position >= 0 && Duration − Position <= 300 s` (`IsPlaybackEndPlausible`, `AppUpdate.cs:3969–3975`). Ein Spurious-Event bei >300 s Restlaufzeit wird verworfen (Test: 301 s außerhalb → ignoriert). Die Begründung im XML-Doc ist stichhaltig: Innerhalb des Fensters greifen der Fortschrittstakt (Angesehen-Schwelle `WatchedEndWindowSeconds`, Standard 300 s) und die Auto-Fortsetzung ohnehin bereits (`AppUpdate.cs:3818–3867`), sodass ein Ende-Ereignis dort zum Normalablauf konsistent ist. Stale-State-Risiko gering: `PlayerState.Position`/`Duration` werden vom 5-s-Fortschrittstakt (`PlayerPage.xaml.cs:25, 133–165`) und zusätzlich bei jedem `StateChanged` (`PlayerPage.xaml.cs:435–452` → `ReportPosition`) geschrieben; ein legitimes Ende liegt damit maximal ~5 s hinter der letzten Meldung — weit innerhalb des Fensters. `OnMediaEnded` liest die Position nicht erneut, wertet also den letzten Takt — bei 300 s Fenster unkritisch. Position/Dauer werden beim Episoden-/Playlist-Wechsel auf 0 zurückgesetzt (`AppUpdate.cs:2591–2592, 3767–3768`), wodurch nachgezogene Events des Vorgängermediums verworfen werden (Test `PlaybackEnded_AfterAdvance_IgnoresStaleEvent`).

**(d) Duration = 0/unbekannt → kein Ende:** Dokumentierte und zur Projektklärung konforme Entscheidung; ohne Referenzdauer nicht unterscheidbar (XML-Doc `AppUpdate.cs:3958–3968`, Test `PlaybackEnded_UnknownDuration_IsIgnored`). Konsequenz: Medien ohne jemals gemeldete Dauer können über das Ende-Ereignis nicht abschließen — der Angesehen-/Auto-Fortsetzungs-Takt benötigt ebenfalls `Duration > 0`, das Verhalten ist also konsistent.

**(e) `IsActive`-Guard:** Sinnvoll und erhalten — Ereignisse ohne angeforderte Wiedergabe werden weiterhin verworfen.

**Einschränkungen (keine Abweichungen, aber dokumentiert):**
- Bei Medien kürzer als 300 s liegt die gesamte Laufzeit im Toleranzfenster; ein verfrühtes `MediaEnded` ist dort prinzipbedingt nicht von einem echten Ende unterscheidbar. Das ist konsistent zur Bestandslogik (Restlaufzeit ≤ Fenster gilt ab dem ersten Takt als „am Ende" — Angesehen-Markierung und Auto-Vorlauf greifen dort bereits) und im Playlist-Test explizit kommentiert.
- Schmale theoretische Restfälle: ein Seek direkt vor einem Ende-Ereignis (Position im Modell noch vor dem Sprung) oder ein vom Player nach dem Ende auf 0 gemeldeter Positionswert könnten in einem kleinen Race-Fenster zu einer falschen Bewertung führen. Durch die `StateChanged`-Positionsmeldung ist das Fenster klein; auf Unit-Ebene nicht weiter nachweisbar.

**Tests:** `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj` lokal ausgeführt — **1335/1335 grün** (0 Fehler, 0 übersprungen), Behauptung bestätigt. Sechs neue Tests decken die Pflichtfälle ab: `PlaybackEnded_MidPlayback_IsIgnored`, `PlaybackEnded_UnknownDuration_IsIgnored`, `PlaybackEnded_WithinTolerance_Completes` (300-s-Grenze inklusiv), `PlaybackEnded_JustOutsideTolerance_IsIgnored` (301 s), `PlaybackEnded_AfterAdvance_IgnoresStaleEvent`, `PlaybackEnded_InPlaylist_MidPlayback_IsIgnored` (Playlist-Pendant). Bestehende Ende-Tests wurden mit plausiblem Positions-/Dauer-Stand versehen (`AppUpdateTests_Downloads`, `AppUpdateTests_Player`, `AppUpdateTests_PlaylistPlayback`); die Playlist-Fixture `PlayingInPlaylist` (Position 96/100 s) liegt bereits im Fenster. Die im `changes.log` genannten 11/11 Wiedergabe-E2E-Tests wurden nicht ausgeführt (außerhalb des Prüfauftrags).
