# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Beim Nachladen einer Folgeseite bleibt die bisherige Scrollposition erhalten | Stabile `ObservableCollection<SourceMediaItem>` als `ItemsSource` + `SourceItemListSync` (Präfix-Abgleich über `Id`, nur `Add` am Ende) — `UpdateSourceItemsLoaded` bleibt unverändert | Unit: `Sync_AppendOnly_AddsTail_WithoutReset` (nur `Add`, kein `Reset`/`Remove`); E2E-Pflicht: `NextPageLoad_KeepsScrollPosition` (`VerticalScrollPercent > 0` nach Append) | Abgedeckt |
| Nachgeladene Einträge erscheinen am Ende angehängt | Derselbe Mechanismus; Modell-Verkettung (`Concat`) unverändert | Unit: Append-Tail; E2E: `SourceBrowserItemTitles` wächst seitenweise auf 40+ | Abgedeckt |
| Gilt gleichermaßen für ungefilterte Liste und Such-/Genre-Treffer | Derselbe Anzeige- und Nachladepfad — Filter nur als Effektparameter; keine pfadbezogene Sonderlogik | Bestands-E2E `SourceBrowsingTests`/`SourceSearchTests` bleiben grün; Unit-Tests der Sync-Logik pfadunabhängig | Abgedeckt |
| Erstladen, Genre-Wechsel, neue Suche zeigen Liste von Anfang an | Reset-Pfad: `Items = []` bzw. abweichender erster Eintrag → Präfixbruch → Ersetzen ab Position 0 → Ansicht ab Anfang | Unit: `Sync_ChangedPrefix_ReplacesFromDivergence`, `Sync_EmptyModel_Clears`; Bestands-E2E `GenreBar_VisibleAndFilters_ResetsOnAll`, `SubmitViaEnter_FiltersFromFirstPage` | Abgedeckt |
| Ansicht springt nicht (Ergebniskriterium) | Kein `ItemsSource`-Referenzwechsel mehr nach Einmal-Zuweisung | E2E-Pflichtszenario | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Durchscrollen mit automatischem Nachladen hält die Scrollposition | `SourcePagingTests.NextPageLoad_KeepsScrollPosition` (ScrollToEnd → Folgeseiten → `VerticalScrollPercent > 0`) | Abgedeckt |
| Nachgeladene Einträge hängen am Ende | im selben Szenario (Titel-Vollständigkeit + Reihenfolge per Modell- und Tree-Assertion) | Abgedeckt |
| Genre-Wechsel / neue Suche / Erstladen zeigen von Anfang an | Bestehende Tests `GenreBar_VisibleAndFilters_ResetsOnAll`, `SubmitViaEnter_FiltersFromFirstPage` (unverändert grün — Liste lädt ab Seite 0 und ist initial am Anfang) | Abgedeckt |

## Hinweise

- Der E2E-Scroll-Trigger setzt auf UIA-`ScrollPattern` am ScrollViewer der
  `ItemsCollection`; der Plan sieht explizit einen Mausrad-Fallback in
  `UiDriver.ScrollToEnd` vor. Ist in der Laufumgebung weder ScrollPattern noch
  Mausrad-Eingabe wirksam, ist das als nicht ausführbares E2E-Szenario zu
  dokumentieren (kein stiller Ersatz durch Unit-Tests).
- `PlaylistDetailsPage` hat denselben Defekt, liegt aber außerhalb der
  Anforderung — korrekt als Nicht-Umfang im Plan vermerkt.
- Keine neuen öffentlichen API-Flächen außer `SourceItemListSync.Sync`
  (Pre-Commit-Hook XML-Doc an public APIs beachten — Kommentare analog
  `DownloadListSync` einplanen).
