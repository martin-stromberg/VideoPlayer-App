# Usability-Review: Dynamische Quellen-Einträge im Navigationsmenü

Review-Scope: uncommitted Änderungen (AppShell.xaml/.cs, VideoPlayer.Mvu Quellen-Laden/Reload/Reset,
gelöschte SourcesPage, Tests, README, docs/help). Perspektive: nicht-technische Endanwenderin.

**Status:** Befunde vorhanden

## Befunde

### Befund 1 (Hauptbefund): Quellen-Eintrag antippen lässt das Menü offen — die geöffnete Seite bleibt hinter dem Menü verborgen

**Fundstelle:** `VideoPlayer.Maui/AppShell.xaml.cs` Z. 198–199 (`entry.Clicked` →
`DispatchSourceSelectedAsync`), Z. 187–189 (Neuversuch-Eintrag), Z. 224–234. Im gesamten
`VideoPlayer.Maui`-Projekt gibt es kein `FlyoutIsPresented`-Handling (grep-Verifikation).

**Beschreibung:** Die dynamischen Einträge sind `MenuItem`-Objekte mit `Clicked`-Handler. In .NET MAUI
schließt die Shell den Flyout beim Antippen eines `MenuItem` **nicht** selbst: In
`Shell.OnFlyoutItemSelectedAsync` führt der `MenuShellItem`-Fall zu `Activate()` und anschließendem
Early-Return (`shellItem == null`), **bevor** `FlyoutIsPresented` auf `false` gesetzt wird — das
Schließen erfolgt nur für navigierende `ShellItem`s. Der Clicked-Handler dispatcht
`SourceSelectedMessage`, der Effekt schiebt `sourceoverview` per `GoToAsync` auf den Stack — ohne den
Flyout zu schließen.

**Auswirkung aus Anwendersicht (plattformabhängig):**

- **Android / iOS / MacCatalyst** (alle echte Build-Ziele, `VideoPlayer.Maui.csproj` Z. 3–5): Das
  Seitenmenü bleibt offen, die aufgeschobene `SourceOverviewPage` liegt dahinter. Die Anwenderin tippt
  „Filme" an und sieht **keinerlei Reaktion**. Wahrscheinliches Folgeverhalten: erneutes Antippen —
  jeder Tap pusht eine weitere Seite auf den Stack (mehrfach gestapelte `SourceOverviewPage`s,
  Zurück-Taste poppt sie einzeln). Erst manuelles Schließen des Menüs zeigt die geöffnete Quelle.
- **Windows**: Die `NavigationView`-Pane klappt bei Elementwahl im Minimal-/Kompakt-Modus
  plattformseitig zu — dort vermutlich kein sichtbares Problem. Die Umsetzung verlässt sich also auf
  ein Platform-Detail statt auf garantiertes Shell-Verhalten.

Für den **Neuversuch-Eintrag** ist das offene Menü sogar hilfreich (die Fehlerzeile verschwindet
sichtbar als Bestätigung) — der Befund betrifft primär die Quellen-Einträge.

**Empfehlung:** In den `Clicked`-Handlern (zumindest für Quellen-Einträge) `FlyoutIsPresented = false`
auf der Shell setzen, analog zum etablierten MAUI-Muster für Flyout-`MenuItem`s. Auf einem
Android-/iOS-Gerät bzw. Emulator verifizieren — die Contract-Tests prüfen nur Quelltext, nicht das
Laufzeitverhalten des Flyouts.

### Befund 2 (klein): Kein Ladehinweis im Menü — während des Ladens fehlt jede Rückmeldung

**Fundstelle:** `AppShell.xaml.cs` `UpdateSourceMenuItems` Z. 122–143 (Einfügen nur bei
`hasError || Sources.Count > 0`), `WithSourcesReload` in `VideoPlayer.Mvu/AppUpdate.cs` Z. 580–592.

Während ein Ladevorgang läuft (Erstladen beim Verbindungsaufbau, Neuversuch), wird im Menü nichts
angezeigt — weder eine Überschrift noch ein „wird geladen …". Öffnet die Anwenderin das Menü in diesem
Zeitraum, sieht sie schlicht keine Quellen und kann „lädt gerade" nicht von „keine Quellen
freigegeben" unterscheiden. Beim Antippen von „erneut versuchen" verschwindet die rote Zeile sofort
(wenn der Flyout offen bleibt, ist das eine implizite Bestätigung; ist er geschlossen, sieht sie bis
zum nächsten Öffnen nichts). Auf langsamen Verbindungen kann der Lauf einige Sekunden dauern.

**Empfehlung (optional):** Während `IsLoading` eine dünne Zeile wie „Quellen werden geladen …" unter
der Überschrift zeigen oder den Fehler-/Retry-Eintrag bis zum Ergebnis stehen lassen.

### Befund 3 (klein): Erfolgreich geladen, aber null Quellen → stiller Leerstand

**Fundstelle:** `AppShell.xaml.cs` Z. 132 (`if (hasError || overview.Sources.Count > 0)`);
`SourcesOverviewState.ShowEmpty` (`VideoPlayer.Mvu/SourcesOverviewState.cs` Z. 35) existiert, wird
aber nicht ausgewertet.

Hat der Server-Benutzer keine freigegebene Quelle (oder wurden alle Freigaben entzogen), erscheint
nach erfolgreichem Laden weder Überschrift noch ein Hinweis — die Sektion fehlt kommentarlos. Nach
einem Entzug aller Freigaben verschwindet die Gruppe bei der nächsten Synchronisation lautlos aus dem
Menü. Das ist definiertes, fehlerfreies Verhalten, für eine Endanwenderin aber nicht von „noch nicht
geladen" unterscheidbar.

**Empfehlung (optional):** Bei `ShowEmpty` eine nicht klickbare Zeile „Keine Quellen freigegeben"
unter der Überschrift zeigen — spiegelt den Zustand, den `SourcesOverviewState` ohnehin schon kennt.

## Unauffällig geprüfte Bereiche

- **Erreichbarkeit / Platzierung:** Die Quellen stehen als eigene, benannte Einträge direkt hinter
  „Startseite" (`Items.Insert` ab Index 1, `AppShell.xaml.cs` Z. 171–180); MAUI rendert `MenuItem`s
  positionsabhängig in Deklarationsreihenfolge (Microsoft-Doku bestätigt). Auswahl nur über sichtbare
  Namen, keine internen Kennungen — erfüllt die Auswahl-/Identifikationsanforderung.
- **Ladefehler-Anzeige + Retry:** Bei Fehlschlag steht unter der Überschrift der klickbare Eintrag
  „Quellen konnten nicht geladen werden — erneut versuchen" (`AppShell.xaml.cs` Z. 187–193), rot und
  fett (`SourceErrorTemplate`, `AppShell.xaml` Z. 26–31) — konsistent zum Fehlerstil der App
  (`TextColor="Red"` auf allen `ErrorLabel`s). Der Tap dispatcht `SourcesReloadRequestedMessage`
  → `LoadSourcesEffect` (`AppUpdate.cs` Z. 732–742); Guards verhindern Reloads außerhalb von
  `Connected` und Doppelläufe (`AppUpdateTests_Sources.SourcesReloadRequested_*` gepinnt). Der
  Vor-Runden-Befund „unsichtbarer Ladefehlschlag ohne Ausweg" ist damit behoben.
- **Unterscheidbarkeit Überschrift vs. Eintrag:** Die Überschrift nutzt ein eigenes Template
  (`SourceSectionTemplate`, `AppShell.xaml` Z. 14–21): kleiner (12), fett, abgesetztes Grau
  (Gray600/Gray300, theme-aware), oberer Abstand — wirkt als Abschnittstitel statt als deaktivierte
  Menüzeile; `IsEnabled = false` verhindert jede Reaktion auf Antippen. Der Vor-Runden-Befund
  „scheinbar kaputter Eintrag" ist deutlich entschärft. Restrisiko (klein): Screenreader könnten die
  Zeile als deaktivierten Menüpunkt ansagen — für eine Sektionsüberschrift vertretbar.
- **Offline-Verhalten:** `UpdateNetworkAvailabilityChanged` setzt `SourcesOverview` beim Übergang
  Connected→Offline zurück (`AppUpdate.cs` Z. 496–504); verspätete `SourcesLoadedMessage`-Antworten
  werden außerhalb von `Connected` verworfen (Z. 714–717). Auch Abmeldung (Z. 1286), Sitzungsablauf
  (Z. 639) und Serverwechsel (Z. 302) räumen den Menüstand ab — keine veralteten oder fremden Einträge.
  Keine toten Menülinks offline; konsistent mit Anforderung und `docs/help/offline-wiederherstellung.md`.
- **Übrige Menüeinträge:** Die fünf statischen `FlyoutItem`s (Startseite, Favoriten, Downloads,
  Profil, Einstellungen) sind unverändert (`AppShell.xaml` Z. 34–48; Contract-Test pinnt Titel, Routen
  und Anzahl).
- **Icons:** Werden asynchron nachgeladen (`LoadSourceIconAsync`, Z. 236–252) über
  `api/sourceicons/{id}`; `MenuShellItem` bindet `Icon` an `MenuItem.IconImageSource`, das Nachladen
  propagiert korrekt. Quellen ohne Icon oder mit Ladefehler zeigen weiterhin den Namen — wie in der
  Webanwendung.
- **Aktualisierung zur Laufzeit:** Laden beim Verbindungsaufbau (`EnterConnected`, Z. 451–454), nach
  Voll-Sync (`UpdateSynchronizationCompleted`, Z. 542) und nach Teil-Syncs/Server-Ereignissen
  (`UpdateCachedListsChanged`, Z. 559; `NewVideosScanned` → `SyncMetadataEffect` →
  `CachedListsChangedMessage`) — Freigaben/Entzüge werden ohne Neustart sichtbar.
- **Kein Menü-Flackern:** `SameSources`-Dedup (Z. 145–152) lässt unveränderte Stände stehen; während
  eines Reloads bleiben bisherige Einträge sichtbar (`EnterConnected` belegt nur `IsLoading`).
- **Fehler beim Reload mit vorhandenen Einträgen:** Ein fehlschlagender Reload ersetzt die bisherigen
  Einträge durch die Fehler-/Retry-Zeile (`LoadSourcesAsync` liefert bei Ausnahme eine leere Liste,
  `ShellAppEffectHandler.cs` Z. 428–444). Ehrliches Signal statt still veralteter Einträge — mit
  sichtbarem Ausweg.
- **Dokumentation:** `docs/help/quellen-browsing-details.md` (Menü-Verhalten, Offline, Retry),
  `app-grundgeruest.md`, `offline-wiederherstellung.md`, `index.md` und `README.md` beschreiben das
  implementierte Verhalten korrekt; keine verwaisten Verweise auf die entfernte `SourcesPage`.
