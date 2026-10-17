# Testergebnisse: Episodenlisten — Angesehen-Symbol bei gesehenen Episoden

## Ergebnis

**Status:** Keine Fehler

## Builds

| Build | Kommando | Ergebnis |
|-------|----------|----------|
| Debug (Lösung) | `dotnet build VideoPlayer.App.sln` | 0 Fehler, Warnungen nur Altbestand |
| Release (Lösung) | `dotnet build VideoPlayer.App.sln -c Release` | 0 Fehler, Warnungen nur Altbestand + Plattform-/Linker-Hinweise |

Hinweis: Der erste Debug-Build scheiterte an `Stretch` statt `Aspect` am
Augen-`Path` (MAUIX2002); nach der Korrektur sind beide Builds fehlerfrei.

## Testsuite

| Lauf | Kommando | Ergebnis |
|------|----------|----------|
| Volle Suite | `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj` | 743 Tests, 743 bestanden, 0 fehlgeschlagen, 0 übersprungen |
| Coverage-Lauf | `dotnet test … --collect:"XPlat Code Coverage"` | 743/743 bestanden; Bericht unter `inventory/test-results/` |

Baseline vor der Umsetzung: 728/728. Neu hinzugekommen: 15 Tests.

## Neue Tests dieser Umsetzung

| Testklasse | Tests | Nachweis |
|------------|-------|----------|
| `MediaEntrySerializationTests` | +2 | `WatchedAt`-Deserialisierung gesetzt (`episode`-JSON, camelCase) / fehlend → `null` |
| `MediaDetailsServiceTests` | +3 | `WatchedAt`-Mapping online, Offline-Snapshot-Roundtrip, Legacy-Cache ohne Feld → ungesehen |
| `AppUpdateTests_Player` | +6 | Markierung bei `PlaybackEnded` und Watched-Fenster, Auto-Fortsetzung, Server-Zeitpunkt bleibt erhalten, Movie-Wiedergabe unberührt, unbekannte Episode → Identität unverändert |
| `MediaDetailsPageContractTests` | +3 | Badge im Episoden-Template (`IsWatched`-Binding), Web-Design-Merkmale (Pill, Tokens, Auge, oben links), `EpisodeItem`-Vertrag |
| `DesignTokensContractTests` | +1 | `VpBadgeBg` #AD000000 / `VpBadgeBorder` #47FFFFFF gepinnt |

## Testabdeckung (XPlat Code Coverage)

**Abdeckung:** 84,7 % Zeilen gesamt.

| Geänderte Datei | Abdeckung |
|-----------------|-----------|
| `MediaEntry.cs` (alle Media-Typen) | 100 % |
| `MediaDetailsService.cs` | 97,0 % |
| `AppUpdate.cs` | 96,8 % |
| `MediaDetailsView.cs` (`EpisodeItem`) | 85,7 % |
| `ShowDetailsCache.cs` | 80,6 % |

Alle neuen Zeilen (`WatchedAt`-Mapping, `MarkEpisodeWatchedInDetails`,
`IsWatched`) sind abgedeckt. Die übrigen Dateien unter 80 % sind
Altbestand (u. a. `VideoWebPlayer.Client`-DTOs, Discovery-/Sync-Dienste)
und von dieser Änderung nicht betroffen.

## E2E-Abdeckung

Kein UI-E2E-Harness im Testprojekt (kein Playwright/Appium; MAUI-Schicht
nicht instanziierbar). Funktionsnachweis über MVU-Übergangstests und
Quell-Vertragstests — das etablierte Muster der UI-Schritte 5 und 6.
Manuelle Verifikation der Darstellung steht beim Gerätetest des Anwenders
aus (Kapitel „Offene Punkte" des Plans: keine).

## Fehlgeschlagene Tests

Keine.
