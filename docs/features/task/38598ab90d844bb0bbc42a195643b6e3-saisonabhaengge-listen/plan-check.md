# Planprüfung: Saisonabhängige Genre-Listen

**Status: Plan vollständig**

## Prüfgrundlage

Erneut unabhängig geprüft wurden `requirement.md`, der aktualisierte `plan.md`, die Bestandsaufnahme samt Detaildokumenten und die Befunde aus `plan-check.1.md`. Maßgeblich sind die Nutzerantworten zur Servererweiterung zuerst, Umstellung der Webansicht, gleichem Medienumfang, serverseitiger Saisonentscheidung und dem Saisonende als Cache-Ablaufgrenze. Die zuvor lesend geprüften Servervorgaben und vorhandenen E2E-Muster wurden beim Abgleich berücksichtigt.

Das im Lifecycle genannte Modell `opus-4.8` steht nicht zur Verfügung; diese Prüfung erfolgte mit dem verfügbaren geerbten Modell. Keine Implementierung, Tests oder Commits wurden vorgenommen.

## Ergebnis der Nachprüfung

| Vorherige Lücke | Nachweis im aktualisierten Plan | Ergebnis |
|---|---|---|
| Unbegrenzte Offlineanzeige trotz Saisonende | Verpflichtendes absolutes UTC-`expiresAt` je Sektion; exklusive Grenze nach dem letzten inklusiven Saisontag. Mapping, Resume und abbrechbarer Timer entfernen abgelaufene Sektionen ohne erfolgreichen Abruf. Fehlendes/ungültiges Ablaufdatum wird nicht unbegrenzt angezeigt. Testbare Uhr sowie Grenz-, Fehler-/Offline- und geöffnete-UI-E2E sind geplant. Anforderung und Plan sind jetzt konsistent. | Geschlossen |
| Kein konkreter Web-E2E-Nachweis | Eigene Playwright-/Kestrel-/SQLite-Fixture und `SeasonalGenresE2ETests.cs`, echter Benutzerlogin, aktives/inaktives/leeres Genre, Quellenrechte, Reihenfolge, Watched-Markierung, Film-/Sammlungs-/Seriennavigation und Ablauf im geöffneten Home sind konkret festgelegt. Requestbeobachtung belegt den echten authentifizierten API-Aufruf; Architekturprüfung schließt direkten EF-Fallback aus. Gezielter Ausführungsbefehl und Pflicht zum erfolgreichen Ergebnis sind vorhanden. | Geschlossen |
| Server-Abschlussprüfungen fehlen | Debug-/Release-Build der Serverlösung, vollständige Server-Testsuite, gezielte neue E2E und Mobile-Projektbefehle sind verbindlich benannt. Unabhängige Backend-Abnahme, Handover-/Autorenregeln, Mutationstest-Nachweis und getrennte Backend-Lieferung sind berücksichtigt. | Geschlossen |

## Vollständigkeitsabgleich

- Der neue authentifizierte saisonale Vertrag ist beschrieben; Backend wird vor Web-Migration und Mobile-Anbindung umgesetzt. Medienumfang, Gruppierung, Sortierung, Quellenberechtigungen, leere Ergebnisse und Fehler bleiben eindeutig geregelt.
- Der Server entscheidet Saisonfenster und liefert die Ablaufgrenze. Clients implementieren nur deren Auswertung, keine zweite Saisonberechnung.
- Mobile API-Client, Sync, Cache, MVU-Zustand, dynamische Home-Sektionen und vorhandene Mediennavigation sind erfasst. Die drei bisherigen Home-Bereiche bleiben erhalten.
- Erfolgreiches `200 []` ersetzt den saisonalen Cache durch leer. Abruffehler erhalten den Rohcache, verlängern dessen Sichtbarkeit aber nicht über das Serverdatum hinaus.
- Sechs konkrete mobile UI-Flüsse und vier konkrete Web-Browserflüsse sind mit Testinfrastruktur, Daten und Ausführung geplant. Fehlende oder nicht ausgeführte E2E bleiben offen und verhindern einen als vollständig getesteten Abschluss.
- Eigenständiger vollständiger Serverklon im schreibbaren Workspace und eigener Feature-Branch schützen den fremden Checkout und dessen Git-Metadaten. Backendpfad, Branch, Basiskommit, finaler Commit, Patch/Diff, Reviews und Tests werden als separates Ergebnis dokumentiert. Der Klon wird nicht als Gitlink oder Quellbaum in das Mobile-Repository aufgenommen.
- Offene Nutzerentscheidungen bestehen nicht. Konkrete Seed-/Selektordetails, DTO-Implementierung und Testuhranbindung sind im Implementierungsschritt anhand des tatsächlichen Codes auszuführen; sie stellen keine neue fachliche Freigabe dar.

## Befunde

Keine verbleibenden Planlücken. Diese Freigabe bewertet die Planung; erfolgreiche Implementierung und tatsächlich ausgeführte Tests sind noch durch die nachfolgenden Lifecycle-Schritte nachzuweisen.
