# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| `HomePage` meldet das `ModelChanged`-Abo beim Verlassen ab | Abschnitt „Änderungen": `OnDisappearing` mit `subscribed = false` + `ModelChanged -=`; Abo-Umzug in `OnAppearing` | `HomePage_UnsubscribesModelChangedOnDisappearing` | Abgedeckt |
| `MediaDetailsPage` meldet das Abo ab | Dito; bestehender `requested`-Guard bleibt erhalten | `MediaDetailsPage_UnsubscribesModelChangedOnDisappearing` | Abgedeckt |
| `DownloadsPage` meldet das Abo ab | Dito; `DownloadsPageOpenedMessage`-Dispatch bleibt | `DownloadsPage_UnsubscribesModelChangedOnDisappearing` | Abgedeckt |
| `FavoritesPage` meldet das Abo ab | Dito; `FavoritesPageOpenedMessage`-Dispatch bleibt | `FavoritesPage_UnsubscribesModelChangedOnDisappearing` | Abgedeckt |
| `SourceOverviewPage` meldet das Abo ab | Dito; bedingter `SourceBrowserOpenedMessage`-Dispatch bleibt | `SourceOverviewPage_UnsubscribesModelChangedOnDisappearing` | Abgedeckt |
| `ProfilePage` meldet das Abo ab | Dito; `ProfilePageOpenedMessage`-Dispatch bleibt | `ProfilePage_UnsubscribesModelChangedOnDisappearing` | Abgedeckt |
| Muster analog `SettingsPage`/`PlayerPage` (inkl. `subscribed`-Guard) | Designentscheidung: Guard im gemarshallten Callback; `volatile` begründet nicht nötig | `*_IgnoresModelChangesAfterDisappearing` je Seite | Abgedeckt |
| Kein akutes Absturzrisiko / strukturelle Schuld ohne Verhaltensbruch | Seiteneffekte-Abschnitt: Update nur noch bei sichtbarer Seite, `UpdateUi` in `OnAppearing` stellt Stand bei Rückkehr her; bestehende `OnAppearing`-Fachlogik bleibt | Vollständige Testsuite (Schritt 3 der Umsetzungsreihenfolge) | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Kein Benutzerfluss betroffen (rein strukturelle Lebenszyklus-Korrektur; Leck/Abwesenheit eines Abos ist weder über UI noch über die Dev-API beobachtbar) | — | Nicht erforderlich mit Begründung (Funktionsnachweis über Contract-Pins, dokumentierte Projektkonvention) |

## Hinweise

- Die Negativprobe der Contract-Tests auf dem Ist-Stand (Schritt 1 der Umsetzungsreihenfolge) stellt sicher, dass die Pins das Leck tatsächlich erfassen.
- `AppShell` abonniert ebenfalls im Konstruktor, ist aber eine lebenslang gültige Einzelinstanz — bewusst außerhalb des Scope; der `SourcesContractTests`-Pin auf `ModelChanged` in `AppShell.xaml.cs` bleibt gültig.
