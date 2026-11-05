← [Zurück zur Übersicht](index.md)

# Apple-TV-App (tvOS) — Beschreibung

## Zweck

Die Apple-TV-App ist die eigenständige tvOS-Ausgabe der Heimkino-Anwendung.
Sie soll persönliche Medienbibliotheken eines selbst gehosteten
VideoPlayer-Servers auf dem Fernseher verfügbar machen — als Lean-back-Player,
der sich per Siri Remote bedienen lässt.

## Funktionsweise

Der aktuelle Stand ist eine minimale Programmhülle: Beim Start zeigt die App
eine dunkle Startansicht mit dem Titel „Heimkino TV". Fachliche Funktionen
(Serververbindung, Browsing, Wiedergabe, Downloads) folgen in den nächsten
Entwicklungsschritten — die App nutzt dafür denselben plattformneutralen Kern
wie die Hauptanwendung.

## Einschränkungen

- Die Programmhülle enthält bewusst keine Bedienelemente oder Inhalte.
- Eine Veröffentlichung (App Store / TestFlight) ist noch nicht eingerichtet;
  die dafür nötigen Signatur-Artefakte liegen nicht vor.
