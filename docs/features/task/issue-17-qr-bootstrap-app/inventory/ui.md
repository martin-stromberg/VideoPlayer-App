# Inventory – UI, Plattform, Dev-API

## `VideoPlayer.Maui/Pages/ServerSetupPage.xaml` (326 Zeilen) + `.xaml.cs` (134 Zeilen)

- Grid `Auto,*,Auto`: Kopf (Kicker „SERVERVERBINDUNG", `ActiveServerCard` mit Health-Pill), ScrollView (bekannte/gefundene Server-Karten), Fuß-ScrollView (`ConnectCard` + `PairingSection`).
- `PairingSection` (Z. 272): nur `IsVisible` bei `model.IsPairingRequired`; `PairingCodeEntry` (kein `MaxLength`, SpellCheck/TextPrediction aus), `PairButton` → `PairingCodeSubmittedMessage`, `PairingIndicator`, `PairingErrorLabel`.
- `UpdateUi(model)` setzt alle Sichtbarkeiten/Enabled-Zustände imperativ; `BindingContext = mvuHost` für XAML-Trigger.
- **Erweiterung**: neue, immer sichtbare Ticket-Karte `BootstrapSection` (Ticket-URL **oder** Kurzcode in einem `BootstrapTicketEntry`): URL-Form trägt die Serveradresse selbst → funktioniert ohne `IsPairingRequired`; Kurzcode nutzt den aktiven Server. `QrScanButton` nur iOS/Android (`#if` im Code-Behind), `BootstrapButton`, `BootstrapIndicator`/`BootstrapRunningLabel`, `BootstrapErrorLabel`. Vp*-Tokens, Dark-only, AutomationIds auf allen neuen Controls.

## Kamera-Scan (nur iOS/Android)

- **Paketwahl**: `ZXing.Net.Maui.Controls` **0.10.4** — MIT-Lizenz (Repo: Redth/ZXing.Net.Maui), veröffentlicht 14.08.2026 (>7 Tage), Assets für `net10.0-android36.0`/`net10.0-ios26.0` (Windows „computed", aber `CameraBarcodeReaderView` hat **keine** Windows-Implementierung).
- `builder.UseBarcodeReader()` in `MauiProgram` (unter `#if ANDROID || IOS` bzw. bedingter PackageReference).
- `QrScannerPage` (nur Mobile): `CameraBarcodeReaderView`, `BarcodesDetected` → erstes Ergebnis → `BootstrapTicketSubmittedMessage` dispatchen + Page schließen (modal). Laufzeit-Permission `Permissions.RequestAsync<Permissions.Camera>()` vor dem Öffnen; `BarcodeScanning.IsSupported`-Check.
- Manifeste: `Platforms/iOS/Info.plist` → `NSCameraUsageDescription`; `Platforms/Android/AndroidManifest.xml` → `<uses-permission android:name="android.permission.CAMERA" />`.
- Windows: kein Scan (unpackaged, keine Webcam-Capability) — `QrScanButton` ausgeblendet; manueller Einstieg deckt Windows ab.
- `PackageReference` auf `net10.0-android`/`net10.0-ios`/`net10.0-maccatalyst` konditionieren (MacCatalyst bekommt den Scanner ebenfalls kostenlos mit, da das Paket Catalyst-Assets enthält — Alternativ nur android/ios, Entscheidung: maccatalyst einschließen, da TFM im Projekt aktiv ist und ZXing es unterstützt).

## `MauiProgram.cs` / `AppShell.xaml.cs`

- `DevicePairingClient` als Singleton mit `RequestTimeout` (Z. 38–40) — `BootstrapAsync` kommt in dieselbe Klasse → keine neue Registration nötig.
- Seiten als `AddTransient`; Shell-Routen via `Routing.RegisterRoute` („serversetup", „login", …). `QrScannerPage` als modal (kein FlyoutItem) — `PushModalAsync` oder Route; schließt sich nach Scan.

## Dev-API (`VideoPlayer.Mvu/DevApi/DevApiModelSnapshot.cs`)

- Enthält `IsPairingRequired`, `IsPairingRunning`, `PairingError`, `ActiveServerAddress`, `ServerHealth` — **secret-frei**. Neue Felder `IsBootstrapRunning`, `BootstrapError` (nur Status/Fehlertext — keine Tickets, keine Tokens!).

## Projekt (`VideoPlayer.Maui.csproj`)

- TFMs: `net10.0-android`; `+ios;maccatalyst` (nicht Linux); `+windows10.0.19041.0` (Windows). `ApplicationVersion` **nicht anfassen**.
- Bestehende Packages: `Microsoft.Maui.Controls`/`Essentials` 10.0.90, `CommunityToolkit.Maui.MediaElement` 10.0.0, `SignalR.Client` 10.0.10.
