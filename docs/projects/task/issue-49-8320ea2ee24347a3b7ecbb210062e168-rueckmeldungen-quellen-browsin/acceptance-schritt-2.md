# Abnahmeprüfung – Entwicklungsschritt 2

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

Keine.

## Hinweise

**Prüfumfang:** Vollständiger Diff `task/…-rueckmeldungen-quellen-browsin...HEAD` (13 Dateien, +419/−6) sowie die relevanten Quelldateien im Ist-Stand (`SourceOverviewPage.xaml(.cs)`, `AppUpdate.cs`, `SourceBrowserState.cs`, DevApi-Snapshot, E2E-Support und -Tests).

**Soll-Verhalten im Code nachgewiesen:**

- **Unbestätigte Eingabe übersteht beliebige Modell-Ticks:** `VideoPlayer.Maui/Pages/SourceOverviewPage.xaml.cs:23` führt den seitenlokalen Merker `mirroredSearchText` ein; `UpdateUi` (Zeilen 92–109) schreibt das `MediaSearchBar` nur noch, wenn sich der abgeschickte `SourceBrowserState.SearchText` geändert hat (`!string.Equals(mirroredSearchText, browser.SearchText, …)`). Der alte Guard verglich `MediaSearchBar.Text` mit dem Modellwert — jede Tipp-Abweichung wurde daher beim nächsten Tick mit dem abgeschickten Text überschrieben (Fehlerbild exakt wie in der Anforderung beschrieben). Da `SearchText` im Modell ausschließlich über `SourceSearchSubmittedMessage` (`AppUpdate.cs:1696-1706`), `NewBrowser` (Zeilen 1525-1531, 1541, 1566) oder Kontext-Resets auf `SourceBrowserState.Empty` (Zeilen 559, 896, 1277, 3529) verändert wird, übersteht eine unbestätigte Eingabe sämtliche Synchronisations-, Fortschritts-, Netzwerk- und Lade-Ticks.
- **Merker kann nicht stale werden:** `mirroredSearchText` wird nur innerhalb des Guards und immer zusammen mit dem Feld-Schreibzugriff gesetzt — Invariante „Merker = zuletzt ins Feld gespiegelter Modellwert". Unterdrückung tritt nur bei unverändertem Modell-`SearchText` ein; dann ist eine Abweichung Feld↔Merker ausschließlich durch Nutzereingabe entstanden, die das Soll-Verhalten schützt. Legitime Spiegelfälle feuern zuverlässig: Quellenwechsel (`NewBrowser` → `SearchText=""`), Browser-Reset (Abmeldung/Sitzungsablauf → `Empty`), Abschicken eines abweichenden Textes.
- **Rückkehr auf die Seite zeigt den aktiven Filter:** neue Seiteninstanz → `mirroredSearchText = null` ≠ Modellwert → sofortige Spiegelung im Konstruktor-`UpdateUi` (Zeile 32); gleiche Instanz → Feldinhalt ohnehin erhalten, und `UpdateSourceBrowserOpened` (`AppUpdate.cs:1558-1561`) lässt den Browser-Zustand bei identischer Quelle unangetastet.
- **Abschicken löst Suche ab der ersten Seite aus:** `OnSearchButtonPressed` (Zeilen 124-127) dispatcht `SourceSearchSubmittedMessage(MediaSearchBar.Text)`; `UpdateSourceSearchSubmitted` trimmt und leitet in `ReloadBrowserItems` (`AppUpdate.cs:1654-1676`) → `Items=[]`, `NextPage=0`, `LoadSourceItemsEffect(SourceId, Page=0, SearchText, GenreId, Append:false)` — Trefferliste wird ab Seite 0 gefiltert neu geladen. Bereits abgeschickter identischer Text erzeugt `Unchanged` (Zeile 1700-1703) — unverändertes Altverhalten, fachlich konsistent.
- **Kein Live-Suchen eingeführt:** `MediaSearchBar` hat kein `TextChanged`-Verdrahtung (XAML Zeile 17-20, nur `SearchButtonPressed`); das Submit-Muster bleibt fachlich unverändert.
- **Kosmetische Randbemerkung (keine Abweichung):** im Wiederherstellungspfad `UpdateSourceBrowserOpened` (Seite mit verwaistem Modellzustand geöffnet) spiegelt der Konstruktor-`UpdateUi` kurz den Vorgänger-Suchtext, bis der Reset-Tick das Feld leert — nur transienter Ausnahmepfad, Endzustand korrekt; Eingabe „nur Leerzeichen" wird modellseitig getrimmt und ggf. getrimmt zurückgespiegelt (Feld zeigt den tatsächlichen Filter).

**Testnachweise:**

- `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj` ausgeführt: **1320 Tests bestanden, 0 Fehler, 0 übersprungen** (Dauer 31 s) — der behauptete grüne Stand ist verifiziert.
- `SourceOverviewSearchContractTests` (4 Tests) pinnt quelltextseitig den Merker, die Guard-Reihenfolge inklusive Verbot des alten Feld-Vergleichs (`DoesNotContain("MediaSearchBar.Text, browser.SearchText")`), die `AutomationId`/`SearchButtonPressed`-XAML-Verdrahtung und den Submit-Dispatch; `DevApiModelSnapshotTests.DevApiModelSnapshot_MapsSourceBrowser` pinnt das neue `SourceBrowserSearchText`-Mapping.
- E2E: `VideoPlayer.E2E.Tests/Tests/SourceSearchTests.cs` (3 Tests) fährt die echte App: `UnsubmittedInput_SurvivesModelTicks` (Texteingabe via `set-text`, Modell-Ticks per Genre-Reload, Feldinhalt bleibt, kein `search=`-Request — zugleich Negativnachweis „kein Live-Suchen"), `SubmitViaEnter_FiltersFromFirstPage` (Return-Taste via `UiDriver.PressEnter`, Server-Request mit `search=Komöd` und `page=0`, gefilterte Liste, Feldtext bleibt), `SubmittedText_RemainsVisibleAfterReturn` (Detail-Navigation und `GoBack`, Suchtext weiter sichtbar).
- Doku konsistent ergänzt: `docs/help/quellen-browsing-details.md` (Textsuche-Abschnitt), `README.md`, `docs/RELEASE_NOTES.md`, `changes.log`.

**Einschränkungen:**

- Die vier Contract-Tests sind reine Quelltext-Pins — sie sichern die Verdrahtung, würden aber bei legitimen Refactorings (z. B. Umbenennung des Merkers) brechen und beweisen allein kein Laufzeitverhalten. Das ist hier vertretbar, weil die MAUI-Seite im Unit-Projekt nicht instanziierbar ist (dokumentiertes Muster `PageLifecycleContractTests`) und das Verhalten zusätzlich durch drei E2E-Tests an der realen App belegt ist.
- Die drei E2E-Tests wurden nicht selbst ausgeführt (erfordern Build und Start der Windows-MAUI-App mit FlaUI/DevApi); der behauptete Stand (3/3 grün; Gesamtsuite 61 Tests mit 1 fachfremdem Flake in `ServerSetupTests`) ist plausibel — alle referenzierten Hilfsmittel existieren und sind stimmig verdrahtet (`DevApiClient.SetTextAsync` → `POST /set-text`, `InvokeAsync` mit `itemIndex`, `PressEnter` neu in `UiDriver.cs:432-437`, `SourceItemRequests`-Aufzeichnung im `MockVideoServer`, `SourceBrowserSearchText` im Snapshot `DevApiModelSnapshot.cs:234,346`).
- Im Build auftretende Warnungen (CS0108/CS8618 in `VideoWebPlayer.Client`) sind Altbestand und stammen nicht aus diesem Diff.
