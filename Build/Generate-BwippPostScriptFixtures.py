#!/usr/bin/env python3
"""Generate test-only module fixtures using an independently built, pinned BWIPP.

Requires Ghostscript and BWIPP's monolithic barcode.ps built from revision
4a678ddb7a9fb59f6b3ba64eba81e2ef1f71ca77. Neither tool is a product dependency.
Usage: python3 Build/Generate-BwippPostScriptFixtures.py barcode.ps output.json
"""
import json
import hashlib
import pathlib
import re
import subprocess
import sys

REVISION = "4a678ddb7a9fb59f6b3ba64eba81e2ef1f71ca77"


def read_symbol(barcode_ps, text, options, bcid):
    data = text.encode("latin-1").hex()
    if bcid == "mailmark4state":
        code = f"""
        /uk.co.terryburton.bwipp._dontdraw true def
        <{data}> (dontdraw) /mailmark4state /uk.co.terryburton.bwipp findresource exec
        dup /bbs get /bottoms exch def /bhs get /heights exch def
        /top 0 heights {{ 2 copy lt {{ exch }} if pop }} forall def
        /states heights length string def
        0 1 heights length 1 sub {{ /i exch def
            /down bottoms i get 0 eq def
            /up bottoms i get heights i get add top 0.999 mul gt def
            states i up {{ down {{ 70 }} {{ 65 }} ifelse }} {{ down {{ 68 }} {{ 84 }} ifelse }} ifelse put
        }} for
        states =
        """
        states = subprocess.run(["gs", "-q", "-dNODISPLAY", "-dBATCH", "-dNOPAUSE", "-f", str(barcode_ps), "-c", code],
                                check=True, capture_output=True, text=True).stdout.strip()
        top = "0".join("1" if c in "AF" else "0" for c in states)
        middle = "0".join("1" for c in states)
        bottom = "0".join("1" if c in "DF" else "0" for c in states)
        return {"states": states, "width": len(middle), "height": 3, "modules": top + middle + bottom}
    # Print integer arrays in PostScript syntax, which also parses as JSON
    # after inserting commas. Avoid rendering or loading external files.
    code = f"""
    /uk.co.terryburton.bwipp._dontdraw true def
    <{data}> ({options} dontdraw) /{bcid} /uk.co.terryburton.bwipp findresource exec
    dup /sbs known {{ /sbs get == }} {{
        dup /pixx get == dup /pixs get length 1 index /pixx get idiv ==
        dup /rowheights known {{ dup /rowheights get == }} {{ ([]) = }} ifelse /pixs get ==
    }} ifelse
    """
    result = subprocess.run(["gs", "-q", "-dNODISPLAY", "-dBATCH", "-dNOPAUSE", "-f", str(barcode_ps), "-c", code],
                            check=True, capture_output=True, text=True).stdout.split()
    if result[0].startswith("["):
        runs = [int(n) for n in " ".join(result).strip("[] ").split()]
        bits = "".join(("1" if i % 2 == 0 else "0") * width for i, width in enumerate(runs))
        return {"runs": runs, "width": len(bits), "height": 1, "modules": bits}
    width, height = map(int, result[:2])
    arrays = re.findall(r"\[([^]]*)\]", " ".join(result[2:]))
    heights = [int(n) for n in arrays[0].split()]
    bits = "".join(arrays[1].split())
    assert len(bits) == width * height
    if heights:
        assert len(heights) == height
        bits = "".join(bits[row * width:(row + 1) * width] * repeats for row, repeats in enumerate(heights))
        height = sum(heights)
    return {"width": width, "height": height, "modules": bits}


def main():
    barcode_ps = pathlib.Path(sys.argv[1])
    output = pathlib.Path(sys.argv[2])
    fixtures = []
    for version in ("a", "b", "limiteda", "limitedb"):
        texts = ["A5", "ABC123", "0123456789", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-."]
        if not version.startswith("limited"):
            texts += ["AB\x1dCD", "abcXYZz", "'!\"#&()\0*,:\x7f", "\xa0", "\xa0" * 4, "x\xff\x80\xc1z"]
        for text in texts:
            fixture = {"bcid": "posicode", "text": text, "version": version, "rows": 1, "exact": True}
            fixture.update(read_symbol(barcode_ps, text, f"version={version}", "posicode"))
            fixtures.append(fixture)
        for rows in (2, 3, 4, 5):
            text = "ABC123abcXYZ\xff" if not version.startswith("limited") else "ABC123DEF456789"
            fixture = {"bcid": "posicode", "text": text, "version": version, "rows": rows, "exact": True}
            fixture.update(read_symbol(barcode_ps, text, f"version={version} rows={rows} rowheight=1", "posicode"))
            fixtures.append(fixture)
    # The reference uses a more compact upper latch for sustained Latin-1 runs.
    for fixture in fixtures:
        if fixture["text"] == "\xa0" * 4:
            fixture["exact"] = False
    for version in ("a", "b"):
        fixture = {"bcid": "posicode", "text": "010952123454321310ABC\x1d17261231", "version": version,
                   "rows": 1, "exact": True, "gs1": True}
        fixture.update(read_symbol(barcode_ps, "^FNC1010952123454321310ABC^FNC117261231", f"version={version} parsefnc", "posicode"))
        fixtures.append(fixture)
    for text in (
        "1100000000000XY11     ", "21B2254800659JW5O9QA6Y", "11000000000000000XY11     ",
        "41038422416563762EF61AH8T ", "0100000000000AA000AA0A", "0100000000009JA500AA0A",
        "01000000000000000AA000AA0A", "01000000000000009JA500AA0A", "01000000000000000C1I2JQ3N ",
        "01777777777777777KM12JQ3N ", "31833333333333333C12JQ3U  ",
        "0100000000000A1B1AB1A ", "0100000000000AB12AB1A ", "0100000000000AB123AB1A",
        "0100000000000AB1C2AB1A", "0100000000000A12AB1A  ", "0100000000000A123AB1A ",
    ):
        fixture = {"bcid": "mailmark4state", "text": text}
        fixture.update(read_symbol(barcode_ps, text, "", "mailmark4state"))
        fixtures.append(fixture)
    header = "JGB 012100123412345678AB19XY1A 0             "
    for kind, tails in ((7, ("", "ABC123")), (9, ("", "REFERENCE 12300AB", "A" * 45)),
                        (29, ("", "www.xyz.com", "A" * 25))):
        for tail in tails:
            fixture = {"bcid": "mailmark", "text": header + tail, "type": kind}
            fixture.update(read_symbol(barcode_ps, fixture["text"], f"type={kind}", "mailmark"))
            fixtures.append(fixture)
    for ratio in (2, 2.5, 3):
        for version in (1, 2, 3, 7):
            for category in (0, 2842, 16383):
                text = "HELLO" if version == 1 else "https://zappar.com"
                fixture = {"bcid": "d3aqr", "text": text, "ratio": ratio, "version": version, "category": category}
                # BWIPP's user-facing mask number is one-based; QR mask 3 is mask=4.
                fixture.update(read_symbol(barcode_ps, text, f"category={category} hostratio={ratio} version={version} eclevel=M fixedeclevel mask=4", "d3aqr"))
                fixtures.append(fixture)
    for body, zip_code in (("910112345678900000001", ""), ("921239123456781234567", "12345"),
                           ("931231234561234567890", "123456789"),
                           ("9212391234567812345678901", "12345"),
                           ("941231212345612345678", ""), ("951231123456612312345", "12345")):
        check = (10 - sum(int(c) * (3 if i % 2 == 0 else 1) for i, c in enumerate(reversed(body))) % 10) % 10
        pic = body + str(check)
        text = (f"(420){zip_code}" if zip_code else "") + f"({pic[:2]}){pic[2:]}"
        fixture = {"bcid": "impb", "text": text, "pic": pic, "zip": zip_code}
        fixture.update(read_symbol(barcode_ps, text, "", "gs1-128"))
        fixtures.append(fixture)
    for text in ("(8110)012345612345611110123", "(8110)01234561234561111012332612314260101"):
        fixture = {"bcid": "gs1northamericancoupon", "text": text}
        fixture.update(read_symbol(barcode_ps, text, "", "gs1northamericancoupon"))
        fixtures.append(fixture)
    output.write_text(json.dumps({"generator": "BWIPP with Ghostscript", "revision": REVISION,
                                "engineSha256": hashlib.sha256(barcode_ps.read_bytes()).hexdigest(),
                                "fixtures": fixtures}, indent=2) + "\n", encoding="utf-8")
    print(f"Generated {len(fixtures)} fixtures in {output}")


if __name__ == "__main__":
    main()
