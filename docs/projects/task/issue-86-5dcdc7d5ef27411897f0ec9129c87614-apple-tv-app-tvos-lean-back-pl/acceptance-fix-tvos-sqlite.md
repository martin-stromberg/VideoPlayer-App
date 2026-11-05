# Abnahmeprüfung – Verifikations-Fix e_sqlite3 (tvossimulator-arm64)

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Hinweise

Prüfung des Branches
`task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl-fix-tvos-sqlite-simulator`
gegen die Basis
`task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl`
(Merge-Base `2448d9d`, HEAD `a18db2d`). Der Diff umfasst 7 Dateien,
+301/−10: `VideoPlayer.TvOS.csproj`, `TvOsProjectContractTests.cs`,
`Retag-Esqlite3-For-Tvos.py` (neu), zwei Binärarchive unter
`VideoPlayer.TvOS/NativeLibs/`, `changes.log`, `installation.md`.

**(a) Storage/SqliteBootstrap netto unverändert — bestätigt.**
`git diff <basis>...HEAD -- VideoPlayer.Storage/` ist leer. Die
Commits `2ee6f8c` (verworfener Provider-Wechsel auf
System-libsqlite3) und `4f5e554` (Rückbau) berühren den Pfad in der
Historie, heben sich im Netto-Diff aber exakt auf — der verworfene
Ansatz ist im `changes.log` und in `installation.md` („Verworfene
Alternative") dokumentiert. Kein Provider-Wechsel, keine Auswirkung
auf iOS/Android/Windows/macOS: sämtliche csproj-Änderungen liegen
ausschließlich in `VideoPlayer.TvOS.csproj` und greifen nur unter
`IncludeTvOsTarget == 'true'`.

**(b) csproj-Änderung plausibel — bestätigt.**
`VideoPlayer.TvOS/VideoPlayer.TvOS.csproj:52-82`:
- `PackageReference SQLitePCLRaw.lib.e_sqlite3.tvos 2.1.11
  ExcludeAssets="all"` — einzige Paketreferenz im Diff, betrifft
  exakt das problematische transitive Paket (einzug via
  `bundle_green` 2.1.11 aus `VideoPlayer.Storage.csproj:18`, Version
  deckt die transitive Mindestforderung).
- `NativeReference` RID-konditioniert korrekt:
  `RuntimeIdentifier == 'tvos-arm64'` → `e_sqlite3.tvos-arm64.a`
  (Gerät, thin); `!= 'tvos-arm64'` → `e_sqlite3.tvossimulator.a`
  (fat, deckt `tvossimulator-x64` und `tvossimulator-arm64` ab — die
  beiden anderen RIDs in `RuntimeIdentifiers`, Z. 35).
  `Kind=Static`, `ForceLoad=true`, `SmartLink=false` ist das
  korrekte Muster, damit der `__Internal`-P/Invoke-Pfad von
  `provider.internal` die `sqlite3_*`-Symbole auflöst.
- Der gesamte ItemGroup-Block steht unter
  `Condition="'$(IncludeTvOsTarget)' == 'true'"` — unter Windows
  (degradierte Hülle, Z. 36-41) greift nichts davon.
- *Hinweis (keine Abweichung):* die Simulator-Bedingung
  `!= 'tvos-arm64'` würde auch bei leerem `RuntimeIdentifier`
  greifen (Compile-Build ohne `-r`); der native Link läuft
  RID-gebunden, daher praktisch unkritisch.

**(c) Retag-Skript plausibel und dokumentiert — bestätigt.**
`scripts/Retag-Esqlite3-For-Tvos.py`: Methode im Docstring
nachvollziehbar beschrieben (Plattform-IDs 2/3/7/8, Aufruf mit
konkreten NuGet-Cache-Pfaden). Implementierung patcht ausschließlich
das 4-Byte-`platform`-Feld in `LC_BUILD_VERSION` (0x32) bzw.
schreibt `LC_VERSION_MIN_IPHONEOS` (0x25) zu `LC_VERSION_MIN_TVOS`
(0x2f) um — Größen/Offsets und damit der ar-SYMDEF-Index bleiben
unverändert; keine Codeänderung am Binary. Der Spezialfall
„x86_64 trägt kein LC_BUILD_VERSION, sondern nur das implizite
`LC_VERSION_MIN`" ist korrekt behandelt (nur für `dst==8 &&
cputype==x86_64` bzw. `dst==3`). Provenienz der Archive
nachvollziehbar: Quelle `SQLitePCLRaw.lib.e_sqlite3` 3.53.3 —
diese Version ist in `VideoPlayer.Storage.csproj:19` bereits
direkt referenziert.

**(d) Größen-/Header-Plausibilität der .a-Dateien — bestätigt**
(direkte Mach-O-/ar-Inspektion per Python, Ersatz für fehlendes
`lipo`/`otool`):
- `e_sqlite3.tvos-arm64.a`: 2.218.576 Bytes, thin ar-Archiv
  (`!<arch>\n`), enthaltene Mach-O-Objekte tragen
  `LC_VERSION_MIN_TVOS` (0x2f) — exakt das Ergebnis des
  dokumentierten Skriptaufrufs `--from 2 --to 3` auf
  `runtimes/ios-arm64`.
- `e_sqlite3.tvossimulator.a`: 4.274.728 Bytes, fat/universal
  (`0xcafebabe`), 2 Slices: x86_64 (0x1000007, inneres ar-Archiv mit
  `LC_VERSION_MIN_TVOS` — implizites Simulator-Tag) und arm64
  (0x100000c, inneres ar-Archiv mit `LC_BUILD_VERSION`,
  `platform = 8` = `tvossimulator`) — exakt `--from 7 --to 8` auf
  `runtimes/iossimulator-arm64`.
- Größen (~2 MB/S Slice) plausibel für sqlite3.o-Archive; die
  Slice-Belegung deckt exakt die fehlende `tvossimulator-arm64`-Lücke
  ab und liefert zusätzlich `tvossimulator-x64`.

**(e) Doku/Contract-Test konsistent, keine Nebenwirkungen —
bestätigt.**
- `TvOsProjectContractTests.cs:56-78`: der Test lässt jetzt nur
  Referenzen mit `ExcludeAssets="all"` zu und blockiert weiterhin
  jede Paketreferenz mit beitragenden Assets — konsistent mit der
  einzigen neuen Referenz.
- `changes.log` (HEAD-Eintrag) beschreibt Befund, Fix, verworfene
  Alternative und Absicherung zutreffend; nennt Run `42110023939`.
  *Hinweis:* der durch eben jenen Commit ausgelöste neuere Run
  `42111278228` ist im Log nicht vermerkt — chronologisch
  nachvollziehbar, beide Runs sind grün.
- `installation.md`: Paketkompatibilitäts-Tabelle und neue
  Sonderregelung decken sich mit Code und Skript (inkl. Slice-Tabelle
  und der beobachteten `ld`-Fehlermeldung).
- Keine Secrets/Schreibzugriffe im Diff (Pattern-Grep negativ);
  `.work/` ist untracked und per `.gitignore` ausgeschlossen.

**(f) `dotnet test VideoPlayer.Maui.Tests` — 1472/1472 bestanden**
(0 Fehler, 0 übersprungen, ~1 m 13 s, net10.0). Einzige Warnung:
bereits bestehende NU1903 (`Microsoft.AspNetCore.SignalR.Common`
10.0.10), nicht aus diesem Diff.

**Externe Verifikation (CI):** `gh run view 42111278228` bestätigt
beide Jobs grün — `verify` 6m17s, `build-tvos` 9m49s. Damit ist der
tvOS-Link unter `tvossimulator-arm64` objektiv nachgewiesen. Auch der
im `changes.log` referenzierte Vorgänger-Run `42110023939` war grün
(`build-tvos` 5m54s, `verify` 7m11s).

## Abweichungen

Keine.
