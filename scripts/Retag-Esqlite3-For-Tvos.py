#!/usr/bin/env python3
"""Retaggt die statischen e_sqlite3-Archive von SQLitePCLRaw.lib.e_sqlite3
fuer tvOS, indem das LC_BUILD_VERSION-Plattformfeld jedes Mach-O-Objekts
im ar-Archiv umgeschrieben wird.

Hintergrund: SQLitePCLRaw.lib.e_sqlite3.tvos (alle Versionen bis
einschliesslich 2.1.14) liefert in der eingebetteten e_sqlite3.a nur
die Slices tvos-arm64 (Geraet) und tvossimulator-x64 — fuer
tvossimulator-arm64 existiert upstream kein Build, sodass der
Simulator-Link auf Apple Silicon fehlschlaegt (ld: built for 'tvOS').

Die iOS-Varianten aus SQLitePCLRaw.lib.e_sqlite3 (3.53.3) sind binaer
identisch nutzbar: iossimulator und tvossimulator teilen Architektur,
ABI und SDK-Featuremenge; nur das Plattform-Tag unterscheidet sich.
Gleiches gilt fuer ios-arm64 -> tvos-arm64.

Plattform-IDs (mach-o/loader.h):
    2 = PLATFORM_IOS, 3 = PLATFORM_TVOS,
    7 = PLATFORM_IOSSIMULATOR, 8 = PLATFORM_TVOSIMULATOR

Aufruf (Quelle aus dem NuGet-Cache, Ziele unter
VideoPlayer.TvOS/NativeLibs/):

    python scripts/Retag-Esqlite3-For-Tvos.py
        <nuget>/sqlitepclraw.lib.e_sqlite3/<ver>/runtimes/ios-arm64/native/e_sqlite3.a
        VideoPlayer.TvOS/NativeLibs/e_sqlite3.tvos-arm64.a --from 2 --to 3
    python scripts/Retag-Esqlite3-For-Tvos.py
        <nuget>/sqlitepclraw.lib.e_sqlite3/<ver>/runtimes/iossimulator-arm64/native/e_sqlite3.a
        VideoPlayer.TvOS/NativeLibs/e_sqlite3.tvossimulator.a --from 7 --to 8

Das Skript aendert ausschliesslich das 4-Byte-Plattformfeld in
LC_BUILD_VERSION-Ladekommandos; Groessen und Offsets bleiben identisch
(das Archiv samt SYMDEF-Index bleibt gueltig).
"""

import struct
import sys


LC_BUILD_VERSION = 0x32
LC_VERSION_MIN_IPHONEOS = 0x25
LC_VERSION_MIN_TVOS = 0x2F
CPU_TYPE_X86_64 = 0x01000007


def patch_object(obj: bytearray, src_platform: int, dst_platform: int) -> int:
    """Setzt die Plattform-Markierung eines 64-Bit-Mach-O-Objekts von
    src_platform auf dst_platform:

    - LC_BUILD_VERSION (0x32): Feld `platform` wird umgeschrieben.
    - LC_VERSION_MIN_IPHONEOS (0x25, Legacy-Tag ohne explizite
      Simulator-Markierung — bei x86_64 impliziert die Architektur den
      Simulator): wird auf LC_VERSION_MIN_TVOS (0x2f, identisches
      Layout) umgeschrieben, wenn das Ziel zur tvOS-Familie gehoert —
      bei arm64-Objekten nur fuer das Geraete-Ziel 3, fuer x86_64
      (implizit Simulator) auch beim Simulator-Ziel 8.
    """
    if len(obj) < 32 or obj[:4] != b"\xcf\xfa\xed\xfe":
        return 0
    (cputype,) = struct.unpack_from("<I", obj, 4)
    (ncmds,) = struct.unpack_from("<I", obj, 16)
    off = 32
    patched = 0
    for _ in range(ncmds):
        cmd, cmdsize = struct.unpack_from("<II", obj, off)
        if cmd == LC_BUILD_VERSION:
            (platform,) = struct.unpack_from("<I", obj, off + 8)
            if platform == src_platform:
                struct.pack_into("<I", obj, off + 8, dst_platform)
                patched += 1
        elif cmd == LC_VERSION_MIN_IPHONEOS and (
            dst_platform == 3
            or (dst_platform == 8 and cputype == CPU_TYPE_X86_64)
        ):
            struct.pack_into("<I", obj, off, LC_VERSION_MIN_TVOS)
            patched += 1
        off += cmdsize
    return patched


def patch_archive(data: bytearray, src_platform: int, dst_platform: int) -> int:
    """Durchlaeuft ein statisches ar-Archiv (duenn oder fat/universal)
    und patcht jedes enthaltene Mach-O-Objekt."""
    total = 0

    def patch_thin(base: int, end: int) -> int:
        nonlocal total
        if data[base : base + 8] != b"!<arch>\n":
            return 0
        pos = base + 8
        while pos + 60 <= end:
            header = data[pos : pos + 60]
            if header[58:60] != b"\x60\x0a":
                break
            try:
                size = int(header[48:58].decode("ascii").strip())
            except ValueError:
                break
            body_start = pos + 60
            name = header[:16].decode("ascii", errors="replace").strip()
            obj_start = body_start
            if name.startswith("#1/"):  # BSD-Langname: Name im Content
                obj_start += int(name[3:])
            total += patch_object(
                memoryview(data)[obj_start : body_start + size],
                src_platform,
                dst_platform,
            )
            pos = body_start + size + (size % 2)
        return total

    if data[:4] == b"\xca\xfe\xba\xbe":  # fat/universal (big-endian)
        (nfat,) = struct.unpack_from(">I", data, 4)
        for i in range(nfat):
            _, _, file_off, file_size, _ = struct.unpack_from(
                ">IIIII", data, 8 + i * 20
            )
            patch_thin(file_off, file_off + file_size)
    else:
        patch_thin(0, len(data))
    return total


def main() -> int:
    # Aufrufkonvention: <src.a> <dst.a> --from <src_platform> --to <dst_platform>
    args = sys.argv[1:]
    if len(args) != 6 or args[2] != "--from" or args[4] != "--to":
        print(__doc__)
        return 2
    src, dst = args[0], args[1]
    src_platform = int(args[3])
    dst_platform = int(args[5])

    with open(src, "rb") as handle:
        data = bytearray(handle.read())
    patched = patch_archive(data, src_platform, dst_platform)
    if patched == 0:
        print(
            f"FEHLER: keine LC_BUILD_VERSION mit Plattform {src_platform} "
            f"in {src} gefunden — nichts geschrieben.",
            file=sys.stderr,
        )
        return 1
    with open(dst, "wb") as handle:
        handle.write(data)
    print(f"{dst}: {patched} LC_BUILD_VERSION-Eintraege "
          f"{src_platform} -> {dst_platform} retaggt.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
