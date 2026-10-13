# Übersetzte Anforderung: Nutzerrückmeldungen Quellen-Browsing, Weiterschauen-Navigation, Wiedergabe-Ende

Branch: `task/issue-49-8320ea2ee24347a3b7ecbb210062e168-rueckmeldungen-quellen-browsin`
Issue: #49 · Bezugsstand: aktuelle Version nach PR #47

## Ziel

Fünf fachlich unabhängige Mängel der .NET-MAUI-App `VideoPlayer.Maui` (MVU-Architektur in `VideoPlayer.Mvu`) sind zu beheben:

1. In der Serien-Übersicht des Quellen-Browsings (`SourceOverviewPage`) steht kein Genre-Filter zur Verfügung.
2. Das Suchfeld der `SourceOverviewPage` leert die Benutzereingabe bei jedem Tastenschlag.
3. Die Trefferliste des Quellen-Browsings springt beim seitenweisen Nachladen an den Anfang.
4. Ein Tipp auf einen Weiterschauen-Eintrag öffnet direkt den Player statt der Titelansicht — Parität zum Verhalten der Serveranwendung ist herzustellen.
5. Die Videoende-Erkennung ist unzuverlässig: Das Folgevideo startet mitten in der laufenden Wiedergabe.

Die Punkte sind laut Anforderung unabhängig und können getrennt bearbeitet/aufgeteilt werden.

## Fachliche Anforderungen

### FA-1: Genre-Filter in der Serien-Übersicht (Quellen-Browsing)

**Ist-Verhalten (belegt):** In der Übersicht einer Serien-Quelle steht dem Anwender kein Genre-Filter zur Verfügung. Die Seite enthält die Leiste zwar in XAML (`GenresCollection`, gebunden an `Model.SourceBrowser.Genres`, `VideoPlayer.Maui/Pages/SourceOverviewPage.xaml:24-58`) und der Ladepfad existiert (`LoadSourceGenresEffect` → `ShellAppEffectHandler.LoadSourceGenresAsync` → `SourceBrowserService.LoadGenresAsync` → `GET api/SourceGenres/{id}`); die `GenresCollection` hat allerdings keine `IsVisible`-Bindung — bei einer leeren `Genres`-Liste rendert sie unsichtbar leer. `SourceBrowserService.LoadGenresAsync` (`VideoPlayer.Mvu/SourceBrowserService.cs:74-99`) stellt der Serverliste stets den Eintrag „Alle" (`SourceBrowserService.AllGenresName`, Id = 0) voran; schlägt der Aufruf fehl, liefert `ShellAppEffectHandler.LoadSourceGenresAsync` (`ShellAppEffectHandler.cs:816-833`) als Fallback ebenfalls nur „Alle". Die Ursache des beobachteten Fehlens ist in der Anforderung selbst als unklar markiert (leere Serverliste, Ladefehler oder fehlende Leiste im Auslieferungsstand).

**Soll-Verhalten (belegt):** Dem Anwender steht in der Übersicht der Serien ein Genre-Filter zur Verfügung.

**Annahmen:**
- Die Soll-Form ist die vorhandene Bedienform: die horizontale Chip-Leiste mit „Alle"-Eintrag, deren Auswahl über `SourceGenreSelectedMessage` den bestehenden Filterpfad (`UpdateSourceGenreSelected` → `LoadSourceItemsEffect` mit `GenreId`) anstößt — es wird keine neue UI-Form gefordert.
- „Genre-Filter zur Verfügung" bedeutet: Bei vom Endpunkt gelieferten Genres ist die Leiste sichtbar und die Auswahl filtert die Trefferliste; ob eine Quelle ganz ohne serverseitige Genres eine ausgeblendete oder eine „Alle"-nur-Leiste zeigen soll, ist nicht spezifiziert.

### FA-2: Suche — Eingabe darf nicht durch Modell-Ticks überschrieben werden

**Ist-Verhalten (belegt):** Wird ein Suchbegriff in `MediaSearchBar` eingegeben, wird das Feld bei jedem `ModelChanged`-Tick mit `browser.SearchText` überschrieben: `SourceOverviewPage.UpdateUi` (`SourceOverviewPage.xaml.cs:96-101`) spiegelt den Modell-Suchtext zurück ins Feld, der Guard prüft nur Ungleichheit. `browser.SearchText` ändert sich erst beim Abschicken (`SourceSearchSubmittedMessage`, `UpdateSourceSearchSubmitted`, `AppUpdate.cs:1696-1706`) — jede unbestätigte Eingabe geht beim nächsten Modell-Tick (Sync, Fortschritt, Netzwerk-Events) verloren; eine Suche ist praktisch nicht auslösbar.

**Soll-Verhalten (belegt):** Die Eingabe ins Suchfeld bleibt während des Tippens erhalten; eine Suche ist auslösbar.

**Annahmen:**
- Das bestehende Suchmuster bleibt fachlich unverändert: Suche wird über die Return-/Suchen-Taste (`SearchButtonPressed` → `SourceSearchSubmittedMessage`) ausgelöst; keine Live-Suche pro Tastenschlag gefordert.
- Der bereits abgeschickte Modell-Suchtext darf weiterhin ins Feld zurückgespiegelt werden (Filter bleibt sichtbar aktiv, z. B. bei Rückkehr auf die Seite) — kritisch ist nur, dass die unbestätigte Benutzereingabe nicht verworfen wird.

### FA-3: Scrollposition beim seitenweisen Nachladen erhalten

**Ist-Verhalten (belegt):** Beim seitenweisen Nachladen springt die Ansicht an den Anfang der Liste. `ItemsCollection` (`SourceOverviewPage.xaml:87-93`, `RemainingItemsThreshold="6"`) löst `SourceItemsNextPageRequestedMessage` aus; `UpdateSourceItemsLoaded` (`AppUpdate.cs:1629-1631`) ersetzt `Items` beim Anhängen durch eine neue Liste (`browser.Items.Concat(message.Items).ToList()`) — die gebundene `ItemsSource` wird als Ganzes ausgetauscht, die Scrollposition geht beim Ersetzen/Relayout verloren.

**Soll-Verhalten (belegt):** Beim seitenweisen Nachladen der gefundenen Serien bleibt die Ansicht an der bisherigen Position; benutzerfreundliches Durchscrollen ist möglich (kein Sprung an den Anfang).

**Annahmen:**
- Die Anforderung beschreibt nur das Ergebnis (Position bleibt); die technische Form (z. B. inkrementelle Erweiterung der ItemsSource statt Austausch) ist frei.
- Das gewünschte Verhalten gilt gleichermaßen für die ungefilterte Erstliste und für Such-/Genre-Treffer (derselbe `ItemsCollection`-Pfad).

### FA-4: Weiterschauen-Liste öffnet die Titelansicht (Parität zur Serveranwendung)

**Ist-Verhalten (belegt):** Ein Tipp auf einen Eintrag der Weiterschauen-Liste (`HomePage`, `OnContinueWatchingItemTapped` → `ContinueWatchingItemSelectedMessage`) startet bei abspielbaren Medientypen (`movie`/`tvshowepisode`) direkt die Wiedergabe ohne Detailseite: `UpdateContinueWatchingItemSelected` (`AppUpdate.cs:1357-1401`) ruft `StartPlayback` auf (`PlayerState` belegen + `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect`, `AppUpdate.cs:3596-3612`). Diese Direktwiedergabe wurde bewusst eingeführt (Issue #43, Schritt 1). Nach dem Schließen des Players landet der Anwender auf der Startliste; Titelinformationen sind nur über den Umweg durch die Quellenansicht einsehbar. Nicht direkt abspielbare Typen (Serie, Staffel, Kollektion) navigieren bereits heute über `NavigateToMediaDetailsEffect` zur Detailseite.

**Soll-Verhalten (belegt):** Wie in der Serveranwendung öffnet ein Tipp auf einen Weiterschauen-Eintrag die Serien-/Filmansicht (Detailseite des Titels) mit Wiedergabe — Ziel ist die Parität zum Server-Verhalten: „Player schließen" endet auf der Detailseite statt auf der Startliste, und die Titelinformationen bleiben einsehbar.

**Annahmen:**
- Die Ziel-Detailseite ist die vorhandene `MediaDetailsPage` (Route `mediadetails`, `NavigateToMediaDetailsEffect` mit den Kontext-Hints `ShowId`/`SeasonId`/`EpisodeId`, die `HomeMediaItem` bereits trägt).
- „Detailansicht mit Player" wird als automatisch startende Wiedergabe über der geöffneten Detailseite interpretiert (analog zum bestehenden Pfad `PlayRequestedMessage` → `StartPlayback`, bei dem die `PlayerPage` auf die Detailseite geschoben wird und „Player schließen" dorthin zurückkehrt). Ob die Wiedergabe beim Tipp weiterhin unmittelbar startet oder die Detailseite erst geöffnet und die Wiedergabe manuell ausgelöst wird, ist aus der Anforderung nicht eindeutig ableitbar (siehe Offene Fragen).
- Der Serienkontext für die Folgevideo-Funktion (`UpcomingEpisodes`/`LoadUpcomingEpisodesEffect`, derzeit separat bei der Direktwiedergabe nachgeladen, `AppUpdate.cs:1390-1398`) bleibt als Funktion erhalten.

### FA-5: Zuverlässige Videoende-Erkennung (kein Auto-Vorlauf mitten im Video)

**Ist-Verhalten (belegt):** `PlayerPage.OnMediaEnded` (`PlayerPage.xaml.cs:417-424`) dispatcht `PlaybackEndedMessage` ungeprüft, sobald das `MediaElement` `MediaEnded` feuert. `UpdatePlaybackEnded` (`AppUpdate.cs:3845-3906`) markiert das Medium als angesehen, meldet die Endposition und startet bei aktivierter Auto-Fortsetzung (`AppSettings.AutoResumeEnabled`) das Folgevideo (`AdvanceToNextEpisode` bzw. `AdvancePlaylistEntry` im Playlist-Kontext). Feuert `MediaEnded` spurious (Stream-Abbruch, Formatwechsel, Seek), wird der Auto-Vorlauf trotzdem ausgelöst — das Folgevideo startet mitten in der Wiedergabe, und das Medium wird fälschlich als vollständig angesehen gemeldet. Ein Plausibilitätscheck (Position vs. Duration) oder ein Debounce fehlt.

**Soll-Verhalten (belegt):** Die Erkennung des Videoendes funktioniert zuverlässig: Das Folgevideo wird nicht mitten in der Wiedergabe abgespielt.

**Annahmen:**
- „Zuverlässig" heißt konkret: Ein `MediaEnded`-Ereignis, das nicht dem tatsächlichen Wiedergabeende entspricht, löst weder Auto-Vorlauf noch die Angesehen-Markierung/End-Fortschrittsmeldung aus (beide Folgen hängen an derselben `PlaybackEndedMessage`).
- Die vom Hinweis genannten Ansätze (Plausibilitätscheck Position vs. Duration oder Debounce) sind Alternativen; die konkrete Schwelle/Umsetzung ist nicht vorgegeben.
- Das reguläre Ende-Verhalten (Angesehen-Markierung, Fortschrittsmeldung, Auto-Fortsetzung bei `AutoResumeEnabled`) bleibt bestehen.

## Betroffene Bereiche

- **`VideoPlayer.Maui/Pages/SourceOverviewPage.xaml` + `.xaml.cs`** (FA-1, FA-2, FA-3): `GenresCollection`, `MediaSearchBar`/`UpdateUi`-Spiegelung, `ItemsCollection` mit `RemainingItemsThreshold`.
- **`VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`** (FA-5): `OnMediaEnded`-Handler; Bezugspunkte `ReportPosition`/`PlaybackProgressMessage` (Position/Duration für einen Plausibilitätscheck).
- **`VideoPlayer.Maui/Pages/HomePage.xaml` + `.xaml.cs`** (FA-4): Weiterschauen-Carousel (`ContinueWatchingCarousel`), `OnContinueWatchingItemTapped`.
- **`VideoPlayer.Maui/Pages/MediaDetailsPage.xaml` + `.xaml.cs`** (FA-4): Ziel-Detailansicht; bestehender Wiedergabe-Einstieg `PlayRequestedMessage` (`OnPlayClicked`/`OnEpisodePlayClicked`).
- **`VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`** (FA-1, FA-4): `LoadSourceGenresAsync` (Fallback „Alle"), `NavigateToMediaDetailsAsync`, `NavigateToPlayerAsync`.
- **`VideoPlayer.Mvu`** (alle Punkte):
  - `AppUpdate.cs`: `UpdateSourceGenresLoaded`, `UpdateSourceSearchSubmitted`, `UpdateSourceItemsNextPageRequested`, `UpdateSourceItemsLoaded` (Append-Pfad), `UpdateContinueWatchingItemSelected`, `UpdatePlayRequested`/`StartPlayback`, `UpdatePlaybackEnded`/`AdvanceToNextEpisode`/`AdvancePlaylistEntry`.
  - `AppMessage.cs`/`AppEffect.cs`: bestehende Nachrichten/Effekte; je nach Lösung neue Nachrichten/Effekte (Annahme).
  - `SourceBrowserState.cs`, `SourceBrowserService.cs` (`LoadGenresAsync`, `AllGenresName`), `SourceGenreItem`, `SourceItemsPage`, `HomeMediaItem`, `MediaDetailsState`/`MediaDetailsView`, `PlayerState`.
- **`VideoPlayer.ApiClient`** (nur Diagnose FA-1): `GetSourceGenresAsync` → `GET api/SourceGenres/{id}` — keine Änderung des Clients gefordert.
- **Tests `VideoWebPlayer.Maui.Tests`**: `AppUpdateTests_Sources`/`AppUpdateTests_Player`, `SourcesContractTests`, `SourceBrowserServiceTests`, UI-Vertragstests der betroffenen Seiten (Annahme: Anpassung/Ergänzung der bestehenden Test-Muster pro Behobenem Punkt).

## Abläufe

**FA-1 (Ist):** `SourceBrowserOpenedMessage` → `LoadSourceGenresEffect` → `LoadSourceGenresAsync` → `api/SourceGenres/{id}` → `SourceGenresLoadedMessage` → `SourceBrowser.Genres`; Fehler → Fallback-Liste nur „Alle". **Soll:** Genre-Auswahl sichtbar und funktional — erst nach Diagnose ist der konkrete Fehlerpfad (leere Serverantwort vs. Ladefehler vs. fehlende Leiste) festzustellen und zu beheben.

**FA-2 (Ist):** Tastenschlag ändert `MediaSearchBar.Text` → nächster `ModelChanged`-Tick → `UpdateUi` schreibt `browser.SearchText` (noch der alte abgeschickte/leere Wert) zurück → Eingabe weg. **Soll:** Unbestätigte Eingabe bleibt bestehen; erst `SourceSearchSubmittedMessage` (SearchButton) übernimmt den Text in den Modell-Suchtext und löst `LoadSourceItemsEffect` (Seite 0, `Append: false`) aus.

**FA-3 (Ist):** Scrollnähe ans Listenende → `OnRemainingItemsThresholdReached` → `SourceItemsNextPageRequestedMessage` → `IsLoadingMore = true` + `LoadSourceItemsEffect(NextPage, Append: true)` → `SourceItemsLoadedMessage` → `Items` neu zusammengesetzt (`Concat().ToList()`) → ItemsSource-Referenzwechsel → Relayout, Sprung an den Anfang. **Soll:** Gleicher Lade-Flow, aber die Ansicht behält ihre Scrollposition beim Anhängen der Folgeseite.

**FA-4 (Ist):** Tipp → `ContinueWatchingItemSelectedMessage` → `movie`/`tvshowepisode`: `StartPlayback` (`NavigateToPlayerEffect` → Route `player`, `ResolvePlaybackSourceEffect`; Episoden zusätzlich `LoadUpcomingEpisodesEffect`) → Player über `HomePage`; Schließen → `HomePage`. Übrige Typen: `NavigateToMediaDetailsEffect`. **Soll:** Tipp → Detailansicht des Titels (`mediadetails`, mit Show-/Season-/Episode-Kontext) mit Wiedergabe; Schließen des Players → Detailansicht des Titels.

**FA-5 (Ist):** `MediaElement.MediaEnded` → `OnMediaEnded` → `PlaybackEndedMessage` → Angesehen + `ReportPlaybackProgressEffect` (Endposition) + ggf. Auto-Vorlauf (`AdvanceToNextEpisode`/`AdvancePlaylistEntry`). **Soll:** `MediaEnded` wird erst gegen das tatsächliche Wiedergabeende geprüft (Plausibilität/Debounce) — erst dann läuft der Abschluss-Flow; spurious gefeuerte Ereignisse lösen nichts davon aus.

## Konfiguration

- Die Anforderung verlangt kein neues konfigurierbares Verhalten.
- Bestehende Einstellungen, die weiter unverändert wirken: `AppSettings.AutoResumeEnabled` (Schalter Auto-Fortsetzung), `AutoResumeRemainingSeconds` (Restlaufzeit-Fenster des Auto-Vorlaufs im Takt-Pfad), `WatchedEndWindowSeconds` (Angesehen-Fenster im Fortschritts-Takt).
- Falls für FA-5 eine feste Plausibilitätsschwelle/Debounce gewählt wird, ist sie nach jetzigem Stand eine Konstante, keine neue Anwendungseinstellung (Annahme).

## Akzeptanzkriterien

**FA-1 — Genre-Filter:**
- Auf der `SourceOverviewPage` einer Serien-Quelle mit vom Server gelieferten Genres ist die Genre-Leiste sichtbar und enthält neben „Alle" die Genres der Quelle.
- Die Auswahl eines Genres filtert die Trefferliste (`SourceGenreSelectedMessage` → Neu-Laden ab Seite 0); „Alle" hebt den Filter auf.
- Die Ursache des bisherigen Fehlens ist identifiziert (leere Serverantwort / Ladefehler / fehlende Leiste) und dokumentiert; die Korrektur behebt die festgestellte Ursache.

**FA-2 — Suche:**
- Eingetippter Text in `MediaSearchBar` bleibt über beliebige `ModelChanged`-Ticks (Sync-, Fortschritts-, Netzwerkereignisse) erhalten und wird nicht durch den Modell-Suchtext überschrieben.
- Das Abschicken über die Such-Taste löst die Suche mit dem eingegebenen Text aus; das Ergebnis lädt die Trefferliste ab Seite 0.
- Der zuletzt abgeschickte Suchtext bleibt im Feld sichtbar (z. B. bei Rückkehr auf die Seite).

**FA-3 — Scrollposition:**
- Beim automatischen Nachladen der Folgeseite (`RemainingItemsThreshold`) bleibt die Scrollposition der `ItemsCollection` erhalten; die Ansicht springt nicht an den Anfang.
- Die nachgeladenen Einträge erscheinen angehängt; Erstladen, Genre-Wechsel und neue Suche zeigen weiterhin die Liste von Anfang an.

**FA-4 — Weiterschauen-Navigation:**
- Ein Tipp auf einen Weiterschauen-Eintrag vom Typ Film/Episode führt zur Titel-Detailansicht mit Wiedergabe; nach dem Schließen des Players ist die Detailansicht des Titels sichtbar — nicht die Startliste.
- Auf der erreichten Detailansicht sind die Titelinformationen ohne Umweg über die Quelle einsehbar.
- Das bestehende Verhalten nicht direkt abspielbarer Einträge (Detail-Navigation) bleibt unverändert.

**FA-5 — Videoende:**
- Ein `MediaEnded`-Ereignis während der laufenden Wiedergabe (tatsächliches Ende noch nicht erreicht) löst weder den Auto-Vorlauf/Folgevideo-Start noch die Angesehen-Markierung/End-Fortschrittsmeldung aus.
- Beim tatsächlichen Wiedergabeende läuft der bestehende Abschluss-Flow weiter (Angesehen-Markierung, Fortschrittsmeldung, Auto-Fortsetzung bei aktiviertem `AutoResumeEnabled`).

## Explizite Nicht-Ziele

- Keine Änderungen am Server oder an API-Verträgen — auch dann nicht, wenn die Diagnose zu FA-1 ergibt, dass `api/SourceGenres/{id}` für Serien-Quellen serverseitig eine leere Liste liefert (die Anforderung betrifft den App-Auslieferungsstand; eine etwaige serverseitige Ursache ist nur zu dokumentieren).
- Keine Umgestaltung des Bedienkonzepts (keine Live-Suche pro Tastenschlag, kein neues Filter-/Player-UI) über die geforderten Verhaltenskorrekturen hinaus.
- Keine Entfernung oder Neuausrichtung der Auto-Fortsetzungs-Funktion — FA-5 betrifft ausschließlich die Zuverlässigkeit der Ende-Erkennung; das bestehende Ende-Verhalten bleibt.
- Keine Änderung des bewusst eingeführten Kontextmenüs der Weiterschauen-Einträge („Ausblenden"/„Überspringen", Issue #43 Schritt 2) und kein Rückbau der Direktwiedergabe an anderen Stellen, sofern nicht von FA-4 erfasst.
- Keine gebündelte Umsetzung erzwungen: Die fünf Punkte sind fachlich unabhängig und dürfen getrennt bearbeitet/aufgeteilt werden.

## Offene Fragen

1. **FA-1 — Ursache:** Liefert `api/SourceGenres/{id}` für die betroffenen Serien-Quellen eine leere Liste, schlägt der Ladevorgang fehl (Fallback: nur „Alle"), oder fehlt die Leiste auf dem betreffenden Auslieferungsstand vollständig? (Aus der Anforderung übernommen — eine Diagnose auf dem Referenzgerät/mit Protokoll ist Teil der Bearbeitung.) Folgefrage: Ist bei einer leeren Serverantwort die „Alle"-nur-Leiste das gewünschte Soll, oder soll die Leiste ganz ausgeblendet werden?
2. **FA-4 — Soll-Ablauf im Detail:** Startet die Wiedergabe beim Tipp weiterhin unmittelbar (Player über der geöffneten Detailseite), oder öffnet der Eintrag nur die Detailseite, auf der der Anwender die Wiedergabe selbst auslöst? Die Anforderung beschreibt „Detailansicht des Titels mit Player" — die Reihenfolge/Automatik ist nicht eindeutig belegt.
3. **FA-4 — Varianten:** Gilt das neue Verhalten auch für playlist-gebundene Weiterschauen-Einträge (derzeit `StartPlaylistPlaybackEffect`) und für den Offline-Pfad (`OfflineContinueWatchingService`, lokale Dateien)? Wie ist mit Einträgen ohne Detail-Kontext (fehlende `ShowId`/`SeasonId`/`EpisodeId`, Platzhalter fehlender Offline-Episoden) umzugehen?
4. **FA-5 — Prüfkriterium:** Nach welcher Regel gilt das Ende als tatsächlich erreicht (z. B. `Position` nahe/≥ `Duration`, Debounce-Zeitspanne)? Wie ist bei unbekannter/0-`Duration` zu verfahren, und gilt die Prüfung auch für den Playlist-Kontext (`AdvancePlaylistEntry`)?
5. **Aufteilung:** Ist eine gemeinsame Lieferung aller fünf Punkte im Rahmen dieses Issues gewünscht, oder werden sie als getrennte Arbeitspakete/PRs abgenommen (die Anforderung erlaubt beides)?
