# Datenmodell

## `DownloadManifestEntry`
Datei: `VideoPlayer.Storage/Entities/DownloadManifestEntry.cs`

SQLite-Tabelle `download_manifest` (nicht backup-pflichtig). Für die Anforderung relevante Eigenschaften:

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `int` | Primärschlüssel (AutoIncrement), Sortiergrundlage der Liste |
| `VideoId` | `int` | Medien-Id (indiziert) |
| `VideoType` | `string` | Medientyp (`movie`, `tvshowepisode`) |
| `FilePath` | `string` | Pfad der lokalen Videodatei — beim Löschen zu entfernen |
| `State` | `string` | Persistierter Zustand (`queued`/`downloading`/`completed`/`failed`/`cancelled`), Abbildung über `DownloadStateNames` |
| `Error` | `string?` | Fehlercode des letzten Fehlschlags |
| `Title` | `string` | Anzeigetitel |
| `UpdatedAtUtc` | `DateTime` | Letzte Aktualisierung |

## `DownloadListItem`
Datei: `VideoPlayer.Mvu/DownloadListItem.cs`

Anzeige-Snapshot eines Manifest-Eintrags (Record). Relevant:

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `EntryId` | `int` | Manifest-Id |
| `State` | `DownloadState` | Aktueller Zustand — Grundlage der Fehler-Filterung |
| `CanRetry` | `bool` | `State is Failed or Cancelled` — identische Zustandsmenge wie die Löschaktion |
| `CanDelete` | `bool` | Immer `true` |
| `MediaType`/`MediaId` | `string`/`long` | Identität für `ItemRemoved`-Meldungen |

## `DownloadsPageState`
Datei: `VideoPlayer.Mvu/DownloadsPageState.cs`

Seitenzustand der Downloads-Seite (Record). Relevant:

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Items` | `IReadOnlyList<DownloadListItem>` | Manifest-Einträge in Auftragsreihenfolge |
| `HasItems` | `bool` | `Items.Count > 0` — steuert Sichtbarkeit von `DeleteAllButton` |
| `OrphanedBytes`/`OrphanedText` | `long`/`string?` | Restdaten ohne Manifest-Eintrag |

Für die Aktion fehlt eine Ableitung „mindestens ein Eintrag in `Failed`/`Cancelled`" (Arbeitstitel `HasFailedItems`).
