# Testergebnisse — FlaUI-E2E-Tests (Issue 17, Schritt 3)

Datum: 2026-09-24, lokale Ausführung auf Windows 11 (interaktive Session).

## E2E-Tests `VideoPlayer.E2E.Tests`

Kommando:

```bash
dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj
```

Ergebnis des finalen Laufs:

| Kennzahl | Wert |
|----------|------|
| Gesamt | 10 |
| Bestanden | 10 |
| Fehlgeschlagen | 0 |
| Übersprungen | 0 |
| Dauer | 17 min 10 s |

Einzelergebnisse:

| Test | Ergebnis |
|------|----------|
| `ServerSetupTests.FreshStart_ShowsServerSetupPage` | bestanden |
| `ServerSetupTests.InvalidAddress_ShowsValidationError` | bestanden |
| `ServerSetupTests.UnreachableServer_ShowsError` | bestanden |
| `ServerSetupTests.ReachableServer_RequiresPairing` | bestanden |
| `PairingTests.MalformedCode_ShowsFormatError` | bestanden |
| `PairingTests.WrongCode_ShowsPairingError` | bestanden |
| `PairingTests.ValidCode_ShowsLoginPage` | bestanden |
| `LoginTests.WrongPassword_ShowsLoginError` | bestanden |
| `LoginTests.ValidLogin_ShowsHomePage` | bestanden |
| `ServerSwitchTests.SwitchToKnownServer_ReturnsToHomePage` | bestanden |

Ausgeführt wird die echte `VideoPlayer.Maui.exe` (Debug-Build,
`bin/Debug/net10.0-windows10.0.19041.0/win-x64/`); die Tests starten
je einen eigenen Prozess mit isoliertem `VIDEOPLAYER_PROFILE`, lesen
den Developer-API-Port aus `devapi.json` und fahren über FlaUI.UIA3
echte UI-Aktionen (Texteingabe, Button-Invoke, Flyout-Navigation).
Die Zustands-Assertions laufen über `GET /model`; die Auswahl des
bekannten Servers erfolgt über `POST /invoke` mit `itemIndex`
(CollectionView ohne Koordinatenklick). Als Backend dient der
projektinterne `MockVideoServer` (TcpListener auf Loopback) mit
Health-, Pairing-Exchange- (ECDH P-256 + AES-256-GCM), Login- und
Listen-Endpunkten.

Nach dem Lauf geprüft: keine laufenden `VideoPlayer.Maui`-Prozesse,
keine `e2e-*`-Profilreste unter `%APPDATA%/VideoPlayer` und
`%LOCALAPPDATA%/VideoPlayer`, kein verwaistes `devapi.json`.

## Bestandstests `VideoWebPlayer.Maui.Tests` (Regressionsprüfung)

Kommando:

```bash
dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj
```

Ergebnis: **926 / 926 bestanden**, 0 fehlgeschlagen, 0 übersprungen
(Dauer ~15 s) — die Produktivänderungen (`ClientGate`-Profil-Isolation
der Token-Datei, `/invoke`-`itemIndex`) verursachen keine Regression.

## Iterationsverlauf (diagnostisch relevant)

| Lauf | Ergebnis | Befund / Fix |
|------|----------|--------------|
| 1 | 3/10 | `isPairingRequired` blieb `false`: die Token-Datei `client-api-token.txt` des Entwicklerprofils wurde von E2E-Instanzen geerbt → `ClientGate.TokenFileDirectory` profil-isoliert (`VIDEOPLAYER_PROFILE`-Unterverzeichnis). |
| 2 | 8/10 | Login schlug fehl: der Mock las `email`/`password` (camelCase), der Client sendet `Email`/`Password` → Mock bindet case-insensitiv. |
| 3 | 9/10 | Flyout-Navigation: lokalisierter Button-Name („Navigation öffnen"), Menüeinträge sind `ListItem` mit `SelectionItem`-Pattern; zudem zeigt die Einstellungen-Auswahl nach abgeschlossenem Serverwechsel die noch geschobene `serversetup`-Route (Shell-Tab-Stack) — Test akzeptiert beide Landezustände, Fund in `review-code.md` dokumentiert. |
| 4 | 10/10 | Finaler Vollauf. |
