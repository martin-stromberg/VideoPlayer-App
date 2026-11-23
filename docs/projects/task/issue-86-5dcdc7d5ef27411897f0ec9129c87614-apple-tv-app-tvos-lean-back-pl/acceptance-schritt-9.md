# Abnahmeprüfung – Entwicklungsschritt 9

## Ergebnis

**Status:** Anforderung erfüllt – externe Verifikation ausstehend

## Abweichungen

Keine.

## Ausstehende externe Verifikationen

- **Laufzeitverhalten auf tvOS-Simulator/-Gerät:** Tatsächliches
  Einplanen eines Downloads von der Detailseite, die Fortsetzung
  laufender Transfers bei suspendierter App über die
  `NSUrlSession`-Hintergrund-Session, die Zustellung abgeschlossener
  Transfers nach einem System-Neustart der App über den
  Completion-Sink sowie die Bedienung der Download-Liste
  (Aktionsmenü, Fortschrittsanzeige, „Fehlerhafte löschen") sind auf
  Code-Ebene, über plattformneutrale Tests, Contract-Tests und den
  macOS-CI-Kompiliernachweis verifiziert — nicht per Laufzeitnachweis.
  Fällt unter den Register-Eintrag „2–laufend" in
  `pending-verification.md` (App-Start/Runtime-Verhalten auf
  tvOS-Simulator jenseits der Kompilierung).
- Der macOS-CI-Nachweis selbst ist erbracht: Run `42135786801`
  (`build-tvos` 8m58s + `verify` 6m11s, `conclusion: success`) auf
  `headSha` `69355e28…` = letzter Code-Commit. Der danach gemeldete
  triviale Push `020d388` ändert ausschließlich `todo.md`
  (Lifecycle-Artefakt, 2 Zeilen) und benötigt keinen erneuten
  Code-Nachweis. HEAD = `origin/<branch>`
  (`git log origin/<branch>..HEAD` leer), Arbeitsbaum sauber.

## Hinweise

Prüfung anhand des Diffs
`task/issue-86-...-apple-tv-app-tvos-lean-back-pl...HEAD` (26 Dateien,
+1551/−52; neues tvOS-Backend, zwei neue UI-Dateien, Erweiterungen an
`AppUpdate`/`AppEffect`/`AppMessage`, beide Effekt-Handler, Tests,
Doku) und Lesen der Quelldateien samt der plattformneutralen
Gegenseiten (`DownloadService`, `IDownloadBackend`, iOS-Referenz
`IosBackgroundDownloadBackend`).

**(a) `TvosBackgroundDownloadBackend`:**
`VideoPlayer.TvOS/Services/TvosBackgroundDownloadBackend.cs` ist ein
wortgetreuer Port des iOS-Backends: `NSUrlSessionConfiguration.
CreateBackgroundSessionConfiguration("videoplayer.downloads")` +
`SessionSendsLaunchEvents = true` (Z. 57–61) hinter
`IDownloadBackend.TransferAsync` (Z. 100–133; `TaskDescription` =
Medien-Key, `pending` pro `TaskIdentifier`, Abbruch → `Cancel` +
Temp-/Ziel-Bereinigung). Delegate-Methoden via `[Export]`
(`didFinishDownloadingToURL`, `didWriteData`, `didCompleteWithError`,
`URLSessionDidFinishEventsForBackgroundURLSession`). `CompletionSink`
(Z. 51) meldet Abschlüsse ohne aktiven `TransferAsync`-Aufruf — in
`AppComposition.cs` Z. 106–108 an
`Downloads.CompleteExternalTransferAsync(key, error, tempPath,
Settings.Load())` verdrahtet. `handleEventsForBackgroundURLSession`
ist per `[Export]` am `AppDelegate` (Z. 76–85) registriert und reicht
an `TvosBackgroundDownloadBackend.Shared` weiter — die Commit-Folge
zeigt den Binding-Fix `69355e2` (Export statt `override`, CS0115),
der CI-`build-tvos`-Nachweis ist grün. `EnsureSession()` läuft in
`AppDelegate.OnActivated` (Z. 52) — `OnActivated` feuert beim Start
und bei jeder Aktivierung, deckt also beide im Plan genannten Fälle ab.

**(b) `AppActivatedMessage` + `ReconcileDownloadsEffect`:**
Beide plattformneutral — `AppMessage.cs` Z. 226, `AppEffect.cs`
Z. 777, Update in `AppUpdate.cs` Z. 217 → `UpdateAppActivated`
(Z. 4212–4218): `Connected` → `ResumeDownloadsEffect` (im
tvOS-Handler = `ReconcileAsync` + `ResumeInterruptedAsync`, Z.
1311–1330, also Abgleich und Neu-Einplanung verwaister Aufträge im
selben Lauf); nicht verbunden → `ReconcileDownloadsEffect` (nur
lokaler `ReconcileAsync`-Lauf ohne Serverkontakt — kein
`no_connection`-Fehlschlag verfrühter Wiederaufnahmen). Dispatch aus
`AppDelegate.OnActivated` (Z. 62). Der `ReconcileDownloadsEffect`
ist zusätzlich im `ShellAppEffectHandler` (Z. 325–327) der MAUI-App
behandelt — Schadenfreiheit der plattformneutralen Erweiterung.
Übrige Ablauf-Trigger unverändert wirksam: Start (`AppUpdate.cs`
Z. 1063), Online-Rückkehr (Z. 1136), Seitenaufruf
(`LoadDownloadsEffect` → Reconcile vor Listenaufbau).

**(c) Download-Anstoß von der Detailseite — echt:**
`MediaDetailsViewController` dispatcht `DownloadRequestedMessage`
(Kommentar Z. 620–621 aktualisiert); `UpdateDownloadRequested`
erzeugt `EnqueueDownloadEffect` inkl. Serienkontext
(`ShowId`/`ShowTitle`/`SeasonNumber`/`EpisodeNumber`/`PictureId`);
`TvosAppEffectHandler.EnqueueDownloadAsync` (Z. 1205–1235) ruft
`DownloadService.EnqueueAsync` mit `DownloadSeriesContext` und
`Origin: DownloadOrigin.Manual` auf — der vorbereitete
Hinweis-Stub ist entfernt.

**(d) Download-Liste:** `DownloadsPageViewController`
(`AppPage.Downloads` in `MenuTabBarController` Z. 22/101 verdrahtet)
mit `UITableView` + `DownloadTableCell` (Zustand, Medientyp, Größe,
Fortschrittsbalken + Prozent bei `Downloading`, Ablaufdatum bzw.
Fehlertext, Poster-Nachladen). Aktionsmenü je Eintrag: Abbrechen
(`CanCancel`), Erneut versuchen (`CanRetry`), Löschen (`CanDelete`);
Kopfzeile: `QuotaText` + `OrphanedText`, Sammelaktion „Fehlerhafte
löschen" (`DownloadsDeleteFailedRequestedMessage`). Die Seite läuft
in jedem Verbindungszustand (`UpdateDownloadsPageOpened` kennt keine
Verbindungsbedingung).

**(e) Ablaufdatum/Aufbewahrungszeit:** `ExpiresAtUtc = utcNow +
DownloadRetentionDays` beim Einplanen (`DownloadService.cs` Z. 424)
und im Completion-Sink (Z. 795); `RunExpiryPassAsync` entfernt
abgelaufene Einträge im Abgleich; `ExpiresText` erscheint in der
Zelle.

**(f) Quota:** `FailIfQuotaExceededAsync` in `EnqueueAsync`
(Z. 310–318) erkennt die Überschreitung **vor** Einplanung und
liefert die verständliche Meldung `NoticeQuotaExceeded` („Das
Speicherplatzkontingent ist erreicht. Downloads löschen oder das
Kontingent erhöhen.") als `DownloadNotice` auf der Detailseite;
laufende Quota-Prüfung im Transfer via `QuotaScanIntervalChunks`.

**(g) Reconcile:** `ReconcileAsync` (aufgerufen aus allen vier
Triggern) deckt ab: Teildatei-Bereinigung und Restdaten-Erkennung
(`ScanFilesAsync` → `OrphanedBytes`, in der Kopfzeile angezeigt),
Korrektur „fertig" ohne Datei auf `Failed`/`missing_file`
(`CorrectCompletedWithoutFileAsync`), Ablauf-Pass. Zusätzlich ist
`PurgeOrphanedDownloadsEffect` im Handler verdrahtet
(`PurgeOrphansAsync`) — Pflicht ist die Erkennung, die erfüllt ist.

**(h) Ausschlüsse — alle eingehalten:**
- Kein Auto-Download: `RunAutoDownloadsEffect`/`AutoDownloadService`
  im tvOS-Handler nicht verdrahtet (No-Op-Default; Contract-Test
  `TvOsEffectHandler_DoesNotWireExcludedAreas` abgesichert);
  `ReconcileAsync` erhält keine Kandidatenmenge.
- Kein „nach Abspielen löschen": `AutoDeletePlayedDownloads` ist
  per Default `false` und hat keinen tvOS-Schreibpfad (keine
  Referenz in `VideoPlayer.TvOS`); der `ExpireWatchedDownloadEffect`-
  Fall im Handler stammt aus Schritt 7 und ist ohne gesetzte
  Einstellung unerreichbar — die Option wird nicht angeboten
  (Einstellungs-UI in Schritt 10 schließt sie explizit aus).
- Kein Inhalts-Schreibzugriff: keine Inhalts-Verwaltungs-Effekte
  verdrahtet; `ToggleFavoriteEffect` ist die Benutzer-Markierung aus
  Schritt 6, kein Inhaltszugriff.

**(i) Tests:** `dotnet test VideoPlayer.Maui.Tests` lokal ausgeführt:
**1504/1504 bestanden** (0 Fehler, 50 s) — inkl. der vier neuen
`AppActivated`-Tests (Connected→Resume, Offline/Connecting→nur
Reconcile, Settings-Transport) sowie zwei neuer Contract-Tests
(`WiresManualDownloads`, `UsesBackgroundUrlSession`).

**(j) tvOS-Kompilierfallen (Stichprobe):** `[Export]` statt
`override` am Delegate (CS0115-Fix in `69355e2`, begründet im
Kommentar — das tvOS-Binding trägt den Selector nicht als
Basistyp-Member); `progressView.Progress = (float)item.Progress`
(UIProgressView ist `float`); `static readonly nfloat` statt
`const`; `nuint`-Schlüssel für `TaskIdentifier`; keine
`SeparatorStyle`/iOS-only-APIs. Kompiliernachweis via CI
`build-tvos` grün.

**(k) CI:** Run `42135786801` → `conclusion: success`
(`build-tvos` 8m58s, `verify` 6m11s) auf `headSha` `69355e2`.
HEAD = `020d388` enthält gegenüber `69355e2` ausschließlich die
`todo.md`-Aktualisierung (`git show 020d388 --stat`: 1 Datei,
2 Zeilen) — Code-Stand des Runs = Code-Stand von HEAD. HEAD ist
gepusht (`git log origin/<branch>..HEAD` leer).

**Kleinigkeiten (keine Abweichungen):** `review-code.md` im
Feature-Verzeichnis beschreibt noch den `override`-Ansatz — der
nachträgliche Fix-Commit `69355e2` stellt auf `[Export]` um
(Dokumentationsstand hinter der Code-Korrektur, fachlich korrekt
aufgelöst). Einmaliger Testhost-Absturz (-1073741819) während der
Entwicklung laut `test-results.md`; der hier wiederholte Lauf war
vollständig grün — kein reproduzierbarer Defekt.
