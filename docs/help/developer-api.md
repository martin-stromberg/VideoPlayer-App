# Developer-API für UI-Automatisierung

Die App `VideoPlayer.Maui` enthält im **Debug-Build** (oder mit explizitem
Opt-in über das Define `DEV_API`, niemals im Release) eine lokale
Developer-API: einen minimalen HTTP-Server, der strukturierte
UI-Analyse und -Steuerung für automatisierte Tests ermöglicht
(Projekt Issue #17, Schritt 2). Die Implementierung liegt in den
wiederverwendbaren Bibliotheken `msTools.DeveloperApi` und
`msTools.DeveloperApi.Maui` (Issue #18) — siehe unten
[Wiederverwendung](#wiederverwendung-in-anderen-projekten).

## Aktivierung und Ablage

- **Nur Debug/Opt-in:** Sämtlicher Code steht unter
  `#if DEBUG || DEV_API` – der Release-Build enthält die API nicht.
  Opt-in für andere Konfigurationen über das zusätzliche Define
  `DEV_API` beim Build.
- **Bindung:** ausschließlich `127.0.0.1` (Loopback), der Port wird vom
  Betriebssystem vergeben (`TcpListener(IPAddress.Loopback, 0)`).
- **Metadaten:** Beim Start schreibt die App `devapi.json` ins
  Programmverzeichnis (`AppContext.BaseDirectory`, bei der unpackageden
  Windows-App neben der Exe); bei nicht schreibbarem Ort als Fallback ins
  App-Datenverzeichnis. Inhalt:

  ```json
  {"port":50146,"pid":10960,"startedUtc":"2026-09-24T07:48:10.4931232Z"}
  ```

- **Lebenszyklus:** Der Server startet mit dem Hauptfenster
  (`App.CreateWindow`) und stoppt beim Schließen (`Window.Destroying`).
  Jede Verbindung beantwortet genau eine Anfrage (`Connection: close`);
  aufeinanderfolgende Verbindungen werden sequentiell bedient.

## Endpunkte

### `GET /model`

Kuratierter JSON-Snapshot des unveränderlichen `AppModel`
(`MvuHost.Model`): `currentPage`, `connectionState`, `serverHealth`/
`serverHealthText`, Start-/Login-/Sync-Flags, `activeServerAddress`,
`knownServers`/`discoveredServers`, Discovery-/Check-Flags,
`serverSetupError`, Pairing-Flags/`pairingError`,
Bootstrap-Flags/`bootstrapError`, `storageNotice`,
`heroTitle`, Zähler der Startseiten-Listen sowie der
Wiedergabe-Zustand (`playerIsActive`, `playerIsPlaying`,
`playerPositionSeconds`, `playerError`). Tokens und Passwörter
stehen per Konstruktion nicht im Modell.

### `GET /tree`

Logischer Elementbaum der aktuellen Seite über `IVisualTreeElement`:
je Knoten `automationId`, `type`, `isVisible`, `bounds`
(`x`/`y`/`width`/`height`), `text` (Label/Button/InputView/MenuItem) und
`placeholder` (Eingabefelder). Einträge einer `CollectionView` oder
eines `BindableLayout`-Containers im Teilbaum (z. B. die Karussells
der Startseite) werden aus der `ItemsSource` abgeleitet (`items` mit
`index`, `type`, `text`, `detail`) – das schließt die UIA-Lücke,
unter der Windows-Automation keine Listeneinträge beider Formen sieht.

### `POST /invoke`

```json
{"automationId":"ConnectButton"}
{"automationId":"KnownServersView","itemIndex":0}
{"automationId":"ContinueWatchingCarousel","itemIndex":0,"gesture":"longpress"}
```

Findet das Element per AutomationId und löst auf dem UI-Thread das
Clicked-Äquivalent aus: `SendClicked()` auf `Button`/`ImageButton`,
Toggle auf `Switch`/`CheckBox`, `Activate()` auf `MenuItem`, sonst
`Command`-Ausführung (inkl. Tap-Gesten mit Command).

Mit optionalem `itemIndex` wird stattdessen ein Listeneintrag
ausgewählt: bei einer `CollectionView` über `SelectedItem` (echtes
`SelectionChanged`-Event mit dem regulären App-Nachrichtenfluss),
bei einem Element mit `BindableLayout`-Container im Teilbaum (z. B.
`MediaCarousel`) über den `TapGestureRecognizer` des indexierten
Eintrags — der interne `SendTapped(View, Func<IElement?, Point?>?)`-Aufruf
wird per Reflection ausgelöst, exakt der Aufruf, den der
Plattform-Gesture-Manager bei einem Fingertipp macht (`Tapped` feuert
mit `sender` = Eintrags-View). Beide Pfade sind der koordinatenfreie
Listenpfad für Einträge, die Windows-UIA nicht zuverlässig adressiert.

Das optionale Feld `gesture` wählt bei `itemIndex` die Geste der
Item-Auswahl: `"tap"` (Standard) wie beschrieben, `"longpress"` ruft
stattdessen die Methode `SendItemLongPressed(int)` des Zielelements
per Reflection auf — die App-seitige Konvention, mit der ein Element
seine Long-Press-Ereigniskette selbst auslöst (dieselbe Kette wie das
tatsächliche 2-s-Halten, z. B. das Kontextmenü der
Weiterschauen-Liste). Unbekannte Werte werden mit `400` und einem
deutschen Fehlertext abgelehnt; Elemente ohne
`SendItemLongPressed`-Konvention ebenso.

Antworten: `200 {"invoked":true,…}`, `404` Element nicht gefunden,
`400` Element nicht auslösbar, `itemIndex` auf Element ohne
CollectionView/BindableLayout-Container oder außerhalb der Liste bzw.
Body ungültig.

### `POST /set-text`

```json
{"automationId":"AddressEntry","text":"http://server/videoplayer/"}
```

Setzt `Text` eines `InputView` (Entry/Editor/SearchBar) auf dem
UI-Thread – der Setter feuert `TextChanged` wie eine echte Eingabe.
Antworten: `200 {"set":true,…}`, `404`, `400` kein Eingabefeld.

### Statuscodes

| Code | Bedeutung |
|------|-----------|
| 200 | Erfolg |
| 400 | Ungültiger Body / Element nicht auslösbar / keine Texteingabe / kaputte Anfrage |
| 404 | Endpunkt oder AutomationId unbekannt |
| 405 | Methode für den Pfad nicht erlaubt (z. B. `POST /model`) |
| 500 | Interner Fehler (z. B. kein aktives Fenster) |

Fehlerantworten tragen `{"error":"<deutscher Text>"}`.

## Hinweise

- Es existiert **kein** `POST /dispatch` – bewusste Entscheidung: Die
  API bleibt auf der echten UI-Eingabekette, Automatisierung wirkt wie
  echte Bedienung.
- Die `BindableLayout`-Item-Auswahl hängt am internen
  `TapGestureRecognizer.SendTapped(View, Func<IElement?, Point?>?)`:
  die Signatur wird einmalig per Reflection aufgelöst und gepinnt —
  ändert ein MAUI-Upgrade den Member, meldet `/invoke` einen klaren
  Fehlertext (HTTP 400) statt still nichts auszulösen.
- Implementierung: wiederverwendbare Bibliotheken `msTools.DeveloperApi`
  (plattformneutraler Kern: Parser, Router, Server, Metadaten,
  Server-Fassade `DevApiServer`, `GET /model`-Provider) und
  `msTools.DeveloperApi.Maui` (Visual Tree, `MauiDevApiService` mit den
  drei UI-Endpunkten). App-gebunden bleiben der `AppModel`-Snapshot
  (`VideoPlayer.Mvu/DevApi/DevApiModelSnapshot.cs`) und der dünne
  Adapter `VideoPlayer.Maui/DevApi/DevApiService.cs`.
- Primärer Einsatzzweck sind die FlaUI-E2E-Tests (Projekt Schritt 3):
  FlaUI für Fenster/Klicks, Dev-API für Zustands-Assertions und
  Listen-Interaktion — siehe `docs/help/e2e-tests.md`.

## Wiederverwendung in anderen Projekten

Die Developer-API liegt seit Issue #18 in zwei Bibliotheken im selben
Repository; der Referenzierungsweg ist die **Projekt-Referenz** (es
existiert kein NuGet-Feed — eine spätere Paket-Veröffentlichung bleibt
vorbereitet, `IsPackable` ist nicht deaktiviert):

| Projekt | Inhalt | Abhängigkeiten |
|---------|--------|----------------|
| `msTools.DeveloperApi` | `DevApiHttpServer`, `DevApiRequestReader`, `DevApiRouter` (+ Delegat `DevApiEndpointHandler`), `DevApiRequest`/`DevApiResponse`, `DevApiRequestException`, `DevApiMetadata`, `DevApiServer`, `DevApiRouterExtensions.MapModel` | keine — net10.0 |
| `msTools.DeveloperApi.Maui` | `DevApiVisualTree`, `DevApiTreeNode`/`DevApiBounds`/`DevApiTreeItem`, `MauiDevApiService` | `msTools.DeveloperApi`, `Microsoft.Maui.*` (Pin `10.0.90`, gleicher Stand wie die App) |

### Verwendung (MAUI-App)

1. `ProjectReference` auf `msTools.DeveloperApi.Maui` (zieht den Kern
   transitiv mit).
2. `MauiDevApiService` mit einem Snapshot-Provider erzeugen und starten —
   er registriert selbst alle vier Endpunkte:

   ```csharp
   var devApi = new MauiDevApiService(() => MyModelSnapshot.FromModel(host.Model));
   devApi.Start();          // devapi.json → AppContext.BaseDirectory
   window.Destroying += (_, _) => devApi.Dispose();
   ```

   Der Provider läuft je Anfrage auf dem Request-Thread (thread-sicher
   lesbares Modell nötig) und darf keine Zugangsdaten oder Geheimnisse
   zurückgeben.

### Verwendung (Nicht-MAUI-Projekt)

Nur `msTools.DeveloperApi` referenzieren: `DevApiRouter` mit eigenen
Handlern füllen (`Map("GET", "/pfad", handler)` bzw. `MapModel(provider)`
für `GET /model`) und an `DevApiServer` übergeben — das ist die
Handler-Schnittstelle, für die der Kern ohne MAUI schlank bleibt.

### Aktivierung

- Sämtliche Dateien beider Bibliotheken stehen unter
  `#if DEBUG || DEV_API` — im Release-Build sind die Assemblies leer.
  Jeder Verweis außerhalb eines `#if`-Blocks erzeugt damit einen
  Compile-Fehler statt eines stillen Laufzeitpfads.
- Das `DEV_API`-Opt-in muss **für App und Bibliotheken gemeinsam**
  gesetzt werden — beim Build propagiert `-p:DefineConstants=DEV_API`
  in alle Projekte; ein Define nur in der App-csproj ließe die
  Bibliotheken leer.
