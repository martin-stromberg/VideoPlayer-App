# Plan-Gegenprüfung

Geprüft: `plan.md` gegen `requirement.md`, `inventory.md` + `inventory/` und die
tatsächliche Codebasis (stichprobenartig verifiziert: `AppShell.xaml(.cs)`,
`AppUpdate.cs`, `AppRoutes.cs`, `AppMessage.cs`, `MauiProgram.cs`,
`PosterImageService.cs`, `ShellAppEffectHandler.cs`, `MvuHost.cs` sowie die
genannten Testklassen in `VideoWebPlayer.Maui.Tests`).

## Ergebnis

**Status:** Plan lückenhaft

Die fachliche Umsetzung ist vollständig und korrekt gegen den Code beschrieben —
alle Akzeptanzkriterien sind abgedeckt, die offenen Fragen sind geklärt, die
Plan-Behauptungen zur Codebasis wurden verifiziert (u. a. `EnterConnected` Zeilen
430–465 ohne Quellen-Laden, `UpdateSourcesPageOpened` Zeilen 652–672,
`UpdateSourcesLoaded` Zeilen 674–687 ohne Connected-Guard, `UpdateSessionExpired`
Zeilen 573–595 ohne Quellen-Reset, `UpdateServerCheckCompleted` Zeilen 261–302,
`UpdateNetworkAvailabilityChanged` Zeilen 475–514, `UpdateLogoutRequested`
Zeilen 1231–1232 mit bereits vorhandenem Reset; `SourcesPageOpenedMessage` wird
nur aus `SourcesPage.xaml.cs` Zeilen 32/65 dispatcht, `CachedSourceIconImage` nur
in `SourcesPage.xaml` Zeile 65 genutzt, `PosterImageService` ist als Singleton in
`MauiProgram.cs` Zeile 70 registriert, `AppShell` in Zeile 94).

Lücke: Die Liste „Betroffene bestehende Tests" ist unvollständig — drei Tests mit
harten Effekt-Zähl-/Index-Assertions auf dem `EnterConnected`-Pfad werden durch
das vorgeschaltete `LoadSourcesEffect` brechen, ohne dass der Plan sie nennt.

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| 1. Pro freigegebener Quelle ein Menüeintrag mit Name und optionalem Icon (`api/sourceicons/{id}`); Daten = `GET api/Sources` | `LoadSourcesEffect` in `EnterConnected` + Sync-Übergängen → `SourcesOverview.Sources`; `AppShell` baut `MenuItem`s mit `Text`/`IconImageSource` über `PosterImageService.GetSourceIconSourceAsync` (Graceful Fallback ohne Icon) | `EnterConnected_StartsSourcesLoad`, `CachedListsChanged_WhenConnected_ReloadsSources`, `SynchronizationCompleted_WhenConnected_ReloadsSources`, `AppShell_BuildsSourceMenuItems_FromModel` (Contract pinnt `MenuItem`, `GetSourceIconSourceAsync`); Bestandstest `LoadSources_MapsServerList_WithIcons` (Server-Mapping inkl. `IconPictureId`) | Abgedeckt |
| 2. Auswahl öffnet `SourceOverviewPage` (`sourceoverview`); Genre-Filter, Suche, Paging wie bisher | `MenuItem.Clicked` → `SourceSelectedMessage` → unveränderter `UpdateSourceSelected`-Pfad (`NavigateToSourceOverviewEffect` + `LoadSourceGenresEffect` + `LoadSourceItemsEffect`) | Bestandstest `SourceSelected_NavigatesAndStartsInitialLoad` pinnt den Pfad; Contract-Test pinnt `SourceSelectedMessage`-Dispatch im Menüaufbau | Abgedeckt |
| 3. Quellenänderungen ohne App-Neustart sichtbar (nach Verbindung/Sync) | `EnterConnected` (Login, Auto-Login, Wiederverbindung, Serverwechsel) + `UpdateSynchronizationCompleted`/`UpdateCachedListsChanged` laden neu; `AppShell` reagiert auf `MvuHost.ModelChanged` mit Änderungsvergleich und Neuaufbau | `SynchronizationCompleted_WhenConnected_ReloadsSources`, `CachedListsChanged_WhenConnected_ReloadsSources`, `EnterConnected_StartsSourcesLoad`, `AppShell_BuildsSourceMenuItems_FromModel` (pinnt `ModelChanged`-Abonnement, `Items.Insert`/`Items.Remove`) | Abgedeckt |
| 4. Definiertes Offline-Verhalten (keine veralteten Einträge, keine Fehler) | `UpdateNetworkAvailabilityChanged` setzt `SourcesOverview = Initial` bei `Connected → Offline`; `UpdateSourcesLoaded` verwirft Antworten außerhalb `Connected`; Einträge werden ausgeblendet | `NetworkLost_FromConnected_ResetsSourcesOverview`, `SourcesLoaded_WhenNotConnected_IsIgnored`, `CachedListsChanged_WhenNotConnected_NoSourcesLoad` | Abgedeckt |
| 5. Übrige Menüeinträge (Startseite, Favoriten, Downloads, Profil, Einstellungen) bleiben erhalten und funktionsfähig | Nur der `FlyoutItem` „Quellen" wird aus `AppShell.xaml` entfernt; fünf statische `FlyoutItem`s bleiben; `MenuPages` schrumpft auf fünf | `AppShell_ContainsAllMenuEntries` (angepasst auf fünf), `AppShell_HasNoStaticSourcesFlyoutItem`, `AppRoutesTests`-Anpassungen | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

- [ ] **`AppUpdateTests_Connection.AutoLoginCompleted_Succeeded_ConnectsAndNavigatesHome`** (Zeilen 89–94) fehlt in „Betroffene bestehende Tests": `Assert.Equal(4, transition.Effects.Count)` plus Index-Pins (`Effects[0]` = `ConnectServerEventsEffect`, `[1]` = `RunFullSyncEffect`, `[2]` = `ResumeDownloadsEffect`, `[3]` = `NavigateToPageEffect`). Mit `LoadSourcesEffect` als erstem Effekt in `EnterConnected` werden es fünf Effekte und alle Indizes verschieben sich — der Test schlägt fehl.
- [ ] **`AppUpdateTests_Connection.LoginCompleted_Succeeded_ConnectsAndNavigatesHome`** (Zeilen 231–236) fehlt: identische `Assert.Equal(4, …)`- und Index-Pins — gleiche Bruchursache.
- [ ] **`MvuHostTests.DispatchAsync_UpdatesModelAndExecutesEffects`** (Zeilen 45–56) fehlt: `Assert.Equal(11, handler.Effects.Count)` mit Index-Pins `Effects[7]`/`[8]` = `ConnectServerEventsEffect`/`RunFullSyncEffect` — die `EnterConnected`-Effektkette des Dispatch-Laufs bekommt ein zwölftes Element mit verschobenen Indizes.
- [ ] **`AppUpdateTests_HomeLists.CachedListsChanged_ReloadsHomeLists`** (Zeile 84): Der Plan nennt den Test, aber nur die `Assert.Single`→`Contains`-Umstellung. Zusätzlich bricht `Assert.Same(model, transition.Model)` — `UpdateCachedListsChanged` ändert jetzt das Model (`SourcesOverview.IsLoading = true`). Die Anpassung muss beide Assertions adressieren.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Flyout öffnen → Quelleneinträge mit Name/Icon sehen (AK 1) | Kein E2E planbar | Nicht erforderlich mit Begründung (siehe Hinweise) |
| Quelleneintrag wählen → Titelübersicht mit Genre-Filter/Suche/Paging (AK 2) | Kein E2E planbar | Nicht erforderlich mit Begründung (siehe Hinweise) |
| Menü aktualisiert sich zur Laufzeit nach Sync (AK 3) | Kein E2E planbar | Nicht erforderlich mit Begründung (siehe Hinweise) |
| Offline → Menü ohne Quelleneinträge (AK 4) | Kein E2E planbar | Nicht erforderlich mit Begründung (siehe Hinweise) |

Der Plan begründet den Verzicht auf E2E-Tests nachvollziehbar und korrekt: Es
existiert keine UI-Automatisierung für die MAUI-App (kein Playwright/Appium), und
`VideoPlayer.Maui` kann vom Testprojekt nicht referenziert werden — beides in
`inventory/tests.md` (Zeilen 13, 32–33) dokumentiert und durch den
`SourcesContractTests`-Klassenkommentar (`VideoWebPlayer.Maui.Tests/
SourcesContractTests.cs` Zeilen 5–12) sowie das Fehlen jeder E2E-Suite im
Repository bestätigt. Als Ersatz sind die etablierten Quelltext-Contract-Tests
plus MVU-Übergangstests geplant; das Restrisiko der Laufzeitdarstellung führt der
Plan ausdrücklich in „Seiteneffekte und Risiken" (manueller Sichtcheck).

## Fehlende oder unvollständige Planbestandteile

Keine weiteren — die Umsetzungsschritte, Designentscheidungen, Voraussetzungen,
Reihenfolge und Risiken sind vollständig. Die fehlenden betroffenen Tests sind
oben unter „Fehlende oder unvollständige Testanforderungen" geführt.

## Hinweise

- **Kernannahme „MenuItems rendern positionsabhängig" ist nur manuell
  verifizierbar.** Der Plan behauptet (mit Verweis auf `Routing.IsImplicit`/
  `D_FAULT_…`-Standardroute), dass zur Laufzeit eingefügte `MenuShellItem`s an der
  Einfügeposition zwischen den `FlyoutItem`s im Standard-Flyout erscheinen. Die
  Anforderung selbst hatte das Gegenteil vermutet (MenuItems hinter allen
  FlyoutItems). Kein Contract-Test kann die tatsächliche Render-Position belegen —
  wenn die Annahme nicht stimmt, müsste auf die dokumentierte Alternative
  (gruppierte `ShellSection`s mit `AsMultipleItems` oder `FlyoutContent`)
  gewechselt werden. Empfehlung: diesen Punkt in der Implementierung zuerst per
  Sichtcheck klären, bevor die übrige Menülogik gebaut wird; ggf. den Contract-Test
  nicht auf `Items.Insert`-Details festnageln.
- **Deaktivierte Überschrift (`MenuItem` mit `IsEnabled = false`):** Darstellung
  und Tastatur-/Barrierefreiheits-Verhalten sind plattformabhängig — ebenfalls nur
  manuell prüfbar (im Plan als Plattform-Risiko bereits benannt).
- **Mehrverkehr:** `UpdateCachedListsChanged` feuert auch nach
  `SyncContinueWatchingEffect`/`SyncFavoritesEffect`/Favoriten-Umschaltungen — der
  Plan benennt das als tolerierbaren Mehrverkehr; die `IsLoading`-Sperre ist durch
  zwei `*_NoDuplicate`-Tests abgesichert. Nachvollziehbar.
- **Risiko-Abdeckung Sitzung/Serverwechsel ist gut durchdacht:**
  `UpdateSessionExpired`- und `UpdateServerCheckCompleted`-Resets schließen die
  Lücken, die `UpdateLogoutRequested` nicht abdeckt (verifiziert: Logout-Reset in
  `AppUpdate.cs` Zeilen 1231–1232 existiert bereits; SessionExpired Zeilen
  573–595 und ServerCheckCompleted Zeilen 261–302 setzen die Quellenzustände
  heute nicht zurück).
- Kleinigkeit: Die CI führt `dotnet format whitespace --verify-no-changes` aus
  (laut `inventory/tests.md` Zeile 11) — im Build-/Test-Schritt nicht erwähnt,
  schadet aber nichts.
