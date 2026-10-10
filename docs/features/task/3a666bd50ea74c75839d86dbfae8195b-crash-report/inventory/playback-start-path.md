# Codepfad beim Start eines Videos (iOS-Fokus)

Bestandsaufnahme des kompletten Wiedergabe-Startpfads vom „Abspielen"-Klick
bis zur AVPlayer-Initialisierung, mit den am Code verifizierten
Crash-Verdächtigen für `EXC_CRASH (SIGABRT)` auf Thread 0.

## 1. Einstiegspunkte (Dispatch von `PlayRequestedMessage`)

Zwei UI-Einstiege, beide auf `MediaDetailsPage` (Datei:
`VideoPlayer.Maui/Pages/MediaDetailsPage.xaml.cs`):

| Stelle | Zeile | Auslöser |
|--------|-------|----------|
| `OnPlayClicked` | :309–322 | „Abspielen"-Schaltfläche im Kopfbereich; Guard `details is null \|\| !details.IsPlayable`; dispatcht `PlayRequestedMessage(details.MediaType, details.MediaId, details.HighlightedEpisodeTitle ?? details.Title)` |
| `OnEpisodePlayClicked` | :324–336 | ▶-Schaltung je Episoden-Eintrag; Guard `IsPlaceholder`; dispatcht `PlayRequestedMessage("tvshowepisode", episode.Id, episode.Title)` |

Beide rufen `_ = mvuHost.DispatchAsync(...)` auf dem UI-Thread — die
verworfene Task kann eine Dispatch-Ausnahme unbeobachtet verschlucken
(für den Crash weniger relevant: `DispatchAsync` fängt selbst nichts ab,
aber Effekt-Methoden sind try/catch-gesichert).

## 2. MVU-Übergang (`AppUpdate.UpdatePlayRequested`)

Datei: `VideoPlayer.Mvu/AppUpdate.cs` :1923–1945

- Normalisiert den Medientyp (`MediaTypeNames.Normalize`); nur
  `movie`/`tvshowepisode` mit `MediaId > 0` werden angenommen.
- Setzt `Player = new PlayerState { MediaType, MediaId, Title,
  IsResolving = true, Attempted = true, UpcomingEpisodes =
  NextEpisodeQueue(model, mediaId) }` — die Fortsetzungsliste kommt aus
  dem geladenen Serienkontext (`MediaDetails.Details.Seasons`,
  staffelübergreifend sortiert, `NextEpisodeQueue` :1956–1978).
- Effekte: `[NavigateToPlayerEffect, ResolvePlaybackSourceEffect]` —
  nacheinander vom `MvuHost` ausgeführt (`MvuHost.DispatchAsync`
  :64–67, sequentiell `await`).

## 3. Effekt-Ausführung (`ShellAppEffectHandler`)

Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`

### 3.1 `NavigateToPlayerEffect` → `NavigateToPlayerAsync` (:235–250)

- Läuft gemarshalt: `HandleAsync` :196–198 →
  `MainThread.InvokeOnMainThreadAsync(NavigateToPlayerAsync)`.
- `Shell.Current.GoToAsync("player")` — die Route ist in
  `AppShell.xaml.cs` :77 per `Routing.RegisterRoute("player",
  typeof(Pages.PlayerPage))` registriert; die Seite wird als Transient
  über DI erzeugt (`MauiProgram.cs` :126) und auf den
  Navigationsstack geschoben (kein FlyoutItem).
- `try/catch` um `GoToAsync` — Navigationsfehler werden nur geloggt.

### 3.2 `ResolvePlaybackSourceEffect` → `ResolvePlaybackSourceAsync` (:252–278)

- `allowServerStream = !Model.IsOffline` — im Offline-Zustand nur
  lokale Dateien.
- `PlaybackSourceService.ResolveAsync(type, id, ct, allowServerStream)`
  → Ergebnis `PlaybackSource?`; `null`/Ausnahme →
  `error = AppUpdate.PlaybackUnavailableError`.
- Dispatcht `PlaybackSourceResolvedMessage` über
  `DispatchMessageAsync` (:859–870), das **immer** über
  `MainThread.InvokeOnMainThreadAsync(() => host.DispatchAsync(...))`
  dispatcht — Modellwechsel aus Effekten laufen also auf dem UI-Thread.

### 3.3 Quellauflösung (`PlaybackSourceService.ResolveAsync`)

Datei: `VideoPlayer.Mvu/PlaybackSourceService.cs` :78–130

1. Fortsetzungsposition: online `GetContinueWatchingAsync`, Fallback
   `cached_lists[continue_watching]` (:141–189); Treffer
   `PositionSeconds > 0` → `TimeSpan.FromSeconds`.
2. Lokale Datei aus `download_manifest` (:210–233): `State ==
   "completed"` + `File.Exists(FilePath)` → `PlaybackSource {
   Kind = LocalFile, Location = FilePath }`. Dateipfade:
   `Path.Combine(layout.VideosDirectory, "{type}_{id}.mp4")`
   (`DownloadService.FilePathFor` :794–795; `VideosDirectory` unter
   `AppDataDirectory/nobackup` auf iOS, `AppStorageLayoutProvider`
   :59–72).
3. Sonst Server-Stream: `{base}/api/items/{type}/{id}/stream` +
   `?access_token={Uri.EscapeDataString(token)}` → `PlaybackSource {
   Kind = ServerStream, Location = url }` (:111–129). URL ist
   wohlgeformt, Token escaped.

Alle Fehlerpfade liefern `null` statt Ausnahmen (try/catch + Guards);
die `Location` ist damit immer ein geparster String — entweder absoluter
Dateipfad oder http(s)-URL.

## 4. `PlayerPage`-Erzeugung und MediaElement-Init (iOS)

Datei: `VideoPlayer.Maui/Pages/PlayerPage.xaml(.cs)`

- `InitializeComponent` erzeugt `mct:MediaElement` (XAML :13–20) mit
  `ShouldAutoPlay="True"`, `ShouldShowPlaybackControls="True"`,
  `ShouldKeepScreenOn="True"` und verdrahteten Events
  `MediaOpened`/`MediaEnded`/`MediaFailed`/`StateChanged`.
- `UseMauiCommunityToolkitMediaElement(true)` (`MauiProgram.cs` :33) —
  der bool-Parameter ist `isAndroidForegroundServiceEnabled`
  (Android-only; API-Doku des Pakets). Auf iOS registriert der Aufruf
  nur den `MediaElementHandler`.
- Beim Anhängen der Page erzeugt der Handler auf iOS den
  `MediaManager` (`CommunityToolkit.Maui.MediaElement` 10.0.0):
  `CreatePlatformView` legt `AVPlayer` + `AVPlayerViewController` an,
  ruft `UIApplication.SharedApplication.BeginReceivingRemoteControlEvents()`,
  `AVAudioSession.SharedInstance().SetCategory(Playback)` und
  `SetActive(true)` und registriert KVO-/Notification-Observer
  (`status`, `timeControlStatus`, `rate`, `muted`, `volume`,
  `DidPlayToEndTime`, `ItemFailedToPlayToEndTime`,
  `PlaybackStalled`, `NewErrorLogEntry`). Fehler bei
  `SetCategory`/`SetActive` werden vom Toolkit ignoriert (Rückgabewerte
  unbeachtet).
- Konstruktor (:37–55): `BindingContext = mvuHost`,
  `PipButton.IsVisible = pictureInPicture.IsToggleSupported` (auf iOS
  `false` — `PictureInPictureService.IsToggleSupported` :16–21),
  `UpdateUi(mvuHost.Model)` — im Konstruktor ist `location == null ==
  appliedLocation` → Frühreturn, kein Elementzugriff.

## 5. Ablauf nach `PlaybackSourceResolvedMessage`

1. `UpdatePlaybackSourceResolved` (:1988–2017): bei Erfolg
   `Player.Source = source`, `IsResolving = false`; bei Fehlschlag
   `AdvanceToNextEpisode` (Fehler + Rückfall, :2030–2072).
2. `ModelChanged` → `PlayerPage.OnModelChanged` (:127–139) →
   `MainThread.BeginInvokeOnMainThread` → `UpdateUi` (nur wenn
   `subscribed`).
3. `UpdateUi` (:141–172): bei geänderter `Location`
   `VideoElement.Source = MediaSource.FromFile(location)` (LocalFile)
   bzw. `MediaSource.FromUri(location)` (ServerStream) — Aufruf auf
   dem UI-Thread, **ohne try/catch**.
4. `MapSource` → `MediaManager.UpdateSource()` →
   `PlatformUpdateSource` (Toolkit, `MediaManager.macios.cs`):
   `CurrentStateChanged(Opening)`, dann `AVAsset.FromUrl(new
   NSUrl(uri.AbsoluteUri))` bzw. `AVAsset.FromUrl(NSUrl.CreateFileUrl(uri))`,
   `new AVPlayerItem(asset)`, `Player.ReplaceCurrentItemWithPlayerItem(PlayerItem)`,
   KVO `error`-Observer, dann **`MediaElement.MediaOpened()`** — laut
   Toolkit-Issue #3249 feuert `MediaOpened` auf iOS/macOS **vor**
   `AVPlayerItemStatus.ReadyToPlay`.
5. `OnMediaOpened` (:174–199): einmalig pro Quelle (`resumeApplied`),
   bei `ResumePosition > 0` →
   `MainThread.BeginInvokeOnMainThread(() => _ = VideoElement.SeekTo(position))`.
6. `MediaElement.SeekTo` (Toolkit `MediaElement.shared.cs`):
   `await seekToSemaphoreSlim.WaitAsync()` →
   `Handler?.Invoke(nameof(SeekRequested), args)` →
   `MediaElementHandler.MapSeekRequested` — **`async void`** (:~150
   im Toolkit) → `await MediaManager.Seek(position)` → `PlatformSeek`.

## 6. Crash-Verdächtige (am Code verifiziert, nach Plausibilität)

### 6.1 `MapSeekRequested` ist `async void` → unbehandelte `InvalidOperationException` auf Thread 0

Toolkit `MediaElementHandler.shared.cs` (10.0.0, Commit-Stand cdf9fa5c):

```csharp
public static async void MapSeekRequested(...)
{
    var positionArgs = (MediaSeekRequestedEventArgs)args;
    await (handler.MediaManager?.Seek(...) ?? Task.CompletedTask);
    ((IMediaElement)mediaElement).SeekCompletedTCS.TrySetResult();
}
```

`PlatformSeek` (Toolkit `MediaManager.macios.cs`) wirft:

- `InvalidOperationException` wenn `Player?.CurrentItem is null`;
- `InvalidOperationException` wenn `Player.Status is not AVPlayerStatus.ReadyToPlay`.

Da `MediaOpened` vor `ReadyToPlay` feuert (Issue #3249), trifft der
appseitige `SeekTo` auf die Fortsetzungsposition genau das
Status-`Unknown`-Fenster: Die `InvalidOperationException` faultet den
Task, der `await` im `async void` gibt sie frei → **unbehandelte
verwaltete Ausnahme → mono abort → `EXC_CRASH (SIGABRT)` auf dem
aufrufenden Thread (hier UI-Thread/Thread 0)**. Der appseitige `_ =`
-Discard auf `SeekTo` schützt davor nicht — die Ausnahme liegt im
`async void` des Handlers, nicht in der zurückgegebenen Task.

Auslösende Bedingung: `PlaybackSource.ResumePosition > 0`, d. h. **nur
Medien mit Weiterschauen-Stand** — konsistent mit „Absturz beim Start
eines Videos" nach >1 h Laufzeit (fortgesetztes Video). Videos ohne
Fortsetzungsposition laufen am `SeekTo` vorbei (Guard in `OnMediaOpened`
:184).

Nebenfolge: `SeekCompletedTCS` wird bei der Ausnahme nie gesetzt →
`SeekTo` hängt an `seekCompletedTaskCompletionSource.Task.WaitAsync()`
und gibt `seekToSemaphoreSlim` nie frei (Folgeproblem, nicht der
Absturz).

### 6.2 `throw` im AVPlayer-Seek-Completion-Callback

`PlatformSeek` enthält zusätzlich:

```csharp
Player.Seek(seekToTime, complete =>
{
    if (!complete) { throw new InvalidOperationException("Seek Failed"); }
    seekTaskCompletionSource.SetResult();
});
```

`complete == false` (z. B. unterbrochener Seek, Item-Wechsel) →
verwaltete Ausnahme innerhalb eines ObjC-Completion-Blocks → ebenfalls
`abort()` → SIGABRT (Callback-Thread, AVPlayer ruft typischerweise auf
main auf).

### 6.3 Hängender Seek → `seekTaskCompletionSource` ohne Treffer

Enthält `SeekableTimeRanges` die Zielposition nicht, wird `Player.Seek`
nie aufgerufen und `seekTaskCompletionSource` nie gesetzt → `WaitAsync`
hängt (kein Crash, aber fachlich relevant).

### 6.4 Unbehandelte Ausnahmen in `BeginInvokeOnMainThread`-Lambdas

Alle `BeginInvokeOnMainThread`-Callbacks der `PlayerPage` sind
try/catch-frei:

- `OnModelChanged` → `UpdateUi` (:132–138): `VideoElement.Source = …`
  → `MapSource` → `PlatformUpdateSource` ist `async ValueTask` —
  `MapSource` ruft `UpdateSource()` fire-and-forget (nicht async void) —
  Ausnahmen faulten den ValueTask unbeobachtet (vermutlich nicht
  absturzwirksam). Synchrone Würfe in `MediaSource.FromFile/FromUri`
  (z. B. `new Uri`) wären aber direkt fatal.
- `OnMediaOpened` → `SeekTo` (:191–197): siehe 6.1 — der Crash liegt
  im Toolkit-`async void`, nicht im Lambda selbst.

### 6.5 Zugriff auf losgelöstes MediaElement

- `subscribed`-Guard verhindert `UpdateUi`/`ReportPosition`/`SeekTo`
  nach `OnDisappearing` (:77, :134, :193, :203, :211, :226).
- `StartProgressTimer`/`Dispatcher.StartTimer` (:93–115) liest nur
  verwaltete `MediaElement`-Bindable-Properties (`CurrentState`,
  `Position`, `Duration`) auf dem UI-Thread — kein nativer Zugriff.
- `timerRunning = false` stoppt den Takt beim Verlassen; zwischen
  `OnDisappearing` und dem nächsten Tick kann ein letzter
  `ReportPosition`-Lauf übrig bleiben — liest nur verwaltete Werte.

### 6.6 Toolkit-Lifecycle/Dispose

`MediaManager.Dispose` (Toolkit) läuft beim Disconnect des Handlers
(Seiten-Pop) auf dem UI-Thread: `Player.Pause()`,
`ReplaceCurrentItemWithPlayerItem(null)`, Observer-Disposes,
`AVAudioSession.SetActive(false)`. Ein dazu parallel laufender
`PlatformUpdateSource`-ValueTask (`await GetVideoDimensions` →
`LoadTracksWithMediaTypeAsync` / `SetPoster`) kann auf ein bereits
disposed `PlayerViewController`/`ContentOverlayView` zugreifen —
Fehlerfault landet im verworfenen ValueTask (unbeobachtet).

### 6.7 Toolkit-Event-Threading (bereits abgesichert)

- `MediaOpened`/`MediaEnded`/`MediaFailed`/`StateChanged` können auf
  Nicht-UI-Threads feuern (NSNotification-/KVO-Callbacks). App-Seite
  marshalt native Zugriffe (`SeekTo`, `ReportPosition`) auf den
  UI-Thread — Windows-Fix `RPC_E_WRONG_THREAD`; auf iOS entsprechend
  abgesichert.
- `OnMediaEnded`/`OnMediaFailed` dispatcht `PlaybackEnded-/Failed-`
  Message direkt vom Event-Thread — `MvuHost.DispatchAsync` ist
  semaphorgesichert; `PropertyChanged`/`ModelChanged` feuern dann auf
  dem Hintergrundthread. `OnModelChanged` marshalt selbst, aber
  **XAML-Bindings** (`LoadingIndicator.IsRunning`, `x:DataType` auf
  `MvuHost`) aktualisieren auf dem auslösenden Thread —
  Hintergrund-Bindings auf iOS können UIKit-Berührungen erzeugen
  (Crash wäre dann nicht auf Thread 0 — sekundärer Verdacht).

### 6.8 ObjC-Exceptions beim AVPlayer-Aufbau

- `new NSUrl(uri.AbsoluteUri)` im Toolkit bei `UriMediaSource` — die
  App übergibt eine wohlgeformte AbsoluteUri (geringes Risiko).
- `NSUrl.CreateFileUrl(path)` bei `LocalFile` — Pfad ist
  `nobackup/videos/{type}_{id}.mp4` (ASCII, existenzgeprüft) — gering.
- `AVAudioSession.SetCategory(Playback)`/`SetActive(true)` beim
  Handler-Connect — Rückgabe ignoriert; `BeginReceivingRemoteControlEvents`
  erfordert Main Thread (erfüllt).
- PiP-Transportsteuerung: `ShowsPlaybackControls = true`; nativer
  `AVPlayerViewController`-PiP setzt `UIBackgroundModes=audio`
  (`Info.plist` :46–49) und eine aktive `AVAudioSession`-Kategorie
  Playback voraus — vom Toolkit gesetzt.

## 7. Build-Bezug des Crash-Reports

- Build 213 = Commit `a9a5a75` (`git rev-list --count` = 213, Stand
  2026-10-10 16:23 — Buildnummer = Commit-Anzahl + run_attempt − 1,
  `.github/actions/package-ios/action.yml` :152).
- `PlayerPage.xaml.cs` unterscheidet sich zwischen `a9a5a75` und HEAD
  **nicht** — die Absturz-Version enthält bereits alle
  Lebenszyklus-/Threading-Härtungen (verwaiste-Seiten-Guard,
  MainThread-Marshal von `SeekTo`/Positionszugriff). Der iOS-Absturz
  liegt damit **nicht** in den bereits behobenen Windows-/Leck-Pfaden.
- Keine globalen Crash-Hooks: weder `AppDomain.CurrentDomain.
  UnhandledException`, `TaskScheduler.UnobservedTaskException` noch
  `UIApplication`/`NSSetUncaughtExceptionHandler` werden in
  `Program.cs`, `AppDelegate.cs` oder `App.xaml.cs` registriert — eine
  unbehandelte Ausnahme geht direkt in den SIGABRT-Abort der Runtime.
