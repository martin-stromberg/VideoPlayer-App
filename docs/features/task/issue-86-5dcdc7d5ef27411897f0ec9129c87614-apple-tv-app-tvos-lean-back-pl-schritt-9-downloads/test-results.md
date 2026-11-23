# Test-Ergebnisse — Schritt 9: tvOS-Downloads

Status: **Keine Fehler**

## Ausgeführte Kommandos

- `dotnet test VideoPlayer.Maui.Tests/VideoPlayer.Maui.Tests.csproj --configuration Debug` → **1504/1504 bestanden** (50 s)
- `dotnet build VideoPlayer.Maui/VideoPlayer.Maui.csproj --configuration Release --framework net10.0-windows10.0.19041.0 -p:IncludeAndroidTarget=false -p:IncludeIosTarget=false -p:IncludeMacCatalystTarget=false` → **0 Fehler** (4 Warnungen, alle Bestand: NU1903/CS0618)

## Neue/angepasste Tests

- `AppUpdateTests_Downloads.AppActivated_Connected_EmitsResumeEffect` — verbunden → `ResumeDownloadsEffect`
- `AppUpdateTests_Downloads.AppActivated_Offline_EmitsReconcileOnly` — offline → nur `ReconcileDownloadsEffect`
- `AppUpdateTests_Downloads.AppActivated_Connecting_EmitsReconcileOnly` — Startzustand → nur `ReconcileDownloadsEffect`
- `AppUpdateTests_Downloads.AppActivated_CarriesSettings` — Effekt trägt `AppSettings` (Aufbewahrungszeit)
- `TvOsAppContractTests.TvOsEffectHandler_DoesNotWireExcludedAreas` — angepasst: Auto-Download weiterhin ausgeschlossen, manueller Download-Pfad erlaubt
- `TvOsAppContractTests.TvOsEffectHandler_WiresManualDownloads` — neu: alle manuellen Download-Effekte + Completion-Sink-Verdrahtung
- `TvOsAppContractTests.TvOsDownloadBackend_UsesBackgroundUrlSession` — neu: Backend-Exporte, Launch-Events, App-Delegate-Override, `AppActivatedMessage`

## Anmerkungen

- Ein einmaliger Testhost-Absturz (Exitcode -1073741819) trat während der Entwicklung auf; der Wiederholungslauf lief vollständig und grün — kein reproduzierbarer Defekt.
- tvOS-Kompilierung läuft ausschließlich über CI `build-tvos` (macOS); Ergebnis wird beim Push gemeldet.
