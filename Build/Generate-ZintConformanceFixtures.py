#!/usr/bin/env python3
"""Capture test-only HIBC MicroPDF417 grids with Zint 2.13.0.

Usage: python3 Build/Generate-ZintConformanceFixtures.py output.json
Zint is an isolated comparison tool and is not a product or build dependency.
"""
import json
import pathlib
import subprocess
import sys

version = subprocess.run(["zint", "--version"], check=True, capture_output=True, text=True).stdout.strip()
if version != "Zint version 2.13.0":
    raise SystemExit(f"Expected Zint 2.13.0, found {version}")
fixtures = []
for text in ("A", "A999BJC5D6E71", "ABCDEF"):
    for columns in range(1, 5):
        result = subprocess.run(["zint", "-b", "HIBC_MICPDF", "-d", text, f"--cols={columns}", "--dump"],
                                check=True, capture_output=True, text=True)
        width = columns * 17 + (21 if columns <= 2 else 31)
        rows = ["".join(f"{int(n, 16):04b}" for n in line.replace(" ", ""))[:width]
                for line in result.stdout.strip().splitlines()]
        assert all(len(row) == width for row in rows)
        fixtures.append({"bcid": "hibcmicropdf417", "text": text, "columns": columns,
                         "width": width, "height": len(rows), "modules": "".join(rows)})
output = pathlib.Path(sys.argv[1])
output.write_text(json.dumps({"generator": version, "source": "https://github.com/zint/zint/tree/2.13.0",
                             "fixtures": fixtures}, indent=2) + "\n", encoding="utf-8")
print(f"Generated {len(fixtures)} fixtures in {output}")
