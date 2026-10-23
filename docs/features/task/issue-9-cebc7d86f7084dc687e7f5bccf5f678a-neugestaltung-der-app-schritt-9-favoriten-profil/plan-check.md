# Plan-Checkliste

- [x] Die Anforderung ist vollständig in `requirement.md` übersetzt.
      Alle Akzeptanzkriterien (Favoriten-Liste online/offline,
      Entfernen mit Server-Aufruf und Folge-Updates, Detail-Navigation,
      Profil-Anzeige mit Benutzername/E-Mail + lokalem Profil,
      Abmelden in den Anmelde-Workflow) sind im Plan abgebildet.
- [x] `plan.md` beschreibt Änderungen an allen beteiligten
      Komponenten (MVU-Modelle, `AppUpdate`, `HomeListService`,
      `FavoritesService`, `AuthSessionService`, `ProfileInfoService`,
      `FavoritesPage`, `ProfilePage`, `ShellAppEffectHandler`,
      `MauiProgram`).
- [x] Die Testabdeckung ist geplant und jede Akzeptanzanforderung hat
      einen nachvollziehbaren E2E-Nachweis (MVU-Fluss-Tests +
      Service-Tests + XAML/DI-Contract-Tests, gemäß Projektkonvention).
- [x] Die Reihenfolge ist umsetzbar: Modelle → Update → Dienste →
      Handler/Seiten → Tests.
- [x] Neue Komponenten sind benannt (`FavoriteListItem`,
      `FavoritesPageState`, `ProfilePageState`, `ProfileInfo`,
      `ProfileInfoService`, 7 Messages, 4 Effekte).
- [x] Änderungen an bestehenden Dateien sind konkret aufgelistet.
- [x] Keine unklaren Anforderungen — die einzige API-Lücke
      (kein Benutzerinfo-/Logout-Endpunkt) ist dokumentiert und als
      Designentscheidung (JWT-Claims + lokale Sitzungsräumung)
      verankert; keine Annahmen offen.

## Testabdeckung (Kurzprüfung)

| Anforderung | Nachweis im Plan |
|-------------|------------------|
| Favoriten online laden | `FavoritesPageOpened`→`FavoritesPageLoaded` + Handler-Contract + XAML-Contract |
| Favoriten offline | `FavoriteRemoveRequested_Offline_NoOp` + Offline-Banner-Contract |
| Entfernen + Folge-Updates | `FavoriteRemove*` + `RemoveFavorite_*` (echte SQLite) + `CachedListsChangedMessage`-Contract |
| Detail-Navigation | `MediaItemSelected`-Übergang + Dispatch-Contract |
| Profil-Anzeige | `ProfileInfoLoaded` + `ProfileInfoServiceTests` + XAML-Contract |
| Abmelden | `LogoutRequested_*` + `LogoutAsync_*` + XAML-Contract |

## Nicht ausgewählte Alternativen (verifiziert)

- `HomeMediaItem` um `FavoriteId` erweitern statt eigenem
  `FavoriteListItem` — verworfen: würde den Startseiten-Typ für einen
  Seitenzweck verunreinigen (`IsRemoving`/`Error` gehören nicht in
  `HomeMediaItem`).
- Eigenes Kontextmenü/Swipe zum Entfernen — verworfen: kein
  Kontextmenü-Bestand in der App; Button je Zeile entspricht der
  Web-Referenz und ist plattformübergreifend sicher.
- „Server-Userinfo über neuen API-Endpunkt" — verworfen: kein
  Frontend-Workaround für fehlende API erlaubt; JWT-Claims +
  gespeicherter Benutzername sind die nächstliegende
  unterstützte Quelle.
