# Umsetzungsplan: Dynamische Quellen-Einträge im Navigationsmenü

## Übersicht

Das Hauptmenü (Shell-Flyout) der MAUI-App `VideoPlayer.Maui` wird dynamisch: Pro für den
angemeldeten Benutzer freigegebener Quelle (`GET api/Sources`) erscheint ein eigener Eintrag
mit Quellenname und optionalem Icon (`api/sourceicons/{id}`), der per Auswahl direkt die
bestehende `SourceOverviewPage` (Route `sourceoverview?id=…&name=…`) auf den Stack schiebt.
Der statische `FlyoutItem`-Eintrag „Quellen" samt `SourcesPage` entfällt. Die Quellenliste
liegt app-weit in `AppModel.SourcesOverview` und wird beim Übergang in `Connected` sowie
nach (Teil-)Synchronisationen geladen; offline werden keine Quelleneinträge angeboten.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Menütechnik | Zur Laufzeit erzeugte `MenuItem`-Objekte in `Shell.Items`: ein deaktivierter `MenuItem` „Quellen" als Überschrift (analog `NavMenu.razor`) plus je Quelle ein `MenuItem` mit `Text`, `IconImageSource` und `Clicked`-Handler — eingefügt direkt hinter dem FlyoutItem „Startseite" | `MenuItem` konvertiert implizit in `MenuShellItem` (ein `ShellItem`) und wird positionsabhängig im Standard-Flyout gerendert; `IsEnabled = false` erzeugt die deaktivierte Überschrift. `Clicked` → `DispatchAsync(SourceSelectedMessage)` fügt sich exakt in den MVU-Kreislauf: keine Shell-Navigation nötig, der Auswahl-Pfad bleibt der vorhandene Push auf `sourceoverview`. Die Alternative „gruppierte `ShellSection`s unter einem `FlyoutItem` mit `AsMultipleItems`" bräuchte je Quelle einen `ShellContent` mit eigener Seite und würde die Seite wechseln statt zu dispatchen — sie kämpft gegen den bestehenden `NavigateToSourceOverviewEffect`-Push. Ein eigenes `FlyoutContent` ersetzte das komplette Menü samt den fünf statischen Einträgen — unverhältnismäßiger Umbau für die Anforderung. |
| Quellenbestand im Model | Wiederverwendung von `AppModel.SourcesOverview` (`SourcesOverviewState.Sources`) als app-weiter Träger der Menü-Quellen | Der Zustand trägt bereits `Sources`, `IsLoading`, `Attempted`, `Error` — ein zweiter paralleler Bestand würde dieselben Daten doppelt führen. Nach dem Entfall der `SourcesPage` bleibt `SourcesOverview` der Träger (XML-Doku der State-Klasse wird angepasst; Umbenennen nicht erforderlich). |
| Ladeart | Live-Laden über den vorhandenen `LoadSourcesEffect` (`SourceBrowserService.LoadSourcesAsync` → `GET api/Sources`) | Kein neuer Cache-Leser für `cached_lists["sources"]` nötig; das Ergebnis entspricht immer dem Serverstand (Akzeptanzkriterium 1). Da offline ohnehin keine Einträge gezeigt werden (siehe unten), wäre ein Cache-Leser nur für den Online-Modus ein Umweg über JSON-Deserialisierung. |
| Offline-Verhalten | Einträge werden ausgeblendet: `UpdateNetworkAvailabilityChanged` setzt beim Übergang `Connected → Offline` `SourcesOverview` zurück; `UpdateSourcesLoaded` verwirft spät eintreffende Antworten außerhalb von `Connected` | Der Anforderungskatalog sieht offline keine Quellenliste vor; die Zielseite `SourceOverviewPage` lädt ihre Inhalte live und könnte offline ohnehin nichts anzeigen — gecachte Einträge wären tote Menülinks. Entspricht dem bisherigen Verhalten (die `SourcesPage` setzte den Zustand beim Öffnen offline zurück — der Reset wandert in den Offline-Übergang selbst). |
| `SourcesPage` | Entfernen — inklusive `AppPage.Sources`, Route `sources`, `SourcesPageOpenedMessage`/`UpdateSourcesPageOpened`, `CachedSourceIconImage` (einziger Nutzer war die Seite) und der DI-Registrierung | Ohne Menüeintrag hat die Seite keinen Einstiegspunkt mehr: `sources` ist eine reine Menü-Route, keine andere Stelle navigiert dorthin (`NavigateToMessage`/`NavigatedMessage` werden dafür nie ausgelöst). Sie dupliziert zudem exakt die Funktion, die das Menü jetzt selbst übernimmt. Als unerreichbare „Rückfalllösung" wäre sie Totcode inklusive gepflegter Contract-Tests. |
| Ladezeitpunkt | `LoadSourcesEffect` in `EnterConnected` (Login, automatische Anmeldung, Serverwechsel, Wiederverbindung) sowie in `UpdateSynchronizationCompleted` und `UpdateCachedListsChanged` (jeweils nur bei `Connected && !SourcesOverview.IsLoading`) | Geklärt durch Anwender-Vorgabe — kein Neu-Laden bei jedem Flyout-Öffnen. `EnterConnected` deckt Anmeldung und Serverwechsel ab; die Sync-Übergänge bringen Freigabe-/Entzug-Änderungen ohne App-Neustart ins Menü. `RunFullSyncAsync`/`RunPartialSyncAsync` dispatcht die Trigger-Messages garantiert (auch bei Fehlschlag). |
| Doppelte Läufe | `SourcesOverview.IsLoading` als Sperre: `EnterConnected` setzt `IsLoading = true` (mit Beibehaltung der bisherigen Einträge, kein Flackern); die Sync-Übergänge überspringen das Nachladen, solange ein Lauf in der Luft ist | Die Effekte eines Übergangs laufen nacheinander — die Sperre verhindert redundante `api/Sources`-Aufrufe bei sich überlappenden Triggern (z. B. `CachedListsChangedMessage` während des Connect-Laufs). |

## Programmabläufe

### Quellen für das Menü laden (Verbindungsaufbau)

1. `UpdateAutoLoginCompleted`/`UpdateLoginCompleted` (Erfolg) → `EnterConnected`: neben den
   bestehenden Modelländerungen `SourcesOverview = model.SourcesOverview with { IsLoading = true, Error = null }`
   (bisherige Einträge bleiben während des Ladens sichtbar — kein Menü-Flackern) und
   `LoadSourcesEffect` wird als erster Effekt in die Effektliste gestellt (vor
   `ConnectServerEventsEffect`/`RunFullSyncEffect` — die Effekte laufen sequenziell, das
   Menü befüllt sich so früh wie möglich).
2. `ShellAppEffectHandler.LoadSourcesAsync` (unverändert): `SourceBrowserService.LoadSourcesAsync`
   → `SourcesLoadedMessage(sources, error)`; Fehlschlag → Fehlertext, leere Liste.
3. `UpdateSourcesLoaded` (geändert): übernimmt `Sources`/`Error`/`Attempted` nur, wenn
   `model.ConnectionState == ConnectionState.Connected` — sonst `Unchanged` (verspätete
   Antworten dürfen das Offline-/Abgemeldet-Menü nicht wieder befüllen).
4. `MvuHost` löst `ModelChanged` aus → `AppShell` baut die Menüeinträge neu auf.

Beteiligte Klassen/Komponenten: `AppUpdate`, `AppModel`, `SourcesOverviewState`, `LoadSourcesEffect`,
`SourcesLoadedMessage`, `ShellAppEffectHandler`, `SourceBrowserService`, `MvuHost`, `AppShell`.

### Menüeinträge in der Shell aufbauen und aktualisieren

1. `AppShell` (Konstruktor) abonniert `MvuHost.ModelChanged` und erhält `PosterImageService`
   per Konstruktor-Injection (bereits als Singleton registriert).
2. `OnModelChanged` marshalt via `MainThread.BeginInvokeOnMainThread` und vergleicht
   `Model.SourcesOverview.Sources` mit dem zuletzt gerenderten Stand (Sequenz aus
   `Id`/`Name`/`IconPictureId`). Gleicher Stand → Rückkehr ohne Umbau.
3. Bei Änderung: zuvor eingefügte, getrackte `ShellItem`s aus `Items` entfernen
   (`Items.Remove`). Bei leerer Quellenliste endet der Umbau hier (kein Menürest).
4. Bei nichtleerer Liste: ab Index 1 (hinter „Startseite", Position des ehemaligen
   „Quellen"-Eintrags) einfügen — zuerst `MenuItem { Text = "Quellen", IsEnabled = false }`
   als Überschrift, dann je `SourceItem` ein `MenuItem { Text = source.Name }` mit
   `Clicked`-Handler → `mvuHost.DispatchAsync(new SourceSelectedMessage(source))`.
   (Implizite Konvertierung `MenuItem` → `MenuShellItem`; die eingefügten `ShellItem`s
   werden zum späteren Entfernen getrackt.)
5. Icons: für Einträge mit `HasIcon` asynchron
   `PosterImageService.GetSourceIconSourceAsync(IconPictureId)` aufrufen und das Ergebnis
   auf dem Main-Thread als `IconImageSource` setzen; `null`/Fehler → Eintrag ohne Icon
   (Graceful Fallback wie in der Webanwendung).

Beteiligte Klassen/Komponenten: `AppShell`, `MvuHost`, `AppModel.SourcesOverview`,
`SourceItem`, `PosterImageService`, `MenuItem`/`MenuShellItem`, `SourceSelectedMessage`.

### Quellenauswahl aus dem Menü

1. `MenuItem.Clicked` → `SourceSelectedMessage` → `UpdateSourceSelected` (unverändert):
   bei `Source.Id > 0` `SourceBrowser = NewBrowser(id, name)` + Effekte
   `NavigateToSourceOverviewEffect`, `LoadSourceGenresEffect`, `LoadSourceItemsEffect(0)`.
2. `ShellAppEffectHandler.NavigateToSourceOverviewAsync` (unverändert): Push auf
   `sourceoverview?id=…&name=…`.
3. Die `MenuItem`-Aktivierung löst keine Shell-Navigation aus → `OnShellNavigated` feuert
   nicht, `CurrentPage` bleibt auf der darunterliegenden Menüseite (identisch zum
   bisherigen Push-Verhalten von der `SourcesPage`).

Beteiligte Klassen/Komponenten: `AppShell`, `MvuHost`, `AppUpdate.UpdateSourceSelected`,
`SourceBrowserState`, `NavigateToSourceOverviewEffect`, `ShellAppEffectHandler`,
`SourceOverviewPage`.

### Laufzeit-Aktualisierung (Freigabe/Entzug, neue Inhalte)

1. `ServerEventReceivedMessage(NewVideosScanned)` → `UpdateServerEventReceived` (unverändert)
   → `SyncMetadataEffect` → `RunPartialSyncAsync` erneuert `cached_lists["sources"]` und
   dispatcht `CachedListsChangedMessage`.
2. `UpdateCachedListsChanged` (geändert): zusätzlich zum `LoadHomeListsEffect` bei
   `Connected && !SourcesOverview.IsLoading` → `SourcesOverview` mit `IsLoading = true`
   + `LoadSourcesEffect` → Menü folgt dem neuen Quellenstand.
3. `UpdateSynchronizationCompleted` (geändert): zusätzlich zum `LoadHomeListsEffect` bei
   `Connected && !SourcesOverview.IsLoading` → `SourcesOverview` mit `IsLoading = true`
   + `LoadSourcesEffect` (Post-Sync-Refresh; deckt auch den Voll-Sync nach
   `HomeListsLoaded`-`DiscardedCaches` ab).

Beteiligte Klassen/Komponenten: `AppUpdate`, `ServerSyncService.SyncMetadataAsync`,
`CachedListsChangedMessage`, `SynchronizationCompletedMessage`, `LoadSourcesEffect`.

### Offline-Übergang

1. `UpdateNetworkAvailabilityChanged(IsOnline: false)` aus `Connected` → zusätzlich zu
   `ConnectionState = Offline` + `DisconnectServerEventsEffect` wird
   `SourcesOverview = SourcesOverviewState.Initial` gesetzt → das Menü zeigt keine
   Quelleneinträge mehr.
2. Eine in der Luft befindliche `SourcesLoadedMessage` wird von `UpdateSourcesLoaded`
   außerhalb `Connected` verworfen (siehe „Quellen für das Menü laden").

Beteiligte Klassen/Komponenten: `AppUpdate`, `NetworkAvailabilityChangedMessage`,
`SourcesOverviewState`, `AppShell` (Menüabbau über `ModelChanged`).

### Sitzungsende und Serverwechsel

1. `UpdateSessionExpired` (geändert): setzt zusätzlich `SourcesOverview = Initial` und
   `SourceBrowser = SourceBrowserState.Empty` — die Quellen der abgelaufenen Sitzung dürfen
   für den nächsten Benutzer nicht sichtbar bleiben.
2. `UpdateServerCheckCompleted` (Erfolgspfad, geändert): setzt zusätzlich
   `SourcesOverview = Initial` und `SourceBrowser = Empty` — mit dem neuen aktiven Server
   verlieren die Quellen des alten ihre Gültigkeit, bis `EnterConnected` neu lädt.
3. `UpdateLogoutRequested` (unverändert): setzt beide Zustände bereits zurück.

Beteiligte Klassen/Komponenten: `AppUpdate`, `SessionExpiredMessage`,
`ServerCheckCompletedMessage`, `LogoutRequestedMessage`.

## Neue Klassen

Keine — alle Bausteine (`LoadSourcesEffect`, `SourcesLoadedMessage`, `SourceSelectedMessage`,
`SourcesOverviewState`, `SourceItem`, `SourceBrowserService`, `PosterImageService`) existieren
bereits und werden wiederverwendet. Die dynamische Menülogik lebt als Code-Behind in
`AppShell.xaml.cs`.

## Änderungen an bestehenden Klassen

### `AppUpdate` (statische Update-Klasse, `VideoPlayer.Mvu/AppUpdate.cs`)

- **Geänderte Methoden:**
  - `EnterConnected` — `SourcesOverview` mit `IsLoading = true`/`Error = null` belegen
    (bestehende Einträge bleiben während des Ladens sichtbar) und `LoadSourcesEffect` als
    ersten Effekt einreihen. Grund: Menü-Quellen unabhängig vom Öffnen einer Seite laden.
  - `UpdateSynchronizationCompleted` — bei `Connected && !SourcesOverview.IsLoading`
    `SourcesOverview` mit `IsLoading = true` belegen und `LoadSourcesEffect` hinzufügen.
    Grund: Menüstand nach Voll-Sync aktualisieren (Anwender-Vorgabe).
  - `UpdateCachedListsChanged` — bei `Connected && !SourcesOverview.IsLoading`
    `SourcesOverview` mit `IsLoading = true` belegen und `LoadSourcesEffect` hinzufügen.
    Grund: Quellenänderungen nach Teil-Synchronisationen (`SyncMetadataEffect`,
    Server-Ereignisse) ohne Neustart sichtbar machen.
  - `UpdateNetworkAvailabilityChanged` — im Offline-Übergang (`Connected → Offline`)
    `SourcesOverview = SourcesOverviewState.Initial` setzen. Grund: offline keine
    Quellenliste (Rücksetzung wandert von der entfallenen `SourcesPage` hierher).
  - `UpdateSessionExpired` — `SourcesOverview = Initial` und
    `SourceBrowser = SourceBrowserState.Empty` zurücksetzen. Grund: sitzungsbezogene
    Quellen nicht für den nächsten Benutzer stehen lassen (analog `UpdateLogoutRequested`).
  - `UpdateServerCheckCompleted` — im Erfolgspfad `SourcesOverview = Initial` und
    `SourceBrowser = Empty` zurücksetzen. Grund: Serverwechsel invalidiert den
    Quellenbestand, bis der neue Stand geladen ist.
  - `UpdateSourcesLoaded` — Ergebnis nur in `ConnectionState.Connected` übernehmen,
    sonst `AppTransition.Unchanged`. Grund: verspätete Antworten dürfen das Menü im
    Offline-/Abmeldezustand nicht wieder befüllen.
- **Entfernte Methoden:** `UpdateSourcesPageOpened` — entfällt mit `SourcesPageOpenedMessage`.
- **Dispatch-Tabelle `Update`:** den Fall `SourcesPageOpenedMessage` entfernen.

### `AppMessage` (`VideoPlayer.Mvu/AppMessage.cs`)

- **Entfernt:** `SourcesPageOpenedMessage` — einziger Auslöser war die entfallene
  `SourcesPage` (`OnAppearing`/Retry); das Menü lädt zustandsgesteuert.
- Unverändert wiederverwendet: `SourcesLoadedMessage`, `SourceSelectedMessage`,
  `CachedListsChangedMessage`, `SynchronizationCompletedMessage`.

### `AppModel` (`VideoPlayer.Mvu/AppModel.cs`)

- **Geänderte Dokumentation:** `SourcesOverview` — XML-Kommentar anpassen (Träger der
  Quellenliste für das Navigationsmenü; Befüllung zustandsgesteuert statt über eine Seite).
  Keine strukturelle Änderung.

### `SourcesOverviewState` (`VideoPlayer.Mvu/SourcesOverviewState.cs`)

- **Geänderte Dokumentation:** Zweckbeschreibung an die neue Rolle als Menü-Datenquelle
  anpassen. Keine strukturelle Änderung (`IsLoading`, `Attempted`, `Error`, `Sources`,
  `HasSources` u. a. bleiben).

### `AppPage` (`VideoPlayer.Mvu/AppPage.cs`)

- **Entfernt:** Enum-Wert `Sources` — die Quellenliste ist keine Menüseite mehr.

### `AppRoutes` (`VideoPlayer.Mvu/AppRoutes.cs`)

- **Geändert:** `Routes` — Eintrag `[AppPage.Sources] = "sources"` entfernen;
  `MenuPages` — `AppPage.Sources` entfernen (Menüseiten: `Home`, `Favorites`, `Downloads`,
  `Profile`, `Settings`). `TryGetPage`/`ToRoute` bleiben unverändert.

### `AppShell` (`VideoPlayer.Maui/AppShell.xaml`, `AppShell.xaml.cs`)

- **XAML:** `FlyoutItem` „Quellen" (`Route="sources"` → `SourcesPage`) entfernen — es
  bleiben fünf statische `FlyoutItem`s.
- **Code-Behind:**
  - **Geänderter Konstruktor:** Signatur `AppShell(MvuHost, PosterImageService)` —
    Icon-Auflösung der Quelleneinträge.
  - **Neue Felder:** getrackte Liste der dynamisch eingefügten `ShellItem`s sowie der
    zuletzt gerenderte Quellenstand (Änderungsvergleich).
  - **Neuer Event-Handler:** `OnModelChanged` — `MainThread`-Marshal, Vergleich
    `SourcesOverview.Sources` gegen den gerenderten Stand, Neuaufbau bei Änderung.
  - **Neue Methoden:** Aufbau der Quellen-Menüeinträge (Überschrift + Einträge an Position 1,
    `Clicked` → `SourceSelectedMessage`), Entfernen der getrackten Einträge, asynchrones
    Setzen der `IconImageSource` über `GetSourceIconSourceAsync`.
  - `OnShellNavigated` bleibt unverändert (Menüeinträge erzeugen keine Shell-Navigation).

### `MauiProgram` (`VideoPlayer.Maui/MauiProgram.cs`)

- **Entfernt:** `AddTransient<Pages.SourcesPage>()`.
- **Geändert:** `AppShell`-Registrierung löst jetzt zusätzlich `PosterImageService` auf
  (bereits registriert — nur der Konstruktor ändert sich).

### `SourcesPage` (`VideoPlayer.Maui/Pages/SourcesPage.xaml`, `.xaml.cs`)

- **Entfernt:** beide Dateien werden gelöscht (kein Einstiegspunkt mehr; Funktion ersetzt
  durch die dynamischen Menüeinträge).

### `CachedSourceIconImage` (`VideoPlayer.Maui/Controls/CachedSourceIconImage.cs`)

- **Entfernt:** Datei wird gelöscht — einziger Nutzer war `SourcesPage`; für
  `MenuItem.IconImageSource` liefert `PosterImageService` das `ImageSource` direkt.

### `ShellAppEffectHandler` (`VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`)

- Keine Änderung — `LoadSourcesAsync` (`LoadSourcesEffect` → `SourcesLoadedMessage`) wird
  unverändert wiederverwendet.

## Datenbankmigrationen

Keine — der `cached_lists`-Schlüssel `"sources"` (`ServerSyncService.SourcesCacheKey`) wird
weiterhin nur geschrieben; es gibt keine Schemaänderung.

## Validierungsregeln

Keine — die Auswahl läuft über benannte Menüeinträge; die bisherige Regel
(`UpdateSourceSelected` ignoriert `Source.Id <= 0`) bleibt bestehen.

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Zusätzliche Server-Aufrufe:** `UpdateCachedListsChanged` feuert auch nach
  `SyncContinueWatchingEffect`/`SyncFavoritesEffect`/`ToggleFavoriteEffect`/
  `RemoveFavoriteEffect` — jede dieser Nachrichten löst danach ein `GET api/Sources` aus.
  Kleiner, tolerierbarer Mehrverkehr; die `IsLoading`-Sperre verhindert Überlagerungen.
- **Verdoppelter Connect-Lauf:** `EnterConnected` lädt die Quellen direkt und
  `SynchronizationCompletedMessage` lädt nach dem Voll-Sync erneut — bewusst gewählt
  (frühe Anzeige + autorisierter Post-Sync-Stand), beides kleine GET-Aufrufe.
- **Laufzeit-Eingriff in `Shell.Items`:** `MenuShellItem`s ohne `Route` landen in der
  Flyout-Gruppierung im `else`-Ast (`Routing.IsImplicit` ist `false`, da die
  Standard-Route `D_FAULT_…` lautet) und werden positionsabhängig gerendert; die
  Aktivierung führt `MenuItem.Command`/`Clicked` aus ohne Navigation. Plattform-Risiko
  (Darstellung deaktivierter Einträge, Flyout-Verhalten) wird über den Contract-Test und
  den manuellen Sichtcheck abgesichert; der Contract-Test pinnen das Vorhandensein von
  `IsEnabled = false`, `Items.Insert`/`Items.Remove`, `SourceSelectedMessage` und
  `GetSourceIconSourceAsync`.
- **`CurrentPage`-Semantik:** Menü-Tap auf eine Quelle ändert `CurrentPage` nicht
  (Push auf `sourceoverview`) — unverändertes Verhalten wie bisher bei Auswahl aus der
  `SourcesPage`.
- **Entfernung von `AppPage.Sources`:** `AppRoutes.MenuPages` schrumpft auf fünf Einträge —
  die Workflow-Sperren in `UpdateNavigateTo`/`UpdateNavigated` greifen automatisch über
  `MenuPages`; kein Aufrufer benutzt `AppPage.Sources` außerhalb der entfernten Pfade.
- **`cached_lists["sources"]`** wird weiterhin von `SyncMetadataAsync` geschrieben, ohne
  Leser — unverändertes Verhalten, kein Risiko.
- **Hilfe-Dokumentation:** `docs/help/quellen-browsing-details.md`,
  `docs/help/app-grundgeruest.md`, `docs/help/index.md` und
  `docs/help/offline-wiederherstellung.md` beschreiben die „Quellen"-Seite als
  Menüeintrag/Offline-Banner — müssen an das dynamische Menü angepasst werden
  (`README.md` Abschnitt `VideoPlayer.Maui/` ebenfalls; Release Notes nach Rücksprache
  im Prozess).

## Umsetzungsreihenfolge

1. **MVU-Übergänge für das Menü-Laden (`AppUpdate`)**
   - Voraussetzungen: Keine (alle Messages/Effekte/States existieren).
   - Beschreibung: `EnterConnected`, `UpdateSynchronizationCompleted`,
     `UpdateCachedListsChanged`, `UpdateNetworkAvailabilityChanged`,
     `UpdateSessionExpired`, `UpdateServerCheckCompleted` und `UpdateSourcesLoaded`
     wie oben ändern; XML-Doku von `AppModel.SourcesOverview`/`SourcesOverviewState`
     anpassen.

2. **Dynamische Quellen-Einträge in der Shell (`AppShell.xaml`/`.xaml.cs`, `MauiProgram`)**
   - Voraussetzungen: Schritt 1 (gefüllter `SourcesOverview`-Stand);
     `PosterImageService` ist bereits als Singleton registriert.
   - Beschreibung: statischen `FlyoutItem` „Quellen" aus `AppShell.xaml` entfernen;
     `AppShell.xaml.cs` um `PosterImageService`-Injection, `ModelChanged`-Abonnement und
     den Aufbau der `MenuItem`-Gruppe (deaktivierte „Quellen"-Überschrift + Einträge mit
     `Clicked` → `SourceSelectedMessage` + Icon-Auflösung) erweitern;
     `AppShell`-Konstruktoraufruf liegt bei DI.

3. **`SourcesPage` und zugehörige Infrastruktur entfernen**
   - Voraussetzungen: Schritt 2 (der einzige Einstiegspunkt ist bereits entfernt).
   - Beschreibung: `Pages/SourcesPage.xaml`/`.xaml.cs` und
     `Controls/CachedSourceIconImage.cs` löschen; `SourcesPageOpenedMessage` +
     `UpdateSourcesPageOpened` + Dispatch-Case in `AppUpdate` entfernen;
     `AppPage.Sources` und den `AppRoutes`-Eintrag/`MenuPages`-Eintrag entfernen;
     `AddTransient<Pages.SourcesPage>()` in `MauiProgram` entfernen.

4. **Tests anpassen und neue Tests schreiben**
   - Voraussetzungen: Schritte 1–3.
   - Beschreibung: gepinnte Tests auf die neue Menüstruktur/Effektmengen umstellen und
     die neuen Übergangs- und Contract-Tests ergänzen (siehe Abschnitt „Tests").

5. **Hilfe-Dokumentation/README anpassen**
   - Voraussetzungen: Schritte 1–3.
   - Beschreibung: `docs/help/quellen-browsing-details.md` (Quellen-Menü statt
     „Quellen"-Seite), `docs/help/app-grundgeruest.md` (Menüpunkt „Quellen"),
     `docs/help/index.md`, `docs/help/offline-wiederherstellung.md` (Offline-Verhalten des
     Menüs statt Quellen-Seite) und den `VideoPlayer.Maui`-Abschnitt der `README.md`
     anpassen.

6. **Build- und Test-Nachweis**
   - Voraussetzungen: Schritte 1–5.
   - Beschreibung: `dotnet build` in Debug **und** Release (inkl.
     `VideoPlayer.Maui` für `net10.0-windows10.0.19041.0`) sowie die volle Testsuite
     `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj
     --configuration Release` — beide fehlerfrei. Zur CI-Parität
     (`.github/workflows/pr-checks.yml`) gehört zusätzlich
     `dotnet format whitespace --verify-no-changes` — Format-Verletzungen werden vor dem
     Abschluss behoben.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `EnterConnected_StartsSourcesLoad` | `AppUpdateTests_Sources` | `AutoLoginCompletedMessage(Succeeded)` → `SourcesOverview.IsLoading` gesetzt (Einträge bleiben erhalten) und `LoadSourcesEffect` unter den Effekten |
| `SynchronizationCompleted_WhenConnected_ReloadsSources` | `AppUpdateTests_Sources` | `SynchronizationCompletedMessage` im `Connected`-Model → `LoadSourcesEffect` zusätzlich zu `LoadHomeListsEffect`; `IsLoading` gesetzt |
| `SynchronizationCompleted_WhileSourcesLoading_NoDuplicate` | `AppUpdateTests_Sources` | bei `SourcesOverview.IsLoading` → kein zweiter `LoadSourcesEffect` |
| `CachedListsChanged_WhenConnected_ReloadsSources` | `AppUpdateTests_Sources` | `CachedListsChangedMessage` im `Connected`-Model → `LoadSourcesEffect` vorhanden |
| `CachedListsChanged_WhileSourcesLoading_NoDuplicate` | `AppUpdateTests_Sources` | laufender Quellen-Lauf → kein `LoadSourcesEffect` |
| `CachedListsChanged_WhenNotConnected_NoSourcesLoad` | `AppUpdateTests_Sources` | `CachedListsChangedMessage` im `Offline`-Model → kein `LoadSourcesEffect` |
| `NetworkLost_FromConnected_ResetsSourcesOverview` | `AppUpdateTests_Offline` | `NetworkAvailabilityChangedMessage(false)` mit befülltem `SourcesOverview` → `SourcesOverview == Initial`, `DisconnectServerEventsEffect` |
| `SessionExpired_ResetsSourcesOverviewAndBrowser` | `AppUpdateTests_Connection` | `SessionExpiredMessage` mit befüllten Quellenzuständen → beide zurückgesetzt |
| `ServerCheckCompleted_Success_ResetsSourcesOverviewAndBrowser` | `AppUpdateTests_Connection` | Erfolgs-Pfad → beide Quellenzustände zurückgesetzt (Serverwechsel-Hygiene) |
| `SourcesLoaded_WhenNotConnected_IsIgnored` | `AppUpdateTests_Sources` | `SourcesLoadedMessage` im `Offline`-/`NeedsLogin`-Model → `Unchanged`, keine Übernahme |
| `AppShell_BuildsSourceMenuItems_FromModel` | `SourcesContractTests` (erweitern) | `AppShell.xaml.cs` enthält: `ModelChanged`-Abonnement, `SourcesOverview.Sources`, `MenuItem`, `IsEnabled = false` („Quellen"-Überschrift), `Items.Insert`/`Items.Remove`, `SourceSelectedMessage`, `GetSourceIconSourceAsync` |
| `AppShell_HasNoStaticSourcesFlyoutItem` | `SourcesContractTests` (erweitern) | `AppShell.xaml` enthält kein `FlyoutItem` mit `Route="sources"`/`Title="Quellen"`; genau fünf statische `FlyoutItem`s |
| `MauiProgram_RegistersPosterImageServiceForShell` | `SourcesContractTests` (erweitern) | `MauiProgram.cs` registriert `PosterImageService` und `AppShell`; `AppShell.xaml.cs` nimmt `PosterImageService` im Konstruktor entgegen |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `VideoPlayerMauiContractTests.AppShell_ContainsAllMenuEntries` | `ExpectedMenuEntries` von sechs auf fünf Einträge ohne „Quellen"/`AppPage.Sources` umstellen |
| `VideoPlayerMauiContractTests.AppShell_MenuEntries_BindToExistingPages` | passt sich über die Eintragszahl an (kein fachlicher Eingriff) |
| `AppRoutesTests.MenuPages_ContainsAllSixMenuEntriesInOrder` | `MenuPages` enthält nur noch fünf Seiten; Test umbenennen/anpassen |
| `AppRoutesTests.TryGetPage_ResolvesShellLocations` | `InlineData("//sources/SourcesPage", AppPage.Sources)` entfernen — `//sources` ist fortan kein Treffer (ggf. als `UnknownLocation`-Fall) |
| `AppUpdateTests_Connection.Navigated_MenuPageWhileWorkflowPending_NavigatesBack` | benutzt `AppPage.Sources` als Menüseite → auf eine verbleibende Menüseite (z. B. `AppPage.Favorites`) umstellen |
| `AppUpdateTests_Offline.SourcesPageOpened_Offline_NoLoadEffect` | `SourcesPageOpenedMessage` entfällt — Test entfernen (Ersatz: `NetworkLost_FromConnected_ResetsSourcesOverview`) |
| `AppUpdateTests_Sources.SourcesPageOpened_*` (4 Tests) | Message/Übergang entfällt — Tests entfernen |
| `AppUpdateTests_HomeLists.CachedListsChanged_ReloadsHomeLists` (Z. 79–84) | `Assert.Single(transition.Effects)` → jetzt `LoadHomeListsEffect` **und** `LoadSourcesEffect` — auf `Contains`-Assertions umstellen; zudem bricht `Assert.Same(model, transition.Model)`, da `UpdateCachedListsChanged` nun `SourcesOverview.IsLoading = true` setzt → `Same` entfernen und stattdessen `IsLoading`/`Sources`-Erhalt sowie `LoadSourcesEffect` explizit prüfen |
| `AppUpdateTests_HomeLists.ServerEventReceived_FullSequence_RefreshesLists` | `Assert.Single(reloadTransition.Effects)` → zusätzlicher `LoadSourcesEffect` — auf `Contains` umstellen |
| `AppUpdateTests_NetworkSync.SynchronizationCompleted_ClearsSyncFlag` | `Assert.Single(transition.Effects)` → jetzt zwei Effekte (`LoadHomeListsEffect` + `LoadSourcesEffect`) — auf `Contains` umstellen |
| `AppUpdateTests_NetworkSync.AutoLoginSucceeded_OnMenuPage_ConnectsEventsAndSyncsWithoutNavigation` / `LoginSucceeded_OnLoginPage_NavigatesHomeAndSyncs` | Assertion um `LoadSourcesEffect` ergänzen (pinnt das neue Menü-Laden beim Connect) |
| `AppUpdateTests_Connection.AutoLoginCompleted_Succeeded_ConnectsAndNavigatesHome` (Z. 89–94) | `Assert.Equal(4, transition.Effects.Count)` + Index-Pins `Effects[0..3]` brechen: `LoadSourcesEffect` steht als erster `EnterConnected`-Effekt an Index 0 — Zählung auf 5 erhöhen, `Assert.IsType<LoadSourcesEffect>(Effects[0])` ergänzen und die Index-Pins um 1 verschieben |
| `AppUpdateTests_Connection.LoginCompleted_Succeeded_ConnectsAndNavigatesHome` (Z. 231–236) | identische Anpassung (Zählung 5, `LoadSourcesEffect` an Index 0, Index-Pins verschieben) |
| `MvuHostTests.DispatchAsync_UpdatesModelAndExecutesEffects` (Z. 45–56) | `Assert.Equal(11, handler.Effects.Count)` + Index-Pins brechen: der zusätzliche `LoadSourcesEffect` steht an Index 7 (erster Effekt des `EnterConnected`-Blocks) — Zählung auf 12 erhöhen und die Pins ab Index 7 verschieben (`LoadSourcesEffect` → 7, `ConnectServerEventsEffect` → 8, `RunFullSyncEffect` → 9, `ResumeDownloadsEffect` → 10, `NavigateToPageEffect` → 11) |
| `AppUpdateTests_Offline.Reconnection_FromOffline_RunsFullSyncAndResumesDownloads` | Assertion um `LoadSourcesEffect` ergänzen (optional — `Contains`-basiert bleibt der Test bestehen) |
| `SourcesContractTests.SourcesPage_ShowsSources_WithIcons_AndOfflineHint` / `SourcesPage_DispatchesOpenAndSelection` | `SourcesPage` entfällt — beide Tests entfernen und durch die neuen `AppShell`-Menü-Contracts ersetzen |

### E2E-Tests (primärer Funktionsnachweis)

Es sind keine UI-E2E-Tests planbar: Im Repository existiert keine UI-Automatisierung für die
MAUI-App (keine Playwright-/Appium-Suite), und `VideoPlayer.Maui` kann vom Testprojekt
wegen der MAUI-Zielframeworks nicht referenziert werden — das ist im Bestand dokumentiert
(`inventory/tests.md`, `SourcesContractTests`-Klassenkommentar). Der Benutzerfluss
„Flyout öffnen → Quelle wählen → Titelübersicht" liegt vollständig in der nicht
instanziierbaren MAUI-Schicht. Als Ersatz-Nachweis dienen die etablierten
Quelltext-Contract-Tests (`AppShell_BuildsSourceMenuItems_FromModel` u. a. — sie pinnen
Menüaufbau, Überschrift, Dispatch der `SourceSelectedMessage` und Icon-Auflösung) sowie die
MVU-Übergangstests für die Zustandslogik (Laden, Aktualisieren, Offline-/Sitzungs-Reset,
Auswahl-Pfad `UpdateSourceSelected` ist bereits durch `SourceSelected_NavigatesAndStartsInitialLoad`
gepinnt). Der tatsächliche Sicht-/Laufzeit-Nachweis (Flyout-Darstellung der
`MenuShellItem`s, deaktivierte Überschrift, Icons) erfolgt manuell beim Bauen der App —
dies wird als Restrisiko in „Seiteneffekte und Risiken" geführt.

Keine bestehenden E2E-Tests betroffen (keine vorhanden).

## Offene Punkte

Keine — die vier offenen Fragen der Anforderung (statischer „Quellen"-Eintrag,
Offline-Menü, Menüdarstellung, Ladezeitpunkt) wurden durch die Anwender-Klarstellung
beantwortet und sind als Designentscheidungen eingearbeitet.
