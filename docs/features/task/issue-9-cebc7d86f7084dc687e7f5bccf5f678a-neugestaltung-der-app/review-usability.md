# Usability-Review (Runde 3) — Dynamische Quellen-Einträge im Navigationsmenü

**Status:** Befunde vorhanden

Geprüft aus Endanwender-Sicht: `VideoPlayer.Maui/AppShell.xaml(.cs)`, `VideoPlayer.Mvu/{AppUpdate,AppMessage,AppModel,AppRoutes,AppPage,SourcesOverviewState,SourceItem}.cs`, `ShellAppEffectHandler.cs`, `MvuHost.cs`, `MauiProgram.cs`, Tests (`SourcesContractTests`, `AppUpdateTests_Sources`, `AppRoutesTests`), README.md, `docs/help/*`. Fachliche Referenz: `requirement.md` (Übersetzte Anforderung). Kein Laufzeit-Test möglich — Aussagen zum Flyout-Rendering sind statisch verifiziert.

## Befunde

### 1. Fehlgeschlagener (Neu-)Ladevorgang löscht alle vorhandenen Quellen-Einträge — mittel

**Fundstellen:**
- `VideoPlayer.Mvu/AppUpdate.cs:719-729` (`UpdateSourcesLoaded`): `Sources = message.Sources` wird unverändert übernommen.
- `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs:428-444` (`LoadSourcesAsync`): bei Ausnahme wird `sources = []` mit `error` dispatcht — eine `SourcesLoadedMessage` mit Fehler trägt also immer eine leere Liste.
- `VideoPlayer.Maui/AppShell.xaml.cs:44-45` und `:171-178` (`MenuKindFor`-Kommentar: „Fehler schlägt Quellen (Retry-Eintrag zusätzlich zu etwaigem Restbestand)") sowie `InsertSourceMenuItems` (Retry-Zeile + `foreach` über `overview.Sources`).

**Beschreibung:** Der kombinierte Zustand „Fehlerzeile + weiterhin sichtbare Quelleneinträge" ist vom Design vorgesehen (`SourceMenuKind.Error` rendert die Retry-Zeile und danach alle Einträge), aber **unerreichbar**: Bei jedem Ladefehlschlag wird `Sources` durch die leere Ergebnisliste ersetzt. Die `EnterConnected`-Absicht „die bisherigen Einträge bleiben während des Ladens sichtbar (kein Menü-Flackern)" (AppUpdate.cs:448-451) gilt nur für die Ladephase — beim Fehlschlag verschwinden alle Einträge trotzdem.

**Endanwender-Sicht:** Reloads laufen auch ohne Nutzeraktion im Hintergrund — nach jeder Synchronisation (`UpdateSynchronizationCompleted`, AppUpdate.cs:542) und jeder `CachedListsChangedMessage` (AppUpdate.cs:559), die u. a. nach jeder Favoriten-Umschaltung/-Entfernung ausgelöst wird (ShellAppEffectHandler.cs:528, 581). Ein einziger transienter Fehler (Timeout, kurzer Serverhänger) lässt sämtliche Quellen-Einträge kommentarlos aus dem Menü verschwinden; übrig bleibt nur „Quellen konnten nicht geladen werden — erneut versuchen". Die Nutzerin verliert ihre funktionierenden Einstiege, bis ein manueller Retry gelingt.

**Empfehlung:** Bei Fehlschlag den bisherigen Bestand behalten, z. B. `Sources = message.Error is null ? message.Sources : model.SourcesOverview.Sources` in `UpdateSourcesLoaded`. Dann tritt die entworfene Darstellung (Retry-Zeile über den weiterhin antippbaren Einträgen) ein; eine tatsächlich entzogene Quelle zeigt auf der `SourceOverviewPage` ohnehin deren eigenen Fehler-/Retry-Zustand.

### 2. Erneuter Tap auf die bereits geöffnete Quelle schiebt eine identische Seite — gering

**Fundstellen:** `ShellAppEffectHandler.cs:409-426` (`NavigateToSourceOverviewAsync` pusht `sourceoverview` ohne Dedup), `AppUpdate.cs:777-780` (`SourceBrowserOpenedMessage` mit gleicher `SourceId` ist No-op).

**Beschreibung:** Das Flyout bleibt auf geschobenen Seiten per Wisch-Geste erreichbar (`FlyoutBehavior="Flyout"`). Steht die `SourceOverviewPage` einer Quelle bereits oben auf dem Stack, erzeugt ein erneuter Menü-Tap auf dieselbe Quelle eine zweite, optisch identische Seite. „Zurück" landet auf der ersten Kopie — für die Nutzerin sieht es aus, als sei nichts passiert.

**Empfehlung:** Vor dem Push prüfen, ob die oberste Route bereits `sourceoverview?id={SourceId}` ist, und den Push in dem Fall unterlassen (bzw. `UpdateSourceSelected` bei gleicher `SourceBrowser.SourceId` ohne Navigation nur neu laden).

## Hinweise (kein Befund, ggf. Verbesserung)

- **Offline verschwindet die komplette „Quellen"-Sektion ohne Erklärung im Menü** (`SourcesOverview = Initial` beim Offline-Übergang, AppUpdate.cs:496-504). Von der Anforderung gedeckt (offene Frage 2, Variante „ausblenden"). Die Offline-Banner auf Startseite („Neue Inhalte, Quellen und Profildaten sind nicht verfügbar", `HomePage.xaml:19`), Downloads, Favoriten und Profil erklären den Kontext — auf der **Einstellungen-Seite fehlt ein solches Banner**; von dort aus ist der Grund des Verschwindens nicht ersichtlich. Eine Statuszeile „Offline — keine Quellen verfügbar" wäre die erklärendere Alternative.
- **Retry-Eintrag ist nur roter Fettdruck** (`SourceErrorTemplate`, AppShell.xaml:26-31) — ohne Icon oder Unterstreichung. Der Text „— erneut versuchen" macht die Tappbarkeit hinreichend klar; ein ↻-Symbol würde sie noch deutlicher machen.
- **Deaktivierte Zeilen sind tappable No-ops:** Ein Tap auf den Sektionstitel „Quellen" oder die Statuszeilen bewirkt nichts (kein `Clicked`-Handler, `IsEnabled=false`), das Flyout bleibt offen. Wer „Quellen" als aufklappbare Gruppe erwartet, erhält keine Reaktion — unkritisch.
- **Ladezeile ohne Timeout-Indikator:** „Quellen werden geladen …" hängt vom `HttpClient`-Timeout ab (kein eigener Timeout in `LoadSourcesAsync`); bei hängender Verbindung steht die Zeile lange, bis der Fehlschlag die Retry-Zeile bringt.

## Unauffällig geprüfte Bereiche

- **Flyout schließt bei Quellen-Tap:** `FlyoutIsPresented = false` vor dem Dispatch (AppShell.xaml.cs:254-258) — die Übersichtsseite wird nicht vom offenen Menü überdeckt. Der Retry-Eintrag schließt bewusst nicht; das Feedback ist der Zeilenwechsel zu „Quellen werden geladen …" (IsLoading-Übergang über `WithSourcesReload`), danach Einträge oder erneute Fehlerzeile — im offenen Menü sichtbar.
- **Statuszeilen verständlich und zustandskorrekt:** „Quellen werden geladen …" nur bei `IsLoading` + leerem Stand (kein Flackern „Keine Quellen freigegeben" während des Ladens); „Keine Quellen freigegeben" nur nach erfolgreichem leerem Lauf (`ShowEmpty` = `Attempted && !IsLoading && !HasError && leer`, SourcesOverviewState.cs:35); Fehler priorisiert. Texte klar und in der Doku dokumentiert (`quellen-browsing-details.md:18-28`).
- **Offline-/Reset-Pfade vollständig:** Reset bei Connected→Offline (AppUpdate.cs:501), Serverwechsel (Z. 302), Sitzungsablauf (Z. 639), Abmeldung (Z. 1286); verspätete `SourcesLoadedMessage` außerhalb `Connected` wird verworfen (Z. 714-717) — keine veralteten Einträge offline oder nach Logout.
- **Keine toten/kaputten Einträge:** `SourcesPage`, `CachedSourceIconImage`, `Route="sources"`, `AppPage.Sources` restlos entfernt (grep-clean außerhalb von Doku/Tests); fünf `FlyoutItem`s intakt (Startseite/Favoriten/Downloads/Profil/Einstellungen); `sourceoverview`-Route registriert; `TryGetPage` trifft `sourceoverview` nicht → `CurrentPage` bleibt konsistent.
- **Icons:** asynchron nachgeladen (`LoadSourceIconAsync`, AppShell.xaml.cs:307-323); Fehlschlag oder fehlendes Icon → Quellenname bleibt als Fallback sichtbar, kein kaputtes Bild.
- **Menü-Aufbau robust:** Vergleich über `SameSources` (Id/Name/IconPictureId) verhindert unnötige Neuaufbauten; `renderedSources`/`renderedKind` werden erst nach erfolgreichem Einfügen markiert; UI-Callback in try/catch; Erstbefüllung der lazy erzeugten Shell greift (AppShell.xaml.cs:86-97); getrackte `MenuShellItem`-Instanzen stimmen mit den eingefügten überein (Kommentar Z. 224-227).
- **Menü-Position:** Sektion steht hinter „Startseite" (`Items.Insert(1, …)`), Überschrift „Quellen" als Abschnittstitel — analog der Webanwendung (offene Frage 3 aufgelöst).
- **Akzeptanzkriterien 1-5:** Eintrag je freigegebener Quelle mit Name/Icon ✓; Auswahl öffnet direkt `sourceoverview` mit Genre/Suche/Paging (unveränderter `SourceSelected`-Pfad) ✓; Änderungen ohne Neustart über Verbindungsaufbau/Sync/Server-Ereignisse ✓; Offline definiert (Sektion entfernt, keine Abstürze) ✓; übrige Einträge erhalten ✓.
- **Tests:** Contract-Tests pinnen Flyout-Schließen (genau ein `FlyoutIsPresented`-Vorkommen), Statuszeilen, Retry-Eintrag, Templates, fünf FlyoutItems, PosterImageService-Verdrahtung; Update-Tests pinnen alle Lade-/Reset-/Retry-Übergänge inkl. Guards gegen Doppelläufe und Not-Connected.
- **Doku konsistent:** README.md und `docs/help/{quellen-browsing-details,offline-wiederherstellung}.md` beschreiben das implementierte Verhalten korrekt; keine verwaisten Verweise auf die entfernte `SourcesPage`.

**Nicht prüfbar (Hinweis an den Prüfer):** Laufzeitverhalten des Flyouts (Position und Darstellung der zur Laufzeit eingefügten `MenuShellItem`s zwischen den `FlyoutItem`s, Tappbarkeit deaktivierter Zeilen je Plattform) konnte nur statisch verifiziert werden — ein manueller Sichtcheck auf Windows/iOS wäre sinnvoll.
