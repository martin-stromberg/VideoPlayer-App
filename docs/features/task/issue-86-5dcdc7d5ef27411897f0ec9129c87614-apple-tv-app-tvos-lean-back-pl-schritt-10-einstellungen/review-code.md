# Code-Review — Schritt 10: Schlanker Einstellungsbereich (tvOS)

Status: **Keine Befunde**

Geprüfte Änderung: `VideoPlayer.TvOS/Ui/SettingsPageViewController.cs` (einzige Code-Datei).

## Prüfpunkte

- **MVU-Konformität:** Alle Änderungen laufen über Messages (`SaveSettingsRequestedMessage`, `LoadSettingsRequestedMessage`, `LogoutRequestedMessage`, `NavigateToMessage`) — kein direkter Dienstzugriff aus der View; `dispatch` auf dem Hauptthread (Composition-Verdrahtung).
- **Render-Robustheit:** `lastModel`-/`IsViewLoaded`-Guard verhindert NRE bei `Render` vor `ViewDidLoad` (Verbesserung gegenüber dem bisherigen ungeschützten `label!`-Muster); spätes `ApplyRender(lastModel)` am Ende von `ViewDidLoad`.
- **Stataktualität:** `settings`-Feld wird in `ApplyRender` stets aus `model.Settings` übernommen; nach jedem Speichern kommt der normalisierte Stand über `SettingsLoaded`-/Update-Pfad zurück — Button-Aktionen arbeiten auf dem aktuellen Wert.
- **tvOS-API-Signaturen:** `UIAlertController.Create(string, string, UIAlertControllerStyle)`, `UIAlertAction.Create(string, UIAlertActionStyle, Action<UIAlertAction>)` (Parameter `action`, nicht `_`), `PresentViewController(alert, true, null)`, `UIButton.SetTitle(string, UIControlState.Normal)`, `IsViewLoaded` — alle exakt wie in `DownloadsPageViewController`/`ProfilePageViewController` bewiesen.
- **Fallen:** kein `const nfloat`, kein `SeparatorStyle`, kein `IReadOnlyList.IndexOf` (`Contains` ist LINQ — `ImplicitUsings` aktiv), kein Basistyp-Member-Zugriff, Named-Args nicht nötig verwendet, XML-Docs auf public-Membern (CS1591 = Fehler).
- **Ausschlüsse:** Keine UI für `AutoDownloadEnabled`/`PreloadEpisodeCount`/`AutoDeletePlayedDownloads`/`DeleteAfterPlaybackDelayMinutes`/`WifiOnlyDownloads`/`LoggingEnabled` — die `with`-Kopien übertragen die bisherigen Werte unverändert (kein stiller Reset nicht exponierter Werte beim Speichern).
- **Abmelde-Guard:** `logoutButton.Enabled` spiegelt den Update-Guard (`Connected or Offline`) — identisch zur Profilseite.
- **Formatierungen:** `FormatDays` (Singular/Plural), `FormatEndWindow` (Sekunden/Minuten) decken Options- und Fremdwerte ab.
- **Nebenbedingung „kein Schreibzugriff auf Inhalte":** erfüllt — ausschließlich Einstellungs-/Navigations-/Abmelde-Messages.

## Befunde

Keine.
