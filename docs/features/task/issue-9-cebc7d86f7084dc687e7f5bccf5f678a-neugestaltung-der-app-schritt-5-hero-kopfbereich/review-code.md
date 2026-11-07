# Code-Review: Startseite — Hero-Kopfbereich (Schritt 5)

Qualitätsprüfung der geänderten Dateien (Fehler, Smells, Konventionen) —
nicht die Plan-Abdeckung. Datum: 2026-09-22 · Iteration 1

## Geprüfte Änderungen

- `VideoPlayer.Storage/AppSettings.cs`, `AppSettingsService.cs`
- `VideoPlayer.ApiClient/VideoPlayerApiClient.cs`
- `VideoPlayer.Mvu/HomeHeroState.cs`, `AppModel.cs`, `AppMessage.cs`,
  `AppEffect.cs`, `AppUpdate.cs`
- `VideoPlayer.Maui/Pages/HomePage.xaml(.cs)`, `SettingsPage.xaml(.cs)`,
  `Services/ShellAppEffectHandler.cs`, `Resources/Styles/Colors.xaml`
- Tests: `AppUpdateTests_HomeHero.cs` (neu), `AppSettingsTests`,
  `AppSettingsServiceTests`, `VideoPlayerApiClientTests_Endpoints`,
  `SettingsStorageContractTests`, `HomePageContractTests`,
  `AppUpdateTests_Connection`, `MvuHostTests`

## Feststellungen

1. **Zustandskonsistenz:** `HomeHero` wird bei Abmeldung, Sitzungsablauf
   und Serverwechsel zurückgesetzt — konsistent zu `SourcesOverview`/
   `SourceBrowser`. Der `IsLoading`-Guard in `UpdateHeroBackgroundLoaded`
   verhindert sowohl verspätete Fremdbilder als auch ein „klemmendes"
   `IsLoading` nach dem Offline-Übergang (Ergebnis darf dort noch
   ankommen). ✅
2. **Kein Lade-Sturm:** `WithHeroLoad` prüft `Connected`,
   `HomeHeroEnabled`, `!IsLoading`, `!HasImage` — der Effekt kann nicht
   doppelt laufen; Retry erfolgt nur beim nächsten Verbindungsaufbau
   bzw. bei der nächsten Einstellungs-Speicherung. ✅
3. **Threading:** Bytes → `ImageSource` läuft auf dem MainThread
   (`UpdateHero` im `ModelChanged`-Rückruf via
   `MainThread.BeginInvokeOnMainThread` bzw. Konstruktor). Der
   Referenzvergleich `appliedHeroBytes` vermeidet wiederholte
   Stream-Erzeugung bei jedem Model-Update. ✅
4. **Fehlerbehandlung:** `LoadHeroBackgroundAsync` fängt alle
   Nicht-Abbruch-Fehler, loggt via `Debug.WriteLine` (Konvention) und
   meldet `null` — der Update-Pfad bleibt ohne Fehlerzustand stabil.
   `OperationCanceledException` bei Anforderungsabbruch wird korrekt
   weitergereicht. ✅
5. **Tokens:** Die Gradientenfarben sind transluzente Ableitungen der
   Web-Tokens, zentral in `Colors.xaml` benannt (`VpHero*`) — kein Hex
   in der Seite; `VpBorder`-Trennlinie wie im Web. ✅
6. **XML-Kommentare:** Alle neuen Member deutsch dokumentiert (CS1591
   ist als Fehler konfiguriert — Build fehlerfrei). ✅
7. **Keine Stubs/`NotImplementedException`, keine neuen Enums** —
   Hooks unberührt. ✅
8. **Persistenz:** neuer Schlüssel rein additiv; fehlende Werte fallen
   auf `true` zurück (positiver Standard); `Normalize()` muss den
   Bool nicht anfassen. ✅
9. **API-Client:** `GetHeroBackgroundAsync` nutzt exakt die gemeinsame
   Pipeline (Bearer, Retry 1/2/4 s, einmalige 401-Reauth). ✅

## Kleinigkeiten (keine Befunde)

- `UpdateSaveSettingsRequested` stößt den Hero-Load auch bei anderen
  Einstellungsänderungen an, wenn noch kein Bild vorliegt — bewusst als
  Retry-Kanal gewertet und im Kommentar dokumentiert.
- `HeightRequest="320"` im XAML ist nur der Wert bis zum ersten
  `SizeChanged` — danach greift die `clamp`-Formel. Unschädlich.

## Ergebnis

**Keine Befunde.** Die Implementierung folgt den bestehenden
Konventionen (MVU-Zyklus, Fehlermuster, Token-Nutzung, Doku-Pflichten).
