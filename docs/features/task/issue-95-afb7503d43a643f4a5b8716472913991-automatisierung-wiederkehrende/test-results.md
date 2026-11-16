# Test-Ergebnisse

Datum: 2026-11-16 — Branch `task/issue-95-afb7503d43a643f4a5b8716472913991-automatisierung-wiederkehrende` (Basis `staging`, Implementierung uncommitted im Arbeitsbaum).

## Ergebnis

**Status:** Fehler vorhanden

Hinweis zum Status: Alle ausgeführten automatisierten Tests sind ohne
Fehlschlag durchgelaufen (1484 Unit-Tests + 74 E2E-Tests, 0 fehlgeschlagen,
0 übersprungen). Der Status `Fehler vorhanden` ergibt sich ausschließlich
aus der unvollständigen realen End-to-End-Abnahme: 5 von 6
Akzeptanzszenarien konnten nicht real durchgespielt werden
(`Nicht durchführbar`, siehe unten). `Nicht durchführbar` gilt nicht als
Erfolg — die betroffenen Szenarien bleiben offen, bis der Anwender sie
manuell abnimmt oder ausdrücklich dokumentiert verzichtet.

## Fehlgeschlagene Tests

Keine fehlgeschlagenen automatisierten Tests. Nach dem Kommando-Verfahren
werden hier die offenen Szenarien der realen Abnahme geführt (wie
fehlgeschlagene Tests behandelt, bis eine Anwender-Entscheidung vorliegt):

### Reale End-to-End-Abnahme (offene Szenarien)

- **Szenario 1: Happy Path monatlich** — Nicht durchführbar: Workflow liegt uncommitted im Arbeitsbaum, nicht auf dem Default-Branch `main`; `workflow_dispatch` ist nicht auslösbar (`gh workflow list` zeigt den Workflow nicht). Ein echter Lauf würde zudem ein reales Issue mit `@softwarewerkstatt`-Erwähnung erzeugen und ist untersagt.
- **Szenario 2: Duplikatvermeidung** — Nicht durchführbar: erfordert einen echten Workflow-Lauf auf GitHub (siehe Szenario 1).
- **Szenario 3: Wöchentliche Prüfung** — Nicht durchführbar: wie Szenario 1.
- **Szenario 4: Jährliches Review** — Nicht durchführbar: wie Szenario 1.
- **Szenario 5: Fehlgeschlagener Lauf** — Nicht durchführbar: erfordert einen realen, fehlschlagenden Lauf auf GitHub (siehe Szenario 1).

## E2E-Abdeckung

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Zeitgesteuerte Issue-Erstellung (monatlich/wöchentlich/jährlich) — geplanter Nachweis via `workflow_dispatch`-Probelauf + Tracker-Sichtprüfung | kein automatisierter Test möglich (Systemablauf auf GitHub, nicht app-seitig beobachtbar) | Nicht ausgeführt — Workflows nicht auf Default-Branch; realer Lauf würde echte Issues erzeugen (untersagt) |
| Duplikatvermeidung — geplanter Nachweis via Zweitlauf | wie oben | Nicht ausgeführt — wie oben |
| Fehlerfall (fehlende Body-Datei, `gh`-Fehler) — geplanter Nachweis als roter Workflow-Lauf | wie oben | Nicht ausgeführt — wie oben; statischer Gegenbeweis: kein `continue-on-error`, kein `|| true` in den `run:`-Blöcken — `gh`-Fehler propagieren als Exit ≠ 0 |
| Bestehende E2E-Suite (Regressionssicherung, kein App-Code geändert) | `VideoPlayer.E2E.Tests` (FlaUI, 74 Tests) | Bestanden |

## Reale End-to-End-Abnahme

Erforderlich: Die Anforderung berührt externe Dienste (GitHub
Actions/Issues). Eine real gestartete Durchführung der Akzeptanzszenarien
ist vor dem Merge auf den Default-Branch `main` nicht möglich
(scheduled-/dispatch-Trigger greifen nur dort; die Workflows sind in
`gh workflow list` nicht registriert). Zulässige reale Teilnachweise
wurden ausgeführt und sind unten aufgeführt.

| Szenario | Durchgeführt über | Ergebnis |
|----------|-------------------|----------|
| 1. Happy Path monatlich (Issue „Architektur- und Sicherheitsreview YYYY-MM") | Workflow-Lauf auf GitHub | Nicht durchführbar — Workflow nicht auf `main`; `gh workflow run` nicht möglich; echter Lauf würde reales Issue mit `@softwarewerkstatt`-Erwähnung erzeugen (untersagt) |
| 2. Duplikatvermeidung (Zweitlauf ohne neues Issue) | Zweitlauf desselben Workflows | Nicht durchführbar — erfordert echten Lauf nach Szenario 1 |
| 3. Wöchentliche Prüfung (Issue „NuGet-Prüfung ISOJahr-WKW") | Workflow-Lauf auf GitHub | Nicht durchführbar — wie Szenario 1 |
| 4. Jährliches Review (Issue „Compliance- und Copyright-Review YYYY") | Workflow-Lauf auf GitHub | Nicht durchführbar — wie Szenario 1 |
| 5. Fehlgeschlagener Lauf (roter Lauf, kein Issue) | Workflow-Lauf mit `gh`-Fehler | Nicht durchführbar — erfordert realen Lauf; statisch abgesichert (keine Fehlerunterdrückung in den `run:`-Blöcken) |
| 6. Vor Default-Branch-Erreichung (kein Lauf, kein Issue) | `gh workflow list`, `gh run list` gegen das echte Repo | Bestanden — die drei `scheduled-*`-Workflows sind nicht registriert und haben keine Läufe; kein Issue wurde erzeugt |

### Ausgeführte reale Teilnachweise (nicht-destruktiv)

| Verifikation | Ergebnis |
|--------------|----------|
| `yaml.safe_load` auf `action.yml` + 3 Workflow-Dateien | Bestanden (4/4 parsebar) |
| `bash -n` auf alle 4 `run:`-Blöcke der Composite Action | Bestanden (4/4 syntaktisch gültig) |
| Label-Anlage exakt wie im `run:`-Block: `gh label create <label> --color ededed --description <desc> --force` | Bestanden — `review-architektur-sicherheit`, `review-nuget`, `review-compliance` real angelegt (geplante Infrastruktur, `gh label list --search review` zeigt alle drei) |
| Duplikatprüfung exakt wie im `run:`-Block: `gh issue list --state open --label <label> --limit 1 --json number --jq '.[0].number // empty'` | Bestanden — je Label leer (→ Erstellpfad würde greifen); Kommando real gegen das Repo verifiziert |
| Titelermittlung exakt wie im `run:`-Block (`date` mit `title-date-format`) | Bestanden — `+%Y-%m` → `2026-11`, `+%G-W%V` → `2026-W47`, `+%Y` → `2026` |
| Erste Zeile der drei Issue-Body-Dateien | Bestanden — alle beginnen mit `@softwarewerkstatt` |

Die real angelegten Labels sind beabsichtigte Infrastruktur (der erste
Workflow-Lauf würde sie ohnehin mit `--force` anlegen). Es wurden keine
Issues erzeugt.

## Zusammenfassung

- Gesamt: 1558
- Bestanden: 1558
- Fehlgeschlagen: 0
- Übersprungen: 0

(Aufschlüsselung: `VideoPlayer.Maui.Tests` 1484/1484 bestanden in ~55 s
nach erfolgreichem `dotnet build … -c Release` mit 0 Fehlern;
`VideoPlayer.E2E.Tests` 74/74 bestanden in ~10,8 min in der sichtbaren
Windows-Konsolensitzung — identisch zur Inventory-Basis. Build-Warnung
NU1903 zu `Microsoft.AspNetCore.SignalR.Common` 10.0.10 unverändert wie im
Ausgangszustand dokumentiert, kein Fehler.)

## Testabdeckung

**Abdeckung:** 89,45 % (Zeilenabdeckung, coverlet/XPlat Code Coverage
über `VideoPlayer.Maui.Tests`; `coverage.cobertura.xml`)

| Datei | Abdeckung |
|-------|-----------|
| `VideoPlayer.ApiClient/Discovery/MdnsServerDiscovery.cs` | 0,0 % |
| `VideoPlayer.Mvu/ServerAddressProvider.cs` | 0,0 % |
| `VideoPlayer.Mvu/ServerHealthProbe.cs` | 0,0 % |
| `VideoPlayer.Mvu/SignalRServerEventsConnection.cs` | 0,0 % |
| `VideoPlayer.ApiClient/Models/MediaSource.cs` | 61,5 % |
| `VideoPlayer.Mvu/DownloadListItem.cs` | 62,3 % |
| `VideoPlayer.ApiClient/ClientGate.cs` | 63,6 % |
| `VideoPlayer.Mvu/PlaylistDetailsState.cs` | 65,0 % |
| `VideoPlayer.Mvu/ProfilePageState.cs` | 70,0 % |
| `VideoPlayer.Mvu/DownloadItemViewModel.cs` | 72,0 % |
| `VideoPlayer.Mvu/MediaTypeNames.cs` | 72,2 % |
| `VideoPlayer.ApiClient/Discovery/DiscoveredServer.cs` | 75,0 % |
| `VideoPlayer.Mvu/SourceItem.cs` | 75,0 % |
| `VideoPlayer.ApiClient/Discovery/ServerDiscoveryService.cs` | 77,3 % |
| `VideoPlayer.ApiClient/Models/MediaEntry.cs` | 78,4 % |
| `VideoPlayer.Mvu/ServerEventsService.cs` | 79,8 % |

Hinweis: Die Abdeckung bezieht sich auf den unveränderten App-Code (das
Feature enthält kein C#). Alle aufgeführten Lücken sind vorhandener
Bestand, nicht durch diese Änderung verursacht.

## Fehlende Tests

Quelle: `Coverage-Daten` (0-%-Abdeckung im Unit-Testlauf). Die neue
`.github`-Implementierung ist nicht unit-testbar und besitzt bewusst keine
Testartefakte (laut Plan: keine automatisierten Tests für Workflow-YAML).

- `VideoPlayer.ApiClient/Discovery/MdnsServerDiscovery.cs` — 0 % Abdeckung (Bestand)
- `VideoPlayer.Mvu/ServerAddressProvider.cs` — 0 % Abdeckung (Bestand)
- `VideoPlayer.Mvu/ServerHealthProbe.cs` — 0 % Abdeckung (Bestand)
- `VideoPlayer.Mvu/SignalRServerEventsConnection.cs` — 0 % Abdeckung (Bestand)
