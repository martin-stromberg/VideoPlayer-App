# Interfaces

## `ISettingsStore`
Datei: `VideoPlayer.Storage/ISettingsStore.cs`

Nicht-geheime Schlüssel/Wert-Ablage (Plattform-Äquivalent von `Preferences`). Wird von `AppSettingsService` für sämtliche Einstellungsschlüssel verwendet; die MAUI-App stellt die Implementierung (`PreferencesSettingsStore`), die Tests `InMemorySettingsStore` (`VideoPlayer.Maui.Tests/Stubs/InMemorySettingsStore.cs`).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `Contains` | `key` | `bool` | Schlüssel vorhanden? |
| `GetString` | `key`, `defaultValue` | `string` | Zeichenfolge lesen |
| `GetInt` | `key`, `defaultValue` | `int` | Ganzzahl lesen (u. a. `AutoResumeRemainingSeconds`, `WatchedEndWindowSeconds`) |
| `GetBool` | `key`, `defaultValue` | `bool` | Wahrheitswert lesen (u. a. `AutoResumeEnabled`) |
| `Set` | `key`, `value` (`string`/`int`/`bool`) | `void` | Wert schreiben (drei Überladungen) |
| `Remove` | `key` | `void` | Schlüssel entfernen — existiert, wird für den verwaisten `AutoResumeRemainingSeconds`-Schlüssel per Vorgabe **nicht** aufgerufen (keine Migration) |
