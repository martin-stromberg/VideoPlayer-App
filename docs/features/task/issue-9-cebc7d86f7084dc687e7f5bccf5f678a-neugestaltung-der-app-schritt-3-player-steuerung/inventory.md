# Bestandsaufnahme: Videoplayer — Steuerelemente in der Vollansicht bedienbar (Schritt 3)

Analysiert wurde die Wiedergabeseite `PlayerPage` der neuen MAUI-App
(`VideoPlayer.Maui`) bezogen auf die Anforderung, die nativen
Transportsteuerungen des `MediaElement` in der Vollansicht bedienbar
zu machen.

## Zusammenfassung

- `PlayerPage.xaml`: Ein `Grid` stapelt `MediaElement`,
  `LoadingIndicator`, `OverlayChrome` und `ErrorPanel` übereinander.
  `OverlayChrome` ist ein `VerticalStackLayout` **ohne
  `VerticalOptions`** → Default `Fill` → der Container belegt das
  gesamte Grid über dem `MediaElement` und fängt die Eingaben ab,
  obwohl nur das obere `Border`-Kopfband (Titel, „Angesehen",
  Mini-Player-Schalter) sichtbaren Inhalt zeigt.
- `PlayerPage.xaml.cs`: `UpdateUi` blendet `OverlayChrome` im
  Mini-Player aus (`IsVisible = !player.IsPictureInPicture`) — deshalb
  funktioniert die Bedienung dort. Der `PipButton` dispatcht
  `PictureInPictureRequestedMessage`; Zurück-Navigation kommt von der
  Shell (die Seite liegt auf dem Navigationsstack).
- Keine weiteren Overlays auf der Videofläche: `LoadingIndicator` ist
  an `IsResolving`/`IsBuffering` gebunden, `ErrorPanel` nur bei
  Fehlern sichtbar.
- Test-Ausgangszustand: Volle Suite `dotnet test
  VideoWebPlayer.Maui.Tests` — **668/668 bestanden, 0 fehlgeschlagen**
  (Nachweis: [inventory/tests.md](inventory/tests.md) und
  [test-results/baseline-dotnet-test.log](inventory/test-results/baseline-dotnet-test.log)).
  Es existieren keine Rendering-/E2E-Tests; die Player-Schicht wird
  über Quelltext-Contract-Tests (`PlayerContractTests`) gepinnt —
  etablierte Projektkonvention.

## Details

- [Wiedergabeseite (UI und Code-Behind)](inventory/player-page.md)
- [Tests](inventory/tests.md)
