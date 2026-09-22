# Neues App-Grundgerüst (Entwicklungsschritt 2)

Die neue VideoPlayer-App wird schrittweise neu aufgebaut. Dieser Schritt liefert
das Grundgerüst: Startvorgang, Hauptnavigation und Platzhalterseiten. Die
bislang vorhandene App (`VideoWebPlayer.Maui`) bleibt unverändert bestehen und
wird erst später abgelöst.

## Start der App

Beim Start zeigt die App kurz eine Ladeanzeige. Dabei wird geprüft, ob der
VideoPlayer-Server erreichbar ist. Anschließend öffnet sich die Startseite; dort
wird der Verbindungsstatus („Server erreichbar" bzw. „Server nicht erreichbar")
angezeigt. Ist der Server nicht erreichbar, kann die App trotzdem geöffnet und
die Navigation benutzt werden — die eigentlichen Inhalte folgen in späteren
Entwicklungsschritten.

## Hauptnavigation

Die Hauptnavigation ist ein Seitenmenü (Hamburger-Menü) mit sechs Einträgen:

- **Startseite** — Einstieg und Serverstatus.
- **Quellen** — Übersicht der Videoquellen (Platzhalter).
- **Favoriten** — gemerkte Inhalte (Platzhalter).
- **Downloads** — lokal gespeicherte Inhalte (Platzhalter).
- **Profil** — Benutzerprofil (Platzhalter).
- **Einstellungen** — App-Einstellungen (Platzhalter).

Jeder Eintrag öffnet eine eigene Seite. Die Seiten enthalten in diesem Schritt
nur ihren Titel und einen kurzen Hinweis auf den späteren Zweck.

## Serveradresse

Die App nutzt denselben konfigurierten Server wie die bisherige App: zuerst die
unter „ServerAddress" gespeicherte Einstellung, dann die Umgebungsvariable
`VIDEOPLAYER_SERVER_URL`, sonst `http://localhost/videoplayer/`. Eine eigene
Einstellungsseite für die Serveradresse folgt in einem späteren Schritt.

## Unterstützte Plattformen

- Windows (Windows 10 ab Version 10.0.19041 bzw. Windows 11)
- iOS (Build über CI auf macOS)
- MacCatalyst
- Android (ab Android 5.0, API 21) — als Erweiterbarkeit vorbereitet

## Für Entwickler

Die neue App besteht aus zwei Projekten:

- `VideoPlayer.Mvu` — plattformneutrale MVU-Schicht (Modell, Messages,
  Update-Funktion, Effekte, `MvuHost`). Es wird ein eigener, schlanker
  MVU-Kreislauf verwendet; kein externes MVU-Framework.
- `VideoPlayer.Maui` — MAUI-App (Shell-Navigation, Seiten, Effekt-Ausführung,
  Anbindung an `VideoPlayer.ApiClient` für die Erreichbarkeitsprüfung).

Build (Windows):

```powershell
dotnet build .\VideoPlayer.Maui\VideoPlayer.Maui.csproj -f net10.0-windows10.0.19041.0
```

Build (Android):

```bash
dotnet build VideoPlayer.Maui/VideoPlayer.Maui.csproj -f net10.0-android
```
