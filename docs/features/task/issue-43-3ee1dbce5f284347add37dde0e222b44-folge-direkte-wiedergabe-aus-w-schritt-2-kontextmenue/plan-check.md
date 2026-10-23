# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| AK 1: 2-s-Long-Press öffnet Kontextmenü; kurzer Tap behält Verhalten | `PointerGestureRecognizer` + 2-s-Timer in `MediaCarousel` (`LongPressEnabled`, `ItemLongPressed`); Tap-Pfad unverändert | E2E `LongPressMovie_ChooseHide_*` (Menü sichtbar) + bestehender Tap-Regressionstest | Abgedeckt |
| AK 2: Menü bietet „Ausblenden" und „Überspringen" | `DisplayActionSheet` mit beiden Klartext-Aktionen + „Abbrechen" | E2E-Menü sichtbar + Contract-Test `HomePage_LongPress_ShowsActionSheet_WithGermanLabels` | Abgedeckt |
| AK 3: Hide→hide-Endpunkt, Skip→skip-Endpunkt; Eintrag verschwindet bei Erfolg | `ContinueWatchingMutationService` (online: Endpunkt + `SyncContinueWatchingAsync`; offline: lokales Entfernen) + `CachedListsChangedMessage` | E2E hide/skip-Szenarien (Request-Nachweis + Listenverhalten); Unit `ContinueWatchingMutationServiceTests`; `AppUpdateTests_ContinueWatchingActions` | Abgedeckt |
| AK 4: Kontextmenü auf Weiterschauen-Liste beschränkt | `LongPressEnabled` nur am Weiterschauen-`MediaCarousel`; `ItemLongPressed` nur dort verdrahtet | Contract-Test `MediaCarousel_LongPressWiring_OnlyOnContinueWatching` | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Keine — Status `Plan vollständig`.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Long-Press → Menü → „Ausblenden" → hide-Endpunkt → Liste aktualisiert | `LongPressMovie_ChooseHide_CallsHideEndpoint_EntryDisappears` | Abgedeckt |
| Long-Press → Menü → „Überspringen" → skip-Endpunkt → Liste aktualisiert | `LongPressMovie_ChooseSkip_CallsSkipEndpoint_EntryDisappears` | Abgedeckt |
| Long-Press → „Abbrechen" → kein Request, Eintrag bleibt | `LongPressMovie_Cancel_KeepsEntry_NoMutationRequests` | Abgedeckt |
| Kurzer Tap = Direktwiedergabe (Regression) | `TapContinueWatchingMovie_StartsPlaybackAtResume_WithoutDetails` (bestehend) | Abgedeckt |

## Fehlende oder unvollständige Planbestandteile

Keine — Status `Plan vollständig`.

## Hinweise

- Die `DisplayActionSheet`-Auswahl wird im E2E über `UiDriver.ClickByName` (Klartext „Ausblenden"/„Überspringen") getrieben; Popups werden prozessbezogen auf Desktop-Ebene gesucht (WinUI-Dialog außerhalb des Fenster-Teilbaums möglich).
- Die DevApi-`gesture`-Erweiterung ist abwärtskompatibel (`gesture` optional, Default `"tap"`).
