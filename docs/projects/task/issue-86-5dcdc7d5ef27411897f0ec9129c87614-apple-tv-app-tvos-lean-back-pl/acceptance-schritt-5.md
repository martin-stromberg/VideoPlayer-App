# Abnahmeprüfung – Entwicklungsschritt 5

## Ergebnis

**Status:** Anforderung erfüllt – externe Verifikation ausstehend

## Ausstehende externe Verifikationen

- **Laufzeitverhalten auf tvOS-Simulator/-Gerät:** Fokussierbarkeit des
  Eintrags-Rasters und der Genre-Leiste per Siri Remote, tatsächliches
  Nachladen am Listenende, der Sucheingabefluss über die
  tvOS-Systemtastatur (Diktat/Continuity-Tastatur) sowie die Befüllung
  mit echten Serverdaten sind auf Code-Ebene verifiziert, nicht per
  Laufzeitnachweis. Ebenso ist das Verhalten des modalen
  Präsentationsstapels (Browser-Seite → vorbereitetes Ziel →
  Menü-Taste zurück) nur statisch geprüft. Im Verifikationsregister
  `pending-verification.md` (Zeile „2–laufend") erfasst.
- Der macOS-CI-Nachweis selbst ist erbracht: Run `42116343319`
  (`build-tvos` + `verify` grün) auf `headSha` `54dc140` = aktueller
  HEAD; `git status` sauber (nur untracked `.work/`), keine
  ungepushten Commits (`git log origin/<branch>..HEAD` leer).

## Hinweise

Prüfung anhand des Diffs `task/issue-86-...-apple-tv-app-tvos-lean-back-pl...HEAD`
(17 Dateien, +1347/−29) und Lesen der Quelldateien samt der
plattformneutralen MVU-Gegenseite.

**(a) Quellenauswahl verdrahtet:** `HomePageViewController` hat eine
Quellen-Sektion mit `SourceCarouselView` (Z. 107–129); Auswahl
dispatcht `SourceSelectedMessage` (Z. 328–329) → `UpdateSourceSelected`
(`VideoPlayer.Mvu/AppUpdate.cs` Z. 1581–1594) emittiert
`NavigateToSourceOverviewEffect` + `LoadSourceGenresEffect` +
`LoadSourceItemsEffect` → `TvosAppEffectHandler` Z. 162–171 ruft
`navigator.PresentSourceBrowser` auf dem Hauptthread →
`TvosNavigator.PresentSourceBrowser` (Z. 112–138) präsentiert
`SourceBrowserViewController` modal FullScreen mit Duplikat-Guard. Der
Wiederherstellungspfad `SourceBrowserOpenedMessage` in `ViewWillAppear`
(Z. 185–192) ist im Update als No-Op bei gleicher Quelle abgedeckt
(`AppUpdate.cs` Z. 1597–1619).

**(b) Genre-Leiste inkl. „Alle":** horizontale `UICollectionView` mit
`SourceGenreCell` (Z. 90–111); „Alle"-Eintrag (Id=0) liefert
`SourceBrowserService.LoadGenresAsync` (`VideoPlayer.Mvu/SourceBrowserService.cs`
Z. 74–99), Sichtbarkeit über `ShowGenreFilter` (`SourceBrowserState.cs`
Z. 71). Auswahl → `SourceGenreSelectedMessage` → Reload ab Seite 0
(`AppUpdate.cs` Z. 1726–1742 via `ReloadBrowserItems`).

**(c) Fokussierbares Raster mit Titel+Poster:** vertikale
`UICollectionView` (Z. 113–136) mit `HomeMediaItemCell` (Poster +
Titelzeile, `CanBecomeFocused`, Fokus-Vergrößerung); Karten werden aus
`SourceMediaItem` (Title, PosterPictureId) belegt (Z. 376–391).

**(d) Nachladen weiterer Seiten:** `WillDisplayCell` fordert ab
`items.Count - NextPageLookahead` die Folgeseite an (Z. 398–406) →
`SourceItemsNextPageRequestedMessage` → `UpdateSourceItemsNextPageRequested`
(`AppUpdate.cs` Z. 1756–1772) dedupliziert über `HasMore`/`IsLoading`/
`IsLoadingMore` und lädt `Append: true`; veraltete Anhänge-Antworten
werden per `NextPage`-Vergleich verworfen (Z. 1670–1675).

**(e) Suche:** `UITextField` mit `EditingDidEnd` →
`SourceSearchSubmittedMessage` (Z. 85–88); Eingabe läuft über die
tvOS-Systemtastatur (Diktat/Continuity systemseitig). Update setzt
Liste zurück und lädt Seite 0 mit `SearchText` (`AppUpdate.cs`
Z. 1744–1754 → `ReloadBrowserItems` Z. 1702–1724). Text bleibt
sichtbar: `ApplySearchText` schreibt nur zurück, wenn das Feld nicht
`IsFirstResponder` ist und der Modell-Text sich änderte
(`mirroredSearchText`-Guard, Z. 245–257).

**(f) Treffer-Auswahl → vorbereitetes Ziel:** `ItemSelected` →
`SourceMediaItemSelectedMessage` → `NavigateToMediaDetailsEffect`
(`AppUpdate.cs` Z. 1789–1800) → `PresentPreparedTarget` mit
`PreparedTargetViewController` (`TvosNavigator.cs` Z. 154–176) —
Platzhalter ohne Fehler, wird bei Folgezielen aktualisiert statt
gestapelt; `NavigateToPlayerEffect`/`StartPlaylistPlaybackEffect`
landen auf demselben Muster.

**(g) Lade-/Leer-/Fehlerzustände mit Wiederholung:** `ApplyState`
(Z. 289–320) deckt `IsLoading`, `ShowEmpty`, `HasError` ab; Fehler
zeigen den fokussierbaren Button „Erneut versuchen" →
`SourceItemsRetryRequestedMessage` (`AppUpdate.cs` Z. 1774–1787:
Voll-Neulauf bei leerer Liste, Folgeseiten-Retry bei Bestand).
Nachlade-Hinweis über `IsLoadingMore`. Die Quellen-Sektion der
Startseite hat analoge Zustände mit `SourcesReloadRequestedMessage`
(`HomePageViewController.cs` Z. 279–324). Menü-Taste schließt modal
(Z. 195–209).

**(h) Kein Schreibzugriff:** `SourceBrowserService` nutzt ausschließlich
Get-Endpunkte (`GetSourcesAsync`, `GetSourceGenresAsync`,
`GetSourceItemsAsync`); `TvosPosterImageService` nur
`GetPictureAsync`/`GetSourceIconAsync`; alle nicht verdrahteten Effekte
laufen im `TvosAppEffectHandler` in ein protokolliertes No-Op
(Z. 210–215).

**(i) Tests:** `dotnet test VideoPlayer.Maui.Tests` — **1480/1480
bestanden, 0 Fehler** (47 s). Neue Contract-Tests
`TvOsBrowsingContractTests.cs` (7 Tests) prüfen Verdrahtung,
Suchtext-Guard, Zustände, Nachladen und Fokussierbarkeit.

**(j) Kompilierfallen-Stichprobe:** benannte Argumente in den neuen
Dateien haben korrekte Groß-/Kleinschreibung (`Append: false`,
`IsContinueWatching:`, `Force:`); Overrides zielen auf existierende
Basistyp-Member (`WillDisplayCell`, `ItemSelected`, `GetCell`,
`GetItemsCount` auf `UICollectionViewSource`; `CanBecomeFocused`,
`DidUpdateFocus`, `PrepareForReuse` auf `UICollectionViewCell`;
`PressesBegan` analog zu `PreparedTargetViewController`/`LoginViewController`);
`TvosControls.Status()` wird parameterlos passend zur Signatur
aufgerufen (Fix-Commit `54dc140` für CS1501). Endgültiger Nachweis:
`build-tvos` auf HEAD grün.
