# Übersetzte Anforderung: Videoplayer — wiederholte Wiedergabe ohne Absturz

Quelle: Entwicklungsschritt 4 des Projekts „Design-Angleichung an Webanwendung"
(steps.md) bzw. Kundenfeedback Issue #9 — nach der ersten Wiedergabe lässt
sich kein weiteres Video abspielen; die App stürzt mit unbehandelter
`COMException` ab (HResult `0x8001010E`, `RPC_E_WRONG_THREAD`).

## Fachliche Zusammenfassung

Die Wiedergabe (`PlayerPage`, Route `player`) muss beliebig oft
hintereinander gestartet werden können, ohne dass die Anwendung mit einer
unbehandelten `COMException` abbricht. Die Beobachtung: Der Absturz erfolgt
beim Zugriff auf das native Player-Element aus einem Threadpool-Thread —
der Stack zeigt `MediaPlayerElement.get_MediaPlayer` innerhalb von
`MediaManager.PlatformSeek` (CommunityToolkit.Maui MediaElement).
Auslösend ist der Positionsrücksprung (`SeekTo` auf die
Fortsetzungsposition in `OnMediaOpened`), der vom Toolkit-Event
`MediaOpened` aus auf einem Nicht-UI-Thread laufen kann. Verstärkend wirkt
ein Lebenszyklusfehler: Die `PlayerPage` wird bei jedem
Wiedergabe-Start neu erzeugt, meldet sich aber nie von
`MvuHost.ModelChanged` ab — verwaiste Seiten reagieren weiter auf
Modelländerungen und greifen dabei auf ihr losgelöstes
`MediaElement` zu.

Gefordert ist die Ursachenbehebung auf zwei Ebenen:

1. Jede Berührung des nativen Player-Elements (Positionssprünge,
   Wiedergabebefehle, Quellenwechsel, Statusabfragen) erfolgt
   ausschließlich auf dem UI-Thread.
2. Bereits verlassene Wiedergabe-Seiten reagieren nicht mehr auf
   Modelländerungen (Unsubscribe beim Verlassen).

Liegt die Ausnahme im `MediaElement`-Baustein des Toolkits selbst, ist ein
app-seitiger Workaround zulässig, sofern das beobachtbare Verhalten stimmt.

## Betroffene Klassen und Komponenten

- `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs` — Wiedergabeseite:
  `ModelChanged`-Abonnement, `UpdateUi`, `OnMediaOpened` (`SeekTo`),
  `OnMediaStateChanged`/`OnMediaEnded`/`OnMediaFailed` (Event-Handler des
  `MediaElement`), `OnDisappearing`.
- `VideoPlayer.Maui/Pages/PlayerPage.xaml` — `MediaElement`-Verdrahtung
  (vermutlich unverändert, nur falls nötig).
- `VideoPlayer.Mvu/MvuHost` — Ereignisquelle `ModelChanged` (keine
  Änderung erwartet; Referenz).
- `VideoWebPlayer.Maui.Tests/PlayerContractTests.cs` — Contract-Tests
  (Quelltext-basiert, da die MAUI-Schicht im Testprojekt nicht
  instanziierbar ist — etablierte Konvention).

## Implementierungsansatz

- `ModelChanged`-Abonnement nach dem etablierten Muster der korrekt
  abmeldenden Seiten (`SettingsPage`, `LoginPage`): Abo in
  `OnAppearing` (mit `-=` vor `+=` gegen Doppelabos), Abmeldung in
  `OnDisappearing`.
- Alle Zugriffe auf `VideoElement` (`SeekTo`, `Stop`, `Source`,
  `CurrentState`, `Position`, `Duration`) über den UI-Thread
  (`MainThread.BeginInvokeOnMainThread` / Dispatcher), weil die
  Toolkit-Events `MediaOpened`/`StateChanged` unter Windows auf
  Threadpool-Threads ausgelöst werden können.
- Keine Änderungen am MVU-Zyklus (`AppUpdate`, `AppMessage`,
  `AppEffect`) — Fortschrittsmeldung (`ReportPlaybackProgressEffect`)
  und Gesehen-Markierung (`IsWatched`, Watched-Schwelle) bleiben
  unverändert.
- Tests: Contract-Tests in `PlayerContractTests`, die das
  Unsubscribe-Muster und den UI-Thread-Marshal in
  `PlayerPage.xaml.cs` pinnen (Muster der bestehenden
  Contract-Tests wie `PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface`).

## Konfiguration

Keine — das Verhalten ist nicht konfigurierbar gefordert.

## Akzeptanzkriterien

- Mindestens zwei Wiedergaben hintereinander ohne Absturz, beliebig
  wiederholbar; keine unbehandelte Ausnahme.
- Die `COMException` `0x8001010E` (`RPC_E_WRONG_THREAD`) tritt nicht mehr
  auf — Nachweis per Test oder dokumentiertem Workflow (Geräteprüfung ist
  im Repository nicht automatisierbar).
- Fortschrittsmeldung an den Server (`ReportPlaybackProgressEffect` →
  `ProgressReportService`) und Gesehen-Markieren (`IsWatched`,
  `WatchedEndWindowSeconds`) funktionieren unverändert.
- Alle Tests grün; `dotnet build VideoPlayer.App.sln` in Debug und
  Release fehlerfrei.

## Offene Fragen

Keine — die Anforderung nennt Ursache (Thread-Affinität des nativen
Elements + fehlendes Unsubscribe), Korrekturrichtung und Akzeptanz
ausreichend konkret.
