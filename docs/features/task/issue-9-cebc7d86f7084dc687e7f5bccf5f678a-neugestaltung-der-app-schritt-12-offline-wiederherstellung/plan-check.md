# Plan-Prüfung — Schritt 12: Offlinebetrieb und Wiederherstellung

## Abdeckung der Anforderungen

| Anforderung | Planabschnitt | Status |
|---|---|---|
| Startseite offline aus Cache befüllt | C (OfflineContinueWatchingService + gecachte Favoriten/Recent) | ✔ |
| „Weiterschauen" ausschließlich aus lokalen Downloads (completed + Datei) | C1 | ✔ |
| Fehlende Episoden als Platzhalter (nicht abspielbar, markiert) | A+B (Serienkontext), C1/C6 (Platzhalter + UI) | ✔ |
| Favoriten offline über lokale Kopie | C1 (cached_favorites) | ✔ |
| Keine neuen Inhalte / keine Quellenliste / kein Profilabruf offline | C (nur Cache), G1/G2 (Profil), Quellenbanner bereits vorhanden | ✔ |
| Heruntergeladene Videos abspielbar, lokale Datei bevorzugt | D (Offline-Detailpfad) + bestehender PlaybackSourceService | ✔ |
| Restore: Einstellungen/bekannter Server überleben, Geheimnisse fehlen | E (vorhandener Ablauf, `SecureStorage` nicht backup-pflichtig) | ✔ |
| Restore: automatischer Verbindungsversuch zum letzten Server | vorhanden (`UpdateServersLoaded` → `CheckServerHealthEffect`) | ✔ |
| Erreichbar: gültiges Token → direkter Einstieg; sonst Anmeldemaske | E1 (Token-Wiederverwendung), E2 (Wiederanmelde-Hinweis) | ✔ |
| Nicht erreichbar: Offline-Modus bei Daten, sonst Serverauswahl | vorhanden (`UpdateOfflineDataCheckCompleted`) | ✔ |
| Vollständiger Sync + Fortschritte nachsenden + Weiterschauen aktualisieren | E3 (bestehender `RunFullSyncEffect`-Pfad) | ✔ |
| Beschädigte Caches verwerfen + neu synchronisieren | F2 + C7 (`DiscardedCaches` → `RunFullSyncEffect`) | ✔ |
| Nicht initialisierbare DB neu anlegen, alte Datei sichern, Nutzer informieren | F1/F3/F4/F5 | ✔ |
| Beschädigte Daten führen nicht zum Absturz | C1 (abgesicherte Teilschritte), F (Recovery-Pfade) | ✔ |

## Regel-Prüfung

- **MVU-Muster**: Änderungen laufen über Model/Message/Effect/Update + `ShellAppEffectHandler`; keine direkte UI-Logik im Kern. ✔
- **Testbelege im Plan**: Abschnitt H listet konkrete Testfälle je Anforderung. ✔
- **Usability-Review erforderlich**: UI-Änderungen vorhanden (Platzhalter-Karten, Banner, Profil-Offlinehinweis) → `review-usability.md` geplant. ✔
- **`RaiseUiActionRequested`-Handler**: kein neuer UI-Action-Mechanismus geplant; bestehende Dispatch-Pfade bleiben. ✔
- **Keine internen IDs als Nutzereingabe**: keine neuen Eingabemasken. ✔
- **XML-Doku**: Pflicht für neue Member eingeplant; Hook prüft beim Commit. ✔
- **Neue Enums**: keine — `EnumCoverageTests` unberührt. ✔
- **Hook-Konformität**: `dotnet format whitespace VideoPlayer.App.sln --no-restore` vor Commit eingeplant (Abschnitt J). ✔
- **Kein Branch-Wechsel/Merge**: bleibt auf dem Feature-Branch. ✔
- **Fremde Änderungen**: `.gitignore`-Änderung und `.agents/` werden nicht committet. ✔

## Risiken und Abwägungen

| Risiko | Abwägung |
|---|---|
| Manifest-Spalten auf bestehender DB | sqlite-net `CreateTableAsync` ergänzt fehlende Spalten per `ALTER TABLE` — bestehende Installationen werden migriert; Spalten sind nullable. |
| Lazy-Connection cached Ausnahmen | `InitializeWithRecoveryAsync` setzt das `Lazy` zurück (Feld wird nicht mehr `readonly`). |
| Resync-Loop bei verworfenen Caches | `DiscardedCaches` nur bei tatsächlich gelöschten Zeilen; fehlende Zeilen melden nichts. |
| Token-Validierung erzeugt Server-Last | ein einzelner `GET api/favorites` beim Start — akzeptabel, vermeidet Login-Maske bei gültigem Token. |
| Offline-Detailseite ohne gecachte Serie | Fallback auf Manifest-Titel; abspielbar bleibt der lokale Download (Play-Button nur bei `HasLocalFile`). |

## Ergebnis

Plan ist vollständig, umsetzbar und ohne offene Fragen. Freigabe zur Implementierung.
