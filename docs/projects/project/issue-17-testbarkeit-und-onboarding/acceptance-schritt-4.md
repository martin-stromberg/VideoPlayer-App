# Abnahme Schritt 4 — Serververbindungs-UI modernisieren

Schrittbranch: `task/issue-17-server-setup-ui` → gemergt in `9c6af1e`.
Lifecycle-Artefakte: `docs/features/task/issue-17-server-setup-ui/` (im Repo erhalten, Konvention wie Schritt 3/Issue #15).

## Ergebnis

`ServerSetupPage.xaml` komplett restrukturiert (+395/−137 über die XAML) mit `Vp*`-Tokens, Dark-only:

- **Kopfbereich:** Kicker-Badge „SERVERVERBINDUNG", `ActiveServerCard` mit Pill-Status-Badge (`ServerHealthDot` + `DataTrigger` auf `Model.ServerHealth`, `ServerHealthLabel` aus `ServerHealthText` — das bisher ungenutzte Modell-Feld ist jetzt sichtbar verdrahtet).
- **Server-Karten:** `DataTemplate`s für bekannte und gefundene Server (Border + RoundRectangle, Name/Adresse/„Zuletzt verwendet" + „Auswählen"-Aktion).
- **`ConnectCard`** für manuelle Eingabe; **hervorgehobene `PairingSection`** mit „NÄCHSTER SCHRITT"-Kicker und Führungstext (Profilseite „Geräte" → Einmal-Code — Vorbereitung für Schritt 5b ohne dessen Funktion).
- **MVU unverändert:** `BindingContext = mvuHost` (PropertyChanged-Trigger), alle Dispatch-Handler und `serversetup`-Route unverändert; `UpdateUi` nur um Status-Zuweisungen erweitert.
- **Contract-Pins:** bewusst um 7 neue AutomationIds erweitert (im Test begründet); `PairingCodeEntry` weiterhin ohne `MaxLength`; `PairingContractTests` unverändert grün.

## Verifikation

- **927/927** `VideoWebPlayer.Maui.Tests` (Baseline 926 + `ServerSetupPage_UsesCardLayout`).
- **11/11** `VideoPlayer.E2E.Tests` (~53 s; Baseline 10 + `ReachableServer_ShowsHealthBadgeAndGuidedPairing` — E2E-Infrastruktur aus Schritt 3 hat den Umbau direkt abgesichert).
- **Visueller Nachweis:** drei Screenshots der echten App in `live-verification/` (Erststart, Pairing-Zustand mit geführter Karte + Status-Pill, bekannte Server als Karten); Pixelanalyse Dark-only.
- Reviews: Plan/Usability/Code ohne Befunde.

## Akzeptanzkriterien

1. ✅ Visuelle Hierarchie: Kopf + aktiver Server, Karten statt Textliste, hervorgehobene Pairing-Führung.
2. ✅ Zustandsfeedback explizit: Status-Pill „Server: erreichbar" via `ServerHealth`/`ServerHealthText`, Check-Indicator, `ErrorLabel`.
3. ✅ MVU-Fluss unverändert (gleiche Messages, Route ohne FlyoutItem).
4. ✅ Gepinnte AutomationIds erhalten (Erweiterung bewusst + kommentiert), `Vp*`-Tokens, Dark-only.

## Commits

- `f983df2` plan, `68b5c2f` feat (19 Dateien), Merge `9c6af1e`.
