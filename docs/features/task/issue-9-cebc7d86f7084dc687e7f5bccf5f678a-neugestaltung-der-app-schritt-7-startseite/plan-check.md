# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| 1. Alle drei Listen mit korrekten Server-Inhalten | `HomeListService` liest die vom `ServerSyncService` befüllten Caches (`continue_watching`, `cached_favorites`, `recent`); Voll-Sync-Abschluss lädt die Listen neu | `HomeListServiceTests` (Mapping je Liste, realer SQLite-Roundtrip), `SynchronizationCompleted_ReloadsHomeLists` | Abgedeckt |
| 2. Bei erneutem Öffnen zunächst gecachte Inhalte | `LoadHomeListsEffect` im Start-Workflow (Cache-First, unabhängig von Serverantwort/Offline) | `StartupRequested_IncludesHomeListLoad`, `LoadHomeLists_EmptyCache_ReturnsEmpty`, Sequenz-Test Start→Sync | Abgedeckt |
| 3. Server-Ereignisse aktualisieren Listen ohne manuelle Aktualisierung | Bestehende Ereignis→Teil-Sync-Verdrahtung + `CachedListsChangedMessage`-Rückdispatch → `LoadHomeListsEffect` | `ServerEventReceived_FullSequence_RefreshesLists`, `CachedListsChanged_ReloadsHomeLists`, Contract-Test des Rückdispatchs | Abgedeckt |
| 4. Auswahl führt zum Medium/Detailseite | `MediaItemSelectedMessage` → `NavigateToMediaDetailsEffect` → Shell-Route `mediadetails` auf Platzhalter-`MediaDetailsPage` (Medientyp+Id+Titel) | `MediaItemSelected_NavigatesToDetails`, `MediaItemSelected_InvalidItem_NoNavigation`, `MediaDetails_RouteRegistered`, `HomePage_ItemsAreTappable` | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Start → gecachte Listen sichtbar → Server-Sync aktualisiert (AK 1+2) | MVU-Message-Sequenz-Test + realer Cache-Roundtrip (`AppUpdateTests_HomeLists`, `HomeListServiceTests`) | Abgedeckt — kein UI-Automatisierungs-Framework im Repo (Begründung im Plan, identisch zu Schritt 6) |
| Server-Ereignis → Liste aktualisiert sich selbst (AK 3) | `ServerEventReceived_FullSequence_RefreshesLists` + Effekt-Handler-Contract-Test | Abgedeckt |
| Eintrag antippen → Detail-Route (AK 4) | `MediaItemSelected_*` + Route-/Tap-Contract-Tests | Abgedeckt |

## Hinweise

- Die Auswahl erfolgt über benannte Einträge mit Vorschaubild (Tap) —
  keine Eingabe interner Kennungen; Usability-Vorgabe erfüllt.
- Betroffener Bestandstest (`SynchronizationCompleted_ClearsSyncFlag`)
  ist in „Betroffene bestehende Tests" und Umsetzungsreihenfolge
  verankert.
- Vorschaubild-Ablage liegt im nicht backup-pflichtigen
  `PreviewsDirectory` — entspricht der Backup-Trennung aus Schritt 3.
