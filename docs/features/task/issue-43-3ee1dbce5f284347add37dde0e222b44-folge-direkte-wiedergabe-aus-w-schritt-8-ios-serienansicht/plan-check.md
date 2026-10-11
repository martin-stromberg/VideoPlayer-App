# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| AK 1: iOS zeigt die Episodenliste (Ursache CollectionView-in-ScrollView behoben) | Schritt 1: `CollectionView` → `BindableLayout`-Liste nach `MediaCarousel`-Muster; keine virtualisierte Liste mehr im `ScrollView` | E2E `ShowDetails_ShowsEpisodesAndAscendingSeasonOrder` (Windows-Anzeige), Contract-Test `MediaDetailsPage_EpisodeList_BindableLayoutInSeasonSection`; iOS-Lauf = dokumentierte Grenze | Abgedeckt |
| AK 2: Staffeln aufsteigend („Staffel 01" zuerst) | Schritt 2: stabile `ItemsSource`-Zuweisung nur bei geänderter Id-Sequenz — der Picker baut die Anzeige nicht mehr pro Tick neu auf; Modell-Sortierung (`OrderBy(s.Number)`) bleibt Maßstab | E2E `…AscendingSeasonOrder` prüft ComboBox-Eintragsreihenfolge + Modell-Felder | Abgedeckt |
| AK 3: Auswahl bleibt bestehen (kein Rücksprung auf „Staffel 01") | Schritt 2: `SelectedItem`-Änderungsguard + Echo-Guard in `OnSeasonSelected` | E2E `SelectAnotherSeason_SwitchesEpisodesAndKeepsSelection` (Wechsel + Persistenz über Model-Ticks) | Abgedeckt |
| AK 4: Windows-Verhalten unverändert korrekt — auf dieser Maschine nachweisbar | Windows-E2E über Weiterschauen-Serieneintrag → Serien-Detailseite; bestehende Unit-/Contract-Tests bleiben grün | E2E `SeriesDetailsTests` (beide Szenarien) + `TapContinueWatchingShowEntry_OpensDetails_NotPlayback` als Regression + voller Unit-Lauf | Abgedeckt |
| AK 5: iOS-Verifikationsgrenze dokumentiert | Schritt 7: Dokumentation der Grenze in `docs/help/` + Release Notes/Feature-Artefakte | Dokumentations-Artefakt (manuell prüfbar) | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Serien-Detailseite: Episodenliste sichtbar + Staffelreihenfolge (AK 1, 2) | `SeriesDetailsTests.ShowDetails_ShowsEpisodesAndAscendingSeasonOrder` | Abgedeckt |
| Staffelwechsel: Auswahl + Episodenliste wechseln und bleiben (AK 3, 4) | `SeriesDetailsTests.SelectAnotherSeason_SwitchesEpisodesAndKeepsSelection` | Abgedeckt |
| iOS-Verifikation (AK 1–3 auf iOS) | Nicht möglich — kein iOS-Host auf dieser Maschine; in den Artefakten als Grenze dokumentiert | Nicht erforderlich mit Begründung (Plattformgrenze, siehe Anforderung) |

## Hinweise

- Die Testinfrastruktur-Voraussetzungen sind vollständig geplant: `ShowSeasons`-Mock-Erweiterung, DevApi-Snapshot-Felder (Debug-only), `UiDriver`-ComboBox-Hilfen — alle in der Umsetzungsreihenfolge vor dem E2E-Test eingeordnet.
- `EpisodesCollection` wird in `MediaDetailsPageContractTests` (Z. 68) gepinnt — die Anpassung ist explizit als betroffener Test benannt.
- Der Episoden-Tap ersetzt `SelectionChanged` durch `TapGestureRecognizer` — identischer Benutzerfluss wie beim `MediaCarousel`-Muster; die inneren Download-/Abspielen-Schalter bleiben unberührt.
