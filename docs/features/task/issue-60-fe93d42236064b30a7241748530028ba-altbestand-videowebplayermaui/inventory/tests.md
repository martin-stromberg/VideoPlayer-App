# Betroffene Tests

`VideoWebPlayer.Maui.Tests` ist das Unit-/Vertragstest-Projekt der neuen App (xunit.v3, `net10.0`). Referenzen: `msTools.DeveloperApi`, `VideoPlayer.ApiClient`, `VideoPlayer.Mvu`, `VideoPlayer.Storage`, **`VideoWebPlayer.Client`** (einzige Altbestand-Referenz).

Drei Testdateien sind vom Altbestand abhängig:

## `ContinueWatchingEventIngressTests.cs` — komplett löschen

Testet ausschließlich Altbestand-DTOs (`using VideoWebPlayer.Client.Models`):

- `ContinueWatchingDto_RoundTripsThroughJsonContract` — JSON-Roundtrip von `ContinueWatchingDto`/`DtoMovie` (nur im Altbestand vorhanden; die neue App nutzt `VideoPlayer.ApiClient/Models/ContinueWatching.cs`)
- `MutationResult_UsesStableStatusAndMessageFields` — `ContinueWatchingMutationResult` aus `VideoWebPlayer.Client.Models`. Pendant `VideoPlayer.ApiClient.Models.ContinueWatchingMutationResult` existiert bereits und wird anderweitig abgedeckt (`ContinueWatchingMutationServiceTests`)

Trotz des Namens existiert keine Klasse `ContinueWatchingEventIngress` mehr im Code — die Datei ist ein reines Legacy-Artefakt.

## `MauiSettingsContractTests.cs` — komplett löschen

Quelltext-Vertragstest, der ausschließlich Dateien des Altbestands liest:

- `MauiAuthAndProfileConsumers_DoNotBypassSettingsService` (Theory, 5 `InlineData`-Pfade unter `VideoWebPlayer.Maui/`: `AuthService.cs`, `ConnectionService.cs`, `DownloadManager.cs`, `DownloadQueue.cs`, `LoginPage.xaml.cs`)
- `SettingsService_StoresSensitiveValuesInProfileAwareSecureStorage` (liest `VideoWebPlayer.Maui/Services/SettingsService.cs`)

## `EnumCoverageTests.cs` — teilweise bereinigen

Sichert die Enum-Wertabdeckung für den Git-Hook `enum-coverage-check` (pre-push `--strict`): jeder Enum-Wert des Repos muss in einer Testdatei genannt sein.

**Behalten** (neue App, Laufzeit-Checks via `Enum.GetValues`):

- `RecentEntryKind_AllValuesAreCovered` (`VideoPlayer.ApiClient.Models.RecentEntryKind`)
- `PlaybackSourceKind_AllValuesAreCovered` (`VideoPlayer.Mvu.PlaybackSourceKind`)
- `HealthCheckResult_AllValuesAreCovered` (`VideoPlayer.ApiClient.HealthCheckResult`)
- `PairingStatus_AllValuesAreCovered` (`VideoPlayer.Mvu.PairingStatus`)

**Entfernen** (Altbestand):

- `RecentEntryType_AllValuesAreCovered` — Enum `RecentEntryType` aus `VideoWebPlayer.Client/Models/DtoRecentEntry.cs` (globaler Namespace); verschwindet mit dem Projekt
- `LegacyDownloadRetentionType_AllValuesAreCovered` — liest `VideoWebPlayer.Maui/Models/DownloadedVideo.cs`
- `LegacyDownloadStatus_AllValuesAreCovered` — liest `VideoWebPlayer.Maui/Models/DownloadedVideo.cs`
- `LegacyVideoSourceType_AllValuesAreCovered` — liest `VideoWebPlayer.Maui/Models/VideoRequest.cs`
- `LegacyCarouselKind_AllValuesAreCovered` — liest `VideoWebPlayer.Maui/ViewModels/MediaCarouselViewModel.cs`

**Mitentfernen** (werden danach ungenutzt): Hilfsmethoden `Read` und `EnumValuesFromSource`, Feld `Root`, `using VideoWebPlayer.Maui.Tests.Stubs`, `using System.Text.RegularExpressions` (nur noch ohne Legacy-Teile prüfen), Klassen-Doc-Kommentar anpassen (Abschnitt über Legacy-Quelltext-Vertrag entfällt).

## Git-Hook `enum-coverage-check`

`.githooks/enum-coverage-check.py` auditiert das gesamte Repo: jeder `public`/`internal` Enum-Wert muss in einer `*Test*`-Datei vorkommen (pre-push `--strict` = harte Grenze). Nach Löschung der Altbestand-Projekte verschwinden deren Enums — die entfernten Legacy-Testfälle sind dann nicht mehr nötig; die verbleibenden neuen Enum-Checks bleiben ausreichend.

## Übrige Testinfrastruktur

- `Stubs/RepositoryRoot.cs` (`RepositoryRoot.Find()`) bleibt — wird weiterhin u. a. von `IosDeploymentResponseFileTests` und anderen Quelltext-Vertragstests genutzt, die `VideoPlayer.Maui`-Quellen lesen.
- `IosDeploymentResponseFileTests.cs` — testet `scripts/iOS-Deployment*.ps1`; ohne Altbestand-Bezug.
- `VideoPlayer.E2E.Tests` — kein Altbestand-Bezug; E2E-Tests laufen ausschließlich gegen `VideoPlayer.Maui`.
