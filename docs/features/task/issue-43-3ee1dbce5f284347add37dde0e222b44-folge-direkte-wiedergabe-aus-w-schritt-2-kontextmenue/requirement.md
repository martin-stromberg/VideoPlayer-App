# Übersetzte Anforderung: Kontextmenü in der Weiterschauen-Liste (Ausblenden / Überspringen)

## Fachliche Zusammenfassung

Die Weiterschauen-Liste der Startseite (`VideoPlayer.Maui/Pages/HomePage.xaml`, `MediaCarousel` mit `AutomationId="ContinueWatchingCarousel"`) erhält ein Kontextmenü: Ein 2-Sekunden-Long-Press auf einen Eintrag öffnet ein Menü mit den Aktionen „Ausblenden" und „Überspringen" — analog zur Serveranwendung. „Ausblenden" ruft `HideContinueWatchingEntryAsync` (`POST api/continue-watching/hide`) auf, „Überspringen" `SkipContinueWatchingEntryAsync` (`POST api/continue-watching/skip`); danach wird die Weiterschauen-Liste aktualisiert — online über die bestehende Sync-Strecke (`SyncContinueWatchingEffect` → Cache → `CachedListsChangedMessage`), offline über lokales Entfernen aus dem `continue_watching`-Cache-Eintrag. Der kurze Tap behält das seit Schritt 1 bestehende Verhalten (`ContinueWatchingItemSelectedMessage` → direkte Wiedergabe bzw. Detailansicht).

## Akzeptanzkriterien

1. Ein 2-Sekunden-Long-Press auf einen Weiterschauen-Eintrag öffnet ein Kontextmenü; ein kurzer Tap startet weiterhin das bisherige Verhalten (direkte Wiedergabe bzw. Detailansicht bei nicht abspielbaren Typen).
2. Das Kontextmenü bietet die Aktionen „Ausblenden" und „Überspringen".
3. „Ausblenden" ruft die Hide-Funktion der Server-API auf, „Überspringen" die Skip-Funktion; bei Erfolg verschwindet der Eintrag aus der angezeigten Weiterschauen-Liste (bzw. verhält sich gemäß Server-Antwort).
4. Das Kontextmenü ist auf die Weiterschauen-Liste beschränkt (keine Ausweitung auf andere Listen).

## Betroffene Klassen und Komponenten

- **UI (neu/geändert):**
  - `VideoPlayer.Maui/Controls/MediaCarousel.xaml` + `.xaml.cs` — Long-Press-Erkennung an den Eintragskarten und `ItemLongPressed`-Ereignis (neu).
  - `VideoPlayer.Maui/Pages/HomePage.xaml` + `.xaml.cs` — Verdrahtung des Long-Press nur am Weiterschauen-Karussell; Menüdarstellung (analog `DisplayActionSheet` im Altbestand); Dispatch der Auswahl-Nachrichten.
- **MVU (`VideoPlayer.Mvu`):**
  - `AppMessage.cs` — neue Nachrichten für die Menüaktionen (Hide/Skip) des ausgewählten `HomeMediaItem`.
  - `AppEffect.cs` — neue Effekte für die Hide-/Skip-Aufrufe (Medientyp + Id).
  - `AppUpdate.cs` — neue Switch-Zweige und Übergangsfunktionen mit den üblichen Guards (`Id > 0`, `MediaType`, `IsSelectable`).
  - Neuer Dienst (Muster `FavoritesService`): Hide/Skip über `VideoPlayerApiClient`, danach `ServerSyncService.SyncContinueWatchingAsync`; ohne Client lokales Entfernen aus dem `cached_lists`-Eintrag `continue_watching` (`CacheDatabase`).
- **App-Rand (`VideoPlayer.Maui`):**
  - `Services/ShellAppEffectHandler.cs` — Ausführung der neuen Effekte, danach `CachedListsChangedMessage` (Muster `RemoveFavoriteAsync`).
  - `MauiProgram.cs` — Registrierung des neuen Dienstes.
- **Test-Infrastruktur (E2E):**
  - `msTools.DeveloperApi.Maui` (`MauiDevApiService`/`DevApiVisualTree`) — Auslösung des Long-Press über die Developer-API (UIA-Lücke, analog `SendTapped`-Trick).
  - `VideoPlayer.E2E.Tests/Support/MockVideoServer.cs` — `POST /api/continue-watching/hide` und `…/skip` samt Diagnose-Zählern und Listenverhalten nach der Mutation.
  - `VideoPlayer.E2E.Tests/Support/DevApiClient.cs` — Client-Aufruf für die Long-Press-Auslösung.
- **Tests:**
  - `VideoWebPlayer.Maui.Tests`: neue `AppUpdateTests_*` für die Nachrichten-Übergänge, Dienst-Tests (Muster `FavoritesServiceTests` mit `StubHttpMessageHandler`), Contract-Tests (Muster `HomePageContractTests`, `NetworkSyncContractTests`).
  - `VideoPlayer.E2E.Tests`: neue Testklasse für den Benutzerfluss Long-Press → Menü → Hide/Skip → Liste aktualisiert.

## Implementierungsansatz

- **Long-Press-Erkennung:** `PointerGestureRecognizer` (`PointerPressed`/`PointerReleased`/`PointerExited`) plus 2-s-Timer im `MediaCarousel`-Code-Behind — kein zusätzliches NuGet-Paket (`CommunityToolkit.Maui` ist in `VideoPlayer.Maui` bewusst nicht referenziert; der Altbestand nutzt zwar `mct:TouchBehavior LongPressDuration="2000"`, das App-Projekt folgt dem schlankeren Referenzsatz). Das Ereignis wird als `ItemLongPressed` nach außen geführt, parallel zum bestehenden `ItemTapped`.
- **Menüdarstellung:** `DisplayActionSheet` mit den Klartext-Aktionen „Ausblenden"/„Überspringen" und „Abbrechen" (Konvention des Altbestands, `MainPage.xaml.cs`/`ServerSetupPage.xaml.cs`) — keine Eingabe interner Kennungen; der Eintrag ist bereits über den Long-Press identifiziert.
- **MVU:** Request-Nachrichten je Aktion → Effekte `HideContinueWatchingEntryEffect`/`SkipContinueWatchingEntryEffect` (Medientyp, Medien-Id) → `ShellAppEffectHandler` → neuer Mutationsdienst → `CachedListsChangedMessage` (bei Erfolg).
- **E2E-Auslösbarkeit:** Erweiterung von `POST /invoke` der Developer-API um eine Long-Press-Auswahl am BindableLayout-Eintrag (Konvention wie `SendTapped` per Reflection).
- **Medientyp-Konvention:** Der `mediaType` des `HomeMediaItem` der Weiterschauen-Liste wird unverändert an hide/skip weitergereicht (Server-Konvention der Liste, wie ihn die API geliefert hat).

## Konfiguration

Keine — das Feature ist nicht konfigurierbar.

## Offene Fragen

- Keine. (Der genaue Semantikunterschied hide vs. skip liegt serverseitig; die App ruft die Endpunkte auf und aktualisiert die Liste gemäß Server-Antwort — wie gefordert.)
