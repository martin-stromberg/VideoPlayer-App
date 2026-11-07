# Test-Ergebnisse

## Ergebnis

**Status:** Fehler vorhanden

Alle automatisierbaren Tests sind bestanden (1257 Unit-/Contract-Tests,
Solution-Build Debug + Release je 0 Fehler). Als `Fehler vorhanden`
gilt der Lauf allein, weil die im Plan als Pflicht definierten manuellen
E2E-Prüfschritte nicht ausgeführt werden konnten: Drei davon erfordern
ein iPad mit TestFlight-Build (dokumentierte iOS-Verifikationsgrenze —
im Repository existiert keine iOS-UI-Automatisierung, siehe plan.md
Abschnitt „E2E-Tests" und plan-check.md Abschnitt „E2E-Abdeckung"),
der Windows-Prüfschritt ist ein manueller Schritt, für den die
automatisierte E2E-Suite in Iteration 1 als Ersatznachweis vollständig
grün lief.

Hinweis zu Iteration 2: Die Erweiterung des Bugfixes
(`OverlayChrome.Margin` unter iOS, Konstante
`IosNativeControlsClearance`, relativer Bar-Versatz) liegt vollständig
im `#if IOS`-Block bzw. in XAML-Kommentaren — der für Windows
kompilierte Codepfad ist funktional unverändert (lediglich eine
unbenutzte private Konstante kommt hinzu). Die Windows-E2E-Suite
(`VideoPlayer.E2E.Tests`) wurde daher nicht erneut ausgeführt; das
Iteration-1-Ergebnis (52/53 im Sammellauf, 1 Timeout-Flake isoliert
grün, also 53/53) bleibt als Ersatznachweis gültig.

## Fehlgeschlagene Tests

### VideoPlayer.E2E.Tests (manueller Prüfschritt — nicht ausgeführt)

- **Manuell iPad: Vollbild-Button der nativen Leiste antippen → Vollbild öffnet/zurück (AK 1)** — Nicht ausgeführt: erfordert iPad-Gerät + TestFlight-Build; iOS-UI-Automatisierung existiert im Repository nicht (im Plan als manueller Pflichtschritt, Schritt 5, definiert).
- **Manuell iPad: PiP-Button der nativen Leiste antippen → PiP startet (AK 1)** — Nicht ausgeführt: gleiche iOS-Verifikationsgrenze.
- **Manuell iPad: Playlist-Wiedergabe → „Zurück"/„Weiter" auslösbar; Titel, PlaylistContextLabel, WatchedBadge sichtbar (AK 2, 3)** — Nicht ausgeführt: gleiche iOS-Verifikationsgrenze.
- **Manuell Windows: Mini-Player-Schalter und Transportsteuerung unverändert bedienbar (AK 4)** — Nicht ausgeführt (manueller Schritt); Ersatznachweis: Windows-E2E-Suite in Iteration 1 vollständig bestanden (53/53 inkl. Wiederholung), Windows-Pfad in Iteration 2 unverändert.

## E2E-Abdeckung

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| iPad: Native Leiste einblenden → Vollbild-Button antippen → Vollbild öffnet/zurück (AK 1) | Manueller TestFlight-Prüfschritt (Plan, Schritt 5) | Nicht ausgeführt — iOS-Gerät/TestFlight erforderlich, nicht automatisierbar (Verifikationsgrenze laut Plan) |
| iPad: PiP-Button der nativen Leiste antippen → PiP startet (AK 1) | Manueller TestFlight-Prüfschritt (Plan, Schritt 5) | Nicht ausgeführt — gleiche Begründung |
| iPad: Playlist-Wiedergabe → „Zurück"/„Weiter" auslösbar; Overlay-Inhalte sichtbar (AK 2, 3) | Manueller TestFlight-Prüfschritt (Plan, Schritt 5); Contract-Pin `PlayerPage_PlaylistNavigationBarClearsNativeControlsOnIos` | Nicht ausgeführt (manuell); Contract-Pin bestanden |
| Windows: Mini-Player-Schalter und Transportsteuerung unverändert (AK 4) | Manueller Prüfschritt (Plan, Schritt 5); Windows-E2E-Suite als Ersatznachweis | Nicht ausgeführt (manuell); Ersatznachweis aus Iteration 1 bestanden — 53/53 E2E-Tests grün inkl. Wiederholung; Windows-Pfad in Iteration 2 unverändert |

## Zusammenfassung

- Gesamt: 1257 automatisierte Tests + 4 manuelle Pflicht-E2E-Szenarien
- Bestanden: 1257
- Fehlgeschlagen: 0 automatisiert; 4 manuelle E2E-Szenarien nicht ausgeführt
- Übersprungen: 0

Ausgeführte Suites:

- `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj -c Release --collect:"XPlat Code Coverage"` → 1257/1257 bestanden
- `dotnet build VideoPlayer.App.sln -c Debug` → 0 Fehler, 0 Warnungen (inkrementell)
- `dotnet build VideoPlayer.App.sln -c Release` → 0 Fehler, 19 Warnungen (Bestand: CS0618 DisplayAlert, CS0649, CA1416, XA4301 — keine neuen)

Nicht ausgeführte Suite:

- `dotnet test VideoPlayer.E2E.Tests` — bewusst nicht wiederholt: Iteration 2 ändert ausschließlich iOS-kompilierten Code (`#if IOS`-Block, `IosNativeControlsClearance`) und XAML-Kommentare; der Windows-Pfad ist unverändert. Iteration-1-Ergebnis: 52/53 im Sammellauf, `PlaylistPlaybackTests.NextAndPreviousButtons_NavigatePlaylist` als Timeout-Flake isoliert grün (53/53).

## Testabdeckung

**Abdeckung:** 87,3 % (Zeilen, XPlat Code Coverage / Cobertura, nur von `VideoWebPlayer.Maui.Tests` referenzierte Projekte)

| Datei | Abdeckung |
|-------|-----------|
| VideoWebPlayer.Client/Models/FavoriteEntry.cs | 0,0 % |
| VideoWebPlayer.Client/VideoWebPlayerClient.cs | 0,0 % |
| VideoWebPlayer.Client/Models/DtoRecentEntry.cs | 0,0 % |
| VideoWebPlayer.Client/Models/MediaEntryDto.cs | 0,0 % |
| VideoWebPlayer.Client/Models/AuthenticationRequest.cs | 0,0 % |
| VideoWebPlayer.Client/Models/DtoSource.cs | 0,0 % |
| VideoWebPlayer.Client/Models/ImpersonateRequest.cs | 0,0 % |
| VideoWebPlayer.Client/Models/AuthorizationToken.cs | 0,0 % |
| VideoPlayer.ApiClient/Discovery/MdnsServerDiscovery.cs | 0,0 % |
| VideoWebPlayer.Client/Models/DtoMovie.cs | 22,2 % |
| VideoPlayer.Mvu/DownloadListItem.cs | 60,0 % |
| VideoPlayer.Mvu/PlaylistDetailsState.cs | 63,6 % |
| VideoPlayer.ApiClient/ClientGate.cs | 63,6 % |
| VideoPlayer.Mvu/ServerEventsService.cs | 69,6 % |
| VideoPlayer.Mvu/MediaTypeNames.cs | 72,2 % |
| VideoPlayer.Mvu/SourceItem.cs | 75,0 % |
| VideoPlayer.ApiClient/Discovery/DiscoveredServer.cs | 75,0 % |

## Fehlende Tests

Quelle: `Coverage-Daten`

- `VideoWebPlayer.Client/Models/FavoriteEntry.cs` — 0 % Abdeckung
- `VideoWebPlayer.Client/VideoWebPlayerClient.cs` — 0 % Abdeckung
- `VideoWebPlayer.Client/Models/DtoRecentEntry.cs` — 0 % Abdeckung
- `VideoWebPlayer.Client/Models/MediaEntryDto.cs` — 0 % Abdeckung
- `VideoWebPlayer.Client/Models/AuthenticationRequest.cs` — 0 % Abdeckung
- `VideoWebPlayer.Client/Models/DtoSource.cs` — 0 % Abdeckung
- `VideoWebPlayer.Client/Models/ImpersonateRequest.cs` — 0 % Abdeckung
- `VideoWebPlayer.Client/Models/AuthorizationToken.cs` — 0 % Abdeckung
- `VideoPlayer.ApiClient/Discovery/MdnsServerDiscovery.cs` — 0 % Abdeckung

## Hinweise

- Die für diese Änderung geplanten Tests sind implementiert und grün:
  `PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface` (erweitert um die
  `InputTransparent`-Assertion am `OverlayChrome`-Elternlayout) und
  `PlayerPage_PlaylistNavigationBarClearsNativeControlsOnIos` (in
  Iteration 2 erweitert: pinnt jetzt zusätzlich den
  `#if IOS`-Versatz `IosNativeControlsClearance`,
  `OverlayChrome.Margin` und den relativen
  `PlaylistNavigationBar.Margin` im Konstruktor sowie das Fehlen eines
  XAML-Margins am Kopfband). Die Testklasse wurde zudem auf
  XAML-Hilfsmethoden (`PlayerPageXaml`, `PlayerPageCode`, `Named`)
  refaktoriert — alle 17 `PlayerContractTests` bestanden.
- Der ausstehende manuelle TestFlight-Prüfschritt (iPad) ist im Plan als
  Umsetzungsschritt 5 vorgesehen und nachgelagert auszuführen; die
  iOS-Verifikationsgrenze ist in plan.md/plan-check.md dokumentiert.
  Dort ist auch der geschätzte Wert `IosNativeControlsClearance = 96`
  zu bestätigen bzw. feinzujustieren.
