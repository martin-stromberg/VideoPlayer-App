# Übersetzte Anforderung: Verbindungs-Workflow und Anmeldung

## Fachliche Zusammenfassung

Die MVU-App `VideoPlayer.Maui` erhält einen durchgängigen Verbindungs- und
Anmeldeworkflow. Beim App-Start läuft hinter der Ladeanzeige eine Zustandskette:
zuletzt verbundener Server hinterlegt? → Erreichbarkeitsprüfung → automatische
Anmeldung mit den sicher gespeicherten Zugangsdaten. Die vier Zustandsausgänge
`Connected`, `NeedsLogin`, `NeedsServerSetup` und `Offline` sind als
Verbindungszustand im MVU-Model verankert und steuern die Navigation
(Startseite, Anmeldemaske, Servereinrichtung/-auswahl). Der clientseitige
API-Zugriff behandelt fehlerhafte Serverantworten nach der festen
Wiederholungsstrategie (1 s, 2 s, 4 s; keine Wiederholung bei eindeutigen
Clientfehlern außer 401/408/429) und erneuert abgelaufene Token durch stille
Neuanmeldung mit den gespeicherten Zugangsdaten. Da Geheimnisse nicht ins
Cloud-Backup eingehen, fordert der Workflow nach einer Wiederherstellung nur
dann zur erneuten Anmeldung auf, wenn die Zugangsdaten fehlen — die Verbindung
zum zuletzt genutzten Server wird automatisch wiederhergestellt.

## Betroffene Klassen und Komponenten

- **MVU-Kern (`VideoPlayer.Mvu`):**
  - `AppModel` — neuer Verbindungszustand (`ConnectionState`-Enum:
    `Connecting`, `Connected`, `NeedsLogin`, `NeedsServerSetup`, `Offline`),
    Anmelde-UI-Zustand (`IsLoginRunning`, `LoginError`, `LoginUsername`),
    Offline-Datenflag (`HasOfflineData`)
  - `AppPage` — neue Seite `Login` (keine Menü-Route, wie `ServerSetup`)
  - `AppMessage` — neue Messages für Auto-Login-Ergebnis, manuelle Anmeldung,
    Offline-Daten-Prüfung, Sitzungsablauf
  - `AppEffect` — neue Effekte `AttemptAutoLoginEffect`,
    `AttemptLoginEffect`, `CheckOfflineDataEffect`
  - `AppUpdate` — Verzweigung der vier Zustandsausgänge
- **Anmeldeorchestrierung (plattformneutral, testbar):**
  - Neuer Dienst (Arbeitstitel `AuthSessionService`): Anmeldung über
    `VideoPlayerApiClient.LoginAsync`, Persistenz von Zugangsdaten/Token über
    `AppSecretsService`, stille Neuanmeldung als `TokenRefresher` für den
    API-Client
- **Ablage (`VideoPlayer.Storage`):**
  - `AppSecretsService` (vorhanden): `HasCredentialsAsync`, `GetUsername`,
    `GetPasswordAsync`, `SetCredentialsAsync`, `SetAuthTokenAsync`,
    `ClearAuthTokenAsync`
  - `CacheDatabase` — neue Abfrage „Offline-Daten vorhanden" über die
    vorhandenen Tabellen (`cached_lists`, `cached_favorites`,
    `download_manifest`)
- **App-Rand (`VideoPlayer.Maui`):**
  - `Pages/LoginPage.xaml(.cs)` — neue Anmeldemaske (Benutzername, Passwort,
    Fehleranzeige, Vorbefüllung des Benutzernamens)
  - `ShellAppEffectHandler` — Ausführung der neuen Effekte, Sonderfall
    `AppPage.Login` in `NavigateAsync` (Window-Root beim Start, geschobene
    Shell-Route `login` aus der laufenden App)
  - `AppShell` — `Routing.RegisterRoute("login", ...)`
  - `MauiProgram` — Registrierung des Anmeldedienstes und der `LoginPage`
  - `LoadingPage` — Statusanzeige während Erreichbarkeitsprüfung **und**
    automatischer Anmeldung
  - `HomePage` — Offline-Zustandsanzeige (Banner), da der Offline-Zustand zur
    Startseite navigiert
- **API-Client (`VideoPlayer.ApiClient`):**
  - `VideoPlayerApiClient` — Retry-Strategie und 401-Token-Refresh sind aus
    Schritt 1 vorhanden; der `TokenRefresher` muss nun produktiv mit der
    stillen Neuanmeldung verdrahtet werden
- **Tests (`VideoWebPlayer.Maui.Tests`):**
  - MVU-Übergangstests für alle vier Zustandsausgänge und die Anmeldeflüsse
  - Service-Tests des Anmeldedienstes mit `StubHttpMessageHandler` +
    `InMemorySecretStore`/`InMemorySettingsStore`
  - Contract-Tests für `LoginPage`-XAML, Route-Registrierung, DI-Registrierung
  - Anpassung bestehender Tests, deren Erwartungen sich durch den neuen
    Workflow ändern (Health-Check-Abschluss, Serverwechsel-Erfolg)

## Implementierungsansatz

- Der Verbindungszustand wird als Enum im unveränderlichen `AppModel` geführt;
  sämtliche Verzweigungen entstehen ausschließlich in `AppUpdate.Update`
  (rein, testbar). Seiteneffekte (Anmeldung, Offline-Daten-Prüfung) laufen als
  neue `AppEffect`s über den `ShellAppEffectHandler`.
- Der Anmeldedienst kapselt die Client-Erzeugung pro Serveradresse
  (inkl. `TokenRefresher`-Verdrahtung für die stille Neuanmeldung), den
  Login-Aufruf und die Persistenz von Zugangsdaten (`AppSecretsService.
  SetCredentialsAsync`) und Token (`SetAuthTokenAsync`). Er liegt in einem
  vom Testprojekt referenzierbaren, plattformneutralen Projekt.
- „Offline-Daten vorhanden" wird anhand der vorhandenen lokalen Ablagen
  (`CacheDatabase`-Tabellen) bewertet; Downloads kommen erst in Schritt 11,
  die Zustandsunterscheidung ist aber jetzt schon korrekt verankert.
- Die Anmeldemaske folgt dem Muster der `ServerSetupPage` (MVU-Model-getrieben,
  Inline-Fehlertext, AutomationIds); der Benutzername wird aus der nicht
  geheimen Ablage vorbefüllt (überlebt eine Backup-Wiederherstellung).
- Auswahl-/Identifikationsanforderung: Die „Serverauswahl" im Offline-Fall ohne
  Offline-Daten führt auf die bestehende `ServerSetupPage` (Liste bekannter
  Server, Netzwerk-Erkennung, manuelle Eingabe) — keine Eingabe interner
  Kennungen. Die Anmeldung selbst verlangt nur Benutzername + Passwort.

## Konfiguration

Keine neuen Konfigurationseinträge. Bestehende Mechanismen werden genutzt:
`VIDEOPLAYER_SERVER_URL` (Adress-Fallback), `SecureStorage`/`ISecretStore`
(Geheimnisse, nicht im Cloud-Backup), `ISettingsStore` (Benutzername).

## Akzeptanzkriterien (aus der Anforderung)

1. Der Start-Workflow funktioniert in allen vier Zustandsausgängen:
   verbunden, Anmeldung erforderlich, Servereinrichtung erforderlich, offline.
2. Fehlerhafte Serverantworten werden nach der definierten Strategie
   wiederholt (drei Versuche, Backoff 1 s/2 s/4 s; keine Wiederholung bei
   eindeutigen Clientfehlern außer 401, 408, 429) statt sofort zu scheitern.
3. Nach Ablauf des Tokens läuft die App ohne erneute manuelle Anmeldung weiter
   (stille Neuanmeldung mit gespeicherten Zugangsdaten).
4. Nach Wiederherstellung der App-Daten aus einem Backup verbindet sich die
   App automatisch mit dem zuletzt genutzten Server und fordert nur bei
   fehlenden Geheimnissen zur erneuten Anmeldung auf.

## Offene Fragen

Keine — die Anforderung und der Projektkontext (Schritt-4-Infrastruktur,
`AppSecretsService`, API-Client-Retry/Refresh) legen alle Entscheidungen
ausreichend fest. Die Bewertung „Offline-Daten vorhanden" anhand der
vorhandenen Ablagen ist im Auftrag explizit vorgegeben.
