# Abnahmeprüfung – Entwicklungsschritt 10

## Ergebnis

**Status:** Anforderung erfüllt – externe Verifikation ausstehend

## Abweichungen

Keine.

## Ausstehende externe Verifikationen

- **Laufzeitverhalten auf tvOS-Simulator/-Gerät:** Tatsächliche
  Fokus-Navigation und Wertänderung per Siri Remote (Buttons,
  `UIAlertController`-Auswahllisten), das sichtbare Persistieren der
  vier Einstellungen über einen echten App-Neustart hinweg sowie die
  wirksame Abmeldung aus dem Einstellungsbereich sind auf Code-Ebene,
  über plattformneutrale Tests und den macOS-CI-Kompiliernachweis
  verifiziert — nicht per Laufzeitnachweis. Fällt unter den
  Register-Eintrag „2–laufend" in `pending-verification.md`
  (App-Start/Runtime-Verhalten auf tvOS-Simulator jenseits der
  Kompilierung).
- Der macOS-CI-Nachweis selbst ist erbracht: Run `42138913426`
  (`verify` 5m57s + `build-tvos` 7m41s, `conclusion: success`) auf
  `headSha` `7511fef9e20d3eac7ff2426e238a6b92f04e8c40` = HEAD.
  HEAD = `origin/<branch>` (`git log origin/<branch>..HEAD` leer),
  Arbeitsbaum sauber.

## Hinweise

Prüfung anhand des Diffs
`task/issue-86-...-apple-tv-app-tvos-lean-back-pl...HEAD` (12 Dateien,
+466/−14; im Code ausschließlich `VideoPlayer.TvOS/Ui/
SettingsPageViewController.cs`, dazu Lifecycle-Artefakte und
Doku-Updates in `README.md`/`docs/help/`) und Lesen der Quelldateien
samt der plattformneutralen Gegenseiten (`AppSettings`,
`AppSettingsService`, `AppUpdate`, tvOS-Effekt-Handler, Komposition).

**(a) Vier Pflicht-Einstellungen — vorhanden, fernbedienbar änderbar,
persistierend:** `SettingsPageViewController.cs` exponiert genau die
vier geforderten Werte — Aufbewahrungszeit
(`retentionButton`, Z. 79–86 → `DownloadRetentionDays`, Optionen
1–365 Tage), Quota (`quotaButton`, Z. 87–94 → `DownloadQuotaGb`,
Optionen 1–500 GB), Auto-Fortsetzung (`autoResumeButton`, Z. 100–103
→ `AutoResumeEnabled`, Ein/Aus-Toggle) und Endfenster
(`endWindowButton`, Z. 104–111 → `WatchedEndWindowSeconds`, Optionen
30–3600 s). Alle Zeilen sind `TvosControls.Button`
(System-`UIButton` via `PrimaryActionTriggered`, `TvosControls.cs`
Z. 98–105) — per Focus-Engine mit der Siri Remote erreichbar und
auslösbar. Numerische Werte laufen über `ShowValuePicker`
(Z. 208–234) als `UIAlertControllerStyle.ActionSheet`-Auswahlliste —
fernbedienungstauglich ohne Tastatureingabe; ein Ist-Wert außerhalb
der Vorgabeliste wird sortiert mit angeboten (kein Werteverlust).
Persistenz-Weg: `SaveSettings` (Z. 201–202) dispatcht
`SaveSettingsRequestedMessage` → `UpdateSaveSettingsRequested`
(`AppUpdate.cs` Z. 355–373) → `PersistSettingsEffect` →
`TvosAppEffectHandler` Z. 106–107/361–372 → `AppSettingsService.Save`
→ `NSUserDefaultsSettingsStore` auf
`NSUserDefaults.StandardUserDefaults` (`AppComposition.cs` Z. 25/30)
— neustartfest. Beim Seitenaufruf lädt `ViewDidAppear`
(Z. 142–148) via `LoadSettingsRequestedMessage` den persistierten
Stand erneut (`LoadSettingsEffect` → `SettingsLoadedMessage` →
`model.Settings`).

**(b) Änderungen wirken auf das Verhalten:** `UpdateSaveSettingsRequested`
normalisiert zwingend (`AppUpdate.cs` Z. 357: `message.Settings.
Normalize()`), aktualisiert das Modell und persistiert. Die
Verbraucher der vier Werte sind verdrahtet: `DownloadRetentionDays`
→ `ExpiresAtUtc` beim Einplanen/Abschließen (`DownloadService.cs`
Z. 424, 795, 1252, 1445); `DownloadQuotaGb` → Quota-Prüfung
(`DownloadService.cs` Z. 1637) und Quota-Anzeige der Download-Liste
(`TvosAppEffectHandler.cs` Z. 1201); `AutoResumeEnabled` →
Auto-Fortsetzung am Titelende (`AppUpdate.cs` Z. 4024, 4036);
`WatchedEndWindowSeconds` → „angesehen"-Schwelle (`AppUpdate.cs`
Z. 3913). Alle Vorgabelisten liegen innerhalb der
`Normalize`-Schranken (`AppSettings.cs` Z. 81–88); ein
fremdgeschriebener Wert außerhalb würde beim nächsten Speichern/Laden
geklammert — die Wertebegrenzung greift.

**(c) Ausgeschlossene Einstellungen — nicht in der Oberfläche:** In
`VideoPlayer.TvOS` existiert kein UI-Element für
`AutoDownloadEnabled`/`PreloadEpisodeCount` (Auto-Download/Vorlauf),
`AutoDeletePlayedDownloads`/`DeleteAfterPlaybackDelayMinutes` („nach
dem Abspielen löschen"), `WifiOnlyDownloads` (Nur-WLAN) oder den
Protokollversand (`SendLogProtocol*`); grep über das tvOS-Projekt
findet die Begriffe nur im erklärenden Doc-Kommentar (Z. 20–23) und
im internen `AppLog.SetEnabled` (Datei-Logger bleibt laut Plan
erlaubt und ist kein Versand). Der `RunAutoDownloadsEffect`-Zweig in
`UpdateSaveSettingsRequested` bleibt auf tvOS unverdrahtetes No-Op
(kein Case im `TvosAppEffectHandler`).

**(d) Konto-/Server-Infos und Abmeldung:** Abschnitt „Konto" zeigt
`LoginUsername` bzw. „Angemeldet" (Z. 166–168); „Abmelden" dispatcht
`LogoutRequestedMessage` (Z. 71–73) mit `Enabled`-Guard
`Connected or Offline` (Z. 172–173) — deckungsgleich mit dem
Update-Guard `UpdateLogoutRequested` (`AppUpdate.cs` Z. 3549–3557)
und dem Muster `ProfilePageViewController`. Abschnitt „Server"
(Z. 117–126, 188–195) zeigt `ActiveServerAddress` bzw. „kein Server
eingerichtet", Offline-Hinweis über `IsOffline` bzw.
`ServerHealthText`, und „Server wechseln" öffnet
`AppPage.ServerSetup` als Workflow-Seite — Bestandsfunktion
unverändert mitgeführt.

**(e) Tests:** `dotnet test VideoPlayer.Maui.Tests` hier ausgeführt:
**1504/1504 bestanden** (0 Fehler, 0 übersprungen, ~50 s).

**(f) tvOS-Kompilierfallen (Stichprobe):** `ImplicitUsings` ist im
tvOS-Projekt aktiv (`VideoPlayer.TvOS.csproj` Z. 15) — LINQ
(`Contains`/`Append`/`OrderBy`/`ToArray` in `ShowValuePicker`)
kompiliert. `UIAlertControllerStyle.ActionSheet` ist auf tvOS
zulässig und bereits in `DownloadsPageViewController.cs` Z. 211 im
grünen Bestandscode verwendet. `IsViewLoaded`/`Enabled`/
`SetTitle`/`PresentViewController` sind Standard-UIKit-Member;
Collection-Expressions (`int[]`, Z. 30–32) sind C#-12/net10-legal.
`TvosControls.Title(Title)` mit nullablem `UIViewController.Title`
(Z. 64) folgt dem bestehenden Muster (`LoginViewController.cs`
Z. 76, `ProfilePageViewController.cs` Z. 37) — die CI-Annotation
„possible null reference" ist eine Warnung, kein Fehler (nur CS1591
ist `WarningsAsErrors`). `Render`/`ApplyRender` ist gegen
`!IsViewLoaded` abgesichert und holt bei `ViewDidLoad` einen
vorgehaltenen `lastModel`-Stand nach (Z. 135–138, 151–160) —
kein Null-Deref auf die erst in `ViewDidLoad` erzeugten Controls.
Kompiliernachweis: `build-tvos` im Run `42138913426` grün
(Simulator-Build `tvossimulator-arm64`).

**(g) CI/Push-Konsistenz:** `env -u GH_TOKEN gh run view
42138913426 --repo martin-stromberg/VideoPlayer-App` →
`status: completed`, `conclusion: success`, `headBranch` =
Schritt-10-Branch, `headSha` `7511fef9…` = `git rev-parse HEAD`.
`git log origin/<branch>..HEAD` ist leer — kein ungepushter Commit.

**Kleinigkeiten (keine Abweichungen):** `todo.md` im
Feature-Verzeichnis nennt CI-Run `42138055992` — der verbindlich zu
prüfende Run `42138913426` auf HEAD liegt darüber und ist grün
(zeitliche Folge der Lifecycle-Commits). Die Änderung ist
ausschließlich UI-Arbeit; plattformneutrale Normalisierungs- und
Persistenzpfade sind unverändert und durch `AppSettingsTests`,
`AppSettingsServiceTests` und `AppUpdateTests_Settings`
(beispielhaft Z. 51–68: Klammerung 9999→500, 0→1) abgedeckt.
