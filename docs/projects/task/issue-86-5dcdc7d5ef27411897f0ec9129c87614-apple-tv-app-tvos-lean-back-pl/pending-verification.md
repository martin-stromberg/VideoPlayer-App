# Ausstehende externe Verifikationen

Register aller Anforderungspunkte, deren Nachweis in der aktuellen Umgebung (Windows-Entwicklungsrechner) nicht geführt werden kann. Eintrag bleibt bis zum erbrachten Nachweis bestehen; Projektabschluss erfordert leeres Register oder dokumentiertes Sign-off.

| Schritt | Kriterium | Benötigte Voraussetzung | Stand |
|---------|-----------|--------------------------|-------|
| ~~1–laufend~~ | ~~`net10.0-tvos` kompiliert auf macOS; CI-Job `build-tvos` läuft erfolgreich~~ | Push → GitHub-macos-Runner | **ERLEDIGT:** `build-tvos` grün seit Run 42111278228; läuft bei jedem Push auf `task/issue-86-**` mit (zuletzt grün auf dem finalen Stand) |
| 2–laufend | App startet auf tvOS-Simulator; UI/Focus-Navigation funktioniert tatsächlich (Runtime-Verhalten jenseits von Kompilierung) | tvOS-Simulator/-Gerät (macOS) oder CI-Testlauf | **Ausstehend** — Kompiliernachweis vorhanden, Laufzeitverhalten ungeprüft |
| 11 | Release-Kandidat landet automatisiert in App Store Connect | Apple-Account-Artefakte (Bundle-ID, Provisioning-Profil, Distribution-Zertifikat, ASC-API-Key — manueller Vorlauf Kontoinhaber) + `TVOS_SIGNING_ENABLED=true` + Secrets | **Ausstehend** — Pipeline ist gebaut und gated; Upload im Ist-Betrieb erst nach Vorlauf nachweisbar |
| 11 | App-Icon- und Top-Shelf-Assets vorhanden (Store-Pflicht) | Grafik-Assets + `Assets.xcassets` im tvOS-Projekt | **Ausstehend** — aktuell kein Asset-Katalog; in `docs/help/tv-app/deployment.md` als Vorlauf dokumentiert |
