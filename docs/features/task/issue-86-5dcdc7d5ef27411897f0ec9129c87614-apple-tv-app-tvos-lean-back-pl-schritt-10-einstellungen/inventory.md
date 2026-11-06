# Bestandsaufnahme — Schritt 10: Schlanker Einstellungsbereich (tvOS)

## Vorhandene Bausteine (plattformneutral)

| Baustein | Ort | Stand |
|----------|-----|-------|
| `AppSettings` | `VideoPlayer.Storage/AppSettings.cs` | fertig — alle vier Pflicht-Felder vorhanden: `DownloadRetentionDays` (1–365, Standard 7), `DownloadQuotaGb` (1–500, Standard 5), `AutoResumeEnabled` (Standard an), `WatchedEndWindowSeconds` (30–3600, Standard 300); `Normalize()` klammert numerische Werte |
| `AppSettingsService` | `VideoPlayer.Storage/AppSettingsService.cs` | fertig — `Load()` (Defaults + Normalisierung) / `Save()` (normalisiert vor dem Schreiben), Profil-Präfix über `AppProfile` |
| `ISettingsStore` | `VideoPlayer.Storage/ISettingsStore.cs` | fertig — `Contains`, `GetString/GetInt/GetBool`, `Set`, `Remove` |
| Settings-Messages | `VideoPlayer.Mvu/AppMessage.cs` Z. 28–39 | `LoadSettingsRequestedMessage`, `SettingsLoadedMessage`, `SaveSettingsRequestedMessage` — vollständig |
| Settings-Effekte | `VideoPlayer.Mvu/AppEffect.cs` | `LoadSettingsEffect`, `PersistSettingsEffect` — vollständig |
| Update-Handler | `VideoPlayer.Mvu/AppUpdate.cs` Z. 185–187, 348–373 | `LoadSettingsRequestedMessage` → `LoadSettingsEffect`; `SettingsLoadedMessage` → `model.Settings` (normalisiert); `SaveSettingsRequestedMessage` → Normalisierung + `PersistSettingsEffect` (+ `RunAutoDownloadsEffect` nur bei AutoDownload-Flanke — auf tvOS unverdrahtetes No-Op; Hero-Nachladeeffekt ebenfalls No-Op-fähig) |
| `AppModel.Settings` | `VideoPlayer.Mvu/AppModel.cs` Z. 61 | beim Start über `LoadSettingsEffect` befüllt (Starteffekte Z. 345) |
| Verbraucher der Werte | `VideoPlayer.Mvu/AppUpdate.cs` | `WatchedEndWindowSeconds` (Endfenster Z. 3913), `AutoResumeEnabled` (Auto-Fortsetzung Z. 4024/4036), `DownloadRetentionDays`/`DownloadQuotaGb` (Download-Effekte tragen `model.Settings`, Z. 4216–4393) — Änderungen wirken ohne weiteren Code |
| `LogoutRequestedMessage` / `LogoutEffect` | `AppMessage.cs` / `AppUpdate.cs` Z. 3555–3600 | bestehender Abmeldefluss (lokales Sitzungsende, Guard: nur `Connected`/`Offline`) |

## tvOS-Bestand (`VideoPlayer.TvOS`)

- `Services/NSUserDefaultsSettingsStore.cs` — `ISettingsStore`-Adapter auf `NSUserDefaults.StandardUserDefaults`; in `AppComposition` Z. 25/30 verdrahtet (`Settings = new AppSettingsService(SettingsStore, Profile)`)
- `Services/TvosAppEffectHandler.cs` — `LoadSettingsEffect` → `LoadSettingsAsync` (Z. 103–105, 344–359); `PersistSettingsEffect` → `SaveSettings` (Z. 106–108, 361–372) — **beide bereits verdrahtet**
- `Ui/SettingsPageViewController.cs` — bisher nur Abschnitt „Server" (`serverLabel`, `statusLabel`, Button „Server wechseln" → `NavigateToMessage(AppPage.ServerSetup)`); `IModelRenderable.Render(model)` befüllt die Labels
- `Ui/ProfilePageViewController.cs` — „Abmelden" über `LogoutRequestedMessage` (Enabled-Guard `Connected or Offline`), Server-/Statusanzeige — Muster für Konto-Elemente
- `Ui/TvosControls.cs` — `Title`, `Heading`, `Body`, `Status`, `Error`, `TextField`, `Button(title, action)` (fokussierbar, `PrimaryActionTriggered`), `Section` (vertikaler `UIStackView`)
- `Ui/DownloadsPageViewController.cs` — Muster für `UIAlertController`-Aktionsmenüs (ActionSheet, `UIAlertAction.Create`, `PresentViewController`), `lastModel`/`IsViewLoaded`-Render-Guard, `ViewDidAppear`-Dispatch
- `Ui/MenuTabBarController.cs` Z. 103 — `AppPage.Settings` → `SettingsPageViewController` (bereits verdrahtet)
- `AppModel`-Anzeigedaten: `ActiveServerAddress`, `ServerHealthText`, `IsOffline`, `LoginUsername` (bekannter Benutzername), `ConnectionState`

## MAUI-Vorlage (`VideoPlayer.Maui/Pages/SettingsPage.xaml`)

Referenz für Beschriftungen/Gruppierung: Abschnitte Darstellung / Downloads / Wiedergabe / Protokoll / Server; Labels „Speicherplatzkontingent für Downloads (GB)", „Aufbewahrungszeit für Downloads (Tage)", „Auto-Fortsetzung", „Als angesehen ab … Sekunden vor Ende". Der tvOS-Bereich übernimmt nur die vier Pflicht-Werte — Ein/Aus-Schalter und Zahleneingaben werden fernbedienungstauglich als Wert-Auswahl umgesetzt (keine Systemtastatur für Zahlwerte nötig).

## Lücken / zu schaffen

1. `SettingsPageViewController` erweitern:
   - Abschnitt „Downloads": `DownloadRetentionDays` (Tage) + `DownloadQuotaGb` (GB) — je fokussierbarer Wert-Button, Auswahl über `UIAlertController`-Werteliste (Vorgabewerte innerhalb der Norm-Schranken; aktueller Wert wird bei Abweichung in die Liste aufgenommen)
   - Abschnitt „Wiedergabe": `AutoResumeEnabled` (Ein/Aus-Umschalter als Wert-Button) + `WatchedEndWindowSeconds` (Sekunden-Minuten-Werteliste)
   - Abschnitt „Konto" (erlaubt): angemeldeter Benutzer (`LoginUsername`) + „Abmelden" über `LogoutRequestedMessage` (Enabled-Guard wie Profilseite)
   - Abschnitt „Server" bleibt unverändert
   - Jede Änderung → `SaveSettingsRequestedMessage(settings with { … })`; `Render` setzt Button-Titel aus `model.Settings`
2. Tests: keine neuen plattformneutralen Messages/Effekte nötig — `dotnet test VideoPlayer.Maui.Tests` muss grün bleiben (Bestandstests `AppSettingsServiceTests`/`AppSettingsTests`/`AppUpdateTests_Settings` decken Normalisierung/Persistenz ab); kein Eingriff in `VideoPlayer.Mvu`/`.Storage` erforderlich
3. Dokumentation: `docs/help/tv-app/beschreibung.md` + `ablauf-anwender.md` (Einstellungsbereich), `README.md` (tvOS-Zeile), `docs/help/index.md` ggf.

## Rahmen

- tvOS lokal nicht kompilierbar → CI `build-tvos` (Push auf `task/issue-86-**` autorisiert)
- Bekannte tvOS-Fallen: Basistyp-Member, Named-Args-Case, `IReadOnlyList.IndexOf`, `const nfloat`, `RegisterClassForCell`, `SeparatorStyle` (existiert nicht auf tvOS), `UIAction`-Parameter nicht `_` nennen
