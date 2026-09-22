# Web-Design-Recherche (deployte Webanwendung, localhost:83)

Recherche am deployten Stand mit Kunden-Zugang `devin@example.com`. Quelle: ausgeliefertes
HTML + `app.css`/`VideoWebPlayer.styles.css` des Server-Deployments
(`D:\Dashboard\VideoPlayer`, Quell-Stand `D:\Repositories\softwareschmiede\fd729906-…`).

## Design-Token (CSS-Variablen, :root in app.css)

| Token | Wert |
|---|---|
| `--vp-bg` | `#131313` |
| `--vp-bg-deep` | `#0e0e0e` |
| `--vp-surface-low` | `#1c1b1b` |
| `--vp-surface` | `#201f1f` |
| `--vp-surface-high` | `#2a2a2a` |
| `--vp-surface-bright` | `#393939` |
| `--vp-text` | `#e5e2e1` |
| `--vp-text-muted` | `#bda9a6` |
| `--vp-text-soft` | `#e9bcb6` |
| `--vp-border` | `rgba(255,255,255,0.1)` |
| `--vp-border-strong` | `rgba(255,180,170,0.42)` |
| `--vp-primary` | `#e50914` |
| `--vp-primary-hover` | `#c0000c` |
| `--vp-primary-soft` | `#ffb4aa` |
| `--vp-secondary` | `#00a8e8` |
| `--vp-secondary-soft` | `#83cfff` |
| `--vp-danger` | `#ffb4ab` |
| `--vp-radius-sm` | `4px` |
| `--vp-radius` | `8px` |

Hinweis „runde Buttons": `.btn-primary`/`.btn-outline-light`/`.btn-ghost` nutzen
`--vp-radius-sm` (4px); `999px` (Pill) nur für Chips, Badges, Menü-Icons, Ticker.
„Genauso rund wie die Webanwendung" bedeutet also: normale Buttons 4px, Chip-/Badge-
Elemente pill-förmig.

## Navigation (eingeloggtes Menü, NavMenu.razor)

Reihenfolge: Startseite → Schauspieler → „Quellen" (Sektionskopf, `nav-link disabled`)
→ ein Eintrag je Quelle (Ordner-Icon oder `api/sourceicons/{id}`-Bild) → Konto → Logout.

## Startseiten-Hero (Home.razor + app.css)

```html
<section class="home-hero" style="--home-hero-image: url('/api/pictures/hero-background?…')">
  <div class="home-hero-content">
    <span class="hero-kicker">Private Media Library</span>
    <h1>Videos</h1>
    <p>Filme, Serien, Favoriten und angefangene Wiedergaben …</p>
  </div>
</section>
```

- `GET api/pictures/hero-background` liefert generiertes JPEG (HeroBackgroundGenerator
  aus Weiterschauen-Liste) oder Placeholder-PNG; `CheckLogedIn` → Auth nötig
  (JWT per `access_token`-Query oder Bearer-Header).
- CSS: `min-height: clamp(260px,42vw,520px)`, Vollbreite via negative margins,
  `background-size: cover`, geschichtete Gradienten:
  90deg `rgba(19,19,19,.98)→.76→.18`, 0deg Bottom-Fade zu `--vp-bg`,
  radiale Akzente `rgba(229,9,20,.3)`/`rgba(0,168,232,.22)`, `border-bottom` `--vp-border`.
- Kicker: uppercase-Chip, `radius-sm`, halbtransparent weiß.
- In der Webapp ein-/ausblendbar über Einstellung (positiv formuliert, Standard: an).

## Detailseiten-Header (TVShowDetails.razor + app.css)

```html
<div class="tvshow-header" style="background-image: url('/api/pictures/{backdropId}?…')">
  <button class="favorite-btn">☆</button>
  <div class="tvshow-header-overlay">
    <div class="tvshow-header-content">
      <h1>{Titel}</h1>
      <div class="tvshow-meta">
        <span class="tvshow-year">{Jahr}</span>
        <span class="tvshow-genres">{Genres}</span>
      </div>
      <div class="tvshow-plot">{Plot}</div>
    </div>
  </div>
</div>
```

- `min-height: clamp(360px,52vw,620px)`, `background-size: cover`, Vollbreite.
- Overlay: 90deg-Dunkelgradient + Bottom-Fade (s. o.), Inhalt unten links,
  `max-width: 820px`; Meta in `--vp-primary-soft` bold, Plot in `--vp-text-soft`.
- Staffel-/Episodenseiten nutzen dasselbe Header-Muster (Staffel: Serien-Backdrop;
  Episode: Episoden-Still) — in TVShowDetails.razor/PlaylistDetail.razor prüfbar.
- Darunter: `season-select-bar` (Dropdown „Staffel NN"), `episode-list` mit
  `episode-box`-Zeilen (Poster links, `episode-title` „{Nr} ‧ {Titel}", `episode-plot`).

## Angesehen-Symbol (WatchedIndicator.razor)

- SVG-Auge (24×24, Titel „Gesehen"), Kreis-Badge: `background: rgba(0,0,0,.68)`,
  `border-radius: 999px`, Farbe `--vp-primary-soft`, absolut oben rechts
  (`watched-indicator-image-left`: oben links über dem Poster).
- Gerendert nur wenn `episode.WatchedAt.HasValue`.

## Weitere Web-UI-Details

- Sidebar-Layout (`.sidebar` + `.content`), auf Mobile einklappbar.
- Karten: `media-box`/Rail-Listen mit Postern, Hover-Lift, `radius` 8px.
- `status-ticker` unten (Marquee) für Server-Status — in MAUI nicht nötig,
  aber `status-ticker`-Styling existiert.
- Buttons: `btn-primary` (rot, hover `--vp-primary-hover`), `btn-outline-light`,
  `btn-ghost`; Fokusringe in `--vp-border-strong`.
