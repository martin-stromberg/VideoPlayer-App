# Tests — Bestandsaufnahme

## Lokale Suite

- `dotnet test VideoPlayer.Maui.Tests` (xunit.v3, `net10.0`) — muss grün bleiben; die Suite enthält die plattformneutralen MVU-/Service-Tests und die tvOS-Quell-Contract-Tests.
- `net10.0-tvos` ist unter Windows nicht kompilierbar (`IncludeTvOsTarget=false` → leere `net10.0`-Hülle) — der tvOS-Kompiliernachweis läuft über den macOS-CI-Job (nach Push, nicht Teil dieses Schritts).

## tvOS-Contract-Test-Muster

- `TvOsAppContractTests`, `TvOsConnectionContractTests` (`VideoPlayer.Maui.Tests/`): lesen tvOS-Quelldateien via `RepositoryRoot.Find()` + `File.ReadAllText` und prüfen `Assert.Contains`/`Assert.DoesNotContain` auf relevante Verdrahtung (Messages, Effekte, UIKit-Typen, Dienste). Helfer `Stubs/RepositoryRoot.cs`.
- `TvOsCompileContractTests`: scannt **alle** `VideoPlayer.TvOS/**/*.cs` (außer bin/obj):
  - `UIView`-deklarierte Variablen dürfen keine `UIStackView`-Member (`Spacing`, `AddArrangedSubview`) nutzen — Regex `\bUIView\??\s+(\w+)\s*[;=]`; auch `UIView[]`-Deklarationen wie `params UIView[] children` treffen den Regex nicht (Typ ist `UIView[]`, aber `UIStackView`-Member dort wären unzulässig — Vorsicht bei `Section(params UIView[] ...)`-Aufrufen mit `.Spacing` auf dem Ergebnis ist ok, das Ergebnis ist `UIStackView`).
  - `MenuPages.IndexOf` verboten.
- `EnumCoverageTests`: bei neuen Enum-Werten Tests pflegen — keine neuen Enums geplant.

## Bekannte Binding-Fallen (Abnahme Schritt 3)

- Basistyp-Rückgaben: `UICollectionView.DequeueReusableCell` → `UICollectionViewCell` (Cast zum abgeleiteten Zellentyp), `presses.ToArray<UIPress>()` für `NSSet<UIPress>`.
- Nicht-existierende Member wurden in der Abnahme gefunden — neue tvOS-Quellen streng gegen die .NET-tvOS-Bindings schreiben (`UICollectionViewSource`, `RegisterClassForCells(Type, NSString)`, `GetCell`/`NumberOfSections`/`GetItemsCount`-Überschreibungen, `DidUpdateFocus(UIFocusUpdateContext, UIFocusAnimationCoordinator)`, `CanBecomeFocused`, `ItemSelected`-Callback, `UIProgressView`, `UIImageView`, `UIImage.LoadFromData`).

## Für Schritt 4 zu ergänzen

- `TvOsHomePageContractTests` (neu): Existenz und Verdrahtung von `HomePageViewController`, `HomeMediaItemCell`, Karussell-DataSource, `TvosPosterImageService`, Prepared-Target-Präsentation, Effekt-Verdrahtung der drei Navigations-Effekte, Nicht-Verdrahtung der Mutations-Effekte.
