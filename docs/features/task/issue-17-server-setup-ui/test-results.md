# Testergebnisse — Modernisierung der Serververbindungs-UI (Issue 17, Schritt 4)

Datum: 2026-09-24, lokale Ausführung auf Windows 11 (interaktive Session).

## Build `VideoPlayer.Maui`

Kommando: `dotnet build VideoPlayer.Maui/VideoPlayer.Maui.csproj -f net10.0-windows10.0.19041.0 -c Debug`

| Kennzahl | Wert |
|----------|------|
| Warnungen | 0 |
| Fehler | 0 |

## Unit-/Contract-Tests `VideoWebPlayer.Maui.Tests`

Kommando:

```bash
dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj
```

| Kennzahl | Wert |
|----------|------|
| Gesamt | 927 |
| Bestanden | 927 |
| Fehlgeschlagen | 0 |
| Dauer | ~9,5 s |

Baseline vor der Änderung: 926/926 — das Plus von 1 ist der neue
Contract-Test `ServerSetupPage_UsesCardLayout` (pinnt `RoundRectangle 8`,
`ServerHealthLabel`, `RoundRectangle 999` und das `#`-Verbot). Die
AutomationId-Menge wurde bewusst um sieben neue Ids erweitert
(`ActiveServerCard`, `ServerHealthLabel`, `DiscoveryRunningLabel`,
`ConnectCard`, `CheckStatusLabel`, `PairingSection`,
`PairingRunningLabel`) — kein bestehender Pin entfernt;
`PairingContractTests` unverändert grün (kein `MaxLength`,
„Verwaltung" im Hinweistext, kein „-stellig").

## E2E-Tests `VideoPlayer.E2E.Tests`

Kommando:

```bash
dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj
```

| Kennzahl | Wert |
|----------|------|
| Gesamt | 11 |
| Bestanden | 11 |
| Fehlgeschlagen | 0 |
| Dauer | ~53 s |

Einzelergebnisse:

| Test | Ergebnis |
|------|----------|
| `ServerSetupTests.FreshStart_ShowsServerSetupPage` (erweitert um `ConnectCard`-Assert) | bestanden |
| `ServerSetupTests.InvalidAddress_ShowsValidationError` | bestanden |
| `ServerSetupTests.UnreachableServer_ShowsError` | bestanden |
| `ServerSetupTests.ReachableServer_RequiresPairing` | bestanden |
| `ServerSetupTests.ReachableServer_ShowsHealthBadgeAndGuidedPairing` (neu) | bestanden |
| `PairingTests.MalformedCode_ShowsFormatError` | bestanden |
| `PairingTests.WrongCode_ShowsPairingError` | bestanden |
| `PairingTests.ValidCode_ShowsLoginPage` | bestanden |
| `LoginTests.WrongPassword_ShowsLoginError` | bestanden |
| `LoginTests.ValidLogin_ShowsHomePage` | bestanden |
| `ServerSwitchTests.SwitchToKnownServer_ReturnsToHomePage` | bestanden |

Baseline vor der Änderung: 10/10 — das Plus von 1 ist der neue Test
`ReachableServer_ShowsHealthBadgeAndGuidedPairing`, der den exakten
Benutzerfluss prüft: Adresse eingeben → verbinden → `ServerHealth =
"Reachable"` → `ActiveServerCard` sichtbar mit Badge „Server:
erreichbar" → geführte Pairing-Karte sichtbar (Texte „Verwaltung"/
„Gerät koppeln") → `PairingCodeEntry`/`PairButton` bedienbar.

## Visuelle Verifikation

Drei Screenshots der echten App (isoliertes Profil + `MockVideoServer`)
in `live-verification/` — Details und Pixel-Analyse (Dark-only
bestätigt) in `live-verification.md`:

- `serversetup-01-erststart.png` — Erststart (Kopf, Discovery, Verbinden-Karte)
- `serversetup-02-pairing.png` — Badge „erreichbar" + geführte Pairing-Karte
- `serversetup-03-bekannte-server.png` — „Server wechseln" mit Kartenliste

## Nachlauf-Prüfung

- Keine Änderungen an `VideoPlayer.Maui/VideoPlayer.Maui.csproj` oder
  `.ios-deploy.user.json` (siehe `git status`).
- `serversetup` bleibt Workflow-Route ohne `FlyoutItem`
  (Contract-Test grün).
