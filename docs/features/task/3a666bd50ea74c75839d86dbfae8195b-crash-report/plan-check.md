# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Das Starten eines Videos („Abspielen"-Schaltfläche Detailseite, ▶ Episodenliste) darf die Anwendung nicht beenden | Verzögerter Resume-Seek via `pendingResumePosition` + `TryApplyPendingResumeSeek`-Gate auf `Playing`/`Paused` (Programmablauf „Verzögerter Resume-Seek", Umsetzungsschritt 1) — adressiert die bestätigte Ursache (`MapSeekRequested` `async void` + `MediaOpened` vor `ReadyToPlay`, Toolkit-Issue #3249) | E2E `PlaybackTests` „Wiedergabestart mit Fortsetzungsposition" (Windows) + Contract-Test `PlayerPage_ResumeSeek_WaitsForReadyState`; manuelle iOS-Verifikation via TestFlight (Schritt 10) | Abgedeckt |
| Fehler beim Wiedergabestart landen als `player.Error` im `ErrorPanel`, nicht als Prozessabbruch | try/catch um `UpdateUi`-Quellenblock, `ApplyResumeSeekAsync` (mit 10-s-Timeout gegen hängenden `SeekCompletedTCS`) und `ReportPosition` → `PlaybackFailedMessage` (Schritt 2) | E2E `PlaybackTests` „Stream-Fehlschlag → `playerError`/`ErrorPanel`, Prozess lebt" + Contract-Test `PlayerPage_NativeAccessesAreFailureIsolated` | Abgedeckt |
| Marshalling aller `VideoElement`-Zugriffe auf den UI-Thread | Bestehende Marshals bleiben; Gate nur aus UI-Thread-Marshals aufrufbar; `subscribed`-Guards bleiben erhalten | Bestandstest `PlayerPage_MarshalsNativeElementAccessOnMainThread` wird auf die neue Struktur umgestellt (als betroffener Test gelistet) | Abgedeckt |
| Validierung der aufgelösten `PlaybackSource.Location` vor `MediaSource` | Validierungsregeln: `ServerStream` → `Uri.TryCreate` http/https; `LocalFile` → nicht leer; ungültig → `PlaybackFailedMessage` (Validierungsregeln-Tabelle + Schritt 2) | In `PlayerPage_NativeAccessesAreFailureIsolated` enthalten | Abgedeckt |
| App-seitiger Workaround bei Toolkit-Ausnahme zulässig | Gewählt — Paket-Upgrade recherchiert und ausgeschlossen (10.0.0 = neuestes NuGet-Release; Fix #3281 nur auf `main`, unveröffentlicht) | s. o. | Abgedeckt |
| Symbolisierung des Reports (erster Untersuchungsschritt) | Nicht möglich — dSYMs von Build 213 wurden nie archiviert, kein macOS-Zugriff, Repro-Build entfällt; im Plan nachvollziehbar begründet und vom Anwender geklärt; Ursache unabhängig durch manuellen Resume-Test bestätigt | Nicht testbar | Abgedeckt (mit Begründung) |
| dSYMs jedes TestFlight-/Store-Builds archivieren (prozessseitig) | `release-ios-dsym.zip` in `package-ios`-Action (Collect-Schritt), existenz-guarded Upload in `main-merge.yml` + `staging-merge.yml`, Export in `scripts/iOS-Deployment.ps1` (Schritt 4) | Kein automatisierter Test möglich; tolerant implementiert (kein Fund → Warnung); `IosDeploymentResponseFileTests`-Zähl-Pins als zu prüfende Bestandstests benannt | Abgedeckt |
| Regressionsschutz: Contract-Tests für vereinbarte Konventionen | Zwei neue Contract-Tests + Anpassung des Marshalling-Tests + DevApi-Contract-Tests (Schritte 3, 5, 6) | `PlayerPage_ResumeSeek_WaitsForReadyState`, `PlayerPage_NativeAccessesAreFailureIsolated`, `DevApiModelSnapshot_MapsPlayerState`, `DevApiVisualTree_SelectItem_InvokesBindableLayoutItemTap` | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Keine.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Wiedergabestart mit Fortsetzungsposition (Resume — bestätigter Absturzpfad): Login → „Neu im Programm"-Eintrag → Mediendetails → „Abspielen" → Player aktiv, Position ≥ Weiterschauen-Stand, Prozess lebt | `PlaybackTests` (Windows): `InvokeAsync("RecentCarousel", index)` über erweiterte `TrySelectItem` (BindableLayout-Pfad), `AutomationId="PlayButton"`, Assertion `playerIsActive`/`playerIsPlaying`/`playerPositionSeconds` via DevApi-Modell | Abgedeckt — Klicklücke aus `plan-check.1.md` geschlossen (siehe Hinweise) |
| Stream-Fehlschlag beim Wiedergabestart → `player.Error`/`ErrorPanel` statt Absturz | `PlaybackTests` (Stream-Endpunkt → 404), `playerError` via DevApi | Abgedeckt |
| ▶-Einstieg aus der Episodenliste | — | Nicht erforderlich mit Begründung: identische Dispatch-Kette (`PlayRequestedMessage` → `PlayerPage`); der Fix ist einstiegspunkt-agnostisch; der Button ist durch `MediaDetailsPage_HasPlayButton` gepinnt |
| iOS-Laufzeitverhalten (eigentlicher SIGABRT) | Keine Automatisierung — manuelle Verifikation via nächstem TestFlight-Build (Resume + Kaltstart) als dokumentierter Schritt 10 | Nicht erforderlich mit Begründung: kein macOS/iOS-Harness verfügbar (vom Anwender geklärt); Windows-E2E ist das maximal Machbare |

## Fehlende oder unvollständige Planbestandteile

Keine.

## Hinweise

- **Verifikation der zuvor gemeldeten Lücke (Klickmechanismus):** Geschlossen durch Umsetzungsschritt 6. Am Code verifiziert: `TapGestureRecognizer.SendTapped` existiert in `Microsoft.Maui.Controls` 10.0.90 exakt mit der geplanten Signatur `internal void SendTapped(View sender, Func<IElement?, Point?>? getPosition = null)` und führt `Tapped?.Invoke(sender, …)` aus (plus `Command`-Ausführung) — dieselbe Ereigniskette wie ein Fingertipp: `OnItemTapped(sender=Border)` → `sender.BindingContext` = `HomeMediaItem` → `ItemTapped` → `HomePage.OnMediaItemTapped` → `MediaItemSelectedMessage` → `NavigateToMediaDetailsEffect` → Shell-Route `mediadetails`. `TrySelectItem` akzeptiert heute tatsächlich nur `CollectionView` (`DevApiVisualTree.cs` :155–192), `MediaCarousel` nutzt `BindableLayout.ItemsSource` auf `HorizontalStackLayout` mit `Tapped="OnItemTapped"` ohne `Command` (`MediaCarousel.xaml` :16,36) — die Erweiterung ist nötig und der Reflection-Ansatz plausibel. Der Mechanismus bleibt innerhalb des bestehenden `/invoke`-Endpunkts → Pin `MauiDevApiService_MapsExactlyTheFourEndpoints` (zählt `Map(`/`MapModel(` = 4) unberührt. `ItemsOf`-Erweiterung liefert die `items`-Liste für `/tree` — Index-Ermittlung wie bei `ServerSwitchTests`/`KnownServersView`. Reflection-Bruchrisiko bei MAUI-Upgrade ist benannt und per Fehlertext + Contract-Test abgefedert (DEBUG/`DEV_API`-only, kein Produktivpfad).
- **Mock-Endpunkte verifiziert:** `api/items/recent`, `api/continue-watching`, `api/items/movie/{id}`, `api/items/{typ}/{id}/stream`, `POST api/continue-watching/progress` entsprechen den realen Client-Endpunkten (`VideoPlayerApiClient.cs`); die Liste `/api/items/recent` existiert im Mock bereits als leere Default-Antwort (`IsListEndpoint`) — Opt-in-Konfiguration hält Bestandstests grün. Die POST-Progress-Senke ist nötig (heute 404) und geplant.
- **E2E-Setup-Strecke vorhanden:** `E2ETestBase` bietet `ConnectToPairingRequiredAsync`/`PairAsync`/`LoginAsync` bis zur Home-Seite; `WaitForModelAsync` ist der etablierte Assertionskanal.
- **Beiläufiger Pin (kein Muss, bei Umsetzung beachten):** `MsToolsDeveloperApi_HasNoSecretSurface` und `MsToolsDeveloperApi_HasNoDispatchEndpoint` scannen die DevApi-Quellen wortbasiert — der neue `DevApiVisualTree`-Code/Kommentare dürfen weder „token"/„password"/„secret" (als eigenes Wort) noch „dispatch" enthalten.
- **Restrisiken (akzeptiert dokumentiert):** Toolkit-Pfad §6.2 (`throw` im `Player.Seek`-Completion-Callback bei `complete == false`) bleibt appseitig nicht abfangbar — das Readiness-Gate verkleinert nur das Fenster; Sekundärverdacht §6.7 (Hintergrundthread-`ModelChanged`/`PropertyChanged`-Bindings) bleibt bewusst unberührt. Beides als Folge-Beobachtung geführt — fachlich vertretbar.
- **Testvideo-Unsicherheit bekannt:** Ob der Windows-`MediaElement` den Loopback-Stream der Mini-MP4 annimmt, wird erst in der Umsetzung verifiziert; Fallback („Seite öffnet sich, Fehlerpfad sichtbar, Prozess lebt") ist im Plan benannt.
