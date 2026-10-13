# Übersetzte Anforderung: Plausibilitätsprüfung des Wiedergabe-Ende-Ereignisses

Hinweis: `docs/features.md` existiert nicht — der optionale Projektkontext-Schritt wurde übersprungen.

## Fachliche Zusammenfassung

Das `MediaEnded`-Ereignis des `MediaElement` löst derzeit ungeprüft den gesamten Abschluss-Ablauf aus: `PlayerPage.OnMediaEnded` dispatcht `PlaybackEndedMessage`, und `AppUpdate.UpdatePlaybackEnded` markiert das Medium als „angesehen", meldet die Endposition als Fortschritt an den Server und startet bei aktivierter Auto-Fortsetzung den Folgetitel (`AdvanceToNextEpisode` im Serien-/Einzelkontext, `AdvancePlaylistEntry` im Playlist-Kontext). Feuert das Ereignis verfrüht — z. B. bei Stream-Abbruch, Formatwechsel oder Positionswechsel — wird mitten in der laufenden Wiedergabe fälschlich abgeschlossen.

Soll-Verhalten: Ein Ende-Ereignis wird erst dann als tatsächliches Wiedergabeende gewertet, wenn es gegen den Wiedergabestand plausibilisiert ist. Nicht plausible Ereignisse lösen weder Auto-Vorlauf/Folgevideo-Start noch Angesehen-Markierung und End-Fortschrittsmeldung aus. Beim tatsächlichen Ende läuft der bestehende Abschluss-Ablauf unverändert weiter. Die Prüfung gilt einheitlich für Serien-/Einzelkontext und Playlist-Kontext.

## Verbindliche Projektklärung (Prüfkriterium)

- Plausibilitätscheck: **Position gegen Gesamtdauer** mit festem Toleranzfenster am Ende (Orientierung am bestehenden Angesehen-Endfenster `AppSettings.WatchedEndWindowSeconds`, Standard 300 s).
- Bei unbekannter/0-Gesamtdauer: das Ereignis **nicht** als Ende werten (zulässige Alternative laut Klärung; die andere Option „kurze Entprellung" entfällt).
- Einheitlich für Serien-/Einzel- und Playlist-Kontext.
- Schwelle als **Konstante**, keine neue Einstellung (kein konfigurierbares Verhalten).

## Betroffene Klassen und Komponenten

- `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs` — `OnMediaEnded` (Auslöser, dispatcht `PlaybackEndedMessage` ungeprüft)
- `VideoPlayer.Mvu/AppMessage.cs` — `PlaybackEndedMessage`
- `VideoPlayer.Mvu/AppUpdate.cs` — `UpdatePlaybackEnded` (Abschluss-Ablauf: `IsWatched`, `ReportPlaybackProgressEffect`, `DeleteWatchedDownloadEffect`, `AdvanceToNextEpisode`, `AdvancePlaylistEntry`)
- `VideoPlayer.Mvu/PlayerState.cs` — `Position`, `Duration`, `IsActive` (Datenbasis der Prüfung)
- Tests: `VideoWebPlayer.Maui.Tests` (`AppUpdateTests_Player`, `AppUpdateTests_PlaylistPlayback`, `AppUpdateTests_Downloads`, ggf. `PlayerContractTests`)

## Implementierungsansatz

- Die Plausibilitätsprüfung wird zentral in `AppUpdate.UpdatePlaybackEnded` vorgenommen, bevor irgendein Abschluss-Effekt ausgelöst wird — damit greift sie einheitlich für beide Kontexte und ist über die reinen MVU-Übergänge vollständig unit-testbar.
- Plausibel ist das Ereignis, wenn `PlayerState.Duration > 0` ist und die Restlaufzeit (`Duration - Position`) innerhalb eines festen Toleranzfensters liegt; bei `Duration <= 0` wird das Ereignis verworfen.
- Die Seite (`PlayerPage.OnMediaEnded`) bleibt unverändert — sie bleibt reiner Event-Übersetzer; die inhaltliche Bewertung liegt im MVU-Zyklus.

## Konfiguration

Keine — die Schwelle ist eine feste Konstante im Code (kein neuer Eintrag in `AppSettings`).

## Offene Fragen

Keine — das Prüfkriterium ist durch die Projektklärung verbindlich vorgegeben; der Nutzer ist nicht erreichbar.
