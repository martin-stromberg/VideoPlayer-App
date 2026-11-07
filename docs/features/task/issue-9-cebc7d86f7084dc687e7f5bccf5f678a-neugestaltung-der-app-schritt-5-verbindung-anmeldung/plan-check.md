# Plan-Gegenprüfung

Geprüft: `requirement.md`, `inventory.md` (inkl. Detaildokumente) gegen
`plan.md`. Hinweis: Ausführung ohne Unteragenten (dokumentierte Abweichung).

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Start-Workflow in vier Zustandsausgängen (verbunden, Anmeldung, Servereinrichtung, offline) | `ConnectionState` im `AppModel`; Verzweigungen in `AppUpdate` (`ServersLoadedMessage` ohne Adresse → `NeedsServerSetup`; Health erreichbar → `AttemptAutoLoginEffect` → `Connected`/`NeedsLogin`; nicht erreichbar → `CheckOfflineDataEffect` → `Offline`/`NeedsServerSetup`); Navigation über `NavigateToPageEffect` | `AppUpdateTests_Connection` (alle vier `StartupFlow_*`-Sequenzen), `MvuHostTests`-Sequenz | Abgedeckt |
| Fehlerhafte Serverantworten nach Strategie wiederholt (3 Versuche, 1 s/2 s/4 s; kein Retry bei eindeutigen Clientfehlern außer 401/408/429) | Bereits in `VideoPlayerApiClient.SendWithPolicyAsync` implementiert (Schritt 1); Plan vermerkt explizit „keine Änderung" | `VideoPlayerApiClientTests_RetryPolicy` (Bestand, dokumentiert als Nachweis) | Abgedeckt |
| Abgelaufener Token → stille Neuanmeldung statt manueller Anmeldung | `AuthSessionService` verdrahtet `TokenRefresher` mit Neuanmeldung über gespeicherte Zugangsdaten (eigener kurzlebiger Client); `SetAuthTokenAsync` persistiert das neue Token | `AuthSessionServiceTests.Client_401_SilentRelogin_ResendsWithNewToken` (E2E über HTTP-Stub), `SilentRelogin_Rejected_RaisesSessionExpired` | Abgedeckt |
| Backup-Wiederherstellung: automatische Verbindung zum letzten Server, Anmeldeaufforderung nur bei fehlenden Geheimnissen | `backup_state.LastConnectedServer` ist backup-pflichtig (Schritt 4); `SecureStorage`-Geheimnisse fehlen nach Restore → `TryStoredLoginAsync` → `MissingCredentials` → `LoginPage` mit `LoginUsername`-Vorbefüllung | `AppUpdateTests_Connection.RestoreFlow_*`, `AuthSessionServiceTests.TryStoredLoginAsync_NoCredentials_ReturnsMissing` | Abgedeckt |
| Ladeanzeige beim Start mit automatischem Verbindungsversuch | Vorhanden (`LoadingPage`); `StartupStatusText` ergänzt „Anmeldung läuft"-Phase | `LoadingPage`-Bindung via Contract-Test; MVU-Sequenztests | Abgedeckt |
| Anmeldemaske bei fehlenden/fehlgeschlagenen Zugangsdaten | `AppPage.Login`, `LoginPage`, `LoginSubmittedMessage`/`AttemptLoginEffect`/`LoginCompletedMessage` | `LoginFlow_*`-Tests, `ConnectionWorkflowContractTests` | Abgedeckt |
| Serverauswahl bei Nichterreichbarkeit ohne Offline-Daten | `OfflineDataCheckCompletedMessage(false)` → `NeedsServerSetup` + `ServerSetupPage` (bestehende Auswahl: Liste, Discovery, manuelle Eingabe — keine internen Kennungen) | `Update_OfflineDataCheckCompleted_WithoutData_GoesServerSetup` | Abgedeckt |
| Verbindungszustände durchgehend bekannt | `AppModel.ConnectionState` wird bei jedem Übergang gesetzt | Assertions in allen Sequenztests | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Keine.

## E2E-Abdeckung

Das Repository hat kein UI-Automatisierungs-Framework; das Testprojekt kann
`VideoPlayer.Maui` nicht referenzieren (Bestandsaufnahme, `tests.md`).
Etablierter Ersatz (Schritte 2–4): vollständige Message-Sequenz-Tests auf
MVU-Ebene plus Contract-Tests der UI-Verdrahtung. Diese Konvention ist im Plan
als E2E-Nachweis verankert.

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Start → verbunden → Startseite | `AppUpdateTests_Connection.StartupFlow_*` (Sequenz bis `NavigateToPageEffect(Home)`) | Abgedeckt |
| Start → Anmeldemaske → manuelle Anmeldung → Startseite | `AppUpdateTests_Connection.StartupFlow_LoginRequired_*` + `LoginFlow_*` | Abgedeckt |
| Start → Servereinrichtung → Server prüfen → Anmeldemaske | `AppUpdateTests_Connection.StartupFlow_ServerSetup_*` | Abgedeckt |
| Start → offline / Serverauswahl | `AppUpdateTests_Connection.StartupFlow_Offline_*` | Abgedeckt |
| Token abgelaufen → stille Neuanmeldung | `AuthSessionServiceTests.Client_401_SilentRelogin_ResendsWithNewToken` | Abgedeckt |
| Backup-Wiederherstellung | `AppUpdateTests_Connection.RestoreFlow_*` + `TryStoredLoginAsync_NoCredentials_ReturnsMissing` | Abgedeckt |
| Retry-Strategie | `VideoPlayerApiClientTests_RetryPolicy` (Bestand) | Abgedeckt |
| Sitzungsablauf zur Laufzeit → Anmeldemaske | `Update_SessionExpired_WhenConnected_GoesToLogin` | Abgedeckt |

## Fehlende oder unvollständige Planbestandteile

Keine.

## Hinweise

- Die Platzierung von `AuthSessionService` im Projekt `VideoPlayer.Mvu` ist im
  Plan begründet (einzige plattformneutrale Stelle mit Zugriff auf
  `VideoPlayer.Storage` und `VideoPlayer.ApiClient`, testbar über das
  Testprojekt).
- Bestehende betroffene Tests sind vollständig benannt.
