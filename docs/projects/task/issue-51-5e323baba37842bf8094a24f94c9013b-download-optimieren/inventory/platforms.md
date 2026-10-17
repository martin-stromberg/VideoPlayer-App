# Plattform-Zustand (iOS)

## `VideoPlayer.Maui/Platforms/iOS/Info.plist`

- `UIBackgroundModes` enthält ausschließlich `audio` (Wiedergabe/PiP).
- Keine Hintergrund-Modi für Downloads (`fetch`, `processing` o. ä.).
- `NSAllowsArbitraryLoads = true`.

## `VideoPlayer.Maui/Platforms/iOS/AppDelegate.cs`

Minimaler `MauiUIApplicationDelegate` — nur `CreateMauiApp()`. Keine
iOS-Lebenszyklus-Behandlung (kein `DidEnterBackground`, kein
`HandleEventsForBackgroundUrlSession`, keine Completion-Handler-Verwahrung).

## Download-Transfer heute

- Rein prozessintern (`HttpClient` + `FileStream` in
  `DownloadService.TransferAsync`). Bei iOS-Suspendierung werden Sockets
  durch das System geschlossen/eingefroren → Downloads enden als
  `network`-Fehler oder hängen bis zur Rückkehr in den Vordergrund.
- Es gibt keine Abstraktion eines Plattform-Downloaders (kein Interface
  zwischen `DownloadService` und `HttpClient` außer der injizierbaren
  `httpClientFactory` für Tests).
- `CreateHttpClient` setzt `Timeout.InfiniteTimeSpan`.

## Windows/übrige Plattformen

- Windows ist das primäre Entwicklungsziel (E2E-getestet); dort läuft die
  App im Hintergrund ohnehin weiter (kein Suspend-Modell wie iOS).
- Android: Build möglich, nicht offiziell unterstützt.
- MacCatalyst: baubar, nicht aktiv gepflegt.

## E2E-Infrastruktur

`VideoPlayer.E2E.Tests` (FlaUI, `net10.0-windows`) testet die echte
Windows-App gegen einen lokalen Mock-Server. Es existiert **kein**
Downloads-E2E-Test (`Tests/` enthält u. a. Playback-, Playlist-,
Pairing-, Sources-Tests, aber keinen `DownloadsTests`).
