# Bestandsaufnahme: Episodenlisten — Angesehen-Symbol bei gesehenen Episoden

Bestandsaufnahme des Datenwegs „Gesehen-Status" von der Server-Antwort bis zur
Episodenliste der Detailseite (`MediaDetailsPage`), bezogen auf die Anforderung
in `requirement.md` (Schritt 7 des Projekts „Design-Angleichung an Webanwendung").

## Zusammenfassung

- **Der Server liefert `WatchedAt` bereits mit** (`DateTime?` auf
  `MediaEntryDto`, befüllt durch `WatchedStatusService.EnrichAsync` für die
  Episoden der Serienantwort, die Einzel-Episode und Kollektions-Filme —
  verifiziert am Server-Quellstand `ItemsController.cs` ~Z. 690-720).
- **Die App ignoriert das Feld vollständig:** `VideoPlayer.ApiClient`-
  `MediaEntry` besitzt kein `WatchedAt`; die STJ-Deserialisierung
  (`PropertyNameCaseInsensitive`, `VideoPlayerApiClient.cs:30-32`) verwirft
  das JSON-Feld still.
- **`EpisodeItem`** (`VideoPlayer.Mvu/MediaDetailsView.cs:33-82`) trägt kein
  Watched-Feld; Mapping-Stellen sind `MediaDetailsService.MapSeasons`
  (online, Z. 605-631) und `MapOfflineShow` (Offline-Snapshot, Z. 456-476).
- **Der Offline-Serien-Cache** (`ShowDetailsCache`, `CachedShowEpisode` Z. 11-21)
  speichert nur Id/Number/Title — kein Watched-Stand.
- **Die Gesehen-Logik existiert bereits im MVU:** `UpdatePlaybackProgress`
  setzt `PlayerState.IsWatched` beim Erreichen des Watched-Endfensters
  (`AppUpdate.cs:1696-1706`), `UpdatePlaybackEnded` setzt es am Ende
  (`AppUpdate.cs:1738-1745`); die Fortschrittsmeldung läuft über
  `ReportPlaybackProgressEffect`. Die geladenen `MediaDetails` werden dabei
  **nicht** angefasst — eine In-Session-Aktualisierung der Episodenliste
  fehlt.
- **Die Episodenliste** (`MediaDetailsPage.xaml:156-233`) zeigt pro Eintrag
  `CachedPosterImage` (110×62), Titel/Plot und Download-/Play-Schalter —
  **kein Watched-Badge**. Das Poster ist noch nicht in ein Overlay-fähiges
  `Grid` gehüllt.
- **Design-Tokens** (`Resources/Styles/Colors.xaml`): `VpPrimarySoft`
  (#ffb4aa) und `VpOverlay` (#80000000, per Contract-Test gepinnt) existieren;
  die Web-Badge-Farben `rgba(0,0,0,.68)` (Hintergrund) und
  `rgba(255,255,255,.28)` (Rand) haben noch kein Token-Äquivalent.
- **Contract-Test-Rahmen:** `DesignTokensContractTests` verbietet Hexfarben
  und Farbliterale (auch `White`) in allen XAML-Dateien unter
  `Pages/`/`Controls/`/`AppShell.xaml` sowie `Colors.`-Aufrufe im Code-Behind;
  alle Tokens müssen `Vp*` heißen.
- **Web-Vorbild verifiziert** (Server-Repo): `WatchedIndicator.razor` ist ein
  SVG-Auge (viewBox 24×24: Augenkontur-Pfad, Pupille r=3,2, Glanzpunkt);
  `app.css` `.watched-indicator` + `.watched-indicator-episode` (2rem) +
  `.watched-indicator-image-left` (oben links, 0,6rem); Einbindung in
  `TVShowDetails.razor:151` bei `episode.WatchedAt.HasValue`.

Test-Ausgangszustand: 728/728 Tests bestanden (Nachweis in
[inventory/tests.md](inventory/tests.md)), keine bekannten Fehlschläge.

## Details

- [Datenmodell](inventory/models.md) — `MediaEntry`-Hierarchie, `EpisodeItem`,
  `CachedShowEpisode`, `PlayerState`
- [Logik](inventory/logic.md) — `MediaDetailsService`, `ShowDetailsCache`,
  `AppUpdate` (Watched-Übergänge), `MediaDetailsPage`-Code-Behind
- [UI](inventory/ui.md) — Episoden-Template, Design-Tokens, Web-Badge-Referenz,
  Contract-Rahmen
- [Tests](inventory/tests.md) — Ausgangslauf und bestehende Testklassen
