# Anforderung: Verwaiste und unvollständige Download-Daten erkennen und bereinigen

Quelle: Entwicklungsschritt 1 des Projektplans „Download optimieren" (Issue #51),
Projekt-Basisbranch `task/issue-51-5e323baba37842bf8094a24f94c9013b-download-optimieren`.
Stakeholder-Entscheidung: erkannte Restdaten ohne Manifest-Bezug bleiben für die
anwendergesteuerte Bereinigung erkennbar; nur zweifelsfrei überholte Teildateien werden
automatisch entfernt.

## Fachliche Zusammenfassung

Die App gleicht den Video-Ablageort mit dem Download-Manifest ab, damit Dateireste
(verwaiste Dateien ohne Manifest-Eintrag, überholte Teildateien) erkannt und — je nach
Kategorie — automatisch entfernt oder dem Anwender bereinigbar gemeldet werden. Die
angezeigte Speicherbelegung soll dem tatsächlichen Dateibestand entsprechen. Der Abgleich
läuft beim App-Start (im Rahmen der bestehenden Wiederaufnahme unterbrochener Downloads)
und beim Öffnen der Downloads-Seite.

## Fachliche Anforderungen (aus dem Entwicklungsschritt)

1. **Abgleich Ablageort ↔ Manifest** beim App-Start und beim Öffnen der Downloads-Seite:
   - Dateien ohne Manifest-Eintrag werden als verwaiste Restdaten erkannt und gesondert
     erfasst (Anzahl + Größe) — **nicht** automatisch entfernt.
   - Zweifelsfrei überholte Teildateien (Reste abgebrochener/fehlgeschlagener Transfers)
     werden automatisch entfernt.
   - Manifest-Einträge „läuft" ohne laufenden Transfer gelten als unterbrochen → bestehende
     Wiederaufnahme-Logik (`ResumeInterruptedAsync`).
   - „Fertig"-Einträge ohne Datei werden als nicht mehr vorhanden behandelt und können
     erneut eingeplant werden (Zustand nicht mehr „fertig").
2. **Belegungsanzeige** zeigt den tatsächlichen Dateibestand inkl. erkannter Restdaten.
3. **Protokollierung** des Abgleichs; niemals Dateien entfernen, die einem
   Manifest-Eintrag in aktivem/abgeschlossenem Zustand zugeordnet sind.

## Akzeptanzkriterien

1. Eine Datei ohne Manifest-Eintrag wird beim Abgleich als Restdaten erkannt (kein
   stilles Löschen, kein unsichtbarer Speicherverbrauch).
2. Ein „fertig" geführter Eintrag ohne Datei wird als nicht mehr vorhanden behandelt und
   kann erneut eingeplant werden.
3. Die angezeigte Belegung entspricht dem tatsächlichen Dateibestand.
4. Laufende oder wartende Downloads werden durch den Abgleich nicht beeinträchtigt.

## Betroffene Klassen und Komponenten

- `DownloadService` (`VideoPlayer.Mvu`) — neuer Abgleich (Scan des `layout.VideosDirectory`
  gegen das Manifest), Erweiterung von `LoadItemsAsync`/`ResumeInterruptedAsync`-Aufrufern,
  Ergebnisobjekt für erkannte Restdaten.
- `DownloadManifestEntry` (`VideoPlayer.Storage.Entities`) — Bestand; ggf. Zustandskorrektur
  für „fertig"-Einträge ohne Datei.
- `DownloadsPageState` / `DownloadsLoadedMessage` / `AppUpdate` — Transport der erkannten
  Restdaten-Größe zur Anzeige (Belegung inkl. Restdaten; die anwenderseitige
  Bereinigungs-Aktion ist Schritt 2 des Projekts und nicht Teil dieses Schritts).
- `DownloadsPage` — Anzeige der erkannten Restdaten (z. B. im Quota-Text oder eigener
  Hinweiszeile), ohne Löschaktion (gehört zu Schritt 2).
- Tests in `VideoWebPlayer.Maui.Tests` (`DownloadServiceTests`, `AppUpdateTests_Downloads`).

## Implementierungsansatz

- Neue Service-Methode (z. B. `ReconcileAsync`) im `DownloadService`: Enumeriert
  `layout.VideosDirectory`, vergleicht mit den `FilePath`-Werten des Manifests.
- Kategorisierung je gefundener Datei: Manifest-Bezug aktiv/fertig (behalten),
  Manifest-Bezug fehlgeschlagen/abgebrochen mit vorhandener Teildatei (überholte
  Teildatei → entfernen), kein Manifest-Bezug (verwaiste Restdaten → erfassen, behalten).
- „Fertig"-Einträge ohne Datei: Zustandskorrektur (z. B. `failed` mit neuem/neutralen
  Hinweis oder erneut einplanbar), damit sie nicht als vorhanden gelten.
- Aufruf des Abgleichs: aus `ResumeInterruptedAsync`-Pfad (App-Start/Verbindungsaufbau)
  und beim Laden der Downloads-Seite (`LoadDownloadsEffect`-Pfad).
- `GetUsedBytes` bleibt die Quelle der Belegung (zählt Dateisystem) — nach Bereinigung
  bzw. mit ausgewiesenen Restdaten ist die Anzeige konsistent.

## Konfiguration

Keine neue Konfiguration. Bestehendes Kontingent und WLAN-Bedingung unverändert.

## Explizite Nicht-Ziele

- Keine Lösch-Aktion für Restdaten in der UI (Schritt 2 des Projekts).
- Keine Hintergrund-Downloads (Schritt 3 des Projekts).
- Keine Änderungen am Altbestand `VideoWebPlayer.Maui`.
- Keine Server-Protokoll-Änderungen (keine Range-Requests).

## Offene Fragen

- Keine (Stakeholder-Entscheidung zur Bereinigung liegt vor; siehe Projektplan).
