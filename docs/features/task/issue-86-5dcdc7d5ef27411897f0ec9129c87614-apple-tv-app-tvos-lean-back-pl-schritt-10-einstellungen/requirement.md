# Anforderung (übersetzt) — Schritt 10: Schlanker Einstellungsbereich

Quelle: `docs/projects/task/issue-86-.../project-plan.md`, Schritt 10 (wörtlich übernommen, verdichtet strukturiert).

## Ziel

Die tvOS-App erhält einen schlanken, fernbedienungstauglichen Einstellungsbereich, der ausschließlich die in der Anforderung genannten Werte über die bestehenden plattformneutralen Einstellungsdienste (`AppSettings`/`AppSettingsService`) und den tvOS-Einstellungsadapter (`NSUserDefaultsSettingsStore`) verwaltet.

## Umfang

1. **Pflicht-Einstellungen** (sichtbar, per Fernbedienung änderbar, persistent über App-Neustarts):
   - `DownloadRetentionDays` — Aufbewahrungszeit für Downloads (Normalisierung 1–365)
   - `DownloadQuotaGb` — Download-Quota/Speicherplatzkontingent (Normalisierung 1–500)
   - `AutoResumeEnabled` — Auto-Fortsetzung an/aus
   - `WatchedEndWindowSeconds` — „angesehen"-Endfenster (Normalisierung 30–3600)
2. **Ergänzend erlaubt** (kein Muss): Anzeige-/Kontoinformationen — aktiver Server, angemeldetes Profil, Abmeldung — soweit der bestehende Abmeldefluss (`LogoutRequestedMessage`/`LogoutEffect`) genutzt wird.

## Ausschlüsse (NICHT exponieren)

- Auto-Download und Vorlauf (`AutoDownloadEnabled`, `PreloadEpisodeCount`) — auf tvOS nicht verdrahtet
- „Downloads nach dem Abspielen löschen" (`AutoDeletePlayedDownloads`, `DeleteAfterPlaybackDelayMinutes`) — nicht im Scope
- Nur-WLAN-Downloads (`WifiOnlyDownloads`) — Schalter entfällt (tvOS typischerweise Ethernet/WLAN)
- Protokollversand — tvOS hat keinen Mail-Composer; lokale Protokolldatei über den bestehenden Datei-Logger bleibt möglich

## Akzeptanzkriterien

- Alle vier Pflicht-Einstellungen sind sichtbar, per Fernbedienung änderbar und persistieren über App-Neustarts
- Änderungen wirken auf das jeweilige Verhalten: Aufbewahrungszeit/Quota auf Downloads, Auto-Fortsetzung und Endfenster auf die Wiedergabe
- Die ausgeschlossenen Einstellungen erscheinen nicht in der Oberfläche
- Wertebegrenzungen der plattformneutralen Normalisierung (`AppSettings.Normalize`) greifen
- tvOS-Lauffähigkeit über den macOS-CI-Build (`build-tvos`) und Simulator

## Abhängigkeiten / Rahmen

- Abhängig von Schritt 7 (Wiedergabe) und Schritt 9 (Downloads) — beide umgesetzt
- `dotnet test VideoPlayer.Maui.Tests` bleibt grün; kein Schreibzugriff auf Inhalte
- tvOS lokal (Windows) nicht kompilierbar → Push autorisiert, Abschluss erst bei grünem `build-tvos`
