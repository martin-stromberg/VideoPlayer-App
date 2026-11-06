# Entwicklungsschritte – Projekt Apple TV-App (tvOS): Lean-back-Player via .NET for tvOS, Deployment nach App Store Connect

Branch: `task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl`

| # | Titel | Abhängigkeiten | Branch | Status |
|---|-------|----------------|--------|--------|
| 1 | tvOS-Projektgerüst und frühe Build-Validierung über macOS-CI | Keine | `task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl-schritt-1-tvos-projektgeruest` | Fertig |
| 2 | MVU-Grundgerüst, Plattform-Anbindung, Navigation und Fokus | 1 | `task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl-schritt-2-mvu-grundgeruest` | Fertig |
| 3 | Serververbindung — Einrichtung, Pairing und Anmeldung | 2 | `task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl-schritt-3-serververbindung` | Fertig |
| 4 | Startseite mit fokussierbaren Karussells | 3 | `task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl-schritt-4-startseite` | Fertig |
| 5 | Quellen- und Genre-Browsing sowie Suche | 3 | `task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl-schritt-5-browsing-suche` | Fertig |
| 6 | Detailseiten für Film, Sammlung, Serie, Staffel und Episode | 4, 5 | `task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl-schritt-6-detailseiten` | Fertig |
| 7 | Wiedergabe mit Fortschritts-Tracking, Auto-Fortsetzung und Spurwahl | 6 | `task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl-schritt-7-wiedergabe` | Fertig |
| 8 | Playlists — lesender Zugriff und Wiedergabe | 7 | `task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl-schritt-8-playlists` | Fertig |
| 9 | Manuelle Downloads über suspensionstaugliche Hintergrund-URL-Session mit Ablaufdatum, Reconcile und Quota | 6, 7 | `task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl-schritt-9-downloads` | Fertig |
| 10 | Schlanker Einstellungsbereich | 7, 9 | `task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl-schritt-10-einstellungen` | Fertig |
| 11 | Signiertes Release-Paket und automatisierter Upload nach App Store Connect | 1 | `task/issue-86-5dcdc7d5ef27411897f0ec9129c87614-apple-tv-app-tvos-lean-back-pl-schritt-11-release-upload` | Offen |

> **Verifikationshinweis:** Die Schritte 1–4 sind fachlich abgenommen und gemergt; der tvOS-Build-/Laufnachweis über den macOS-CI-Job `build-tvos` steht aus und ist in `pending-verification.md` registriert.
