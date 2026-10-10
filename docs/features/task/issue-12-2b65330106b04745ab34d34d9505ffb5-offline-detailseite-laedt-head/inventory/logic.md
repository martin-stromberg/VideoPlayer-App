# Logik

## `AppUpdate`

Datei: `VideoPlayer.Mvu/AppUpdate.cs` (statische Update-Funktion, reine Übergänge `AppModel` + `AppTransition`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Update` | `public static` | Zentrale Nachrichtenverteilung (switch über `AppMessage`-Subtypen) |
| `EnterConnected` | `private static` | Einziger Übergang in `ConnectionState.Connected`: setzt Zustandsfelder, reiht `LoadSourcesEffect`, `ConnectServerEventsEffect`, `RunFullSyncEffect`, `ResumeDownloadsEffect` ein und ruft `WithHeroLoad` auf; navigiert von Workflow-Seiten auf `Home`. **Kein** `WithDetailsHeaderLoad`-Aufruf. |
| `UpdateAutoLoginCompleted` | `private static` | Bei `AuthAttemptStatus.Succeeded` → `EnterConnected`; sonst `NeedsLogin`/Re-Pairing |
| `UpdateLoginCompleted` | `private static` | Bei `AuthAttemptStatus.Succeeded` → `EnterConnected`; sonst Anmeldefehler/Re-Pairing |
| `UpdateNetworkAvailabilityChanged` | `private static` | Offline bei `!IsOnline` aus `Connected` heraus (`MediaDetails` bleibt erhalten); Online-Rückkehr aus `Offline` → `CheckServerHealthEffect` (Reconnect-Kette) |
| `UpdateServerHealthCheckCompleted` | `private static` | Erfolg → `AttemptAutoLoginEffect`; Versagen → Offline/Einrichtung |
| `UpdateMediaItemSelected` | `private static` | Legt neuen `MediaDetailsState` an (`IsLoading`, `Attempted`) + `LoadMediaDetailsEffect` |
| `UpdateMediaDetailsLoaded` | `private static` | Übernimmt Details/Fehler; ruft `WithDetailsHeaderLoad` auf |
| `WithDetailsHeaderLoad` | `private static` | Gate: `ConnectionState != Connected` ODER `Details == null` ODER keine Bildquelle (`HeaderEpisodeId`/`HeaderPictureId`) ODER `IsHeaderImageLoading` → kein Effekt; sonst `LoadMediaDetailsHeaderEffect` + `IsHeaderImageLoading = true`. **Keine** `HasHeaderImage`-Prüfung. |
| `UpdateMediaDetailsHeaderImageLoaded` | `private static` | Identitätsabgleich (Medientyp, Id, Bildquelle) — verspätete Antworten werden verworfen; Fehlschlag (`null`) löscht kein vorhandenes Bild |
| `UpdateSeasonSelected` | `private static` | Wählt erste Episode der Staffel; bei geänderter Bildquelle Bild verwerfen + `WithDetailsHeaderLoad` |
| `WithHeroLoad` | `private static` | Referenzmuster: Gate `Connected` + `HomeHeroEnabled` + `!IsLoading` + `!HasImage` → `LoadHeroBackgroundEffect`; aufgerufen in `EnterConnected` und `UpdateSaveSettingsRequested` |
| `UpdateSessionExpired` | `private static` | `NeedsLogin` + Rücksetzen u. a. `MediaDetails = MediaDetailsState.Empty` |
| `UpdateLogout` | `private static` | `NeedsLogin` + vollständige Sitzungs-Rücksetzung inkl. `MediaDetails = MediaDetailsState.Empty` |

Abonnierte Events: keine (reine Funktion).
Publizierte Events: keine (Effekte/Nachrichten über `AppTransition`).

## `ShellAppEffectHandler`

Datei: `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `HandleAsync` (switch, Z. 181) | `private` | Verteilt `LoadMediaDetailsHeaderEffect` an `LoadMediaDetailsHeaderAsync` |
| `LoadMediaDetailsHeaderAsync` | `private` | Bildabruf über `authSession.Client`: `GetEpisodeBackgroundImageAsync` bei `HeaderEpisodeId > 0`, sonst `GetPictureAsync` bei `HeaderPictureId > 0`; Fehlschlag → `ImageBytes = null` (kein Fehlerzustand); meldet stets `MediaDetailsHeaderImageLoadedMessage` (außer bei Abbruch via `OperationCanceledException`) |
| `OnNetworkAvailabilityChanged` | `private` | `INetworkStatusMonitor.AvailabilityChanged` → `NetworkAvailabilityChangedMessage` |

Abonnierte Events: `networkMonitor.AvailabilityChanged`.
Publizierte Events: `MediaDetailsHeaderImageLoadedMessage`, `NetworkAvailabilityChangedMessage` (via `DispatchMessageAsync`).

## `MediaDetailsService`

Datei: `VideoPlayer.Mvu/MediaDetailsService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadAsync` | `public` | Ohne angemeldeten Client → `LoadOfflineAsync`; sonst Serverabruf je Medientyp (`MapMovie`, `MapShow`, `LoadSeasonInShowContextAsync`, `LoadEpisodeAsync`, `LoadCollectionOrMovieAsync`) |
| `LoadOfflineAsync` | `private` | Offline-Snapshot aus Download-Manifest, `ShowDetailsCache` und Favoriten-Kopie; wirft `InvalidOperationException` ohne lokalen Kontext |
| `MapOfflineShow` | `private` | Serien-Snapshot: setzt `Seasons`, `SelectedSeasonId`, `SelectedEpisodeId` (bei `tvshowepisode`), `IsOfflineSnapshot`; **kein** `BannerPictureId` — `HeaderPictureId` ist offline `null`, `HeaderEpisodeId` über `SelectedEpisodeId` ableitbar |

Wird aufgerufen von `ShellAppEffectHandler.LoadMediaDetailsAsync` (für `LoadMediaDetailsEffect`).

## `MediaDetailsPage` (Anzeige, App-Rand)

Datei: `VideoPlayer.Maui/Pages/MediaDetailsPage.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `UpdateHeaderImage` | `private` | Übernimmt `state.HeaderImageBytes` in `HeaderImage.Source` (Referenzvergleich gegen `appliedHeaderBytes`); ohne Bild bleibt die Bildebene leer — Gradienten/Texte tragen den Kopfbereich. Reagiert rein auf den Model-Stand: ein nachgeladenes Bild erscheint ohne Seiten-Neuaufbau. |
