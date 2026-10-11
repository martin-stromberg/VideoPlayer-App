# Planprüfung — Direkte Wiedergabe aus Weiterschauen, Protokollversand und Playlist-Support (Issue #43)

## Ergebnis

**Status:** Projektplan lückenhaft

## Abgleich Anforderung ↔ Entwicklungsschritte

| Anforderung | Abgedeckt durch | Deckung |
|---|---|---|
| 1. Direkte Wiedergabe aus der Weiterschauen-Liste (ab Fortsetzungsposition; andere Listen weiterhin nur Detailansicht) | Schritt 1 | vollständig — Direktstart, Fortsetzungsposition (via `PlaybackSourceService`), Abgrenzung zu Favoriten/„Neu im Programm"/Quellen-Übersichten (plus Favoriten-Seite, Episodenliste, Kollektionen) sowie Fallback Detailansicht für nicht abspielbare Typen (`show`/`season`/`collection`) sind jeweils in den Akzeptanzkriterien gepinnt. |
| 2. Protokollierung aktivieren + „Protokoll senden"-Button (Mailentwurf mit Protokoll-Anhang an mstromberg84+heimkino@gmail.com) | Schritt 3 | vollständig — Einstellungs-Schalter, persistente Einstellung, Button nur bei aktivierter Protokollierung, Mailentwurf mit Anhang, Empfängeradresse exakt genannt, Nicht-Ziel „kein Versand ohne aktivierte Protokollierung" explizit als Akzeptanzkriterium. |
| 3. Playlist-Unterstützung (Umfang verbindlich „wie Server-UI"; Aufruf über das Menü) | Schritte 4, 5, 6 | **teilweise** — Menüeintrag, eigene + öffentliche Playlists, paginierte Detailansicht in Server-Reihenfolge, Wiedergabe inkl. Vor/Zurück/Auto-Fortsetzung sowie Verwaltung (CRUD, Einträge, Sortieren, Sortiermodus, Genres, Öffentlich-Flag, 409-Konflikt) sind abgedeckt. **Abweichung:** Die Cover-Verwaltung (Upload/Regenerieren/Vorschau, ggf. Löschen) wird in Schritt 6 als „Annahme" ausdrücklich ausgeschlossen, obwohl der Nutzer den Umfang verbindlich als „wie Server-UI" (voller Funktionsumfang der Serveranwendung) entschieden hat und die Cover-Endpunkte in der Plan-Übersicht selbst als Teil der Playlist-API aufgeführt sind. Zudem ist selbst die Cover-Anzeige in Schritt 4 nur konditional formuliert („sofern Cover angezeigt werden" / „sofern der Server sie liefert"). |
| 4. Wiedergabe- und Download-Reihenfolge bei Playlists | Schritt 5 (Folgevideo), Schritt 7 (Download) | vollständig — Folgevideo bei playlist-gebundenem Weiterschauen-Eintrag über die `play`-Endpunkte inkl. Erweiterung der `ContinueWatchingEntry`-DTO um `PlaylistId`/`PlaylistName`/`PlaylistEntryId` (Schritt 5, Akzeptanzkriterium 3); Download-Reihenfolge mit Verifizierung des Ist-Zustands und Korrektur der Einplanung (Schritt 7), wie von der Anforderung („falls noch nicht der Fall") vorgesehen. |
| 5. iOS-Serienansicht: fehlende Episodenliste, falsche Staffelreihenfolge, Zurückspringen der Auswahl | Schritt 8 | vollständig — alle drei Teilbefunde sind eigene Akzeptanzkriterien (Episodenliste sichtbar, aufsteigende Staffelreihenfolge, Auswahl bleibt bestehen), ergänzt um „kein Verhaltensrückbau auf anderen Plattformen"; plausible Ursachenhypothesen aus der Bestandsaufnahme sind dokumentiert, der iOS-Testlauf als Nachweis benannt. |
| 6. Kontextmenü in der Weiterschauen-Liste (2-Sekunden-Long-Press, „Ausblenden"/„Überspringen") | Schritt 2 | vollständig — 2-Sekunden-Long-Press, Menü mit beiden Aktionen, Nutzung der bereits vorhandenen API-Endpunkte `hide`/`skip`, Listen-Aktualisierung und Beschränkung auf die Weiterschauen-Liste (Nicht-Ziel) sind gepinnt. |

## Abhängigkeitsprüfung

- Alle Abhängigkeitsverweise zeigen ausschließlich auf frühere Schritte; der Graph ist zyklenfrei (1: –, 2: –, 3: –, 4: –, 5: 1+4, 6: 4, 7: 5, 8: –).
- Fachlich korrekt: Schritt 5 benötigt den Direktwiedergabe-Pfad aus Schritt 1 (Tap → Wiedergabe statt Detailseite) und die Playlist-Infrastruktur aus Schritt 4; Schritt 6 baut auf den in Schritt 4 angelegten Ansichten/Endpunkten auf; Schritt 7 setzt zutreffend den in Schritt 5 geschaffenen Playlist-Bezug an den Weiterschauen-Daten voraus.
- Schritte 1 und 2 berühren beide das Weiterschauen-Karussell ohne gegenseitige Abhängigkeit — fachlich zulässig, da unabhängige Features (Tap vs. Long-Press); die geplante Aufeinanderfolge ist im Plan begründet.
- Übersichtstabelle stimmt mit den Schritten überein (Titel, Abhängigkeiten, betroffene Bereiche konsistent; Bereichsangaben plausibel und deckungsgleich mit der Bestandsaufnahme).

## Fehlende oder unvollständige Punkte

1. **Cover-Verwaltung der Playlists trotz verbindlich geklärtem Umfang ausgeschlossen** (Schritt 6, Abschnitt „Annahmen"; vgl. Übersicht Z. 14, wo die Cover-Endpunkte `GET/DELETE .../cover`, `POST .../cover/upload|regenerate|preview` als Teil der Playlist-API gelistet sind). Die Nutzerentscheidung „Playlist-Umfang = wie Server-UI (voller Funktionsumfang)" ist verbindlich; der Plan setzt ihr eine begründete, aber nicht bestätigte Annahme entgegen („geringer praktischer Nutzen auf Mobilgeräten"). Damit ist Anf. 3 nur teilweise abgedeckt — entweder ist die Cover-Verwaltung zu ergänzen oder die Ausnahme ausdrücklich mit dem Nutzer zu klären.
2. **Zwischenzustand nach Schritt 1: Folgevideo/Auto-Fortsetzung für Serien-Episoden ohne Playlist-Bezug vorübergehend leer.** Da die Detailansicht bei Direktwiedergabe nicht mehr geladen wird, bleibt `PlayerState.UpcomingEpisodes` (wird bisher aus `model.MediaDetails` befüllt) leer — die bisher funktionierende Auto-Fortsetzung/Folge-Episoden-Auflösung für Weiterschauen-Serien entfällt, bis Schritt 5 die serienkontext-basierte Auflösung „unabhängig von der Detailseite" nachzieht. Der Plan benennt das in Schritt 5 korrekt, der Endzustand ist abgedeckt; als eigenständig lieferbarer Schritt erzeugt Schritt 1 jedoch ein Zwischen-Regression. Für die Lifecycle-Tauglichkeit wäre eine explizite Festlegung hilfreich (z. B. Akzeptanzkriterium oder Hinweis in Schritt 1, dass die Folgevideo-Auflösung ohne Detailseite erst mit Schritt 5 wiederhergestellt ist).

## Hinweise

- **Nicht-Ziele sauber eingehalten:** keine Server-Änderungen (reine Client-Umsetzung, Mock-Server-Erweiterung ist Testinfrastruktur), keine Direktwiedergabe in anderen Listen, Kontextmenü explizit auf die Weiterschauen-Liste beschränkt, kein Protokollversand ohne aktivierte Protokollierung. Der große Playlist-Umfang ist verbindlich entschieden und kein Scope-Verstoß.
- **Schrittqualität insgesamt gut:** Alle acht Schritte sind fachlich geschnitten (keine Klassenebenen-Zerlegung), eigenständig verständlich, mit konkreten Akzeptanzkriterien, dokumentierten Annahmen und plausiblen Fundstellen-Hinweisen aus der Bestandsaufnahme — als Lifecycle-Eingabe grundsätzlich geeignet.
- **Verifikationsstrategie konsistent:** Statt API-Exploration wird in den Playlist-Schritten gegen die lokale Instanz `http://localhost:83/` und das Server-Repo (`IPlaylistApiClient`, Blazor-UI) verifiziert — passt zur geklärten Ausgangslage.
- **Kleinigkeit:** Schritt 4 verweist die Cover-Frage noch an Schritt 6 („dort getroffene Entscheidung"); Schritt 6 entscheidet dann final gegen die Verwaltung — intern konsistent, fachlich aber eben die oben beanstandete Umfangsabweichung.
