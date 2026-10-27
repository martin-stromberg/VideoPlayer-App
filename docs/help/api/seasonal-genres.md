# API: Saisonale Startseitenlisten

Mobile App und Webansicht verwenden denselben authentifizierten Endpunkt:

```http
GET /api/home/seasonal-genres
Authorization: Bearer <Benutzer-Token>
```

Ein nicht authentifizierter Aufruf folgt dem vorhandenen Bearer-Verhalten
(HTTP 401). Die erfolgreiche Antwort ist ein JSON-Array in der Reihenfolge der
Genre-IDs. Leere Antwort ist `200 []`.

```json
[
  {
    "genreId": 42,
    "name": "Halloween",
    "expiresAt": "2026-11-01T00:00:00Z",
    "entries": [
      { "$type": "movie", "id": 123, "name": "Beispielfilm", "mediaSourceId": 1, "posterPictureId": 456 }
    ]
  }
]
```

`entries` verwendet die vorhandene polymorphe Medienrepräsentation mit den
Diskriminatoren `movie`, `movieCollection` und `show`. Die vollständigen Felder
entsprechen den jeweiligen Medien-DTOs und enthalten erforderlichenfalls den
Detailkontext, Bildkennungen und den benutzerspezifischen Gesehen-Status.
Nicht unterstützte Typen dürfen nicht als Film interpretiert werden.

Der Server filtert Medien anhand der Quellenberechtigungen des angemeldeten
Benutzers. Sammlungen werden dedupliziert; eine Sammlung mit genau einem Film
wird als dieser Film geliefert. Filme ohne Sammlung und Serien werden einzeln
aufgeführt. Je Genre wird nach `EndedAt`, `ReleaseDate`, `PremieredAt` und
`CreatedAt` absteigend, danach nach Name aufsteigend sortiert. Genres ohne
Medien fehlen in der Antwort.

Saisonfenster werden jährlich anhand von UTC-Kalendertagen und mit inklusivem
Start- und Enddatum ausgewertet. `expiresAt` ist die exklusive Grenze um 00:00
UTC am Tag nach dem letzten aktiven Saisontag. Clients berechnen keine eigenen
Saisonzeiträume; sie zeigen die Sektion nur an, solange `nowUtc < expiresAt`.
HTTP- und Transportfehler sind von `200 []` zu unterscheiden. Ein Fehler
ersetzt den mobilen Cache nicht; er verlängert jedoch nicht die sichtbare
Gültigkeit abgelaufener Sektionen.

## Bereitstellungsreihenfolge

Zuerst den Server mit diesem Endpunkt bereitstellen, anschließend die mobile
App verteilen. Die App erwartet den saisonalen API-Vertrag. Die Webansicht des
Servers nutzt ebenfalls diesen Endpunkt.

Weitere Implementierungs- und Testdetails enthält die
[Integrationsdokumentation](../../integration/seasonal-genres/README.md).
