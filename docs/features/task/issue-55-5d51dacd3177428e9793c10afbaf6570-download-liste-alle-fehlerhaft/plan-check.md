# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Aktion „Alle Fehler löschen" (o. ä.) neben „Alle löschen" | `DeleteFailedButton` auf `DownloadsPage`, gruppiert mit `DeleteAllButton` | Contract-Test `DownloadsPage_WiresDeleteFailed`, E2E `DownloadsTests` | Abgedeckt |
| Entfernt alle `failed`/`cancelled`-Einträge in einem Schritt inkl. Datei und Manifest-Eintrag | `DownloadService.DeleteFailedAsync` (Filter `FromPersisted` → `Failed`/`Cancelled`, `TryDeleteFile`, Zeilenlöschung) | Service-Test `DeleteFailed_FailedAndCancelled_RemovesOnlyThose` | Abgedeckt |
| Laufende, wartende und fertige Downloads bleiben unberührt | Zustandsfilter im Service; kein `runningTransfers`-Eingriff | Service-Test (verbleibende Einträge) + `DeleteFailed_NoMatchingEntries_ReturnsZero` | Abgedeckt |
| Aktion nur sichtbar/aktiv, wenn mind. ein fehlerhafter Eintrag existiert | `DownloadsPageState.HasFailedItems` → `UpdateUi` + Guard in `UpdateDownloadsDeleteFailedRequested` | `DownloadsPageState_HasFailedItems`, `DownloadsDeleteFailed_WithoutFailedItems_NoEffect`, E2E-Negativ | Abgedeckt |
| Rückfrage vor dem Löschen (Muster „Alle löschen") | `DisplayAlertAsync` in `OnDeleteFailedClicked` | E2E: Dialog-Bestätigung; Contract-Test (`DisplayAlertAsync`) | Abgedeckt |
| Liste und Belegungsanzeige aktualisiert nach dem Löschen | `LoadDownloadsAsync` im Effekt-Handler (wie `DeleteAllDownloadsAsync`) | E2E: Eintrag verschwindet aus der UI-Liste | Abgedeckt |
| Funktioniert offline (lokale Manifest-/Datei-Operationen) | Ausschließlich lokale Operationen; kein Serverzugriff in `DeleteFailedAsync` | Service-Test läuft ohne Netzwerk; implizit | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Fehlerhafte Downloads per Aktion löschen (Happy Path inkl. Rückfrage) | `DownloadsTests`: Download über UI auslösen (`PlaybackStreamFails`), Downloads-Seite, `DeleteFailedButton`, Dialog bestätigen, Eintrag weg | Abgedeckt |
| Schalter-Sichtbarkeit nur bei Fehlereinträgen | Negativ-Behauptung im selben Szenario (vor dem Fehlschlag nicht sichtbar) | Abgedeckt |
| Gemischte Liste (andere Zustände bleiben) | E2E nicht herstellbar — `MockVideoServer` bedient nur einen Download-Kandidaten; Filterregel stattdessen per Service-Test bewiesen | Nicht erforderlich mit Begründung |

## Hinweise

- Die AutomationId-Ergänzungen (`DownloadButton`, `DownloadsCollection`, `EmptyLabel`, `DeleteFailedButton`) sind Voraussetzung der E2E-Erreichbarkeit und im Plan explizit verankert (Schritt 5/8).
- Rückfrage-Dialog (`DisplayAlertAsync`) hat keine AutomationIds — die Bestätigung erfolgt im E2E per `FindByName`/Button-Lookup, Disambiguierung gegenüber den Eintrags-„Löschen"-Schaltern über den Dialogkontext.
