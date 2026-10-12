# Logik — Overlay-Sichtbarkeit, PiP-Umschaltung und Effekt-Verdrahtung

## `PlayerPage` (Code-Behind)

Datei: `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs` (431 Zeilen,
vollständig gelesen). `public partial class PlayerPage : ContentPage`,
transient registriert (`MauiProgram.cs:149` —
`AddTransient<Pages.PlayerPage>()`), Shell-Route `player`
(`AppShell.xaml.cs:82`).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `PlayerPage(MvuHost, IPictureInPictureService)` | public | `InitializeComponent`, `BindingContext = mvuHost`, `PipButton.IsVisible = pictureInPicture.IsToggleSupported` (Zeile 53); unter `#if WINDOWS` Tap-Gesture auf `PlayerRoot` → `OnPlayerTapped` (Zeilen 54–63); initial `UpdateUi(mvuHost.Model)` |
| `OnAppearing` | protected override | `subscribed = true`, `mvuHost.ModelChanged += OnModelChanged`, dispatcht `PlayerPageOpenedMessage`, `UpdateUi`, `StartProgressTimer` |
| `OnDisappearing` | protected override | `subscribed = false`, `ModelChanged`-Abmeldung, `pictureInPicture.RestoreAsync()`, dispatcht `PlayerClosedMessage` |
| `StartProgressTimer` | private | `Dispatcher.StartTimer` alle 5 s → `ReportPosition` bei `Playing` |
| `ReportPosition` | private | dispatcht `PlaybackProgressMessage(Position, Duration, IsPlaying, IsBuffering)`; try/catch-isoliert |
| `OnModelChanged` | private | `MainThread.BeginInvokeOnMainThread` → `UpdateUi` bei `subscribed` |
| `UpdateUi(AppModel)` | private | **Sichtbarkeitssteuerung, siehe unten**; setzt/leert `VideoElement.Source` (`MediaSource.FromFile` bzw. `FromUri` je `PlaybackSourceKind`) |
| `IsValidSourceLocation` | private static | Frühvalidierung der Quell-Adresse (`ServerStream`: absolute http/https-URL; `LocalFile`: nicht leer) |
| `FailPlaybackStart` | private | setzt `sourceApplyFailed = true`, dispatcht `PlaybackFailedMessage` |
| `OnMediaOpened` | private | merkt `ResumePosition` als `pendingResumePosition` (kein Sofort-Seek — iOS feuert `MediaOpened` vor `ReadyToPlay`, Toolkit-Issue #3249) |
| `TryApplyPendingResumeSeek` | private | Gate auf `Playing`/`Paused` → `ApplyResumeSeekAsync` |
| `ApplyResumeSeekAsync` | private | `VideoElement.SeekTo(position)` mit 10-s-`ResumeSeekTimeout` |
| `OnMediaEnded` | private | dispatcht `PlaybackEndedMessage` |
| `OnMediaFailed` | private | dispatcht `PlaybackFailedMessage` |
| `OnMediaStateChanged` | private | gemarshalt → `ReportPosition` + `TryApplyPendingResumeSeek` |
| `OnPipClicked` | private | dispatcht `PictureInPictureRequestedMessage` |
| `OnPreviousClicked` | private | dispatcht `PlaylistPreviousRequestedMessage` |
| `OnNextClicked` | private | dispatcht `PlaylistNextRequestedMessage` |
| `OnPlayerTapped` | private | `#if WINDOWS`-Rumpf: `MediaPlayerElement.TransportControls?.Show()` |

Abonnierte Events: `mvuHost.ModelChanged` (nur zwischen
`OnAppearing`/`OnDisappearing`), `MediaElement`-Events
(`MediaOpened`/`MediaEnded`/`MediaFailed`/`StateChanged`),
`Clicked` der drei Buttons, Windows-only `TapGestureRecognizer.Tapped`.

Publizierte MVU-Nachrichten: `PlayerPageOpenedMessage`,
`PlaybackProgressMessage`, `PlaybackEndedMessage`,
`PlaybackFailedMessage`, `PictureInPictureRequestedMessage`,
`PlaylistPreviousRequestedMessage`, `PlaylistNextRequestedMessage`,
`PlayerClosedMessage`.

### `UpdateUi` — relevante Sichtbarkeitsregeln (Zeilen 171–188)

```csharp
TitleLabel.Text = player.Title ?? string.Empty;
PlaylistContextLabel.Text = player.PlaylistLabel;
PlaylistContextLabel.IsVisible = player.IsPlaylistPlayback;
WatchedBadge.IsVisible = player.IsWatched;
ErrorPanel.IsVisible = player.HasError && !player.IsPictureInPicture;
ErrorLabel.Text = player.Error;
OverlayChrome.IsVisible = !player.IsPictureInPicture;
PlaylistNavigationBar.IsVisible = player.IsPlaylistPlayback && !player.IsPictureInPicture;
PreviousButton.IsEnabled = player.CanNavigatePrevious;
NextButton.IsEnabled = player.CanNavigateNext;
LoadingIndicator.IsVisible = player.IsResolving || player.IsBuffering;
```

- `OverlayChrome` wird im Mini-Player komplett ausgeblendet (nur
  Windows-PiP-Pfad; unter iOS steuert der `AVPlayerViewController` PiP
  selbst, `IsPictureInPicture` bleibt dort ohne App-Schalter).
- `PlaylistNavigationBar` nur bei Playlist-Wiedergabe und nicht im
  Mini-Player.
- `PipButton` wird in `UpdateUi` nicht angefasst — Sichtbarkeit steht
  seit dem Konstruktor (`IsToggleSupported`).

## `AppUpdate` (MVU-Reducer, Ausschnitt)

Datei: `VideoPlayer.Mvu/AppUpdate.cs` — nur die anforderungsrelevanten
Handler:

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `UpdatePictureInPictureRequested` (≈3979) | private static | bei aktivem Player → `AppTransition` mit `TogglePictureInPictureEffect` |
| `UpdatePictureInPictureChanged` (≈3986) | private static | setzt `Player.IsPictureInPicture = message.IsActive` |
| `UpdatePlayerClosed` (≈3995) | private static | bei `IsPictureInPicture` erst `TogglePictureInPictureEffect` (Overlay verlassen), dann Abschluss-Fortschritt; `Player = PlayerState.Empty` |

Dispatch-Tabelle Zeilen 273–274:
`PictureInPictureRequestedMessage` → `UpdatePictureInPictureRequested`,
`PictureInPictureChangedMessage` → `UpdatePictureInPictureChanged`.
Weitere Resets von `IsPictureInPicture = false` bei
Zeilen 2560 und 3736 (Logout-/Reset-Pfade).

## `ShellAppEffectHandler` (App-Rand-Effekte, Ausschnitt)

Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`:

- Switch-Zweig `case TogglePictureInPictureEffect` (Zeile 295) →
  `TogglePictureInPictureAsync()`.
- `TogglePictureInPictureAsync` (≈Zeile 485): ruft gemarshalt
  `IPictureInPictureService.ToggleAsync()` (via DI), fängt Fehler
  (`LogError`), dispatcht anschließend
  `PictureInPictureChangedMessage(isActive)` zurück in den MVU-Zyklus —
  die Meldung steuert `IsPictureInPicture` und damit die
  Overlay-Ausblendung in `UpdateUi`.

Auf iOS ist dieser Pfad ohne Funktion: `IsToggleSupported` ist `false`,
der `PipButton` unsichtbar, `ToggleAsync()` ist ein No-Op
(Rückgabe `isActive = false`).

## `PictureInPictureService`

Datei: `VideoPlayer.Maui/Services/PictureInPictureService.cs`
(70 Zeilen). `public sealed class PictureInPictureService :
IPictureInPictureService`.

| Methode/Eigenschaft | Sichtbarkeit | Kurzbeschreibung |
|---------------------|-------------|------------------|
| `IsToggleSupported` | public | `#if WINDOWS` → `true`, sonst `false` |
| `IsActive` | public | interner `isActive`-Flag |
| `ToggleAsync` | public | nur `#if WINDOWS`: `SetCompactOverlay(!isActive)`; sonst No-Op |
| `RestoreAsync` | public | nur `#if WINDOWS`: bei aktivem Overlay `SetCompactOverlay(false)`; sonst No-Op |
| `SetCompactOverlay(bool)` | private (`#if WINDOWS`) | `Application.Current.Windows[0]` → `AppWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay \| Default)`; Fehler nur geloggt |

Unter iOS ist der Dienst vollständig inaktiv — iOS-PiP kommt aus der
nativen Transportsteuerung des `AVPlayerViewController` (Voraussetzung
`UIBackgroundModes=audio` in `Platforms/iOS/Info.plist` ist erfüllt und
per Contract-Test `IosInfoPlist_DeclaresAudioBackgroundMode` gepinnt).

## iOS-Plattformschicht

`VideoPlayer.Maui/Platforms/iOS/` enthält nur `AppDelegate.cs`
(Standard-`MauiUIApplicationDelegate`), `Program.cs`
(`UIApplication.Main`), `Info.plist` und `Entitlements.plist` —
**kein** eigener MediaElement-Mapper/-Handler am
`AVPlayerViewController` vorhanden.

## Relevante MVU-Nachrichten (`VideoPlayer.Mvu/AppMessage.cs`)

| Nachricht | Zeile | Zweck |
|-----------|-------|-------|
| `PlayerPageOpenedMessage` | 974 | Seite erschienen (einmalig pro Öffnen) |
| `PlaybackProgressMessage(Position, Duration, IsPlaying, IsBuffering)` | 1003 | 5-s-Positions-Takt |
| `PlaybackEndedMessage` | 1010 | Medium regulär zu Ende |
| `PlaybackFailedMessage` | 1013 | Wiedergabefehler |
| `PictureInPictureRequestedMessage` | 1016 | PiP-Umschaltung angefordert |
| `PictureInPictureChangedMessage(bool IsActive)` | 1024 | Ergebnis der PiP-Umschaltung → `IsPictureInPicture` |
| `PlayerClosedMessage` | 1030 | Seite verlassen → Abschluss-Report + Reset |
| `PlaylistNextRequestedMessage` / `PlaylistPreviousRequestedMessage` | 595 / 603 | Playlist-Navigation „Weiter"/„Zurück" |
| `TogglePictureInPictureEffect` (`AppEffect.cs:662`) | — | App-Rand-Effekt der PiP-Umschaltung |
