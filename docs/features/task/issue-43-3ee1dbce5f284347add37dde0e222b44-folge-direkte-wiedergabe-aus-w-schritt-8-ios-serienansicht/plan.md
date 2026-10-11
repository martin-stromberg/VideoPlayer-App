# Umsetzungsplan: iOS-Bugfix — Serienansicht: Episodenliste und Staffelauswahl

## Übersicht

Die Serienansicht der `MediaDetailsPage` wird für iOS stabilisiert: Die `CollectionView` `EpisodesCollection` wird durch eine `BindableLayout`-Liste nach dem `MediaCarousel`-Muster ersetzt (keine virtualisierte Liste mehr im `ScrollView`), der `SeasonPicker` erhält stabile ItemsSource-/SelectedItem-Updates nur bei tatsächlicher Änderung plus einen Echo-Guard im Auswahl-Handler. Als Windows-Regressionsnachweis wird ein E2E-Test der Serienansicht aufgebaut (Mock-Serie mit drei Staffeln, Developer-API-Snapshot-Felder, ComboBox-Auswahl per FlaUI). Die iOS-Laufzeitverifikation ist auf dieser Maschine nicht möglich und wird als dokumentierte Grenze festgehalten.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Episodenliste | `VerticalStackLayout` + `BindableLayout.ItemsSource`/`ItemTemplate` statt `CollectionView` | Beseitigt das iOS-Messproblem (CollectionView-in-ScrollView → Höhe 0); `MediaCarousel` ist das projekteigene Referenzmuster für Listen in Scroll-Containern; Staffeln haben wenige Episoden — Virtualisierung entbehrlich; optisches Ergebnis identisch |
| Episoden-Auswahl | `TapGestureRecognizer` auf der Eintrags-`Border` (Muster `MediaCarousel.OnItemTapped`, `BindingContext` = `EpisodeItem`) | Ersetzt `SelectionChanged`; identischer Benutzerfluss (Antippen öffnet die Episoden-Detailseite); Platzhalter bleiben nicht auslösbar |
| Staffel-Picker-Stabilität | `ItemsSource`/`SelectedItem` nur bei Änderung setzen (Vergleich der Staffel-Id-Sequenz bzw. der Auswahl) + Echo-Guard in `OnSeasonSelected` (kein Dispatch, wenn gemeldete Staffel = `SelectedSeason` des Modells) | Behebt Reihenfolge- und Rücksprung-Ursache ohne iOS-Spezialcode: kein Picker-Neuaufbau pro Model-Tick, verzögerte Echo-Events können die Auswahl nicht zurücksetzen; Windows-Verhalten bleibt identisch |
| E2E-Nachweis | Neuer `SeriesDetailsTests`-E2E über Weiterschauen-Serieneintrag → Serien-Detailseite; Mock-Serie mit drei Staffeln; `DevApiModelSnapshot` um Serienansicht-Felder erweitert | Der Kundenfluss ist UI-erreichbar — E2E ist der primäre Funktionsnachweis (Windows-Regression); die neuen Snapshot-Felder sind Debug-only (`#if DEBUG \|\| DEV_API`) und folgen dem kuratierten Snapshot-Muster |

## Programmabläufe

### Serienansicht laden (bestehender Ablauf, stabilisiert)

1. `MediaDetailsPage.OnAppearing` → `MediaDetailsRequestedMessage` → `LoadMediaDetailsEffect` → `MediaDetailsService` liefert `MediaDetailsView` mit `Seasons` aufsteigend (`OrderBy(s.Number)`).
2. `ModelChanged` → `UpdateUi` → `UpdateSeasonSection`: `SeasonSection.IsVisible = details.HasSeasons`; `SeasonPicker.ItemsSource` wird nur gesetzt, wenn sich die Staffel-Id-Sequenz geändert hat; `SelectedItem` nur, wenn es von `details.SelectedSeason` abweicht; `EpisodesList` (BindableLayout) erhält `details.SelectedSeasonEpisodes`.
3. Episoden werden als Eintragskarten gerendert (unverändertes `DataTemplate` inkl. `WatchedIndicator`, Platzhalter-Dimmung, Download-/Abspielen-Schaltern); ein Tap auf die Karte löst `OnEpisodeTapped` → `MediaItemSelectedMessage` → Navigation zur Episoden-Detailseite.

Beteiligte Klassen/Komponenten: `MediaDetailsPage`, `MvuHost`, `MediaDetailsService`, `MediaDetailsView`, `SeasonItem`, `EpisodeItem`.

### Staffel wechseln (gehärtet)

1. Benutzer wählt eine Staffel im `SeasonPicker` → `OnSeasonSelected`.
2. Guard: bei `updatingSeasonPicker` (synchroner programmatischer Event) Rückkehr; zusätzlich Rückkehr, wenn `season.Id == mvuHost.Model.MediaDetails.Details?.SelectedSeason?.Id` (Echo-Event eines bereits angewendeten Stands).
3. Sonst: `SeasonSelectedMessage(season.Id)` → `AppUpdate.UpdateSeasonSelected` → `SelectedSeasonId` + erste Episode als `SelectedEpisodeId` → `UpdateUi` zeigt die Staffel-Episoden.
4. Spätere Model-Ticks (z. B. `MediaDetailsHeaderImageLoadedMessage`) verändern `ItemsSource`/`SelectedItem` nicht mehr, solange Staffeln und Auswahl unverändert sind — Echo-Events, die zur Rücksetzung führten, entstehen nicht bzw. werden ignoriert.

Beteiligte Klassen/Komponenten: `MediaDetailsPage`, `SeasonSelectedMessage`, `AppUpdate.UpdateSeasonSelected`.

### Windows-E2E-Nachweis der Serienansicht

1. `MockVideoServer` mit `ContinueWatchingShowId` + `ShowSeasons` (3 Staffeln à 2 Episoden) → Login → Weiterschauen-Karussell-Eintrag per `DevApiClient.InvokeAsync("ContinueWatchingCarousel", …)` → Serien-Detailseite.
2. Prüfung: `EpisodesList` im Elementbaum trägt die Episoden der ersten Staffel; `SeasonPicker`-ComboBox listet „Staffel 1", „Staffel 2", „Staffel 3" aufsteigend; Modell-Snapshot `MediaDetailsSelectedSeasonNumber == 1`.
3. ComboBox-Auswahl „Staffel 2" per FlaUI → Modell `MediaDetailsSelectedSeasonNumber == 2`, `EpisodesList` zeigt Staffel-2-Episoden; nach Wartezeit (Model-Ticks, u. a. Header-Bild-404) bleibt Auswahl und Episodenliste bestehen.

Beteiligte Klassen/Komponenten: `SeriesDetailsTests`, `MockVideoServer` (`ShowSeasons`, `MockShowSeason`, `MockShowEpisode`), `DevApiModelSnapshot`, `DevApiClient`, `UiDriver`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `SeriesDetailsTests` | E2E-Testklasse (`VideoPlayer.E2E.Tests/Tests`) | Serienansicht: Episodenliste sichtbar, Staffeln aufsteigend, Staffelwechsel stabil |
| `MockShowSeason` | Datenmodellklasse (E2E-Support) | Staffel des E2E-Mock (`Id`, `Number`, `Name`, `Episodes`) |
| `MockShowEpisode` | Datenmodellklasse (E2E-Support) | Episode des E2E-Mock (`Id`, `Number`, `Title`) |

## Änderungen an bestehenden Klassen

### `MediaDetailsPage` (XAML, `VideoPlayer.Maui/Pages/MediaDetailsPage.xaml`)

- **Ersetzt:** `<CollectionView x:Name="EpisodesCollection" SelectionMode="Single" SelectionChanged="OnEpisodeSelected">` → `<VerticalStackLayout x:Name="EpisodesList" AutomationId="EpisodesList">` mit `BindableLayout.ItemsSource` (aus Code-Behind) und `BindableLayout.ItemTemplate` (bisheriges `DataTemplate` unverändert, `x:DataType="mvu:EpisodeItem"`).
- **Neu:** `TapGestureRecognizer Tapped="OnEpisodeTapped"` in `Border.GestureRecognizers` des Episoden-Templates.
- **Neu:** `AutomationId="SeasonPicker"` am `SeasonPicker` (E2E-Auffindbarkeit).

### `MediaDetailsPage` (Code-Behind, `VideoPlayer.Maui/Pages/MediaDetailsPage.xaml.cs`)

- **Neue Felder:** `appliedSeasonItems` (`IReadOnlyList<SeasonItem>?`) — zuletzt an den Picker gegebene Staffelliste (Referenzstabilität für `SelectedItem`).
- **Geänderte Methode:** `UpdateSeasonSection` — `ItemsSource` nur bei geänderter Staffel-Id-Sequenz neu setzen (Vergleich `Id`-Sequenz); `SelectedItem` nur bei Abweichung; `EpisodesList` via `BindableLayout.SetItemsSource`; bei `!HasSeasons` den Picker-Cache zurücksetzen.
- **Geänderte Methode:** `OnSeasonSelected` — zusätzlicher Echo-Guard (`season.Id` == `Details.SelectedSeason?.Id` → kein Dispatch).
- **Ersetzt:** `OnEpisodeSelected` → `OnEpisodeTapped(object? sender, TappedEventArgs e)` — `EpisodeItem` aus `(sender as Element)?.BindingContext`; Platzhalter-Guard und `MediaItemSelectedMessage`-Dispatch unverändert (ohne Selection-Rücksetzung, da keine `CollectionView` mehr).

### `DevApiModelSnapshot` (`VideoPlayer.Mvu/DevApi/DevApiModelSnapshot.cs`, `#if DEBUG || DEV_API`)

- **Neue Eigenschaften:** `MediaDetailsSeasonNames` (`IReadOnlyList<string>`), `MediaDetailsSelectedSeasonNumber` (`int`), `MediaDetailsSeasonEpisodeTitles` (`IReadOnlyList<string>`) — Serienansicht-Nachweis für E2E.
- **Geänderte Methode:** `FromModel` — Befüllung aus `model.MediaDetails.Details` (leer/0 ohne Details).

### `DevApiModel` (`VideoPlayer.E2E.Tests/Support/DevApiModels.cs`)

- **Neue Eigenschaften:** Gegenstücke der Snapshot-Felder (`mediaDetailsSeasonNames`, `mediaDetailsSelectedSeasonNumber`, `mediaDetailsSeasonEpisodeTitles`).

### `MockVideoServer` (`VideoPlayer.E2E.Tests/Support/MockVideoServer.cs`)

- **Neue Eigenschaft:** `ShowSeasons` (`List<MockShowSeason>`) — Opt-in-Staffelstruktur der Detailserie.
- **Geänderte Methode:** `HandleTvShowItem` — `ContinueWatchingShowId`-Branch liefert `Seasons`, wenn `ShowSeasons` befüllt ist (Standard leer → bisheriges Verhalten).

### `UiDriver` (`VideoPlayer.E2E.Tests/Support/UiDriver.cs`)

- **Neue Methode:** `SelectComboBoxItem(string automationId, string itemText)` — ComboBox per ExpandCollapse öffnen, `ListItem` per Name wählen (SelectionItem/Invoke-Fallback).
- **Neue Methode:** `GetComboBoxItemNames(string automationId)` — Anzeigenamen der ComboBox-Einträge in Reihenfolge.

### `MediaDetailsPageContractTests` (`VideoWebPlayer.Maui.Tests`)

- **Geänderter Test:** `MediaDetailsPage_HeaderOverlaysContentAndActions` — String `EpisodesCollection` → `EpisodesList`.
- **Neuer Test:** `MediaDetailsPage_SeasonPicker_StableItemsSourceAndEchoGuard` — `UpdateSeasonSection` vergleicht Staffel-Ids vor `ItemsSource`-Neuzuweisung; `OnSeasonSelected` enthält den `SelectedSeason`-Vergleich (MethodBody-Extraktion wie bestehende Tests).
- **Neuer Test:** `MediaDetailsPage_EpisodeList_BindableLayoutInSeasonSection` — `EpisodesList` nutzt `BindableLayout`-Template statt `CollectionView`, Episoden-Tap via `OnEpisodeTapped`.

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine.

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **`MediaDetailsPage` Kollektionsansicht:** `CollectionCollection` bleibt eine `CollectionView` im `ScrollView` — außerhalb der Anforderung (iOS-Befund nur Serienansicht gemeldet); Risiko als bekannte Einschränkung dokumentiert.
- **Episoden-Tap vs. Eintrags-Schalter:** `TapGestureRecognizer` auf der Karten-`Border` neben den inneren Download-/Abspielen-`Button`s — Buttons konsumieren ihre Taps selbst (Muster unverändert zu anderen Karten); Windows-Verhalten wird per E2E implizit mitabgedeckt (Listenaufbau).
- **Picker-Auswahl bei geänderter Staffelliste:** Bei tatsächlich geänderter Id-Sequenz wird `ItemsSource` neu gesetzt — Auswahl bleibt stabil, weil `details.SelectedSeason` aus dem Modell stammt.
- **`DevApiModelSnapshot`-Erweiterung:** Debug-only-Code, kein Release-Einfluss.
- **E2E-Laufzeit:** Die FlaUI-Suite ist langsam; der neue Test fügt App-Start(s) hinzu.

## Umsetzungsreihenfolge

1. **XAML-Umbau Serienansicht**
   - Voraussetzungen: Keine
   - Beschreibung: `EpisodesCollection` durch `EpisodesList` (`VerticalStackLayout` + `BindableLayout`) ersetzen, Template 1:1 übernehmen, `TapGestureRecognizer`, `AutomationId`s.

2. **Code-Behind-Härtung `MediaDetailsPage`**
   - Voraussetzungen: Schritt 1
   - Beschreibung: `appliedSeasonItems`-Cache, Änderungsguards in `UpdateSeasonSection`, Echo-Guard in `OnSeasonSelected`, `OnEpisodeTapped` statt `OnEpisodeSelected`.

3. **DevApi-Snapshot-Felder + E2E-Modell-Gegenstücke**
   - Voraussetzungen: Keine
   - Beschreibung: `DevApiModelSnapshot` um `MediaDetailsSeasonNames`/`MediaDetailsSelectedSeasonNumber`/`MediaDetailsSeasonEpisodeTitles` erweitern; `DevApiModel` in den E2E-Support-Klassen ergänzen.

4. **Mock-Server-Staffelstruktur**
   - Voraussetzungen: Keine
   - Beschreibung: `MockShowSeason`/`MockShowEpisode`, `ShowSeasons`-Opt-in, `HandleTvShowItem` liefert Staffeln im `ContinueWatchingShowId`-Branch.

5. **UiDriver-ComboBox-Hilfen + E2E-Testklasse**
   - Voraussetzungen: Schritte 3 und 4 (Testinfrastruktur), Schritte 1–2 (getestetes Verhalten)
   - Beschreibung: `SelectComboBoxItem`/`GetComboBoxItemNames`, `SeriesDetailsTests` mit den zwei Szenarien (Episodenliste + Reihenfolge; Wechsel + Persistenz).

6. **Contract-Tests anpassen/ergänzen**
   - Voraussetzungen: Schritte 1–2
   - Beschreibung: `EpisodesCollection`-Assertion auf `EpisodesList`, neue Contract-Tests für Picker-Stabilität und BindableLayout-Struktur.

7. **iOS-Verifikationsgrenze dokumentieren**
   - Voraussetzungen: Schritte 1–6
   - Beschreibung: Grenze in `docs/help/` (Serienansicht/Detailansicht-Doku) und Release-Notes-/Feature-Artefakten festhalten.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `ShowDetails_ShowsEpisodesAndAscendingSeasonOrder` | `SeriesDetailsTests` (E2E) | Serien-Detailseite zeigt `EpisodesList` mit den Episoden der ersten Staffel; `SeasonPicker` listet „Staffel 1/2/3" aufsteigend; `MediaDetailsSelectedSeasonNumber == 1` |
| `SelectAnotherSeason_SwitchesEpisodesAndKeepsSelection` | `SeriesDetailsTests` (E2E) | ComboBox-Auswahl „Staffel 2" → Modell-Auswahl + Episodenliste wechseln; Auswahl und Liste bleiben nach Wartezeit bestehen (kein Rücksprung) |
| `SelectComboBoxItem` / `GetComboBoxItemNames` | `UiDriver` | FlaUI-Hilfen für `Picker`-Auswahl und Eintragsreihenfolge |
| `MediaDetailsPage_SeasonPicker_StableItemsSourceAndEchoGuard` | `MediaDetailsPageContractTests` | Änderungsguards + Echo-Guard im Quelltext |
| `MediaDetailsPage_EpisodeList_BindableLayoutInSeasonSection` | `MediaDetailsPageContractTests` | `BindableLayout`-Episodenliste + `OnEpisodeTapped` im XAML |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `MediaDetailsPageContractTests.MediaDetailsPage_HeaderOverlaysContentAndActions` | `EpisodesCollection`-String entfällt → `EpisodesList` |
| `ContinueWatchingPlaybackTests.TapContinueWatchingShowEntry_OpensDetails_NotPlayback` | Regressionstest: `ShowSeasons` bleibt leer → CW-Serie ohne Staffeln unverändert (kein Codewechsel, nur Lauf) |

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Serien-Detailseite öffnen → Episodenliste sichtbar, Staffeln aufsteigend | `SeriesDetailsTests.ShowDetails_ShowsEpisodesAndAscendingSeasonOrder` | AK 1 (Layout-Ursache behoben), AK 2 | Benutzerfluss: Anzeige der Serienansicht über echte UI + Serverantwort |
| Pflicht | Andere Staffel wählen → Auswahl bleibt, Episodenliste wechselt | `SeriesDetailsTests.SelectAnotherSeason_SwitchesEpisodesAndKeepsSelection` | AK 3, AK 4 (Windows-Verhalten korrekt) | Benutzerfluss: Picker-Interaktion + Model-Persistenz gegen Rücksprung |
| Nicht möglich | iOS-Laufverifikation | — | AK 1–3 auf iOS | Plattformgrenze: kein iOS-Host auf dieser Maschine — dokumentierte Verifikationsgrenze |

Welche bestehenden E2E-Tests müssen angepasst werden? Keine — `TapContinueWatchingShowEntry_OpensDetails_NotPlayback` bleibt unverändert gültig (leere `ShowSeasons`).

## Offene Punkte

Keine.
