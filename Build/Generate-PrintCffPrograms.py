"""Generate original CFF operator fixtures with optional fontTools 4.67.0.

The programs are authored here; BoundsPen supplies an independent outline oracle
for curve operators. Arithmetic programs draw the same explicit 540 by 700 box.
"""
from pathlib import Path
from tempfile import TemporaryDirectory
import hashlib
import json
import fontTools
from fontTools.fontBuilder import FontBuilder
from fontTools.misc.psCharStrings import T2CharString
from fontTools.cffLib import SubrsIndex, FDArrayIndex, FDSelect, FontDict, PrivateDict
from fontTools.pens.boundsPen import BoundsPen
from fontTools.ttLib import TTFont
from fontTools.cffLib.CFFToCFF2 import convertCFFToCFF2
from fontTools.designspaceLib import DesignSpaceDocument, AxisDescriptor, SourceDescriptor
from fontTools.varLib import build
from fontTools.varLib.instancer import instantiateVariableFont

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


def cid_fixture():
    names = [".notdef", "cid00001", "cid00002"]
    builder = FontBuilder(1000, isTTF=False)
    builder.setupGlyphOrder(names)
    builder.setupCharacterMap({65: names[1], 66: names[2]})
    builder.setupHorizontalMetrics({name: (600, 20) for name in names})
    builder.setupHorizontalHeader(ascent=800, descent=-200)
    builder.setupNameTable({"familyName": "CodeGlyphX Original CID", "styleName": "Regular",
                           "psName": "CodeGlyphX-TestCID", "fullName": "CodeGlyphX Test CID"})
    builder.setupOS2(sTypoAscender=800, sTypoDescender=-200, usWinAscent=900, usWinDescent=200)
    builder.setupPost()
    strings = {name: T2CharString(program=MOVE + [-107, "callsubr", 100, 100, "rlineto", -107, "callgsubr", 0, 50, "rlineto", "endchar"]) for name in names}
    strings[".notdef"] = T2CharString(program=[600, "endchar"])
    builder.setupCFF("CodeGlyphX-TestCID", {"FullName": "CodeGlyphX Test CID"}, strings, {})
    cff = builder.font["CFF "].cff
    cff.GlobalSubrs.append(T2CharString(program=[0, 100, "rlineto", "return"], globalSubrs=cff.GlobalSubrs))
    top = cff.topDictIndex[0]
    top.ROS, top.CIDCount = ("Adobe", "Identity", 0), 3
    top.FDArray = FDArrayIndex()
    top.FDArray.GlobalSubrs = cff.GlobalSubrs
    for index, width in enumerate((300, 500)):
        dictionary = FontDict(GlobalSubrs=cff.GlobalSubrs)
        dictionary.FontName = "CodeGlyphX-CID-" + str(index)
        dictionary.Private = PrivateDict()
        dictionary.Private.Subrs = SubrsIndex()
        dictionary.Private.Subrs.append(T2CharString(
            program=[width, 0, 0, 700, -width, 0, "rlineto", "return"],
            private=dictionary.Private, globalSubrs=cff.GlobalSubrs))
        top.FDArray.append(dictionary)
    del top.Private
    top.FDSelect = FDSelect(format=3)
    top.FDSelect.gidArray = [0, 0, 1]
    top.CharStrings.fdArray, top.CharStrings.fdSelect = top.FDArray, top.FDSelect
    for index, name in enumerate(names):
        top.CharStrings[name].fdSelectIndex = top.FDSelect.gidArray[index]
        top.CharStrings[name].private = top.FDArray[top.FDSelect.gidArray[index]].Private
        top.CharStrings[name].globalSubrs = cff.GlobalSubrs
    builder.font["head"].created = builder.font["head"].modified = 3406620153
    builder.font.recalcBBoxes = False
    top.FontBBox = [20, 0, 520, 950]
    path = ROOT / "TestCffCid.otf"
    builder.save(path)
    first_subroutine = top.FDArray[0].Private.Subrs[0]
    first_subroutine.decompile()
    terminated = list(first_subroutine.program)
    first_subroutine.setProgram(terminated[:-1])
    builder.save(ROOT / "TestCffCidUnterminated.otf")
    first_subroutine.setProgram(terminated)
    top.FDSelect.format = 0
    builder.save(ROOT / "TestCffCidFormat0.otf")
    top.FDSelect.format = 3
    convertCFFToCFF2(builder.font)
    cff2_top = builder.font["CFF2"].cff.topDictIndex[0]
    cff2_top.FDSelect.format = 4
    for dictionary in cff2_top.FDArray:
        if hasattr(dictionary, "FontName"):
            del dictionary.FontName  # CFF2 Font DICTs omit the CFF1-only name key.
    builder.save(ROOT / "TestCff2Dictionaries.otf")
    return path


def seac_fixture():
    font = TTFont(ROOT / "TestOutlines.otf", recalcTimestamp=False)
    top = font["CFF "].cff.topDictIndex[0]
    # StandardEncoding A plus acute shifted 300 units to the right.
    top.CharStrings["eacute"] = T2CharString(program=[500, 300, 0, 65, 194, "endchar"],
                                           private=top.Private, globalSubrs=font["CFF "].cff.GlobalSubrs)
    path = ROOT / "TestCffSeac.otf"
    font.save(path)
    pen = BoundsPen(font.getGlyphSet())
    font.getGlyphSet()["eacute"].draw(pen)
    return path, pen.bounds


def variable_cff_fixture():
    with TemporaryDirectory() as temp:
        work = Path(temp)
        document = DesignSpaceDocument()
        axis = AxisDescriptor()
        axis.name, axis.tag = "Weight", "wght"
        axis.minimum, axis.default, axis.maximum = 400, 400, 900
        document.addAxis(axis)
        for weight, delta in ((400, 0), (900, 100)):
            font = TTFont(ROOT / "TestOutlines.otf", recalcTimestamp=False)
            top = font["CFF "].cff.topDictIndex[0]
            top.CharStrings["A"] = T2CharString(
                program=[600 + delta, 20, 0, "rmoveto", 540 + delta, 0, 0, 700, -540 - delta, 0, "rlineto", "endchar"],
                private=top.Private, globalSubrs=font["CFF "].cff.GlobalSubrs)
            font["hmtx"].metrics["A"] = (600 + delta, 20)
            path = work / f"cff-master-{weight}.otf"
            font.save(path)
            source = SourceDescriptor()
            source.path, source.name = str(path), path.stem
            source.location = {"Weight": weight}
            document.addSource(source)
        variable, _, _ = build(document)
        variable.recalcTimestamp = False
        variable["head"].created = variable["head"].modified = 3406620153
        path = ROOT / "TestVariableCff2.otf"
        variable.save(path)
    samples = []
    for weight in (400, 650, 900):
        instance = instantiateVariableFont(TTFont(path), {"wght": weight}, inplace=False)
        pen = BoundsPen(instance.getGlyphSet())
        instance.getGlyphSet()["A"].draw(pen)
        samples.append({"weight": weight, "A": instance["hmtx"].metrics["A"][0], "bounds": pen.bounds})
    return path, samples


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
    cid = cid_fixture()
    seac, bounds = seac_fixture()
    manifest["cid"] = {"sha256": hashlib.sha256(cid.read_bytes()).hexdigest(), "A_bounds": [20, 0, 320, 950], "B_bounds": [20, 0, 520, 950]}
    manifest["seac"] = {"sha256": hashlib.sha256(seac.read_bytes()).hexdigest(), "bounds": bounds}
    variable, samples = variable_cff_fixture()
    manifest["variable_cff2"] = {"sha256": hashlib.sha256(variable.read_bytes()).hexdigest(), "instances": samples}
    manifest["dictionary_variants"] = {p.name: hashlib.sha256(p.read_bytes()).hexdigest()
                                      for p in (ROOT / "TestCffCidFormat0.otf", ROOT / "TestCff2Dictionaries.otf", ROOT / "TestCffCidUnterminated.otf")}
    (ROOT / "test-cff-program-metadata.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
