# Plan-Gegenprüfung

**Status:** Plan vollständig

| AK | Abdeckung | Status |
|----|-----------|--------|
| 1 Hintergrund-Abschluss | iOS-Backend + Delegate-Verschiebung | Abgedeckt (Unit/Contract, iOS-Gerätetest offen) |
| 2 Wartende rücken nach | PumpQueue + Resume/Load-Pfade | Abgedeckt |
| 3 Abbruch/Löschen | Backend-Cancel je Key | Abgedeckt |
| 4 Regeln unverändert | Quota/WLAN/Parallelität im Service | Abgedeckt |
| 5 Neustart-Konsistenz | HandleEvents + Reconcile/Resume | Abgedeckt |

## E2E-Abdeckung
iOS-Hintergrund auf diesem Windows-System nicht ausführbar → plattformneutrale
Tests + Contract-Tests (Projektkonvention).
