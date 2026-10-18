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
- **Playlists:** auf der Playlist-Detailseite startet die Schaltfläche
  **„Abspielen"** den ersten abspielbaren Eintrag; der Tippen auf
  einen abspielbaren Eintrag startet die Wiedergabe ab genau diesem
  Eintrag — jeweils über den serverseitigen Wiedergabestart
  (`POST api/playlists/{id}/play`).
- Serien, Staffeln und Filmkollektionen haben keinen eigenen
  Abspielen-Schalter — dort wird über die enthaltenen Einträge
  gestartet.

## Bedienung in der Vollansicht

Die nativen Transportsteuerungen des `MediaElement`
(`ShouldShowPlaybackControls` — Play/Pause, Positionsregler/Seek und
die übrigen Transportfunktionen) sind in der normalen Vollansicht
direkt bedienbar. Das App-Kopfband `OverlayChrome` (Titel,
„Angesehen"-Hinweis) liegt nur am oberen Rand
(`VerticalOptions="Start"`) und ist eingabedurchlässig — das
`InputTransparent` steht dabei am Elternlayout selbst, nicht nur am
inneren `Border`: Unter iOS hit-testet ein Layout auch bei
eingabedurchlässigem Inhalt in den eigenen Bounds weiter und hätte
sonst die Eingaben der nativen Transportleiste abgefangen. Der
Mini-Player-Schalter liegt als eigenes Element über dem Kopfband,
da `InputTransparent` unter Windows den gesamten Teilbaum vom
Hit-Test ausnimmt — ein eingebetteter Button wäre sonst unbedienbar.

**Windows-Besonderheit:** Das `MediaPlayerElement` blendet die
Steuerung nach dem Timeout zwar zuverlässig aus, aber in dieser
Kombination nicht über eigene Zeiger-Ereignisse wieder ein. Ein Tip
auf die Videofläche ruft daher `TransportControls.Show()`; das
Ausblenden übernimmt weiterhin der Auto-Timer. Auf iOS/Android
regelt der Plattform-Player das Einblenden nativ bei Berührung.

**iOS — native Vollbild-/PiP-Schaltflächen frei erreichbar
(Issue #45):** iOS blendet die native Top-Leiste des
`AVPlayerViewController` mit den Vollbild- und PiP-Schaltflächen im
oberen Randbereich ein — genau dort lagen die App-Überlagerungen,
die auf dem iPad deren Bedienung blockierten. Neben der
Eingabedurchlässigkeit des Kopfband-Elternlayouts (s. o.) verschiebt
die Seite unter iOS (`#if IOS` im Konstruktor) `OverlayChrome` und
`PlaylistNavigationBar` um den benannten Versatz
`IosNativeControlsClearance` (96 pt) unter die native Leiste: Das
eingabedurchlässige Kopfband würde die nativen Schaltflächen sonst
weiterhin visuell überdecken (Scrim-Abdunklung, „Angesehen"-Badge
oben rechts), und die interaktive Playlist-Navigation darf nicht
eingabedurchlässig sein — sie muss physisch darunter liegen und
hält ihren XAML-Abstand zum mitverschobenen Kopfband. Die XAML-Werte
bleiben der Windows-/Standardpfad; das Windows-Verhalten ist
unverändert.

**iOS-Verifikationsgrenze:** Der Fehler war iOS-/iPad-spezifisch;
eine iOS-UI-Automatisierung existiert im Repository nicht, und die
Windows-E2E-Suite erreicht die native `AVPlayerViewController`-
Ebene grundsätzlich nicht. Der Nachweis stützt sich auf die
quelltextbasierten Contract-Tests (`PlayerContractTests` pinnen
`InputTransparent` am Elternlayout, den `#if IOS`-Versatzblock und
die Lage der Playlist-Navigation außerhalb des eingabedurchlässigen
Bands) sowie den dokumentierten manuellen TestFlight-Prüfschritt
auf dem iPad (Vollbild antippen, PiP starten, Playlist-Navigation
bedienen). Der Versatz von 96 pt ist eine konservative Schätzung
der Leistenhöhe inklusive Safe-Area — zeigt die Geräteprüfung eine
Restüberdeckung, ist er feinzujustieren; als letzter
Eskalationspfad wäre ein eigener iOS-Mapper am
`AVPlayerViewController` des Toolkit-`MediaElement` denkbar.

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
  gespeicherten Kopie (`cached_lists`). Der Sprung wird angewendet,
  sobald der Player wiedergabebereit ist; erreicht ein Medium die
  Bereitschaft nie, beginnt die Wiedergabe ohne Sprung am Anfang
  (Degradation statt Absturz, siehe „Stabilität" unten). Schlägt der
  Sprung selbst fehl oder läuft sein Zeitlimit ab, läuft die
  Wiedergabe ebenfalls weiter — an der vom Player erreichten
  Position, ohne Fehlerhinweis und ohne Episodenwechsel (siehe
  „Fehlerfall").
- **Fortschrittsmeldung:** alle 5 Sekunden wird die Position an den
  Server gemeldet, solange die Wiedergabe läuft; unveränderte
  Positionen (Pause) werden nicht erneut gesendet. Beim Ende und beim
  Verlassen der Seite wird der Stand abschließend gemeldet.
- **Playlist-Bezug:** läuft die Wiedergabe im Playlist-Kontext, trägt
  jede Fortschrittsmeldung — Takt, Ende und Schluss — die `PlaylistId`
  des aktiven Kontexts mit (`PlayerState.ProgressReportPlaylistId`,
  `IsPlaylistPlayback ? PlaylistId : null`). Der Server schreibt damit
  den playlist-gebundenen Weiterschauen-Eintrag fort, statt daneben
  einen zweiten, kontextlosen Eintrag anzulegen; kontextlose
  Wiedergabe meldet `null` und verhält sich wie bisher.
- **Offline:** nicht zustellbare Meldungen landen verlustfrei in der
  persistenten Warteschlange `pending_progress` und werden bei der
  nächsten Online-Rückkehr automatisch nachgesendet — sie überstehen
  App-Neustarts und Backup-Wiederherstellungen. Der Playlist-Bezug
  wird dabei in der Warteschlange mitgespeichert (Spalte `PlaylistId`)
  und beim Nachsenden wieder übergeben, sodass auch nachträglich
  zugestellte Meldungen den playlist-gebundenen Eintrag erreichen.
- **„Angesehen":** erreicht die Position das Ende abzüglich des
  eingestellten Zeitfensters („Als angesehen"-Einstellung), wird das
  Medium als angesehen markiert — die Anzeige erscheint dezent im
  Kopfbereich. Der Fenstereintritt ist dabei bestätigt: erst zwei
  aufeinanderfolgende Fortschrittstakte mit real weitergelaufener
  Position und nicht geschrumpfter Dauer gelten als erreichtes Ende —
  ein einzelner Ausreißertakt eines gestörten Streams markiert nicht
  (siehe „Automatische Fortsetzung").

## Automatische Fortsetzung

Bei Serien startet — sofern die Auto-Fortsetzung in den Einstellungen
aktiviert ist — automatisch die nächste Episode, sobald die
Restlaufzeit das eingestellte Zeitfenster unterschreitet (auch
staffelübergreifend, in Episodenreihenfolge). Pausiert der Anwender,
wird nicht weitergeschaltet — die Fortsetzung greift nur bei
laufender Wiedergabe. Ausgeschaltet bleibt das Verhalten wie bisher.

**Playlist-Kontext:** läuft die Wiedergabe aus einer Playlist, holt
die Fortsetzung den Folge-Eintrag über den serverseitigen
`advance`-Endpunkt in der Playlist-Reihenfolge (nicht die
Serienreihenfolge); meldet der Server keinen Folgeeintrag, endet die
Wiedergabe.

**Bestätigter Fenstereintritt:** der Eintritt ins Endfenster gilt
erst ab dem zweiten aufeinanderfolgenden Fortschrittstakt
(5-Sekunden-Raster) als erreicht — der Takt muss eine tatsächlich
weitergelaufene Position und eine nicht geschrumpfte Gesamtdauer
melden. Einzelne Takte mit eingefrorener oder gesprungener Position
bzw. verkürzter Dauer — etwa bei einem gestörten Stream, der im
Wiedergabezustand verharrt — bestätigen das Ende nicht: sie lösen
weder die Fortsetzung noch die „angesehen"-Markierung samt
eventueller Download-Löschung aus, und die Wiedergabe läuft weiter.
Jeder nicht plausible Takt — auch ein Pausentakt, ein Seek zurück
oder ein Takt außerhalb des Fensters — lässt die Bestätigung von
vorn beginnen; während der Quellauflösung eines Folgetitels ist die
Fensterwertung gesperrt. Zusätzlich ist jeder Takt an das aktuelle
Medium gebunden: nachlaufende Positionsmeldungen eines bereits
gewechselten Vorgängermediums werden verworfen und können das neue
Medium weder als gesehen markieren noch dessen Download löschen
oder einen weiteren Wechsel auslösen.

Damit verschiebt sich der Wechsel um höchstens ein Taktintervall
(~5 Sekunden) nach hinten — bei den üblichen Zeitfenstern von
mehreren Minuten fachlich unerheblich. Medien, die kürzer als zwei
Taktintervalle Restlaufzeit im Fenster laufen, sowie ein vor dem
Ende pausiertes oder geschlossenes Medium werden taktseitig nicht
mehr markiert bzw. weitergeschaltet — in diesen Fällen greift das
unveränderte, ebenfalls plausibilisierte Ende-Ereignis (siehe
„Wiedergabeende").

Technischer Hintergrund: `PlayerState.EndWindowStreak` zählt die
aufeinanderfolgenden plausiblen Fenstertakte; `IsProgressTickPlausible`
und `EvaluateEndWindow` in `AppUpdate` koppeln Markierung,
Download-Löschung und beide Fortsetzungspfade an die interne
Konstante `EndWindowRequiredTicks` (= 2; Streak-Fenster über
`AppSettings.EndWindow` = größeres der beiden eingestellten
Fenster). Die Medienbindung läuft über die `MediaId` der
`PlaybackProgressMessage` — die `PlayerPage` meldet die zuletzt am
`MediaElement` tatsächlich angewendete `MediaId` (`appliedMediaId`,
erst nach erfolgreichem Quellenwechsel gesetzt); ein Takt mit
fremder Id wird vollständig verworfen und erreicht auch die
Fortschrittsmeldung an den Server nicht. Zur Diagnose schreibt
`ReportPosition` innerhalb des Fensters einen Debug-Protokolleintrag
mit Position, Dauer, Lauf-/Pufferzustand, `MediaId` und Streak.

## Wiedergabeende

Erreicht die Wiedergabe das Ende des Mediums, wird der Titel als
„angesehen" markiert, die Endposition abschließend an den Server
gemeldet und — bei aktivierter Auto-Fortsetzung — der Folgetitel
gestartet. Das Ende-Ereignis des Players wird dabei gegen den
gemeldeten Wiedergabestand plausibilisiert: nur wenn die Position
innerhalb eines festen Toleranzfensters (300 Sekunden, Orientierung
am „Als angesehen"-Endfenster) vor bzw. an der Gesamtdauer liegt,
gilt das Ereignis als tatsächliches Ende. Ein verfrüht feuerndes
Ereignis — etwa bei einem Stream-Abbruch, einem Formatwechsel oder
einem Positionswechsel mitten in der Wiedergabe — sowie ein
nachgezogenes Ereignis eines bereits gewechselten Mediums werden
verworfen und lösen weder die Markierung noch die Fortschritts-
meldung oder die Fortsetzung aus. Ist die Gesamtdauer unbekannt,
wird das Ereignis ebenfalls nicht als Ende gewertet. Die Prüfung
gilt einheitlich für die Serien-/Einzelwiedergabe und die
Playlist-Wiedergabe.

Technischer Hintergrund: die Auswertung liegt in
`AppUpdate.UpdatePlaybackEnded` (`IsPlaybackEndPlausible` prüft
`PlayerState.Duration`/`Position` gegen die Konstante
`PlaybackEndedToleranceSeconds`); `PlayerPage.OnMediaEnded` bleibt
reiner Event-Übersetzer.

## Playlist-Navigation

Bei Playlist-Wiedergabe zeigt der Player die Schaltflächen **„Zurück"**
und **„Weiter"** (serverseitige `previous`-/`next`-Endpunkte) sowie
unter dem Titel eine Kontextzeile „„Playlist-Name" · Eintrag n/m".
Die Schaltflächen erscheinen nur im Playlist-Kontext und werden am
Listenrand deaktiviert; im Mini-Player sind sie wie das Kopfband
ausgeblendet. Unter iOS liegt die Zeile etwas tiefer — sie wird mit
dem Kopfband unter die eingeblendete native Top-Leiste gerückt,
bleibt aber voll bedienbar (siehe „Bedienung in der Vollansicht").

## Fehlerfall

Ist eine Quelle nicht verfügbar oder bricht die Wiedergabe ab,
erscheint eine verständliche Meldung; bei Serien fällt der Player
automatisch auf die nächste Episode zurück, bei Playlist-Wiedergabe
über den `advance`-Endpunkt auf den nächsten Playlist-Eintrag. Ohne
Folge-Eintrag bleibt der Fehlerhinweis stehen und die Seite lässt sich
normal verlassen.
Fehler beim Wiedergabestart — etwa eine ungültige oder nicht setzbare
Quell-Adresse — landen ebenfalls in diesem Fehlerhinweis; die
Anwendung wird dadurch nicht beendet.

**Nicht-fataler Fortsetzungs-Sprung:** schlägt dagegen der Sprung auf
die Fortsetzungsposition fehl oder überschreitet er sein Zeitlimit
(10 Sekunden), wird die laufende Wiedergabe nicht unterbrochen — sie
läuft an der vom Player erreichten Position weiter, ohne
Fehlerhinweis und ohne Wechsel zur nächsten Episode bzw. zum nächsten
Playlist-Eintrag. Hintergrund: die Vollendung des Sprungs kann bei
Server-Streams ausbleiben, während der native Sprung bereits wirksam
war — der Fehlschlag belegt daher keinen Abspielfehler. Sichtbar ist
er ausschließlich als Warnung im Programmprotokoll; ein erneuter
Versuch findet nicht statt. Der Episoden-/Playlist-Rückfall bleibt
echten Abspielfehlern vorbehalten: nicht verfügbare oder nicht
setzbare Quelle sowie das `MediaFailed`-Ereignis des Players.

## Picture-in-Picture / Mini-Player

- **Windows:** der Schalter **„Mini-Player"** verkleinert das
  App-Fenster zu einem kompakten, immer vordergrundigen Overlay, das
  nur das Videobild zeigt; erneutes Umschalten stellt das normale
  Fenster wieder her. Beim Verlassen der Seite wird der Normalmodus
  automatisch wiederhergestellt.
- **iOS:** die PiP-Schaltfläche ist Teil der nativen
  Transportsteuerung des Players (`AVPlayerViewController`); der
  dafür nötige Hintergrund-Audio-Modus ist in der `Info.plist`
  deklariert — ein eigener Schalter ist nicht nötig und erscheint
  unter iOS nicht. Die Vollbild- und PiP-Schaltflächen der
  eingeblendeten nativen Leiste sind seit der Overlay-Korrektur
  auf dem iPad wieder frei erreichbar (siehe „Bedienung in der
  Vollansicht").

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

**iOS — Absturz beim Fortsetzen behoben:** Auf iOS feuert das
`MediaOpened`-Ereignis des Toolkit-`MediaElement` (10.0.0) noch vor
`AVPlayerItemStatus.ReadyToPlay` (Toolkit-Issue #3249; der Fix des
Toolkits ist gemergt, aber in keinem NuGet-Release enthalten). Ein
`SeekTo` in diesem Fenster warf im `async void`-Handler des Toolkits
eine unbehandelte Ausnahme — `EXC_CRASH (SIGABRT)`, nur im
Resume-Fall reproduzierbar. Die Seite merkt die Fortsetzungsposition
daher bei `MediaOpened` nur noch (`pendingResumePosition`) und wendet
den Sprung über `TryApplyPendingResumeSeek` an, sobald der Player
`Playing` oder `Paused` meldet — beide Zustände implizieren
`ReadyToPlay`. `ApplyResumeSeekAsync` läuft mit einer
Abbruchschwelle von 10 Sekunden (`ResumeSeekTimeout`), weil die
Toolkit-Vollendung des Seeks im Fehlerfall ausbleiben kann. Timeout
oder Fehlschlag des Sprungs degradieren still — sie lösen weder
einen Fehlerhinweis noch den Episoden-Rückfall aus (siehe
„Fehlerfall").

Zusätzlich sind alle nativen `VideoElement`-Zugriffe fehlerisoliert
(Quellenwechsel und Leeren der Quelle in `UpdateUi`, der
Fortsetzungs-Seek, `ReportPosition` und die Zustandsabfrage im
Fortschrittstakt) und `IsValidSourceLocation` prüft die Quell-Adresse
vor der Übergabe ans Element (Server-Stream: absolute
http/https-URL; lokale Datei: nicht leerer Pfad). Fehler beim Setzen
oder Zurücksetzen der Quelle laufen über `FailPlaybackStart` —
ebenso das `MediaFailed`-Ereignis über `OnMediaFailed` — als
`PlaybackFailedMessage` in den MVU-Zyklus, sichtbar im Fehlerpanel
bzw. mit Episoden-Rückfall, statt den Prozess zu beenden; das Flag
`sourceApplyFailed` erzwingt dabei, dass die nächste Modelländerung
den Quellenwechsel nach einem Fehlschlag erneut durchläuft (auch bei
unveränderter oder leerender Adresse). Die übrigen isolierten Pfade
protokollieren ihren Fehlschlag nur als Warnung: der Fortsetzungs-
Seek degradert still (kein `PlaybackFailedMessage`, kein
Episoden-Rückfall), und Positionsmeldung bzw. Fortschrittstakt
versuchen es im nächsten Takt erneut.

## MVU-Anbindung

Der Wiedergabe-Zustand liegt in `AppModel.Player` (`PlayerState`):
Medium, aufgelöste Quelle (`PlaybackSource` mit
`PlaybackSourceKind.ServerStream`/`LocalFile`), Position/Dauer,
Lauf-/Pufferzustand, „angesehen"-Markierung, Bestätigungs-Streak
des Endfensters (`EndWindowStreak`), PiP-Status,
Fortsetzungsliste (`UpcomingEpisodes` aus dem Serienkontext der
Detailseite), Playlist-Kontext (`PlaylistId`, `PlaylistName`,
`PlaylistEntryId`, `PlaylistPosition`, `PlaylistTotalCount`,
`PlaylistNextExhausted`) und Fehlertext. Die Seite dispatcht
ausschließlich Messages (`PlayRequested`, `PlaylistNextRequested`,
`PlaylistPreviousRequested`, `PlaybackProgress` — trägt die `MediaId`
des aktuell am Element angewendeten Mediums, sodass nachlaufende
Takte des Vorgängermediums verworfen werden —, `PlaybackEnded`,
`PlaybackFailed`, `PictureInPictureRequested`, `PlayerClosed` u. a.);
die Effekte `NavigateToPlayer`, `ResolvePlaybackSource`,
`StartPlaylistPlayback`, `LoadNextPlaylistEntry`,
`LoadPreviousPlaylistEntry`, `AdvancePlaylistEntry`,
`ReportPlaybackProgress` und `TogglePictureInPicture` laufen im
`ShellAppEffectHandler` am App-Rand — Fortschritte gehen über den
bestehenden `ProgressReportService`, es gibt kein zweites
Warteschlangensystem.
