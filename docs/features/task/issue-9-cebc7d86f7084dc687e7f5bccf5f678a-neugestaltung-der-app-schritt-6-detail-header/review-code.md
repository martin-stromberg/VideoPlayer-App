# Code-Review: Detailseiten — Vollbreiten-Header mit Hintergrundgrafik

## Ergebnis

**Status:** Keine Befunde

## Prüfschwerpunkte

| Bereich | Befund |
|---------|--------|
| MVU-Reinheit | Kein `ImageSource`/MAUI-Typ im MVU-Core; Bildtransport als `byte[]` (Vertragstest `MediaDetailsView_CarriesHeaderIdentityAndTexts` pinnt das). |
| Race-Bedingungen | `MediaDetailsHeaderImageLoadedMessage` trägt Medien- **und** Bildidentität; `UpdateMediaDetailsHeaderImageLoaded` verwirft verspätete Antworten (Staffelwechsel während des Abrufs, abgelöste Seite). |
| Race: Staffelwechsel | Bei geänderter Bildquelle werden `HeaderImageBytes` verworfen und `IsHeaderImageLoading` zurückgesetzt, bevor `WithDetailsHeaderLoad` den neuen Abruf anstößt — kein flackerndes Fremdbild, kein hängender Ladezustand. |
| Fehlertoleranz | `LoadMediaDetailsHeaderAsync` fängt alle Fehler außer `OperationCanceledException` (wie `LoadHeroBackgroundAsync`); `null`-Bytes sind kein Fehlerzustand und löschen kein vorhandenes Bild. |
| Offline | `WithDetailsHeaderLoad` gate't auf `ConnectionState.Connected`; Offline-Snapshots tragen keine Bild-Ids — kein Abruf, kein Absturz. |
| Ungültige Ids | `GetEpisodeBackgroundImageAsync`/`GetPictureAsync` werfen bei Id ≤ 0; der Handler ruft nur bei `is > 0` — kein falscher Request. |
| Bildkette | `BannerPictureId` ist bereits in den Map-Funktionen als Banner → Fanart → Poster aufgelöst; Staffel-Banner (`SeasonItem.BannerPictureId`) gleiche Kette, Fallback Serien-Banner — deckt die Web-Kette ab und ist robuster. |
| Episoden-Still | Nutzt den dedizierten Endpunkt `api/episodes/{id}/background-image` (serverseitige Kette generiert → Banner/Fanart → Platzhalter) — wie im Web. |
| Bestehende API | `SelectedEpisodeId` ist `init`-only; `HighlightedEpisodeId`/`IsHighlighted` unverändert (Play-/Download-Titel der navigierten Episode bleiben). Bestehender Test `SeasonSelected_SwitchesSelectedSeason` bleibt gültig. |
| Lecks | Kein neues `ModelChanged`-Abo; `SizeChanged += OnSizeChanged` folgt dem HomePage-Muster (gleiche Lebensdauer wie die Seite). |
| XAML | Kein hartcodierter Farbwert — alle Header-Farben über `Vp*`-Tokens; `AutomationId`s an Header-Elementen; Trennlinie als 1px-`BoxView`. |
| Stil | Deutsche XML-Dokumentation an allen neuen öffentlichen Membern; kein `NotImplementedException`; kein kommentierter Alt-Code. |

## Geprüfte Randfälle

- `SeasonSelected` mit unbekannter `SeasonId`: `SelectedSeason` fällt auf
  die erste Staffel zurück, `SelectedEpisodeId = null` → stabiler
  Serien-/Staffel-Header.
- `MediaDetailsLoaded` mit `Details == null` (Fehler): kein
  Header-Effekt, `IsHeaderImageLoading` bleibt `false`.
- Eigenständige Episode ohne Serienkontext: `SelectedEpisodeId =
  MediaId` → Still-Endpunkt; `HeaderMeta` zeigt „Episode · {Jahr}".
- Offline-Einzel-Episode: `SelectedEpisodeId` gesetzt, aber kein Abruf
  (nicht verbunden) → nur Texte.

## Nächster Schritt

Schritt 10 — Builds (Debug/Release) und volle Testsuite.
