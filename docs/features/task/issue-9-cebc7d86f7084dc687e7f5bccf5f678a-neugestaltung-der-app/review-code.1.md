# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### AppShell.xaml.cs (AppShell)

- **Race-Condition / veralteter Render-Stand** — `OnModelChanged` (Zeile 79-83) liest
  `mvuHost.Model` auf dem auslösenden Dispatch-Thread und captured den Snapshot in die
  Main-Thread-Closure. `MvuHost.DispatchAsync` löst `ModelChanged` erst nach
  `gate.Release()` aus (`MvuHost.cs` Zeile 57-62) — überlappende Dispatches können die
  Handler daher verzahnt ausführen: Handler A liest Modell v5, Handler B liest v6 und
  queued zuerst, A queued danach v5. Das Menü endet auf dem veralteten Stand v5, bis ein
  beliebiger späterer `ModelChanged` korrigiert.

  Empfehlung: Das Modell erst im Main-Thread-Callback lesen —
  `MainThread.BeginInvokeOnMainThread(() => UpdateSourceMenuItems(mvuHost.Model))`.
  Jeder gequeue Rendervorgang nutzt dann den zum Ausführungszeitpunkt aktuellen Stand;
  `SameSources` macht Mehrfachläufe ohnehin idempotent.

- **Fehlende Erstbefüllung des Menüs** — `AppShell` wird lazy über
  `services.GetRequiredService<AppShell>()` in `ShellAppEffectHandler.NavigateAsync`
  (`ShellAppEffectHandler.cs` Zeile 1018) erzeugt — also innerhalb des
  `NavigateToPageEffect`, dem letzten Effekt des `EnterConnected`-Übergangs. Der zuvor
  sequenziell ausgeführte `LoadSourcesEffect` hat `SourcesLoadedMessage` dann bereits
  angewendet: `SourcesOverview.Sources` ist zum Zeitpunkt des Konstruktors gefüllt, die
  `ModelChanged`-Subscription verpasst den Stand aber. Das Menü zeigt keine
  Quelleneinträge, bis der nächste Dispatch zufällig feuert (typischerweise erst nach
  Abschluss des Voll-Syncs).

  Empfehlung: Nach dem Abonnieren im Konstruktor einmalig
  `UpdateSourceMenuItems(mvuHost.Model)` aufrufen (der Konstruktor läuft über
  `MainThread.InvokeOnMainThreadAsync` bereits auf dem Main-Thread).

- **Inkonsistenter Zustand bei Ausnahme / fehlende Fehlerbehandlung im UI-Callback** —
  `UpdateSourceMenuItems` setzt `renderedSources = sources` (Zeile 94) *vor*
  `InsertSourceMenuItems`. Wirft `Items.Insert` mittendrin, behauptet `renderedSources`
  einen gerenderten Stand und `SameSources` unterdrückt jeden Neuversuch — das Menü
  bleibt dauerhaft halb aufgebaut. Zudem ist die gemarshallte Lambda der einzige Pfad
  der Datei ohne `try/catch` (`DispatchNavigatedAsync`, `DispatchSourceSelectedAsync`,
  `LoadSourceIconAsync` fangen alle `Exception` und loggen) — eine Ausnahme dort ist ein
  unbehandelter Fehler auf dem Main-Thread.

  Empfehlung: `renderedSources` erst nach erfolgreichem Einfügen setzen und den
  Callback-Rumpf in `try/catch` mit `Debug.WriteLine` wie im übrigen File kapseln.

### AppUpdate.cs (AppUpdate)

- **Doppelter Code** — Der Block „bei `Connected && !SourcesOverview.IsLoading`:
  `SourcesOverview` mit `IsLoading = true` belegen + `LoadSourcesEffect` anhängen" ist
  wortgleich in `UpdateSynchronizationCompleted` (Zeile 541-548) und
  `UpdateCachedListsChanged` (Zeile 566-573) dupliziert (~8 Zeilen identische Logik
  samt Guard).

  Empfehlung: In eine kleine Hilfsmethode auslagern, z. B.
  `private static AppModel WithSourcesReload(AppModel model, List<AppEffect> effects)`,
  die Guard, State-Update und Effekt-Anhängung kapselt und das ggf. aktualisierte Model
  zurückgibt.

## Geprüfte Dateien

- `VideoPlayer.Maui/AppShell.xaml`
- `VideoPlayer.Maui/AppShell.xaml.cs`
- `VideoPlayer.Maui/MauiProgram.cs`
- `VideoPlayer.Maui/Controls/CachedSourceIconImage.cs` (entfernt)
- `VideoPlayer.Maui/Pages/SourcesPage.xaml` (entfernt)
- `VideoPlayer.Maui/Pages/SourcesPage.xaml.cs` (entfernt)
- `VideoPlayer.Mvu/AppMessage.cs`
- `VideoPlayer.Mvu/AppModel.cs`
- `VideoPlayer.Mvu/AppPage.cs`
- `VideoPlayer.Mvu/AppRoutes.cs`
- `VideoPlayer.Mvu/AppUpdate.cs`
- `VideoPlayer.Mvu/SourcesOverviewState.cs`
- `VideoWebPlayer.Maui.Tests/AppRoutesTests.cs`
- `VideoWebPlayer.Maui.Tests/AppUpdateTests_Connection.cs`
- `VideoWebPlayer.Maui.Tests/AppUpdateTests_HomeLists.cs`
- `VideoWebPlayer.Maui.Tests/AppUpdateTests_NetworkSync.cs`
- `VideoWebPlayer.Maui.Tests/AppUpdateTests_Offline.cs`
- `VideoWebPlayer.Maui.Tests/AppUpdateTests_Sources.cs`
- `VideoWebPlayer.Maui.Tests/MvuHostTests.cs`
- `VideoWebPlayer.Maui.Tests/SourcesContractTests.cs`
- `VideoWebPlayer.Maui.Tests/VideoPlayerMauiContractTests.cs`
- `README.md`
- `docs/help/app-grundgeruest.md`
- `docs/help/index.md`
- `docs/help/offline-wiederherstellung.md`
- `docs/help/quellen-browsing-details.md`

Gegenprüfungen: `dotnet build` für `VideoPlayer.Maui` (net10.0-windows) und
`VideoWebPlayer.Maui.Tests` fehlerfrei; 185 betroffene Tests grün. Keine verbliebenen
Referenzen auf `SourcesPage`/`AppPage.Sources`/`SourcesPageOpenedMessage`/
`CachedSourceIconImage` außerhalb der bewusst angepassten Tests. Keine Memory-Leaks
festgestellt: `Clicked`-Lambda hängt am verworfenen `MenuItem` (Referenzrichtung
MenuItem → AppShell), `ModelChanged`-Subscription teilt die Singleton-Lebensdauer von
`AppShell`/`MvuHost`.
