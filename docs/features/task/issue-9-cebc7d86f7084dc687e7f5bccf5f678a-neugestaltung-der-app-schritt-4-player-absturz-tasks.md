# Tasks: Videoplayer — wiederholte Wiedergabe ohne Absturz

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | UI | `subscribed`-Feld in `PlayerPage` einführen (Setzen in `OnAppearing`/`OnDisappearing`) | Offen | — |
| 2 | UI | `ModelChanged`-Abo vom Konstruktor nach `OnAppearing` verlagern (`-=` vor `+=`) | Offen | — |
| 3 | UI | `OnDisappearing`: `subscribed = false` + `ModelChanged -=` vor `PlayerClosedMessage`-Dispatch | Offen | — |
| 4 | UI | `OnModelChanged`: `UpdateUi` im gemarshallten Callback an `subscribed` binden | Offen | — |
| 5 | UI | `OnMediaOpened`: `SeekTo` über `MainThread.BeginInvokeOnMainThread` ausführen, an `subscribed` binden | Offen | — |
| 6 | UI | `OnMediaStateChanged`: `ReportPosition` über `MainThread.BeginInvokeOnMainThread` ausführen, an `subscribed` binden | Offen | — |
| 7 | UI | `OnMediaEnded`/`OnMediaFailed`: Frühreturn bei `!subscribed` | Offen | — |
| 8 | Tests | Contract-Test `PlayerPage_UnsubscribesModelChangedOnDisappearing` | Offen | — |
| 9 | Tests | Contract-Test `PlayerPage_MarshalsNativeElementAccessOnMainThread` | Offen | — |
| 10 | Tests | Contract-Test `PlayerPage_IgnoresModelChangesAfterDisappearing` | Offen | — |
| 11 | Verifikation | Build Debug + Release, volle Testsuite | Offen | — |
