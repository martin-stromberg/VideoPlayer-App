# Plan-Check — Downloads-Übersicht ohne Flackern (Schritt 2)

## Ergebnis

**Status:** Plan vollständig

## Prüfung gegen die Anforderung

| Kriterium | Abdeckung im Plan |
|-----------|-------------------|
| Fortschrittsbalken aktualisiert sich flüssig, nur betroffener Eintrag ändert sich | Stabile `ObservableCollection` + `DownloadItemViewModel.Update` → nur `PropertyChanged` am Eintrag, kein `CollectionChanged`, kein ItemsSource-Reset. Trifft die verifizierte Ursache (neue Listeninstanz pro Tick) direkt. |
| Kein Flackern/Neuzeichnen der übrigen Anzeige | In-place-Updates erzeugen keine Collection-Events; `UpdateUi` schreibt Quota-/Status-Labels mit unveränderten Werten (no-op). |
| Bestehende Download-Übergänge unverändert | MVU-Kern (`AppUpdate`, Messages, Effekte, `DownloadService`) wird nicht angefasst; Request-Messages werden mit dem gekapselten `DownloadListItem` (`viewModel.Item`) dispatcht — `CanCancel`/`CanRetry`-Guards greifen unverändert. |
| Kleinster sauberer Eingriff | Zwei neue kleine Typen in `VideoPlayer.Mvu`, eine Page angepasst; kein Umbau des Modells. |
| DoD Debug+Release+Tests | Build-Läufe und volle Testsuite im Ablauf vorgesehen. |

## Prüfung des Testbedarfs

| Bedarf | Geplant |
|--------|---------|
| Neuer Update-Pfad per xUnit testbar | `DownloadItemViewModel` + `DownloadListSync` liegen in `VideoPlayer.Mvu` — vom Testprojekt referenzierbar (nur Mvu/ApiClient/Storage/Client). |
| In-place-Verhalten nachgewiesen | Test „kein CollectionChanged bei Fortschritts-Tick" + „keine Benachrichtigung bei unverändertem Record". |
| Strukturänderungen | Insert/Remove/Move-Tests. |
| Identitätsregeln | EntryId- und MediaId+MediaType-Abgleich getestet (Spiegel von `UpsertItem`). |
| UI-Pins | `DownloadsContractTests` wird angepasst: alter Pin `{Binding Model.Downloads.Items}` entfällt zwingend (wird entfernt), neue Pins auf `DownloadsCollection`, `DownloadItemViewModel`, `DownloadListSync.Sync`. |
| Bestandstests | `AppUpdateTests_Downloads` bleibt unverändert grün (MVU-Seite unangetastet). |

## Risiken / geprüfte Details

- `ObservableCollection<T>.Move` existiert (nicht in `IList`) —
  `Sync`-Signatur bindet bewusst `ObservableCollection`.
- Record-Gleichheit von `DownloadListItem` deckt alle Anzeige-Eingaben
  ab (keine externen abgeleiteten Daten) → `Equals`-Kurzschluss in
  `Update` ist korrekt.
- Klick-Handler-Umstellung auf `DownloadItemViewModel` ist vollständig
  (drei Handler + DataTemplate-`x:DataType`); BindingContext der Zelle
  wird zum VM — `Item` liefert den Record für die Messages.
- Keine neuen Enums, Tabellen oder NuGet-Pakete → Hook-/DoD-
  Zusatzpflichten entfallen.
- Bekanntes Muster `ModelChanged +=` ohne Unsubscribe (Bestandsaufnahme
  §6) bleibt unverändert — nicht Teil dieses Schritts.

## Hinweise

Keine.
