# Anforderung: Auto-Fortsetzung nur am tatsächlichen Medienende — konfigurierbarer Vorlauf entfernen

## Fachliche Zusammenfassung

Der positionsgetriebene Vorlauf der Auto-Fortsetzung wird vollständig entfernt: Der Wechsel zur nächsten Episode (Serienzweig `AdvanceToNextEpisode`) bzw. zum nächsten Playlist-Eintrag (`AdvancePlaylistEntry` über `play/advance`) darf ausschließlich über das echte Wiedergabeende (`MediaEnded` → `PlaybackEndedMessage` → `AppUpdate.UpdatePlaybackEnded`) ausgelöst werden — nicht mehr im Fortschrittstakt bei `remaining <= AutoResumeRemainingSeconds`. Die Einstellung `AutoResumeRemainingSeconds` wird aus `AppSettings`, Persistenz, `SettingsPage` (UI + Code-Behind) und sämtlichen Tests entfernt. Unverändert bleiben der Schalter `AutoResumeEnabled` (steuert weiterhin den Wechsel am Medienende), die „angesehen"-Markierung über `WatchedEndWindowSeconds` samt ihrer Streak-Plausibilisierung (`EndWindowStreak`, `IsProgressTickPlausible`, `EndWindowRequiredTicks`) sowie die Ende-Plausibilitätsprüfung `IsPlaybackEndPlausible`/`PlaybackEndedToleranceSeconds`, die damit zum alleinigen Wechsel-Gate wird.

Hintergrund (Vorfall, Build 370 / v1.1.0-RC.29): Episode 21614 wurde ~4–5 Minuten vor dem tatsächlichen Ende legitimerweise über das 300-s-Restlaufzeitfenster auf 21615 weitergeschaltet (kein `MediaEnded`, kein `MediaFailed`, kein Streak-Fehlverhalten). Die fachliche Entscheidung des Anwenders ist bereits getroffen: Jedes Video muss bis zum tatsächlichen Ende abgespielt werden; der konfigurierbare Vorlauf entfällt ersatzlos.

## Betroffene Klassen und Komponenten

### Datenmodell / Persistenz

- `VideoPlayer.Storage/AppSettings.cs`
  - `AutoResumeRemainingSeconds` (Z. 23–27, Default 300) — Property entfernen.
  - `Normalize()` (Z. 73–79) — Clamp-Zeile `AutoResumeRemainingSeconds` (Z. 76) entfernen.
  - `EndWindow` (Z. 47–48) — nach Entfernen des zweiten Fensters faktisch `WatchedEndWindowSeconds`; Property vereinfachen oder durch `WatchedEndWindowSeconds` ersetzen (s. Offene Punkte).
  - `AutoResumeEnabled` (Z. 21) und `WatchedEndWindowSeconds` (Z. 36) bleiben.
- `VideoPlayer.Storage/AppSettingsService.cs`
  - Konstante `AutoResumeRemainingSecondsKey` (Z. 19–20), `Load()`-Zeile (Z. 62), `Save()`-Zeile (Z. 82) — entfernen. Der bereits persistierte Schlüssel in `Preferences` bleibt verwaist (keine Migration, per Vorgabe).
- `VideoPlayer.Mvu/PlayerState.cs`
  - `EndWindowStreak` (Z. 128–134) bleibt — die „angesehen"-Markierung bleibt streak-plausibilisiert.

### Logik (MVU-Kern)

- `VideoPlayer.Mvu/AppUpdate.cs`
  - `EvaluateEndWindow` (Z. 3876–3961): beide Advance-Zweige entfernen — Playlist-Zweig (Z. 3924–3937) und Serienzweig (Z. 3939–3951) samt der Bedingungen `remaining <= AutoResumeRemainingSeconds`. Streak-Aufbau (Z. 3892–3899) und Watched-Markierung samt `DeleteWatchedDownloadEffect` (Z. 3903–3914) bleiben.
  - `UpdatePlaybackProgress` (Z. 3812–3851) — Doc-Kommentar anpassen (Z. 3795–3808 verweist auf `AutoResumeRemainingSeconds`/Fortsetzung).
  - `EvaluateEndWindow`-Doc-Kommentar (Z. 3853–3868) anpassen.
  - `UpdatePlaybackEnded` (Z. 3981–4045) — unverändert der einzige Wechselpfad: `IsPlaybackEndPlausible` (Z. 4058–4064) mit `PlaybackEndedToleranceSeconds` (Z. 88), danach `AdvancePlaylistEntry`/`AdvanceToNextEpisode` bei `AutoResumeEnabled`.
  - `IsProgressTickPlausible` (Z. 4078–4084), `EndWindowRequiredTicks` (Z. 97), `AdvanceToNextEpisode` (u. a. `EndWindowStreak`-Reset Z. 3783), `AdvancePlaylistEntry` (Z. 2623–2638), `UpdatePlaylistNavigationCompleted` (Streak-Reset Z. 2606) bleiben.
  - Konstanten-Kommentar `PlaybackEndedToleranceSeconds` (Z. 77–87) erwähnt die Auto-Fortsetzung „greift bereits" im Fenster — fachlich anpassen.

### UI (App-Rand)

- `VideoPlayer.Maui/Pages/SettingsPage.xaml`
  - Block „Zeitfenster Auto-Fortsetzung" mit `AutoResumeRemainingEntry` inkl. umschließender Trennlinie (Z. 118–134) entfernen; `AutoResumeSwitch` (Z. 109–116) und `WatchedEndWindowEntry` (Z. 136–146) bleiben.
- `VideoPlayer.Maui/Pages/SettingsPage.xaml.cs`
  - `WireNumeric(AutoResumeRemainingEntry, …)` (Z. 38), `SetTextIfNotFocused(AutoResumeRemainingEntry, …)` (Z. 81), Handler `OnAutoResumeRemainingCompleted` (Z. 144–147) entfernen.
- `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`
  - `ReportPosition` (Z. 172–216): `LogDebug`-Fenster-Diagnose nutzt `model.Settings.EndWindow` (Z. 189–199) — an die verbleibende Fensterquelle (`WatchedEndWindowSeconds`) anpassen, sofern `EndWindow` entfällt. `PlaybackProgressMessage`-Dispatch mit `appliedMediaId` (Z. 202–207) und `OnMediaEnded` → `PlaybackEndedMessage` (Z. 455–460) bleiben.

### Tests (`VideoPlayer.Maui.Tests`)

- `AppSettingsTests.cs` — `Defaults_AreExpected` (Z. 17), `Normalize_ClampsAutoResumeRemainingSeconds` (Z. 43–53, entfällt), `EndWindow_IsMaxOfBothWindows` (Z. 80–101, abhängig von der `EndWindow`-Entscheidung).
- `AppSettingsServiceTests.cs` — `Save_ThenLoad_RoundTrips` (Z. 37) u. a. Aufrufstellen.
- `SettingsStorageContractTests.cs` — `ExpectedAutomationIds` ohne `"AutoResumeRemainingEntry"` (Z. 27); Entry-Zählung 4 → 3 (Z. 57–61).
- `AppUpdateTests_Player.cs` — alle `PlaybackProgress_*`-Tests mit Advance-Erwartung/-Bedingung anpassen bzw. entfernen: `PlaybackProgress_AutoResumeAdvancesWhenEnabled` (Z. 334), `PlaybackProgress_AutoResumeDisabled_DoesNotAdvance` (Z. 367), `PlaybackProgress_AboveResumeWindow_DoesNotAdvance` (Z. 387), `PlaybackProgress_PausedWithinWindow_DoesNotAdvance` (Z. 409), `PlaybackProgress_NoNextEpisode_DoesNotAdvance` (Z. 429), `PlaybackProgress_SingleWindowTick_DoesNotAdvance` (Z. 447), `PlaybackProgress_SecondWindowTick_AdvancesAndMarksWatched` (Z. 470 — Watched-Assert bleibt, Advance-Assert entfällt), `PlaybackProgress_PositionJump_SingleTick_NoAdvance` (Z. 579), `PlaybackProgress_PausedTick_ResetsStreak` (Z. 613), `PlaybackProgress_WhileResolving_NoWindowEffects` (Z. 682), `PlaybackProgress_SeriesAdvance_RequiresNotResolving` sowie die `AutoResumeRemainingSeconds`-Fixture-Belegungen (Z. 340, 393, 415). Die `PlaybackEnded_*`-Tests (Z. 710–968) werden zum alleinigen Wechsel-Nachweis.
- `AppUpdateTests_PlaylistPlayback.cs` — `PlaybackProgress_NearEnd_TwoTicks_AdvancesPlaylist` (Z. 533), `PlaybackProgress_NearEnd_WhileResolving_NoSecondAdvance` (Z. 567), `PlaybackProgress_NearEnd_InPlaylist_AdvancesPlaylist` u. a. anpassen; `PlaybackEnded_InPlaylist_*` bleiben als Wechsel-Nachweis.
- `AppUpdateTests_Downloads.cs` — `PlaybackProgress_TwoWindowTicks_EmitsAutoDelete` (Z. 505) bleibt fachlich (Watched-Markierung + AutoDelete bleiben streak-gekoppelt); Fenster-Fixtures ggf. an `WatchedEndWindowSeconds` angleichen.
- `PlayerContractTests.cs` — `ReportPosition`-Body-Assert (Z. 384) ggf. an die geänderte Fensterquelle anpassen; `PlaybackEndedMessage`-/`appliedMediaId`-Pins (Z. 105–106, 457–499) bleiben.

### E2E-Tests (`VideoPlayer.E2E.Tests`)

- `PlaylistPlaybackTests.PlaybackEnd_AdvancesToNextPlaylistEntry` (Z. 213–254), `ContinueWatchingPlaylistEntry_AdvancesByPlaylistOrder_NotSeries` (Z. 312+), `ContinueWatchingPlaybackTests.TapContinueWatchingEpisode_OpensDetailsAndStartsPlaybackAndAdvancesToNextEpisode` (Z. 96–155): Diese Tests stützen sich darauf, dass das kurze Testvideo „sofort im Fortsetzungsfenster" liegt. Nach der Änderung erfolgt der Wechsel erst beim echten `MediaEnded`; Kommentare und ggf. Wartezeiten (aktuell 60 s) sind zu prüfen — das kurze Testvideo sollte das Ende weiterhin innerhalb des Timeouts erreichen (Annahme, im Testlauf zu verifizieren).

### Dokumentation (nachgelagerter Schritt, aber betroffen)

- `docs/help/wiedergabe-pip.md` — Abschnitt „Automatische Fortsetzung" (Z. 130–185): positionsgetriebener Wechsel und Auto-Fortsetzungs-Fenster entfallen; „Wiedergabeende" (Z. 187–210) bleibt der beschriebene Mechanismus.
- `docs/help/einstellungen-datenhaltung.md` — Eintrag „Zeitfenster Auto-Fortsetzung" (Z. 36–38) entfernen.

## Implementierungsansatz

1. **Einstellung entfernen:** `AutoResumeRemainingSeconds` aus `AppSettings` (Property, Default, `Normalize()`-Clamp) und `AppSettingsService` (Schlüssel-Konstante, `Load`, `Save`) streichen. Kein Migrationscode — der persistierte `Preferences`-Schlüssel bleibt verwaist (Vorgabe). `EndWindow`-Property: nach Entfernen des zweiten Fensters auf `WatchedEndWindowSeconds` vereinfachen oder die Property entfernen und die zwei Verwendungsstellen (`AppUpdate.EvaluateEndWindow` Z. 3883, `PlayerPage.ReportPosition` Z. 190) direkt auf `WatchedEndWindowSeconds` umstellen.
2. **MVU-Kern:** In `EvaluateEndWindow` die beiden Advance-Blöcke (Playlist Z. 3924–3937, Serien Z. 3939–3951) entfernen. Der Fortschrittstakt behält ausschließlich: Positions-/Zustandsübernahme, Fortschrittsmelde-Effekt (`ReportPlaybackProgressEffect`), Streak-Aufbau und `IsWatched`-Markierung samt optionalem `DeleteWatchedDownloadEffect`. Doc-Kommentare in `UpdatePlaybackProgress`, `EvaluateEndWindow` und an `PlaybackEndedToleranceSeconds` aktualisieren. `UpdatePlaybackEnded` ist damit der einzige Auto-Fortsetzungs-Auslöser (bestehender Mechanismus, keine Änderung an der Plausibilitätslogik vorgesehen).
3. **UI:** `AutoResumeRemainingEntry`-Block aus `SettingsPage.xaml` sowie die Verdrahtung in `SettingsPage.xaml.cs` entfernen.
4. **Tests:** Fortschrittstakt-Tests von „schaltet um"/„schaltet nicht um" auf das neue Verhalten umstellen — kein Takt löst mehr einen Wechsel aus (Advance-Asserts entfernen bzw. in „kein Wechsel im Fenster"-Negativ-Tests überführen); Wechsel-Nachweis vollständig auf `PlaybackEnded_*`-/MediaEnded-Pfad verlagern. Settings-/Contract-Tests an die reduzierte Steuerelement- und Property-Menge anpassen.
5. **E2E:** Auto-Advance-Erwartungen auf den echten Ende-Pfad prüfen; Kommentare anpassen, Wartezeiten im Testlauf verifizieren.

## Konfiguration

- **Entfällt:** `AutoResumeRemainingSeconds` (bisher 30–3600 s, Default 300) — aus Einstellungsmodell, Persistenz und UI.
- **Bestehen bleiben:** `AutoResumeEnabled` (Schalter, steuert den Wechsel am Medienende) und `WatchedEndWindowSeconds` („als angesehen"-Fenster, positionsgetrieben mit Streak-Plausibilisierung). `PlaybackEndedToleranceSeconds` und `EndWindowRequiredTicks` bleiben interne Konstanten — keine neue Konfiguration entsteht; das Verhalten ist nicht mehr konfigurierbar (ausdrückliche Entscheidung).

## Offene Fragen

1. **`AppSettings.EndWindow`:** Property als Alias für `WatchedEndWindowSeconds` beibehalten (minimale Diff) oder entfernen und beide Verwendungsstellen direkt auf `WatchedEndWindowSeconds` umstellen? Beides erfüllt die Anforderung; Entscheidung in der Planung (Empfehlung: entfernen — die Property existierte nur für das Maximum zweier Fenster).
2. **Streak-Verbleib (Annahme):** Es wird angenommen, dass `EndWindowStreak`/`IsProgressTickPlausible` für die „angesehen"-Markierung und die Download-Löschung bestehen bleiben — die Anforderung betrifft nur den Fortsetzungs-Vorlauf. Gegenprobe in der Planung.
3. **`PlaybackEndedToleranceSeconds` (300 s):** Ein verfrüht feuerndes `MediaEnded` außerhalb des Toleranzfensters wird weiterhin verworfen; tritt danach kein weiteres Ende-Ereignis auf, gibt es keinen Wechsel — das ist die fachlich gewünschte Konsequenz („bis zum tatsächlichen Ende"). Als Risiko zu dokumentieren: Ein Stream, der still hängen bleibt und nie `MediaEnded` liefert, schaltet künftig nie automatisch weiter (zuvor griff der Positionsvorlauf).
4. **E2E-Timeouts:** Die 60-s-Wartezeiten der Auto-Advance-Tests dürften das echte Ende des kurzen Testvideos weiterhin abdecken — Annahme, im Testlauf zu bestätigen (kommentarlos belassen oder Kommentare angleichen).
