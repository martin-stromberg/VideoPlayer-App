# Übersetzte Anforderung: Absturz beim Start eines Videos unter iOS untersuchen und beheben

Quelle: Kundenanforderung (issue.md) — „untersuche die Ursache des Crash beim
Start eines Videos", belegt durch ein TestFlight-Feedback-Paket
(`testflight_feedback_extracted/` mit `feedback.json` und `crashlog.crash`).
Der erste Teil der Anforderung (Branch auf den neuesten Stand bringen) ist
bereits erledigt.

## Fachliche Zusammenfassung

Die App `de.martinstromberg.videos` (Version 1.1.0, Build 213) stürzt unter
iOS beim Start einer Videowiedergabe ab. Laut Crash-Report bricht der Prozess
`VideoPlayer.Maui` mit `EXC_CRASH (SIGABRT)` / `SIGNAL 6 Abort trap` auf
Thread 0 (UI-/Hauptthread) ab — Gerät iPad11,1, iOS 26.6.1, arm64e, Nutzer-
Kommentar „Absturz". Der Stack ist weitgehend unsymbolisiert; die Frames in
der App-Binärdatei (Basisadresse `0x102c84000`, UUID
`4a985358bbc739088b694b223bf59f2e`) deuten auf den typischen
SIGABRT-Pfad einer unbehandelten Ausnahme bzw. eines nativen Aborts unter
.NET für iOS hin (`pthread_kill` → `abort` → App-Frames → `_sigtramp` →
erneut `abort` → `dyld start`).

Gefordert ist die Ursachenanalyse und -behebung: Das Starten eines Videos
(„Abspielen"-Schaltfläche auf der Detailseite bzw. ▶ in der Episodenliste)
darf die Anwendung nicht beenden. Da der Crash-Dump nicht symbolisiert ist,
ist die Symbolisierung des Reports gegen die Symboldateien (dSYM) des Builds
213 der erste Untersuchungsschritt; parallel ist der komplette
Wiedergabe-Startpfad auf iOS-spezifische Absturzursachen zu prüfen.

## Betroffene Klassen und Komponenten

- `VideoPlayer.Maui/Pages/PlayerPage.xaml` / `PlayerPage.xaml.cs` —
  Wiedergabeseite (Shell-Route `player`): `MediaElement`-Verdrahtung,
  `UpdateUi` (`VideoElement.Source`, `Stop()`), `OnMediaOpened` (`SeekTo`
  auf die Fortsetzungsposition), `OnMediaStateChanged`/`OnMediaFailed`/
  `OnMediaEnded`, `ReportPosition`, `StartProgressTimer`,
  `OnAppearing`/`OnDisappearing` (ModelChanged-Abo, `PlayerClosedMessage`).
- `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` — Effekte am
  App-Rand: `NavigateToPlayerEffect` (`NavigateToPlayerAsync`,
  `Shell.Current.GoToAsync("player")`), `ResolvePlaybackSourceEffect`
  (`ResolvePlaybackSourceAsync`), `ReportPlaybackProgressEffect`,
  `TogglePictureInPictureEffect`.
- `VideoPlayer.Mvu/PlaybackSourceService.cs` — Quellauflösung
  (`ResolveAsync`): lokale Download-Datei (`PlaybackSourceKind.LocalFile`)
  vs. Server-Stream-URL `api/items/{typ}/{id}/stream` mit
  `access_token`-Parameter, Fortsetzungsposition aus Weiterschauen-Liste.
- `VideoPlayer.Mvu/AppUpdate.cs` / `AppMessage.cs` / `PlayerState` —
  Wiedergabe-Zustandsautomat: `PlayRequestedMessage`,
  `PlayerPageOpenedMessage`, `PlaybackSourceResolvedMessage`,
  `PlaybackEndedMessage`, `PlaybackFailedMessage`, `PlayerClosedMessage`,
  `AdvanceToNextEpisode` (Serien-Fallback).
- `VideoPlayer.Maui/Platforms/iOS/` (`AppDelegate.cs`, `Program.cs`) und
  `Info.plist` (PiP-/Hintergrund-Audio-Deklaration) — iOS-spezifische
  Rahmenbedingungen des `MediaElement` (`AVPlayer`/`AVPlayerViewController`
  über `CommunityToolkit.Maui.MediaElement`).
- Symbolisierungs-/Build-Artefakte — dSYM bzw. Symboldateien des
  TestFlight-Builds 213 (iOS-Store-Build über CI,
  `scripts/`-Verzeichnis), zur Auflösung der unsymbolisierten Frames.
- `VideoWebPlayer.Maui.Tests` — Contract-Tests (Quelltext-basiert, da die
  MAUI-Schicht im Testprojekt nicht instanziierbar ist — etablierte
  Konvention, vgl. `PlayerContractTests`).

## Implementierungsansatz

1. **Symbolisierung:** `crashlog.crash` gegen die dSYMs des Builds 213
   auflösen (z. B. `atos`/Xcode-Symbolicator bzw. `symbolicatecrash`), um
   den realen verwalteten/nativen Stack des abstürzenden Threads 0 zu
   erhalten. Voraussetzung: Die zum Store-Build gehörenden Symboldateien
   sind verfügbar (Annahme — siehe Offene Fragen).
2. **Ursachenanalyse am Startpfad:** Den Wiedergabe-Start
   (`PlayRequestedMessage` → `ResolvePlaybackSourceEffect` →
   `NavigateToPlayerEffect` → `PlayerPage`/`MediaElement`) auf
   iOS-typische Abort-Ursachen prüfen. Bekannte Verdächtige (als
   Hypothesen, nicht als bestätigte Ursache):
   - unbehandelte verwaltete Ausnahme auf dem UI-Thread bzw. in einem
     Toolkit-Event-Handler (`MediaOpened`/`StateChanged` können auf
     Nicht-UI-Threads feuern — Zugriffe auf `VideoElement` müssen
     gemarshalt bleiben; Muster aus dem Windows-Fix
     `RPC_E_WRONG_THREAD`, siehe `docs/help/wiedergabe-pip.md`);
   - `NSException`/ObjC-Ausnahme beim Aufbau des `AVPlayer`
     (ungültige/leere Quell-URL, lokaler Dateipfad mit unzulässigen
     Zeichen oder fehlender Sandbox-Berechtigung);
   - `SeekTo` auf ein noch nicht bereites oder bereits freigegebenes
     natives Player-Objekt;
   - Zugriff auf ein losgelöstes `MediaElement` aus Timer- oder
     Modelländerungs-Callbacks (`UpdateUi`, `ReportPosition`);
   - Plattform-Einschränkungen (PiP-/Hintergrund-Audio-Modus,
     Transportsteuerung des `AVPlayerViewController`).
3. **Behebung:** Je nach Befund — defensive Absicherung des
   Player-Startpfads (Try/Catch um native Player-Zugriffe mit
   `PlaybackFailedMessage` statt Abort), Marshalling aller
   `VideoElement`-Zugriffe auf den UI-Thread, Validierung der aufgelösten
   `PlaybackSource.Location` vor der Übergabe an `MediaSource`, oder ein
   App-seitiger Workaround, falls die Ausnahme im
   `MediaElement`-Baustein des CommunityToolkit liegt (zulässig, sofern
   das beobachtbare Verhalten stimmt — Konvention aus dem
   Windows-Absturz-Fix).
4. **Regressionsschutz:** Contract-Test(s) in
   `VideoWebPlayer.Maui.Tests` für das vereinbarte Verhalten (z. B.
   Marshalling-/Fehlerpfad-Konventionen der `PlayerPage`); Fehler beim
   Wiedergabestart müssen sichtbar als `player.Error` im `ErrorPanel`
   landen, nicht als Prozessabbruch.

## Konfiguration

Keine neue Laufzeit- oder Anwendungskonfiguration erforderlich. Prozess-
seitig ist sicherzustellen (Annahme/zu prüfen), dass die dSYMs jedes
TestFlight-/Store-Builds archiviert werden, damit künftige Crash-Reports
direkt symbolisierbar sind.

## Offene Fragen

- Sind die Symboldateien (dSYM) zum TestFlight-Build 213
  (Binary-Image-UUID `4a985358bbc739088b694b223bf59f2e`) verfügbar — aus
  dem CI-Build oder dem lokalen Build-Verzeichnis? Ohne sie bleibt die
  Ursachenlokalisation auf Vermutungen angewiesen.
- Ist der Absturz reproduzierbar — bei jedem Video, nur bei bestimmten
  Medientypen (`movie`/`tvshowepisode`), nur bei lokaler Datei oder nur
  beim Server-Stream, nur mit gesetzter Fortsetzungsposition (`SeekTo`)?
- Betrifft es nur iOS/iPadOS (ggf. nur iOS 26) oder auch andere
  Plattformen (Windows/Android)?
- Lässt sich der Absturz intern nachstellen (verfügbares Testgerät,
  Simulator); gibt es weitere Crash-Reports desselben Testers?
- Der Nutzerkommentar lautet nur „Absturz" — gibt es vom Tester weitere
  Angaben zum Ablauf (welches Medium, welcher Einstiegspunkt, online oder
  offline)? Zum Zeitpunkt des Absturzes war die App bereits >1 Stunde
  gestartet (Launch 16:50, Crash 18:06), der Absturz erfolgte also nicht
  beim App-Start.
