# Planprüfung – QR-/Ticket-Onboarding (Schritt 5b)

Geprüft: `plan.md` gegen `requirement.md`, Server-Vertrag (`acceptance-schritt-5a.md`) und Inventory.

## Checkliste

| # | Prüfpunkt | Ergebnis |
|---|-----------|----------|
| 1 | QR-URL-Parser: http(s) only, `/pairing`-Suffix abschneiden, Unterpfad erhalten, `t=` opak, kein `PairingCode.TryNormalize` | ✅ Plan §1 – deckt alle Ablehnungsfälle ab |
| 2 | Kurzcode direkt an `bootstrap` gegen aktiven Server | ✅ Plan §5 – `ShortCode` ohne URL-Prüfung |
| 3 | Reihenfolge: Check → Persist (aktiv + KnownServers) → Bootstrap → Session → Home | ✅ `CheckBootstrapServerEffect` → `PersistActiveServerEffect` → `ExchangeBootstrapTicketEffect` → `AttemptAutoLoginEffect` (sequenziell, bewährtes Muster aus `UpdateServerCheckCompleted`) |
| 4 | Krypto identisch zu `exchange` | ✅ `PairingCrypto.DecryptToken` wiederverwendet (Klartext-String → JSON-Payload) |
| 5 | Fehler-Mapping 400/401/429/404/Transport | ✅ `BootstrapStatus` vollständig; 400→`InvalidRequest` (fachlich getrennt von 401), Texte verständlich |
| 6 | Refresh vor Credential-Fallback + Rotation persistieren + Refresh-401→Fallback | ✅ Plan §4 – Gate-Probe trennt Gerätewiderruf von ungültigem Refresh-Token (`api/auth/refresh` ist MauiOnly-gated) |
| 7 | Logout: `api/auth/logout` + Refresh-Token löschen | ✅ best-effort Revoke vor `ClearCredentialsAsync` |
| 8 | Scan nur iOS/Android; Windows manuell; Version ≥7 Tage alt + Lizenz + TFM dokumentiert | ✅ ZXing.Net.Maui.Controls 0.10.4 (MIT, 14.08.2026), TFM-bedingte Reference, `#if ANDROID || IOS` |
| 9 | UI: neue Sektion, Loading/Fehler, Fallbacks erhalten, `PairingCodeEntry` ohne `MaxLength` | ✅ `BootstrapSection` immer sichtbar (nötig, da URL den Server mitbringt); bestehende Karten unverändert |
| 10 | Dev-API secret-frei | ✅ nur `IsBootstrapRunning`/`BootstrapError` (Status/Fehlertext) |
| 11 | Kein `ApplicationVersion`-/`.ios-deploy.user.json`-Eingriff | ✅ nicht im Plan |
| 12 | Nur App-Repo | ✅ Server nur gelesen/Scratch-Lauf |

## Befunde (eingearbeitet)

1. **Kurzcode-Modus im Parser**: ursprüngliche Ablehnungsregel „Whitespace/:`" präzisiert — der Server entscheidet 401 bei ungültigem Code; der Parser lehnt nur Leere/URL-artige Sonderfälle ab (einfacher, verständlicher Fehlerweg).
2. **Session-Persistenz bei Stale-Kontext**: verfeinert — JWT/Refresh-Token werden nur persistiert, wenn der Bootstrap-Kontext noch aktuell ist (Serverwechsel mitten im Exchange darf keine Session für den falschen Server einspeichern); Geräte-Token bleibt wie beim Pairing adressspezifisch hinterlegt.
3. **Permission-Dialog**: Verweigerte Kamera-Permission löst keinen Model-Fehler aus, sondern einen lokalen `DisplayAlert` — der Fluss wurde noch nicht gestartet (UI-lokale Ausnahme, im Plan begründet).
4. **`BootstrapServerCheckedMessage` trägt `Ticket` mit**: nötig, damit der Erfolgs-Übergang den `ExchangeBootstrapTicketEffect` ohne Zwischenspeicherung des Tickets im Model (secret-adjacent) auslösen kann.
5. **Login stellt keinen Refresh-Token aus** (verifiziert: `AuthorizationToken { token, expires }` im Server) → `LoginCoreAsync` bleibt unverändert; Refresh-Token entsteht ausschließlich via Bootstrap.

## Restrisiken (akzeptiert)

- ZXing auf `net10.0-maccatalyst` nicht referenziert (Anforderung = iOS/Android); Scan-Code `#if ANDROID || IOS`.
- Echte Kamera-Läufe iOS/Android = manuelle Restnachweise (kein Gerät im Testlauf).
- Live-Lauf gegen unmerged Server-Branch nur mit Scratch-DB.
