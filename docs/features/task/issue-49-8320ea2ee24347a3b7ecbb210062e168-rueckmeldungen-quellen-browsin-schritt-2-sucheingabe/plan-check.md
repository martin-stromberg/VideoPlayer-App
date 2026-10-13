# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Unbestätigte Eingabe bleibt über beliebige Modell-Aktualisierungen erhalten (kein Überschreiben durch Modell-Suchtext) | `mirroredSearchText`-Guard in `SourceOverviewPage.UpdateUi`: Feld wird nur bei Änderung des abgeschickten `SearchText` geschrieben (Programmablauf „Tippen ohne Abschicken", Umsetzungsschritt 2) | `SourceOverviewSearchContractTests.UpdateUi_MirrorsSearchText_OnlyWhenModelTextChanged` (Quelltext-Pin), E2E `UnsubmittedInput_SurvivesModelTicks` | Abgedeckt |
| Abschicken über Such-/Return-Taste löst Suche mit eingegebenem Text aus, Trefferliste ab erster Seite | Bestehender Pfad bleibt: `OnSearchButtonPressed` → `SourceSearchSubmittedMessage` → `ReloadBrowserItems` ab Seite 0 (Programmablauf „Abschicken der Suche", unverändert) | Bestehende Unit-Tests `SearchSubmitted_ReloadsFromFirstPage_WithSearchText`/`SearchSubmitted_SameText_NoReload`, E2E `SubmitViaEnter_FiltersFromFirstPage` | Abgedeckt |
| Zuletzt abgeschickter Suchtext bleibt im Feld sichtbar (z. B. bei Rückkehr) | Neue Seiten-Instanz spiegelt `SearchText` einmalig beim ersten `UpdateUi` (`mirroredSearchText` = `null` → Änderung erkannt); Programmablauf „Kontext-Reset / Rückkehr" | E2E `SubmittedText_RemainsVisibleAfterReturn` | Abgedeckt |
| Kein Live-Suchen pro Tastenschlag (Suchmuster fachlich unverändert) | Kein `TextChanged`-Handler, keine Eingabe-Message; `MediaSearchBar` bleibt ein reines Eingabe-Widget bis zum Submit | E2E `UnsubmittedInput_SurvivesModelTicks` prüft „kein `search=`-Request vor dem Abschicken" | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Tippen + Modell-Ticks → Eingabe bleibt | `SourceSearchTests.UnsubmittedInput_SurvivesModelTicks` | Abgedeckt (Pflicht) |
| Return-Taste → Suche gefiltert ab Seite 0 | `SourceSearchTests.SubmitViaEnter_FiltersFromFirstPage` | Abgedeckt (Pflicht) |
| Rückkehr auf die Seite → abgeschickter Text sichtbar | `SourceSearchTests.SubmittedText_RemainsVisibleAfterReturn` | Abgedeckt (Soll) |

## Hinweise

- Kernlogik liegt in MAUI-UI-Code (nicht unit-instanzierbar); die
  Quelltext-Contract-Pins folgen dem etablierten
  `PageLifecycleContractTests`-Muster. Die Snapshot-Erweiterung
  (`SourceBrowserSearchText`) ist regulär unit-testbar.
- FlaUI-Enter auf dem MAUI-`SearchBar` (AutoSuggestBox) ist der einzige
  technisch neue Baustein; Restrisiko ist in „Seiteneffekte und Risiken"
  benannt und wird in Schritt 10 verifiziert bzw. dokumentiert.
