# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Ende-Ereignis nur bei Plausibilisierung gegen Wiedergabestand (Position nahe/an Gesamtdauer, feste Zeit-Schwelle) | `IsPlaybackEndPlausible` + Guard in `UpdatePlaybackEnded` (Programmablauf Schritt 3, Konstante `PlaybackEndedToleranceSeconds = 300`) | `PlaybackEnded_WithinTolerance_Completes`, `PlaybackEnded_JustOutsideTolerance_IsIgnored`, `PlaybackEnded_MidPlayback_IsIgnored` | Abgedeckt |
| Nicht plausible Ereignisse lösen weder Auto-Vorlauf/Folgevideo noch Angesehen-Markierung und End-Fortschrittsmeldung aus | Verwerfen via `AppTransition.Unchanged` vor jeder Effekt-Erzeugung und vor der Kontextverzweigung | `PlaybackEnded_MidPlayback_IsIgnored` (keine Effekte, `IsWatched` bleibt false), `PlaybackEnded_UnknownDuration_IsIgnored`, `PlaybackEnded_InPlaylist_MidPlayback_IsIgnored` | Abgedeckt |
| Beim tatsächlichen Ende läuft der bestehende Abschluss-Ablauf unverändert (Angesehen, End-Fortschrittsmeldung, Auto-Fortsetzung) | Bestehender Ablauf nach bestandener Prüfung unverändert | Bestehende Tests `PlaybackEnded_MarksWatchedAndAdvances`, `PlaybackEnded_WithoutNext_MarksWatchedOnly`, `PlaybackEnded_InPlaylist_AdvancesPlaylist` u. a. bleiben plausibel angesetzt und unverändert grün; angepasste Tests `PlaybackEnded_WatchedEpisode_EmitsAutoDelete`, `PlaybackEnded_WithoutPlaylist_KeepsSeriesAdvance` | Abgedeckt |
| Einheitlich für Serien-/Einzel- und Playlist-Kontext | Prüfung sitzt vor der Verzweigung `AdvancePlaylistEntry`/`AdvanceToNextEpisode` | Serienpfad: `PlaybackEnded_MidPlayback_IsIgnored`, `PlaybackEnded_AfterAdvance_IgnoresStaleEvent`; Playlistpfad: `PlaybackEnded_InPlaylist_MidPlayback_IsIgnored` | Abgedeckt |
| Schwelle als fester Wert, kein neues konfigurierbares Verhalten | `private const int PlaybackEndedToleranceSeconds`, kein `AppSettings`-Eintrag; `installation`/Konfiguration nicht betroffen | Indirekt über Grenzfall-Tests (301 s verworfen / 300 s akzeptiert) | Abgedeckt |
| Bei unbekannter/0-Gesamtdauer nicht als Ende werten | `Duration <= 0` → verwerfen (in `IsPlaybackEndPlausible`) | `PlaybackEnded_UnknownDuration_IsIgnored` | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Positiver Pfad: echtes Wiedergabeende → Angesehen + Auto-Fortsetzung (Playlist) | bestehender `PlaylistPlaybackTests.PlaybackEnd_AdvancesToNextPlaylistEntry` (20-s-Testvideo, weiterhin grün) | Abgedeckt (Bestand) |
| Negativpfad: verfrühtes `MediaEnded` verwerfen | — | Nicht erforderlich mit Begründung: Das Ereignis ist nicht deterministisch über die UI auslösbar (Stream-Abbruch/Formatwechsel nicht steuerbar); die Regel ist über die reine Update-Funktion vollständig unit-testbar. Begründung steht im Plan. |

## Hinweise

- Die vier angepassten Bestandstests sind vollständig benannt; ihre neue Positions-/Dauer-Belegung hält sie im plausiblen Fenster (Dauer ≫ 300 s, Position am Ende).
- E2E-Infra vorhanden (FlaUI, maui-windows-Workload installiert); ein E2E-Lauf ist für den Funktionsnachweis nicht erforderlich — sollte er im Testschritt ausgeführt werden, ist das Bestandsszenario `PlaybackEnd_AdvancesToNextPlaylistEntry` der relevante Regressionstest.
- Keine Datenbankmigration, keine Konfigurationsänderung, keine UI-Änderung — die entsprechenden Planabschnitte sind korrekt leer.
