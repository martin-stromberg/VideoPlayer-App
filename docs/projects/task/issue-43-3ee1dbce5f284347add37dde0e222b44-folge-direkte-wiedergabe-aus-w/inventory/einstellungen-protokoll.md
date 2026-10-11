# Detailbefund — Anforderung 2: Einstellungen und Protokollierung

## Einstellungen (Ist-Zustand)

- **Seite:** `VideoPlayer.Maui/Pages/SettingsPage.xaml` — Abschnitte
  „Darstellung" (Kopfbereich anzeigen), „Downloads" (Automatische Downloads, Anzahl
  vorzuladender Episoden, WLAN-only, Kontingent, Gesehene löschen), „Wiedergabe"
  (Auto-Fortsetzung, Zeitfenster, Angesehen-Fenster), „Server" (Button „Server wechseln" →
  `NavigateToMessage(AppPage.ServerSetup)`).
- **Code-Behind** `SettingsPage.xaml.cs`: liest `mvuHost.Model.Settings`, schreibt über
  `SaveSettingsRequestedMessage` → `PersistSettingsEffect` → `AppSettingsService.Save`
  (MVU-Strecke, `ShellAppEffectHandler` Z. 103–105).
- **Modell:** `VideoPlayer.Storage/AppSettings.cs` — `sealed record` mit 8 Werten:
  `AutoDownloadEnabled`, `PreloadEpisodeCount` (1–10), `AutoResumeEnabled`,
  `AutoResumeRemainingSeconds` (30–3600), `WifiOnlyDownloads`, `WatchedEndWindowSeconds`
  (30–3600), `DownloadQuotaGb` (1–500, Standard 5), `AutoDeleteWatchedEpisodes`,
  `HomeHeroEnabled` (Standard true). `Normalize()` klemmt numerische Werte.
- **Persistenz:** `AppSettingsService` (`VideoPlayer.Storage/AppSettingsService.cs`) —
  Schlüssel-Konstanten pro Einstellung, Profil-Präfix über `AppProfile.PrefixKey`.
  Store-Abstraktion `ISettingsStore` (`VideoPlayer.Storage/ISettingsStore.cs`),
  produktiv `PreferencesSettingsStore` auf `Microsoft.Maui.Storage.Preferences`
  (`VideoPlayer.Maui/Services/PreferencesSettingsStore.cs`, registriert `MauiProgram.cs` Z. 60).
  In-Memory-Stub für Tests: `VideoWebPlayer.Maui.Tests/Stubs/InMemorySettingsStore.cs`.

**Befund:** Es gibt **keine** Einstellung für Protokollierung und **keinen**
„Protokoll senden"-Button — beides ist neu zu schaffen.

## Logging/Protokoll-Infrastruktur (Ist-Zustand)

- **Konvention der App:** `System.Diagnostics.Debug.WriteLine` in `try/catch`-Blöcken
  (durchgängig in `VideoPlayer.Mvu/*Service.cs`, `VideoPlayer.Maui/Services/*`,
  Pages-Code-Behinds). Schreibt nur in die Debug-Ausgabe, keine Datei.
- **`Microsoft.Extensions.Logging.Debug`**: Paket in `VideoPlayer.Maui.csproj` (Z. 70),
  aktiviert nur unter `#if DEBUG` via `builder.Logging.AddDebug()`
  (`MauiProgram.cs` Z. 151–153). Kein File-Sink, kein Serilog, keine anderen Provider.
- **`ILogger?`-Option** des API-Clients: `VideoPlayerApiClientOptions.Logger`
  (`VideoPlayer.ApiClient/VideoPlayerApiClientOptions.cs`), genutzt für Warnungen in der
  Sende-Pipeline (`VideoPlayerApiClient.cs` Z. 608, 619, 637). Der alte Client
  `VideoWebPlayer.Client` hat ebenfalls eine `Logger`-Eigenschaft
  (`Microsoft.Extensions.Logging.Abstractions`-Referenz).
- **Dateiablage für ein Log:** `AppStorageLayout`/`AppStorageLayoutProvider`
  (`VideoPlayer.Storage/AppStorageLayout.cs`, `VideoPlayer.Maui/Services/AppStorageLayoutProvider.cs`)
  bieten Backup-/NoBackup-/Temp-Verzeichnisse — geeigneter Ort für eine Protokolldatei,
  aktuell aber keine Log-Datei vorhanden.

**Befund:** Ein versendbares Programmprotokoll existiert nicht; es gibt weder einen
Datei-Logger noch eine Log-Sammlung. Neu zu bauen (z. B. Datei-Logging in einen
Ablageort des `AppStorageLayout`, aktivierbar über eine neue `AppSettings`-Eigenschaft).

## Mail-/Share-Funktionalität (Ist-Zustand)

- **Keine** Verwendung von `Microsoft.Maui.ApplicationModel.Communication` (`Email.*`,
  `EmailMessage`, `ComposeAsync`), `Share.*`, `Launcher.*`, `mailto:` oder Mail-Paketen
  (MailKit/MimeKit) — im gesamten Repository nicht vorhanden (grep über alle Projekte).
- `Microsoft.Maui.Essentials` ist in `VideoPlayer.Maui.csproj` (Z. 67) referenziert —
  `Email.ComposeAsync`/`Share`/`Launcher` wären damit ohne neue Paketabhängigkeit
  verfügbar (Essentials liegt bei MAUI bei). Nutzung wäre neu zu implementieren.
- Dialoge: Es werden bereits `DisplayAlert`-Aufrufe genutzt (z. B. `ServerSetupPage.xaml.cs`
  Z. 166, 213–235); `DisplayActionSheet` gibt es nur im Altbestand
  (`VideoWebPlayer.Maui/MainPage.xaml.cs` Z. 116, `ServerSetupPage.xaml.cs` Z. 129).
