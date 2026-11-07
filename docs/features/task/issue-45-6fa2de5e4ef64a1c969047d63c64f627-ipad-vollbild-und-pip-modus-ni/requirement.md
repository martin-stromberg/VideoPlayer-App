# Übersetzte Anforderung: iPad — Vollbild- und PiP-Schaltflächen der nativen Wiedergabesteuerung frei erreichbar (Issue #45)

Branch: `task/issue-45-6fa2de5e4ef64a1c969047d63c64f627-ipad-vollbild-und-pip-modus-ni`

Quelle: TestFlight-Rückmeldung (Issue #45) — auf dem iPad kann bei der
Videowiedergabe weder in den Vollbild- noch in den PiP-Modus gewechselt
werden, weil das Titel-Overlay die entsprechenden Schaltflächen der
nativen Wiedergabesteuerung verdeckt bzw. deren Eingaben abfängt.

## Fachliche Zusammenfassung

Auf der Wiedergabeseite (`PlayerPage`, Shell-Route `player`) des
`VideoPlayer.Maui`-Projekts sind unter iOS (iPad) die Vollbild- und
PiP-Schaltflächen der nativen Transportsteuerung des `MediaElement`
(`ShouldShowPlaybackControls="True"`, plattformseitig ein
`AVPlayerViewController`) nicht bedienbar. Verursachend sind die App-
Überlagerungen im oberen Bereich des `PlayerRoot`-Grids über dem
`VideoElement`: das Kopfband `OverlayChrome` (`VerticalStackLayout` mit
`Border` — darin `TitleLabel`, `PlaylistContextLabel`, `WatchedBadge`)
sowie `PlaylistNavigationBar` (`PreviousButton`/`NextButton`, oben
rechts, `Margin="0,64,16,0"`) und `PipButton` (oben rechts,
`Margin="0,18,16,0"` — unter iOS ohnehin unsichtbar, da
`IPictureInPictureService.IsToggleSupported` dort `false` ist). Genau
in diesem oberen Bereich blendet iOS die native Transportleiste mit
den Vollbild-/PiP-Buttons ein.

Der `Border` des Kopfbands ist zwar `InputTransparent="True"`; unter
iOS fangen die umschließenden Layouts die Zeigeeingaben in ihren
Bounds dennoch ab (Hit-Testing auf MAUI-Ebene bzw. der nativen
Steuerungs-Ebene) — dasselbe Hit-Test-Problem, das in Schritt 3 der
Neugestaltung unter Windows bereits einmal aufgetreten und dort durch
`VerticalOptions="Start"` plus eingabedurchlässigem Band gelöst wurde
(siehe `PlayerContractTests.PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface`
und den Kommentarblock in `PlayerPage.xaml`).

Gefordert ist: Auf dem iPad sind die Vollbild- und PiP-Schaltflächen
der nativen Wiedergabesteuerung frei erreichbar und bedienbar — das
Titel-Overlay darf sie weder überdecken noch deren Eingaben abfangen.
Die Overlay-Funktionen selbst (Titel-/Kontextanzeige, „Angesehen"-
Badge, Playlist-Navigation) bleiben funktionsfähig; das Windows-
Verhalten bleibt unverändert.

## Betroffene Klassen und Komponenten

- **App-Rand (`VideoPlayer.Maui`):**
  - `Pages/PlayerPage.xaml` — `PlayerRoot`-Grid mit den gestapelten
    Elementen `VideoElement` (`mct:MediaElement`), `LoadingIndicator`,
    `OverlayChrome` (`VerticalStackLayout` `VerticalOptions="Start"`
    → innerer `Border` `InputTransparent="True"` mit Scrim-Verlauf,
    `TitleLabel`, `PlaylistContextLabel`, `WatchedBadge`),
    `PlaylistNavigationBar` (`HorizontalStackLayout`, oben rechts,
    nur bei Playlist-Wiedergabe sichtbar), `PipButton` (oben rechts,
    nur Windows sichtbar), `ErrorPanel` (unten).
  - `Pages/PlayerPage.xaml.cs` — `UpdateUi` steuert die Sichtbarkeit
    (`OverlayChrome.IsVisible = !player.IsPictureInPicture`,
    `PlaylistNavigationBar.IsVisible = player.IsPlaylistPlayback
    && !player.IsPictureInPicture`); im Konstruktor
    `PipButton.IsVisible = pictureInPicture.IsToggleSupported`.
    Ggf. plattformbedingte Anpassungen (`#if IOS`) — Code-Behind-
    Änderung nur, falls das Layout allein nicht ausreicht.
- **Plattformschicht iOS (`VideoPlayer.Maui/Platforms/iOS/`):** nur
  falls ein Handler-Eingriff nötig würde (z. B. MediaElement-Mapper
  am `AVPlayerViewController`) — derzeit nicht vorhanden und als
  Option zu bewerten, keine Vorgabe (Annahme: Layout-Korrektur
  genügt).
- **MVU-Kern (`VideoPlayer.Mvu`):** `PlayerState`
  (`IsPlaylistPlayback`, `IsPictureInPicture`, `PlaylistLabel`,
  `IsWatched`, `Title`) — keine Änderung erwartet; die
  Sichtbarkeitssteuerung bleibt wie sie ist.
- **Dienste (Kontext, unverändert):** `IPictureInPictureService`/
  `PictureInPictureService` — iOS-PiP kommt aus der nativen
  Transportsteuerung des `AVPlayerViewController` (Voraussetzung
  `UIBackgroundModes=audio` in `Platforms/iOS/Info.plist` besteht);
  der Windows-Pfad (`AppWindowPresenterKind.CompactOverlay`) bleibt
  unberührt.
- **Tests (`VideoWebPlayer.Maui.Tests`):**
  - `PlayerContractTests` — quelltextbasierte Contract-Tests
    (MAUI-Schicht im Testprojekt nicht instanziierbar, etablierte
    Konvention); der bestehende Pin
    `PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface` (prüft
    `VerticalOptions="Start"`, `InputTransparent` am Band, kein
    `Button` im Band, `PipButton` als eigenes Grid-Element) ist um
    die gewählte iOS-Korrektur zu erweitern bzw. anzupassen.
  - Echte iOS-UI-Automatisierung existiert im Repository nicht
    (dokumentierte Einschränkung) — die Geräteverifikation erfolgt
    manuell über TestFlight, die Grenze ist wie bei früheren
    iOS-Fixes in `docs/help/` zu dokumentieren.
- **Dokumentation:** `docs/help/wiedergabe-pip.md` (Abschnitte
  „Bedienung in der Vollansicht" und „Picture-in-Picture/Mini-Player")
  ist im Lifecycle-Schritt Dokumentation anzupassen.

## Implementierungsansatz

- Der Eingriff liegt auf Layout-/Hit-Test-Ebene in `PlayerPage.xaml`
  (plus ggf. konditionalem Code im `.xaml.cs`); alle Overlay-
  Funktionen müssen erhalten bleiben.
- Kandidatenrichtungen (Entscheidung im Umsetzungsplan):
  1. Das umschließende `OverlayChrome`-Layout selbst eingabedurch-
     lässig machen (`InputTransparent` auf die `VerticalStackLayout`,
     nicht nur auf den inneren `Border`) — derzeit fehlt das Attribut
     am Elternlayout, sodass dessen Bounds unter iOS trotz transparentem
     Inhalt hit-testen können. Unbedenklich, weil das Band keine
     interaktiven Kinder enthält (XAML-Kommentar-Vorgabe: ein
     interaktives Element darf nicht im eingabedurchlässigen Band
     liegen — unter Windows deaktiviert `InputTransparent` am Elter
     den gesamten Teilbaum).
  2. Die Überlagerungselemente aus dem Bereich der nativen
     Transportleiste verlegen bzw. ihre Ausdehnung verkleinern — z. B.
     `OverlayChrome`/`PlaylistNavigationBar` plattformbedingt
     positionieren, einengen (`HorizontalOptions`, Breitenbegrenzung)
     oder unter die Leiste schieben, damit die Ecke frei bleibt, in
     der iOS Vollbild-/PiP-Buttons einblendet. Hinweis: `OnPlatform`/
     `OnIdiom` und `DeviceInfo.Idiom` werden im Projekt derzeit nicht
     verwendet; plattformbedingte Varianten wären per `#if IOS` oder
     Laufzeit-Idiom-Prüfung neu einzuführen (Annahme, in der Planung
     zu entscheiden).
  3. Bedarfsgesteuertes Ein-/Ausblenden des Overlays synchron zur
     nativen Steuerleiste — auf iOS ist deren Sichtbarkeit ohne
     Handler-Erweiterung nicht beobachtbar; daher vermutlich nicht
     ohne Plattformcode umsetzbar (Option, keine Vorgabe).
- Harte Rahmenbedingungen aus dem Bestand:
  - `PlaylistNavigationBar` (`PreviousButton`/`NextButton`) muss
    klickbar bleiben — sie darf nicht pauschal `InputTransparent`
    werden und muss außerhalb eingabedurchlässiger Elternlayouts
    bleiben.
  - `OverlayChrome` bleibt im Mini-Player ausgeblendet
    (`IsPictureInPicture`) und `PlaylistNavigationBar` ebenfalls;
    `PipButton` bleibt Windows-only (`IsToggleSupported`).
  - Der Windows-Workaround (`#if WINDOWS`-Tap auf `PlayerRoot` →
    `TransportControls.Show()` in `OnPlayerTapped`) bleibt
    unberührt; Windows-Verhalten darf nicht regressieren.
  - `ShouldShowPlaybackControls="True"` bleibt bestehen — die
    native Steuerung ist der geforderte Bedienweg (PiP auf iOS läuft
    ausschließlich darüber).
- Verifikation: Geräteprüfung auf iPad per TestFlight ist nicht
  automatisierbar — Nachweis über angepassten Contract-Test plus
  dokumentiertem manuellen Prüfschritt (Muster der bisherigen
  iOS-Verifikationsgrenzen in `docs/help/`).

## Konfiguration

Keine — das Verhalten ist nicht konfigurierbar gefordert.

## Akzeptanzkriterien

1. Auf dem iPad sind die Vollbild- und PiP-Schaltflächen der nativen
   Transportsteuerung während der Wiedergabe frei erreichbar und
   bedienbar; das Titel-Overlay überdeckt sie nicht und fängt ihre
   Eingaben nicht ab.
2. `OverlayChrome` (Titel, `PlaylistContextLabel`, `WatchedBadge`)
   bleibt in der Vollansicht sichtbar und im Mini-Player
   (`IsPictureInPicture`) ausgeblendet.
3. `PlaylistNavigationBar` („Zurück"/„Weiter") bleibt bei Playlist-
   Wiedergabe sichtbar und bedienbar — sie darf die nativen Buttons
   ebenfalls nicht blockieren.
4. Windows-Verhalten unverändert: Mini-Player-Schalter funktioniert,
   Transportsteuerung bleibt bedienbar (bestehender Contract-Pin
   darf nicht verletzt werden).
5. Contract-Tests (`PlayerContractTests`) pinnen die korrigierte
   Overlay-Anordnung; `dotnet test` grün; `dotnet build
   VideoPlayer.App.sln` in Debug und Release fehlerfrei.
6. Manuelle iPad-Verifikation (TestFlight) als dokumentierter
   Prüfschritt — mit dokumentierter iOS-Verifikationsgrenze, falls
   keine Automatisierung möglich ist.

## Offene Fragen

- **Genauer Blockierbereich:** Wo genau rendert iOS die Vollbild-/PiP-
  Buttons relativ zu den App-Overlays (obere Leiste, Ecke rechts)?
  Das bestimmt, ob `InputTransparent` am Elternlayout genügt oder ob
  `PlaylistNavigationBar`/`OverlayChrome` zusätzlich verlegt bzw.
  eingeengt werden müssen — nur per Geräteprüfung verlässlich klärbar.
- **Geltungsbereich:** Gilt die Korrektur nur für das iPad oder für
  iOS allgemein (iPhone eingeschlossen)? Die Rückmeldung nennt das
  iPad; eine Idiom-Unterscheidung (`DeviceInfo.Idiom`) existiert im
  Code noch nicht — zu klären, ob die Korrektur plattform- oder
  idiomweit greifen soll.
- **Fallback-Tiefe:** Reicht die Layout-/Transparenz-Korrektur auf
  MAUI-Ebene, oder ist ein Handler-Eingriff am `AVPlayerViewController`
  des Toolkit-`MediaElement` nötig (invasiver, eigener Plattformcode)?
  Vermutlich ersteres — Annahme, im Plan zu verifizieren.
