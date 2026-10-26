# Serverlieferung: Saisonale Genre-Listen
## Aktueller Übernahmestand (2026-10-26)

Die Serveränderung ist auf aktuellem `staging` integriert, unabhängig abgenommen und als [PR #287](https://github.com/martin-stromberg/VideoPlayer/pull/287) vom Anwender nach `staging` gemergt. Alle erforderlichen GitHub-CI-Checks sind grün. Details, neuer Integrationsbranch und separates Bundle stehen im [Integrationsbericht](server-integration.md); der [unabhängige Review](server-integration-review.md) ist erfüllt. Der Branch ist auch im ursprünglichen lokalen Serverrepository importiert, ohne dessen aktiven Checkout zu ändern. Mergecommit ist `0df39e661348a4bd9b6f8d55313310dbc23c4a7b`; der Server-Prerelease ist erfolgreich abgeschlossen. Der Anwender bestätigte das Self-Update der Serverinstanzen, darunter `http://localhost:83/`. Dort waren Startseite und `api/health` mit HTTP 200 erreichbar; der saisonale API-Endpunkt verlangte ohne Anmeldung erwartungsgemäß einen Bearer-Token (HTTP 401). Ein authentifizierter Payload-Abruf und die exakte lokale Versionsnummer wurden nicht geprüft.

Der mobile Gesamtlauf bleibt offen; die [aktuellen Testergebnisse](mobile-test-results.md) dokumentieren den nachgewiesenen MAUI-Shutdown-Crash. Die folgenden Angaben beschreiben die ursprüngliche isolierte Serverlieferung vor der aktuellen Übernahme.

Für die Bedienung siehe [Hilfe zu saisonalen Startseitenlisten](../../help/saisonale-startseitenlisten.md);
der technische Endpunkt und seine UTC-Ablaufsemantik stehen im
[API-Vertrag](../../help/api/seasonal-genres.md).

Die Serveränderungen entstehen in einem unabhängigen lokalen Klon, weil der
ursprüngliche Servercheckout außerhalb des freigegebenen Mobile-Workspace liegt.
Der Ursprungscheckout und seine Git-Metadaten bleiben unverändert. Es wurde nichts gepusht.

## Herkunft

- Repository: VideoWebPlayer (Server und Webansicht).
- Basis: `eae60a6003ac0619958331351d21c5d983cb175f`.
- Ursprungsbranch der Basis: `task/issue-277-1f22729c5f434f6994c5aee41f073cac-tv-sender-platzhalter-clips-gr`.
- Finaler Dokumentationscheckout: `.work/seasonal-server-docs` im Mobile-Workspace, eigener Branch `docs/saisonlisten-hilfe`.
- Lieferbranch: `task/38598ab90d844bb0bbc42a195643b6e3-saisonlisten-schritt-1-api-web`.
- Bearbeitungsübernahme: `6082e08`.
- Notwendige Begleitkorrektur Filmroute: `6b819d9` (separater Zwischencommit).
- Unabhängig geprüfter Produktstand: `1bb12a42ffeadab62702117fcdc9eb9955e7fd48`.
- Finaler API-/Webliefercommit: `a36d2db5272b122dff0b7f54e3b9d8ff29309438` (`feat:`). Produktcode unverändert zum geprüften Stand; unabhängige Abnahme übernommen.
- Reiner Dokumentationscommit: `38027cb` (`docs:`) auf `docs/saisonlisten-hilfe`; enthält Hilfe, API-Doku, README, Release Notes und Handover/Status. Keine Produktänderungen oder erneuten Tests.
- Der Integrationspatch und das Bundle wurden danach von der Serverbasis bis zum Dokumentationscommit erneuert. Beide liefern den Branch `docs/saisonlisten-hilfe` auf Basis `eae60a6003ac0619958331351d21c5d983cb175f`.

## Inhalt und Reihenfolge

Zuerst die Serveränderung übernehmen, prüfen und bereitstellen, danach die mobile App.
`GET /api/home/seasonal-genres` liefert für den angemeldeten Benutzer ausschließlich
aktive, nichtleere Genre-Sektionen samt polymorphen Medien und UTC-`expiresAt`.
Die Webstartseite bezieht diese Daten über den internen authentifizierten HTTP-Client.
Eine Filmkartenroute `/movie/{id}` wurde zusätzlich über die bestehende Detailansicht
implementiert, da die bisher bereits verwendete URL keinen Handler hatte. Der
geforderte saisonale Film-/Singleton-Navigationsfluss wäre sonst bei 404 geendet.
Diese Begleitkorrektur enthält keine Schemaänderung und steht in einem eigenen Commit.

Der Patch umfasst nur Änderungen relativ zur oben genannten Serverbasis; er enthält
keine fremden Änderungen dieses Basisbranches. Der Server-Quellbaum und die Browser
werden nicht im Mobile-Repository versioniert.

## Übernahme

Den beigefügten Patch in einem eigenen Server-Featurecheckout auf dieser Basis anwenden:

```powershell
git switch -c feature/saisonlisten eae60a6003ac0619958331351d21c5d983cb175f
git apply --check PATH/seasonal-server.patch
git apply PATH/seasonal-server.patch
```

Alternativ das beigefügte Bundle lokal importieren und den dokumentierten Lieferbranch
in einem eigenen Checkout prüfen. Nicht ungeprüft auf einen fremden aktiven Branch
anwenden. Bei neuerer Serverbasis Konflikte regulär auflösen und dieselben Tests laufen lassen.

## Prüfungen

Im isolierten Klon sind Debug-/Release-Build, acht SQLite-/Zeitfenster-/Vertragstests
und vier echte Web-Playwright-Flüsse erfolgreich ausgeführt. Die vollständige
Server-Testsuite ist grün: 2358 bestanden, 0 Fehler, 2 schon vorher vorhandene Skips (Unix-Dateirechte auf Windows; deaktivierter ContinueWatching-Singleton-Befund). Die unabhängige Abnahme ist erfüllt; Quellenfilter- und Ablaufmutationen wurden von unveränderten Tests erkannt und anschließend zurückgestellt.
Siehe Backend `docs/projects/saisonlisten/implementation-results.md` und den
unabhängigen Abnahmebericht `docs/projects/saisonlisten/acceptance-schritt-1.md` im Dokumentationscheckout sowie [lokale Berichtskopie](acceptance-schritt-1.md). Kein fehlender E2E-Nachweis gilt als bestanden. Die saisonale Serverabnahme bescheinigt nicht den vollständigen Mobile-E2E-Status; dort bleiben die in [continue.md](../../features/task/38598ab90d844bb0bbc42a195643b6e3-saisonabhaengge-listen/continue.md) protokollierten Punkte offen.

Für erneute Browserläufe kann `PLAYWRIGHT_BROWSERS_PATH` auf den absoluten Ordner
`.work/seasonal-server/.playwright-browsers` gesetzt werden. Für einen anderen Rechner
zuerst `pwsh VideoWebPlayer.Tests/bin/Debug/net10.0/playwright.ps1 install chromium` ausführen.

Bundle-Import in ein Repository mit vorhandenem Basiskommit:

    git fetch PATH/seasonal-server.bundle HEAD:refs/heads/review/saisonlisten

Patch rückwärts gegen den finalen Dokumentationscheckout sauber anwendbar; Bundle verifiziert und enthält den Dokumentationscommit `38027cb`, mit der oben genannten Serverbasis als Voraussetzung. Der finale Produktstand `a36d2db5272b122dff0b7f54e3b9d8ff29309438` ist gegenüber der unabhängig geprüften Produktlieferung unverändert.
