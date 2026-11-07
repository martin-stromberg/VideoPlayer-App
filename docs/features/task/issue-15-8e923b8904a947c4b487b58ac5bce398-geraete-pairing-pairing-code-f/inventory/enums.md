# Enums — Bestandsaufnahme

## `ConnectionState`
Datei: `VideoPlayer.Mvu/ConnectionState.cs`

| Wert | Bedeutung |
|------|-----------|
| `Unknown` | Startzustand vor Beginn des Verbindungs-Workflows. |
| `Connecting` | Verbindungsaufbau läuft (Erreichbarkeitsprüfung, automatische Anmeldung). |
| `Connected` | Mit dem Server verbunden und angemeldet. |
| `NeedsLogin` | Server erreichbar, aber Anmeldung erforderlich (Anmeldemaske). |
| `NeedsServerSetup` | Kein Server hinterlegt bzw. kein Offline-Betrieb möglich (Servereinrichtung/-auswahl) — **Navigationsanker, in den laut Anforderung auch der Pairing-Fall einmündet** („Server verbunden, aber nicht gekoppelt" ist derzeit nicht unterscheidbar). |
| `Offline` | Server nicht erreichbar; lokale Offline-Daten vorhanden. |

## `AuthAttemptStatus`
Datei: `VideoPlayer.Mvu/AuthAttempt.cs`

| Wert | Bedeutung |
|------|-----------|
| `Succeeded` | Anmeldung erfolgreich; Token und Zugangsdaten gespeichert. |
| `MissingCredentials` | Keine gespeicherten Zugangsdaten (nur automatische Anmeldung). |
| `InvalidCredentials` | Server lehnt Zugangsdaten ab (401/403). |
| `ServerUnreachable` | Server nicht erreichbar (Transportfehler/Timeout nach Retry-Strategie). |
| `Failed` | Sonstiger Fehler der Anmeldung. |

Ein analoges Ergebnis-Enum für den Pairing-Versuch (400/401/429/Nicht-Erreichbarkeit) existiert nicht.

## `ServerHealthStatus`
Datei: `VideoPlayer.Mvu/ServerHealthStatus.cs`

| Wert | Bedeutung |
|------|-----------|
| `Unknown` | Erreichbarkeit noch nicht geprüft. |
| `Checking` | Prüfung läuft. |
| `Reachable` | Server erreichbar. |
| `Unreachable` | Server nicht erreichbar. |

## `AppPage`
Datei: `VideoPlayer.Mvu/AppPage.cs`

| Wert | Bedeutung |
|------|-----------|
| `Loading` | Start-Ladeanzeige (kein Menüeintrag). |
| `Home` | Startseite. |
| `Favorites` | Menüseite „Favoriten". |
| `Downloads` | Menüseite „Downloads". |
| `Profile` | Menüseite „Profil". |
| `Settings` | Menüseite „Einstellungen". |
| `ServerSetup` | Server-Einrichtung/-verwaltung — kein Menüeintrag; Window-Root beim Erststart bzw. Shell-Route `serversetup`. **Trägt künftig die Pairing-Eingabe; kein neuer Seitenwert erforderlich (Anforderung).** |
| `Login` | Anmeldemaske — kein Menüeintrag; Window-Root bzw. Shell-Route `login`. |

## Weitere, nicht betroffene Enums (Kontext)

`PlaybackSourceKind` (`VideoPlayer.Mvu`), `ServerEventKind` (`VideoPlayer.Mvu`), `DownloadState` (`VideoPlayer.Mvu`), `RecentEntryKind` (`VideoPlayer.ApiClient.Models`), `RecentEntryType` (`VideoWebPlayer.Client`), Legacy-Enums `DownloadRetentionType`/`DownloadStatus`/`VideoSourceType`/`CarouselKind` (`VideoWebPlayer.Maui`, nicht referenzierbar — Quelltext-Vertrag über `EnumCoverageTests`).

## Testabdeckungs-Pflicht

Der Git-Hook `enum-coverage-check` (`.githooks/enum-coverage-check.py`) bzw. `EnumCoverageTests` verlangt, dass jeder Enum-Wert in mindestens einer Testdatei vorkommt — neue Enum-Werte (z. B. ein Pairing-Ergebnis-Enum oder ein neuer `ConnectionState`-Wert) müssen Testabdeckung erhalten, sonst schlägt der Vertragstest/Hooks fehl.
