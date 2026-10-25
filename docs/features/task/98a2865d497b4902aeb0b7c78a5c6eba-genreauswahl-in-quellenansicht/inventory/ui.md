# UI-Layout

## `SourceOverviewPage` (VideoPlayer.Maui/Pages/SourceOverviewPage.xaml)

Seiten-Grid: `Padding="16"`, `RowDefinitions="Auto,Auto,Auto,Auto,*"`, `RowSpacing="12"`.

| Zeile | Inhalt |
|-------|--------|
| 0 (`Auto`) | `Border` mit `InputFrame`-Style → `SearchBar` `MediaSearchBar` (AutomationId `MediaSearchBar`) |
| 1 (`Auto`) | **`CollectionView` `GenresCollection`** (AutomationId `GenresCollection`): `ItemsSource="{Binding Model.SourceBrowser.Genres}"`, `IsVisible="{Binding Model.SourceBrowser.ShowGenreFilter}"`, `SelectionMode="Single"`, `SelectionChanged="OnGenreSelected"`, `LinearItemsLayout Orientation="Horizontal" ItemSpacing="8"`. **Keine `HeightRequest`, kein `VerticalOptions`.** ItemTemplate: `Border` mit `Chip`-Style + `DataTrigger` auf `IsSelected` (Background `VpPrimaryTint`, Stroke `Transparent`); innen `Label` `Text="{Binding Name}"`, FontSize 13, `TextColor VpTextMuted`, Trigger bei `IsSelected` → `VpPrimarySoft` + `InterSemiBold`. |
| 2 (`Auto`) | `ActivityIndicator` `InitialLoadingIndicator` (`IsLoading`), `VerticalStackLayout` `ErrorPanel` (Fehler + „Erneut versuchen"), `Label` `EmptyLabel` („Keine Einträge gefunden.") |
| 4 (`*`) | `CollectionView` `ItemsCollection` (AutomationId `ItemsCollection`): `GridItemsLayout Orientation="Vertical" Span="3"`, `RemainingItemsThreshold="6"` → `OnRemainingItemsThresholdReached`, ItemTemplate = Posterkarte (`CachedPosterImage`, HeightRequest 210) + Titel-`Label`; Footer mit `FooterErrorPanel`/`FooterLoadingIndicator`. ItemsSource wird im Code-Behind auf die stabile `browserItems`-`ObservableCollection` gesetzt (`SourceItemListSync.Sync`). |

**Fehlerbild (TestFlight-Screenshot, iPad):** `GenresCollection` belegt die gesamte Resthöhe; jede Chip-`Border` füllt ihre Zelle vertikal → spaltenförmige, bildschirmhohe Pillen („Alle", „Abenteuer", „Action", …); das Medien-Raster ist nicht sichtbar.

## Styles (VideoPlayer.Maui/Resources/Styles/Styles.xaml)

| Style | TargetType | Relevante Setter |
|-------|-----------|------------------|
| `InputFrame` | `Border` | Stroke `VpBorder`, `RoundRectangle 10`, `BackgroundColor VpSurfaceLow`, `Padding 14,0`, `MinimumHeightRequest 48`, `VerticalOptions Center` |
| `Chip` | `Border` | Stroke `VpBorder`, `RoundRectangle 999`, `StrokeThickness 1`, `BackgroundColor VpSurfaceHigh`, `Padding 14,7` — **keine Höhen-/Options-Setter** |
| `KickerChip` | `Border` | wie `Chip`, aber `BackgroundColor VpOverlay`, `Padding 12,5`, `HorizontalOptions Start` |

`Chip` wird außerhalb der `SourceOverviewPage` nicht verwendet (`KickerChip` u. a. auf `HomePage`, `ServerSetupPage`; eigene Inline-Pill in `DownloadsPage`).

## Vergleichsmuster `MediaCarousel` (VideoPlayer.Maui/Controls/MediaCarousel.xaml)

Bewährtes horizontales Listenmuster **ohne** `CollectionView`:

```
<ScrollView Orientation="Horizontal" HorizontalScrollBarVisibility="Never">
    <HorizontalStackLayout BindableLayout.ItemsSource="{Binding … Items}" Spacing="14" …>
        <BindableLayout.ItemTemplate>
            <DataTemplate x:DataType="mvu:HomeMediaItem">
                … TapGestureRecognizer Tapped="OnItemTapped" …
            </DataTemplate>
        </BindableLayout.ItemTemplate>
    </HorizontalStackLayout>
</ScrollView>
```

- Höhe ergibt sich aus dem Inhalt (Auto-Maß), kein vertikales Füllen.
- Auswahl per `TapGestureRecognizer` `OnItemTapped` → `BindingContext` des Senders liefert das Item (MediaCarousel.xaml.cs `OnItemTapped`, Zeilen 115–121).
