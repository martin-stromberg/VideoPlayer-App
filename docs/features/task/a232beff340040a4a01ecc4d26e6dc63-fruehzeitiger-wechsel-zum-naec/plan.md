# Umsetzungsplan: Auto-Fortsetzung nur am tatsächlichen Medienende — konfigurierbarer Vorlauf entfernen

## Übersicht

Der positionsgetriebene Vorlauf der Auto-Fortsetzung wird ersatzlos entfernt: Der Wechsel zur nächsten Episode bzw. zum nächsten Playlist-Eintrag erfolgt künftig ausschließlich über `MediaEnded` → `PlaybackEndedMessage` → `AppUpdate.UpdatePlaybackEnded` (mit bestehendem `IsPlaybackEndPlausible`-Gate). Die Einstellung `AutoResumeRemainingSeconds` wird aus `AppSettings`, `AppSettingsService`, `SettingsPage` und allen Tests gestrichen; `AutoResumeEnabled`, die „angesehen"-Markierung über `WatchedEndWindowSeconds` und deren Streak-Plausibilisierung bleiben unverändert. Betroffen sind `VideoPlayer.Storage`, `VideoPlayer.Mvu`, `VideoPlayer.Maui`, `VideoPlayer.Maui.Tests`, `VideoPlayer.E2E.Tests` und die Hilfedokumentation.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| `AppSettings.EndWindow` | **Entfernen**; die zwei Konsumenten (`AppUpdate.EvaluateEndWindow` Z. 3883, `PlayerPage.ReportPosition` Z. 190) direkt auf `TimeSpan.FromSeconds(WatchedEndWindowSeconds)` umstellen | Die Property existierte ausschließlich, um das Maximum zweier Fenster zu bilden. Nach Entfernung des Fortsetzungsfensters wäre sie ein reiner Alias, der weiterhin „zwei Fenster" suggeriert — ein Alias mit irreführender Semantik ist schlechter als der direkte Bezug auf die verbliebene Einstellung (Empfehlung der Anforderung, Offene Frage 1). |
| `EndWindowStreak` / `IsProgressTickPlausible` / `EndWindowRequiredTicks` | **Beibehalten** (keine Vereinfachung) | Die Streak-Plausibilisierung schützt weiterhin die „angesehen"-Markierung und das optionale `DeleteWatchedDownloadEffect` vor einzelnen Ausreißer-Takten gestörter Streams — genau dafür wurde sie in RC.29 eingeführt. Eine Vereinfachung (Markierung ab erstem Fenstertakt) würde diesen Schutz abbauen; die Anforderung betrifft nur den Fortsetzungs-Vorlauf, nicht die Markierung. |
| Struktur der Streak-Bedingung in `EvaluateEndWindow` | **Beibehalten** (`remaining <=` Fenster + `EndWindowStreak >= EndWindowRequiredTicks`), Fensterquelle wechselt von `Settings.EndWindow` auf `WatchedEndWindowSeconds` | Minimaler Eingriff; Streak-Fenster und Markierungs-Schwelle sind danach fachlich identisch (beide `WatchedEndWindowSeconds`), die getrennten Prüfungen bleiben aber für künftige Divergenz und Lesbarkeit erhalten. |
| Persistierter `AutoResumeRemainingSeconds`-Schlüssel | **Verwaist lassen** — kein Migrationscode, kein `ISettingsStore.Remove` | Per Vorgabe; der Schlüssel wird schlicht nicht mehr gelesen/geschrieben. |
| Fortschrittstakt-Tests mit Advance-Erwartung | **Zu Negativ-Tests umwidmen** statt löschen („kein Wechsel im Fenster, auch nicht beim zweiten bestätigten Takt") | Der Wechsel-Nachweis liegt vollständig in den bestehenden `PlaybackEnded_*`-Tests; die umgewidmeten Takt-Tests pinnen die neue Invariante, dass kein Fortschrittstakt mehr einen Advance auslöst — genau der Regressionsschutz, den die Anforderung braucht. |

## Programmabläufe

### Fortschrittstakt (geändert — ohne Auto-Advance)

1. `PlayerPage.ReportPosition` dispatcht `PlaybackProgressMessage` mit `appliedMediaId` (Timer, unverändert).
2. `AppUpdate.UpdatePlaybackProgress`: `IsActive`-Guard, `MediaId`-Guard (nachlaufende Takte des Vorgängermediums verwerfen), Position/Dauer/`IsPlaying`/`IsBuffering` übernehmen, `ReportPlaybackProgressEffect` bei geänderter Position (> 0,5 s).
3. `EvaluateEndWindow`: bei bekannter Dauer Streak-Aufbau (`IsProgressTickPlausible` + `remaining <= WatchedEndWindowSeconds`) bzw. Streak-Reset; ab `EndWindowStreak >= EndWindowRequiredTicks` und `remaining <= WatchedEndWindowSeconds` → `IsWatched = true` und ggf. `DeleteWatchedDownloadEffect` (`AutoDeleteWatchedEpisodes` + `tvshowepisode`). **Keine** Playlist-/Serien-Advance-Zweige mehr; `IsResolving` sperrt weiterhin jede Fensterwirkung.
4. `MarkEpisodeWatchedInDetails` überträgt `IsWatched` in die geladene Episodenliste; Rückgabe Transition mit gesammelten Effekten.

Beteiligte Klassen/Komponenten: `PlayerPage`, `AppUpdate`, `PlayerState`, `AppSettings`, `ReportPlaybackProgressEffect`, `DeleteWatchedDownloadEffect`.

### Wiedergabeende (unverändert — einziger Auto-Advance-Pfad)

1. `MediaElement` feuert `MediaEnded` → `PlayerPage.OnMediaEnded` → `PlaybackEndedMessage`.
2. `AppUpdate.UpdatePlaybackEnded`: `IsActive` + `IsPlaybackEndPlausible` (`Duration > 0`, `Position >= 0`, `Duration - Position <= PlaybackEndedToleranceSeconds`) — unplausible Ereignisse werden unverändert verworfen.
3. Plausibles Ende: `IsWatched`, Endpositions-Meldung (`ReportPlaybackProgressEffect`), optionales `DeleteWatchedDownloadEffect`.
4. Bei `AutoResumeEnabled`: Playlist-Kontext → `AdvancePlaylistEntry` (`AdvancePlaylistEntryEffect`, `play/advance`), sonst `AdvanceToNextEpisode` (`ResolvePlaybackSourceEffect` der Folge-Episode); `MarkEpisodeWatchedInDetails` vorab.

Beteiligte Klassen/Komponenten: `PlayerPage`, `AppUpdate`, `PlayerState`, `AppSettings`, `AdvancePlaylistEntry`, `AdvanceToNextEpisode`, `ShellAppEffectHandler`.

### Einstellungen speichern/laden (geändert — ohne `AutoResumeRemainingSeconds`)

1. `AppSettingsService.Load` liest alle Schlüssel außer `AutoResumeRemainingSecondsKey` (entfernt) und liefert `Normalize()`d `AppSettings` — ein alter persistierter Schlüsselwert wird ignoriert.
2. `SettingsPage`: `AutoResumeSwitch` → `OnAutoResumeToggled` → `Save` → `SaveSettingsRequestedMessage` → `PersistSettingsEffect` → `AppSettingsService.Save` (schreibt den Schlüssel nicht mehr).
3. `SaveNumeric`/`SetTextIfNotFocused`/`WireNumeric` bleiben für die drei verbleibenden numerischen Entries (`PreloadEpisodeCountEntry`, `DownloadQuotaGbEntry`, `WatchedEndWindowEntry`) bestehen.

Beteiligte Klassen/Komponenten: `SettingsPage`, `AppSettingsService`, `ISettingsStore`, `AppUpdate` (Save/Load-Übergänge), `ShellAppEffectHandler`.

## Neue Klassen

Keine — die Anforderung ist eine reine Entfernungs-/Vereinfachungsaufgabe.

## Änderungen an bestehenden Klassen

### `AppSettings` (Datenmodell, `VideoPlayer.Storage/AppSettings.cs`)

- **Entfernte Eigenschaften:** `AutoResumeRemainingSeconds` (Z. 23–27, inkl. Doc) — Fortsetzungsvorlauf entfällt; `EndWindow` (Z. 38–48) — Alias ohne zweites Fenster.
- **Geänderte Methoden:** `Normalize()` — Clamp-Zeile `AutoResumeRemainingSeconds` (Z. 76) entfernen; `WatchedEndWindowSeconds`-Clamp (Z. 77) bleibt.
- **Unverändert:** `AutoResumeEnabled`, `WatchedEndWindowSeconds`, alle übrigen Eigenschaften.

### `AppSettingsService` (Persistenz, `VideoPlayer.Storage/AppSettingsService.cs`)

- **Entfernte Elemente:** Konstante `AutoResumeRemainingSecondsKey` (Z. 19–20), `Load()`-Zeile (Z. 62), `Save()`-Zeile (Z. 82).
- **Hinweis:** Der persistierte `Preferences`-Schlüssel `"AutoResumeRemainingSeconds"` bleibt verwaist (Vorgabe — keine Migration, kein `Remove`-Aufruf).

### `AppUpdate` (MVU-Kern, `VideoPlayer.Mvu/AppUpdate.cs`)

- **Geänderte Methoden:** `EvaluateEndWindow` (Z. 3876–3961) — beide Advance-Blöcke entfernen (Playlist Z. 3924–3937, Serien Z. 3939–3951) samt `AutoResumeRemainingSeconds`-/`AutoResumeEnabled`-/`IsPlaying`-Advance-Bedingungen und den Advance-Kommentaren (Z. 3916–3923); Fensterquelle `model.Settings.EndWindow` (Z. 3883) auf `TimeSpan.FromSeconds(model.Settings.WatchedEndWindowSeconds)` umstellen (lokale Variable `endWindow` entfällt oder wird ersetzt). Streak-Aufbau, Watched-Markierung, `DeleteWatchedDownloadEffect`, `IsResolving`-Sperre und `Duration <= 0`-Reset bleiben.
- **Geänderte Methoden:** `UpdatePlaybackProgress` — Doc-Kommentar (Z. 3791–3808) um den Fortsetzungs-Vorlauf kürzen: der Takt meldet Fortschritt, baut die Fenster-Streak auf und markiert „angesehen" — er schaltet nicht mehr weiter.
- **Geänderte Elemente:** Doc-Kommentar `EvaluateEndWindow` (Z. 3853–3868) — Advance-Auslöser entfernen; Doc `PlaybackEndedToleranceSeconds` (Z. 77–87) — Formulierung „greift die Auto-Fortsetzung bereits" fachlich anpassen (die Toleranz wird zum alleinigen Ende-Gate); Doc `EndWindowRequiredTicks` (Z. 90–97) — „Auto-Fortsetzung" aus der Auslöser-Liste streichen.
- **Unverändert:** `UpdatePlaybackEnded`, `IsPlaybackEndPlausible`, `IsProgressTickPlausible`, `AdvanceToNextEpisode`, `AdvancePlaylistEntry`, `UpdatePlaylistNavigationCompleted`, `MarkEpisodeWatchedInDetails` — kein neuer Mechanismus, nur Entfernung.

### `PlayerState` (Datenmodell, `VideoPlayer.Mvu/PlayerState.cs`)

- **Geänderte Elemente:** Doc-Kommentar `EndWindowStreak` (Z. 128–134) — „und der Auto-Fortsetzung" entfernen; die Streak bestätigt nur noch die „angesehen"-Schwelle. Property selbst bleibt.

### `SettingsPage` (UI, `VideoPlayer.Maui/Pages/SettingsPage.xaml`)

- **Entfernte Elemente:** Block „Zeitfenster Auto-Fortsetzung (Sekunden Restlaufzeit)" mit `AutoResumeRemainingEntry` (Z. 121–131) sowie eine der beiden umschließenden `BoxView`-Trennlinien (Z. 118–120 oder Z. 133–135 — so entfernen, dass zwischen `AutoResumeSwitch` und `WatchedEndWindowEntry` genau eine Trennlinie bleibt; Layout-Muster wie in der Downloads-Karte).
- **Unverändert:** `AutoResumeSwitch` (Label „Auto-Fortsetzung", bool) und `WatchedEndWindowEntry` bleiben — die Wiedergabe-Karte enthält danach Schalter, Trennlinie, Watched-Entry.

### `SettingsPage` (Code-Behind, `VideoPlayer.Maui/Pages/SettingsPage.xaml.cs`)

- **Entfernte Elemente:** `WireNumeric(AutoResumeRemainingEntry, OnAutoResumeRemainingCompleted)` (Z. 38), `SetTextIfNotFocused(AutoResumeRemainingEntry, settings.AutoResumeRemainingSeconds)` (Z. 81), Handler `OnAutoResumeRemainingCompleted` (Z. 144–147).

### `PlayerPage` (App-Rand, `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`)

- **Geänderte Methoden:** `ReportPosition` — Fensterquelle des `LogDebug`-Blocks von `model.Settings.EndWindow` (Z. 190) auf `TimeSpan.FromSeconds(model.Settings.WatchedEndWindowSeconds)` umstellen; Kommentar (Z. 185–188) „…darf „angesehen"/Auto-Fortsetzung auslösen" anpassen (die Diagnose belegt jetzt Takte im „angesehen"-Fenster). `appliedMediaId`-Dispatch und `OnMediaEnded` bleiben unverändert.

### Hilfedokumentation (`docs/help/`)

- **`wiedergabe-pip.md`:** Abschnitt „Automatische Fortsetzung" (Z. 130–185) überarbeiten — positionsgetriebener Wechsel, eingestelltes Zeitfenster und „bestätigter Fenstereintritt" als Fortsetzungs-Voraussetzung entfallen; der Abschnitt beschreibt künftig den Wechsel ausschließlich am Wiedergabeende. Der „Bestätigter Fenstereintritt"-Text bleibt für die „angesehen"-Markierung erhalten (Abschnitt „Fortschritt/Angesehen" bzw. technischer Hintergrund Z. 172–185: `AppSettings.EndWindow`-Verweis entfernen, „beide Fortsetzungspfade" streichen). Verweis in Z. 121–128 auf „siehe Automatische Fortsetzung" anpassen.
- **`einstellungen-datenhaltung.md`:** Eintrag „Zeitfenster Auto-Fortsetzung" (Z. 36–38) entfernen; den Eintrag „Auto-Fortsetzung" (Z. 34–35) ggf. auf „Wechsel zum nächsten Titel am Wiedergabeende" präzisieren.
- **`index.md`:** Einleitungssatz zu `wiedergabe-pip.md` (Z. 16) — „Auto-Fortsetzung erst bei bestätigtem Endfenster" auf den Ende-Pfad umformulieren.

## Datenbankmigrationen

Keine. Der persistierte `Preferences`-Schlüssel `AutoResumeRemainingSeconds` bleibt per Vorgabe verwaist (kein `Remove`, keine Migration).

## Validierungsregeln

Keine neuen. Die bestehende `SaveNumeric`-/`InvalidInputLabel`-Validierung der `SettingsPage` bleibt für die drei verbleibenden numerischen Entries bestehen; die Schranken 30–3600 für `WatchedEndWindowSeconds` bleiben über `Normalize()` gewahrt.

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `AutoResumeRemainingSeconds` (`AppSettings` + `AppSettingsService.AutoResumeRemainingSecondsKey`) | `int` | (bisher 300, 30–3600) | **Entfällt** — Zeitfenster der Auto-Fortsetzung; kein Ersatz, das Verhalten ist nicht mehr konfigurierbar |

Bestehen bleiben: `AutoResumeEnabled` (bool, Standard `true`), `WatchedEndWindowSeconds` (int, 30–3600, Standard 300). Interne Konstanten `PlaybackEndedToleranceSeconds` (300) und `EndWindowRequiredTicks` (2) bleiben — keine neue Konfiguration.

## Seiteneffekte und Risiken

- **Hängender Stream ohne `MediaEnded`:** Bleibt ein Stream still stehen, ohne je ein Ende-Ereignis zu liefern, schaltet die App künftig nie automatisch weiter — zuvor griff der Positionsvorlauf. Das ist die ausdrücklich gewünschte fachliche Konsequenz („jedes Video bis zum tatsächlichen Ende"); als bekanntes Risiko dokumentiert.
- **Verfrühtes `MediaEnded` außerhalb des Toleranzfensters:** Ein Ereignis jenseits der 300-s-Toleranz wird weiterhin verworfen; feuert danach kein weiteres Ende, gibt es keinen Wechsel (fachlich gewollt).
- **Watched-Markierung sehr kurzer Medien:** Medien, die das Ende erreichen, bevor zwei Fenstertakte liefen, werden weiterhin erst über den `PlaybackEnded`-Pfad als „angesehen" markiert — unverändertes Verhalten.
- **Tests:** Zahlreiche `PlaybackProgress_*`-Tests pinnen den bisherigen Vorlauf und brechen mit der Entfernung (siehe Tests). Fixture-Belegungen `AutoResumeRemainingSeconds` werden Kompilierfehler erzeugen und müssen zeitgleich entfernt werden.
- **E2E-Timing:** Der Wechsel erfolgt nun beim echten `MediaEnded` statt „sofort im Fenster" — das kurze Testvideo sollte das Ende innerhalb der 60-s-Wartezeiten weiterhin erreichen (im Testlauf zu verifizieren); die `IsPlaybackEndPlausible`-Prüfung ist für das kurze Video erfüllt (Restlaufzeit stets < 300 s).
- **Dokumentation:** `docs/help/wiedergabe-pip.md`, `docs/help/einstellungen-datenhaltung.md` und `docs/help/index.md` beschreiben den Vorlauf noch — Pflicht-Nachzug (siehe Umsetzungsreihenfolge).
- **`EndWindow`-Entfernung:** Alle Referenzen (`EvaluateEndWindow`, `ReportPosition`, `AppSettingsTests.EndWindow_IsMaxOfBothWindows`, `docs/help/wiedergabe-pip.md` Z. 177) müssen zeitgleich angepasst werden — sonst Kompilier-/Testbruch.

## Umsetzungsreihenfolge

1. **Einstellung und Persistenz entfernen**
   - Voraussetzungen: Keine.
   - Beschreibung: `AppSettings`: `AutoResumeRemainingSeconds`, `EndWindow` und die `Normalize()`-Clamp-Zeile entfernen. `AppSettingsService`: `AutoResumeRemainingSecondsKey` sowie die zugehörigen `Load()`-/`Save()`-Zeilen entfernen.

2. **MVU-Kern bereinigen**
   - Voraussetzungen: Schritt 1 (`AppSettings.EndWindow` ist entfernt — `EvaluateEndWindow` muss zeitgleich kompilierbar bleiben).
   - Beschreibung: In `AppUpdate.EvaluateEndWindow` beide Advance-Blöcke (Playlist/Serie) entfernen, Fensterquelle auf `WatchedEndWindowSeconds` umstellen; Doc-Kommentare von `UpdatePlaybackProgress`, `EvaluateEndWindow`, `PlaybackEndedToleranceSeconds` und `EndWindowRequiredTicks` anpassen. Doc-Kommentar `PlayerState.EndWindowStreak` anpassen.

3. **App-Rand: Diagnose-Fensterquelle umstellen**
   - Voraussetzungen: Schritt 1 (`AppSettings.EndWindow` ist entfernt).
   - Beschreibung: `PlayerPage.ReportPosition`: `LogDebug`-Fensterbedingung auf `WatchedEndWindowSeconds` umstellen, Kommentar anpassen.

4. **Settings-UI bereinigen**
   - Voraussetzungen: Schritt 1 (`AutoResumeRemainingSeconds` ist entfernt — Code-Behind muss zeitgleich kompilierbar bleiben).
   - Beschreibung: `AutoResumeRemainingEntry`-Block inkl. einer Trennlinie aus `SettingsPage.xaml` entfernen; `WireNumeric`-Aufruf, `SetTextIfNotFocused`-Zeile und `OnAutoResumeRemainingCompleted` aus `SettingsPage.xaml.cs` entfernen.

5. **Unit-/Contract-Tests anpassen**
   - Voraussetzungen: Schritte 1–4 (sonst Kompilierfehler durch entfernte Member).
   - Beschreibung: Alle `AutoResumeRemainingSeconds`-/`AutoResumeRemainingEntry`-/`EndWindow`-Referenzen in `AppSettingsTests`, `AppSettingsServiceTests`, `SettingsStorageContractTests` entfernen; `PlaybackProgress_*`-Tests mit Advance-Erwartung in `AppUpdateTests_Player`/`AppUpdateTests_PlaylistPlayback` zu „kein Wechsel im Fenster"-Negativ-Tests umwidmen (siehe Tests); neuen `PlaybackEnded_AutoResumeDisabled_DoesNotAdvance` (Serie) ergänzen; `dotnet test VideoPlayer.Maui.Tests` ausführen.

6. **E2E-Tests anpassen und verifizieren**
   - Voraussetzungen: Schritte 1–4 (verändertes Laufzeitverhalten); `VideoPlayer.E2E.Tests`-Infrastruktur (`AppFixture`, `MockVideoServer`, `testvideo.mp4` als `Content`) ist vorhanden.
   - Beschreibung: Kommentare der Auto-Advance-E2E-Tests auf den `MediaEnded`-Pfad umstellen (`PlaybackEnd_AdvancesToNextPlaylistEntry`, `ContinueWatchingPlaylistEntry_AdvancesByPlaylistOrder_NotSeries`, `TapContinueWatchingEpisode_OpensDetailsAndStartsPlaybackAndAdvancesToNextEpisode`, `PlayButton_StartsPlaybackAtFirstPlayableEntry`, `NextAndPreviousButtons_NavigatePlaylist`); Testlauf `VideoPlayer.E2E.Tests` ausführen und prüfen, ob die 60-s-Wartezeiten das echte Ende des Testvideos abdecken (Timeouts ggf. erhöhen).

7. **Hilfedokumentation nachziehen**
   - Voraussetzungen: Schritte 1–6 (Dokumentation beschreibt das Zielverhalten).
   - Beschreibung: `docs/help/wiedergabe-pip.md`, `docs/help/einstellungen-datenhaltung.md`, `docs/help/index.md` wie oben beschrieben aktualisieren.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `PlaybackEnded_AutoResumeDisabled_DoesNotAdvance` | `AppUpdateTests_Player` | Ende-Ereignis + `AutoResumeEnabled = false` + Folge-Episode vorhanden → `IsWatched` + Abschlussmeldung, aber kein `ResolvePlaybackSourceEffect`, MediaId bleibt (Schließt die Lücke, die `PlaybackProgress_AutoResumeDisabled_DoesNotAdvance` als Takt-Test hinterlässt; Playlist-Gegenstück `PlaybackEnded_InPlaylist_AutoResumeOff_NoAdvance` existiert bereits) |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `AppSettingsTests.Defaults_AreExpected` | `AutoResumeRemainingSeconds`-Assert (Z. 17) entfernen |
| `AppSettingsTests.Normalize_ClampsAutoResumeRemainingSeconds` | Entfällt — Property und Clamp entfernt |
| `AppSettingsTests.EndWindow_IsMaxOfBothWindows` | Entfällt — `EndWindow` entfernt |
| `AppSettingsServiceTests.Save_ThenLoad_RoundTrips` | Fixture-Zeile `AutoResumeRemainingSeconds = 600` (Z. 37) entfernen |
| `SettingsStorageContractTests.SettingsPage_ContainsAllSettingControls` | `"AutoResumeRemainingEntry"` aus `ExpectedAutomationIds` (Z. 27) entfernen; Entry-Zählung 4 → 3 und Kommentar (Z. 57–59) anpassen |
| `AppUpdateTests_Player.PlaybackProgress_AutoResumeAdvancesWhenEnabled` | Advance-Erwartung entfällt → Negativ-Test „zwei bestätigte Fenstertakte lösen keinen Wechsel aus" (MediaId bleibt 31, kein `ResolvePlaybackSourceEffect`); Fixture-Zeile `AutoResumeRemainingSeconds` (Z. 340) entfernen; Umbenennung (z. B. `PlaybackProgress_ConfirmedWindow_DoesNotAdvance`) |
| `AppUpdateTests_Player.PlaybackProgress_AutoResumeDisabled_DoesNotAdvance` | Entfällt bzw. durch neuen `PlaybackEnded_AutoResumeDisabled_DoesNotAdvance` ersetzt — die Schalter-Wirkung liegt jetzt ausschließlich im Ende-Pfad |
| `AppUpdateTests_Player.PlaybackProgress_AboveResumeWindow_DoesNotAdvance` | `AutoResumeRemainingSeconds`-Fixture (Z. 393) entfernen; bleibt als Streak-Reset-Test bei Takt außerhalb des Watched-Fensters gültig (Asserts: Streak 0, kein Advance — Letzteres ist jetzt invariant wahr), Kommentar/Umbenennung anpassen |
| `AppUpdateTests_Player.PlaybackProgress_PausedWithinWindow_DoesNotAdvance` | `AutoResumeRemainingSeconds`-Fixture (Z. 415) entfernen; verbleibt als Pausentakt-Test (Deckung mit `PlaybackProgress_PausedTick_ResetsStreak` prüfen — ggf. zusammenführen/entfernen) |
| `AppUpdateTests_Player.PlaybackProgress_NoNextEpisode_DoesNotAdvance` | Advance-Bedingung entfällt — Test wird trivial; entfernen oder zu „keine Fensterwirkung ohne Folge-Episode" umwidmen |
| `AppUpdateTests_Player.PlaybackProgress_SingleWindowTick_DoesNotAdvance` | Bleibt funktional gültig (Streak 1, kein Watched, kein Wechsel); Doc-Kommentar anpassen |
| `AppUpdateTests_Player.PlaybackProgress_SecondWindowTick_AdvancesAndMarksWatched` | Advance-Asserts entfernen (MediaId bleibt 31, kein `ResolvePlaybackSourceEffect`, Streak bleibt 2); Watched- und Details-Markierungs-Asserts bleiben; Umbenennung (z. B. `PlaybackProgress_SecondWindowTick_MarksWatched`) |
| `AppUpdateTests_Player.PlaybackProgress_PositionJump_SingleTick_NoAdvance` | Zweiter Teil: zweiter Takt markiert „angesehen" statt zu wechseln (Advance-Assert Z. 600–608 → Watched-Assert) |
| `AppUpdateTests_Player.PlaybackProgress_FrozenPosition_ResetsStreak`, `PlaybackProgress_ShrunkDuration_ResetsStreak`, `PlaybackProgress_PausedTick_ResetsStreak`, `PlaybackProgress_WrongMediaId_IsDiscarded`, `PlaybackProgress_WhileResolving_NoWindowEffects`, `PlaybackProgress_MarksWatchedWithinWindow`, `PlaybackProgress_WatchedWindow_MarksEpisodeWatchedInDetails` | Bleiben fachlich gültig (Streak/Watched unverändert); ggf. `DoesNotContain(ResolvePlaybackSourceEffect)`-Asserts und Kommentare an die neue Invariante anpassen |
| `AppUpdateTests_PlaylistPlayback.PlaybackProgress_NearEnd_TwoTicks_AdvancesPlaylist` | Advance-Erwartung entfällt → Negativ-/Watched-Test „zwei Fenstertakte markieren `IsWatched`, aber kein `AdvancePlaylistEntryEffect`"; Umbenennung (z. B. `PlaybackProgress_NearEnd_TwoTicks_MarksWatchedOnly`); Wechsel-Nachweis liegt in `PlaybackEnded_InPlaylist_AdvancesPlaylist` |
| `AppUpdateTests_PlaylistPlayback.PlaybackProgress_NearEnd_WhileResolving_NoSecondAdvance` | Bleibt gültig (Streak 0, kein Advance); Doc-Kommentar „kein weiteres Advance" anpassen |
| `AppUpdateTests_PlaylistPlayback.PlaybackProgress_InPlaylist_ReportsPlaylistId` | Kommentar „Auto-Fortsetzung abgeschaltet" anpassen (Fortsetzung greift im Takt nicht mehr — Fixture `AutoResumeEnabled = false` kann entfallen oder für Eindeutigkeit bleiben) |
| `AppUpdateTests_Downloads.PlaybackProgress_TwoWindowTicks_EmitsAutoDelete` | Bleibt fachlich gültig (Watched + Delete ab 2. Takt); kein Advance-Assert vorhanden — ggf. Doc-Kommentar prüfen |
| `PlayerContractTests` | Voraussichtlich unbetroffen: der `ReportPosition`-Assert prüft nur `try`/`catch` (Z. 384–386), kein `EndWindow`; `appliedMediaId`-/`PlaybackEndedMessage`-Pins bleiben — im Testlauf bestätigen |

### E2E-Tests (Pflicht)

Der geänderte Benutzerablauf „Wechsel erst am tatsächlichen Medienende" ist durch die bestehenden Auto-Advance-Szenarien abgedeckt — sie werden angepasst (Kommentare/Timing), nicht neu geschrieben. Ein E2E-Test für das entfernte Einstellungsfeld entfällt (Entfernung, keine neue Interaktion; `SettingsStorageContractTests` pinnt die reduzierte Elementmenge).

| Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium |
|----------|------------------------|-------------------------------|
| Wechsel zum Folge-Playlist-Eintrag am echten Medienende über `play/advance` | `PlaylistPlaybackTests.PlaybackEnd_AdvancesToNextPlaylistEntry` | Auto-Fortsetzung erfolgt ausschließlich über `MediaEnded` (Happy Path) |
| Playlist-Weiterschauen: Folgevideo aus Playlist-Reihenfolge am Medienende | `PlaylistPlaybackTests.ContinueWatchingPlaylistEntry_AdvancesByPlaylistOrder_NotSeries` | Ende-Pfad im Playlist-Kontext, kein Serienpfad |
| Serien-Fortsetzung zur Folge-Episode am echten Medienende | `ContinueWatchingPlaybackTests.TapContinueWatchingEpisode_OpensDetailsAndStartsPlaybackAndAdvancesToNextEpisode` | Ende-Pfad im Serienkontext |

Anzupassende bestehende E2E-Tests:

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `PlaylistPlaybackTests.PlaybackEnd_AdvancesToNextPlaylistEntry` | Kommentar „kurzes Testvideo liegt sofort im Fortsetzungsfenster" (Z. 232–234) auf den `MediaEnded`-Pfad umstellen; 60-s-Timeout im Testlauf verifizieren |
| `PlaylistPlaybackTests.ContinueWatchingPlaylistEntry_AdvancesByPlaylistOrder_NotSeries` | Kommentar „Auto-Fortsetzung" (Z. 358–359) anpassen; Timeout verifizieren |
| `PlaylistPlaybackTests.PlayButton_StartsPlaybackAtFirstPlayableEntry` | Stabiler Endzustand „letzter Eintrag" beruht auf Auto-Fortsetzung — Kommentar (Z. 97–100) anpassen, Timeout verifizieren |
| `PlaylistPlaybackTests.NextAndPreviousButtons_NavigatePlaylist` | Ausgangsstand via Auto-Fortsetzung + Kommentar „Fortschritts-Takt (5 s) lässt den Wechsel beobachtbar" (Z. 157–159) anpassen — der Beobachtbarkeits-Kommentar entfällt/ändert sich, Timeout verifizieren |
| `ContinueWatchingPlaybackTests.TapContinueWatchingEpisode_OpensDetailsAndStartsPlaybackAndAdvancesToNextEpisode` | Kommentar „kurzes Testvideo liegt im Fortsetzungsfenster" (Z. 123–125) anpassen; Timeout verifizieren |

## Offene Punkte

Keine.
