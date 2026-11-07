# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| ≥2 Wiedergaben hintereinander ohne Absturz, beliebig wiederholbar | Umsetzungsreihenfolge Schritt 1: `subscribed`-Guard + Abo-Umzug (`OnAppearing`/`OnDisappearing`) beseitigt verwaiste Seiten; Marshal aller nativen Zugriffe beseitigt die Fremdthread-Quelle | `PlayerPage_UnsubscribesModelChangedOnDisappearing`, `PlayerPage_IgnoresModelChangesAfterDisappearing` + dokumentierter Geräte-Workflow | Abgedeckt |
| `COMException 0x8001010E` tritt nicht mehr auf | `OnMediaOpened`: `SeekTo` über `MainThread.BeginInvokeOnMainThread`; `OnMediaStateChanged`: `ReportPosition` gemarshalt; `UpdateUi` nur noch über Guard | `PlayerPage_MarshalsNativeElementAccessOnMainThread` | Abgedeckt |
| Ursache statt Symptom: kein Zugriff auf natives Element von Fremdthread; verwaiste Seiten reagieren nicht | Beide Ursachen sind eigene Planelemente (Marshal + Unsubscribe + `subscribed`-Guard inkl. blockierter Media-Event-Messages) | alle drei neuen Contract-Tests | Abgedeckt |
| Fortschrittsmeldung an Server und Gesehen-Markieren unverändert | Plan fasst `AppUpdate`/`ReportPlaybackProgressEffect`/`IsWatched` nicht an (Abschnitt „Unberührt" in der Bestandsaufnahme; Programmablauf 2) | bestehende `AppUpdateTests_Player` bleiben unverändert grün | Abgedeckt |
| Alle Tests grün; Build Debug+Release fehlerfrei | Umsetzungsreihenfolge Schritt 3 (Verifikation) | voller Suite-Lauf + beide Builds | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Wiedergabe → zurück → erneute Wiedergabe ohne Absturz | keiner | Nicht erforderlich mit Begründung: keine UI-/Geräte-Automatisierung im Repo; der Fehler ist ein natives COM-Threading-Problem und auf dem Testhost nicht reproduzierbar. Nachweis per Contract-Tests + dokumentiertem Geräte-Workflow (Akzeptanz lässt beides zu). |

## Hinweise

- Die `PlayerClosedMessage`-Abfolge bleibt bewusst unverändert; das
  Unsubscribe vor dem Dispatch verhindert lediglich, dass die eigene
  verlassene Seite den Reset rendert.
- Andere Seiten mit demselben `ModelChanged`-Leck sind ausdrücklich
  außerhalb des Schritts (kein Absturzrisiko, da kein natives Element).
