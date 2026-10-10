# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan lückenhaft

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Das Starten eines Videos („Abspielen"-Schaltfläche Detailseite, ▶ Episodenliste) darf die Anwendung nicht beenden | Verzögerter Resume-Seek via `pendingResumePosition` + `TryApplyPendingResumeSeek`-Gate auf `Playing`/`Paused` (Programmablauf, Schritt 1) — adressiert die bestätigte Ursache (`MapSeekRequested` `async void` + `MediaOpened` vor `ReadyToPlay`) | E2E `PlaybackTests` „Wiedergabestart mit Fortsetzungsposition" + Contract-Test `PlayerPage_ResumeSeek_WaitsForReadyState`; manuelle iOS-Verifikation via TestFlight | Lücke (E2E-Klickmechanismus fehlt — siehe unten; Umsetzung selbst abgedeckt) |
| Fehler beim Wiedergabestart landen als `player.Error` im `ErrorPanel`, nicht als Prozessabbruch | try/catch um `UpdateUi`-Quellenblock, `ApplyResumeSeekAsync`, `ReportPosition` → `PlaybackFailedMessage` (Schritt 2) | E2E `PlaybackTests` „Stream-Fehlschlag → `playerError`/`ErrorPanel`, Prozess lebt" + Contract-Test `PlayerPage_NativeAccessesAreFailureIsolated` | Abgedeckt |
| Marshalling aller `VideoElement`-Zugriffe auf den UI-Thread | Bestehende Marshals bleiben; Gate nur aus UI-Thread-Marshals aufrufbar; `subscribed`-Guards bleiben | Bestandstest `PlayerPage_MarshalsNativeElementAccessOnMainThread` wird auf neue Struktur umgestellt | Abgedeckt |
| Validierung der aufgelösten `PlaybackSource.Location` vor `MediaSource` | Validierungsregeln: `ServerStream` → `Uri.TryCreate` http/https; `LocalFile` → nicht leer; ungültig → `PlaybackFailedMessage` (Schritt 2) | In `PlayerPage_NativeAccessesAreFailureIsolated` enthalten (Validierung vor `MediaSource.FromUri`) | Abgedeckt |
| App-seitiger Workaround bei Toolkit-Ausnahme zulässig | Gewählt (Paket-Upgrade ausgeschlossen: Fix #3281 in keinem NuGet-Release; 10.0.0 = neuestes) | s. o. | Abgedeckt |
| Symbolisierung des Reports (erster Untersuchungsschritt) | Nicht möglich — dSYMs von Build 213 nicht archiviert, kein macOS-Zugriff; im Plan nachvollziehbar begründet und geklärt | Nicht testbar; Ursache durch manuellen Resume-Test bestätigt | Abgedeckt (mit Begründung) |
| dSYMs jedes TestFlight-/Store-Builds archivieren (prozessseitig) | `release-ios-dsym.zip` in `package-ios`-Action, Upload in beiden Workflows, Export in `scripts/iOS-Deployment.ps1` (Schritt 4) | Kein automatisierter Test; tolerant implementiert (kein Fund → Warnung) | Abgedeckt |
| Regressionsschutz: Contract-Tests für vereinbarte Konventionen | Zwei neue Contract-Tests + Anpassung des Marshalling-Tests (Schritt 3) | `PlayerPage_ResumeSeek_WaitsForReadyState`, `PlayerPage_NativeAccessesAreFailureIsolated` | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

- [ ] Der Pflicht-E2E-Test „Wiedergabestart mit Fortsetzungsposition" ist wie beschrieben nicht ausführbar: Der Plan setzt voraus, dass `DevApiClient.InvokeAsync(automationId, itemIndex)` einen Eintrag der `MediaCarousel` „Neu im Programm" anklickt (plan.md „Änderungen an bestehenden Klassen" → `HomePage.xaml`). Tatsächlich akzeptiert `DevApiVisualTree.TrySelectItem` (`msTools.DeveloperApi.Maui/DevApiVisualTree.cs` :155–162) ausschließlich `CollectionView`; `MediaCarousel` ist ein `ContentView` mit `BindableLayout.ItemsSource` auf einem `HorizontalStackLayout` (`VideoPlayer.Maui/Controls/MediaCarousel.xaml` :14–18) und der Eintrags-Tap ist ein `Tapped`-Event ohne `Command` (`MediaCarousel.xaml` :36), sodass auch `TryInvoke` nicht greift. Es fehlt ein Umsetzungsschritt für den Item-Klickmechanismus (z. B. Erweiterung von `TrySelectItem`/`TryInvoke` für `BindableLayout`-Container inkl. `Tapped`-Auslösung, `AutomationId` je Eintrag + FlaUI-Klick, oder Umbau der `MediaCarousel` auf `CollectionView`) — inklusive des zugehörigen Test-/Contract-Nachweises für den Mechanismus.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Wiedergabestart mit Fortsetzungsposition (Resume — bestätigter Absturzpfad): Login → „Neu im Programm" → Details → „Abspielen" → Player aktiv, Position ≥ Weiterschauen-Stand, Prozess lebt | `PlaybackTests` (Windows) mit `playerIsActive`/`playerIsPlaying`/`playerPositionSeconds` via DevApi | Lücke — Szenario geplant, aber der nötige Item-Klick auf `MediaCarousel`-Einträge scheitert am `CollectionView`-Zwang von `TrySelectItem` (kein Mechanismus im Plan) |
| Stream-Fehlschlag beim Wiedergabestart → `player.Error`/`ErrorPanel` statt Absturz | `PlaybackTests` (Stream-Endpunkt → 404), `playerError` via DevApi | Abgedeckt (vom selben Mechanismus-Problem nur mittelbar betroffen: der Fluss bis „Abspielen" teilt die Klicklücke) |
| ▶-Einstieg aus der Episodenliste | — | Nicht erforderlich mit Begründung: identische Dispatch-Kette (`PlayRequestedMessage` → `PlayerPage`), der Fix ist einstiegspunkt-agnostisch; der Button ist durch `MediaDetailsPage_HasPlayButton` gepinnt |
| iOS-Laufzeitverhalten (eigentlicher SIGABRT) | Keine Automatisierung — manuelle Verifikation via TestFlight | Nicht erforderlich mit Begründung: kein macOS/iOS-Harness verfügbar (vom Anwender geklärt); Windows-E2E ist das maximal Machbare |

## Fehlende oder unvollständige Planbestandteile

- [ ] Fehlender Umsetzungsschritt: Mechanismus zum Klicken eines `MediaCarousel`-Eintrags im E2E-Harness (Voraussetzung für Umsetzungsschritt 8). `DevApiClient.InvokeAsync(automationId, itemIndex)` → `TrySelectItem` unterstützt nur `CollectionView`; `MediaCarousel` nutzt `BindableLayout` auf `HorizontalStackLayout` mit `Tapped`-Event (kein `Command`). Zu planen ist eine der Varianten: (a) `DevApiVisualTree` um Item-Auswahl für `BindableLayout`-Container erweitern (Index-Kind finden, `TapGestureRecognizer.Tapped` auslösen — ggf. per `SendTapped`/Reflection; Contract-Test `DevApiVisualTree_UsesPlatformInvokeEquivalents` dann erweitern/anpassen), (b) `AutomationId` auf das Eintrags-Root-Element in `MediaCarousel.xaml` setzen und per FlaUI/`UiDriver` klicken, oder (c) `MediaCarousel` auf `CollectionView` umbauen (mit `SelectionChanged` → `OnMediaItemTapped`).

## Hinweise

- Zur Nachplanung des Item-Klicks: `UiDriver.OpenFlyoutItem` zeigt mit `item.Click()` bereits einen FlaUI-Fallback-Pfad; `UiDriver.FindByName` kann das Titel-Label eines Karussell-Eintrags finden. `DevApiContractTests.MauiDevApiService_MapsExactlyTheFourEndpoints` pinnt die Endpunktmenge — ein etwaiger neuer DevApi-Endpunkt würde diesen Test brechen (Erweiterung von `TrySelectItem` innerhalb des bestehenden `/invoke`-Endpunkts bleibt davon unberührt).
- `IosDeploymentResponseFileTests` pinnt Vorkommenszahlen im Skript (`Add-PropertyLine` ×3, `Add-MaskedDisplay` ×2, `Write-MaskedPropertyLines` ×3) — der neue dSYM-Schritt darf diese gezählten Stellen nicht verändern; ergänzend als zu prüfender Bestandstest notieren.
- Restrisiko (akzeptabel dokumentiert, kein Muss): Toolkit-Pfad §6.2 der Bestandsaufnahme (`throw` im `Player.Seek`-Completion-Callback bei `complete == false`) bleibt appseitig nicht abfangbar; sekundärer Verdacht §6.7 (ModelChanged-/Binding-Threading aus `OnMediaEnded`/`OnMediaFailed`) bleibt bewusst unberührt — beide als Folge-Beobachtung führen.
- Alternativ könnte der Resume-E2E direkt über die „Weiterschauen"-Karussell fahren (Eintrag hat dort bereits `PositionSeconds > 0`); unabhängig davon bleibt die Klicklücke dieselbe.
