# Ausstehende externe Verifikationen

Register aller Anforderungspunkte, deren Nachweis in der aktuellen Umgebung (Windows-Entwicklungsrechner, kein Push durchgeführt) nicht geführt werden kann. Eintrag bleibt bis zum erbrachten Nachweis bestehen; Projektabschluss erfordert leeres Register oder dokumentiertes Sign-off.

| Schritt | Kriterium | Benötigte Voraussetzung | Stand |
|---------|-----------|--------------------------|-------|
| 1–laufend | `net10.0-tvos` kompiliert auf macOS; CI-Job `build-tvos` läuft erfolgreich (Simulator-Build ohne Signierung) | Push der Branches → CI-Lauf auf GitHub-macos-Runner | **FEHLGESCHLAGEN (Run 42103098219, 2026-11-05):** C# kompiliert sauber, aber nativer Linker-Fehler — `ld: building for 'tvOS-simulator', but linking in object file (e_sqlite3.a[arm64](sqlite3.o)) built for 'tvOS'`. `SQLitePCLRaw.lib.e_sqlite3` 3.53.3 hat keine tvossimulator-Slice. Fix auf eigenem Branch in Arbeit |
| 2–laufend | App startet auf tvOS-Simulator; UI/Focus-Navigation funktioniert tatsächlich (Runtime-Verhalten jenseits von Kompilierung) | tvOS-Simulator/-Gerät oder CI-Testlauf | Ausstehend |
| 11 | Release-Kandidat landet automatisiert in App Store Connect | Apple-Account-Artefakte (Bundle-ID, Provisioning-Profil, Zertifikate — manueller Vorlauf Kontoinhaber) + Secrets + Push | Ausstehend; Gate-Ansatz hält Pipeline bis dahin grün |

**Benötigte Entscheidung/Autorisierung:** Push der Branches nach `origin` (github.com/martin-stromberg/VideoPlayer-App) autorisieren, damit `build-tvos` real läuft — oder dedizierten Verifikationsschritt nach Bereitstellung eines macOS-Zugangs.
