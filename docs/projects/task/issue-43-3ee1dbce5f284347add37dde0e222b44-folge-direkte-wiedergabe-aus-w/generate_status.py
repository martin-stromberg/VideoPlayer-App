#!/usr/bin/env python3
"""Erzeugt status.html aus steps.md (+ optional blocked.md) im selben Verzeichnis."""
import html
import re
import sys
from pathlib import Path

project_dir = Path(__file__).resolve().parent
steps_file = project_dir / "steps.md"
blocked_file = project_dir / "blocked.md"
out_file = project_dir / "status.html"

steps_text = steps_file.read_text(encoding="utf-8")

title = "Projekt"
m = re.search(r"# Entwicklungsschritte\s+[–-]\s+Projekt\s+(.+)", steps_text)
if m:
    title = m.group(1).strip()
branch = ""
m = re.search(r"Branch:\s*`([^`]+)`", steps_text)
if m:
    branch = m.group(1)

rows = []
for line in steps_text.splitlines():
    line = line.strip()
    if not line.startswith("|") or "---" in line or "Titel" in line and "Status" in line:
        continue
    cells = [c.strip() for c in line.strip("|").split("|")]
    if len(cells) >= 5 and cells[0].isdigit():
        rows.append(
            {
                "nr": cells[0],
                "titel": cells[1],
                "deps": cells[2],
                "branch": cells[3].strip("`"),
                "status": cells[4],
            }
        )

total = len(rows)
done = sum(1 for r in rows if r["status"] == "Fertig")
pct = round(done / total * 100) if total else 0

blocked_html = ""
if blocked_file.exists():
    content = blocked_file.read_text(encoding="utf-8")
    blocked_html = (
        '<div class="blocked"><h2>Blockade</h2><pre>'
        + html.escape(content)
        + "</pre></div>"
    )

status_class = {
    "Fertig": "done",
    "In Arbeit": "wip",
    "Blockiert": "blocked-status",
    "Offen": "open",
}

rows_html = ""
for r in rows:
    cls = status_class.get(r["status"], "open")
    rows_html += (
        f'<tr class="{cls}"><td>{html.escape(r["nr"])}</td>'
        f"<td>{html.escape(r['titel'])}</td>"
        f"<td>{html.escape(r['deps'])}</td>"
        f"<td><code>{html.escape(r['branch'])}</code></td>"
        f"<td>{html.escape(r['status'])}</td></tr>\n"
    )

page = f"""<!DOCTYPE html>
<html lang="de">
<head>
<meta charset="utf-8">
<title>Projektstatus – {html.escape(title)}</title>
<style>
body {{ font-family: Segoe UI, Arial, sans-serif; margin: 2em; color: #222; }}
h1 {{ font-size: 1.4em; }}
.meta {{ color: #555; margin-bottom: 1em; }}
.progress {{ background: #e0e0e0; border-radius: 6px; height: 22px; width: 100%; max-width: 600px; }}
.progress > div {{ background: #2e7d32; height: 100%; border-radius: 6px; color: #fff;
  font-size: 0.85em; line-height: 22px; text-align: center; min-width: 2em; }}
table {{ border-collapse: collapse; margin-top: 1.2em; width: 100%; max-width: 1100px; }}
th, td {{ border: 1px solid #ccc; padding: 6px 10px; text-align: left; font-size: 0.92em; }}
th {{ background: #f0f0f0; }}
tr.done td {{ background: #e8f5e9; }}
tr.wip td {{ background: #fff8e1; }}
tr.blocked-status td {{ background: #ffebee; }}
code {{ font-size: 0.8em; word-break: break-all; }}
.blocked {{ margin-top: 1.5em; padding: 1em; border: 2px solid #c62828; border-radius: 6px;
  background: #ffebee; max-width: 1100px; }}
.blocked h2 {{ color: #c62828; margin-top: 0; }}
.blocked pre {{ white-space: pre-wrap; font-size: 0.85em; }}
</style>
</head>
<body>
<h1>Projektstatus – {html.escape(title)}</h1>
<div class="meta">Basisbranch: <code>{html.escape(branch)}</code> &middot;
{done} von {total} Schritten fertig</div>
<div class="progress"><div style="width:{pct}%">{pct}%</div></div>
<table>
<tr><th>#</th><th>Titel</th><th>Abhängigkeiten</th><th>Branch</th><th>Status</th></tr>
{rows_html}</table>
{blocked_html}
</body>
</html>
"""

out_file.write_text(page, encoding="utf-8")
print(f"status.html geschrieben: {done}/{total} Schritten fertig ({pct}%)")
