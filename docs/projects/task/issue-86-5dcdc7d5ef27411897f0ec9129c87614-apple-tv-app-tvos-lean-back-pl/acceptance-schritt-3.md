# Abnahmeprüfung – Entwicklungsschritt 3

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Hinweise

Nachbesserungsrunde 2. Prüfung anhand des Fix-Commits `db61461`
(Diff `4fa56b4..HEAD`: 4 Dateien, +102/−5 — nur die beiden tvOS-Quelldateien,
der neue Contract-Test und `steps.md`) sowie einer eigenständigen
Stichprobenprüfung aller 22 tvOS-Quelldateien auf dieselbe Fehlerklasse.

**(a) Gemeldete Abweichung behoben — verifiziert:**
- `VideoPlayer.TvOS/Ui/ServerSetupViewController.cs:36,45`: die Felder
  `addressStage` und `authStage` sind jetzt `UIStackView?` deklariert;
  die Zuweisungen `addressStage.Spacing = 30` (Z. 159) und
  `authStage.Spacing = 30` (Z. 202) kompilieren damit — `Spacing` ist
  `UIStackView`-Member, `TvosControls.Section` liefert `UIStackView`
  (`Ui/TvosControls.cs:114`). Die übrigen Zugriffe auf beide Felder
  (`Hidden`, Z. 292-293) sind `UIView`-Member und ebenfalls gültig.
- `VideoPlayer.TvOS/Ui/MenuTabBarController.cs`: kein `MenuPages.IndexOf`
  mehr; `AppRoutes.MenuPages` ist `IReadOnlyList<AppPage>`
  (`VideoPlayer.Mvu/AppRoutes.cs:34`) und besitzt kein `IndexOf` — das
  wäre ein weiterer CS1061 gewesen. Die neue Hilfsmethode
  `IndexOfMenuPage` (Z. 111-124) liefert per Schleife den ersten
  Trefferindex bzw. `-1` — semantisch identisch; Aufrufstellen
  `Select` (Z. 60) und Tab-Tag (Z. 104, `int`→`nint` implizit).
- Neue Contract-Tests `VideoPlayer.Maui.Tests/TvOsCompileContractTests.cs`
  decken exakt diese Fehlerklasse plattformunabhängig ab
  (Quelltext-Prüfung: `UIView`-deklarierte Namen ohne
  `Spacing`/`AddArrangedSubview`-Zugriff; kein `MenuPages.IndexOf`) —
  sinnvoll, weil das tvOS-Projekt unter Windows zur leeren Bibliothek
  degradiert und der lokale Build solche Fehler nicht sieht.

**(b) Eigenständige Stichprobe auf dieselbe Fehlerklasse — keine
weiteren Befunde:**
- Deklaration-vs.-Zugriff in allen `Ui/*ViewController.cs`: verbleibende
  `UIView?`-Felder (`pairingSection`, `credentialsSection` in
  `ServerSetupViewController.cs:50,55`) erhalten ausschließlich
  `Hidden`-Zugriffe; alle `.Spacing`-/`.AddArrangedSubview`-Aufrufe
  stehen auf `UIStackView`-Instanzen (`var content`/`manualSection`/
  `ticketSection` aus `TvosControls.Section`,
  `LoginViewController.cs:65-66`, `SettingsPageViewController.cs:34-37`,
  `ProfilePageViewController.cs:36-37`).
- UIKit-Oberfläche (per Inspektion gegen die Bindings): `UITabBarController`
  (`ViewControllerSelected`, `SelectedIndex`, `ViewControllers`),
  `UITabBarItem(title, image, nint tag)`, `UITableViewSource`
  (`RowsInSection`, `GetCell`, `RowSelected`), `UITableViewCell.Subtitle`
  mit `DetailTextLabel`, `UIResponder.PressesBegan(NSSet<UIPress>,
  UIPressesEvent)` inkl. `ToArray<UIPress>`-LINQ, `NSLayoutAnchor`-
  `ConstraintEqualTo`/`ConstraintLessThanOrEqualTo`-Überladungen,
  `UIScrollView.ContentLayoutGuide`/`FrameLayoutGuide`,
  `UIControl.PrimaryActionTriggered`, `NWPathMonitor`
  (`SnapshotHandler`, `SetQueue`, `UsesInterfaceType`,
  `NWInterfaceType.Wifi`/`Wired`), `SecKeyChain`
  (`QueryAsRecord`/`Add`/`Remove`, `SecRecord.ValueData`),
  `NSUserDefaults`-Zugriffsmethoden, `NSFileManager.GetUrls`,
  `DispatchQueue.MainQueue.DispatchAsync`, `NSThread.IsMain`,
  `UIDevice.CurrentDevice` — allesamt auf `net10.0-tvos` gebundene
  Standard-Member, keine Basistyp-/abgeleiteter-Typ-Verwechslung
  gefunden.
- Signaturen der plattformneutralen Aufrufe (unter Windows nicht
  mitkompiliert, daher ebenfalls stichprobenartig verifiziert):
  `IServerHealthProbe.CheckAsync`-Überladungen, `DevicePairingClient`
  (`ExchangeAsync`/`BootstrapAsync`/`RequestTimeout`-ctor),
  `BootstrapResult` (`DeviceToken`/`Token`/`RefreshToken`),
  `ServerAddress.TryNormalize(string?, out string)`,
  `DeviceTokenService` (`LoadForServerAsync`, `SetPairedTokenIfCurrentAsync`,
  `DiscardCurrent/IfCurrentAsync`, `NotifyRejected`,
  `DeviceTokenRejected`, 3-arg-ctor), `AuthSessionService`
  (5-arg-ctor, `TryStoredLoginAsync`, `LoginAsync`, `LogoutAsync`,
  `Client`, `SessionExpired`), `ServerEventsService`-ctor/-Events,
  `INetworkStatusMonitor`, `KnownServersService`/`KnownServer`,
  `ServerDiscoveryService.CreateDefault`/`DiscoveredServer`,
  `SyncResult(5 Parameter)`, `HomeLists.Empty`, `MvuHost(1 arg)`,
  alle im Handler/Composition verwendeten `AppEffect`-/`AppMessage`-
  Datensätze und `AppModel`-Member sowie alle
  `AppComposition`-Konstruktoraufrufe — konsistent.

**(c) Fachliche Vollständigkeit unverändert — stichprobenartig
bestätigt:** der Diff seit dem Feature-Commit `4fa56b4` berührt keine
Datei in `VideoPlayer.Mvu/`, `VideoPlayer.ApiClient/` oder
`VideoPlayer.Storage/`; die in Runde 1 verifizierte Verdrahtung
(Erkennung, manuelle Eingabe, Serverwechsel, Pairing-/Bootstrap-Code-
Eingabe, Login/Auto-Login, Geräte-Token, Offline, alle Fehlerzustände,
Persistenz, Auto-Start, keine Schreibzugriffe) ist unverändert. Die
beiden Fixes sind lokal und semantisch neutral: Feldtyp-Präzisierung
ohne Laufzeitänderung und `IndexOf`-Ersatz mit identischem Ergebnis
(erster Treffer bzw. `-1`).

**(d) Testsuite — grün:** `dotnet test VideoPlayer.Maui.Tests` (net10.0)
lokal ausgeführt: **1462/1462 bestanden, 0 Fehler, 0 übersprungen**
(Runde 1: 1460 — die Differenz sind die 2 neuen
`TvOsCompileContractTests`, die mitgelaufen und bestanden sind).
Warnung NU1903 (bekannte SignalR.Common-Sicherheitsanfälligkeit) ist
Bestand, nicht durch diesen Schritt eingeführt.

**Einschränkungen (Hinweise, keine Abweichungen):**
- Echter tvOS-Kompilier-/Simulator-Nachweis steht weiterhin über den
  macOS-CI-Job `build-tvos` aus — er wird erst nach Push sichtbar. Die
  CS1061-Fehlerklasse der Runde-1-Abweichung ist per Inspektion und
  Contract-Test ausgeschlossen; der restliche tvOS-Quelltext wurde
  gegen die Binding-Signaturen stichprobenartig geprüft, kann aber
  erst der macOS-Build abschließend verifizieren.
- Die in Runde 1 notierten Randfälle (Rückweg aus dem Offline-Zustand
  nur über Systemverhalten der Menü-Taste bzw. Serverwahl;
  `LoadSources`/`ResumeDownloads` als bewusstes No-Op) sind unverändert
  und bleiben außerhalb der Anforderung dieses Schritts.
