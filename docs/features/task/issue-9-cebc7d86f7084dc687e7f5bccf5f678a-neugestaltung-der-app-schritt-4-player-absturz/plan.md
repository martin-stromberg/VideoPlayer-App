# Umsetzungsplan: Videoplayer — wiederholte Wiedergabe ohne Absturz

## Übersicht

Die `PlayerPage` wird absturzfest für wiederholte Wiedergaben gemacht:
(1) Das `ModelChanged`-Abonnement wird beim Verlassen der Seite
abgemeldet, damit verwaiste Seiten nicht mehr auf ihr losgelöstes
`MediaElement` zugreifen. (2) Alle Zugriffe auf `VideoElement` aus
Toolkit-Event-Handlern (`MediaOpened`, `StateChanged` u. a.) werden auf
den UI-Thread gemarshalt, da diese Events unter Windows auf
Threadpool-Threads ausgelöst werden können und `MediaManager.PlatformSeek`
sonst `MediaPlayerElement.get_MediaPlayer` thread-fremd aufruft
(`COMException 0x8001010E`). Der MVU-Kern bleibt unverändert.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Abmeldung von `ModelChanged` | Abo in `OnAppearing` (`-=` vor `+=`), Abmeldung in `OnDisappearing` — Muster von `SettingsPage` (SettingsPage.xaml.cs:43-56) | Etablierte Konvention im Repo; deckt „verlassen, aber noch auf dem Navigationsstack" und „gepoppt" gleichermaßen ab |
| Reaktions-Sperre verwaister Seiten | Bool-Feld `subscribed`: `true` in `OnAppearing`, `false` in `OnDisappearing`; alle Reaktionspfade (`UpdateUi`-Marshal, Media-Event-Handler) prüfen es | `BeginInvokeOnMainThread`-Callbacks, die vor dem Verlassen eingereiht wurden, laufen sonst noch auf dem toten Element; Media-Events der alten Seite dürfen auch keine Messages mehr dispatchen (ein `PlaybackEndedMessage` der alten Seite würde den neuen `PlayerState` fälschlich als gesehen markieren) |
| UI-Thread-Marshal | `MainThread.BeginInvokeOnMainThread` an den Event-Grenzen (`OnMediaOpened`, `OnMediaStateChanged`, `OnModelChanged` bereits vorhanden) | Deckt die beobachtete Crash-Quelle (`SeekTo` aus `MediaOpened`) und alle anderen nativen Zugriffe ab; kein Umbau des Toolkits nötig (app-seitiger Workaround, von der Anforderung ausdrücklich erlaubt) |
| Tests | Quelltext-Contract-Tests in `PlayerContractTests` | MAUI-Schicht ist im Testprojekt nicht instanziierbar (dokumentierte Konvention); der native COM-Fehler ist auf Testhost nicht reproduzierbar |

## Programmabläufe

### Wiederholte Wiedergabe ohne Absturz

1. `PlayRequestedMessage` → `UpdatePlayRequested` → neuer `PlayerState` +
   `NavigateToPlayerEffect` + `ResolvePlaybackSourceEffect` (unverändert).
2. Neue `PlayerPage`-Instanz: `OnAppearing` setzt `subscribed = true`,
   abonniert `ModelChanged`, dispatcht `PlayerPageOpenedMessage`.
3. `PlaybackSourceResolvedMessage` → `OnModelChanged` →
   `MainThread.BeginInvokeOnMainThread` → `UpdateUi` setzt die Quelle
   (UI-Thread).
4. `MediaOpened` (ggf. Threadpool-Thread) → `OnMediaOpened` marshalt
   `VideoElement.SeekTo` auf den UI-Thread → `PlatformSeek` läuft auf dem
   Thread mit COM-Affinität → keine `RPC_E_WRONG_THREAD`.
5. Verlassen: `OnDisappearing` setzt `subscribed = false`, meldet
   `ModelChanged` ab, stoppt den Timer, dispatcht `PlayerClosedMessage`
   (unverändert). Die verwaiste Seite reagiert danach nicht mehr.

Beteiligte Klassen: `PlayerPage`, `MvuHost`, `AppUpdate`,
`ShellAppEffectHandler`, `MediaElement` (Toolkit).

### Fortschrittsmeldung und Gesehen-Markierung (unverändert)

`ReportPosition` (jetzt stets UI-Thread) → `PlaybackProgressMessage` →
`UpdatePlaybackProgress` → `ReportPlaybackProgressEffect` →
`ProgressReportService`; `IsWatched` über `WatchedEndWindowSeconds`;
Abschluss über `PlayerClosedMessage`/`PlaybackEndedMessage`. Keine
Änderung an diesen Pfaden.

## Neue Klassen

Keine.

## Änderungen an bestehenden Klassen

### `PlayerPage` (ContentPage, `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`)

- **Neue Felder:** `subscribed` (`bool`) — `true` solange die Seite aktiv
  auf das Modell reagiert; gesetzt in `OnAppearing`, zurückgesetzt in
  `OnDisappearing`.
- **Geänderte Methoden:**
  - Konstruktor: `mvuHost.ModelChanged += OnModelChanged` entfällt
    (verschiebt sich nach `OnAppearing`); `UpdateUi(mvuHost.Model)` für
    die Anfangsdarstellung bleibt.
  - `OnAppearing`: `subscribed = true`; `ModelChanged -= OnModelChanged`
    gefolgt von `+=` (Doppelaboschutz, Muster `SettingsPage`).
  - `OnDisappearing`: `subscribed = false` und
    `ModelChanged -= OnModelChanged` **vor** dem Dispatch von
    `PlayerClosedMessage`, damit die eigene Seite den Reset nicht mehr
    auf ihrem Element rendert; `timerRunning = false` und
    `pictureInPicture.RestoreAsync()` bleiben.
  - `OnModelChanged`: Callback-Inhalt um `if (!subscribed) return;`
    bzw. Guard um `UpdateUi` ergänzt.
  - `OnMediaOpened`: `SeekTo` wird über `MainThread.BeginInvokeOnMainThread`
    ausgeführt und im Callback an `subscribed` gebunden.
  - `OnMediaStateChanged`: `ReportPosition` wird über
    `MainThread.BeginInvokeOnMainThread` ausgeführt und an `subscribed`
    gebunden.
  - `OnMediaEnded` / `OnMediaFailed`: Frühreturn bei `!subscribed`
    (verwaiste Seiten dispatchen keine Wiedergabe-Messages mehr).

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine.

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Fortschrittstakt:** `ReportPosition` läuft über
  `OnMediaStateChanged` jetzt asynchron gemarshalt — die Reihenfolge der
  Meldungen bleibt erhalten (MainThread-Queue ist FIFO); kein
  Verhaltensunterschied.
- **PlayerClosed-Meldung:** Das Unsubscribe vor dem Dispatch verhindert,
  dass die verlassene Seite den eigenen Reset (`Player = Empty`)
  rendert — gewollt; Abschluss-Fortschritt (`ReportPlaybackProgressEffect`
  in `UpdatePlayerClosed`) ist davon nicht betroffen.
- **Andere Seiten mit demselben Leck** (`HomePage`, `MediaDetailsPage`,
  `DownloadsPage`, `FavoritesPage`, `SourceOverviewPage`, `ProfilePage`):
  bewusst nicht Teil dieses Schritts — sie greifen nicht auf das native
  Player-Element zu und verursachen keinen Absturz (Event-Leak-Muster
  ist in der Projektinventur §6 als Querschnittsbefund dokumentiert).
- **PiP/Mini-Player:** `OnDisappearing` bleibt der einzige Ort, der den
  Zustand schließt — unverändert.

## Umsetzungsreihenfolge

1. **`PlayerPage.xaml.cs` anpassen**
   - Voraussetzungen: Keine (nur vorhandene APIs `MainThread`, `Dispatcher`).
   - Beschreibung: `subscribed`-Feld, Abo-Umzug Konstruktor →
     `OnAppearing`/`OnDisappearing`, Guards und Marshal in den
     Media-Event-Handlern und `OnModelChanged`.

2. **Contract-Tests in `PlayerContractTests` ergänzen**
   - Voraussetzungen: Schritt 1 (Tests pinnen die neue Struktur).
   - Beschreibung: Tests für (a) Abmeldung von `ModelChanged` beim
     Verlassen, (b) UI-Thread-Marshal aller `VideoElement`-Zugriffe aus
     Event-Handlern, (c) Reaktions-Sperre verwaister Seiten.

3. **Verifikation**
   - Voraussetzungen: Schritte 1–2.
   - Beschreibung: `dotnet build VideoPlayer.App.sln` Debug + Release,
     `dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj`.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `PlayerPage_UnsubscribesModelChangedOnDisappearing` | `PlayerContractTests` | `ModelChanged -= OnModelChanged` im Rumpf von `OnDisappearing`; Abo in `OnAppearing` statt im Konstruktor |
| `PlayerPage_MarshalsNativeElementAccessOnMainThread` | `PlayerContractTests` | Jeder `VideoElement`-Aufruf (`SeekTo`, `Stop`, `Source`, `CurrentState`, `Position`, `Duration`) steht in einem UI-Thread-Kontext (Marshal oder UI-Thread-Methode); insbesondere `SeekTo` nie direkt im `MediaOpened`-Handler |
| `PlayerPage_IgnoresModelChangesAfterDisappearing` | `PlayerContractTests` | `subscribed`-Guard in `OnModelChanged` und den Media-Event-Handlern |

### Betroffene bestehende Tests

Keine — die Änderung verändert keine Signaturen und kein MVU-Verhalten;
`AppUpdateTests_Player` bleibt unverändert grün und belegt, dass
Fortschrittsmeldung und Gesehen-Markierung unverändert funktionieren.

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|---------------------|
| — | Keine automatisierten E2E-Szenarien geplant | — | — | Das Repository besitzt keine UI-/Geräte-Automatisierung (dokumentierte Lücke, `PlayerContractTests.cs:7-13`); der Fehler ist ein natives COM-Threading-Problem auf Windows-Geräten und auf dem Testhost nicht reproduzierbar. Nachweis per Contract-Tests der Korrekturstruktur + dokumentiertem Geräte-Workflow (in `test-results.md`/`docs/help/`) — von der Akzeptanz ausdrücklich erlaubt („Nachweis per Test oder dokumentiertem Workflow"). |

Bestehende E2E-Tests: Keine im Repository vorhanden — keine Anpassung nötig.

## Offene Punkte

Keine.
