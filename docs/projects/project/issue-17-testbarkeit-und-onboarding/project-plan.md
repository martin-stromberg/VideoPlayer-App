# Projektplan — Issue #17: iOS-Deployment, Developer-API, E2E-Tests (FlaUI), Serververbindungs-UI, QR-Bootstrap

Quelle: `requirement.md` (GitHub Issue #17, `martin-stromberg/VideoPlayer-App`), `inventory.md` + `inventory/*.md`.
Basisbranch: `staging` (nach Merge von PR #16); Projektbranch: `project/issue-17-testbarkeit-und-onboarding`.
Jeder Entwicklungsschritt läuft als eigener Lifecycle auf einem Schrittbranch (`task/issue-17-<kurzname>`, abgezweigt vom Projektbranch) und muss unabhängig mergebar und in sich abgeschlossen sein.

## Ziel

Das Projekt beseitigt den blockierenden MSB1006-Fehler im iOS-Deployment, schafft mit Developer-API und FlaUI-E2E-Tests die Testbarkeitsgrundlage für die Windows-App, modernisiert die Serververbindungs-UI und ergänzt ein Selbstbedienungs-Onboarding per QR-Code (Bootstrap-Ticket). Damit werden der verbliebene Geräte-Restnachweis aus Issue #15 und ein deutlich vereinfachter Erststart ermöglicht.

## Entscheidungen (bereits getroffen / aus requirement + inventory abgeleitet)

- **Feste Schritt-Reihenfolge** (vom Anwender bestätigt): 1 iOS-Deploy-Fix → 2 Developer-API → 3 E2E-Tests → 4 UI-Modernisierung → 5 QR-Bootstrap. Die Inventur legt keine Abweichung nahe: Schritt 2 ist Enabler für Schritt 3 (Zustandslesen, CollectionView-Lücke), Schritt 4 vor Schritt 5 ist sinnvoll, weil der QR-/Ticket-Einstieg und die Hinweistexte auf der modernisierten `ServerSetupPage` landen (Synergie, vermeidet doppelten Umbau derselben Datei).
- **QR-Payload:** `https://<server>/pairing?t=<ticket>` — Server-Adresse + Einmal-Ticket, niemals Zugangsdaten.
- **Ticket-Lieferumfang:** Geräte-Token + Usersession (JWT); ECDH P-256 + AES-256-GCM-Kanal der bestehenden Pairing-Maschinerie wird wiederverwendet.
- **Repo-Split Schritt 5:** Server-Teil (Ticket-Modell, Profilseite „Gerät koppeln", Bootstrap-Endpunkt, Session-Ausstellung) ist ein eigener Lifecycle im Server-Repo `martin-stromberg/VideoPlayer` (Checkout `D:\Repositories\softwareschmiede\487c3041-d72b-486e-811f-9ed8db04630b`); im App-Projektplan nur als Koordinationsschritt. App-Teil (Scan, Parsing, Exchange, Session-Ablage) läuft hier.
- **Datenmodell-Perspektive:** serverseitig beide Pairing-Richtungen vorsehen (App-scannt-QR + Gerät-zeigt-Code für TV, Device-Authorization-Grant-Muster); TV-Richtung selbst ist Nicht-Ziel.
- **Dev-API-Sicherheit:** nur Debug-Build oder explizites Opt-in, niemals Release; Bindung `127.0.0.1`, Port 0 (systemvergeben), Port-Ablage in Metadatendatei im Programmverzeichnis. Release-Ausschluss wird compile-time abgesichert (`#if DEBUG || DEV_API`), nicht per Laufzeitflag.
- **E2E/CI:** lokale Ausführung verbindlich; GitHub-Actions-Integration optional (Billing-Blocker, externe Klärung).
- **Fallbacks bleiben:** Admin-Code, manuelle Eingabe, E-Mail/Passwort-Login bleiben voll funktionsfähig.

## Offene Punkte — alle entschieden (ENTSCHEIDUNG-Markierungen je Punkt)

### OP-1: E2E-Treiberstrategie — FlaUI gegen UIA oder Dev-API? (Schritt 3)

- **Kontext:** FlaUI nutzt UIA und hat dieselbe belegte Lücke wie die #15-Verifikation: CollectionView-Items erscheinen nicht im Windows-UIA-Baum (Koordinatenklicks nötig), `CurrentPage`/`ConnectionState` sind über UIA nicht lesbar (`inventory.md` Z. 85, `inventory/flaui-e2e.md` Z. 18–26).
- **Optionen:**
  - *A — rein FlaUI/UIA:* echte Nutzerinteraktion, aber Listen nur per Koordinatenklick (fragil), Zustandsassertions nur über Screenshots — genau der Aufwandstreiber aus #15.
  - *B — Dev-API als Treiber:* schließt beide Lücken (`GET /model`, `GET /tree` inkl. Collection-Items), aber die Tests prüfen nicht mehr die echte UI-Eingabekette.
  - *C — Kombination:* FlaUI für Prozessstart, Seitenpräsenz und echte Klicks/Eingaben; Dev-API für Zustands-Assertions und Listen-Interaktion.
- **Empfehlung: C.** FlaUI bleibt der Testrahmen (requirement nennt FlaUI verbindlich); die Dev-API ergänzt als Assertions-/Steuerungskanal genau dort, wo UIA nachweislich lückenhaft ist.
- **ENTSCHEIDUNG: C (Kombination)** — FlaUI für Start/Klicks/Seitenpräsenz, Dev-API für Zustands-Assertions und Listen-Interaktion.

### OP-2: Session-Erneuerung nach Bootstrap (Schritt 5, Server-Abhängigkeit)

- **Kontext:** Das Bootstrap-Ticket liefert einen JWT mit 12 h Laufzeit; es existiert **kein** Refresh-Token (`AuthorizationTokenService`, server). `SilentReloginAsync` (`AuthSessionService.cs` Z. 303–351) erneuert bei 401 nur über gespeicherte Zugangsdaten — die der QR-Flow gerade nicht ablegt. Ohne Lösung landet der Nutzer nach 12 h wieder auf der Login-Seite bzw. muss neu scannen.
- **Optionen:**
  - *A — Refresh-Token:* klassisch, erfordert neue Token-Entität + Rotationslogik serverseitig.
  - *B — Gerätegebundene Erneuerung:* neuer Endpunkt, der bei gültigem Geräte-Token einen neuen JWT ausstellt (Geräte-Token ist ohnehin revokierbar — passt zum „Gerät koppeln/widerrufen"-Mentalmodell). Deutlich weniger serverseitiger Neubau als A.
  - *C — Vorerst 12-h-Fallback dokumentieren:* App zeigt nach Ablauf den Login; Erneuerung wird nachgezogen.
- **Empfehlung: B** (gerätegebundene Erneuerung über Geräte-Token) als Ziel im Server-Lifecycle; falls der Server-Lifecycle das nicht mitliefert, **C** als dokumentiertes Interim.
- **ENTSCHEIDUNG: A (klassischer Refresh-Token)** — neue Token-Entität + Rotationslogik serverseitig; Teil des Schritt-5a-Server-Lifecycles.

### OP-3: Dev-API-Implementierung — eigener Mini-HTTP vs. Paketabhängigkeit (Schritt 2)

- **Kontext:** Port 0 (systemvergeben) ist mit `HttpListener` nicht möglich (feste Prefixes nötig); kein HTTP-Server-Paket im Projekt (`inventory/developer-api.md` Z. 36).
- **Optionen:**
  - *A — `TcpListener(IPAddress.Loopback, 0)` + eigener Mini-HTTP-Parser:* plattformneutral, keine Paketabhängigkeit, Port 0 nativ; Mehraufwand für HTTP-Parsing (überschaubar: GET/POST mit JSON, ~4 Routen).
  - *B — Kestrel/ASP.NET-Core-Minimalserver:* komfortables Routing, aber schwergewichtige Framework-Abhängigkeit in der MAUI-App für eine Debug-API.
  - *C — `HttpListener` mit Workaround:* Port vorab per `TcpListener` ermitteln, freigeben, dann `HttpListener` auf diesen Port binden — Race-Condition, unsauber.
- **Empfehlung: A.** Kleine, kontrollierte Oberfläche ohne neue Abhängigkeit; die API hat nur 3–5 Routen mit JSON-Payloads.
- **ENTSCHEIDUNG: A** — `TcpListener(IPAddress.Loopback, 0)` + eigener Mini-HTTP-Parser, keine Paketabhängigkeit.

### OP-4: QR-Unterpfad-Parsing-Konvention (Schritt 5b)

- **Kontext:** `ServerAddress.TryNormalize` kürzt auf `GetLeftPart(Path)`; ein QR `https://host/pairing?t=…` muss auf die Server-Basis reduziert werden. Bei Servern unter einem Unterpfad (`http://server/videoplayer/`, Placeholder in `AddressEntry`) darf `/pairing` nicht in die Basisadresse wandern (`inventory.md` Z. 160).
- **Optionen:**
  - *A — Konvention „letztes Pfadsegment `pairing` abschneiden":* QR ist immer `<basis>/pairing?t=…`; die App entfernt genau das abschließende `/pairing`-Segment und normalisiert den Rest. Funktioniert auch bei Unterpfad-Servern (`https://host/videoplayer/pairing?t=…` → Basis `https://host/videoplayer/`).
  - *B — Ticket enthält die Basisadresse explizit (z. B. zweiter Query-Parameter):* robuster gegen Konvention, aber längere QR-Payload und Vertragsänderung.
- **Empfehlung: A** — einfach, deckt Unterpfad-Server ab; die Parsing-Regel wird im App-Schritt als Contract-Test gepinnt. Manuelle Ticket-Eingabe (8-Zeichen-Fallback) nutzt weiterhin die bereits verbundene/eingegebene Adresse.
- **ENTSCHEIDUNG: A** — abschließendes `/pairing`-Segment abschneiden; `t=`-Wert ist opak und wird nicht mit `PairingCode.TryNormalize` geprüft.

### OP-5: `POST /dispatch`-Endpunkt in der Dev-API? (Schritt 2)

- **Kontext:** `dispatch` erzeugt `AppMessage`-Records direkt am `MvuHost` und umgeht die echte UI-Eventkette — im Issue ausdrücklich „mit Vorsicht" versehen (requirement Z. 41). Für E2E-Szenarien ist er der einzige Weg zu deterministischen Zustandsvorgaben (z. B. Serverliste vorbefüllen).
- **Optionen:**
  - *A — enthalten:* volle Steuerbarkeit, aber Tests können unbeabsichtigt die UI-Kette umgehen (falsches Vertrauen).
  - *B — weglassen:* Tests bleiben „ehrlich", verlieren aber Szenario-Setup-Möglichkeiten.
  - *C — enthalten, aber separat abgesichert:* eigener Opt-in-Schalter/Route-Präfix, dokumentiert als „kein Ersatz für echte UI-Pfade".
- **Empfehlung: A/C** — aufnehmen (die Dev-API ist ohnehin debug-only), aber als Opt-in-Endpunkt markieren und in den E2E-Konventionen festlegen: UI-nahe Pfade bevorzugen, `dispatch` nur für Setup/Assertions.
- **ENTSCHEIDUNG: B (kein dispatch-Endpunkt)** — die Dev-API umfasst nur `GET /model`, `GET /tree`, `POST /invoke`, `POST /set-text`; Tests bleiben auf der echten UI-Eingabekette.

### OP-6: Bootstrap-Endpunkt — `exchange` erweitern oder neu? (Schritt 5, Server-Lifecycle)

- **Kontext:** `POST api/pairing/exchange` liefert heute nur den Geräte-Token; Bootstrap muss zusätzlich eine Usersession liefern. DTOs sind in beiden Repos separate Kopien (`inventory/qr-bootstrap-server.md` Z. 13).
- **Optionen:**
  - *A — `exchange` erweitern:* ein Endpunkt, aber der bestehende Vertrag ändert sich (Response-Payload wächst; Admin-Code-Flow würde plötzlich Sessions ausstellen — unerwünscht).
  - *B — neuer `api/pairing/bootstrap`:* sauber getrennter Vertrag (Ticket statt Kurz-Code, Response = Geräte-Token + JWT); `exchange` bleibt unverändert für den Admin-Code-Fallback.
- **Empfehlung: B** — eigener Endpunkt; vermeidet, dass der Admin-Code-Flow implizit User-Sessions ausstellt, und hält beide Verträge stabil.
- **ENTSCHEIDUNG: B** — neuer Endpunkt `api/pairing/bootstrap`; `exchange` bleibt unverändert.

### OP-7: Ticket-Berechtigung — „nur Admins" bzw. Re-Auth vor QR-Anzeige? (Schritt 5, Server-Lifecycle)

- **Kontext:** Das Ticket gewährt eine volle Session — sensibler als der heutige Gate-Code. Requirement nennt optional konfigurierbar „nur Admins dürfen Tickets erzeugen" / Re-Auth vor Anzeige (requirement Z. 75, 103, 130). Serverseitig existiert `IsAdmin`-Claim und `CheckAdmin`-Muster; ein Rate-Limit **je Benutzer** existiert nicht (nur IP-basiert) und wäre neu zu bauen.
- **Optionen:**
  - *A — jeder eingeloggte Benutzer + Rate-Limit je User:* einfachste UX, Sicherheit über TTL/Single-Use/Rate-Limit.
  - *B — Option „nur Admins" (konfigurierbar, Default aus):* flexible Absicherung, geringer Mehraufwand (Claim-Check + Config).
  - *C — Re-Auth vor QR-Anzeige:* höchste Hürde, aber UX-Bruch im „Gerät koppeln"-Flow.
- **Empfehlung: B** — konfigurierbare Admin-Option einbauen (Default: alle eingeloggten Benutzer), Rate-Limit je User verbindlich; Re-Auth nur falls der Anwender es explizit will.
- **ENTSCHEIDUNG: B** — alle eingeloggten Benutzer + Rate-Limit je User + konfigurierbare Admin-Option (Default: aus).

## Entwicklungsschritte

### Schritt 1 — iOS-Deployment-Skript reparieren (MSB1006)

- **Ziel:** `scripts/iOS-Deployment.ps1` führt den Release-Build mit App-Store-Connect-Upload ohne `MSB1006` aus; Ursache der fehlerhaften Argumentzeile identifiziert und behoben.
- **Schrittbranch:** `task/issue-17-ios-deploy-fix` (von `project/issue-17-testbarkeit-und-onboarding`)
- **Zusammenfassung:** Alle drei MSBuild-Aufrufe laufen über Response-Dateien (`Add-PropertyLine` → `-p:Name="Wert"`, `New-ResponseFile`); das neu eingeführte `-p:VideoPlayerApiToken`-Passthrough (Z. 725, 884, 976) ist der Hauptverdächtige (Quotes/`#`-Kommentarzeichen/Zeilenumbruch im Token — Hypothesen in `inventory/ios-deployment.md` Z. 26–34). Der Schritt verifiziert die Ursache (gezielte Reproduktion mit Test-Tokens), implementiert eine sanitizing-sichere `Add-PropertyLine`-Variante (Trimmen, `\r\n"#` prüfen/escapen oder Quotes weglassen, kontrollierter Abbruch bei Verstoß) und pinnt die Response-Datei-Erzeugung per Contract-Test.
- **Betroffene Bereiche:** `scripts/iOS-Deployment.ps1` (`Add-PropertyLine` Z. 685–693, `New-ResponseFile` Z. 710–716, Einspeisestellen Z. 725/884/976); ggf. neuer Contract-Test in `VideoWebPlayer.Maui.Tests` (Muster `PairingContractTests`-Quelltext-Prüfung).
- **Abhängigkeiten:** keine (erster Schritt).
- **Akzeptanzkriterien:**
  1. Die MSB1006-Ursache ist identifiziert und dokumentiert (welche Zeichenklasse/Quote-Konstellation die Zeile bricht).
  2. `Add-PropertyLine`/`New-ResponseFile` erzeugen für problematische Token-Werte gültige Response-Dateien oder brechen mit verständlicher Fehlermeldung ab — kein unverständliches MSB1006 mehr.
  3. Ein automatisierter Test pinnt die Sanitizing-Regeln (Zeichenklassen, Quoting-Verhalten der rsp-Zeilen).
  4. Secrets werden weiterhin nur maskiert angezeigt (`Add-MaskedDisplay`-Konvention).
  5. Der Geräte-Restnachweis aus Issue #15 ist nach dem Fix ausführbar (manueller Nachweis, nicht Teil der Code-Verifikation).
- **Verifikation:** In dieser Umgebung nur bis Code-Ebene (kein Mac): Skript-Analyse, Sanitizing-Implementierung, Contract-Tests, maskierte Diagnoseausgabe bei Fehlschlag. **Nicht hier verifizierbar:** tatsächlicher `store`-Lauf mit Upload (Transporter/macOS), Geräte-Pairing-Lauf — bleibt manueller Nachweis; der Schritt dokumentiert den Fix-Ansatz und die Verifikationsanleitung.

### Schritt 2 — Developer-API für UI-Automatisierung (Debug/Opt-in)

- **Ziel:** HTTP-Schnittstelle in der Windows-App für strukturierte UI-Analyse/-Steuerung; `127.0.0.1`, Port 0, Metadatendatei, niemals Release.
- **Schrittbranch:** `task/issue-17-developer-api`
- **Zusammenfassung:** Neuer lokaler Mini-HTTP-Server (entschieden: `TcpListener` Port 0, OP-3) greift auf `MvuHost.Model` (`GET /model`), traversiert den Visual Tree plus `ItemsSource`-Ableitung für Collection-Items (`GET /tree`) und steuert über `POST /invoke`/`/set-text`. **Kein `dispatch`-Endpunkt** (entschieden, OP-5) — die API bleibt auf der echten UI-Eingabekette. Aktivierung compile-time (`#if DEBUG` + Opt-in-Konstante), Start über DI-Singleton in `MauiProgram`/`App.CreateWindow`; vergebener Port wird in `devapi.json` neben der Exe abgelegt. **Plattformscope:** Metadaten-Ablage/Opt-in zielt auf Windows (`AppContext.BaseDirectory` schreibbar bei unpackaged); auf anderen Plattformen Fallback-Ablageort (`FileSystem.AppDataDirectory`) oder Verzicht — die Dev-API dient primär der Windows-E2E.
- **Betroffene Bereiche:** `VideoPlayer.Maui` (neuer Dev-API-Server, `MauiProgram.cs` DI-Registrierung Z. 21–128, `App.xaml.cs` `CreateWindow` Z. 32–60); `VideoPlayer.Mvu` (`MvuHost.cs` Z. 30/44–68, `AppModel.cs`-Auszug, `AppRoutes.cs`); Tests in `VideoWebPlayer.Maui.Tests`.
- **Abhängigkeiten:** Schritt 1 nicht fachlich nötig (rein Windows), aber Reihenfolge ist vorgegeben. Keine Code-Abhängigkeit zu Schritt 1.
- **Akzeptanzkriterien:**
  1. `GET /model` liefert mindestens `CurrentPage`, `ConnectionState`, Flags (`IsPairingRequired`, `IsServerCheckRunning`, `LoginError` o. ä.) — keine Tokens/Passwörter (per Konstruktion: nur `AppModel`-Felder).
  2. `GET /tree` liefert den logischen Elementbaum mit `AutomationId`, Typ, Text, Bounds, Sichtbarkeit — **inklusive CollectionView-Items** (Ableitung aus `ItemsSource` + ItemTemplate).
  3. `POST /invoke` löst das Clicked-Äquivalent eines Elements per `AutomationId` aus; `POST /set-text` setzt `Entry.Text` inkl. `TextChanged`.
  4. Server bindet ausschließlich `127.0.0.1`, Port vom System vergeben, Port in Metadatendatei im Programmverzeichnis; API im Release-Build nicht vorhanden (Compile-Time-Ausschluss, gepinnt per Contract-Test auf `#if`-Struktur oder Assembly-Prüfung).
  5. **Kein** `dispatch`-Endpunkt (entschieden, OP-5); die API umfasst nur `GET /model`, `GET /tree`, `POST /invoke`, `POST /set-text`.
- **Verifikation:** Debug-Build starten, `devapi.json` lesen, alle Endpunkte per HTTP-Client prüfen (automatisiert: Integrationstest gegen laufende Debug-App oder strukturelle Tests); Release-Build-Nachweis, dass die API nicht existiert. In dieser Umgebung verifizierbar, sofern Windows-Debug-Build lauffähig; andernfalls Code-Ebene + Contract-Tests.

### Schritt 3 — E2E-Tests mit FlaUI

- **Ziel:** Automatisierte UI-Tests gegen die echte `VideoPlayer.Maui.exe`; Szenarien Server-Setup, Pairing, Serverwechsel, Anmeldung; lokal ausführbar, CI optional.
- **Schrittbranch:** `task/issue-17-flaui-e2e`
- **Zusammenfassung:** Neues FlaUI-Testprojekt (xUnit, FlaUI.UIA3) startet die unpackagede `VideoPlayer.Maui.exe` (`WindowsPackageType=None`, csproj Z. 23) direkt — im **Debug-Build**, damit die Dev-API aus Schritt 2 verfügbar ist. Treiberstrategie (entschieden OP-1, Kombination): FlaUI für Start/Klicks/Seitenpräsenz, Dev-API für Zustands-Assertions und Listen-Interaktion. App-Start ohne Server landet deterministisch auf `ServerSetupPage` — idealer Ausgangszustand. **Fixture-Provisionierung** (wegen entschiedenem Verzicht auf `dispatch`): Test-Isolation über `VIDEOPLAYER_PROFILE` (vorhandener Mechanismus, `AppProfile.FromEnvironment`); Backend-Wahl im Schritt-Lifecycle (lokaler Server-Checkout `487c3041-…` mit Seed-Daten vs. Mock-Server — ein Mock müsste den ECDH/AES-GCM-Austausch implementieren, `PairingCrypto` ist im Test-Harness wiederverwendbar); der gepaarte Zustand für Serverwechsel/Anmeldung wird über die **echte UI-Kette** hergestellt (Adresse → Pairing-Code → Login), da kein `dispatch`-Endpunkt existiert; das Test-Backend muss so konfiguriert sein, dass das Client-Gate ein Pairing verlangt (kein statischer API-Token), damit `IsPairingRequired` greift.
- **Betroffene Bereiche:** neues Projekt `VideoPlayer.E2E.Tests` (o. ä.) in der Solution; ggf. `.github/workflows/` für optionale CI-Stufe; Dev-API-Nutzung aus Schritt 2.
- **Abhängigkeiten:** **Schritt 2 erforderlich** — die gewählte Kombinationsstrategie (OP-1 = C) braucht die Dev-API für `GET /model`/`/tree`-Assertions und Listen-Interaktion.
- **Akzeptanzkriterien:**
  1. E2E-Projekt baut und startet `VideoPlayer.Maui.exe` selbstständig (inkl. `dotnet build` der App; RID-Unterordner `win-x64` lokal verifizieren, `inventory.md` Z. 158).
  2. Szenarien abgedeckt: Server-Setup (Erststart → Adresse → Verbinden → `ConnectionState`-Assertion), Pairing-Flow, Serverwechsel (`SettingsPage` → `serversetup`-Route → Rückkehr zur Startseite — Regression für den #15-Fehler), Anmeldung.
  3. Tests laufen lokal per `dotnet test`; CI-Integration vorbereitet oder als optional dokumentiert (Billing-Blocker, kein interaktiver Desktop auf GH-hosted Runnern).
  4. Keine Koordinatenklicks für CollectionView-Items (abgedeckt über Dev-API).
- **Verifikation:** Lokaler Testlauf; Server-abhängige Szenarien benötigen laufenden Server (lokaler Server-Checkout möglich). **Nicht verifizierbar hier:** CI-Ausführung (Billing + kein Desktop) — als optional dokumentiert.

### Schritt 4 — UI der Serververbindung modernisieren

- **Ziel:** `ServerSetupPage` mit visueller Hierarchie, Zustandsfeedback, Server-Karten und klarerer Pairing-Führung — bei unverändertem MVU-Fluss.
- **Schrittbranch:** `task/issue-17-server-setup-ui`
- **Zusammenfassung:** Restrukturierung der 113-zeiligen Workflow-Seite mit `Vp*`-Tokens und Kartenmuster aus `HomePage.xaml` (Border + `RoundRectangle`, Gradient-Layering): Server-Einträge werden Karten mit Health-Badge (`AppModel.ServerHealth`/`ServerHealthText` existiert, ist aber ungenutzt), der `PairingSection` wird als hervorgehobene Karte geführt, Zustandsfeedback (Prüfung läuft/erreichbar/nicht erreichbar) wird visuell explizit. Gepinnte AutomationIds und XAML-Merkmale bleiben erhalten oder Contract-Tests werden bewusst angepasst.
- **Betroffene Bereiche:** `VideoPlayer.Maui/Pages/ServerSetupPage.xaml` + `.xaml.cs` (`UpdateUi` Z. 48–82); ggf. `Resources/Styles/Styles.xaml`; Contract-Tests `ServerSetupContractTests`/`PairingContractTests` (Pins Z. 27–120 bzw. Z. 32/45–55).
- **Abhängigkeiten:** keine fachliche (MVU-Modell unverändert). **Vor Schritt 5b sinnvoll** (vorgegebene Reihenfolge), weil der QR-/Ticket-Einstieg und Hinweistexte auf der modernisierten Seite landen.
- **Akzeptanzkriterien:**
  1. Visuelle Hierarchie: Kopfbereich mit aktivem Server, Karten für bekannte/gefundene Server statt reiner Textliste, hervorgehobene Pairing-Führung.
  2. Zustandsfeedback je Server/Aktion: „Prüfung läuft" (Indicator), „erreichbar"/„nicht erreichbar" (Badge/Text via `ServerHealthText`/`ErrorLabel`).
  3. MVU-Fluss unverändert: Zustand ausschließlich aus `AppModel`, gleiche Dispatch-Messages, Route `serversetup` weiterhin ohne FlyoutItem.
  4. Alle gepinnten AutomationIds erhalten (oder Contract-Tests bewusst aktualisiert, inkl. `PairingCodeEntry` ohne `MaxLength`); `Vp*`-Tokens statt harter Farben; Dark-only.
- **Verifikation:** `dotnet build`, Contract-Test-Suite (`ServerSetupContractTests`, `PairingContractTests`, `DesignTokensContractTests`), manuelle Sichtprüfung der Seite in beiden Kontexten (Erststart, „Server wechseln" aus Einstellungen).

### Schritt 5a — QR-Bootstrap Server (Koordination + eigener Server-Lifecycle)

- **Ziel:** Serverseitige Bootstrap-Implementierung: Ticket-Modell, Profilseite „Gerät koppeln" mit QR, Bootstrap-Endpunkt mit Session-Ausstellung.
- **Schrittbranch:** eigener Task-Branch im **Server-Repo** `487c3041-…`, Basis `staging` (z. B. `task/qr-bootstrap-ticket` bzw. nach dortiger Konvention `task/issue-<n>-…`).
- **Zusammenfassung:** Dieser Schritt läuft als eigener Lifecycle im Server-Repo; im App-Projektplan ist er der Schnittstellen-/Koordinationsschritt. Zu liefern: `PairingCode`-Modell-Erweiterung (Ticket-`Kind`/`Purpose` für beide Richtungen, längeres Ticket-Secret; **Verhältnis Kurzcode↔Ticket im Vertrag festlegen** — der 8-Zeichen-Text-Fallback ist ein Alias/Lookup auf das Ticket, nicht das Secret selbst), Profilseite „Geräte" → „Gerät koppeln" (QR `https://<server>/pairing?t=<ticket>` aus `Scheme+Host` des Requests, 8-Zeichen-Text-Fallback, Kurzanleitung), neuer `api/pairing/bootstrap`-Endpunkt (entschieden OP-6; öffentlich wie `exchange`, atomarer Ticket-Verbrauch via `ExecuteUpdateAsync`-Muster, AES-GCM-Payload = Geräte-Token + JWT + Refresh-Token), Rate-Limit je Benutzer, TTL ~5 min, konfigurierbare Admin-Option (entschieden OP-7), Refresh-Endpunkt mit Rotation (entschieden OP-2). Vertrag (Request/Response-Feldnamen, Krypto-Parameter, Fehlercode-Mapping 400/401/429 für QR-Ticket und Kurzcode-Alias, appseitige Vorvalidierung der manuellen Eingabe) wird in beiden Repos gepflegt.
- **Betroffene Bereiche (Server-Repo):** `Data/PairingCode.cs` (+ Migration), `Services/PairingService.cs` (Bootstrap-Variante von `ExchangeAsync` Z. 182–233), `Controllers/` neuer Bootstrap-Endpunkt, `Components/Account/Pages/Manage/*` oder neue Profilseite, `AuthorizationTokenService` (Session-Ausstellung + **klassischer Refresh-Token** mit Rotation, entschieden OP-2), `docs/API.md`.
- **Abhängigkeiten:** **Vorab prüfen: ist der Server-Pairing-Branch `task/issue-228-…` (Geräte-Pairing, alle 47 Tasks „Erledigt", Checkout steht aktuell auf diesem Branch) bereits nach `staging` gemergt?** Falls nicht, muss das vor dem Anlegen des Bootstrap-Branches geklärt werden — die Bootstrap-Arbeit baut direkt auf `PairingCode`/`PairingService`/`PairingController` auf.
- **Akzeptanzkriterien:**
  1. Eingeloggter Nutzer kann auf der Profilseite ein Einmal-Ticket erzeugen (QR + 8-Zeichen-Fallback + Anleitung); TTL ~5 min, Single-Use, Rate-Limit je Benutzer.
  2. `api/pairing/bootstrap` verbraucht das Ticket atomar und liefert ECDH-verschlüsselt Geräte-Token + Usersession (JWT); Endpunkt öffentlich (Gerät hat noch keinen Gate-Schlüssel).
  3. Datenmodell trägt beide Pairing-Richtungen (Feld für `Kind`/`Purpose` o. ä.).
  4. Vertrag (Endpunkt, Feldnamen, Fehlercodes 400/401/429, Krypto-Layout) ist spezifiziert und mit dem App-Schritt abgestimmt; `docs/API.md` aktualisiert.
  5. Ticket-Schutz: alle eingeloggten Benutzer + Rate-Limit je User + konfigurierbare Admin-Option (entschieden, OP-7); Session-Erneuerung über klassischen Refresh-Token mit Rotation (entschieden, OP-2).
  6. Paketwahl dokumentiert: QR-Erzeugung (z. B. QRCoder o. ä.), gepinnte Version (≥7 Tage alt), Lizenz (MIT o. ä.), .NET-10-Kompatibilität.
- **Verifikation:** Server-Tests (`PairingWebApplicationFactory`-Muster, `DevicePairingE2ETests`-Vorbild vorhanden); läuft im Server-Repo, nicht in dieser Umgebung.

### Schritt 5b — QR-Bootstrap App (Scan, Exchange, Session-Ablage)

- **Ziel:** „App öffnen → Code scannen → fertig": QR-Scan oder manuelle Ticket-Eingabe, verschlüsselter Austausch, Ablage von Geräte-Token + Session, direkte Landung auf der Startseite.
- **Schrittbranch:** `task/issue-17-qr-bootstrap-app`
- **Zusammenfassung:** Kamera-Scan primär auf **iOS/Android** (Kandidat ZXing.Net.Maui — Berechtigungen: `NSCameraUsageDescription` iOS, `CAMERA` Android). **Wichtig:** ZXing.Net.Maui unterstützt Barcode-*Scanning* auf Windows offiziell nicht, und die `webcam`-Capability ist für die unpackagede App (`WindowsPackageType=None`) wirkungslos — unter Windows bleibt der **manuelle Ticket-Einstieg** der Weg (erfüllt AK5 „scannen *oder* manuell"); die Windows-Tauglichkeit der Paketwahl ist im Schritt-Lifecycle zu verifizieren (Alternative z. B. `BarcodeScanning.Native.Maui`). Manueller Ticket-Einstieg landet auf der modernisierten `ServerSetupPage` (Synergie Schritt 4). Neuer `BootstrapTicketParser` (Konvention OP-4: abschließendes `/pairing`-Segment abschneiden, `t=` opak — **nicht** mit `PairingCode.TryNormalize` prüfen; Kurzcode-Alias gemäß 5a-Vertrag), neuer MVU-Flow analog `PairingCodeSubmittedMessage` → `ExchangePairingCodeEffect` → `EnterConnected` (`AppUpdate.cs` Z. 479–543, 681) — **mit Zwischenschritt**: QR-Basisadresse normalisieren, Erreichbarkeit prüfen, als aktiven Server persistieren und in `KnownServers` aufnehmen (`PersistActiveServer`-/`KnownServersService`-Muster), **dann erst** Exchange + Session (sonst scheitert `AttemptAutoLoginEffect`/die adressgebundene Gate-Aktivierung `SetPairedTokenIfCurrentAsync`, `ShellAppEffectHandler.cs` Z. 1230–1249). Exchange über erweiterten `DevicePairingClient` gegen den Bootstrap-Endpunkt, Ablage via `AppSecretsService.SetDeviceTokenAsync`/`SetAuthTokenAsync` + neuem Refresh-Token-Schlüssel. Fehlerpfad „QR-Adresse nicht erreichbar" → verständliche Meldung (`PairingStatus.ServerUnreachable`-Muster).
- **Betroffene Bereiche:** `VideoPlayer.Maui` (csproj-Paket, Plattform-Manifeste — iOS `NSCameraUsageDescription`, Android `CAMERA`, **kein** Windows-Capability nötig, `ServerSetupPage` QR-/Ticket-Einstieg + Hinweistexte); `VideoPlayer.Mvu` (neue Messages/Effekte, `BootstrapStatus`-Fehlermapping, **Refresh-Pfad in `AuthSessionService`**: `SilentReloginAsync`/`TryStoredLoginAsync`/`LogoutAsync` Z. 100–127/159–174/303–351 — Refresh vor Credential-Fallback, Rotations-Ablage); `VideoPlayer.ApiClient` (`DevicePairingClient`-Erweiterung/analoger Bootstrap-Call, neue DTOs, Refresh-Endpunkt-Aufruf); `VideoPlayer.Storage` (`AppSecretsService` — Session-Ablage vorhanden Z. 89–93/125–149, **neuer profilpräfixierter Refresh-Token-Schlüssel** analog `AuthTokenKey`).
- **Abhängigkeiten:** **Schritt 5a-Vertrag muss feststehen** (Endpunkt, Payload-Feldnamen, Fehlercodes, Kurzcode-Alias) — Implementierung kann nach Vertragsfreigabe beginnen; **der Refresh-Endpunkt (Krit. 5) muss von 5a geliefert werden**; Endverifikation braucht lauffähigen Server-Stand. **Schritt 4 empfohlen vorher** (UI-Einstieg landet auf modernisierter Seite; Reihenfolge vorgegeben).
- **Akzeptanzkriterien:**
  1. QR `https://<server>/pairing?t=<ticket>` per Kamera scannbar (iOS/Android; Windows via manuellem Ticket-Einstieg) oder Ticket manuell eingebbar; Unterpfad-Server korrekt aufgelöst (OP-4-Konvention, Contract-Test-gepinnt).
  2. QR-Basisadresse wird normalisiert, auf Erreichbarkeit geprüft, als aktiver Server persistiert und in `KnownServers` aufgenommen **vor** dem Exchange; verschlüsselter Austausch am Bootstrap-Endpunkt; Geräte-Token + Usersession + Refresh-Token werden abgelegt; App landet ohne Adress-/Passworteingabe auf der Startseite (`EnterConnected`).
  3. Nicht erreichbare Serveradresse aus dem QR → verständliche Fehlermeldung; ungültiges/abgelaufenes Ticket und ungültiger Kurzcode-Alias → passendes Fehler-Mapping (400/401/429 gemäß 5a-Vertrag).
  4. Fallbacks (Admin-Code, manuelle Eingabe, E-Mail/Passwort) bleiben funktionsfähig (Contract-Tests).
  5. Session-Erneuerung: App nutzt den Refresh-Token aus der Bootstrap-Response gegen den Refresh-Endpunkt (aus 5a), mit Rotation — Ablage über `AppSecretsService`/SecureStorage, Einhängung in `SilentReloginAsync`/`TryStoredLoginAsync` vor dem Credential-Fallback, Aufräumen in `LogoutAsync`.
  6. Paketwahl dokumentiert: gewähltes Scan-/QR-Paket, gepinnte Version (≥7 Tage alt), Lizenz (MIT o. ä.), .NET-10-Kompatibilität, Windows-Support-Status verifiziert.
- **Verifikation:** Unit-/Contract-Tests (Parser, MVU-Transitions, Fehlermapping, Refresh-Pfad); Live-Durchlauf gegen lokalen Server-Checkout, sofern 5a-Stand verfügbar; **nicht verifizierbar hier:** Kamera-Scan auf echtem Gerät (manueller Nachweis), iOS-Berechtigungsdialog.

## Risiken

| Risiko | Eintritt/Wirkung | Gegenmaßnahme |
|--------|------------------|----------------|
| **MSB1006-Hypothese unbewiesen** | Kein dokumentierter Fehlerlauf; der Fix könnte die falsche Ursache adressieren | Schritt 1 beginnt mit gezielter Reproduktion (Test-Tokens mit `"`, `#`, Zeilenumbruch); Sanitizing deckt alle Kandidatenklassen ab; Fehlschlag-Transparenz (maskierte rsp-Zeile loggen) |
| **UIA-Lücke unterläuft E2E-Wert** | CollectionView-Items/Zustandslesen auch mit FlaUI nicht adressierbar → fragile Koordinatenklicks | Entschieden (OP-1 = Kombination mit Dev-API); Restrisiko ist die korrekte Umsetzung der Kombination; Schritt 2 vor Schritt 3 (vorgegeben) |
| **CI-Billing + fehlender Desktop** | E2E-Tests können nicht in GitHub Actions laufen (Billing blockiert, kein interaktiver Desktop) | Lokale Ausführung verbindlich, CI optional dokumentiert; Billing-Klärung außerhalb des Projekts (Nicht-Ziel) |
| **Repo-Koordination Schritt 5** | Vertragsdrift App↔Server (separate DTO-Kopien); Server-Branch issue-228 ggf. ungemergt; parallele Lifecycles | Vertrag zuerst festzurren (5a vor 5b-Implementierung); Merge-Status von issue-228 vor Server-Branch-Anlage prüfen; Contract-Tests auf beiden Seiten |
| **Session-Erneuerung (entschieden: Refresh-Token, OP-2)** | Bootstrap-Session endet nach 12 h; ohne Refresh-Mechanismus UX-Bruch („dauerhaft gekoppelt" erwartet) | Server-Lifecycle 5a liefert Refresh-Endpunkt mit Rotation; falls 5a dies nicht mitliefert, ist 5b-Krit. 5 nicht erfüllbar → Abhängigkeit explizit |
| **Dev-API als Einfallstor** | Versehentlich im Release aktiv → lokale Steuerungsschnittstelle exponiert | Compile-Time-Ausschluss statt Laufzeitflag; Contract-Test pinnt Release-Abwesenheit; Loopback-only |
| **Contract-Test-Pins blockieren UI-Modernisierung** | AutomationId-/Struktur-Pins bremsen Schritt 4 | Pins explizit auflisten; bewusste Test-Anpassung als Teil des Schritts, nicht als Verstoß |
| **Neue Paketabhängigkeiten** | ZXing.Net.Maui (App) und QR-Erzeugung (Server) sind neue Dependencies | Bewährte Pakete wählen; Alternativen (manueller Einstieg bleibt Fallback) im Schritt dokumentieren |

## Nicht-Ziele

- **Server-Implementierung des QR-Bootstrap** (Ticket-Modell, Profilseite, Bootstrap-Endpunkt, Session-Ausstellung) — eigener Lifecycle im Server-Repo `martin-stromberg/VideoPlayer`; hier nur Schnittstellen-/Koordinationsschritt (Schritt 5a).
- **Behebung des GitHub-Actions-Billing-Problems** — externe Klärung; E2E-CI-Integration nur optional.
- **TV-Richtung des Pairings** (Gerät zeigt Code) — nur Datenmodell-Perspektive, keine Implementierung.
- **Universal Links / App Links** — nur Perspektive (Bonus der https-URL), kein Umsetzungsziel.
- **Änderung des MVU-Flusses** der `ServerSetupPage` über die Modernisierung hinaus; kein Umbau zu anderem Seiten-/Workflow-Typ.
- **Entfernung der Fallback-Flows** (Admin-Code, manuelle Eingabe, E-Mail/Passwort) — bleiben erhalten.
- **Developer-API im Release-Build** — ausdrücklich ausgeschlossen.
