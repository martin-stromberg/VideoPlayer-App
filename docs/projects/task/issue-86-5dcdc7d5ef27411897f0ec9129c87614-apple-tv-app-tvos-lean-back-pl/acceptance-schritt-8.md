# Abnahmeprüfung – Entwicklungsschritt 8

## Ergebnis

**Status:** Anforderung erfüllt – externe Verifikation ausstehend

## Abweichungen

Keine.

## Ausstehende externe Verifikationen

- **Laufzeitverhalten auf tvOS-Simulator/-Gerät:** Tatsächliches
  Öffnen der Playlists-Menüseite, das seitenweise Nachladen der
  Einträge beim Fokussieren/Scrollen ans Listenende, das Starten der
  Wiedergabe über „▶ Abspielen" bzw. ab einem gewählten Eintrag, die
  Positionsanzeige im Info-Tab sowie der Übergang zum nächsten/
  vorherigen Eintrag per Transport-Bar-Eintrag sind auf Code-Ebene,
  über Contract-Tests und den macOS-CI-Kompiliernachweis verifiziert —
  nicht per Laufzeitnachweis. Fällt unter den Register-Eintrag
  „2–laufend" in `pending-verification.md` (App-Start/
  Runtime-Verhalten auf tvOS-Simulator jenseits der Kompilierung).
- Der macOS-CI-Nachweis selbst ist erbracht: Run `42132280331`
  (`build-tvos` 7m42s + `verify` 6m52s, beide grün) auf `headSha`
  `2a7f3efeef7b947e8abc606b5961e5142c01c51e` = aktueller HEAD =
  `origin/<branch>`; keine ungepushten Commits
  (`git log origin/<branch>..HEAD` leer), Arbeitsbaum sauber.

## Hinweise

Prüfung anhand des Diffs
`task/issue-86-...-apple-tv-app-tvos-lean-back-pl...HEAD` (16 Dateien,
+1519/−10; vier neue tvOS-UI-Dateien, zwei Service-/Navigator-
Erweiterungen, eine Testdatei, Doku) und Lesen der Quelldateien samt
der plattformneutralen MVU-Gegenseite (`AppUpdate`, `PlaylistsPageState`,
`PlaylistDetailsState`, `PlayerState`, `PlaylistService`).

**(a) Übersicht eigener + öffentlicher Playlists:**
`PlaylistsPageViewController` (`Ui/PlaylistsPageViewController.cs`)
ist über `MenuTabBarController.cs` Z. 100 als echte Menüseite
verdrahtet (`AppPage.Playlists` steht in `AppRoutes.MenuPages`,
`AppRoutes.cs` Z. 38 — bisher Platzhalter). Die gruppierte
`UITableView` bildet die flache `PlaylistOverviewItem`-Liste auf
Sektionen ab: `BuildItems(own, public)` des plattformneutralen
`PlaylistsPageState` stellt den nicht-leeren Sektionen die
Überschriften „Meine Playlists"/„Öffentliche Playlists" voran;
`BuildSections` (Z. 233–250) gruppiert Header-Zeilen → `TitleForHeader`,
Einträge → `PlaylistTableCell` (Cover über `GetPlaylistCoverAsync`,
Name, `SortModeText` + `GenreText`). Ladepfad: `ViewDidAppear` →
`PlaylistsPageOpenedMessage` → `UpdatePlaylistsPageOpened`
(`AppUpdate.cs` Z. 2134) → `LoadPlaylistsEffect` →
`TvosAppEffectHandler.LoadPlaylistsAsync` (Z. 1012–1027) →
`PlaylistService.LoadPlaylistsAsync` (Own + Public) →
`PlaylistsLoadedMessage`. Reines Online-Feature: ohne
`ConnectionState.Connected` kommt die Fehleranzeige mit
Retry-Schaltfläche (Z. 144–171), kein Cache-Stand.

**(b) Detailansicht mit seitenweisem Nachladen:**
`PlaylistDetailsViewController` (`Ui/PlaylistDetailsViewController.cs`)
wird vom Navigator modal präsentiert (`PresentPlaylistDetails`,
`TvosNavigator.cs` Z. 270–296, Singleton + Duplikat-Guard +
`TopmostPresented`). Kopfbereich: Cover (`CoverPlaylistId`),
Name, `SummaryText`, `GenreText`, `Description` — über
`LoadPlaylistDetailsEffect`/`LoadPlaylistEntriesEffect` (Seite 1)
beim `PlaylistDetailsOpenedMessage`-Wiederherstellungspfad in
`ViewWillAppear` (Z. 204–217). Eintragsraster:
`UICollectionView` mit `PlaylistEntryCell` (Poster über
`ResolvedPictureId`, Titel, `Subtitle` = übergeordneter Kontext,
`IsAccessible`-Abdunklung + „Kein Zugriff"). Nachladen:
`WillDisplayCell`-Lookahead (5 vor Ende, Z. 400–407) →
`PlaylistEntriesNextPageRequestedMessage` →
`UpdatePlaylistEntriesNextPageRequested` (`AppUpdate.cs` Z. 2346)
→ `LoadPlaylistEntriesEffect(NextPage, Append: true)` →
`services.Playlists.LoadEntriesAsync` (`entries/paged`-Endpunkt);
Dedup über `IsLoadingMore`/`HasMore`, veraltete Anhänge werden über
den Seitenzähler verworfen (`AppUpdate.cs` Z. 2327–2330);
Anhänge-Fehler lassen die Liste stehen und zeigen Retry (Z. 347–353),
`IsLoadingMore`-Hinweis am Fuß (Z. 319).

**(c) Abspielen gesamt + ab gewähltem Eintrag mit Playlist-Kontext:**
„▶ Abspielen" dispatcht
`PlaylistPlaybackStartRequestedMessage(playlistId, null)` →
`StartPlaylistPlaybackEffect` (Z. 358–359);
`PlaylistEntrySelectedMessage` startet für zugängliche
movie/tvshowepisode-Einträge die Wiedergabe ab diesem Eintrag
(`StartPlaylistPlaybackEffect(playlistId, entryId)`,
`AppUpdate.cs` Z. 2414–2420), alle übrigen (Serie/Staffel/Sammlung/
nicht zugänglich) öffnen die Medien-Detailseite mit Show-/Season-
Hints — korrektes Verhalten. Der Effekt ist im Handler an
`Playlists.StartPlaybackAsync` verdrahtet (`TvosAppEffectHandler.cs`
Z. 1074–1090, bestand seit Schritt 7). Playlist-Kontext:
`PlayerState.PlaylistLabel` („„Name" · Eintrag X/Y",
`PlayerState.cs` Z. 103–105) im Info-Tab
(`TvosPlayerInfoViewController`); next/previous als
`TransportBarCustomMenuItems` nur bei `IsPlaylistPlayback`
(`TvosPlayerViewController.cs` Z. 180–211, deaktiviert über
`CanNavigateNext`/`CanNavigatePrevious`) →
`LoadNext/PreviousPlaylistEntryEffect` →
`GetNextEntryAsync`/`GetPreviousEntryAsync`; Auto-Fortsetzung und
Fehler-Rückfall über `AdvancePlaylistEntryEffect` → `AdvanceAsync`
(advance-Endpunkt, `TvosAppEffectHandler.cs` Z. 263–266,
1092–1118) — alles seit Schritt 7 verdrahtet, Ergebnis-Weg über
`UpdatePlaylistNavigationCompleted` mit Identitäts-Guard und
Erschöpfungs-Flag.

**(d) Keine Verwaltungsfunktionen:** Weder UI noch Effekte —
die tvOS-Seiten dispatchten ausschließlich lesende Messages
(`PlaylistsPageOpened`, `PlaylistSelected`, `PlaylistDetailsOpened`,
`PlaylistEntriesNextPageRequested`, `PlaylistEntriesRetryRequested`,
`PlaylistEntrySelected`, `PlaylistPlaybackStartRequested`); ein
Repo-weiter Grep über `VideoPlayer.TvOS/` findet keinerlei
Verwaltungs-Message/-Effekt-Verwendung. Alle
Verwaltungs-Effekte des plattformneutralen Kerns
(`SavePlaylistEffect`, `DeletePlaylistEffect`,
`RemovePlaylistEntryEffect`, `MovePlaylistEntry*`,
`ChangePlaylistSortModeEffect`, `LoadGenreOptionsEffect`,
`SetPlaylistGenresEffect`, `ResetPlaylistGenresEffect`,
`SetPlaylistPublicEffect`, `LoadPlaylistTargetsEffect`,
`AddPlaylistEntryEffect` — `AppEffect.cs` Z. 410 ff.) fallen im
`TvosAppEffectHandler` in den protokollierten No-Op-`default`-Zweig
(Z. 276–281) und wären selbst bei irrtümlichem Absenden wirkungslos.
`IsCurrentUserAdminAsync` wird zwar geladen (Z. 1035), speist aber
nur `PlaylistDetailsState.IsAdmin` ohne jede tvOS-Oberfläche
(steuert auf MAUI den Öffentlich-Schalter) — konsistent zum
MAUI-Muster, kein Schreibpfad. Der Negativ-Contract-Test
`Pages_DoNotExposePlaylistManagement` sichert den Ausschluss.

**(e) Tests:** `dotnet test VideoPlayer.Maui.Tests` ausgeführt —
**1498/1498 bestanden, 0 Fehler** (50 s). Neue
`TvOsPlaylistsContractTests` (6 Tests): Übersicht/Sektionen +
Zellenbelegung, Detail/Paging/Identitäts-Guard, modale
Navigator-Präsentation, Menü-Verdrahtung, komplette
Effekt-Verdrahtung inkl. Cover-Endpunkt, Verwaltungs-Ausschluss.

**(f) tvOS-Kompilierfallen (Stichprobe):** `UITableView`/
`UITableViewSource`/`UITableViewCell`-Muster sind auf tvOS bereits
etabliert (`ServerListTableSource`, `ServerSetupViewController`);
`UITableViewStyle.Plain` mit `TitleForHeader` tvOS-tauglich;
`UICollectionViewSource.WillDisplayCell`, `UIPressType.Menu` +
`presses.ToArray<UIPress>()` (Muster aus drei Bestandsdateien),
`UIModalPresentationStyle.FullScreen`, `RemembersLastFocusedIndexPath`,
`NativeHandle`-Zellkonstruktor für `RegisterClassForCell` —
alles tvOS-/Binding-konform. Eine reale Falle wurde bereits vom CI
gefangen und im Folge-Commit `c99c95e` behoben
(`SeparatorStyle` existiert auf tvOS nicht — CS0117/CS1061, eine
entfernte Zeile); danach ist `build-tvos` auf HEAD grün — der
Kompiliernachweis selbst ist damit erbracht. `TvosPosterImageService`
nutzt die bestehende Kaskade (Speicher → Datei → Server) mit
eigenem Schlüsselraum und `.playlistcover`-Suffix; die
`?? []`-Faltung der `byte[]?`-Rückgabe von
`client.GetPlaylistCoverAsync` (`VideoPlayerApiClient.cs` Z. 630)
ist korrekt.

**(g) CI/Push:** `env -u GH_TOKEN gh run view 42132280331 --repo
martin-stromberg/VideoPlayer-App` → `status: completed`,
`conclusion: success`, `headBranch` = Feature-Branch, `headSha`
`2a7f3ef...` = HEAD = `origin/<branch>`; `git status` sauber,
keine ungepushten Commits.

**Weitere Beobachtungen (keine Abweichung):**

- `PlaylistDetailsState.CanManage` (`PlaylistDetailsState.cs` Z. 226)
  existiert plattformneutral weiter, hat auf tvOS aber keine
  Oberfläche — konsistent zum geteilten Modell.
- Die CI-Annotations zeigen ausschließlich Nullable-Warnungen in
  tvOS-Bestandsdateien (keine neuen aus diesem Schritt) sowie
  bekannte Altlasten (DisplayAlert-Obsolete, Node-20-Hinweis,
  NU1903 SignalR) — nicht Gegenstand dieses Schritts.
- `README.md`, `docs/help/tv-app/*` und `changes.log` sind
  konsistent zum lesenden Playlist-Zugriff aktualisiert;
  `review.md`/`review-code.md`/`test-results.md` des Schritts sind
  vorhanden und plausibel (eigener Testfehler im Erstlauf
  transparent dokumentiert und korrigiert).
