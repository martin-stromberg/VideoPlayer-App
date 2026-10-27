# Code-Review: Saisonabhängige Listen

**Status:** Keine Befunde

## Umfang

Unabhängig geprüft: tatsächlicher Mobile-Diff gegen `staging` einschließlich neuer Dateien sowie Server/Web-Diff `eae60a6003ac0619958331351d21c5d983cb175f..1bb12a42ffeadab62702117fcdc9eb9955e7fd48`. Der Prüfer hat keine Produktdateien korrigiert und keine Commits erstellt.

## Befunde

Keine.

## Geprüfte Aspekte

- Authentifizierter API-Zugriff, Quellenfilter und zulässiger verschachtelter Sammlungskontext; Gruppierung von Sammlungen, Singleton-Filmen und Serien sowie bestehende Sortierung.
- Serverzeit entscheidet Saisonfenster; UTC-`expiresAt` ist exklusiv und wird in App/Web ohne zweite Saisonberechnung ausgewertet. Timer werden beim Verlassen/Dispose beendet; die App prüft Ablauf auch beim Erscheinen, Resume und Offline-Neustart.
- Erfolgreiches `[]` ersetzt saisonalen Cache, HTTP-/Transportfehler erhalten den bisherigen gültigen Stand; ungültige, leere und abgelaufene Sektionen werden nicht angezeigt. Logout leert den neuen Modellzustand wie die bestehenden Benutzerlisten.
- Dynamische Karten verwenden den vorhandenen MediaItemSelected-Handler und die vorhandenen Detailziele. Neue Debug-Testuhr ist auf Debug und explizite `e2e-`-Profile begrenzt.
- E2E-Fixture liest beide Ausgabepipes parallel mit aktivem Timeout und beendet bei Fehler den gestarteten Buildprozess samt Kindprozessen. Die sechs neuen mobilen E2E-Flüsse prüfen tatsächliche gebundene UI-Elemente und konkrete Navigationsziele.
- `RaiseUiActionRequested`: keine Aufrufe in den betroffenen Komponenten bzw. Implementierungen gefunden; kein neu eingeführter handlerloser UI-Aktionspfad.

## Unabhängige Validierung

- Mobile: `dotnet test VideoPlayer.Maui.Tests/VideoPlayer.Maui.Tests.csproj --no-restore --filter FullyQualifiedName~SeasonalGenreTests --disable-build-servers` — 7/7 bestanden.
- Backend in eigenem Klon `.work/seasonal-server-review`: 8 Service-/SQLite-/Zeitfenstertests und vier echte Web-E2E — 12/12 bestanden, keine Skips.
- Zwei absichtliche Regressionen in der Backend-Prüfkopie wurden zuverlässig entdeckt: entfernter Film-Quellenfilter (1 Testfehler, unerlaubter Film `Secret`) und um einen Tag verlängertes Ablaufdatum (5 Testfehler). Beide Mutationen zurückgestellt, danach unveränderte 12 Tests grün.
- Abnahmebericht liegt im eigenen Backend-Prüfklon unter `docs/projects/saisonlisten/acceptance-schritt-1.md` und ist für die finale Serverlieferung zu übernehmen. Unabhängiger breiter Lifecycle-Testlauf folgt separat.
