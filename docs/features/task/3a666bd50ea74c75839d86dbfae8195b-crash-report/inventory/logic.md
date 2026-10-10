# Logikklassen — Wiedergabe-Pfad

## `PlayerPage`

Datei: `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs` (View:
`PlayerPage.xaml`, Shell-Route `player`, transient via DI)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `PlayerPage(MvuHost, IPictureInPictureService)` | public | `InitializeComponent`, `PipButton`-Sichtbarkeit (iOS `false`), Windows-Tap-Geste (`#if WINDOWS`), `UpdateUi(mvuHost.Model)` — no-op im Konstruktor (`appliedLocation == null`). |
| `OnAppearing` | protected override | `subscribed = true`, `ModelChanged -=/+=` (Re-Abonnement), `_ = DispatchAsync(PlayerPageOpenedMessage)`, `UpdateUi`, `StartProgressTimer`. |
| `OnDisappearing` | protected override | `subscribed = false`, `ModelChanged -=`, `timerRunning = false`, `_ = pictureInPicture.RestoreAsync()`, `_ = DispatchAsync(PlayerClosedMessage)` — erst abmelden, dann schließen. |
| `StartProgressTimer` | private | `Dispatcher.StartTimer(5 s)` → `ReportPosition` solange `Playing` (UI-Thread-Callback). |
| `ReportPosition` | private | Liest `VideoElement.CurrentState`/`Position`/`Duration` (verwaltete Properties), dispatcht `PlaybackProgressMessage`. |
| `OnModelChanged` | private | `MainThread.BeginInvokeOnMainThread(() => { if (subscribed) UpdateUi(model); })` — marshalt Modelländerungen vom Dispatch-Thread. |
| `UpdateUi` | private | Labels/Overlays; bei geänderter `Source.Location`: `VideoElement.Stop()` + `Source = null` (bei `null`) bzw. `MediaSource.FromFile`/`FromUri` — **ohne try/catch**, läuft auf UI-Thread. |
| `OnMediaOpened` | private | Einmalig (`resumeApplied`): bei `ResumePosition > 0` → `BeginInvokeOnMainThread` → `_ = VideoElement.SeekTo(position)` (Guard `subscribed`). |
| `OnMediaEnded` | private | `_ = DispatchAsync(PlaybackEndedMessage)` (Guard `subscribed`; Event-Thread beliebig). |
| `OnMediaFailed` | private | `_ = DispatchAsync(PlaybackFailedMessage)` (Guard `subscribed`). |
| `OnMediaStateChanged` | private | `BeginInvokeOnMainThread` → `ReportPosition` (Guard `subscribed`) — Positions-/Zustandslesezugriff gemarshalt. |
| `OnPipClicked` | private | `_ = DispatchAsync(PictureInPictureRequestedMessage)`. |
| `OnPlayerTapped` | private | `#if WINDOWS`-Workaround: `TransportControls.Show()`. |

Abonnierte Events: `mvuHost.ModelChanged` (nur im Sichtbarkeitsfenster),
`MediaElement`-Events `MediaOpened`/`MediaEnded`/`MediaFailed`/`StateChanged`
(XAML), `PipButton.Clicked`, `Dispatcher`-Timer.

Publizierte Messages: `PlayerPageOpenedMessage`,
`PlaybackProgressMessage`, `PlaybackEndedMessage`,
`PlaybackFailedMessage`, `PictureInPictureRequestedMessage`,
`PlayerClosedMessage`.

Felder: `appliedLocation` (Source-Dedup), `resumeApplied`/`subscribed`
(`volatile` — Toolkit-Events können threadfremd feuern), `timerRunning`.

## `ShellAppEffectHandler` (Wiedergabe-relevante Teile)

Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` —
implementiert `IAppEffectHandler` (Singleton, `MauiProgram.cs` :54)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `HandleAsync` | public | Switch über alle `AppEffect`s; Player-Cases :196–207 (`NavigateToPlayer` → `InvokeOnMainThreadAsync`; `ResolvePlaybackSource`; `ReportPlaybackProgress`; `TogglePictureInPicture`). |
| `NavigateToPlayerAsync` | private | `Shell.Current.GoToAsync("player")`, try/catch → Log (:235–250). |
| `ResolvePlaybackSourceAsync` | private | `PlaybackSourceService.ResolveAsync` mit `allowServerStream = !Model.IsOffline`; Fehler/`null` → `PlaybackUnavailableError`; dispatcht `PlaybackSourceResolvedMessage` (:252–278). |
| `ReportPlaybackProgressAsync` | private | `ProgressReportService.ReportProgressAsync`; Fehler nur geloggt (:280–294). |
| `TogglePictureInPictureAsync` | private | `IPictureInPictureService.ToggleAsync` auf MainThread; dispatcht `PictureInPictureChangedMessage(isActive)` — auf iOS liefert der Dienst immer `false` (:296–310). |
| `DispatchMessageAsync` | private | `MainThread.InvokeOnMainThreadAsync(() => host.DispatchAsync(message))` mit try/catch (:859–870) — Modell-Updates aus Effekten laufen auf dem UI-Thread. |

## `AppUpdate` (Wiedergabe-Übergänge)

Datei: `VideoPlayer.Mvu/AppUpdate.cs` (reine statische Update-Funktion)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Update` | public static | Zentrales Switch; Player-Messages :150–158. |
| `UpdatePlayRequested` | private static | Neuer `PlayerState` + `[NavigateToPlayerEffect, ResolvePlaybackSourceEffect]`; nur `movie`/`tvshowepisode` mit `Id > 0` (:1923–1945). |
| `NextEpisodeQueue` | private static | `UpcomingEpisodes` aus `MediaDetails.Details.Seasons` (staffelübergreifend sortiert, alle nach `mediaId`) (:1956–1978). |
| `UpdatePlayerPageOpened` | private static | Setzt `Attempted`, falls noch nicht (:1980–1986). |
| `UpdatePlaybackSourceResolved` | private static | Stale-Guard (Identität); Erfolg → `Source`/`IsResolving=false`; Fehler → `AdvanceToNextEpisode` (:1988–2017). |
| `AdvanceToNextEpisode` | private static | Fehlerzustand ohne Folge-Episode; sonst Reset + `ResolvePlaybackSourceEffect("tvshowepisode", next)` (:2030–2072). |
| `UpdatePlaybackProgress` | private static | Position/Dauer/Flags; Fortschrittsmeldung (>0,5 s Änderung); `IsWatched` im Fenster `WatchedEndWindowSeconds`; Auto-Fortsetzung `AutoResumeEnabled && IsPlaying && remaining <= AutoResumeRemainingSeconds`; ggf. `DeleteWatchedDownloadEffect` (:2087–2152). |
| `UpdatePlaybackEnded` | private static | `IsWatched`, Abschluss-`ReportPlaybackProgressEffect` (Endposition), ggf. Auto-Fortsetzung (:2154–2202). |
| `MarkEpisodeWatchedInDetails` | private static | `WatchedAt`-Markierung der Episode in den geladenen Details (:2217–2254). |
| `UpdatePlaybackFailed` | private static | `AdvanceToNextEpisode(model, player, PlaybackUnavailableError)` (:2256–2267). |
| `UpdatePictureInPictureRequested/Changed` | private static | `TogglePictureInPictureEffect` / `IsPictureInPicture`-Flag (:2269–2282). |
| `UpdatePlayerClosed` | private static | `TogglePictureInPictureEffect` (falls PiP), Abschluss-`ReportPlaybackProgressEffect` (bei Position > 0), `Player = Empty` (:2284–2306). |

Konstante: `PlaybackUnavailableError` (:75) — einzige Fehlertextquelle
des Players.

## `PlaybackSourceService`

Datei: `VideoPlayer.Mvu/PlaybackSourceService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `ResolveAsync(mediaType, mediaId, ct, allowServerStream)` | public | Guard `movie`/`tvshowepisode` + `id > 0`; Fortsetzungsposition → lokale Datei → Stream-URL; liefert `PlaybackSource?` (:78–130). |
| `ResolveResumePositionAsync` | private | `GetContinueWatchingAsync` vom Server; `ApiException`/`HttpRequestException`/`TaskCanceledException` → Cache-Fallback (:141–166). |
| `ReadCachedResumePositionAsync` | private | `cached_lists[continue_watching]` → JSON → `FindResumePosition`; Fehler → `null` (:168–189). |
| `FindResumePosition` | private static | Erster Eintrag mit passendem Typ+Id und `PositionSeconds > 0` → `TimeSpan` (:191–201). |
| `ResolveLocalFileAsync` | private | `download_manifest`-Kandidaten (`VideoId == id`), Filter `completed` + `FilePath` + `fileExists`; Fehler → `null` (:210–233). |

## `ProgressReportService`

Datei: `VideoPlayer.Mvu/ProgressReportService.cs` —
`ReportProgressAsync` (:44–86): online →
`client.ReportPlaybackProgressAsync`; permanente 4xx verwerfen,
überholbare Fehler/Offline → `PendingProgressSyncService.EnqueueAsync`.
`IsPermanentClientError` (:94–98). Wird ausschließlich aus
`ReportPlaybackProgressEffect` aufgerufen.

## `MvuHost`

Datei: `VideoPlayer.Mvu/MvuHost.cs` — `DispatchAsync` (:44–68):
semaphorgesicherter Übergang (`gate`), danach `PropertyChanged` +
`ModelChanged` auf dem Dispatch-Thread (nicht gemarshalt!), dann
sequenzielle `HandleAsync` der Effekte. Ruft keine try/catch ab —
Ausnahmen aus Effekt-Handlern propagieren an die Dispatch-Task
(die die Seiten per `_ =` verwerfen).

## `AppShell` (Routing)

Datei: `VideoPlayer.Maui/AppShell.xaml.cs` — `Routing.RegisterRoute("player",
typeof(Pages.PlayerPage))` :77; `OnShellNavigated` synchronisiert
`NavigatedMessage` in das Model :100–108 (PlayerRoute hat keinen
`AppPage`-Eintrag — `AppRoutes.TryGetPage` greift für `player` nicht,
kein Navigated-Dispatch).

## `MediaDetailsPage` (Einstiegspunkt)

Datei: `VideoPlayer.Maui/Pages/MediaDetailsPage.xaml.cs` —
`OnPlayClicked` :309–322, `OnEpisodePlayClicked` :324–336 (siehe
[playback-start-path.md](playback-start-path.md) §1). `ModelChanged`
im Sichtbarkeits-Lebenszyklus (:87–105).

## iOS-Plattform-Einstieg

- `Platforms/iOS/Program.cs` :10–15 — `UIApplication.Main(args, null,
  typeof(AppDelegate))`, kein Exception-Handler.
- `Platforms/iOS/AppDelegate.cs` :6–11 — `MauiUIApplicationDelegate`
  → `MauiProgram.CreateMauiApp()`.
- `MauiProgram.cs` :33 — `UseMauiCommunityToolkitMediaElement(true)`
  (`isAndroidForegroundServiceEnabled`), :102–103 — DI
  `PlaybackSourceService` (Singleton) + `IPictureInPictureService`,
  :126 — `PlayerPage` (Transient).

## `PictureInPictureService`

Datei: `VideoPlayer.Maui/Services/PictureInPictureService.cs` —
`IsToggleSupported` nur `#if WINDOWS`; `ToggleAsync`/`RestoreAsync`
sind auf iOS No-ops (:27–45).

## Externe Logik (Toolkit, nicht im Repo)

`CommunityToolkit.Maui.MediaElement` 10.0.0 auf iOS —
`MediaManager.macios.cs` / `MediaElementHandler.shared.cs` /
`MediaElement.shared.cs` / `MauiMediaElement.macios.cs`: siehe
[playback-start-path.md](playback-start-path.md) §4–§6 für die
crash-relevanten Methoden (`CreatePlatformView`,
`PlatformUpdateSource`, `PlatformSeek`, `MapSeekRequested` async void,
Observer).
