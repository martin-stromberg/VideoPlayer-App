# Übersetzte Anforderung: App-Grundgerüst mit MVU-Struktur und Navigation

(Entwicklungsschritt 2 des Projekts „Neugestaltung der App `VideoPlayer`", Issue #9)

## Fachliche Zusammenfassung

Es ist das Grundgerüst der neuen .NET-MAUI-App `VideoPlayer` zu erstellen — eine eigenständige
Neuimplementierung, die den bisherigen MVVM-Bestand (`VideoWebPlayer.Maui`) nicht als
Laufzeitbasis übernimmt. Die Architektur folgt dem MVU-Muster (Model–View–Update) auf Basis
eines eigenen, schlanken MVU-Zyklus ohne externes MVU-Framework. Die App erhält eine
Hauptnavigation per Hamburgermenü mit sechs Einträgen („Startseite", „Quellen", „Favoriten",
„Downloads", „Profil", „Einstellungen"), deren Zielseiten als funktionslose Grundgerüste
angelegt werden, sowie eine Ladeanzeige für den App-Start. Die Serveranbindung erfolgt
ausschließlich über den in Schritt 1 neu implementierten `VideoPlayer.ApiClient`
(Erreichbarkeitsprüfung via `CheckHealthAsync`).

## Akzeptanzkriterien (aus der Anforderung)

1. Die App baut und startet unter Windows.
2. Der iOS-Build ist über die CI-Umgebung nachweisbar (lokal auf diesem Windows-Rechner
   nicht möglich; Nachweis über korrekte iOS-Targets/Konfiguration und CI-Workflow).
3. Das Hamburgermenü enthält alle sechs genannten Einträge und führt zu den jeweiligen Seiten.
4. Die Struktur folgt MVU ohne Übernahme des bisherigen MVVM-Codes und ohne externes
   MVU-Framework.
5. Die App kann die API des lokal im IIS eingerichteten Webprojekts „VideoPlayer" grundsätzlich
   aufrufen (z. B. Erreichbarkeitsprüfung).

## Betroffene Klassen und Komponenten

- **Neues App-Projekt** (Platzierung im Rahmen der Planung zu entscheiden; Arbeitstitel
  `VideoPlayer.Maui`): `MauiProgram`, `App`, `AppShell` mit Flyout-/Hamburgernavigation,
  sechs Grundgerüst-Seiten (`Startseite`/`Home`, `Quellen`/`Sources`, `Favoriten`,
  `Downloads`, `Profil`, `Einstellungen`) sowie `LoadingPage` als Ladeanzeige beim Start.
- **MVU-Kern (eigen, ohne externes Framework):** unveränderliches `Model`, `Message`-Typen,
  reine `Update`-Funktion, Effekt-Beschreibungen (Navigation, Server-Erreichbarkeitsprüfung)
  und ein Host/Dispatcher, der den Zyklus `Message → Update → Model → View/Effekt` treibt.
- **Services:** Abstraktion für die Server-Basisadresse (Vorgriff auf Schritt 3/4 minimal
  halten) und einen Health-Check, der `VideoPlayerApiClient.CheckHealthAsync` nutzt.
- **Projektdateien/CI:** neues `*.csproj` mit MAUI-Targets (`net10.0-ios`,
  `net10.0-windows10.0.19041.0`; Android-Erweiterbarkeit berücksichtigen),
  Solution-Eintrag in `VideoPlayer.App.sln`, Anpassung der GitHub-Workflows
  (`pr-checks.yml`, `staging-merge.yml`, `main-merge.yml`) sowie `Set-Version.ps1`,
  damit der iOS-Build der neuen App über `macos-latest` nachweisbar ist.
- **Tests:** `VideoWebPlayer.Maui.Tests` (xUnit) — Unit-Tests für den MVU-Zyklus sowie
  Contract-/Struktur-Tests für die Shell-Navigation (etabliertes Muster: Quelltext-/
  XAML-Contract-Tests, da das Testprojekt keine MAUI-Referenz besitzt).

## Implementierungsansatz

- MVU-Zyklus selbst implementieren: `Update(model, message)` als reine Funktion liefert
  neues Model plus Effekte; Effekte (Shell-Navigation, Health-Check) werden durch einen
  Host an den MAUI-Rand delegiert. Keine `ViewModel`-/`INotifyPropertyChanged`-Logik der
  Alt-App, kein `Fabulous`/`Comet`.
- Hauptnavigation über `Shell` mit `FlyoutBehavior="Flyout"` (Hamburgermenü) und einem
  `FlyoutItem` je Menüeintrag mit `Route` + `ContentTemplate` auf die jeweilige Seite —
  anlehnend an das bestehende `AppShell`-Muster, aber neu aufgebaut.
- `LoadingPage` als Startanzeige; nach Abschluss der Erreichbarkeitsprüfung wechselt die
  App per MVU-Effekt zur Shell-Navigation (Startseite).
- `VideoPlayer.ApiClient` wird per `ProjectReference` eingebunden; die Serveradresse ist
  über eine kleine Auflösungslogik konfigurierbar (Default: lokale IIS-Installation).
- UI und Bedienung orientieren sich an der Webanwendung des Servers (kein separater
  Designentwurf).

## Konfiguration

- Server-Basisadresse: minimal konfigurierbar (z. B. Umgebungsvariable/Preferences-Key
  `ServerAddress` mit Default `http://localhost/videoplayer`); vollständige
  Serververwaltung ist Gegenstand von Schritt 4.
- API-Schlüssel des Client-Gates: unverändert über `ClientGate.Resolve()`
  (`VIDEOWEBPLAYER_MAUI_API_TOKEN` / `VideoPlayerApiToken`-Build-Eigenschaft).

## Offene Fragen

- Keine fachlich blockierenden Fragen. Technische Entscheidungen (Projektplatzierung,
  Umgang mit den Alt-Projekten, exakte Form des iOS-/Windows-Nachweises) werden in der
  Planung getroffen und dort dokumentiert.
