# Ausstehende externe Verifikationen

Register aller Anforderungspunkte, deren Nachweis in der aktuellen Umgebung (Windows-Entwicklungsrechner) nicht geführt werden kann. Eintrag bleibt bis zum erbrachten Nachweis bestehen; Projektabschluss erfordert leeres Register oder dokumentiertes Sign-off.

| Schritt | Kriterium | Benötigte Voraussetzung | Stand |
|---------|-----------|--------------------------|-------|
| ~~1–laufend~~ | ~~`net10.0-tvos` kompiliert auf macOS; CI-Job `build-tvos` läuft erfolgreich~~ | Push → GitHub-macos-Runner | **ERLEDIGT:** `build-tvos` grün seit Run 42111278228; läuft bei jedem Push auf `task/issue-86-**` mit (zuletzt grün auf dem finalen Stand) |
| 2–laufend | App startet auf tvOS-Simulator; UI/Focus-Navigation funktioniert tatsächlich (Runtime-Verhalten jenseits von Kompilierung) | tvOS-Simulator/-Gerät (macOS) oder CI-Testlauf | **Teilweise erbracht:** App startet und bleibt im tvOS-Simulator aktiv (PID-Check, kein Crash-Report); Screenshot zeigt die „Serververbindung"-UI (Launch 06.11.2026 18:17, Commit 76289ca, UDID 6F1CFB83-199D-42E6-8CA5-3623E2BC0A46). Vier Laufzeitdefekte wurden dabei gefunden und gefixt (base.OnActivated-Generierter-Fehler, fehlende lib.e_sqlite3-Managed-Assembly → provider.internal + Stub, Adhoc-Signatur aller Mach-O-Dateien). **Offen:** Fokus-Navigation und tiefergehende UI-Interaktion im laufenden Betrieb |
| 11 | Release-Kandidat landet automatisiert in App Store Connect | Apple-Account-Artefakte (Bundle-ID, Provisioning-Profil, Distribution-Zertifikat, ASC-API-Key — manueller Vorlauf Kontoinhaber) + `TVOS_SIGNING_ENABLED=true` + Secrets | **Ausstehend** — Pipeline ist gebaut und gated; Upload im Ist-Betrieb erst nach Vorlauf nachweisbar |
| 11 | App-Icon- und Top-Shelf-Assets vorhanden (Store-Pflicht) | Grafik-Assets + `Assets.xcassets` im tvOS-Projekt | **Ausstehend** — aktuell kein Asset-Katalog; in `docs/help/tv-app/deployment.md` als Vorlauf dokumentiert |
