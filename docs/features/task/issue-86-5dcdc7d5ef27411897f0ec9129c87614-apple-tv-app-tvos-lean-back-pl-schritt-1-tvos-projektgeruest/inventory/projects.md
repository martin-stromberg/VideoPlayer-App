# Projekte und Paketkompatibilität — Bestandsaufnahme

## Solution `VideoPlayer.App.sln`

Bestehende Mitglieder (GUIDs aus der `.sln`):

| Projekt | Pfad | TFM | Projekt-GUID |
|---------|------|-----|--------------|
| `VideoPlayer.Maui` | `VideoPlayer.Maui/VideoPlayer.Maui.csproj` | `net10.0-{android,ios,maccatalyst,windows10.0.19041.0}` (konditional via `Include*Target`-Schaltern + OS-Bedingungen, „Reporter-Muster") | `{4257935B-DCE4-410F-8A25-24B09965ADE1}` |
| `VideoPlayer.Maui.Tests` | `VideoPlayer.Maui.Tests/VideoPlayer.Maui.Tests.csproj` | `net10.0` | `{6FC81FCF-EB2A-4680-8B64-E048455D5527}` |
| `VideoPlayer.ApiClient` | `VideoPlayer.ApiClient/VideoPlayer.ApiClient.csproj` | `net10.0` | `{A4B7C2D1-3E5F-4A6B-9C8D-1E2F3A4B5C6D}` |
| `VideoPlayer.Mvu` | `VideoPlayer.Mvu/VideoPlayer.Mvu.csproj` | `net10.0` | `{ED7AE013-E0B9-45CC-8BE7-975AD4310FDE}` |
| `VideoPlayer.Storage` | `VideoPlayer.Storage/VideoPlayer.Storage.csproj` | `net10.0` | `{DC9BA2AD-BDFC-48E1-955F-CBB0A4AC4EE4}` |
| `VideoPlayer.E2E.Tests` | `VideoPlayer.E2E.Tests/VideoPlayer.E2E.Tests.csproj` | `net10.0-windows` | `{A1FEAF1E-7C3C-4FA4-BEAB-C87535C18736}` |
| `msTools.DeveloperApi` | `msTools.DeveloperApi/msTools.DeveloperApi.csproj` | `net10.0` | `{858B13CC-51B9-4C5D-9EC3-24F50712C47B}` |
| `msTools.DeveloperApi.Maui` | `msTools.DeveloperApi.Maui/msTools.DeveloperApi.Maui.csproj` | MAUI-Bindung | `{1D14A79C-7B86-446F-9168-4E382E1A4FDB}` |

**Kein tvOS-Projekt, keine tvOS-Workload, kein `package-tvos`-Workflow vorhanden.**

## Zu referenzierende plattformneutrale Projekte (alle `net10.0`)

| Projekt | Relevante Paketabhängigkeiten | tvOS-Relevanz |
|---------|-------------------------------|----------------|
| `VideoPlayer.Mvu` | `Microsoft.Extensions.Logging.Abstractions` 10.0.10; `ProjectReference` auf `VideoPlayer.ApiClient` + `VideoPlayer.Storage` | reine `net10.0`-Bibliothek — `net10.0-tvos`-App kann `net10.0` konsumieren (NuGet-Kompatibilität gegeben) |
| `VideoPlayer.Storage` | `Microsoft.Extensions.Logging.Abstractions` 10.0.10, `sqlite-net-pcl` 1.11.285, `SQLitePCLRaw.bundle_green` 2.1.11, `SQLitePCLRaw.lib.e_sqlite3` 3.53.3, `SQLitePCLRaw.lib.e_sqlite3.android` 2.1.13 | SQLite-Stack muss unter `net10.0-tvos` native Assets liefern (s. unten) |
| `VideoPlayer.ApiClient` | `Microsoft.Extensions.Logging.Abstractions` 10.0.10, `Zeroconf` 3.7.16 | mDNS-Discovery (Zeroconf) muss unter `net10.0-tvos` funktionieren (s. unten) |
| `msTools.DeveloperApi` | keine externen Pakete (Loopback-HTTP-Server, Routing, `devapi.json`) | `netstandard`-nahe BCL-APIs; Kompilierbarkeit unter tvOS gegeben |

## Paketkompatibilität unter `net10.0-tvos` (Prüfergebnis)

Geprüft gegen NuGet-Metadaten (nuget.org, kompatible-TFM-Listen) — Detailbeleg auch in
der Dauer-Dokumentation vorgesehen.

| Paket | Version | Ergebnis | Anmerkung |
|-------|---------|----------|-----------|
| `SQLitePCLRaw.lib.e_sqlite3` | 3.53.3 | **kompatibel** | NuGet-„Computed compatible TFMs" enthalten ausdrücklich `net6.0-tvos` bis `net10.0-tvos` — natives tvOS-Asset vorhanden |
| `SQLitePCLRaw.bundle_green` | 2.1.11 | **kompatibel** | `netstandard2.0`-Provider-Bundle; wählt `e_sqlite3`-Provider — kein Plattformcode nötig |
| `sqlite-net-pcl` | 1.11.285 | **kompatibel** | `netstandard2.0`; hängt von `bundle_green` ab |
| `SQLitePCLRaw.lib.e_sqlite3.android` | 2.1.13 | **relevant, aber unproblematisch** | Android-spezifisches Zusatzpaket; unter tvOS schlicht ohne Wirkung (RID-Auswahl greift nicht); kein Eingriff nötig |
| `Zeroconf` | 3.7.16 | **kompilierbar; Laufzeitrisiko Multicast-Entitlement** | Rein verwaltete `netstandard`-mDNS-Implementierung (UDP). Kompiliert unter `net10.0-tvos`. Zur Laufzeit gilt auf tvOS wie iOS: mDNS-Multicast auf echten Geräten erfordert das Entitlement `com.apple.developer.networking.multicast` und `NSLocalNetworkUsageDescription` + `NSBonjourServices` in der `Info.plist` (Simulator läuft meist ohne Entitlement). Ausweichvorschlag dokumentiert: UDP-only-/Broadcast-Discovery bzw. direkte Servereingabe — der bestehende `UdpServerDiscovery`-Pfad in `VideoPlayer.ApiClient` ist dafür die Referenz |
| `Microsoft.Extensions.Logging.Abstractions` | 10.0.10 | **kompatibel** | plattformneutral |
| `Microsoft.Maui.*`, `CommunityToolkit.Maui.*`, `ZXing.Net.Maui.*` | — | **nicht referenziert** | MAUI unterstützt tvOS nicht; tvOS-Projekt referenziert nur die `net10.0`-Bibliotheken (keine transitive MAUI-Abhängigkeit — `msTools.DeveloperApi.Maui` wird **nicht** referenziert) |

## Konventionen in `VideoPlayer.Maui.csproj` (wiederverwendbares Muster)

- „Reporter-Muster": `IncludeIosTarget`/`IncludeAndroidTarget`/`IncludeMacCatalystTarget`
  — TFM-Liste wird per Property + OS-Bedingung zusammengesetzt, damit Restore auf
  Runnern ohne die jeweilige Workload nicht bricht (Kommentar in der csproj nennt
  explizit `NETSDK1147`-Propagation in `net10.0`-Bibliotheken als Motiv).
- App-Metadaten: `ApplicationId` `de.martinstromberg.videos`, `ApplicationTitle`
  `Heimkino`, `ApplicationDisplayVersion` `0.1`, `ApplicationVersion` `1`,
  `SupportedOSPlatformVersion` je Plattform, `GenerateDocumentationFile`,
  `WarningsAsErrors` `CS1591`, `ImplicitUsings`, `Nullable`, LICENSE-Pack-Verweis.
- Signatur nur für Geräte-Builds (`!RuntimeIdentifier.StartsWith('iossimulator')`).

## tvOS-App-Grundgerüst (Referenzwissen)

Ein natives .NET-for-tvOS-App-Gerüst besteht aus: `*.csproj` mit
`<TargetFramework>net10.0-tvos</TargetFramework>` + `<OutputType>Exe</OutputType>` +
App-Metadaten (`ApplicationId`, `ApplicationDisplayVersion`, `ApplicationVersion`,
`SupportedOSPlatformVersion`, optional `RuntimeIdentifiers` `tvossimulator-arm64;tvos-arm64`),
einer `Info.plist` (Bundle-Identifier, Name, Versionen, `UILaunchStoryboardName`
entbehrlich bei leerem Gerüst) und `Program.cs` (`UIApplication.Main(args, null,
typeof(AppDelegate))`) + `AppDelegate : UIApplicationDelegate` mit `Window`-Override
(`UIWindow` mit `RootViewController` auf einen einfachen `UIViewController` setzen,
`MakeKeyAndVisible()`).
