# Übersetzte Anforderung: Startseite mit gecachten Medienlisten

## Fachliche Zusammenfassung

Die Startseite der MVU-App `VideoPlayer.Maui` (`AppPage.Home`, Route `home`)
zeigt drei horizontale Medienlisten: **„Weiterschauen"** (Server-Endpunkt
`api/continue-watching`, Cache-Schlüssel `continue_watching`), **„Favoriten"**
(`api/favorites`, Cache-Tabelle `cached_favorites`) und **„Neu im Programm"**
(`api/items/recent`, Cache-Schlüssel `recent`). Die Listen werden aus der
`CacheDatabase` (`VideoPlayer.Storage`) befüllt — die Startseite zeigt sofort
die gecachten Inhalte, ohne auf eine Serverantwort zu warten; die laufende
Synchronisation (`ServerSyncService`) und die verdrahteten Server-Ereignisse
(`ServerEventReceivedMessage` → Teil-Sync) aktualisieren die Caches, woraufhin
die Anzeige ohne manuelle Aktualisierung frisch wird. Die Auswahl eines
Listen-Eintrags identifiziert das Medium (Medientyp + Id) und navigiert zur
Detail-Route — die eigentliche Detailseite wird in Entwicklungsschritt 8
gefüllt; für diesen Schritt wird eine Platzhalter-Detailseite mit
registrierter Shell-Route angelegt. Aussehen und Bedienung folgen der
bestehenden Webanwendung (horizontale Listen mit Vorschaubildern).

## Betroffene Klassen und Komponenten

- **MVU-Kern (`VideoPlayer.Mvu`):**
  - `AppModel` — neue Eigenschaften für die drei Startseiten-Listen
    (anzeigefertige Einträge) und ggf. einen Ladezustand
  - `AppMessage` — neue Messages: Listen aus dem Cache laden anfordern,
    geladene Listen übernehmen, Listeneintrag ausgewählt; Rückmeldung nach
    Teil-Synchronisation, damit die Anzeige aktualisiert wird
  - `AppEffect` — neue Effekte: gecachte Listen lesen (Ergebnis-Message),
    Navigation zur Detail-Route
  - `AppUpdate` — Befüllung der Listen beim Start (Cache-First), nach
    Synchronisation und nach Ereignis-Teil-Sync; Auswahl → Detail-Navigation
  - Neuer plattformneutraler Dienst zum Lesen der gecachten Listen aus
    `CacheDatabase` (`cached_lists`-Schlüssel `recent`/`continue_watching`,
    `cached_favorites`) und Abbilden auf das Anzeige-Modell
- **App-Rand (`VideoPlayer.Maui`):**
  - `Pages/HomePage.xaml(.cs)` — Ausbau der Platzhalterseite: drei
    horizontale Listen (Carousels) mit Vorschaubildern nach dem Muster der
    Webanwendung (`MediaCarousel`), Auswahl per Tap
  - `Pages/MediaDetailsPage.xaml(.cs)` — neue Platzhalter-Detailseite
    (Route `mediadetails`, Query-Parameter Medientyp + Id; wird in Schritt 8
    gefüllt)
  - `AppShell.xaml.cs` — Registrierung der Detail-Route (kein Menüeintrag)
  - `ShellAppEffectHandler` — Ausführung der neuen Effekte (Cache-Lesen,
    Detail-Navigation); Rückdispatch nach Teil-Synchronisation
  - Vorschaubilder: Dienst zum Laden über `api/pictures/{id}` mit lokalem
    Datei-Cache (`AppStorageLayout.PreviewsDirectory`, nicht
    backup-pflichtig) — Bearer-Token erfordert Abruf über den angemeldeten
    `VideoPlayerApiClient`, kein direkter URL-`ImageSource`
  - `MauiProgram` — DI-Registrierungen der neuen Dienste/Seiten
- **Ablage (`VideoPlayer.Storage`):**
  - `CacheDatabase`/`cached_lists`/`cached_favorites` werden gelesen
    (Befüllung bereits durch `ServerSyncService` vorhanden); ggf.
    Lese-Erweiterungen
- **Tests (`VideoWebPlayer.Maui.Tests`):**
  - MVU-Übergangstests: Cache-First-Befüllung, Aktualisierung nach
    Sync-Abschluss und nach Server-Ereignis, Auswahl → Detail-Navigation
  - Service-Tests: Lesen/Mappen der gecachten Listen (reale SQLite-Datei)
  - Contract-Tests: HomePage-Struktur (drei Listen, Titel), Detail-Route-
    Registrierung, DI-Verdrahtung, Bild-Cache-Ablage im
    NoBackup-Verzeichnis

## Implementierungsansatz

- Die drei Listen werden als anzeigefertige Eintragslisten im `AppModel`
  geführt (Titel, Medientyp, Id, Poster-Bild-Id, optional Fortschritt/
  Untertitel für „Weiterschauen"). Das MVU-Model bleibt plattformneutral
  (keine `ImageSource` im Kern — die Bildauflösung passiert am App-Rand
  über die Bild-Id).
- **Cache-First:** Im Start-Workflow (`StartupRequestedMessage`) wird nach
  der Ablage-Initialisierung ein `LoadHomeListsEffect` ergänzt; der Handler
  liest die Caches und dispatcht die geladenen Listen zurück ins Model —
  die Startseite zeigt damit beim Öffnen sofort den letzten Stand, auch
  ohne Serverantwort bzw. im Offline-Zustand.
- **Aktualisierung:** Nach `SynchronizationCompletedMessage` sowie nach
  jeder Teil-Synchronisation (`SyncContinueWatchingEffect`,
  `SyncFavoritesEffect`, `SyncMetadataEffect` — ausgelöst durch
  `ServerEventReceivedMessage`) wird das Nachladen der Listen aus dem
  Cache angestoßen; die Ereignisverdrahtung aus Schritt 6 bleibt
  unverändert.
- **UI:** Die `HomePage` rendert die drei Listen als horizontale,
  scrollbare Bildlisten nach dem Vorbild von `MediaCarousel`
  (Titelzeile + horizontaler `ScrollView`/`CollectionView`, Poster mit
  abgerundeten Ecken; leere Listen werden ausgeblendet). Der Tap auf einen
  Eintrag dispatcht eine Auswahl-Message; `AppUpdate` erzeugt einen
  Navigations-Effekt auf die Detail-Route `mediadetails` mit Medientyp
  und Id als Query-Parametern (Platzhalterseite `MediaDetailsPage`).
- **Auswahl-/Identifikationsanforderung:** Der Anwender wählt ein Medium
  durch Antippen eines benannten Eintrags (Titel + Vorschaubild) aus den
  Listen — keine Eingabe interner Kennungen.
- Vorschaubilder werden über `GetPictureAsync` des angemeldeten
  `AuthSessionService.Client` geladen und in `PreviewsDirectory`
  (NoBackup) dateibasiert gecacht; ohne Client/Offline werden nur
  vorhandene Datei-Caches verwendet.

## Konfiguration

Keine neuen Konfigurationseinträge. Serveradresse und Anmeldung kommen aus
dem bestehenden Verbindungs-Workflow (`AuthSessionService`).

## Akzeptanzkriterien (aus der Anforderung)

1. Alle drei Listen („Weiterschauen", „Favoriten", „Neu im Programm")
   werden mit den korrekten Inhalten des Servers angezeigt.
2. Bei erneutem Öffnen erscheinen zunächst die gecachten Inhalte (keine
   Wartezeit auf die Serverantwort).
3. Server-Ereignisse (aktualisierte Weiterschauen-Liste, geänderte
   Favoriten, neue Inhalte) aktualisieren die betroffenen Listen ohne
   manuelle Aktualisierung.
4. Die Auswahl eines Eintrags führt zum jeweiligen Medium bzw. dessen
   Detailseite (in diesem Schritt: Navigation zur Detail-Route mit
   identifiziertem Medium; Platzhalter-Detailseite).

## Offene Fragen

Keine — die Cache-Tabellen und -Schlüssel sind befüllt und bekannt, die
Server-Ereignisse sind verdrahtet, die Detailseiten kommen in Schritt 8
(Platzhalter-Route ist ausdrücklich erlaubt), und das UI-Muster ist durch
den Altbestand (`MediaCarousel`/`HomePage`) sowie die Webanwendung
vorgegeben.
