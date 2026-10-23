# Anforderung: Einheitliches Erscheinungsbild nach Web-Vorbild (Entwicklungsschritt 1)

Quelle: `docs/projects/task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app/project-plan.md`,
Schritt 1 (fachliche Grundlage: FA-1 aus der Projekt-`requirement.md`, Kundenfeedback Issue #9).
Design-Referenz: `docs/projects/task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app/web-design-recherche.md`
(dokumentierte CSS-Design-Tokens der deployten Webanwendung).

## Fachliche Zusammenfassung

Die .NET-MAUI-App (`VideoPlayer.Maui`, neue App — die Legacy-App `VideoWebPlayer.Maui`
bleibt unangetastet) soll durchgängig das dunkle Erscheinungsbild der Webanwendung
übernehmen: tiefschwarzer Hintergrund statt des bisherigen Dunkelgraus sowie die
Farben und Eckenradien der Webanwendung. Umzusetzen ist eine reine
Design-Umstellung: Die zentralen Stil-Ressourcen werden auf die dokumentierten
Web-Design-Tokens umgestellt, sämtliche hartcodierten Hexfarben in Seiten und
Steuerelementen werden durch die zentralen Ressourcen ersetzt, und die
App-Navigation (Shell/Flyout einschließlich der dynamischen Quellen-Menüeinträge)
erhält die Web-Farbgebung. Das Verhalten der App bleibt unverändert.

## Design-Tokens (Vorgaben aus `web-design-recherche.md`)

| Token | Wert | Bedeutung |
|-------|------|-----------|
| `--vp-bg` | `#131313` | Seitenhintergrund |
| `--vp-bg-deep` | `#0e0e0e` | tieferer Hintergrund je Fläche |
| `--vp-surface-low` | `#1c1b1b` | Flächenfarbe abgestuft |
| `--vp-surface` | `#201f1f` | Flächenfarbe abgestuft |
| `--vp-surface-high` | `#2a2a2a` | Flächenfarbe abgestuft |
| `--vp-surface-bright` | `#393939` | Flächenfarbe abgestuft |
| `--vp-text` | `#e5e2e1` | Text normal |
| `--vp-text-muted` | `#bda9a6` | Text gedämpft |
| `--vp-text-soft` | `#e9bcb6` | Text weich |
| `--vp-border` | `rgba(255,255,255,0.1)` | Rahmen normal |
| `--vp-border-strong` | `rgba(255,180,170,0.42)` | Rahmen betont / Fokus |
| `--vp-primary` | `#e50914` | Primärfarbe |
| `--vp-primary-hover` | `#c0000c` | Primärfarbe Hover |
| `--vp-primary-soft` | `#ffb4aa` | Primärfarbe weich |
| `--vp-secondary` | `#00a8e8` | Sekundärfarbe |
| `--vp-secondary-soft` | `#83cfff` | Sekundärfarbe weich |
| `--vp-danger` | `#ffb4ab` | Fehler-/Danger-Farbe |
| `--vp-radius-sm` | `4px` | Eckenradius normale Buttons |
| `--vp-radius` | `8px` | Eckenradius Karten |

Rundungsregel: normale Buttons 4px (wie die Web-Buttons), Karten 8px, Chips,
Badges und runde Icon-Schaltflächen pill-förmig (999px bzw. kreisrund).

## Betroffene Klassen und Komponenten

- `VideoPlayer.Maui/Resources/Styles/Colors.xaml` — zentrale Farbressourcen,
  derzeit noch die unveränderte MAUI-Template-Palette.
- `VideoPlayer.Maui/Resources/Styles/Styles.xaml` — globale Styles
  (Button-`CornerRadius`, `ContentPage`/Shell-Hintergrund, weitere Control-Styles).
- Alle Seiten mit hartcodierten Hexfarben: `Pages/HomePage.xaml`,
  `Pages/MediaDetailsPage.xaml`, `Pages/DownloadsPage.xaml`,
  `Pages/FavoritesPage.xaml`, `Pages/ProfilePage.xaml`,
  `Pages/SourceOverviewPage.xaml`, `Pages/PlayerPage.xaml`,
  `Pages/LoginPage.xaml`, `Pages/SettingsPage.xaml`,
  `Pages/ServerSetupPage.xaml` sowie `Controls/MediaCarousel.xaml`.
- `AppShell.xaml` — Flyout/Navigation inkl. der dynamischen Quellen-Menüeinträge
  (Templates, seit Commit `1fd22fc`/`1831c53`).
- `VideoWebPlayer.Maui.Tests` — Contract-Tests, die die Farb-/Radius-Pins
  absichern (Muster: `SourcesContractTests` bzw. bestehende
  `VideoPlayerMauiContractTests`).

## Implementierungsansatz

- `Colors.xaml`: Die Web-Design-Tokens werden als benannte `Color`-Ressourcen
  abgebildet (inkl. der `rgba`-Rahmenfarben via Alpha); die bestehenden
  Template-Schlüssel werden entweder auf die Web-Werte umgepinnt oder durch
  Web-token-benannte Schlüssel ersetzt — alle Referenzen (`StaticResource`/
  `AppThemeBinding`) werden entsprechend umgestellt, sodass keine toten
  Schlüssel und keine verstreuten Hexwerte zurückbleiben.
- `Styles.xaml`: `ContentPage`-/Shell-Hintergrund auf `#131313`/`#0e0e0e`,
  globaler `Button`-Style auf `CornerRadius="4"`, Karten-Rahmen 8px,
  Chip-/Badge-Styles pill-förmig, Textfarben und Rahmenfarben aus den Tokens.
- Seiten/Controls: Jede hartcodierte Hexfarbe wird durch `StaticResource`
  (bzw. vorhandene DynamicResource-Konventionen) auf die zentralen Ressourcen
  ersetzt; semantisch passende Tokens werden zugeordnet (z. B. gedämpfter Text
  → `TextMuted`, Fehler → `Danger`).
- `AppShell.xaml`: Flyout-Hintergrund, Eintragsfarben und die Templates der
  dynamischen Quellen-Menüeinträge auf Web-Farben umstellen.
- Keine Verhaltensänderung: ausschließlich Farben, Radien und
  Stil-Ressourcen-Referenzen.

## Konfiguration

Keine — reine Umstellung der Stil-Ressourcen.

## Akzeptanzkriterien

1. Der App-Hintergrund entspricht dem Schwarzton der Webanwendung (`#131313`
   bzw. `#0e0e0e` je Fläche), kein Dunkelgrau.
2. Alle dokumentierten Design-Tokens der Webanwendung sind in den zentralen
   Stilen der App abgebildet.
3. Normale Buttons haben einen Eckenradius von 4px; Karten 8px; Chip-/Badge-
   Elemente und runde Icon-Schaltflächen sind pill-förmig.
4. In den Seiten und Steuerelementen von `VideoPlayer.Maui` sind keine
   hartcodierten Hexfarben mehr vorhanden.
5. Die App-Navigation (Shell/Flyout inkl. dynamischer Quelleneinträge) trägt
   die Web-Farbgebung.
6. Die Bedienung ist auf Windows und iOS unverändert (reine Design-Umstellung).
7. Alle bestehenden Tests bleiben grün; es gilt die Definition of Done
   (`dotnet build VideoPlayer.App.sln` in Debug und Release fehlerfrei,
   `dotnet test VideoWebPlayer.Maui.Tests` grün,
   `dotnet format whitespace --verify-no-changes` sauber).

## Explizite Nicht-Ziele

- Keine Änderungen an der Legacy-App `VideoWebPlayer.Maui`.
- Keine Änderungen an der Blazor-Webanwendung oder der Server-API.
- Keine strukturelle Umstellung der Navigation (Shell/Flyout bleibt, nur
  Farbgebung).
- Keine inhaltlichen/verhaltensändernden Umbauten (Hero, Detail-Header,
  Angesehen-Symbol, Player-/Download-Fixes sind eigene Entwicklungsschritte).

## Offene Fragen

Keine — die Token-Werte und die Rundungsregeln sind durch
`web-design-recherche.md` und den Projektplan (behandelte offene Punkte)
vollständig vorgegeben.
