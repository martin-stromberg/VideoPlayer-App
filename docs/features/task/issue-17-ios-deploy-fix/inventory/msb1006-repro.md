# Detail: MSB1006-Reproduktion

Methode: minimales Test-csproj (`net10.0`-Bibliothek) in einem
Temp-Verzeichnis, Aufruf `dotnet build min.csproj @test.rsp` mit
gezielt präparierten Response-Dateien. dotnet SDK 10.0.401, Windows.

## Ergebnistabelle

| rsp-Zeile (Szenario) | Ergebnis |
|----------------------|----------|
| `-p:Token="plainvalue"` | OK |
| `-p:Token="  abc  "` (Whitespace im Wert) | OK |
| `-p:Token="<257 Zeichen>"` (langer Wert) | OK |
| `-p:Token="a=b"`, `="a;b;c"`, `="my secret token"`, `="abc\+"` (Ende `\`) | OK |
| `-p:Token="abc#def"` bzw. unquotiert `=abc#def` | OK — `#` ist **nur am Zeilenanfang** Kommentarzeichen |
| `# kommentar` als eigene Zeile | OK (wird als Kommentar ignoriert) |
| `-p:Token="abc" # x` | **MSB4177** (Rest wird als Eigenschaftsname gelesen) |
| `-p:Token="a" b"` (`"` + Leerzeichen im Wert) | **MSB4177** |
| `-p:Token="a"=b"` (`"` gefolgt von `=` im Wert) | **MSB1006** |
| `-p:Token="ab""` (Wert endet auf `"`) | **MSB1006** |
| `-p:Token="""` (Wert ist nur `"`) | **MSB1006** |
| `-p:=abc` (leerer Eigenschaftsname) | **MSB1006** |
| `-p:Token="abc`<LF>`def"` (Zeilenumbruch im Wert) | **MSB1008** („nur ein Projekt") |
| `-p:Token="abc`<LF>`-p:=x"` (Umbruch + Folgezeile `-p:`) | **MSB1006** |
| `-p:Token="a`<LF>`#x"` (Folgezeile beginnt mit `#`) | OK — Folgezeile faellt als Kommentar weg, **Wert wird still gekuerzt** |
| `-p:Token` (ohne `=`), `-p:` allein, `-p:Token=` | OK (leere Eigenschaft) |
| `-p:My Prop="x"` (Leerzeichen im Namen) | **MSB4177** |
| `-p:Token='abc'` (einfache Quotes) | OK — `'…'` wird zum Literalwert |

## Bewertung der Hypothesen aus der Projekt-Inventur

1. **`"` im Token** — **bestaetigt als MSB1006-Ausloeser.** Je nach
   Position erzeugt das Quote beim Response-Datei-Tokenizer ein
   `-p:`-Argument ohne Namen (Wert endet auf `"`, Wert ist `"`, `"=`
   im Wert) oder MSB4177 (`"` + Leerzeichen). Ein mitkopiertes
   Anfuehrungszeichen am Token-Ende ist ein realistischer
   Eingabefehler.
2. **`#` im Token** — **widerlegt als MSB1006-Ausloeser** (mid-line
   harmlos), aber zwei Nebenbefunde: `#` am Zeilenanfang (nur nach
   Umbruch moeglich) kuerzt den Wert still; `#` nach schliessendem
   Quote erzeugt MSB4177.
3. **Zeilenumbruch im Token** — **bestaetigt als Fehlerausloeser**
   (MSB1008 typischerweise; MSB1006, wenn die Folgezeile als `-p:`-
   Argument ohne Namen gelesen wird). Ein mitkopierter Zeilenumbruch
   (z. B. aus einer mehrzeiligen Datei) ist realistisch.
4. **Whitespace** — harmlos, aber Trimmen schadet nicht.
5. **Langer Wert (257 Zeichen)** — harmlos, Laenge ist nicht die
   Ursache.

## Schlussfolgerung (dokumentierte Ursache)

`MSB1006` wird ausgeloest, wenn MSBuild aus der Response-Datei ein
`-p:`-Argument ohne gueltigen Eigenschaftsnamen liest. Aus einem
Token-Wert erzeugbar ueber:

- `"` im Wert (insbesondere am Wertende oder gefolgt von `=`),
- einen Zeilenumbruch im Wert, dessen Folgezeile als eigenes
  `-p:`-Argument geparst wird.

Weil `Add-PropertyLine` weder trimmt noch Zeichen prueft, kommt ein
solcher Wert ungeprueft in die rsp-Datei — MSBuild meldet dann nur
das kryptische `MSB1006` ohne Zeilenangabe.
