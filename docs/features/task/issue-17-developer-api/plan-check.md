# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

Der Plan deckt alle fünf Akzeptanzkriterien des Projektplans (Schritt 2)
sowie die gebundenen Entscheidungen OP-3 und OP-5 ab. Die Aufteilung
„plattformneutraler Kern in `VideoPlayer.Mvu/DevApi/`" vs. „MAUI-gebundene
Verdrahtung in `VideoPlayer.Maui/DevApi/`" löst die Testbarkeitsforderung
(HTTP/Routing/Snapshot als echte Unit-Tests, MAUI-Teile als
Quelltext-Contract-Tests) ohne neue Projektdatei und ohne Änderung an der
nicht committbaren `VideoPlayer.Maui.csproj`.

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis | Status |
|--------------------|-------------------|--------------|--------|
| AC 1: `GET /model` mit CurrentPage/ConnectionState/Flags, keine Secrets | `DevApiModelSnapshot.FromModel` (Schritt 6) + Route in `DevApiService` (Schritt 10); kuratierter Feldsatz gemäß `inventory/dev-api.md` | `DevApiModelSnapshotTests` (Befüllung, camelCase, kein token/password im JSON) | Abgedeckt |
| AC 2: `GET /tree` mit AutomationId/Typ/Text/Bounds/Sichtbarkeit inkl. CollectionView-Items | `DevApiVisualTree.BuildTree` (Schritt 9): `IVisualTreeElement`-Traversierung, `ItemsSource`-Ableitung mit Anzeige-Property-Kette | `DevApiContractTests` (Struktur-Pins) + Live-Verifikation; Logik der Item-Textkette rein quelltextbasiert (MAUI-Abhängigkeit) | Abgedeckt |
| AC 3: `POST /invoke` Clicked-Äquivalent, `POST /set-text` Entry.Text + TextChanged | `TryInvoke`/`TrySetText` (Schritt 9) mit SendClicked/Toggle/Activate/Command-Regeln, alles via `MainThread.InvokeOnMainThreadAsync` | `DevApiContractTests` (SendClicked/InputView/MainThread-Pins) + Live-Verifikation | Abgedeckt |
| AC 4: Bindung 127.0.0.1, Port 0, devapi.json im Programmverzeichnis, kein Release | `DevApiHttpServer` (Loopback, Port 0), `DevApiMetadata.WriteAsync` → `AppContext.BaseDirectory` mit Fallback, `#if DEBUG || DEV_API` um alle Dateien + Verdrahtung | `DevApiHttpServerTests` (Loopback-Start, sequentielle Verbindungen), `DevApiMetadataTests` (JSON-Felder), `DevApiContractTests` (`#if`-Struktur, `IPAddress.Loopback`, keine vier Routen hinaus) | Abgedeckt |
| AC 5: kein `dispatch`-Endpunkt | Router-Map-Liste exakt vier Routen; Contract-Test assertiert `Map(`-Anzahl und Abwesenheit von `dispatch` in `DevApiService.cs` | `DevApiContractTests` | Abgedeckt |

## E2E-/UI-Fluss-Abdeckung

Die Anforderung ist selbst Testinfrastruktur ohne sichtbaren Benutzerfluss.
Der Funktionsnachweis der UI-nahen Endpunkte (`/tree`, `/invoke`,
`/set-text`) ist im Testprojekt nur strukturell möglich (keine
MAUI-Referenz); die verbindliche Verifikation erfolgt per Live-Nachweis
gegen den laufenden Windows-Debug-Build (im Plan als Verifikationsschritt
und als Restnachweis-Dokumentation vorgesehen). Das ist konsistent mit der
Repo-Konvention (Contract-Tests ersetzen fehlende UI-Test-Infrastruktur,
vgl. `PairingContractTests`) und mit der Verifikationsvorgabe des
Projektplans („Debug-Build starten, devapi.json lesen, alle Endpunkte per
HTTP-Client prüfen … andernfalls Code-Ebene + Contract-Tests").

## Hinweise

- `DevApiService.Start()` muss `devapi.json`-Schreibfehler abfangen
  (Fallback-Pfad), damit die App im Debug nicht an einem readonly
  Programmverzeichnis scheitert.
- `dotnet test` baut `VideoPlayer.Mvu` in Debug → die `#if`-umschlossenen
  DevApi-Typen stehen den Tests zur Verfügung; ein Release-Testlauf würde
  sie nicht sehen (dokumentiert: Tests laufen per Konvention in Debug).
- Kleine Ungenauigkeit in `inventory.md` Z. 3x korrigiert: Die
  `CollectionView`-Items leiten sich aus `ItemsSource` ab (ItemTemplate
  bleibt nur Kontext – im Plan als „ItemsSource + Anzeige-Property-Kette"
  präzisiert, entspricht der Inventur-Entscheidung „entscheide pragmatisch").
