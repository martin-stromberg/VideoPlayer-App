# Code-Review

Basisbranch: `task/issue-9-cebc7d86f7084dc687e7f5bccf5f678a-neugestaltung-der-app`

## Ergebnis

**Status:** Keine Befunde

## Befunde

Keine.

Anmerkungen (keine Befunde, zur Nachvollziehbarkeit):

- `subscribed` und `resumeApplied` sind `volatile` — sie werden auf dem
  UI-Thread geschrieben (`OnAppearing`/`OnDisappearing`/`UpdateUi`) und
  auf dem jeweiligen Event-Thread gelesen (`OnMediaOpened`,
  `OnMediaEnded`, `OnMediaFailed`). Ohne `volatile` wäre die
  Sichtbarkeit der Abmeldung auf dem Event-Thread nicht garantiert.
- Der Marshal-Block (`BeginInvokeOnMainThread` + `subscribed`-Guard)
  wiederholt sich dreimal — bewusst nicht in eine Hilfsmethode
  ausgelagert, da die drei Handler unterschiedliche Nutzlasten haben und
  die Muster-Konsistenz mit `OnModelChanged` erhalten bleibt.
- `OnPipClicked` bleibt ungeschützt: reine UI-Eingabe auf der sichtbaren
  Seite, dispatcht nur eine Message, kein nativer Elementzugriff.
- `MethodBody` im Test nutzt `Assert.True` als Vorbedingungsprüfung —
  konsistent mit der Contract-Test-Konvention (Assertions in
  Testhelfern sind im Projekt üblich).

## Geprüfte Dateien

- `VideoPlayer.Maui/Pages/PlayerPage.xaml.cs`
- `VideoWebPlayer.Maui.Tests/PlayerContractTests.cs`
