# Testergebnisse

## Ergebnis

**Status:** Keine Fehler

## Läufe

| Lauf | Kommando | Ergebnis |
|------|----------|----------|
| 1 (Erstlauf) | `dotnet test VideoPlayer.Maui.Tests` | 1 Fehler: eigener neuer Contract-Test `Overview_RendersBothSectionsAndDispatchesMessages` — `SortModeText`/`GenreText` wurden im Controller statt in der Zellen-Datei erwartet (Testfehler, korrigiert) |
| 2 (tvOS-Tests) | `dotnet test VideoPlayer.Maui.Tests --filter "FullyQualifiedName~TvOs"` | 58/58 bestanden |
| 3 (Gesamtsuite) | `dotnet test VideoPlayer.Maui.Tests` | **1498/1498 bestanden**, 0 übersprungen |

## Neue Tests

`TvOsPlaylistsContractTests` (6 Tests): Übersichtsseite,
Detailseite/Paging, Navigator-Präsentation, Menü-Verdrahtung,
Effekt-Verdrahtung inkl. Cover-Endpunkt, Verwaltungs-Ausschluss
(Negativprüfung).

## Fehlgeschlagene Tests

Keine (nach Korrektur des eigenen Contract-Tests).
