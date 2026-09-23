# Code-Review

## Ergebnis

**Status:** Keine Befunde

Fünfte Prüfung (Runde 4, Fortsetzung nach `review-code.3.md`). Schwerpunkt: die in dieser Iteration geänderten Stellen — `ShellAppEffectHandler.CheckCandidateServerAsync` (gezieltes `ClearDeviceTokenAsync` bei `GateRejected`), `DeviceTokenService.DiscardIfCurrentAsync` (atomarer Guard), Entfernung von `DeviceTokenService.SetPairedTokenAsync`, `ServerSetupPage.xaml` (`MaxLength` entfernt) sowie die angepassten Tests.

Feststellungen zu den Korrekturen:

- `CheckCandidateServerAsync` (`ShellAppEffectHandler.cs`): `secrets.ClearDeviceTokenAsync(address)` läuft in eigenem try/catch vor dem Provider-Reset — Fehler der sicheren Ablage brechen die Kandidatenprüfung nicht; `DiscardIfCurrentAsync` bleibt für den Provider-Reset des aktiven Servers; `IsPairingRequiredAsync` wird erst danach aufgerufen und sieht die bereinigte Ablage. Entspricht dem Muster aus `AuthSessionService.HandleGateRejectionAsync`.
- `DiscardIfCurrentAsync` (`DeviceTokenService.cs`): Adressvergleich und Provider-Rücksetzung laufen jetzt atomar unter `providerLock`; die Store-Löschung erfolgt nach dem Release (keine I/O unter der Serialisierung); `ArgumentException.ThrowIfNullOrWhiteSpace` ersetzt die bisherige `NullReferenceException`-Falle in `SameServer`. `discarded` ist im Erfolgspfad definitiv zugewiesen; der Early-Return bei Adress-Mismatch bleibt unverändert ohne Store-Zugriff.
- `SetPairedTokenAsync` entfernt: keine Produktivaufrufer (Produktivpfad nutzt `SetPairedTokenIfCurrentAsync` mit Stale-Guard); die Test-Aufrufstellen sind über die Hilfsfunktionen `PairAsync` (`DeviceTokenServiceTests`) bzw. `PairDeviceAsync` (`AuthSessionServiceTests`) auf `SetDeviceTokenAsync` + `LoadForServerAsync` umgestellt — semantisch identisch (Token hinterlegen + Kontext/Provider aktivieren). Der Erfolgspfad bleibt über `SetPairedTokenIfCurrent_CurrentServer_StoresAndActivates` abgedeckt.
- `PairingCodeEntry` ohne `MaxLength`: keine clientseitige Längenbegrenzung mehr; der serverseitige Kontrakt (`Pairing:CodeLength`, Minimum 4) wird nicht überstimmt. Der Quelltext-Vertrag (`PairingContractTests.ServerSetupPage_ContainsPairingSection`) prüft jetzt die Abwesenheit des Attributs.
- Beide betroffenen Testklassen laufen weiterhin in der `[Collection("ClientGate")]` (Mutation des statischen `ClientGate.DeviceTokenProvider` über `LoadForServerAsync` ist damit serialisiert).

Verifikation: `dotnet test VideoWebPlayer.Maui.Tests -c Release` → 873/873 bestanden, 0 Fehler.

## Geprüfte Dateien

- `VideoPlayer.ApiClient/ClientGate.cs`
- `VideoPlayer.ApiClient/DevicePairingClient.cs` (neu)
- `VideoPlayer.ApiClient/HealthCheckResult.cs` (neu)
- `VideoPlayer.ApiClient/Models/PairingExchangeRequest.cs` (neu)
- `VideoPlayer.ApiClient/Models/PairingExchangeResponse.cs` (neu)
- `VideoPlayer.ApiClient/PairingCrypto.cs` (neu)
- `VideoPlayer.ApiClient/VideoPlayerApiClient.cs`
- `VideoPlayer.Mvu/AppEffect.cs`
- `VideoPlayer.Mvu/AppMessage.cs`
- `VideoPlayer.Mvu/AppModel.cs`
- `VideoPlayer.Mvu/AppUpdate.cs`
- `VideoPlayer.Mvu/AuthAttempt.cs`
- `VideoPlayer.Mvu/AuthSessionService.cs`
- `VideoPlayer.Mvu/DeviceTokenService.cs` (neu)
- `VideoPlayer.Mvu/PairingCode.cs` (neu)
- `VideoPlayer.Mvu/PairingStatus.cs` (neu)
- `VideoPlayer.Storage/AppSecretsService.cs`
- `VideoPlayer.Storage/KnownServersService.cs`
- `VideoPlayer.Maui/MauiProgram.cs`
- `VideoPlayer.Maui/Pages/ServerSetupPage.xaml`
- `VideoPlayer.Maui/Pages/ServerSetupPage.xaml.cs`
- `VideoPlayer.Maui/Services/IServerHealthProbe.cs`
- `VideoPlayer.Maui/Services/ServerHealthProbe.cs`
- `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`
- `scripts/iOS-Deployment.ps1`

Tests:
- `VideoWebPlayer.Maui.Tests/AppSecretsServiceTests.cs`
- `VideoWebPlayer.Maui.Tests/AppUpdateTests_Connection.cs`
- `VideoWebPlayer.Maui.Tests/AppUpdateTests_Offline.cs`
- `VideoWebPlayer.Maui.Tests/AppUpdateTests_Pairing.cs` (neu)
- `VideoWebPlayer.Maui.Tests/AppUpdateTests_ServerManagement.cs`
- `VideoWebPlayer.Maui.Tests/AppUpdateTests_Startup.cs`
- `VideoWebPlayer.Maui.Tests/AuthSessionServiceTests.cs`
- `VideoWebPlayer.Maui.Tests/ClientGateTests.cs`
- `VideoWebPlayer.Maui.Tests/DevicePairingClientTests.cs` (neu)
- `VideoWebPlayer.Maui.Tests/DeviceTokenServiceTests.cs` (neu)
- `VideoWebPlayer.Maui.Tests/EnumCoverageTests.cs`
- `VideoWebPlayer.Maui.Tests/MvuHostTests.cs`
- `VideoWebPlayer.Maui.Tests/PairingCodeTests.cs` (neu)
- `VideoWebPlayer.Maui.Tests/PairingContractTests.cs` (neu)
- `VideoWebPlayer.Maui.Tests/Stubs/ClientGateCollection.cs` (neu)
- `VideoWebPlayer.Maui.Tests/VideoPlayerApiClientTests_Authentication.cs`
