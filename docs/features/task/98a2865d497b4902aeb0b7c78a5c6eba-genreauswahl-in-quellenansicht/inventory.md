# Bestandsaufnahme: Genreauswahl in Quellenansicht verhindert die Einsicht der Titel

Analysiert wurde die Quellen-Browsing-Seite (`SourceOverviewPage`) mit ihrer horizontalen Genre-Leiste sowie die zugehörigen MVU-Zustände, die Developer-API (Elementbaum mit Bounds) und die bestehenden Unit-/E2E-Tests — bezogen auf die Anforderung, dass die Genre-Buttons unter iOS die gesamte Seitenhöhe belegen und die Titelliste verdecken.

## Zusammenfassung

- Die Genre-Leiste ist eine `CollectionView` (`GenresCollection`) mit `LinearItemsLayout Orientation="Horizontal"` in einer `Auto`-Grid-Zeile — **ohne Höhenbegrenzung**. Unter iOS/iPadOS dehnt die `CollectionView` sich über die angebotene Resthöhe aus; die Chip-`Border` (Standard-`VerticalOptions` = Fill) strecken sich auf die volle Zellhöhe → bildschirmhohe Pillen, Titelliste (`ItemsCollection`, Zeile `*`) wird verdrängt (Fehlerbild des TestFlight-Screenshots).
- Im Projekt existiert ein bewährtes alternatives Muster für horizontale Listen: `MediaCarousel` nutzt `ScrollView Orientation="Horizontal"` + `HorizontalStackLayout` mit `BindableLayout.ItemsSource` + `TapGestureRecognizer` pro Eintrag.
- Die Developer-API (`GET /tree`) liefert serverseitig bereits `Bounds` (X/Y/Width/Height) je Element; das Test-DTO `DevApiTreeNode` (E2E-Projekt) bildet `bounds` noch **nicht** ab. `POST /invoke` mit `itemIndex` unterstützt neben `CollectionView` auch `BindableLayout`-Container (`TrySelectBindableLayoutItem` → `TapGestureRecognizer.SendTapped`) — ein Umbau der Genre-Leiste auf das Karussell-Muster bleibt damit E2E-steuerbar.
- Die Chip-Optik (`Chip`-Style, `IsSelected`-Trigger) und das Auswahlverhalten (`SourceGenreSelectedMessage` → gefilterter Reload) sind vorhanden und müssen erhalten bleiben; Contract-Tests pinnen `{Binding Model.SourceBrowser.Genres}` und `{StaticResource Chip}` in `SourceOverviewPage.xaml`.
- Fehlende Teile: keine Höhenbegrenzung/kein alternatives Layout der Genre-Leiste; kein `bounds`-Feld im E2E-Baum-DTO; kein Test, der die belegte Höhe der Genre-Leiste prüft.

Test-Ausgangszustand: Unit-Suite `VideoPlayer.Maui.Tests` zweimal ausgeführt — Lauf 1 meldete 1249 erfolgreiche Tests, endete aber mit einem Prozessabsturz beim Teardown (Exit-Code 1, xUnit „Catastrophic failure … -1073741819"); der Wiederholungslauf (`--no-build`) war sauber: 1354 erfolgreich, 0 fehlgeschlagen, Exit-Code 0. Der Absturz von Lauf 1 ist damit als Flake beim Prozessende einzuordnen, kein Testfehler. E2E-Suite (FlaUI, ~25 Tests) wurde im Ausgangszustand nicht ausgeführt (Laufzeit ~20–25 min, Desktop-Session erforderlich) — Details in [Tests](inventory/tests.md).

## Details

- [UI-Layout](inventory/ui.md) — `SourceOverviewPage.xaml`, `Chip`-Style, Vergleichsmuster `MediaCarousel`
- [Datenmodell](inventory/models.md) — `SourceBrowserState`, `SourceGenreItem`, `SourceMediaItem`
- [Logik](inventory/logic.md) — Seiten-Code-Behind, `AppUpdate`-Handler, `SourceBrowserService`, Developer-API
- [Tests](inventory/tests.md) — Test-Ausgangszustand, bestehende Unit-/Contract-/E2E-Tests
