# Usability-Review — QR-/Ticket-Onboarding (Issue 17, Schritt 5b)

Geprüft aus Endanwendersicht an `ServerSetupPage.xaml`/`.xaml.cs`,
`QrScannerPage.cs`, den MVU-Fehlertexten in `AppUpdate.cs` und den
Live-Screenshots `live-verification/*.png` (2026-09-24). Grundlage ist
die ursprüngliche Anforderung (`requirement.md`), nicht der Plan.

## Leseführung und Orientierung

- Die neue Karte „Mit Kopplungscode verbinden" steht direkt unter der
  Verbinden-Karte und über der klassischen Pairing-Karte — der
  beworbene Weg (QR-Code der Serververwaltung) ist optisch der erste
  geführte Einstieg, ohne die bisherigen Wege zu verdecken.
- Der Führungstext benennt die Herkunft in Endanwender-Sprache:
  „In der Serververwaltung (Profilseite ‚Geräte') liegt ein QR-Code
  bereit — scanne ihn oder gib die Ticket-Adresse bzw. den Kurzcode
  ein. Adresse und Anmeldung entfallen." Die zentrale Erleichterung
  (kein Adressen-Tippen, keine Anmeldung) steht am Satzende — gut
  verständlich.
- Keine internen Kennungen als Endanwender-Eingabe: die Eingabe ist
  die Ticket-Adresse aus dem QR-Code oder ein 8-stelliger Kurzcode —
  beides Werte, die die Serververwaltung dem Anwender explizit anzeigt.
  Es werden keine Geräte-Ids, GUIDs oder Tokens abgefragt.

## Zustandsverständlichkeit

- **Laufzustand:** „Kopplung läuft …" (Text neben dem drehenden
  Indikator) benennt die Phase; Screenshot `bootstrap-02` zeigt den
  Zustand real. Der Text allein würde den Unterschied zwischen
  Serverprüfung und Ticket-Einlösung nicht zeigen — für den Anwender
  ist das eine zusammenhängende Wartezeit, das ist angemessen.
- **Gesperrte Eingaben:** während Prüfung/Bootstrap sind Eingabefeld,
  beide Buttons und der Scan-Button deaktiviert — ein Doppel-Auslösen
  oder ein Widerspruch zwischen Ticket- und Adress-Fluss ist
  ausgeschlossen (Screenshot `bootstrap-02` bestätigt die Sperrung).
- **Erfolg:** der Erfolgsfall ist der stärkste Usability-Gewinn des
  Features — nach dem Einlösen landet die App ohne weitere Eingabe
  direkt auf der Startseite (Screenshot `bootstrap-04`; E2E belegt
  `LoginCount==0`). Es gibt keine überflüssige „Erfolg"-Zwischenseite.

## Fehlerverständlichkeit

- Fehler erscheinen als roter Inline-Text direkt in der
  Kopplungs-Karte (`BootstrapErrorLabel`) — am Ort der zugehörigen
  Eingabe, wie bei Adress- und Pairing-Fehlern.
- Die Texte sind in einfacher Sprache ohne Fachjargon:
  „Kopplungscode nicht lesbar", „Der Server ist nicht erreichbar",
  „Der Kopplungscode ist ungültig oder abgelaufen", „Der Server
  unterstützt das Kopplungs-Verfahren nicht", „Zu viele Versuche —
  bitte später erneut versuchen", „Für den Kurzcode muss zuerst ein
  Server verbunden sein".
- Besonders hilfreich: der Kurzcode-Hinweis („Für den Kurzcode muss
  zuerst ein Server verbunden sein") erklärt den kontextabhängigen
  Unterschied zwischen Ticket-Adresse (bringt den Server mit) und
  Kurzcode (nutzt den verbundenen Server) — die einzige Stelle, an
  der ein Anwender die beiden Formen auseinanderhalten muss.
- Ein ungültiges/abgelaufenes Ticket hinterlässt die App in einem
  sauberen Wiederholzustand: Eingabe bleibt editierbar, Fehler steht
  sichtbar, alle Fallback-Wege offen (Screenshot `bootstrap-03`).

## Scanner-Seite (mobil)

- Die modale `QrScannerPage` zeigt nur das Notwendige: Kamerabild,
  Hinweistext „QR-Code der Serververwaltung scannen", „Abbrechen" —
  kein Fachjargon, kein Optionen-Overhead. Der Scan ist auf
  zweidimensionale Codes beschränkt (weniger Fehltreffer).
- Fehlschläge sprechen Endanwendersprache und nennen die Alternative:
  „Dieses Gerät hat keine nutzbare Kamera. Bitte die Ticket-Adresse
  oder den Kurzcode eingeben." / „Bitte den Kamera-Zugriff erlauben
  oder die Ticket-Adresse bzw. den Kurzcode eingeben." — die
  verweigerte Permission endet nicht in einer Sackgasse.
- `NSCameraUsageDescription` begründet die Permission im
  Systemdialog sachlich („Zum Scannen des Kopplungs-QR-Codes der
  Serververwaltung.").

## Windows-/Desktop-Einschränkung

- Unter Windows ist der Scan-Button unsichtbar (kein Kamera-Pfad) —
  der manuelle Einstieg über das Ticket-Feld bleibt vollständig;
  die Karte erklärt beide Wege, sodass der fehlende Scan-Button
  nicht als Defekt wirkt.

## Befunde

Keine. Der Fluss führt ohne interne Kennungen, mit klaren Phasen- und
Fehlertexten und einem Erfolgsfall ohne Zwischenschritte zum Ziel;
die mobilen Sonderfälle (keine Kamera, verweigerte Permission) weisen
jeweils auf den manuellen Ersatzweg.

**Status: Keine Befunde.**
