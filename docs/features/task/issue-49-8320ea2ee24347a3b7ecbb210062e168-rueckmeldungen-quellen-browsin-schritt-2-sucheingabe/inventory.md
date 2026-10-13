# Bestandsaufnahme: Sucheingabe der Quellen-Übersichtsseite übersteht Modell-Aktualisierungen

Analysiert wurde der Quellen-Browsing-Bereich (`SourceOverviewPage`,
`SourceBrowserState`, `AppUpdate`, zugehörige Tests und E2E-Infrastruktur)
bezogen auf die Anforderung, dass eine unbestätigte Sucheingabe nicht mehr
durch Modell-Aktualisierungen überschrieben werden darf.

## Zusammenfassung

- `SourceOverviewPage.UpdateUi` (`VideoPlayer.Maui/Pages/SourceOverviewPage.xaml.cs`,
  Zeilen 88–102) spiegelt bei **jedem** `ModelChanged`-Tick
  `browser.SearchText` nach `MediaSearchBar.Text`, sobald Feldinhalt und
  Modellwert ungleich sind. Da `SourceBrowserState.SearchText` sich erst beim
  Abschicken ändert (`UpdateSourceSearchSubmitted`, `AppUpdate.cs:1696`),
  wird jede unbestätigte Eingabe durch den nächsten beliebigen Tick
  (Sync, Fortschritt, Netzwerk, Ladezustände, Genre-/Eintragsantworten)
  mit dem alten Suchtext überschrieben — der gemeldete Defekt.
- Es gibt **keinen** eigenen Speicher für die unbestätigte Eingabe — weder
  im `SourceBrowserState` noch seitenlokal.
- Das Submit-Verhalten ist bereits korrekt: `OnSearchButtonPressed`
  dispatcht `SourceSearchSubmittedMessage(MediaSearchBar.Text)`;
  `UpdateSourceSearchSubmitted` trimmt, ignoriert unveränderte Texte und
  lädt über `ReloadBrowserItems` ab Seite 0 neu — genau das geforderte
  Suchmuster.
- Vergleichbares Projekt-Muster: `PlaylistFormPanel` schreibt Feldinhalte
  nur beim Öffnen/externer Änderung ins Formular, nie pro Tick — der
  Entwurf liegt seiten-/widgetnah, Tippen wird nicht zurückgespiegelt.
- `MediaSearchBar` besitzt derzeit **keine** `AutomationId` — für
  E2E-/DevApi-Zugriff (Setzen per `POST /set-text`, Auslesen über
  `GET /tree`, `Text`-Knoten) muss sie ergänzt werden.
- `DevApiModelSnapshot` exponiert den `SearchText` des Browsers nicht —
  für E2E-Nachweise des aktiven Filters ist eine Ergänzung sinnvoll.
- `MockVideoServer` unterstützt den `search`-Parameter bereits und
  protokolliert alle `api/items`-Aufrufe in `SourceItemRequests`.

Test-Ausgangszustand: `dotnet test VideoWebPlayer.Maui.Tests` —
**1316/1316 bestanden**, Exit 0 (Nachweis:
[inventory/tests.md](inventory/tests.md)). Die FlaUI-E2E-Suite
(`VideoPlayer.E2E.Tests`) wurde im Ausgangslauf nicht ausgeführt — sie baut
und startet die reale Windows-App (Ausführbarkeit wird in Schritt 10
geprüft).

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
