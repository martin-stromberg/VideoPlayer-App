# Umsetzungsplan: Offline-Detailseite lädt Header-Bild nach Reconnect nicht nach

## Übersicht

Beim Übergang in `ConnectionState.Connected` (`AppUpdate.EnterConnected`) wird zusätzlich zum Startseiten-Hero (`WithHeroLoad`) auch der Kopfbereichs-Bildlauf einer bereits geladenen Detailseite angestoßen (`WithDetailsHeaderLoad`). Damit ein vorhandenes Bild beim Reconnect nicht erneut abgerufen wird, erhält das Gate in `WithDetailsHeaderLoad` die `HasHeaderImage`-Bedingung — analog `HomeHero.HasImage` in `WithHeroLoad`.

## Designentscheidungen

Keine — folgt bestehenden Mustern. `EnterConnected` ist der einzige Übergang in `Connected`; der Reconnect-Trigger dort entspricht exakt dem `WithHeroLoad`-Vorbild derselben Methode.

## Programmabläufe

### Header-Bild-Nachladung bei Wiederverbindung

1. `NetworkAvailabilityChangedMessage(true)` (Online-Rückkehr) → `CheckServerHealthEffect`.
2. `ServerHealthCheckCompletedMessage` (Server erreichbar) → `AttemptAutoLoginEffect`.
3. `AutoLoginCompletedMessage(AuthAttemptStatus.Succeeded)` → `UpdateAutoLoginCompleted` → `EnterConnected`.
4. `EnterConnected` setzt `ConnectionState = Connected` und ruft — nach `WithHeroLoad` — `WithDetailsHeaderLoad(updated, effects)` auf.
5. `WithDetailsHeaderLoad` prüft das Gate: `Connected` (erfüllt, `updated` trägt den neuen Zustand), `Details != null`, Bildquelle vorhanden (`HeaderEpisodeId`/`HeaderPictureId`), `!IsHeaderImageLoading`, `!HasHeaderImage` → `LoadMediaDetailsHeaderEffect` wird in `effects` gereiht und `IsHeaderImageLoading = true` gesetzt.
6. `ShellAppEffectHandler.LoadMediaDetailsHeaderAsync` ruft das Bild über den angemeldeten API-Client ab (`GetEpisodeBackgroundImageAsync` bzw. `GetPictureAsync`) und meldet `MediaDetailsHeaderImageLoadedMessage`.
7. `UpdateMediaDetailsHeaderImageLoaded` prüft die Identität (Medientyp, Id, Bildquelle) und übernimmt `ImageBytes` in `MediaDetails.HeaderImageBytes`.
8. `MediaDetailsPage.UpdateHeaderImage` erkennt die neuen Bytes und zeigt das Bild an — ohne erneutes Öffnen der Seite.

Beteiligte Klassen/Komponenten: `AppUpdate`, `MediaDetailsState`, `MediaDetailsView`, `LoadMediaDetailsHeaderEffect`, `MediaDetailsHeaderImageLoadedMessage`, `ShellAppEffectHandler`, `MediaDetailsPage`.

### Erstverbindung (unverändertes Verhalten)

`EnterConnected` läuft auch beim ersten Verbindungsaufbau; `MediaDetails` ist dann `Empty` → das Gate in `WithDetailsHeaderLoad` (`Details == null`) lässt das Modell unverändert — keine Seiteneffekte.

## Neue Klassen

Keine.

## Änderungen an bestehenden Klassen

### `AppUpdate` (`VideoPlayer.Mvu/AppUpdate.cs`)

- **Geänderte Methoden:**
  - `EnterConnected` — nach `WithHeroLoad(updated, effects)` zusätzlich `updated = WithDetailsHeaderLoad(updated, effects);` aufrufen, mit kurzer Kommentarzeile im Stil des Hero-Kommentars (Nachladen des Detail-Kopfbilds bei Verbindungsaufnahme).
  - `WithDetailsHeaderLoad` — Gate um `state.HasHeaderImage` erweitern (zusätzliche Abbruchbedingung): ein bereits geladenes Bild wird beim Reconnect nicht erneut abgerufen; nach einem Fehlschlag (`HeaderImageBytes == null`, `IsHeaderImageLoading == false`) greift die Bedingung beim nächsten Verbindungsaufbau erneut. XML-Doku der Methode entsprechend ergänzen („…weder ein Ladevorgang läuft noch ein Bild vorliegt" — Wortlaut des `WithHeroLoad`-Kommentars).

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine.

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Erstverbindung:** Kein Effekt — `MediaDetails` ist `Empty`, das Gate blockt.
- **Bereits geladenes Bild:** Durch die neue `HasHeaderImage`-Bedingung wird bei einem Reconnect kein redundanter Bildabruf ausgelöst (vorheriges Verhalten wäre ein unnötiger zweiter Abruf gewesen).
- **Gleiche Detailseite erneut geladen:** `UpdateMediaItemSelected` verhindert bereits ein Nachladen derselben Identität (`IsLoading || Details is not null` → `Unchanged`); der neue Guard kann dadurch kein veraltetes Bild festhalten, weil `MediaDetailsLoaded` für dieselbe Identität ohne Quellwechsel nicht erneut eintrifft.
- **Abmeldung/Sitzungsablauf/Gate-Ablehnung:** `MediaDetails` wird auf `Empty` zurückgesetzt — beim nächsten `EnterConnected` greift das Detail-Gate nicht.
- **Staffelwechsel:** `UpdateSeasonSelected` setzt `HeaderImageBytes` bei geänderter Bildquelle vor dem `WithDetailsHeaderLoad`-Aufruf auf `null` — der Guard greift nicht, das Verhalten bleibt unverändert.
- **Laufender Ladevorgang beim Offline-Übergang:** `IsHeaderImageLoading` bleibt gesetzt, bis `MediaDetailsHeaderImageLoadedMessage` eintrifft (Identitätsprüfung, kein Verbindungs-Gate) — kein dauerhaft blockierter Zustand; bei Abbruch des Effekts ohne Antwort bliebe der Zustand bestehen (bestehendes, unverändertes Verhalten — gleiche Semantik wie `HomeHero.IsLoading`).

## Umsetzungsreihenfolge

1. **Gate-Erweiterung `WithDetailsHeaderLoad`**
   - Voraussetzungen: Keine
   - Beschreibung: Bedingung `state.HasHeaderImage` in die Abbruch-Prüfung aufnehmen; XML-Doku ergänzen.

2. **Reconnect-Trigger in `EnterConnected`**
   - Voraussetzungen: Schritt 1 (Guard-Logik konsistent zum Hero-Muster)
   - Beschreibung: `WithDetailsHeaderLoad(updated, effects)` nach dem `WithHeroLoad`-Aufruf einreihen; Kommentar im Stil des Hero-Kommentars.

3. **Tests schreiben**
   - Voraussetzungen: Schritte 1–2 (Tests prüfen das Endverhalten)
   - Beschreibung: Neue Testmethoden in `AppUpdateTests_DetailsHeader` (siehe Tests-Abschnitt); Hilfsmethoden `WithDetails`/`ShowDetails` wiederverwenden.

4. **Build und Tests ausführen**
   - Voraussetzungen: Schritte 1–3
   - Beschreibung: `dotnet build` der Solution und `dotnet test VideoWebPlayer.Maui.Tests` — alle Tests grün.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `Reconnect_OfflineEpisodeDetails_EmitsHeaderEffect` | `AppUpdateTests_DetailsHeader` | Offline-Modell mit geladenen Episoden-Details (`SelectedEpisodeId` → `HeaderEpisodeId`), Reconnect-Kette bis `AutoLoginCompletedMessage(Succeeded)` → `LoadMediaDetailsHeaderEffect` in den Effekten, `IsHeaderImageLoading = true` |
| `Reconnect_OfflineDetailsWithoutHeaderSource_NoHeaderEffect` | `AppUpdateTests_DetailsHeader` | Offline-Details ohne Bildquelle (Film ohne `BannerPictureId`) → kein `LoadMediaDetailsHeaderEffect` beim Reconnect |
| `Reconnect_ExistingHeaderImage_NoReload` | `AppUpdateTests_DetailsHeader` | Details + `HeaderImageBytes` gesetzt → Reconnect löst keinen Header-Effekt aus (`HasHeaderImage`-Guard) |
| `Reconnect_NoDetails_NoHeaderEffect` | `AppUpdateTests_DetailsHeader` | `MediaDetailsState.Empty` beim Reconnect → kein Header-Effekt |
| `MediaDetailsLoaded_ExistingHeaderImage_NoReload` | `AppUpdateTests_DetailsHeader` | `MediaDetailsLoaded` bei bereits gesetztem `HeaderImageBytes` → kein erneuter Effekt (Guard-Konsistenz auf dem Detail-Ladepfad) |

### Betroffene bestehende Tests

Keine — `MediaDetailsLoaded_Offline_NoHeaderEffect` bleibt gültig (Gate blockt weiterhin im `Offline`-Zustand); alle bestehenden Header-Tests laufen ohne `HeaderImageBytes` im Arrange, sodass der neue Guard ihr Verhalten nicht ändert.

### E2E-Tests (primärer Funktionsnachweis)

Der Benutzerfluss (Offline-Detailseite → Verbindungsrückkehr → Bild erscheint) lässt sich im vorhandenen E2E-Stack (`VideoPlayer.E2E.Tests`, FlaUI + Dev-API + `MockVideoServer`) nicht auslösen: Der Offline↔Online-Wechsel wird vom OS-seitigen `Connectivity`-Monitor (`INetworkStatusMonitor`) gemeldet — die Dev-API (`/model`, `/tree`, `/invoke`, `/set-text`) bietet keinen Hook, um `NetworkAvailabilityChangedMessage` zu injizieren oder den Netzwerkstatus zu schalten, und das Stoppen des `MockVideoServer` ändert `Connectivity.NetworkAccess` nicht. Der etablierte Funktionsnachweis für diesen Mechanismus (identisch bei Schritt-5-Hero, Schritt-6-Detail-Header, Schritt-12-Offline-Wiederherstellung) sind MVU-Übergangstests — dieser Plan folgt demselben Muster. Keine bestehenden E2E-Tests betroffen.

## Offene Punkte

Keine.
