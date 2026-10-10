# Tasks: Absturz beim Start eines Videos unter iOS untersuchen und beheben

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Crash-Fix | `PlayerPage`: Feld `pendingResumePosition` (`TimeSpan?`) einführen und `resumeApplied` ersetzen (Reset in `UpdateUi` beim Quellenwechsel) | Offen | — |
| 2 | Crash-Fix | `PlayerPage`: `TryApplyPendingResumeSeek()` + `ApplyResumeSeekAsync(TimeSpan)` implementieren (Gate auf `MediaElementState.Playing`/`Paused`, `SeekTo` mit ~10-s-Timeout, Fehler → `PlaybackFailedMessage` bei `subscribed`) | Offen | — |
| 3 | Crash-Fix | `PlayerPage.OnMediaOpened` umstellen: `ResumePosition` in `pendingResumePosition` merken, kein direktes `SeekTo` mehr, Gate über `BeginInvokeOnMainThread` anstoßen (`subscribed`-Guard beibehalten) | Offen | — |
| 4 | Crash-Fix | `PlayerPage.OnMediaStateChanged`: `TryApplyPendingResumeSeek()` nach `ReportPosition()` im bestehenden Marshal ergänzen | Offen | — |
| 5 | Fehlerisolierung | `PlayerPage.UpdateUi`: try/catch um `VideoElement.Stop()`/`Source`-Zuweisung/`MediaSource.FromFile`/`FromUri`; Fehler → `appliedLocation`-Reset + `PlaybackFailedMessage` | Offen | — |
| 6 | Fehlerisolierung | `PlayerPage.UpdateUi`: `Location`-Validierung vor `MediaSource`-Erzeugung (`ServerStream` → absolute http/https-URI via `Uri.TryCreate`; `LocalFile` → nicht leer); ungültig → `PlaybackFailedMessage` | Offen | — |
| 7 | Fehlerisolierung | `PlayerPage.ReportPosition`: try/catch (log-only) um die `VideoElement`-Lesezugriffe im Timer-/Marshal-Pfad | Offen | — |
| 8 | CI/Symbolisierung | `package-ios`-Action: `Collect iOS package` erweitern — `*.dSYM` aus `bin/Release` zu `release-ios-dsym.zip` zippen (tolerant bei leerem Fund) | Offen | — |
| 9 | CI/Symbolisierung | `main-merge.yml` + `staging-merge.yml`: `Upload iOS artifact` um `gh release upload release-ios-dsym.zip --clobber` (existenz-guarded) erweitern | Offen | — |
| 10 | CI/Symbolisierung | `scripts/iOS-Deployment.ps1`: dSYM-Export (Zip neben der `.ipa`) im `build`-Pfad ergänzen — ohne die gezählten `Add-PropertyLine`-/`Add-MaskedDisplay`-/`Write-MaskedPropertyLines`-Aufrufmuster zu verändern (Pin `IosDeploymentResponseFileTests`) | Offen | — |
| 11 | Tests | `PlayerContractTests.PlayerPage_MarshalsNativeElementAccessOnMainThread` anpassen (`SeekTo` nicht mehr in `OnMediaOpened`; genau ein `SeekTo(` in `ApplyResumeSeekAsync`) | Offen | — |
| 12 | Tests | `PlayerContractTests.PlayerPage_ResumeSeek_WaitsForReadyState` neu anlegen (kein `SeekTo` in `OnMediaOpened`, Gate `Playing`/`Paused`, Gate-Aufrufe aus beiden Handlern) | Offen | — |
| 13 | Tests | `PlayerContractTests.PlayerPage_NativeAccessesAreFailureIsolated` neu anlegen (try/catch in `UpdateUi`/`ApplyResumeSeekAsync`/`ReportPosition`, `PlaybackFailedMessage` in Fehlerpfaden, `Location`-Validierung) | Offen | — |
| 14 | Tests | `DevApiModelSnapshotTests.DevApiModelSnapshot_MapsPlayerState` neu anlegen (Mapping der Player-Felder) | Offen | — |
| 15 | Tests | `DevApiContractTests.DevApiVisualTree_SelectItem_InvokesBindableLayoutItemTap` neu anlegen (BindableLayout-Pfad in `TrySelectItem`, `SendTapped`-Reflection, Fehlerzweige, `ItemsOf` für BindableLayout) — Pin des Klickmechanismus | Offen | — |
| 16 | E2E-Infrastruktur | `DevApiModelSnapshot` um `PlayerIsActive`/`PlayerIsPlaying`/`PlayerPositionSeconds`/`PlayerError` erweitern; `DevApiModel` (E2E-DTO) spiegeln | Offen | — |
| 17 | E2E-Infrastruktur | `DevApiVisualTree.TrySelectItem` um `BindableLayout`-Item-Auswahl erweitern (Container-Suche im Teilbaum, Index-Prüfung, indexiertes Kind-View, `TapGestureRecognizer.SendTapped` per Reflection mit Fehler statt No-op); `ItemsOf` für `BindableLayout`-Container; Kommentare/`InvokeOnPage`-Doku anpassen — kein neuer Endpunkt | Offen | — |
| 18 | E2E-Infrastruktur | `AutomationId="PlayButton"` in `MediaDetailsPage.xaml` und `AutomationId="RecentCarousel"` an der `MediaCarousel` „Neu im Programm" (`HomePage.xaml`) ergänzen | Offen | — |
| 19 | E2E-Infrastruktur | `MockVideoServer` um opt-in konfigurierbare Medien-Endpunkte erweitern (`api/items/recent`, `api/items/movie/{id}`, `api/continue-watching` mit `PositionSeconds`, `api/items/movie/{id}/stream` inkl. Range-Support, `api/continue-watching/progress`-Sink); Defaults = bisherige Leerlisten | Offen | — |
| 20 | E2E-Infrastruktur | Kurze MP4-Testdatei (< 200 KB) erzeugen — Variante A: ffmpeg (ggf. per `winget install --id Gyan.FFmpeg` nachinstallieren); Variante B: frei verwendbares Mini-Sample-MP4 herunterladen — und als `Assets/testvideo.mp4` in `VideoPlayer.E2E.Tests` committen (`Content`/`CopyToOutputDirectory`) | Offen | — |
| 21 | E2E-Tests | `PlaybackTests` (neu): Wiedergabestart mit Fortsetzungsposition — Karussell-Eintrag per `DevApi.InvokeAsync("RecentCarousel", index)`, dann „Abspielen" → `playerIsPlaying`, `playerPositionSeconds` ≥ Resume-Position, Prozess lebt | Offen | — |
| 22 | E2E-Tests | `PlaybackTests`: Stream-Fehlschlag (404) → `playerError`/`ErrorPanel` sichtbar, kein Prozessabbruch | Offen | — |
| 23 | Verifikation | Manuelle iOS-Verifikation des Fixes im nächsten TestFlight-Build durch den Anwender (bestätigter Absturzpfad: Video mit Weiterschauen-Position > 0 sowie ohne Position) — keine Automatisierung | Offen | — |
