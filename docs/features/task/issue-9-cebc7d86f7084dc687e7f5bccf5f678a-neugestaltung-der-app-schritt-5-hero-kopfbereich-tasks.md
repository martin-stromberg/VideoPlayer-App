# Umsetzungsaufgaben: Startseite — Hero-Kopfbereich (Schritt 5)

Plan: `issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-5-hero-kopfbereich/plan.md`

| Status | Nr. | Aufgabe | Datei(en) |
|--------|-----|---------|-----------|
| [x] | 1 | `HomeHeroEnabled` (bool, Default `true`) in `AppSettings` | `VideoPlayer.Storage/AppSettings.cs` |
| [x] | 2 | Ablageschlüssel + `Load()`/`Save()` für `HomeHeroEnabled` | `VideoPlayer.Storage/AppSettingsService.cs` |
| [x] | 3 | `GetHeroBackgroundAsync` (`GET api/pictures/hero-background`) | `VideoPlayer.ApiClient/VideoPlayerApiClient.cs` |
| [x] | 4 | `HomeHeroState`-Record (`IsLoading`, `ImageBytes`, `Empty`) | `VideoPlayer.Mvu/HomeHeroState.cs` (neu) |
| [x] | 5 | `HomeHero`, `ShowHomeHero`, `HeroTitle` im Model | `VideoPlayer.Mvu/AppModel.cs` |
| [x] | 6 | `HeroBackgroundLoadedMessage(byte[]?)` | `VideoPlayer.Mvu/AppMessage.cs` |
| [x] | 7 | `LoadHeroBackgroundEffect` | `VideoPlayer.Mvu/AppEffect.cs` |
| [x] | 8 | Update-Verdrahtung: `EnterConnected`, `UpdateSaveSettingsRequested`, `HeroBackgroundLoadedMessage` (+ `WithHeroLoad`-Hilfsfunktion; Resets bei Abmeldung/Sitzungsablauf/Serverwechsel) | `VideoPlayer.Mvu/AppUpdate.cs` |
| [x] | 9 | Effekt-Ausführung `LoadHeroBackgroundEffect` (Client → Bytes → Message; Fehler → `null`) | `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` |
| [x] | 10 | Hero-`Grid` in `HomePage.xaml` (Bild, Gradienten, Trennlinie, Kicker/Titel/Beschreibung, `IsVisible` ← `Model.ShowHomeHero`) | `VideoPlayer.Maui/Pages/HomePage.xaml` |
| [x] | 11 | Hero-Aktualisierung + responsive Höhe (`clamp`-Äquivalent) | `VideoPlayer.Maui/Pages/HomePage.xaml.cs` |
| [x] | 12 | Schalter „Kopfbereich anzeigen" (Abschnitt „Darstellung") | `VideoPlayer.Maui/Pages/SettingsPage.xaml(.cs)` |
| [x] | 13 | Tests: Settings-Default/-Persistenz, Update-Zyklen, API-Endpunkt, Vertragstests XAML | `VideoWebPlayer.Maui.Tests/` |
| [x] | 14 | Dokumentation: `docs/help/` (Startseite + Einstellungen + Index), `changes.log`, `README.md`, `docs/RELEASE_NOTES.md` | diverse |

## Akzeptanzbezug

- K1 (Hero über den Listen, servergeneriertes Bild): Nr. 3, 4, 7–11
- K2 (Web-Darstellung: Cover, Höhe, Gradienten, Kicker/Titel/Text, Trennlinie): Nr. 10, 11
- K3 (Bearer-Auth): Nr. 3, 9
- K4 (flüchtiger Cache): Nr. 4, 5 (Bytes im Model, keine Datei)
- K5 (Offline/Fehler stabil): Nr. 8, 9, 13
- K6 (positive Einstellung, Standard an, persistiert): Nr. 1, 2, 12, 13
- K7 (DoD Build Debug+Release, volle Testsuite): Nr. 13 + Testphase (697/697)
