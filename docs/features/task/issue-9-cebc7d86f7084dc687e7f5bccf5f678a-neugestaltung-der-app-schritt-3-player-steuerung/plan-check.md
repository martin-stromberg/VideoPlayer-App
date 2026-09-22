# Plan-Gegenprüfung — Steuerelemente in der Vollansicht bedienbar (Schritt 3)

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Play/Pause und Seek in der Vollansicht unter Windows sofort bedienbar | `OverlayChrome` schrumpft auf `VerticalOptions="Start"` → verifizierte Hit-Test-Blockade (Container mit Default `Fill` über dem `MediaElement`) entfällt; zusätzlich `InputTransparent` auf dem nicht-interaktiven Kopfband | `PlayerContractTests.PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface` pinnt `VerticalOptions="Start"` und das `InputTransparent`-Muster; Negativprobe auf Ausgangsstand vorgesehen | Abgedeckt |
| Zurück- und Mini-Player-Schalter der App funktionieren weiterhin | `PipButton` bleibt eingabefähig (`InputTransparent="False"`, `Clicked="OnPipClicked"` gepinnt); Zurück-Navigation kommt von der Shell (unberührt) | Derselbe Contract-Test pinnt `Clicked="OnPipClicked"`; `PlayerContractTests.EffectHandler_HandlesPlayerEffects` + `PlayerPage_RouteRegistered` bleiben grün | Abgedeckt |
| Mini-Player bleibt unverändert nutzbar | `OverlayChrome.IsVisible = !IsPictureInPicture` im Code-Behind unverändert; `PictureInPictureService`/`TogglePictureInPictureEffect` unberührt | `PlayerContractTests.PictureInPictureService_UsesCompactOverlayOnWindows`, `AppUpdateTests_Player` bleiben grün | Abgedeckt |
| Alle bestehenden Tests grün; Build Debug+Release fehlerfrei | Umsetzungsreihenfolge Schritt 3 | Voller `dotnet test`-Lauf + beide Build-Konfigurationen | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Transportsteuerung in der Vollansicht bedienen | Contract-Pin `PlayerPage_OverlayChromeDoesNotCoverPlaybackSurface` | Nicht erforderlich mit Begründung: Keine UI-/E2E-Automatisierung im Repository vorhanden (dokumentierte Projektkonvention); die native Transportsteuerung ist Betriebssystem-UI und quelltextseitig nicht adressierbar. Der Contract-Test pinnt die verursachende Struktur; Endabnahme auf dem Gerät. |

## Hinweise

- Der Plan verlangt die Negativprobe (Contract-Test zuerst rot auf dem
  Ausgangsstand) — dadurch ist nachgewiesen, dass der Pin die
  tatsächliche Blockade-Struktur erfasst.
- `InputTransparent` wird in MAUI auf Kinder vererbt; die explizite
  `InputTransparent="False"`-Ausnahme am `PipButton` ist notwendig,
  nicht optional.
- Keine neuen NuGet-Pakete, Enums, Tabellen oder Migrationen →
  Hook-/DoD-Zusatzpflichten entfallen.
