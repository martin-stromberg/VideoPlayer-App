# Mobile-Implementierung – Übergabe

## Stand

Die Mobile-App verwendet `GET api/home/seasonal-genres` mit dem im isolierten Servercheckout implementierten Vertrag. `SeasonalGenre` enthält `genreId`, `name`, `expiresAt` und die vorhandenen polymorphen `MediaEntry`-Typen. Saisonregeln werden nicht clientseitig berechnet.

Saisonale Sektionen werden getrennt synchronisiert und im Cache `seasonal_genres` gespeichert. HTTP-/Transportfehler erhalten den Cache, erfolgreiches `[]` ersetzt alte Inhalte durch leer. Ein fehlender, ungültiger oder abgelaufener Ablaufzeitpunkt erlaubt keine Anzeige. Die Projektion erhält Reihenfolge, Typ, Titel, Medien-/Poster-ID; nur Film, Sammlung und Serie werden unterstützt.

MVU und HomePage zeigen dynamische Karussells mit derselben Detailnavigation wie Favoriten/Neuheiten. Die bestehenden drei Listen bleiben erhalten. Ein abbrechbarer Timer beobachtet die nächste Server-Ablaufgrenze (lange Intervalle werden nach spätestens einer Minute neu gegen die Uhr geprüft). Anzeigen der Seite und Window.Resume laden die Projektion erneut; Verlassen beendet den Seitentimer. OfflineContinueWatchingService übernimmt nur gültige saisonale Sektionen. Logout leert den saisonalen Modellzustand.

Die neue RefreshView verwendet dieselbe MVU-Voll-Synchronisation, inklusive neuer HomeRefreshRequested-/HomeListsReloadRequested-Nachrichten. Stabile AutomationIds sind SeasonalGenres und SeasonalGenre{genreId}. Alle sichtbaren neuen Überschriften sind Serverinhalt; neue statische UI-Texte wurden nicht eingeführt.

## Testinfrastruktur

Eine Dateiuhr ist ausschließlich in DEBUG und nur bei ausdrücklich gesetztem VIDEOPLAYER_TEST_CLOCK_FILE plus e2e-Profil aktiv. Release verwendet immer TimeProvider.System. Der Ablauftest steuert damit unmittelbar vor und exakt an der exklusiven Servergrenze und prüft das Verschwinden auf geöffneter Seite sowie bei einem ausdrücklich offline gestarteten Profil.

Die vorhandene E2E-Build-Fixture wurde für reproduzierbare Läufe korrigiert: --disable-build-servers verhindert weitervererbte offene Ausgabepipes; stdout/stderr und Prozessende werden gemeinsam unter aktivem Timeout gelesen. Vorher wurde das Timeout erst nach unbeschränktem ReadToEnd erzeugt. Der saisonale Setup-Helfer setzt die reale PasswordBox über die vorhandene Developer-API, weil UIA keinen lesbaren ValuePattern bietet und globale Fokusraces die Passworteingabe verloren gehen ließen. Produktive Loginhandler und Navigation bleiben aktiv.

## Validierung

- Windows-Debug-MAUI-Build erfolgreich, keine Fehler; bestehende NU1903-/DisplayAlert-Warnungen.
- Volle plattformneutrale Testsuite: 1396 bestanden, keine Fehler oder Skips, 50 s.
- Neue/angepasste gezielte Saison-/Sync-Tests: 17 bestanden; zusätzlicher Offline-Service-Regressionstest in voller Suite enthalten.
- Sechs saisonale Windows-E2E-Flüsse abschließend erfolgreich (6/6, 59,6 s), einschließlich ausdrücklich IsOffline+Home beim Ablauf-Neustart. Kein neuer Test übersprungen.

Keine Featurecommits erstellt. Backendlieferung und isolierter Servercheckout wurden nicht geändert. Reviews und die vollständige Windows-E2E-Suite bleiben für die nachfolgenden Lifecycle-Schritte.

