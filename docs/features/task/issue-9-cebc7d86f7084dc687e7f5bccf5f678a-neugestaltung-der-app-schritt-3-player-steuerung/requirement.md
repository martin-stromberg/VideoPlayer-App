# Übersetzte Anforderung: Videoplayer — Steuerelemente in der Vollansicht bedienbar (Entwicklungsschritt 3)

## Fachliche Zusammenfassung

Auf der Wiedergabeseite der neuen MAUI-App (`VideoPlayer.Maui`,
Route `player`) sind die nativen Transportsteuerungen des
`MediaElement` (Play/Pause, Seek sowie die übrigen Transportfunktionen)
in der normalen Vollansicht nicht klickbar. Erst im Mini-Player
(Windows-CompactOverlay bzw. iOS-PiP) lässt sich die Wiedergabe
bedienen. Durch die Projekt-Bestandsaufnahme verifizierte Ursache:
Das App-Overlay `OverlayChrome` (`VerticalStackLayout` mit Titel,
„Angesehen"-Hinweis und Mini-Player-Schalter) hat kein
`VerticalOptions` und füllt deshalb per Default (`Fill`) das gesamte
Wiedergabe-`Grid` über dem `MediaElement`. Obwohl nur das obere
Kopfband sichtbaren Inhalt zeigt, deckt der Container die komplette
Videofläche und fängt die Eingaben ab. Im Mini-Player funktioniert die
Bedienung, weil `OverlayChrome` dort per
`IsVisible = !player.IsPictureInPicture` ausgeblendet ist
(`PlayerPage.xaml.cs`).

Gefordert ist: In der Vollansicht sind alle nativen Steuerelemente —
mindestens Play/Pause und Seek — unmittelbar bedienbar, ohne dass der
Mini-Player geöffnet werden muss. Die App-Überlagerung (Titel,
Zurück-Navigation der Shell, Mini-Player-Schalter) muss funktionsfähig
bleiben und der Mini-Player unverändert nutzbar.

## Betroffene Klassen und Komponenten

- **App-Rand (`VideoPlayer.Maui`):**
  - `Pages/PlayerPage.xaml` — `Grid` mit `MediaElement` (Autoplay,
    `ShouldShowPlaybackControls="True"`), `LoadingIndicator`,
    `OverlayChrome` (`VerticalStackLayout` ohne `VerticalOptions` →
    füllt das gesamte Grid → Hit-Test-Blocker) und `ErrorPanel`.
  - `Pages/PlayerPage.xaml.cs` — `UpdateUi` setzt
    `OverlayChrome.IsVisible = !player.IsPictureInPicture`; der
    Mini-Player-Schalter `PipButton` dispatcht
    `PictureInPictureRequestedMessage`. Keine Code-Behind-Änderung
    erforderlich (reine Layout-Korrektur).
- **Tests (`VideoWebPlayer.Maui.Tests`):**
  - `PlayerContractTests` — Quelltext-Pins der Player-Schicht (Route,
    MediaElement-Verdrahtung, PiP-Dienst); um einen Pin zu erweitern,
    dass `OverlayChrome` die Videofläche nicht vollständig belegt.

## Implementierungsansatz

Kleinster sauberer Eingriff im XAML der `PlayerPage`: Die Ausdehnung
des `OverlayChrome`-Containers auf den sichtbaren Kopfbereich
beschränken (`VerticalOptions` auf einen Nicht-`Fill`-Wert, z. B.
`Start`), damit der unsichtbare Rest der Videofläche keine Eingaben
mehr abfängt. Alternativkandidaten aus der Bestandsaufnahme
(`InputTransparent` auf nicht-interaktive Teile, bedarfsgesteuertes
Einblenden) bleiben Optionen — die Entscheidung trifft der
Umsetzungsplan. Die nativen Transportsteuerungen des `MediaElement`
(`ShouldShowPlaybackControls`) bleiben unverändert aktiv; der
Mini-Player-Pfad (`IsPictureInPicture` → Overlay ausgeblendet) bleibt
unberührt.

## Konfiguration

Keine — das Verhalten ist nicht konfigurierbar.

## Akzeptanzkriterien

1. Play/Pause und Seek (und die übrigen nativen Transportfunktionen)
   sind in der normalen Vollansicht unter Windows sofort bedienbar.
2. Der Mini-Player-Schalter der App und die Zurück-Navigation der
   Shell funktionieren weiterhin.
3. Der Mini-Player bleibt unverändert nutzbar (Overlay dort weiter
   ausgeblendet).
4. Alle bestehenden Tests bleiben grün; Build in Debug und Release
   fehlerfrei.

## Offene Fragen

Keine — die Ursache ist durch die Projekt-Bestandsaufnahme verifiziert,
die Zielplattform des Akzeptanznachweises ist Windows (iOS nutzt
dieselbe Überlagerung und profitiert von derselben Korrektur; die
native PiP-Schaltfläche bleibt über die Transportsteuerung erhalten).
