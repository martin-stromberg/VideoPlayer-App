# Enums — Wiedergabe-Pfad

## `PlaybackSourceKind`

Datei: `VideoPlayer.Mvu/PlaybackSourceKind.cs`

| Wert | Bedeutung |
|------|-----------|
| `ServerStream` | Stream-Endpunkt `api/items/{typ}/{id}/stream` — `PlayerPage` nutzt `MediaSource.FromUri`. |
| `LocalFile` | Lokale Videodatei aus dem Download-Manifest — `PlayerPage` nutzt `MediaSource.FromFile`. |

## `AppPage` (kein Player-Eintrag)

Datei: `VideoPlayer.Mvu/AppPage.cs`

| Wert | Bedeutung |
|------|-----------|
| `Loading` | Start-Ladeanzeige. |
| `Home` | Startseite. |
| `Favorites` | Menüseite. |
| `Downloads` | Menüseite. |
| `Profile` | Menüseite. |
| `Settings` | Menüseite. |
| `ServerSetup` | Server-Einrichtung/-verwaltung (kein Menüeintrag). |
| `Login` | Anmeldemaske (kein Menüeintrag). |

Anmerkung: Es gibt **keinen** `AppPage.Player`-Wert — die
Wiedergabeseite ist eine geschobene Shell-Route (`"player"` ohne
FlyoutItem); `NavigatedMessage`/`AppRoutes` decken sie nicht ab
(kein `Navigated`-Dispatch beim Öffnen/Schließen des Players).

## `MediaElementState` (extern, Toolkit)

`CommunityToolkit.Maui.Core.MediaElementState` — in `PlayerPage`
verwendet: `Playing` (Takt-Bedingung), `Buffering`/`Opening`
(→ `IsBuffering` der `PlaybackProgressMessage`). Definiert im Paket
`CommunityToolkit.Maui.MediaElement` 10.0.0.
