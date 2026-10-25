# API- und Serververträge

## Im mobilen API-Client vorhandene Verträge

`VideoPlayer.ApiClient/VideoPlayerApiClient.cs` implementiert unter anderem:

- `GET api/items/recent` → `RecentEntry[]` (`GetRecentEntriesAsync`). `RecentEntry` enthält Id, Quellen-Id, Zeitstempel, `RecentEntryKind` und eingebetteten Medieneintrag.
- `GET api/continue-watching` → `ContinueWatchingEntry[]`.
- `GET api/favorites` → Favoriteneinträge.
- `GET api/SourceGenres/{id}` → Genres einer konkreten Medienquelle; genutzt beim Quellen-Browsing.
- `GET api/items/genres` → allgemeine Genre-Optionen für den Playlist-Genre-Editor.
- `GET api/items` erlaubt Genre-Filterung beim Browsing. Playlist-Verträge erlauben außerdem Genres einer Playlist.

Die vorhandenen Genre-Verträge beschreiben Genres als Optionen/Filter bzw. Playlist-Metadaten. Im Clientmodell und `HomeListService` findet sich keine Saison, Start-/Endzeit, saisonale Genre-Liste oder API-Methode dafür. `GET api/items/recent` ist eine zeitlich sortierte Neuheitenliste und kein Vertrag für Anlasslisten.

## Serverseitige Grenze

In der Solution sind API-Client-, Storage-, MVU-, MAUI- und E2E-Projekte enthalten, aber kein Server- oder Webprojekt. Damit kann die Aussage der Anforderung, dass die Server-Webanwendung das Feature bereits unterstützt, hier nicht unabhängig verifiziert werden. Es ist auch kein konkreter saisonaler Endpunkt oder Beispielpayload dokumentiert. Die konkrete Integration muss vor Implementierung mit der maßgeblichen Serverquelle geklärt werden; nicht aus den allgemeinen Genre-Endpunkten ableiten.

## Relevante Fundstellen

- `VideoPlayer.ApiClient/VideoPlayerApiClient.cs`: `GetRecentEntriesAsync`, `GetSourceGenresAsync`, `GetGenreOptionsAsync`, `GetSourceItemsAsync`.
- `VideoPlayer.ApiClient/Models/RecentEntry.cs`: Neuheitenvertrag mit den Medientypen Movie, MovieCollection, TvShow, TvShowSeason und TvShowEpisode.
- `VideoPlayer.Mvu/ServerSyncService.cs`: synchronisiert Recent-Einträge unter Cache-Schlüssel `recent`.
- `VideoPlayer.Mvu/HomeListService.cs`: mappt nur `continue_watching`, Favoriten und `recent` in Home-Listen.

## Lokale Server-/Webreferenz (lesend geprüft)

Die lokale Serverlösung liegt unter
`D:/Repositories/softwareschmiede/1f22729c-5f43-4f69-94c5-aee41f073cac`,
Branch `task/issue-277-1f22729c5f434f6994c5aee41f073cac-tv-sender-platzhalter-clips-gr`.
`VideoWebPlayer/Components/Shared/Home/SeasonalGenreList.razor` injiziert
`ApplicationDbContext` und fragt Daten direkt ab; ein saisonaler Controller-
oder API-Clientvertrag wurde dort nicht gefunden.

`GenreService.GetSeasonalGenresAsync()` wählt nur Genres mit beiden Grenzen,
schiebt das Fenster jahresweise bis zum Ende >= `DateTime.UtcNow.Date` vor und
prüft `start <= heute <= end`. Die Webkomponente lädt Filme, Sammlungen und
Serien aus Quellen, die dem aktuellen Benutzer freigegeben sind. Eine
Filmsammlung wird nur einmal aufgeführt; bei genau einem Film ersetzt dieser
die Sammlung. Filme ohne Sammlung sowie Serien werden separat aufgenommen.
Je Genre sortiert die Komponente absteigend nach EndedAt, ReleaseDate,
PremieredAt und CreatedAt, dann aufsteigend nach Name. Sektionen ohne Einträge
werden nicht gerendert. Diese Bestandsaufnahme begründet den im Plan
festgelegten gemeinsamen API-Vertrag; die Webansicht soll künftig denselben
Endpunkt verwenden wie Mobile.

Die Serverkopie liegt außerhalb des freigegebenen Schreibbereichs und ihr
Branch gehört einem anderen Thema. Für eine spätere Umsetzung kommt nur ein
separater Klon/Checkout im Workspace mit eigenem Feature-Branch infrage;
weder Dateien noch `.git`-Metadaten des Ursprungsrepo werden geändert.
