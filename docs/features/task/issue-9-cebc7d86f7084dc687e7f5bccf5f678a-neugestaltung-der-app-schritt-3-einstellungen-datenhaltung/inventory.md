# Bestandsaufnahme: Einstellungsverwaltung und lokale Datenhaltung mit Backup-Trennung

Analysiert wurden die neue App `VideoPlayer.Maui` / `VideoPlayer.Mvu` (Umsetzungsziel)
sowie die Alt-App `VideoWebPlayer.Maui` (nur fachliche Referenz für Ablagen und
Profil-Isolation), bezogen auf die Anforderung in `requirement.md`.

## Zusammenfassung

- **Vorhanden:** MVU-Kern (`AppModel`, `AppUpdate`, `MvuHost`, `IAppEffectHandler`)
  mit etabliertem Muster für Messages/Effekte; `SettingsPage`-Grundgerüst (leer);
  `ServerAddressProvider` (liest `Preferences` direkt, noch ohne Profil-Präfix);
  DI-Verdrahtung in `MauiProgram`; Contract-Test-Muster für MAUI-Quelldateien.
- **Fehlt:** Einstellungsmodell und -dienst der neuen App, sichere Geheimnisablage,
  Profil-Isolation (`VIDEOPLAYER_PROFILE`), lokale Datenbank(en), strukturierte
  Dateiablage, Backup-Klassifikation/-Ausschluss, pflegbare Einstellungsseite.
- **Referenz (Alt-App, nicht wiederverwendet):** `SettingsService`
  (`Preferences`/`SecureStorage` mit `GetPrefixedKey`), `ProfileManager`
  (`VIDEOWEBPLAYER_PROFILE`), `ClientDatabase` (`sqlite-net-pcl`,
  `FileSystem.AppDataDirectory/downloads[_profil].db3`) — Details in
  `docs/projects/task/.../inventory/services.md`.
- **Test-Ausgangszustand:** 101 Tests, alle bestanden (siehe
  [Tests](inventory/tests.md)). Kein E2E-/UI-Automatisierungs-Framework im Repo;
  MAUI-Projekt ist vom Testprojekt nicht referenzierbar (Contract-Test-Muster
  über Quelldateien etabliert).

## Details

- [Datenmodell und MVU-Typen](inventory/models.md)
- [Logik und Services](inventory/logic.md)
- [Enums](inventory/enums.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
