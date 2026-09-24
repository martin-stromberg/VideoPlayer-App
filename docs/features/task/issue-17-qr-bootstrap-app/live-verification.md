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

## Randnotiz

Auf Windows fehlt der QR-Scan-Button bewusst (kein Kamera-Pfad);
die mobile Scan-Seite `QrScannerPage` ist Gerätetest-Restnachweis.
