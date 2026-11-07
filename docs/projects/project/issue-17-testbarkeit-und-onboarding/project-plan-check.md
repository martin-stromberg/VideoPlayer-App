# Planprüfung — Projektplan Issue #17 (Vier-Augen-Prinzip)

Geprüft: `project-plan.md` gegen `requirement.md`, `inventory.md`, `inventory/*.md` sowie Stichproben im App-Repo (`8e923b89-…`) und Server-Repo (`487c3041-…`).

## Ergebnis

**Freigabe mit Anmerkungen** — Der Plan ist fachlich vollständig, die Fundstellen sind überwiegend exakt verifiziert, alle ENTSCHEIDUNGen sind korrekt in die Schritte überführt. Ein **kritischer Befund** (falsch kalkulierte Windows-Kamera-Unterstützung von ZXing.Net.Maui + irrtümliche `webcam`-Capability-Annahme) wurde in den Plantext eingearbeitet. Die übrigen Befunde wurden als Präzisierungen übernommen.

## Geprüfte Punkte

| Kriterium | Ergebnis |
|-----------|----------|
| Vollständigkeit: alle Akzeptanzkriterien der Requirement in Schritt-Kriterien überführt | ✅ AK1→Schritt 1, AK2→Schritt 2, AK3→Schritt 3, AK4→Schritt 4, AK5→Schritt 5b, AK6→Schritt 5a; alle Nicht-Ziele übernommen |
| `Add-PropertyLine`-Mechanik | ✅ verifiziert: `scripts/iOS-Deployment.ps1` Z. 685–693, `New-ResponseFile` Z. 710–716, Token-Einspeisung Z. 725/884/976, `Add-MaskedDisplay` Z. 695–708 |
| `MvuHost`-API | ✅ `Model` Z. 30, `DispatchAsync` Z. 44–68, `SemaphoreSlim` Z. 15, `ModelChanged`/`PropertyChanged` Z. 33/36 |
| `WindowsPackageType` | ✅ `None` (csproj Z. 23); `OutputType=Exe`, TFM Z. 5 — unpackaged exe direkt startbar |
| `ServerAddress.TryNormalize` | ✅ Z. 19–52, `GetLeftPart(UriPartial.Path)` Z. 50 behält den Pfad → OP-4-Konvention notwendig und korrekt begründet |
| `SilentReloginAsync`-Credential-Abhängigkeit | ✅ `AuthSessionService.cs` Z. 303–351: `HasCredentialsAsync`-Vorprüfung → ohne Credentials `SessionExpired`; `TokenRefresher`-Verdrahtung Z. 289 |
| `PairingService.ExchangeAsync`-Atomarität | ✅ Server-Repo Z. 182–233: `ExecuteUpdateAsync` Z. 217–219, `IssueAsync(deviceName, CreatedByUserId)` Z. 227, AES-GCM `nonce‖ct‖tag` Z. 260–276 |
| Weitere Fundstellen | ✅ `ServerSetupPage.xaml` 113 Zeilen; `UpdateUi` Z. 48–82; `AppUpdate.cs` Z. 479–543 + `EnterConnected` Z. 681; `ExchangePairingCodeAsync` Z. 1213–1288; `AppSecretsService` Z. 89–93/125–149; Contract-Test-Pins; Server: `PairingCode.cs` mit `CreatedByUserId`, `PairingController` öffentlich + IP-Rate-Limit, `AuthorizationTokenService` JWT 12 h ohne Refresh |
| Machbarkeit/Abhängigkeiten | ✅ 2→3 nötig (OP-1 = C), 4→5b begründet, 5a-Vertrag vor 5b festgezurrt; jeder Schritt eigenständig mergebar |
| issue-228-Merge-Prüfung | ✅ als Abhängigkeit in 5a enthalten und berechtigt — Server-Checkout steht tatsächlich auf `task/issue-228-…` |

## Befunde (alle eingearbeitet)

### Befund 1 — ZXing.Net.Maui: Windows-Scan nicht unterstützt; `webcam`-Capability wirkungslos — **kritisch** (behoben)

- ZXing.Net.Maui unterstützt Barcode-*Scanning* auf Windows offiziell nicht; die `webcam`-Capability greift bei `WindowsPackageType=None` nicht.
- **Übernommen:** Schritt 5b formuliert jetzt Kamera-Scan primär iOS/Android; Windows = manueller Ticket-Einstieg; `webcam`-Capability gestrichen; Paketwahl mit Windows-Support-Prüfung im Schritt-Lifecycle.

### Befund 2 — Neue Paketabhängigkeiten ohne Versions-/Lizenz-Notiz — übernommen

- Akzeptanzkriterien von 5a/5b verlangen jetzt: gewähltes Paket, gepinnte Version (≥7 Tage alt), Lizenz, .NET-10-Kompatibilität.

### Befund 3 — E2E-Szenario-Aufsetzung ohne `dispatch` — übernommen

- Schritt 3 enthält jetzt „Fixture-Provisionierung": `VIDEOPLAYER_PROFILE`-Isolation, Backend-Wahl (lokaler Server-Checkout vs. Mock mit `PairingCrypto`-Wiederverwendung), gepaarter Zustand über die echte UI-Kette, Debug-Build-Vorgabe.

### Befund 4 — Refresh-Token-Einhängung appseitig — übernommen

- `AuthSessionService` (`SilentReloginAsync`, `TryStoredLoginAsync`, `LogoutAsync`), Refresh-Client-Methode und Rotations-Ablage sind jetzt explizit in „Betroffene Bereiche" von 5b; Abhängigkeit: Refresh-Endpunkt aus 5a.

### Befund 5 — QR-Adresse als aktiven Server persistieren — übernommen

- 5b-Kriterium ergänzt: QR-Basisadresse normalisieren, Erreichbarkeit prüfen, als aktiven Server persistieren (`PersistActiveServer`-Analogie) vor Exchange + `EnterConnected`.

### Befund 6 — „8-Zeichen-Fallback" vs. „längeres Ticket-Secret" — übernommen

- 5a-Vertrag legt fest: Kurzcode ist Alias/Lookup auf das Ticket; appseitige Vorvalidierung der manuellen Eingabe; Fehlercode-Mapping für beide Varianten.

### Befund 7 — Veraltete Bedingungs-/Empfehlungsformulierungen — übernommen

- Alle „falls/Empfehlung"-Reste auf entschiedenen Stand gezogen; Risikozeilen aktualisiert.

### Befund 8 — `devapi.json`-Ablageort: Plattformscope — übernommen

- Schritt 2 legt fest: Windows-first; auf anderen Plattformen Fallback-Ablageort (`FileSystem.AppDataDirectory`) oder Verzicht.

## Fazit

Der Plan ist belastbar: alle geprüften Fundstellen stimmen mit dem Code überein, die Requirement-Akzeptanzkriterien sind lückenlos abgebildet, die ENTSCHEIDUNGen sind konsistent eingearbeitet und die Schritt-Abhängigkeiten korrekt. Alle acht Befunde wurden in `project-plan.md` eingearbeitet.
