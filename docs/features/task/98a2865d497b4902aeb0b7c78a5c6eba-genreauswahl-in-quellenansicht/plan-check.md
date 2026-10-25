# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Genre-Buttons ragen nicht über den unteren Rand / Leiste füllt nicht die Seite | Schritt 3: Umbau `CollectionView` → `ScrollView` + `HorizontalStackLayout` + `BindableLayout` (Auto-Höhe aus Chip-Inhalt) | `SourcesContractTests.SourceOverviewPage_GenreBar_UsesBoundedHorizontalLayout` (Red→Green) + `SourceBrowsingTests.GenreBar_HeightBounded_ItemsGridVisible` (E2E-Bounds ≤ 120) | Abgedeckt |
| Titelliste der Quelle einsehbar | Schritt 3: `ItemsCollection` (Zeile `*`) erhält Resthöhe zurück | `GenreBar_HeightBounded_ItemsGridVisible`: `ItemsCollection` sichtbar, `Bounds.Height` ≥ 200 | Abgedeckt |
| Filme und Serien aufrufbar | Auswahl-Ereigniskette `ItemsCollection` unverändert; `OnGenreTapped` ersetzt `OnGenreSelected` fachlich identisch | Bestehende E2E: `SourceSearchTests.SubmittedText_RemainsVisibleAfterReturn` (öffnet Eintrag → `/api/items/movie/501`), `GenreBar_VisibleAndFilters_ResetsOnAll` (Filterauswahl) | Abgedeckt |
| Keine funktionslose „Alle"-nur-Leiste (Bestandsregel, darf nicht brechen) | `IsVisible="{Binding Model.SourceBrowser.ShowGenreFilter}"` bleibt am Container | Bestehende E2E: `GenreBar_Hidden_WhenServerHasNoGenres` | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Quelle öffnen → begrenzte Genre-Leiste + sichtbares Raster | `GenreBar_HeightBounded_ItemsGridVisible` (neu) | Abgedeckt |
| Genre wählen → gefilterter Reload; „Alle" → ungefiltert | `GenreBar_VisibleAndFilters_ResetsOnAll` (bestehend) | Abgedeckt |
| Titel öffnen → Detail laden | `SourceSearchTests.SubmittedText_RemainsVisibleAfterReturn` (bestehend, enthält `InvokeAsync("ItemsCollection", 0)` + Request-Nachweis) | Abgedeckt |
| Keine Server-Genres → Leiste verborgen | `GenreBar_Hidden_WhenServerHasNoGenres` (bestehend) | Abgedeckt |
| Sucheingabe über/unter der Leiste | `SourceSearchTests` (bestehend) | Abgedeckt |

## Hinweise

- Der neue E2E-Test kann den iOS-spezifischen Messfehler unter Windows nicht reproduzieren; er sichert das Akzeptanzkriterium plattformübergreifend als Regression (Bounds-Nachweis). Der Red→Green-Funktionsnachweis des Umbaus liegt beim Contract-Test (XAML-Struktur), da der Layoutfehler selbst kein modellseitig fassbares Verhalten ist.
- Voraussetzungen in der Umsetzungsreihenfolge sind stimmig: `DevApiBounds`-DTO vor dem Bounds-Test; DevApi-Infrastruktur (`TrySelectBindableLayoutItem`, `ItemsOf`) ist laut Bestandsaufnahme bereits vorhanden.
- Negative Fälle (Leiste ausgeblendet ohne Genres; Leiste nicht bildschirmfüllend) sind als Szenarien verankert; Berechtigungs-/Sichtbarkeitslogik ist nicht betroffen (keine Datenfilteränderung — `ShowGenreFilter` bleibt unverändert).
