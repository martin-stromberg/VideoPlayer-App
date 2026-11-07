# Anforderung: Episodenlisten — Angesehen-Symbol bei gesehenen Episoden (Entwicklungsschritt 7)

Quelle: `docs/projects/task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app/steps.md`,
Schritt 7 (Kundenfeedback Issue #9, Projekt „Design-Angleichung an Webanwendung").
Design-Referenz: `docs/projects/task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app/web-design-recherche.md`
(Abschnitt „Angesehen-Symbol (WatchedIndicator.razor)"), verifiziert gegen den Web-Quellstand:
`WatchedIndicator.razor` (SVG-Auge, Titel „Gesehen"), `app.css` `.watched-indicator` /
`.watched-indicator-episode` / `.watched-indicator-image-left` und `TVShowDetails.razor:151`
(`episode.WatchedAt.HasValue` steuert die Sichtbarkeit).

Kundenvorgabe (sinngemäß): „Bei den Episoden fehlt das Angesehen-Symbol" — in den
Episodenlisten soll wie in der Webanwendung ein rundes dunkles Badge mit Augen-Symbol
in weicher Primärfarbe über dem Episoden-Poster erscheinen, sobald eine Episode als
gesehen markiert ist.

## Fachliche Zusammenfassung

Die Episodenliste der Staffel-Ansicht auf der Detailseite der neuen App
(`VideoPlayer.Maui`, `Pages/MediaDetailsPage.xaml` — die Legacy-App
`VideoWebPlayer.Maui` bleibt unangetastet) zeigt pro Episode ein Angesehen-Badge
nach Web-Vorbild: rundes, dunkles Badge (`rgba(0,0,0,.68)`, `border-radius: 999px`,
feiner heller Rand `rgba(255,255,255,.28)`) mit Augen-Symbol in `VpPrimarySoft`
(`--vp-primary-soft`), oben links über dem Episoden-Poster positioniert
(Web: `.watched-indicator-image-left`, `top/left: 0.6rem`, Episodengröße `2rem`).
Gerendert wird es nur bei gesetztem Gesehen-Zeitpunkt (`WatchedAt`); ungesehene
Episoden zeigen kein Symbol.

Der Server liefert `WatchedAt` (`DateTime?`) bereits über `MediaEntryDto`
(befüllt durch `WatchedStatusService.EnrichAsync`, `ItemsController.cs`) mit —
die App übernimmt das bislang still ignorierte Feld in ihr Datenmodell
(`MediaEntry`), führt es über das MVU-View-Modell (`EpisodeItem`) bis in die
Anzeige und aktualisiert den Anzeigestand der geladenen Detailansicht, wenn der
Player eine Episode als „angesehen" markiert (`PlayerState.IsWatched` bei
Ende/Watched-Fenster). Keine Server-Änderung; die bisherige Gesehen-Logik
(ReportPlaybackProgressEffect, WatchedEndWindowSeconds, AutoDeleteWatchedEpisodes)
bleibt unverändert.

## Akzeptanzkriterien (aus der Kundenanforderung abgeleitet)

1. In der Episodenliste der Staffel-Ansicht (`EpisodesCollection` auf der
   `MediaDetailsPage`) zeigt jede Episode mit gesetztem `WatchedAt` ein rundes
   Augen-Badge: dunkler Badge-Hintergrund (Web: `rgba(0,0,0,.68)`), Pill-Form
   (`RoundRectangle 999`), Auge in `VpPrimarySoft`, feiner heller Rand
   (Web: `rgba(255,255,255,.28)`), oben links über dem Poster.
2. Episoden ohne `WatchedAt` zeigen kein Badge.
3. Der Badge-Zustand aktualisiert sich innerhalb der Sitzung: Markiert der
   Player eine Episode der angezeigten Serie als gesehen (Ende der Wiedergabe
   bzw. Erreichen des Watched-Endfensters), trägt das geladene
   `MediaDetailsView` den Gesehen-Zeitpunkt — ohne erneutes Laden der Seite.
4. Das Feld wird zusätzlich in den Offline-Serienkontext (`ShowDetailsCache`)
   übernommen, damit der Offline-Snapshot denselben Anzeigestand liefert
   (abwärtskompatibel: alte Cache-Einträge ohne das Feld gelten als ungesehen).
5. Keine Server-Änderung; keine Änderung an der Gesehen-Logik
   (Schwellenwert, Fortschrittsmeldung, Auto-Löschen gesehener Downloads).
6. Definition of Done: `dotnet build VideoPlayer.App.sln` in Debug **und**
   Release fehlerfrei, volle Testsuite
   `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj` grün.

## Betroffene Klassen und Komponenten

### API-Client (`VideoPlayer.ApiClient`)

- `Models/MediaEntry.cs` — neue Eigenschaft `DateTime? WatchedAt` an der
  Basisklasse `MediaEntry` (deckt Episode wie Film ab; STJ-Deserialisierung ist
  case-insensitiv und toleriert das Feld auf jedem Diskriminator-Typ).

### MVU-Kern (`VideoPlayer.Mvu`, plattformneutral — keine MAUI-Typen)

- `MediaDetailsView.cs` — `EpisodeItem` erhält `DateTime? WatchedAt` sowie die
  Ableitung `IsWatched => WatchedAt.HasValue` (Bindeziel der Badge-Sichtbarkeit).
- `MediaDetailsService.cs` — `MapSeasons` mappt `e.WatchedAt` auf
  `EpisodeItem.WatchedAt`; `MapOfflineShow` übernimmt den gecachten Stand.
- `ShowDetailsCache.cs` — `CachedShowEpisode` erhält `DateTime? WatchedAt`
  (JSON-Cache, tolerant gegenüber Alteinträgen); `WriteAsync` schreibt es mit.
- `AppUpdate.cs` — `UpdatePlaybackProgress` (Watched-Schwelle) und
  `UpdatePlaybackEnded` setzen zusätzlich `WatchedAt` am passenden
  `EpisodeItem` des geladenen `MediaDetailsView` (nur bei `tvshowepisode`,
  nur wenn die Episode in den angezeigten Staffeln liegt).

### UI (`VideoPlayer.Maui`)

- `Pages/MediaDetailsPage.xaml` — das Episoden-Poster (`CachedPosterImage`)
  wird in ein `Grid` gehüllt; darüber liegt das Badge (oben links,
  `IsVisible="{Binding IsWatched}"`): `Border` mit `RoundRectangle 999`,
  dunkler transluzenter Hintergrund, feiner Rand, darin das Auge als
  `Path` (SVG-Kontur des Web-`WatchedIndicator`) plus Pupille/Glanz als
  `Ellipse` in `VpPrimarySoft` bzw. `VpText`.
- `Resources/Styles/Colors.xaml` — neue Hilfstokens für den Badge-Hintergrund
  (`rgba(0,0,0,.68)` → `#AD000000`) und den Badge-Rand
  (`rgba(255,255,255,.28)` → `#47FFFFFF`); `VpOverlay` (`#80000000`) ist per
  Contract-Test gepinnt und bleibt unverändert.
- `Pages/MediaDetailsPage.xaml.cs` — kein Eingriff nötig: die Sichtbarkeit
  bindet direkt an `EpisodeItem.IsWatched`, die Listenaktualisierung läuft
  über `EpisodesCollection.ItemsSource` in `UpdateSeasonSection`.

### Tests (`VideoWebPlayer.Maui.Tests`)

- `MediaEntrySerializationTests` — `WatchedAt` wird aus dem JSON übernommen
  (Episode mit Datum → gesetzt; ohne Feld → `null`).
- `MediaDetailsServiceTests` — Serienantwort mit `WatchedAt` → `EpisodeItem`
  trägt den Zeitpunkt; ungesehene Episoden `null`; Offline-Snapshot aus dem
  `ShowDetailsCache` übernimmt den Stand.
- `AppUpdateTests_Player`/`_MediaDetails` — `PlaybackEndedMessage` bzw. das
  Erreichen des Watched-Fensters setzt `WatchedAt` am `EpisodeItem` der
  geladenen Details (nur bei `tvshowepisode`; andere Episoden unverändert).
- `MediaDetailsPageContractTests` — Badge-Markup im Episoden-Template
  (`IsWatched`-Binding, Pill-Form, `VpPrimarySoft`, Badge-Tokens, Position
  oben links über dem Poster).

## Implementierungsansatz

- Reine Übernahme+Darstellung: `WatchedAt` wandert `MediaEntry` →
  `EpisodeItem` → Badge-Sichtbarkeit. Der Serverteil ist unverändert.
- In-Session-Aktualisierung ohne Server-Roundtrip: Die Player-Übergänge
  kennen die gespielte Identität (`PlayerState.MediaType`/`MediaId`); beim
  Übergang auf `IsWatched` wird das betroffene `EpisodeItem` im geladenen
  View-Modell per `with`-Kopie markiert (lokaler Zeitpunkt — nur der
  Sichtbarkeitszustand ist relevant, der exakte Server-Zeitpunkt wird beim
  nächsten Laden geliefert).
- Offline-Parität: Der `ShowDetailsCache` speichert `WatchedAt` je Episode
  (JSON-Schemawechsel ist verträglich — fehlendes Feld deserialisiert zu
  `null`); der Offline-Snapshot zeigt damit den zuletzt bekannten Stand.
- Design-Tokens: neue Farben nur in `Colors.xaml` (`VpBadgeBg`,
  `VpBadgeBorder`); die Contract-Tests verbieten Hexfarben und Farbliterale
  (auch `White`) in Seiten — der Glanzpunkt nutzt `VpText`.
- Das Auge wird als MAUI-Shapes (`Path`+`Ellipse`) gerendert — kein
  Bildasset, keine externe Icon-Quelle; Skalierung über das Badge-Grid.

## Konfiguration

Keine — das Badge ist fester Bestandteil der Episodenliste (wie im Web ohne
Schalter).

## Offene Fragen

Keine — Feld, Darstellung und Position sind aus dem Web-Quellstand verifiziert
(`MediaEntryDto.WatchedAt`, `WatchedIndicator.razor`, `app.css`,
`TVShowDetails.razor`). Die In-Session-Aktualisierung beschränkt sich auf die
lokal gespielte Episode; ein Server-Push fremder Geräte (`ContinueWatchingUpdated`)
aktualisiert die Startliste, nicht rückwirkend die offene Detailseite —
deckt sich mit dem geforderten Akzeptanzpunkt („wenn eine Episode gesehen wurde").
