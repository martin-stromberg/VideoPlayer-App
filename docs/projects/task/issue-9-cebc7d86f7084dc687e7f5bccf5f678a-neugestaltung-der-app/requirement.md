# Anforderung: Design-Angleichung der App an die Webanwendung (Kundenfeedback Issue #9)

Quelle: `docs/features/task/issue-9-customer-feedback.md` (wörtliche Kundenrückmeldung).
Zusatzkontext: `web-design-recherche.md` im selben Ordner — aus dem deployten Stand der
Webanwendung extrahierte Design-Token, Hero-Markup, Detailseiten-Header und Angesehen-Symbol.

Zielplattform ist die .NET-10-MAUI-App (Windows + iOS) mit eigener MVU-Architektur
(`VideoPlayer.Mvu`); die App spricht ausschließlich die öffentliche Server-API. Die
Blazor-Webanwendung des Servers ist die gestalterische Referenz, wird selbst aber nicht
verändert.

## Ziel

Die App soll in Aussehen und Bedienung der Webanwendung entsprechen. Kundenzitat (Fazit):
„Eigentlich hätte ein einziger Satz gereicht: Pass das Design an die Webanwendung an."
Zusätzlich zu den Designpunkten enthält die Rückmeldung drei Fehlerberichte (Flickern der
Download-Seite, nicht klickbare Player-Steuerelemente, Absturz bei der Zweitwiedergabe),
die ebenfalls als Anforderungen gelten.

## Fachliche Anforderungen

### FA-1 Allgemein: Layout und Design-Token wie die Webanwendung

Belegte Vorgaben (Kundenzitate):

- „Das Dunkelgrau als Hintergrundfarbe sieht häßlich aus. Die App soll genauso wie die
  Webanwendung einen tiefschwarzen Hintergrund haben."
- „Du hast dich zu sehr von der altenm App beeinflussen lassen! Das layout sollte so
  gestaltet werden, wie in der Webanwendung"
- „Sämtliche Buttons sollen genauso rund sein wie in der Webanwendung"
- „Ich habe die Zugangsdaten für die Webanwendung gegeben. Nutze sie, um dir selbst mal
  ein Bild vom Design und Handling der App zu machen."

Anforderung: Hintergrund, Flächenfarben, Textfarben, Akzentfarben, Rahmen und
Eckenradien der App entsprechen den Design-Tokens der Webanwendung. Maßgeblich sind die
in `web-design-recherche.md` dokumentierten CSS-Variablen (u. a. `--vp-bg` `#131313`,
`--vp-bg-deep` `#0e0e0e`, `--vp-surface*` `#1c1b1b`–`#393939`, `--vp-text` `#e5e2e1`,
`--vp-primary` `#e50914`, `--vp-secondary` `#00a8e8`, `--vp-radius-sm` `4px`,
`--vp-radius` `8px`).

Annahme/Präzisierung (aus der Recherche, nicht aus dem Kundentext): „Tiefschwarz" meint
den Schwarzton der Webanwendung (`#131313`/`#0e0e0e`), nicht zwingend `#000000`.
„Genauso rund wie in der Webanwendung" bedeutet: normale Buttons 4px-Radius
(`--vp-radius-sm`), Chip-/Badge-Elemente pill-förmig (`999px`) — nicht alle Elemente
vollständig rund.

### FA-2 Startseite: Hero-Kopfbereich mit Einstellung

Belegte Vorgabe (Kundenzitat): „Der Kopfbereich mit dem generierten Bild soll auch in
der App gezeigt werden. Er soll in den Einstellungen ausblendbar sein. Die Einstellung
muss positiv gestaltets ein, also so, dass der Kopfbereich aktivierbar ist und
standardgemäß aktiviert ist."

Anforderung:

- Die Startseite zeigt oberhalb der Inhalte einen Hero-Bereich mit dem servergenerierten
  Bild (`GET api/pictures/hero-background`, liefert generiertes JPEG bzw. Placeholder;
  Authentifizierung erforderlich).
- Darstellung wie in der Webanwendung: Vollbreite, `background-size: cover`-äquivalent,
  geschichtete Dunkelgradienten, Kicker-Chip + Titel + Beschreibungstext (Markup- und
  CSS-Referenz in `web-design-recherche.md`, Abschnitt „Startseiten-Hero").
- In den Einstellungen gibt es einen positiv formulierten Schalter (Sinnbild:
  „Kopfbereich anzeigen"), der den Hero ein-/ausblendet. Standardwert: aktiviert.
- In der Webanwendung existiert eine solche Einstellung bereits (Standard: an) — die
  App übernimmt dieses Verhalten; ob die Einstellung serverseitig synchronisiert oder
  app-lokal gehalten wird, ist eine Entwurfsentscheidung (Annahme: app-lokal, analog zu
  den übrigen App-Einstellungen in `AppSettings`).

### FA-3 Detailseiten-Header für Serien, Staffeln und Episoden

Belegte Vorgabe (Kundenzitat): „Der Kopfbereich soll sich sio darstellen, wie bei der
Webanwendung. Die Hintergrundgrafik soll auf kompletter Breite gezeigt werden. Darauf
sind dann die Serien-, Staffel-, oder Episodeninformationen platziert."

Anforderung:

- Serien-, Staffel- und Episoden-Detailseiten zeigen einen Header mit Hintergrundgrafik
  auf kompletter Breite (Serie/Staffel: Serien-Backdrop; Episode: Episoden-Still —
  Zuordnung aus der Web-Referenz, `web-design-recherche.md` Abschnitt
  „Detailseiten-Header").
- Auf der Grafik liegt ein Dunkelgradient-Overlay; darauf platziert sind die
  jeweiligen Informationen (Titel, Meta wie Jahr/Genres, Plot) — Position unten links
  wie im Web-Header (`tvshow-header`-Muster).
- Bildquelle über die bestehende Bild-API (`api/pictures/{backdropId}`).

### FA-4 Angesehen-Symbol bei Episoden

Belegte Vorgabe (Kundenzitat): „Das Angesehen-Symbol bei den Episoden fehlt."

Anforderung: In Episodenlisten wird pro Episode ein Angesehen-Symbol angezeigt, wenn die
Episode als gesehen markiert ist (Web-Referenz: `WatchedIndicator.razor` — SVG-Auge als
rundes Badge über dem Poster, gerendert nur wenn `WatchedAt` gesetzt ist; Details in
`web-design-recherche.md`). Annahme: Die für die Anzeige nötige Information
(`WatchedAt` o. ä.) ist über die Server-API verfügbar bzw. bereits im MVU-Model
vorhanden (`AppUpdate.cs`/`PlayerState.cs` kennen Watched-Zustände); falls die API das
Feld für Episodenlisten nicht liefert, ist das eine offene Frage (s. u.).

### FA-5 Download-Seite: kein Flickern bei Fortschrittsanzeige

Belegte Vorgabe (Kundenzitat): „Ist ein Titel in den Downloads eingeplant und wird die
Download-Seite angezeigt, so fklacket die ganze Anzeige während der Fortschrittsbalken
sich verändert."

Anforderung: Während laufender/geplanter Downloads aktualisiert sich der
Fortschrittsbalken auf der Download-Seite flüssig, ohne dass die übrige Anzeige
flackert (kein sichtbares Neuzeichnen/Ruckeln der gesamten Seite oder anderer
Listenelemente). Die konkrete Ursache (z. B. vollständiger View-Neuaufbau bei jeder
Fortschritts-Message) ist Implementierungssache — gefordert ist das beobachtbare
Ergebnis.

### FA-6 Videoplayer: Steuerelemente in der großen Ansicht klickbar

Belegte Vorgabe (Kundenzitat): „Wird ein Video in der normalen großen Ansicht gestartet,
so sind die Videosteuerelemente nicht klickbar. Erst wenn man den Mini-Player aufruft,
lässt sich das Video dort bspw. pausieren."

Anforderung: Die Steuerelemente des Videoplayers (Play/Pause, Seek, ggf. weitere
Transportfunktionen) sind in der normalen Vollansicht der Player-Seite bedienbar — ohne
dass zuvor der Mini-Player (Picture-in-Picture, `PictureInPictureService`) geöffnet
werden muss.

### FA-7 Videoplayer: Absturz bei Zweitwiedergabe beheben (COMException)

Belegte Vorgabe (Kundenzitat): „nachdem einmal ein Video abgespielt wurde lässt sich nun
kein weiteres abspielen. Das Programm stürzt offenbar mit einer unbehandelten Ausnahme
ab: System.Runtime.InteropServices.COMException" — mit diesem Stacktrace:

```
System.Runtime.InteropServices.COMException
  HResult=0x8001010E
  Quelle = WinRT.Runtime
  Stapelüberwachung:
   bei WinRT.ExceptionHelpers.<ThrowExceptionForHR>g__Throw|38_0(Int32 hr) in WinRT\ExceptionHelpers.cs: Zeile134
   bei ABI.Microsoft.UI.Xaml.Controls.IMediaPlayerElementMethods.get_MediaPlayer(IObjectReference _obj)
   bei Microsoft.UI.Xaml.Controls.MediaPlayerElement.get_MediaPlayer()
   bei CommunityToolkit.Maui.Core.Views.MediaManager.<PlatformSeek>d__33.MoveNext()
   bei CommunityToolkit.Maui.Core.Handlers.MediaElementHandler.<MapSeekRequested>d__18.MoveNext()
   bei System.Threading.Tasks.Task.<>c.<ThrowAsync>b__124_1(Object state)
   bei System.Threading.ThreadPoolWorkQueue.Dispatch()
   bei System.Threading.PortableThreadPool.WorkerThread.WorkerThreadStart()
   bei System.Threading.Thread.StartCallback()
```

Anforderung: Nach der Wiedergabe eines Videos muss ein weiteres Video abspielbar sein,
ohne dass die App mit einer unbehandelten Ausnahme abstürzt. Der Stacktrace zeigt einen
`HResult 0x8001010E` (RPC_E_WRONG_THREAD) beim Zugriff auf
`MediaPlayerElement.MediaPlayer` aus `MediaManager.PlatformSeek` auf einem
Threadpool-Thread — sprich: UI-Thread-Verletzung bzw. ungültiger Player-Zustand bei der
Zweitwiedergabe. Gefordert ist, dass die Wiedergabe wiederholt funktioniert und kein
unbehandelter Absturz auftritt (Behebung der Ursache, nicht nur Abfangen der
Ausnahme — falls die Ausnahme in der CommunityToolkit-Komponente liegt, ist ein
Workaround auf App-Seite zulässig, sofern das Verhalten stimmt).

## Betroffene Bereiche

- **Theme/Styles (`VideoPlayer.Maui`):** `Resources/Styles/Colors.xaml`,
  `Resources/Styles/Styles.xaml` sowie inline gesetzte Farben/Radien in den Seiten —
  Angleichung an die Web-Design-Tokens (FA-1).
- **Startseite (`VideoPlayer.Maui`):** `Pages/HomePage.xaml(.cs)` — neuer Hero-Bereich
  (FA-2); Bildbezug über `api/pictures/hero-background` (Auth über bestehenden
  HttpClient/`AuthSessionService`).
- **Einstellungen (`VideoPlayer.Maui` + `VideoPlayer.Storage` +
  `VideoPlayer.Mvu`):** `Pages/SettingsPage.xaml(.cs)`, `AppSettings`/
  `AppSettingsService` — neuer, positiv formulierter Schalter für den Hero,
  Standard „an" (FA-2).
- **Detailseiten (`VideoPlayer.Maui`):** `Pages/MediaDetailsPage.xaml(.cs)` — Header
  mit Vollbreiten-Hintergrundgrafik + Overlay für Serien/Staffeln/Episoden (FA-3) und
  Angesehen-Symbol in Episodenlisten (FA-4).
- **Download-Seite (`VideoPlayer.Maui` + `VideoPlayer.Mvu`):**
  `Pages/DownloadsPage.xaml(.cs)` und die Fortschritts-Messages/Übergänge in
  `AppUpdate.cs` (`AppUpdateTests_Downloads` zeigen den bestehenden Pfad) —
  flackerfreie Fortschrittsaktualisierung (FA-5).
- **Videoplayer (`VideoPlayer.Maui`):** `Pages/PlayerPage.xaml(.cs)`,
  `Services/PictureInPictureService.cs`, MediaElement
  (`CommunityToolkit.Maui.MediaElement` 10.0.0) — klickbare Steuerelemente (FA-6) und
  Absturz bei Zweitwiedergabe (FA-7); ggf. MVU-Player-Zustand (`PlayerState.cs`,
  Seek-/Playback-Messages in `AppUpdate.cs`/`AppEffect.cs`).
- **Tests (`VideoWebPlayer.Maui.Tests`):** Contract-Tests zu den geänderten Seiten
  und `AppUpdateTests_*` für neue/geänderte Übergänge (Hero-Einstellung,
  Watched-Anzeige, Download-Fortschritt, Player-Zweitwiedergabe).

## Abläufe

- **Startseite öffnen (verbunden):** Hero-Bild wird über `api/pictures/hero-background`
  geladen und als Vollbreiten-Kopfbereich mit Gradient-Overlay, Kicker, Titel und
  Beschreibung angezeigt — sofern die Einstellung aktiv ist (Standard).
- **Einstellung ändern:** Schalter „Kopfbereich anzeigen" (positiv formuliert) blendet
  den Hero sofort bzw. beim nächsten Anzeigen der Startseite aus/ein; der Wert wird
  persistiert.
- **Detailseite öffnen:** Serie/Staffel/Episode zeigt Header-Grafik voller Breite mit
  Overlay und darauf platzierten Informationen; Episodenlisten zeigen bei gesehenen
  Episoden das Angesehen-Symbol.
- **Download-Seite bei laufendem Download:** Fortschrittsbalken aktualisiert sich
  kontinuierlich; restliche Anzeige bleibt stabil (kein Flackern).
- **Wiedergabe:** Video in großer Ansicht starten → Steuerelemente sofort klickbar.
  Anschließend ein zweites Video starten → Wiedergabe ohne Absturz (wiederholbar).

## Konfiguration

- Ein neuer App-Einstellungswert für den Startseiten-Hero (positiv formuliert,
  Standard: aktiviert) in `AppSettings`/`AppSettingsService`; Persistenz wie die
  übrigen App-Einstellungen.
- Keine weiteren neuen Konfigurationseinträge; Bild- und Datenbezüge laufen über die
  bestehende Server-API und den vorhandenen authentifizierten HttpClient.

## Akzeptanzkriterien

- **FA-1:** Hintergrund der App ist der Schwarzton der Webanwendung (`#131313` bzw.
  `#0e0e0e` je Fläche), kein Dunkelgrau. Die dokumentierten Design-Tokens aus
  `web-design-recherche.md` sind in den App-Styles abgebildet. Normale Buttons haben
  denselben Eckenradius wie in der Webanwendung (4px), Chip-/Badge-Elemente sind
  pill-förmig.
- **FA-2:** Die Startseite zeigt den Hero mit servergeneriertem Bild. Der
  Einstellungsschalter ist positiv formuliert, standardmäßig aktiviert und blendet den
  Hero wirksam aus/ein; der Wert überlebt einen App-Neustart.
- **FA-3:** Serien-, Staffel- und Episoden-Detailseiten zeigen je einen
  Vollbreiten-Header mit Hintergrundgrafik, auf dem Titel/Meta/Plot platziert sind —
  entsprechend dem `tvshow-header`-Muster der Webanwendung.
- **FA-4:** Episoden mit gesetztem Gesehen-Status zeigen das Angesehen-Symbol
  (Augen-Badge wie in der Webanwendung); ungesehene Episoden zeigen keins.
- **FA-5:** Bei eingeplantem/laufendem Download verändert sich auf der Download-Seite
  nur der Fortschrittsbalken sichtbar; kein Flackern/Neuzeichnen der übrigen Anzeige
  (manuell beobachtbar; kein Test wird deshalb abgeschwächt).
- **FA-6:** In der normalen Player-Ansicht sind alle Steuerelemente (mindestens
  Play/Pause und Seek) ohne Umweg über den Mini-Player bedienbar.
- **FA-7:** Nach erfolgreicher Wiedergabe eines Videos lässt sich ein weiteres Video
  abspielen; die beschriebene COMException tritt nicht mehr auf. Nachweis z. B. durch
  einen Test/Workflow, der zwei Wiedergaben hintereinander ausführt.
- Alle bestehenden Tests bleiben grün; es gilt die Definition of Done aus `AGENTS.md`
  (Build Debug+Release, volle Testsuite).

## Explizite Nicht-Ziele

- Kein Umbau der Server-Webanwendung (Blazor); sie dient nur als Design-Referenz.
- Kein Android-Support; Zielplattformen bleiben Windows und iOS.
- Kein Funktionsausbau über die genannte Rückmeldung hinaus (keine neuen
  Navigationspunkte, keine zusätzlichen Player-Features, kein Redesign von Bereichen,
  die im Feedback nicht genannt sind, soweit sie nicht von FA-1 „Layout wie die
  Webanwendung" erfasst sind).
- Keine Änderung der Server-API; die App nutzt ausschließlich die öffentliche API.
- Kein Update der CommunityToolkit-Hauptversion ohne Prüfung von Lizenz, Release-Build
  und CI (vgl. `AGENTS.md`).

## Offene Fragen

1. **Gesehen-Status in Episodenlisten:** Liefert die Server-API für Episodenlisten ein
   Feld wie `WatchedAt`, oder muss der Status aus einem anderen Endpunkt/dem lokalen
   Wiedergabestand abgeleitet werden? (Betrifft FA-4.)
2. **Hero-Einstellung serverseitig?** Existiert die Web-Einstellung zum Hero
   serverseitig (dann ggf. Synchronisation) oder rein clientseitig? Annahme bis zur
   Klärung: app-lokale Einstellung.
3. **Ursache der nicht klickbaren Steuerelemente:** Liegt ein Overlay/ein
   InputTransparent-Problem vor, oder werden die Controls vom MVU-Zustand deaktiviert?
   Klärung bei der Bestandsaufnahme; ändert nichts am geforderten Ergebnis (FA-6).
