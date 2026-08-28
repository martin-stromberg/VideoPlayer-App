# Übersetzte Anforderung: Staging Branch

## Auslöser

Das Projekt benötigt einen `staging`-Branch als Drehscheibe für Qualitätsprüfungen und vorbereitete Releases.

## Ziele

- `main` darf ausschließlich über gut geprüfte Pull Requests vom `staging`-Branch aktualisiert werden.
- `staging` dient als Integrationsbranch, auf dem Tests, Linter und ein automatisierter Prerelease-Prozess laufen.
- Jede Änderung, die `main` erreicht, erzeugt Releases für Windows, iOS und Android sowie einen Back-Merge auf `staging`.

## Akzeptanzkriterien

### Pull Requests gegen `main`

- Die einzige erlaubte Quelle ist der `staging`-Branch.
- Alle Tests müssen erfolgreich sein.
- Nach dem Merge:
  - Semantic Release erzeugt Versionierte Builds für Windows, iOS und Android.
  - Ein Back-Merge-Pull Request von `main` auf `staging` wird erzeugt.

### Pull Requests gegen `staging`

- Alle Tests müssen erfolgreich sein.
- Linterprüfungen müssen bestehen.
- Nach dem Merge:
  - Ein Draft-PR von `staging` auf `main` wird vorbereitet.
  - Eine Prerelease-Version für Windows, iOS und Android wird erstellt (Format `v1.2.3.4-RC.1`).
  - Der RC-Zähler wird nach jedem Back-Merge neu hochgezählt.

## Offene Punkte

- Keine (freigabe der vorgeschlagenen CI-Architektur durch den initialen Plan).
