# Plan-Review — Schritt 10: Schlanker Einstellungsbereich (tvOS)

Status: **Vollständig umgesetzt**

Geprüft: `plan.md` gegen die Implementierung in `VideoPlayer.TvOS/Ui/SettingsPageViewController.cs`.

## Checkliste

| Planelement | Umsetzung | Status |
|-------------|-----------|--------|
| 1.1 Render-Robustheit (`settings`-Feld, `lastModel`/`IsViewLoaded`-Guard, `ViewDidAppear` → `LoadSettingsRequestedMessage`) | Z. 45–46, 135–138, 142–148, 151–160 | ✅ |
| 1.2 Abschnitt „Downloads" — `DownloadRetentionDays` + `DownloadQuotaGb` als Wert-Buttons mit `UIAlertController`-Werteliste {1,3,7,14,30,60,90,180,365} / {1,2,5,10,25,50,100,250,500} | Z. 30–31, 79–98 | ✅ |
| `ShowValuePicker`-Hilfsmethode inkl. Ist-Wert-Angebot bei Abweichung | Z. 207–235 | ✅ |
| 1.3 Abschnitt „Wiedergabe" — `AutoResumeEnabled` Ein/Aus-Button + `WatchedEndWindowSeconds`-Werteliste {30–3600} | Z. 32, 100–115 | ✅ |
| 1.4 Abschnitt „Konto" — `LoginUsername`-Anzeige + „Abmelden" über `LogoutRequestedMessage` mit Enabled-Guard | Z. 69–77, 166–173 | ✅ |
| 1.5 Abschnitt „Server" unverändert (Adresse, Status, „Server wechseln") | Z. 117–126, 188–195 (plus Offline-Text wie Profilseite) | ✅ |
| 1.6 Layout — `TvosControls.Section`-Abschnitte, ausgeschlossene Einstellungen nicht gerendert | ViewDidLoad Z. 57–139 | ✅ |
| 1.7 tvOS-Kompilierfallen — bewiesene `UIAlertController`/`UIAlertAction`-Signaturen, kein `const nfloat`, kein `SeparatorStyle`, `UIAction`-Parameter heißt `action` | gesamte Datei | ✅ |
| 2. Tests — keine neuen plattformneutralen Bausteine nötig | nur `VideoPlayer.TvOS` geändert | ✅ |
| 3. Dokumentation (`beschreibung.md`, `ablauf-anwender.md`, `README.md`, `index.md`) | ausstehend — Schritt 9 des Lifecycles | ⏳ |
| 4. Push + `build-tvos` grün | ausstehend — Abschluss | ⏳ |

## Akzeptanzkriterien (aus requirement.md)

- ✅ Alle vier Pflicht-Einstellungen sichtbar, per Fernbedienung änderbar (fokussierbare Buttons/Auswahllisten) und persistent (`PersistSettingsEffect` → `NSUserDefaultsSettingsStore`)
- ✅ Änderungen wirken: `model.Settings` fließt in Download-Effekte (`LoadDownloadsEffect`, `EnqueueDownloadEffect`, `ResumeDownloadsEffect` u. a.) und die Wiedergabe-Update-Pfade (`AutoResumeEnabled`, `WatchedEndWindowSeconds`)
- ✅ Ausgeschlossene Einstellungen erscheinen nicht (kein Auto-Download/Vorlauf, kein nach-Abspielen-löschen, kein Nur-WLAN, kein Protokollversand/Logging)
- ✅ Normalisierung greift (`UpdateSaveSettingsRequested` → `Normalize()`); Vorgabewerte innerhalb der Schranken
- ⏳ tvOS-Lauffähigkeit über `build-tvos` (CI nach Push)

## Offene Aufgaben

Keine — Implementierung entspricht dem Plan; Restpunkte (Dokumentation, CI-Nachweis) sind reguläre Lifecycle-Folgeschritte.
