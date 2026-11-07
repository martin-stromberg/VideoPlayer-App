# Code-Review

Geprüft werden alle neuen/geänderten Quelldateien gegenüber dem Basisbranch
`task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app`
(Selbstprüfung ohne Unteragenten — dokumentierte Abweichung).

## Geänderte/neue Quelldateien

### `VideoPlayer.Mvu`

- `ConnectionState.cs` — klar benannter Enum mit XML-Dokumentation je
  Wert; `Unknown` bleibt defensiver Restzustand.
- `AuthAttempt.cs` — `AuthAttemptStatus` + `AuthAttempt`-record; ein
  Ergebnistyp für Auto- und manuellen Login; `Username` dokumentiert als
  nicht-geheime Vorbefüllung.
- `AuthSessionService.cs` — Service Layer für den Anmeldeablauf:
  `TryStoredLoginAsync`/`LoginAsync` mit gemeinsamem `LoginCoreAsync`;
  Statusklassifikation zentral (401/403 → `InvalidCredentials`,
  Statuscode-freie `ApiException`/Transport/Timeout → `ServerUnreachable`,
  sonstige `ApiException` → `Failed`); gehaltener `Client` mit
  `TokenRefresher`-Verdrahtung; `SilentReloginAsync` über eigenen
  kurzlebigen Client (keine Rekursion); `SessionExpired` nur bei
  fehlenden/endgültig abgelehnten Zugangsdaten — transienten Fehler lösen
  bewusst kein `SessionExpired` aus (kein falscher Anmeldezwang bei
  Netzproblemen); Ereignis-Invoke ist gegen Handler-Fehler abgesichert.
- `AppPage.cs` — `Login` analog `ServerSetup` dokumentiert (kein
  Menüeintrag).
- `AppMessage.cs`/`AppEffect.cs` — fünf Messages / drei Effekte als
  `sealed record`, konsistent zum Bestand.
- `AppModel.cs` — neue Eigenschaften mit Defaults; `IsOffline` und
  `StartupStatusText` als berechnete Eigenschaften (keine duplizierte
  Logik in den Pages).
- `AppUpdate.cs` — erschöpfende `switch`-Abdeckung aller neuen Messages;
  Fehlertexte als Konstanten; `LoginErrorFor` kapselt die
  Status→Text-Abbildung; `GoToServerSetup`-Hilfsmethode bündelt den
  wiederholten Einrichtungs-Übergang; Navigations-Guards in
  `UpdateNavigateTo`/`UpdateNavigated` halten `NeedsLogin`/
  `NeedsServerSetup` stabil (Menüseiten unerreichbar, Workflow-Seiten
  untereinander erlaubt). Unveränderte Semantik: kein Effekt bei
  Navigation auf die bereits aktive Seite.

### `VideoPlayer.Storage`

- `CacheDatabase.cs` — `HasOfflineDataAsync` liest nur (keine Migration);
  frühes `return true` je Tabelle; Tabellen sind die im Plan benannten.

### `VideoPlayer.Maui`

- `Pages/LoginPage.xaml(.cs)` — Muster der `ServerSetupPage`: Zustand
  vollständig aus dem MVU-Model (`UpdateUi`), `ModelChanged`-
  Abonnement mit Deregistrierung in `OnDisappearing`; Benutzername nur
  einmalig vorbefüllt (`usernamePrefilled` — überschreibt keine
  Eingaben); während `IsLoginRunning` sind sämtliche Eingaben gesperrt;
  beide Aktionen dispatcht Messages (`LoginSubmittedMessage`,
  `NavigateToMessage`) — keine UI-Logik außerhalb des MVU-Zyklus.
- `Services/ShellAppEffectHandler.cs` — drei neue Effekt-Zweige mit dem
  etablierten Muster (Fehler → defensives Ergebnis + Debug-Ausgabe,
  Rückdispatch via `MainThread.InvokeOnMainThreadAsync`); `NavigateAsync`
  erhält den `AppPage.Login`-Sonderfall exakt parallel zum
  `ServerSetup`-Muster; `SessionExpired`-Abonnement im Konstruktor —
  `MvuHost` wird erst im Ereignis aufgelöst (keine DI-Zirkularität
  `MvuHost → IAppEffectHandler → MvuHost`).
- `AppShell.xaml.cs` — Route `login` registriert (kein FlyoutItem —
  Contract-Test abgesichert).
- `Pages/LoadingPage.xaml` — Bindung auf `StartupStatusText`.
- `Pages/HomePage.xaml(.cs)` — `OfflineBanner` mit `AutomationId`,
  Sichtbarkeit aus `Model.IsOffline`; `ModelChanged`-Abonnement ergänzt.
- `MauiProgram.cs` — `AuthSessionService` (Singleton — hält den
  angemeldeten Client), `LoginPage` (Transient).

### `VideoWebPlayer.Maui.Tests`

- `AppUpdateTests_Connection.cs` — alle neuen Übergänge inkl.
  Offline-Verzweigung, Fehlertext-Mapping je `AuthAttemptStatus`,
  SessionExpired-Pfade und Navigations-Guards.
- `AppUpdateTests_Startup.cs` — Health-Check-Tests an die neue
  Verzweigung angepasst (Auto-Login-/Offline-Daten-Effekt statt
  Home-Navigation).
- `AppUpdateTests_ServerManagement.cs`, `MvuHostTests.cs` — an die
  Anmeldung nach Serverwechsel bzw. die verlängerte Effektkette angepasst.
- `AuthSessionServiceTests.cs` — echte HTTP-Stub-Tests inkl.
  `TokenRefresher`-Kette (401 → `api/auth/login` → Neuversand mit neuem
  Token), `SessionExpired` bei fehlenden/abgelehnten Zugangsdaten, kein
  `SessionExpired` bei transienten Fehlern; In-Memory-Stores für die
  Ablage-Assertions.
- `ConnectionWorkflowContractTests.cs` — Quelltext-Contract-Tests der
  UI-/DI-Verdrahtung (Muster der bestehenden Contract-Tests).
- `LocalDatabaseTests.cs` — `HasOfflineDataAsync` (leer/Liste/Manifest)
  gegen reale SQLite-Ablage.

## Geprüfte Stil- und Qualitätspunkte

- Alle neuen Typen/Member tragen XML-Dokumentation; Kommentare sind
  deutsch und fachlich (Projektkonvention).
- Keine Geheimnisse im MVU-Model: `LoginSubmittedMessage`/`AttemptLoginEffect`
  tragen das Passwort nur flüchtig bis zum Service; persistiert wird
  ausschließlich über `AppSecretsService` (sichere Ablage).
- Keine UI-Aktion ohne Handler: alle `Clicked`-/`SelectionChanged`-
  Verdrahtungen der geänderten XAML-Dateien haben Code-Behind-Handler;
  es gibt keine `RaiseUiActionRequested`-Aktionen in dieser
  Implementierung (das Muster existiert in der MAUI-App nicht).
- `IDisposable`-/Lebensdauer: Sitzungs-Client bleibt gehalten
  (`Client`-Eigenschaft), Refresh-Client wird pro Aufruf verworfen;
  Test-Fabrik nutzt `disposeHandler: false`, damit der geteilte
  Stub-Handler den Dispose überlebt.
- `dotnet build` (alle Projekte, Windows-TFM inkl. XAML-SourceGen):
  0 Fehler, keine neuen Warnungen.

## Feststellungen

Keine offenen Befunde. Hinweis ohne Korrekturbedarf: Die Ansammlung von
`GoToAsync("login")`-Aufrufen bei wiederholtem Sitzungsablauf kann die
Route erneut auf den Stack schieben — fachlich korrekt (Anmeldung ist
zwingend), optisch unauffällig (gleiche Maske).
