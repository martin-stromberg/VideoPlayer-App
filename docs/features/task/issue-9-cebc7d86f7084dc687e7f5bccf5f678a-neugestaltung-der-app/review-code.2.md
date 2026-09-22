# Code-Review (Runde 2)

## Ergebnis

**Status:** Keine Befunde

## Nachprüfung der Vor-Befunde (review-code.1.md)

Alle vier Befunde der ersten Runde sind korrekt behoben:

- **Race im Model-Snapshot** — behoben: `OnModelChanged` (`AppShell.xaml.cs:101-108`) marshalt
  jetzt `UpdateSourceMenuItemsSafe` auf den Main-Thread; das Model wird erst im Callback
  gelesen (`UpdateSourceMenuItems(mvuHost.Model)`, Z.114) — kein Snapshot-Capture mehr im
  Event-Handler. Jeder gequeue Rendervorgang nutzt den aktuellen Stand; `SameSources`
  dedupliziert.
- **Fehlende Erstbefüllung** — behoben: der Konstruktor ruft nach der Subscription einmalig
  `UpdateSourceMenuItemsSafe()` auf — direkt bei `MainThread.IsMainThread`, sonst gemarshallt
  (`AppShell.xaml.cs:65-76`, kommentiert).
- **renderedSources-Reihenfolge + fehlendes try/catch** — behoben: `UpdateSourceMenuItemsSafe`
  (`Z.110-120`) kapselt den UI-Callback in `try/catch` mit `Debug.WriteLine` (Muster des
  übrigen Files); `renderedSources`/`renderedHasError` werden erst nach erfolgreichem
  `InsertSourceMenuItems` gesetzt (`Z.137-142`). Zusätzlich verifiziert: Schlägt das Einfügen
  fehl, bleiben die Marker auf dem alten — vom neuen Stand abweichenden — Wert, sodass das
  nächste `ModelChanged` den Aufbau erneut versucht; teil-eingefügte Einträge sind über
  `sourceMenuItems` getrackt und werden dabei entfernt (kein gepinnter Halbzustand).
- **Duplizierter Guard-Code** — behoben: `WithSourcesReload` (`AppUpdate.cs:580-592`) kapselt
  Guard (`Connected && !IsLoading`), `IsLoading = true`/`Error = null` und das Anhängen des
  `LoadSourcesEffect`; genutzt in `UpdateSynchronizationCompleted` (Z.542),
  `UpdateCachedListsChanged` (Z.559) und `UpdateSourcesReloadRequested` (Z.738).

## Neuer Code — geprüft, unauffällig

- **`WithSourcesReload`/`UpdateSourcesReloadRequested`** (`AppUpdate.cs:732-742`): Guard- und
  Übergangslogik korrekt; `ReferenceEquals(updated, model)` erkennt den No-op-Fall zuverlässig,
  weil der Helper bei Guard-Fail dieselbe Instanz zurückgibt.
- **`SourcesReloadRequestedMessage`** (`AppMessage.cs:198-202`): XML-Doku vollständig
  (CS1591 ist per `WarningsAsErrors` im csproj Build-Fehler — abgedeckt); Dispatch-Case in
  `Update` vorhanden (Z.82).
- **Retry-MenuItem** (`AppShell.xaml.cs:182-194`): eigener `SourceErrorTemplate`-Stil,
  `Clicked` → `DispatchSourcesReloadAsync` mit `try/catch`+Logging (Z.212-222); der Eintrag
  verschwindet beim Neuversuch (`Error = null` → `hasError`-Wechsel → Neuaufbau).
- **`SetMenuItemTemplate` + implizite Konvertierung** (`Z.172-179`, `187-192`): das Attached
  Property haftet am `MenuItem` und überlebt die Konvertierung; die eingefügte
  `MenuShellItem`-Instanz wird genau einmal erzeugt und identisch in `sourceMenuItems`
  getrackt — `Items.Remove` trifft dieselbe Instanz (Kommentar Z.174-176 korrekt).
- **Erstbefüllung/Threading**: `ModelChanged` feuert in `MvuHost.DispatchAsync` nach
  `gate.Release()` auf dem Dispatch-Thread (`MvuHost.cs:61-62`) — der Marshal ist korrekt und
  notwendig; `Model`-Referenzlesezugriff ist atomar.
- **Memory**: `ModelChanged`-Subscription (Singleton↔Singleton) und `Clicked`-Lambdas
  (MenuItem → AppShell, Einträge werden mit dem Umbau verworfen) — keine Leaks. Icon-Tasks
  halten ihr `MenuItem` nur bis zum Abschluss (transient).
- **Exception-Pfade**: alle Async-Pfade (`DispatchNavigatedAsync`, `DispatchSourcesReloadAsync`,
  `DispatchSourceSelectedAsync`, `LoadSourceIconAsync`, `UpdateSourceMenuItemsSafe`) fangen
  `Exception` und loggen. `LoadSourcesAsync` (`ShellAppEffectHandler.cs:428-444`) dispatcht
  auch im Fehlerfall `SourcesLoadedMessage` → `IsLoading` bleibt nicht hängen.
- **Entfernte Artefakte**: `Pages/SourcesPage.*` und `Controls/CachedSourceIconImage.cs`
  sind gelöscht; keine Referenzen auf `SourcesPage`/`AppPage.Sources`/
  `SourcesPageOpenedMessage`/`CachedSourceIconImage`/`Route="sources"` mehr (grep-clean).
  `AppRoutes`/`AppPage` konsistent auf fünf Menüseiten; `MauiProgram` ohne
  `SourcesPage`-Registrierung.
- **MVU-Übergänge**: `EnterConnected` (Z.435-474) setzt `IsLoading` unter Beibehaltung der
  Einträge und reiht `LoadSourcesEffect` als ersten Effekt ein; Offline-Übergang setzt
  `SourcesOverview = Initial` (Z.496-503); `UpdateSessionExpired` (Z.639-640) und
  `UpdateServerCheckCompleted`-Erfolgspfad (Z.302-303) räumen sitzungs-/serverbezogenen
  Quellenstand ab; `UpdateSourcesLoaded` (Z.708-730) verwirft Antworten außerhalb `Connected`.
- **Serialisierung**: `LoadSourcesEffect` läuft sequenziell im Effekt-Loop und der Handler
  awaited den Ergebnis-Dispatch — überlappende Quellen-Läufe sind durch `IsLoading`-Sperre
  + Sequentialität ausgeschlossen; keine Stale-Response-Problematik.
- **Doku-Konventionen**: XML-Doku aller neuen/angepassten public Member vollständig;
  Namenskonventionen (`Update<Message>`, `Dispatch<...>Async`, `With…`-Helper) eingehalten;
  `TextColor="Red"` des Fehler-Eintrags entspricht der `ErrorLabel`-Konvention der Seiten.
- **Keine** `NotImplementedException`/Stubs/TODOs in den geänderten Dateien.
- Hilfe-Doku (`quellen-browsing-details.md`, `app-grundgeruest.md`, `index.md`,
  `offline-wiederherstellung.md`) ist auf das dynamische Menü umgestellt.

## Hinweise (keine Befunde)

- `UpdateSourcesReloadRequested` erkennt den No-op-Fall über `ReferenceEquals` — korrekt,
  aber implizit an den Rückgabe-Vertrag von `WithSourcesReload` gekoppelt; ein alternatives
  `effects.Count == 0`-Kriterium wäre robuster gegen spätere Helper-Änderungen.
- `ShellAppEffectHandler.LoadSourcesAsync` fängt auch `OperationCanceledException` und meldet
  es als Ladefehler (pre-existing, in diesem Diff unverändert).
- Kleine Testabdeckungs-Lücke (nicht blockierend): `SourcesLoadedMessage` im Zustand
  `Connecting` ist nicht explizit getestet (der Guard verwirft sie korrekt); die Contract-
  Tests pinnen die Retry-/Template-Struktur nur über String-Matching (Repo-Konvention).
- Der manuelle Sichtcheck der `MenuShellItem`-Darstellung (Sektionsüberschrift, Retry-Stil,
  Icons, Flyout-Verhalten) bleibt wie in Runde 1 nicht-automatisierbares Restrisiko.

## Verifikationsnachweis

- Statische Vollprüfung des uncommitteten Stands (alle Feature-Dateien vollständig gelesen).
- Build-/Test-Ausführung durch den koordinierenden Agenten nachgeholt (s. test-results.md /
  Abschlussprotokoll): `dotnet build` + gefilterte Suite.
