# Wiedergabeseite — Lebenszyklus und Threading

## `PlayerPage.xaml.cs`

Datei: `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `PlayerPage(MvuHost, IPictureInPictureService)` | public | `InitializeComponent`, `PipButton.IsVisible`, **`mvuHost.ModelChanged += OnModelChanged` im Konstruktor (Z. 38)**, `UpdateUi` |
| `OnAppearing` | protected override | `PlayerPageOpenedMessage`, `UpdateUi`, `StartProgressTimer` |
| `OnDisappearing` | protected override | `timerRunning = false`, `pictureInPicture.RestoreAsync()`, `PlayerClosedMessage` — **kein `ModelChanged`-Unsubscribe** |
| `StartProgressTimer` | private | 5-s-Takt über `Dispatcher.StartTimer` (läuft auf dem UI-Thread), ruft `ReportPosition` bei `Playing` |
| `ReportPosition` | private | liest `VideoElement.CurrentState`/`Position`/`Duration`, dispatcht `PlaybackProgressMessage` |
| `OnModelChanged` | private | `MainThread.BeginInvokeOnMainThread(() => UpdateUi(model))` |
| `UpdateUi` | private | Overlay-/Label-Updates; bei Quellenwechsel `VideoElement.Stop()`, `VideoElement.Source = null` bzw. `MediaSource.FromFile`/`FromUri` |
| `OnMediaOpened` | private | einmalig `SeekTo` auf `Player.Source.ResumePosition` (Z. 153) — **einzige SeekTo-Stelle** |
| `OnMediaEnded` / `OnMediaFailed` / `OnMediaStateChanged` | private | dispatchen `PlaybackEndedMessage`/`PlaybackFailedMessage`; `OnMediaStateChanged` ruft `ReportPosition` |
| `OnPipClicked` | private | `PictureInPictureRequestedMessage` |

Abonnierte Events: `mvuHost.ModelChanged` (ohne Abmeldung),
`MediaElement`-Events `MediaOpened`/`MediaEnded`/`MediaFailed`/
`StateChanged` (XAML-verdrahtet), `PipButton.Clicked`, Fortschrittstimer
über `Dispatcher.StartTimer`.

## Befund: Verwaiste Seiten (Lebenszyklus)

- Die Seite wird pro Wiedergabe neu erzeugt
  (`ShellAppEffectHandler.NavigateToPlayerAsync` →
  `Shell.Current.GoToAsync("player")`, ShellAppEffectHandler.cs:201-216).
- `ModelChanged +=` steht im Konstruktor (Z. 38), ein `-=` existiert
  **nirgends** — jede verlassene `PlayerPage` bleibt dauerhaft am
  `MvuHost`-Singleton hängen (Referenz hält die Seite samt
  `MediaElement` am Leben).
- Folge: Beim nächsten `PlayRequestedMessage`/`PlaybackSourceResolvedMessage`
  läuft `UpdateUi` auch auf der alten Seite: `location`-Wechsel löst
  `VideoElement.Stop()`/`VideoElement.Source = null` auf dem losgelösten
  Element aus — und das neue `MediaOpened` der alten Quellzuweisung kann
  `SeekTo` auf dem toten Element anstoßen.
- Vergleichsmuster im Repo: `SettingsPage` (SettingsPage.xaml.cs:46-56)
  abonniert in `OnAppearing` (`-=` vor `+=`) und meldet in
  `OnDisappearing` ab; ebenso `LoginPage`/`ServerSetupPage` (Inventur
  Projektebene §6: „melden korrekt ab").

## Befund: Thread-Affinität des nativen Elements

- `MediaOpened`, `StateChanged`, `MediaEnded`, `MediaFailed` kommen vom
  `CommunityToolkit.Maui.MediaElement` 10.0.0 (PackageReference in
  `VideoPlayer.Maui.csproj:53`). Auf Windows hängt das Element an
  `MediaPlayerElement` (WinUI) — COM-Objekt mit UI-Thread-Affinität;
  Zugriff von Threadpool-Threads wirft `COMException 0x8001010E`
  (`RPC_E_WRONG_THREAD`), wie im Kunden-Stack
  (`MediaPlayerElement.get_MediaPlayer` ← `MediaManager.PlatformSeek`).
- `OnMediaOpened` ruft `SeekTo` direkt im Event-Thread — marshalt das
  Toolkit das Event nicht auf den UI-Thread (Crash belegt das), läuft
  `PlatformSeek` auf einem Threadpool-Thread.
- `OnMediaStateChanged` → `ReportPosition` liest `VideoElement.CurrentState`/
  `Position`/`Duration` — dieselbe potentielle Fremdthread-Quelle.
- `UpdateUi` läuft über `OnModelChanged` bereits gemarshalt
  (`MainThread.BeginInvokeOnMainThread`, Z. 105); Aufrufe aus Konstruktor
  und `OnAppearing` sind UI-Thread. Dort wird aber kein „Seite verlassen"-
  Zustand geprüft — eine verwaiste Seite führt `UpdateUi` weiter aus.
- `Dispatcher.StartTimer`-Callback läuft auf dem UI-Thread — unproblematisch.

## Ablauf der zweiten Wiedergabe (Crash-Szenario, verifiziert am Code)

1. `PlayRequestedMessage` → `UpdatePlayRequested` (AppUpdate.cs:1334-1356):
   neuer `PlayerState` (`IsResolving = true`, `Source = null`) +
   `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect`.
2. `ModelChanged` feuert → **alte** `PlayerPage` (noch abonniert) rendert:
   `location`-Wechsel → `VideoElement.Stop()`/`Source = null` auf dem
   losgelösten `MediaElement`.
3. `ResolvePlaybackSourceAsync` (ShellAppEffectHandler.cs:218-244) löst die
   Quelle asynchron und dispatcht `PlaybackSourceResolvedMessage` über den
   MainThread → neuer `Source` im Modell → `UpdateUi` setzt
   `VideoElement.Source` (alte und neue Seite).
4. `MediaOpened` des neuen Elements → `OnMediaOpened` → `SeekTo` →
   `MediaManager.PlatformSeek` → `MediaPlayerElement.get_MediaPlayer` —
   läuft der Event auf einem Threadpool-Thread: `RPC_E_WRONG_THREAD`,
   unbehandelt → Absturz.
