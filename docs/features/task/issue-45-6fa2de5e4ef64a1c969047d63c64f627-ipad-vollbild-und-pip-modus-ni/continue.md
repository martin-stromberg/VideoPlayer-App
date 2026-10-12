# Offene Aufgaben

Erstellt am: 2026-10-12
Abbruchgrund: Kein Fortschritt zwischen den letzten zwei Iterationen

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

Keine im Implementierungsumfang — alle geplanten Code- und Testelemente sind umgesetzt.

## Code-Review-Befunde

Keine.

## Usability-Befunde

Keine.

## Fehlgeschlagene Tests

- [ ] Manuell iPad: Vollbild-Button der nativen Leiste antippen → Vollbild öffnet/zurück (AK 1) — erfordert iPad-Gerät + TestFlight-Build; iOS-UI-Automatisierung existiert im Repository nicht
- [ ] Manuell iPad: PiP-Button der nativen Leiste antippen → PiP startet (AK 1) — gleiche iOS-Verifikationsgrenze
- [ ] Manuell iPad: Playlist-Wiedergabe → „Zurück"/„Weiter" auslösbar; Titel, PlaylistContextLabel, WatchedBadge sichtbar (AK 2, 3) — gleiche iOS-Verifikationsgrenze; zugleich ist der Schätzwert `IosNativeControlsClearance = 96` (`PlayerPage.xaml.cs`) auf dem Gerät zu verifizieren
- [ ] Manuell Windows: Mini-Player-Schalter und Transportsteuerung unverändert bedienbar (AK 4) — manueller Schritt; Ersatznachweis: Windows-E2E-Suite 53/53 grün
