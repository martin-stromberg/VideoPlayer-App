# Code-Review — FlaUI-E2E-Tests (Issue 17, Schritt 3)

Geprüfte Änderungen (2026-09-24):

- Produktiv: `VideoPlayer.ApiClient/ClientGate.cs`,
  `VideoPlayer.Maui/DevApi/DevApiService.cs`,
  `VideoPlayer.Maui/DevApi/DevApiVisualTree.cs`
- Neu: `VideoPlayer.E2E.Tests/` (Fixture, Support, 4 Testklassen,
  10 Tests)

## Produktivcode

### `ClientGate` — profil-isolierte Token-Datei

`TokenFileDirectory` hängt jetzt bei gesetztem
`VIDEOPLAYER_PROFILE` das Profil-Unterverzeichnis an
(`%LocalAppData%/VideoPlayer/<profil>`), mit derselben
Normalisierung wie `AppProfile.Create` (trimmen, kleinschreiben,
`default` = Wurzel). Begründung: die Token-Datei war die letzte
profil-übergreifende Geheimnisquelle — ein isoliertes Profil hätte
den Gate-Schlüssel des Standardprofils geerbt und den Pairing-Pfad
nie betreten (im ersten E2E-Lauf beobachtet: `isPairingRequired`
blieb `false`, Login-Fehler „gespeicherte Anmeldung nicht mehr
verfügbar"). Für das Standardprofil bleibt der Pfad bytegleich;
die Konstante `VIDEOPLAYER_PROFILE` wird bewusst gespiegelt statt
eine ApiClient→Storage-Abhängigkeit einzuführen (Storage referenziert
selbst nichts, ApiClient wäre die falsche Richtung). Bestandstests
926/926 grün — die internen `Resolve`-Überladungen sind unverändert.

### `DevApiService`/`DevApiVisualTree` — `/invoke` mit `itemIndex`

`InvokeBody.ItemIndex` (optional), `InvokeOnPage` reicht an
`TrySelectItem` weiter: prüft `CollectionView`, iteriert
`ItemsSource` (Indexgrenzen sauber validiert, verständliche
Fehlertexte), setzt `SelectedItem` — das echte
`SelectionChanged`-Event dispatcht die App-Nachricht. Kein
Koordinatenklick, keine Umgehung des MVU-Flusses. Ohne `itemIndex`
unverändert (Command/Clicked-Pfad).

## Testprojekt

- **`AppFixture`**: einmaliger Debug-Build als `Lazy<Task>`; Exe-Pfad
  `bin/Debug/net10.0-windows10.0.19041.0/win-x64/VideoPlayer.Maui.exe`
  verifiziert (existiert nach dem Build).
- **`AppInstance`**: PID-geprüftes `devapi.json`-Warten, UIA-Attach
  mit Fenster-Timeout, `Dispose` räumt garantiert auf
  (CloseMainWindow → Kill-Baum, Metadaten + Profildirs unter beiden
  AppData-Wurzeln). Entfernt `VIDEOWEBPLAYER_MAUI_API_TOKEN` aus der
  Umgebung, damit kein statischer Schlüssel das Pairing unterläuft.
- **`DevApiClient`**: ein `HttpClient` je Instanz (der API-Server ist
  sequentiell), `WaitForModelAsync` meldet Timeout mit dem letzten
  JSON-Snapshot — sehr gute Diagnosequalität (hat alle Fehlerbilder
  der Iterationen tracbar gemacht).
- **`MockVideoServer`**: `TcpListener` auf Loopback mit OS-Port;
  deckt `api/health` (ungated), `api/pairing/exchange` (ECDH P-256 +
  AES-256-GCM, deckungsgleich mit `PairingCrypto`/`PairingService`),
  `api/auth/login` und die für Home/Token-Validierung nötigen
  Listen-Endpunkte ab. Gate-/Credential-Ablehnung wird wie beim
  echten Server am Content-Type unterschieden (`problem+json` vs.
  `text/plain`) — das ist das Erkennungsmerkmal von
  `DeviceTokenService.IsGateRejectedAsync`; JSON-Binding
  case-insensitiv (Client sendet PascalCase).
- **`UiDriver`**: AutomationId-Suche mit Retry, Value-Pattern-
  Texteingabe, Invoke-Pattern für Buttons, `SelectionItem` für
  NavigationView-Einträge (kein Koordinatenklick), `DumpTree` als
  Diagnoseausgabe.
- **Tests**: sequentielle `E2E`-Collection (Fensterfokus +
  `devapi.json` sind exklusiv), ein Prozess + ein Profil je Test,
  Model-Assertions über die API, UI-Aktionen über FlaUI —
  Trennlinie entspricht der Vorgabe.

## Befunde und Restrisiken

1. **Shell-Tab-Stack-Rest** (App-Verhalten, nicht Testfehler): nach
   dem Auto-Login bleibt die geschobene Route `serversetup` im
   Einstellungen-Tab liegen; „Einstellungen" öffnen kann danach
   direkt die Serververwaltung zeigen (Zurück nötig). Standard-
   Shell-Verhalten, vom #15-Fix bewusst nicht angefasst — der Test
   akzeptiert beide Landezustände und deckt die eigentliche
   #15-Regression (Bekannter-Server-Auswahl → Startseite) ab.
   Empfehlung: separat bewerten, ob die Route nach erfolgreichem
   Wechsel gepoppt werden soll.
2. **Mock statt echtem Server**: hermetisch und deterministisch,
   prüft aber nur den vertraglich implementierten Ausschnitt; das
   optionale Real-Server-Szenario bleibt als manueller Nachweis
   dokumentiert (`inventory/backend-optionen.md`).
3. **Laufzeit** (~17 min für 10 Tests): geprägt von App-Starts und
   den echten Warte-/Retry-Fenstern (u. a. nicht erreichbarer Server
   mit Client-Retry). Für lokale Ausführung akzeptabel; CI nur
   optional vorgesehen (kein interaktiver Desktop auf gehosteten
   Runnern).
4. **`devapi.json` ist pro Exe-Verzeichnis global** — die Collection
   serialisiert die Tests; ein parallel gestarteter zweiter Lauf
   auf derselben Exe würde die Metadatei überbieten (als bekannte
   Einschränkung dokumentiert, sequentielle Ausführung ist ohnehin
   durch den Fensterfokus nötig).

## Fazit

Produktivänderungen minimal und abwärtskompatibel; Testinfrastruktur
führt echte UI-Ketten aus, ohne Produktivpfade zu umgehen. Kein
Blocker; zwei dokumentierte Befunde (Shell-Stack-Rest, Laufzeit).
