# Projektplan-Prüfung: Design-Angleichung an Webanwendung (Kundenfeedback)

Geprüft: `project-plan.md` (7 Schritte) gegen `requirement.md` (FA-1 bis FA-7),
`inventory.md`, `web-design-recherche.md` sowie Stichproben im Quellcode der App
und (lesend) im Server-Repo `fd729906-…`.

## Ergebnis

**Status:** Projektplan vollständig

## Abgleich Anforderung ↔ Entwicklungsschritte

| Anforderung | Abdeckende(r) Schritt(e) | Bemerkung |
|-------------|--------------------------|-----------|
| FA-1 Layout/Design-Tokens wie Web (tiefschwarzer Hintergrund `#131313`/`#0e0e0e`, Flächen-, Text-, Akzentfarben, Rahmen, Radien: Buttons 4px, Cards 8px, Chips/Badges pill) | Schritt 1 | Vollständig: Token-Liste deckt sich mit `web-design-recherche.md`; Ersetzung aller hartcodierten Hexfarben und Shell/Flyout-Farbgebung sind enthalten. |
| FA-2 Startseiten-Hero + positiv formulierter Schalter (Standard an, persistiert) | Schritt 5 | Vollständig: Endpunkt `api/pictures/hero-background`, Cover-Darstellung, geschichtete Gradienten, Kicker/Titel/Beschreibung, positiver Schalter mit Default „an", Persistenz, Offline-Verhalten. Annahme „app-lokal" ist als behandelter offener Punkt begründet dokumentiert. |
| FA-3 Detail-Header Serie/Staffel/Episode (Vollbreiten-Backdrop + Overlay-Infos) | Schritt 6 | Vollständig und darüber hinaus: umfasst zusätzlich Film- und Kollektions-Detailseiten sowie Episoden-Still über `api/episodes/{id}/background-image` und Header-Wechsel bei Episodenauswahl (Web-Muster). Erweiterung s. Hinweise. |
| FA-4 Angesehen-Symbol bei Episoden | Schritt 7 | Vollständig: rundes Augen-Badge über dem Poster, nur bei gesetztem `WatchedAt`; Übernahme des bereits vom Server gelieferten, bislang ignorierten Felds. |
| FA-5 Downloads-Seite ohne Flickern | Schritt 2 | Vollständig: nur Fortschrittsanzeige des betroffenen Eintrags darf sich sichtbar ändern; Ursachenrichtung (Listen-Neuaufbau pro Fortschritts-Tick trotz granularer Meldung) stimmt mit dem Code überein. |
| FA-6 Steuerelemente in Vollansicht klickbar | Schritt 3 | Vollständig: eingegrenzte Ursache (`OverlayChrome` füllt die gesamte Wiedergabefläche) ist im Code verifiziert; die App-Überlagerung muss funktionsfähig bleiben. |
| FA-7 COMException bei Zweitwiedergabe | Schritt 4 | Vollständig: Ursachenbehebung gefordert (UI-Thread-Zugriff, Abmeldung verwaister Seiten), Workaround bei Toolkit-Ursache zulässig — entspricht der Anforderung. |

**Nicht-Ziele:** werden eingehalten — Webanwendung und Server-API bleiben
unverändert (nur öffentliche Endpunkte), kein Android-Ausbau (Ziele bleiben
Windows/iOS), kein Funktionsausbau (der tolerierte Sidebar-Verzicht ist als
behandelter offener Punkt begründet), kein Toolkit-Hauptversionsupdate
(Schritt 4 erlaubt ausdrücklich einen app-seitigen Workaround).

## Abhängigkeitsprüfung

| Schritt | Abhängigkeiten | Prüfung |
|---------|----------------|---------|
| 1 | Keine | OK |
| 2 | Keine | OK |
| 3 | Keine | OK |
| 4 | Keine | OK |
| 5 | 1 | OK — früherer Schritt, fachlich begründet (Hero-Gradienten/Farben nutzen die Tokens aus Schritt 1) |
| 6 | 1 | OK — gleiche Begründung (Header-Overlay/Soft-Farben) |
| 7 | 6 | OK — früherer Schritt, begründet (Badge sitzt in der Episodenliste derselben Detailseite, die Schritt 6 umbaut) |

Zyklenfrei, nur Rückwärtsverweise auf frühere Schritte, alle fachlich begründet.
Schritte 2–4 sind bewusst unabhängig vom Design-Strang (frühe, separat
lieferbare Fehlerbehebungen) — konsistent mit den Vorgehensentscheidungen.

## Fehlende oder unvollständige Punkte

Keine.

## Hinweise

1. **Schritt 6 geht über den FA-3-Wortlaut hinaus:** Die Anforderung nennt nur
   Serien-, Staffel- und Episoden-Detailseiten; der Schritt umfasst zusätzlich
   Film- und Kollektionsseiten. Das ist über FA-1 („Layout wie die
   Webanwendung" — dieselbe Detailseite, dasselbe Web-Header-Muster) begründbar
   und kein Nicht-Ziel-Verstoß, aber faktisch eine Erweiterung, die der
   spätere Prüfer kennen sollte.
2. **Schritt 3 nennt im Akzeptanzkriterium nur Windows** („in der Vollansicht
   unter Windows sofort bedienbar"). iOS ist ebenfalls Zielplattform; die
   verifizierte Ursache (`OverlayChrome` ohne `VerticalOptions`,
   `PlayerPage.xaml:31-32`) existiert plattformübergreifend. Die Behebung sollte
   nicht versehentlich Windows-only verifiziert werden.
3. **Neue Tests sind nur implizit gefordert:** Die Anforderung nennt unter
   „Betroffene Bereiche" neue `AppUpdateTests_*` (Hero-Einstellung,
   Watched-Anzeige, Download-Fortschritt, Player-Zweitwiedergabe). Die Schritte
   fordern jeweils nur „bestehende Tests bleiben grün" + DoD; die Teststrategie
   verweist auf die etablierte MVU-/Contract-Test-Konvention. Rein clientseitig
   testbare Punkte (WatchedAt-Mapping, Hero-Schalter-Übergang) sollten in der
   Umsetzung nicht ohne neue Tests bleiben — der Plan verlangt sie nicht
   ausdrücklich.
4. **Flicker-Ursache (Verifikation):** Die Plan-Beschreibung trifft zu, die
   Inventory-Formulierung ist leicht ungenau: `UpdateUi` setzt `ItemsSource`
   nicht selbst — die Quelle ist per Binding an `Model.Downloads.Items`
   gebunden (`DownloadsPage.xaml:59`), und jede `DownloadItemChangedMessage`
   erzeugt über `UpsertItem` eine neue Listeninstanz (`AppUpdate.cs:1765`,
   `1818-1835` mit `items.ToList()`), gedrosselt auf 250 ms
   (`DownloadService.cs:104`, `704-724`) → CollectionView-Reset pro Tick.
   Substanz (voller Neuaufbau pro Fortschrittsmeldung) stimmt.
5. **`WatchedAt` im Offline-Snapshot:** Schritt 7 deckt die Server-Übernahme
   ab; ob das Feld auch in den Offline-Serien-Cache (`ShowDetailsCache`)
   einfließt, ist offenes Implementierungsdetail — unkritisch, da der
   Offline-Pfad ohnehin reduzierte Daten zeigt.

## Verifikationsprotokoll (Stichproben)

| Plan-/Inventory-Behauptung | Befund |
|----------------------------|--------|
| `OverlayChrome` füllt das gesamte Grid über dem MediaElement, im Mini-Player ausgeblendet | Bestätigt: `PlayerPage.xaml:31-32` (kein `VerticalOptions` → Fill), `PlayerPage.xaml.cs:117` (`IsVisible = !IsPictureInPicture`) |
| PlayerPage wird pro Wiedergabe neu erzeugt, meldet sich nie von `ModelChanged` ab | Bestätigt: `AddTransient` (`MauiProgram.cs:108`), Route „player" (`AppShell.xaml.cs:77`), `ModelChanged +=` ohne `-=` (`PlayerPage.xaml.cs:38`), `PlayerClosedMessage` in `OnDisappearing` (Z. 58), einziges `SeekTo` in `OnMediaOpened` (Z. 153) |
| Downloads-Seite baut Liste pro Fortschritts-Tick neu auf, granulare Meldung vorhanden | Bestätigt in Substanz (s. Hinweis 4): `UpsertItem` erzeugt neue Liste pro `DownloadItemChangedMessage`; Drossel 250 ms |
| `WatchedAt` wird vom Server geliefert und von der App ignoriert | Bestätigt: `ItemsController.cs:717` (`EnrichAsync` über Episoden), DTO `MediaEntryDto.WatchedAt` (Z. 51); App-`MediaEntry` hat kein `WatchedAt`-Feld (STJ ignoriert es still), `EpisodeItem`/`MediaDetailsView` ohne Watched-Feld |
| Kein Hero-Endpunkt im ApiClient, kein Hero-State, kein Hero-Setting | Bestätigt: `VideoPlayerApiClient.cs` hat nur `GetPictureAsync(long)` (Z. 394); `AppSettings.cs` ohne Hero-Flag; `SettingsPage.xaml` ohne Hero-Schalter; kein „Hero" im App-Code/XAML |
| Server-Endpunkte existieren | Bestätigt: `api/pictures/hero-background` (`PicturesController.cs:90`), `api/episodes/{id}/background-image` (`EpisodesController.cs:50`, lazy-generierend mit ETag) |
| Template-Palette statt Web-Tokens; Button-Radius 8px | Bestätigt: `Colors.xaml:9-29` (`Primary #512BD4`, `OffBlack #1f1f1f`), `Styles.xaml:32` (`CornerRadius 8`) |
| Detailseite ohne Vollbreiten-Backdrop/Overlay | Bestätigt: `MediaDetailsPage.xaml:34-80` (140×200-Poster-Grid, kein Overlay) |
| Toolkit-Version MediaElement 10.0.0 | Bestätigt: `VideoPlayer.Maui.csproj:53` |
