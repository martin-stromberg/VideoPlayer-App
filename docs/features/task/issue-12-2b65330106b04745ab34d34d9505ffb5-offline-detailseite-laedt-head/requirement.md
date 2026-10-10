# Übersetzte Anforderung: Offline-Detailseite lädt Header-Bild nach Reconnect nicht nach

## Fachliche Zusammenfassung

Wird die Medien-Detailseite im Zustand `ConnectionState.Offline` geöffnet, liefert `MediaDetailsService` einen Offline-Snapshot, und `WithDetailsHeaderLoad` in `AppUpdate` verzichtet wegen des Verbindungs-Gates auf den `LoadMediaDetailsHeaderEffect` — der Kopfbereich zeigt korrekt nur Gradienten und Texte. Beim Übergang nach `ConnectionState.Connected` (`EnterConnected`) wird der Header-Ladevorgang für die bereits geöffnete Detailseite jedoch nie erneut angestoßen — anders als der Startseiten-Hero, der über `WithHeroLoad` in `EnterConnected` bei jeder Verbindungsaufnahme nachgeladen wird. Gefordert ist das Nachladen des Header-Bilds bei Wiederherstellung der Verbindung, ohne dass die Seite erneut geöffnet werden muss.

## Betroffene Klassen und Komponenten

- `AppUpdate` (`VideoPlayer.Mvu/AppUpdate.cs`) — `EnterConnected` (einziger Übergang nach `Connected`: `UpdateAutoLoginCompleted` und `UpdateLoginCompleted`), `WithDetailsHeaderLoad` (Lade-Gate für den Kopfbereichs-Bildeffekt)
- `MediaDetailsState` (`VideoPlayer.Mvu/MediaDetailsState.cs`) — `Details`, `HeaderImageBytes`, `IsHeaderImageLoading`, `HasHeaderImage`
- `MediaDetailsView` (`VideoPlayer.Mvu/MediaDetailsView.cs`) — `HeaderEpisodeId`/`HeaderPictureId` (Bildquellen-Ableitung, auch im Offline-Snapshot über `SelectedEpisodeId`)
- `LoadMediaDetailsHeaderEffect` (`VideoPlayer.Mvu/AppEffect.cs`) — unverändert
- `MediaDetailsHeaderImageLoadedMessage` (`VideoPlayer.Mvu/AppMessage.cs`) — unverändert
- `ShellAppEffectHandler.LoadMediaDetailsHeaderAsync` (`VideoPlayer.Maui/Services/ShellAppEffectHandler.cs`) — Bildabruf, unverändert
- Tests: `AppUpdateTests_DetailsHeader` (`VideoWebPlayer.Maui.Tests`)

## Implementierungsansatz

- In `EnterConnected` nach `WithHeroLoad` zusätzlich `WithDetailsHeaderLoad(updated, effects)` aufrufen. Da `EnterConnected` der einzige Übergang in `Connected` ist (automatische und manuelle Anmeldung sowie die Reconnect-Kette Offline → `CheckServerHealthEffect` → `AutoLoginCompletedMessage(Succeeded)`), deckt der Aufruf sowohl die Erstverbindung als auch die Wiederverbindung zur Laufzeit ab.
- Das Gate in `WithDetailsHeaderLoad` um die Bedingung `state.HasHeaderImage` erweitern (analog `HomeHero.HasImage` in `WithHeroLoad`), damit ein bereits geladenes Bild bei der Wiederverbindung nicht erneut abgerufen wird. Nach einem Fehlschlag (`ImageBytes == null`) greift die Bedingung beim nächsten Verbindungsaufbau erneut — gleiches Wiederhol-Muster wie beim Hero.
- Keine neue Klasse, kein neues Interface, kein neues Event nötig — Erweiterung bestehender Methoden im vorhandenen Muster. Effekt, Nachricht und App-Rand (`ShellAppEffectHandler`) bleiben unverändert.

## Konfiguration

Keine — das Verhalten folgt dem bestehenden Hero-Mechanismus und benötigt keine Einstellung.

## Offene Fragen

Keine — Anforderung, Codepfad (`EnterConnected` als einziger Connected-Übergang) und Referenzmuster (`WithHeroLoad`) sind eindeutig.
