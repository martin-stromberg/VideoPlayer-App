# Fehlerprotokoll-Auswertung: `videoplayer (5).log`

Datei: `videoplayer (5).log` (Repository-Root, 3840 Zeilen). Protokoll stammt von einem iOS-Gerät (Pfade `/var/mobile/Containers/...`, `AVPlayerItem`-Notifications der `MediaManager`-Kategorie). Zeitraum: 17.–18.10.2026.

## Fehlersequenz (dreimal reproduziert)

Identisches Muster bei allen drei Vorkommnissen — jeweils `ServerStream`-Quelle mit gesetzter Fortsetzungsposition:

1. `PlaybackSourceService`: „Wiedergabequelle für {id} aufgelöst: ServerStream, Fortsetzung {pos}."
2. `ShellAppEffectHandler`: „Wiedergabe gestartet: {id} ({titel}) über ServerStream."
3. `PlayerPage`: „Wiedergabe geöffnet: {titel} (Fortsetzung {pos})." (`OnMediaOpened`)
4. ~10–15 s später `PlayerPage` [WARN]: „Fortsetzungs-Seek auf {pos} fehlgeschlagen." mit `System.Threading.Tasks.TaskCanceledException` aus `CommunityToolkit.Maui.Views.MediaElement.SeekTo` → `PlayerPage.ApplyResumeSeekAsync` — das interne 10-s-`ResumeSeekTimeout`.
5. ~0,5–1 s danach: Auflösung und Start der **Folge-Episode** (neuer `ResolvePlaybackSourceEffect`-Zyklus — ausgelöst durch den `PlaybackFailedMessage`-Dispatch im catch-Zweig → `UpdatePlaybackFailed` → `AdvanceToNextEpisode`).

## Die drei Vorkommnisse

| Episode (gestartet) | Fortsetzung | Seek-Fehlschlag (Log-Z.) | Folge-Episode (Log-Z.) |
|---------------------|-------------|--------------------------|------------------------|
| `tvshowepisode/7038` „Der Wolf im Schafspelz" (Z. 1296–1298, 10:28:37) | 00:44:08 | Z. 1299–1302, 10:28:52 | `tvshowepisode/7039` „Kennen Sie Tribbles?" (Z. 1303–1305, 10:28:53) |
| `tvshowepisode/7040` „Meister der Sklaven" (Z. 1679–1681, 12:00:49) | 00:00:35 | Z. 1682–1685, 12:01:03 | `tvshowepisode/7041` „Epigonen" (Z. 1686–1688, 12:01:04) |
| `tvshowepisode/21599` „Der Schulwechsel" (Z. 3826–3828, 10:18:24) | 00:17:04 | Z. 3833–3836, 10:18:35 | `tvshowepisode/21600` „Normale Helden" (Z. 3837–3839, 10:18:35) |

Stack-Trace (identisch, Z. 1300–1302 / 1683–1685 / 3834–3836):

```
System.Threading.Tasks.TaskCanceledException: TaskCanceledException_ctor_DefaultMessage
   at CommunityToolkit.Maui.Views.MediaElement.SeekTo(TimeSpan position, CancellationToken token)
   at VideoPlayer.Maui.Pages.PlayerPage.ApplyResumeSeekAsync(TimeSpan position)
```

## Weitere Beobachtungen

- Die Sequenz bei 21599/21600 ist die in `issue.md` beschriebene: „Der Schulwechsel" läuft (Fortsetzung 00:17:04, also „mitten im Video"), elf Sekunden nach dem Öffnen schlägt der Seek fehl, „Normale Helden" startet.
- Das Protokoll enthält keinen eigenen Eintrag für `PlaybackFailedMessage`/`AdvanceToNextEpisode` (der MVU-Kern loggt nicht auf diesem Level) — der Episodenwechsel ist nur über die anschließende Quellauflösung (Schritt 5) belegt.
- Zuvor (10:00:35, Z. 3606–3608) lief dieselbe Episode 21599 ohne Fortsetzungsposition (`Fortsetzung (null)`) fehlerfrei; dazwischen `AVPlayerItemPlaybackStalledNotification`-Warnungen (Z. 3744, 3762) — Stream-Aussetzer ohne Seek-Bezug. Auch für 7038 lagen `PlaybackStalled`-Warnungen vor (Z. 563, 736, 869, 882).
- `LocalFile`-Wiedergaben im selben Protokoll (Z. 2183, 2273, 3280, 3578) zeigen keinen Seek-Fehlschlag — allerdings hatten diese auch keine bzw. nur eine 00:00:00-Fortsetzungsposition (kein positiver Seek nötig).
- Unabhängige Download-Probleme im selben Protokoll (`DirectoryNotFoundException`, „No space left on device", z. B. Z. 1300-Umfeld/1679-Umfeld) sind nicht Teil der Fehlerkette.
