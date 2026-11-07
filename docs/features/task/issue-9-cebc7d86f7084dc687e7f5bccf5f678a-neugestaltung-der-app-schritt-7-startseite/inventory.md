# Bestandsaufnahme: Startseite mit gecachten Medienlisten

Analyse des neuen App-Bestands (`VideoPlayer.Mvu`, `VideoPlayer.Maui`,
`VideoPlayer.Storage`, `VideoPlayer.ApiClient`) sowie des Altbestands
(`VideoWebPlayer.Maui` als fachliche UI-/Cache-Referenz) bezogen auf die
Anforderung „Startseite mit gecachten Medienlisten" (Entwicklungsschritt 7).

## Zusammenfassung

- **Startseite:** `VideoPlayer.Maui/Pages/HomePage.xaml` ist eine
  Platzhalterseite (Titel, `OfflineBanner`, Statuszeile, Hinweis „Diese
  Seite wird in einem späteren Entwicklungsschritt gefüllt."). Sie bindet
  an `MvuHost` (`BindingContext`), abonniert `ModelChanged` in
  `OnAppearing`/`OnDisappearing` und aktualisiert die UI über
  `MainThread.BeginInvokeOnMainThread` in `UpdateUi`. **Keine Listen.**
- **Caches sind bereits befüllt:** `ServerSyncService` (Schritt 6)
  schreibt `api/Sources` → `cached_lists`-Schlüssel `sources`,
  `api/items/recent` → `recent`, `api/continue-watching` →
  `continue_watching` (jeweils als JSON in `CachedListEntry`) sowie
  `api/favorites` → Tabelle `cached_favorites` (`CachedFavoritesEntry`,
  Json-Payload je Eintrag). Es fehlt ausschließlich das **Lesen** und das
  Anzeige-Mapping — siehe [inventory/logic.md](inventory/logic.md).
- **Server-Ereignisse sind verdrahtet:** `ServerEventReceivedMessage`
  löst im Zustand `Connected` die Teil-Syncs `SyncContinueWatchingEffect`
  / `SyncFavoritesEffect` / `SyncMetadataEffect` aus. **Lücke:** Die
  Effekte melden keinen Abschluss zurück — die Anzeige erfährt nichts von
  aktualisierten Caches (siehe `ShellAppEffectHandler.RunPartialSyncAsync`).
- **Navigation:** Menüseiten laufen über `AppRoutes`/`NavigateToPageEffect`
  (`//{route}`); Seiten ohne Menüeintrag (`serversetup`, `login`) werden
  per `Routing.RegisterRoute` in `AppShell.xaml.cs` registriert und über
  `Shell.Current.GoToAsync` geschoben. **Es existiert keine Detail-Route**
  — `mediadetails` muss neu angelegt werden (Platzhalter-Seite, Schritt 8
  füllt sie).
- **Vorschaubilder:** `api/pictures/{id}` ist JWT-geschützt → Abruf nur
  über den angemeldeten `AuthSessionService.Client`
  (`GetPictureAsync` liefert `byte[]`). Es gibt noch keinen Bild-Dienst
  und keinen Datei-Cache; `AppStorageLayout.PreviewsDirectory`
  (`NoBackupDirectory/previews`) ist dafür vorgesehen und wird bereits
  angelegt. Im Altbestand lädt `HomePageViewModel.LoadImageAsync` Bytes
  per Client in `StreamImageSource`.
- **UI-Referenz (Altbestand/Webanwendung):** `VideoWebPlayer.Maui/
  HomePage.xaml` + `Components/MediaCarousel.xaml` — vertikal gestapelte
  horizontale `ScrollView`s mit `BindableLayout.ItemsSource`, Poster-
  `Border` (140×210, `RoundRectangle 8`, `Shadow`), Titelzeile
  „Weiterschauen"/„Favoriten"/„Neu im Programm", `TapGestureRecognizer`
  `OnItemTapped` → Detail-Navigation nach Medientyp
  (`GetEntryDetails` in `HomePageViewModel`: Episode/Season → Show,
  Movie mit Collection → Collection, sonst eigenes Medium).
  `ElementCacheService`+`CarouselCache` ist der Altbestand-Pendant zum
  neuen `cached_lists`-Mechanismus (nicht wiederverwendbar — neue
  Ablage).
- **API-Modelle:** `ContinueWatchingEntry` (`MediaType`, `Entry`,
  `PositionSeconds`, `DurationSeconds`, `Title`, `PosterPictureId`),
  `FavoriteEntry` (`Id`, `Entry`, `CreatedAt`), `RecentEntry` (`Id`,
  `Type` (`RecentEntryKind`), `Entry`), polymorphe `MediaEntry`-
  Hierarchie (`$type`-Diskriminator: `movie`, `movieCollection`, `show`,
  `season`, `episode`) — siehe [inventory/models.md](inventory/models.md).
- **MVU-Erweiterungspunkte:** `AppModel` (record, `with`-Updates),
  `AppMessage`/`AppEffect` (records), `AppUpdate.Update` (switch auf
  Messages, reine Funktion), `ShellAppEffectHandler.HandleAsync` (switch
  auf Effekte, Rückdispatch via `DispatchMessageAsync` + `MainThread`).
- **Test-Ausgangszustand:** `dotnet test VideoWebPlayer.Maui.Tests`
  (Release) — **324 Tests, alle erfolgreich**. Kein UI-Automatisierungs-
  Framework; MAUI-Code wird über Contract-Tests auf Quelltext abgesichert
  (`VideoPlayerMauiContractTests`, `MauiSettingsContractTests`),
  SQLite-Zugriffe über reale Temp-Datenbanken (`[Collection("SqliteDatabase")]`).
  Details: [inventory/tests.md](inventory/tests.md).

## Details

- [Modelle und Entitäten](inventory/models.md)
- [Enums](inventory/enums.md)
- [Interfaces](inventory/interfaces.md)
- [Logik / Abläufe (MVU, Sync, Navigation, App-Rand)](inventory/logic.md)
- [Tests — Bestand und Ausgangszustand](inventory/tests.md)
