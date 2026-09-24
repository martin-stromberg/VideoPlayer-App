# Übersetzte Anforderung: Developer-API für UI-Automatisierung

Branch: `task/issue-17-developer-api`
Projektkontext: `docs/projects/project/issue-17-testbarkeit-und-onboarding/` – Schritt 2 des Projektplans (Enabler für Schritt 3, FlaUI-E2E-Tests).

## Fachliche Zusammenfassung

Die Windows-MAUI-App (`VideoPlayer.Maui`) erhält eine lokale Developer-API:
einen minimalen HTTP-Server, der ausschließlich an `127.0.0.1` bindet und vom
Betriebssystem einen freien Port zugewiesen bekommt (`TcpListener(IPAddress.Loopback, 0)`,
kein `HttpListener`, kein Kestrel, keine Paketabhängigkeit – Entscheidung OP-3
des Projektplans). Der vergebene Port wird zusammen mit Prozess-ID und
Startzeitpunkt in `devapi.json` im Programmverzeichnis (`AppContext.BaseDirectory`)
abgelegt; auf anderen Plattformen Fallback `FileSystem.AppDataDirectory` oder
Verzicht (Windows-first).

Die API umfasst **genau vier Endpunkte** (Entscheidung OP-5: kein
`POST /dispatch` – die API bleibt auf der echten UI-Eingabekette):

- `GET /model` – JSON-Snapshot des unveränderlichen `AppModel` aus `MvuHost.Model`
  (sicherer Auszug; Tokens/Passwörter stehen per Konstruktion nicht im Modell).
- `GET /tree` – logischer Elementbaum ab `Window.Page`/`AppShell` über
  `IVisualTreeElement` mit `AutomationId`, Typname, `IsVisible`, Bounds und
  Text (`Text`/`Placeholder`); **CollectionView-Items werden aus
  `ItemsSource` + ItemTemplate-Datenkontext abgeleitet** (schließt die
  belegte UIA-Lücke unter Windows).
- `POST /invoke` – `{"automationId":"…"}` findet das Element im Visual Tree
  und löst das Clicked-Äquivalent aus (`SendClicked()` auf `Button`, Toggle
  auf `Switch`, Command-Ausführung wo vorhanden) auf dem UI-Thread.
- `POST /set-text` – `{"automationId":"…","text":"…"}` setzt `Entry.Text`
  bzw. `InputView.Text` inkl. `TextChanged` auf dem UI-Thread.

Aktivierung erfolgt **compile-time**: `#if DEBUG || DEV_API` – der
Release-Build enthält die API nicht; Opt-in für Nicht-Debug-Builds über die
eigene Konstante `DEV_API` (zusätzliches Define beim Build). Der
Compile-Time-Ausschluss wird per Contract-Test auf die `#if`-Struktur gepinnt.

Alle UI-Zugriffe laufen über `MainThread.InvokeOnMainThreadAsync`
(Muster `ShellAppEffectHandler` Z. 95ff.). Der Mini-HTTP-Server versteht
GET/POST mit JSON-Bodies, liefert sinnvolle Statuscodes (200, 400
Bad Request, 404 Element nicht gefunden, 405 Method Not Allowed), verarbeitet
mehrere sequentielle Verbindungen und wird sauber mit dem App-Lifecycle
gestartet und gestoppt.

## Akzeptanzkriterien (Projektplan Schritt 2)

1. `GET /model` liefert mindestens `CurrentPage`, `ConnectionState`, Flags
   (`IsPairingRequired`, `IsServerCheckRunning`, `LoginError` o. ä.) – keine
   Tokens/Passwörter (per Konstruktion nur `AppModel`-Felder).
2. `GET /tree` liefert den logischen Elementbaum mit `AutomationId`, Typ,
   Text, Bounds, Sichtbarkeit – **inklusive CollectionView-Items** (Ableitung
   aus `ItemsSource` + ItemTemplate).
3. `POST /invoke` löst das Clicked-Äquivalent eines Elements per
   `AutomationId` aus; `POST /set-text` setzt `Entry.Text` inkl. `TextChanged`.
4. Server bindet ausschließlich `127.0.0.1`, Port vom System vergeben, Port in
   Metadatendatei im Programmverzeichnis; API im Release-Build nicht vorhanden
   (Compile-Time-Ausschluss, gepinnt per Contract-Test auf `#if`-Struktur).
5. **Kein** `dispatch`-Endpunkt; die API umfasst nur `GET /model`,
   `GET /tree`, `POST /invoke`, `POST /set-text`.

## Betroffene Klassen und Komponenten

- **`VideoPlayer.Mvu` (neu, `DevApi/` – plattformneutral, im Testprojekt
  `VideoWebPlayer.Maui.Tests` referenzierbar und testbar):**
  - `DevApiHttpServer`: `TcpListener`-basierter Mini-HTTP-Server (Loopback,
    Port 0, Accept-Loop, eine Anfrage je Verbindung mit `Connection: close`).
  - `DevApiRequestReader`/`DevApiRequest`/`DevApiResponse`: HTTP/1.1-Parsing
    (Request-Line, Header, `Content-Length`-Body) und Antworterzeugung mit
    Statuscodes.
  - `DevApiRouter`: Routen-Tabelle (Methode + Pfad → Handler), liefert
    404/405 nach festen Regeln.
  - `DevApiModelSnapshot`: kuratierter, JSON-serialisierbarer `AppModel`-Auszug
    (sicherer Feldsatz laut Projekt-Inventur).
  - `DevApiMetadata`: Datensatz für `devapi.json` (`port`, `pid`,
    `startedUtc`).
  - Sämtliche Dateien vollständig in `#if DEBUG || DEV_API` eingeschlossen.
- **`VideoPlayer.Maui` (neu, `DevApi/` – MAUI-gebunden):**
  - `DevApiService`: startet/stoppt den Server, verdrahtet die vier
    Endpunkte (Router-Handler auf `MvuHost.Model`, Visual-Tree-Aufbau,
    Element-Invoke/Textsetzen via `MainThread`), schreibt `devapi.json`.
  - `DevApiVisualTree`: Traversierung via `IVisualTreeElement`,
    Knoten-Extraktion (AutomationId/Typ/Sichtbarkeit/Bounds/Text),
    CollectionView-Item-Ableitung, AutomationId-Lookup, Clicked-Äquivalente
    und Textsetzen.
  - `MauiProgram.cs` (erweitern): DI-Registrierung des `DevApiService` unter
    `#if DEBUG || DEV_API`.
  - `App.xaml.cs` (erweitern): Start in `CreateWindow` und Stop beim
    `Window.Destroying`, jeweils unter `#if DEBUG || DEV_API`.
- **`VideoWebPlayer.Maui.Tests` (erweitern):**
  - Einzeltests des HTTP-Parsers/-Servers/Routers und des Modell-Snapshots
    (plattformneutral, ohne MAUI).
  - Contract-Tests auf die `#if`-Struktur (Release-Ausschluss), die genau
    vier Routen (kein `dispatch`), die `MainThread`-Marshalla- und die
    `devapi.json`-Ablage.

## Verbindliche Entscheidungen (aus dem Projektplan, nicht neu verhandelt)

- `TcpListener(IPAddress.Loopback, 0)` + eigener Mini-HTTP-Parser (OP-3).
- Kein `POST /dispatch` (OP-5).
- Compile-Time-Aktivierung `#if DEBUG || DEV_API`; niemals Release.
- Metadaten `devapi.json` in `AppContext.BaseDirectory`; andere Plattformen:
  Fallback `FileSystem.AppDataDirectory` oder Verzicht.
- Bindung ausschließlich `127.0.0.1`.

## Randbedingungen

- `VideoPlayer.Maui/VideoPlayer.Maui.csproj` und `.ios-deploy.user.json`
  werden nicht committed (lokale Änderungen des Anwenders).
- Keine Secrets; deutsche Kommentare/Fehlertexte im vorhandenen Stil
  (`/// <summary>` auf Deutsch, sparsam).
- Live-Verifikation über Windows-Debug-Build, wenn die Umgebung das zulässt;
  andernfalls dokumentierter Restnachweis.
