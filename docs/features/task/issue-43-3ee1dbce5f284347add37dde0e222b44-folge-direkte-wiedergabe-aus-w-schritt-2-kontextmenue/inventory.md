# Bestandsaufnahme: Kontextmenü in der Weiterschauen-Liste (Ausblenden / Überspringen)

Analysiert wurden die Weiterschauen-Liste der neuen MAUI-App (`VideoPlayer.Maui`, `VideoPlayer.Mvu`), der API-Client (`VideoPlayer.ApiClient`), die Developer-API (`msTools.DeveloperApi.Maui`) und die E2E-Infrastruktur (`VideoPlayer.E2E.Tests`) bezogen auf die Anforderung „Kontextmenü in der Weiterschauen-Liste".

## Zusammenfassung

- Die Server-Endpunkte `POST api/continue-watching/hide` und `POST api/continue-watching/skip` existieren im neuen `VideoPlayerApiClient` (`HideContinueWatchingEntryAsync`, `SkipContinueWatchingEntryAsync`, Ergebnis `ContinueWatchingMutationResult`) und sind durch `VideoPlayerApiClientTests_Endpoints` gepinnt — sie werden aber noch nirgends aufgerufen.
- Das Weiterschauen-Karussell (`controls:MediaCarousel` mit `AutomationId="ContinueWatchingCarousel"` in `HomePage.xaml`) kennt nur `TapGestureRecognizer` → `ItemTapped` → `ContinueWatchingItemSelectedMessage` (Direktstart seit Schritt 1). Eine Long-Press-Erkennung existiert in `VideoPlayer.Maui` nicht; `CommunityToolkit.Maui` (TouchBehavior) ist nicht referenziert — nur `CommunityToolkit.Maui.MediaElement`.
- Die Listen-Aktualisierung nach einer Mutation kann über die bestehende Strecke laufen: `ServerSyncService.SyncContinueWatchingAsync` schreibt den `cached_lists`-Schlüssel `continue_watching`, `CachedListsChangedMessage` löst `LoadHomeListsEffect` aus.
- Der Altbestand (`VideoWebPlayer.Maui`) zeigt das Vorbild: `mct:TouchBehavior LongPressDuration="2000"` am Karussell-Eintrag, `DisplayActionSheet`-Menüs an anderer Stelle; Hide/Skip sind dort ebenfalls ungenutzt (Long-Press entfernt dort nur lokal aus der Liste).
- Der `MockVideoServer` der E2E-Tests kennt hide/skip noch nicht und liefert Weiterschauen-Einträge über Opt-in-Eigenschaften; die Developer-API kann Eintragsauswahl (`/invoke` + `itemIndex` → `SendTapped`), aber noch keinen Long-Press.
- Test-Ausgangszustand: `VideoWebPlayer.Maui.Tests` 1079/1079 grün; `VideoPlayer.E2E.Tests` siehe `inventory/tests.md` (Lauf separat dokumentiert).

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Enums](inventory/enums.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
