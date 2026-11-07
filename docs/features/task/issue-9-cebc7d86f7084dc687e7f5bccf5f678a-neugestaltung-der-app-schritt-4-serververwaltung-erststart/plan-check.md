# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Erststart ohne hinterlegten Server führt zum Einrichtungsdialog | Start-Workflow lädt Serververwaltung (`LoadServersEffect`); `ServersLoadedMessage` ohne aktive Adresse → `CurrentPage = ServerSetup` + `NavigateToPageEffect`; `NavigateAsync` setzt `ServerSetupPage` als Window-Root | `AppUpdateTests_ServerSetup` (Verzweigung), `ServerSetupContractTests` (Seite, Route, Root-Swap) | Abgedeckt |
| Automatisch gefundene Server werden zur Auswahl angeboten | `ServerDiscoveryService` (mDNS + UDP, Protokoll neu implementiert), `ServerDiscoveryRequested`/`Completed`, `DiscoveredServers` im Model, Auswahlliste auf `ServerSetupPage` | `UdpServerDiscoveryTests` (Socket-Roundtrip), `DiscoveryProtocolTests`, `ServerDiscoveryServiceTests`, `AppUpdateTests_ServerSetup` | Abgedeckt |
| Nach erfolgreicher Einrichtung startet die App künftig direkt mit dem gespeicherten Server | `PersistActiveServerEffect` → `SetActiveServerAsync` (`known_servers` + `backup_state.LastConnectedServer`); nächster Start: `GetConfiguredServerAddressAsync` → `CheckServerHealthEffect` → `Home` | `KnownServersServiceTests` (Persistenz-Roundtrip), `AppUpdateTests_ServerSetup` (beide Startzweige) | Abgedeckt |
| „Server wechseln" listet bekannte Server, erlaubt Hinzufügen und Wechsel ohne Verlust der übrigen Einstellungen | Unauffälliger `SwitchServerLink` auf `SettingsPage`; `ServerSetupPage` zeigt `KnownServers` + Hinzufügen (manuell/Discovery); Wechsel schreibt nur `BackupDatabase`, nie `AppSettings` | `ServerSetupContractTests`, `AppUpdateTests_ServerSetup`, Bestand `AppSettingsServiceTests` | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

— (Status „Plan vollständig")

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Erststart → Einrichtungsdialog (AC 1) | Nachweiskette MVU-Update-Tests + Contract-Tests (kein UI-Automatisierungs-Framework im Repo — Begründung im Plan-Abschnitt „E2E-Tests") | Abgedeckt (Ersatzkette, wie in Schritt 3 akzeptiert) |
| Discovery → Auswahl (AC 2) | `UdpServerDiscoveryTests` (echter Socket-Roundtrip) + MVU-Fluss + Contract-Test der Auswahlliste | Abgedeckt (Ersatzkette) |
| Einrichtung → persistenter Direktstart (AC 3) | `KnownServersServiceTests` Roundtrip + `AppUpdateTests_ServerSetup` beider Startzweige | Abgedeckt (Ersatzkette) |
| „Server wechseln" ohne Einstellungsverlust (AC 4) | Contract-Tests + `AppUpdateTests_ServerSetup` + Bestands-Persistenztests | Abgedeckt (Ersatzkette) |
| Fehlerpfad ungültig/nicht erreichbar | `AppUpdateTests_ServerSetup` (kein Persist-Effekt, `ServerSetupError`) | Abgedeckt (Ersatzkette) |

## Fehlende oder unvollständige Planbestandteile

— (Status „Plan vollständig")

## Hinweise

- Die serverseitige Discovery-Verifikation ist bereits abgeschlossen
  (`inventory/discovery-verification.md`): UDP beantwortet ✅, mDNS nicht
  implementiert (Issue #223), gemeldete Adresse ggf. unbrauchbar (Issue
  #224). Der Plan enthält zu Recht keinen App-seitigen Protokoll-Workaround.
- Der Plan sieht bewusst vor, dass alle Discovery-Treffer vor der Übernahme
  per `CheckHealthAsync` geprüft werden — damit sind generische
  mDNS-Fehltreffer und unbrauchbare UDP-Meldungen abgedeckt.
- Betroffene Bestandstests (Effektliste des Starts, Provider-Assertions)
  sind im Plan vollständig benannt.
