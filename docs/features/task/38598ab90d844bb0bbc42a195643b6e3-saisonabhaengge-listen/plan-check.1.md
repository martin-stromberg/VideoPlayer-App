# Planprüfung: Saisonabhängige Genre-Listen

**Status: Plan lückenhaft**

## Prüfgrundlage

Geprüft wurden `requirement.md`, `inventory.md` einschließlich aller verlinkten Details, `plan.md` und lesend die Serverreferenz einschließlich `AGENTS.md`, `SeasonalGenreList.razor`, `InternalVideoWebPlayerClient` und bestehender Playwright-Testinfrastruktur. Maßgeblich sind die Nutzerantworten: Server zuerst erweitern, Web auf API umstellen, gleicher Medienumfang, serverseitige Saisonentscheidung, Cache-Ablauf durch Saisonende und leere Genres entfernen.

Das im Lifecycle genannte Modell `opus-4.8` ist nicht verfügbar. Diese unabhängige Prüfung erfolgte mit dem verfügbaren geerbten Modell. Keine Implementierung, Tests oder Commits wurden vorgenommen.

## Abgedeckte Anforderungen

- Authentifizierter gemeinsamer Endpunkt für Web und Mobile, Quellenberechtigungen, Medienumfang, Sammlungssonderfälle und Sortierung sind vorgesehen.
- Umsetzung beginnt mit Server/API und Web; Mobile folgt auf demselben Vertrag.
- Fehlerantwort und erfolgreiches `200 []` sind getrennt behandelt.
- Mobile Darstellung, Navigation, dynamische Sektionen und bestehende Home-Bereiche sind berücksichtigt.
- Isolierter vollständiger Serverklon innerhalb des schreibbaren Workspace schützt Dateien und Git-Metadaten des fremden Ursprungscheckouts. Eigenständiger Feature-Branch und getrennte Backend-/Mobile-Reviews sind vorgesehen.

## Lücken

### 1. Cache-Ablauf widerspricht der Nutzerentscheidung

`requirement.md` Akzeptanzkriterium 6 und mehrere Stellen des Plans erlauben Offlineanzeige unbegrenzt bis zum nächsten erfolgreichen Abruf. Der Vertrag verbietet ausdrücklich die Übermittlung eines Ablaufzeitpunkts und die Umsetzung eine lokale Ablaufprüfung. Damit bleibt beispielsweise eine Weihnachtssektion nach Saisonende offline sichtbar. Die Nutzerantwort 4 bestimmt das Saisonende als Ablaufdatum.

Erforderliche Korrektur:

- Server berechnet und liefert je Sektion ein eindeutig definiertes absolutes UTC-`expiresAt`/`validUntil`. Bei inklusivem letzten Saisontag ist dies beispielsweise der exklusive Folgetag um 00:00 UTC. Der Client berechnet keine Saisonfenster; er wertet nur das gelieferte Ablaufdatum aus.
- Offline-Start, Cache-Mapping und eine bereits geöffnete Startseite dürfen abgelaufene Sektionen nicht mehr anzeigen. Planung muss den Zeitpunkt der erneuten Prüfung und gegebenenfalls Refresh/Timer angeben, damit ein über Mitternacht geöffneter Bildschirm nicht unbegrenzt veraltet bleibt.
- Abruffehler bewahren die gespeicherten Daten, verlängern deren Sichtbarkeit aber nicht über `expiresAt`. Erfolgreiches `[]` ersetzt den Bestand weiterhin durch leer.
- Anforderung und API-/Cachevertrag konsistent ändern; Grenztests für unmittelbar vor und ab Ablauf sowie Offline-/Fehler- und UI-E2E-Szenario ergänzen. Kontrollierte Uhr/ablaufende Testdaten vermeiden datumsabhängige Tests.

### 2. Für die geänderte Webansicht fehlt der konkrete ausführbare E2E-Nachweis

Der Plan nennt Web-Contract-/Komponententests, aber keine genauen Web-Benutzerflüsse, zu erweiternde Testklasse, Testausführung oder Fixture. Die API-Migration ändert den authentifizierten Datenpfad einer Blazor-Home-Komponente. Mobile-Mock-E2E belegen diese Änderung nicht.

Erforderliche Korrektur:

- Konkreten Playwright-E2E-Test im vorhandenen Server-Testprojekt mit lokaler Kestrel-/SQLite-Fixture vorsehen, beispielsweise `SeasonalGenresE2ETests`, analog zu den vorhandenen E2E-Basen.
- Echten Benutzer anmelden, Startseite öffnen, aktive Genreüberschrift und Medienkarten sehen und Film-/Sammlungs-/Serienziel über die Karte öffnen. Reihenfolge und Watched-Funktion erhalten; inaktive, leere und nicht freigegebene Inhalte fehlen.
- Nachweisen, dass die Webansicht tatsächlich den neuen HTTP-Vertrag benutzt, einschließlich gültigem Token für den angemeldeten Benutzer über den bestehenden internen Client, und keine direkten saisonalen Datenbankabfragen mehr enthält. API-Nutzung etwa durch Request-/Servicebeobachtung und gesonderten Contract-Test absichern.
- Befehl für die gezielte E2E-Ausführung, Fixture/Seed und das verpflichtende erfolgreiche Ergebnis vor Featureabschluss festhalten. Vorhandene Playwright-Infrastruktur ist im Serverrepo vorhanden.

### 3. Verpflichtende Server-Abschlussprüfungen fehlen im ausführbaren Plan

Der Server-`AGENTS.md` definiert als Abschlussbedingungen Debug- und Release-Build sowie die vollständige Server-Testsuite. Der Plan enthält nur einen ausführbaren Mobile-E2E-Befehl und keine konkreten Serverbefehle. Die Serveränderung gehört jetzt ausdrücklich zum Auftrag.

Erforderliche Korrektur:

- Im isolierten Serverklon `dotnet build VideoPlayer.sln -c Debug`, `dotnet build VideoPlayer.sln -c Release` und `dotnet test VideoWebPlayer.Tests/VideoWebPlayer.Tests.csproj` einplanen; ergänzend gezielte neue Contract-/E2E-Tests und die Mobile-Tests mit Projektbefehlen nennen.
- Die unabhängige Backend-Abnahme und nötigen Handover-/Autorenregeln aus Server-`AGENTS.md` im dort jeweils anwendbaren Umfang berücksichtigen. Keine fremden aktiven Projektschritte bearbeiten.
- Ergebnisse je Checkout dokumentieren; fehlende oder nicht ausgeführte E2E gelten als offen. Ein isolierter Backend-Commit ist als gesondertes Lieferergebnis mit Branch, Basiskommit und Commit-ID nachvollziehbar zu halten; nicht versehentlich den verschachtelten Klon als Gitlink in das Mobile-Repository aufnehmen.

## Empfehlung

Plan und Anforderung anhand dieser drei Lücken überarbeiten und anschließend erneut unabhängig prüfen. Die fachlichen Entscheidungen sind bereits autorisiert; für diese Korrekturen ist keine weitere Nutzerentscheidung erforderlich.
