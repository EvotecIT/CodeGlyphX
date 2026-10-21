"""Extract Ultracode's fixed tile definitions from the pinned Zint 2.15.0 reference.

Usage: python Build/Generate-UltracodeTables.py ultra.c output.cs
Normal builds use the generated data and do not require Python or Zint.
"""
import hashlib
import re
import sys
from pathlib import Path

source_path, output_path = map(Path, sys.argv[1:])
raw = source_path.read_bytes()
if hashlib.sha256(raw).hexdigest() != "a69984036d7149e7d4274ec04c8f19b91e73d12a15dfaf5b2b6137796bca4d52":
    raise ValueError("The reference must be the pinned Zint 2.15.0 backend/ultra.c")
source = raw.decode("utf-8")
notice = re.search(r"/\*\n.*?\*/", source, re.S).group(0)
lines = ["// Generated fixed Ultracode tile data; do not edit.",
         "// https://github.com/zint/zint/blob/2.15.0/backend/ultra.c",
         "// Regenerate with Build/Generate-UltracodeTables.py.", notice,
         "// SPDX-License-Identifier: BSD-3-Clause", "namespace CodeGlyphX.Ultracode;", "",
         "internal static class UltracodeTables {"]
for source_name, name, count in [("ult_tiles", "Tiles", 285), ("ult_dccu", "UpperDcc", 64), ("ult_dccl", "LowerDcc", 64)]:
    body = re.search(r"\b" + source_name + r"\s*(?:\[[^]]*\])+\s*=\s*\{(.*?)\n\};", source, re.S).group(1)
    body = re.sub(r"/\*.*?\*/", "", body, flags=re.S)
    values = [str(int(value, 8)) for value in re.findall(r"\b0[0-7]+\b", body)]
    if len(values) != count:
        raise ValueError(f"Unexpected {source_name} count: {len(values)}")
    lines.append(f"    internal static readonly int[] {name} = {{")
    for start in range(0, count, 12):
        lines.append("        " + ", ".join(values[start:start + 12]) + ",")
    lines.append("    };")
lines.append("}")
output_path.parent.mkdir(parents=True, exist_ok=True)
output_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
