# Testergebnisse: Startseite — Hero-Kopfbereich (Schritt 5)

Datum: 2026-09-22 (lokale Zeit, UTC+2) · Iteration 1
Branch: `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-5-hero-kopfbereich`
SDK: .NET SDK 10.0.401

## Builds

| Kommando | Ergebnis |
|----------|----------|
| `dotnet build VideoPlayer.App.sln -c Debug` | **0 Fehler** (4 Alt-Warnungen, unverändert) |
| `dotnet build VideoPlayer.App.sln -c Release` | **0 Fehler** (339 Alt-Warnungen, Legacy-Projekte unverändert) |

Hinweis: die Lösung heißt `VideoPlayer.App.sln` (nicht `VideoPlayer.sln`).

## Testsuite

Kommando: `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj --logger "console;verbosity=normal"`

- Ergebnis: **697/697 bestanden**, 0 fehlgeschlagen, 0 übersprungen, ~6–20 s
- Baseline vor der Änderung: 672/672 → **+25 Tests** (12 neue Update-Tests,
  2 Settings-Service-Tests, 2 API-Client-Tests, 5 Contract-Tests,
  4 angepasste Bestands-Assertions/-Tests: Switch-Anzahl, Effekt-Zahlen im
  Verbindungsaufbau)
- Hinweis: das DoD-Kommando `VideoWebPlayer.Tests` existiert in dieser
  Lösung nicht — verifiziert ist `VideoWebPlayer.Maui.Tests`.

## Abdeckung

Kommando: `dotnet test … --no-build --collect:"XPlat Code Coverage"`
(lief erfolgreich, Cobertura-Report erzeugt).

- Gesamt (Testprojekt-Sicht): ~82,9 % Zeilen
- `HomeHeroState`: 100 % · `AppSettings`/`AppSettingsService`: 100 % ·
  `AppUpdate`: ~96,5 % · `AppModel`: ~81 % · `VideoPlayerApiClient`: ~89 %

## Neue/geänderte Tests im Detail

- `AppUpdateTests_HomeHero.cs` (neu, 12 Tests): Ladeanstoß bei
  `LoginCompleted` (aktiviert/deaktiviert/bereits geladen), Übernahme der
  Bilddaten, Fehlschlag-Stabilität (`null` → kein Bild, `ShowHomeHero`
  bleibt, kein Crash), Fehlschlag erhält vorhandenes Bild, verspätete
  Antwort ohne Ladevorgang wird ignoriert, `LogoutRequested` setzt den
  Hero zurück, `SaveSettingsRequested` (Einschalten connected → Effekt;
  Ausschalten → verborgen ohne Effekt; Einschalten offline → kein Effekt;
  Einschalten mit Bild → kein erneuter Abruf), `HeroTitle` (Servername /
  Adresse / App-Fallback).
- `AppSettingsTests`: Default `HomeHeroEnabled = true`.
- `AppSettingsServiceTests`: fehlender Schlüssel → `true`; Roundtrip
  `false` persistiert (Schlüssel liegt in der Ablage).
- `VideoPlayerApiClientTests_Endpoints`: Endpunktpfad
  `/api/pictures/hero-background` + Byte-Rückgabe; Bearer-Header nach
  Login.
- `SettingsStorageContractTests`: `HomeHeroSwitch`-AutomationId,
  Switch-Anzahl 4 → 5.
- `HomePageContractTests`: Hero vor den Carousels, Kicker/Titel/
  Sichtbarkeitsbindung, `AspectFill`, Gradienten-Brushes, `VpBorder`,
  keine Dateiablage; Code-Behind (`ImageSource.FromStream`,
  `Math.Clamp(Width*0.42, 260, 520)`); Handler-Case; API-Client-Methode;
  Settings-Schalter (positive Formulierung); `VpHero*`-Tokens.
- Angepasste Bestandstests: `AppUpdateTests_Connection` (Effektliste des
  Verbindungsaufbaus 5 → 6), `MvuHostTests` (12 → 13).

## E2E-Abdeckung

**Nicht ausgeführt — keine Infrastruktur vorhanden.** Das Repository
enthält kein MAUI-UI-/E2E-Testprojekt (kein Appium/Playwright-Maui-
Gerätetest). Die sichtbare Wirkung „Schalter → Hero ein/aus" wurde
daher abgesichert über: (a) Model-Ebene (`ShowHomeHero`, unit-getestet),
(b) Bindungs-Vertrag (`IsVisible="{Binding Model.ShowHomeHero}"` per
Quell-Vertragstest), (c) Persistenz-Roundtrip über den echten
`AppSettingsService`. Diese Einschränkung ist projektbekannt und
entspricht der bisherigen Testkonvention der Schritte 1–4.

Manuell zu verifizierende Restpunkte (nicht automatisierbar):
optische Wirkung der Gradienten-Höhe auf sehr schmalen/breiten Fenstern.
