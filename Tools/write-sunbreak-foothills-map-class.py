"""Pastes the collision rows into Assets/Scripts/Maps/SunbreakFoothillsMap.cs (between the `static readonly string[] Rows =` braces).

Usage: python Tools/write-sunbreak-foothills-map-class.py     (after Tools/build-sunbreak-foothills-rows.py)
Keeps the rest of the class untouched, so the rows can be rebuilt from the painting + override file without hand-copying 32 strings.
"""
import os, re
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ROWS = os.path.join(ROOT, "plans", "261113-1010-world-map-new-maps", "layout-sunbreak", "sunbreak-foothills-48x32-rows.txt")
CS = os.path.join(ROOT, "Assets", "Scripts", "Maps", "SunbreakFoothillsMap.cs")

rows = [l.strip() for l in open(ROWS) if l.strip()]
assert len(rows) == 32 and all(len(r) == 48 for r in rows), "expected 32 rows of 48"
src = open(CS, encoding="utf8").read()
body = "\n".join('            "%s",' % r for r in rows)
new, n = re.subn(r"(static readonly string\[\] Rows =\s*\{\n)(.*?)\n?(        \};)", lambda m: m.group(1) + body + "\n" + m.group(3), src, count=1, flags=re.S)
assert n == 1, "Rows block not found"
open(CS, "w", encoding="utf8", newline="\n").write(new)
print("rows written to", CS)
