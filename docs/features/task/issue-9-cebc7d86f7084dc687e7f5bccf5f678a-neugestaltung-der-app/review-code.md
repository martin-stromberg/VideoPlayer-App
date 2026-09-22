# Code-Review (Runde 3)

**Status:** Keine Befunde

Review-Scope: uncommitted Änderungen auf `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app`. Geprüfter neuer Code seit Runde 2: `SourceMenuKind`-Enum + `MenuKindFor` (Priorität Error > Sources > Loading > Empty), `renderedKind` statt `renderedHasError`, `SourceInfoTemplate`, Statuszeilen Loading/Empty über `InsertInfoEntry`, `FlyoutIsPresented = false` im Quellen-Clicked, Helfer `InsertMenuEntry`/`InsertInfoEntry`.

## Nachprüfung der Vor-Runden

- **review-code.1/.2:** Alle vier Befunde bleiben behoben und unverändert korrekt — Model erst im Main-Thread-Callback gelesen (`AppShell.xaml.cs:122-141`), Erstbefüllung im Konstruktor (Z.86-97), Marker erst nach erfolgreichem Aufbau (Z.158-164), `WithSourcesReload`-Kapselung (`AppUpdate.cs:580-592`).
- **review-usability.2:** Alle drei Befunde adressiert — Befund 1 (offenes Flyout) durch `FlyoutIsPresented = false` im Quellen-Clicked (Z.256); Befund 2 (fehlender Ladehinweis) durch `Loading`-Statuszeile (Z.243-245); Befund 3 (stiller Leerstand) durch `Empty`-Statuszeile (Z.246-248). Der Neuversuch-Eintrag schließt bewusst nicht — deckt sich mit der eigenen Einschätzung der Usability-Runde.

## Geprüfte Schwerpunkte

**Zustands-Priorität `MenuKindFor` (Z.169-192) — korrekt und vollständig:**

- *Error+Sources gleichzeitig:* `UpdateSourcesLoaded` (`AppUpdate.cs:719-729`) übernimmt `Sources = message.Sources` und `Error = message.Error` aus derselben Nachricht — die Kombination ist typseitig möglich. Der Effekt-Handler (`ShellAppEffectHandler.cs:428-444`) erzeugt sie jedoch nie: Im Fehlerfall bleibt `sources = []`. Die defensive Behandlung (Retry-Zeile + Restbestand, Enum-Kommentar Z.44) ist trotzdem korrekt.
- *IsLoading+Error:* unerreichbar — jeder Pfad, der `IsLoading = true` setzt (`EnterConnected` Z.451, `WithSourcesReload` Z.590), setzt gleichzeitig `Error = null`; `UpdateSourcesLoaded` erzeugt einen frischen State mit `IsLoading = false`.
- *IsLoading+Sources* (Reload mit behaltenen Einträgen): `Sources` gewinnt korrekt vor `Loading` — Einträge bleiben während des Reloads sichtbar, keine Ladezeile ersetzt die Liste (kommentiert Z.171-175; gepinnt durch `EnterConnected_StartsSourcesLoad`).
- *ShowEmpty vs. IsLoading:* `ShowEmpty = Attempted && !IsLoading && !HasError && !HasSources` (`SourcesOverviewState.cs:35`) schließt IsLoading und Error per Definition aus — die Reihenfolge in `MenuKindFor` ist damit lückenlos; `None` bleibt der reine Initial-Stand (offline/nicht verbunden/vor dem ersten Lauf).

**Dedup-Logik mit `renderedKind` (Z.143-164) — vollständig:**

- Der Dedup-Schlüssel (kind, Quelleninhalt) erfasst jeden darstellungsrelevanten Wechsel: None/Loading/Empty/Error/Sources unterscheiden sich im Kind; Sources→Sources mit geänderter Liste erkennt `SameSources` (Z.194-201, paarweiser Id/Name/IconPictureId-Vergleich).
- Alle übersprungenen Rebuilds sind darstellungsäquivalent: Reload mit unveränderten Quellen (IsLoading toggelt, kind bleibt `Sources`) flackert nicht; ein geänderter Fehlertext bei gleichen Quellen bleibt ohnehin unsichtbar (Retry-Zeile hat festen Text).
- `renderedKind`/`renderedSources` werden erst nach erfolgreichem `InsertSourceMenuItems` gesetzt — ein Fehlschlag pinnt keinen Halbzustand (teileingefügte Items sind über `sourceMenuItems` getrackt und werden beim nächsten `ModelChanged` entfernt).
- Initialwert `renderedKind = None` (Enum-Erstelement) passt zum Initial-Stand `SourcesOverviewState.Initial` → `MenuKindFor` = `None` → sauberer Skip beim ersten Lauf.

**`FlyoutIsPresented = false` vor dem Dispatch (Z.253-258) — korrekte Reihenfolge:** Das Schließen läuft synchron im Clicked-Handler (Main-Thread, sofortige Rückmeldung); der fire-and-forget Dispatch führt die eigentliche Navigation erst später gemarshallt aus (`NavigateToSourceOverviewEffect` → `InvokeOnMainThreadAsync`, `ShellAppEffectHandler.cs:138-139, 409-426`). Das bewusste Nicht-Schließen beim Retry (Z.240) ist kommentiert im Test und per Vorkommenszählung gepinnt.

**Test-Pins — vollständig für die Repo-Konvention:** `AppShell_ShowsStatusRows_ForLoadingAndEmpty` (beide Statustexte + `overview.IsLoading`/`overview.ShowEmpty`/`sourceInfoTemplate`), `AppShell_ClosesFlyout_OnSourceSelection` (exakt ein `FlyoutIsPresented` → pinnt zugleich das Nicht-Schließen beim Retry), `AppShell_ShowsRetryEntry_OnSourcesError`, `AppShell_DefinesSourceSectionAndErrorTemplates` (alle drei Template-Keys, in `AppShell.xaml` Z.14/26/35 vorhanden), `AppShell_MarksRenderedStateOnlyAfterSuccessfulBuild` (Positionspin Insert vor Marker-Setzung — Z.155 < 162 erfüllt). MVU-Seite: Reload/Retry/Offline/Empty/Fehler in `AppUpdateTests_Sources` gepinnt.

**XML-Doku:** Enum samt Membern und `MenuKindFor` dokumentiert; CS1591 ist per `WarningsAsErrors` für öffentliche Member Build-Fehler — keine Lücke. `SourcesOverviewState`- und `AppModel.SourcesOverview`-Doku stimmen mit dem Verhalten überein; `docs/help/quellen-browsing-details.md` (Z.18-28) beschreibt Loading-/Empty-/Retry-Zeilen korrekt.

**Threading — unverändert korrekt:** `ModelChanged` feuert nach `gate.Release()` (`MvuHost.cs:58-62`) auf dem Dispatch-Thread; Marshal via `BeginInvokeOnMainThread` mit Model-Lesezugriff im Callback; Clicked-Handler und `FlyoutIsPresented` laufen auf dem Main-Thread; Icon-Setzen gemarshallt (Z.314); Clicked-Lambdas hängen an den verworfenen MenuItems (Referenzrichtung MenuItem → AppShell, kein Leak). Farbressourcen `Gray500`/`Gray400` des neuen `SourceInfoTemplate` existieren (`Resources/Styles/Colors.xaml:25-26`).

## Hinweise (keine Befunde)

- *Error+Sources* (Retry + Restbestand gleichzeitig) ist mit dem aktuellen Effekt-Handler unerreichbar — `LoadSourcesAsync` liefert im Fehlerfall immer eine leere Liste. Der Rendering-Pfad „Quelleneinträge nach der Fehlerzeile" ist defensive Reserve; korrekt, aber aktuell wirkungslos.
- Die Prioritätsreihenfolge von `MenuKindFor` ist als private Methode der im Testprojekt nicht instanziierbaren MAUI-Klasse nur über String-Pins (Vorhandensein von `overview.HasError`/`IsLoading`/`ShowEmpty`) abgesichert — eine spätere Umordnung (z. B. Loading vor Sources) erkennt kein Test. Durch den erklärenden Kommentar (Z.171-175) gemildertes Restrisiko.
- Die Klassen-`<summary>` von `AppShell` (Z.6-17) erwähnt Quelleneinträge und Neuversuch-Eintrag, nicht die neuen Statuszeilen Loading/Empty — kosmetische Doku-Lücke.
- `LoadSourceIconAsync` (Z.307-323) kann `IconImageSource` auf ein zwischenzeitlich durch einen Rebuild verworfenes `MenuItem` setzen — harmlos (Item wird mit dem Menüstand verworfen), aus den Vor-Runden bekannt.
- `SourcesLoadedMessage` im Zustand `Connecting` ist weiterhin nicht explizit getestet (Hinweis aus Runde 2, unverändert zutreffend; der Guard verwirft sie korrekt).

## Verifikationsnotiz

Statische Vollprüfung des uncommitteten Stands: alle Scope-Dateien vollständig gelesen, Aufrufpfade über `MvuHost`, `ShellAppEffectHandler` und `SourceBrowserService` nachverfolgt, Vor-Reviews (`review-code.1/.2`, `review-usability.1/.2`) gegen den Code nachgeprüft. Build/Test-Ausführung durch den koordinierenden Agenten (s. test-results.md). Die neuen Contract-Tests sind String-Pins: sie erwarten u. a. exakt ein `FlyoutIsPresented`-Vorkommen (`AppShell.xaml.cs:256` erfüllt) und die drei Template-Keys in `AppShell.xaml` (erfüllt).
