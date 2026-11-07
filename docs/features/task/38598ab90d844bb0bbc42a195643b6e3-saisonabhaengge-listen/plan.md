# Umsetzungsplan: Saisonabhängige Genre-Listen

## Ziel und verbindliche Entscheidungen

Server/API und beide Clients gehören zum Lieferumfang. Der Server erhält zuerst
einen authentifizierten saisonalen Home-Endpunkt; anschließend werden sowohl
die mobile App als auch die Webansicht auf diesen Vertrag umgestellt. Der Server
entscheidet, welche Genres gerade aktiv sind. Die Clients berechnen keine
Saisonfenster.

Die Webansicht bleibt fachlich gleich: derselbe Medienumfang, dieselbe
Sammlungsgruppierung, Sortierung und Reihenfolge wie bisher. Genre-Sektionen
ohne Medien entfallen. Das mobile Caching folgt den vorhandenen Regeln:
Transport-/Serverfehler behalten den letzten Cache, eine erfolgreiche leere
Antwort ersetzt ihn durch leer. Der Server liefert je Sektion ein absolutes UTC-`expiresAt` aus dem Ende
der konkreten Saison. Cache-Inhalte dürfen nur bei `nowUtc < expiresAt`
sichtbar sein, auch offline und bei Abruffehlern. Die Clients berechnen keine
Saisonfenster, sondern werten ausschließlich diesen Serverzeitpunkt aus.

## Lokaler Serverstand und sichere Arbeitsstrategie

Das Server/Webrepo liegt unter
`D:/Repositories/softwareschmiede/1f22729c-5f43-4f69-94c5-aee41f073cac`.
Der lesend ermittelte Branch ist
`task/issue-277-1f22729c5f434f6994c5aee41f073cac-tv-sender-platzhalter-clips-gr`.
Die lokale Arbeitskopie war bei der Bestandsaufnahme sauber. Relevante Regeln
stehen in dessen `AGENTS.md`; insbesondere keine Arbeit im gemeinsamen
Checkout und keine Commits auf dem Branch eines anderen aktiven Schritts.

Die Serverquelle liegt außerhalb des freigegebenen Schreibbereichs. Bei
Implementierungsfreigabe wird daher zunächst ein vollständiger, eigenständiger
Klon/Checkout innerhalb des freigegebenen Workspace erstellt, mit eigenem
Feature-Branch auf Basis des gelesenen Serverstands. Der ursprüngliche Pfad
bleibt unverändert. Keine `git worktree`-Operation am Quellrepo, da sie dessen
Git-Metadaten verändern würde. Der Backend-Diff und Commit entstehen nur im
isolierten Checkout; der Mobile-Diff entsteht auf dem aktuellen Feature-Branch.
Vor Umsetzung ist im isolierten Servercheckout dessen Branch/Status und
`AGENTS.md` nochmals zu prüfen. Ein möglicher Parallel-Branch ist nach dem
Server-Repository-Verfahren zu benennen und als eigener Review-/Merge-Schritt
zu behandeln.

## API-Vertrag (festgelegt für die Implementierung)

- `GET /api/home/seasonal-genres`, ohne Query-Parameter.
- Authentifizierung wie bei den bestehenden Benutzerlisten über
  `[BearerTokenCheck]`; nur Medien aus Quellen, die für den angemeldeten
  Benutzer freigegeben sind.
- Erfolgreiche Antwort `200 application/json`: Array in Reihenfolge der
  saisonalen Genre-Abfrage, jedes Element mit `genreId` (stabile Genre-ID),
  `name`, `expiresAt` und `entries`. `expiresAt` ist ein verpflichtender
  ISO-8601-UTC-Zeitpunkt mit `Z` und bezeichnet die exklusive Ablaufgrenze:
  00:00 UTC am Folgetag des letzten inklusiven Saisontags. Beispielsweise
  wird eine bis 31.10.2026 aktive Sektion mit `2026-11-01T00:00:00Z` geliefert.
- `entries` nutzt die vorhandene polymorphe Medienrepräsentation mit `$type`
  und deren IDs, Name, Poster-ID und Detailkontext. Unterstützt werden
  `movie`, `movieCollection` und `show`. Die mobile Abbildung verwendet
  vorhandene Mediennavigation und `api/pictures/{id}`. Nicht unterstützte
  Typen werden nicht stillschweigend als Film interpretiert.
- Eine Sammlung erscheint genau einmal. Sammlungen mit einem Film werden wie
  in der Webansicht durch diesen Film ersetzt. Filme ohne Sammlung erscheinen
  einzeln; Serien erscheinen als Serie. Je Genre wird absteigend nach
  `EndedAt`, `ReleaseDate`, `PremieredAt`, `CreatedAt`, danach aufsteigend nach
  Name sortiert. Genre-Sektionsreihenfolge entspricht der bestehenden
  serverseitigen saisonalen Genre-Abfrage.
- Es werden nur Genres mit beiden Saison-Grenzen und mit nicht leerem
  Medienergebnis zurückgegeben. Datumsfenster werden serverseitig aus
  `DateTime.UtcNow.Date` gebildet, jährlich vorgeschoben und mit inklusiven
  Grenzen `start <= heute <= end` ausgewertet. Ungültige/teilweise Grenzen
  ergeben keine aktive Sektion. Der Server berechnet `expiresAt` aus genau
  diesem konkreten Saisonfenster.
- Keine aktiven Sektionen ergibt `200 []`. Nicht authentifizierte Aufrufe
  folgen dem vorhandenen Bearer-Fehlerverhalten (401); unerwartete Fehler folgen
  der bestehenden API-Fehlerbehandlung. Clients unterscheiden HTTP-Fehler von
  erfolgreichem `[]`.

## Umsetzung in Reihenfolge

| Schritt | Bereich | Änderung und Abnahme |
|---|---|---|
| 1 | Server/Web isolierter Checkout | Authentifizierten Endpunkt und DTO nach obigem Vertrag ergänzen; Zugriffsfilter und Medienabfrage in einen wiederverwendbaren saisonalen Service legen. Serverseitige Grenz-, Berechtigungs-, Medien- und Sortiertests ergänzen. Ursprungsrepo unverändert lassen und Backend-Diff/Commit im isolierten Branch nachvollziehbar dokumentieren. |
| 2 | Webansicht | `SeasonalGenreList.razor` bezieht Genre-Sektionen über denselben API-Vertrag statt direkter Genre-/Medientabellenabfragen. Darstellung, Watched-Markierungen, Bilder, Reihenfolge, Sammlungsbehandlung und leere Sektionen bleiben erhalten. Web-Contract-/Komponententests und die nachstehenden Playwright-E2E ergänzen. Ablaufzeitpunkt wie unten beschrieben beobachten. |
| 3 | API-Client | Methode und DTOs für `GET api/home/seasonal-genres`; polymorphe Typen, `[]` und Fehlerstatus testen. Der Client übernimmt keine Saisonberechnung, wertet aber `expiresAt` aus. |
| 4 | Mobile Sync und Cache | Saisonantwort synchronisieren und unter stabilem Schlüssel `seasonal_genres` ablegen. Fehler bewahren Cache; erfolgreiche leere Antwort löscht ihn. Bestehender Cache-First-Start zeigt ausschließlich noch nicht abgelaufene Sektionen. Ablaufprüfung beim Laden/Mapping, bei Rückkehr/Vordergrund und über einen abbrechbaren Timer für den nächsten gelieferten Ablaufzeitpunkt auf geöffneter Startseite; bei Ablauf UI-Zustand neu abbilden. Fehler verlängern Sichtbarkeit nicht. |
| 5 | MVU und Home-UI | Dynamische geordnete Genre-Sektionen in `HomeLists`/`AppModel` laden; Sektionstitel, Medienkarten und Auswahl an bestehende Navigation anbinden. Leere Sektionen ausblenden; bestehende drei Bereiche unverändert lassen. |
| 6 | Mobile Tests/E2E | Cache-, Sync-, Mapping- und UI-Contract-Tests sowie die nachstehenden vollständigen Windows-E2E-Flüsse ergänzen. |
| 7 | Integration | Vertrag, Fehlerszenarien und IDs zwischen Server, Web und Mobile abgleichen; Backend- und Mobile-Diffs getrennt reviewen. Kein Backend-Code im Hauptrepo oder auf dem Server-Branch eines anderen Schritts. |

## Testplan

### Server/API und Web

- Saisonstart vor, am ersten Tag, innerhalb des Fensters, am letzten Tag und
  nach dem Fenster; beide Grenzen inklusiv, jährlicher Übertrag über
  Jahreswechsel, fehlende/teilweise Grenzen ausgeblendet.
- Quellenberechtigungen: erlaubte Medien erscheinen, nicht freigegebene
  Quellen und ihre Medien werden nicht offengelegt; nicht authentifiziert =
  401.
- Filme, Serien, Sammlungen mit mehreren Filmen, Singleton-Sammlungen und
  Filme ohne Sammlung; identische Gruppierung und Sortierung wie die bisherige
  Webdarstellung. Genre ohne Einträge wird aus der API-Antwort entfernt.
- Erfolgreiche leere Antwort ist `200 []`; unerwarteter Serverfehler bleibt
  Fehlerantwort. Web-Komponente rendert ausschließlich API-Ergebnis und erhält
  bestehende Karten-/Watched-Funktion.

### Mobile Tests

- API-Client deserialisiert alle unterstützten `$type`-Varianten und leeres
  Array; Fehlerstatus wird nicht als leere Antwort behandelt.
- Sync legt Sektionen geordnet ab; erfolgreicher leerer Abruf entfernt
  `seasonal_genres`; Transport-/HTTP-Fehler erhalten die gecachten Daten.
- Home-Mapping erhält Genre-ID/-Titel, Reihenfolge, Poster-ID, Typ,
  `expiresAt` und Navigationskontext; leere oder abgelaufene Sektionen
  erscheinen nicht. Fehlendes/ungültiges `expiresAt` wird nicht unbegrenzt
  angezeigt (fail closed). Mit injizierbarer Uhr/TimeProvider unmittelbar
  vor, exakt an und nach der Grenze testen, einschließlich Offline-/Fehlerfall
  und Timer-/Resume-Neuberechnung.

### Exakte Windows-E2E-Flüsse

Ausführung: `dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj`.
`MockVideoServer` wird um den Endpunkt und konfigurierbare Antworten ergänzt.
E2E belegt den mobilen API- und Benutzerfluss; Saisonberechnung und
Berechtigungen werden separat serverseitig getestet.

1. **Aktive Sektion öffnen:** Mock liefert „Halloween“ mit einem Film und
   Poster-ID; Benutzer startet App, sieht die Sektion, tippt die Filmkarte und
   landet auf der Detailseite exakt dieser Medien-ID.
2. **Reihenfolge und Sammlungsdarstellung:** Mock liefert zwei Genres in
   definierter Reihenfolge, davon Film und Sammlung; Benutzer sieht beide
   Sektionen in dieser Reihenfolge, öffnet einen Eintrag je Sektion und erhält
   jeweils das korrekte Detailziel.
3. **Erfolgreich leeres Ergebnis:** nach erfolgreichem Abruf mit Sektion liefert
   der Mock `200 []`; Benutzer aktualisiert die Startseite und sieht keine
   saisonale Sektion mehr.
4. **Fehler und Offline-Cache:** zuerst wird „Weihnachten“ erfolgreich
   synchronisiert, danach schlägt der nächste Abruf mit HTTP-Fehler fehl;
   Benutzer startet offline bzw. nach Fehler und sieht den letzten Cache nur
   vor dessen `expiresAt`. Nach anschließend erfolgreichem `200 []`
   verschwindet die Sektion.
5. **Keine leeren Genres:** Mock-Antwort mit keiner Sektion (Serververtrag
   filtert leere Genres); Benutzer sieht keine leere Genre-Karte/kein leeres
   Karussell. Serverseitiger Integrationstest verifiziert, dass leere Genres
   gar nicht in der API-Antwort stehen.

## Ergänzender Ablauf- und Web-E2E-Nachweis

### Ablauf auf einer geöffneten Startseite

Mobile und Web planen nach dem Rendern jeweils einen abbrechbaren Timer auf
den nächsten gelieferten `expiresAt`-Zeitpunkt. Dessen Auslösung entfernt
abgelaufene Sektionen aus dem sichtbaren Zustand und plant den nächsten
Termin. Bei Navigation/Dispose werden Timer beendet; bei erneutem Anzeigen
und App-Resume wird sofort neu gefiltert. Die Ablaufprüfung ist unabhängig
von einem erfolgreichen Netzabruf. Ein optionaler Refresh darf die Entfernung
nicht verzögern. Vor `expiresAt` erhalten Abruffehler den vorhandenen Cache;
ab `expiresAt` kann der Rohcache gespeichert bleiben, wird aber nicht mehr
angezeigt. Erfolgreiches `[]` ersetzt ihn unabhängig davon durch leer.

**Mobile E2E 6 — Ablauf ohne Netz und bei offener App:** `SeasonalGenresTests`
verwendet den vorhandenen `MockVideoServer`, `AppFixture`/`AppInstance`,
`UiDriver` und `DevApiClient`. Eine feste Testuhr bzw. eng begrenzte,
ausschließlich im Debug-/Testprofil aktivierte Zeitsteuerung erlaubt einen
bestimmten UTC-Zeitpunkt; derselbe produktive Ablaufpfad bleibt aktiv.
Mock liefert eine Sektion mit diesem Zeitpunkt plus einer kontrollierten
Frist als `expiresAt`. Benutzer sieht die Sektion, Server wird auf Fehler
umgestellt, Testuhr springt über die Grenze. Ohne Navigation und ohne
erfolgreichen Abruf verschwindet Überschrift samt Karten aus dem sichtbaren
UI-Baum. Neustart desselben Offlineprofils nach Ablauf zeigt sie ebenfalls
nicht. Ergänzend wird mit der Uhr unmittelbar vor und exakt an der Grenze
geprüft. Keine mehrtägigen Wartezeiten oder Kalenderabhängigkeit.

### Ausführbare Web-Playwright-E2E

Im isolierten Serverklon wird `VideoWebPlayer.Tests/SeasonalGenresE2ETests.cs`
mit eigener Kestrel-/SQLite-Fixture ergänzt, nach dem vorhandenen Muster
`Helpers/MediaBoxContextMenuE2ETestBase`: `WebApplicationFactory<Program>` in
`Testing`, `UseKestrel()`, zufälliger Loopback-Port, eigenes SQLite-DB-File,
Seed und Headless-Chromium. Playwright muss installiert und tatsächlich
ausgeführt werden; fehlender Browser/übersprungene Tests zählen als offen.
Keine Abhängigkeit von produktiven Accounts oder externen Servern.

Seed: ein echter Identity-Testbenutzer mit Passwort, eine freigegebene und
eine gesperrte Quelle, zwei aktive saisonale Genres in definierter
Abfragereihenfolge, ein inaktives sowie ein aktives leeres Genre. Aktive
Genres enthalten einen Film, eine Mehrfilm-Sammlung, eine Singleton-Sammlung
und eine Serie mit eindeutigem Titel/ID; ein Film ist als gesehen markiert.
Eine injizierbare Serveruhr fixiert die Saisonentscheidung und erlaubt den
Ablaufgrenztest; die Webkomponente nutzt dieselbe testbare Uhr für die
Auswertung des vom API gelieferten `expiresAt`.

1. **Anmeldung, gemeinsame API und sichtbare Inhalte:** Benutzer öffnet
   `/Account/Login?ReturnUrl=%2F`, meldet sich über `#email`, `#password` und
   „Log in“ an. Nach interaktivem Home-Render sind beide aktiven Überschriften
   und erwarteten Medienkarten in der Serverreihenfolge sichtbar; inaktives,
   leeres Genre und gesperrte Medien fehlen. Die Gesehen-Markierung des
   vorbereiteten Films ist sichtbar.
2. **Medienziele:** Benutzer öffnet nacheinander Film, Mehrfilm-Sammlung und
   Serie über deren saisonale Karten (jeweils zurück zur Startseite). URL und
   Detailüberschrift/ID entsprechen genau dem Seed. Singleton-Sammlung öffnet
   den enthaltenen Film. Keine Karte wird auf einen fremden Medientyp geroutet.
3. **API-Pfad statt EF in der Komponente:** Eine testseitige
   HTTP-Middleware/Request-Beobachtung zeichnet den tatsächlichen
   `GET /api/home/seasonal-genres` auf, auch wenn der Request bei Blazor Server
   nicht im Browsernetzwerk erscheint. Assert auf erfolgreichen Aufruf und
   authentifizierte Benutzer-ID; Tokeninhalt wird nicht geloggt. Der bestehende
   interne API-Client übergibt den gültigen Token des angemeldeten Benutzers.
   Ein separater Contract-Test prüft 401 ohne Token und den Quellfilter.
   Ein Architektur-/Komponenten-Contract-Test und das unabhängige Code-Review
   sichern ab, dass `SeasonalGenreList.razor` keine direkten saisonalen
   EF-/GenreService-Abfragen mehr enthält. Ein API-Fehler wird nicht durch
   einen direkten DB-Fallback verdeckt.
4. **Ablauf im geöffneten Home:** Benutzer bleibt auf der Startseite; die
   kontrollierte Testuhr wird exakt auf das gelieferte `expiresAt` gestellt.
   Nach dem produktiven Timer-/Ablaufpfad fehlen Überschrift und Karten, ohne
   manuelle Navigation. Reload nach Ablauf zeigt die Sektion ebenfalls nicht.

Gezielter Pflichtlauf im Serverklon:

```powershell
dotnet test VideoWebPlayer.Tests/VideoWebPlayer.Tests.csproj --filter FullyQualifiedName~SeasonalGenresE2ETests
```

Alle vier Browserflüsse müssen erfolgreich sein. Reine Contract- oder
Komponententests ersetzen diesen Nachweis nicht.

## Verbindliche Abschlussprüfungen und Lieferergebnisse

Im isolierten Serverklon sind nach Implementierung und unabhängiger Prüfung
mindestens diese Befehle erfolgreich auszuführen und die Ausgaben im
checkoutbezogenen Testbericht zu dokumentieren:

```powershell
dotnet build VideoPlayer.sln -c Debug
dotnet build VideoPlayer.sln -c Release
dotnet test VideoWebPlayer.Tests/VideoWebPlayer.Tests.csproj
```

Dazu die gezielten neuen API-/Ablauf-/Contract-Tests und der obige Web-E2E-Lauf.
Ein einmaliger Playwright-Ausfall wird gezielt mehrfach wiederholt und offen
berichtet; Tests werden nicht deaktiviert oder abgeschwächt.

Im Mobile-Checkout:

```powershell
dotnet test VideoPlayer.Maui.Tests/VideoPlayer.Maui.Tests.csproj
dotnet test VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj
```

Die Windows-E2E-Suite baut den Debug-Windows-MAUI-Client selbst. Alle sechs
geplanten saisonalen Benutzerflüsse müssen vorhanden und erfolgreich sein;
fehlende, übersprungene oder nicht ausgeführte UI-E2E gelten als offene Tests.

Server-`AGENTS.md` gilt im isolierten Klon: eigener Branch/Arbeitsbereich,
kein fremder aktiver Projektschritt, Bearbeiter- und Handoverstatus sowie
Basiskommit vor Codearbeit dokumentieren. Soweit Projektartefakte anwendbar
sind, wird ein eigener saisonaler Schritt/Handover angelegt und der Bearbeiter
vor Implementierung separat committed. Ein unabhängiger Prüfer erhält einen
eigenen Checkout auf dem Reviewstand, prüft echten Code und führt die
Abnahmeprüfungen selbst aus; Bericht mit Status, Abweichungen und Hinweisen
nach Serverkonvention. Bei Tests mit Schutzfunktion wird die abgesicherte
Stelle kurz im Prüfercheckout verändert, der erwartete Testfehler belegt und
die Mutation wieder vollständig zurückgenommen. Vor Übergabe/Pause werden
Handover und Bearbeiterstatus aktualisiert. Servercommits verwenden deutschen
Text und agentenspezifischen Autor über `GIT_AUTHOR_NAME`/
`GIT_COMMITTER_NAME` pro Aufruf; Bot-E-Mail und Co-Author bleiben erhalten.
Keine Datenbankschemaänderung ist geplant; sollte eine nötig werden, gelten
Migration-, Backup-Kompatibilitäts- und SQLite-Regeln aus `AGENTS.md`.

Backend- und Mobile-Lieferung werden getrennt ausgewiesen: isolierter
Backendpfad, Branch, Basiskommit, finaler Commit und Backend-Diff/Patch sowie
Tests und Reviewbericht werden nachvollziehbar dokumentiert. Der Backendklon
wird lokal vom Mobile-Tracking ausgeschlossen und niemals als Gitlink oder
vollständiger Quellbaum in den Mobile-Commit aufgenommen. Der ursprüngliche
Servercheckout einschließlich seiner `.git`-Metadaten bleibt unverändert.
Das verfügbare geerbte Modell ersetzt für diese Planrevision das nicht
verfügbare Skill-Modell `opus-4.8` (Effort high).

## Offene Punkte

Keine fachlichen oder technischen Entscheidungen offen. Der Serverbranch wird
bei tatsächlichem Start der Implementierung im isolierten Checkout erneut
geprüft, weil der ermittelte lokale Feature-Branch bereits einem anderen
Serverthema zugeordnet ist; daraus folgt keine Änderung am Serverrepo im
aktuellen Planungsschritt.


