# Übersetzte Anforderung: Downloads-Übersicht ohne Flackern bei der Fortschrittsanzeige (Entwicklungsschritt 2)

## Fachliche Zusammenfassung

Auf der Downloads-Seite der neuen MAUI-App (`VideoPlayer.Maui`,
`AppPage.Downloads`, Route `downloads`) flackert laut Kundenrückmeldung
die gesamte Anzeige, sobald ein Download eingeplant ist oder läuft und
sich der Fortschrittsbalken verändert. Ursache (durch die Projekt-
Bestandsaufnahme verifiziert): Die Seite reagiert auf jede Modell-
änderung — also auch auf jeden Fortschritts-Tick der gedrosselten
`DownloadItemChangedMessage` (250 ms) — mit einem neuen `ItemsSource`-
Wert auf dem `CollectionView`, weil das unveränderliche MVU-Modell bei
jeder Änderung eine neue `Items`-Liste erzeugt (`UpsertItem` kopiert die
Liste). Der CollectionView verwirft daraufhin alle Zellen und baut die
komplette Liste neu auf — sichtbar als Flackern der ganzen Seite.

Gefordert ist ein Aktualisierungspfad, der die bereits granular
gemeldeten Änderungen (`DownloadItemChangedMessage` mit dem geänderten
`DownloadListItem`) nutzt: Während laufender oder geplanter Downloads
aktualisiert sich nur der Fortschrittsbalken bzw. nur der betroffene
Listeneintrag — die übrige Anzeige wird nicht neu gezeichnet und ruckelt
nicht.

## Betroffene Klassen und Komponenten

- **MVU-Kern (`VideoPlayer.Mvu`, plattformneutral, testbar):**
  - `AppUpdate.UpdateDownloadItemChanged` (AppUpdate.cs:1758-1769) —
    upsertet den geänderten Eintrag bereits granular in
    `Downloads.Items` (Identität = `EntryId` bzw. `MediaId`+`MediaType`,
    `UpsertItem` AppUpdate.cs:1818-1835); liefert aber jedes Mal eine
    neue Listeninstanz
  - `DownloadListItem` (VideoPlayer.Mvu/DownloadListItem.cs) —
    unveränderlicher Anzeige-Record mit abgeleiteten Anzeigewerten
    (`Progress`, `ProgressPercentText`, `StateText`, `CanCancel`,
    `CanRetry`, `ShowProgress`, `ErrorText`, `MediaTypeText`)
  - `DownloadsPageState.Items` (DownloadsPageState.cs:25) —
    `IReadOnlyList<DownloadListItem>`, wird per `with` ersetzt
  - `DownloadService.ReportProgressThrottledAsync`
    (DownloadService.cs:704-724) — drosselt Fortschrittsmeldungen auf
    250 ms (`ProgressInterval`)
- **App-Rand (`VideoPlayer.Maui`):**
  - `Pages/DownloadsPage.xaml` — `CollectionView` mit
    `ItemsSource="{Binding Model.Downloads.Items}"` (Zeile 60)
  - `Pages/DownloadsPage.xaml.cs` — `OnModelChanged` → `UpdateUi`;
    Aktions-Handler dispatchten `DownloadCancelRequestedMessage` /
    `DownloadRetryRequestedMessage` / `DownloadDeleteRequestedMessage`
    mit dem `DownloadListItem` aus dem `BindingContext`
- **Tests (`VideoWebPlayer.Maui.Tests`):**
  - `DownloadsContractTests` — pinnen u. a. das ItemsSource-Binding
    `{Binding Model.Downloads.Items}` (Zeile 24)
  - `AppUpdateTests_Downloads` — MVU-Übergänge der Download-Messages
    (bleiben unverändert grün)

## Implementierungsansatz (frei wählbar, kleinster sauberer Eingriff)

Kandidaten aus der Bestandsaufnahme (Offene Frage 7):

1. Stabile `ObservableCollection` am Seitenrand mit In-Place-Updates
   (Identitäts-Abgleich, nur geänderte Einträge anfassen).
2. `DownloadListItem` als `INotifyPropertyChanged`-Objekt mit Property-
   Updates statt Record-Ersatz.
3. `ItemsSource` nur bei Identitäts-/Strukturänderung neu setzen.

Kombination aus 1+2 am Seitenrand ist der sauberste Eingriff: Das MVU-
Modell bleibt unveränderlich; eine `ObservableCollection` von
Änderungs-benachrichtigenden Eintrags-ViewModels wird einmal als
`ItemsSource` gesetzt und pro Modelländerung abgeglichen (entfernen/
einfügen nur bei Strukturänderung, Property-Updates in-place sonst).

## Akzeptanzkriterien

- Bei eingeplantem/laufendem Download verändert sich sichtbar nur der
  Fortschrittsbalken des betroffenen Eintrags; kein Flackern oder
  Neuzeichnen der übrigen Anzeige.
- Die bestehenden Download-Übergänge (Laden, Einreihen, Abbrechen,
  Löschen, Fortsetzen, automatische Downloads) funktionieren unverändert.
- Alle bestehenden Tests bleiben grün.
- DoD: Build Debug + Release fehlerfrei, volle Testsuite grün.

## Offene Fragen

- Keine — Ursache und Lösungsraum sind durch die Bestandsaufnahme
  eingegrenzt; die Wahl des Aktualisierungspfads ist freigestellt.
