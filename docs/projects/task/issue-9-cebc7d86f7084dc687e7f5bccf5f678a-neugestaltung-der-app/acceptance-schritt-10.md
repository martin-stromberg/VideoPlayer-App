# Abnahmeprüfung Entwicklungsschritt 10

**Schritt:** Videowiedergabe mit Picture-in-Picture und Fortschrittsverfolgung
**Branch:** `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-10-wiedergabe-pip`
**Datum:** 2026-09-23
**Prüfer:** Devin (unabhängige Abnahme gemäß Projekt-Workflow)

## Geprüfte Akzeptanzkriterien

| Kriterium | Nachweis | Ergebnis |
|---|---|---|
| MediaElement-Wiedergabe | `CommunityToolkit.Maui.MediaElement` 10.0.0, `UseMauiCommunityToolkitMediaElement(true)`, `PlayerPage` (Route `player`) | Erfüllt |
| PiP auf Windows | `PictureInPictureService` → `AppWindowPresenterKind.CompactOverlay` (Mini-Player/Overlay) | Erfüllt |
| PiP auf iOS | Native PiP der MediaElement-Transportsteuerung + `UIBackgroundModes`/`audio` in `Info.plist` | Erfüllt |
| Server-Streams + lokale Dateien | `PlaybackSourceKind` (`ServerStream`/`LocalFile`), `PlaybackSourceService` (Manifest-Download vor Stream `api/items/{typ}/{id}/stream?access_token=…`) | Erfüllt |
| Fortsetzungsposition beim Start | `api/continue-watching` + Cache-Fallback, `SeekTo` auf `PlayerPage` | Erfüllt |
| Fortschritt online sofort / offline verzögert verlustfrei | Ausschließlich über `ProgressReportService` → `pending_progress` (Drain bei Online-Rückkehr aus Schritt 6); 5-s-Takt mit Throttling | Erfüllt |
| „Angesehen"-Zeitfenster | `PlayerState`/Update: Ende abzüglich `WatchedEndWindowSecondsKey` | Erfüllt |
| Auto-Fortsetzung konfigurierbar | `AutoResumeEnabled` + Restlaufzeit-Fenster; greift nur bei laufender Wiedergabe (Review-Befund behoben, `IsPlaying`-Guard + Test) | Erfüllt |
| Nicht verfügbare Quelle → Fehlermeldung + nächste Episode | `AdvanceToNextEpisode` mit `PlaybackUnavailableError`, `NextEpisodeQueue` aus Serienkontext | Erfüllt |

## Testverifikation (eigenständig ausgeführt)

- `dotnet test VideoWebPlayer.Maui.Tests -c Release`
- Ergebnis: **510 bestanden, 0 fehlgeschlagen, 0 übersprungen** (Baseline vor Schritt: 469; +41 neue Tests: `AppUpdateTests_Player`, `PlaybackSourceServiceTests`, `PlayerContractTests`, `EnumCoverageTests`)
- Reviews: 2 Befunde gefunden und behoben (Auto-Fortsetzung im Pausenzustand; sqlite-net int/long-Vergleich) — dokumentiert in `docs/features/.../review-code.md`
- Build `VideoPlayer.Maui` (net10.0-windows): 0 Warnungen, 0 Fehler; Pre-Commit-Hooks eingehalten.

## Festgestellte Abweichungen

Keine. Hinweis: Echte PiP-Verifikation auf einem iOS-Gerät/Windows-Desktop ist auf dieser Maschine nicht automatisierbar — die Implementierung nutzt die dokumentierten Plattform-Mechanismen (CompactOverlay / AVPiP via MediaElement-Transportsteuerung); abgesichert über Contract-Tests.

## Ergebnis

**Anforderung vollständig erfüllt**
