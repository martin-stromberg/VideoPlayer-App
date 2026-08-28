# Code-Review

Status: **Keine sicherheitskritischen Befunde**

## Befunde

- Keine Befunde an den neu hinzugefügten Workflow-Dateien und Skripten.

## Hinweise

- Der iOS-Build-Job setzt auf `macos-latest` voraus, dass die erforderlichen Apple-Zertifikate und Provisioning-Profile als Repository-Secrets hinterlegt sind.
- Der Lint-Schritt in `pr-checks.yml` wird voraussichtlich bei bestehendem C#-Quellcode fehlschlagen, bis dieser mit `dotnet format` korrigiert wurde.
