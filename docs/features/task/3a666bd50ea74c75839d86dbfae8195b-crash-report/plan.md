# Umsetzungsplan: Absturz beim Start eines Videos unter iOS untersuchen und beheben

## Übersicht

Ursache bestätigt — am Code verifiziert **und** durch manuellen Test reproduziert (Anwender: Der Absturz tritt im Resume-Fall auf, d. h. bei Medien mit Weiterschauen-Position > 0): `PlayerPage.OnMediaOpened` ruft `VideoElement.SeekTo(ResumePosition)` auf, während auf iOS das Toolkit-`MediaOpened` noch vor `AVPlayerItemStatus.ReadyToPlay` feuert (Toolkit-Issue #3249). `PlatformSeek` wirft daraufhin eine `InvalidOperationException` innerhalb des `async void`-Handlers `MediaElementHandler.MapSeekRequested` → unbehandelte Ausnahme → `abort()` → `EXC_CRASH (SIGABRT)` auf Thread 0. Umgesetzt wird eine appseitige Korrektur in der `PlayerPage` (Seek-Wunsch verzögern bis zur Wiedergabebereitschaft), defensive Fehlerisolierung aller nativen `MediaElement`-Zugriffe (Fehler → `PlaybackFailedMessage` → `ErrorPanel` statt Prozessabbruch), die dSYM-Archivierung im CI-Build für künftige symbolisierbare Crash-Reports sowie eine E2E-Abdeckung des Wiedergabestarts auf Windows. Für den E2E-Einstieg über die Startseiten-Karussells wird `DevApiVisualTree.TrySelectItem` (`/invoke` mit `itemIndex`) um die Item-Auswahl in `BindableLayout`-Containern erweitert (in der Gegenprüfung verifiziert: der bisherige Pfad deckt nur `CollectionView` ab; `MediaCarousel` nutzt `BindableLayout` + `Tapped`-Event).

**Toolkit-Recherche (NuGet + GitHub, Stand dieser Planung):** `CommunityToolkit.Maui.MediaElement` **10.0.0 ist der neueste veröffentlichte Release** — ein Release > 10.0.0 existiert auf NuGet nicht. Der Fix für Issue #3249 (PR #3281 „Fix iOS/macOS MediaElement.MediaOpened Fires Before AVPlayerItem is ReadyToPlay", gemergt am 2026-10-02 auf `main`, Issue dadurch geschlossen) ist **noch in keinem NuGet-Release enthalten**. Ein Paket-Upgrade als Korrektur scheidet daher aus → der appseitige Workaround bleibt die primäre Behebung; die defensive Härtung bleibt unabhängig davon sinnvoll.

**Symbolisierungslage (geklärt):** Der vorliegende `crashlog.crash` bleibt unsymbolisiert — die dSYMs des Builds 213 wurden nicht archiviert, und ein Repro-Build von Commit `a9a5a75` + `atos` entfällt, da kein macOS-Zugriff besteht (Anwender steuert remote, Mac nicht verfügbar). Da die Ursache durch den manuellen Resume-Test unabhängig bestätigt ist, ist das unkritisch; die geplante dSYM-Archivierung deckt künftige Reports ab.

**iOS-Verifikation (geklärt):** Der Fix wird manuell über den nächsten TestFlight-Build verifiziert (Resume-Fall abspielen) — dokumentiert als letzter Schritt; keine Automatisierung geplant.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Zeitpunkt des Resume-Seeks | Der Seek-Wunsch wird bei `MediaOpened` lediglich in einem neuen Feld `pendingResumePosition` gemerkt; ein Gate `TryApplyPendingResumeSeek()` wendet ihn erst an, wenn `VideoElement.CurrentState` `Playing` oder `Paused` ist. Das Gate wird aus beiden Ereignispfaden aufgerufen (`OnMediaOpened` für den Fall „bereits bereit", `OnMediaStateChanged` für den Übergang). | `Playing`/`Paused` implizieren auf iOS `AVPlayerItemStatus.ReadyToPlay` — genau das Fenster, in dem `PlatformSeek` heute wirft, wird damit umgangen (Ursache manuell im Resume-Fall bestätigt). Ereignisgesteuert statt Polling/festem `Task.Delay` (kein geratenes Warte-Intervall). Alternativen „natives Start-Offset" (Toolkit bietet keines) und „Toolkit-Upgrade" (kein Release > 10.0.0 verfügbar — Fix #3281 nur auf `main`, unveröffentlicht; s. Übersicht) entfallen — der appseitige Workaround ist die etablierte Konvention des Windows-Absturz-Fixes. |
| Fehlerisolierung nativer Zugriffe | try/catch um jeden `VideoElement`-Zugriff (`UpdateUi`-Quellenblock, `ApplyResumeSeekAsync`, `ReportPosition`); bei Fehlern, die den Wiedergabestart betreffen, `PlaybackFailedMessage` dispatchen. | Anforderung: Fehler beim Wiedergabestart sichtbar als `player.Error` im `ErrorPanel` statt Prozessabbruch. Entscheidend: Ein try/catch um `SeekTo` allein würde den Absturz **nicht** verhindern — die fatale Ausnahme liegt im `async void` des Toolkit-Handlers, nicht in der zurückgegebenen Task. Das Readiness-Gate ist die eigentliche Korrektur; die Catches sichern Restrisiken (faulted/hängende `SeekTo`-Task, synchrone Würfe in `MediaSource.FromUri`/`Source`-Setter). |
| Vollendung/Timeout des Seeks | `SeekTo(position, cancellationToken)` mit `CancellationTokenSource`-Timeout (~10 s); Timeout oder Fault → Log + `PlaybackFailedMessage` (bei `subscribed`). | Die Toolkit-Implementierung setzt `SeekCompletedTCS` im Fehlerfall nie — ein ungebremster `await` hinge ewig und blockierte `seekToSemaphoreSlim` für alle Folge-Seeks. `PlaybackFailedMessage` folgt der vorhandenen Fehlerkonvention (`MediaFailed`-Pfad inkl. Episoden-Fallback). |
| Validierung der `Source.Location` | Vor der `MediaSource`-Erzeugung in `UpdateUi`: `ServerStream` → `Uri.TryCreate` mit http/https-Schema; `LocalFile` → nicht leerer Pfad. Ungültig → `PlaybackFailedMessage`. | Zweite Hypothese der Anforderung (ObjC-Ausnahme beim `AVPlayer`-Aufbau durch ungültige URL); billige Frühvalidierung am selben Fehlerpfad. |
| dSYM-Archivierung | Die `package-ios`-Action erzeugt zusätzlich `release-ios-dsym.zip` (Zip aller `*.dSYM`-Bundles aus `bin/Release`, tolerant bei leerem Fund); beide Workflows laden es wie die `.ipa` ans GitHub-Release. Analoger Export in `scripts/iOS-Deployment.ps1` beim lokalen `build`. | Release-Assets sind der heute etablierte Ablageort je Build (`release-ios.ipa` geht bereits dorthin) — kein neues Artefakt-System nötig; jede TestFlight-Buildnummer ist so später symbolisierbar. |
| Item-Klick auf `MediaCarousel` im E2E | `DevApiVisualTree.TrySelectItem` wird innerhalb des bestehenden `POST /invoke`-Endpunkts um die Item-Auswahl in `BindableLayout`-Containern erweitert: Ist das Zielelement keine `CollectionView`, wird im Teilbaum das erste `BindableObject` mit `BindableLayout.GetItemsSource(...) is IEnumerable` gesucht, das indexierte Kind-View genommen und dessen `TapGestureRecognizer` (im Kind-Teilbaum) per Reflection über die interne Methode `SendTapped(View, Func<IElement?, Point?>?)` ausgelöst — exakt der Aufruf, den der Plattform-Gesture-Manager bei einem Fingertipp macht (`Tapped` feuert mit `sender` = Kind-View → `MediaCarousel.OnItemTapped` → `ItemTapped` → `OnMediaItemTapped` → `MediaItemSelectedMessage`). Ergänzend liefert `ItemsOf` für solche Container die `items`-Liste in `/tree` (gleiche UIA-Lücke wie bei CollectionView). | Die `itemIndex`-Auswahl über `/invoke` ist das etablierte E2E-Muster für genau diese UIA-Lücke (`ServerSwitchTests` → `KnownServersView`): `BindableLayout`-Items fehlen unter Windows ebenso im Automation-Baum. Minimalinvasiv: keine Änderung an App-UI oder Laufzeitverhalten, kein neuer Endpunkt (`MauiDevApiService_MapsExactlyTheFourEndpoints` bleibt grün). Abgelehnt: **(b)** `AutomationId` je Eintrag + FlaUI-Klick — bräuchte ein gebundenes AutomationId-Template, ein Invoke-Pattern gibt es auf `Border`/Layouts nicht (nur `element.Click()` = Koordinatenmausklick), und horizontal offscreen liegende Einträge müssten erst gescrollt werden — deutlich fragiler als die direkte Ereignisauslösung; **(c)** Umbau der `MediaCarousel` auf `CollectionView` — verändert Selektions-, Virtualisierungs- und Layoutverhalten aller drei Karussells (Weiterschauen/Favoriten/Neu) für ein reines Testbarkeitsproblem, unverhältnismäßig invasiv. Reflection auf `SendTapped` ist DEBUG-only-Code der DevApi und wird per Contract-Test gepinnt (Upgrade-Bruch → roter Test statt stillem No-op). |
| E2E-Assertion des Wiedergabestarts | Der `DevApiModelSnapshot` wird um Player-Felder (`PlayerIsActive`, `PlayerIsPlaying`, `PlayerPositionSeconds`, `PlayerError`) erweitert; der E2E-Test navigiert über UI-Automation/DevApi bis „Abspielen" und assertiert gegen das Modell. | DevApi/`WaitForModelAsync` ist der etablierte Assertionskanal der E2E-Suite; die native Player-Oberfläche (`AVPlayerViewController`/`MediaPlayerElement`) ist für UI-Automation praktisch nicht zugänglich. |
| Testvideo-Asset | Eine generierte MP4 (< 200 KB, wenige Sekunden) wird als `Content`-Asset ins E2E-Projekt committed; der Mock-Stream erhält `Accept-Ranges`/`206`-Antworten. Erzeugung per ffmpeg; da ffmpeg auf dem Recherche-Rechner nicht installiert ist, wird im Umsetzungsschritt zunächst `ffmpeg -version` geprüft und bei Fehlen entweder per `winget` nachinstalliert oder ein frei verwendbares Mini-Sample-MP4 heruntergeladen (siehe Schritt 7). | Anwender-freigegeben; beide Erzeugungsvarianten geplant, weil die ffmpeg-Verfügbarkeit auf dem Umsetzungsrechner nicht gesichert ist. Eine selbstgeschriebene MP4-Box-Konstruktion wäre fehleranfällig — Download bzw. Installation ist robuster. |

## Programmabläufe

### Verzögerter Resume-Seek (Crash-Korrektur — bestätigte Ursache)

1. `PlaybackSourceResolvedMessage` → `UpdatePlaybackSourceResolved` → `ModelChanged` → `OnModelChanged` → `MainThread.BeginInvokeOnMainThread` → `UpdateUi`: bei geänderter `Source.Location` wird `pendingResumePosition` zurückgesetzt (ersetzt das bisherige `resumeApplied`-Reset) und die Quelle im try/catch gesetzt.
2. Das Toolkit feuert `MediaOpened` — auf iOS **vor** `ReadyToPlay` (Issue #3249; der Absturz im Resume-Fall ist manuell bestätigt). `OnMediaOpened` liest `mvuHost.Model.Player.Source?.ResumePosition`; liegt sie `> TimeSpan.Zero`, wird sie in `pendingResumePosition` gemerkt und über `BeginInvokeOnMainThread` → `if (subscribed) TryApplyPendingResumeSeek()` angestoßen. Es wird **kein** `SeekTo` mehr direkt aus dem Event ausgelöst.
3. `TryApplyPendingResumeSeek` (läuft ausschließlich auf dem UI-Thread): Ist `pendingResumePosition` gesetzt **und** `VideoElement.CurrentState` `MediaElementState.Playing` oder `MediaElementState.Paused`, wird die Position entnommen, `pendingResumePosition` geleert (Einmaligkeit) und `_ = ApplyResumeSeekAsync(position)` gestartet. Andernfalls bleibt der Wunsch liegen.
4. `OnMediaStateChanged` ruft im bestehenden `BeginInvokeOnMainThread`-Marshal nach `ReportPosition()` zusätzlich `TryApplyPendingResumeSeek()` — beim Übergang nach `Playing`/`Paused` wird ein noch liegender Seek ausgeführt.
5. `ApplyResumeSeekAsync` führt `await VideoElement.SeekTo(position, timeoutCts.Token)` im try/catch aus (UI-Thread-Kontext). Erfolg → fertig. `Exception`/`OperationCanceledException` (Timeout ~10 s) → Log und — bei `subscribed` — `DispatchAsync(PlaybackFailedMessage)` → `UpdatePlaybackFailed` → `player.Error` + `AdvanceToNextEpisode` → `ErrorPanel` (bzw. Serien-Rückfall).
6. Beide Event-Reihenfolgen sind abgedeckt: Feuert `MediaOpened` vor dem ersten `Playing`/`Paused` (iOS-Fall, bestätigt), trägt `OnMediaStateChanged` den Seek; liegt der Bereitschaftszustand bei `MediaOpened` bereits vor, wendet das Gate sofort an.

Beteiligte Klassen/Komponenten: `PlayerPage` (`OnMediaOpened`, `OnMediaStateChanged`, `TryApplyPendingResumeSeek`, `ApplyResumeSeekAsync`, `UpdateUi`), `MediaElement`/`MediaElementState` (Toolkit), `AppUpdate.UpdatePlaybackFailed`.

### Fehlerisolierter Quellenwechsel in `UpdateUi`

1. Der Block `VideoElement.Stop()` / `VideoElement.Source = null` / `MediaSource.FromFile|FromUri` / `VideoElement.Source = …` liegt in try/catch.
2. Vor dem Setter: Validierung der `Location` (siehe Validierungsregeln).
3. Catch/Validierungsfehler → `appliedLocation` zurücksetzen (erneuter Versuch bei nächster Modelländerung möglich) + `DispatchAsync(PlaybackFailedMessage)` → `ErrorPanel` statt Abort.

Beteiligte Klassen/Komponenten: `PlayerPage.UpdateUi`, `MediaSource`, `PlaybackSource`, `AppUpdate.UpdatePlaybackFailed`.

### Item-Auswahl in einer `MediaCarousel` per DevApi `/invoke` (E2E-Klickmechanismus)

1. `DevApiClient.InvokeAsync("RecentCarousel", itemIndex)` → `POST /invoke` → `MauiDevApiService.InvokeOnPage` → `FindByAutomationId` findet die `MediaCarousel` (`ContentView`) auf der `HomePage`.
2. `DevApiVisualTree.TrySelectItem(element, itemIndex)`: `element is CollectionView` → bisheriger `SelectedItem`-Pfad (unverändert). Andernfalls neuer Pfad: Im Teilbaum des Elements wird das erste `BindableObject` mit `BindableLayout.GetItemsSource(...) is IEnumerable` gesucht (in der `MediaCarousel` das `HorizontalStackLayout`).
3. Index-Prüfung gegen die enumerierte Eintragszahl (Fehlertexte im bisherigen Stil, z. B. „Der Item-Index {n} liegt außerhalb der Liste ({m} Einträge)" bzw. „… enthält keinen BindableLayout-Container").
4. Das indexierte Kind-View des Layouts wird genommen; in seinem Teilbaum wird das erste `View` mit einem `TapGestureRecognizer` in `GestureRecognizers` gesucht (in `MediaCarousel.xaml` die `Border` der Karte).
5. `TapGestureRecognizer.SendTapped(View, Func<IElement?, Point?>?)` ist `internal` (mit Source-Verweis verifiziert, `TapGestureRecognizer.cs` im MAUI-Repo) → Aufruf per Reflection (`BindingFlags.NonPublic | Instance`, `getPosition = null`). `SendTapped` führt ggf. `Command` aus und feuert `Tapped` mit `sender` = Kind-View — dieselbe Ereigniskette wie ein Fingertipp: `MediaCarousel.OnItemTapped` liest `sender.BindingContext` (`HomeMediaItem`) → `ItemTapped`-Event → `HomePage.OnMediaItemTapped` → `MediaItemSelectedMessage` → Detailseite. Schlägt die Reflection fehl (Signatur geändert), gibt `TrySelectItem` einen klaren Fehler zurück statt still nichts zu tun.
6. `ItemsOf` wird um `BindableLayout`-Container erweitert, damit `/tree` die Einträge der Karussells als `items` mit Index/Text auflistet (Test kann den Index wie bei `KnownServersView` ermitteln statt blind `0` zu klicken).

Beteiligte Klassen/Komponenten: `DevApiVisualTree` (`TrySelectItem`, `ItemsOf`, neue Hilfsmethoden), `MauiDevApiService` (`InvokeOnPage` — nur Kommentar/Endpunkt-Doku, kein neuer Endpunkt), `MediaCarousel` (unverändert — Mechanismus wirkt generisch auf `BindableLayout`-Container), `HomePage` (`OnMediaItemTapped`).

### dSYM-Archivierung (CI/Prozess)

1. `Collect iOS package` in `.github/actions/package-ios/action.yml` sammelt nach dem Publish zusätzlich alle `*.dSYM`-Verzeichnisse unter `VideoPlayer.Maui/bin/Release` und zippt sie zu `release-ios-dsym.zip` im Workspace-Root (kein Fund → Warnung, kein Fehlschlag).
2. `Upload iOS artifact` in `main-merge.yml` und `staging-merge.yml` lädt `release-ios-dsym.zip` — sofern vorhanden — per `gh release upload --clobber` ans Release (Pendant zur `.ipa`, an die Buildnummer gekoppelt).
3. `scripts/iOS-Deployment.ps1` legt beim `build` die dSYMs des Publish-Outputs als Zip neben der `.ipa` ab, damit auch lokale Store-Builds symbolisierbar bleiben. **Pin-Achtung:** Der neue Skriptteil darf die von `IosDeploymentResponseFileTests` gezählten Aufrufmuster nicht verändern oder nachbilden (`Add-PropertyLine -List $rspLines -Name "VideoPlayerApiToken"` ×3, `Add-MaskedDisplay -List $displayLines -Name "VideoPlayerApiToken"` ×2, `Write-MaskedPropertyLines -Lines $rspLines` ×3) — der dSYM-Export kommt ohne Response-Datei-Erzeugung aus (eigene `Compress-Archive`-/Copy-Logik).

Hinweis: Der vorliegende Report von Build 213 bleibt unsymbolisiert — ein Repro-Build von Commit `a9a5a75` + `atos` entfällt (kein macOS-Zugriff; geklärt). Die dSYM-Archivierung dient ausschließlich künftigen Reports.

Beteiligte Klassen/Komponenten: `.github/actions/package-ios/action.yml`, `.github/workflows/main-merge.yml`, `.github/workflows/staging-merge.yml`, `scripts/iOS-Deployment.ps1`.

### Manuelle iOS-Verifikation (TestFlight — kein automatisierter Schritt)

1. Nach Umsetzung und Merge erzeugt die CI den nächsten TestFlight-Build.
2. Der Anwender installiert ihn und startet den bestätigten Absturzpfad: ein Video mit Weiterschauen-Position > 0 (Resume-Fall) sowie ein Video ohne Fortsetzungsposition.
3. Erwartet: Wiedergabe startet an der Fortsetzungsposition bzw. am Anfang — ohne Absturz; bei einem Stream-Fehlschlag erscheint das `ErrorPanel` statt eines Abbruchs.
4. Rückmeldung erfolgt manuell (TestFlight); es wird keine Automatisierung geplant — der E2E-Harness läuft nur auf Windows, ein iOS-Geräte-/Simulator-Testprojekt existiert nicht.

## Neue Klassen

Keine.

## Änderungen an bestehenden Klassen

### `PlayerPage` (`VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`, Code-Behind)

- **Neue Felder:** `pendingResumePosition` (`TimeSpan?`) — gemerkter Seek-Wunsch pro Quelle; ersetzt `resumeApplied` (das Feld entfällt).
- **Neue Konstanten:** `ResumeSeekTimeout` (`TimeSpan`, ~10 s) — Abbruchschwelle für `SeekTo`.
- **Neue Methoden:**
  - `TryApplyPendingResumeSeek()` — Gate: wendet `pendingResumePosition` an, sobald `VideoElement.CurrentState` `Playing`/`Paused`; nur aus UI-Thread-Marshals aufrufbar.
  - `ApplyResumeSeekAsync(TimeSpan position)` — `await VideoElement.SeekTo(position, cts.Token)` mit Timeout; catch → Log + `PlaybackFailedMessage` (Guard `subscribed`).
- **Geänderte Methoden:**
  - `OnMediaOpened` — merkt `ResumePosition` in `pendingResumePosition` statt direkt zu seeken; stößt `TryApplyPendingResumeSeek` über `BeginInvokeOnMainThread` an; `subscribed`-Guard bleibt im Lambda.
  - `OnMediaStateChanged` — ruft im bestehenden Marshal zusätzlich `TryApplyPendingResumeSeek()`; `subscribed`-Guard bleibt.
  - `UpdateUi` — `Location`-Validierung + try/catch um alle `VideoElement`-Zugriffe; Fehler → `appliedLocation`-Reset + `PlaybackFailedMessage`; Reset von `pendingResumePosition` beim Quellenwechsel.
  - `ReportPosition` — try/catch (log-only) um die `VideoElement`-Lesezugriffe, da der Aufruf aus dem `Dispatcher`-Timer-Callback heraus sonst unbehandelt wäre.
- **Entfernte Felder:** `resumeApplied` (ersetzt durch `pendingResumePosition`).
- XAML (`PlayerPage.xaml`) bleibt unverändert — `StateChanged` ist bereits verdrahtet.

### `DevApiVisualTree` (`msTools.DeveloperApi.Maui/DevApiVisualTree.cs`, `#if DEBUG || DEV_API`)

- **Geänderte Methoden:**
  - `TrySelectItem` — neuer Zweig für Nicht-`CollectionView`-Elemente: `BindableLayout`-Container im Teilbaum finden (erste `BindableObject` mit `BindableLayout.GetItemsSource(...) is IEnumerable`), Index-Prüfung, indexiertes Kind-View nehmen, erstes `TapGestureRecognizer` im Kind-Teilbaum per Reflection-internem `SendTapped` auslösen; Fehlerfälle mit deutschen Meldungen wie bisher. Der `CollectionView`-Pfad bleibt unverändert.
  - `ItemsOf` — befüllt die `items`-Liste zusätzlich für `BindableLayout`-Container (gleiche UIA-Lücke wie bei `CollectionView`: Einträge fehlen im Automation-Baum).
- **Neue private Hilfsmethoden:** Suche des `BindableLayout`-Containers im Teilbaum, Suche des ersten `TapGestureRecognizer` im Kind-Teilbaum, `SendTapped`-Reflection (`MethodInfo` einmalig auflösen, Signatur pinnen — Fehlschlag → Fehlertext, kein stiller No-op).
- Kommentare/Dokstrings von `TrySelectItem`/`ItemsOf` und `MauiDevApiService.InvokeOnPage` von „CollectionView" auf „CollectionView oder BindableLayout-Container" erweitern — **kein** neuer Endpunkt (Pin `MauiDevApiService_MapsExactlyTheFourEndpoints` bleibt bestehen).

### `package-ios` (Composite Action, `.github/actions/package-ios/action.yml`)

- **Geänderter Schritt:** `Collect iOS package` — sammelt zusätzlich `*.dSYM` und schreibt `release-ios-dsym.zip`.

### Workflows `main-merge.yml` / `staging-merge.yml` (`.github/workflows/`)

- **Geänderter Schritt:** `Upload iOS artifact` — lädt `release-ios-dsym.zip` zusätzlich ans Release (existenz-guarded).

### `scripts/iOS-Deployment.ps1` (Deployment-Skript)

- **Neuer Schritt im `build`-Pfad:** dSYMs aus `VideoPlayer.Maui/bin/<config>/<framework>/<rid>` als Zip neben der `.ipa` ablegen — ohne die gezählten `Add-PropertyLine`-/`Add-MaskedDisplay`-/`Write-MaskedPropertyLines`-Aufrufmuster zu verändern (Pin `IosDeploymentResponseFileTests`).

### `DevApiModelSnapshot` (`VideoPlayer.Mvu/DevApi/DevApiModelSnapshot.cs`)

- **Neue Eigenschaften:** `PlayerIsActive` (`bool`), `PlayerIsPlaying` (`bool`), `PlayerPositionSeconds` (`double`), `PlayerError` (`string?`) — befüllt aus `model.Player` in `FromModel`.

### `DevApiModel` (`VideoPlayer.E2E.Tests/Support/DevApiModels.cs`)

- **Neue Eigenschaften:** `playerIsActive`, `playerIsPlaying`, `playerPositionSeconds`, `playerError` (`JsonPropertyName`-Spiegel des Snapshots).

### `MediaDetailsPage.xaml` (`VideoPlayer.Maui/Pages/`)

- **Neues Attribut:** `AutomationId="PlayButton"` am „Abspielen"-Button (E2E-Erreichbarkeit; heute nur `x:Name`).

### `HomePage.xaml` (`VideoPlayer.Maui/Pages/`)

- **Neues Attribut:** `AutomationId="RecentCarousel"` an der `MediaCarousel` „Neu im Programm" — macht einen Eintrag per `DevApiClient.InvokeAsync(automationId, itemIndex)` über die erweiterte `TrySelectItem` (`BindableLayout`-Pfad) anklickbar. (Dieselbe Mechanik deckt optional eine spätere `AutomationId` an der „Weiterschauen"-Karussell ab — Eintrag dort hat bereits `PositionSeconds > 0`; gleiche Klicklücke, keine Zusatzarbeit im Mechanismus.)

### `MockVideoServer` (`VideoPlayer.E2E.Tests/Support/`)

- **Neue Endpunkte (opt-in konfigurierbar — Defaults bleiben die bisherigen Leerlisten, damit Bestandstests unverändert bleiben):**
  - `GET api/items/recent` → ein konfigurierbarer Movie-Eintrag;
  - `GET api/items/movie/{id}` → Movie-Details-JSON;
  - `GET api/continue-watching` → Eintrag mit `PositionSeconds > 0` für den Resume-Pfad;
  - `GET api/items/movie/{id}/stream` → Bytes des Testvideos, `Content-Type: video/mp4`, minimaler Range-Support (`Accept-Ranges`-Header, `206 Partial Content` auf `Range`-Requests) für den Seek;
  - `POST api/continue-watching/progress` → 204-Sink (Positionsmeldungen der Wiedergabe).

### `VideoPlayer.E2E.Tests.csproj` (E2E-Projektdatei)

- **Neues Asset:** `Assets/testvideo.mp4` (generierte MP4, < 200 KB, wenige Sekunden, committed — vom Anwender freigegeben) als `Content` mit `CopyToOutputDirectory`, damit `MockVideoServer` sie aus dem Output-Verzeichnis ausliefert.

## Datenbankmigrationen

Keine.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `PlayerState.Source.Location` (Kind `ServerStream`) | `Uri.TryCreate` absolut + Schema `http`/`https` | Ungültig → `PlaybackFailedMessage` → `player.Error` im `ErrorPanel` |
| `PlayerState.Source.Location` (Kind `LocalFile`) | Nicht `null`/leer/whitespace | Ungültig → `PlaybackFailedMessage` → `player.Error` im `ErrorPanel` |
| `/invoke`-`itemIndex` auf `BindableLayout`-Container | Index ≥ 0 und < Eintragszahl; Container und `TapGestureRecognizer` müssen im Teilbaum vorhanden sein | Fehlertext (deutsch, Stil der bestehenden `TrySelectItem`-Meldungen) → HTTP 400 |

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Seek-Zeitpunkt auf allen Plattformen verschoben:** Der Resume-Sprung erfolgt nicht mehr bei `MediaOpened`, sondern beim ersten `Playing`/`Paused`. Auf Windows und Android war der bisherige Pfad funktionsfähig — dort entsteht eine minimal spätere Seek-Anwendung (Millisekunden-Bereich). Der Windows-Pfad wird durch den neuen E2E-Test abgedeckt.
- **Episoden-Rückfall bei Seek-Fehlschlag:** `PlaybackFailedMessage` aus `ApplyResumeSeekAsync` löst `AdvanceToNextEpisode` aus — bei Serien springt ein verfehlter Resume-Seek zur nächsten Episode statt nur eine Fehlermeldung zu zeigen. Bewusst übernommene Konvention des `MediaFailed`-Pfads.
- **Kein `Playing`/`Paused` → kein Seek:** Erreicht ein Medium nie den Bereitschaftszustand, wird die Fortsetzungsposition nicht angesprungen — die Wiedergabe beginnt ggf. am Anfang. Degradation statt Absturz; der Fehlerpfad (`MediaFailed` → `PlaybackFailedMessage`) bleibt unverändert wirksam.
- **`OnMediaStateChanged` wird häufiger aufgerufen:** Pro Zustandswechsel läuft ein zusätzlicher Flag-Check — vernachlässigbar.
- **`MockVideoServer`-Erweiterung:** Neue Endpunkte dürfen die Default-Antworten (leere Listen) der bestehenden E2E-Tests nicht verändern — Umsetzung über Opt-in-Konfiguration.
- **DevApi-Snapshot:** Neue Player-Felder stehen nur unter `DEBUG || DEV_API` zur Verfügung — kein Produktiv-Impact, keine Geheimnisse im Player-State.
- **DevApi-`SendTapped`-Reflection:** Der Aufruf hängt an einem internen MAUI-Member (`TapGestureRecognizer.SendTapped(View, Func<IElement?, Point?>?)`). Ein Umbenennen/Umsignieren in einem MAUI-Upgrade macht die Item-Auswahl unwirksam — abgefedert durch Fehlschlag-mit-Fehlertext statt stiller No-op und durch den Contract-Test, der den Mechanismus pinnt. Nur DEBUG/`DEV_API`-Code, kein Produktivpfad. Der Mechanismus ist generisch (`BindableLayout`), wirkt also auch auf künftige gleichartige Listen.
- **dSYM-Upload:** Vergrößert die Release-Assets (zusätzliche Zip je Build); tolerant implementiert — fehlende dSYMs brechen den Build nicht.
- **Windows-E2E deckt die iOS-Ursache nicht ab:** Der eigentliche SIGABRT ist iOS-spezifisch (`AVPlayer`/`async void`-Handler); der E2E-Test sichert den Benutzerfluss und die Resume-Korrektheit unter Windows. Die iOS-Verifikation erfolgt manuell über den nächsten TestFlight-Build (geklärt — siehe Programmablauf „Manuelle iOS-Verifikation", keine Automatisierung).
- **Künftiger Toolkit-Release > 10.0.0:** Der gemergte Fix #3281 lässt `MediaOpened` erst bei `ReadyToPlay` feuern — mit einem künftigen Upgrade würde das Gate den Seek sofort bei `MediaOpened` anwenden (bereits sicherer Zustand). Der Workaround bleibt auch mit gefixtem Toolkit korrekt und kann dann erneut bewertet werden.
- **Abspielbarkeit des Testvideos ungeprüft:** Ob der Windows-`MediaElement` (Media Foundation) den Loopback-Stream der Mini-MP4 annimmt (Progressive Download, Range-Requests), wird erst in der Umsetzung verifiziert. Fallback: E2E auf „Player-Seite öffnet sich, Fehlerpfad sichtbar, Prozess lebt" begrenzen und den Seek-Nachweis den Contract-Tests überlassen.
- **Restrisiko §6.2 der Bestandsaufnahme (akzeptiert, Folgebeobachtung):** `PlatformSeek` wirft im `Player.Seek`-Completion-Callback bei `complete == false` eine `InvalidOperationException` innerhalb eines ObjC-Blocks — appseitig **nicht** abfangbar (kein managed `await`-Pfad). Das Readiness-Gate verkleinert das Eintrittsfenster deutlich (Seek läuft nur noch im `ReadyToPlay`-Zustand), schließt es aber nicht vollständig; als bewusst offenes Restrisiko führen.
- **Sekundärverdacht §6.7 (akzeptiert, Folgebeobachtung):** `OnMediaEnded`/`OnMediaFailed` dispatchen vom Event-Thread; `ModelChanged`/`PropertyChanged` und XAML-Bindings können dadurch auf einem Hintergrundthread feuern (mögliche UIKit-Berührung auf iOS). Bleibt bewusst unberührt — beobachten, falls nach dem Fix weiterhin iOS-Abstürze gemeldet werden.

## Umsetzungsreihenfolge

1. **`PlayerPage`: verzögerter Resume-Seek**
   - Voraussetzungen: Keine.
   - Beschreibung: `pendingResumePosition`, `TryApplyPendingResumeSeek`, `ApplyResumeSeekAsync` (mit `ResumeSeekTimeout`), Umbau von `OnMediaOpened`/`OnMediaStateChanged`/`UpdateUi`-Reset gemäß Programmablauf.

2. **`PlayerPage`: Fehlerisolierung + `Location`-Validierung**
   - Voraussetzungen: Schritt 1 (Methodenstruktur).
   - Beschreibung: try/catch um den `UpdateUi`-Quellenblock und `ReportPosition`; `Location`-Validierung vor `MediaSource`-Erzeugung; Fehlerpfade → `PlaybackFailedMessage`.

3. **Contract-Tests anpassen und ergänzen** (`VideoWebPlayer.Maui.Tests`)
   - Voraussetzungen: Schritte 1–2.
   - Beschreibung: `PlayerPage_MarshalsNativeElementAccessOnMainThread` auf die neue Struktur umstellen; neue Contract-Tests `PlayerPage_ResumeSeek_WaitsForReadyState` und `PlayerPage_NativeAccessesAreFailureIsolated` (siehe Tests).

4. **dSYM-Archivierung in CI und lokalem Skript**
   - Voraussetzungen: Keine.
   - Beschreibung: `Collect iOS package` (Action) + `Upload iOS artifact` (beide Workflows) + `scripts/iOS-Deployment.ps1` — ohne die von `IosDeploymentResponseFileTests` gezählten Aufrufmuster zu verändern.

5. **DevApi-Player-Felder**
   - Voraussetzungen: Keine.
   - Beschreibung: `DevApiModelSnapshot` um `PlayerIsActive`/`PlayerIsPlaying`/`PlayerPositionSeconds`/`PlayerError` erweitern; `DevApiModel`-DTO spiegeln; Mapping-Test in `DevApiModelSnapshotTests`.

6. **DevApi `TrySelectItem` um `BindableLayout`-Item-Auswahl erweitern** (Klickmechanismus für `MediaCarousel`-Einträge)
   - Voraussetzungen: Keine (baut auf vorhandener `msTools.DeveloperApi.Maui`-Struktur auf).
   - Beschreibung: `BindableLayout`-Container-Suche + `TapGestureRecognizer.SendTapped`-Reflection in `TrySelectItem`; `ItemsOf` für `BindableLayout`-Container; Kommentare/`InvokeOnPage`-Doku anpassen; neuer Contract-Test `DevApiVisualTree_SelectItem_InvokesBindableLayoutItemTap` (siehe Tests). Kein neuer Endpunkt — `MauiDevApiService_MapsExactlyTheFourEndpoints` muss grün bleiben.

7. **Testvideo-Asset erzeugen/beschaffen**
   - Voraussetzungen: Keine. (Variante B benötigt ggf. `winget` oder Internetzugang.)
   - Beschreibung: Zunächst `ffmpeg -version` prüfen. **Variante A (ffmpeg vorhanden oder per `winget install --id Gyan.FFmpeg` nachinstalliert):** Mini-MP4 generieren, z. B. `ffmpeg -f lavfi -i testsrc=duration=3:size=320x180:rate=15 -f lavfi -i sine=frequency=440:duration=3 -c:v libx264 -pix_fmt yuv420p -c:a aac -movflags +faststart -y Assets/testvideo.mp4` (Ziel < 200 KB). **Variante B (kein ffmpeg verfügbar/installierbar — auf dem Recherche-Rechner war ffmpeg nicht installiert):** frei verwendbares Mini-Sample-MP4 herunterladen (öffentliche Test-Asset-Quelle, z. B. W3C-/Projekt-Testassets oder file-examples.com), auf < 200 KB und Abspielbarkeit unter Windows Media Foundation stichprobenartig prüfen. Ergebnis als `Assets/testvideo.mp4` ins E2E-Projekt committen und in der csproj als `Content`/`CopyToOutputDirectory` eintragen.

8. **E2E-Infrastruktur für den Wiedergabefluss**
   - Voraussetzungen: Schritte 5–7.
   - Beschreibung: `AutomationId`s (`PlayButton`, `RecentCarousel`); `MockVideoServer`-Endpunkte (recent, movie-Details, continue-watching, stream inkl. Range-Support, progress-Sink).

9. **`PlaybackTests` (E2E)**
   - Voraussetzungen: Schritte 1–2 und 5–8.
   - Beschreibung: Szenarien „Wiedergabestart mit Fortsetzungsposition" und „Stream-Fehler → sichtbarer Fehler statt Absturz" (siehe Tests). Der Karussell-Klick läuft über `DevApi.InvokeAsync("RecentCarousel", index)` — Mechanismus aus Schritt 6.

10. **Manuelle iOS-Verifikation über den nächsten TestFlight-Build** (kein Repo-Arbeitsschritt, nicht blockierend)
    - Voraussetzungen: Gemergte Umsetzung der Schritte 1–4 im nächsten TestFlight-Build; iOS-Gerät beim Anwender.
    - Beschreibung: Anwender spielt den bestätigten Absturzpfad manuell nach — Video mit Weiterschauen-Position > 0 (Resume) sowie Video ohne Position; erwartet Start ohne Absturz. Bewusst ohne Automatisierung (E2E-Harness nur Windows).

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `PlayerPage_ResumeSeek_WaitsForReadyState` | `PlayerContractTests` | `OnMediaOpened` enthält kein `SeekTo(` mehr (nur Merken des Seek-Wunsches); genau ein `SeekTo(` im File, in `ApplyResumeSeekAsync`; Gate `MediaElementState.Playing`/`Paused` in `TryApplyPendingResumeSeek`; Aufruf des Gates aus `OnMediaOpened`- und `OnMediaStateChanged`-Marshal; `BeginInvokeOnMainThread` in beiden Handlern bleibt. |
| `PlayerPage_NativeAccessesAreFailureIsolated` | `PlayerContractTests` | try/catch um den `UpdateUi`-Quellenblock, um `ApplyResumeSeekAsync` und um `ReportPosition`; `PlaybackFailedMessage` in den Fehlerpfaden von `UpdateUi`/`ApplyResumeSeekAsync`; `Location`-Validierung vor `MediaSource.FromUri`. |
| `DevApiModelSnapshot_MapsPlayerState` | `DevApiModelSnapshotTests` | `FromModel` mappt `PlayerIsActive`/`PlayerIsPlaying`/`PlayerPositionSeconds`/`PlayerError` korrekt aus einem befüllten `PlayerState`. |
| `DevApiVisualTree_SelectItem_InvokesBindableLayoutItemTap` | `DevApiContractTests` | Pinnt den neuen Mechanismus quelltextbasiert: `TrySelectItem` enthält einen `BindableLayout`-Pfad (`BindableLayout.GetItemsSource`), den `SendTapped`-Reflection-Aufruf mit gepinnter Signatur, Fehlerzweige für Index außerhalb/kein Container/kein `TapGestureRecognizer`; `ItemsOf` befüllt `BindableLayout`-Items. Bricht bei MAUI-Upgrade-Umbau — gewollt. |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `PlayerContractTests.PlayerPage_MarshalsNativeElementAccessOnMainThread` | `SeekTo` steht nicht mehr im `OnMediaOpened`-Marshal — die Assertion „SeekTo innerhalb von `OnMediaOpened`" muss auf die neue Struktur umgestellt werden (SeekTo genau einmal, in `ApplyResumeSeekAsync`, nur über das UI-Thread-Gate erreichbar). |
| `DevApiContractTests.MauiDevApiService_MapsExactlyTheFourEndpoints` | Zu prüfender Bestandstest: die `TrySelectItem`-Erweiterung läuft innerhalb des bestehenden `/invoke`-Endpunkts — es darf **kein** neuer `Map(`-Aufruf/`dispatch`-Bezeichner entstehen, sonst bricht der Pin. |
| `DevApiContractTests.DevApiVisualTree_UsesPlatformInvokeEquivalents` | Zu prüfender Bestandstest: Contains-basierte Assertions (`SendClicked()`, `InputView`, `ItemsSource`) bleiben grün; der neue BindableLayout-Pfad wird vom neuen Test gepinnt. |
| `IosDeploymentResponseFileTests.DeploymentScript_FeedsTokenThroughSanitizingAtAllCallSites` / `DeploymentScript_LogsMaskedLinesOnBuildFailure` | Zu prüfende Zähl-Pins (`Add-PropertyLine -List $rspLines -Name "VideoPlayerApiToken"` ×3, `Add-MaskedDisplay -List $displayLines -Name "VideoPlayerApiToken"` ×2, `Write-MaskedPropertyLines -Lines $rspLines` ×3): der neue dSYM-Schritt in `scripts/iOS-Deployment.ps1` darf diese exakten Aufrufmuster weder verändern noch ergänzen. |

(`PlayerPage_IgnoresModelChangesAfterDisappearing`, `PlayerPage_DispatchesPlaybackMessages` u. a. bleiben grün, sofern die `subscribed`-Guards in den Marshals und die bestehenden Message-Dispatches erhalten bleiben — im Umsetzungsumfang enthalten.)

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Wiedergabestart mit Fortsetzungsposition: Pairing/Login → „Neu im Programm"-Eintrag per `/invoke`+`itemIndex` (BindableLayout-Pfad) → Mediendetails → „Abspielen" → `playerIsActive`/`playerIsPlaying`, `playerPositionSeconds` ≥ Weiterschauen-Position, Prozess lebt weiter. | `VideoPlayer.E2E.Tests/Tests/PlaybackTests.cs` | „Das Starten eines Videos darf die Anwendung nicht beenden"; Resume-Seek landet auf der Fortsetzungsposition. | Einziger in-Repo-Nachweis des kompletten Benutzerflusses inkl. verzögertem Seek; die Contract-Tests pinnen nur Quelltext-Struktur, nicht das Laufzeitverhalten. (iOS selbst ist im Harness nicht automatisierbar — Windows-Deckung als Regressionsschutz, iOS-Nachweis manuell via TestFlight.) |
| Pflicht | Stream-Fehlschlag beim Wiedergabestart (Stream-Endpunkt liefert 404): Karussell-Eintrag → „Abspielen" → `playerError` gesetzt/`ErrorPanel` sichtbar, Prozess lebt weiter. | `VideoPlayer.E2E.Tests/Tests/PlaybackTests.cs` | Fehler beim Wiedergabestart landen als `player.Error`, nicht als Prozessabbruch. | Über die UI auslösbarer, anwendersichtbarer Fehlerfall; prüft die Fehlerisolierung end-to-end. |

Bestehende E2E-Tests müssen nicht angepasst werden — die `MockVideoServer`-Erweiterung bleibt per Default rückwirkungskompatibel (leere Listen), die neuen `AutomationId`s ändern kein sichtbares Verhalten, und die `TrySelectItem`-Erweiterung lässt den `CollectionView`-Pfad (`ServerSwitchTests` → `KnownServersView`) unverändert.

## Offene Punkte

Keine.
