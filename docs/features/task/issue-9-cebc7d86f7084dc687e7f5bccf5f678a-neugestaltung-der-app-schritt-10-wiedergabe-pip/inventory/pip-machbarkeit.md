# Bestandsaufnahme: MediaElement und PiP-Machbarkeit

Verifiziert am 22.09.2026 anhand des lokalen NuGet-Caches
(`communitytoolkit.maui.mediaelement/10.0.0`) und der
Toolkit-Quellen (github.com/CommunityToolkit/Maui).

## Paket

- `CommunityToolkit.Maui.MediaElement` **10.0.0** — stabil,
  veröffentlicht ~03.08.2026 (> 7 Tage alt), bereits vom Altbestand
  `VideoWebPlayer.Maui` genutzt und im lokalen NuGet-Cache
  vorhanden. Zielframeworks des Pakets: `net10.0`,
  `net10.0-android36.0`, `net10.0-ios26.0`,
  `net10.0-maccatalyst26.0`, `net10.0-windows10.0.19041` —
  kompatibel mit den Zielen von `VideoPlayer.Maui`.
- Aktivierung: `builder.UseMauiCommunityToolkitMediaElement()` in
  `MauiProgram` (Namespace `CommunityToolkit.Maui.MediaElement`).

## MediaElement-API (10.0.0, aus der XML-Doku der Assembly)

- Eigenschaften: `Source` (`MediaSource.FromUri`/`FromFile`),
  `Position`, `Duration`, `CurrentState`, `ShouldAutoPlay`,
  `ShouldShowPlaybackControls`, `ShouldKeepScreenOn`,
  `ShouldLoopPlayback`, `ShouldMute`, `Speed`, `Volume`, `Aspect`,
  `MetadataTitle`, `MetadataArtist`, `MetadataArtworkUrl`
- Ereignisse: `MediaOpened`, `MediaEnded`, `MediaFailed`,
  `StateChanged`, `PositionChanged`, `SeekCompleted`
- Methoden: `Play()`, `Pause()`, `Stop()`, `SeekTo(TimeSpan, ct)`

## PiP auf iOS — machbar, nativ

- Das Toolkit hostet auf iOS einen `AVPlayerViewController`
  (`MediaManager.macios.cs`: `PlayerViewController = new() { Player
  = Player }`, `ShowsPlaybackControls` an
  `ShouldShowPlaybackControls` gekoppelt, Audio-Session-Kategorie
  `Playback` gesetzt).
- `AVPlayerViewController.AllowsPictureInPicturePlayback` ist unter
  iOS standardmäßig `true` — mit sichtbarer Transportsteuerung
  erscheint die native PiP-Schaltfläche; der PiP-Modus bleibt beim
  Verlassen der App aktiv.
- **Voraussetzung:** `UIBackgroundModes` mit `audio` in
  `Platforms/iOS/Info.plist` (in beiden App-Projekten aktuell nicht
  gesetzt) — sonst endet PiP beim Hintergrund-Übergang.
- Kein eigener Plattformcode auf iOS nötig (PiP-Schaltfläche ist
  Teil der Plattform-Steuerung).

## PiP auf Windows — machbar, AppWindow-CompactOverlay

- Das Toolkit nutzt auf Windows `MediaPlayerElement` (WinUI) —
  ohne eigene PiP-Funktion. Ein „Mini-Player" ist über den
  `CompactOverlay`-Presenter des `AppWindow` realisierbar:
  `window.Handler.PlatformView` → `Microsoft.UI.Xaml.Window` →
  `AppWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay)`
  bzw. `Default`. `AppWindow` steht auch unverpackten Apps zur
  Verfügung (`WindowsPackageType=None` im Projekt, WinAppSDK ist
  Teil des MAUI-Windows-Stacks — kein zusätzliches SDK nötig).
- Ausprägung „kompaktes Overlay-/Mini-Player-Fenster": beim
  Aktivieren zeigt die Seite nur noch das MediaElement (restliche
  UI ausgeblendet), das Fenster geht in den immer-im-Vordergrund-
  Kompaktmodus; beim Verlassen zurück zum Vollbild-Layout.
- Risiko: keines festgestellt; der Mechanismus ist dokumentiertes
  WinAppSDK-API und benötigt nur `#if WINDOWS`-Code am App-Rand.
