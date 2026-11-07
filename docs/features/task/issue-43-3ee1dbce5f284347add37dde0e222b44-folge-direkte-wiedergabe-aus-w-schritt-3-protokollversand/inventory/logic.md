# Bestandsaufnahme: Logik, MVU und UI-Verdrahtung

## `MauiProgram`
Datei: `VideoPlayer.Maui/MauiProgram.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `CreateMauiApp()` | public static | Baut `MauiApp`; DI-Registrierungen (Singletons: `AppSettingsService`, `AppStorageLayout` via `AppStorageLayoutProvider.CreateLayout`, `ShellAppEffectHandler`, `MvuHost` u. a.); `#if DEBUG` → `builder.Logging.AddDebug()` (Zeile ~151) |

Relevant: `builder.Logging` (ILoggingBuilder) ist vorhanden; `AddProvider`/`ILoggerProvider`-Registrierung ist dort der Erweiterungspunkt. `Microsoft.Extensions.Logging.Debug` 10.0.10 ist referenziert.

## `ShellAppEffectHandler`
Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` (1558 Zeilen)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `HandleAsync(AppEffect, CancellationToken)` | public | Großes Switch über alle `AppEffect`-Records |
| `LoadSettingsAsync()` | private | `AppSettingsService.Load()` → `SettingsLoadedMessage` (bei Fehlschlag Defaults) |
| `SaveSettings(AppSettings)` | private | `AppSettingsService.Save()` mit `Debug.WriteLine` bei Fehlern |
| `DispatchMessageAsync(AppMessage)` | private | MainThread-Dispatch in den `MvuHost` |

- 51 `System.Diagnostics.Debug.WriteLine`-Aufrufe (alle Fehler-/Diagnosepfade) — Kandidaten für Weiterleitung ins Protokoll.
- Dienste werden über `IServiceProvider services` aufgelöst (`GetRequiredService<T>()`).

## `SettingsPage`
Dateien: `VideoPlayer.Maui/Pages/SettingsPage.xaml` / `.xaml.cs`

| Methode/Element | Kurzbeschreibung |
|-----------------|------------------|
| `PopulateFromModel()` | Befüllt Schalter/Einträge aus `mvuHost.Model.Settings` (`suppressEvents`-Guard) |
| `Save(Func<AppSettings, AppSettings>)` | `SaveSettingsRequestedMessage`-Dispatch |
| `OnXxxToggled` / `SaveNumeric` | Ereignis-Handler der Schalter/Zahlenfelder |
| `OnServerSwitchClicked` | Dispatch `NavigateToMessage(AppPage.ServerSetup)` — Muster für den Protokoll-Button |
| `InvalidInputLabel` | Vorhandenes Inline-Fehler-Label (`ErrorText`-Stil) — Muster für den Versand-Hinweis |

XAML-Konvention: Abschnitte als `VerticalStackLayout` mit `Kicker`-Label + `Card`-`Border`; Schalter in `Grid ColumnDefinitions="*,Auto"`; `AutomationId` auf allen interaktiven Elementen.

## `AppUpdate`
Datei: `VideoPlayer.Mvu/AppUpdate.cs`

- `Update(AppModel, AppMessage)` — statisches Switch, je Nachricht eine private Übergangsfunktion.
- `UpdateSaveSettingsRequested` — persistiert via `PersistSettingsEffect`, löst bei Bedarf Folgeeffekte aus (Vorbild: `RunAutoDownloadsEffect` beim Einschalten von `AutoDownloadEnabled`).
- Fehlertexte als `public const string`-Konstanten im Kopf der Klasse (z. B. `PlaybackUnavailableError`).

## `DevApiModelSnapshot` / `DevApiModel` (E2E)
Dateien: `VideoPlayer.Mvu/DevApi/DevApiModelSnapshot.cs` (unter `#if DEBUG || DEV_API`), `VideoPlayer.E2E.Tests/Support/DevApiModels.cs`

- `FromModel(AppModel)` bildet Modellfelder auf camelCase-JSON-Felder ab; die E2E-Seite spiegelt sie in `DevApiModel` (`[JsonPropertyName]`).
- Erweiterung für `LoggingEnabled`/`LogSendError` folgt diesem Muster.

## Plattform-APIs (Microsoft.Maui.Essentials, in `VideoPlayer.Maui` verfügbar)

- `Email.ComposeAsync(EmailMessage)` + `EmailAttachment(path)` — wirft `FeatureNotSupportedException` ohne Mail-Client; auf Windows → `mailto:`-Launcher.
- `Share.RequestAsync(ShareFileRequest)` — Share-Fallback; beides auf dem Hauptthread auszuführen (`MainThread.InvokeOnMainThreadAsync`).

## E2E-Infrastruktur
Dateien: `VideoPlayer.E2E.Tests/` (`AppFixture` baut Windows-Debug-App einmalig; `AppInstance` isoliert Profil via `VIDEOPLAYER_PROFILE`; `UiDriver` FlaUI/UIA3 mit `FindByAutomationId`, `ClickButton`, `Exists`, `OpenFlyoutItem`; `DevApiClient.WaitForModelAsync`; `MockVideoServer`).

- Einstellungsseite erreichbar über `OpenFlyoutItem("Einstellungen")` (Vorbild `ServerSwitchTests`).
- Profilaufräumung: `AppInstance.DeleteProfileDirectory` löscht `%APPDATA%\VideoPlayer\{profil}` und `%LOCALAPPDATA%\VideoPlayer\{profil}` — das tatsächliche NoBackup-Verzeichnis (`%LOCALAPPDATA%\User Name\de.martinstromberg.videos\Data\nobackup\{profil}`) wird bisher nicht gelöscht (bekannte Leiche, bei Logs-Verzeichnis zu berücksichtigen).
