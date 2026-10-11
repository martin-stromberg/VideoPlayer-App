# Umsetzungsplan: Kontextmenü in der Weiterschauen-Liste (Ausblenden / Überspringen)

## Übersicht

Die Weiterschauen-Liste der Startseite erhält ein Kontextmenü: 2-s-Long-Press auf einen Eintrag öffnet ein `DisplayActionSheet` mit „Ausblenden" und „Überspringen"; die Auswahl läuft über neue MVU-Nachrichten und -Effekte an die bisher ungenutzten Server-Endpunkte `POST api/continue-watching/hide` bzw. `…/skip`. Die Listen-Aktualisierung erfolgt online über `SyncContinueWatchingAsync` + `CachedListsChangedMessage`, offline über lokales Entfernen aus dem `cached_lists`-Eintrag `continue_watching`. Betroffen: `MediaCarousel`/`HomePage` (UI), `AppMessage`/`AppEffect`/`AppUpdate`/`ShellAppEffectHandler` (MVU), neuer `ContinueWatchingMutationService`, Developer-API (`/invoke` um `gesture`), `MockVideoServer` (hide/skip-Routen) sowie Unit-/Contract-/E2E-Tests.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Long-Press-Erkennung | `PointerGestureRecognizer` (`PointerPressed`/`PointerReleased`/`PointerExited`) + 2-s-`IDispatcherTimer` im `MediaCarousel`-Code-Behind; `LongPressEnabled`-BindableProperty (Default `false`) + `ItemLongPressed`-Ereignis | `CommunityToolkit.Maui` (`mct:TouchBehavior`) ist in `VideoPlayer.Maui` bewusst nicht referenziert — kein neues Paket für eine einzelne Geste; die .NET-MAUI-Bordmittel erfüllen die 2-s-Anforderung identisch zum Altbestand-Muster. |
| Menüdarstellung | `DisplayActionSheet` auf `HomePage` (Titel = Eintragstitel, „Abbrechen", „Ausblenden", „Überspringen") | Konvention des Altbestands (`MainPage.xaml.cs`, `ServerSetupPage.xaml.cs`); nativer Dialog auf allen Plattformen; keine internen Kennungen als Eingabe — der Eintrag ist per Long-Press bereits bestimmt. |
| MVU-Modellierung | Getrennte Nachrichten/Effekte je Aktion (`ContinueWatchingHideRequestedMessage`/`ContinueWatchingSkipRequestedMessage`, `HideContinueWatchingEntryEffect`/`SkipContinueWatchingEntryEffect`) | Folgt `FavoriteRemoveRequestedMessage` → `RemoveFavoriteEffect`; kein neuer Aktions-Enum nötig (kein `EnumCoverageTests`-Eingriff). |
| Serveraufruf + Cache | Neuer `ContinueWatchingMutationService` (Mvu): online `HideContinueWatchingEntryAsync`/`SkipContinueWatchingEntryAsync` + `SyncContinueWatchingAsync`; ohne Client `RemoveCachedEntryAsync` am `continue_watching`-Cache-Eintrag | Service-Layer-Muster von `FavoritesService` (Server führend, Cache autoritativ erneuert); Offline-Pfad exakt wie gefordert als lokales Entfernen. |
| `mediaType` an hide/skip | `item.MediaType` unverändert weitergeben (Server-Konvention der Weiterschauen-Liste) | Der Server liefert den Typ in der Liste; die Mutation referenziert denselben Eintrag — keine erneute Übersetzung erfinden. |
| Fehlschlag der Mutation | Stilles Scheitern mit Debug-Log; `CachedListsChangedMessage` nur bei Erfolg (Eintrag bleibt sichtbar) | „bei Erfolg verschwindet der Eintrag" — kein Fehler-UX gefordert; hält die MVU-Oberfläche minimal. |
| E2E-Auslösung des Long-Press | `POST /invoke` um optionales Feld `gesture: "longpress"`; `DevApiVisualTree` ruft per Reflection `SendItemLongPressed(int)` auf dem Zielelement (Konvention wie `SendTapped`) | Carousel-Einträge fehlen im UIA-Baum; echte Maus-Halteeingaben sind in diesem Repo bewusst vermieden (Koordinatenklick-Konvention). Dieselbe App-seitige Ereigniskette wie der Timer-Auslauf. |
| Menü-Beschränkung | `LongPressEnabled` nur am Weiterschauen-Karussell gesetzt; `ItemLongPressed` nur dort verdrahtet | AK 4 — keine Ausweitung auf andere Listen, explizit und contract-testbar. |

## Programmabläufe

### Long-Press öffnet Kontextmenü

1. `PointerGestureRecognizer` am Karten-`Border` meldet `PointerPressed` → `MediaCarousel` startet (nur bei `LongPressEnabled`) einen 2-s-`IDispatcherTimer` und merkt Eintrag+Ansicht.
2. `PointerReleased`/`PointerExited` vor Ablauf → Timer verworfen (kurzer Tap bleibt beim `TapGestureRecognizer` → `ItemTapped` → `ContinueWatchingItemSelectedMessage`, unverändert).
3. Timer läuft ab → `ItemLongPressed` mit dem `HomeMediaItem` aus dem `BindingContext` der gedrückten Ansicht.
4. `HomePage.OnContinueWatchingItemLongPressed` (nur Weiterschauen-Karussell verdrahtet) zeigt `DisplayActionSheet(item.Title, "Abbrechen", null, "Ausblenden", "Überspringen")`.
5. Auswahl „Ausblenden" → `mvuHost.DispatchAsync(new ContinueWatchingHideRequestedMessage(item))`; „Überspringen" → `ContinueWatchingSkipRequestedMessage(item)`. „Abbrechen" → keine Nachricht.

Beteiligte Klassen/Komponenten: `MediaCarousel`, `HomePage`, `MvuHost`.

### Ausblenden/Überspringen (online)

1. `AppUpdate` auf `ContinueWatchingHideRequestedMessage` → Guards (`Id > 0`, `MediaType` nicht leer, `IsSelectable`) → `HideContinueWatchingEntryEffect(item.MediaType, item.Id)` (analog für Skip).
2. `ShellAppEffectHandler` → `ContinueWatchingMutationService.HideAsync(mediaType, mediaId, ct)` → `client.HideContinueWatchingEntryAsync` → `ServerSyncService.SyncContinueWatchingAsync` (schreibt `cached_lists`/`continue_watching` vom Serverstand).
3. Bei Erfolg dispatcht der Handler `CachedListsChangedMessage` → `LoadHomeListsEffect` → `HomeListsLoadedMessage` → `Model.ContinueWatching` aktualisiert → Eintrag verschwindet aus der Anzeige.

Beteiligte Klassen/Komponenten: `AppUpdate`, `ShellAppEffectHandler`, `ContinueWatchingMutationService`, `ServerSyncService`, `VideoPlayerApiClient`.

### Ausblenden/Überspringen (offline / ohne Client)

1. Wie oben bis `ContinueWatchingMutationService`.
2. `authSession.Client` ist `null` → `RemoveCachedEntryAsync(mediaType, mediaId)`: `cached_lists`-Zeile `continue_watching` lesen, `List<ContinueWatchingEntry>` deserialisieren, Eintrag mit passender `Entry.Id` (Typvergleich über `MediaTypeNames.Normalize`) entfernen, JSON zurückschreiben.
3. Erfolg → `CachedListsChangedMessage` → Anzeige aktualisiert aus dem Cache.

Beteiligte Klassen/Komponenten: `ContinueWatchingMutationService`, `CacheDatabase`, `CachedListEntry`.

### E2E-Auslösung (Developer-API)

1. `POST /invoke` mit `{ automationId: "ContinueWatchingCarousel", itemIndex: n, gesture: "longpress" }`.
2. `MauiDevApiService` reicht `gesture` an `DevApiVisualTree.TrySelectItem`; bei `"longpress"` wird `SendItemLongPressed(itemIndex)` auf dem Zielelement (`MediaCarousel`) per Reflection aufgerufen — dieselbe Ereigniskette wie der Timer-Auslauf.
3. `MediaCarousel.SendItemLongPressed` löst `ItemLongPressed` mit `Items[itemIndex]` aus → Menü → `DisplayActionSheet`; der Test wählt die Aktion per FlaUI über den Klartext-Namen.

Beteiligte Klassen/Komponenten: `MauiDevApiService`, `DevApiVisualTree`, `MediaCarousel`, `DevApiClient`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `ContinueWatchingHideRequestedMessage` | `AppMessage`-Record (sealed) | „Ausblenden" im Kontextmenü gewählt (trägt `HomeMediaItem Item`). |
| `ContinueWatchingSkipRequestedMessage` | `AppMessage`-Record (sealed) | „Überspringen" gewählt (trägt `HomeMediaItem Item`). |
| `HideContinueWatchingEntryEffect` | `AppEffect`-Record (sealed) | Hide-Aufruf (`string MediaType`, `long MediaId`). |
| `SkipContinueWatchingEntryEffect` | `AppEffect`-Record (sealed) | Skip-Aufruf (`string MediaType`, `long MediaId`). |
| `ContinueWatchingMutationService` | Klasse (`VideoPlayer.Mvu`, sealed) | Hide/Skip gegen den Server + Cache-Erneuerung; offline lokales Entfernen aus `cached_lists`/`continue_watching`. |
| `ContinueWatchingMutationServiceTests` | Testklasse | Online-/Offline-Verhalten des Dienstes (Muster `FavoritesServiceTests`). |
| `AppUpdateTests_ContinueWatchingActions` | Testklasse | Übergänge der neuen Nachrichten inkl. Guards. |
| `ContinueWatchingContextMenuContractTests` | Testklasse | Quelltext-Verträge der Long-Press-/Menü-Verdrahtung und Effekt-Behandlung. |
| `ContinueWatchingContextMenuTests` | E2E-Testklasse | Benutzerfluss Long-Press → Menü → Hide/Skip/Abbrechen. |

## Änderungen an bestehenden Klassen

### `MediaCarousel` (`VideoPlayer.Maui/Controls/MediaCarousel.xaml(.cs)`)

- **Neue Bindable Property:** `LongPressEnabled` (`bool`, Default `false`) — schaltet die Long-Press-Erkennung frei.
- **Neues Event:** `ItemLongPressed` (`EventHandler<HomeMediaItem>`) — Eintrag wurde 2 s gehalten.
- **Neue Handler (XAML-Element `PointerGestureRecognizer` an der Karten-`Border`):** `OnItemPointerPressed` (Timer starten), `OnItemPointerReleased`/`OnItemPointerExited` (Timer verwerfen).
- **Neue interne Methode:** `SendItemLongPressed(int itemIndex)` — Developer-API-Auslöser; löst `ItemLongPressed` für `Items[itemIndex]` aus (identische Kette wie Timer-Ablauf).
- **Neue Felder:** `IDispatcherTimer?` + angehaltener Eintrag/Ansicht.

### `HomePage` (`VideoPlayer.Maui/Pages/HomePage.xaml(.cs)`)

- **Geändertes Markup:** am Weiterschauen-`MediaCarousel` zusätzlich `LongPressEnabled="True"` und `ItemLongPressed="OnContinueWatchingItemLongPressed"`.
- **Neuer Handler:** `OnContinueWatchingItemLongPressed` — `DisplayActionSheet` mit „Abbrechen", „Ausblenden", „Überspringen"; Dispatch der beiden Request-Nachrichten; Guard auf `item.IsSelectable`.

### `AppMessage` (`VideoPlayer.Mvu/AppMessage.cs`)

- **Neue Records:** `ContinueWatchingHideRequestedMessage(HomeMediaItem Item)`, `ContinueWatchingSkipRequestedMessage(HomeMediaItem Item)` mit deutscher XML-Doku.

### `AppEffect` (`VideoPlayer.Mvu/AppEffect.cs`)

- **Neue Records:** `HideContinueWatchingEntryEffect(string MediaType, long MediaId)`, `SkipContinueWatchingEntryEffect(string MediaType, long MediaId)` mit deutscher XML-Doku.

### `AppUpdate` (`VideoPlayer.Mvu/AppUpdate.cs`)

- **Neue Switch-Zweige:** beide Request-Nachrichten → je eine Übergangsfunktion mit den Guards aus `UpdateContinueWatchingItemSelected` (`Id > 0`, `MediaType` nicht leer, `IsSelectable`); Ergebnis = Modell unverändert + ein Mutations-Effekt.

### `ShellAppEffectHandler` (`VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`)

- **Neue Switch-Zweige:** `HideContinueWatchingEntryEffect` → `MutateContinueWatchingAsync(s => s.HideAsync(...))`; `SkipContinueWatchingEntryEffect` analog.
- **Neue Methode:** `MutateContinueWatchingAsync(Func<ContinueWatchingMutationService, Task<bool>>)` — Erfolg → `CachedListsChangedMessage`, Fehlschlag/Exception → `Debug.WriteLine` (Eintrag bleibt).

### `MauiProgram` (`VideoPlayer.Maui/MauiProgram.cs`)

- **Registrierung:** `builder.Services.AddSingleton<ContinueWatchingMutationService>()`.

### `MauiDevApiService` (`msTools.DeveloperApi.Maui/MauiDevApiService.cs`)

- **`InvokeBody`:** neues optionales Feld `Gesture` (`string?`, Default `"tap"`); `InvokeOnPage` reicht es an `DevApiVisualTree.TrySelectItem` weiter.

### `DevApiVisualTree` (`msTools.DeveloperApi.Maui/DevApiVisualTree.cs`)

- **Geänderte Methode:** `TrySelectItem(Element, int, string? gesture, out string? error)` — bei `"longpress"` `SendItemLongPressed`-Konvention: Reflection-Invoke der gleichnamigen Methode (1 × `int`-Parameter) auf dem Zielelement; Fehlertexte analog der bisherigen Fehlerfälle. Unbekannte Gestenwerte → Fehler.

### `MockVideoServer` (`VideoPlayer.E2E.Tests/Support/MockVideoServer.cs`)

- **Neue Routen:** `POST /api/continue-watching/hide` und `…/skip` (Gate + Bearer wie `/progress`): Body `(mediaType, mediaId)` lesen, in `RemovedContinueWatchingEntries`-Menge aufnehmen, Antwort `{"status":"hidden","message":"ok"}` bzw. `{"status":"skipped","message":"ok"}`.
- **Neue Diagnose-Properties:** `HideContinueWatchingCount`, `SkipContinueWatchingCount`, `RemovedContinueWatchingEntries` (gepaarte `mediaType`/`mediaId`).
- **Geänderte Methode:** `ListOverride` für `/api/continue-watching` — entfernte Einträge nicht mehr liefern.

### `UiDriver` (`VideoPlayer.E2E.Tests/Support/UiDriver.cs`)

- **Neue Methode:** `ClickByName(name, timeout)` — Element per Name mit Retry suchen (Fenster-Descendants, danach Desktop-Descendants des App-Prozesses für Dialog-Popups) und per Invoke/Click auslösen. Für die `DisplayActionSheet`-Schaltflächen.

### `DevApiClient` (`VideoPlayer.E2E.Tests/Support/DevApiClient.cs`)

- **Neue Methode:** `LongPressAsync(automationId, itemIndex, ct)` — `POST /invoke` mit `gesture = "longpress"`.

## Datenbankmigrationen

Keine — der `cached_lists`-Eintrag wird inhaltlich verändert, nicht strukturell.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `HomeMediaItem` bei Hide/Skip-Request | `Id > 0`, `MediaType` nicht leer, `IsSelectable` | Übergang wirkungslos (`AppTransition.Unchanged`). |
| `/invoke`-`gesture` | nur `"tap"` (Default) oder `"longpress"` | HTTP-Fehlerantwort mit deutschem Fehlertext. |
| `SendItemLongPressed`-`itemIndex` | `0 ≤ index < Items.Count` | Kein Ereignis (DevApi meldet Erfolg der Auslösung nur bei gültigem Index; bei ungültigem Index Fehlertext). |

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Kurzer Tap:** Der `PointerGestureRecognizer` ergänzt den `TapGestureRecognizer` — bei Loslassen vor 2 s wird der Timer verworfen; das bestehende Direktstart-Verhalten (`ContinueWatchingItemSelectedMessage`) bleibt unverändert. Risiko: plattformspezifische Gesten-Konkurrenz (Tap vs. Pointer) — mitigiert durch die E2E-Tap-Regression der `ContinueWatchingPlaybackTests`.
- **Offline-Liste:** Die Offline-Weiterschauen-Liste wird aus dem Download-Manifest gebildet, nicht aus `continue_watching` — das lokale Entfernen wirkt auf den Cache-Eintrag (wie gefordert); ein Download-Manifest-Eintrag bliebe sichtbar. Als bekannte, anforderungsgemäße Grenze dokumentiert.
- **DevApi-Vertrag:** `gesture` ist optional abwärtskompatibel; `DevApiContractTests` wird um die neue Fähigkeit ergänzt.
- **Doppelte Auslösung:** Mehrfach-Hide/Skip ohne Busy-Guard — der Server ist idempotent tolerant (Endpunkt-Semantik); UI-seitig schließt das `DisplayActionSheet` nach der Auswahl.

## Umsetzungsreihenfolge

1. **MVU-Nachrichten und -Effekte** — `AppMessage.cs`, `AppEffect.cs` erweitern.
   - Voraussetzungen: Keine.
2. **`ContinueWatchingMutationService`** (neu) + Registrierung in `MauiProgram`.
   - Voraussetzungen: Schritt 1 (Namensbezug), `CacheDatabase`/`ServerSyncService`/`AuthSessionService` vorhanden.
3. **`AppUpdate`-Switch-Zweige + Übergangsfunktionen.**
   - Voraussetzungen: Schritt 1.
4. **`ShellAppEffectHandler`-Zweige + `MutateContinueWatchingAsync`.**
   - Voraussetzungen: Schritte 1–2.
5. **`MediaCarousel`: `LongPressEnabled`, `ItemLongPressed`, Pointer-Timer, `SendItemLongPressed`.**
   - Voraussetzungen: Keine.
6. **`HomePage`: Verdrahtung + `DisplayActionSheet` + Dispatch.**
   - Voraussetzungen: Schritte 1, 5.
7. **Developer-API: `gesture`-Feld + `TrySelectItem`-Long-Press-Zweig.**
   - Voraussetzungen: Schritt 5 (`SendItemLongPressed`-Konvention).
8. **`MockVideoServer`: hide/skip-Routen + Listen-Verhalten + Diagnose.**
   - Voraussetzungen: Keine.
9. **`DevApiClient.LongPressAsync` + `UiDriver.ClickByName`.**
   - Voraussetzungen: Schritte 7–8.
10. **Unit-Tests:** `AppUpdateTests_ContinueWatchingActions`, `ContinueWatchingMutationServiceTests`.
    - Voraussetzungen: Schritte 1–4.
11. **Contract-Tests:** `ContinueWatchingContextMenuContractTests` (+ `DevApiContractTests`-Ergänzung falls erforderlich).
    - Voraussetzungen: Schritte 4–7.
12. **E2E-Tests:** `ContinueWatchingContextMenuTests`.
    - Voraussetzungen: Schritte 5–9.
13. **Build + vollständige Testläufe** (Unit + E2E).
    - Voraussetzungen: Schritte 1–12.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `ContinueWatchingHideRequested_ValidItem_ProducesHideEffect` | `AppUpdateTests_ContinueWatchingActions` | Übergang → `HideContinueWatchingEntryEffect` mit `MediaType`/`MediaId` des Eintrags. |
| `ContinueWatchingSkipRequested_ValidItem_ProducesSkipEffect` | dto. | Übergang → `SkipContinueWatchingEntryEffect`. |
| `ContinueWatchingHideRequested_NonSelectable_Unchanged` / Skip-Variante | dto. | Guards: Id 0, leerer Typ, Platzhalter → `Unchanged`. |
| `HideAsync_Online_CallsHideEndpoint_AndRefreshesCache` | `ContinueWatchingMutationServiceTests` | POST `/api/continue-watching/hide` mit `{ mediaType, mediaId }`, dann `GET api/continue-watching` (Sync), Cache überschrieben. |
| `SkipAsync_Online_CallsSkipEndpoint_AndRefreshesCache` | dto. | POST `/api/continue-watching/skip` + Resync. |
| `HideAsync_Offline_RemovesEntryFromCacheLocally` | dto. | Ohne Client: Eintrag aus `cached_lists`/`continue_watching` entfernt, keine Requests. |
| `HideAsync_Offline_MissingEntry_ReturnsFalse` | dto. | Kein Treffer → `false`, Cache unverändert. |
| `MediaCarousel_LongPressWiring_OnlyOnContinueWatching` | `ContinueWatchingContextMenuContractTests` | `LongPressEnabled`, `ItemLongPressed`, `PointerGestureRecognizer`, Timer ≈ 2 s; nur am Weiterschauen-Karussell. |
| `HomePage_LongPress_ShowsActionSheet_WithGermanLabels` | dto. | `DisplayActionSheet` mit „Ausblenden"/„Überspringen"/„Abbrechen"; Dispatch der beiden Nachrichten. |
| `ShellAppEffectHandler_HandlesHideSkipEffects` | dto. | Switch-Zweige + `CachedListsChangedMessage`. |
| `DevApi_Invoke_SupportsLongPressGesture` | `DevApiContractTests` (erweitert) oder neue Klasse | `gesture`-Feld + `SendItemLongPressed`-Auflösung. |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `HomePageContractTests.HomePage_ContinueWatchingCarousel_DispatchesOwnMessage` | Prüft `Assert.Equal(2, xaml.Split("OnMediaItemTapped").Length - 1)` — bleibt gültig; neue Attribute müssen den Test nicht brechen (Reihenfolge-Checks beachten). |
| `DevApiContractTests` | Neues optionales `gesture`-Feld — bestehende Asserts bleiben gültig, Ergänzung um Long-Press-Vertrag. |

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Long-Press auf Weiterschauen-Eintrag → Menü mit „Ausblenden"/„Überspringen" sichtbar → „Ausblenden" → `POST /api/continue-watching/hide` → Eintrag verschwindet | `ContinueWatchingContextMenuTests.LongPressMovie_ChooseHide_CallsHideEndpoint_EntryDisappears` | AK 1, 2, 3 | Kompletter Benutzerfluss: Geste → Dialog → Server → Listen-Aktualisierung — nur E2E prüfbar. |
| Pflicht | Long-Press → „Überspringen" → `POST /api/continue-watching/skip` → Eintrag verschwindet | `ContinueWatchingContextMenuTests.LongPressMovie_ChooseSkip_CallsSkipEndpoint_EntryDisappears` | AK 2, 3 | dto. |
| Pflicht | Long-Press → „Abbrechen" → kein hide/skip-Request, Eintrag bleibt | `ContinueWatchingContextMenuTests.LongPressMovie_Cancel_KeepsEntry_NoMutationRequests` | AK 1, 2 | Abbruchpfad des Menüs (sichtbarer Fehlerfall). |
| Pflicht | Kurzer Tap startet weiterhin Direktwiedergabe (Regression) | bestehend: `ContinueWatchingPlaybackTests.TapContinueWatchingMovie_StartsPlaybackAtResume_WithoutDetails` | AK 1 | Regressionsschutz des bisherigen Verhaltens — bereits vorhanden, muss grün bleiben. |

Bestehende E2E-Tests anzupassen: Keine (nur Ergänzung); `MockVideoServer` erhält neue Routen — bestehende Routen bleiben unverändert.

## Offene Punkte

Keine.
