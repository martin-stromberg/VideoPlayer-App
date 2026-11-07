# Anforderung: Detailseiten — Vollbreiten-Header mit Hintergrundgrafik (Entwicklungsschritt 6)

Quelle: `docs/projects/task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app/steps.md`,
Schritt 6 (Kundenfeedback Issue #9, Projekt „Design-Angleichung an Webanwendung").
Design-Referenz: `docs/projects/task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app/web-design-recherche.md`
(Detailseiten-Header der Webanwendung: `.tvshow-header`/`.movie-header`/`.collection-header` mit
`-overlay` und `-content`; verifiziert gegen den Web-Quellstand `TVShowDetails.razor` und
`MovieCollectionDetails.razor` im Server-Repository).

Kundenvorgabe (wortlaut): „Die Hintergrundgrafik soll auf kompletter Breite gezeigt werden.
Darauf sind dann die Serien-, Staffel-, oder Episodeninformationen platziert."

## Fachliche Zusammenfassung

Die Detailseite der neuen App (`VideoPlayer.Maui`, `Pages/MediaDetailsPage.xaml` — die
Legacy-App `VideoWebPlayer.Maui` bleibt unangetastet) ersetzt den bisherigen schmalen
Kopfbereich (140×200-Poster links neben Titel/Meta) durch einen vollbreiten
Header nach Web-Vorbild: Hintergrundgrafik im Cover-Modus (`AspectFill`), darüber
geschichtete Dunkelgradienten (seitlicher 90°-Verlauf plus Bottom-Fade in den
Seitenhintergrund), unten links Titel, Meta-Zeile (Jahr, Genres in weicher
Primärfarbe), Handlungstext sowie die Aktions-/Favoriten-Schaltflächen. Die Höhe
folgt responsiv der Seitenbreite (Web-Referenz `clamp(360px, 52vw, 620px)`).

Die Bildquelle folgt der Web-Kette: Serien, Staffeln, Filme und Kollektionen
nutzen `api/pictures/{id}` mit der Fallbackkette Banner → Fanart → Poster (bereits
in `MediaDetailsView.BannerPictureId` aufgelöst); Episoden nutzen den
Episoden-Still-Endpunkt `api/episodes/{id}/background-image` (serverseitig:
generiertes Hintergrundbild → Banner/Fanart → Platzhalter). Wie im Web zeigt der
Serien-Header bei ausgewählter Episode deren Still und Episoden-Informationen.

## Akzeptanzkriterien (aus der Kundenanforderung abgeleitet)

1. Alle fünf Detailansichten (Serie `tvshow`, Staffel `tvshowseason`, Episode
   `tvshowepisode`, Film `movie`, Filmkollektion `moviecollection`) zeigen
   statt des bisherigen Posterbereichs einen Header mit Hintergrundgrafik auf
   kompletter Breite (Cover-Darstellung, Höhe sinngemäß `clamp(360px, 52vw, 620px)`).
2. Auf der Grafik liegt ein Dunkelgradient-Overlay: seitlicher Verlauf
   (90°, `rgba(19,19,19,.98)→.72→.1`) plus Bottom-Fade in den Seitenhintergrund
   (`0deg` zu `VpBg`), Trennlinie am unteren Rand (`VpBorder`).
3. Unten links auf dem Overlay stehen Titel, Meta-Zeile (Jahr, Genres in
   `VpPrimarySoft`, fett), Handlung (`VpTextSoft`) sowie die
   Aktions-Schaltflächen („Abspielen", „Herunterladen") und der
   Favoriten-Schalter (im Web oben rechts auf dem Header).
4. Bildquellen: `tvshow`/`movie`/`moviecollection` → `api/pictures/{id}` über die
   aufgelöste Fallbackkette (Banner → Fanart → Poster);
   `tvshowseason` → Backdrop der gewählten Staffel, Fallback Serien-Backdrop;
   `tvshowepisode` → `api/episodes/{id}/background-image`.
5. Wie im Web wechselt der Serien-Detail-Header bei Episoden-Auswahl auf das
   Still der Episode (bei navigiertem Episoden-Kontext und bei Staffelwechsel
   auf die jeweils erste Episode der gewählten Staffel — Web-Verhalten:
   `SelectSeason` wählt die erste Episode mit).
6. Die bisherigen Inhalte unter dem Header bleiben erhalten: Staffel-Auswahl,
   Episodenliste, Kollektions-Filme, Download-Hinweis.
7. Fallback: ohne verfügbares Bild zeigt der Header die Gradienten über dem
   Flächenhintergrund (`VpSurfaceLow`-Äquivalent) und bricht nicht zusammen;
   offline bleibt die Seite stabil (kein Bildabruf ohne Sitzung).
8. Definition of Done: `dotnet build VideoPlayer.App.sln` in Debug **und**
   Release fehlerfrei, volle Testsuite
   `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj` grün.

## Betroffene Klassen und Komponenten

### API-Client (`VideoPlayer.ApiClient`)

- `VideoPlayerApiClient` — neue Methode `GetEpisodeBackgroundImageAsync(long episodeId)`
  (`GET api/episodes/{id}/background-image` → `byte[]`), analog
  `GetPictureAsync`/`GetHeroBackgroundAsync` (Sende-Pipeline, Bearer, stille
  Token-Erneuerung).

### MVU-Kern (`VideoPlayer.Mvu`, plattformneutral — keine MAUI-Typen)

- `MediaDetailsView` — Header-Ableitungen: `SelectedEpisodeId` (wechselbar),
  `SelectedEpisode`, `HeaderTitle`, `HeaderMeta`, `HeaderPlot`,
  `HeaderEpisodeId` (Episoden-Still-Identität) und `HeaderPictureId`
  (`api/pictures`-Identität) als berechnete Eigenschaften.
- `SeasonItem` — neue Eigenschaften `BannerPictureId` (aufgelöste Kette) und
  `Year` (Staffeljahr aus `PremieredAt`/`ReleaseDate`) für den Staffel-Header.
- `EpisodeItem` — neue Eigenschaft `ReleaseYear` für die Episoden-Meta-Zeile.
- `MediaDetailsState` — Header-Bildzustand (`HeaderImageBytes` als `byte[]`,
  `IsHeaderImageLoading`).
- `AppEffect` — neuer Effekt `LoadMediaDetailsHeaderEffect` (Medien-Identität +
  Header-Identität: entweder `PictureId` oder `EpisodeId`).
- `AppMessage` — neue Nachricht `MediaDetailsHeaderImageLoadedMessage`
  (Medien- + Header-Identität + Bilddaten).
- `AppUpdate` — `UpdateMediaDetailsLoaded` löst den Header-Ladeeffekt aus;
  `UpdateSeasonSelected` wählt die erste Episode der neuen Staffel und löst
  bei geänderter Header-Identität den Ladeeffekt erneut aus;
  `UpdateMediaDetailsHeaderImageLoaded` übernimmt das Bild nur bei
  passender (nicht veralteter) Identität.
- `MediaDetailsService` — Mapping der neuen Felder (`SeasonItem.BannerPictureId`,
  `SeasonItem.Year`, `EpisodeItem.ReleaseYear`, `SelectedEpisodeId`).

### UI (`VideoPlayer.Maui`)

- `Pages/MediaDetailsPage.xaml` — der gepaddete Kopf (140×200-`CachedPosterImage`
  plus Textspalte) wird durch einen vollbreiten Header-`Grid` ersetzt:
  Bildebene (`Image`, `AspectFill`), seitlicher Dunkelgradient, Bottom-Fade,
  Trennlinie, Inhalt unten links (Titel, Meta, Plot, Aktions-Schalter),
  Favoriten-Schalter oben rechts. Vollbreite über negative Margins bzw.
  verschobenes Seiten-Padding (Web: `margin: -2rem calc(var(--vp-gutter) * -1)`).
- `Pages/MediaDetailsPage.xaml.cs` — Header-Inhalte und Bilddaten aus dem
  Model befüllen (`byte[]` → `ImageSource.FromStream`, Referenzvergleich wie
  `HomePage.UpdateHero`), responsive Höhe via `Math.Clamp` in `OnSizeChanged`,
  Favoriten-/Aktions-Schalter bleiben verdrahtet.
- `Services/ShellAppEffectHandler.cs` — Ausführung von
  `LoadMediaDetailsHeaderEffect`: Episoden-Still über
  `GetEpisodeBackgroundImageAsync`, sonst `GetPictureAsync`; Ergebnis-Rückmeldung
  per `MediaDetailsHeaderImageLoadedMessage`; Fehlschlag = sauberes Auslassen
  (Debug-Log-Muster).
- `Resources/Styles/Colors.xaml` — Wiederverwendung der `VpHero*`-Tokens aus
  Schritt 5 (keine neuen Hexfarben in Seiten).

### Tests (`VideoWebPlayer.Maui.Tests`)

- `VideoPlayerApiClientTests_Endpoints` — Endpunkt-Abruf
  `api/episodes/{id}/background-image` (Methode, Pfad, Bearer).
- `AppUpdateTests_*` (Thema Detail-Header) — Ladeeffekt nach erfolgreicher
  Detail-Ladung je Bildquelle (Picture vs. Episode-Still), Staffelwechsel
  aktualisiert die Header-Identität, veraltete Antworten werden verworfen,
  Fehlschlag ohne Bild bleibt stabil, kein Abruf ohne Header-Identität.
- `MediaDetailsServiceTests` — Mapping der neuen Felder
  (Staffel-Bannerkette, Staffeljahr, Episodenjahr, `SelectedEpisodeId`).
- `MediaDetailsPageContractTests` (neu) — Header-Markup und
  Effekt-/Client-Verdrahtung als Quell-Vertragstests (MAUI-Schicht ist im
  Testprojekt nicht instanziierbar — etabliertes Muster).

## Implementierungsansatz

- MVU-konform: die Header-Bilddaten liegen flüchtig als `byte[]` im
  `MediaDetailsState` (`ImageSource` ist ein MAUI-Typ und darf nicht in den
  plattformneutralen MVU-Kern); die Seite wandelt die Bytes in
  `ImageSource.FromStream` um (Muster: `HomePage.UpdateHero` aus Schritt 5).
- Der Header-Bildabruf ist ein eigener Effekt, der nach erfolgreicher
  Detail-Ladung ausgelöst wird — die Bild-Identität (Picture-Id oder
  Episoden-Id) wird aus dem geladenen `MediaDetailsView` abgeleitet. So bleibt
  die Detail-Ladung selbst unverändert schnell und der Header kann bei
  Staffelwechsel gezielt neu geladen werden.
- Veraltete Bild-Antworten werden anhand der Medien-Identität
  (`MediaType`/`MediaId`) und der Header-Identität verworfen.
- Fehlerfall: `MediaDetailsHeaderImageLoadedMessage` ohne Bilddaten →
  `IsHeaderImageLoading` entfällt, der Header bleibt mit Gradienten/Texten
  stehen (Web-Verhalten: Gradienten über dem Flächenhintergrund), keine
  Exception an die Oberfläche.
- Episoden-Header zeigt — wie im Web — Episoden-Titel, „Episode {Nr}" mit Jahr
  sowie den Episoden-Plot statt der Serien-Informationen; Staffel-Header zeigt
  „{Serie} - {Staffel}", Staffeljahr und Episodenanzahl (Web:
  `showSeasonInfo`-Zweig).

## Konfiguration

Keine — der Header ist fester Bestandteil der Detailseiten (im Gegensatz zum
ausblendbaren Startseiten-Hero aus Schritt 5 gibt es keine Kundenvorgabe für
einen Schalter).

## Offene Fragen

Keine — Endpunkte, Fallbackkette und Darstellung sind aus dem Web-Quellstand
verifiziert (`TVShowDetails.razor`, `MovieCollectionDetails.razor`,
`EpisodesController.GetBackgroundImage`, `app.css`). Die Staffelwechsel-
Vorwahl der ersten Episode folgt dem Web-Verhalten (`SelectSeasonCoreAsync`).
