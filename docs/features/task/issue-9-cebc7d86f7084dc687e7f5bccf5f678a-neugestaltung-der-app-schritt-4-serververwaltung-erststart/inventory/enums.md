# Enums — Bestandsaufnahme

## `AppPage`

Datei: `VideoPlayer.Mvu/AppPage.cs`

| Wert | Bedeutung |
|------|-----------|
| `Loading` | Start-Ladeanzeige, kein Menüeintrag, keine Shell-Route |
| `Home` | Startseite (`home`) |
| `Sources` | Quellen (`sources`) |
| `Favorites` | Favoriten (`favorites`) |
| `Downloads` | Downloads (`downloads`) |
| `Profile` | Profil (`profile`) |
| `Settings` | Einstellungen (`settings`) |

Nicht vorhanden: eine Seite für die Server-Einrichtung/-verwaltung
(Erststart-Dialog, „Server wechseln").

## `ServerHealthStatus`

Datei: `VideoPlayer.Mvu/ServerHealthStatus.cs`

| Wert | Bedeutung |
|------|-----------|
| `Unknown` | noch nicht geprüft |
| `Checking` | Prüfung läuft |
| `Reachable` | Server erreichbar |
| `Unreachable` | Server nicht erreichbar |
