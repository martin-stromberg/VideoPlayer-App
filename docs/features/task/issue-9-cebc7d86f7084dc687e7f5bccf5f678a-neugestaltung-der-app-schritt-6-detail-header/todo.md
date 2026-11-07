# Aufgabenliste – Anforderungsbearbeitung

Branch: `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app-schritt-6-detail-header`

| Status | Schritt | Beschreibung | Artefakt |
|--------|---------|--------------|----------|
| [x] | 1 | Branch-Name ermitteln | – |
| [x] | 2 | Verzeichnisstruktur vorbereiten | `docs/features/{branchname}/` |
| [x] | – | Einstiegspunkt ermitteln | – |
| [x] | 3 | Anforderung übersetzen (Unteragent) | `requirement.md` |
| [x] | 4 | Bestandsaufnahme (Unteragent) | `inventory.md`, `inventory/` |
| [x] | 5 | Umsetzungsplanung (Unteragent) | `plan.md` |
| [x] | 5a | Offene Punkte prüfen und ggf. Planung wiederholen | `plan.md` (aktualisiert) |
| [x] | 5b | Plan gegen Anforderung und Testbedarf prüfen (Unteragent) | `plan-check.md` |
| [x] | 5c | Planungscommit | `d862ff6` |
| [x] | 6 | Implementierung | Codeänderungen (API-Client `GetEpisodeBackgroundImageAsync`, Modell-Ableitungen `Header*`, MVU-Effekt/Message/State, `MediaDetailsPage`-Vollbreiten-Header, Effekt-Handler) |
| [x] | 7 | Plan-Review | `review.md` |
| [x] | 8 | Usability-Review (UI-Änderung → Pflicht) | `review-usability.md` |
| [x] | 9 | Code-Review | `review-code.md` |
| [x] | 10 | Tests ausführen | `test-results.md` (728/728 bestanden, Debug + Release fehlerfrei) |
| [x] | – | Iteration oder Abschluss entscheiden | Abschluss nach Korrektur `SelectedEpisodeId` im Offline-Pfad |
| [x] | 11 | Folgeaufgaben dokumentieren (bei Schleifenabbruch) | nicht erforderlich (kein Abbruch) |
| [x] | 12 | Dokumentation erstellen | `docs/help/quellen-browsing-details.md`, `docs/help/index.md` |
| [x] | 12b | README aktualisieren | `README.md` |
| [x] | 12c | Release Notes aktualisieren | `docs/RELEASE_NOTES.md`, `changes.log` |
| [ ] | – | Feature-Verzeichnis löschen | bleibt als Nachweis bestehen (Konvention der Schritte 2–5) |
| [ ] | – | Commit durchführen | – |
