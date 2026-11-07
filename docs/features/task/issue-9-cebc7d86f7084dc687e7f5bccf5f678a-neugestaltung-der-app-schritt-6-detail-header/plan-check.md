# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| 1. Vollbreiter Header auf allen fünf Detailansichten (Cover, `clamp(360,52vw,620)`) | `MediaDetailsPage.xaml`-Umbau: `HeaderSection` volle Breite (Wurzel-Padding verschoben), `AspectFill`, `Math.Clamp(Width*0.52,360,620)`; `MediaDetailsView`-Header-Ableitungen decken alle fünf `MediaType`s | `MediaDetailsPageContractTests` (XAML-Struktur, Clamp-Code), `MediaDetailsView_HeaderText` je Kontext | Abgedeckt |
| 2. Dunkelgradient-Overlay (seitlicher Verlauf + Bottom-Fade + Trennlinie) | Zwei `LinearGradientBrush`-`Border` (`VpHeroSide*`, `VpHeroFadeClear`→`VpBg`) + `BoxView` `VpBorder` — Wiederverwendung der Schritt-5-Tokens | `MediaDetailsPageContractTests.MediaDetailsPage_FullWidthHeader` prüft `VpHeroSideStrong`, `VpHeroFadeClear`, `VpBorder`, `LinearGradientBrush` | Abgedeckt |
| 3. Inhalt unten links: Titel, Meta (Jahr/Genres, Primär-Soft), Plot, Aktions-/Favoriten-Schalter | `HeaderTitle`/`HeaderMeta`/`HeaderPlot` auf `MediaDetailsView`; `TitleLabel`/`MetaLabel` (`VpPrimarySoft`, fett)/`PlotLabel` (`VpTextSoft`) im Content-Block; `FavoriteButton` oben rechts, `PlayButton`/`DownloadButton` unten rechts | `MediaDetailsView_HeaderText` (je Kontext), `MediaDetailsPage_FullWidthHeader` (Button-Platzierung) | Abgedeckt |
| 4. Bildquellen + Fallbackkette | `HeaderPictureId` (`BannerPictureId` = Banner→Fanart→Poster bereits aufgelöst; Staffel: `SeasonItem.BannerPictureId` → Fallback Serien-Backdrop); `HeaderEpisodeId` → `api/episodes/{id}/background-image` (serverseitige Kette generiert→Banner/Fanart→Platzhalter) | `MediaDetailsLoaded_Show/Season/Episode_Emits…Effect`, `MapShow_SeasonBannerAndYears`, Endpunkt-Test `api/episodes/{id}/background-image` | Abgedeckt |
| 5. Header wechselt auf Still der ausgewählten Episode | `SelectedEpisodeId`: navigierte Episode initial; `SeasonSelectedMessage` wählt erste Episode der Staffel (Web `SelectSeasonCoreAsync`) und löst Header-Reload aus; Episoden-Tap navigiert weiterhin zur Episoden-Detailseite mit Still-Header | `SeasonSelected_SelectsFirstEpisode_ReloadsHeader`, `LoadAsync_Episode_SetsSelectedEpisodeId`, `HeaderImageLoaded_MatchingIdentity_AppliesBytes` | Abgedeckt |
| 6. Bisherige Inhalte bleiben erhalten | `SeasonSection`, `CollectionSection`, `DownloadNoticeLabel` wandern unverändert in den gepaddeten Unterbereich; keine Handler-/Template-Änderungen | `MediaDetailsPageContractTests` prüft Erhalt der Sektionen; bestehende `AppUpdateTests_MediaDetails` bleiben grün | Abgedeckt |
| 7. Fallback ohne Bild / Offline-Stabilität | `VpSurfaceLow`-Grund + Gradienten ohne Bildebene; kein Effekt ohne `Connected`; Fehlschlag → `null`-Bytes → stabiler Zustand; Identitäts-Abgleich verwirft veraltete Antworten | `MediaDetailsLoaded_Offline_NoHeaderEffect`, `HeaderImageLoaded_Failure_StaysStable`, `…_StaleIdentity_Discarded`, `MapOfflineShow_NoPictureIds_HeaderStable` | Abgedeckt |
| 8. DoD (Debug+Release, volle Testsuite) | Umsetzungsreihenfolge Schritt 7 verlangt vollständige Suite; DoD in Anforderung | Gesamtlauf `dotnet test` am Ende | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Detailseite öffnen und Header sehen | — | Nicht erforderlich mit Begründung: Das Testprojekt enthält kein UI-E2E-Harness für die MAUI-App (kein Playwright/Appium/FlaUI; MAUI-Schicht nicht instanziierbar). Der etablierte Funktionsnachweis der UI-Schritte 5 und 7 sind MVU-Übergangstests + Quell-Vertragstests; der Plan folgt diesem Muster (§ Tests → E2E-Tests). |
| Staffelwechsel ändert Header | — | Ebenso — abgesichert über `SeasonSelected_SelectsFirstEpisode_ReloadsHeader` (MVU) und Vertragstests. |

## Hinweise

- Der Plan setzt bewusst auf das Schritt-5-Muster (`byte[]` im Model statt
  `PosterImageService`-Dateicache). Header-Bilder werden dadurch bei jedem
  Detail-Aufruf neu vom Server geholt — vertretbar, da der Server-Platzhalter
  klein ist und der Besuch kurzlebig; eine Caching-Erweiterung wäre ein
  eigenes Thema.
- `MapOfflineShow` liefert keine Bild-Identität — der Offline-Header zeigt
  bewusst nur Gradienten/Texte; das erfüllt AK 7.
- Das bestehende `ModelChanged`-Abo-Leck der Seite (Querschnittsbefund
  Schritt 4) wird nicht verschärft: es kommt kein weiteres Abo hinzu.
