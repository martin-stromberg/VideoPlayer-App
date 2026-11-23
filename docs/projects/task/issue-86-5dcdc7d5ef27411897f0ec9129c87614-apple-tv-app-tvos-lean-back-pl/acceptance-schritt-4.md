# Abnahmeprüfung – Entwicklungsschritt 4

## Ergebnis

**Status:** Anforderung erfüllt – externe Verifikation ausstehend

## Ausstehende externe Verifikationen

- **macOS-CI-Build `build-tvos` (`net10.0-tvos`):** Das tvOS-Ziel
  kompiliert unter Windows nicht (`VideoPlayer.TvOS.csproj` Z. 12-14,
  36-42 — degradiert zur leeren Bibliothek); der CI-Job existiert in
  `.github/workflows/pr-checks.yml`, ist aber noch nie gelaufen (kein
  Push). Erst der Lauf beweist, dass keine weiteren tvOS-spezifischen
  Kompilierfehler (Binding-Mitglieder, UIKit-Signaturen) vorliegen —
  die Klasse hat in Schritt 3.1 (CS1061) und Schritt 4.1 (CS1739)
  bereits zwei echte Funde geliefert.
- **Laufzeitverhalten auf tvOS-Simulator/-Gerät:** Durchgängige
  Fokussierbarkeit und Scrollbarkeit der Karussells per Siri Remote,
  nachvollziehbare Fokusführung zwischen Reihen und Einträgen sowie
  tatsächliche Befüllung mit echten Serverdaten sind auf Code-Ebene
  verifiziert (Fokus-Engine-Muster, `RemembersLastFocusedIndexPath`,
  `CanBecomeFocused`, Cache-First-Lesepfad), nicht per Laufzeitnachweis.
- Beide Punkte sind im Verifikationsregister
  `pending-verification.md` (Zeilen „1–laufend" und „2–laufend")
  bereits erfasst und bleiben dort bis zum Nachweis bestehen.

## Hinweise

Prüfung der Nachbesserungsrunde 2 anhand des Fix-Commits `06a384f`
(2 Dateien: `VideoPlayer.TvOS/Ui/HomePageViewController.cs` ±4 Zeilen,
`VideoPlayer.Maui.Tests/TvOsCompileContractTests.cs` +161 Zeilen) sowie
Stichproben der Schritt-4-Quelldateien gegen die plattformneutralen
Deklarationen im Ist-Stand.

**(a) Runde-1-Befund verifiziert behoben:** Alle vier Aufrufstellen in
`HomePageViewController.BuildSections` verwenden jetzt
`IsContinueWatching:` (Z. 188, 195, 202, 211) — exakt der Fall des
positionalen Record-Parameters `bool IsContinueWatching`
(`HomeSectionDescriptor`, Z. 308). Repo-weite Suche nach
`isContinueWatching` findet außer Prosa/Historie keine Stelle mehr in
tvOS-Code. Der neue Contract-Test
`NamedArguments_MatchRecordParameterCase`
(`TvOsCompileContractTests.cs` Z. 91-116) sammelt die Parameter aller
positionalen Records des tvOS-Projekts und seiner Projektreferenzen und
schlägt an, wenn ein benanntes Argument case-insensitiv passt, aber im
Fall abweicht — er deckt die CS1739-Fehlerklasse generisch (nicht nur
den Einzelfund) ab und lief grün im Testlauf.

**(b) Stichprobe tvOS-Kompilierfehler-Klassen — keine weiteren Befunde:**
Benannte Argumente in den Schritt-4-Dateien: nur `IsContinueWatching:`
(korrekt); in Alt-Dateien `secure:` (`TvosControls.TextField(string
placeholder, bool secure = false)` — passt) und
`backupRoot:`/`noBackupRoot:`/`cacheRoot:`/`profile:`
(`AppStorageLayout`-Konstruktor Z. 30 — identische Schreibweise).
Member-Verwendungen gegen Deklarationen gegengeprüft:
`model.{ContinueWatching,Favorites,RecentEntries,SeasonalGenres,IsOffline,StorageNotice,Player}`
(`AppModel.cs` Z. 44, 123-144, 212, 238), `HomeMediaItem.{Title,
PosterPictureId,Progress,IsSelectable,CardOpacity,ShowProgress}`
(`HomeMediaItem.cs` Z. 24-85), `HomeSeasonalSection.{GenreId,Name,
ExpiresAt,Items,AutomationId}` (`HomeSeasonalSection.cs` Z. 7-15),
`details.Title` (`NavigateToMediaDetailsEffect`, `AppEffect.cs` Z.
196-202), `authSession.Client` (`AuthSessionService.cs` Z. 97),
`client.GetPictureAsync` (`VideoPlayerApiClient`, belegt u. a. in
`VideoPlayerApiClientTests_Endpoints.cs`), `layout.PreviewsDirectory`
(`AppStorageLayout.cs` Z. 72), `MainThreadInvoker.InvokeOnMainThread`
(`MainThreadInvoker.cs` Z. 41), `TvosControls.{Title,Heading,Body,
Section,Button}` (`TvosControls.cs` Z. 16-114), `MainViewController.
{Show,ShowLoading,UpdateLoadingStatus}` (`MainViewController.cs` Z.
26-56). Basistyp-/Member-Verwechslungen: `content` ist `UIStackView?`
deklariert (`AddArrangedSubview` zulässig), `HomeCarouselView : UIView`
nutzt nur UIView-Member, `HomeCarouselSource : UICollectionViewSource`
überschreibt vorhandene Bindings (`GetItemsCount`, `GetCell`,
`ItemSelected`); `ITimer`/`Timeout` lösen über aktivierte
ImplicitUsings (`System.Threading`) auf, `TimeProvider.CreateTimer` ist
BCL. Messages (`MediaItemSelectedMessage`,
`ContinueWatchingItemSelectedMessage`, `HomeListsReloadRequestedMessage`)
existieren als Records in `AppMessage.cs` Z. 215-264.

**(c) Fachliche Vollständigkeit ungebrochen:** Der Fix-Diff ist minimal
und rein syntaktisch (vier benannte Argumente + ausschließlich additive
Testdatei) — keinerlei Änderung an der in Runde 1 verifizierten
Funktionalität: drei Pflicht-Karussells plus saisonale Sektionen
modellgetrieben aus `AppModel`, leere Listen ausgeblendet
(`BuildSections` Z. 181-216); horizontale `UICollectionView`-Karussells
mit `RemembersLastFocusedIndexPath` und fokushervorhebender Skalierung
(`HomeCarouselView`/`HomeMediaItemCell`); Cache-First über
`HomeListService`/`OfflineContinueWatchingService` und
`LoadHomeListsEffect`; Poster-Nachladen mit Titel-Fallback
(`TvosPosterImageService`/`HomeMediaItemCell.Update`); Auswahl dispatcht
auf den modalen `PreparedTargetViewController` (kein Fehlerpfad) über
die drei Effekt-Mappings in `TvosAppEffectHandler`; Aktualisierung bei
Server-Ereignissen, `ViewWillAppear` und Vordergrund-Rückkehr sowie
saisonalem Ablauf-Timer; Offline-Banner; kein Schreibzugriff
(`TvOsEffectHandler_DoesNotWireContentMutations`). Keine
Unstimmigkeiten festgestellt.

**(d) Tests:** `dotnet test VideoPlayer.Maui.Tests` — **1472 bestanden,
0 Fehler, 0 übersprungen** (net10.0, ~1,5 min), inkl. der neuen
`TvOsHomePageContractTests` (9 Tests) und
`NamedArguments_MatchRecordParameterCase`. Die NU1903-Warnung
(`Microsoft.AspNetCore.SignalR.Common` 10.0.10) ist altbekannt und
schrittunabhängig.

**Einschränkungen:** Die finale tvOS-Lauffähigkeit bleibt dem
macOS-CI-Build/Simulator vorbehalten (siehe ausstehende externe
Verifikationen); der Contract-Test mindert das Restrisiko der
CS1739-Klasse, kann aber andere Binding-/Signaturfehler nicht
ausschließen.
