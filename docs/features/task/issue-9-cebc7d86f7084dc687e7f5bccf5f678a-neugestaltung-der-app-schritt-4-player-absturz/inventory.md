# Bestandsaufnahme: Videoplayer — wiederholte Wiedergabe ohne Absturz

Analysiert wurde die Wiedergabeseite `PlayerPage` (VideoPlayer.Maui) samt
MVU-Wiedergabefluss (VideoPlayer.Mvu) und Effektverdrahtung
(`ShellAppEffectHandler`), bezogen auf die übersetzte Anforderung in
`requirement.md`.

## Zusammenfassung

- `PlayerPage` abonniert `mvuHost.ModelChanged` im Konstruktor
  (`PlayerPage.xaml.cs:38`) und meldet sich **nie** ab — jede verlassene
  Wiedergabe-Seite bleibt am `MvuHost`-Singleton hängen und rendert auf
  ihrem losgelösten `MediaElement` weiter (`UpdateUi`: `Stop()`,
  `Source = null`, Quellwechsel).
- `SeekTo` auf die Fortsetzungsposition läuft in `OnMediaOpened`
  (`PlayerPage.xaml.cs:153`) auf dem Thread des Toolkit-Events — auf
  Windows kann das ein Threadpool-Thread sein; `MediaManager.PlatformSeek`
  greift dann auf `MediaPlayerElement.MediaPlayer` zu →
  `COMException 0x8001010E` (Kunden-Crash).
- Gleiches gilt für `OnMediaStateChanged` → `ReportPosition` (liest
  `CurrentState`/`Position`/`Duration` des nativen Elements).
- Vergleichsmuster vorhanden: `SettingsPage`/`LoginPage`/
  `ServerSetupPage` abonnieren in `OnAppearing` und melden in
  `OnDisappearing` ab.
- MVU-Kern (`AppUpdate`, `MvuHost`, Messages/Effekte) ist korrekt und
  muss nicht angefasst werden — Fortschrittsmeldung und Gesehen-
  Markierung sind dort implementiert.

Test-Ausgangszustand: vollständig grün — 669/669 Tests bestanden,
Debug-Build der Gesamtlösung 0 Fehler (Nachweis:
[inventory/tests.md](inventory/tests.md)).

## Details

- [Wiedergabeseite — Lebenszyklus und Threading](inventory/player-page.md)
- [MVU-Wiedergabefluss](inventory/mvu-player-flow.md)
- [Tests](inventory/tests.md)
