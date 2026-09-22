# Testergebnisse

## Zusammenfassung

| Lauf | Ergebnis |
|------|----------|
| Baseline (vor der Umsetzung) | **431 Tests bestanden, 0 Fehler, 0 übersprungen** — `inventory/test-results/baseline-dotnet-test.log` |
| Finaler Testlauf | **469 Tests bestanden, 0 Fehler, 0 übersprungen** — `final-dotnet-test.log` |
| Build `VideoPlayer.Maui` (net10.0-windows) | **0 Warnungen, 0 Fehler** |

## Ausgeführte Kommandos

```text
dotnet test VideoWebPlayer.Maui.Tests/VideoWebPlayer.Maui.Tests.csproj
```

Ergebniszeile:

```text
Bestanden!   : Fehler: 0, erfolgreich: 469, übersprungen: 0, gesamt: 469
```

```text
dotnet build VideoPlayer.Maui/VideoPlayer.Maui.csproj -f net10.0-windows10.0.19041.0
```

Ergebnis: `0 Warnung(en), 0 Fehler`.

## Neue Tests (44 gegenüber der Baseline)

### MVU-Übergänge (`VideoWebPlayer.Maui.Tests/AppUpdateTests_FavoritesProfile.cs`)

- `FavoritesPageOpenedMessage` → Lauf bei `Connected` mit
  `LoadFavoritesPageEffect(RefreshFromServer: true)`; Dedup bei
  laufendem Lauf; keine Ladung in anderen Zuständen (z. B.
  `LoginPending`/`RequiresLogin`).
- `FavoritesPageOpenedMessage` bei `Offline` → Lauf mit
  `RefreshFromServer = false` (nur Cache, kein Serveraufruf).
- `FavoritesPageLoadedMessage` → Liste/Empty/Fehler übernehmen;
  veraltete Antwort ohne ausstehende Ladung wird verworfen.
- `FavoriteRemoveRequestedMessage` → `IsRemoving` +
  `RemoveFavoriteEffect(FavoriteId)`; Guards: offline, unbekannte
  `FavoriteId`, laufende Entfernung.
- `FavoriteRemoveCompletedMessage` → Erfolg entfernt den Eintrag und
  löscht Fehler; Fehlschlag behält den Eintrag, setzt `Error`.
- `ProfilePageOpenedMessage` → Lauf nur bei `Connected` + Dedup;
  `ProfileInfoLoadedMessage` übernimmt `ProfileInfo`.
- `LogoutRequestedMessage` → Guard auf `Connected`; Sitzungsdaten
  geleert (`ConnectionState.RequiresLogin`, Listen/Quellen/Details/
  Favoriten-/Profil-Zustand zurückgesetzt, Gerätedaten behalten),
  Effektfolge `DisconnectServerEventsEffect` → `LogoutEffect` →
  `NavigateToPageEffect(AppPage.Login)`; `RequiresLogout` gesperrt.

### Dienste

- `ProfileInfoServiceTests` — Mapping gespeicherter Kennung +
  `AppProfile.Name`; Token-Claims (`unique_name`, URI-Formen,
  `email`, `preferred_username`-Fallback); Fallbacks bei fehlendem
  Token/ungültigem JWT/fehlenden Claims.
- `FavoritesServiceTests` — Remove-Anfrage `api/favorites/remove`
  mit `{ Id, UserId: "anonymous" }`, autoritative Erneuerung der
  Kopie via `SyncFavoritesAsync`, `false` ohne Client.
- `AuthSessionServiceTests` — `LogoutAsync` gibt den Client frei und
  löscht Benutzername/Passwort/Token (`ClearCredentialsAsync`).
- `HomeListServiceTests` — `LoadFavoriteItemsAsync`: `FavoriteId`
  aus dem Cache-Eintrag, Serienkontext-Hints (Show/Season/Episode),
  Poster-ID-Mapping, Reihenfolge.

### Oberflächen-Verträge (`FavoritesProfileContractTests.cs`)

- `FavoritesPage.xaml`: `CollectionView`-Bindung, Poster via
  `CachedPosterImage`, `MediaTypeText`, „Entfernen"-Button je
  Eintrag, Offline-Banner-Text, Lade-/Fehler-/Leerzustand.
- `FavoritesPage.xaml.cs`: `MvuHost`-Verdrahtung,
  `FavoritesPageOpenedMessage` beim Erscheinen,
  `MediaItemSelectedMessage` bei Auswahl, Entfernen dispatcht
  `FavoriteRemoveRequestedMessage` mit der Favoriten-ID.
- `ProfilePage.xaml`/`.xaml.cs`: Benutzername/E-Mail/Profil/
  Serveradresse gebunden, „Abmelden" dispatcht
  `LogoutRequestedMessage`, `ProfilePageOpenedMessage` beim
  Erscheinen.

## Anmerkungen

- Die Baseline-Nummer 431 stammt aus dem Inventar-Lauf
  (`inventory/test-results/baseline-dotnet-test.log`); ein
  anfänglich gemeldeter Exit-Code 1 des Wrapper-Aufrufs wurde als
  Shell-Artefakt identifiziert — der direkte Lauf lieferte stets
  Exit-Code 0 bei 431/0/0.
