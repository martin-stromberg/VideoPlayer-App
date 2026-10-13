# Umsetzungsplan: Sucheingabe der Quellen-Übersichtsseite übersteht Modell-Aktualisierungen

## Übersicht

`SourceOverviewPage.UpdateUi` spiegelt heute bei jedem `ModelChanged`-Tick
den abgeschickten `browser.SearchText` in das Suchfeld und zerstört damit
unbestätigte Eingaben. Die Spiegelung wird so eingeschränkt, dass sie nur
noch bei einer tatsächlichen Änderung des abgeschickten Suchtexts schreibt
(Abschicken, Quellen-Wechsel, Zustands-Reset); die unbestätigte Eingabe im
`MediaSearchBar` bleibt über beliebige Ticks erhalten. Das Suchmuster
(explizites Abschicken, Reload ab Seite 0) bleibt unverändert.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Schutz der unbestätigten Eingabe | Seitenlokaler Merker `mirroredSearchText` in `SourceOverviewPage`: `UpdateUi` schreibt `browser.SearchText` nur ins Feld, wenn sich der **Modellwert** seit der letzten Spiegelung geändert hat | `SearchText` ändert sich nur bei Abschicken oder Kontext-Reset — genau den Fällen, in denen das Feld synchronisiert werden soll/darf. Eine Entwurfs-Eigenschaft im `SourceBrowserState` mit `TextChanged`-Message (Muster `PlaylistFormState.Name`) würde pro Tastenschlag dispatchen; ein Tick-Zurückspiegeln des Entwurfs könnte durch die asynchrone `DispatchAsync`-Latenz frisch getippte Zeichen wieder überschreiben (Echo-Race) — das `PlaylistFormPanel` vermeidet genau deshalb die Spiegelung während des Tippens. Der Merker ist minimal, race-frei und erfüllt „zuletzt abgeschickter Text bleibt bei Rückkehr sichtbar" automatisch (neue Seiten-Instanz spiegelt einmalig). |
| E2E-Erreichbarkeit des Suchfelds | `AutomationId="MediaSearchBar"` am `SearchBar`; Eingabe per DevApi `POST /set-text` (feuert `TextChanged`), Abschicken per FlaUI-Tastatur (Fokus + Return) | DevApi-`TryInvoke` kann `SearchButtonPressed` nicht auslösen; die Return-Taste ist die geforderte Nutzeraktion und wird über das bereits genutzte FlaUI-`Keyboard` gesendet. |
| Beobachtbarkeit des aktiven Filters | `DevApiModelSnapshot.SourceBrowserSearchText` + Gegenstück im E2E-`DevApiModels.cs` | E2E-Tests können den abgeschickten Filter zusätzlich zum Request-Log (`SourceItemRequests`) über den Modell-Snapshot prüfen; Snapshot-Felder sind über `DevApiModelSnapshotTests` unit-testbar. Muster aus Schritt 1 (`SourceBrowserGenreNames` u. a.). |

## Programmabläufe

### Tippen ohne Abschicken (unbestätigte Eingabe)

1. Anwender tippt in `MediaSearchBar` — kein `TextChanged`-Handler
   verdrahtet, kein Dispatch (kein Live-Suchen).
2. Beliebige `ModelChanged`-Ticks (Sync, Fortschritt, Netzwerk,
   `SourceItemsLoadedMessage`, `SourceGenresLoadedMessage`, Genre-Auswahl)
   laufen durch `UpdateUi`.
3. `browser.SearchText` ist unverändert → `mirroredSearchText` entspricht
   dem Modellwert → `MediaSearchBar.Text` wird **nicht** geschrieben; die
   Eingabe bleibt stehen.

Beteiligte Klassen/Komponenten: `SourceOverviewPage.UpdateUi`,
`SourceBrowserState.SearchText`

### Abschicken der Suche

1. Anwender betätigt Return/die Such-Schaltfläche → `OnSearchButtonPressed`
   dispatcht `SourceSearchSubmittedMessage(MediaSearchBar.Text)` (unverändert).
2. `UpdateSourceSearchSubmitted` trimmt; bei unverändertem/inaktivem
   Browser keine Aktion, sonst `ReloadBrowserItems` → Liste zurückgesetzt,
   `LoadSourceItemsEffect` mit `search`-Parameter ab Seite 0 (unverändert).
3. `SearchText` hat sich geändert → `mirroredSearchText`-Guard lässt die
   Spiegelung zu → das Feld zeigt den (getrimmten) abgeschickten Text als
   erkennbaren aktiven Filter.

Beteiligte Klassen/Komponenten: `SourceOverviewPage`, `AppUpdate`,
`ShellAppEffectHandler.LoadSourceItemsAsync`, `VideoPlayerApiClient`

### Kontext-Reset / Rückkehr auf die Seite

1. Quellen-Wechsel (`NewBrowser`), Serverwechsel, Gerätewiderruf,
   Sitzungsablauf oder Abmeldung setzen `SourceBrowser` auf einen Zustand
   mit `SearchText = ""` → Guard erkennt die Änderung → Feld wird geleert.
2. Rückkehr auf die Übersichtsseite (neue Seiten-Instanz) spiegelt beim
   ersten `UpdateUi` den vorhandenen `SearchText` → der aktive Filter ist
   sichtbar.

Beteiligte Klassen/Komponenten: `SourceOverviewPage.UpdateUi`, `AppUpdate`

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `SourceSearchTests` | E2E-Testklasse (`VideoPlayer.E2E.Tests/Tests`) | Szenarien der Sucheingabe: unbestätigte Eingabe übersteht Modell-Ticks, Submit lädt gefiltert ab Seite 0, abgeschickter Text bleibt bei Rückkehr sichtbar |
| `SourceOverviewSearchContractTests` | Testklasse (`VideoWebPlayer.Maui.Tests`) | Quelltext-Contract-Pins des Spiegel-Guards und der `AutomationId` (Muster `PageLifecycleContractTests`) |

## Änderungen an bestehenden Klassen

### `SourceOverviewPage` (ContentPage)

- **Neue Eigenschaften/Felder:** `private string? mirroredSearchText` — der
  zuletzt aus dem Modell ins Feld gespiegelte `SearchText` (`null` = noch
  nie gespiegelt).
- **Geänderte Methoden:** `UpdateUi(AppModel)` — die Feldspiegelung
  (heute Zeilen 96–101) wird von „Feld ≠ Modell" auf „Modellwert seit
  letzter Spiegelung geändert" umgestellt: nur dann Merker aktualisieren
  und `MediaSearchBar.Text` schreiben.
- **Neue Events/Event-Handler:** keine — `TextChanged` bleibt bewusst
  unverdrahtet (kein Live-Suchen).

### `SourceOverviewPage.xaml`

- `AutomationId="MediaSearchBar"` am `SearchBar` ergänzen
  (DevApi-/FlaUI-Erreichbarkeit).

### `DevApiModelSnapshot` (record)

- **Neue Eigenschaften:** `SourceBrowserSearchText` (`string`) — der
  abgeschickte Suchfilter des Quellen-Browsers; Mapping in
  `FromModel` aus `model.SourceBrowser.SearchText`.

### `DevApiSourceBrowserSnapshot`-Gegenstück (`VideoPlayer.E2E.Tests/Support/DevApiModels.cs`)

- **Neue Eigenschaften:** `sourceBrowserSearchText` (`JsonPropertyName`)
  als `string?`-Eigenschaft am E2E-Snapshot-Modell.

### `UiDriver` (`VideoPlayer.E2E.Tests/Support/UiDriver.cs`)

- **Neue Methoden:** `PressEnter(string automationId)` — Element per
  AutomationId suchen, fokussieren und `Keyboard`-Return senden
  (Submit-Geste auf dem Suchfeld).

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine — der Suchtext wird wie bisher serverseitig angewendet; das bestehende
Trimmen in `UpdateSourceSearchSubmitted` bleibt unverändert.

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Feldverhalten:** Das Suchfeld wird nicht mehr bei jedem Tick
  überschrieben — exakt das geforderte Verhalten. Der Filter bleibt nach
  dem Abschicken sichtbar (Anforderung erlaubt das ausdrücklich).
- **TextChanged-Rückspiegelung:** Das programmatische Schreiben des
  abgeschickten Texts nach dem Submit feuert `TextChanged`; da kein
  Handler verdrahtet ist, entsteht kein Dispatch-Rückfluss — kein
  Schleifenrisiko.
- **Verwaister Zustand:** Wird der Browser-Zustand zurückgesetzt, während
  die Seite noch abonniert ist (Serverwechsel, Sitzungsablauf,
  Abmeldung), leert die Guard-Logik das Feld — wie zuvor.
- **E2E-Laufzeit:** Die FlaUI-Tests benötigen eine interaktive Windows-
  Sitzung und den Debug-Build der App; sind sie nicht ausführbar, wird das
  dokumentiert statt ersetzt.

## Umsetzungsreihenfolge

1. **Contract-Test für den Spiegel-Guard (rot) anlegen**
   - Voraussetzungen: `PageLifecycleContractTests`-Muster vorhanden
   - Beschreibung: `SourceOverviewSearchContractTests` in
     `VideoWebPlayer.Maui.Tests` anlegen; pinnt, dass `UpdateUi` den
     Schreibzugriff auf `MediaSearchBar.Text` hinter einem
     `mirroredSearchText`-Änderungsguard führt (Test schlägt auf dem
     Ist-Stand fehl).

2. **Spiegel-Guard in `SourceOverviewPage` implementieren**
   - Voraussetzungen: keine
   - Beschreibung: `mirroredSearchText`-Feld ergänzen, `UpdateUi`-
     Spiegelung umstellen, `AutomationId` am `MediaSearchBar` setzen.

3. **DevApi-Snapshot erweitern + Unit-Test**
   - Voraussetzungen: keine
   - Beschreibung: `SourceBrowserSearchText` in `DevApiModelSnapshot` und
     `DevApiModels.cs` (E2E) ergänzen; Abbildung in
     `DevApiModelSnapshotTests` pinnen.

4. **E2E-Hilfsmethode `PressEnter` in `UiDriver`**
   - Voraussetzungen: FlaUI-Paket bereits referenziert
   - Beschreibung: Fokussieren + Return-Taste via
     `FlaUI.Core.Input.Keyboard` (Muster `GoBack`).

5. **E2E-Testklasse `SourceSearchTests`**
   - Voraussetzungen: Schritte 2–4, `MockVideoServer` mit `search`-
     Unterstützung (vorhanden)
   - Beschreibung: Szenarien nach Abschnitt „E2E-Tests" implementieren.

6. **Unit-Tests ausführen; E2E-Ausführbarkeit prüfen und ausführen oder dokumentieren**
   - Voraussetzungen: alle vorherigen Schritte
   - Beschreibung: `dotnet test VideoWebPlayer.Maui.Tests`; E2E-Lauf
     versuchen (`dotnet test VideoPlayer.E2E.Tests --filter SourceSearch`),
     bei Nicht-Ausführbarkeit in `test-results.md` dokumentieren.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `UpdateUi_MirrorsSearchText_OnlyWhenModelTextChanged` | `SourceOverviewSearchContractTests` | `UpdateUi` schreibt `MediaSearchBar.Text` nur hinter dem `mirroredSearchText`-Guard (Quelltext-Pin) |
| `MediaSearchBar_HasAutomationId` | `SourceOverviewSearchContractTests` | XAML-Pin: E2E-/DevApi-Erreichbarkeit des Suchfelds |
| `OnSearchButtonPressed_DispatchesEnteredText` | `SourceOverviewSearchContractTests` | Pin: Submit sendet `SourceSearchSubmittedMessage` mit dem Feldinhalt |
| `FromModel_MapsSourceBrowserSearchText` | `DevApiModelSnapshotTests` | Snapshot bildet `SourceBrowser.SearchText` auf `SourceBrowserSearchText` ab |
| `PressEnter` | `UiDriver` (Support) | Hilfsmethode: Return-Taste auf fokussiertem Element |

### Betroffene bestehende Tests

Keine — `UpdateSourceSearchSubmitted` und das Ladeverhalten bleiben
unverändert; die bestehenden `SearchSubmitted_*`-Tests decken das Submit
weiterhin ab.

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Text eintippen → Genre-Auswahl löst Modell-Ticks aus → Feldinhalt bleibt, kein `search=`-Request | `SourceSearchTests` `UnsubmittedInput_SurvivesModelTicks` | Unbestätigte Eingabe bleibt über Modell-Aktualisierungen erhalten; kein Live-Suchen | Der Defekt liegt im Zusammenspiel Seite↔Modell-Ticks — nur über die reale UI nachweisbar |
| Pflicht | Return-Taste auf dem Suchfeld → `search=`-Request, gefilterte Trefferliste ab Seite 0, `SourceBrowserSearchText` gesetzt | `SourceSearchTests` `SubmitViaEnter_FiltersFromFirstPage` | Abschicken löst die Suche mit dem eingegebenen Text aus | Die geforderte Nutzeraktion (Return-Taste) ist ein UI-Fluss |
| Soll | Nach Submit: Eintrag öffnen → zurück → Feld zeigt den abgeschickten Text | `SourceSearchTests` `SubmittedText_RemainsVisibleAfterReturn` | Abgeschickter Suchtext bleibt bei Rückkehr sichtbar | Navigation/Rückkehr ist ein UI-Fluss über Seiten-Instanzen hinweg |

Welche bestehenden E2E-Tests müssen angepasst werden? Keine.

## Offene Punkte

Keine — getroffene Annahmen (selbst entschieden, da der Nutzer nicht
erreichbar ist):

- Der Entwurf der unbestätigten Eingabe wird seitenlokal gehalten (kein
  `SearchInput` im `SourceBrowserState`) — siehe Designentscheidung.
- Das Abschicken in E2E erfolgt über die echte Return-Taste (FlaUI-
  `Keyboard`), nicht über einen neuen DevApi-Endpunkt — kein zusätzlicher
  Test-Eingriff in die App nötig.
- Bei Nicht-Ausführbarkeit der FlaUI-Suite werden die E2E-Szenarien als
  implementiert, aber nicht ausgeführt dokumentiert (Infrastruktur-Limit,
  kein Planverzicht).
