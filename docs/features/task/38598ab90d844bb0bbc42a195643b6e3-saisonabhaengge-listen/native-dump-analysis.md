# Native Dumpanalyse vom 26.10.2026

## Befund

Der Fullrun unter `.work/test-results/full-final-20261026` erfasste für `VideoPlayer.Maui.exe`, PID 27688, einen vollständigen 789-MB-Dump in `dumps-27688/VideoPlayer.Maui.exe_261026_142606.dmp`. ProcDump meldete um 14:26:06 Ortszeit `Unhandled: C000027B`; der Dump wurde um 14:26:13 fertig geschrieben. Die Auswertung des Minidump-Exception-Streams und der im Dump enthaltenen Stowed-Exception-Struktur ergab:

| Merkmal | Wert |
| --- | --- |
| Ausnahmecode | `0xC000027B` (WinRT stowed exception) |
| Ausnahme-Thread | 57640 |
| Ausnahmeadresse | `CoreMessagingXP.dll+0x93b66` |
| Anzahl gespeicherter Ausnahmen | 1 |
| Struktur | `STOWED_EXCEPTION_INFORMATION_V2`, Größe 56 Bytes, Signatur `SE02` |
| HRESULT | `0x8000FFFF` (`E_UNEXPECTED`) |
| Ursprungs-Thread | 57640 |
| Gespeicherte Ausnahmeadresse | `combase.dll+0x5cf34` |
| Gespeicherter Trace | 33 Adressen, 8-Byte-Wörter |
| Verschachtelte Ausnahme | Keine (`NestedExceptionType = 0`, Zeiger null) |

Der gespeicherte Trace beginnt mit `combase.dll+0x5de97`, führt über zahlreiche Frames in `CoreMessagingXP.dll`, dann `user32.dll`/`win32u.dll` und endet bei zwei Frames in `Microsoft.UI.Xaml.dll` (`+0x16712e`, `+0x166eb2`). Ohne native Symbole sind keine Funktionsnamen dieser Frames belegt. Die Struktur enthält weder einen CLR-Ausnahmetyp noch ein Anwendungsobjekt. Das HRESULT beschreibt nur eine unerwartete WinRT-Fehlersituation, nicht deren Ursache.

## Zeitliche Einordnung und Grenzen

Laut `process.json` begann der Test-Fixture-Shutdown um 13:26:05.805 UTC. ProcDump sah die unbehandelte Ausnahme in der nächsten Sekunde; der Prozess wurde erst nach der Dump-Erstellung zwangsweise beendet. Die enge zeitliche Nähe und der UI-Messaging-Trace **sprechen für einen Zusammenhang mit dem Test-Shutdown**, beweisen aber weder dessen Kausalität noch einen Defekt im Produktcode. Ein konkreter Fix lässt sich daraus nicht ableiten. Im ProcDump-Protokoll stehen vor der unbehandelten Ausnahme zudem CLR- und `40080201`/`40080202`-Ereignisse; diese First-Chance-Ereignisse sind ohne weitere Kontextanalyse keine Fehlerursache.

Der früher beobachtete separate Prozess PID 45732 endete mit `0xC0000005` im WinRT-`ReferenceTrackerFinalizer` nach `CloseMainWindow`. Der vorliegende Dump zeigt `0xC000027B` im `CoreMessagingXP`-Pfad und liefert keinen Nachweis, dass beide Ereignisse dieselbe Ursache haben. Der Fullrun hatte 70/72 erfolgreiche Tests; die saisonalen Tests waren erfolgreich. Die beiden fehlgeschlagenen Tests sind gesondert zu diagnostizieren.

## Methode und nächster Nachweis

Die Analyse erfolgte ausschließlich lokal, durch Lesen der Minidump-Streams für Modulverzeichnis, Ausnahme und Speicherbereiche sowie der von Microsoft dokumentierten `STOWED_EXCEPTION_INFORMATION_V2`-Felder. Das temporäre Auswerteskript liegt unter `.work/native-debugger/inspect_dump.cjs`; der Dump wurde weder hochgeladen noch als Artefakt eingecheckt. Ein nativer Debugger (`cdb`/WinDbg) ist auf dem System nicht vorhanden. `dotnet-dump` ist installiert, kann native Aufrufrahmen aber nicht auflösen. Der offizielle WinDbg-MSIX-Installer hat etwa 1,2 GB; er wurde nicht installiert.

Für einen konkreten Ursachenbeleg braucht es die nativen Symbole und eine Analyse im WinDbg mit `.exr -1`, `!pde.dse`, `~* k` sowie gegebenenfalls SOS für die unmittelbar vorausgehenden CLR-Ereignisse. Dabei müssen die gespeicherten `CoreMessagingXP`-Frames und der UI-Thread auf Funktionsnamen aufgelöst und mit dem Fixture-Aufruf `CloseMainWindow` sowie mit einem Wiederholungslauf ohne Shutdown verglichen werden. Erst wenn dort eine reproduzierbare Anwendungsstelle sichtbar ist, ist ein Runtime-Patch begründet. Der Dump bleibt dabei lokal.

## Quellen

- [Microsoft: Stowed Exception C000027B](https://learn.microsoft.com/en-us/shows/inside/c000027b)
- [Microsoft: STOWED_EXCEPTION_INFORMATION_V2](https://learn.microsoft.com/en-us/windows/win32/wer/stowed-exception-information-v2)
- [Microsoft: STOWED_EXCEPTION_INFORMATION_HEADER](https://learn.microsoft.com/en-us/windows/win32/wer/stowed-exception-information-header)
- [Microsoft: Common HRESULT Values](https://learn.microsoft.com/en-us/windows/win32/seccrypto/common-hresult-values)
- [Microsoft: dotnet-dump limitations](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dump)
