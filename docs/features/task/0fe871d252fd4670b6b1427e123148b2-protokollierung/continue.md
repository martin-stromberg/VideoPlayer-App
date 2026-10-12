# Offene Aufgaben

Erstellt am: 2026-10-12
Abbruchgrund: Maximale Iterationsanzahl erreicht

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

Keine — `review.md` trägt den Status `Vollständig umgesetzt`.

## Code-Review-Befunde

- [ ] `DownloadService.ResumeInterruptedAsync` (VideoPlayer.Mvu/DownloadService.cs, Z. 565): `{RequeuedCount}`-Information läuft unbedingt, auch bei 0 — `ResumeDownloadsEffect` feuert bei jeder Online-Meldung im `Connected`-Zustand (WLAN↔Mobilfunk-Wechsel) → „0 unterbrochene Downloads wieder eingeplant." pro Netzwerkflanke. Nur bei `toRequeue.Count > 0` loggen (analog zu `sent > 0` in `ServerSyncService.FlushPendingProgressAsync`).
- [ ] `DownloadService.TransferAsync` (VideoPlayer.Mvu/DownloadService.cs, Z. 765–784): doppelte Warnung pro Transfer-Fehlschlag — `catch (IOException)`/`catch (Exception)` schreiben `LogWarning` und `SetStateIfPresentAsync(Failed, …)` schreibt via `SetStateAsync` (Z. 834–843) eine zweite. Auf einen Eintrag je Fehlschlag reduzieren (Ausnahme an `SetState*` durchreichen oder catch-Log belassen und `SetStateIfPresentAsync` ohne eigenen Log aufrufen).
- [ ] `ShellAppEffectHandler.ReportPlaybackProgressAsync` (VideoPlayer.Maui/Services/ShellAppEffectHandler.cs, Z. 488–492): Catch-all bleibt `LogError` pro ~5-s-Takt für Ausnahmen außerhalb des Dienst-Filters (`ApiException`/`HttpRequestException`/`TaskCanceledException`), inkl. rethrown `OperationCanceledException` bei Token-Abbruch (routinemäßiger Abbruch als ERROR; Kommentar „nicht fatal" widerspricht Level). `catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)` durchreichen, Rest auf `LogWarning` senken.

## Usability-Befunde

Keine.

## Fehlgeschlagene Tests

Keine — `test-results.md` trägt den Status `Keine Fehler` (Unit: 1280/1280, E2E: 56/56).
