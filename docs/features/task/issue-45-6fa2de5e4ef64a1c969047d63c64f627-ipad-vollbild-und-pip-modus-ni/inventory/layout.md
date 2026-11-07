# Layout — `PlayerPage.xaml` (Overlay-Struktur)

Datei: `VideoPlayer.Maui/Pages/PlayerPage.xaml` (132 Zeilen,
vollständig gelesen).

Die Seite (`ContentPage`, `x:DataType="mvu:MvuHost"`, Titel
„Wiedergabe") enthält genau ein Wurzelelement: `Grid
x:Name="PlayerRoot"` mit `BackgroundColor="{StaticResource VpBgDeep}"`.
Alle Kinder stapeln sich implizit in derselben Grid-Zelle in
Deklarationsreihenfolge (später = weiter oben).

## Elemente des `PlayerRoot`-Grids

| Element | Typ | Position/Ausdehnung | Eingabeverhalten | Sichtbarkeit |
|---------|-----|---------------------|------------------|--------------|
| `VideoElement` | `mct:MediaElement` | füllt das Grid (Default) | `ShouldShowPlaybackControls="True"` → unter iOS `AVPlayerViewController` mit nativer Transportleiste (Vollbild-/PiP-Buttons oben) | immer |
| `LoadingIndicator` | `ActivityIndicator` | zentriert, 48×48 | nicht interaktiv | `Model.Player.IsResolving` (Binding) plus `IsBuffering` per Code-Behind |
| `OverlayChrome` | `VerticalStackLayout` | `VerticalOptions="Start"`, volle Breite, `Spacing="0"` | **kein `InputTransparent` am Layout selbst** | `!player.IsPictureInPicture` (Code-Behind, initial `true`) |
| `PlaylistNavigationBar` | `HorizontalStackLayout` | `HorizontalOptions="End"`, `VerticalOptions="Start"`, `Margin="0,64,16,0"`, `Spacing="10"` | interaktiv (Buttons) | `player.IsPlaylistPlayback && !player.IsPictureInPicture`, initial `IsVisible="False"` |
| `PipButton` | `Button` | `HorizontalOptions="End"`, `VerticalOptions="Start"`, `Margin="0,18,16,0"`, `HeightRequest="38"` | `Clicked="OnPipClicked"` | initial `IsVisible="False"`; im Konstruktor `pictureInPicture.IsToggleSupported` (nur Windows `true`) |
| `ErrorPanel` | `Border` | `VerticalOptions="End"`, `Margin="16"`, `Padding="14,10"` | nicht interaktiv | `player.HasError && !player.IsPictureInPicture`, initial `IsVisible="False"` |

## `VideoElement` (Zeilen 13–20)

```xaml
<mct:MediaElement x:Name="VideoElement"
                  ShouldAutoPlay="True"
                  ShouldShowPlaybackControls="True"
                  ShouldKeepScreenOn="True"
                  MediaOpened="OnMediaOpened"
                  MediaEnded="OnMediaEnded"
                  MediaFailed="OnMediaFailed"
                  StateChanged="OnMediaStateChanged" />
```

`ShouldShowPlaybackControls="True"` ist der geforderte Bedienweg —
unter iOS blendet der `AVPlayerViewController` die Transportleiste mit
Vollbild-/PiP-Buttons oben über dem Video ein.

## `OverlayChrome` (Zeilen 30–77)

Kommentarblock (Zeilen 30–37) dokumentiert die bestehende
Windows-Korrektur: `VerticalOptions="Start"` ist Pflicht, weil der
LayoutOptions-Default `Fill` das Layout auf das gesamte Grid ausdehnen
und als Hit-Test-Blocker alle Eingaben auf dem `MediaElement` abfangen
würde. Außerdem dokumentiert: ein interaktives Element darf nicht im
eingabedurchlässigen `Border` liegen — unter Windows deaktiviert
`InputTransparent` am Elter den gesamten Teilbaum und ein
`InputTransparent="False"` am Kind reaktiviert es nicht.

```xaml
<VerticalStackLayout x:Name="OverlayChrome"
                     VerticalOptions="Start"
                     Spacing="0">
    <Border Padding="14"
            InputTransparent="True">
        <Border.Background>
            <LinearGradientBrush StartPoint="0,0" EndPoint="0,1"> …
        </Border.Background>
        <Grid ColumnDefinitions="*,Auto" ColumnSpacing="8">
            <VerticalStackLayout Grid.Column="0" Spacing="2">
                <Label x:Name="TitleLabel" FontSize="16"
                       FontFamily="InterSemiBold"
                       TextColor="{StaticResource VpText}"
                       VerticalOptions="Center"
                       LineBreakMode="TailTruncation" />
                <Label x:Name="PlaylistContextLabel" FontSize="12"
                       TextColor="{StaticResource VpSecondary}"
                       VerticalOptions="Center"
                       LineBreakMode="TailTruncation"
                       IsVisible="False" />
            </VerticalStackLayout>
            <Label x:Name="WatchedBadge" Grid.Column="1"
                   Text="Angesehen" FontSize="12"
                   TextColor="{StaticResource VpSecondary}"
                   VerticalOptions="Center" IsVisible="False" />
        </Grid>
    </Border>
</VerticalStackLayout>
```

Feststellung zur Anforderung:

- `InputTransparent="True"` steht **nur** am inneren `Border`
  (Zeile 42), nicht am umschließenden `VerticalStackLayout`
  (Zeilen 38–40). Die Bounds des Elternlayouts können unter iOS
  weiterhin hit-testen.
- Das Band enthält ausschließlich `Label`s (`TitleLabel`,
  `PlaylistContextLabel`, `WatchedBadge`) — kein interaktives Element.
- `PlaylistContextLabel` und `WatchedBadge` sind initial
  `IsVisible="False"` und werden per `UpdateUi` geschaltet.

## `PlaylistNavigationBar` (Zeilen 79–103)

Enthält `PreviousButton` (`AutomationId="PlayerPreviousButton"`,
Text „Zurück", `Clicked="OnPreviousClicked"`) und `NextButton`
(`AutomationId="PlayerNextButton"`, Text „Weiter",
`Clicked="OnNextClicked"`), beide `Style="{StaticResource
SecondaryButton}"`, `FontSize="12"`, `HeightRequest="38"`. Liegt oben
rechts (`Margin="0,64,16,0"`) — unterhalb der `PipButton`-Position, im
selben Randbereich, in dem iOS die Transportleiste einblendet.
Muss klickbar bleiben (darf nicht `InputTransparent` werden).

## `PipButton` (Zeilen 105–116)

Eigener Button direkt im `PlayerRoot`-Grid (nicht im `Border` des
Kopfbands — bewusst, siehe Kommentar Zeilen 105–106).
`Margin="0,18,16,0"` oben rechts. Unter iOS unsichtbar, weil
`IPictureInPictureService.IsToggleSupported` dort `false` liefert
(Konstruktor-Zeile 53 in `PlayerPage.xaml.cs`).

## `ErrorPanel` (Zeilen 118–128)

`Border` mit `ErrorLabel` (`Style="{StaticResource ErrorText}"`), unten
(`VerticalOptions="End"`), außerhalb des umstrittenen oberen Bereichs.

## Plattform-/Idiom-Mechanismen

Im gesamten `VideoPlayer.Maui`-Projekt (und `VideoWebPlayer.Maui`)
kommen **keine** `OnPlatform`-/`OnIdiom`-XAML-Blöcke und keine
`DeviceInfo.Idiom`-Prüfungen vor (durchsucht: `*.xaml`, `*.cs`).
Plattformbedingtes Verhalten existiert ausschließlich per
`#if WINDOWS` im Code-Behind (`PlayerPage.xaml.cs` Konstruktor und
`OnPlayerTapped`) sowie in `PictureInPictureService`.

Weiteres `InputTransparent`-Vorkommen außerhalb der PlayerPage:
`VideoPlayer.Maui/Pages/MediaDetailsPage.xaml:228` — „Gesehen"-Badge
über dem Poster der Detailseite, nicht relevant für diese Anforderung.
