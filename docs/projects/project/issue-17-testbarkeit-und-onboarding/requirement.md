# Anforderung — Projekt: iOS-Deployment, Developer-API, E2E-Tests (FlaUI) und Serververbindungs-UI

Quelle: GitHub Issue #17 (`martin-stromberg/VideoPlayer-App`) — Sammel-Issue für ein mehrteiliges Projekt; jeder Teil ist ein eigener Lifecycle-Entwicklungsschritt. Ergänzt um vom Anwender bestätigte Entscheidungen aus der Vorbesprechung.
Basisbranch: `staging` (nach Merge von PR #16); Projektbranch: `project/issue-17-testbarkeit-und-onboarding`.

**Vorgabe (bestätigt):** Feste Reihenfolge der Entwicklungsschritte:
1. iOS-Deploy-Fix (MSB1006)
2. Developer-API
3. E2E-Tests (FlaUI)
4. Serververbindungs-UI modernisieren
5. QR-Bootstrap (unabhängig positionierbar)

## Ziel

Das Projekt beseitigt den blockierenden Fehler im iOS-Deployment, schafft mit der Developer-API und FlaUI-E2E-Tests die Testbarkeitsgrundlage für die Windows-App, modernisiert die Serververbindungs-UI und ergänzt ein Selbstbedienungs-Onboarding per QR-Code (Bootstrap-Ticket). Damit werden sowohl der verbliebene Geräte-Restnachweis aus Issue #15 als auch ein deutlich vereinfachter Erststart ermöglicht.

## Fachliche Anforderungen

### Schritt 1 — iOS-Deployment-Skript reparieren (MSB1006)

**Vorgaben:**

- `scripts/iOS-Deployment.ps1` schlägt beim Release-Build mit App-Store-Connect-Upload mit `MSB1006` fehl (ungültige Property/Argumentzeile beim msbuild-Aufruf). Der Fehler ist zu beheben, sodass der Release-Build mit Upload wieder durchläuft.
- **Mitgebende Nebenbedingung aus Issue #15 (`continue.md`):** Nach dem Fix muss der verbliebene Geräte-Restnachweis ausführbar sein — Pairing-Lauf auf einem echten iOS- und einem Android-Gerät (Code-Eingabe → Koppeln → Neustart → Auto-Login mit gespeichertem Geräte-Token; iOS zusätzlich: Keychain-Zugriff nach Neuinstallation/Neustart, `DeviceInfo`-Gerätename, UI-Bedienung). Auf Windows bereits vollständig verifiziert; gerätelos per Contract-Tests gepinnt (883/883).

**Hypothese (zu verifizieren, keine Vorgabe):** Das mit Feature #15 neu eingeführte `-VideoPlayerApiToken`-Passthrough (maskiertes 257-Zeichen-Token) bricht die msbuild-Argumentzeilen-Quotierung.

### Schritt 2 — Developer-API für UI-Automatisierung (nur Debug/Opt-in)

**Vorgaben:**

- HTTP-Schnittstelle in der Windows-App zur strukturierten UI-Analyse und -Steuerung, damit Live-Verifikationen nicht über Screenshots und Koordinatenklicks laufen müssen.
- HTTP-Server auf Port 0; der vom System vergebene Port wird in einer Metadatendatei im Programmverzeichnis abgelegt; Bindung ausschließlich auf `127.0.0.1`.
- Aktivierung **nur im Debug-Build bzw. per explizitem Opt-in** — niemals im Release (Einfallstor).
- Motivation (belegt aus #15-Verifikation): Element-Adressierung (CollectionView-Items erscheinen nicht im UIA-Baum) und Zustandslesen (`CurrentPage`/`ConnectionState` nur über Screenshots/IIS-Logs erschließbar) waren die aufwändigsten Teile.

**Vorschlag aus dem Issue (Annahme, in der Planung zu konkretisieren):**

- `GET /model` — AppModel-Auszug: `CurrentPage`, `ConnectionState`, Flags.
- `GET /tree` — logischer Elementbaum mit `AutomationId`, Text, Bounds, Sichtbarkeit — inklusive Collection-Items, die UIA unter Windows nicht ausliefert.
- `POST /invoke`, `POST /set-text`, optional `POST /dispatch` (MVU-Messages — mit Vorsicht, umgeht die echte UI-Eventkette).

### Schritt 3 — E2E-Tests mit FlaUI

**Vorgaben:**

- Automatisierte UI-Tests gegen die echte `VideoPlayer.Maui.exe` per FlaUI (UIA-Wrapper), damit fehlerhafte Abläufe wie bei #15 (verschwindender Pairing-Abschnitt, Serverwechsel ohne Rückkehr zur Startseite) früher auffallen.
- Szenarien u. a.: Server-Setup-Flows, Pairing-Flow, Serverwechsel, Anmeldung.
- **CI-Einschränkung (belegt):** GitHub Actions auf `VideoPlayer-App` sind derzeit durch ein Billing-Problem blockiert — die E2E-Tests müssen zumindest lokal laufen; die CI-Integration ist als **optional** zu markieren.

**Offener Punkt aus dem Issue (in der Planung zu klären):** Verhältnis zur Developer-API (Schritt 2) — FlaUI nutzt UIA und hat dieselbe CollectionView-Lücke; die Dev-API könnte alternativ als Treiber dienen. Zu klären, ob FlaUI gegen UIA oder gegen die Dev-API fährt.

### Schritt 4 — UI der Serververbindung modernisieren

**Vorgaben:**

- `ServerSetupPage` (Erststart + „Server wechseln" aus den Einstellungen) ist funktional, aber schlicht gehalten: Listen für bekannte/gefundene Server, manuelle Eingabe, Pairing-Abschnitt.
- Modernere Gestaltung: visuelle Hierarchie, Zustandsfeedback (Prüfung läuft/erreichbar/nicht erreichbar), ansprechende Server-Karten statt reiner Textliste, klarere Pairing-Führung.
- Der bestehende MVU-Fluss muss respektiert werden: Zustand kommt aus dem Modell; die Seite bleibt eine Workflow-Seite.

### Schritt 5 — Selbstbedienungs-Pairing und Login per QR-Code (Bootstrap-Ticket)

**Vorgaben (vom Anwender bestätigte Entscheidungen):**

- **QR-Inhalt:** Im QR steckt **Server-Adresse + Einmal-Ticket** als `https://<server>/pairing?t=<ticket>` — niemals Zugangsdaten (kein E-Mail/Passwort im QR).
- **Zielablauf:** „App öffnen → Code scannen → fertig" — ohne Adress- und ohne Passworteingabe.
- **Ticket-Lieferumfang:** Das Ticket liefert **Geräte-Token und Usersession** (JWT); der Refresh-/Erneuerungsmechanismus ist noch zu klären (siehe Offene Fragen).
- **Datenmodell-Perspektive:** Das Modell soll später auch die umgekehrte Richtung für TV tragen (Gerät zeigt Code, Nutzer bestätigt auf der Profilseite — Device-Authorization-Grant-Muster); dieselbe Pending-Pairing-Primitive, Datenmodell von Anfang an für beide Richtungen anlegen (serverseitig).
- **Fallbacks:** Die bestehenden Flows (Admin-Code, manuelle Eingabe, E-Mail/Passwort-Login) bleiben erhalten.

**Kontext aus dem Issue (Vorgaben zum Konzept):**

- Server-Profilseite, Abschnitt „Geräte": Button „Gerät koppeln" erzeugt ein einmaliges Bootstrap-Ticket (kurze TTL ~5 min, gebunden an den eingeloggten Benutzer, Rate-Limit je Benutzer). Anzeige als QR + 8-Zeichen-Text-Fallback + Kurzanleitung.
- App-Ablauf: Kamera-Scan (z. B. ZXing.Net.Maui) oder manuelle Eingabe → flüchtiges ECDH-Schlüsselpaar (vorhandene `PairingCrypto`-Maschinerie) → `POST api/pairing/exchange` bzw. neuer Bootstrap-Endpunkt → Server verbraucht das Ticket atomar und stellt Geräte-Token plus Usersession aus → alles AES-256-GCM-verschlüsselt zurück → App legt Geräte-Token im SecureStorage und die Session ab → direkt Startseite, ohne Passworteingabe.
- Sicherheitsvorgaben: Das Ticket gewährt eine volle Session und ist damit sensibler als der heutige reine Gate-Code — kurze TTL, Single-Use, Rate-Limit; optional konfigurierbar „nur Admins dürfen Tickets erzeugen" / Re-Auth vor Anzeige.

**Repo-Aufteilung (Vorgabe):**

- Der Server-Anteil (Ticket-Modell auf Basis `PairingCodes`/`CreatedByUserId`, Profilseite „Gerät koppeln" mit QR, Bootstrap-Endpunkt, Session-Ausstellung) liegt im Server-Repo `martin-stromberg/VideoPlayer` (lokaler Checkout: `D:\Repositories\softwareschmiede\487c3041-d72b-486e-811f-9ed8db04630b`). Die Server-Implementierung läuft als **eigener Lifecycle im Server-Repo**; im App-Projektplan ist dieser Teil nur als **Schnittstellen-/Koordinationsschritt** abbildbar.
- App-Anteil in diesem Projekt: Kamera-Scan, QR-Parsing, Reuse des Exchange-Clients, Session-Ablage, angepasste Hinweistexte auf der Serververbindungsseite (Synergie mit Schritt 4).

## Betroffene Bereiche

- **`scripts/iOS-Deployment.ps1`** (Schritt 1): msbuild/`dotnet publish`-Aufruf mit Response-Datei (`Add-PropertyLine`/`-p:VideoPlayerApiToken`-Passthrough, u. a. Zeilen ~725, ~884, ~976); Release-Build- und App-Store-Connect-Upload-Pfad.
- **`VideoPlayer.Maui`** (Schritte 2, 4, 5): neuer lokaler HTTP-Server für die Dev-API (Debug/Opt-in); `Pages/ServerSetupPage.xaml` + Code-Behind (Modernisierung, QR-/Ticket-Hinweise, ggf. Scan-Einstieg); `MauiProgram` (DI-Registrierung, bedingte Dev-API-Aktivierung); Kamera-/Scan-Integration (z. B. ZXing.Net.Maui).
- **`VideoPlayer.Mvu`** (Schritte 2, 4, 5): `AppModel`-Auszug für `GET /model`; Elementbaum-Quelle für `GET /tree`; Dispatch-Einstieg für `POST /invoke`/`/set-text`/optional `/dispatch`; neue Messages/Effekte für den QR-Bootstrap-Flow (Ticket-Eingabe/Scan → Exchange → Session-Ablage → `EnterConnected`).
- **`VideoPlayer.ApiClient`** (Schritt 5): Reuse/Erweiterung des Pairing-Exchange-Clients für den Bootstrap-Endpunkt; Empfang und Ablage von Geräte-Token plus Usersession.
- **`VideoPlayer.Storage`** (Schritt 5): Ablage der Usersession neben dem bestehenden Geräte-Token (SecureStorage/`AppSecretsService`).
- **Neues E2E-Testprojekt** (Schritt 3): FlaUI-basierte UI-Tests gegen `VideoPlayer.Maui.exe`; Ausführung mindestens lokal.
- **Server-Repo `martin-stromberg/VideoPlayer`** (Schritt 5, nur Koordination hier): Ticket-Modell, Profilseite „Gerät koppeln", Bootstrap-Endpunkt, Session-Ausstellung — eigener Lifecycle im Server-Repo.

## Abläufe

- **iOS-Release-Deployment (Schritt 1):** `iOS-Deployment.ps1` → Release-Build mit App-Store-Connect-Upload läuft ohne `MSB1006` durch → anschließend manueller Pairing-Lauf auf echtem iOS-/Android-Gerät möglich.
- **Dev-API-Nutzung (Schritt 2):** App im Debug-Build bzw. mit Opt-in starten → HTTP-Server bindet auf `127.0.0.1:0` → Port steht in der Metadatendatei im Programmverzeichnis → Aufrufer liest Modell/Baum und steuert über Invoke/Set-Text.
- **E2E-Ausführung (Schritt 3):** Testlauf startet/treibt die echte `VideoPlayer.Maui.exe` → Szenarien Server-Setup, Pairing, Serverwechsel, Anmeldung → lokal ausführbar; CI-Anbindung optional (Billing-Blocker).
- **QR-Bootstrap (Schritt 5):** Nutzer erzeugt auf der Server-Profilseite ein Einmal-Ticket (QR + Text-Fallback) → App scannt QR bzw. nimmt Code manuell entgegen → flüchtiges ECDH-Paar → Exchange am (neuen) Bootstrap-Endpunkt → Server verbraucht Ticket atomar und liefert Geräte-Token + Usersession verschlüsselt → App speichert beides → Startseite ohne weitere Eingabe. Fehlerpfad: Gerät erreicht die QR-Adresse nicht → verständliche Fehlermeldung; Fallbacks bleiben Admin-Code, manuelle Eingabe, E-Mail/Passwort.

## Konfiguration

- **Developer-API:** nur Debug-Build oder explizites Opt-in; niemals Release. Bindung `127.0.0.1`, Port 0 (systemvergeben), Port-Ablage in Metadatendatei im Programmverzeichnis.
- **E2E/CI:** Lokale Ausführung verbindlich; GitHub-Actions-Integration optional (Billing-Problem, Klärung außerhalb dieses Projekts).
- **Bootstrap-Ticket (serverseitig, nur Kontext):** TTL ~5 Minuten, Single-Use, Rate-Limit je Benutzer; optional konfigurierbar „nur Admins dürfen Tickets erzeugen" / Re-Auth vor QR-Anzeige.
- Keine weiteren neuen App-Einstellungen sind aus der Anforderung belegt.

## Akzeptanzkriterien

1. **Schritt 1:** `scripts/iOS-Deployment.ps1` führt den Release-Build mit App-Store-Connect-Upload ohne `MSB1006` aus; die Ursache der fehlerhaften Argumentzeile ist identifiziert und behoben. Nach dem Fix ist der verbliebene Geräte-Restnachweis aus Issue #15 ausführbar (Pairing-Lauf auf echtem iOS- und Android-Gerät inkl. Keychain-/Keystore-Ablage, `DeviceInfo`-Gerätename, UI-Bedienung).
2. **Schritt 2:** Die Developer-API stellt im Debug-Build bzw. bei Opt-in auf `127.0.0.1` mindestens Modellauszug (`CurrentPage`, `ConnectionState`, Flags) und logischen Elementbaum (inkl. Collection-Items) bereit; der vergebene Port liegt in der Metadatendatei; im Release-Build ist die API nicht erreichbar.
3. **Schritt 3:** FlaUI-E2E-Tests decken Server-Setup-Flows, Pairing-Flow, Serverwechsel und Anmeldung gegen die echte `VideoPlayer.Maui.exe` ab und laufen lokal; CI-Integration ist vorbereitet oder als optional dokumentiert.
4. **Schritt 4:** Die `ServerSetupPage` zeigt modernisierte Gestaltung mit visueller Hierarchie, Zustandsfeedback (Prüfung läuft/erreichbar/nicht erreichbar), Server-Karten und klarerer Pairing-Führung — bei unverändertem MVU-Fluss (Zustand aus dem Modell, Workflow-Seite).
5. **Schritt 5 (App-Anteil):** Die App kann den QR `https://<server>/pairing?t=<ticket>` per Kamera scannen oder das Ticket manuell entgegennehmen, führt den verschlüsselten Austausch durch, legt Geräte-Token und Usersession ab und landet ohne Adress-/Passworteingabe auf der Startseite. Bestehende Flows (Admin-Code, manuelle Eingabe, E-Mail/Passwort) bleiben als Fallback funktionsfähig. Nicht erreichbare Serveradresse aus dem QR führt zu einer verständlichen Fehlermeldung.
6. **Schritt 5 (Koordination):** Die Server-Schnittstelle (Bootstrap-Endpunkt, Ticket-Vertrag, Session-Lieferumfang) ist für die Server-Implementierung im Server-Repo spezifiziert/abgestimmt; das Datenmodell trägt beide Pairing-Richtungen (App-scannt-QR und Gerät-zeigt-Code für TV).

## Nicht-Ziele

- **Server-Implementierung des QR-Bootstrap** (Ticket-Modell, Profilseite, Bootstrap-Endpunkt, Session-Ausstellung) — eigener Lifecycle im Server-Repo `martin-stromberg/VideoPlayer`; hier nur Schnittstellen-/Koordinationsschritt.
- **Behebung des GitHub-Actions-Billing-Problems** — externe Klärung außerhalb dieses Projekts; E2E-CI-Integration nur optional.
- **TV-Richtung des Pairings** (Gerät zeigt Code) — nur als Datenmodell-Perspektive mitnehmen, keine Implementierung.
- **Universal Links / App Links** — nur als Perspektive erwähnt (Bonus-Eigenschaft der https-URL), kein Umsetzungsziel.
- **Änderung des bestehenden MVU-Flusses** der `ServerSetupPage` über die Modernisierung hinaus; kein Umbau zu einem anderen Seiten-/Workflow-Typ.
- **Entfernung der Fallback-Flows** (Admin-Code, manuelle Eingabe, E-Mail/Passwort) — sie bleiben erhalten.
- **Developer-API im Release-Build** — ausdrücklich ausgeschlossen.

## Offene Fragen

- **E2E-Treiber (Schritt 2/3):** Fährt FlaUI gegen UIA (mit der bekannten CollectionView-Lücke) oder gegen die Developer-API als Treiber — oder eine Kombination? Entscheidung in der Planung.
- **Session-Erneuerung (Schritt 5):** Refresh-Token oder gerätegebundene Session-Verlängerung (Refresh nur mit gültigem Geräte-Token)? Sonst muss nach JWT-Ablauf neu gescannt werden. Zu klären — mit Server-Abhängigkeit.
- **QR-Adresse (Schritt 5, serverseitig):** Rendert der Server `Scheme + Host` des aktuellen Requests ins QR? Behandlung des Edge-Cases „Gerät erreicht die Adresse nicht" (verständliche Fehlermeldung gefordert).
- **Ticket-Berechtigung (Schritt 5, serverseitig):** Wird die Option „nur Admins dürfen Tickets erzeugen" bzw. Re-Auth vor QR-Anzeige aktiviert/umgesetzt?
- **Endpunktwahl (Schritt 5):** Wird der bestehende `POST api/pairing/exchange` erweitert/reused oder ein neuer Bootstrap-Endpunkt geschaffen (hängt vom Ticket-Vertrag im Server-Lifecycle ab)?
