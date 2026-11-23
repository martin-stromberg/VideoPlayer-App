# Projektplan-Gegenprüfung

## Ergebnis

**Status:** Projektplan lückenhaft

Begründung: Fachlich ist der Funktionsumfang vollständig abgedeckt und alle Nicht-Ziele werden eingehalten. Der Plan enthält jedoch eine inkonsistente Reihenfolge: Die Schritte 4 (Startseite) und 5 (Browsing/Suche) nutzen die Detailansicht als Navigationsziel — in Schritt 5 sogar als explizites Akzeptanzkriterium —, obwohl die Detailansichten erst in Schritt 6 entstehen und keiner der beiden Schritte eine Abhängigkeit auf 6 deklariert. Eine nachträgliche Deklaration dieser Abhängigkeit würde wegen der bestehenden Abhängigkeit 6 → 4, 5 einen Zyklus erzeugen.

## Abgleich Anforderung ↔ Entwicklungsschritte

| Anforderungspunkt | Abgedeckt durch Schritt(e) | Status |
|--------------------|-----------------------------|--------|
| Serververbindung: Servererkennung/-wechsel, Pairing-Code-Flow (Code-Eingabe am TV, geklärt), Anmeldung, Geräte-Token, Offline-Erkennung | Schritt 3 | Abgedeckt |
| Startseite: Weiterschauen-, „Neu im Programm"- und Favoriten-Karussells, Cache-First, saisonale Genres, Offline-Darstellung | Schritt 4 | Abgedeckt (siehe Abhängigkeitsbefund: verweist auf Detailansicht aus Schritt 6) |
| Browsing und Suche: Quellen-/Genre-Browsing, Paging, Suche per Diktat/Continuity-Tastatur | Schritt 5 | Abgedeckt (siehe Abhängigkeitsbefund: Akzeptanzkriterium setzt Detailansicht aus Schritt 6 voraus) |
| Detailseiten: Film, Sammlung, Serie, Staffel, Episode inkl. Offline-Snapshot, Favoriten-Markierung | Schritt 6 | Abgedeckt |
| Wiedergabe: Fortschritts-Tracking (5-s-Takt, Offline-Warteschlange), „angesehen"-Schwelle (bestätigter Fenstereintritt), Toleranz-Ende-Ereignis, Auto-Fortsetzung Serie + Playlist, Fortsetzungsposition, lokale-Datei-Bevorzugung, Spur-/Untertitelwahl | Schritt 7 | Abgedeckt (Spurwahl konditional: nur wenn der Stream wählbare Spuren anbietet, sonst entfällt sie — fachlich vertretbare Klärung, siehe Hinweise) |
| Playlists: nur lesender Zugriff (Übersicht eigen/öffentlich, Detail mit Paging, Abspielen ab Eintrag mit Playlist-Kontext) — keine Anlage/Bearbeitung | Schritt 8 | Abgedeckt, Verwaltung explizit ausgeschlossen und nicht verdrahtet |
| Manuelle Downloads: Anstoß über Detailseite, schlanke Liste (Status, Abbrechen, Wiederholen, Löschen), Ablaufdatum via einstellbarer Aufbewahrungszeit, Quota-Obergrenze | Schritt 9 | Abgedeckt |
| Downloads: Bereinigung bei App-Start **und** App-Aktivierung (Suspension-Problem) | Schritt 9 (neue Aktivierungs-Message im plattformneutralen Update-/Effekt-Fluss, Dispatch aus tvOS-Lebenszyklus) | Abgedeckt |
| Downloads: Reconcile-Pflicht (`missing_file`, Teildateien, Waisen; Aufbewahrungszeit als Obergrenze) | Schritt 9 | Abgedeckt |
| Downloads: suspensionstaugliche `NSUrlSession`-Hintergrundkonfiguration (MVP-Bestandteil, Stakeholder-geklärt) | Schritt 9 (tvOS-Pendant zu `IosBackgroundDownloadBackend`, CompletionSink, App-Delegate-Events) | Abgedeckt |
| Schlanker Einstellungsbereich (Aufbewahrungszeit, Quota, Auto-Resume, Endfenster o. ä.) | Schritt 10 | Abgedeckt |
| Nicht-Ziel: Keine Metadaten-Bearbeitung / kein Schreibzugriff auf Inhalte | Schritte 2, 4, 5, 6, 7, 8, 9 (jeweils explizit) | Eingehalten |
| Nicht-Ziel: Keine Playlist-Verwaltung | Schritte 2 und 8 (explizit nicht verdrahtet) | Eingehalten |
| Nicht-Ziel: Kein Auto-Download | Vorgehensentscheidung + Schritte 2, 9, 10 (Dienst nicht verdrahtet, keine „Cache"-Herkunft) | Eingehalten |
| Nicht-Ziel: Kein Protokollversand | Schritte 2, 10 (lokale Protokolldatei bleibt möglich) | Eingehalten |
| Neue tvOS-App (`net10.0-tvos`), Referenz nur auf plattformneutrale Projekte, leere Starthülle | Schritt 1 | Abgedeckt |
| MVU-Wiederverwendung, tvOS-`IAppEffectHandler`, Lebenszyklus-Verdrahtung | Schritt 2 | Abgedeckt |
| tvOS-Plattform-Adapter: `ISettingsStore`, `ISecretStore` (Keychain), Ablage-Verzeichnisse, `INetworkStatusMonitor`, Server-Adressquelle inkl. Entwicklungs-Fallbacks | Schritt 2 | Abgedeckt |
| Nativer Player (AVPlayer-Stack) als Ereignis-Übersetzer; Logik bleibt plattformneutral | Schritt 7 | Abgedeckt |
| Aktivierungs-Hook (Vordergrund-Eintritt) für Download-Abgleich | Schritt 9 | Abgedeckt |
| Abhängigkeits-/Kompatibilitätsprüfung: `net10.0-tvos`-Workload-Pinning (analog iOS-Rollback), NuGet-Kompatibilität SQLite-Bindung und mDNS-Bibliothek inkl. Ausweichvorschlag | Schritt 1 | Abgedeckt |
| Konfiguration: MVP-Einstellungs-Teilmenge, entfallene Werte (`AutoDownloadEnabled`, „nach Abspielen löschen", `WifiOnlyDownloads`, Protokollversand) nicht exponiert | Schritt 10 | Abgedeckt |
| Konfiguration: Entwicklungs-Fallbacks `VIDEOPLAYER_SERVER_URL`/`VIDEOPLAYER_PROFILE` | Schritt 2 („bestehende Entwicklungs-Fallback-Konventionen") | Abgedeckt (implizit, siehe Hinweise) |
| Konfiguration: tvOS-CI-Secrets analog iOS (Signaturidentität, Provisioning-Profil, API-Key) | Schritt 11 | Abgedeckt |
| Deployment: CI-Job baut/signiert tvOS-App für den Release-Tag, Release-Kandidat automatisiert nach App Store Connect, Gate-Ansatz für fehlende Apple-Account-Artefakte (manueller Vorlauf bestätigt), lokales Hilfsskript | Schritt 11 | Abgedeckt |
| Tests: bestehende plattformneutrale Suite bleibt lauffähig; neue plattformneutrale Message testbar | Schritte 1, 2, 9 | Abgedeckt |

## Abhängigkeitsprüfung

| Prüfpunkt | Befund |
|-----------|--------|
| Auflösbarkeit der deklarierten Abhängigkeiten | Alle Abhängigkeiten verweisen auf kleinere Schrittnummern (1–11), keine deklarierten Zyklen |
| Schritt 2 → 1 | Konsistent (MVU-Verdrahtung braucht Projektgerüst) |
| Schritt 3 → 2 | Konsistent (Pairing/Anmeldung braucht Plattform-Adapter und Effekt-Handler) |
| Schritt 4 → 3 | **Inkonsistent:** Beschreibung „Einträge … führen zur jeweiligen Detailansicht" nutzt die Detailansicht aus Schritt 6 implizit mit; Abhängigkeit auf 6 fehlt. Abgemildert dadurch, dass es nur Beschreibungstext ist, kein Akzeptanzkriterium |
| Schritt 5 → 3 | **Inkonsistent:** Akzeptanzkriterium „Auswahl eines Eintrags öffnet dessen Detailansicht" ist zum Schrittzeitpunkt nicht erfüllbar — die Detailansicht entsteht erst in Schritt 6; Abhängigkeit auf 6 fehlt |
| Schritt 6 → 4, 5 | Deklariert konsistent (Einstiegspfade vorhanden), steht aber in Spannung zu den impliziten Rück-Abhängigkeiten 4/5 → 6: Bei nachträglicher Deklaration entstünde ein Zyklus 4/5 ↔ 6 |
| Schritt 7 → 6 | Konsistent (Abspielen-Einstieg über Detailseite; Player braucht Medienkontext) |
| Schritt 8 → 7 | Konsistent (Playlist-Wiedergabe nutzt den Player-Schritt) |
| Schritt 9 → 6, 7 | Konsistent (Download-Anstoß über Detailseite; Zusammenspiel lokale Quelle/Wiedergabeschutz mit Player) |
| Schritt 10 → 7, 9 | Konsistent (Auto-Resume/Endfenster wirken auf Wiedergabe; Aufbewahrung/Quota auf Downloads) |
| Schritt 11 → 1 | Konsistent (Release-Job braucht tvOS-Projekt und macOS-CI-Grundlage) |

## Fehlende oder unvollständige Punkte

- [ ] Schritt 5 (Browsing/Suche) fordert im Akzeptanzkriterium „Auswahl eines Eintrags öffnet dessen Detailansicht", obwohl die Detailansichten erst in Schritt 6 entstehen. Implizite Abhängigkeit 5 → 6 ist nicht deklariert; eine Deklaration würde mit der bestehenden Abhängigkeit 6 → 5 einen Zyklus erzeugen. Der Plan muss die Inkonsistenz auflösen (z. B. Akzeptanzkriterium auf ein vorbereitetes Navigationsziel reduzieren — analog der Formulierung „wird mit dem Download-Schritt funktional" in Schritt 6 — oder die Reihenfolge der Detailansichten vorziehen).
- [ ] Schritt 4 (Startseite) beschreibt „Einträge … führen zur jeweiligen Detailansicht", obwohl die Detailansicht erst in Schritt 6 entsteht. Gleiche implizite Abhängigkeit 4 → 6; nicht als Akzeptanzkriterium formuliert, sollte aber zur Klarstellung mitbehandelt werden.

## Hinweise

- Die Spur-/Untertitelwahl ist in Schritt 7 konditional formuliert (nur wenn der Server-Stream wählbare Spuren anbietet, z. B. HLS; sonst entfällt die Auswahl ersatzlos). Das ist eine plausible Auflösung der früheren offenen Frage, bei der Nachplanung ist jedoch sicherzustellen, dass diese Einschränkung als Erfüllung der Anforderung akzeptiert ist.
- `VIDEOPLAYER_PROFILE` ist in Schritt 2 nur implizit über „bestehende Entwicklungs-Fallback-Konventionen" abgedeckt; für die Weitergabe an /lifecycle wäre eine namentliche Nennung präziser.
- Top Shelf (im Issue nur „ggf.") hat keinen eigenen Schritt — keine Muss-Anforderung, offenbar bewusst aus dem MVP ausgeschlossen.
- Die Prüfung der `msTools.DeveloperApi`-Wiederverwendung unter tvOS ist kein eigener Schritt; die Validierungsstrategie (macOS-CI + plattformneutrale Testsuite) deckt die Lauffähigkeit pauschal ab.
- Kein Schritt fordert Schreibzugriffe auf Inhalte, Playlist-Verwaltung, Auto-Download oder Protokollversand — die Nicht-Ziele sind durchgängig und explizit gewahrt.
