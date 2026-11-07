# Abnahme Schritt 2 — Developer-API für UI-Automatisierung

Schrittbranch: `task/issue-17-developer-api` → gemergt in `728afe3`.
Lifecycle: vollständig durchlaufen (`docs/features/task/issue-17-developer-api/` — Artefakte im Planungscommit `b58319b`, Feature-Verzeichnis danach gemäß Konvention gelöscht).

## Ergebnis

Lokaler Mini-HTTP-Server (`TcpListener` Loopback:0, keine Paketabhängigkeit) in der Windows-App — nur Debug/Opt-in (`#if DEBUG || DEV_API` komplett um alle DevApi-Dateien + Verdrahtung), Release enthält die API nicht.

- **Endpunkte (exakt die 4 vereinbarten):** `GET /model` (AppModel-Snapshot, keine Secrets), `GET /tree` (logischer Elementbaum mit AutomationId/Typ/IsVisible/Bounds/Text + **CollectionView-Items aus `ItemsSource`** — schließt die UIA-Lücke), `POST /invoke` (Clicked-/Toggle-/Command-Äquivalent per AutomationId), `POST /set-text` (`Entry.Text` + `TextChanged`). **Kein `dispatch`** — verifiziert (404).
- **Metadaten:** `devapi.json` (Port/PID/Zeit) in `AppContext.BaseDirectory`, Fallback `FileSystem.AppDataDirectory`.
- **Struktur:** Protokoll-/Routing-Kern in `VideoPlayer.Mvu/DevApi/` (testbar ohne MAUI), MAUI-Teile (Visual Tree, Service) in `VideoPlayer.Maui/DevApi/`; UI-Zugriffe über `MainThread`.

## Live-Verifikation (echte Debug-App)

- `devapi.json`: `{"port":50146,"pid":10960}`; `GET /model` → korrekter Snapshot; `GET /tree` → Baum inkl. `items`.
- `POST /set-text` → Text in `/tree` sichtbar; `POST /invoke ConnectButton` → **echte UI-Kette** lief (`isServerCheckRunning` → `serverSetupError`).
- Fehlerfälle: `/dispatch` → 404, `POST /model` → 405, unbekannte ID → 404, Label-Invoke → 400.

## Akzeptanzkriterien

1. ✅ `GET /model` liefert Modellfelder, keine Tokens/Passwörter.
2. ✅ `GET /tree` inkl. CollectionView-Items.
3. ✅ `invoke`/`set-text` per AutomationId auf UI-Thread.
4. ✅ Loopback-only, Port 0, `devapi.json`, Release-Ausschluss compile-time (6 Contract-Tests pinnen `#if`-Struktur/4 Routen/kein dispatch/Loopback/MainThread).
5. ✅ Kein `dispatch`-Endpunkt.

**926/926 Tests grün** (30 neue). Kein Restnachweis offen.

## Commits

- `b58319b` plan: Developer-API fuer UI-Automatisierung
- `ab81c6f` feat: Developer-API fuer UI-Automatisierung (Debug/Opt-in)
- `728afe3` merge: Schritt 2 in Projektbranch
