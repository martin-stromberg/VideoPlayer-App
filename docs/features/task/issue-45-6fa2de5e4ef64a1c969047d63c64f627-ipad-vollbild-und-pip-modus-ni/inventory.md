# Bestandsaufnahme: iPad — Vollbild- und PiP-Schaltflächen der nativen Wiedergabesteuerung frei erreichbar (Issue #45)

Analysiert wurde der Overlay- und PiP-Bereich der Wiedergabeseite
(`PlayerPage`) der `VideoPlayer.Maui`-App sowie die angrenzenden
MVU-Zustands-, Dienst- und Test-Artefakte, bezogen auf die übersetzte
Anforderung in [requirement.md](requirement.md).

## Zusammenfassung

- `PlayerPage.xaml` stapelt im `PlayerRoot`-Grid über dem
  `mct:MediaElement` (`ShouldShowPlaybackControls="True"`) vier
  Overlay-Elemente: `LoadingIndicator`, `OverlayChrome`
  (`VerticalStackLayout` mit `VerticalOptions="Start"`, innerer `Border`
  mit `InputTransparent="True"`), `PlaylistNavigationBar` (oben rechts,
  `Margin="0,64,16,0"`) und `PipButton` (oben rechts,
  `Margin="0,18,16,0"`). Das umschließende `OverlayChrome`-Layout selbst
  trägt **kein** `InputTransparent` — nur der innere `Border` ist
  eingabedurchlässig. Genau dieser obere Bereich überlappt unter iOS die
  native Transportleiste des `AVPlayerViewController`.
- `PlayerPage.xaml.cs` steuert die Overlay-Sichtbarkeit in `UpdateUi`
  (`OverlayChrome.IsVisible = !player.IsPictureInPicture`,
  `PlaylistNavigationBar.IsVisible = IsPlaylistPlayback &&
  !IsPictureInPicture`); `PipButton.IsVisible =
  pictureInPicture.IsToggleSupported` ist unter iOS `false`. Es gibt
  **keine** plattform-/idiombedingten Anpassungen — `OnPlatform`,
  `OnIdiom` und `DeviceInfo.Idiom` kommen im gesamten MAUI-Projekt nicht
  vor.
- Der iOS-Plattformcode beschränkt sich auf `AppDelegate`/`Program`;
  ein eigener MediaElement-Handler/Mapper am `AVPlayerViewController`
  existiert nicht. `Info.plist` deklariert bereits
  `UIBackgroundModes=audio` (PiP-Voraussetzung).
- `PlayerState` liefert alle für die Sichtbarkeitssteuerung genutzten
  Eigenschaften (`IsPictureInPicture`, `IsPlaylistPlayback`,
  `PlaylistLabel`, `IsWatched`, `Title` u. a.) — keine Änderung nötig.
- `IPictureInPictureService`/`PictureInPictureService` kapseln die
  Windows-Mini-Player-Umschaltung (`AppWindowPresenterKind.CompactOverlay`);
  unter iOS ist der Dienst inaktiv (`IsToggleSupported == false`) —
  iOS-PiP läuft ausschließlich über die native Transportsteuerung.
- Der Contract-Pin `PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface`
  in `PlayerContractTests` sichert heute `VerticalOptions="Start"` am
  Layout, `InputTransparent="True"` am inneren `Border`, kein `Button`
  im Band und `PipButton` als eigenes Grid-Element — er prüft die
  iOS-Problematik (Hit-Test der Elternlayouts) noch nicht ab.
- Was offensichtlich noch fehlt: eine iOS-seitige Freigabe des
  oberen Bedienbereichs (Eingabedurchlässigkeit des Elternlayouts bzw.
  verlegte/eingeengte Overlays) sowie die Test-Erweiterung dafür; echte
  iOS-UI-Automatisierung existiert im Repository nicht (dokumentierte
  Einschränkung).

**Test-Ausgangszustand:** `dotnet test
VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj
--configuration Release` — **1256 erfolgreich, 0 fehlgeschlagen,
0 übersprungen**, Exit-Code `0`. Keine nachgewiesenen Testfehler. Die
Windows-E2E-Suite (`VideoPlayer.E2E.Tests`, FlaUI) wurde nicht
ausgeführt — Grund und Umfang siehe
[Tests](inventory/tests.md).

## Details

- [Layout der PlayerPage (XAML-Overlay-Struktur)](inventory/layout.md)
- [Logik (Code-Behind, MVU-Update, Effekt-Handler, PiP-Dienst)](inventory/logic.md)
- [Datenmodell](inventory/models.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
