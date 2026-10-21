"""Generate wide Composite input vectors with an isolated Zint 2.15.0 CLI.

Usage: python Build/Generate-ZintWideCompositeFixtures.py <zint-executable> <output-file>
The CLI is a test oracle, never a product or normal build requirement.
"""
import hashlib
import pathlib
import re
import subprocess
import sys

engine = pathlib.Path(sys.argv[1]).resolve()
version = subprocess.check_output([str(engine), "--version"], text=True).strip()
assert version.startswith("Zint version 2.15.0"), version
primary = "[01]09521234543213[10]Abcdefghijklmnopqrst[21]12345678"
secondary = "[11]990102"
records = []
for carrier, name, columns in [("DataBarExpanded", "DBAR_EXP_CC", None),
                               ("DataBarExpandedStacked", "DBAR_EXPSTK_CC", 5),
                               ("DataBarExpandedStacked", "DBAR_EXPSTK_CC", 6)]:
    for mode in (1, 2):
        args = [str(engine), "-b", name, f"--mode={mode}", "--primary", primary, "-d", secondary]
        if columns is not None:
            args.append(f"--cols={columns}")
        svg = subprocess.check_output(args + ["--notext", "--noquietzones", "--scale=0.5", "--filetype=SVG", "--direct"], text=True)
        width = int(re.search(r'<svg width="(\d+)"', svg).group(1))
        raw = subprocess.check_output(args + ["--dump"], text=True)
        rows = []
        for line in raw.splitlines():
            hexadecimal = line.replace(" ", "")
            assert re.fullmatch("[0-9A-F]+", hexadecimal), line
            bits = "".join(f"{int(value, 16):04b}" for value in hexadecimal)
            assert set(bits[width:]) <= {"0"}
            rows.append(bits[:width])
        assert width > 200 and all(len(row) == width for row in rows)
        records.append("|".join([carrier, "a" if mode == 1 else "b", primary.replace("[", "(").replace("]", ")"),
                                 secondary.replace("[", "(").replace("]", ")"), str(width), str(len(rows)), "".join(rows)]))
pathlib.Path(sys.argv[2]).write_text(
    "# " + version + " wide Expanded Composite inputs\n"
    "# CLI SHA256 " + hashlib.sha256(engine.read_bytes()).hexdigest() + "\n"
    "# Upstream tag https://github.com/zint/zint/tree/2.15.0\n"
    "# Zint output is preserved unchanged; see THIRD-PARTY-NOTICES.md\n" + "\n".join(records) + "\n", encoding="utf-8")
