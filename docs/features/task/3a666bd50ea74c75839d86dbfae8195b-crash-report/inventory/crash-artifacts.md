# Crash-Artefakte und Build-/Symbolisierungslage

Bestandsaufnahme der TestFlight-Artefakte unter
`testflight_feedback_extracted/` und der verfügbaren
Symbolisierungs-/Build-Artefakte im Repo.

## `feedback.json`

Datei: `testflight_feedback_extracted/feedback.json`

| Feld | Wert |
|------|------|
| App | `de.martinstromberg.videos`, Version 1.1.0, Build 213 |
| Gerät | `iPad11,1` (iPad mini 6), arm64e |
| OS | iOS 26.6.1 (23G83) |
| Zeitpunkt | 2026-10-10 16:06:25 UTC (Feedback) |
| Kommentar | „Absturz" (E-Mail: mstromberg84@icloud.com) |
| Umgebung | WLAN, Akku 15 %, ~6 GB frei von ~54 GB |

## `crashlog.crash`

Datei: `testflight_feedback_extracted/crashlog.crash`

- `Process: VideoPlayer.Maui`, `Version: 1.1.0 (213)`,
  `Beta: YES`, `Role: Foreground`.
- `Date/Time: 2026-10-10 18:06:04 +0200`, `Launch Time:
  2026-10-10 16:50:58 +0200` → App lief bereits ~75 Minuten,
  Absturz **nicht** beim App-Start.
- `Exception Type: EXC_CRASH (SIGABRT)`, `Termination Reason: SIGNAL 6
  Abort trap: 6`, `Triggered by Thread: 0` (UI-/Hauptthread).
- Thread-0-Stack (unsymbolisiert, Basis `0x102c84000`, UUID
  `4a985358bbc739088b694b223bf59f2e`):

  | Frame | Inhalt |
  |-------|--------|
  | 0–2 | `__pthread_kill` → `pthread_kill` → `abort` |
  | 3–4 | App-Frames (`0x102c84000 + 48408752`, `+ 47848596`) — Mono/Runtime-Abort-Pfad |
  | 5 | `_sigtramp` (Signal-Trampoline) |
  | 6–7 | erneut `pthread_kill` → `abort` |
  | 8–12 | App-Frames (`+48270200` … `+48084948`) |
  | 13 | `dyld start` |

  Die Doppel-Abort-Struktur (`abort` → App-Frames → `_sigtramp` →
  `abort`) entspricht dem typischen Pfad einer unbehandelten verwalteten
  Ausnahme bzw. einer ObjC-Ausnahme, die über den
  Runtime-Signalhandler in `abort` mündet. Keine verwalteten Frames
  symbolisiert; keine `Last Exception Backtrace`-Sektion im Report.

## Zuordnung Build 213 → Commit

- Buildnummer = `git rev-list --count HEAD` + `run_attempt` − 1
  (`.github/actions/package-ios/action.yml` :152).
- `git rev-list --count` von Commit `a9a5a75` („fix: package-ios —
  TFM-Scoping …", 2026-10-10 16:23) = 213 → **Build 213 stammt sehr
  wahrscheinlich von `a9a5a75`** (run_attempt 1) oder dessen direktem
  Vorgänger.
- `PlayerPage.xaml.cs` ist zwischen `a9a5a75` und HEAD (`bd3b54e`,
  `git rev-list --count` = 222) unverändert (`git diff` leer) — alle
  heute im Player-Pfad vorhandenen Härtungen waren im Crash-Build
  enthalten.

## dSYM-/Symbolisierungslage (Befund: nicht archiviert)

- CI-Job `package-ios` (`.github/actions/package-ios/action.yml`):
  `dotnet publish … -p:ArchiveOnBuild=true`, dann „Collect iOS
  package" (:168–192) — sammelt ausschließlich `*.ipa` aus
  `VideoPlayer.Maui/bin/Release`. Ein Upload an App Store Connect
  erfolgt per `iTMSTransporter` (:198–248).
- **Keine Stelle** in `.github/`, `scripts/` oder den Workflows
  erzeugt, sammelt oder archiviert `*.dSYM`-Bundles — kein
  `artifact`-Upload, kein dsym-Upload an App Store Connect.
- `scripts/iOS-Deployment.ps1` (lokaler Store-Pfad) enthält ebenfalls
  keinen dSYM-Export (grep: keine Treffer für `dsym`/`symbol`).
- Konsequenz: Der vorliegende Report lässt sich nur symbolisieren,
  wenn der Store-Build lokal auf dem CI-/Build-Rechner reproduziert
  wird (gleicher Commit `a9a5a75`, gleiche Workload —
  `ios-workload-version` Default `26.5.10318`, MauiXamlInflator wird
  im CI-Build auf `XamlC` gesetzt statt `SourceGen` der csproj :161)
  und die dSYM aus `bin/Release/net10.0-ios/ios-arm64/` des
  reproduzierten Builds verwendet wird. Adress-Offsets im Report
  (Basis `0x102c84000`) sind dafür verwendbar (`atos -o … -l
  0x102c84000 <addr>`).

## iOS-Plattformkonfiguration

- `VideoPlayer.Maui/Platforms/iOS/Info.plist`: `CFBundleIdentifier
  de.martinstromberg.videos`, `UIBackgroundModes` = `audio` (PiP- und
  Hintergrund-Audio), `NSAppTransportSecurity.NSAllowsArbitraryLoads =
  true` (HTTP-Streams im Heimnetz erlaubt), Orientierungen Portrait +
  beide Landscape (iPad zusätzlich PortraitUpsideDown).
- `VideoPlayer.Maui/Platforms/iOS/Program.cs`: Standard-
  `UIApplication.Main` — keine Crash-Handler.
- `VideoPlayer.Maui/Platforms/iOS/AppDelegate.cs`: Standard
  `MauiUIApplicationDelegate` — keine Crash-Handler.
- `VideoPlayer.Maui/Platforms/iOS/Entitlements.plist` und
  `csproj` :44–51: CodesignKey `Apple Development` /
  `VS: WildCard Development` für lokale Geräte-Builds; Store-Build
  kommt aus der CI mit Distribution-Signatur.
- Toolkit-Aktivierung: `builder.UseMauiCommunityToolkitMediaElement(true)`
  (`MauiProgram.cs` :33) — `true` ist
  `isAndroidForegroundServiceEnabled` (Android-Manifest-Permissions;
  auf iOS wirkungslos). Paketversion `CommunityToolkit.Maui.MediaElement
  10.0.0` (`VideoPlayer.Maui.csproj` :68).

## Bekannte Toolkit-Befunde (Referenz)

- `CommunityToolkit.Maui.MediaElement` 10.0.0, iOS-Implementierung
  `Views/MediaManager.macios.cs` + `Handlers/MediaElementHandler.shared.cs`
  (verifiziert gegen Toolkit-Quellstand Commit `cdf9fa5c`, entspricht
  dem 10.0.0-Release-Zeitraum):
  - `MediaOpened` feuert auf iOS/macOS **vor**
    `AVPlayerItemStatus.ReadyToPlay` (GitHub-Issue #3249, offen).
  - `MapSeekRequested` ist `public static async void` — Fehler aus
    `PlatformSeek` (`CurrentItem is null`, `Status != ReadyToPlay`,
    `complete == false` im Native-Callback) werden zu unbehandelten
    Ausnahmen → SIGABRT.
  - `MapSource` ist synchron (`UpdateSource()`-ValueTask wird
    verworfen) — Quell-Fehler faulten unbeobachtet.
