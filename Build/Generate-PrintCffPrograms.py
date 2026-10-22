"""Generate original CFF operator fixtures with optional fontTools 4.67.0.

The programs are authored here; BoundsPen supplies an independent outline oracle
for curve operators. Arithmetic programs draw the same explicit 540 by 700 box.
"""
from pathlib import Path
import hashlib
import json
import fontTools
from fontTools.fontBuilder import FontBuilder
from fontTools.misc.psCharStrings import T2CharString
from fontTools.cffLib import SubrsIndex
from fontTools.pens.boundsPen import BoundsPen

ROOT = Path(__file__).resolve().parents[1] / "CodeGlyphX.Tests/Fixtures/Fonts"
MOVE = [600, 20, 0, "rmoveto"]
BOX_END = [0, 700, -540, 0, "rlineto", "endchar"]
PROGRAMS = {
    "horizontal": [600, 20, "hmoveto", 540, 700, -540, "hlineto", "endchar"],
    "vertical": [600, 20, 0, "rmoveto", 700, 540, -700, "vlineto", "endchar"],
    "relative": MOVE + [540, 0, 0, 700, -540, 0, "rlineto", "endchar"],
    "curves": MOVE + [100, 200, 200, 300, 100, -200, "rrcurveto", "endchar"],
    "curveLine": MOVE + [100, 200, 200, 300, 100, -200, 50, -300, "rcurveline", "endchar"],
    "lineCurve": MOVE + [50, 20, 100, 200, 200, 300, 100, -200, "rlinecurve", "endchar"],
    "vv": MOVE + [20, 100, 200, 300, 100, "vvcurveto", "endchar"],
    "hh": MOVE + [20, 100, 200, 300, 100, "hhcurveto", "endchar"],
    "hv": MOVE + [100, 200, 300, 100, 40, "hvcurveto", "endchar"],
    "vh": MOVE + [100, 200, 300, 100, 40, "vhcurveto", "endchar"],
    "hflex": MOVE + [60, 80, 100, 60, 60, 80, 60, "hflex", "endchar"],
    "flex": MOVE + [60, 20, 80, 100, 60, -20, 60, 20, 80, -100, 60, -20, 50, "flex", "endchar"],
    "hflex1": MOVE + [60, 20, 80, 100, 60, 60, 80, -100, 60, "hflex1", "endchar"],
    "flex1": MOVE + [60, 20, 80, 100, 60, -20, 60, 20, 80, -100, 60, "flex1", "endchar"],
    "hints": [600, 10, 20, "hstemhm", 10, 20, "vstemhm", "hintmask", b"\xc0", 20, 0,
              "rmoveto", 540, 0, 0, 700, -540, 0, "rlineto", "endchar"],
    "subroutines": MOVE + [-107, "callsubr", -107, "callgsubr", "endchar"],
    "arithmetic": MOVE + [600, 60, "sub", -1, "neg", "mul", 0, "rlineto"] + BOX_END,
    "sqrtDiv": MOVE + [729, "sqrt", 2, "div", 40, "mul", 0, "rlineto"] + BOX_END,
    "transient": MOVE + [540, 0, "put", 0, "get", 0, "rlineto"] + BOX_END,
    "stack": MOVE + [270, "dup", "add", 0, "exch", "exch", "rlineto"] + BOX_END,
    "conditional": MOVE + [540, 999, 1, 2, "ifelse", 0, "rlineto"] + BOX_END,
}


def main():
    if fontTools.__version__ != "4.67.0":
        raise RuntimeError("Use fontTools 4.67.0")
    names = [".notdef"] + list(PROGRAMS)
    builder = FontBuilder(1000, isTTF=False)
    builder.setupGlyphOrder(names)
    builder.setupCharacterMap({0xE000 + i: name for i, name in enumerate(PROGRAMS)})
    builder.setupHorizontalMetrics({name: (600, 20) for name in names})
    builder.setupHorizontalHeader(ascent=800, descent=-200)
    builder.setupNameTable({"familyName": "CodeGlyphX Original CFF Programs", "styleName": "Regular",
                            "psName": "CodeGlyphX-TestCffPrograms", "fullName": "CodeGlyphX Test CFF Programs",
                            "copyright": "Original CodeGlyphX test programs, Apache-2.0"})
    builder.setupOS2(sTypoAscender=800, sTypoDescender=-200, usWinAscent=900, usWinDescent=200)
    builder.setupPost()
    strings = {name: T2CharString(program=program) for name, program in PROGRAMS.items()}
    strings[".notdef"] = T2CharString(program=[600, "endchar"])
    builder.setupCFF("CodeGlyphX-TestCffPrograms", {"FullName": "CodeGlyphX Test CFF Programs"}, strings, {})
    top = builder.font["CFF "].cff.topDictIndex[0]
    globals_ = builder.font["CFF "].cff.GlobalSubrs
    top.Private.Subrs = SubrsIndex()
    top.Private.Subrs.append(T2CharString(program=[540, 0, "rlineto", "return"], private=top.Private, globalSubrs=globals_))
    globals_.append(T2CharString(program=[0, 700, -540, 0, "rlineto", "return"], private=top.Private, globalSubrs=globals_))
    for name in names:
        top.CharStrings[name].private = top.Private
        top.CharStrings[name].globalSubrs = globals_
    builder.font["head"].created = builder.font["head"].modified = 3406620153
    # fontTools intentionally does not execute arithmetic operators while compiling.
    builder.font.recalcBBoxes = False
    top.FontBBox = [0, 0, 1000, 1000]
    path = ROOT / "TestCffPrograms.otf"
    builder.save(path)
    glyph_set = builder.font.getGlyphSet()
    cases = []
    for index, name in enumerate(PROGRAMS):
        if index < 16:
            pen = BoundsPen(glyph_set)
            glyph_set[name].draw(pen)
            bounds = pen.bounds
        else:
            bounds = (20, 0, 560, 700)
        cases.append({"scalar": 0xE000 + index, "program": name, "bounds": bounds})
    manifest = {"tool": "fontTools 4.67.0; BoundsPen and authored rectangle arithmetic", "sha256": hashlib.sha256(path.read_bytes()).hexdigest(), "cases": cases}
    (ROOT / "test-cff-program-metadata.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
