# Usability-Review

## Ergebnis

**Status:** Abgeschlossen — keine Befunde.

## Prüfpunkte

| Frage | Beurteilung |
|-------|-------------|
| Findet ein nicht-technischer Anwender die Einstellung? | Ja — eigener Abschnitt „Protokoll" am Ende der Einstellungsseite mit dem Schalter „Protokollierung", konsistent zum bestehenden Karten-Layout der anderen Abschnitte. |
| Versteht er, was das Einschalten bewirkt? | Ja — „Protokollierung" in Kombination mit dem erst danach sichtbaren Button „Protokoll senden" macht klar: die App schreibt ein Protokoll, das gesendet werden kann. |
| Ist erkennbar, wann der Versand verfügbar ist? | Ja — der Versand-Bereich (Trennlinie + Button) erscheint nur bei aktiviertem Schalter; der Zusammenhang ist räumlich direkt darunter. |
| Versteht er, warum der Versand scheitert? | Ja — ohne Versandweg erscheint der Inline-Hinweis „Der Protokollversand ist auf diesem Gerät nicht möglich — es konnte weder ein E-Mail-Entwurf geöffnet noch die Datei geteilt werden." (verständlicher Fließtext, keine Exception/Fehlernummern); ohne Protokolldatei „Es ist noch kein Protokoll vorhanden." |
| Technische Begriffe? | „Protokoll"/„Protokollierung" entspricht wörtlich der Formulierung der Anforderung; im Mailentwurf steht deutsch „Heimkino – Programmprotokoll". Keine IDs, keine Klassennamen in der UI. |
| Inkonsistente Zustände? | Deaktivieren der Protokollierung blendet den Versand-Bereich sofort aus und beendet das Schreiben (Marker-Zeile); der Update-Guard verhindert zusätzlich veraltete Versand-Anforderungen. Fehlerhinweise werden beim erneuten Versuch zurückgesetzt (`LogSendError = null` vor dem Effekt). |

## Hinweise

- Der Teilen-Fallback zeigt bei fehlendem Mail-Client den Windows-Teilen-Dialog — ein verständliches, plattformübliches Bedienelement ohne Fehlermeldung. Das ist bewusst so gestaltet (Anforderung: „ggf. Share-Fallback").
