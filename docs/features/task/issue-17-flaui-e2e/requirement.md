# Übersetzte Anforderung: E2E-Tests mit FlaUI gegen die echte VideoPlayer.Maui.exe

Branch: `task/issue-17-flaui-e2e`
Quelle: Projektplan Issue #17, Entwicklungsschritt 3
(`docs/projects/project/issue-17-testbarkeit-und-onboarding/project-plan.md`),
Requirement GitHub Issue #17.

## Fachliche Zusammenfassung

Für die Windows-App `VideoPlayer.Maui` soll ein automatisiertes
End-to-End-Testprojekt entstehen, das die echte, unpackagede
`VideoPlayer.Maui.exe` (Debug-Build) selbst baut, startet und gegen die
laufende App testet. Als Testrahmen ist FlaUI (UIA3) mit xUnit festgelegt.

Die Treiberstrategie ist im Projektplan entschieden (OP-1, Kombination):
FlaUI übernimmt Prozessstart, Fenster-/Seitenpräsenz und echte
Klicks/Eingaben; die Developer-API aus Schritt 2 (`GET /model`,
`GET /tree`, `POST /invoke`, `POST /set-text`) dient für
Zustands-Assertions und die CollectionView-/Listen-Interaktion — dort
ist Windows-UIA nachweislich lückenhaft. Ein `dispatch`-Endpunkt existiert
bewusst nicht: Szenarien werden ausschließlich über die echte
UI-Eingabekette hergestellt.

Test-Isolation läuft über das vorhandene Profil-Feature
(`VIDEOPLAYER_PROFILE` → `AppProfile.FromEnvironment`): jeder Testlauf
bekommt ein eigenes Profil mit eigener `known_servers`-Ablage und
eigenen Secrets. Lokal ist die Ausführung verbindlich; CI ist wegen
Billing-Blocker und fehlendem interaktivem Desktop auf GitHub-Runnern
optional zu dokumentieren.

## Akzeptanzkriterien (aus dem Projektplan)

1. Das E2E-Projekt (`VideoPlayer.E2E.Tests` o. ä., in der Solution) baut
   die App als Debug-Build und startet sie selbstständig; Exe-Pfad inkl.
   RID-Ordner ist lokal verifiziert.
2. Szenarien abgedeckt:
   - **Server-Setup:** Erststart → Adresse eingeben → Verbinden →
     `connectionState`-/Fehler-Assertion (inkl. Fehlerszenario
     „Server nicht erreichbar" ohne Backend).
   - **Pairing-Flow:** Pairing-Code eingeben → Koppeln →
     Erfolgs-/Fehler-Mapping.
   - **Serverwechsel:** Einstellungen → „Server wechseln" → bekannten
     Server wählen → Rückkehr zur Startseite (Regression für den
     #15-Navigationsbug).
   - **Anmeldung:** Zugangsdaten eingeben → Startseite.
3. `dotnet test` läuft lokal; Test-Isolation via `VIDEOPLAYER_PROFILE`;
   CI-Integration vorbereitet oder als optional dokumentiert.
4. Keine Koordinatenklicks für CollectionView-Items — Listen-Interaktion
   läuft über die Dev-API (`POST /invoke` auf Item-Knoten bzw.
   `GET /tree`-Adressierung).

## Festgelegte Entscheidungen (Projektplan — nicht neu verhandeln)

- Treiberstrategie: Kombination FlaUI + Dev-API (siehe oben).
- Die getestete App läuft als Debug-Build (Dev-API ist nur
  `#if DEBUG || DEV_API` aktiv).
- Isolation über `VIDEOPLAYER_PROFILE` je Testlauf.
- Lokal verbindlich, CI optional (Dokumentationspflicht).
- Keine Koordinatenklicks für CollectionView-Items.

## Backend-Frage (im Schritt zu entscheiden)

Pairing-, Login- und Serverwechsel-Szenarien brauchen einen Server.
Option A: lokaler Server-Checkout
(`D:\Repositories\softwareschmiede\487c3041-d72b-486e-811f-9ed8db04630b`,
echter `VideoWebPlayer`, `localhost:83`, SQLite, Seed nötig).
Option B: minimaler Mock-Server im Testprojekt (`api/health`,
`api/pairing/exchange` mit ECDH/AES-GCM, `api/auth/login`, leere
Listen-Endpunkte). Hybrid ist legitim: Mock für Hermetik +
dokumentierter echter Server-Test.

## Betroffene Bereiche

- Neues Projekt `VideoPlayer.E2E.Tests` in `VideoPlayer.App.sln`.
- Test-Infrastruktur: App-Launcher (Build, Prozessstart/-stopp,
  `devapi.json`-Erkennung, Profil-Umgebung), Dev-API-Client,
  Mock-Backend, FlaUI-Hilfen (Retry/Waits).
- Ggf. kleine Erweiterung der Dev-API für Item-Auswahl in
  CollectionViews (kein `dispatch`, bleibt auf der UI-Eingabekette).
- Dokumentation: `docs/help/`, README, Release Notes; CI optional.
