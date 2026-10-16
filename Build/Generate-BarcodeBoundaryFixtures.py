#!/usr/bin/env python3
"""Generate test-only capacity and image-width vectors with pinned BWIPP.

Usage: python3 Build/Generate-BarcodeBoundaryFixtures.py barcode.ps output.json
Requires the same BWIPP revision and Ghostscript as Generate-BwippPostScriptFixtures.py.
"""
import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
import sys

spec = importlib.util.spec_from_file_location("bwipp_fixtures", Path(__file__).with_name("Generate-BwippPostScriptFixtures.py"))
reference = importlib.util.module_from_spec(spec)
spec.loader.exec_module(reference)
engine = Path(sys.argv[1])
fixtures = []
for length in (37, 38, 42, 43, 44, 49):
    text = "A" * length
    fixtures.append(dict(bcid="code49", text=text, **reference.read_symbol(engine, text, "", "code49")))
for variant in ("a", "b", "limiteda", "limitedb"):
    for text in ("12", "1234"):
        fixtures.append(dict(bcid="posicode", text=text, variant=variant, rows=2,
                             **reference.read_symbol(engine, text, f"version={variant} rows=2 rowheight=6", "posicode")))
text = "0123456789012"
code = f"""
/uk.co.terryburton.bwipp._dontdraw true def
<{text.encode().hex()}> (dontdraw) /planet /uk.co.terryburton.bwipp findresource exec
/bhs get /heights exch def /top 0 heights {{ 2 copy lt {{ exch }} if pop }} forall def
heights {{ top 0.999 mul gt {{ (1) }} {{ (0) }} ifelse print }} forall (\\n) print
"""
states = subprocess.run(["gs", "-q", "-dNODISPLAY", "-dBATCH", "-dNOPAUSE", "-f", str(engine), "-c", code],
                        check=True, capture_output=True, text=True).stdout.strip()
top = "0".join(states)
bottom = "0".join("1" for _ in states)
fixtures.append(dict(bcid="planet", text=text, expected="01234567890122", width=len(bottom), height=2, modules=top + bottom))
Path(sys.argv[2]).write_text(json.dumps(dict(bwippRevision=reference.REVISION,
    engineSha256=hashlib.sha256(engine.read_bytes()).hexdigest(), fixtures=fixtures), indent=2) + "\n", encoding="utf-8")
print(f"Wrote {len(fixtures)} independent boundary vectors.")
