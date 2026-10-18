# Umsetzungsplan: Alle fehlerhaften Downloads auf einmal löschen

## Übersicht

Die Downloads-Seite erhält neben „Alle löschen" eine Sammelaktion „Alle Fehler löschen", die alle Manifest-Einträge im Zustand `Failed`/`Cancelled` in einem Schritt entfernt (Datei + Manifest-Zeile). Umsetzung vollständig im etablierten MVU-Fluss: neuer `DownloadService`-Methodenaufruf, neue Message, neuer Effect, neuer Update-Fall, Effekt-Verdrahtung im `ShellAppEffectHandler`, Schalter auf der `DownloadsPage`.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Zustandsmenge | `Failed` **und** `Cancelled` | Issue-Text nennt beide explizit; `Cancelled` entspricht fachlich „nicht erfolgreich beendet" und deckt sich mit der `CanRetry`-Menge des `DownloadListItem`. |
| Service-API | Neue Methode `DeleteFailedAsync` statt Parameter an `DeleteAllAsync` | Eigenständige, klar benannte Operation wie `PurgeOrphansAsync`; `DeleteAllAsync` bleibt unverändert für „Alle löschen". |
| Sichtbarkeitsregel | Abgeleitete Eigenschaft `HasFailedItems` auf `DownloadsPageState` | Muster `HasItems`/`ShowEmpty` — keine separate Liste, die Auswertung erfolgt über `Items`. |
| Laufende Transfers | Kein Abbruch nötig | `Failed`/`Cancelled`-Einträge haben per Definition keinen aktiven Transfer; im Gegensatz zu `DeleteAllAsync` entfällt der `runningTransfers`-Sweep. |

## Programmabläufe

### „Alle Fehler löschen" auslösen

1. `DownloadsPage.UpdateUi` setzt `DeleteFailedButton.IsVisible = model.Downloads.HasFailedItems`.
2. `OnDeleteFailedClicked` zeigt `DisplayAlertAsync` (Rückfrage, Muster `OnDeleteAllClicked`); bei Bestätigung `DispatchAsync(new DownloadsDeleteFailedRequestedMessage())`.
3. `AppUpdate.Update` → `UpdateDownloadsDeleteFailedRequested`: bei `model.Downloads.HasFailedItems` `DeleteFailedDownloadsEffect(model.Settings)`, sonst `AppTransition.Unchanged`.
4. `ShellAppEffectHandler` → `DeleteFailedDownloadsAsync`: `downloads.DeleteFailedAsync(ct)`, danach `LoadDownloadsAsync(settings)` (Liste + `QuotaText`/`OrphanedBytes` aktualisieren sich wie bei `DeleteAllDownloadsAsync`).

### `DownloadService.DeleteFailedAsync`

1. Manifest-Tabelle lesen (Fehlerfall wie `DeleteAllAsync`: Warnung + `0`).
2. Einträge mit `DownloadStateNames.FromPersisted(e.State) is Failed or Cancelled` selektieren.
3. Je Eintrag: `TryDeleteFile(entry.FilePath)`, Manifest-Zeile unter `manifestGate` löschen, `RaiseItemRemoved(entry.VideoType, entry.VideoId)`.
4. Rückgabe: Anzahl entfernter Einträge; `LogInformation` wie bei `DeleteAllAsync`.

Beteiligte Klassen/Komponenten: `DownloadsPage`, `DownloadsPageState`, `AppUpdate`, `AppMessage`, `AppEffect`, `ShellAppEffectHandler`, `DownloadService`, `DownloadManifestEntry`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `DownloadsDeleteFailedRequestedMessage` | `AppMessage` (Record) | Anwender-Anforderung „Alle Fehler löschen" (nach Rückfrage). |
| `DeleteFailedDownloadsEffect` | `AppEffect` (Record, `AppSettings Settings`) | Effekt-Datensatz für die gefilterte Sammel-Löschung. |

## Änderungen an bestehenden Klassen

### `DownloadService` (Logikklasse)

- **Neue Methoden:** `DeleteFailedAsync(CancellationToken)` → `Task<int>` — entfernt alle `Failed`/`Cancelled`-Einträge inkl. Datei und Manifest-Zeile, meldet `ItemRemoved` je Eintrag.

### `DownloadsPageState` (Zustandsrecord)

- **Neue Eigenschaften:** `HasFailedItems` (`bool`) — `Items.Any(i => i.State is Failed or Cancelled)`; steuert die Sichtbarkeit des neuen Schalters.

### `AppUpdate` (Logikklasse)

- **Neue Methoden:** `UpdateDownloadsDeleteFailedRequested` — Effekt nur bei `HasFailedItems`.
- **Geänderte Methoden:** Dispatch-Tabelle in `Update` — neuer Fall `DownloadsDeleteFailedRequestedMessage`.

### `ShellAppEffectHandler` (Effekt-Handler)

- **Geänderte Methoden:** Effekt-Switch — neuer Fall `DeleteFailedDownloadsEffect`.
- **Neue Methoden:** `DeleteFailedDownloadsAsync(AppSettings, CancellationToken)` — Service-Aufruf + `LoadDownloadsAsync` (Muster `DeleteAllDownloadsAsync`, inkl. Fehlerprotokollierung).

### `DownloadsPage` (XAML + Code-Behind)

- XAML: neuer `Button` `DeleteFailedButton` („Alle Fehler löschen", Stil wie `DeleteAllButton`: `GhostButton` + `VpDanger`, `AutomationId="DeleteFailedButton"`); `DeleteAllButton` und `DeleteFailedButton` werden gemeinsam in einem `HorizontalStackLayout` am rechten Rand von `Grid.Row="0"` gruppiert; `AutomationId` ergänzend an `DeleteAllButton`, `DownloadsCollection` und `EmptyLabel` (E2E-Erreichbarkeit, Muster anderer Seiten).
- Code-Behind: `OnDeleteFailedClicked` (Rückfrage + Dispatch), `UpdateUi` um `DeleteFailedButton.IsVisible` erweitert.

### `MediaDetailsPage` (XAML)

- `AutomationId="DownloadButton"` am vorhandenen `DownloadButton` ergänzen (E2E-Auslöser des Download-Flusses; `x:Name` bleibt für die Sichtbarkeitssteuerung).

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine — die Aktion ist zustandsgesteuert und nur bei vorhandenen Fehlereinträgen erreichbar (Bedingung in `UpdateUi` und nochmals in `AppUpdate`).

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Layout `DownloadsPage`:** `DeleteAllButton` wird in ein `HorizontalStackLayout` umgruppiert — optische Anordnung der beiden Schalter prüfen (Usability-Review).
- **`DownloadsContractTests`:** Die Quelltext-Verträge prüfen weiterhin vorhandene Muster; neue Vertragsprüfungen für die Aktion kommen hinzu, bestehende bleiben unverändert.
- **Race `Cancelled`↔laufender Transfer:** Ein vom Transfer selbst auf `Cancelled` gesetzter Eintrag hat keinen aktiven Transfer mehr; `TryDeleteFile` toleriert gesperrte/fehlende Dateien — Restrisiko: eine verwaischte Teildatei bleibt liegen und wird beim nächsten `ReconcileAsync` als Restdatum erkannt (`PurgeOrphans`). Akzeptabel, analog `DeleteAllAsync`-Semantik.

## Umsetzungsreihenfolge

1. **`DownloadService.DeleteFailedAsync`**
   - Voraussetzungen: keine (vorhandene Hilfsmittel `TryDeleteFile`, `RaiseItemRemoved`, `manifestGate`, `DownloadStateNames`)
   - Beschreibung: Methode nach dem Muster `DeleteAllAsync` mit Zustandsfilter `Failed`/`Cancelled` ergänzen.

2. **`DownloadsPageState.HasFailedItems`**
   - Voraussetzungen: keine
   - Beschreibung: abgeleitete Eigenschaft ergänzen.

3. **Message + Effect + Update-Fall**
   - Voraussetzungen: Schritt 2 (`HasFailedItems` für die Effekt-Bedingung)
   - Beschreibung: `DownloadsDeleteFailedRequestedMessage` (`AppMessage.cs`), `DeleteFailedDownloadsEffect` (`AppEffect.cs`), Dispatch + `UpdateDownloadsDeleteFailedRequested` (`AppUpdate.cs`).

4. **Effekt-Verdrahtung `ShellAppEffectHandler`**
   - Voraussetzungen: Schritte 1 und 3
   - Beschreibung: Switch-Fall + `DeleteFailedDownloadsAsync` (Service + `LoadDownloadsAsync`).

5. **UI `DownloadsPage` + `MediaDetailsPage`-AutomationId**
   - Voraussetzungen: Schritte 2 und 3
   - Beschreibung: Schalter `DeleteFailedButton` + Gruppierung, `OnDeleteFailedClicked`, `UpdateUi`-Erweiterung, AutomationIds.

6. **Tests (Unit/Contract)**
   - Voraussetzungen: Schritte 1–5
   - Beschreibung: Service-, Transition- und Contract-Tests (Tabelle unten).

7. **E2E-Test**
   - Voraussetzungen: Schritte 1–5 inkl. AutomationIds
   - Beschreibung: `DownloadsTests` (FlaUI) — fehlgeschlagener Download über `PlaybackStreamFails`, Aktion + Rückfrage, Ergebnis am UI-Baum.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `DeleteFailed_FailedAndCancelled_RemovesOnlyThose` | `DownloadServiceTests` | `failed`- und `cancelled`-Einträge (Manifest-Vorbelegung) werden samt Datei entfernt; `queued`/`completed` bleiben; `ItemRemoved` je Eintrag; Rückgabe = Anzahl. |
| `DeleteFailed_NoMatchingEntries_ReturnsZero` | `DownloadServiceTests` | Leere/keine Treffer → `0`, keine Events, andere Einträge unberührt. |
| `DownloadsDeleteFailed_WithFailedItems_EmitsEffect` | `AppUpdateTests_Downloads` | `DownloadsDeleteFailedRequestedMessage` → `DeleteFailedDownloadsEffect` bei `Failed`-Eintrag. |
| `DownloadsDeleteFailed_WithoutFailedItems_NoEffect` | `AppUpdateTests_Downloads` | Nur `Queued`/`Completed` → keine Effekte. |
| `DownloadsPageState_HasFailedItems` | `DownloadsContractTests` | Ableitung über `Failed`/`Cancelled`, negativ bei anderen Zuständen (State-Test). |
| `DownloadsPage_WiresDeleteFailed` | `DownloadsContractTests` | `DeleteFailedButton`/`OnDeleteFailedClicked` im XAML, `DownloadsDeleteFailedRequestedMessage` + `DisplayAlertAsync` im Code-Behind, `HasFailedItems` in `UpdateUi`, `DeleteFailedDownloadsEffect` im Effekt-Handler. |

### Betroffene bestehende Tests

Keine.

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Fehlgeschlagener Download → Downloads-Seite → „Alle Fehler löschen" → Rückfrage bestätigen → Eintrag entfernt, Schalter verschwindet | `VideoPlayer.E2E.Tests/Tests/DownloadsTests.cs` | Kompletter Benutzerfluss inkl. Sichtbarkeitsregel, Rückfrage und Listen-/Belegungsaktualisierung | Einzige Stufe, die UI-Verdrahtung (Schalter, Dialog, Flyout-Navigation) real nachweist |
| Pflicht | Downloads-Seite ohne fehlerhafte Einträge → Schalter nicht sichtbar | `DownloadsTests` (selbes Szenario, Vorbedingung) | Sichtbarkeitsregel „nur bei Fehlereinträgen" | Negativ-Nachweis am echten UI-Baum |

Welche bestehenden E2E-Tests müssen angepasst werden? Keine.

Hinweis: Das gemischte Szenario „fehlerhafte + andere Einträge → nur Fehler entfernt" ist E2E-seitig nicht herstellbar (der `MockVideoServer` bedient genau einen Download-Kandidaten; zwei Downloads unterschiedlichen Ausgangs sind nicht konfigurierbar) — die Filterregel wird stattdessen auf Service-Ebene bewiesen (`DeleteFailed_FailedAndCancelled_RemovesOnlyThose`), die E2E-Abdeckung belegt den Benutzerfluss selbst.

## Offene Punkte

Keine.
