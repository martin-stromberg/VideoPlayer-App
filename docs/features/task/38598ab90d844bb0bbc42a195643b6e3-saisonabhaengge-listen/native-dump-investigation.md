# Nativer Crashdump: begrenzter Reproduktionsversuch

Stand: 2026-10-26

## Ergebnis

Der sporadische MAUI-Windows-Absturz `0xC0000005` trat in diesem begrenzten Versuch nicht erneut auf. **Es entstand kein Crashdump; nativer Callstack, betroffenes Modul und Ursache sind weiterhin unbekannt.** Der zuvor beobachtete Absturz von PID 45732 und der damalige Gesamtlauf mit 69/72 bestanden bleiben maßgeblich. Aus den hier erfolgreichen Teiltests folgt kein vollständig grüner Gesamtnachweis und kein begründbarer Codefix.

## Erfassung und Eingrenzung

- Microsoft ProcDump 12.02 von der [offiziellen Sysinternals-Seite](https://learn.microsoft.com/en-us/sysinternals/downloads/procdump) als ZIP geladen und ausschließlich unter `.work/native-dump-tools/` entpackt. Authenticode für `procdump64.exe`: `Valid`, Aussteller `Microsoft Corporation`. SHA-256 des geladenen ZIP: `5FE68E81632F13188930AFFFF20A9A9910170BAE654360850D3108CEAD708A2D`.
- Pro Teststart wurde über `Win32_Process.ExecutablePath` nur der MAUI-Prozess aus diesem Workspace identifiziert. ProcDump wurde an dessen **konkrete PID** mit `-accepteula -ma -e -f C0000005 -n 1 <PID> <Zielordner>` gehängt. `-e` beobachtet unbehandelte Ausnahmen, `-f` filtert das Ereignis und `-ma` fordert einen vollständigen Dump an; siehe [ProcDump-Dokumentation](https://learn.microsoft.com/en-us/sysinternals/downloads/procdump). Es wurde kein globaler Postmortem-Debugger registriert und keine Windows-Registry verändert.
- Ein gezielter Lauf von `SourceSearchTests.SubmittedText_RemainsVisibleAfterReturn` sowie elf Wiederholungen mit `--no-build --no-restore`: **12/12 bestanden**, jeweils eigener Kindprozess überwacht, kein Dump. Protokolle unter `.work/native-dump-probe-20261026/`.
- Ein weiterer gemeinsamer, gefilterter Lauf von `PairingTests` und `SourceSearchTests` im selben Testhost: **6/6 bestanden**, sechs eigene MAUI-Kindprozesse überwacht, alle mit Exitcode 0, kein Dump. Protokolle unter `.work/native-dump-probe-20261026/mixed/`.
- Die lokalen Skripte und Protokolle liegen im ignorierten `.work/`; `git check-ignore` bestätigt den Ausschluss. Es wurden weder Dumps noch Testzugangsdaten ausgegeben, versioniert oder übertragen.

## Nächster Diagnosepunkt

Beim nächsten echten `0xC0000005` ist ein an **die betroffene eigene PID** angehängter vollständiger Dump nötig. Danach sollte der native Stack mit passenden Microsoft-Symbolen aufgelöst und das fehlerhafte `Release` einem konkreten Modul und Objekt zugeordnet werden. Erst diese Evidenz kann einen engen App-, Fixture- oder Runtime-Fix tragen. Wiederholte Vollsuiten ohne neue Hypothese wurden bewusst nicht gestartet. Die vorhandene [Shutdown-Diagnose](diagnosis-winrt-shutdown.md) dokumentiert den verwalteten Stack und den ursprünglichen Absturz.
