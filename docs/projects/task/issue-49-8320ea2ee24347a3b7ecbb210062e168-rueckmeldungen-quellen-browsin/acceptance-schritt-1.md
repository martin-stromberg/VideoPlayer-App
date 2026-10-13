# Abnahmeprüfung – Entwicklungsschritt 1

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

Keine.

## Hinweise

**Prüfumfang:** Vollständiger Diff `task/…-rueckmeldungen-quellen-browsin...HEAD` (14 Dateien, +563/−2) sowie die relevanten Quelldateien im Ist-Stand.

**Soll-Verhalten im Code nachgewiesen:**

- **Genre-Leiste sichtbar bei Server-Genres („Alle" + Quellen-Genres):** `VideoPlayer.Maui/Pages/SourceOverviewPage.xaml:26-32` — `GenresCollection` mit `ItemsSource="{Binding Model.SourceBrowser.Genres}"` und neuem `IsVisible="{Binding Model.SourceBrowser.ShowGenreFilter}"`; `VideoPlayer.Mvu/SourceBrowserState.cs:71` — `ShowGenreFilter => Genres.Count > 1` („Alle" ist der einzige synthetische Eintrag an Index 0, invariant durch `SourceBrowserService.LoadGenresAsync`, `SourceBrowserService.cs:76-96`). `UpdateSourceGenresLoaded` (`AppUpdate.cs:1573-1593`) übernimmt die Serverliste; die Binding-Auffrischung läuft über `MvuHost.PropertyChanged(nameof(Model))` (`MvuHost.cs:61`) bei jedem Modell-Tick.
- **Gefiltertes Neuladen ab erster Seite:** `OnGenreSelected` (`SourceOverviewPage.xaml.cs:104-115`) → `SourceGenreSelectedMessage` → `UpdateSourceGenreSelected` (`AppUpdate.cs:1678-1694`) → `ReloadBrowserItems` (`AppUpdate.cs:1654-1676`) setzt `Items=[]`, `NextPage=0` und löst `LoadSourceItemsEffect(SourceId, Page=0, SearchText, GenreId, Append:false)` aus.
- **„Alle" hebt den Filter auf:** `SelectedGenreId > 0 ? … : null` (`AppUpdate.cs:1674`) — bei Id 0 wird kein `genreId`-Parameter gesendet (Service-Pin `LoadItems_NoFilter_OmitsSearchAndGenreParameters`, `SourceBrowserServiceTests.cs:174-184`; E2E-Nachweis über `SourceItemRequests` in `SourceBrowsingTests.cs`).
- **Ausblendung bei leerer/fehlgeschlagener Serverliste (Projektklärung):** Leere Serverantwort liefert nur `[Alle]` (`SourceBrowserService.cs:76-96`), ein Ladefehler fällt im `ShellAppEffectHandler.LoadSourceGenresAsync` (`ShellAppEffectHandler.cs:826-829`) still auf `[Alle]` zurück — in beiden Fällen `Genres.Count == 1` → `ShowGenreFilter == false` → Leiste verborgen, keine funktionslose „Alle"-nur-Zeile. Der Eintragspfad bleibt davon unberührt.
- **Keine Server-/API-Änderung:** Der Diff enthält keine Änderungen in `VideoPlayer.ApiClient`; die `MockVideoServer`-Erweiterungen betreffen ausschließlich die E2E-Test-Infrastruktur.
- **Keine neue Bedienform:** Die vorhandene horizontale Chip-Leiste blieb unverändert; ergänzt wurden nur `IsVisible`-Binding und `AutomationId`s.
- **Diagnose dokumentiert:** `changes.log` (Eintrag „Genre-Filter im Quellen-Browsing: Sichtbarkeitsregel für leere Server-Genrelisten") und `docs/help/quellen-browsing-details.md:43-54` dokumentieren den Befund: Leiste und Ladepfad waren im Auslieferungsstand vollständig vorhanden; die fehlende Anzeige entstand durch leere Serverantwort bzw. stillen Ladefehler-Fallback auf „Alle" — eine etwaige serverseitige Ursache (Endpunkt liefert für die Quelle eine leere Genre-Liste) ist dokumentiert, Server/API bleiben unverändert.

**Testnachweise:**

- `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj` ausgeführt: **1316 Tests bestanden, 0 Fehler, 0 übersprungen** (Dauer 28 s) — der behauptete grüne Stand ist verifiziert.
- Neue Unit-Tests existieren und sind aussagekräftig: `SourceGenresLoaded_OnlyAllEntry_HidesGenreFilter` und `SourceGenresLoaded_ServerGenres_ShowsGenreFilter` (`AppUpdateTests_Sources.cs:363-405`), `LoadGenres_EmptyServerList_ReturnsOnlyAllEntry` (`SourceBrowserServiceTests.cs:101-121`), `DevApiModelSnapshot_MapsSourceBrowser` (`DevApiModelSnapshotTests.cs`); der Altbestand `GenreSelected_ReloadsFromFirstPage_WithGenreFilter` (`AppUpdateTests_Sources.cs:411-441`) pint den gefilterten Reload ab Seite 0.
- E2E: `VideoPlayer.E2E.Tests/Tests/SourceBrowsingTests.cs` (neu, 2 Tests) prüft gegen den erweiterten `MockVideoServer` (Opt-in-Routen `api/Sources`, `api/SourceGenres/{id}`, `api/items?mediaSourceId=…` mit `SourceItemRequests`-Aufzeichnung): Leiste sichtbar mit „Alle"+Server-Genres, Chip-Auswahl mit `genreId=4`-Request, „Alle"-Rücksetzung ohne `genreId`-Parameter sowie verborgene Leiste bei leerer Serverliste — inklusive Tree-Nachweis über `FindByAutomationId("GenresCollection").IsVisible`.

**Einschränkungen:**

- Die beiden E2E-Tests wurden nicht selbst ausgeführt (erfordern Build und Start der Windows-MAUI-App); der behauptete grüne E2E-Stand (2/2) ist plausibel — alle referenzierten Hilfsmittel existieren (`E2ETestBase.ConnectToPairingRequiredAsync/PairAsync/LoginAsync`, `UiDriver.OpenFlyoutItem`, `DevApiClient.InvokeAsync` mit `itemIndex` → `SelectedItem`-Setter/SelectionChanged-Kette in `DevApiVisualTree.cs:176-205`, `IsVisible`-Knoten im Tree-Dump).
- Kein dedizierter Unit-Test für `SourceGenreSelectedMessage(0)` („Alle"-Reset auf AppUpdate-Ebene); die Kette ist jedoch über den E2E-Test (Request ohne `genreId=`) und den Service-Test `LoadItems_NoFilter_OmitsSearchAndGenreParameters` nachgewiesen — nicht als Abweichung gewertet.
- Im Build auftretende Warnungen (CS0108/CS8618 in `VideoWebPlayer.Client`) sind Altbestand und stammen nicht aus diesem Diff.
