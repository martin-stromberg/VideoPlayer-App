# Übersetzte Anforderung: Scrollposition beim Nachladen der Quellen-Übersicht erhalten

Herkunft: Issue #49, Entwicklungsschritt 3 (Projektplan, Originaltext liegt dem Lauf bei).
`docs/features.md` existiert nicht — optionaler Projektkontext entfällt.

## Fachliche Zusammenfassung

Beim automatischen Nachladen der nächsten Ergebnisseite in der Quellen-Übersicht
(`SourceOverviewPage`, Auslöser `RemainingItemsThresholdReached` bei `RemainingItemsThreshold="6"`)
springt die Trefferliste an den Anfang, weil die an `ItemsCollection.ItemsSource` gebundene
Liste beim Anhängen komplett ausgetauscht wird (`UpdateSourceItemsLoaded` erzeugt per
`Concat().ToList()` eine neue `Items`-Instanz → `ItemsSource`-Referenzwechsel →
`CollectionView`-Reset). Soll: Beim Nachladen einer Folgeseite bleibt die Scrollposition
erhalten; die neuen Einträge erscheinen am Ende angehängt. Das gilt für die ungefilterte
Liste sowie für Such- und Genre-Treffer gleichermaßen (derselbe Anzeige- und Nachladepfad).
Erstladen, Genre-Wechsel und neue Suche zeigen die Liste weiterhin von Anfang an
(Listenersatz/Reset ist dort ausdrücklich erwünscht). Die technische Umsetzungsform ist
frei — entscheidend ist nur, dass die Ansicht beim Nachladen nicht springt.

## Betroffene Klassen und Komponenten

- `VideoPlayer.Maui/Pages/SourceOverviewPage.xaml` — `ItemsCollection` mit
  `ItemsSource="{Binding Model.SourceBrowser.Items}"` und `RemainingItemsThreshold="6"`.
- `VideoPlayer.Maui/Pages/SourceOverviewPage.xaml.cs` — `UpdateUi`, `OnRemainingItemsThresholdReached`.
- `VideoPlayer.Mvu/SourceBrowserState.cs` — `Items` (`IReadOnlyList<SourceMediaItem>`),
  `NextPage`, `HasMore`, `IsLoading`, `IsLoadingMore`.
- `VideoPlayer.Mvu/AppUpdate.cs` — `UpdateSourceItemsLoaded` (Append: `Concat().ToList()`),
  `UpdateSourceItemsNextPageRequested`, `ReloadBrowserItems`.
- `VideoPlayer.Mvu/SourceMediaItem.cs` — Eintragsmodell (unveränderliches Record).
- `VideoPlayer.Mvu/DownloadListSync.cs` + `VideoPlayer.Maui/Pages/DownloadsPage.xaml.cs` —
  bestehendes Präzedenzmuster: stabile `ObservableCollection` als ItemsSource plus
  granularer Sync-Helper im Mvu-Projekt (unit-testbar).
- Tests: `VideoWebPlayer.Maui.Tests` (Unit-Tests des Mvu-Projekts,
  Vorlage `DownloadListSyncTests`), `VideoPlayer.E2E.Tests`
  (`SourceBrowsingTests`, `SourceSearchTests` als Vorlagen; `MockVideoServer` paginiert
  bereits über `page`/`size`).

## Implementierungsansatz

- Die gebundene Liste (`ItemsSource` der `ItemsCollection`) darf beim Anhängen nicht
  ausgetauscht werden. Naheliegende, bereits im Projekt etablierte Lösung: stabile
  `ObservableCollection<SourceMediaItem>` im Code-Behind der Seite (einmal als
  `ItemsSource` gesetzt) plus ein Sync-Helper im `VideoPlayer.Mvu`-Projekt analog zu
  `DownloadListSync`, der die gebundene Liste mit `SourceBrowser.Items` abgleicht —
  angehängte Folgeseiten erzeugen nur `Add`-Ereignisse (Scrollposition bleibt),
  Listenwechsel (Erstladen, Genre-Wechsel, neue Suche, Retry) setzen die Liste neu.
- Der MVU-Zustand (`SourceBrowserState.Items`) bleibt unveränderlich; keine Änderung
  am Modell, an `AppUpdate` oder am Ladepfad nötig — das gleiche Verhalten gilt damit
  automatisch für ungefilterte, Such- und Genre-Treffer.
- Identitätsregel des Abgleichs: `SourceMediaItem.Id`.

## Konfiguration

Keine — das Verhalten ist fest verdrahtet, kein Konfigurationsbedarf in der Anforderung.

## Offene Fragen

Keine (der Anwender ist nicht erreichbar; die Anforderung lässt die Umsetzungsform
ausdrücklich frei und nennt die inkrementelle Erweiterung der gebundenen Liste als
Beispiel — das etablierte `DownloadListSync`-Muster deckt genau das ab).
