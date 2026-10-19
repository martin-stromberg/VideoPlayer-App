# Logik

## `AppUpdate` (MVU-Kern)
Datei: `VideoPlayer.Mvu/AppUpdate.cs` — reine statische Update-Funktionen; liefert `AppTransition` (Model + Effekte).

| Methode / Konstante | Sichtbarkeit | Kurzbeschreibung |
|---------------------|-------------|------------------|
| `PlaybackEndedToleranceSeconds` = 300 | `private const` | Toleranzfenster der Ende-Plausibilität (Z. 77–88); Kommentar verweist auf die Auto-Fortsetzung „greift bereits" im Fenster |
| `EndWindowRequiredTicks` = 2 | `private const` | Erforderliche plausible Fenstertakte für „angesehen"/Fortsetzung (Z. 90–97) |
| `UpdatePlaybackProgress` | `private static` | Fortschrittstakt: `IsActive`- und `MediaId`-Guard (Z. 3815–3827), Position/Dauer/Zustand übernehmen, `ReportPlaybackProgressEffect` bei >0,5 s Änderung (Z. 3837–3848), danach `EvaluateEndWindow` (Z. 3812–3851; Doc Z. 3791–3808 beschreibt den Fortsetzungs-Vorlauf) |
| `EvaluateEndWindow` | `private static` | Endfenster-Auswertung (Z. 3876–3961): Streak-Aufbau `EndWindowStreak` bei plausiblem Takt im `Settings.EndWindow`-Fenster (Z. 3892–3899, Fenster Z. 3883); „angesehen"-Markierung + optionales `DeleteWatchedDownloadEffect` ab `EndWindowRequiredTicks` (Z. 3903–3914); **Playlist-Advance** `remaining <= AutoResumeRemainingSeconds` (Z. 3924–3937) und **Serien-Advance** (Z. 3939–3951) — beide Advance-Zweige entfallen; `Duration <= 0` → Streak-Reset (Z. 3953–3956); Doc Z. 3853–3868 |
| `UpdatePlaybackEnded` | `private static` | Ende-Pfad (Z. 3981–4045): `IsActive` + `IsPlaybackEndPlausible`-Gate, dann `IsWatched`, Endpositions-Meldung, `DeleteWatchedDownloadEffect` (Episode), Auto-Fortsetzung bei `AutoResumeEnabled` über `AdvancePlaylistEntry` (Playlist) bzw. `AdvanceToNextEpisode` (Serie) — **einziger verbleibender Wechselpfad** |
| `IsPlaybackEndPlausible` | `private static` | Ende-Ereignis nur bei `Duration > 0`, `Position >= 0`, `Duration - Position <= 300 s` (Z. 4058–4064) |
| `IsProgressTickPlausible` | `private static` | Takt plausibel bei `Duration > 0`, `Position >= 0`, realem Fortschritt (`Position > player.Position`), nicht geschrumpfter Dauer (Z. 4078–4084) |
| `AdvanceToNextEpisode` | `private static` | Wechsel zur nächsten `UpcomingEpisodes`-Episode: State-Reset inkl. `EndWindowStreak = 0` (Z. 3783), `ResolvePlaybackSourceEffect`; ohne Folge-Episode Fehlerzustand (Z. 3746–3789). Aufgerufen von `UpdatePlaybackEnded`, `UpdatePlaybackSourceResolved` (Fehler-Rückfall Z. 3732) und `UpdatePlaybackFailed` |
| `AdvancePlaylistEntry` | `private static` | `AdvancePlaylistEntryEffect(PlaylistId, PlaylistEntryId)` + `IsResolving` (Z. 2623–2638). Aufgerufen von `UpdatePlaybackEnded` (Z. 4026), Fehler-Rückfällen (Z. 3729) und dem zu entfernenden Takt-Vorlauf (Z. 3932) |
| `UpdatePlaylistNavigationCompleted` | `private static` | Serverseitiger Eintragswechsel: setzt u. a. `EndWindowStreak = 0` (Z. 2606) — bleibt |
| `MarkEpisodeWatchedInDetails` | `private static` | Überträgt `IsWatched` des Players als `WatchedAt` auf die geladene Episodenliste (Z. 4099 ff.) — bleibt |

Effekt-Typen im Bereich: `ReportPlaybackProgressEffect`, `DeleteWatchedDownloadEffect`, `ResolvePlaybackSourceEffect`, `AdvancePlaylistEntryEffect`.

## `AppSettingsService`
Datei: `VideoPlayer.Storage/AppSettingsService.cs` (Z. 8–92)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `AutoResumeRemainingSecondsKey` | `public const string` | Ablageschlüssel `"AutoResumeRemainingSeconds"` (Z. 19–20) — entfällt |
| `AppSettingsService(store, profile)` | `public` | Konstruktor mit `ISettingsStore` + `AppProfile` (Z. 46–50) |
| `Load()` | `public AppSettings` | Liest alle Schlüssel mit Defaults, liefert `Normalize()`d Instanz; `AutoResumeRemainingSeconds`-Zeile Z. 62 entfällt (Z. 54–70) |
| `Save(settings)` | `public void` | Normalisiert und schreibt alle Schlüssel; `AutoResumeRemainingSeconds`-Zeile Z. 82 entfällt (Z. 74–89) |
| `Key(key)` | `private string` | `profile.PrefixKey(key)` (Z. 91) |

## `PlayerPage` (App-Rand)
Datei: `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`

| Methode / Feld | Sichtbarkeit | Kurzbeschreibung |
|----------------|-------------|------------------|
| `appliedMediaId` | `private long` | Zuletzt erfolgreich am `MediaElement` angewendete Medien-Id (Zuweisung erst nach `VideoElement.Source =` in `UpdateUi`; gepinnt in `PlayerContractTests.PlayerPage_AppliedMediaId_AssignedAfterSourceSet`) — **bleibt** |
| `ReportPosition` | `private void` | Timer-Takt (Z. 172–216): liest `CurrentState`/`Position`/`Duration`, schreibt `LogDebug`-Fensterdiagnose bei `duration - position <= model.Settings.EndWindow` (Z. 189–200 — Fensterquelle anpassen) und dispatcht `PlaybackProgressMessage` mit `appliedMediaId` (Z. 202–207) |
| `OnMediaEnded` | `private void` | `MediaEnded`-Übersetzer: Log + `PlaybackEndedMessage` unter `subscribed`-Guard (Z. 455–462) — **bleibt, wird zum alleinigen Fortsetzungs-Auslöser** |
| `OnMediaFailed` | `private void` | `PlaybackFailedMessage` (Fehler-Rückfall, unverändert; Z. 464–471) |
| `UpdateUi` | `private void` | Wendet `Player.Source` am Element an, setzt `appliedMediaId` nach erfolgreichem Quellenwechsel |

## `SettingsPage` (App-Rand)
Datei: `VideoPlayer.Maui/Pages/SettingsPage.xaml` + `SettingsPage.xaml.cs`

| Element / Methode | Sichtbarkeit | Kurzbeschreibung |
|-------------------|-------------|------------------|
| `AutoResumeSwitch` | XAML Z. 109–116 | Bool-Schalter „Auto-Fortsetzung" — **bleibt** |
| `AutoResumeRemainingEntry` | XAML Z. 118–134 | Numerisches Entry „Zeitfenster Auto-Fortsetzung (Sekunden Restlaufzeit)" inkl. umschließender `BoxView`-Trennlinien — **entfällt** |
| `WatchedEndWindowEntry` | XAML Z. 133–146 | Entry „Als angesehen ab … Sekunden vor Ende" — **bleibt** |
| `WireNumeric(entry, handler)` | `private static` | Verdrahtet `Completed` + `Unfocused` auf denselben Handler (`.xaml.cs` Z. 42–46); Aufruf für `AutoResumeRemainingEntry` Z. 38 entfällt |
| `PopulateFromModel` | `private void` | Spiegelt `Model.Settings` in die Controls; `SetTextIfNotFocused(AutoResumeRemainingEntry, …)` Z. 81 entfällt (Z. 66–94) |
| `SetTextIfNotFocused` | `private static` | Schreibt Wert nur bei unfokussiertem Entry (Z. 96–102) |
| `OnAutoResumeRemainingCompleted` | `private void` | `SaveNumeric(AutoResumeRemainingEntry, …)` (Z. 144–147) — entfällt |
| `OnAutoResumeToggled` | `private void` | `Save(s => s with { AutoResumeEnabled = e.Value })` (Z. 124–127) — **bleibt** |
| `SaveNumeric` / `Save` / `DispatchSaveAsync` | `private` | Zahlen-Validierung mit `InvalidInputLabel` und `SaveSettingsRequestedMessage`-Dispatch (Z. 177–217) — bleiben für die verbleibenden Entries |
