# Bestandsaufnahme: UI und Navigation

## Navigation

- `AppShell.xaml`: Flyout mit 6 Menüseiten (`home`, `sources`,
  `favorites`, `downloads`, `profile`, `settings`) — per
  Contract-Test `VideoPlayerMauiContractTests` abgesichert
  (Reihenfolge/Titel/Routen).
- `AppShell.xaml.cs` registriert geschobene Routen:
  `serversetup`, `login`, `mediadetails`, `sourceoverview`.
  Eine Route `player` fehlt.
- `ShellAppEffectHandler` führt `NavigateTo…`-Effekte über
  `Shell.Current.GoToAsync(route)` aus (Query-Parameter per
  `Uri.EscapeDataString`).
- Detailseiten-Navigation: `NavigateToMediaDetailsEffect` →
  `mediadetails?mediaType=…&id=…&title=…[&show&season&episode]`;
  `MediaDetailsPage` liest sie über `QueryProperty`.

## `MediaDetailsPage` (`VideoPlayer.Maui/Pages/`)

- Zeigt Kopfbereich (Poster, Titel, Meta, Favoriten-Stern),
  Handlung, Serienansicht (Staffel-`Picker` + Episodenliste,
  `SelectionChanged` → `MediaItemSelectedMessage` zur
  Episoden-Detailseite) und Kollektionsraster.
- Eine „Abspielen"-Schaltfläche existiert noch nicht — weder für
  Filme/Episoden noch je Episoden-Eintrag.
- Muster der Seitenverdrahtung: Konstruktor erhält `MvuHost`,
  `BindingContext = mvuHost`, `ModelChanged` → `UpdateUi(model)`,
  `OnAppearing` dispatcht die Seiten-Message genau einmal
  (`requested`-Flag).

## Player-Seite

Existiert nicht. Nächster Nachbar im Altbestand:
`MediaBannerPlayer` (ContentView mit `MediaElement`, 5-s-
Positionstakt über `System.Timers.Timer`, `MediaFailed`-Anzeige —
s. `legacy.md`).

## Stile

Dunkles Farbschema (`#23232a` Karten, `#FFDDAA` Akzente,
`#FFB84D` Ladeanzeige) in `Resources/Styles/Colors.xaml` /
`Styles.xaml` — die Player-Seite schließt sich daran an.
