# Übersetzte Anforderung: Frühzeitiger Wechsel zum nächsten Video bei fehlgeschlagenem Fortsetzungs-Seek

## Fachliche Zusammenfassung

Fehlerbehebung im Wiedergabepfad der `PlayerPage`: Während der
Wiedergabe einer Episode (Serienkontext, Quelle `ServerStream`) wird die
laufende Wiedergabe ungewollt beendet und automatisch die Folge-Episode
gestartet. Als Fehlerquelle ist das beigefügte Programmprotokoll
`videoplayer (5).log` (Repository-Root) zu analysieren — dort zeigt sich
der Ablauf reproduzierbar mehrfach: Die Wiedergabe der Episode „Der
Schulwechsel" (`tvshowepisode/21599`) startet mit Fortsetzungsposition
00:17:04; elf Sekunden später schlägt der Fortsetzungs-Seek
(`ApplyResumeSeekAsync` → `MediaElement.SeekTo`) mit einer
`TaskCanceledException` fehl — der interne 10-Sekunden-Timeout
(`ResumeSeekTimeout`) der Toolkit-Vollendung. Die Seite dispatcht darauf
`PlaybackFailedMessage`, und `AppUpdate.UpdatePlaybackFailed` fällt über
`AdvanceToNextEpisode` auf die nächste Episode „Normale Helden"
(`tvshowepisode/21600`) zurück. Dasselbe Muster tritt im Protokoll auch
bei „Der Wolf im Schafspelz" → „Kennen Sie Tribbles?" sowie „Meister
der Sklaven" → „Epigonen" auf — jeweils `ServerStream` mit gesetzter
Fortsetzungsposition.

Erwartetes Verhalten: Ein fehlschlagender oder zeitüberschreitender
Fortsetzungs-Seek darf die laufende Wiedergabe nicht beenden und keinen
Wechsel zur nächsten Episode auslösen — die Wiedergabe läuft weiter
(an der vom Player erreichten Position bzw. ohne angewendeten Sprung,
Degradation statt Abbruch). Der Episoden-Rückfall bleibt echten
Abspielfehlern vorbehalten (nicht verfügbare/unsetzbare Quelle,
`MediaFailed`-Ereignis). Annahme zur Ursache, im Protokoll gestützt:
Die `SeekTo`-Vollendung des Toolkits bleibt unter dieser Konstellation
aus, während der native Sprung selbst möglicherweise bereits erfolgt
ist — aus Anwendersicht läuft das Video „mitten im Video" (an der
Fortsetzungsposition) und bricht erst beim Timeout zusammen; der
Fehlschlag ist folglich nicht als Wiedergabefehler, sondern als
nicht-fataler Fortsetzungs-Seek zu werten.

## Betroffene Klassen und Komponenten

- **App-Rand (`VideoPlayer.Maui`):**
  - `Pages/PlayerPage.xaml.cs` — `ApplyResumeSeekAsync` (SeekTo unter
    `ResumeSeekTimeout`, Fehlerpfad dispatcht derzeit
    `PlaybackFailedMessage`), `TryApplyPendingResumeSeek`,
    `pendingResumePosition`; `OnMediaFailed` und `FailPlaybackStart`
    bleiben unverändert die echten Fehlerpfade mit Episoden-Rückfall.
- **MVU-Kern (`VideoPlayer.Mvu`, plattformneutral):**
  - `AppUpdate` — `UpdatePlaybackFailed` / `AdvanceToNextEpisode`
    (Episoden-Rückfall); im Playlist-Kontext die Parallellogik
    `AdvancePlaylistEntry`. Annahme: entweder wird der
    Fortsetzungs-Seek-Fehlschlag gar nicht mehr als
    `PlaybackFailedMessage` gemeldet, oder es wird eine gesonderte,
    nicht-fatale Meldung eingeführt, die keinen Rückfall auslöst.
  - `AppMessage` — ggf. neue Message für den nicht-fatalen
    Fortsetzungs-Seek-Fehlschlag (Annahme).
- **Ablage/Server (`VideoPlayer.Storage`/`VideoPlayer.ApiClient`):**
  - keine Änderung erkennbar.
- **Tests:**
  - `VideoPlayer.Maui.Tests/PlayerContractTests` — Verträge des
    Fortsetzungs-Seeks (`ApplyResumeSeekAsync`, Gate
    `TryApplyPendingResumeSeek`); das geänderte Fehlerverhalten ist
    vertraglich zu pinnen (kein `PlaybackFailedMessage`-Dispatch beim
    Seek-Timeout/-Fehlschlag).
  - `VideoPlayer.Maui.Tests`/`VideoPlayer.Mvu`-Update-Tests
    (`AppUpdateTests_*`) — Regression: `PlaybackFailedMessage` löst
    weiterhin den Episoden-Rückfall aus; der neue Seek-Pfad löst ihn
    nicht aus.
  - `VideoPlayer.E2E.Tests/Tests/PlaybackTests.cs` — ggf. Szenario
    „Fortsetzungsposition: Wiedergabe läuft trotz Seek-Fehlschlag
    weiter" (Annahme; technische Nachbildbarkeit des Toolkit-Timeouts
    im E2E ist zu prüfen).

## Implementierungsansatz

- Analyse des Protokolls `videoplayer (5).log` als Beleg der
  Fehlerkette: `Wiedergabe geöffnet` mit Fortsetzung → `Fortsetzungs-
  Seek … fehlgeschlagen` (`TaskCanceledException` aus
  `MediaElement.SeekTo`) → `PlaybackFailedMessage` →
  `AdvanceToNextEpisode` → Start der Folge-Episode.
- Minimalvariante: `ApplyResumeSeekAsync` behandelt Timeout/Abbruch
  (`TaskCanceledException`/`OperationCanceledException`) — ggf. alle
  Ausnahmen des Seek-Aufrufs — als nicht-fatalen Pfad: protokollieren
  und die Wiedergabe unverändert fortsetzen, ohne
  `PlaybackFailedMessage` zu dispatchen. Der Episoden-Rückfall bleibt
  `OnMediaFailed`, `FailPlaybackStart` (ungültige/nicht setzbare
  Quelle) und der fehlgeschlagenen Quellauflösung vorbehalten.
- Zu prüfen (Annahme): Ob der native Sprung trotz ausbleibender
  Toolkit-Vollendung bereits wirksam war — dann wäre ein erneuter
  Seek-Versuch oder ein längeres Zeitlimit sinnlos bis kontraproduktiv;
  alternativ ist ein begrenzter Wiederholungsversuch denkbar.
- Gleiche Behandlung ist für den Playlist-Kontext zu erwägen, da der
  Rückfall dort über `AdvancePlaylistEntry` denselben Fehlerpfad nutzt.
- Erweiterungspunkte: `PlayerPage.ApplyResumeSeekAsync` (Fehlerpfad),
  `AppUpdate.UpdatePlaybackFailed` (Trennung fataler/nicht-fataler
  Fehler, falls eine eigene Message gewählt wird).

## Konfiguration

Keine neue Konfiguration erkennbar. Die bestehenden benutzerseitigen
Einstellungen (`AutoResumeEnabled`, `AutoResumeRemainingSeconds`,
`WatchedEndWindowSeconds`) bleiben unverändert; das interne Zeitlimit
`ResumeSeekTimeout` ist ein Implementierungsdetail der Seite — ob es
angepasst oder konfigurierbar werden soll, ist unter „Offene Fragen"
zu klären.

## Offene Fragen

- Soll ein fehlgeschlagener Fortsetzungs-Seek still degradiert werden
  (Wiedergabe läuft ohne Sprung bzw. an der vom Player erreichten
  Position weiter) oder soll ein sichtbarer Hinweis erscheinen — ohne
  den Episodenwechsel?
- Soll der Fortsetzungs-Seek nach einem Fehlschlag erneut versucht
  werden (mit begrenzter Anzahl) oder bleibt es bei einem einmaligen
  Versuch?
- Gilt die nicht-fatale Behandlung nur für Timeout/Abbruch
  (`TaskCanceledException`) oder für sämtliche Ausnahmen des
  Seek-Aufrufs?
- Das Protokoll zeigt das Muster ausschließlich bei `ServerStream`-
  Quellen mit Fortsetzungsposition — ist das Fehlverhalten
  plattformspezifisch (Windows/iOS/Android) bzw. tritt es auch bei
  `LocalFile` auf?
- Soll das Zeitlimit `ResumeSeekTimeout` (derzeit 10 s) angepasst
  werden — etwa weil die Toolkit-Vollendung bei Server-Streams
  regelmäßig ausbleibt — oder bleibt der Timeout bestehen und wird nur
  die Reaktion darauf korrigiert?
