"""Extract selected independently published Zint 2.15.0 Composite regression vectors.

Usage: python Build/Generate-ZintCompositeFixtures.py <test_composite.c> <output-file>
The source is BSD-3-Clause, Copyright (c) 2019-2025 Robin Stuart and contributors.
See THIRD-PARTY-NOTICES.md. The reference engine is never needed by product builds.
"""
import hashlib
import pathlib
import re
import sys

source = pathlib.Path(sys.argv[1]).read_bytes()
assert hashlib.sha256(source).hexdigest() == "10c3d9a53610aee87dc422522a8cb3ba384be3cc2e9dbf30360e82f3421dbfc4"
examples = source.decode().split("static void test_examples(", 1)[1].split("const int data_size", 1)[0]
pattern = re.compile(r'/\*\s*(\d+)\*/\s*\{ BARCODE_(DBAR_LTD_CC|DBAR_EXPSTK_CC), [^,]+, ([12]), "([^"]*)", "([^"]*)", 0, (\d+), (\d+), [01], "[^"]*",\s*((?:"[01]+"\s*)+)\}', re.S)
records = []
for item in pattern.finditer(examples):
    index, kind, component, primary, secondary, height, width, encoded = item.groups()
    if int(index) not in {1, 12, 16, 31, 40}:
        continue
    if kind == "DBAR_LTD_CC":
        digits = primary.zfill(13)
        if len(digits) == 13:
            check = (10 - sum(int(digit) * (3 if at % 2 == 0 else 1) for at, digit in enumerate(digits)) % 10) % 10
            digits += str(check)
        primary = "(01)" + digits
    else:
        primary = primary.replace("[", "(").replace("]", ")")
    secondary = secondary.replace("[", "(").replace("]", ")")
    cells = "".join(re.findall(r'"([01]+)"', encoded))
    assert len(cells) == int(height) * int(width)
    records.append("|".join(["DataBarLimited" if kind == "DBAR_LTD_CC" else "DataBarExpandedStacked",
                              "a" if component == "1" else "b", primary, secondary, width, height, cells]))
assert len(records) == 5
pathlib.Path(sys.argv[2]).write_text(
    "# Zint 2.15.0 test_examples cases 1,12,16,31,40\n"
    "# https://github.com/zint/zint/blob/2.15.0/backend/tests/test_composite.c\n"
    "# BSD-3-Clause, Copyright (c) 2019-2025 Robin Stuart and contributors; see THIRD-PARTY-NOTICES.md\n"
    "# sourceSha256 " + hashlib.sha256(source).hexdigest() + "\n" + "\n".join(records) + "\n", encoding="utf-8")
