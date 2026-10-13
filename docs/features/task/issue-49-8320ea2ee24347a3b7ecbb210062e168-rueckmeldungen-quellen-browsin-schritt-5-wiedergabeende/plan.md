# Umsetzungsplan: Plausibilitätsprüfung des Wiedergabe-Ende-Ereignisses

## Übersicht

Das `PlaybackEndedMessage` (ausgelöst durch `MediaElement.MediaEnded` über `PlayerPage.OnMediaEnded`) wird in `AppUpdate.UpdatePlaybackEnded` künftig gegen den Wiedergabestand plausibilisiert: Nur wenn die gemeldete Position innerhalb eines festen Toleranzfensters vor bzw. an der Gesamtdauer liegt, läuft der bestehende Abschluss (Angesehen-Markierung, End-Fortschrittsmeldung, Auto-Fortsetzung) an; andernfalls wird die Nachricht verworfen. Die Prüfung sitzt vor der Kontextverzweigung und gilt damit einheitlich für Serien-/Einzel- und Playlist-Kontext. `PlayerPage` bleibt unverändert — die Seite bleibt reiner Event-Übersetzer.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Ort der Prüfung | Zentral in `AppUpdate.UpdatePlaybackEnded` auf `PlayerState.Position`/`Duration` | Reiner, unit-testbarer MVU-Übergang; greift einheitlich für beide Kontexte vor jeder Effekt-Erzeugung; kein erneuter Zugriff auf das native `MediaElement` (kein Thread-Marshalling, kein Ausnahmerisiko im Event-Handler). Modell-Position hinkt der Element-Position maximal um den 5-s-Takt hinterher — weit unterhalb des Toleranzfensters. |
| Toleranzfenster | Konstante `PlaybackEndedToleranceSeconds = 300` (`private const int` in `AppUpdate`) | Orientierung am Angesehen-Endfenster `AppSettings.WatchedEndWindowSeconds` (Standard 300 s) gemäß Projektklärung — innerhalb dieses Fensters würde der Fortschrittstakt das Medium ohnehin als angesehen markieren bzw. die Auto-Fortsetzung bereits greifen; ein Ende-Ereignis dort ist mit dem Normalablauf konsistent. Feste Konstante, keine neue Einstellung. |
| Unbekannte/0-Gesamtdauer | Ereignis wird nicht als Ende gewertet (Nachricht verworfen) | Von der Projektklärung zugelassene einfache Variante; konsistent zum Fortschrittstakt, der bei `Duration <= 0` ebenfalls keine Watched-/Auto-Resume-Prüfung vornimmt. Eine Entprellung wäre zusätzlicher Zustand ohne messbaren Mehrwert. |
| Bewertung `IsActive` | Bestehender Guard bleibt unverändert vor der Plausibilitätsprüfung | Ohne aktive Wiedergabe ändert sich nichts. |

## Programmabläufe

### Ende-Ereignis mit Plausibilitätsprüfung

1. `MediaElement` feuert `MediaEnded` → `PlayerPage.OnMediaEnded` dispatcht `PlaybackEndedMessage` (unverändert, `subscribed`-Guard).
2. `AppUpdate.Update` bildet die Nachricht auf `UpdatePlaybackEnded(model)` ab (unverändert).
3. `UpdatePlaybackEnded`: Guard `player.IsActive` (unverändert), dann neue Prüfung `IsPlaybackEndPlausible(player)`:
   - `player.Duration <= TimeSpan.Zero` → nicht plausibel (Dauer unbekannt).
   - `player.Duration - player.Position > PlaybackEndedToleranceSeconds` → nicht plausibel (Restlaufzeit außerhalb des Fensters; Position > Dauer ergibt negative Restlaufzeit → plausibel).
   - Nicht plausibel → `AppTransition.Unchanged(model)` zurückgeben: keine `IsWatched`-Markierung, kein `ReportPlaybackProgressEffect`, kein `DeleteWatchedDownloadEffect`, kein `AdvancePlaylistEntry`/`AdvanceToNextEpisode`.
4. Plausibel → bisheriger Abschluss-Ablauf unverändert (Angesehen-Markierung, End-Fortschrittsmeldung, optionaler Download-Löscheffekt, Auto-Fortsetzung Playlist- bzw. Serienpfad).

Beteiligte Klassen/Komponenten: `PlayerPage`, `PlaybackEndedMessage`, `AppUpdate`, `PlayerState`, `AppTransition`.

## Neue Klassen

Keine.

## Änderungen an bestehenden Klassen

### `AppUpdate` (Logikklasse, `VideoPlayer.Mvu/AppUpdate.cs`)

- **Neue Konstante:** `PlaybackEndedToleranceSeconds` (`private const int`, Wert 300) — festes Toleranzfenster der Ende-Plausibilitätsprüfung in Sekunden; XML-Doc-Kommentar mit Begründung und Orientierung an `AppSettings.WatchedEndWindowSeconds`.
- **Neue Methode:** `IsPlaybackEndPlausible(PlayerState player)` → `bool` (private, statisch) — liefert `true`, wenn `Duration > 0` und `Duration - Position <= TimeSpan.FromSeconds(PlaybackEndedToleranceSeconds)`; XML-Doc-Kommentar.
- **Geänderte Methode:** `UpdatePlaybackEnded` — nach dem `IsActive`-Guard: vorzeitige Rückkehr `AppTransition.Unchanged(model)`, wenn `IsPlaybackEndPlausible(player)` `false` liefert; XML-Doc der Methode um die Prüfung ergänzen (die Methode hat derzeit keinen Doc-Kommentar — gemäß Dateistil einen ergänzen).

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine neuen Eingabevalidierungen — die Plausibilitätsregel selbst ist die Änderung (siehe Programmablauf).

## Konfigurationsänderungen

Keine (Schwelle ist eine Konstante).

## Seiteneffekte und Risiken

- **Medien ohne bekannte Dauer:** Ein Ende-Ereignis mit `Duration <= 0` wird verworfen — diese Medien erhalten keine Angesehen-Markierung/Endmeldung mehr. Von der Projektklärung zugelassen; der Fortschrittstakt behandelt solche Medien bereits ohne Watched-/Auto-Resume-Logik.
- **Verspätete Ende-Ereignisse nach Medienwechsel:** Werden jetzt korrekt verworfen (neuer Zustand trägt `Duration`/`Position` = 0 bzw. Anfangsposition) — behebt den in der Bestandsaufnahme benannten Fehler, dass ein nachgezogenes `MediaEnded` die Folge-Episode als gesehen markiert.
- **Kurze Medien (< 300 s):** Liegen komplett im Fenster — ein Ende-Ereignis ist immer plausibel; konsistent zur bestehenden Watched-Fenster-Logik (solche Medien gelten ab dem ersten Takt als angesehen). E2E-Testvideo (20 s) ist davon betroffen und bleibt grün.
- **`PlayerClosed`-Pfad:** Unverändert — anwendergesteuertes Verlassen meldet weiterhin die tatsächliche Position.

## Umsetzungsreihenfolge

1. **Plausibilitätsprüfung in `AppUpdate` implementieren**
   - Voraussetzungen: Keine (`PlayerState.Position`/`Duration` vorhanden).
   - Beschreibung: Konstante `PlaybackEndedToleranceSeconds`, Methode `IsPlaybackEndPlausible`, Guard in `UpdatePlaybackEnded`, Doc-Kommentare.
2. **Bestehende Tests an die Prüfung anpassen**
   - Voraussetzungen: Schritt 1 (konkretes Prüfverhalten festgelegt).
   - Beschreibung: `PlaybackEnded_WatchedEpisode_EmitsAutoDelete`, `PlaybackEnded_WithoutPlaylist_KeepsSeriesAdvance`, `PlaybackEnded_Movie_DoesNotTouchEpisodeList`, `PlaybackEnded_UnknownEpisode_LeavesDetailsUnchanged` mit plausiblem `Position`/`Duration`-Stand versehen.
3. **Neue Unit-Tests für die Plausibilitätsregel**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: Siehe Tests — Verwerfung mitten in der Wiedergabe, bei Dauer 0, einheitlich im Playlist-Kontext; positive Grenzfälle.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|---------------------|------------|-------------------------------------|
| `PlaybackEnded_MidPlayback_IsIgnored` | `AppUpdateTests_Player` | Position weit vor dem Ende (10 min von 45 min) → Modell unverändert, keine Effekte, `IsWatched` bleibt `false` |
| `PlaybackEnded_UnknownDuration_IsIgnored` | `AppUpdateTests_Player` | `Duration = 0` → verworfen, keine Abschlussmeldung, kein Wechsel trotz Folge-Episode |
| `PlaybackEnded_WithinTolerance_Completes` | `AppUpdateTests_Player` | Position = Dauer − 300 s (im Fenster) → Abschluss läuft (Watched + Report-Effekt) |
| `PlaybackEnded_JustOutsideTolerance_IsIgnored` | `AppUpdateTests_Player` | Position = Dauer − 301 s → verworfen |
| `PlaybackEnded_InPlaylist_MidPlayback_IsIgnored` | `AppUpdateTests_PlaylistPlayback` | Playlist-Modell mit `Position` weit vor `Duration` → kein `AdvancePlaylistEntryEffect`, Modell unverändert |
| `PlaybackEnded_AfterAdvance_IgnoresStaleEvent` | `AppUpdateTests_Player` | Ende-Ereignis auf bereits gewechseltem State (`Position`/`Duration` = 0, Folge-Episode aktiv) → verworfen, Folge-Episode nicht als gesehen markiert |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `AppUpdateTests_Downloads.PlaybackEnded_WatchedEpisode_EmitsAutoDelete` | `Position` 0 bei `Duration` 40 min liegt außerhalb des Fensters → Position auf die Dauer setzen (plausibles Ende) |
| `AppUpdateTests_PlaylistPlayback.PlaybackEnded_WithoutPlaylist_KeepsSeriesAdvance` | `Duration`/`Position` nicht gesetzt (0) → beide auf ein plausibles Ende setzen |
| `AppUpdateTests_Player.PlaybackEnded_Movie_DoesNotTouchEpisodeList` | Position 0 bei Dauer 45 min → Position ans Ende setzen, damit der Test weiterhin den regulären Ende-Pfad prüft |
| `AppUpdateTests_Player.PlaybackEnded_UnknownEpisode_LeavesDetailsUnchanged` | dto. |

### E2E-Tests (primärer Funktionsnachweis)

Keine neuen E2E-Tests — Begründung: Die Anforderung ändert kein anwendergesteuertes Verhalten und keinen Benutzerfluss; sie präzisiert die Reaktion auf ein nicht deterministisch auslösbares Geräteereignis (`MediaEnded` vor dem tatsächlichen Ende). Ein verfrühtes `MediaEnded` lässt sich über die UI nicht reproduzierbar auslösen (Stream-Abbruch/Formatwechsel sind nicht deterministisch steuerbar; der E2E-Mock-Server kann einen Stream-Abriss nicht so dosieren, dass das native Element `MediaEnded` statt `MediaFailed` meldet). Der positive Pfad (echtes Ende → Abschluss + Auto-Fortsetzung) bleibt über den bestehenden E2E-Test `PlaylistPlaybackTests.PlaybackEnd_AdvancesToNextPlaylistEntry` nachgewiesen. Die neue Regel ist vollständig über Unit-Tests der reinen Update-Funktion abgedeckt.

Anzupassende bestehende E2E-Tests: Keine.

## Offene Punkte

Keine.
