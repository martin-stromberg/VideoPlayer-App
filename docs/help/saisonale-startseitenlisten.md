# Saisonale Startseitenlisten

Die Startseite kann zeitweise Genre-Listen wie „Halloween" oder
„Weihnachten" anzeigen. Welche Genres gerade aktiv sind, legt der Server
anhand der dort gepflegten Saisonzeiträume fest. Die App und die Webansicht
verwenden dieselbe Liste; eine Einstellung in der App ist dafür nicht nötig.

Jede Sektion zeigt passende Filme, Filmsammlungen und Serien. Eine Sammlung
mit genau einem Film wird als Filmkarte dargestellt. Leere Genres erscheinen
nicht. Tippe eine Karte an, um wie gewohnt die zugehörige Detailansicht zu
öffnen.

## Offlinebetrieb und Aktualisierung

Die mobile App kann eine zuvor geladene Sektion aus dem lokalen Cache zeigen.
Ein vorübergehender Verbindungs- oder Serverfehler löscht diesen Cache nicht.
Die Sektion bleibt jedoch nur bis zu ihrem vom Server gelieferten Ablaufzeitpunkt
sichtbar. Wird das Saisonende erreicht, verschwindet sie auch dann, wenn die
Startseite geöffnet bleibt oder das Gerät offline ist. Ein erfolgreicher Abruf
ohne aktive Saisonlisten entfernt die zuvor gespeicherten Sektionen.

## Wenn eine Liste fehlt

Eine Saisonliste erscheint nur, wenn der Server das Genre derzeit als aktiv
führt und mindestens ein für dein Benutzerkonto freigegebenes Medium dazu
gefunden wurde. Prüfe bei fehlenden Inhalten, ob du online bist und ob dein
Konto Zugriff auf die passende Medienquelle hat. Die Saisonzeiträume werden
serverseitig gepflegt.

Technische Einzelheiten zum gemeinsamen Web-/App-Vertrag stehen im
[API-Vertrag der saisonalen Startseitenlisten](api/seasonal-genres.md).
