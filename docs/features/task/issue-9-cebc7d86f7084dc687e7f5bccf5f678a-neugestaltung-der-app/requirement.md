# Übersetzte Anforderung: Dynamische Quellen-Einträge im Navigationsmenü

Quelle: Kundenanforderung auf dem Branch `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app`. Referenzimplementierung: Sektion „Quellen" in `NavMenu.razor` der Webanwendung (Serverprojekt, nicht Teil dieses Repositories), die pro freigeschalteter Quelle einen NavLink auf `/mediasource/{id}` mit Quellennamen und optionalem Icon erzeugt; Datenquelle ist `GET api/Sources`.

## Fachliche Zusammenfassung

Das Hauptmenü (Shell-Flyout) der MVU-App `VideoPlayer.Maui` wird dynamisch: Pro für den angemeldeten Benutzer freigegebener Quelle (`GET api/Sources` — liefert nur die freigeschalteten Quellen) erhält das Menü einen eigenen Eintrag mit Quellenname und optionalem Icon (`api/sourceicons/{id}`). Die Auswahl eines Quellen-Eintrags öffnet direkt die Titelübersicht der Quelle — die bestehende `SourceOverviewPage` (Shell-Route `sourceoverview?id=…&name=…`). Der bisherige statische `FlyoutItem`-Eintrag „Quellen" (Route `sources` → `SourcesPage` mit Quellenliste) bleibt erhalten oder wird durch die dynamischen Einträge ersetzt — siehe Offene Fragen.

## Betroffene Klassen und Komponenten

- **MVU-Kern (`VideoPlayer.Mvu`):**
  - `AppModel` — die Quellen müssen app-weit im Model stehen (unabhängig vom Öffnen der `SourcesPage`); vorhandener Zustand `SourcesOverview` (`SourcesOverviewState.Sources`) ist der fachliche Träger, ein eigener Menü-Quellenbestand oder die Wiederverwendung ist eine Entwurfsentscheidung (Annahme).
  - `AppMessage` — bestehende Nachrichten `SourcesLoadedMessage` und `SourceSelectedMessage` sind wiederverwendbar; ggf. neue Nachricht zum Anstoßen des Menü-Ladens (z. B. nach Verbindungsaufbau/Synchronisation).
  - `AppEffect` — `LoadSourcesEffect` ist wiederverwendbar; ggf. neuer Effekt zum Cache-First-Lesen der Quellen aus `cached_lists` (Schlüssel `ServerSyncService.SourcesCacheKey` = `"sources"` — wird bereits von `SyncMetadataAsync` geschrieben).
  - `AppUpdate` — neue/geänderte Übergänge: Quellen für das Menü laden beim Übergang in `ConnectionState.Connected` (`EnterConnected`) bzw. nach `SynchronizationCompletedMessage`; Rücksetzen der Menü-Einträge im Offline-Übergang (analog `UpdateSourcesPageOpened`); Auswahl aus dem Menü über den bestehenden `UpdateSourceSelected`-Pfad (`NavigateToSourceOverviewEffect` + `LoadSourceGenresEffect` + `LoadSourceItemsEffect`).
  - `SourceItem` — enthält bereits `Id`, `Name`, `IconPictureId`; ausreichend für die Menüdarstellung.
  - `SourceBrowserService.LoadSourcesAsync` — Wiederverwendung (`GET api/Sources` über `AuthSessionService.Client`).
  - `AppRoutes`/`NavigatedMessage` — `sourceoverview` ist keine Menü-Route; `AppRoutes.TryGetPage` liefert keinen Treffer, `CurrentPage` bleibt unverändert (entspricht dem bisherigen Push-Verhalten — keine Änderung nötig).
- **App-Rand (`VideoPlayer.Maui`):**
  - `AppShell.xaml`/`AppShell.xaml.cs` — Aufbau der dynamischen Menüeinträge. Technische Optionen: (a) `MenuItem`-Objekte zur Laufzeit hinzufügen (`Text` = Quellenname, `IconImageSource`, `Command`/Clicked → Dispatch `SourceSelectedMessage`) — werden vom Standard-Flyout jedoch hinter allen `FlyoutItem`s gerendert; (b) `FlyoutItem` „Quellen" mit dynamischen `ShellSection`-Einträgen (`FlyoutDisplayOptions.AsMultipleItems`) als gruppierte Sektion wie in der Webanwendung; (c) eigenes `FlyoutContent`/`FlyoutContentTemplate`. Die Shell abonniert `MvuHost.ModelChanged` und baut die Einträge bei Änderung der Quellenliste neu auf.
  - `Pages/SourcesPage.xaml(.cs)` — bleibt je nach Entscheidung als Übersichtsseite bestehen oder entfällt (inkl. `AppPage.Sources`, `AppRoutes`-Eintrag, `SourcesOverviewState`, zugehörige Messages/Übergänge).
  - `Pages/SourceOverviewPage.xaml(.cs)` — unverändertes Navigationsziel der Menüeinträge.
  - `Controls/CachedSourceIconImage` (View, bindet `PictureId`) bzw. `Services/PosterImageService.GetSourceIconSourceAsync` (liefert `ImageSource`) — Icon-Auflösung über `api/sourceicons/{id}`; für `MenuItem.IconImageSource`/`FlyoutItem.Icon` ist ein `ImageSource` erforderlich, das vorhandene Control ist nicht direkt wiederverwendbar.
  - `Services/ShellAppEffectHandler` — Ausführung des Lade-Effekts für die Menü-Quellen (Rückdispatch der Ergebnis-Message).
  - `MauiProgram` — ggf. DI-Registrierung eines Lesedienstes für die gecachten Quellen (Muster `HomeListService`).
- **Ablage (`VideoPlayer.Storage`):**
  - Keine Schemaänderung — `cached_lists`-Schlüssel `sources` existiert bereits.
- **Tests (`VideoWebPlayer.Maui.Tests`):**
  - `VideoPlayerMauiContractTests.AppShell_ContainsAllMenuEntries` — pinnt aktuell exakt sechs `FlyoutItem`s inkl. „Quellen"/`sources`; an die neue Menüstruktur anzupassen.
  - `SourcesContractTests` — Verdrahtung des dynamischen Menüs (Shell-Aufbau, Dispatch der Auswahl-Message, Icon-Bindung).
  - `AppUpdateTests_Sources` — neue Übergänge: Menü-Quellen beim Verbindungsaufbau/nach Sync laden, Auswahl aus dem Menü (Navigation + Browser-Initialisierung), Offline-Verhalten.

## Implementierungsansatz

- Die Quellen für das Menü werden unabhängig vom Öffnen der `SourcesPage` geladen: beim Übergang in `ConnectionState.Connected` (`EnterConnected` stößt bereits `RunFullSyncEffect` an, dessen `SyncMetadataAsync` die Quellen in `cached_lists["sources"]` schreibt) bzw. nach `SynchronizationCompletedMessage`; Cache-First-Anzeige über den vorhandenen Cache-Schlüssel ist möglich (Muster `HomeListService`). Ein Ladefehlschlag ergibt ein Menü ohne Quelleneinträge — kein Abbruch.
- Die Menüeinträge werden aus dem `AppModel` abgeleitet: die `AppShell` reagiert auf `MvuHost.ModelChanged` und baut die dynamischen Einträge neu auf, sobald sich die Quellenliste ändert.
- Die Auswahl eines Menüeintrags dispatcht `SourceSelectedMessage` — der bestehende Ablauf (Browser-Zustand initialisieren, `sourceoverview?id=…&name=…` auf den Stack schieben, Genres und erste Eintragsseite laden) wird unverändert wiederverwendet.
- Aktualisierung zur Laufzeit: das Server-Ereignis `ServerEventKind.NewVideosScanned` löst bereits `SyncMetadataEffect` aus (erneuert den Quellen-Cache); danach ist der Menüstand zu aktualisieren (Annahme: über den bestehenden `CachedListsChangedMessage`-Mechanismus bzw. das Neu-Lesen des Cache-Schlüssels).
- Icons werden über `SourceItem.IconPictureId` → `api/sourceicons/{id}` aufgelöst (`PosterImageService.GetSourceIconSourceAsync`); Quellen ohne Icon erscheinen ohne Bild — wie in der Webanwendung.
- **Offline-Verhalten:** Per Anforderungskatalog steht offline keine Quellenliste zur Verfügung (Abschnitt 12.3 des Issue-Katalogs; `SourcesPage` zeigt einen Hinweis) — die dynamischen Menüeinträge werden im Offline-Zustand nicht angezeigt bzw. auf einen definierten Zustand zurückgesetzt (Annahme — siehe Offene Fragen).
- **Auswahl-/Identifikationsanforderungen:** Quellen werden ausschließlich über benannte, sichtbare Menüeinträge ausgewählt — keine Eingabe interner Kennungen.

## Konfiguration

Keine neuen Konfigurationseinträge. Serveradresse und Anmeldung kommen aus dem bestehenden Verbindungs-Workflow (`AuthSessionService`); die Quellenmenge ergibt sich allein aus der Server-API.

## Akzeptanzkriterien

1. Das Navigationsmenü enthält pro für den Benutzer freigegebener Quelle einen eigenen Eintrag mit Quellenname und — sofern vorhanden — Icon (`api/sourceicons/{id}`); die Daten entsprechen `GET api/Sources` des angemeldeten Benutzers.
2. Die Auswahl eines Quellen-Eintrags öffnet direkt die Titelübersicht der Quelle (`SourceOverviewPage`, Route `sourceoverview`); Genre-Filter, Suche und seitenweises Nachladen funktionieren wie beim bisherigen Weg über die Quellenliste.
3. Änderungen der freigegebenen Quellen (Freigabe/Entzug am Server) werden ohne App-Neustart im Menü sichtbar — nach (Wieder-)Verbindung bzw. Synchronisation.
4. Im Offline-Zustand zeigt das Menü definiertes Verhalten (keine/veraltete Quelleneinträge führen nicht zu Fehlern oder Abstürzen).
5. Die übrigen Menüeinträge (Startseite, Favoriten, Downloads, Profil, Einstellungen) bleiben erhalten und funktionsfähig.

## Offene Fragen

1. **Statischer Eintrag „Quellen":** Bleibt der bisherige Menüeintrag mit der Übersichtsseite `SourcesPage` zusätzlich bestehen („nicht nur …, sondern …") oder wird er durch die dynamischen Einträge ersetzt? Die Webanwendung kennt nur die Sektion mit Einträgen je Quelle, keine separate Übersichtsseite im Menü.
2. **Offline-Menü:** Werden die dynamischen Einträge offline ausgeblendet (konsistent zu „keine Quellenliste") oder aus dem Cache (`cached_lists["sources"]`) angezeigt — wobei die `SourceOverviewPage` ihre Inhalte live lädt und offline ohnehin keine Einträge liefern kann?
3. **Menüdarstellung:** Soll die Quellengruppe als eigene Sektion mit Überschrift „Quellen" erscheinen (wie die Sektion in `NavMenu.razor`) oder sollen die Einträge flach in die Menüliste eingereiht werden? Das beeinflusst die Shell-Umsetzung (`MenuItem` vs. gruppierte `ShellSection`s vs. `FlyoutContent`).
4. **Ladezeitpunkt:** Reicht das Laden beim Verbindungsaufbau plus Aktualisierung über Server-Ereignisse/Synchronisation, oder soll die Liste zusätzlich bei jedem Öffnen des Flyouts (neu) vom Server geladen werden (Live-Stand wie auf der `SourcesPage`)?
