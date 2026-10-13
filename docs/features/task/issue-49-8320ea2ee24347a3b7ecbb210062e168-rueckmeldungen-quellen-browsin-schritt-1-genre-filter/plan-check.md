# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Diagnose des Fehlens dokumentieren (leere Serverantwort / Ladefehler / fehlende Leiste) | Abschnitt „Diagnoseergebnis" mit Kandidaten-Abgleich; dokumentiert in `requirement.md`, `plan.md` und später `docs/help/` | — (Dokumentationspflicht, kein Test) | Abgedeckt |
| Server liefert Genres → Leiste sichtbar mit „Alle" + Quellen-Genres | `ShowGenreFilter` (`Genres.Any(g => g.Id > 0)`) + `IsVisible`-Binding auf `GenresCollection`; Ladepfad bereits vorhanden | Unit: `SourceGenresLoaded_ServerGenres_ShowsGenreFilter`; E2E: `GenreBar_VisibleAndFilters_ResetsOnAll` | Abgedeckt |
| Genre-Auswahl lädt Trefferliste gefiltert ab Seite 0 | Bestehender Pfad `UpdateSourceGenreSelected` → `ReloadBrowserItems` (`genreId`, `Page 0`) bleibt | Unit: bestehender `GenreSelected_ReloadsFromFirstPage_WithGenreFilter`; E2E: `genreId`-Assertion über `SourceItemRequests` | Abgedeckt |
| „Alle" hebt Filter auf | Bestehend (`SelectedGenreId = 0` → `genreId` nicht gesendet) | E2E: Teil desselben Szenarios (Aufruf ohne `genreId`) | Abgedeckt |
| Keine neue Bedienform — Chip-Leiste bleibt Soll-Form | Plan ändert nur Sichtbarkeit/AutomationIds, kein neues UI | — | Abgedeckt |
| Server und API-Verträge unverändert | Änderungen nur clientseitig (`VideoPlayer.Mvu`, `VideoPlayer.Maui`, E2E-Infra); Mock-Server ist Testinfra, kein Vertrag | — | Abgedeckt |
| Leere Server-Genreliste → keine funktionslose „Alle"-Zeile (verbindliche Projektklärung) | `ShowGenreFilter == false` bei `[Alle]` → Leiste ausgeblendet | Unit: `SourceGenresLoaded_OnlyAllEntry_HidesGenreFilter`, `LoadGenres_EmptyServerList_ReturnsOnlyAllEntry`; E2E: `GenreBar_Hidden_WhenServerHasNoGenres` | Abgedeckt |
| Serverseitige Ursache dokumentieren | Diagnoseabschnitt + `docs/help/` + Release Notes im Plan vorgesehen | — | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Quelle öffnen → Genre-Leiste → Genre wählen → gefilterter Reload → „Alle" | `SourceBrowsingTests.GenreBar_VisibleAndFilters_ResetsOnAll` | Abgedeckt (Voraussetzungen `MockVideoServer`-Routen + `DevApiModelSnapshot`-Felder sind als Umsetzungsschritte 3–4 im Plan) |
| Sichtbarkeitsregel „keine funktionslose Leiste" | `SourceBrowsingTests.GenreBar_Hidden_WhenServerHasNoGenres` | Abgedeckt |

## Hinweise

- Die Diagnose-Unterscheidung „leere Serverantwort" vs. „Ladefehler" ist ohne laufenden Server nicht beweisbar; der Plan dokumentiert beide Pfade und behandelt sie einheitlich — das ist die vom Auftrag geforderte Vorgehensweise.
- E2E-Ausführbarkeit ist eine dokumentierte Annahme (Workload `maui-windows` installiert); falls der App-Build/Start in dieser Umgebung scheitert, ist die Dokumentations-Fallback-Regel des Auftrags anzuwenden und der Testlauf in `test-results.md` als nicht ausführbar zu vermerken.
- `RequestedPaths` des Mock-Servers speichert Pfade ohne Query — für die `genreId`-Assertion ist die geplante `SourceItemRequests`-Aufzeichnung (roher Pfad inkl. Query) notwendig; im Plan enthalten.
