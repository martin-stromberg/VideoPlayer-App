# Testergebnisse

## Zusammenfassung

|| Lauf | Ergebnis |
||------|----------|
|| Baseline (vor der Umsetzung) | **469 Tests bestanden, 0 Fehler, 0 übersprungen** — Inventar-Lauf aus `inventory/tests.md` |
|| Finaler Testlauf | **510 Tests bestanden, 0 Fehler, 0 übersprungen** — `final-dotnet-test.log` |
|| Build `VideoPlayer.Mvu` | **0 Warnungen, 0 Fehler** |
|| Build `VideoPlayer.Maui` (net10.0-windows10.0.19041.0) | **0 Warnungen, 0 Fehler** |

## Ausgeführte Kommandos

```text
dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj
```

Ergebniszeile:

```text
Bestanden!   : Fehler: 0, erfolgreich: 510, übersprungen: 0, gesamt: 510
```

```text
dotnet build VideoPlayer.Maui/VideoPlayer.Maui.csproj -f net10.0-windows10.0.19041.0
```

Ergebnis: `0 Warnung(en), 0 Fehler`.

## Neue Tests (41 gegenüber der Baseline)

### MVU-Übergänge (`VideoWebPlayer.Maui.Tests/AppUpdateTests_Player.cs`)

- `PlayRequestedMessage` → Player-State (Medium, Titel,
  `IsResolving`, `Attempted`) + `NavigateToPlayerEffect` +
  `ResolvePlaybackSourceEffect`; Fortsetzungsliste aus dem
  Serienkontext über Staffeln hinweg (32→33→34); leere Liste bei
  letzter Episode; Guards (`MediaId <= 0`, nicht abspielbare Typen).
- `PlaybackSourceResolvedMessage` → Erfolg übernimmt die Quelle;
  Fehlschlag setzt `Error` und fällt auf die nächste Episode zurück
  (bzw. bleibt ohne Folge-Episode im Fehlerzustand); veraltete
  Antworten werden verworfen.
- `PlaybackProgressMessage` → `ReportPlaybackProgressEffect` bei
  geänderter Position (> 0,5 s), keine erneute Meldung unveränderter
  Position; `IsWatched` ab `Duration - WatchedEndWindowSeconds`;
  Auto-Fortsetzung bei Restlaufzeit < `AutoResumeRemainingSeconds`
  (Schalter an) — nicht bei ausgeschaltetem Schalter, oberhalb des
  Fensters, ohne Folge-Episode oder im Pausenzustand.
- `PlaybackEndedMessage` → `IsWatched` + Abschlussmeldung mit
  `Position = Duration` + Auto-Fortsetzung; ohne Folge-Episode nur
  die Meldung.
- `PlaybackFailedMessage` → Fehlermeldung + Rückfall auf die nächste
  Episode.
- `PlayerClosedMessage` → Abschlussmeldung + `PlayerState.Empty` +
  PiP-Rücksprung (`TogglePictureInPictureEffect`); ohne Position nur
  Reset.
- `PictureInPictureRequestedMessage`/`…ChangedMessage` → Effekt +
  Flag; ohne aktive Wiedergabe unverändert.
- `LogoutRequestedMessage` → `Player` wird mit dem Sitzungszustand
  geleert.

### Quellauflösung (`PlaybackSourceServiceTests.cs`)

- Stream-URL `{base}/api/items/{typ}/{id}/stream?access_token=…` für
  Film und Episode.
- Fortsetzungsposition aus der Server-Weiterschauen-Liste
  (`PositionSeconds`); bei Server-Fehlschlag (Transportfehler über
  alle Wiederholungen) aus der Cache-Kopie `cached_lists`.
- Fertiger Download (`download_manifest`, `State = "completed"`,
  Datei vorhanden) → `LocalFile` mit Dateipfad und Fortsetzung;
  fehlende Datei oder unfertiger Download → Stream-Rückfall.
- Weder Datei noch Server → `null`; nicht abspielbare Typen und
  `MediaId <= 0` → `null`.

### Oberflächen-Verträge (`PlayerContractTests.cs`)

- Route `player` in `AppShell`; `CommunityToolkit.Maui.MediaElement`
  im csproj + `UseMauiCommunityToolkitMediaElement` und
  Dienst-Registrierungen in `MauiProgram`; `UIBackgroundModes`/
  `audio` in der iOS-`Info.plist`.
- `PlayerPage.xaml`: `MediaElement` mit Autoplay, nativer
  Transportsteuerung (iOS-PiP) und den Ereignissen
  `MediaOpened`/`MediaEnded`/`MediaFailed`.
- `PlayerPage.xaml.cs`: dispatcht `PlayerPageOpened`/`PlaybackProgress`/
  `PlaybackEnded`/`PlaybackFailed`/`PictureInPictureRequested`/
  `PlayerClosed`; `SeekTo`-Fortsetzung; `MediaSource.FromFile` für
  lokale Dateien.
- `PictureInPictureService`: `CompactOverlay`/`Default` unter
  `#if WINDOWS`.
- `MediaDetailsPage`: „Abspielen"-Kopfbutton + ▶ je Episode →
  `PlayRequestedMessage`.
- `ShellAppEffectHandler`: alle vier Player-Effekte verdrahtet;
  Fortschritte ausschließlich über `ProgressReportService`.

### Enum-Abdeckung (`EnumCoverageTests.cs`)

- `PlaybackSourceKind`: `ServerStream`, `LocalFile`.

## Anmerkungen

- Die Baseline-Nummer 469 stammt aus dem Inventar-Lauf
  (Schritt-9-Abschlussstand).
- Die MAUI-Schicht kann im Testprojekt nicht instanziiert werden —
  UI-relevante Anforderungen sind über Quell-Verträge abgedeckt
  (dokumentierte Einschränkung, Muster der Vor-Schritte).
