# Umsetzungsplan: Verwaiste und unvollständige Download-Daten erkennen und bereinigen

## Übersicht

Ein Abgleich („Reconcile") zwischen Video-Ablageort und Download-Manifest wird im
`DownloadService` ergänzt: überholte Teildateien werden automatisch entfernt, „fertig"
geführte Einträge ohne Datei werden als fehlerhaft korrigiert, und Dateien ohne
Manifest-Bezug werden als nicht zugeordnete Restdaten erfasst (Größe/Anzahl) — sichtbar
auf der Downloads-Seite, aber nicht automatisch gelöscht (Stakeholder-Entscheidung).
Der Abgleich läuft beim App-Start/Verbindungsaufbau (vor der bestehenden Wiederaufnahme)
und beim Öffnen der Downloads-Seite.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Abgleich-Logik | Neue Methode `ReconcileAsync` im bestehenden `DownloadService` (kein separater Dienst) | Der Dienst besitzt bereits Manifest-Zugriff, `layout.VideosDirectory`, `TryDeleteFile`, `fileExists`-Abstraktion und `runningTransfers`; ein Abgleich ist eine Erweiterung desselben Verantwortungsbereichs (Service Layer). |
| Ergebnis des Abgleichs | Neuer unveränderlicher Record `DownloadReconcileResult` (Value Object) mit `RemovedPartialBytes`/`RemovedPartialCount`, `OrphanedBytes`/`OrphanedFileCount`, `CorrectedCompletedCount` | Der Abgleich läuft an zwei Stellen (Start/Seite); ein Value Object kapselt das Ergebnis ohne Zustand am Dienst. |
| Behandlung „fertig ohne Datei" | Zustandskorrektur auf `failed` mit neuem Fehlercode `missing_file` | Macht den Eintrag in der Liste sichtbar falsch (statt falschem „Fertig") und direkt erneut einplanbar (`CanRetry`) — bestehendes Fehler-/Retry-Muster. |
| Restdaten-Anzeige | `DownloadsLoadedMessage` + `DownloadsPageState` um `OrphanedBytes` erweitert; neue Zeile/Anzeige `OrphanedText` | Minimaler, reversibler Anzeigepunkt; die eigentliche Löschaktion folgt in Projekt-Schritt 2. |
| Aufrufpunkte | `ResumeDownloadsAsync` (vor `ResumeInterruptedAsync`) und `LoadDownloadsAsync` (vor dem Laden) im `ShellAppEffectHandler` | Beide Pfade sind die etablierten Eintrittspunkte (App-Start/Online-Wechsel bzw. Seitenaufruf); keine neuen Effekte/Messages nötig. |

## Programmabläufe

### Abgleich beim App-Start / Verbindungsaufbau

1. `AppUpdate` dispatcht `ResumeDownloadsEffect` (bestehend).
2. `ShellAppEffectHandler.ResumeDownloadsAsync` ruft zuerst
   `downloads.ReconcileAsync()` auf, danach unverändert
   `ResumeInterruptedAsync(settings)`.
3. `ReconcileAsync` korrigiert `downloading`-Einträge ohne laufenden Transfer
   nicht selbst — das übernimmt `ResumeInterruptedAsync` (bestehende Logik);
   zugehörige Rest-Teildateien werden vorher entfernt.
4. Fehler des Abgleichs werden geloggt und unterbrechen die Wiederaufnahme
   nicht (robustes Verhalten).

Beteiligte Klassen/Komponenten: `ShellAppEffectHandler`, `DownloadService`,
`ResumeDownloadsEffect`, `AppUpdate`.

### Abgleich beim Öffnen der Downloads-Seite

1. `DownloadsPageOpenedMessage` → `UpdateDownloadsPageOpened` →
   `LoadDownloadsEffect` (bestehend).
2. `ShellAppEffectHandler.LoadDownloadsAsync` ruft `downloads.ReconcileAsync()`
   auf; Ergebnis liefert `OrphanedBytes`/`OrphanedFileCount`.
3. Danach unverändert: `LoadItemsAsync` + `GetUsedBytes` →
   `DownloadsLoadedMessage` (um `OrphanedBytes` erweitert).
4. `UpdateDownloadsLoaded` überträgt `OrphanedBytes` in `DownloadsPageState`.
5. `DownloadsPage` zeigt unterhalb der Kontingentzeile eine zweite
   Caption-Zeile („davon x GB nicht zugeordnet"), nur sichtbar wenn
   `OrphanedBytes > 0`.

Beteiligte Klassen/Komponenten: `ShellAppEffectHandler`, `DownloadService`,
`DownloadsLoadedMessage`, `DownloadsPageState`, `AppUpdate`, `DownloadsPage`.

### Abgleichslogik `ReconcileAsync`

1. Alle Dateien in `layout.VideosDirectory` enumerieren (nicht vorhanden →
   leer; `IOException`/`UnauthorizedAccessException` → loggen + leeres
   Ergebnis).
2. Manifest-Einträge laden; `FilePath`-Menge (OrdinalIgnoreCase) bilden.
3. Je gefundener Datei:
   - kein Manifest-Eintrag → Restdaten (Größe summieren, behalten);
   - Eintrag `completed` → behalten;
   - Eintrag `downloading` **und** Key in `runningTransfers` → behalten
     (aktiv geschrieben);
   - Eintrag `queued`/`failed`/`cancelled` oder `downloading` ohne laufenden
     Transfer → überholte Teildatei → `TryDeleteFile`, Größe summieren.
4. Je Manifest-Eintrag mit `completed` und `!fileExists(FilePath)` →
   `SetStateAsync(entry, Failed, ErrorMissingFile)` (korrigierte Einträge
   zählen).
5. Ergebnis-Record zurückgeben; Zusammenfassung per `LogInformation`
   (entfernte Teildateien, erkannte Restdaten, korrigierte Einträge).

Beteiligte Klassen/Komponenten: `DownloadService`, `DownloadManifestEntry`,
`DownloadReconcileResult`, `AppStorageLayout`, `CacheDatabase`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `DownloadReconcileResult` | Record (unveränderlich) | Ergebnis des Abgleichs: entfernte Teildateien, erkannte Restdaten, korrigierte „fertig"-Einträge |

## Änderungen an bestehenden Klassen

### `DownloadService` (Logikklasse)

- **Neue Konstante:** `ErrorMissingFile` (`string`, Wert `"missing_file"`) —
  Fehlercode für „fertig"-Einträge ohne Datei.
- **Neue Methoden:** `ReconcileAsync(CancellationToken)` —
  `Task<DownloadReconcileResult>`; führt den oben beschriebenen Abgleich aus.

### `DownloadListItem` (Anzeige-Record)

- **Geänderte Eigenschaft:** `ErrorText` — neuer Fall `missing_file` →
  „Die Datei ist nicht mehr vorhanden." (verständlicher Hinweis, der die
  erneute Einplanung erklärt).

### `DownloadsLoadedMessage` (MVU-Message)

- **Neuer Parameter:** `long OrphanedBytes` — Bytes der erkannten Restdaten
  (0, wenn keine bzw. Abgleich fehlgeschlagen). Parameter wird ans Ende
  angehängt.

### `DownloadsPageState` (MVU-State)

- **Neue Eigenschaft:** `OrphanedBytes` (`long`, Standard 0) — erkannte
  nicht zugeordnete Restdaten.
- **Neue abgeleitete Eigenschaft:** `OrphanedText` (`string?`) — z. B.
  „davon 0,8 GB nicht zugeordnet"; `null` wenn `OrphanedBytes == 0`.
- `QuotaText` bleibt unverändert (Belegung inkl. Restdaten = realer
  Dateibestand).

### `ShellAppEffectHandler` (Effekt-Handler)

- **Geänderte Methoden:** `ResumeDownloadsAsync` — ruft vor der
  Wiederaufnahme `ReconcileAsync` (Fehler geloggt, nicht fatal);
  `LoadDownloadsAsync` — ruft `ReconcileAsync` auf und gibt
  `OrphanedBytes` in `DownloadsLoadedMessage` weiter.

### `AppUpdate` (MVU-Update)

- **Geänderte Methode:** `UpdateDownloadsLoaded` — übernimmt
  `OrphanedBytes` aus der Message in `DownloadsPageState`.

### `DownloadsPage` (XAML + Code-Behind)

- **Neue Anzeige:** Caption-Label `OrphanedLabel` unter `QuotaLabel`,
  gebunden an `model.Downloads.OrphanedText` (sichtbar nur bei `null !=
  OrphanedText`). Keine Aktionen — Bereinigung per Anwenderaktion ist
  Projekt-Schritt 2.

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine.

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Quota-Verhalten:** `GetUsedBytes` bleibt unverändert (zählt alle Dateien);
  erkannte Restdaten bleiben in der Belegung sichtbar bis zur Bereinigung —
  gewollt (Transparenz statt verstecktem Verbrauch).
- **Auto-Löschen gesehener Episoden:** `DeleteIfCompletedAsync` unverändert;
  korrigierte `missing_file`-Einträge sind `failed`, nicht `completed` →
  werden vom Auto-Delete nicht berührt (korrekt: nichts zu löschen).
- **Wiederholte Abgleiche:** `ReconcileAsync` ist idempotent; läuft bei jedem
  Online-Wechsel/Seitenaufruf — Kosten = ein Verzeichnis-Scan + Manifest-Read,
  vernachlässigbar.
- **Race mit laufenden Transfers:** Dateien von `downloading`-Einträgen mit
  Eintrag in `runningTransfers` werden nie angefasst; `queued`-Einträge ohne
  laufenden Transfer können keine aktive Schreibdatei besitzen (der Transfer
  setzt `downloading` vor dem `FileStream`).

## Umsetzungsreihenfolge

1. **`DownloadReconcileResult` anlegen**
   - Voraussetzungen: Keine
   - Beschreibung: Neuer Record in `VideoPlayer.Mvu` mit den vier
     Ergebniswerten.
2. **`DownloadService.ReconcileAsync` + `ErrorMissingFile`**
   - Voraussetzungen: Schritt 1
   - Beschreibung: Abgleichslogik gemäß Programmablauf; Logging;
     `fileExists`-Abstraktion für die Datei-Prüfung verwenden.
3. **`DownloadListItem.ErrorText`-Fall `missing_file`**
   - Voraussetzungen: Schritt 2 (Konstante)
   - Beschreibung: Neuer Switch-Fall mit Anzeigetext.
4. **MVU-Transport: `DownloadsLoadedMessage`, `DownloadsPageState`, `AppUpdate`**
   - Voraussetzungen: Schritt 1
   - Beschreibung: `OrphanedBytes` durchreichen; `OrphanedText` ableiten.
5. **`ShellAppEffectHandler`: Aufrufpunkte verdrahten**
   - Voraussetzungen: Schritte 2, 4
   - Beschreibung: `ReconcileAsync` in `ResumeDownloadsAsync` und
     `LoadDownloadsAsync` aufrufen; Ergebnis in die Message.
6. **`DownloadsPage.xaml`: `OrphanedLabel`**
   - Voraussetzungen: Schritt 4
   - Beschreibung: Sichtbarkeitsgesteuertes Label unter `QuotaLabel`.
7. **Tests: neue `DownloadServiceReconcileTests` + Anpassungen**
   - Voraussetzungen: Schritte 1–6
   - Beschreibung: Neue Testklasse (realer Temp-Ablageort wie
     `DownloadServiceTests`), betroffene bestehende Tests anpassen.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `Reconcile_OrphanedFile_IsCountedAndKept` | `DownloadServiceReconcileTests` | Datei ohne Manifest-Eintrag → `OrphanedBytes`/`OrphanedFileCount` > 0, Datei bleibt |
| `Reconcile_StalePartialOfFailedEntry_IsRemoved` | `DownloadServiceReconcileTests` | `failed`-Eintrag mit Datei → Datei entfernt, `RemovedPartial*` > 0 |
| `Reconcile_StalePartialOfCancelledEntry_IsRemoved` | `DownloadServiceReconcileTests` | `cancelled`-Eintrag mit Datei → entfernt |
| `Reconcile_StalePartialOfOrphanedDownloadingEntry_IsRemoved` | `DownloadServiceReconcileTests` | `downloading` ohne laufenden Transfer + Datei → entfernt |
| `Reconcile_CompletedFile_IsKept` | `DownloadServiceReconcileTests` | `completed`-Eintrag mit Datei → unverändert |
| `Reconcile_CompletedWithoutFile_MarkedMissing` | `DownloadServiceReconcileTests` | `completed` ohne Datei → `failed` + `missing_file`, `CanRetry` |
| `Reconcile_QueuedEntryFile_IsRemoved` | `DownloadServiceReconcileTests` | verwaiste Teildatei zu `queued`-Eintrag → entfernt |
| `Reconcile_RunningTransferFile_IsKept` | `DownloadServiceReconcileTests` | laufender Transfer (verzögerter Stub-Stream) + Datei → bleibt |
| `Reconcile_MissingDirectory_ReturnsEmpty` | `DownloadServiceReconcileTests` | nicht vorhandenes Video-Verzeichnis → leeres Ergebnis, kein Fehler |
| `UpdateDownloadsLoaded_OrphanedBytes_InState` | `AppUpdateTests_Downloads` | `DownloadsLoadedMessage` mit `OrphanedBytes` → `Downloads.OrphanedBytes` |
| `DownloadListItem_MissingFile_ErrorText` | `DownloadsContractTests` | `Error = "missing_file"` → verständlicher Text |
| `DownloadsPageState_OrphanedText_WhenZero_Null` | `DownloadsContractTests` | `OrphanedBytes = 0` → `OrphanedText` null |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `AppUpdateTests_Downloads` (Aufrufe von `DownloadsLoadedMessage`) | Neuer positionaler Parameter `OrphanedBytes` |

### E2E-Tests (primärer Funktionsnachweis)

Keine neuen E2E-Tests erforderlich — Begründung: Der Schritt ändert keinen
Benutzerfluss (keine neue Interaktion); der Abgleich läuft automatisch und
die einzige sichtbare Änderung ist eine zusätzliche Textzeile auf der
bestehenden Downloads-Seite. Projektkonvention für Download-Features: die
Absicherung erfolgt über MVU-Übergangstests (`AppUpdateTests_Downloads`),
Service-Tests mit realem Temp-Dateisystem (`DownloadServiceReconcileTests`)
und Contract-Tests der Anzeige-Ableitungen (`DownloadsContractTests`). Ein
Downloads-E2E-Szenario existiert in `VideoPlayer.E2E.Tests` bislang nicht;
der Automatik-Abgleich ohne UI-Aktion ist für FlaUI nicht sinnvoll
auslösbar.

## Offene Punkte

Keine.
