# Übersetzte Anforderung: Saisonabhängige Listen in der mobilen App

## Fachliche Zusammenfassung

Die mobile App soll auf ihrer Startseite saisonale Genre-Listen unterstützen. Diese Listen haben eine zeitlich begrenzte Sichtbarkeit und sollen passende Medien zu Anlässen wie Halloween oder Weihnachten anbieten. Server/API, Webansicht und mobile App werden gemeinsam angepasst: Der Server stellt zuerst einen authentifizierten Vertrag bereit; Webansicht und Mobile-App beziehen saisonale Listen danach über diesen Vertrag. Der Server entscheidet, welche Genres aktiv sind. Clients implementieren keine eigene Saisonberechnung; sie werten den vom Server gelieferten absoluten Ablaufzeitpunkt aus.

## Akzeptanzkriterien

1. Der Server bietet einen authentifizierten API-Vertrag für aktive saisonale Genre-Sektionen; die Webansicht und die mobile App verwenden ihn.
2. Die mobile Startseite kann die vom Server gelieferten Genre-Sektionen mit passenden Medien anzeigen und Einträge über den vorhandenen Detailfluss öffnen.
3. Umfang, Gruppierung, Reihenfolge und Sortierung entsprechen der bisherigen Webansicht: Filme, Serien und Filmsammlungen; Singleton-Sammlungen werden als Film angezeigt.
4. Saisonfenster werden ausschließlich serverseitig entschieden. Der Server verwendet die vorhandene jährliche, inklusive UTC-Datumssemantik.
5. Genres ohne Medien werden nicht ausgeliefert und nicht angezeigt.
6. Bei einem Abruffehler bleibt ein vorheriger mobiler Cache erhalten; eine erfolgreiche leere Antwort löscht die saisonalen Sektionen. Der Server liefert je Sektion einen absoluten UTC-Ablaufzeitpunkt am Ende der konkreten Saison (exklusiv: 00:00 UTC nach dem letzten inklusiven Saisontag). Offline und bei Abruffehlern bleibt eine Sektion nur vor diesem Zeitpunkt sichtbar. Abgelaufene Sektionen werden auch auf einer bereits geöffneten Startseite entfernt; ein Fehler verlängert ihre Sichtbarkeit nicht.

## Konfiguration

Die Anforderung nennt keine appseitige Konfiguration.

## Offene Fragen

Keine. Nutzerentscheidungen zu Server/API zuerst, Webansicht auf API umstellen, Medienumfang/Sortierung/Gruppierung, serverseitiger Zeitlogik, Ende der Saison als Ablaufgrenze, leeren Genres sowie Cache- und Fehlerregeln sind beantwortet.

