# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### PlayerPage.xaml.cs (PlayerPage)

- **Hardcodierte Werte** — Zeile 61: `PlaylistNavigationBar.Margin = new Thickness(0, 96, 16, 0)` verwendet die Magic Number `96` direkt im `#if IOS`-Block. Die Klasse führt andere abstimmbare Werte bereits als benannte Konstanten (`ProgressTickInterval`, `ResumeSeekTimeout`, Zeilen 23–24) — der neue Wert weicht von dieser Konvention ab und ist ohne den Kommentar nicht selbsterklärend.

  Empfehlung: Als benannte Konstante der Klasse auslagern, z. B. `private static readonly Thickness IosPlaylistNavigationBarMargin = new(0, 96, 16, 0);` (oder zumindest den Top-Anteil `96` als `const double`), damit die Feinjustage aus der geplanten Geräteprüfung (plan.md, „Offene Punkte") an einer benannten Stelle erfolgt.

### PlayerContractTests.cs (PlayerContractTests)

- **Doppelter Code** — `PlayerPage_PlaylistNavigationBarClearsNativeControlsOnIos` (Zeilen 152–184) wiederholt ~12 Zeilen Boilerplate aus `PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface` (Zeilen 103–140): `XDocument.Parse(Read(...))`, die beiden `XNamespace.Get`-Aufrufe sowie die `PlayerRoot`-/`OverlayChrome`-Lookups per `Descendants`/`SingleOrDefault`. Mit dem neuen Test existiert dieselbe Suchlogik nun mehrfach identisch in der Datei.

  Empfehlung: Hilfsmethoden extrahieren, z. B. `private static XDocument PlayerPageXaml()` und `private static XElement? Named(XContainer parent, XName element, string name)` — die bestehenden Tests können denselben Helper nutzen.

## Geprüfte Dateien

Liste aller geprüften Dateien:
- `VideoPlayer.Maui/Pages/PlayerPage.xaml`
- `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`
- `VideoWebPlayer.Maui.Tests/PlayerContractTests.cs`
