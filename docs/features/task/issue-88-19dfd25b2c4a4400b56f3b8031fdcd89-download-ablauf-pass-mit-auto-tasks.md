# Tasks: Download-Ablauf-Pass mit Auto-Download-Schutz — Komposition im Effect-Handler statt im Service

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Logik | `AutoDownloadService.ReconcileWithExpiryProtectionAsync(settings, ct)` als einziger Einstiegspunkt des Geschäftsvorgangs „Ablauf-/Bereinigungslauf mit Auto-Download-Schutz": intern `GetAutoDownloadCandidateKeysAsync` → `downloads.ReconcileAsync` (bestehende `downloads`-Abhängigkeit, kein neuer Zyklus); XML-Dokumentation | Erledigt | `AutoDownloadServiceTests.ReconcileWithExpiryProtection_ExtendsCandidate_RemovesNonCandidate` |
| 2 | Logik | `ShellAppEffectHandler.ReconcileWithExpiryPassAsync` entfernen; die drei Aufrufstellen (`LoadDownloadsAsync`, `ResumeDownloadsAsync`, `ReconcileDownloadsAsync`) rufen `autoDownloads.ReconcileWithExpiryProtectionAsync` direkt | Erledigt | `DownloadsContractTests.DownloadService_Reconcile_WiredInHandler` (Komposition im Service, kein `downloads.ReconcileAsync` mehr im Handler) |
| 3 | Tests | `DownloadsContractTests.DownloadService_Reconcile_WiredInHandler` anpassen: Komposition (`GetAutoDownloadCandidateKeysAsync` + `downloads.ReconcileAsync`) in `AutoDownloadService.cs` pinnen, Handler-Aufruf des neuen Einstiegspunkts pinnen | Erledigt | `DownloadsContractTests.DownloadService_Reconcile_WiredInHandler` |
| 4 | Tests | `AutoDownloadServiceTests.ReconcileWithExpiryProtection_ExtendsCandidate_RemovesNonCandidate` neu: abgelaufener Weiterschauen-Kandidat wird verlängert (Herkunft `auto`), abgelaufener Nicht-Kandidat entfernt — über den neuen Einstiegspunkt | Erledigt | `AutoDownloadServiceTests.ReconcileWithExpiryProtection_ExtendsCandidate_RemovesNonCandidate` |
| 5 | Testlauf | `dotnet build` + `dotnet test VideoPlayer.Maui.Tests` — alle Tests erfolgreich | Erledigt | `dotnet test` Release: 1484/1484 bestanden |

## Hinweise

- tvOS (`TvosAppEffectHandler`) bleibt unverändert: dort existiert kein `AutoDownloadService` (kein Vorlauf), der explizite `null`-Kandidatenaufruf ist dokumentierte Absicht, kein Schutzverlust.
- Keine Release-Notes: interne Kompositions-Verlagerung ohne Anwender-Änderung.
