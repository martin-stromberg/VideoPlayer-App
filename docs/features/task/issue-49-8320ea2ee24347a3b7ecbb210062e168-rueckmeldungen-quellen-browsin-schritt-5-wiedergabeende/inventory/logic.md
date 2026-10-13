# Logik — Bestandsaufnahme

## `PlayerPage`

Datei: `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs` (Code-Behind; XAML `PlayerPage.xaml` verdrahtet `MediaEnded="OnMediaEnded"`, Zeile 18)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `OnMediaEnded` (Z. 417) | private | Protokolliert „Wiedergabe beendet" und dispatcht `PlaybackEndedMessage` — ungeprüft, nur `subscribed`-Guard (verwaiste Seiten). Kein Zugriff auf `VideoElement.Position`/`Duration`. |
| `ReportPosition` (Z. 167) | private | Dispatcht `PlaybackProgressMessage(Position, Duration, IsPlaying, IsBuffering)` aus `VideoElement` — liefert dem Modell Positions- und Dauerstand. |
| `OnMediaStateChanged` (Z. 435) | private | Marshalt auf den UI-Thread und ruft `ReportPosition` — Zustandswechsel (u. a. am Medienende) melden aktuelle Position. |
| `StartProgressTimer` (Z. 133) | private | 5-s-Takt (`ProgressTickInterval`), ruft `ReportPosition` nur bei `MediaElementState.Playing`. |
| `OnMediaOpened` / `TryApplyPendingResumeSeek` / `ApplyResumeSeekAsync` | private | Fortsetzungs-Seek beim Öffnen (gebremst über Bereitschaftszustand). |
| `OnMediaFailed` (Z. 426) | private | Dispatcht `PlaybackFailedMessage` (Fehler-Rückfall). |

Abonnierte Events (XAML): `MediaOpened`, `MediaEnded`, `MediaFailed`, `StateChanged` des `MediaElement`; `mvuHost.ModelChanged`.
Publizierte Messages: `PlaybackEndedMessage`, `PlaybackProgressMessage`, `PlaybackFailedMessage`, `PlayerPageOpenedMessage`, `PlayerClosedMessage`, `PictureInPictureRequestedMessage`, `PlaylistPreviousRequestedMessage`, `PlaylistNextRequestedMessage`.

## `AppUpdate`

Datei: `VideoPlayer.Mvu/AppUpdate.cs` (reine statische Update-Funktion, `AppMessage` → `AppTransition` mit `AppModel` + `AppEffect`s)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Update` (Z. ~270) | public | Dispatch-Tabelle: `PlaybackEndedMessage → UpdatePlaybackEnded(model)` (Z. 271). |
| `UpdatePlaybackEnded` (Z. 3862) | private | Abschluss-Ablauf: Guard `IsActive`; `IsWatched = true`, `Position = Duration` (Fallback Position), `ReportPlaybackProgressEffect` (Endposition), optional `DeleteWatchedDownloadEffect`; Auto-Fortsetzung `AdvancePlaylistEntry` (Playlist) / `AdvanceToNextEpisode` (Serien) bei `AutoResumeEnabled`. **Keinerlei Plausibilisierung von Position gegen Dauer.** |
| `UpdatePlaybackProgress` (Z. 3777) | private | Übernimmt Position/Dauer in den State, meldet Fortschritt (Throttle 0,5 s über `LastReportedPositionSeconds`), setzt `IsWatched` innerhalb `WatchedEndWindowSeconds`, Auto-Fortsetzung bei `AutoResumeRemainingSeconds` — rein positionsbasiert. |
| `AdvanceToNextEpisode` (Z. 3720) | private | Wechsel zur nächsten Episode der `UpcomingEpisodes` (Auto-Fortsetzung/Fehler-Rückfall, Serien-/Einzelkontext). Setzt `Position`/`Duration` auf `TimeSpan.Zero` zurück. |
| `AdvancePlaylistEntry` (Z. 2597) | private | Wechsel zum nächsten Playlist-Eintrag über `AdvancePlaylistEntryEffect` (serverseitige Reihenfolge). |
| `UpdatePlaybackFailed` (Z. 3977) | private | Fehler-Rückfall (Playlist → `AdvancePlaylistEntry`, sonst `AdvanceToNextEpisode` mit `PlaybackUnavailableError`). |
| `UpdatePlayerClosed` (Z. 4011) | private | Abschluss-Fortschritt beim Seitenwechsel + Player-Reset; anwendergesteuert. |
| `MarkEpisodeWatchedInDetails` (Z. 3938) | private | Überträgt `IsWatched` des Players auf geladene Episoden-Details. |

Relevante Nebenbedingung: `AdvanceToNextEpisode` setzt `Position`/`Duration` des neuen Mediums auf `TimeSpan.Zero` — ein verspätetes/nachgezogenes `MediaEnded` des alten Mediums trifft heute den bereits gewechselten State und würde die Folge-Episode fälschlich als gesehen markieren (Position 0, Dauer 0 → ohne Prüfung Abschluss der neuen Episode!).

## `ShellAppEffectHandler` (Verdrahtung)

Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` — führt `ReportPlaybackProgressEffect`, `AdvancePlaylistEntryEffect`, `ResolvePlaybackSourceEffect` u. a. aus (Contract-Pin in `PlayerContractTests.EffectHandler_HandlesPlayerEffects`). Keine Änderung nötig.
