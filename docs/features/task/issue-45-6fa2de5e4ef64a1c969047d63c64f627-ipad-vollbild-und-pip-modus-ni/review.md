# Plan-Review

Geprüft: `plan.md` gegen die Implementierung (Iteration 1). Alle
Planelemente wurden im Code gesucht und die relevanten Quelldateien
vollständig gelesen: `VideoPlayer.Maui/Pages/PlayerPage.xaml`,
`VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`,
`VideoWebPlayer.Maui.Tests/PlayerContractTests.cs`.

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

### `PlayerPage` — XAML (`VideoPlayer.Maui/Pages/PlayerPage.xaml`)

- [x] Attribut `InputTransparent="True"` am `OverlayChrome`-
  `VerticalStackLayout` (Elternlayout) — vorhanden (Zeile 45).
- [x] Kommentarblock zu `OverlayChrome` um die iOS-Hit-Test-Begründung
  erweitert (Elternlayout hit-testet unter iOS trotz eingabedurchlässigem
  Inhalt in den eigenen Bounds und fing die Eingaben der nativen
  Transportleiste ab; Vorgabe „keine interaktiven Elemente im Band"
  bestehen geblieben) — vorhanden (Zeilen 30–42).
- [x] `PlaylistNavigationBar` XAML-seitig unverändert:
  `Margin="0,64,16,0"`, `HorizontalOptions="End"`,
  `VerticalOptions="Start"`, eigenes `PlayerRoot`-Kind, kein
  `InputTransparent` — vorhanden (Zeilen 89–94).
- [x] `PipButton`, `ErrorPanel`, `LoadingIndicator`, `VideoElement`
  (`ShouldShowPlaybackControls="True"`) unverändert — bestätigt.

### `PlayerPage` — Code-Behind (`VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`)

- [x] `#if IOS`-Block im Konstruktor nach der
  `PipButton.IsVisible`-Zuweisung: `PlaylistNavigationBar.Margin =
  new Thickness(0, 96, 16, 0)` mit erläuterndem Kommentar (native
  Top-Leiste mit Vollbild-/PiP-Buttons bleibt frei; Bar bleibt
  interaktiv) — vorhanden (Zeilen 54–62).
- [x] Keine weiteren Änderungen: `UpdateUi`-Sichtbarkeitsregeln
  (`OverlayChrome.IsVisible = !player.IsPictureInPicture`,
  `PlaylistNavigationBar.IsVisible = player.IsPlaylistPlayback &&
  !player.IsPictureInPicture`), `#if WINDOWS`-Tap-Workaround in
  `OnPlayerTapped`, alle Event-Handler und MVU-Dispatches unverändert —
  bestätigt.
- [x] Keine Änderungen an `PlayerState`, `AppUpdate`,
  `ShellAppEffectHandler`, `PictureInPictureService`,
  `Platforms/iOS/Info.plist` — plan-konform (Diff betrifft nur die drei
  geplanten Dateien).

### Tests (`VideoWebPlayer.Maui.Tests/PlayerContractTests.cs`)

- [x] `PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface` erweitert um
  die Assertion `InputTransparent="True"` am `OverlayChrome`-
  Elternlayout selbst; Bestands-Assertions (`VerticalOptions="Start"`,
  `InputTransparent` am `Border`, kein `Button` im Band, `PipButton`
  als eigenes Grid-Element) erhalten — vorhanden (Zeilen 101–140).
- [x] Neuer Test `PlayerPage_PlaylistNavigationBarClearsNativeControlsOnIos`
  — prüft den `#if IOS`-Block samt `Thickness(0, 96, 16, 0)` im
  Konstruktor, die Bar als direktes `PlayerRoot`-Kind außerhalb des
  eingabedurchlässigen Bands und fehlendes `InputTransparent` an der Bar
  — vorhanden (Zeilen 152–184).
- [x] Build und Testsuite: `dotnet test
  VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj
  --configuration Release` — 1257 Tests grün (berichtet); die beiden
  geänderten/neuen Tests wurden im Review direkt verifiziert (2/2
  bestanden).

## Offene Aufgaben

Keine im Implementierungsumfang — alle geplanten Code- und
Testelemente sind umgesetzt.

## Hinweise

- **Manuelle iPad-Verifikation (Plan-Schritt 5, Tasks-Datei #8):**
  bleibt als dokumentierter, nicht automatisierbarer Geräte-Prüfschritt
  per TestFlight offen — iOS-UI-Automatisierung existiert im Repository
  nicht (plan-konforme Verifikationsgrenze). Zeigt die Prüfung
  Restblockaden, ist der iOS-Top-Margin (`96`) feinzujustieren bzw. der
  dokumentierte Handler-Fallback als Follow-up zu eskalieren.
- **Hilfedokumentation (Plan-Schritt 6, Tasks-Datei #9):** ist im Plan
  explizit dem Lifecycle-Schritt Dokumentation zugeordnet und daher
  bewusst noch offen (`docs/help/wiedergabe-pip.md`).
- Die Aufgabenliste (`…-tasks.md`) wurde aktualisiert: #1–#7 Erledigt
  mit Testnachweis, #8–#9 Offen (manueller Geräteschritt bzw.
  separater Dokumentationsschritt).
