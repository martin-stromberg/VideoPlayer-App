# Umsetzungsplan — Schritt 10: Schlanker Einstellungsbereich (tvOS)

## Ansatz

Die gesamte Verdrahtung existiert bereits: `AppSettings`/`AppSettingsService` (Normalisierung inklusive), `NSUserDefaultsSettingsStore`, die MVU-Messages (`LoadSettingsRequestedMessage`/`SettingsLoadedMessage`/`SaveSettingsRequestedMessage`) samt Update-Handlern und die tvOS-Effekthandler (`LoadSettingsEffect`/`PersistSettingsEffect`). Es ist **ausschließlich UI-Arbeit** in `VideoPlayer.TvOS/Ui/SettingsPageViewController.cs` nötig — keine Änderungen an `VideoPlayer.Mvu`, `VideoPlayer.Storage` oder den Tests.

## 1. `VideoPlayer.TvOS/Ui/SettingsPageViewController.cs` (einzige Code-Datei)

### 1.1 Zustand/Render-Robustheit

- Feld `private AppSettings settings = new();` (+ `lastModel`-/`IsViewLoaded`-Guard nach Muster `DownloadsPageViewController`, damit `Render` vor `ViewDidLoad` nicht auf `null`-Labels läuft — Bestandscode dereferenziert bisher ungeschützt).
- `ViewDidAppear` → `LoadSettingsRequestedMessage` dispatchen (Seitenaufruf lädt den persistierten Stand erneut — Muster `DownloadsPageOpenedMessage`).
- `Render(model)`: `settings = model.Settings`; Button-Titel/Labels aus `model` befüllen.

### 1.2 Abschnitt „Downloads" (neu)

Zwei fokussierbare Wert-Buttons (`TvosControls.Button`), Titel zeigt aktuellen Wert; Auswahl öffnet `UIAlertController` (`UIAlertControllerStyle.ActionSheet`) mit Vorgabewerten + „Zurück" (Cancel) — Muster `DownloadsPageViewController.ShowItemActions`:

- `retentionButton` — Titel `Aufbewahrungszeit für Downloads: {n} Tag(e)`; Werteliste {1, 3, 7, 14, 30, 60, 90, 180, 365} Tage
- `quotaButton` — Titel `Speicherplatzkontingent: {n} GB`; Werteliste {1, 2, 5, 10, 25, 50, 100, 250, 500} GB

Hilfsmethode `ShowValuePicker(title, options, formatValue, apply)`:
- Liegt der aktuelle Wert außerhalb der Vorgaben (z. B. von Hand/älterem Stand), wird er sortiert in die Liste aufgenommen — kein Werteverlust.
- Auswahl dispatcht `SaveSettingsRequestedMessage(settings with { … })` — Update normalisiert und persistiert über `PersistSettingsEffect`.

### 1.3 Abschnitt „Wiedergabe" (neu)

- `autoResumeButton` — Titel `Auto-Fortsetzung: Ein|Aus`; Auswahl toggelt → `SaveSettingsRequestedMessage(settings with { AutoResumeEnabled = !settings.AutoResumeEnabled })`.
- `endWindowButton` — Titel `„Angesehen“-Endfenster: {Anzeige}`; Werteliste {30, 60, 120, 300, 600, 900, 1800, 3600} s, Anzeige als „n Sekunden" bzw. „n Minuten" bei glatter Minute; derselbe `ShowValuePicker`-Mechanismus.

### 1.4 Abschnitt „Konto" (neu, erlaubter Zusatz)

- `accountLabel` (`TvosControls.Body`): `Angemeldet als: {model.LoginUsername}` — bei leerem Benutzernamen `—`.
- `logoutButton` — „Abmelden" → `LogoutRequestedMessage`; `Enabled`-Guard `model.ConnectionState is Connected or Offline` (Muster `ProfilePageViewController`).

### 1.5 Abschnitt „Server" (Bestand)

Unverändert: Serveradresse, Serverstatus (`ServerHealthText`/`Offline`-Text wie Profilseite), „Server wechseln" → `NavigateToMessage(AppPage.ServerSetup)`.

### 1.6 Layout

Bestehender vertikaler `UIStackView` (leading 90, top 160, width 1100) bleibt; neue Abschnitte werden als `TvosControls.Section(Heading, …)` angehängt — Reihenfolge: Konto, Downloads, Wiedergabe, Server (Server bleibt wie bisher am Ende, analog MAUI-Seite). Ausgeschlossene Einstellungen (Auto-Download/Vorlauf, nach-Abspielen-löschen, Nur-WLAN, Protokollversand, Logging) werden **nicht** gerendert.

Klassenkommentar aktualisieren (vier Einstellungen, Wertelisten, Konto/Abmeldung, bewusste Ausschlüsse).

### 1.7 tvOS-Kompilierfallen

- `UIAlertController.Create(title, message, style)` + `UIAlertAction.Create(title, style, action => …)` — Signaturen exakt wie in `DownloadsPageViewController` bewiesen.
- Kein `const nfloat`, kein `SeparatorStyle`, kein `IndexOf` auf `IReadOnlyList`, Named-Args korrekt, `UIAction`-Parameter nicht `_`.

## 2. Tests

- Keine neuen plattformneutralen Bausteine → keine neuen Tests erforderlich; `dotnet test VideoPlayer.Maui.Tests` muss grün bleiben (Normalisierung/Persistenz bereits durch `AppSettingsTests`, `AppSettingsServiceTests`, `AppUpdateTests_Settings` abgedeckt).

## 3. Dokumentation

- `docs/help/tv-app/beschreibung.md`: Absatz zum Einstellungsbereich (vier Werte, Wertelisten, Konto/Abmeldung, Ausschlüsse); Satz „Weitere Einstellungsbereiche folgen …" entfernen/anpassen.
- `docs/help/tv-app/ablauf-anwender.md`: Schritt „Einstellungen anpassen" ergänzen; Erwähnung „„Server wechseln" (Einstellungen)" bleibt gültig, „Abmelden" jetzt zusätzlich unter Einstellungen.
- `README.md`: tvOS-Zeile um Einstellungsbereich ergänzen.
- `docs/help/index.md`: tvOS-Zusammenfassung ggf. kurz ergänzen.

## 4. Abschluss

- Commits: Planungscommit → Implementierungscommit (`feat:`) → Push; `build-tvos` (Run via `gh run watch --exit-status`) muss grün sein, Run-ID nennen.

## Offene Punkte

Keine.
