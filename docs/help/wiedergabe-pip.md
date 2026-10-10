# Videowiedergabe, Picture-in-Picture und Fortschrittsverfolgung (Entwicklungsschritt 10)

Die neue VideoPlayer-App kann jetzt Medien abspielen: eine eigene
Wiedergabeseite (`PlayerPage`, Shell-Route `player`) nutzt das
`MediaElement` aus `CommunityToolkit.Maui.MediaElement` 10.0.0 und
läuft vollständig über den MVU-Kreislauf.

## Wiedergabe starten

- **Detailseite:** bei Filmen und Episoden erscheint im Kopfbereich
  die Schaltfläche **„Abspielen"**; bei einer navigierten Episode
  wird der Episodentitel (statt des Seriennamens) übernommen.
- **Episodenliste:** jede Episode trägt rechts eine ▶-Schaltfläche —
  direkter Start ohne Umweg über die Episoden-Detailseite.
- Serien, Staffeln und Filmkollektionen haben keinen eigenen
  Abspielen-Schalter — dort wird über die enthaltenen Einträge
  gestartet.

## Bedienung in der Vollansicht

Die nativen Transportsteuerungen des `MediaElement`
(`ShouldShowPlaybackControls` — Play/Pause, Positionsregler/Seek und
die übrigen Transportfunktionen) sind in der normalen Vollansicht
direkt bedienbar. Das App-Kopfband `OverlayChrome` (Titel,
„Angesehen"-Hinweis) liegt nur am oberen Rand
(`VerticalOptions="Start"`) und ist eingabedurchlässig
(`InputTransparent`); der Mini-Player-Schalter liegt als eigenes
Element über dem Kopfband, da `InputTransparent` unter Windows den
gesamten Teilbaum vom Hit-Test ausnimmt — ein eingebetteter Button
wäre sonst unbedienbar.

**Windows-Besonderheit:** Das `MediaPlayerElement` blendet die
Steuerung nach dem Timeout zwar zuverlässig aus, aber in dieser
Kombination nicht über eigene Zeiger-Ereignisse wieder ein. Ein Tip
auf die Videofläche ruft daher `TransportControls.Show()`; das
Ausblenden übernimmt weiterhin der Auto-Timer. Auf iOS/Android
regelt der Plattform-Player das Einblenden nativ bei Berührung.

## Quellen

Die Quellauflösung (`PlaybackSourceService`) wählt automatisch:

1. **Lokale Datei** — ein abgeschlossener Download aus dem
   Download-Manifest (`download_manifest`, Status „fertig",
   Datei vorhanden) hat Vorrang; das ist die Grundlage der
   Offline-Wiedergabe, die Schritt 11 um die Downloads erweitert.
2. **Server-Stream** — sonst wird der Stream-Endpunkt
   `api/items/{typ}/{id}/stream` mit dem Anmelde-Token als
   `access_token`-Parameter angesprochen (Konvention des Altbestands).

## Fortsetzung und Fortschritt

- **Fortsetzen:** beim Öffnen springt die Wiedergabe auf den gemerkten
  Stand der Weiterschauen-Liste — online vom Server, sonst aus der
  gespeicherten Kopie (`cached_lists`).
- **Fortschrittsmeldung:** alle 5 Sekunden wird die Position an den
  Server gemeldet, solange die Wiedergabe läuft; unveränderte
  Positionen (Pause) werden nicht erneut gesendet. Beim Ende und beim
  Verlassen der Seite wird der Stand abschließend gemeldet.
- **Offline:** nicht zustellbare Meldungen landen verlustfrei in der
  persistenten Warteschlange `pending_progress` und werden bei der
  nächsten Online-Rückkehr automatisch nachgesendet — sie überstehen
  App-Neustarts und Backup-Wiederherstellungen.
- **„Angesehen":** erreicht die Position das Ende abzüglich des
  eingestellten Zeitfensters („Als angesehen"-Einstellung), wird das
  Medium als angesehen markiert — die Anzeige erscheint dezent im
  Kopfbereich.

## Automatische Fortsetzung

Bei Serien startet — sofern die Auto-Fortsetzung in den Einstellungen
aktiviert ist — automatisch die nächste Episode, sobald die
Restlaufzeit das eingestellte Zeitfenster unterschreitet (auch
staffelübergreifend, in Episodenreihenfolge). Pausiert der Anwender,
wird nicht weitergeschaltet — die Fortsetzung greift nur bei
laufender Wiedergabe. Ausgeschaltet bleibt das Verhalten wie bisher.

## Fehlerfall

Ist eine Quelle nicht verfügbar oder bricht die Wiedergabe ab,
erscheint eine verständliche Meldung; bei Serien fällt der Player
automatisch auf die nächste Episode zurück. Ohne Folge-Episode bleibt
der Fehlerhinweis stehen und die Seite lässt sich normal verlassen.

## Picture-in-Picture / Mini-Player

- **Windows:** der Schalter **„Mini-Player"** verkleinert das
  App-Fenster zu einem kompakten, immer vordergrundigen Overlay, das
  nur das Videobild zeigt; erneutes Umschalten stellt das normale
  Fenster wieder her. Beim Verlassen der Seite wird der Normalmodus
  automatisch wiederhergestellt.
- **iOS:** die PiP-Schaltfläche ist Teil der nativen
  Transportsteuerung des Players (`AVPlayerViewController`); der
  dafür nötige Hintergrund-Audio-Modus ist in der `Info.plist`
  deklariert — kein eigener Schalter nötig.

## Stabilität bei wiederholter Wiedergabe

Die Wiedergabeseite lässt sich beliebig oft hintereinander öffnen und
schließen, ohne dass die Anwendung abbricht. Fachlich heißt das: Nach
zurückgelegter Wiedergabe startet das nächste Medium sofort wieder —
Fortsetzungsposition, Fortschrittsmeldung und „angesehen"-Markierung
verhalten sich dabei wie bei der ersten Wiedergabe.

Technischer Hintergrund (für Betreuer): Die `PlayerPage` ist transient —
jede Wiedergabe erzeugt eine neue Instanz. Die Seite abonniert
`MvuHost.ModelChanged` nur, solange sie sichtbar ist (Abo in
`OnAppearing`, Abmeldung in `OnDisappearing`), damit verwaiste Seiten
nicht mehr auf ihr losgelöstes `MediaElement` zugreifen. Dieses
Lebenszyklus-Muster gilt inzwischen einheitlich für alle Seiten der
App, die das Modell-Ereignis abonnieren (siehe
`app-grundgeruest.md`, „Seiten-Lebenszyklus"). Alle Zugriffe
auf das native Player-Element aus den Toolkit-Ereignissen
(`MediaOpened`, `StateChanged` u. a.) werden zusätzlich auf den
UI-Thread gemarshalt — das `MediaElement` des CommunityToolkit kann
diese Ereignisse unter Windows auf Threadpool-Threads auslösen; ein
thread-fremder Zugriff (z. B. `SeekTo` → `MediaManager.PlatformSeek` →
`MediaPlayerElement.get_MediaPlayer`) führte dort zur unbehandelten
`COMException 0x8001010E` (`RPC_E_WRONG_THREAD`).

## MVU-Anbindung

Der Wiedergabe-Zustand liegt in `AppModel.Player` (`PlayerState`):
Medium, aufgelöste Quelle (`PlaybackSource` mit
`PlaybackSourceKind.ServerStream`/`LocalFile`), Position/Dauer,
Lauf-/Pufferzustand, „angesehen"-Markierung, PiP-Status,
Fortsetzungsliste (`UpcomingEpisodes` aus dem Serienkontext der
Detailseite) und Fehlertext. Die Seite dispatcht ausschließlich
Messages (`PlayRequested`, `PlaybackProgress`, `PlaybackEnded`,
`PlaybackFailed`, `PictureInPictureRequested`, `PlayerClosed` u. a.);
die Effekte `NavigateToPlayer`, `ResolvePlaybackSource`,
`ReportPlaybackProgress` und `TogglePictureInPicture` laufen im
`ShellAppEffectHandler` am App-Rand — Fortschritte gehen über den
bestehenden `ProgressReportService`, es gibt kein zweites
Warteschlangensystem.
