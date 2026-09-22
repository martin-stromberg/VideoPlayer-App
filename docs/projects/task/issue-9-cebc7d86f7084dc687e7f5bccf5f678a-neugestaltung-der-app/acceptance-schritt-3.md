# Abnahmeprüfung – Entwicklungsschritt 3

Prüfgegenstand: `git diff task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app...HEAD` (Commit `8752e81`), geprüft gegen die Schritt-Anforderung „Player-Steuerung in der Vollansicht bedienbar" (native Transportsteuerungen klickbar ohne Mini-Player-Umweg; Zurück-/Mini-Player-Schalter und Mini-Player unverändert), nicht gegen die Lifecycle-Artefakte.

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

(keine)

## Hinweise

**Ursache und Fix (a):** `OverlayChrome` liegt im selben Wurzel-`Grid` wie das `MediaElement` (`PlayerPage.xaml:10-82`) und steht in der Deklarationsreihenfolge hinter ihm (`MediaElement` Z. 13, `ActivityIndicator` Z. 23, `OverlayChrome` Z. 35, `ErrorPanel` Z. 70) — also oberhalb in der Z-Ordnung. Der Ausgangsstand (`git show <basis>:...PlayerPage.xaml` verifiziert) hatte kein `VerticalOptions` am `VerticalStackLayout` → Default `Fill` → das Layout belegte das gesamte Grid und fungierte als Hit-Test-Blocker über der Videofläche; die Inventur-Ursache ist damit plausibel belegt. Der Fix (`PlayerPage.xaml:36`): `VerticalOptions="Start"` schrumpft das Overlay auf das sichtbare Kopfband; zusätzlich ist das Kopfband-`Border` `InputTransparent="True"` (Z. 39) und der `PipButton` explizit `InputTransparent="False"` (Z. 63) — das kanonische MAUI-Muster „transparenter Container, eingabefähiges Kind". Selbst falls die `InputTransparent`-Vererbung plattformspezifisch abwiche, trägt `VerticalOptions="Start"` die Korrektur allein; die Transportsteuerung des `MediaElement` sitzt unter Windows am unteren Rand und liegt in keinem Fall im Kopfband.

**Weitere Eingabe-Blocker geprüft (Nebenwirkungen):** Der `ActivityIndicator` (Z. 23-28) ist nur während `IsResolving`/`IsBuffering` sichtbar und 48×48 mittig — klein, transient, unverändert zum Ausgangsstand. Das `ErrorPanel` (Z. 70-80) steht `VerticalOptions="End"`, `IsVisible="False"` und wird nur bei `HasError` eingeblendet (`PlayerPage.xaml.cs:114`) — im Fehlerfall gibt es ohnehin keine Wiedergabe zu bedienen. Ein `TapGestureRecognizer` existiert auf der `PlayerPage` nicht (Grep: solche Recognizer nur in `MediaCarousel`, `TVShowDetailsPage`, `HomePage`, `MediaBannerPlayer` — alles andere Dateien). Das Code-Behind `PlayerPage.xaml.cs` ist im Diff unverändert.

**Mini-Player-Schalter und Zurück (b):** `PipButton` behält `Clicked="OnPipClicked"`; der Handler dispatcht unverändert `PictureInPictureRequestedMessage` (`PlayerPage.xaml.cs:175-178`), die Sichtbarkeit kommt weiter von `pictureInPicture.IsToggleSupported` (`:37`). Die explizite `InputTransparent="False"`-Ausnahme ist notwendig und korrekt gesetzt. Zurück-Navigation ist Shell-Angelegenheit (`AppShell.xaml.cs:75-77` registriert Route „player", `ShellAppEffectHandler.cs:210` pusht auf den Stack) — die Zurück-Schaltfläche sitzt in der Shell-Navigationsleiste oberhalb des Seiten-Grids und ist vom Overlay unberührt.

**Mini-Player unverändert (c):** Im kompakten Overlay wird `OverlayChrome` komplett ausgeblendet (`PlayerPage.xaml.cs:117`: `IsVisible = !player.IsPictureInPicture`) — die Layout-Änderung kann im Mini-Player definitionsgemäß nicht wirken. PiP-Dienst und Effekt-Handler kommen im Diff nicht vor.

**Contract-Test-Echtheit:** `PlayerContractTests.PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface` (`PlayerContractTests.cs`, Diff +35 Zeilen) parst die echte XAML-Datei via `XDocument.Parse(Read("VideoPlayer.Maui","Pages","PlayerPage.xaml"))`, selektiert `OverlayChrome` per `x:Name` und pinnt alle drei Fix-Facetten: `VerticalOptions="Start"`, `Border` `InputTransparent="True"`, `PipButton` `InputTransparent="False"` plus `Clicked="OnPipClicked"`. Logische Mutationsprüfung: Beim Revert des XAML-Fixes wäre das `VerticalOptions`-Attribut nicht vorhanden → `Assert.Equal("Start", null)` schlägt fehl; ebenso würden die beiden `InputTransparent`-Assertions ohne die Attribut-Ergänzungen fehlschlagen. Der Test pinnt die Korrektur also tatsächlich, keine Tautologie. Die vom Implementierer dokumentierte Negativprobe auf dem Ausgangsstand (`plan.md`) ist mit dieser Analyse konsistent.

**Build/Tests:** `dotnet test VideoWebPlayer.Maui.Tests -c Debug` selbst ausgeführt: **669/669 grün, 0 Fehler, 0 übersprungen**, ~8 s (Baseline Schritt 2: 668 → +1 = genau der neue Contract-Test; die Debug-Buildstufe lief dabei fehlerfrei mit). `dotnet build VideoPlayer.App.sln -c Release` selbst nachgebaut: **0 Fehler**, 339 Warnungen (Altbestand, u. a. die bereits in Schritt 2 bekannten XA4301-`libe_sqlite3`-APK-Duplikate und CS0618-Bestandswarnungen in `VideoWebPlayer.Maui`).

**DoD-Nebenpunkte:** Kein Storage-/DB-Diff → keine EF-Migration und keine `VideoWebPlayerBackupData`-Erweiterung nötig; keine neuen NuGet-Pakete; keine neuen Berechtigungsprüfungen relevant. Doku aktualisiert: `docs/help/wiedergabe-pip.md` (neuer Abschnitt „Bedienung in der Vollansicht"), `README.md`, `docs/RELEASE_NOTES.md` (deutsch + englisch), `changes.log`. Der Diff ist vollständig auf den Schritt-Umfang beschränkt (XAML, Contract-Test, Lifecycle-Artefakte, Doku); Änderungen außerhalb des Auftrags wurden nicht festgestellt.

**Prozess-/Restrisiko-Notiz:** Die tatsächliche Klickbedienbarkeit auf einem Windows-Gerät ist wie in den Vor-Schritten nicht automatisierbar abgedeckt — abgesichert über die Layout-Semantik (`VerticalOptions="Start"` → Overlay-Bounds nur noch Kopfband) plus das gepinnte `InputTransparent`-Muster; ein UI-Klicktest auf Gerät bleibt der Endabnahme vorbehalten. `handover.md` existiert weiterhin nicht im Projektordner (AGENTS.md §2), wie schon in den Schritten 1/2. Arbeitsbaum sauber; einzige untracked Datei ist die fremde `docs/features/task/issue-9-customer-feedback.md` — unverändert, nicht angefasst (§8).
