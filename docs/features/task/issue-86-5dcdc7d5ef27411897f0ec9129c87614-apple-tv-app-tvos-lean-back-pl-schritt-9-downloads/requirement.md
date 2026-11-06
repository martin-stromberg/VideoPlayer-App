# Anforderung (übersetzt) — Schritt 9: Manuelle Downloads über suspensionstaugliche Hintergrund-URL-Session mit Ablaufdatum, Reconcile und Quota

Quelle: `docs/projects/task/issue-86-.../project-plan.md`, Schritt 9 (wörtlich übernommen, verdichtet strukturiert).

## Ziel

Die tvOS-App erhält den manuellen Download-Funktionsumfang über den bestehenden plattformneutralen `DownloadService` mit Manifest — **ohne Auto-Download** (kein Vorlauf, keine Kandidaten-Logik, keine „Cache"-Herkunft; `AutoDownloadService` wird nicht verdrahtet). Der Transfer läuft suspensionstauglich über eine `NSUrlSession`-Hintergrundkonfiguration hinter `IDownloadBackend` (tvOS-Apps bleiben wochenlang suspendiert; Vordergrund-Transfers würden abbrechen).

## Umfang

1. **Transfer-Backend:** tvOS-Pendant `TvosBackgroundDownloadBackend` zum iOS-`IosBackgroundDownloadBackend` (`VideoPlayer.Maui/Platforms/iOS/`, `#if IOS`) — identische `NSUrlSession`-Hintergrund-API auf tvOS:
   - Hintergrund-Session mit fester Kennung (analog `videoplayer.downloads`)
   - `TaskDescription` als Medien-Key
   - `CompletionSink` → `DownloadService.CompleteExternalTransferAsync` (Transfers ohne aktiven `TransferAsync`-Aufruf nach System-Neustart)
   - App-Delegate `application:handleEventsForBackgroundURLSession:completionHandler:`
   - `HttpDownloadBackend` bleibt Fallback/Test-Referenz
2. **Download-Anstoß:** je Eintrag über die Detailseite (`EnqueueDownloadEffect` real verdrahten statt vorbereitetem Hinweis, inkl. Serienkontext)
3. **Download-Liste (schlank):** Zustandsanzeige, Fortschritt, Abbrechen, Erneut versuchen, Löschen (einzeln + fehlgeschlagene gesammelt)
4. **Ablaufdatum:** je Download gemäß einstellbarer Aufbewahrungszeit
5. **Quota:** Obergrenze für den geteilten tvOS-Speicher mit verständlicher Meldung vor Einplanung
6. **Reconcile (Pflicht):** überholte Teildateien entfernen; „fertig" ohne Datei → Fehlerzustand `missing_file` (tvOS purgt App-Daten unter Speicherdruck); verwaiste Restdaten erkennen
7. **Aktivierungs-Message (neu, plattformneutral):** bei Eintritt in den Vordergrund Ablauf-/Abgleichslauf auslösen — neben den Triggern App-Start, Online-Rückkehr, Seitenaufruf — plus Neu-Einplanung verwaister Aufträge über `ResumeInterruptedAsync`; tvOS-Lebenszyklus (`OnActivated`) dispatcht sie; Session-Instanziierung bei Start/Aktivierung, damit ausstehende Hintergrund-Transfers Delegate-Rückrufe erhalten

## Ausschlüsse

- Kein Auto-Download (AutoDownloadService nicht verdrahten)
- Kein „nach dem Abspielen automatisch löschen" (geklärt: entfällt)
- Kein Schreibzugriff auf Server-Inhalte

## Akzeptanzkriterien

- Downloads von der Detailseite einplanbar, sichtbarer Fortschritt, abbrechbar/wiederholbar/löschbar
- Laufende Transfers setzen sich bei suspendierter App über Hintergrund-Session fort; nach Wiederkehr/System-Neustart werden Abschlüsse über den Completion-Sink dem Manifest gemeldet bzw. offene Aufträge über den Unterbrechungspfad neu eingeplant
- Quota-Überschreitung vor Einplanung erkannt und verständlich gemeldet
- Abgelaufene Downloads werden bei Start, Online-Rückkehr, Seitenaufruf und App-Aktivierung entfernt
- Vom System gelöschte Dateien erscheinen nach Abgleich als fehlgeschlagene Einträge (`missing_file`)
- Neue Aktivierungs-Message durch plattformneutrale Tests abgedeckt
- Lokale Downloads stehen der Wiedergabe als bevorzugte Quelle zur Verfügung (Zusammenspiel mit Schritt 7 — bereits über `PlaybackSourceService` gegeben)

## Stakeholder-Entscheidungen (verbindlich)

- Hintergrund-Downloads sind MVP: `TvosBackgroundDownloadBackend` nach iOS-Vorlage, geteilter `IOS || TVOS`-Teil wo sinnvoll
- „Nach dem Abspielen löschen" wird NICHT angeboten
- KEIN Auto-Download
