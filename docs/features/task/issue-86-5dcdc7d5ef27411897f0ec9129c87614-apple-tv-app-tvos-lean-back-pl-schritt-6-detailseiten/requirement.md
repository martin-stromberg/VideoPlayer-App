# Übersetzte Anforderung

## Titel

tvOS-Detailansichten für alle Medientypen (Entwicklungsschritt 6)

## Ziel

Die tvOS-App (`VideoPlayer.TvOS`) erhält vollständige Detailansichten für
alle Medientypen des bestehenden Detailseiten-Dienstes, die den bisherigen
Platzhalter (`PreparedTargetViewController`) für `NavigateToMediaDetailsEffect`
ersetzen.

## Funktionale Anforderungen

- Detailansichten für die Medientypen **Film** (`movie`), **Filmsammlung**
  (`moviecollection`), **Serie** (`tvshow`), **Staffel** (`tvshowseason`)
  und **Episode** (`tvshowepisode`).
- Inhalte je Ansicht: Titel, Beschreibung, Genre-/Jahres-Metadaten,
  Bildmaterial, „Gesehen"-Kennzeichnung und Hinweis auf eine lokal
  vorhandene Datei.
- Serien zeigen Staffeln und Episoden (inkl. Gesehen-Status und
  lokaler-Datei-Hinweis je Episode); Staffel- und Episodenauswahl ist per
  Fernbedienung fokussierbar.
- Aktionen je nach Typ:
  - **Abspielen** — Wiedergabeeinstieg; wird mit dem Wiedergabe-Schritt
    funktional, bis dahin vorbereitet (kein Fehler).
  - **Download anstoßen** — manueller Download; wird mit dem
    Download-Schritt funktional; ausgegraut/vorbereitet ist akzeptabel.
  - **Favoriten umschalten** — erlaubtes Nutzungsdatum (kein
    Inhalts-Schreibzugriff); aktualisiert Anzeige und Server/Cache.
- Online aus dem Server, offline aus dem gespeicherten Detail-Snapshot
  (`MediaDetailsService`-Offline-Pfad); Offline-Aufruf zeigt den Snapshot
  statt einer Fehlerseite.
- Keine Bearbeitung von Inhalts-Metadaten und Playlists (ausgeschlossen).

## Nicht-funktionale Anforderungen

- Bestehende MVU-Verdrahtung nutzen: `MediaDetailsRequestedMessage` →
  `LoadMediaDetailsEffect` → `MediaDetailsLoadedMessage`;
  `SeasonSelectedMessage`, `FavoriteToggleRequestedMessage` →
  `ToggleFavoriteEffect`, `PlayRequestedMessage`, `DownloadRequestedMessage`.
- Favoriten-Toggle ausschließlich über Messages/Effekte — kein direkter
  Schreibzugriff.
- tvOS-Kompilierbarkeit über den macOS-CI-Job `build-tvos` (Push des
  Branches autorisiert); lokale Kompilierung unter Windows nicht möglich.
- `dotnet test VideoPlayer.Maui.Tests` bleibt grün; Contract-Tests analog
  `TvOsBrowsingContractTests` hinzufügen.
- Bekannte tvOS-Kompilierfallen vermeiden (`.Spacing` nur auf
  `UIStackView`, Named-Args-Case, kein `IReadOnlyList.IndexOf`, kein
  `const nfloat`, `RegisterClassForCell`, `TvosControls.*`-Signaturen).

## Akzeptanzkriterien

1. Jeder Medientyp öffnet eine vollständige Detailansicht mit den
   genannten Informationen.
2. Serien zeigen Staffeln und Episoden inkl. Gesehen-Status und
   lokaler-Datei-Hinweis.
3. Favoriten-Umschalten aktualisiert Anzeige und Server/Cache.
4. Offline-Aufruf zeigt den gespeicherten Snapshot statt einer Fehlerseite.
5. Die vorbereiteten Wiedergabe-/Download-Einstiege sind sichtbar und
   lösen keinen Fehler aus.
6. `build-tvos` (macOS-CI) ist grün auf diesem Branch.
