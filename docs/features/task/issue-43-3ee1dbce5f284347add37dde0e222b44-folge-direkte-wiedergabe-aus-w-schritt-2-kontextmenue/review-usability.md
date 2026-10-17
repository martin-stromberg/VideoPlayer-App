# Usability-Review

## Ergebnis

**Status:** Keine Befunde

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:

- **2-Sekunden-Long-Press auf einen Weiterschauen-Eintrag → Kontextmenü öffnet sich** → unauffällig. Der `PointerGestureRecognizer` an der Karten-`Border` (`MediaCarousel.xaml`) reagiert auf Drücken überall auf der Karte; die 2-s-Haltedauer (`LongPressDurationMs = 2000`) entspricht der Anforderung. Loslassen vor Ablauf bricht ab — der kurze Tap bleibt die gewohnte Auswahl (Direktwiedergabe bzw. Detailansicht).
- **Menü zeigt die Aktionen „Ausblenden" und „Überspringen"** → unauffällig. `DisplayActionSheet` mit dem Eintragstitel als Überschrift (Kontext klar) und den deutschen Klartext-Aktionen; die Beschriftungen entsprechen exakt der Anforderung/Serveranwendung. Kein interner Wert (Id, Medientyp) muss eingegeben oder gekannt werden — der Eintrag ist über den Long-Press identifiziert.
- **„Abbrechen"-Möglichkeit** → unauffällig. Der Cancel-Button „Abbrechen" ist vorhanden; ein Zurück ohne Aktion ist jederzeit möglich.
- **Aktion führt zum Verschwinden des Eintrags aus der Liste** → unauffällig (Nachweis im E2E-Test); die Liste aktualisiert sich über die bestehende Cache-Geändert-Meldung ohne manuelle Neuladen-Aktion.
- **Beschränkung auf die Weiterschauen-Liste** → unauffällig. `LongPressEnabled="True"` ist nur am Weiterschauen-Karussell gesetzt; „Favoriten" und „Neu im Programm" reagieren unverändert nur auf Tap — keine versehentliche Änderung anderer Bedienwege.

## Geprüfte Dateien

Liste aller geprüften UI-Dateien (Basisbranch `task/issue-43-3ee1dbce5f284347add37dde0e222b44-folge-direkte-wiedergabe-aus-w`):

- `VideoPlayer.Maui/Controls/MediaCarousel.xaml`
- `VideoPlayer.Maui/Controls/MediaCarousel.xaml.cs`
- `VideoPlayer.Maui/Pages/HomePage.xaml`
- `VideoPlayer.Maui/Pages/HomePage.xaml.cs`

## Hinweis (kein Befund)

Ein Long-Press ist eine wenig sichtbare Interaktion — die Anforderung fordert diese explizit (analog zur Serveranwendung), daher keine zusätzliche Entdeckbarkeits-UI nötig.
