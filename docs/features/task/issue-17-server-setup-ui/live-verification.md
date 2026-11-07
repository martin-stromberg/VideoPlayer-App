# Live-Verifikation — Modernisierte `ServerSetupPage` (Issue 17, Schritt 4)

Datum: 2026-09-24, Windows 11, echte `VideoPlayer.Maui.exe`
(Debug, `bin/Debug/net10.0-windows10.0.19041.0/win-x64/`).

## Vorgehen

Einmaliges Screenshot-Harness (Konsolenprojekt unter `%TEMP%\shot-harness`),
das die E2E-Infrastruktur wiederverwendet: `AppInstance` startet die
Exe mit isoliertem `VIDEOPLAYER_PROFILE=screenshot`, `MockVideoServer`
(TcpListener auf Loopback) liefert Health/Pairing/Login/Listen,
`UiDriver` bedient die echte UI per UIA (Texteingabe per Value-Pattern,
Buttons per Invoke, Flyout per SelectionItem). Fensterfotos per
`Graphics.CopyFromScreen` auf das FlaUI-Fensterrechteck.

## Aufgenommene Zustände

| Datei | Zustand | Sichtbarer Nachweis |
|-------|---------|---------------------|
| `live-verification/serversetup-01-erststart.png` | Erststart ohne Server | Kopfbereich (Kicker/Titel/Hinweis), Discovery-Sektion, Verbinden-Karte mit Adresseingabe |
| `live-verification/serversetup-02-pairing.png` | Erreichbarer Server → Pairing erforderlich | Aktiver-Server-Karte mit Badge „Server: erreichbar", hervorgehobene Pairing-Karte („NÄCHSTER SCHRITT", „Gerät koppeln", Führungstext) |
| `live-verification/serversetup-03-bekannte-server.png` | Nach Pairing + Anmeldung → Einstellungen → „Server wechseln" | Aktiver-Server-Karte, bekannte Server als Kartenliste, Verbinden-Karte |

## Sichtprüfung (Ergebnis)

- **Dark-only bestätigt:** Pixelanalyse aller drei Aufnahmen — 97–99 %
  dunkle Fläche, durchschnittliche Farbe RGB ~ (23–31), keine hellen
  Bereiche; alle Akzente aus den `Vp*`-Tokens.
- **Kartenstruktur:** abgerundete Flächen mit abgesetzten Rändern
  sichtbar; Überschriften/Statuszeilen klar hierarchisch
  (fett/muted).
- **Status-Badge:** im Pairing-Zustand zeigt die Aktiver-Server-Karte
  den Punkt + Text „Server: erreichbar" (auch per Dev-API `/tree`
  verifiziert: `ServerHealthLabel.Text = "Server: erreichbar"`).
- **Geführte Pairing-Karte:** Sekundärfarben-Rahmen + Badge sichtbar,
  Führungstext verweist auf die Server-Verwaltung/„Geräte".
- **Kein abgeschnittenes Layout** in 1920×1023; Fußbereich bleibt
  scrollbar.

## Randnotiz

Der erste Durchlauf verwendete den Pairing-Code „SHOT42" — die App
wies den Buchstaben „O" korrekt als Format-Fehler ab (Pairing-Alphabet
ohne 0/O/1/I/L); Wiederholung mit „ABCD23" erfolgreich. Verhalten ist
der spezifizierte Validierungs-Pin, kein Defekt.
