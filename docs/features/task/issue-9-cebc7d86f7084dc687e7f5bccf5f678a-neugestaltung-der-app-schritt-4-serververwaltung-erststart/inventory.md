# Bestandsaufnahme: Serververwaltung und Erststart-Einrichtung

Analysiert wurde der Stand der neuen App (`VideoPlayer.Maui`, `VideoPlayer.Mvu`,
`VideoPlayer.Storage`, `VideoPlayer.ApiClient`) sowie die fachliche Referenz im
Altbestand (`VideoWebPlayer.Maui`) bezogen auf die Anforderung
„Serververwaltung und Erststart-Einrichtung" (`requirement.md`). Zusätzlich
wurde die serverseitige Beantwortung des Discovery-Protokolls verifiziert.

## Zusammenfassung

- **Vorhanden (Schritt 3):** `BackupDatabase` mit Tabelle `known_servers`
  (`KnownServerEntry`: `Id`, `Address`, `DisplayName`, `LastUsedAtUtc`) und
  Tabelle `backup_state` (`BackupStateEntry` mit Schlüsselkonstante
  `LastConnectedServerKey`) — die backup-pflichtige Ablage für Serverliste und
  zuletzt verbundenen Server existiert bereits, **ohne Zugriffsschicht**
  (kein Dienst liest/schreibt diese Tabellen).
- **Vorhanden (Schritt 2):** `ServerAddressProvider` löst genau eine
  Serveradresse auf (Ablage-Schlüssel `ServerAddress` → `VIDEOPLAYER_SERVER_URL`
  → IIS-Default `http://localhost/videoplayer/`); `ServerHealthProbe` prüft die
  Erreichbarkeit über `VideoPlayerApiClient.CheckHealthAsync`; der
  Start-Workflow (`StartupRequestedMessage` → Effekte →
  `ServerHealthCheckCompletedMessage` → `NavigateToPageEffect(Home)`) kennt
  **keine** Verzweigung „kein Server hinterlegt" — ohne gespeicherte Adresse
  wird stillschweigend der IIS-Default geprüft.
- **Fehlt:** Mehr-Server-Verwaltung (Liste, Wechsel, „zuletzt verbunden"),
  Einrichtungsdialog beim Erststart, Server-Discovery (mDNS + UDP),
  „Server wechseln"-Eintrag in den Einstellungen.
- **Discovery-Referenz (Altbestand):** `SettingsService.DiscoverServersAsync`
  (mDNS `_http._tcp.local.` via Zeroconf 3.7.16) und
  `DiscoverServersUdpAsync` (UDP-Broadcast Port 5001,
  `VIDEOWEBPLAYER_DISCOVERY` → `VIDEOWEBPLAYER_SERVER:<adresse>`) — nur als
  Protokoll-Referenz, kein Code wird übernommen.
- **Discovery-Verifikation (durchgeführt, Ergebnis unten):** UDP-Broadcast
  wird serverseitig beantwortet (live verifiziert); mDNS wird serverseitig
  **nicht** beantwortet und die gemeldete Adresse ist bei Nicht-Standard-
  Bereitstellung unbrauchbar → zwei Issues im Server-Repository gemeldet
  (#223, #224). Details: `inventory/discovery-verification.md`.

Test-Ausgangszustand: 160 Tests, alle bestanden (`inventory/tests.md`,
Nachweis `inventory/test-results/baseline-dotnet-test.log`).

## Details

- [Datenmodell](inventory/models.md)
- [Logik / Services](inventory/services.md)
- [Enums](inventory/enums.md)
- [Interfaces](inventory/interfaces.md)
- [Discovery-Verifikation](inventory/discovery-verification.md)
- [Tests](inventory/tests.md)
