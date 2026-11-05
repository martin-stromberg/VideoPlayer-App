# Anforderung: tvOS-Startseite mit fokussierbaren Karussells (Entwicklungsschritt 4)

Quelle: Projektplan Schritt 4 (`docs/projects/.../project-plan.md`), Issue #86, Aufgaben-ID `5dcdc7d5-ef27-4118-97f0-ec9129c87614`.

## Fachliche Zusammenfassung

Die tvOS-App erhält eine Lean-back-Startseite entsprechend dem Funktionsumfang der bestehenden MAUI-App: horizontale, per Siri Remote fokussierbare Karussells für „Weiterschauen" (mit Fortschrittsanzeige), „Neu im Programm" und Favoriten sowie saisonale Genre-Sektionen. Die Daten kommen aus den bestehenden plattformneutralen Startseiten- und Synchronisationsdiensten mit Cache-First-Verhalten: gespeicherte Listen erscheinen sofort, Aktualisierung läuft im Hintergrund (Synchronisation) und bei Server-Ereignissen. Poster werden vom Server nachgeladen und zwischengespeichert; fehlende Bilder erhalten einen Platzhalter. Einträge sind auswählbar und führen zu einem vorbereiteten Navigationsziel je Eintrag — Detailansicht und Wiedergabe folgen in späteren Schritten; bis dahin genügt ein Platzhalter-Zielzustand ohne Fehler. Im Offline-Zustand zeigt die Startseite die lokal gespeisten Bereiche.

## Funktionsumfang

1. **Karussell „Weiterschauen"** (`AppModel.ContinueWatching`): horizontale Reihe mit Fortschrittsbalken je Eintrag (`HomeMediaItem.Progress`), Platzhalter-Kennzeichnung (`IsPlaceholder`), Auswahl → `ContinueWatchingItemSelectedMessage`.
2. **Karussell „Favoriten"** (`AppModel.Favorites`): Auswahl → `MediaItemSelectedMessage`.
3. **Karussell „Neu im Programm"** (`AppModel.RecentEntries`): Auswahl → `MediaItemSelectedMessage`.
4. **Saisonale Genre-Sektionen** (`AppModel.SeasonalGenres`): je Sektion eine benannte Karussell-Reihe (`HomeSeasonalSection.Name`/`Items`); abgelaufene Sektionen lösen ein Nachladen aus (`ExpiresAt`-Timer → `HomeListsReloadRequestedMessage`).
5. **Cache-First:** Anzeige speist sich aus dem MVU-Modell (`HomeListsLoadedMessage` nach `LoadHomeListsEffect` — bereits verdrahtet; `HomeListsReloadRequestedMessage` bei App-Aktivierung bereits dispatcht); beim Sichtbarwerden der Seite wird zusätzlich nachgeladen.
6. **Poster:** Laden über `api/pictures/{id}` (`PosterPictureId`), tvOS-eigener Bild-Dienst analog `PosterImageService` (Arbeitsspeicher + `PreviewsDirectory`), Fehlschlag/fehlende Bilder → Titel-Platzhalter.
7. **Auswahl/Navigationsziel:** `NavigateToMediaDetailsEffect`, `NavigateToPlayerEffect` und `StartPlaylistPlaybackEffect` werden am tvOS-Effekt-Rand auf eine vorbereitete Platzhalter-Zielseite abgebildet (kein Fehler, kein No-Op-Verlust).
8. **Offline:** die Seite rendert die aus lokalen Daten gespeisten Bereiche (`IsOffline`-Hinweis analog Offline-Banner der MAUI-Startseite); leere Listen blenden ihre Sektion aus, die App bleibt bedienbar.
9. **Fokus:** alle Einträge per Siri Remote fokussierbar/scrollbar; Fokus wandert nachvollziehbar zwischen Reihen und Einträgen (Focus-Engine, sichtbare Fokus-Hervorhebung).

## Betroffene Klassen und Komponenten

### Wiederverwendung (vorhanden)

- `VideoPlayer.Mvu`: `AppModel.ContinueWatching`/`Favorites`/`RecentEntries`/`SeasonalGenres`/`IsOffline`/`StorageNotice`, `HomeMediaItem`, `HomeSeasonalSection`, Messages (`MediaItemSelectedMessage`, `ContinueWatchingItemSelectedMessage`, `HomeListsReloadRequestedMessage`, `HomeListsLoadedMessage`), Effekte (`LoadHomeListsEffect` — bereits verdrahtet in `TvosAppEffectHandler`), `AuthSessionService.Client.GetPictureAsync`.
- `VideoPlayer.TvOS`: `MenuTabBarController` (Seitenslot Home), `IModelRenderable`, `TvosControls`, `TvosNavigator`, `MainViewController`, `AppComposition`, `TvosAppEffectHandler`, `MainThreadInvoker`, `AppStorageLayout.PreviewsDirectory`.

### Neu zu erstellen

- `Ui/HomePageViewController` — modellgetriebene Startseite (UIScrollView + Sektions-Stack mit je UILabel + horizontalem UICollectionView).
- `Ui/HomeMediaItemCell` — fokussierbare Medienkarte (UIImageView, UIProgressView, Titel, Fokus-Hervorhebung, Platzhalter-Darstellung).
- `Ui/HomeCarouselView`/DataSource — horizontale Reihe pro Sektion (UICollectionView + FlowLayout, UICollectionViewSource).
- `Services/TvosPosterImageService` — `UIImage`-Ladung aus Speicher-/Datei-Cache bzw. Server.
- `Ui/PreparedTargetViewController` — Platzhalter-Navigationsziel für Detail-/Wiedergabeziele.
- Verdrahtung: `MenuTabBarController` (Home-Slot), `TvosNavigator` (Poster-Dienst + Prepared-Target-Präsentation), `AppComposition` (Poster-Dienst), `TvosAppEffectHandler` (drei Navigations-Effekte).
- Tests: `TvOsHomePageContractTests` (Quell-Contract-Tests nach Muster `TvOsConnectionContractTests`).

## Implementierungsansatz

- Seite als `UIViewController` + `IModelRenderable`; Sektionsaufbau aus dem Modell, Sektionen ohne Einträge ausblenden, Änderungen über `ReloadData` der betroffenen Reihe (Vergleich via `Enumerable.SequenceEqual` auf den Records).
- Fokus über Standard-Focus-Engine: Zellen fokussierbar, `DidUpdateFocus` skaliert die Karte; `RemembersLastFocusedIndexPath` für nachvollziehbaren Fokus zwischen Reihen.
- Bild-Nachladung asynchron je Zelle mit Generations-Check (verworfene Recycle-Bilder); Fehlschlag → Titel-Fallback.
- Prepared Target: modal präsentierter Platzhalter-ViewController (Menü-Taste schließt); bei aufeinanderfolgenden Navigations-Effekten (Detail→Player) wird der präsentierte Platzhalter aktualisiert statt gestapelt.
- Kein Schreibzugriff: keine Mutations-Effekte verdrahten (Kontextmenü/Favoriten-Toggle bleiben unverdrahtet).

## Konfiguration

- Keine neuen Einstellungen; `HomeHeroEnabled`/Hero-Bereich ist nicht Teil dieses Schritts.

## Akzeptanzkriterien (aus dem Projektplan)

- Alle drei Pflicht-Karussells mit echten Serverdaten befüllt, per Fernbedienung durchgängig fokussierbar und scrollbar.
- Fokus wandert nachvollziehbar zwischen Reihen und Einträgen.
- Inhalte aktualisieren sich bei Server-Ereignissen und Rückkehr in den Vordergrund.
- Cache-First: gespeicherte Daten erscheinen sofort.
- App bleibt bedienbar bei leeren Listen oder offline.
- Keinerlei Schreibzugriff auf Inhalte; Validierung über macOS-CI-Build und Simulator.
