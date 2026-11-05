# Bestandsaufnahme: tvOS-Serververbindung — Einrichtung, Pairing und Anmeldung

Analysiert wurden der plattformneutrale MVU-Verbindungs-Workflow
(`VideoPlayer.Mvu`, `VideoPlayer.ApiClient`, `VideoPlayer.Storage`), der
tvOS-App-Rand (`VideoPlayer.TvOS`) sowie die MAUI-Oberflächen als
Interaktionsvorlage — bezogen auf die Serveranbindung der tvOS-App.

## Zusammenfassung

- Der **gesamte Verbindungs-Workflow ist plattformneutral bereits
  vollständig implementiert**: Servererkennung, Kandidatenprüfung,
  Pairing-Austausch, Ticket-/Kurzcode-Bootstrap, manuelle und automatische
  Anmeldung, Geräte-Token-Verwaltung, Offline-Erkennung — inklusive aller
  Fehlerzustände und Modellfelder. Die bestehende Logik-Testsuite deckt
  ihn ab (`AppUpdateTests_Connection`, `PairingContractTests` u. a.).
- Der **`TvosAppEffectHandler` führt alle 23 Start-/Verbindungs-Effekte
  bereits aus** (Schritt 2) — inklusive `DiscoverServersEffect`,
  `CheckCandidateServerEffect`, `ExchangePairingCodeEffect`,
  `CheckBootstrapServerEffect`, `ExchangeBootstrapTicketEffect`,
  `AttemptAutoLoginEffect`, `AttemptLoginEffect`. **Einzige Lücke:**
  `LogoutEffect` fällt noch in das protokollierte No-Op.
- **Fehlend ist ausschließlich die tvOS-Oberfläche**: `AppPage.ServerSetup`
  und `AppPage.Login` werden bislang von der Platzhalterseite
  `ConnectionStatePageViewController` dargestellt (nur Statustext +
  „Erneut prüfen" — keine Eingaben, keine Serverlisten). Es gibt keinen
  Einstieg „Server wechseln" im Menürahmen und kein „Abmelden".
- Die **MAUI-Seiten `ServerSetupPage`/`LoginPage`** liefern die
  abgenommene Interaktionsvorlage: zweistufiger Fluss (Adresswahl →
  Kurzcode/Zugangsdaten, Pairing-Abschnitt bei `IsPairingRequired`),
  dieselben Messages, dieselben Modellfelder.
- `Info.plist` enthält bereits `NSBonjourServices` (`_http._tcp`) und
  `NSLocalNetworkUsageDescription` — keine Ergänzung nötig.
- tvOS-Code ist unter Windows nicht kompilierbar; Absicherung erfolgt
  über Quell-Contract-Tests (`TvOsAppContractTests`) und den macOS-CI-Job
  `build-tvos`.

## Details

- [MVU-Verbindungs-Workflow (Messages, Effekte, Modell, Update)](inventory/mvu.md)
- [tvOS-App-Rand (Ist-Zustand)](inventory/tvos.md)
- [MAUI-Oberflächen als Interaktionsvorlage](inventory/maui-templates.md)
- [Bestehende Tests](inventory/tests.md)
