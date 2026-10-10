# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Bei `ConnectionState.Connected` (Wiederherstellung) wird das Header-Bild der bereits geöffneten Detailseite nachgeladen | `EnterConnected` ruft `WithDetailsHeaderLoad(updated, effects)` auf — `EnterConnected` ist der einzige Übergang in `Connected` und deckt damit Erstverbindung, manuelle Anmeldung und die Offline-Reconnect-Kette ab (Programmablauf „Header-Bild-Nachladung bei Wiederverbindung", Umsetzungsschritt 2) | `Reconnect_OfflineEpisodeDetails_EmitsHeaderEffect` | Abgedeckt |
| Verhalten analog dem Hero-Mechanismus (`WithHeroLoad`-Guard bzw. Reconnect-Trigger) | Trigger an derselben Stelle wie `WithHeroLoad`; Guard um `state.HasHeaderImage` analog `HomeHero.HasImage` erweitert, inkl. Wiederholung nach Fehlschlag beim nächsten Verbindungsaufbau | `Reconnect_ExistingHeaderImage_NoReload`, `MediaDetailsLoaded_ExistingHeaderImage_NoReload` | Abgedeckt |
| Offline-Anzeige ohne Bild bleibt korrekt (kein Abrufversuch ohne Verbindung) | Gate `ConnectionState != Connected` bleibt unverändert; bestehender Test `MediaDetailsLoaded_Offline_NoHeaderEffect` bleibt gültig | `MediaDetailsLoaded_Offline_NoHeaderEffect` (bestehend), `Reconnect_OfflineDetailsWithoutHeaderSource_NoHeaderEffect` (neu) | Abgedeckt |
| Betroffene Stelle `ShellAppEffectHandler` (Bildabruf) | Kein Änderungsbedarf — `LoadMediaDetailsHeaderAsync` wird durch den nun beim Reconnect emittierten `LoadMediaDetailsHeaderEffect` erreicht; im Plan als „unverändert" geführt und im Programmablauf als Schritt 6 beschrieben | `Reconnect_OfflineEpisodeDetails_EmitsHeaderEffect` (Effekt-Emmission) | Abgedeckt |
| Kein Redundanzhandeln / keine Seiteneffekte auf anderen Pfaden | Seiteneffekt-Analyse im Plan deckt Erstverbindung, bereits geladenes Bild, Sitzungsende (`MediaDetails = Empty`), Staffelwechsel (`HeaderImageBytes`-Reset vor dem Guard) und den laufenden Ladevorgang ab | `Reconnect_NoDetails_NoHeaderEffect`, `Reconnect_ExistingHeaderImage_NoReload` | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Offline-Detailseite → Reconnect → Header-Bild erscheint | Keiner | Nicht erforderlich mit Begründung: Der Offline↔Online-Wechsel wird OS-seitig über `Connectivity`/`INetworkStatusMonitor` gemeldet; die Dev-API (`/model`, `/tree`, `/invoke`, `/set-text`) bietet keinen Hook zur Injektion von `NetworkAvailabilityChangedMessage` oder zum Schalten des Netzwerkstatus, und das Stoppen des `MockVideoServer` ändert `Connectivity.NetworkAccess` nicht. Der Fluss ist damit im vorhandenen E2E-Stack nicht auslösbar — etablierter Funktionsnachweis sind MVU-Übergangstests (Präzedenz: Schritt-5-Hero, Schritt-6-Detail-Header, Schritt-12-Offline-Wiederherstellung). |

## Hinweise

- `Reconnect_OfflineEpisodeDetails_EmitsHeaderEffect` soll die vollständige Reconnect-Kette nach dem Muster `AppUpdateTests_Offline.Reconnection_FromOffline_RunsFullSyncAndResumesDownloads` durchlaufen (`NetworkAvailabilityChangedMessage` → `ServerHealthCheckCompletedMessage` → `AutoLoginCompletedMessage`), damit der Trigger an `EnterConnected` und nicht nur am Zielzustand hängt.
