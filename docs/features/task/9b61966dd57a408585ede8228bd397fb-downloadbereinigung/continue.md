# Offene Aufgaben

Erstellt am: 2026-10-27
Abbruchgrund: Maximale Iterationsanzahl erreicht

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

Keine — `review.md` meldet „Vollständig umgesetzt".

## Code-Review-Befunde

- [ ] `DownloadService.cs` — God-Methode `ReconcileAsync` (~165 Zeilen): sechs getrennte Schritte in einer Methode; den Ablauf-Pass in `RunExpiryPassAsync` auslagern
- [ ] `DownloadService.cs` — Long Parameter List `EnqueueAsync` (9 Parameter): den internen `EnqueueRequest`-Record öffentlich machen und die Signatur darauf reduzieren
- [ ] `AutoDownloadService.cs` — Doppelter Code: Playlist-Bindungs-Bedingung `item.PlaylistId is > 0 && item.PlaylistEntryId is > 0` steht 4× inline → `HasPlaylistBinding`-Hilfsmethode
- [ ] `DownloadsPage.xaml` — Doppelter Code: „Cache"-Chip dupliziert das Zustands-Chip-Markup nahezu identisch → gemeinsamen `Style` auslagern
- [ ] `Stubs/FakeNetworkMonitor.cs` — Toter Code: `SetOnline` hat keinen Aufrufer im Testprojekt
- [ ] `VideoPlayer.Maui.Tests` — Test-Duplikation: `ReadEntryAsync` und `InsertCompletedAsync` nahezu identisch in `DownloadServiceTests_Expiry.cs` und `DownloadServiceTests.cs` (plus dritte `ReadEntryAsync`-Variante in `DownloadServiceReconcileTests.cs`) → gemeinsame Hilfsklasse unter `Stubs/`

## Fehlgeschlagene Tests

Keine — `test-results.md` meldet „Keine Fehler" (1440/1440 bestanden).
