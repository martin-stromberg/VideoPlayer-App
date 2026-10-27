# Code-Review — Downloads-Übersicht ohne Flackern (Schritt 2)

## Ergebnis

**Status:** Keine Befunde

## Geprüfter Diff (Basis: `task/issue-9-…-neugestaltung-der-app`)

- `VideoPlayer.Mvu/DownloadItemViewModel.cs` (neu)
- `VideoPlayer.Mvu/DownloadListSync.cs` (neu)
- `VideoPlayer.Maui/Pages/DownloadsPage.xaml` / `.xaml.cs`
- `VideoWebPlayer.Maui.Tests/DownloadListSyncTests.cs` (neu),
  `DownloadsContractTests.cs`

## Prüfpunkte

| Punkt | Befund |
|-------|--------|
| Flicker-Ursache beseitigt | `ItemsSource`-Binding entfernt; `ItemsSource` wird einmalig gesetzt (`DownloadsPage.xaml.cs:31`); `Sync` erzeugt bei Inhalts-Updates kein `CollectionChanged` (Test `Sync_ProgressTick_UpdatesInPlace_WithoutCollectionChanged` belegt es). |
| Korrektheit `Sync` | Pass 1 (rückwärts): Entfernen + in-place `Update`; danach ⊆ Ziel. Pass 2: Positionsgleichheit → Skip, sonst `Move` vorhandener bzw. `Insert` neuer VM — deckt Anhängen, Einfügen mittendrin, Reorder ab. `IndexOf` ab `i+1` verhindert Selbst-Match; Loop-Invariante „Positionen < i korrekt" hält. |
| Identitätsregel | `EntryId` **oder** `MediaId`+`MediaType` — identisch zu `UpsertItem` (AppUpdate.cs:1822-1824). Konsistent. |
| MVU-Unveränderlichkeit | `DownloadListItem` bleibt `record`; `AppUpdate`/`DownloadsPageState` unverändert — `Assert.Same`-basierte Tests unberührt. |
| Threading | `Sync` läuft ausschließlich im Konstruktor und in `UpdateUi` (via `MainThread.BeginInvokeOnMainThread`) — UI-Thread; `ObservableCollection` nicht thread-sicher, hier eingehalten. |
| Dispatch-Korrektheit | Klick-Handler entpacken `viewItem.Item` — Request-Messages erhalten den aktuellen Record; `CanCancel`/`CanRetry`-Guards bleiben in `AppUpdate` (doppelte Absicherung). |
| `Update`-Kurzschluss | Record-`Equals` deckt alle Anzeige-Eingaben ab (keine externen abgeleiteten Daten) → kein Benachrichtigungs-Rauschen bei Gleichstand (Test `Sync_UnchangedItems_NoNotifications`). |
| PropertyChanged-Vollmeldung | `Update` meldet alle Anzeige-Properties bei echter Änderung — Bindings mit unverändertem Wert sind no-ops; akzeptabel und dokumentiert. |
| Hooks/Konventionen | Deutsche XML-Doku vollständig (CS1591-fähig), kein `NotImplementedException`, kein neuer Enum, keine Legacy-Hexfarben, keine neuen NuGets, keine DB-Änderungen. |
| Fremddateien | `docs/features/task/issue-9-customer-feedback.md` unberührt (untracked, nicht gestaged). |

## Befunde

Keine.

## Hinweise

- `PropertyChanged(nameof(Item))` wird zusätzlich gemeldet, obwohl
  `Item` nicht gebunden ist — bewusst konservativ (Imperative Zugriffe
  sehen ohnehin den aktuellen Record über den Getter).
- Der Sync-Aufwand ist O(n²) im schlechtesten Fall (FirstOrDefault je
  Eintrag); die Download-Liste ist klein (Manifest-Aufträge) — kein
  praktisches Problem.
