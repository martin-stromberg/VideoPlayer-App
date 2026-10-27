# Anforderung: Downloadbereinigung — Ablaufdatum, Herkunft und automatische Bereinigung von Downloads

Quelle: Kundenanforderung zur Aufgabe `9b61966d-d57a-4085-85ed-e8228bd397fb` („Downloadbereinigung", `issue.md` im Projektstamm).

## Fachliche Zusammenfassung

Jeder Download im `download_manifest` erhält ein Ablaufdatum (`ExpiresAtUtc`):
Fertigstellungszeitpunkt plus konfigurierbarer Aufbewahrungszeit. Der reguläre
Bereinigungslauf (`ReconcileAsync`) entfernt abgelaufene Einträge vollständig
(Datei, Manifest-Eintrag, `ItemRemoved`); Einträge, die weiterhin
Auto-Download-Kandidaten sind, werden stattdessen verlängert und auf die
Herkunft `auto` umgestellt (UI-Bezeichnung „Cache"). Ergänzend ersetzt eine neue
Option „Downloads nach dem Abspielen automatisch löschen" die bisherige
Episoden-Sofortlöschung durch eine verkürzte Ablauffrist, und ein
Wiedergabe-Schutz verlängert das Ablaufdatum während des Abspielens. Alle
Funktionen arbeiten offline auf lokalen Manifest-, Cache- und Dateidaten.

## Akzeptanzkriterien

**Herkunft der Downloads**

1. Jeder Manifest-Eintrag führt seine Herkunft: `manual` (über die Oberfläche
   eingeplant: Detailseite, Episodenliste, „Erneut versuchen") oder `auto`
   (durch den `AutoDownloadService` eingeplant bzw. durch Verlängerung
   übernommen, siehe Kriterium 11).
2. Die Herkunft wird beim Einreihen festgelegt und persistiert. Ein erneutes
   Einreihen (Re-Enqueue) behält die bisherige Herkunft — Ausnahme: die
   Verlängerung als Auto-Download-Kandidat (Kriterium 11) stellt einen
   manuellen Eintrag auf `auto` um.
3. In der Downloads-Liste ist die Herkunft je Eintrag sichtbar; für
   `auto`-Einträge wird in der UI der Begriff **„Cache"** verwendet
   (z. B. Kennzeichnung „Cache" am Eintrag — kein technisches „auto").

**Ablaufdatum und Bereinigung**

4. Jeder Download erhält ein Ablaufdatum (`ExpiresAtUtc`): Fertigstellungs-
   zeitpunkt + konfigurierte Aufbewahrungszeit. Wartende/laufende Aufträge
   haben kein Ablaufdatum — die Frist beginnt erst mit `completed`.
5. Der reguläre Bereinigungslauf (im bestehenden Abgleich `ReconcileAsync`,
   also App-Start und Öffnen der Downloads-Seite — ggf. ergänzt um
   periodische Trigger) prüft alle Einträge mit erreichtem Ablaufdatum:
   - Weiterhin Auto-Download-Kandidat → Verlängerung (Kriterium 11).
   - Kein Kandidat → vollständige Entfernung: Datei löschen,
     Manifest-Eintrag löschen, `ItemRemoved` melden (wie manuelles Löschen).
   Laufende Transfers werden nie angefasst.
6. Die Aufbewahrungszeit ist eine neue Einstellung
   („Aufbewahrungszeit für Downloads", Angabe in Tagen; Vorschlag Standard:
   7 Tage).
7. Änderungen der Einstellungen (Aufbewahrungszeit, Löschverzögerung) wirken
   **nur auf neue Downloads** bzw. neu gesetzte Ablaufdaten — bestehende
   `ExpiresAtUtc`-Werte werden nicht nachträglich neu berechnet.

**Automatisches Löschen nach dem Abspielen**

8. Neue Option „Downloads nach dem Abspielen automatisch löschen"
   (standardmäßig aus). Sie ersetzt die bisherige, nur auf Episoden
   wirkende Option „Gesehene Episoden automatisch löschen" und gilt für
   alle ladbaren Medientypen (Filme und Episoden); die bestehende
   Einstellung wird migriert bzw. entfällt.
9. Mit aktivierter Option wird bei Erreichen der „angesehen"-Schwelle
   (Endsequenz bzw. eingestelltes Endfenster) das Ablaufdatum des Downloads
   auf `jetzt + Löschverzögerung` gesetzt — statt der bisherigen Sofort-
   Löschung. Die eigentliche Entfernung übernimmt der reguläre
   Bereinigungslauf.
10. Die Löschverzögerung ist eine eigene Einstellung
    („Löschverzögerung nach dem Abspielen", Angabe in Minuten, Standard 15).

**Verlängerung statt Löschung bei Auto-Download-Kandidaten**

11. Erreicht der Bereinigungslauf einen abgelaufenen Eintrag, dessen Medium
    weiterhin Auto-Download-Kandidat ist — Selbst-Kandidat in der
    Weiterschauen-Liste oder im Vorlauf-Fenster eines Weiterschauen-Eintrags
    (Folge-Episode in Serienreihenfolge bzw. Nachfolger in Playlist-
    Reihenfolge bis `PreloadEpisodeCount`) —, wird der Eintrag **nicht
    gelöscht**, sondern:
    - `ExpiresAtUtc` wird auf `jetzt + Aufbewahrungszeit` hochgesetzt,
    - die Herkunft wird auf `auto` gesetzt — ein manueller Download wird
      damit zum automatischen Download und erscheint in der UI als „Cache".
12. Dieselbe Verlängerung kann bereits beim regulären Auto-Download-Lauf
    erfolgen: berührt der Vorlauf einen Kandidaten, der bereits als
    Download existiert (Dedup-Pfad), wird dessen Ablaufdatum auf
    `jetzt + Aufbewahrungszeit` verlängert und die Herkunft auf `auto`
    gestellt — abgelaufene Kandidaten erreichen den Bereinigungslauf dann
    gar nicht erst.
13. Ein so verlängerter Eintrag wird bei jedem weiteren Lauf erneut
    geprüft; fällt der Kandidatenbezug weg und die (erneute) Frist läuft
    ab, greift die normale Löschung.
14. Die Kandidaten-Prüfung muss ohne Serververbindung funktionieren — auf
    Basis der gecachten Weiterschauen-Liste, der gecachten Serienstruktur
    (`ShowDetailsCache`) und des im Manifest gespeicherten Playlist-Bezugs.

**Schutz während der Wiedergabe**

15. Wird ein heruntergeladenes Video abgespielt (lokale Datei als
    Wiedergabequelle), wird beim Wiedergabestart geprüft, ob das Ablaufdatum
    innerhalb der doppelten (Rest-)Wiedergabedauer erreicht würde
    (`ExpiresAtUtc < jetzt + 2 × Restdauer`). Falls ja, wird das Ablaufdatum
    auf `jetzt + 2 × Restdauer` hochgesetzt, sodass während des Abspielens
    kein Löschversuch erfolgt.

**Altbestand und Allgemeines**

16. Bestehende Manifest-Einträge ohne Ablaufdatum (Altbestand vor der
    Migration) gelten als **abgelaufen** (`ExpiresAtUtc` in der
    Vergangenheit bzw. bei der Prüfung wie abgelaufen behandelt) und werden
    bei der nächsten Bereinigung den regulären Regeln unterworfen: noch
    vorhandene Auto-Download-Kandidaten werden verlängert (Herkunft `auto`),
    alle übrigen entfernt. Herkunft des Altbestands: `manual`.
17. Alle Bereinigungs- und Verlängerungsfunktionen arbeiten offline
    (lokale Manifest-, Cache- und Dateidaten).
18. Die Downloads-Seite kann das Ablaufdatum je Eintrag anzeigen
    (z. B. „läuft ab am …") — optionaler Mehrwert, im Umsetzungsplan zu
    entscheiden.

## Betroffene Klassen und Komponenten

- `DownloadManifestEntry` (+ `ExpiresAtUtc`, + Herkunft `manual`/`auto`);
  Schema-Migration der Cache-Datenbank; Altbestand ohne Ablaufdatum gilt als
  abgelaufen und als `manual`
- `AppSettings` (+ Aufbewahrungszeit, + „Nach dem Abspielen löschen",
  + Löschverzögerung; Migration von `AutoDeleteWatchedEpisodes`; geänderte
  Werte wirken nur auf neue Downloads/Ablaufdaten)
- `DownloadService` (`EnqueueAsync` um Herkunftsparameter; Setzen von
  `ExpiresAtUtc` beim Übergang nach `completed`; Erweiterung von
  `ReconcileAsync` bzw. eigener Bereinigungslauf mit Ablauf-Prüfung,
  Kandidaten-Verlängerung und Löschung; Methode zum Verlängern des
  Ablaufdatums für Wiedergabe-Schutz und „angesehen"-Verkürzung)
- `AutoDownloadService` (Einreihung mit Herkunft `auto`; Verlängerung
  bestehender Kandidaten beim Lauf; Bereitstellung/Wiederverwendung der
  offline-fähigen Kandidaten-Ermittlung für den Bereinigungslauf)
- `AppUpdate` (Angesehen-Schwelle: Ablaufdatum verkürzen statt
  `DeleteWatchedDownloadEffect` sofort zu löschen; Wiedergabestart eines
  Downloads: Ablaufdatum-Schutzprüfung)
- `PlaybackSourceService` (liefert den lokalen-Pfad-Fall — Triggerspunkt
  der Schutzprüfung aus Kriterium 15)
- `AppEffect`/`AppMessage` (neue Effekte, z. B. für Ablauf-Verkürzung und
  Wiedergabe-Schutz), `ShellAppEffectHandler`
- `SettingsPage` (drei neue Einstellungen im Download-Abschnitt)
- `DownloadsPage`/`DownloadListItem`/`DownloadItemViewModel` (Herkunfts-
  anzeige „Cache", optional Ablaufdatum)
- Tests: `DownloadServiceTests`/`DownloadServiceReconcileTests`
  (Ablaufdatum, Bereinigung, Verlängerung, Herkunftswechsel,
  Wiedergabe-Schutz, Altbestand), `AutoDownloadServiceTests` (Herkunft,
  Kandidaten-Ermittlung, Verlängerung beim Lauf), `AppUpdateTests`
  (Angesehen-Verkürzung), `SettingsPage`-/`DownloadsPage`-Contract-Tests

## Nicht-Ziele

- Keine Änderung an der Speicherkontingent-Mechanik (Quota bleibt
  wie gehabt, keine Verdrängung bestehender Downloads bei Volllauf).
- Kein manuelles „Pinnen"/„Aufbewahren"-Kommando je Eintrag (vorerst).
- Keine getrennten Aufbewahrungszeiten je Herkunft.
- Keine serverseitige Ablaufverwaltung; keine Änderung am
  iOS-Hintergrund-Transfer.

## Offene Fragen

- Standardwerte: Aufbewahrungszeit 7 Tage, Löschverzögerung 15 Minuten
  (vorgegeben), „Nach dem Abspielen löschen" aus — im Plan zu bestätigen.
- „Restdauer" bei Kriterium 15: verbleibende Wiedergabedauer
  (Gesamtdauer − Startposition); liegt keine Dauer vor, entfällt die
  Hochsetzung (kein künstliches Verlängern ohne Wiedergabekontext).
- Ob die Verlängerung zusätzlich beim Auto-Download-Lauf (Kriterium 12)
  oder nur im Bereinigungslauf erfolgt, ist eine Umsetzungsentscheidung —
  fachlich gleichwertig, beide Trigger sind zulässig.
