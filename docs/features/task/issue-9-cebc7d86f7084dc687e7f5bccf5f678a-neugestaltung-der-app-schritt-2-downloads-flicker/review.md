# Plan-Review — Downloads-Übersicht ohne Flackern (Schritt 2)

## Ergebnis

**Status:** Vollständig umgesetzt

## Prüfung der Planelemente

| Planelement | Umgesetzt | Nachweis |
|-------------|-----------|----------|
| `DownloadItemViewModel` (neu, Mvu, INPC, kapselt `DownloadListItem`, `Update` mit Record-Gleichheits-Kurzschluss) | Ja | `VideoPlayer.Mvu/DownloadItemViewModel.cs` — alle gebundenen Anzeige-Properties delegiert, `Update` meldet `PropertyChanged` nur bei Ungleichheit |
| `DownloadListSync` (neu, Mvu, statisch; Entfernen/In-place-Update/Insert/Move; Identität = EntryId oder MediaId+MediaType) | Ja | `VideoPlayer.Mvu/DownloadListSync.cs` — zweiphasiger Abgleich wie geplant |
| ItemsSource einmalig im Konstruktor setzen, XAML-Binding entfernen | Ja | `DownloadsPage.xaml.cs:22,31`; `DownloadsPage.xaml:62-64` ohne `ItemsSource`-Binding |
| DataTemplate-`x:DataType` auf `mvu:DownloadItemViewModel` | Ja | `DownloadsPage.xaml:65` (VM liegt in `VideoPlayer.Mvu` → `mvu:`-Namespace, nicht `pages:` — Plan entsprechend korrigiert umgesetzt) |
| Klick-Handler dispatchten mit `viewItem.Item` | Ja | `DownloadsPage.xaml.cs:64-86` (Cancel/Retry/Delete) |
| MVU-Kern unverändert | Ja | `git diff` zeigt keine Änderung an `AppUpdate.cs`, `DownloadsPageState.cs`, `DownloadListItem.cs`, `AppMessage.cs`, `AppEffect.cs` |
| `DownloadListSyncTests` (neu) | Ja | `VideoWebPlayer.Maui.Tests/DownloadListSyncTests.cs` — 10 Tests: In-place ohne CollectionChanged, keine Benachrichtigung bei Gleichheit, Remove/Insert/Move, Identität via MediaId+MediaType, VM-Delegation |
| `DownloadsContractTests` angepasst | Ja | Pin `{Binding Model.Downloads.Items}` ersetzt durch `x:Name="DownloadsCollection"` + `DoesNotContain("ItemsSource=\"{Binding")` + `x:DataType`-Pin; neuer Test `DownloadsPage_SyncsItems_WithoutItemsSourceReset` pinnt die Code-Behind-Verdrahtung |

## Offene Aufgaben

Keine.

## Hinweise

- Plan nannte `xmlns:pages` für das DataTemplate; da das ViewModel
  bewusst im `VideoPlayer.Mvu`-Projekt liegt (Testbarkeit — Projekt-
  konvention), wurde `x:DataType="mvu:DownloadItemViewModel"` ohne
  zusätzliches Xmlns umgesetzt. Inhaltlich gleichwertig.
- Das bekannte `ModelChanged +=`-Muster ohne Unsubscribe
  (Bestandsaufnahme §6) wurde nicht angefasst — gehört nicht zu diesem
  Schritt.
