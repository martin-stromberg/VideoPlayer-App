# Doku-Struktur und Flake-Nachweise

## `docs/help/`

Bestehende Struktur (flache `*.md`-Dateien + 3 Unterverzeichnisse):

- Fachliche Seiten (Auswahl): `app-grundgeruest.md`, `einstellungen-datenhaltung.md`, `serververwaltung-erststart.md`, `verbindung-anmeldung.md`, `netzwerk-synchronisation.md`, `startseite-medienlisten.md`, `saisonale-startseitenlisten.md`, `quellen-browsing-details.md`, `favoriten-profil.md`, `playlists.md`, `wiedergabe-pip.md`, `downloads.md`, `offline-wiederherstellung.md`, `erscheinungsbild.md`, `staging-branch.md`
- `developer-api.md` — lokaler Mini-HTTP-Server (Debug/Opt-in) für UI-Automatisierung.
- `e2e-tests.md` — beschreibt `VideoPlayer.E2E.Tests` (Voraussetzungen: Windows mit Desktop-Session, sequentielle `E2E`-Collection, eigener Debug-Build; erwartetes Ergebnis „19 Tests, alle bestanden, ca. 20–25 min" — **Anmerkung: aktuell 19 Testklassen/74 Testfälle; Zählung der Datei teils veraltet**). Kein Hinweis auf CI-Ausführung — korrekt, da die Suite in keinem Workflow läuft.
- `index.md` — verlinkt alle Seiten, gegliedert in „App" und „Infrastruktur"; eine neue Seite müsste dort eingehängt werden.
- Unterverzeichnisse: `api/` (`seasonal-genres.md`), `api-client/` (6 Dateien), `tv-app/` (5 Dateien).

**Nicht vorhanden:** `docs/help/stabilitaet/` bzw. jede Seite zu Lane-Regeln, Trait-Konvention, Retry oder Quarantäne (Anforderung: zu erzeugen).

## Flake-Nachweise im Repo

Unter `docs/features/task/38598ab90d844bb0bbc42a195643b6e3-saisonabhaengge-listen/` dokumentierte, datierte E2E-Flake-Belege (lokale Läufe vom 26.10.2026):

| Dokument | Befund |
|----------|--------|
| `diagnosis-winrt-shutdown.md` | CsWinRT-GC-Finalizer → `Marshal.Release`/`DisposeTrackerSource` → `0xC0000005` beim geordneten App-Shutdown (`CloseMainWindow`-Phase); `process.json`-Beleg `exitedBeforeShutdown=false`, kein Fixture-Kill; 2/2 Wiederholungen grün → sporadisch. Ursache nicht abschließend bestimmt, kein Produktfix. |
| `diagnosis-stream-failure-exit.md` | `PlaylistPlaybackTests.StreamFailure_AdvancesToNextPlaylistEntry`: nativer Exit `0xC000027B` ~370–390 ms nach `shutdownStartedAtUtc` in 2 aufeinanderfolgenden Läufen; Assertions zuvor erfolgreich — nur die getrennte Aufräumphase meldete den Exit; isolierter Folgelauf 1/1 grün. |
| `native-dump-analysis.md` / `native-dump-investigation.md` | WinRT-Stowed-Exception `E_UNEXPECTED` im UI-Messaging-Pfad nach Fixture-Shutdown-Beginn (PID 27688, anderer Prozess, gleicher Exitcode `0xC000027B` — Indiz, kein Beleg gleicher Ursache). |
| `test-results*.md` | Lokale E2E-Läufe mit 69/72 bzw. 70/72 bestandenen Tests (laut `issue.md`), u. a. `WaitForName`-Timeouts. |

Weitere Diagnose-/Review-Dokumente (`continue.md`, `implementation-*.md`, `review-*.md`, `plan*.md`, `todo.md`) beschreiben die iterative Härtung, aus der der heutige Stand von `AppInstance`/`E2ETestBase` stammt (Diagnose-Protokolle, `forcedKill`-Regel, `AggregateException`-Aufräumphase, begrenzte Desktop-Baumsuche).

**Befund:** Die belegten Flakes entstehen überwiegend in der Shutdown-/Cleanup-Phase nativer App-Prozesse und bei Warte-Timeouts — beides Bereiche, die die Anforderung adressiert (Best-Effort-Lane, Cleanup-Retry, Deadline-Staffelung).
