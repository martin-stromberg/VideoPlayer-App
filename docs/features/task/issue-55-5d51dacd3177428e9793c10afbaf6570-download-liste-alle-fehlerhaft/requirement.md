# Übersetzte Anforderung: Alle fehlerhaften Downloads auf einmal löschen

Quelle: Issue #55 (VideoPlayer-App), `issue.md` im Aufgaben-Workspace.

## Fachliche Zusammenfassung

Die Downloads-Seite (`DownloadsPage`) erhält neben der bestehenden Gesamtaktion „Alle löschen" eine zweite Sammelaktion „Alle Fehler löschen" (Arbeitstitel), die sämtliche Manifest-Einträge im Zustand `DownloadState.Failed` und `DownloadState.Cancelled` in einem Schritt entfernt — jeweils inklusive lokaler Datei und Manifest-Zeile. Wartende (`Queued`), laufende (`Downloading`) und fertige (`Completed`) Downloads bleiben unberührt. Die Aktion arbeitet ausschließlich auf lokalen Daten (Manifest/Dateisystem) und ist daher offline funktionsfähig.

## Betroffene Klassen und Komponenten

- `VideoPlayer.Mvu/DownloadService.cs` — neue Methode analog `DeleteAllAsync`, gefiltert auf `failed`/`cancelled`.
- `VideoPlayer.Mvu/AppMessage.cs` — neue Nachricht nach dem Muster `DownloadsDeleteAllRequestedMessage`.
- `VideoPlayer.Mvu/AppEffect.cs` — neuer Effekt nach dem Muster `DeleteAllDownloadsEffect`.
- `VideoPlayer.Mvu/AppUpdate.cs` — neuer Update-Fall nach dem Muster `UpdateDownloadsDeleteAllRequested`.
- `VideoPlayer.Mvu/DownloadsPageState.cs` — neue abgeleitete Eigenschaft für die Sichtbarkeit der Aktion (mindestens ein fehlerhafter/abgebrochener Eintrag).
- `VideoPlayer.Maui/Pages/DownloadsPage.xaml` + `.xaml.cs` — neuer Schalter neben `DeleteAllButton`, Rückfrage-Muster wie `OnDeleteAllClicked`.
- `VideoPlayer.Maui/Services/ShellAppEffectHandler.cs` — neuer Effekt-Fall nach dem Muster `DeleteAllDownloadsAsync` (Service-Aufruf + `LoadDownloadsAsync` zum Aktualisieren von Liste und Belegung).
- Tests (`VideoWebPlayer.Maui.Tests`) — Service-Test (`DownloadServiceTests` bzw. thematisch passende Testklasse), MVU-Transition-Test (`AppUpdateTests_Downloads`), Contract-Test (`DownloadsContractTests`).

## Implementierungsansatz

- Das Feature folgt vollständig dem etablierten MVU-Fluss: UI-Schalter → `*RequestedMessage` → `AppUpdate` erzeugt `*Effect` (nur wenn mind. ein betroffener Eintrag existiert) → `ShellAppEffectHandler` ruft die `DownloadService`-Methode und lädt anschließend die Liste neu (`LoadDownloadsAsync`, aktualisiert Liste und `QuotaText`/`OrphanedBytes`).
- Die Rückfrage erfolgt wie bei „Alle löschen" im Code-Behind via `DisplayAlertAsync` — die Message wird nur nach Bestätigung dispatcht.
- Im `DownloadService` wird eine gefilterte Variante von `DeleteAllAsync` ergänzt: Manifest lesen, Einträge mit `FromPersisted(State) is Failed or Cancelled` selektieren, je Eintrag Datei löschen (`TryDeleteFile`), Manifest-Zeile entfernen, `RaiseItemRemoved` melden. Laufende Transfers müssen nicht abgebrochen werden, da `Failed`/`Cancelled` per Definition keine aktiven Transfers haben; ein defensiver Abbruch ist dennoch prüfenswert.
- Sichtbarkeit des Schalters: neue abgeleitete Eigenschaft auf `DownloadsPageState` (z. B. `HasFailedItems` über `Items` mit `State is Failed or Cancelled`), im `UpdateUi` der Seite ausgewertet.

## Konfiguration

Keine Konfiguration erforderlich — Verhalten ist fest verdrahtet (Muster „Alle löschen"/„Speicher bereinigen").

## Offene Fragen

- Zählt `Cancelled` als „fehlerhaft" im Sinne der Aktion? — Annahme: ja (Issue-Text nennt `failed`/`cancelled` explizit; „Erneut versuchen" deckt beide Zustände ab und „Abgebrochen"-Einträge sind ebenfalls toter Ballast). Als umgesetzt gekennzeichnet: beide Zustände werden entfernt.
