# Bestandsaufnahme — UI und Navigation

## `DownloadsPage` (Platzhalter)

Dateien: `VideoPlayer.Maui/Pages/DownloadsPage.xaml(.cs)` — Menüseite
`AppPage.Downloads`, Shell-Route `downloads` (Flyout-Eintrag
„Downloads" in `AppShell.xaml`), DI `AddTransient` in `MauiProgram`.
Aktueller Inhalt: Titel + Hinweistext „wird in einem späteren
Entwicklungsschritt gefüllt". Kein `MvuHost`-Binding.

Referenz-Muster für die Umsetzung: `FavoritesPage.xaml(.cs)` —
`BindingContext = MvuHost`, `ModelChanged` → `UpdateUi` auf dem
MainThread, `OnAppearing` dispatcht `…PageOpenedMessage`,
`CollectionView` mit `DataTemplate x:DataType`, Offline-Banner,
Fehler-Panel mit „Erneut laden", Leer-Hinweis, Aktions-Buttons je
Eintrag (`BindingContext` des Senders).

## `MediaDetailsPage`

Dateien: `VideoPlayer.Maui/Pages/MediaDetailsPage.xaml(.cs)` —
Kopfbereich mit „Abspielen" (`PlayButton`, sichtbar bei
`details.IsPlayable` = `movie`/`tvshowepisode`) und Favoriten-Stern;
Episodenliste mit ▶-Schaltfläche je Eintrag. Ansatzpunkt für eine
„Herunterladen"-Schaltfläche (Kopfbereich für Film/Episode, ggf. je
Episoden-Eintrag).

## `SettingsPage`

Dateien: `VideoPlayer.Maui/Pages/SettingsPage.xaml(.cs)` — alle
Download-Einstellungen bereits verdrahtet (`AutoDownloadSwitch`,
`PreloadEpisodeCountEntry`, `WifiOnlySwitch`, `DownloadQuotaGbEntry`,
`AutoDeleteWatchedSwitch`) über `SaveSettingsRequestedMessage`.

## Navigation/Menü

- `AppRoutes.MenuPages` enthält `AppPage.Downloads` (Route `downloads`)
  — zustandsgesteuerte Navigation via `NavigateToMessage`/
  `NavigatedMessage` + `NavigateToPageEffect`.
- Webanwendung-Referenz (Anforderung „an der Webanwendung orientieren"):
  Liste der Downloads mit Titel, Status und Fortschritt, Aktionen
  Abbrechen/Löschen (Altbestand zeigt Downloads als Karten-Liste auf
  der Startseite/`CarouselKind.Downloads`).

## Binding-Konventionen

- Seiten binden an `MvuHost` (`x:DataType="mvu:MvuHost"`), lesen
  `Model.<Seiten>State`; einfache Formatierung (Statustexte,
  Prozent-Strings) als berechnete Properties auf den State-/Item-
  Records (Muster `FavoriteListItem.MediaTypeText`, `CanRemove`).
- Fortschrittsbalken: `ProgressBar` mit `Progress` 0–1 (verfügbar in
  MAUI, siehe `MediaCarousel`-Fortschritt `HomeMediaItem.Progress`).
