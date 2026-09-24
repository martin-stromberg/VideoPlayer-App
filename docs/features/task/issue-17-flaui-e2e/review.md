# Plan-Review — FlaUI-E2E-Tests (Issue 17, Schritt 3)

Geprüft: Umsetzung gegen `plan.md` und `requirement.md` (2026-09-24).

## Abdeckung der Anforderungen

| Anforderung | Umsetzung | Status |
|-------------|-----------|--------|
| Neues Projekt `VideoPlayer.E2E.Tests` in der Solution | Projekt angelegt (`net10.0-windows`, xUnit v3 + FlaUI.UIA3 5.0.0 vom 2025-02-25, deutlich älter als 7 Tage), per `dotnet sln add` eingebunden | erfüllt |
| Echter Prozessstart der Debug-Exe | `AppFixture` baut `VideoPlayer.Maui` einmalig (`-f net10.0-windows10.0.19041.0 -c Debug`), `AppInstance` startet den Prozess mit eigenem Environment | erfüllt |
| `devapi.json`-Portdiscovery | `WaitForDevApiPortAsync` pollt die Datei und prüft die eigene PID | erfüllt |
| Profil-Isolation je Instanz | `VIDEOPLAYER_PROFILE=e2e-<test>-<guid>`; ergänzend wurde `ClientGate.TokenFileDirectory` profil-isoliert — ohne die Erweiterung hätten E2E-Instanzen den realen Gate-Schlüssel des Entwicklerprofils geerbt (Fund im ersten Testlauf) | erfüllt |
| Prozess-Cleanup in `finally`/`Dispose` | `CloseMainWindow` → `Kill(entireProcessTree)` nach 10 s, `devapi.json` + Profilverzeichnisse unter `%APPDATA%` und `%LOCALAPPDATA%` werden gelöscht; Nachlauf-Check ohne Reste | erfüllt |
| FlaUI für echte UI-Aktionen | `UiDriver`: `EnterText` (Value-Pattern/Typing), `ClickButton` (Invoke), `OpenFlyoutItem` (SelectionItem), `FindByAutomationId`, `Exists` | erfüllt |
| Developer-API für Modell-Assertions | `WaitForModelAsync` mit Bedingung + Timeout + Letztstand im Fehlertext | erfüllt |
| Keine Koordinatenklicks für CollectionView | `/invoke` um `itemIndex` erweitert (`DevApiService`, `DevApiVisualTree.TrySelectItem` über `SelectedItem` → echtes `SelectionChanged`); Listen lesen über `GET /tree` (`items`) | erfüllt |
| Server-Setup-Szenario | 4 Tests: Erststart, ungültige Adresse, nicht erreichbarer Server, erreichbarer Server → Pairing einfordern | erfüllt |
| Pairing-Szenario | 3 Tests: Formatfehler (lokal, kein Serverkontakt), falscher Code (Server-401), gültiger Code → Anmeldemaske | erfüllt |
| Login-Szenario | 2 Tests: falsches Passwort → Inline-Fehler, gültige Anmeldung → Startseite | erfüllt |
| Serverwechsel-Szenario (#15) | 1 Test: A einrichten → über Einstellungen auf B wechseln (Pairing + Auto-Login) → A per `KnownServersView` + `itemIndex` wählen → Startseite A | erfüllt |
| Backend-Entscheidung dokumentiert | Hermetischer `MockVideoServer` im Testprojekt (deterministisch, kein Checkout nötig); Real-Server-Option in `inventory/backend-optionen.md` dokumentiert | erfüllt |
| CI optional dokumentiert | Lokaler Lauf verpflichtend (durchgeführt); CI-Ausführung als optional dokumentiert (kein zuverlässiger interaktiver Desktop auf gehosteten Runnern) | erfüllt |

## Abweichungen vom Plan

1. **`ClientGate`-Erweiterung** (nicht im Plan): die statische
   Token-Datei war profil-übergreifend — die Isolation gehört
   konsequenterweise zum Profilmodell und war für den Pairing-Pfad
   zwingend nötig. Produktiv unverändert (Standardprofil liest weiter
   `%LocalAppData%/VideoPlayer`).
2. **`/invoke` um `itemIndex`** — bereits im Plan vorgesehene
   Erweiterung, umgesetzt wie geplant.
3. **UIA-Realitäten**: Flyout-Umschalter lokalisiert
   („Navigation öffnen", AutomationId `OK`), Menüeinträge als
   `ListItem`/`SelectionItem`, UIA-Baum hinkt dem Modell hinterher —
   im `UiDriver` gelöst, ohne Koordinatenklicks.
4. **Shell-Tab-Stack-Befund** (Fund, kein Planbruch): nach dem
   Auto-Login bleibt `serversetup` als geschobene Route im
   Einstellungen-Stack — die Menüauswahl „Einstellungen" kann direkt
   auf der Einrichtungsseite landen. Test toleriert beide Landezustände;
   Bewertung in `review-code.md`.

## Fazit

Alle Anforderungen sind umgesetzt und durch echte Testläufe belegt
(10/10 E2E, 926/926 Bestandstests). Die Abweichungen sind dokumentiert
und fachlich begründet; kein verbleibender Planabzug.
