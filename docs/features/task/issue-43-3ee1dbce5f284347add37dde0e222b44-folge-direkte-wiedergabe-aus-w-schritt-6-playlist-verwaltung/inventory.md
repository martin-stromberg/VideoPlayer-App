# Bestandsaufnahme — Schritt 6: Playlist-Verwaltung

## Umgebung

- Branch: `task/issue-43-3ee1dbce5f284347add37dde0e222b44-folge-direkte-wiedergabe-aus-w-schritt-6-playlist-verwaltung`
- Vergleichs-Branch (Reviews): `task/issue-43-3ee1dbce5f284347add37dde0e222b44-folge-direkte-wiedergabe-aus-w` (wird NICHT gemerged)
- HEAD bei Bestandsaufnahme: `504c8cc chore: Schritt 5 fertig gemerged, Schritt 6 in Arbeit`
- `git status`: sauber bis auf das neue Feature-Verzeichnis (untracked)
- .NET SDK: 10.0.401
- Testprojekt: `VideoWebPlayer.Maui.Tests` (xUnit, net10.0); E2E: `VideoPlayer.E2E.Tests` (xUnit + FlaUI.UIA3, Windows-Build der App via AppFixture)
- Lösung: `VideoPlayer.App.sln` (10 Projekte)

## Baseline (vor Änderungen)

- `dotnet build VideoPlayer.App.sln`: **erfolgreich** — 0 Fehler, 293 Warnungen (allesamt vorhandener Altbestand)
- `dotnet test VideoWebPlayer.Maui.Tests --no-build`: **1206/1206 grün** (12 s)
- Protokolle: `inventory/test-results/baseline-unittests.log`
- E2E-Baseline: E2E-Suite läuft im Test-Schritt vollständig; Schritt-5-Stand laut `changes.log` zuletzt 39/39 grün.

## Betroffene Projekte und Artefakte

| Bereich | Dateien | Stand |
|---|---|---|
| API-DTOs | `VideoPlayer.ApiClient/Models/*.cs` | `Playlist`, `PlaylistEntry` (mit `SortOrder`), `GenreOption`, `PlaylistSortModeValues` vorhanden; **keine** Verwaltungs-Request-DTOs |
| API-Client | `VideoPlayer.ApiClient/VideoPlayerApiClient.cs` | GET-Playlists/Entries/Cover, POST play/* vorhanden; **keine** POST/PUT/PATCH/DELETE-Verwaltungsmethoden; `SendWithPolicyAsync` zentral (Retry 1/2/4 s, 409 wird nicht wiederholt → `ApiException` mit `StatusCode`+`ResponseBody`) |
| MVU-Service | `VideoPlayer.Mvu/PlaylistService.cs` | Nur Lesepfade (Übersicht, Kopfdaten, Eintragsseiten, Wiedergabe); DTO→Item-Mapping intern |
| MVU-Zustände | `VideoPlayer.Mvu/PlaylistsPageState.cs`, `PlaylistDetailsState.cs`, `MediaDetailsState.cs`, `AppModel.cs` | Lese-/Wiedergabezustand; `PlaylistListItem` hat `IsOwner`/`IsPublic`/`GenreText`, **nicht** `AllGenreIds`/`GenresManuallyOverridden`; `PlaylistEntryItem` ohne `SortOrder` |
| MVU-Messages/Effekte | `VideoPlayer.Mvu/AppMessage.cs`, `AppEffect.cs`, `AppUpdate.cs` | 15 Playlist-Messages / 8 Playlist-Effekte (Lese-/Wiedergabepfad); Update ist `switch` über alle Messages |
| Effekt-Handler | `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` | Serviceauflösung über `IServiceProvider`, Dispatch über `DispatchMessageAsync`; Playlist-Zweige vorhanden |
| UI Übersicht | `VideoPlayer.Maui/Pages/PlaylistsPage.xaml(.cs)` | Flache CollectionView (Sektionen + Karten), kein Erstellen-Schalter |
| UI Details | `VideoPlayer.Maui/Pages/PlaylistDetailsPage.xaml(.cs)` | Kopf (Cover/Name/Beschreibung/SummaryText) + Eintrags-CollectionView + „Abspielen"; keine Verwaltungsaktionen |
| UI Mediendetails | `VideoPlayer.Maui/Pages/MediaDetailsPage.xaml(.cs)` | Kopf-Aktionsleiste (Favorit/Download/Abspielen); keine Playlist-Aktion |
| Dialoge | `HomePage.xaml.cs` (DisplayActionSheet-Long-Press), `ServerSetupPage.xaml.cs` (DisplayAlert) | Muster für Kontextmenü + Bestätigungen seiten-nah verankert |
| DevApi | `msTools.DeveloperApi.Maui` | `/invoke` (Button/Switch/CheckBox/MenuItem/Tap/Command), `/set-text` (InputView), ItemIndex-Auswahl + `longpress` für `SendItemLongPressed`, `/tree` mit Items; **Picker ist nicht setzbar** → Umschaltungen über Buttons/CheckBoxen |
| DevApi-Snapshot | `VideoPlayer.Mvu/DevApi/DevApiModelSnapshot.cs` | Playlist-Lese-/Wiedergabefelder vorhanden; Verwaltungsfelder fehlen |
| MockServer | `VideoPlayer.E2E.Tests/Support/MockVideoServer.cs` | Playlist-Lesen/Wiedergabe/Cover + Weiterschauen-Endpunkte; **keine** Verwaltungsrouten; `MockPlaylist` hat bereits `SortMode`, `Genres`, `IsPublic`, `IsOwner`, `Entries` |
| Unit-Tests | `VideoWebPlayer.Maui.Tests/` | `AppUpdateTests_Playlists` (Lesepfad), `PlaylistServiceTests`, `VideoPlayerApiClientTests_Playlists`, `EnumCoverageTests` (Enum-Wertabdeckung per Vertrag!) |
| E2E-Tests | `VideoPlayer.E2E.Tests/Tests/` | `PlaylistsTests` (Anzeige), `PlaylistPlaybackTests`, `ContinueWatchingContextMenuTests` (Long-Press + ActionSheet-Muster) |
| Hilfsdienste | `VideoPlayer.Mvu/ProfileInfoService.cs` | JWT-Payload-Dekodierung (`ReadTokenClaims`) — der Server-JWT enthält den Claim `IsAdmin` (`AuthorizationTokenService.CreateToken`: `new Claim("IsAdmin", user.IsAdmin.ToString())`) |
| Medientypen | `VideoPlayer.Mvu/MediaTypeNames.cs` | `Normalize` auf CLR-Kleinschreibung; Server parst `MediaType` via `Enum.TryParse(ignoreCase)` (`MediaHierarchyRegistry`) — kanonische Servernamen: `Movie`, `TVShow`, `TVShowSeason`, `TVShowEpisode`, `MovieCollection` |

## Server-Referenz (Repository `martin-stromberg/VideoPlayer`)

- `IPlaylistApiClient.cs` (Client-Methodenliste), `VideoWebPlayerClient.Playlists.cs` (exakte Routen/Query-Parameter)
- `PlaylistsController.cs`: Owner-Regeln (nur eigene änderbar), 403 beim Öffentlich-Flag für Nicht-Admins, 409 + `DtoRemovePlaylistEntryConflictResponse` beim Entfernen mit Weiterschauen-Bezug, 409 + `DtoChangeSortModeConflictResponse` beim Sortiermodus-Wechsel weg von `Manual`, 400 `PlaylistNotInManualSortMode` bei Reorder außerhalb `Manual`
- `PlaylistEntryReorderService.cs`: `move-between` schiebt alle Einträge zwischen alter und neuer `SortOrder` um (Ziel = `SortOrder` des Nachbareintrags → Tausch bei benachbarter Verschiebung); „ans Ende" = `GET max-sort-order` + `PUT order` mit `max+1`; `move-to-beginning` existiert als eigener Endpunkt
- `PlaylistService.cs` (Server): `AddMediaToPlaylist` kaskadiert (Serie → Staffeln+Episoden, Staffel → Episoden, Kollektion → Filme), Duplikate übersprungen (`SkippedCount`), Antwort `DtoPlaylistAddResult{AddedCount, SkippedCount, Message}`
- Server-UI (`Components/Playlists/`): `PlaylistForm` (Name Pflicht, Beschreibung, Sortiermodus nur beim Anlegen wählbar), `PlaylistDeleteConfirmationDialog`, `PlaylistEntryContinueWatchingConfirmationDialog`, `PlaylistGenreEditor` (Genre-Toggle-Chips + Zurücksetzen bei `GenresManuallyOverridden`), `PlaylistSortModeButton` mit Bestätigungsdialog, Öffentlich-Schalter nur bei `IsAdmin`-Claim + `IsOwner`, Eintrags-Aktionen nur bei `IsOwner` + `SortMode=Manual`
- `MediaHierarchyRegistry.ParseMediaType` = `Enum.TryParse` (ignoreCase) → die kleingeschriebene CLR-Konvention der App ist direkt sendefähig; für Klarheit werden kanonische Servernamen gemappt.

## Detail-Dateien

- `inventory/models.md` — DTO-/State-Bestand und Erweiterungsbedarf
- `inventory/logic.md` — MVU-Fluss, Effekt-Handler, Dialogmuster
- `inventory/interfaces.md` — Server-API-Schnittstellen (Routen/Bodies/Statuscodes)
- `inventory/tests.md` — Testinfrastruktur und -befehle

## Risiken / Besonderheiten

- **EnumCoverageTests**: neue Enums erfordern Vertrags-Tests → Verwaltungsflüsse werden über Records/Bools modelliert (kein neues Enum).
- **Picker nicht testbar**: Sortiermodus-/Genre-Auswahl wird über Schalter/Chips statt `Picker` realisiert (DevApi `/invoke` deckt Button/CheckBox ab).
- **409-Handling**: `ApiException` trägt `ResponseBody` — der Service parst die Bestätigungs-Flags aus dem Konflikt-JSON und signalisiert sie dem MVU-Zyklus (Dialog → Wiederholung mit Flagge).
- **Fremde öffentliche Playlists**: `IsOwner=false` → alle Schreibaktionen ausgeblendet (Server würde 403 liefern); die öffentliche Sektion enthält ausschließlich fremde Playlists (`GET api/playlists/public` ohne `IsOwner` ist per DTO trotzdem mitgegeben).
- **Weiterschauen-Konsistenz**: nach bestätigtem Entfernen mit Weiterschauen-Bezug entfernt der Server den Eintrag auch aus der Weiterschauen-Liste — die App lädt nach der Mutation die Eintragsliste neu; die Startseiten-Liste aktualisiert sich über den üblichen Sync-Pfad (kein Sonderfall nötig, da `PlaylistEntryId`-Verlust bereits den Serien-Fallback auslöst).
