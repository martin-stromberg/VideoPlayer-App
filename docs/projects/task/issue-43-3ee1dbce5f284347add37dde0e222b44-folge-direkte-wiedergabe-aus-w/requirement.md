# Anforderung: Folge: Direkte Wiedergabe aus Weiterschauen, Protokollversand und Playlist-Support

**Issue:** #43
**Aufgaben-ID:** 3ee1dbce-5f28-4347-add3-7dde0e222b44
**Branch:** `task/issue-43-3ee1dbce5f284347add37dde0e222b44-folge-direkte-wiedergabe-aus-w`
**Quelle:** `issue.md` (Repository-Root)

## Ziel

Die App soll Einträge der Weiterschauen-Liste direkt ab der Fortsetzungsposition abspielen, ein per Mail versendbares Programmprotokoll anbieten, die Playlist-Funktionalität des Servers unterstützen und die beschriebenen Bedienfehler (iOS-Serienansicht, fehlendes Kontextmenü) beheben.

## Fachliche Anforderungen

### 1. Direkte Wiedergabe aus der Weiterschauen-Liste

Wird ein Eintrag aus der **Weiterschauen-Liste** aufgerufen, soll nicht nur dessen Detailansicht geladen werden, sondern direkt das Abspielen des Videos initiiert werden (ab der Fortsetzungsposition).

Bei den **anderen Listen** (Favoriten, „Neu im Programm", Quellen-Übersichten) soll das nicht passieren — dort öffnet sich weiterhin nur die Detailansicht.

### 2. Protokollierung und Protokollversand

In den **Einstellungen** wird eine Möglichkeit benötigt, die Protokollierung zu aktivieren. Ist diese aktiviert, soll das Programmprotokoll über einen **„Protokoll senden"-Button** als Anhang in einem Mailentwurf geöffnet werden.

Die Mail soll an **mstromberg84+heimkino@gmail.com** adressiert sein.

### 3. Playlist-Unterstützung

Die **Playlist-Funktionalität des Servers** soll nun auch in der App unterstützt werden. Die Playlists sollen über das **Menü** aufgerufen werden können.

### 4. Wiedergabe- und Download-Reihenfolge bei Playlists

- Sichergestellt werden muss, dass beim Abspielen eines Videos aus der Weiterschauen-Liste das **richtige Folgevideo** bereitgestellt wird, wenn der Weiterschauen-Eintrag aus einer Playlist entstand.
- Genauso muss die **richtige Reihenfolge der Videos beim Download** berücksichtigt werden, falls das noch nicht der Fall ist.

### 5. iOS: Serienansicht — Episodenliste und Staffelauswahl defekt (Bug)

Auf **iOS** zeigt die Serienansicht **keine Episodenliste**.

Zusätzlich verhält sich die **Staffelauswahlbox** fehlerhaft:

- Die Einträge erscheinen in falscher Reihenfolge — Beispiel: „Staffel 02", „Staffel 03" und als letztes „Staffel 01".
- Versucht man eine andere Staffel als „Staffel 01" auszuwählen, **springt die Auswahl immer wieder zurück**.

### 6. Kontextmenü in der Weiterschauen-Liste

Tippt der Anwender auf einen Eintrag der Weiterschauen-Liste für **zwei Sekunden** (Long-Press), soll sich ein **Kontextmenü** öffnen. Genauso wie bei der Serveranwendung soll er dort die Möglichkeit bekommen, den Eintrag **„Ausblenden"** oder **„Überspringen"** zu lassen. Die entsprechende Funktionalität sollte die API bereits bieten.

## Betroffene Bereiche

Fachlich betroffene App-Bereiche (aus der Anforderung ableitbar):

- **Weiterschauen-Liste:** Direkte Wiedergabe statt nur Detailansicht (Anf. 1); Kontextmenü per 2-Sekunden-Long-Press mit „Ausblenden"/„Überspringen" (Anf. 6); richtige Folgevideo-Bereitstellung bei Playlist-Herkunft (Anf. 4).
- **Andere Listen** (Favoriten, „Neu im Programm", Quellen-Übersichten): Verhalten bleibt bei der Detailansicht (Anf. 1) — betroffen nur als explizite Abgrenzung.
- **Detailansicht:** Bleibt Einstiegspunkt der anderen Listen; bei der Weiterschauen-Liste wird sie durch die direkte Wiedergabe ergänzt/ersetzt (Anf. 1 — Annahme: Die Detailansicht bleibt über andere Wege erreichbar; die Anforderung sagt nur „nicht nur dessen Detailansicht geladen").
- **Einstellungen:** Aktivierung der Protokollierung und „Protokoll senden"-Button (Anf. 2).
- **Menü/Navigation:** Aufruf der Playlists (Anf. 3).
- **Wiedergabe:** Start ab Fortsetzungsposition (Anf. 1), Folgevideo-Auflösung bei Playlists (Anf. 4).
- **Downloads:** Reihenfolge der Videos bei Playlist-Bezug (Anf. 4).
- **Serienansicht (iOS):** Episodenliste und Staffelauswahlbox (Anf. 5).

## Abläufe

- **Direkte Wiedergabe:** Anwender tippt auf einen Eintrag der Weiterschauen-Liste → die Wiedergabe des Videos startet direkt ab der Fortsetzungsposition (statt nur die Detailansicht zu öffnen).
- **Detailansicht in anderen Listen:** Anwender tippt auf einen Eintrag in Favoriten, „Neu im Programm" oder einer Quellen-Übersicht → es öffnet sich weiterhin nur die Detailansicht.
- **Protokollversand:** Anwender aktiviert in den Einstellungen die Protokollierung → bei aktivierter Protokollierung betätigt er den „Protokoll senden"-Button → ein Mailentwurf mit dem Programmprotokoll als Anhang wird geöffnet, adressiert an mstromberg84+heimkino@gmail.com.
- **Playlists aufrufen:** Anwender öffnet das Menü → wählt Playlists → die Playlist-Funktionalität des Servers ist in der App nutzbar (Annahme: zumindest Anzeige/Aufruf der Playlists; der konkrete Funktionsumfang ist in der Anforderung nicht weiter beschrieben).
- **Weiterschauen-Eintrag aus Playlist:** Anwender spielt einen Weiterschauen-Eintrag ab, der aus einer Playlist entstand → das richtige Folgevideo (gemäß Playlist-Reihenfolge) wird bereitgestellt.
- **Download mit Playlist-Bezug:** Videos werden in der richtigen (Playlist-)Reihenfolge heruntergeladen.
- **Serienansicht iOS (Soll-Verhalten):** Serienansicht zeigt die Episodenliste; die Staffelauswahlbox listet die Staffeln in richtiger Reihenfolge (z. B. „Staffel 01", „Staffel 02", „Staffel 03") und die Auswahl einer anderen Staffel bleibt bestehen.
- **Kontextmenü:** Anwender hält einen Eintrag der Weiterschauen-Liste zwei Sekunden gedrückt → ein Kontextmenü öffnet sich mit den Aktionen „Ausblenden" und „Überspringen" (wie bei der Serveranwendung) → Auswahl führt die entsprechende Aktion auf dem Eintrag aus.

## Konfiguration

- **Protokollierung:** Über die Einstellungen aktivierbar (Anf. 2).
- **Empfänger-Mailadresse für den Protokollversand:** `mstromberg84+heimkino@gmail.com` (in der Anforderung fest vorgegeben; ob sie konfigurierbar sein soll, ist nicht angegeben — Annahme: fest hinterlegt).
- Weitere konfigurierbare Punkte nennt die Anforderung nicht.

## Akzeptanzkriterien

**Zu Anforderung 1 (Direkte Wiedergabe):**
- Ein Tap auf einen Weiterschauen-Eintrag startet die Wiedergabe direkt, ohne dass zuvor nur die Detailansicht geöffnet wird.
- Die Wiedergabe beginnt an der Fortsetzungsposition, nicht am Anfang.
- Taps auf Einträge in Favoriten, „Neu im Programm" und Quellen-Übersichten öffnen weiterhin nur die Detailansicht (kein automatischer Wiedergabestart).

**Zu Anforderung 2 (Protokollierung/Versand):**
- In den Einstellungen lässt sich die Protokollierung aktivieren.
- Bei aktivierter Protokollierung öffnet der „Protokoll senden"-Button einen Mailentwurf mit dem Programmprotokoll als Anhang.
- Der Mailentwurf ist an mstromberg84+heimkino@gmail.com adressiert.

**Zu Anforderung 3 (Playlists):**
- Die Playlist-Funktionalität des Servers ist in der App nutzbar.
- Playlists sind über das Menü erreichbar.

**Zu Anforderung 4 (Reihenfolge bei Playlists):**
- Wird ein Weiterschauen-Eintrag abgespielt, der aus einer Playlist entstand, wird das richtige Folgevideo gemäß Playlist-Reihenfolge bereitgestellt.
- Downloads von Videos mit Playlist-Bezug erfolgen in der richtigen Reihenfolge.

**Zu Anforderung 5 (iOS-Serienansicht):**
- Die Serienansicht zeigt auf iOS die Episodenliste an.
- Die Staffelauswahlbox zeigt die Staffeln in richtiger Reihenfolge (aufsteigend, „Staffel 01" zuerst).
- Die Auswahl einer anderen Staffel als „Staffel 01" bleibt bestehen und springt nicht zurück.

**Zu Anforderung 6 (Kontextmenü):**
- Ein 2-Sekunden-Long-Press auf einen Weiterschauen-Eintrag öffnet ein Kontextmenü.
- Das Kontextmenü bietet die Aktionen „Ausblenden" und „Überspringen" analog zur Serveranwendung.
- „Ausblenden" und „Überspringen" wirken auf den jeweiligen Eintrag (über die bereits vorhandene API-Funktionalität).

## Nicht-Ziele

- **Keine direkte Wiedergabe in anderen Listen:** Favoriten, „Neu im Programm" und Quellen-Übersichten öffnen weiterhin nur die Detailansicht (explizit in Anf. 1 ausgeschlossen).
- **Keine Server-/API-Erweiterung für „Ausblenden"/„Überspringen":** Die Anforderung geht davon aus, dass die API die Funktionalität bereits bietet (Anf. 6). Ebenso existiert die Playlist-Funktionalität bereits auf dem Server (Anf. 3) — aus der Anforderung sind keine Server-Änderungen ableitbar.
- **Kein Protokollversand ohne aktivierte Protokollierung:** Der Versand ist an die aktivierte Protokollierung gebunden (Anf. 2).
- **Kontextmenü nur in der Weiterschauen-Liste:** Die Anforderung nennt das Kontextmenü ausschließlich für die Weiterschauen-Liste; eine Ausweitung auf andere Listen ist nicht gefordert (Annahme als Abgrenzung, aus dem Wortlaut ableitbar).

## Offene Fragen

- **Anf. 3 — Umfang der Playlist-Unterstützung:** Die Anforderung beschreibt nur, dass die Playlist-Funktionalität des Servers „unterstützt" und über das Menü aufrufbar sein soll. Welche Aktionen konkret möglich sein sollen (nur Anzeige/Abspielen, auch Verwaltung wie Erstellen/Bearbeiten von Playlists?), bleibt offen.
- **Anf. 4 — Ist-Zustand der Download-Reihenfolge:** „falls das noch nicht der Fall ist" — ob die Download-Reihenfolge bereits korrekt ist oder korrigiert werden muss, ist in der Anforderung offen gelassen und bei der Umsetzung zu prüfen.
- **Anf. 6 — API-Abdeckung:** „Die entsprechende Funktionalität sollte die API bereits bieten" — die Formulierung („sollte") lässt offen, ob „Ausblenden"/„Überspringen" tatsächlich vollständig von der API abgedeckt ist; dies ist zu verifizieren.
- **Anf. 1 — Detailansicht bei Weiterschauen:** „nicht nur dessen Detailansicht geladen" — ob die Detailansicht gar nicht mehr geöffnet wird oder vor/hinter der Wiedergabe weiterhin eine Rolle spielt (z. B. nach Beenden der Wiedergabe), ist nicht eindeutig festgelegt.
- **Anf. 2 — Sichtbarkeit des „Protokoll senden"-Buttons:** Ob der Button nur bei aktivierter Protokollierung sichtbar/bedienbar ist oder dauerhaft angezeigt wird, ist nicht spezifiziert (die Funktion selbst ist an die aktivierte Protokollierung gebunden).
