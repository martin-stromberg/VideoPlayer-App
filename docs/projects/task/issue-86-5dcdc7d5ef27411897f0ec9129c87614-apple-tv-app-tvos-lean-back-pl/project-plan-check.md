# Projektplan-Gegenprüfung

## Ergebnis

**Status:** Projektplan vollständig

Begründung: Beide Befunde der ersten Prüfrunde (`project-plan-check.1.md`) sind behoben. Schritte 4 und 5 verlangen nun nur noch ein „vorbereitetes Navigationsziel" mit explizit akzeptablem Platzhalter-Zielzustand — die implizite Vorwärtsabhängigkeit 4/5 → 6 ist aufgelöst, ohne dass ein Zyklus entsteht. Zusätzlich wurde `VIDEOPLAYER_PROFILE` in Schritt 2 namentlich genannt. Alle Muss-Anforderungen sind mindestens einem Schritt zugeordnet, alle Nicht-Ziele werden eingehalten, und die deklarierten Abhängigkeiten sind auflösbar und zyklenfrei.

## Abgleich Anforderung ↔ Entwicklungsschritte

| Anforderungspunkt | Abgedeckt durch Schritt(e) | Status |
|--------------------|-----------------------------|--------|
| Serververbindung: Servererkennung/-wechsel, Pairing-Code-Flow (Code-Eingabe am TV, geklärt), Anmeldung, Geräte-Token, Offline-Erkennung | Schritt 3 | Abgedeckt |
| Startseite: Weiterschauen-, „Neu im Programm"- und Favoriten-Karussells, Cache-First, saisonale Genres, Offline-Darstellung | Schritt 4 | Abgedeckt (Navigationsziel jetzt als vorbereiteter/Platzhalter-Zielzustand formuliert — Vorwärtsabhängigkeit behoben) |
| Browsing und Suche: Quellen-/Genre-Browsing, Paging, Suche per Diktat/Continuity-Tastatur | Schritt 5 | Abgedeckt (Akzeptanzkriterium jetzt „vorbereitetes Navigationsziel" — Vorwärtsabhängigkeit behoben) |
| Detailseiten: Film, Sammlung, Serie, Staffel, Episode inkl. Offline-Snapshot, Favoriten-Markierung | Schritt 6 | Abgedeckt |
| Wiedergabe: Fortschritts-Tracking (5-s-Takt, Offline-Warteschlange), „angesehen"-Schwelle (bestätigter Fenstereintritt), Toleranz-Ende-Ereignis, Auto-Fortsetzung Serie + Playlist, Fortsetzungsposition, lokale-Datei-Bevorzugung, Wiedergabeschutz für Downloads, Spur-/Untertitelwahl | Schritt 7 | Abgedeckt (Spurwahl konditional: nur wenn der Stream wählbare Spuren anbietet, sonst entfällt sie — fachlich vertretbare Klärung, siehe Hinweise) |
| Playlists: nur lesender Zugriff (Übersicht eigen/öffentlich, Detail mit Paging, Abspielen ab Eintrag mit Playlist-Kontext) — keine Anlage/Bearbeitung | Schritt 8 | Abgedeckt, Verwaltung explizit ausgeschlossen und nicht verdrahtet |
| Manuelle Downloads: Anstoß über Detailseite, schlanke Liste (Status, Fortschritt, Abbrechen, Wiederholen, Löschen), Ablaufdatum via einstellbarer Aufbewahrungszeit, Quota-Obergrenze | Schritt 9 | Abgedeckt |
| Downloads: Bereinigung bei App-Start **und** App-Aktivierung (Suspension-Problem) | Schritt 9 (neue Aktivierungs-Message im plattformneutralen Update-/Effekt-Fluss, Dispatch aus tvOS-Lebenszyklus, plus `ResumeInterruptedAsync`-Wiederanschluss) | Abgedeckt |
| Downloads: Reconcile-Pflicht (`missing_file`, Teildateien, Waisen; Aufbewahrungszeit als Obergrenze) | Schritt 9 | Abgedeckt |
| Downloads: suspensionstaugliche `NSUrlSession`-Hintergrundkonfiguration (MVP-Bestandteil, Stakeholder-geklärt) | Schritt 9 (tvOS-Pendant `TvosBackgroundDownloadBackend` zu `IosBackgroundDownloadBackend`, CompletionSink, `handleEventsForBackgroundURLSession`) | Abgedeckt |
| Schlanker Einstellungsbereich (Aufbewahrungszeit, Quota, Auto-Resume, Endfenster o. ä.) | Schritt 10 | Abgedeckt |
| Nicht-Ziel: Keine Metadaten-Bearbeitung / kein Schreibzugriff auf Inhalte | Schritte 2, 4, 5, 6, 7, 8, 9 (jeweils explizit) | Eingehalten |
| Nicht-Ziel: Keine Playlist-Verwaltung | Schritte 2 und 8 (explizit nicht verdrahtet) | Eingehalten |
| Nicht-Ziel: Kein Auto-Download | Vorgehensentscheidung + Schritte 2, 9, 10 (Dienst nicht verdrahtet, keine „Cache"-Herkunft) | Eingehalten |
| Nicht-Ziel: Kein Protokollversand | Schritte 2, 10 (lokale Protokolldatei bleibt möglich) | Eingehalten |
| Neue tvOS-App (`net10.0-tvos`), Referenz nur auf plattformneutrale Projekte, leere Starthülle | Schritt 1 | Abgedeckt |
| MVU-Wiederverwendung, tvOS-`IAppEffectHandler`, Lebenszyklus-Verdrahtung | Schritt 2 | Abgedeckt |
| tvOS-Plattform-Adapter: `ISettingsStore`, `ISecretStore` (Keychain), Ablage-Verzeichnisse, `INetworkStatusMonitor`, Server-Adressquelle inkl. Entwicklungs-Fallbacks (`VIDEOPLAYER_SERVER_URL`, `VIDEOPLAYER_PROFILE` — jetzt namentlich) | Schritt 2 | Abgedeckt |
| Nativer Player (AVPlayer-Stack) als Ereignis-Übersetzer; Logik bleibt plattformneutral | Schritt 7 | Abgedeckt |
| Aktivierungs-Hook (Vordergrund-Eintritt) für Download-Abgleich | Schritt 9 | Abgedeckt |
| Abhängigkeits-/Kompatibilitätsprüfung: `net10.0-tvos`-Workload-Pinning (analog iOS-Rollback), NuGet-Kompatibilität SQLite-Bindung und mDNS-Bibliothek inkl. Ausweichvorschlag | Schritt 1 | Abgedeckt |
| Konfiguration: MVP-Einstellungs-Teilmenge, entfallene Werte (`AutoDownloadEnabled`, „nach Abspielen löschen", `WifiOnlyDownloads`, Protokollversand) nicht exponiert | Schritt 10 | Abgedeckt |
| Konfiguration: Entwicklungs-Fallbacks `VIDEOPLAYER_SERVER_URL`/`VIDEOPLAYER_PROFILE` | Schritt 2 (namentlich genannt — Hinweis aus Runde 1 behoben) | Abgedeckt |
| Konfiguration: tvOS-CI-Secrets analog iOS (Signaturidentität, Provisioning-Profil, API-Key) | Schritt 11 | Abgedeckt |
| Deployment: CI-Job baut/signiert tvOS-App für den Release-Tag, Release-Kandidat automatisiert nach App Store Connect, Gate-Ansatz für fehlende Apple-Account-Artefakte (manueller Vorlauf bestätigt), lokales Hilfsskript | Schritt 11 | Abgedeckt |
| Tests: bestehende plattformneutrale Suite bleibt lauffähig; neue plattformneutrale Message testbar | Schritte 1, 2, 9 | Abgedeckt |

## Abhängigkeitsprüfung

| Prüfpunkt | Befund |
|-----------|--------|
| Auflösbarkeit der deklarierten Abhängigkeiten | Alle Abhängigkeiten verweisen auf kleinere Schrittnummern (1–11), keine deklarierten Zyklen |
| Schritt 2 → 1 | Konsistent (MVU-Verdrahtung braucht Projektgerüst) |
| Schritt 3 → 2 | Konsistent (Pairing/Anmeldung braucht Plattform-Adapter und Effekt-Handler) |
| Schritt 4 → 3 | **Behoben:** „vorbereitetes Navigationsziel … Platzhalter-Zielzustand akzeptabel" — keine implizite Abhängigkeit auf Schritt 6 mehr |
| Schritt 5 → 3 | **Behoben:** Akzeptanzkriterium fordert jetzt „vorbereitetes Navigationsziel" statt fertiger Detailansicht — keine implizite Abhängigkeit auf Schritt 6 mehr |
| Schritt 6 → 4, 5 | Konsistent (Einstiegspfade aus Startseite und Browsing vorhanden); die vorbereiteten Navigationsziele aus 4/5 werden hier funktional — kein Zyklus, da 4/5 kein funktionales Detailergebnis mehr verlangen |
| Schritt 7 → 6 | Konsistent (Abspielen-Einstieg über Detailseite; Player braucht Medienkontext) |
| Schritt 8 → 7 | Konsistent (Playlist-Wiedergabe nutzt den Player-Schritt) |
| Schritt 9 → 6, 7 | Konsistent (Download-Anstoß über Detailseite; Zusammenspiel lokale Quelle/Wiedergabeschutz mit Player) |
| Schritt 10 → 7, 9 | Konsistent (Auto-Resume/Endfenster wirken auf Wiedergabe; Aufbewahrung/Quota auf Downloads) |
| Schritt 11 → 1 | Konsistent (Release-Job braucht tvOS-Projekt und macOS-CI-Grundlage) |
| Implizite Vorwärtsverweise in anderen Schritten | Schritt 6 nennt Abspielen/Download als „wird mit dem Wiedergabe-/Download-Schritt funktional" bzw. „ausgegraute/vorbereitete Aktion" — korrekt als Vorbereitung deklariert, kein fachlicher Anspruch zum Schrittzeitpunkt |

## Hinweise

- Die Spur-/Untertitelwahl bleibt in Schritt 7 konditional formuliert (nur wenn der Server-Stream wählbare Spuren anbietet, z. B. HLS; sonst entfällt die Auswahl ersatzlos). Das ist eine plausible Auflösung der früheren offenen Frage; bei der Nachplanung ist sicherzustellen, dass diese Einschränkung als Erfüllung der Anforderung akzeptiert ist.
- Das Muster „vorbereitetes Navigationsziel / Platzhalter-Zielzustand" in den Schritten 4, 5 und 6 ist als Zwischenzustand konsistent; bei der /lifecycle-Übergabe ist darauf zu achten, dass die Platzhalter in den Folgeschritten (6 für Detailansichten, 7 für Wiedergabe, 9 für Downloads) tatsächlich an die funktionalen Ziele angeschlossen werden — die Schrittbeschreibungen legen dies nahe, formulieren das Verdrahten der Platzhalter aber nicht als eigenes Akzeptanzkriterium.
- Top Shelf (im Issue nur „ggf.") hat keinen eigenen Schritt — Stakeholder-geklärt nicht im MVP.
- Die Prüfung der `msTools.DeveloperApi`-Wiederverwendung unter tvOS ist kein eigener Schritt; die Validierungsstrategie (macOS-CI + plattformneutrale Testsuite) deckt die Lauffähigkeit pauschal ab.
- Schritt 6 umfasst zusätzlich den Medientyp „Sammlung" über die Muss-Liste hinaus — als Obermenge unkritisch.
- Kein Schritt fordert Schreibzugriffe auf Inhalte, Playlist-Verwaltung, Auto-Download oder Protokollversand — die Nicht-Ziele sind durchgängig und explizit gewahrt.
