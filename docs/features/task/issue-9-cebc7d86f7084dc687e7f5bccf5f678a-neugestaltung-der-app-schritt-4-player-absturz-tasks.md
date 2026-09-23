# Tasks: Videoplayer — wiederholte Wiedergabe ohne Absturz

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | UI | `subscribed`-Feld in `PlayerPage` einführen (Setzen in `OnAppearing`/`OnDisappearing`) | Erledigt | `PlayerPage_IgnoresModelChangesAfterDisappearing` |
| 2 | UI | `ModelChanged`-Abo vom Konstruktor nach `OnAppearing` verlagern (`-=` vor `+=`) | Erledigt | `PlayerPage_UnsubscribesModelChangedOnDisappearing` |
| 3 | UI | `OnDisappearing`: `subscribed = false` + `ModelChanged -=` vor `PlayerClosedMessage`-Dispatch | Erledigt | `PlayerPage_UnsubscribesModelChangedOnDisappearing` |
| 4 | UI | `OnModelChanged`: `UpdateUi` im gemarshallten Callback an `subscribed` binden | Erledigt | `PlayerPage_IgnoresModelChangesAfterDisappearing` |
| 5 | UI | `OnMediaOpened`: `SeekTo` über `MainThread.BeginInvokeOnMainThread` ausführen, an `subscribed` binden | Erledigt | `PlayerPage_MarshalsNativeElementAccessOnMainThread` |
| 6 | UI | `OnMediaStateChanged`: `ReportPosition` über `MainThread.BeginInvokeOnMainThread` ausführen, an `subscribed` binden | Erledigt | `PlayerPage_MarshalsNativeElementAccessOnMainThread` |
| 7 | UI | `OnMediaEnded`/`OnMediaFailed`: Frühreturn bei `!subscribed` | Erledigt | `PlayerPage_IgnoresModelChangesAfterDisappearing` |
| 8 | Tests | Contract-Test `PlayerPage_UnsubscribesModelChangedOnDisappearing` | Erledigt | Test selbst (Negativprobe auf Altstand fehlgeschlagen) |
| 9 | Tests | Contract-Test `PlayerPage_MarshalsNativeElementAccessOnMainThread` | Erledigt | Test selbst (Negativprobe auf Altstand fehlgeschlagen) |
| 10 | Tests | Contract-Test `PlayerPage_IgnoresModelChangesAfterDisappearing` | Erledigt | Test selbst (Negativprobe auf Altstand fehlgeschlagen) |
| 11 | Verifikation | Build Debug + Release, volle Testsuite | Erledigt | 672/672 grün, beide Builds 0 Fehler (`test-results.md`) |
