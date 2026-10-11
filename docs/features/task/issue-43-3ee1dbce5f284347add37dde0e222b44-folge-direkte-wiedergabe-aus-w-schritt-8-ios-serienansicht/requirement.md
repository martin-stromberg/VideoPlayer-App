# Übersetzte Anforderung: iOS-Bugfix — Serienansicht: Episodenliste und Staffelauswahl

Entwicklungsschritt 8 des Projekts `issue-43-…-folge-direkte-wiedergabe-aus-w` (siehe `docs/projects/task/…/project-plan.md`, Schritt 8).

## Fachliche Zusammenfassung

Die Serienansicht der Medien-Detailseite (`MediaDetailsPage`, Abschnitt `SeasonSection`) ist auf iOS defekt: Die Episodenliste wird nicht gerendert, die Staffelauswahlbox (`SeasonPicker`) listet die Staffeln nicht aufsteigend („Staffel 01" zuerst), und die Auswahl einer anderen Staffel springt auf „Staffel 01" zurück. Die plausibilisierten Ursachen liegen im gemeinsamen UI-Code: `CollectionView`-in-`ScrollView` (bekanntes iOS-Renderproblem: Höhe 0) sowie instabiles Picker-Update-Verhalten (`ItemsSource`/`SelectedItem` werden bei jedem Model-Tick neu gesetzt; das Schutzflag `updatingSeasonPicker` deckt nur synchrone `SelectedIndexChanged`-Aufrufe ab). Da auf dieser Maschine kein iOS-Lauf möglich ist, sind die Ursachen im gemeinsamen Code zu beheben, das unverändert korrekte Windows-Verhalten nachzuweisen und die iOS-Verifikationsgrenze zu dokumentieren.

## Betroffene Klassen und Komponenten

- `VideoPlayer.Maui/Pages/MediaDetailsPage.xaml` — `SeasonSection` (Staffel-`Picker`, Episoden-`CollectionView` innerhalb `ScrollView` `DetailsScroll`)
- `VideoPlayer.Maui/Pages/MediaDetailsPage.xaml.cs` — `UpdateSeasonSection`, `OnSeasonSelected`, `OnEpisodeSelected`, Flag `updatingSeasonPicker`
- `VideoPlayer.Mvu/MediaDetailsService.cs` — Staffelmapping mit `OrderBy(s.Number)` (offline ~Z. 456–457, online `MapSeasons` ~Z. 606–610; bereits korrekt sortiert, voraussichtlich keine Änderung)
- `VideoPlayer.Mvu/MediaDetailsView.cs` — `SeasonItem`, `EpisodeItem`, `SelectedSeason`, `SelectedSeasonEpisodes`
- `VideoPlayer.Mvu/AppUpdate.cs` — `UpdateSeasonSelected` (Z. ~1889–1932)
- `VideoPlayer.Mvu/DevApi/DevApiModelSnapshot.cs` — ggf. Erweiterung um Serienansicht-Sichtbarkeits-/Auswahlfelder für E2E-Nachweise
- `VideoPlayer.E2E.Tests/Support/MockVideoServer.cs` — ggf. Erweiterung der `api/items/tvshow/{id}`-Antwort um mehrere Staffeln (Serienansicht-E2E)
- `VideoPlayer.E2E.Tests/Tests/` — ggf. neue E2E-Testklasse für die Serienansicht (Windows-Regression)
- `VideoWebPlayer.Maui.Tests/MediaDetailsPageContractTests.cs` — betroffen, falls `EpisodesCollection` ersetzt wird
- `docs/help/` — Dokumentation der iOS-Verifikationsgrenze

## Implementierungsansatz

- Episodenliste: Die `CollectionView` `EpisodesCollection` aus dem `ScrollView`-Kontext lösen. Projektweites Referenzmuster ist `BindableLayout` (vgl. `MediaCarousel` mit `HorizontalStackLayout` + `BindableLayout.ItemsSource` + `TapGestureRecognizer`) — dieselbe Struktur als `VerticalStackLayout` wahrt das Windows-Verhalten und umgeht das iOS-Messproblem einer virtualisierten Liste im `ScrollView`.
- Staffel-Picker: `ItemsSource`/`SelectedItem` nur bei tatsächlicher Änderung setzen (Vergleich der Staffel-Id-Sequenz bzw. der Auswahl), damit der iOS-`Picker` die Anzeige nicht bei jedem Model-Tick neu aufbaut und `SelectedIndexChanged`-Echo-Events entstehen.
- Auswahl-Stabilität: In `OnSeasonSelected` zusätzlich zum `updatingSeasonPicker`-Flag ein Echo-Guard — kein `SeasonSelectedMessage`-Dispatch, wenn die gemeldete Staffel bereits der `SelectedSeason` des Modells entspricht.
- Auswahl-/Identifikationsanforderung: Die Staffelwahl bleibt eine Klartext-Auswahl über den `Picker` (Anzeigename „Staffel {n}"), keine Kennungseingabe.
- Nachweis: Windows-E2E-Test des Serienansicht-Flusses (Weiterschauen-Serieneintrag → Detailseite → Episodenliste sichtbar, Staffel-Reihenfolge aufsteigend, Staffelwechsel bleibt bestehen); Unit-/Contract-Tests für die geänderten Strukturen.

## Konfiguration

Keine.

## Akzeptanzkriterien (aus der Kundenanforderung)

1. Die Serienansicht zeigt auf iOS die Episodenliste an — soweit ohne iOS-Lauf prüfbar: Ursache `CollectionView`-in-`ScrollView` ist behoben.
2. Die Staffelauswahlbox zeigt die Staffeln aufsteigend sortiert („Staffel 01" zuerst).
3. Die Auswahl einer anderen Staffel bleibt bestehen und springt nicht auf „Staffel 01" zurück.
4. Das Verhalten der Serienansicht auf Windows bleibt unverändert korrekt — auf dieser Maschine nachzuweisen.
5. Die iOS-Verifikationsgrenze ist in den Projekt-/Feature-Artefakten dokumentiert.

## Offene Fragen

- Keine fachlich offenen Fragen. Die iOS-Laufzeitverifikation der endgültigen Behebung ist plattformbedingt nicht möglich und wird als dokumentierte Grenze akzeptiert (`scripts/iOS-Deployment.ps1` erfordert einen iOS-Host/macOS).
