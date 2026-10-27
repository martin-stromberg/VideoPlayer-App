# Plan-Review: saisonabhängige Genre-Listen

## Status: Offene Aufgaben vorhanden

## Geprüfte Umsetzung

- [x] Mobile API-Client bindet `GET api/home/seasonal-genres` an die saisonalen DTOs an; Saisonfenster werden nicht im Client berechnet.
- [x] Saisonsektionen liegen unter `seasonal_genres`; erfolgreiche leere Antworten ersetzen den Cache, Abruffehler bewahren ihn. Mapping filtert leere und abgelaufene Sektionen sowie nicht unterstützte Medientypen.
- [x] Home-MVU/XAML zeigt geordnete dynamische Genre-Karussells mit vorhandener Mediennavigation. `HomePage` plant und beendet einen Ablauf-Timer, prüft Zeitkorrekturen spätestens minütlich und lädt bei Rückkehr erneut.
- [x] Die sechs geplanten mobilen E2E-Flüsse sind als echte Windows-App-Tests vorhanden: Filmziel, geordnete Genres und Sammlung, erfolgreiches `[]`, HTTP-Fehler mit Cache-Neustart, keine leeren Karussells sowie Ablauf auf offener Seite und Offline-Neustart. `implementation-mobile.md` dokumentiert einen erfolgreichen Lauf (6/6).
- [x] Im Servercheckout auf Basis `eae60a6003ac0619958331351d21c5d983cb175f` sind API, Webanbindung, Zeitfenster-/Vertragstests und vier Playwright-E2E-Flüsse umgesetzt. `implementation-results.md` und `server-test-results.md` dokumentieren Debug-/Release-Builds, 8 gezielte API-/Service-Tests, 4 echte Browser-E2E und die Servertestsuite (2358 bestanden, 2 vorhandene Skips).
- [x] Mobile Integrationsunterlagen enthalten API-Vertrag, Serverbasis/Branch und getrennte Backendlieferung. `seasonal-server.patch` lässt sich rückwärts gegen den aktuellen Serverstand anwenden; das Bundle ist gültig und enthält HEAD `1bb12a42ffeadab62702117fcdc9eb9955e7fd48`. Der Patchstand umfasst auch den zusätzlichen Filmrouten-Commit.

## Offene Aufgaben

- [ ] Die im Plan und Server-Handover geforderte unabhängige Backend-Abnahme fehlt. `docs/integration/seasonal-genres/server-test-results.md` nennt sie ausdrücklich als offen; im Lieferbranch steht noch kein unabhängiger Prüfbericht. Dazu gehören eigener Prüfercheckout, Gegenbeispiele/Mutation für Quellfilter und Ablaufprüfung sowie die Abnahmeprüfungen nach Server-`AGENTS.md`.
- [ ] Nach dieser Abnahme den Backendlieferstand abschließend festschreiben und bei etwaigen Reviewkorrekturen `seasonal-server.patch` und `.bundle` neu erzeugen. Der aktuelle Backend-HEAD ist ein Übergabe-/Step-Commit, kein finaler Feature-Commit.

Die vorhandenen Implementierergebnisse und E2E-Berichte wurden geprüft; dieser Plan-Review hat keine vollständigen Tests erneut ausgeführt. Der unabhängige Testlauf ist separat vorgesehen.
