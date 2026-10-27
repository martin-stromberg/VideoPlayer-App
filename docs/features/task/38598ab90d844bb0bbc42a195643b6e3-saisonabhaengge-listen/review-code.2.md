# Code-Review: Saisonabhängige Listen – Iteration 2

**Status:** Keine Befunde

## Befunde

Keine.

## Umfang und Ergebnis

Geprüft wurden ausschließlich die seit dem grünen Erstbericht `review-code.1.md` ergänzten Änderungen: gemeinsamer `E2ETestBase.LoginAsync`, Falsches-Passwort-Fluss in `LoginTests`, Verwendung des gemeinsamen Helpers in `SeasonalGenresTests` und finaler Backendlieferstand. Keine Produktkorrekturen oder Commits durch den Prüfer.

- Der Helper setzt über die bestehende Developer-API die tatsächlichen MAUI-`InputView.Text`-Eigenschaften der beiden Eingabefelder im aktuellen visuellen Baum. Er setzt weder Sitzungszustand noch Login-Ergebnis direkt.
- `/invoke` verwendet für `LoginButton` den bestehenden `Button.SendClicked()`-Pfad. Dadurch läuft `OnSetupLoginClicked` mit den echten Feldwerten, `LoginSubmittedMessage`, Authentifizierungsclient und HTTP-Serverprüfung.
- Vorbedingungen Pairing und Anmeldemaske bleiben erhalten. Der Mock erzwingt weiterhin das Geräte-Gate und prüft Benutzername/Passwort; die Umstellung verändert weder Serverantworten noch Authentifizierungsregeln.
- Erfolg wird unverändert über `Home`, `Connected` und die erwartete Serveradresse geprüft. `ValidLogin_ShowsHomePage` prüft weiterhin den tatsächlichen `LoginCount == 1`. Der Falsches-Passwort-Test sendet weiter ein falsches Passwort und verlangt Fehlermeldung sowie Verbleib auf `ServerSetup`.
- Die saisonalen Szenarien verwenden jetzt denselben Helper; ihre Assertions für Genreinhalt, Navigation, Cache, leere Antwort und Ablauf bleiben erhalten. Diese Änderung beseitigt die Windows-UIA-/PasswordBox-Eingabegrenze ohne den Authentifizierungsfluss zu umgehen. Die native Tastatureingabeschicht selbst ist dadurch kein Gegenstand dieses E2E-Setups.
- Backend `a36d2db5272b122dff0b7f54e3b9d8ff29309438` verglichen mit unabhängig abgenommenem `1bb12a42ffeadab62702117fcdc9eb9955e7fd48`: kein Diff in `VideoWebPlayer`, `VideoWebPlayer.Client` oder `VideoWebPlayer.Tests`; nur übernommener Abnahmebericht und Handover. Die Erstabnahme samt erfolgreichen Quellenfilter-/Ablaufmutationen gilt unverändert.
- Keine neuen `RaiseUiActionRequested`-Aktionen oder UI-Handleränderungen. Finale Lieferhinweise benennen den geprüften und finalen Backendstand nachvollziehbar.

## Validierung

In dieser Reviewrunde Code- und Vertragsprüfung ohne erneute Testausführung; die 11 gezielten Tests wurden vom Implementierungsagenten als bestanden gemeldet und werden durch den separaten unabhängigen Lifecycle-Testschritt ergänzt. Es wurde kein fehlender Test als bestanden gewertet. Die im Erstbericht selbst ausgeführten 7 Mobile- und 12 Backend-/Webtests betreffen unveränderten Produktcode.
