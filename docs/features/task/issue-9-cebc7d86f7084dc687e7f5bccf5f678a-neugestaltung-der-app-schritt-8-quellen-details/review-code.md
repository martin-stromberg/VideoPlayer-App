# Code-Review

Geprüft: vollständige Änderungsmenge des Schritts gegenüber dem
Basis-Branch (Selbstprüfung ohne Unteragenten — dokumentierte
Abweichung). Iteration 1 von max. 3.

## Ergebnis

**Status:** 2 Befunde gefunden und behoben; danach keine weiteren Befunde.

## Geprüfte Bereiche

### MVU-Kern (`VideoPlayer.Mvu`)

- `AppUpdate` (Quellen/Details/Favoriten): Übergänge sind rein,
  Effekte nur im Ergebnis; Guards vorhanden:
  - Offline → kein `LoadSourcesEffect`, Zustand zurückgesetzt
    (kein veralteter Erfolgszustand).
  - Stale-Antworten werden verworfen (`SourceId` bei Browser-
    Antworten, `MediaType`+`MediaId` bei Detail-/Favoriten-Antworten,
    `Page != NextPage` bei Append-Antworten).
  - Doppelte Läufe werden verhindert (`IsLoading`/`IsLoadingMore`-
    Guards, Dedup bei identischer Detail-Anfrage).
- `SourceBrowserService`: Quellen-/Genre-/Eintrags-Mapping korrekt;
  `HasMore` nur bei voller Seite; Einträge ohne identifizierbares
  Medium werden übersprungen; kein Client → kontrollierte
  `InvalidOperationException` (wird vom Effekt-Handler in einen
  Fehlerzustand übersetzt).
- `MediaDetailsService`: Kollektions-404-Fallback auf `movie`;
  Episoden-Kontextauflösung über `show`-Hint oder eingebettete
  `Season.Show`; Staffel ohne Serie → verständlicher Fehler statt
  leerer Ansicht; Favoriten-Ziel bleibt das navigierte Medium
  (`favoriteOverride`).
- `FavoritesService`: Server führend (`toggle`), danach autoritative
  Erneuerung der lokalen Kopie über `SyncFavoritesAsync` — keine
  doppelte Schreiblogik für `cached_favorites`.
- `HomeListService`-Erweiterung: Serienkontext-Hints werden nur bei
  auflösbarem Graphen gesetzt (Episoden/Staffeln), sonst `null`.

### Maui-Schicht (`VideoPlayer.Maui`)

- `ShellAppEffectHandler`: alle neuen Effekte behandelt; Fehler jedes
  Lade-Effekts landen als `*LoadedMessage` mit `Error` im Model (kein
  stiller Ausfall); Navigationen auf dem Hauptthread;
  `ToggleFavoriteEffect` dispatcht bei Erfolg
  `CachedListsChangedMessage` (Startseiten-Favoriten folgen).
- `PosterImageService.GetSourceIconSourceAsync`: gleiche Cache-
  Konvention wie Poster (nicht backup-pflichtiges
  `PreviewsDirectory`, Datei `{id}.sourceicon`, eigener Memory-Cache);
  Fehler → `null` (Titel bleibt Fallback).
- `CachedSourceIconImage`: verwirft nachgereichte Ergebnisse
  (`PictureId`-Vergleich), wie `CachedPosterImage`.
- `SourcesPage`/`SourceOverviewPage`/`MediaDetailsPage`: binden an
  `Model.*`, dispatchen alle Benutzeraktionen als Messages; Auswahl-
  Rücksetzung (`SelectedItem = null`) verhindert Kleben der Markierung;
  `OnAppearing`-Dispatches sind idempotent (Update dedupliziert).
- `AppShell`/`MauiProgram`: Route `sourceoverview` und alle neuen
  Dienste/Seiten registriert.

## Befunde und Behebung

| # | Befund | Behebung |
|---|--------|----------|
| 1 | `MediaDetailsPage`: `FavoriteError` wurde nie sichtbar — das Fehler-Panel hing allein am Lade-`Error` und wurde bei vorhandenen Details ausgeblendet | `UpdateUi` zeigt das Panel jetzt bei `HasError \|\| FavoriteError` der eigenen Identität (`MediaDetailsPage.xaml.cs`, `UpdateUi`) |
| 2 | `SourceOverviewPage`: der aktive Suchtext des Browser-Zustands wurde bei Rückkehr nicht in das Suchfeld zurückgeschrieben — der Filter blieb aktiv, aber unsichtbar | `UpdateUi` spiegelt `browser.SearchText` in `MediaSearchBar.Text` zurück (`SourceOverviewPage.xaml.cs`, `UpdateUi`) |

Beide Behebungen verifiziert: `VideoPlayer.Maui` (Windows-Target) baut
mit 0 Warnungen/0 Fehlern.

## Verbleibende bekannte Einschränkungen (keine Befunde)

- Episodenliste innerhalb des Seiten-`ScrollView` (verschachteltes
  Scrollen wie im Altbestand) — Hinweis in `review-usability.md`.
- Keine UI-E2E-Automatisierung verfügbar — dokumentiert in
  `test-results.md`.
