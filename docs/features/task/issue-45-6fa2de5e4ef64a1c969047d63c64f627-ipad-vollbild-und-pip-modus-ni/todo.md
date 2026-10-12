# Aufgabenliste – Anforderungsbearbeitung

Branch: `task/issue-45-6fa2de5e4ef64a1c969047d63c64f627-ipad-vollbild-und-pip-modus-ni`

| Status | Schritt | Beschreibung | Artefakt |
|--------|---------|--------------|----------|
| [x] | 1 | Branch-Name ermitteln | – |
| [x] | 2 | Verzeichnisstruktur vorbereiten | `docs/features/{branchname}/` |
| [x] | – | Einstiegspunkt ermitteln (Schritt 3 – keine Artefakte vorhanden; Fortsetzungslauf 2026-10-12: Schritt 6 wegen `continue.md`) | – |
| [x] | 3 | Anforderung übersetzen (Unteragent) | `requirement.md` |
| [x] | 4 | Bestandsaufnahme (Unteragent) | `inventory.md`, `inventory/` |
| [x] | 5 | Umsetzungsplanung (Unteragent) | `plan.md` |
| [x] | 5a | Offene Punkte prüfen und ggf. Planung wiederholen | `plan.md` (aktualisiert) |
| [x] | 5b | Plan gegen Anforderung und Testbedarf prüfen (Unteragent) | `plan-check.md` |
| [x] | 5c | Planungscommit | – |
| [x] | 6 | Implementierung (Unteragent) | Codeänderungen |
| [x] | 7 | Plan-Review (Unteragent, bedingt) | `review.md` |
| [x] | 8 | Usability-Review (Unteragent, bei UI-Änderungen) | `review-usability.md` |
| [x] | 9 | Code-Review (Unteragent) | `review-code.md` |
| [x] | 10 | Tests ausführen (Unteragent) | `test-results.md` |
| [x] | – | Iteration oder Abschluss entscheiden (3 Iterationen, Abbruch: kein Fortschritt — nur manuelle Prüfschritte offen) | – |
| [x] | 11 | Folgeaufgaben dokumentieren (bei Schleifenabbruch) | `continue.md` |
| [x] | 12 | Dokumentation erstellen (Unteragent) | `docs/help/` |
| [x] | 12b | README aktualisieren (geprüft: keine Änderung nötig — README beschreibt PiP-/Vollbild-Verhalten bereits korrekt) | `README.md` |
| [x] | 12c | Release Notes aktualisieren (Eintrag vorhanden, Commit cff1349) | `docs/RELEASE_NOTES.md` |
| [ ] | 13 | Nacharbeiten abschließen (offene Punkte aus `continue.md`) | `continue-done.md` |
| [ ] | – | Feature-Verzeichnis löschen | – |
| [ ] | – | Commit durchführen | – |

**Fortsetzungslauf 2026-10-12:** Einstieg Schritt 6 (vorhandene `continue.md`).
Alle 4 offenen Punkte erneut geprüft — keiner ist automatisierbar oder
code-seitig lösbar: Die drei iPad-Prüfschritte erfordern ein physisches
iPad mit TestFlight-Build (im Repository existiert ausschließlich eine
Windows-E2E-Suite auf FlaUI/UIA3-Basis, `net10.0-windows`; keine
Appium-/XCUITest-/MAUI-UI-Automatisierung), der Windows-Prüfschritt ist
im Plan ausdrücklich als manueller Schritt definiert (Ersatznachweis
53/53 bereits dokumentiert, Windows-Codepfad seitdem unverändert).
Code-seitig ist die Anordnung bereits durch Contract-Tests gepinnt
(`PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface`,
`PlayerPage_PlaylistNavigationBarClearsNativeControlsOnIos`).
`continue.md` bleibt bestehen — manuelle Geräteprüfung erforderlich.
