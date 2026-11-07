# Wiedergabeseite — Aufbau und Eingabefluss

## `PlayerPage.xaml`

Datei: `VideoPlayer.Maui/Pages/PlayerPage.xaml`

Ein einziges `Grid` (Hintergrund `VpBgDeep`) stapelt vier Kinder in
der Reihenfolge:

| Element | Rolle | Sichtbarkeitssteuerung |
|---------|-------|------------------------|
| `mct:MediaElement` `VideoElement` | Videofläche mit nativen Transportsteuerungen (`ShouldShowPlaybackControls="True"`, `ShouldAutoPlay="True"`, `ShouldKeepScreenOn="True"`) | immer sichtbar |
| `ActivityIndicator` `LoadingIndicator` | Lade-/Puffer-Anzeige | `IsVisible`/`IsRunning` an `IsResolving`/`IsBuffering` gebunden (Code-Behind) |
| `VerticalStackLayout` `OverlayChrome` | App-Overlay: Kopfband (`Border` mit `VpOverlay`-Hintergrund) mit `TitleLabel`, `WatchedBadge` („Angesehen"), `PipButton` („Mini-Player") | **kein `VerticalOptions` → Default `Fill` → belegt das gesamte Grid**; `IsVisible` im Code-Behind auf `!IsPictureInPicture` gesetzt |
| `Border` `ErrorPanel` | Fehlerhinweis | `VerticalOptions="End"`, `IsVisible="False"` (Code-Behind bei Fehler) |

### Hit-Test-Befund

- `OverlayChrome` hat **kein** `VerticalOptions`-Attribut. In MAUI
  gilt für Kinder eines `Grid` der `LayoutOptions`-Default `Fill`:
  Das Layout dehnt sich über die gesamte Zelle — hier die gesamte
  Videofläche — aus. MAUI-Layouts nehmen auf ihren Bounds Eingaben
  entgegen (u. a. damit Layout-Gesten funktionieren); der leere,
  unsichtbare Bereich unterhalb des Kopfbands liegt damit über dem
  `MediaElement` und fängt Zeigeeingaben ab, bevor sie die nativen
  Transportsteuerungen erreichen.
- Das Kopfband selbst (`Border` mit `VpOverlay`-Hintergrund) ist
  sichtbarer Inhalt und darf Eingaben beanspruchen — der
  `PipButton` muss klickbar bleiben.
- Im Mini-Player ist `OverlayChrome` komplett ausgeblendet
  (`IsVisible = false`), weshalb dort keine Blockade auftritt —
  konsistent mit der Kundenbeobachtung.

## `PlayerPage.xaml.cs`

Datei: `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `PlayerPage(MvuHost, IPictureInPictureService)` | public | `InitializeComponent`, `PipButton.IsVisible = pictureInPicture.IsToggleSupported`, `ModelChanged`-Abo |
| `OnAppearing` | protected override | `PlayerPageOpenedMessage` dispatchen, `UpdateUi`, Fortschrittstimer starten |
| `OnDisappearing` | protected override | Timer stoppen, `pictureInPicture.RestoreAsync()`, `PlayerClosedMessage` |
| `StartProgressTimer` | private | 5-s-Takt (`Dispatcher.StartTimer`), ruft `ReportPosition` bei `Playing` |
| `ReportPosition` | private | `PlaybackProgressMessage` mit Position/Dauer/Zustand dispatchen |
| `OnModelChanged` | private | `UpdateUi` auf dem Main-Thread |
| `UpdateUi` | private | Titel, WatchedBadge, `ErrorPanel`, **`OverlayChrome.IsVisible = !player.IsPictureInPicture`**, `LoadingIndicator`, Quellenwechsel am `MediaElement` |
| `OnMediaOpened` | private | Einmalig `SeekTo` auf die Fortsetzungsposition |
| `OnMediaEnded` | private | `PlaybackEndedMessage` |
| `OnMediaFailed` | private | `PlaybackFailedMessage` |
| `OnMediaStateChanged` | private | `ReportPosition` bei Zustandswechsel |
| `OnPipClicked` | private | `PictureInPictureRequestedMessage` dispatchten |

Abonnierte Events: `mvuHost.ModelChanged`, `MediaElement`-Events
(`MediaOpened`, `MediaEnded`, `MediaFailed`, `StateChanged`),
`PipButton.Clicked`.
Publizierte Nachrichten: `PlayerPageOpenedMessage`,
`PlaybackProgressMessage`, `PlaybackEndedMessage`,
`PlaybackFailedMessage`, `PictureInPictureRequestedMessage`,
`PlayerClosedMessage`.

## Mini-Player (Kontext, unverändert zu belassen)

- `VideoPlayer.Maui/Services/PictureInPictureService.cs` — Windows:
  `AppWindowPresenterKind.CompactOverlay`/`Default` (Contract-Pin in
  `PlayerContractTests.PictureInPictureService_UsesCompactOverlayOnWindows`).
- iOS: PiP-Schaltfläche kommt aus der nativen Transportsteuerung des
  `MediaElement` (`ShouldShowPlaybackControls`); `PipButton` ist dort
  ausgeblendet (`IsToggleSupported == false`).
- MVU: `TogglePictureInPictureEffect` in
  `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` (Contract-Pin
  `EffectHandler_HandlesPlayerEffects`); `IsPictureInPicture` steuert
  `OverlayChrome.IsVisible` und `ErrorPanel.IsVisible`.
