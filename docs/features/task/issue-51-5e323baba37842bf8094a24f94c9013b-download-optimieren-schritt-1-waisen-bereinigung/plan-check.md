# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| (1) Datei ohne Manifest-Eintrag wird als Restdaten erkannt (kein stilles Löschen, kein unsichtbarer Verbrauch) | `ReconcileAsync` kategorisiert „kein Manifest-Bezug" als Restdaten (summiert, behalten); `OrphanedBytes` wird bis in `DownloadsPageState`/`OrphanedLabel` durchgereicht | `Reconcile_OrphanedFile_IsCountedAndKept`, `UpdateDownloadsLoaded_OrphanedBytes_InState`, `DownloadsPageState_OrphanedText_WhenZero_Null` | Abgedeckt |
| (2) „Fertig"-Eintrag ohne Datei wird als nicht vorhanden behandelt und ist erneut einplanbar | Zustandskorrektur auf `failed` + `missing_file` in `ReconcileAsync` | `Reconcile_CompletedWithoutFile_MarkedMissing`, `DownloadListItem_MissingFile_ErrorText` | Abgedeckt |
| (3) Angezeigte Belegung entspricht tatsächlichem Dateibestand | `GetUsedBytes` (Dateisystem) bleibt Quelle; Restdaten werden als Teilmenge ausgewiesen | `Reconcile_OrphanedFile_IsCountedAndKept`, `UpdateDownloadsLoaded_OrphanedBytes_InState` | Abgedeckt |
| (4) Laufende/wartende Downloads werden nicht beeinträchtigt | Dateien von `completed` und von `downloading` mit laufendem Transfer werden nie angefasst; Race-Betrachtung in „Seiteneffekte" | `Reconcile_CompletedFile_IsKept`, `Reconcile_RunningTransferFile_IsKept`, `Reconcile_QueuedEntryFile_IsRemoved` (nur verwaiste Teildatei) | Abgedeckt |
| Automatisches Entfernen überholter Teildateien (Stakeholder-Entscheidung) | Regel: Eintrag `queued`/`failed`/`cancelled`/`downloading`-ohne-Runner mit Datei → `TryDeleteFile` | `Reconcile_StalePartialOfFailedEntry_IsRemoved`, `…CancelledEntry…`, `…OrphanedDownloadingEntry…` | Abgedeckt |
| Abgleich bei App-Start und Seitenaufruf | Aufrufe in `ResumeDownloadsAsync` und `LoadDownloadsAsync` | indirekt über MVU-Tests + Service-Tests | Abgedeckt |
| Protokollierung des Abgleichs | Zusammenfassung via `LogInformation`; Fehler geloggt, nicht fatal | implizit über Reconcile-Tests (Ergebnis-Record) | Abgedeckt |
| Nicht-Ziele (kein Altbestand, keine Server-Änderung, keine Lösch-UI) | Plan enthält keine solchen Schritte; UI-Änderung ist rein lesend | — | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

—

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Öffnen der Downloads-Seite löst Abgleich aus; Restdaten-Anzeige erscheint | keiner | Nicht erforderlich mit Begründung: keine neue oder geänderte Benutzerinteraktion — der Abgleich läuft automatisch, die einzige sichtbare Änderung ist eine passive Textzeile. Projektkonvention: MVU-Übergangstests + Contract-Tests. Kein Downloads-E2E-Szenario in `VideoPlayer.E2E.Tests` vorhanden. |

## Fehlende oder unvollständige Planbestandteile

—

## Hinweise

- Die Restdaten-Anzeige (`OrphanedLabel`) ist eine UI-Änderung — das
  Usability-Review (Schritt 8 des Lifecycle) bleibt daher anwendbar.
- Bei einer späteren Projektfortsetzung (Schritt 2) wird die
  Bereinigungs-Aktion ergänzt; der jetzige Plan legt mit dem
  Ergebnis-Record die Grundlage dafür.
