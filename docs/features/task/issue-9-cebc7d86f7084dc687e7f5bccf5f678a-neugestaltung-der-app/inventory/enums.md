# Enums

## `AppPage`

Datei: `VideoPlayer.Mvu/AppPage.cs`

Seiten der App; die sechs Menüseiten sind in `AppRoutes.MenuPages` gepinnt.

| Wert | Bedeutung |
|------|-----------|
| `Loading` | Start-Ladeanzeige (kein Menüeintrag, keine Route). |
| `Home` | Menüseite „Startseite" (Route `home`). |
| `Sources` | Menüseite „Quellen" (Route `sources` → `SourcesPage`). |
| `Favorites` | Menüseite „Favoriten" (Route `favorites`). |
| `Downloads` | Menüseite „Downloads" (Route `downloads`). |
| `Profile` | Menüseite „Profil" (Route `profile`). |
| `Settings` | Menüseite „Einstellungen" (Route `settings`). |
| `ServerSetup` | Server-Einrichtung/-verwaltung (kein Menüeintrag; Route `serversetup` bzw. Window-Root). |
| `Login` | Anmeldemaske (kein Menüeintrag; Route `login` bzw. Window-Root). |

Hinweis: Für die `sourceoverview`-Route existiert kein `AppPage`-Wert — die Browsing-Seite verändert `CurrentPage` nicht.

## `ConnectionState`

Datei: `VideoPlayer.Mvu/ConnectionState.cs`

| Wert | Bedeutung |
|------|-----------|
| `Unknown` | Startzustand vor dem Verbindungs-Workflow. |
| `Connecting` | Verbindungsaufbau läuft (Erreichbarkeitsprüfung, automatische Anmeldung). |
| `Connected` | Verbunden und angemeldet — Quellen-Ladevorgänge sind nur hier wirksam. |
| `NeedsLogin` | Server erreichbar, Anmeldung erforderlich. |
| `NeedsServerSetup` | Kein Server hinterlegt / kein Offline-Betrieb möglich. |
| `Offline` | Server nicht erreichbar, lokale Offline-Daten vorhanden — keine Quellenliste. |

## `ServerEventKind`

Datei: `VideoPlayer.Mvu/ServerEventKind.cs`

| Wert | Bedeutung |
|------|-----------|
| `ContinueWatchingUpdated` | Weiterschauen-Liste aktualisiert → `SyncContinueWatchingEffect`. |
| `FavoritesChanged` | Favoriten geändert → `SyncFavoritesEffect`. |
| `NewVideosScanned` | Neue Inhalte eingescannt (mit `SourceId`/`Count`) → `SyncMetadataEffect` — erneuert u. a. den Quellen-Cache `cached_lists["sources"]`. |
