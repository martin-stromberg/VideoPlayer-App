# E2E-Abschlussdiagnose am 2026-10-26

## Ergebnis

Nur Testinfrastruktur geändert: `AppInstance.cs` und `E2ETestBase.cs`. Kein Mobile-/Server-Produktcode, keine Pakete, keine Assertions der vorhandenen Benutzerflüsse geändert. Kein Commit.

Der WinRT-Abbruch ist jetzt einem MAUI-App-Prozess zugeordnet. Die genaue fehlerhafte COM-Objektlebensdauer ist noch nicht belegt und wurde nicht durch spekulative Produktänderungen oder Paketupdates behandelt. Die bisherigen E2E-Tests ignorierten den Exitcode der Kind-App; dadurch konnten Fälle trotz nativer Prozessfehlermeldung als bestanden erscheinen. Der neue Guard verhindert diese falsche Erfolgsaussage.

## Belastbare Prozesszuordnung

Windows-Anwendungsprotokoll, Provider `.NET Runtime`, Event-ID `1026`, Record-ID `1885682`, Zeit `2026-10-26T05:57:34.1503797Z` (lokal 06:57:34):

- Anwendung: `VideoPlayer.Maui.exe`
- Prozess-ID: `49896`
- Runtime: .NET `10.0.14`
- Stack: `Marshal.Release` → `WinRT.IObjectReference.DisposeTrackerSource` → `Dispose` → `Finalize` → `GC.RunFinalizers`

Dieser Eintrag passt zeitlich und inhaltlich zur nativen Fehlermeldung des vorherigen vollständigen 71/72-Laufs. Er belegt den Kind-App-Prozess statt eines xUnit-/FlaUI-Testhostabsturzes. Er belegt allein nicht, ob der Fehler bereits während aktiver Bedienung oder erst beim Fensterabbau eintrat. Ähnliche Ereignisse vom Vortag sind vorhanden, beweisen aber ohne Commit-/Testzuordnung keinen Fehler im unveränderten Basisstand.

Die E2E-Runner-Abhängigkeiten enthalten `WinRT.Runtime` nicht; die MAUI-App-Abhängigkeiten enthalten es. Es wurde keine Änderung an Windows- oder Firewall-Einstellungen vorgenommen; eine Firewallabfrage ist weiterhin nicht nachgewiesen.

## Eng begrenzte Änderung

- Standardausgabe und Fehlerausgabe jeder gestarteten App werden sofort asynchron gelesen, pro Prozess unter `.work/e2e-process-logs/{UTC-Zeit}-{PID}/` gespeichert und beim Ende unverändert an die Testausgabe weitergereicht.
- `process.json` enthält PID, Testprofil, Exitcode, Beginn des Fixture-Shutdowns, den Zustand `exitedBeforeShutdown`, expliziten Fixture-Kill und Abschluss der Ausgabe-Leser. Die Prozesszuordnung bleibt damit beim nächsten Auftreten erhalten.
- Nach einem expliziten Kill wartet die Fixture zusätzlich auf das Prozessende, bevor Profil und Prozesshandle aufgeräumt werden.
- Ein unerwarteter Exit ohne expliziten Fixture-Kill beziehungsweise ein weiterhin laufender Prozess wird als Fehler gemeldet. Absichtliche Fixture-Kills werden getrennt protokolliert und nicht als erfolgreicher geordneter Exit ausgegeben.
- Native Fehler, Protokollfehler und UIA-Cleanupfehler werden über die getrennte `IAsyncLifetime.DisposeAsync`-Phase der Testbasisklasse an xUnit gemeldet. Eine bereits fehlgeschlagene Assertion im eigentlichen Test bleibt dabei erhalten; es wird keine Ausnahme unterdrückt.

## Ausgeführte Nachweise

1. Alle elf `PlaylistManagementTests` mit Prozessdiagnose: **11/11 bestanden**, Exitcode 0, Laufzeit 1m03s. Alle elf App-Prozesse endeten mit Exitcode 0 ohne Fixture-Kill. Genre-Editor-Wiederöffnung erfolgreich; kein belastbarer Produktfehler aus dem einzelnen vorherigen Timeout ableitbar, deshalb keine Änderung an Playlistcode oder Testassertions.
2. Temporäres, nicht versioniertes Gegenbeispiel-Projekt `.work/e2e-shutdown-guard-probe`: zwei echte App-Prozesse wurden außerhalb der Fixture beendet. Beide Probe-Tests schlugen **wie vorgesehen** fehl. Der erste meldete den App-Exit als Fixturefehler; der zweite enthielt im xUnit-/TRX-Ergebnis sowohl `PRIMARY_ASSERTION_SENTINEL` als auch den App-Exitfehler. Die Probe-Dateien gehören nicht zur regulären Suite und werden nicht committed.
3. Nach Guard-Ergänzung: alle sechs `SeasonalGenresTests` und der Genre-Editor-Fall gemeinsam **7/7 bestanden**, Exitcode 0, Laufzeit 1m04s. Acht der neun gestarteten App-Prozesse endeten geordnet mit Exitcode 0; ein Offline-Neustartprozess benötigte nach dem dokumentierten Zehn-Sekunden-Warten den expliziten Fixture-Kill (Exit -1). Kein nativer WinRT-Abbruch in diesem Lauf.
4. Finaler Build des E2E-Testprojekts: erfolgreich, **0 Warnungen, 0 Fehler**. Der erste Sandbox-Build benötigte Zugriff auf Windows-SDK-Metadaten und wurde anschließend mit genehmigtem Zugriff erfolgreich wiederholt. `git diff --check` ohne Fehler.

Lokale maschinenlesbare Ergebnisse:

- `.work/test-results/e2e-process-diagnosis-20261026/playlist-diagnosis.trx`
- `.work/e2e-shutdown-guard-probe/results/guard-probe.trx` (erwartete Gegenbeispiel-Fehler)
- `.work/test-results/e2e-shutdown-guard-positive-20261026/positive.trx`
- `.work/e2e-process-logs/` (pro Prozess)

## Übergabe

Unabhängiges Code-Review der Testfixtureänderung und anschließend vollständiger Windows-E2E-Lauf mit dem neuen Guard sind nötig. Aus den gezielten Erfolgen wird kein neuer 72/72-Gesamtlauf abgeleitet. Falls der native Abbruch erneut auftritt, muss die jetzt protokollierte konkrete App-PID, der Exitcode und `exitedBeforeShutdown` ausgewertet werden; der betroffene Fall kann dann nicht mehr unbemerkt als bestanden gelten.

## Review-Nachbesserung: Kill-Race (P2)

Der unabhängige Reviewer hat zutreffend einen Racepfad gefunden: Das ursprüngliche Flag wurde bereits vor dem Killaufruf gesetzt und konnte einen zwischenzeitlich natürlich beendeten Prozess fälschlich als absichtlichen Kill behandeln. Dieser Befund ist eng in der Fixture korrigiert; Produktcode bleibt unverändert.

- `killAttempted` dokumentiert ausschließlich den Versuch.
- `killRequestSucceeded` wird erst gesetzt, nachdem `Process.Kill` ohne Ausnahme zurückgekehrt ist. Fehler des Killaufrufs oder Wartevorgangs werden separat in `killError` protokolliert.
- `forcedKill` wird erst bei der abschließenden Prozessdiagnose aus Versuch, erfolgreichem Aufruf und dem tatsächlich beobachteten Windows-Kill-Exitcode `-1` (`0xFFFFFFFF`) gebildet.
- Jeder andere Nichtnull-Exitcode, insbesondere `0xC0000005`, bleibt unabhängig von gesetzten Killflags ein Fixturefehler. Ein noch laufender Prozess bleibt ebenfalls ein Fehler. Die Übergabe an die getrennte xUnit-Aufräumphase ist unverändert.

Gezielte temporäre Probe `.work/e2e-kill-race-probe/RaceProbe.csproj`, Exitcode 0:

| Tatsächlicher Prozessabschluss | Killversuch | Killaufruf erfolgreich | Guard meldet Fehler |
|---|---|---|---|
| Natürlicher Exit `0xC0000005` | nein | nein | ja |
| Natürlicher Exit `0xC0000005` | ja | nein | ja |
| Natürlicher Exit `0xC0000005` | ja | ja (Gegenbeispiel-Flag) | ja |
| Tatsächlicher Fixture-Kill, beobachteter Exit `0xFFFFFFFF` | ja | ja | nein |
| Beobachteter Exit `0xFFFFFFFF`, kein erfolgreicher Killaufruf belegt | ja | nein | ja |

Die Probe startet eigene kurze Kindprozesse. Der native Fehlercode wird gezielt als normaler Prozess-Exit zurückgegeben; es wird kein tatsächlicher Speicherzugriffsfehler ausgelöst. Für die Positivkontrolle wird dagegen ein laufender eigener Kindprozess über dieselbe `TryKillAndWait`-Methode der Fixture beendet; sein tatsächlicher Exitcode `-1` wurde geprüft. Alle fünf erwarteten Klassifikationen sind eingetreten. Das Protokoll steht in `.work/e2e-kill-race-probe/console.log`; die Probe ist nicht Bestandteil der versionierten Testsuite.

Der abschließende E2E-Projektbuild war erfolgreich (0 Warnungen, 0 Fehler). Keine zusätzliche Vollsuite ausgeführt; nach unabhängigem Re-Review steht weiterhin der vollständige Lauf an.
