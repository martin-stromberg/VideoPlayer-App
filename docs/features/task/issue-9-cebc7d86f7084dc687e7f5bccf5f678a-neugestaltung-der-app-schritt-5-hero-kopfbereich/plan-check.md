# Plan-Check: Startseite — Hero-Kopfbereich (Schritt 5)

Prüfung des Plans (`plan.md` + Tasks-Datei) gegen die Anforderung
(`requirement.md`) und den Testbedarf. Ergebnis: **Plan vollständig** —
keine Lücken gefunden.

## Abdeckung der Akzeptanzkriterien

| Kriterium | Plan-Abdeckung | Test-Nachweis |
|-----------|----------------|---------------|
| K1 Hero oberhalb der Listen mit Serverbild `api/pictures/hero-background` | Aufgaben 3, 4, 7–11 (Endpunkt, Model-State, Effekt, Handler, XAML-Grid an erster Position) | `AppUpdateTests`: Effekt bei `EnterConnected`; API-Client-Test: Pfad/Bytes; `HomePageContractTests`: Markup vor den Carousels |
| K2 Web-Darstellung (Cover, `clamp(260,42vw,520)`, Gradienten, Kicker/Titel/Beschreibung, `VpBorder`-Trennlinie) | Aufgaben 10–11 (`AspectFill`, `Math.Clamp(Width*0.42,260,520)`, 4 Overlay-Ebenen, Tokenfarben) | `HomePageContractTests`: `AspectFill`, Gradient-Brushes, Kicker-Text, Beschreibung; Höhenformel per Quell-Vertrag |
| K3 Bearer-Authentifizierung | Aufgabe 3 nutzt `SendWithPolicyAsync(allowReauth: true)`; Aufgabe 9 nutzt `authSession.Client` | `StubHttpMessageHandler` zeichnet `Authorization` auf → Assertion im Endpoint-Test |
| K4 Flüchtiger Cache | `byte[]` im `AppModel` (verliert sich mit der App-Session), keine Dateiablage | implizit durch Model-Tests; kein `File`-Zugriff im Hero-Pfad (Vertragstest auf fehlende Ablage nicht nötig — kein Code dafür) |
| K5 Offline-/Fehler-Stabilität | Aufgabe 9: Fehler → `HeroBackgroundLoadedMessage(null)`; Aufgabe 8: ohne Verbindung kein Effekt; Hero ohne Bild = Gradienten | `AppUpdateTests`: `null`-Ergebnis → `IsLoading=false`, `ShowHomeHero` unverändert; kein Effekt im Offline-Zustand |
| K6 Positive Einstellung, Standard an, wirksam, persistiert | Aufgaben 1, 2, 12 (`HomeHeroEnabled = true`, positiver Label-Text, `Preferences`-Pfad) | `AppSettingsTests`: Default `true`; `AppSettingsServiceTests`: fehlender Schlüssel → `true`, Roundtrip `false`; `AppUpdateTests`: `ShowHomeHero` folgt Toggle; `SettingsStorageContractTests`: Switch + Handler |
| K7 DoD: Build Debug+Release, volle Testsuite | Testphase: beide Konfigurationen + `dotnet test` | `test-results.md` |

## Testbedarf-Prüfung

- **Unit/Update-Tests** decken: Default, Persistenz-Roundtrip,
  Toggle-Wirkung im Model, Effekt-Auslösung (connected/enabled/kein Bild),
  Sperre gegen Doppel-Laden, Fehlschlag-Stabilität. ✅
- **API-Client-Test** deckt: Endpunktpfad, Byte-Rückgabe,
  Authorization-Header (Bearer). ✅
- **Vertragstests** (etabliertes Muster, da MAUI-Views nicht
  instanziierbar): Hero-Markup (Gradienten, Kicker, Titel, `AspectFill`,
  Sichtbarkeitsbindung), Settings-Schalter (AutomationId, Handler,
  `HomeHeroEnabled`-Verdrahtung), Effekt-Case im Handler. ✅
- **E2E:** keine MAUI-E2E-/UI-Testinfrastruktur im Repository vorhanden —
  die sichtbare Schalter→Hero-Wirkung kann nicht automatisiert gegen eine
  laufende App geprüft werden. Kompensation: die gesamte
  Entscheidungslogik (`ShowHomeHero`, Effekt-Bedingungen, Persistenz)
  liegt im unit-testbaren Kern; UI-Bindungen werden per Quell-Vertragstest
  gesichert. Einschränkung wird in `test-results.md` dokumentiert. ⚠️ (akzeptiert)

## Konsistenz

- Keine neuen Hexfarben: Gradienten leiten sich aus `VpBg`/`VpPrimary`/
  `VpSecondary`-Werten ab (transluzente Varianten) — konsistent mit
  `Colors.xaml`-Konvention (transluzente Token-Ableitungen wie `VpBorder`
  existieren bereits). ✅
- `byte[]` im plattformneutralen MVU-Kern ist erlaubt; `ImageSource`
  bleibt in `VideoPlayer.Maui`. ✅
- `Normalize()` muss `HomeHeroEnabled` nicht anfassen (bool, kein Bereich). ✅
- Enum-Coverage-Hook: kein neuer Enum. ✅ · `NotImplementedException`:
  keine. ✅ · CS1591: alle neuen Member deutsch dokumentiert. ✅

## Ergebnis

Plan ist vollständig und testbar → Implementierung (Schritt 6) freigegeben.
