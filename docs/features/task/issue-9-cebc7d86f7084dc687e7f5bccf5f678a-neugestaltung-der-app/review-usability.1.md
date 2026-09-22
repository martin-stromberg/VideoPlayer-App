# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### AppShell.xaml.cs (Navigationsmenü / Flyout)

- **Erreichbarkeit** — Ladefehlschlag der Quellenliste bleibt für die Anwenderin unsichtbar und ohne Ausweg: Schlägt `LoadSourcesEffect` fehl, wird `SourcesOverviewState.Error` zwar im Model gesetzt (`AppUpdate.cs` `UpdateSourcesLoaded`), aber die Shell wertet ausschließlich `Sources` aus (`UpdateSourceMenuItems`, AppShell.xaml.cs:87-98). Das Menü enthält dann schlicht keine Quellen-Einträge — nicht einmal die Überschrift „Quellen" erscheint. Eine nicht-technische Anwenderin kann „keine Quellen freigegeben" nicht von „Laden fehlgeschlagen" unterscheiden und findet ihre bisherigen Medienbereiche kommentarlos nicht mehr vor. Die bisherige `SourcesPage` bot in dieser Situation einen Fehlertext samt Schaltfläche „Erneut laden"; mit ihrem Wegfall existiert kein anwenderseitiger Weg, den Ladevorgang erneut anzustoßen (kein „Jetzt synchronisieren" o. ä. in den Einstellungen — ein erneuter Versuch erfolgt erst bei Wiederverbindung, Serverwechsel oder einem Server-Ereignis).

  Empfehlung: Bei `SourcesOverview.HasError` im Menü einen sichtbaren, verständlichen Eintrag anzeigen (z. B. deaktivierte Zeile „Quellen konnten nicht geladen werden" unter der Überschrift bzw. einen Eintrag „Quellen", der eine Hinweis-/Retry-Seite öffnet), damit die Anwenderin den Zustand erkennt und eine erneute Auslösung der Abfrage möglich ist.

- **Erreichbarkeit** — Die Sektionsüberschrift „Quellen" ist ein deaktiviertes `MenuItem` (`InsertSourceMenuItems`, AppShell.xaml.cs:127): Sie wird im Flyout als Menüzeile gerendert, reagiert aber nicht auf Antippen. Eine Anwenderin, die den Eintrag aus der bisherigen Version als öffnende Quellen-Seite kannte oder ihn schlicht für einen normalen Menüpunkt hält, tippt auf eine scheinbar „kaputte"/wirkungslose Zeile — aus der Darstellung ist nicht erkennbar, dass es sich um eine Überschrift handelt.

  Empfehlung: Die Überschrift optisch eindeutig als Gruppentitel gestalten (z. B. über eigenes `FlyoutItem` mit `FlyoutDisplayOptions.AsMultipleItems` als gruppierte Sektion, eigenes `FlyoutContent` mit Label-Überschrift, oder zumindest abweichende Formatierung), damit sie nicht wie ein funktionsloser Menüeintrag wirkt.

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:

- Pro freigegebener Quelle einen benannten Menüeintrag (mit Icon, falls vorhanden) im Hauptmenü sehen → unauffällig (Einträge mit Quellenname werden dynamisch eingefügt; Icon wird nachgeladen, fällt bei Fehlschlag auf den Namen zurück)
- Quellen-Eintrag auswählen und direkt zur Titelübersicht der Quelle gelangen → unauffällig (Auswahl über benannte, sichtbare Menüeinträge; keine internen Kennungen erforderlich; bestehender `SourceSelectedMessage`-Pfad mit Genre-Filter, Suche und Paging wird wiederverwendet)
- Geänderte Quellenfreigaben ohne App-Neustart im Menü sehen (nach Verbindung/Synchronisation) → unauffällig (Laden bei `EnterConnected`, nach `SynchronizationCompletedMessage` und `CachedListsChangedMessage`)
- Menü im Offline-Zustand benutzen → unauffällig (Quelleneinträge werden zurückgesetzt; kein Fehler-/Absturzverhalten — entspricht der Anforderung „keine Quellenliste offline")
- Übrige Menüeinträge (Startseite, Favoriten, Downloads, Profil, Einstellungen) weiterhin nutzen → unauffällig (alle fünf `FlyoutItem`s bleiben erhalten)
- Umgang mit einem Ladefehlschlag der Quellenliste → Befund vorhanden (siehe oben: keine Fehleranzeige, keine Wiederholungsmöglichkeit)
- Sektionsbeschriftung „Quellen" im Menü verstehen → Befund vorhanden (siehe oben: deaktivierter Menüeintrag wirkt wie funktionslose Zeile)

## Geprüfte Dateien

Liste aller geprüften UI-Dateien (ungecommittete Änderungen im Arbeitsverzeichnis, `git status`/`git diff`):

- `VideoPlayer.Maui/AppShell.xaml`
- `VideoPlayer.Maui/AppShell.xaml.cs`
- `VideoPlayer.Maui/Pages/SourcesPage.xaml` (gelöscht — bisherige Quellen-Übersichtsseite)
- `VideoPlayer.Maui/Pages/SourcesPage.xaml.cs` (gelöscht)
- `VideoPlayer.Maui/Controls/CachedSourceIconImage.cs` (gelöscht — bisheriges Icon-Control der Quellenliste)
- `VideoPlayer.Maui/MauiProgram.cs` (DI-Registrierung der entfallenen Seite)
- `VideoPlayer.Mvu/AppModel.cs`, `VideoPlayer.Mvu/SourcesOverviewState.cs`, `VideoPlayer.Mvu/AppPage.cs`, `VideoPlayer.Mvu/AppRoutes.cs`, `VideoPlayer.Mvu/AppMessage.cs`, `VideoPlayer.Mvu/AppUpdate.cs` (soweit sie sichtbare Bedienzustände des Menüs definieren)
