# Bestandsaufnahme: Automatisierung wiederkehrender Architektur-, Sicherheits- und Compliance-Reviews

Analysiert wurden die CI-/Workflow-Landschaft (`.github/`), die
NuGet-Abhängigkeiten der Projekte im Repo-Root sowie die
Dokumentationsstruktur — bezogen auf die Anforderung, drei
zeitgesteuerte GitHub-Actions-Workflows einzuführen, die periodisch
Issues mit Erwähnung von `@softwarewerkstatt` erzeugen
(`docs/features/task/issue-95-afb7503d43a643f4a5b8716472913991-automatisierung-wiederkehrende/requirement.md`,
GitHub Issue #95).

## Zusammenfassung

- **Keine zeitgesteuerten Workflows vorhanden:** Unter
  `.github/workflows/` existieren nur `pr-checks.yml`,
  `staging-merge.yml` und `main-merge.yml` — ausschließlich
  `push`/`pull_request`-Trigger, kein `schedule:` und kein
  `workflow_dispatch:`. Die geforderte Cron-Automatisierung ist ein
  komplett neues Muster im Projekt.
- **Keine Issue-erzeugende Automatisierung vorhanden:** Bisherige
  Workflows nutzen `gh` nur für `gh pr create` und `gh release
  upload`; `issues: write` kommt in keinem `permissions`-Block vor.
  Duplikatprüfung und Issue-Erstellung müssen neu aufgebaut werden.
- **Klare Workflow-Konventionen als Vorbild:** Kebab-Case-Dateinamen,
  deutscher Kommentarstil mit ASCII-Umschrift (ae/oe/ue/ss), minimaler
  `permissions`-Block pro Workflow, `GH_TOKEN: ${{
  secrets.GITHUB_TOKEN }}` für `gh`-Aufrufe, Gates über
  Repository-Variablen (`vars.*`). Details: [workflows.md](inventory/workflows.md).
- **Kein Duplikat-Merkmal vorhanden:** Weder ein dediziertes
  Review-/Automatisierungs-Label noch eine Titel-Konvention für
  wiederkehrende Issues existiert (Label-Bestand = GitHub-Standard +
  `released on @staging`); als einziges offenes Issue liegt #95 selbst
  vor. Details: [workflows.md](inventory/workflows.md).
- **NuGet-Bestand für die wöchentliche Prüfung erfasst:** 10 Projekte
  mit dezentralen Versionen (kein `Directory.Packages.props`), eine
  Quelle (`nuget.org`). Auffällig: die transitive Abhängigkeit
  `Microsoft.AspNetCore.SignalR.Common` 10.0.10 meldet bereits heute
  die NU1903-Warnung (bekannte Hoch-Severity-Schwachstelle
  GHSA-334f-vvp4-7g6p) — ein konkreter, bereits existierender Befund.
  Details: [nuget.md](inventory/nuget.md).
- **Dokumentationsorte vorhanden:** `docs/help/` mit
  Index-Pflicht (`index.md`), `README.md` (Abschnitt „Branching und
  CI"), `docs/RELEASE_NOTES.md` (englisch), `changes.log` (deutsch) —
  das Akzeptanzkriterium „vollständig im Repository dokumentiert" hat
  etablierte Ankerpunkte. Details:
  [dokumentation.md](inventory/dokumentation.md).
- **Wichtiger Betriebsumstand:** Scheduled Workflows laufen nur auf
  dem Default-Branch — die Automatisierung wird erst nach dem
  Staging-Pfad (`staging` → `main`) aktiv, siehe
  `docs/help/staging-branch.md`.

**Test-Ausgangszustand:** Beide Testsuiten sind vollständig grün —
`VideoPlayer.Maui.Tests` 1484/1484 und `VideoPlayer.E2E.Tests`
74/74 erfolgreich, 0 Fehlschläge, 0 Übersprungen (Exit-Code 0);
Restore-, Build- und Lint-Nachweise ebenfalls sauber (bis auf die
dokumentierte NU1903-Warnung). Keine bekannten Testfehler, keine
Testlücken. Nachweis: [inventory/tests.md](inventory/tests.md).

## Details

- [Workflows, CI-Konventionen und Repository-Automatisierung](inventory/workflows.md)
- [NuGet-Abhängigkeiten (für die wöchentliche Paketprüfung)](inventory/nuget.md)
- [Dokumentationsstruktur](inventory/dokumentation.md)
- [Tests und Test-Ausgangszustand](inventory/tests.md)
