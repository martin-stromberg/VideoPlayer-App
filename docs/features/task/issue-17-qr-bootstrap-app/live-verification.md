# Live-Verifikation — QR-/Ticket-Onboarding (Issue 17, Schritt 5b)

Datum: 2026-09-24, Windows 11, echte `VideoPlayer.Maui.exe`
(Debug, `bin/Debug/net10.0-windows10.0.19041.0/win-x64/`).

## Vorgehen

Einmaliges Screenshot-Harness (Konsolenprojekt unter
`%TEMP%\shot-harness-bootstrap`), das die E2E-Infrastruktur
wiederverwendet: `AppInstance` startet die Exe mit isoliertem Profil
(`VIDEOPLAYER_PROFILE=screenshot-bootstrap`), `MockVideoServer`
(TcpListener auf Loopback) liefert Health/Bootstrap/Listen,
`UiDriver` bedient die echte UI per UIA (Value-Pattern, Invoke).
Fensterfotos per `Graphics.CopyFromScreen` auf das
Fensterrechteck (`GetWindowRect`).

## Aufgenommene Zustände

| Datei | Zustand | Sichtbarer Nachweis |
|-------|---------|---------------------|
| `live-verification/bootstrap-01-erststart.png` | Erststart ohne Server | Kopplungs-Karte „Mit Kopplungscode verbinden" mit Führungstext, Ticket-Eingabe und Button „Kopplungscode einlösen"; Scan-Button unter Windows bewusst unsichtbar |
| `live-verification/bootstrap-02-pruefung-laeuft.png` | Serverprüfung läuft | „Kopplung läuft …" + Indikator; Eingabe und Buttons gesperrt (unerreichbare Ticket-Adresse → langes Prüffenster) |
| `live-verification/bootstrap-03-fehler.png` | Erreichbarkeitsfehler | Roter Inline-Fehler in der Kopplungs-Karte; Server nicht übernommen (Dev-API: `activeServerAddress` weiter `null`) |
| `live-verification/bootstrap-04-home.png` | Erfolgreicher Bootstrap | Direkter Einstieg auf die Startseite — ohne Adress-, Pairing-Code- oder Anmeldeeingabe; Mock-Server: `BootstrapCount=1`, `LoginCount=0`, `PairingExchangeCount=0` |

## Sichtprüfung (Ergebnis)

- **Kartenstruktur:** die Kopplungs-Karte folgt dem Vp*-Token-Design
  (abgerundete Fläche, abgesetzter Rand) und sitzt zwischen
  Verbinden- und Pairing-Karte — konsistent zum übrigen Layout,
  Dark-only.
- **Laufzustand:** Textphase + Indikator + gesperrte Eingaben sind
  sichtbar; kein Flackern/Overlapping.
- **Fehler:** Inline-Fehler in `VpDanger` an der Eingabe; die Seite
  bleibt vollständig bedienbar (Retry + alle Fallback-Wege).
- **Erfolg:** Übergang ServerSetup → Home ohne Zwischenmaske —
  der Kernnutzen des Features ist visuell belegt.

| `live-verification/bootstrap-05-echter-server.png` | Live-Lauf echter Server | Startseite `http://localhost:83/` mit echtem Hero-Bild und Kontoinhalt nach Ticket-Einlösung (siehe Nachtrag) |

## Nachtrag 2026-10-08 — Lauf gegen den echten Server

Nach dem Merge des Server-PRs (martin-stromberg/VideoPlayer#232) und der
lokalen Installation der Server-Version mit `api/pairing/bootstrap` wurde
der komplette Ablauf gegen die echte IIS-Instanz `http://localhost:83`
(SQLite `D:\Dashboard\VideoPlayer\Data\WebVideoPlayer.db`) verifiziert:

- Testticket direkt in `PairingCodes` eingesetzt (`Kind=1`, SHA-256-Hashes
  wie `PairingBootstrapService`), Benutzer `devin@example.com`.
- App mit frischem Profil (`VIDEOPLAYER_PROFILE=livetest-bootstrap-*`)
  gestartet, Dev-API: `BootstrapTicketEntry` ←
  `http://localhost:83/pairing?t=<ticket>`, `BootstrapButton` per Invoke.
- Ergebnis `/model`: `currentPage=Home`, `connectionState=Connected`,
  `activeServerAddress=http://localhost:83/` — **ohne Adress- und
  Passworteingabe**.
- Serverseitig belegt: Ticket atomar verbraucht (`ConsumedAtUtc` gesetzt),
  `PairedDevices` Id 4 (`DESKTOP-CM8OBSG`, `CreatedByUserId`=devin-User),
  `RefreshTokens` Id 1 (DeviceId=4, 30 Tage, nicht widerrufen).
- Screenshot `live-verification/bootstrap-05-echter-server.png`:
  Startseite mit echtem Hero-Bild und „Weiterschauen"-Inhalt des Kontos.

Damit ist der Restnachweis „Live-Lauf gegen echten Server" erbracht;
offen bleibt nur der Kamera-Scan auf echtem iOS-/Android-Gerät.
