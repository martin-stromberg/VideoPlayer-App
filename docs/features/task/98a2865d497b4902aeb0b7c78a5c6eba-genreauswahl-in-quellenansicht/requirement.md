# Übersetzte Anforderung: Genreauswahl in Quellenansicht verhindert die Einsicht der Titel

Quelle: `issue.md` (Aufgaben-ID 98a2865d-497b-4902-aeb0-b7c78a5c6eba), TestFlight-Feedback inkl. Screenshot (`testflight_feedback (1).zip` → `screenshot_01.jpg`, iPad11,1, iOS 26.7.1, App 1.1.0 (387), Kommentar „Ansicht der Quelle").

## Fachliche Zusammenfassung

In der Browsing-Ansicht einer Quelle (`SourceOverviewPage`, Shell-Route `sourceoverview`) überdeckt die horizontale Genre-Leiste (`GenresCollection`) auf iOS/iPadOS praktisch den gesamten verfügbaren Platz: Die Genre-Chips („Alle", „Abenteuer", „Action", …) werden als bildschirmhohe vertikale Pillen gerendert. Das Medien-Raster (`ItemsCollection`) mit den Titeln der Quelle ist dadurch nicht sichtbar bzw. nicht erreichbar — Filme und Serien der Quelle können weder eingesehen noch geöffnet werden. Gefordert ist, dass die Genre-Leiste nur eine schmale, einzeilige horizontale Chip-Leiste einnimmt und die Titelliste darunter sichtbar und bedienbar bleibt.

## Betroffene Klassen und Komponenten

- `VideoPlayer.Maui/Pages/SourceOverviewPage.xaml` — Layout der Seite (Grid `Auto,Auto,Auto,Auto,*`), `GenresCollection` (`CollectionView` mit `LinearItemsLayout Orientation="Horizontal"`) und `ItemsCollection` (`CollectionView` mit `GridItemsLayout`, Span 3).
- `VideoPlayer.Maui/Resources/Styles/Styles.xaml` — `Chip`-Style (Pill-Border, `RoundRectangle 999`, `Padding 14,7`), ggf. Höhen-/VertikalOptions-Setters.
- `VideoPlayer.Mvu` — `SourceBrowserState` (`Genres`, `ShowGenreFilter`, `Items`, …), `SourceGenreItem`, `SourceMediaItem`; voraussichtlich unverändert (reines UI-Layout-Problem).
- `VideoPlayer.E2E.Tests` — `SourceBrowsingTests` (Genre-Leiste sichtbar/filtert); DevApi `GET /tree` liefert Element-Bounds → Höhenprüfung der Genre-Leiste als Funktionsnachweis möglich.

## Implementierungsansatz

Layoutkorrektur in `SourceOverviewPage.xaml`: Die Höhe der horizontalen Genre-Leiste muss begrenzt werden, damit sie im `Auto`-Grid nicht die gesamte Resthöhe belegt (unter iOS misst die `CollectionView` die angebotene Höhe statt der Inhaltshöhe). Denkbar: feste `HeightRequest` an `GenresCollection`, `VerticalOptions` der Chip-`Border` im `DataTemplate`, oder Ersatz durch das im Projekt bewährte Muster `ScrollView Orientation="Horizontal"` + `HorizontalStackLayout` mit `BindableLayout` (vgl. `MediaCarousel`). Auswahlverhalten (`OnGenreSelected` → `SourceGenreSelectedMessage`) und `IsVisible`-Bindung an `ShowGenreFilter` bleiben fachlich unverändert.

## Konfiguration

Keine — reine Layoutkorrektur, kein konfigurierbares Verhalten.

## Offene Fragen

- Keine fachlich offenen Fragen; der Screenshot zeigt den Fehler eindeutig. (Technische Auswahl des Layout-Ansatzes wird im Umsetzungsplan entschieden.)
