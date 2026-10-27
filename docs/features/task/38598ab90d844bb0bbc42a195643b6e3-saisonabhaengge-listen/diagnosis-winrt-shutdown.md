# Begrenzte Diagnose: WinRT-Abbruch beim Windows-Shutdown

Geprüft am: 2026-10-26

## Ergebnis

**Ursache nicht abschließend bestimmt; kein belegbarer enger Produktfix.** Die betroffene Operation und ihre Phase sind nachgewiesen: Beim geordnet angeforderten Beenden eines MAUI-Kindprozesses läuft der GC-Finalizer eines CsWinRT-Objekts in eine Access Violation. Der vorhandene verwaltete Stack benennt weder das konkrete native Objekt noch dessen Eigentümer oder die fehlerhafte Lebensdauer. Daher wurden keine Produkt-, Paket- oder Testdateien geändert und keine weitere Vollsuite gestartet.

Die Saisonfunktion darf auf dieser Basis weder als Ursache noch als sicher unabhängig von diesem Fehler bezeichnet werden; ein Vergleich mit einem reproduzierenden Vorfeature-Baselinebuild fehlt.

## Lokale Belege

- App-Prozess **45732**, Profil `e2e-submittedtextremains-df9b7ff6bfda49d2a9c98c9c28086e99`.
- `process.json` unter `.work/e2e-process-logs/20261026-062851082-45732`: Exit `-1073741819` (`0xC0000005`), `killAttempted=false`, `killRequestSucceeded=false`, `forcedKill=false`, `exitedBeforeShutdown=false`.
- Beginn des Aufräumens `2026-10-26T06:28:50.708399Z`, Protokollabschluss `06:28:51.0833205Z`: ungefähr 375 ms Abstand. Das ist eine zeitliche Zuordnung zum Shutdown, kein Beweis einer bestimmten COM-Ursache.
- `stderr.log`: `Marshal.Release` → `WinRT.IObjectReference.DisposeTrackerSource` → `Dispose` → `Finalize` → `GC.RunFinalizers`.
- Windows-Ereignisprotokoll Application, Event 1026 um 07:28:50 Ortszeit bestätigt denselben Stack für `VideoPlayer.Maui.exe`; im eng geprüften Zeitfenster war kein ergänzendes Event 1000 mit nativem Modul/Offset vorhanden.
- Die Fixture fordert zunächst `CloseMainWindow()` an. Der Prozess endete hier bereits ohne ihren Kill-Fallback. Der unabhängig geprüfte Exit-Guard meldet diesen App-Abbruch als Fixturefehler.
- Im Appcode wurden keine appseitigen `Marshal.Release`-/`ReleaseComObject`-Aufrufe als direkter Kandidat gefunden. Das schließt einen indirekten Lifetimefehler nicht aus.
- Zwei anschließende isolierte Wiederholungen desselben Suchfalls bestanden laut Testbericht ohne Crash. Eine kurze neue Wiederholung ohne neue Hypothese würde den sporadischen Fehler weder erklären noch einen Fix belegen; deshalb wurde kein weiterer Lauf ausgeführt.

## Tatsächlich verwendete Komponenten

Aus Projektdatei, `obj/project.assets.json` und Dateiversion der ausgelieferten DLL:

| Komponente | Version |
|---|---|
| Microsoft.Maui.Controls / Essentials | 10.0.90 |
| Microsoft.WindowsAppSDK | 1.8.260508005 |
| Microsoft.WindowsAppSDK.WinUI | 1.8.260505002 |
| Microsoft.Windows.SDK.NET.Ref | 10.0.19041.57 |
| WinRT.Runtime.dll | 2.2.0.48161, Quellcommit `8649ee3eeb2445ca2a36d80d878ef60b96a6c65d` |
| Runtime im Event 1026 | .NET 10.0.14, CoreCLR 10.0.1426.52326 |

## Abgleich mit Primärquellen

1. [CsWinRT-Quellstand der tatsächlich ausgelieferten Runtime](https://github.com/microsoft/CsWinRT/blob/8649ee3eeb2445ca2a36d80d878ef60b96a6c65d/src/WinRT.Runtime/ObjectReference.cs): Der Finalizer ruft `Dispose()` auf; dieser Pfad führt zur Freigabe des Reference-Tracker-Interfaces mit `Marshal.Release`. Das passt zur verwalteten Stackfolge. Die Datei allein verrät nicht, welches native Interface in diesem App-Prozess bereits ungültig war.
2. [CsWinRT-Issue 2532](https://github.com/microsoft/CsWinRT/issues/2532): Beschreibt einen Finalizer-/Apartment-Shutdown-Konflikt. Der gemeldete Fall verwendet allerdings UWP-Sekundärviews und `ObjectReferenceWithContext.Release`, nicht den hier belegten `DisposeTrackerSource`-Stack. **Keine bestätigte Dublette und keine belastbare Fixempfehlung für diese App.**
3. [WinUI-Issue 11653](https://github.com/microsoft/microsoft-ui-xaml/issues/11653): Beschreibt einen Shutdown-Abbruch mit fokussierter TextBox und einen dort getesteten Fokus-Workaround. Der veröffentlichte native Stack betrifft InputServices/RichEdit; er ist nicht identisch mit dem hier verfügbaren verwalteten Stack. Fokus könnte eine untersuchbare Hypothese sein, ist aber **nicht als Ursache oder sicherer Workaround dieser App nachgewiesen**. Ein pauschales App-Unfocus-/GC-Workaround würde die vorliegende Diagnose überschreiten.
4. [Offizielle MAUI-Lebenszyklusdokumentation](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/app-lifecycle?view=net-maui-10.0): Unter Windows entspricht `Destroying` dem nativen `Closed`-Ereignis. Die Dokumentation empfiehlt das Entfernen nativer Ereignisabonnements; sie bietet keinen spezifischen Fix für diesen nativen Finalizer-Stack.

## Konkrete Restarbeit und Abschlussgrenze

Für einen belastbaren Runtime-/Produktfix fehlt ein nativer Crashdump mit Symbolen, der das fehlerhafte `Release` samt Pointer und tatsächlichem nativen Callee auflöst. Danach ist eine minimierte Reproduktion mit genau diesem Komponentenstand und kontrollierter Shutdown-/Fokusfolge erforderlich. Erst dann lässt sich entscheiden, ob App-Lifetimecode, eine bestätigte WinUI-/CsWinRT-Korrektur oder ein eng begründetes Paketupdate benötigt wird.

Keine weiteren beliebigen Vollsuite-Wiederholungen, kein Ignorieren von Exit `0xC0000005`, kein Kill als Ersatz für geordneten Shutdown, keine pauschalen Paketupdates und keine Behauptung eines vollständig grünen Gesamtnachweises. Die vorhandene Prozessdiagnose verbessert die Beweislage, löst aber die native Ursache nicht. Der mobile Lifecycle-Cleanup kann auf dem aktuellen Nachweisstand nicht vollständig freigegeben werden. Serverintegration kann unabhängig vorbereitet und geprüft werden; eine mobile Freigabe darf daraus nicht abgeleitet werden.
