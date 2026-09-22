# Umsetzungsaufgaben: Startseite — Hero-Kopfbereich (Schritt 5)

Plan: `issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-5-hero-kopfbereich/plan.md`

| Status | Nr. | Aufgabe | Datei(en) |
|--------|-----|---------|-----------|
| [ ] | 1 | `HomeHeroEnabled` (bool, Default `true`) in `AppSettings` | `VideoPlayer.Storage/AppSettings.cs` |
| [ ] | 2 | Ablageschlüssel + `Load()`/`Save()` für `HomeHeroEnabled` | `VideoPlayer.Storage/AppSettingsService.cs` |
| [ ] | 3 | `GetHeroBackgroundAsync` (`GET api/pictures/hero-background`) | `VideoPlayer.ApiClient/VideoPlayerApiClient.cs` |
| [ ] | 4 | `HomeHeroState`-Record (`IsLoading`, `ImageBytes`, `Empty`) | `VideoPlayer.Mvu/HomeHeroState.cs` (neu) |
| [ ] | 5 | `HomeHero`, `ShowHomeHero`, `HeroTitle` im Model | `VideoPlayer.Mvu/AppModel.cs` |
| [ ] | 6 | `HeroBackgroundLoadedMessage(byte[]?)` | `VideoPlayer.Mvu/AppMessage.cs` |
| [ ] | 7 | `LoadHeroBackgroundEffect` | `VideoPlayer.Mvu/AppEffect.cs` |
| [ ] | 8 | Update-Verdrahtung: `EnterConnected`, `UpdateSaveSettingsRequested`, `HeroBackgroundLoadedMessage` (+ `WithHeroLoad`-Hilfsfunktion) | `VideoPlayer.Mvu/AppUpdate.cs` |
| [ ] | 9 | Effekt-Ausführung `LoadHeroBackgroundEffect` (Client → Bytes → Message; Fehler → `null`) | `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` |
| [ ] | 10 | Hero-`Grid` in `HomePage.xaml` (Bild, Gradienten, Trennlinie, Kicker/Titel/Beschreibung, `IsVisible` ← `Model.ShowHomeHero`) | `VideoPlayer.Maui/Pages/HomePage.xaml` |
| [ ] | 11 | Hero-Aktualisierung + responsive Höhe (`clamp`-Äquivalent) | `VideoPlayer.Maui/Pages/HomePage.xaml.cs` |
| [ ] | 12 | Schalter „Kopfbereich anzeigen" (Abschnitt „Darstellung") | `VideoPlayer.Maui/Pages/SettingsPage.xaml(.cs)` |
| [ ] | 13 | Tests: Settings-Default/-Persistenz, Update-Zyklen, API-Endpunkt, Vertragstests XAML | `VideoWebPlayer.Maui.Tests/` |
| [ ] | 14 | Dokumentation: `docs/help/` (Startseite + Einstellungen), `changes.log`, `README.md`, `docs/RELEASE_NOTES.md` | diverse |

## Akzeptanzbezug

- K1 (Hero über den Listen, servergeneriertes Bild): Nr. 3, 4, 7–11
- K2 (Web-Darstellung: Cover, Höhe, Gradienten, Kicker/Titel/Text, Trennlinie): Nr. 10, 11
- K3 (Bearer-Auth): Nr. 3, 9
- K4 (flüchtiger Cache): Nr. 4, 5 (Bytes im Model, keine Datei)
- K5 (Offline/Fehler stabil): Nr. 8, 9, 13
- K6 (positive Einstellung, Standard an, persistiert): Nr. 1, 2, 12, 13
- K7 (DoD Build Debug+Release, volle Testsuite): Nr. 13 + Testphase
