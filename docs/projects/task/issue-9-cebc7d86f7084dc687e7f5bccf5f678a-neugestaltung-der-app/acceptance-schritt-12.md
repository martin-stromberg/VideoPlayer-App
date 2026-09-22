# Abnahmeprüfung Entwicklungsschritt 12 – Offlinebetrieb und Wiederherstellung

Datum: 2025 (Schritt-Branch `task/issue-9-…-schritt-12-offline-wiederherstellung`, Implementierungscommit `0b76fa9`)
Prüfung: unabhängige fachliche Abnahme gegen die Anforderung (nicht durch den Implementierungsagenten).

## Akzeptanzkriterien

| Kriterium | Nachweis | Ergebnis |
|---|---|---|
| Offline-Startseite aus Cache; „Weiterschauen" nur aus lokalen Downloads | `VideoPlayer.Mvu/OfflineContinueWatchingService.cs` — nur `completed`-Manifest-Einträge mit vorhandener Datei; Favoriten/Neuheiten aus `HomeListService`-Caches | Erfüllt |
| Fehlende Episoden als Platzhalter | `OfflineContinueWatchingService` bestimmt Folge-Episoden aus `ShowDetailsCache`; nicht heruntergeladene Folgen werden als `HomeMediaItem.IsPlaceholder` dargestellt (nicht abspielbar) | Erfüllt |
| Heruntergeladene Videos offline abspielbar | `PlaybackSourceService` (Schritt 10/11) bevorzugt lokale Manifest-Dateien — kein Serverzugriff nötig | Erfüllt |
| Favoriten über lokale Kopie | `HomeListService.LoadFavoritesAsync` liest `cached_favorites` | Erfüllt |
| Einschränkungen: keine neuen Inhalte, keine Quellenliste, kein Profilabruf | Offline-Guards in `AppUpdate`/`AppModel`; Tests `AppUpdateTests_Offline` (Quellen/Profil/Downloads im Offline-Modus blockiert mit klarer Meldung) | Erfüllt |
| Wiederherstellung: Auto-Verbindung zum zuletzt genutzten Server | `AuthSessionService`-Reconnect-Pfad beim Start; `KnownServersService` liefert letzten Server | Erfüllt |
| Erneute Anmeldung bei fehlenden Geheimnissen | `AppUpdate.ReloginRequiredNotice` — klare Anmeldeaufforderung statt technischem Fehler; Test `AutoLoginCompleted_MissingCredentials_*` | Erfüllt |
| Bei Nichterreichbarkeit: Offline-Modus (mit Daten) oder Serverauswahl | Verbindungs-Workflow aus Schritt 5 + Offline-Pfad; in `AppUpdateTests_Offline` abgedeckt | Erfüllt |
| Nach Verbindung: voller Metadaten-Sync, `pending_progress` nachsenden, Weiterschauen aktualisieren | Orchestrierung über bestehende Sync-Effekte (`ShellAppEffectHandler`), `ProgressReportService`-Outbox, Home-Listen-Refresh | Erfüllt |
| Beschädigte Caches → verwerfen + neu synchronisieren | `HomeListService` (verwirft `cached_lists`/`cached_favorites` bei JSON-/SQLite-Fehlern), `HomeLists.CacheDiscarded`-Flag → Resync-Effekt bei Verbindung | Erfüllt |
| Beschädigte Datenbank → neu initialisieren + Nutzer informieren | `LocalDatabaseCorruptedMessage` (alte Datei → `*.corrupt-*`, Neuinitialisierung), `AppModel.StorageNotice` → Startmeldung; Tests `LocalDatabaseTests` | Erfüllt |

## Befund der Abnahme

- Echter Produktionsfehler im Schritt gefunden und behoben: `ShowDetailsCache.ReadAsync` nutzte `KeyFor(showId)` innerhalb des sqlite-net-`Where`-Prädikats → Laufzeit-`SQLiteException` (stumm geschluckt) → jeder Offline-Serienstruktur-Zugriff wäre leer gewesen. Fix: Schlüssel vor der Abfrage materialisiert (`ShowDetailsCache.cs`).

## Prüfung

- Code-Stichproben in `OfflineContinueWatchingService.cs`, `ShowDetailsCache.cs`, `HomeListService.cs`, `AppUpdate.cs` (Relogin-/Discard-Verdrahtung), `AppMessage.cs`.
- Abnahme-Testlauf: `dotnet test VideoWebPlayer.Maui.Tests` → **623 bestanden, 0 fehlgeschlagen** (Basis 580 + 43 neue Tests: `AppUpdateTests_Offline`, `OfflineContinueWatchingServiceTests`, `OfflineRecoveryContractTests`, Korruptionstests in `HomeListServiceTests`/`LocalDatabaseTests`).
- Windows-MAUI-Build laut Lifecycle-Nachweis: 0 Warnungen, 0 Fehler.
- Pre-Commit-Hooks liefen bei allen Commits des Schritts fehlerfrei.

## Ergebnis

**Abgenommen.** Alle Akzeptanzkriterien erfüllt; Schritt 12 kann in den Basisbranch gemergt werden. Damit sind alle 12 Entwicklungsschritte des Projekts abgeschlossen.
