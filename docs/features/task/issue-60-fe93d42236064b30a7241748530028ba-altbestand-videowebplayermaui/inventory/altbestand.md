# Altbestand: Struktur und Inhalt

## `VideoWebPlayer.Client` (12 Dateien, ~1.300 Zeilen)

`VideoWebPlayer.Client/VideoWebPlayer.Client.csproj` — `net10.0` Bibliothek, PackageReferences: `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Http`, `Microsoft.Extensions.Configuration.Abstractions`. Keine ProjectReferences.

### `VideoWebPlayerClient` (`VideoWebPlayer.Client/VideoWebPlayerClient.cs`)

HTTP-Client der alten App. Öffentliche Methoden (Auswahl): `AuthenticateAsync`, `EnsureAuthorizationTokenAsync`, `HealthCheckAsync`, `RequestSourcesAsync`, `RequestSourceAsync`, `RequestSourceGenresAsync`, `RequestRecentEntriesAsync`, `RequestContinueWatchingAsync`, `HideContinueWatchingAsync`, `SkipContinueWatchingAsync`, `RequestFavoritesAsync`, `ToggleFavorite`, `RemoveFavoriteAsync`, `RequestSourceItems`, `RequestMovieCollectionAsync`, `RequestMovieAsync`, `RequestTVShowAsync`, `RequestTVShowEpisodeAsync`, `RequestGenreOptionsAsync`, `SaveMetadataAsync`, `GetContinueWatchingAsync`, `ReportPlaybackProgressAsync` (ohne Playlist-Kontext — die in Issue #59 für den neuen Client ergänzte Lücke), `GetPictureAsync`, `GetSourcePictureAsync`, `DeleteSourceAsync`.

### Modelle (`VideoWebPlayer.Client/Models/`)

`AuthenticationRequest.cs`, `AuthorizationToken.cs`, `ContinueWatchingDto.cs`, `ContinueWatchingMutationResult.cs` (record `(string Status, string Message)`), `DtoMovie.cs`, `DtoRecentEntry.cs` (enthält Enum `RecentEntryType` im globalen Namespace, Werte `Movie`, `MovieCollection`, `TVShow`, `TVShowSeason`, `TVShowEpisode`), `DtoSource.cs`, `FavoriteEntry.cs`, `ImpersonateRequest.cs`, `MediaEntryDto.cs`.

## `VideoWebPlayer.Maui` (117 Dateien, ~47.300 Zeilen)

`VideoWebPlayer.Maui/VideoWebPlayer.Maui.csproj` — MAUI-App, `ProjectReference` auf `..\VideoWebPlayer.Client\VideoWebPlayer.Client.csproj`. PackageReferences u. a. `CommunityToolkit.Maui`, `CommunityToolkit.Maui.MediaElement`, `Microsoft.AspNetCore.SignalR.Client`, `SkiaSharp`, `sqlite-net-pcl`, `Zeroconf`.

Verzeichnisstruktur:

| Pfad | Inhalt |
|------|--------|
| `VideoWebPlayer.Maui/` | `App.xaml`, `AppShell.xaml`, `MainPage.xaml`, `MauiProgram.cs`, Pages (`HomePage`, `LoadingPage`, `LoginPage`, `SettingsPage`, `MovieCollectionDetailsPage`, `SourceOverviewPage`, `TVShowDetailsPage`, `ServerSetupPage.xaml.cs`) |
| `Components/`, `Converters/`, `Resources/` | UI-Bausteine, AppIcon/Fonts/Images/Styles |
| `Models/` | u. a. `DownloadedVideo.cs` (Enums `DownloadRetentionType`, `DownloadStatus`), `VideoRequest.cs` (Enum `VideoSourceType`) |
| `Services/` (inkl. `Services/Events/`) | u. a. `AuthService.cs`, `ConnectionService.cs`, `SettingsService.cs`, `DownloadManager.cs`, `DownloadQueue.cs`, `WatchlistDownloadCoordinatorService.cs` |
| `ViewModels/` | u. a. `HomePageViewModel.cs`, `MediaCarouselViewModel.cs` (Enum `CarouselKind`) |
| `Platforms/` | Android, iOS, MacCatalyst, Windows |
| `Docs/`, `Properties/` | Projektdoku, Assembly-Attribute |

## Verhältnis zur neuen App

Keine Datei der neuen App (`VideoPlayer.Maui`, `VideoPlayer.Mvu`, `VideoPlayer.ApiClient`, `VideoPlayer.Storage`, `msTools.DeveloperApi`, `msTools.DeveloperApi.Maui`) referenziert `VideoWebPlayer.*`. Einzige Querverbindung: XML-Doc-Kommentar in `VideoPlayer.ApiClient/Models/PlaylistSortModeValues.cs` („spiegelt `VideoWebPlayer.Data.PlaylistSortMode`").
