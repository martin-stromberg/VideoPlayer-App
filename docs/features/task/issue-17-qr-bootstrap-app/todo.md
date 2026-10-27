# Aufgabenliste – QR-/Ticket-Onboarding (Bootstrap) in der App

Feature: Schritt 5b des Projekts Issue #17 – App-seitiges QR-/Ticket-Onboarding (Bootstrap-Kopplung)
Branch: `task/issue-17-qr-bootstrap-app` (Basis: `project/issue-17-testbarkeit-und-onboarding`)
Server-Vertrag: `acceptance-schritt-5a.md` (Server-Repo `487c3041-…`, Branch `task/issue-231-qr-bootstrap-geraetekopplung-login`, Issue #231 – **nicht gemergt**)

| Status | Phase | Beschreibung | Artefakt |
|--------|-------|--------------|----------|
| [x] | 1 | Branchprüfung (kein geschützter Branch, working tree clean) | – |
| [x] | 2 | Verzeichnis/Tracking | `todo.md` |
| [x] | 3 | Anforderung aufbereiten | `requirement.md` |
| [x] | 4 | Bestandsaufnahme | `inventory.md`, `inventory/` |
| [x] | 5/5a/5b | Umsetzungsplan + Planprüfung | `plan.md`, `plan-check.md` |
| [x] | 5c | Planungscommit | `e65de0a` |
| [x] | 6 | Implementierung (Parser, ApiClient, MVU, UI, Refresh, Tests) | – |
| [x] | 7–9 | Reviews (Funktion: Vollständig umgesetzt; Usability: Keine Befunde; Code: 2 Befunde in Iteration behoben) | `review.md`, `review-usability.md`, `review-code.md` |
| [x] | 10 | Voller Testlauf (1013 Unit + 17 E2E) + Screenshots | `test-results.md`, `live-verification/` |
| [x] | 11 | Iteration/ggf. `continue.md` — nicht nötig (alle Prüfungen grün) | – |
| [x] | 12/12b/12c | Doku (`docs/help/*`, `README.md`, `docs/RELEASE_NOTES.md`) | – |
| [ ] | – | Abschluss-Commit (Feature-Verzeichnis bleibt im Repo – Projektkonvention) | – |
| [ ] | – | Optional: Scratch-Lauf gegen echten Server (Issue-#231-Branch, eigene Scratch-DB) | `live-verification/` |

## Feste Vorgaben für diesen Schritt

- Feature-Verzeichnis im Abschluss-Commit **behalten** (Konvention wie Schritte 3–5a).
- Nur App-Repo ändern; Server-Repo nur lesen bzw. mit Scratch-DB lokal laufen lassen (niemals produktive/dev-DB migrieren).
- `ApplicationVersion` in `VideoPlayer.Maui.csproj` und `.ios-deploy.user.json` nicht anfassen.
- Scan-Paket: **ZXing.Net.Maui.Controls 0.10.4** (MIT, veröffentlicht 14.08.2026 → >7 Tage alt, net10.0-Assets für android/ios; Windows ohne Kamera-Support → nur iOS/Android).
