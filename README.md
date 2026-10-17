# VideoPlayer-App („Heimkino")

[![PR Checks](https://img.shields.io/github/actions/workflow/status/martin-stromberg/VideoPlayer-App/pr-checks.yml?label=PR%20Checks)](https://github.com/martin-stromberg/VideoPlayer-App/actions/workflows/pr-checks.yml)
[![Release](https://img.shields.io/github/v/release/martin-stromberg/VideoPlayer-App)](https://github.com/martin-stromberg/VideoPlayer-App/releases)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)

Die mobile und Desktop-App für den selbst gehosteten [VideoPlayer-Server](https://github.com/martin-stromberg/VideoPlayer) — eine .NET-MAUI-Anwendung zum Streamen und Offline-Speichern persönlicher Medienbibliotheken.

Die App koppelt sich beim Erststart über einen vom Server erzeugten Einmal-QR-Code (oder Kurzcode) mit dem Server und speichert ein gerätegebundenes Token im Secure Storage — kein statischer API-Schlüssel in der verteilten App.

## Funktionen

- **QR-/Kurzcode-Onboarding** — Kopplung über den QR-Code der Server-Verwaltung oder 8-stelligen Kurzcode; alternativ klassisches Pairing per Einmal-Code. Erzeugt Geräte-, Anmelde- und Refresh-Token in einem Schritt.
- **Server-Entdeckung** — automatische Erkennung der Server im lokalen Netz (UDP-Broadcast, mDNS).
- **Wiedergabe** — Server-Stream oder lokale Datei mit Fortsetzungsposition, „angesehen"-Markierung, automatischem Fortsetzen und Picture-in-Picture (Windows: kompaktes Overlay-Fenster; iOS: nativer PiP der Transportsteuerung); das Ende-Ereignis wird gegen den Wiedergabestand plausibilisiert, sodass verfrühte Signale weder „angesehen" noch die Fortsetzung auslösen; bei Playlist-Wiedergabe steuern „Zurück"/„Weiter"-Schaltflächen die serverseitige Reihenfolge.
- **Offline-Betrieb** — Downloads mit GB-Kontingent und WLAN-Bedingung; ein einstellbarer automatischer Vorlauf hält je Weiterschauen-Eintrag die aktuelle und die folgenden Episoden bereit; die Startseite, Serienstruktur und Detail-Snapshots bleiben ohne Serververbindung nutzbar, fehlende Kopfbereichs-Bilder der Detailseite werden nach der Wiederverbindung automatisch nachgeladen.
- **Startseite** — „Weiterschauen", „Favoriten" und „Neu im Programm" mit Hero-Kopfbereich im Erscheinungsbild der Web-Oberfläche; abspielbare Weiterschauen-Einträge (Filme/Episoden) öffnen die Titel-Detailansicht und starten darüber die Wiedergabe an der Fortsetzungsposition (Player-Schluss kehrt zur Detailansicht zurück), ein 2-s-Long-Press öffnet das Kontextmenü „Ausblenden"/„Überspringen".
- **Playlists** — Menüseite „Playlists" mit eigenen und öffentlichen Playlists des Servers (Cover, Sortiermodus, Genres), Detailansicht mit Einträgen in Server-Reihenfolge und seitenweisem Nachladen sowie direkter Wiedergabe aus der Playlist-Reihenfolge (Vor-/Zurück-Navigation, Auto-Fortsetzung und Fehler-Rückfall über die serverseitigen Play-Endpunkte); eigene Playlists lassen sich auf dem Gerät verwalten — Anlage/Bearbeitung/Löschung über Formular und Verwaltungsmenü, Einträge hinzufügen (von der Medien-Detailseite) und entfernen (inkl. Weiterschauen-Bestätigung bei HTTP 409), Reihenfolge und Sortiermodus ändern, Genres setzen/zurücksetzen und für Administrator:innen öffentlich schalten; reines Online-Feature mit verständlicher Fehleranzeige. Fortschrittsmeldungen aus der Playlist-Wiedergabe tragen den Playlist-Bezug mit — der Server schreibt den playlist-gebundenen Weiterschauen-Eintrag fort statt ein kontextloses Duplikat anzulegen; der Bezug bleibt auch in der Offline-Fortschrittswarteschlange erhalten. Der Download-Vorlauf playlist-gebundener Weiterschauen-Einträge folgt der Playlist-Reihenfolge ab einschließlich des aktuellen Eintrags.
- **Quellen-Browsing** — freigegebene Server-Quellen als eigene Menüeinträge; die Übersicht einer Quelle bietet eine horizontale Genre-Leiste (nur sichtbar, wenn die Quelle Genres liefert), Textsuche per Return-Taste (unbestätigte Eingaben bleiben bei Hintergrund-Aktualisierungen erhalten, der aktive Filter bleibt sichtbar) und ein Poster-Raster mit seitenweisem Nachladen, das die Scrollposition beim Anhängen beibehält; Detailseiten für Filme, Kollektionen und Serien mit Staffeln/Episoden.
- **Verbindungs-Workflow** — Anmelde- und Servermaske mit Offline-Modus, automatischer Wiederverbindung und SignalR-Ereignisverbindung.

## Plattformen

| Plattform | Status |
|-----------|--------|
| Windows | Unterstützt (primäres Entwicklungsziel, E2E-getestet) |
| iOS | Unterstützt (Geräte-Deployment via Pair-to-Mac) |
| MacCatalyst | Baubar, nicht aktiv gepflegt |
| Android | Build möglich, **nicht offiziell unterstützt** — kein Test-/CI-Gate |

## Voraussetzungen

- [.NET 10 SDK](https://dotnet.microsoft.com/download) und die MAUI-Workload: `dotnet workload install maui`
- Windows-Ziel: Windows App SDK
- iOS/MacCatalyst: macOS mit Xcode (lokales Deployment über `scripts/iOS-Deployment.ps1` und Pair-to-Mac)
- Ein laufender [VideoPlayer-Server](https://github.com/martin-stromberg/VideoPlayer) mit der öffentlichen API

## Bauen und Testen

```bash
dotnet restore VideoPlayer.App.sln
dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj
```

Windows-App:

```powershell
dotnet build .\VideoPlayer.Maui\VideoPlayer.Maui.csproj -f net10.0-windows10.0.19041.0
```

## Projektstruktur

| Projekt | Zweck |
|---------|-------|
| `VideoPlayer.Maui` | Die App (MVU-Architektur): Seitenmenü, Serververwaltung, Startseite, Quellen-Browsing, Detailseiten, Wiedergabe/PiP, Downloads, Offline-Modus |
| `VideoPlayer.Mvu` | Plattformneutrale MVU-Schicht: Modell, Update, Effekte; Authentifizierung, Synchronisation, Ereignisverbindung, Pairing-Flows |
| `VideoPlayer.ApiClient` | Zugriff auf die öffentliche Server-API: Pairing/Bootstrap (ECDH-nistP256 + AES-256-GCM), Refresh/Logout, Server-Discovery |
| `VideoPlayer.Storage` | Lokale Datenhaltung: SQLite-Cache, Einstellungen, Geheimnis-Ablage (`SecureStorage`), Download-Manifest, schaltbares Programmprotokoll (Datei-Logging mit Ablauf-Einträgen, Token-Maskierung und Rotation) |
| `VideoWebPlayer.Maui` | Altbestand der bisherigen App — wird schrittweise durch `VideoPlayer.Maui` abgelöst |
| `msTools.DeveloperApi` | Wiederverwendbarer Kern der lokalen Developer-API (Loopback-HTTP-Server, Routing, `devapi.json`, Snapshot-Provider) — plattformneutral, nur Debug/`DEV_API` |
| `msTools.DeveloperApi.Maui` | MAUI-Handler der Developer-API (`/tree`, `/invoke`, `/set-text`, Elementbaum) — baut auf `msTools.DeveloperApi` auf |
| `VideoWebPlayer.Maui.Tests` | Testprojekt (Contract-Tests der MAUI-Oberfläche, MVU-Verhalten, API-Client) |
| `VideoPlayer.E2E.Tests` | Windows-End-to-End-Tests (FlaUI/UI-Automation gegen die echte App + lokalen Mock-Server, inkl. Wiedergabestart) |
| `scripts/` | Deployment-Skripte (u. a. `iOS-Deployment.ps1` für Simulator-, Geräte- und Store-Builds) |

## Secrets und Client-Gate

Der Server schützt die Client-API über ein Client-Gate (`X-API-Key`, Scope `MauiOnly`). Die App löst den Schlüssel zur Laufzeit auf — in dieser Reihenfolge:

1. **Geräte-Token** des gekoppelten Geräts (empfohlen; via QR-/Kurzcode oder Pairing-Code)
2. Umgebungsvariable `VIDEOWEBPLAYER_MAUI_API_TOKEN`
3. MSBuild-Eigenschaft `VideoPlayerApiToken` beim Build (`-p:VideoPlayerApiToken="…"`)
4. Datei `client-api-token.txt` im lokalen Anwendungsdatenverzeichnis (`%LocalAppData%/VideoPlayer`)
5. Platzhalter `<MAUI_CLIENT_API_TOKEN>` (kein produktiv verwendbarer Wert)

Der statische Schlüssel bleibt als Übergangs-Mechanismus konfigurierbar, gilt aber nicht als ausreichende Absicherung in einer verteilten App — Geräte-Tokens sind der primäre Weg. Echte JWT-Schlüssel, Benutzerpasswörter und produktive API-Tokens gehören nicht in dieses Repository.

## Developer-API (nur Debug/Opt-in)

Der Debug-Build startet einen lokalen Mini-HTTP-Server auf `127.0.0.1` (`GET /model`, `GET /tree`, `POST /invoke`, `POST /set-text`) für UI-Automatisierung und E2E-Tests. Release-Builds enthalten die API nicht. Details: `docs/help/developer-api.md`.

## Branching und CI

- `staging` ist der Integrationsbranch; Pull Requests gegen `staging` laufen durch Build-, Test- und Lint-Prüfungen.
- `main` wird ausschließlich aus `staging` über PRs aktualisiert. Nach dem Merge entsteht ein Prerelease bzw. Semantic Release; der iOS-Build legt neben der `.ipa` das Symbolarchiv `release-ios-dsym.zip` am Release ab, damit TestFlight-Crash-Reports symbolisierbar bleiben.
- Details: `docs/help/staging-branch.md`.

## Dokumentation

Ausführliche fachliche Dokumentation liegt unter `docs/help/` — u. a. zu [Serververwaltung & Erststart](docs/help/serververwaltung-erststart.md), [Verbindung & Anmeldung](docs/help/verbindung-anmeldung.md), [Wiedergabe & PiP](docs/help/wiedergabe-pip.md), [Downloads](docs/help/downloads.md), [Offline-Betrieb](docs/help/offline-wiederherstellung.md) und [E2E-Tests](docs/help/e2e-tests.md).

## Changelog

Das Änderungsprotokoll liegt in [changes.log](changes.log), geplante und ausgelieferte Änderungen je Release zusätzlich unter [Releases](https://github.com/martin-stromberg/VideoPlayer-App/releases).

## Lizenz

[PolyForm Noncommercial License 1.0.0](LICENSE) — Nutzung, Veränderung und Weitergabe für nicht-kommerzielle Zwecke erlaubt. Kommerzielle Nutzung erfordert eine separate Lizenz; Anfragen an mstromberg84+videoplayer@gmail.com.
