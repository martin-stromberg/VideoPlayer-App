# Plan-Review — Modernisierung der Serververbindungs-UI (Issue 17, Schritt 4)

Geprüft: Umsetzung gegen `plan.md` und `requirement.md` (2026-09-24).

## Abdeckung der Akzeptanzkriterien

| Kriterium | Umsetzung | Status |
|-----------|-----------|--------|
| AK1: Kopfbereich mit aktivem Server | Kicker-Badge „SERVERVERBINDUNG", Titel „VideoPlayer-Server", Hinweissatz und `ActiveServerCard` (`Border`, `RoundRectangle 8`, `VpSurfaceHigh`/`VpBorderStrong`) mit Adresse + Status-Badge — `ServerSetupPage.xaml` Z. 14–115 | erfüllt |
| AK1: Bekannte/gefundene Server als Karten | Beide `CollectionView`-`DataTemplate`s sind `Border`-Karten (`RoundRectangle 8`, `VpSurface`/`VpBorder`) mit `NameOrAddress` fett, `Address` muted, Statuszeile („Zuletzt verwendet …" bzw. „Im Netzwerk gefunden") und „Auswählen"-Hinweis — Z. 128–158, 189–219 | erfüllt |
| AK1: Pairing als hervorgehobene, geführte Karte | `PairingSection`: `RoundRectangle 8`, `Stroke=VpSecondary`, `VpSurfaceHigh`, „NÄCHSTER SCHRITT"-Badge, Führungstext zur Server-Verwaltung/Profilseite „Geräte" — Z. 272–322 | erfüllt |
| AK2: „Prüfung läuft" explizit sichtbar | `CheckStatusLabel` („Verbindung wird geprüft …"/„Verbindung wird hergestellt …") + `CheckIndicator`, `DiscoveryRunningLabel` („Suche läuft …") + `DiscoveryIndicator`, `PairingRunningLabel` („Kopplung läuft …") + `PairingIndicator` — `.xaml.cs` Z. 69–95 | erfüllt |
| AK2: „erreichbar"/„nicht erreichbar" sichtbar | Status-Badge der Aktiver-Server-Karte: `ServerHealthLabel` zeigt `model.ServerHealthText`, `ServerHealthDot` färbt per `DataTrigger` (`VpSecondary`/`VpDanger`/`VpTextMuted`), `ErrorLabel` bleibt für Detailfehler — Z. 50–112, `.xaml.cs` Z. 56–61 | erfüllt |
| AK3: MVU-Fluss unverändert | `UpdateUi(AppModel)` bleibt die einzige UI-Quelle; alle fünf Dispatch-Handler unverändert (`ServerDiscoveryRequested`, `ServerAddressSubmitted`, `DiscoveredServerSelected`, `ServerSwitchRequested`, `PairingCodeSubmitted`); Route `serversetup` weiterhin ohne `FlyoutItem` (Contract-Test grün) | erfüllt |
| AK4: Pins respektiert | Alle bisherigen AutomationIds erhalten; neue Ids bewusst in `ServerSetupContractTests` ergänzt (Kommentar begründet die Erweiterung); `PairingCodeEntry` ohne `MaxLength`, Spellcheck/TextPrediction aus; nur `Vp*`-Tokens (Pin: kein `#` im XAML); Dark-only | erfüllt |

## Planpunkte im Einzelnen

- **Schritt 1 (XAML-Restrukturierung):** umgesetzt wie geplant —
  Grundgerüst `Grid Auto,*,Auto`, Kopf direkt sichtbar, Listen in
  `ScrollView`, Fuß in eigener `ScrollView`. Abweichung zum Plan:
  `x:Name="Page"` entfiel wie geplant (Bindings laufen über
  `BindingContext = mvuHost`).
- **Schritt 2 (Code-Behind):** `BindingContext = mvuHost` im
  Konstruktor; `UpdateUi` um die sechs neuen Status-Zuweisungen
  erweitert (`ServerHealthLabel/-Indicator/-Dot`,
  `CheckStatusLabel`, `DiscoveryRunningLabel`, `PairingRunningLabel`).
  Alle Dispatch-Handler bytegleich.
- **Schritt 3 (Contract-Tests):** `ServerSetupPage_ControlsHaveAutomationIds`
  um sieben neue Ids erweitert; neuer Test
  `ServerSetupPage_UsesCardLayout` pinnt `RoundRectangle 8`,
  `ServerHealthLabel`, `RoundRectangle 999` und das `#`-Verbot.
- **Schritt 4 (E2E):** `FreshStart_ShowsServerSetupPage` prüft
  zusätzlich `ConnectCard` über `/tree`; neuer Test
  `ReachableServer_ShowsHealthBadgeAndGuidedPairing` verifiziert
  Badge-Text „Server: erreichbar", `ActiveServerCard`-Sichtbarkeit,
  Pairing-Karte mit „Verwaltung"/„Gerät koppeln" und bedienbare
  `PairingCodeEntry`/`PairButton`.
- **Schritt 5 (Build + Tests + Screenshots):** Build 0 Warnungen/
  0 Fehler; `VideoWebPlayer.Maui.Tests` 927/927;
  `VideoPlayer.E2E.Tests` 11/11; drei Screenshots in
  `live-verification/` (Erststart, Pairing-Zustand, bekannte Server).

## Abweichungen vom Plan

Keine fachlichen Abweichungen. Kleinigkeit beim Verifikationslauf:
das Pairing-Alphabet schließt O/I/L/0/1 aus — ein erster
Screenshot-Versuch mit Code „SHOT42" zeigte korrekt den
Format-Fehler (verworfen, Wiederholung mit „ABCD23").

## Fazit

Plan vollständig umgesetzt, alle Akzeptanzkriterien erfüllt, alle
Pins eingehalten bzw. bewusst erweitert. Kein Handlungsbedarf.
