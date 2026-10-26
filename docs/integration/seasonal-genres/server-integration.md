# Serverintegration der saisonalen Startseitenlisten

Stand: 2026-10-26. Übernahme auf aktuellem Server-`staging` unabhängig abgenommen und vom Anwender gemergt; PR-CI grün. Der anschließende Server-Prerelease ist erfolgreich abgeschlossen; das Self-Update der laufenden Instanzen wurde vom Anwender bestätigt und am lokalen Health-Endpunkt geprüft.

## Konkreter Übernahmestand

- Serverrepository: `https://github.com/martin-stromberg/VideoPlayer`.
- Gemergter PR gegen `staging`: [PR #287](https://github.com/martin-stromberg/VideoPlayer/pull/287).
- Integrationsbasis: `6a89228edf77a756563abbc7c211b8abe634533f` (aktueller `origin/staging` beim Start).
- Branch: `task/saisonlisten-schritt-2-server-integration`.
- Produktcommit: `e7e5e56aa175083e7f3d72e523fa87f62d55895c`.
- Vollständiger Integrations-HEAD: `b4f33de517136930434bd699f787b933cd8a6a89` (einschließlich reinem Abnahme-Dokumentationscommit).
- Eigener Checkout im Mobile-Workspace: `.work/seasonal-server-integration`.
- Portable Übernahme: [server-integration.bundle](server-integration.bundle), verifiziert. Voraussetzung ist der oben genannte Staging-Basiskommit; der Bundle-Ref zeigt exakt auf den Integrations-HEAD.

Der Diff der zuvor unabhängig geprüften Saisonlieferung wurde auf aktuellem `staging` übernommen, ohne die alte Feature-Basishistorie zusammenzuführen. Produktdateien passten unverändert. README und zweisprachige Release Notes mussten wegen zusätzlicher Stagingeinträge manuell in ihren aktuellen Kontext eingefügt werden; vorhandene Inhalte blieben erhalten. Die ursprünglichen saisonalen Abnahmeberichte sind als historische Nachweise mitgeliefert. Keine Paket-, Schema- oder fremden Featureänderungen.

## Übernahme ins ursprüngliche lokale Serverrepository

Der fertige Integrationsbranch wurde per lokalem Git-Fetch in `D:/Repositories/softwareschmiede/1f22729c-5f43-4f69-94c5-aee41f073cac` als neuer Branch importiert. Das ergänzt den neuen Ref samt benötigten Git-Objekten und `FETCH_HEAD`; es wechselt keinen Checkout.

Vorher und nachher überprüft:

- Aktiver Branch unverändert: `task/issue-277-1f22729c5f434f6994c5aee41f073cac-tv-sender-platzhalter-clips-gr`.
- Aktiver HEAD unverändert: `eae60a6003ac0619958331351d21c5d983cb175f`.
- Arbeitsdateien unverändert und sauber.
- Neuer Branch `task/saisonlisten-schritt-2-server-integration` zeigt auf `b4f33de517136930434bd699f787b933cd8a6a89`.

## Prüfungen auf aktueller Integrationsbasis

- Gesamte Lösung Debug: erfolgreich, 0 Fehler (141 vorhandene Warnungen).
- Gesamte Lösung Release: erfolgreich, 0 Fehler (141 vorhandene Warnungen).
- Exakter CI-Formatcheck `dotnet format VideoPlayer.sln --verify-no-changes --no-restore --severity error`: erfolgreich, Exitcode 0.
- Saisonale SQLite-/Zeitfenster-/Vertrags- und echte Web-Playwright-Tests: **12/12 bestanden**, 0 Skips (16 s).
- Vollständige Serversuite: **2434 bestanden, 0 Fehler, 2 bestehende Skips, 2436 gesamt**, Exitcode 0, etwa 5 Minuten.
- Vollständiger Diff gegen `origin/staging`: `git diff --check` erfolgreich.

Die zwei bereits bestehenden Skips betreffen Unix-Dateirechte unter Windows und den schon deaktivierten ContinueWatching-Singleton-Test. Keine Tests wurden für diese Integration deaktiviert, übersprungen oder abgeschwächt.

Ausführliche lokale Logs und TRX: `.work/server-integration-results/`. Im Serverbranch liegen nachvollziehbare Berichte und Übergabe unter `docs/projects/saisonlisten-integration/`; der Schritt bleibt bis grüner CI `In Arbeit`, Bearbeiter `pausiert` für die Übergabe. Die unabhängige Abnahme ist erfüllt und im Serverbranch als `acceptance-schritt-2.md` übernommen.

## Remote-CI und noch offene Freigaben

Letzte gelesene CI-Momentaufnahme: [PR CI for Staging, Run 40283574471](https://github.com/martin-stromberg/VideoPlayer/actions/runs/40283574471).

- Neuer Lauf für HEAD `b4f33de517136930434bd699f787b933cd8a6a89`: `pending` bei der letzten Lesung.
- PR wurde am 2026-10-26 um 12:01:12 UTC vom Anwender nach `staging` gemergt; Mergecommit `0df39e661348a4bd9b6f8d55313310dbc23c4a7b`.

Die unabhängige Abnahme des Integrationsstands `194f0e64eceb0fb403f9ee1eb03ed1977b2b9baa` ist **Erfüllt**; siehe [dauerhafte unabhängige Abnahme](server-integration-review.md). Der zusätzliche Commit `b4f33de517136930434bd699f787b933cd8a6a89` ändert ausschließlich den übernommenen Abnahmebericht, Handover und Schrittstatus; Produktdateien und Tests sind unverändert. Deshalb wurden keine Tests wiederholt. Vor einem Merge ist grüne CI auf dem aktuellen Dokumentations-HEAD erforderlich. Der bestehende Promotion-PR #256 (`staging` nach `main`) wurde nicht verändert oder gemergt: `staging` lag bei der Prüfung 276 Commits vor `main`; diese fremden Änderungen werden nicht nebenbei freigegeben.

Zum Zeitpunkt der PR-Übergabe war die Laufzeitinstanz noch nicht benannt. Inzwischen hat der Anwender das Self-Update bestätigt; siehe den aktuellen Abschnitt unten. Eine Main-Promotion und mobile Veröffentlichung wurden nicht durchgeführt. Der mobile Lifecycle bleibt wegen der dokumentierten Windows-E2E-Fehler offen; die erfolgreiche Serverprüfung bescheinigt keinen vollständig grünen mobilen Gesamtlauf.

## Abnahmeübernahme am 2026-10-26

Der Dokumentationscommit `b4f33de517136930434bd699f787b933cd8a6a89` wurde auf den bestehenden PR #287 gepusht. Das verifizierte Bundle und der neue lokale Featurebranch im ursprünglichen Serverrepository zeigen ebenfalls auf diesen HEAD. Aktiver ursprünglicher Branch, aktiver HEAD und Arbeitsdateien blieben unverändert. Der PR bleibt Draft; es erfolgte weder Ready-Markierung noch Merge oder Deployment. Der neue CI-Lauf [40283574471](https://github.com/martin-stromberg/VideoPlayer/actions/runs/40283574471) für genau diesen HEAD war bei der letzten Lesung `pending`; auf sein Ende wurde hier nicht gewartet.
## Laufende Serverinstanz nach Self-Update

Der Anwender bestätigte am 2026-10-26, dass die neue Serverversion durch Self-Update verteilt ist, unter anderem unter `http://localhost:83/`. Lokaler Smokecheck: `GET /` und `GET /api/health` antworteten mit HTTP 200 (`OK`); der saisonale Endpunkt `GET /api/home/seasonal-genres` antwortete ohne Bearer-Token erwartungsgemäß mit HTTP 401. Der [Server-Prerelease-Lauf für den Mergecommit](https://github.com/martin-stromberg/VideoPlayer/actions/runs/40319353867) ist erfolgreich abgeschlossen. Ohne Anmeldung wurde weder die Saison-Payload noch die konkrete Versionsnummer der lokalen Instanz geprüft. Der Server-Übernahme- und Bereitstellungsschritt ist nach Bestätigung des Anwenders erledigt; der separate mobile Testabschluss bleibt offen.
