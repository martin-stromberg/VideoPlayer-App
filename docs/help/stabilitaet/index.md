# Stabilität der Test-Infrastruktur

Härtung der CI-Test-Infrastruktur: umgebungssensitive Tests laufen
getrennt von der blockierenden Lane, Flakes werden per Einmal-Re-Run
und TRX-Auswertung sichtbar und bei Häufung als Quarantäne-Issue
gemeldet.

## Inhalt

- [OsInterface-Tests und Flake-Quarantäne](os-interface-tests.md) —
  Trait-Konvention `Category=OsInterface`, CI-Lanes `verify` und
  `test-os-interface`, Einmal-Re-Run, deduplizierte
  Quarantäne-Auswertung und lokale gefilterte Läufe.
