# Plan-Review: Startseite — Hero-Kopfbereich (Schritt 5)

Prüfung der Implementierung gegen `plan.md` und die Tasks-Datei.
Datum: 2026-09-22 · Iteration 1

## Aufgaben-Abgleich

| Nr. | Aufgabe | Umgesetzt | Nachweis |
|-----|---------|-----------|----------|
| 1 | `HomeHeroEnabled` (Default `true`) | ✅ | `VideoPlayer.Storage/AppSettings.cs` (Z. 44–49) |
| 2 | Ablageschlüssel + Load/Save | ✅ | `AppSettingsService.cs` (Schlüssel `HomeHeroEnabled`, Load Z. 64, Save Z. 83) |
| 3 | `GetHeroBackgroundAsync` | ✅ | `VideoPlayerApiClient.cs` (Z. 420–435), `SendWithPolicyAsync` mit `allowReauth: true` |
| 4 | `HomeHeroState`-Record | ✅ | `VideoPlayer.Mvu/HomeHeroState.cs` (`Empty`, `IsLoading`, `ImageBytes`, `HasImage`) |
| 5 | `HomeHero`, `ShowHomeHero`, `HeroTitle` | ✅ | `AppModel.cs` (Z. 115–143) |
| 6 | `HeroBackgroundLoadedMessage` | ✅ | `AppMessage.cs` (Z. 165–174) |
| 7 | `LoadHeroBackgroundEffect` | ✅ | `AppEffect.cs` (Z. 118–124) |
| 8 | Update-Verdrahtung | ✅ | `AppUpdate.cs`: Switch-Case (Z. 79), `WithHeroLoad` (Z. 604–635), Aufrufe in `EnterConnected` (Z. 475) und `UpdateSaveSettingsRequested` (Z. 175), `UpdateHeroBackgroundLoaded` (Z. 637 ff.) — plus Sitzungs-/Server-Reset in `UpdateSessionExpired`, `UpdateLogoutRequested`, `UpdateServerCheckCompleted` und Stale-Guard über `IsLoading` |
| 9 | Effekt-Ausführung | ✅ | `ShellAppEffectHandler.cs`: Case (Z. 132–134), `LoadHeroBackgroundAsync` (Z. 378–404) über `authSession.Client`, Fehler → `HeroBackgroundLoadedMessage(null)` |
| 10 | Hero-`Grid` in `HomePage.xaml` | ✅ | Z. 11–109: Bildebene `AspectFill`, Seiten-/Radial-/Bottom-Gradienten, `VpBorder`-Trennlinie, Kicker/Titel/Beschreibung, `IsVisible` ← `Model.ShowHomeHero`, vor den Carousels |
| 11 | Hero-Aktualisierung + Höhe | ✅ | `HomePage.xaml.cs`: `UpdateHero` (Referenzvergleich, `ImageSource.FromStream`), `OnSizeChanged` → `Math.Clamp(Width * 0.42, 260, 520)` |
| 12 | Schalter „Kopfbereich anzeigen" | ✅ | `SettingsPage.xaml` (Abschnitt „Darstellung", `HomeHeroSwitch`), `SettingsPage.xaml.cs` (Handler, `PopulateFromModel`) |
| 13 | Tests | ✅ | `AppUpdateTests_HomeHero.cs` (12 Tests), `AppSettingsTests`, `AppSettingsServiceTests` (+2), `VideoPlayerApiClientTests_Endpoints` (+2), `SettingsStorageContractTests` (aktualisiert), `HomePageContractTests` (+5); angepasste Bestandstests: `AppUpdateTests_Connection` (2), `MvuHostTests` (1) |
| 14 | Dokumentation | ⏳ | folgt in Schritt 12 |

## Abweichungen vom Plan

- **Zusätzlich (nicht im Plan, fachlich erforderlich):** `HomeHero`-Reset bei
  Abmeldung, Sitzungsablauf und Serverwechsel sowie der `IsLoading`-Stale-Guard
  in `UpdateHeroBackgroundLoaded`. Begründung: das Bild wird benutzerbezogen
  generiert; ohne Reset bliebe ein sitzungs-/serverfremdes Bild sichtbar
  (Konsistenz mit `SourcesOverview`-/`SourceBrowser`-Resets). Durch Tests
  abgedeckt.
- **Titel-Entscheidung** wie geplant umgesetzt (aktiver Server,
  `NameOrAddress`, Fallback „VideoPlayer").

## Ergebnis

**Vollständig umgesetzt** — alle Code-Aufgaben erledigt, Tests grün
(697/697, inkl. neuer Hero-Tests). Dokumentation folgt in Schritt 12.
