# Discovery-Verifikation (serverseitig)

Verifiziert wurde, ob die Serveranwendung „VideoPlayer" das bestehende
Discovery-Protokoll beantwortet: mDNS-Dienstsuche (`_http._tcp.local.`) sowie
UDP-Broadcast `VIDEOWEBPLAYER_DISCOVERY` → `VIDEOWEBPLAYER_SERVER:<adresse>`
auf Port 5001.

**Zeitpunkt:** 2026-09-22, Europe/Berlin (UTC+02:00)
**Umgebung:** Windows-Client 192.168.10.230; lokaler IIS „VideoPlayer"
(`http://localhost/videoplayer/`); Server-Repo `martin-stromberg/VideoPlayer`
(HEAD, Stand 2026-09-22).

## Ergebnis im Überblick

| Prüfung | Ergebnis |
|---------|----------|
| UDP-Broadcast Port 5001 (Code-Prüfung Server-Repo) | implementiert: `VideoWebPlayer/Services/UdpDiscoveryListener.cs`, gestartet in `Program.cs` (außer Environment `Testing`) |
| UDP-Broadcast Port 5001 (Live-Test im LAN) | **beantwortet** ✅ — Antwort von `192.168.10.217:5001`: `VIDEOWEBPLAYER_SERVER:http://localhost:5000` |
| mDNS `_http._tcp.local.` (Code-Prüfung Server-Repo) | **nicht implementiert** ❌ — kein Zeroconf-/mDNS-Paket, kein Advertisement im Server |
| Gemeldete Adresse verwendbar | **eingeschränkt** ⚠ — Antwort nutzt `Host:Address`/`Host:Port` (Default `localhost:5000`); für entfernte Clients unbrauchbar, bei IIS-Bereitstellung inkorrekt |
| Lokaler IIS `http://localhost/videoplayer/` | antwortet mit HTTP 500 auf `/api/health` und `/` — lokale Installation derzeit fehlerhaft, unabhängig vom Discovery-Protokoll |

## Details

### UDP-Broadcast (funktioniert)

- Server-Implementierung: `UdpDiscoveryListener` lauscht auf UDP-Port 5001,
  beantwortet exakt `VIDEOWEBPLAYER_DISCOVERY` mit
  `VIDEOWEBPLAYER_SERVER:{serverAddress}` an den Absender
  (`result.RemoteEndPoint`). `serverAddress` =
  `http://{Configuration["Host:Address"] ?? "localhost"}:{Configuration["Host:Port"] ?? "5000"}`.
- Live-Test (PowerShell, UDP-Broadcast `255.255.255.255:5001`, 3 s
  Antwortfenster): eine Antwort — `192.168.10.217:5001` →
  `VIDEOWEBPLAYER_SERVER:http://localhost:5000`. Protokollschema wird exakt
  eingehalten.

### Gemeldete Adresse unbrauchbar bei Nicht-Default-Bereitstellung

Die Antwort enthält die konfigurierte `Host:Address`/`Host:Port` (Default
`localhost:5000`), nicht die tatsächlich erreichbare externe Basis-URL. Die
beobachtete Antwort `http://localhost:5000` ist für Clients auf anderen
Geräten unbrauchbar (localhost = Client selbst); auf dem antwortenden Host
war Port 5000 zusätzlich nicht erreichbar (TCP 80 und 5000 auf
`192.168.10.217` nicht offen — vermutlich Firewall bzw. abweichende
Bereitstellung).

→ **Issue im Server-Repository gemeldet:**
<https://github.com/martin-stromberg/VideoPlayer/issues/224>
(„Discovery: UDP-Antwort meldet konfigurierte Adresse statt erreichbarer
Basis-URL"). Die App baut keinen Workaround; sie prüft gefundene Adressen
ohnehin vor Übernahme per `CheckHealthAsync` — unbrauchbare Meldungen werden
dadurch verworfen bzw. als nicht verbindbar angezeigt. Zusätzlich wird eine
`localhost`-/Loopback-Meldung eines **entfernten** Antwortenden
protokollkonform als „der antwortende Host auf dem gemeldeten Port" gedeutet
(Responder-Adresse + gemeldeter Port) — das ist keine Umgehung des
Protokolls, sondern die sachgemäße Interpretation der Antwort.

### mDNS (nicht beantwortet)

Der Server veröffentlicht keinen mDNS-Dienst: kein Zeroconf-/mDNS-Paket im
`VideoWebPlayer.csproj`, kein Advertisement in `Program.cs`. Die
mDNS-Dienstsuche (`_http._tcp.local.`) kann den VideoPlayer-Server daher
grundsätzlich nicht finden; sie findet allenfalls beliebige andere
HTTP-Dienste im Netz (False-Positives, die per `CheckHealthAsync`
herausgefiltert werden).

→ **Issue im Server-Repository gemeldet:**
<https://github.com/martin-stromberg/VideoPlayer/issues/223>
(„Discovery: Server ist über mDNS nicht auffindbar"). Die App implementiert
die mDNS-Abfrage protokollgemäß; bis der Server einen mDNS-Dienst
veröffentlicht, liefert dieser Weg in der Praxis keine Treffer.

### Lokaler IIS „VideoPlayer"

`http://localhost/videoplayer/` und `http://localhost/videoplayer/api/health`
antworten mit HTTP 500 (Microsoft-IIS/10.0, `w3wp` läuft). Die lokale
Installation ist derzeit fehlerhaft — unabhängig vom Discovery-Protokoll;
die Live-Verifikation der UDP-Erkennung erfolgte über den LAN-Host
`192.168.10.217`. Beobachtung dokumentiert; kein Eingriff in diesem Schritt.

## Fazit für die Umsetzung

- UDP-Broadcast-Erkennung wird implementiert und ist serverseitig belegt.
- mDNS-Dienstsuche wird implementiert (Protokollbestandteil), liefert aber
  erst Treffer, wenn Issue #223 serverseitig gelöst ist.
- Alle Discovery-Ergebnisse werden vor der Übernahme per `CheckHealthAsync`
  verifiziert (Anforderung: „eine erfolgreich geprüfte Verbindung wird
  gespeichert") — falsche/unbrauchbare Meldungen schaden nicht.
