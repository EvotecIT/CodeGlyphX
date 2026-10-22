"""Generate small, original test fonts with fontTools 4.67.0 (test-only).

No font is used in shipped artifacts. Normal builds do not execute this script.
Run with a Python environment containing fonttools==4.67.0.
"""
from pathlib import Path
import hashlib
import json
import gzip
import struct
from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.pens.t2CharStringPen import T2CharStringPen
from fontTools.pens.boundsPen import BoundsPen
from fontTools.feaLib.builder import addOpenTypeFeaturesFromString
from fontTools.ttLib import TTCollection, TTFont
from fontTools.ttLib.tables.DefaultTable import DefaultTable
from fontTools.cffLib.CFFToCFF2 import convertCFFToCFF2
import fontTools

ROOT = Path(__file__).resolve().parent.parent / "CodeGlyphX.Tests/Fixtures/Fonts"
ORDER = [".notdef", "space", "question", "A", "a", "B", "f", "i", "fi", "eacute", "smile",
         "alef", "alefFinal", "beh", "behFinal", "behInitial", "behMedial", "hebrewAlef", "hebrewBet", "ka", "acute", "fatha"]
WIDTHS = dict(zip(ORDER, [500, 250, 450, 600, 500, 650, 350, 250, 500, 500, 900,
                        500, 450, 600, 900, 700, 800, 500, 650, 600, 0, 0]))
CMAP = {32:"space", 63:"question", 65:"A", 97:"a", 66:"B", 102:"f", 105:"i", 233:"eacute", 0x1f642:"smile",
        0x627:"alef", 0xfe8e:"alefFinal", 0x628:"beh", 0xfe90:"behFinal", 0xfe91:"behInitial", 0xfe92:"behMedial",
        0x5d0:"hebrewAlef", 0x5d1:"hebrewBet", 0x915:"ka", 0x301:"acute", 0x64e:"fatha"}

def draw(pen, glyph, cff=False):
    if glyph == "space": return
    if glyph in ("acute", "fatha"):
        pen.moveTo((-250, 750)); pen.lineTo((-100, 900)); pen.lineTo((-40, 900)); pen.lineTo((-190, 750)); pen.closePath(); return
    width = WIDTHS[glyph]
    if glyph == "B":
        # A curve whose true bound differs from the off-curve control point.
        pen.moveTo((20, 0)); pen.lineTo((20, 700)); pen.lineTo((300, 700))
        if cff: pen.curveTo((700, 700), (700, 0), (300, 0))
        else: pen.qCurveTo((900, 350), (300, 0))
        pen.closePath(); return
    pen.moveTo((20, 0)); pen.lineTo((width - 40, 0)); pen.lineTo((width - 40, 700))
    pen.lineTo((20, 700)); pen.closePath()
    if glyph in ("A", "a", "eacute"):
        # Opposite winding: a hole, used by all print output routes.
        pen.moveTo((100, 100)); pen.lineTo((100, 600)); pen.lineTo((width - 120, 600))
        pen.lineTo((width - 120, 100)); pen.closePath()
    if glyph == "eacute":
        pen.moveTo((180, 750)); pen.lineTo((300, 900)); pen.lineTo((360, 900))
        pen.lineTo((240, 750)); pen.closePath()

def build(path, cff=False, second=False):
    builder = FontBuilder(1000, isTTF=not cff)
    builder.setupGlyphOrder(ORDER)
    builder.setupCharacterMap(CMAP)
    advances = {name:(value + (200 if second and name == "A" else 0), 20) for name,value in WIDTHS.items()}
    builder.setupHorizontalMetrics(advances)
    builder.setupHorizontalHeader(ascent=800, descent=-200)
    builder.setupNameTable({"familyName":"CodeGlyphX Test Outlines", "styleName":"Wide" if second else "Regular",
                           "uniqueFontIdentifier":path.stem, "fullName":path.stem, "psName":path.stem,
                           "version":"Version 1.0", "copyright":"Original CodeGlyphX test shapes, Apache-2.0"})
    builder.setupOS2(sTypoAscender=800, sTypoDescender=-200, usWinAscent=900, usWinDescent=200)
    builder.setupPost()
    if cff:
        charstrings = {}
        for name in ORDER:
            pen = T2CharStringPen(advances[name][0], None)
            draw(pen, name, True)
            charstrings[name] = pen.getCharString()
        builder.setupCFF(path.stem, {"FullName":path.stem, "FamilyName":"CodeGlyphX Test Outlines", "Weight":"Regular"}, charstrings, {})
    else:
        glyphs = {}
        for name in ORDER:
            pen = TTGlyphPen(None); draw(pen, name)
            glyphs[name] = pen.glyph()
        builder.setupGlyf(glyphs)
    addOpenTypeFeaturesFromString(builder.font, "languagesystem DFLT dflt; languagesystem latn dflt; feature liga { sub f i by fi; } liga;")
    builder.font["head"].created = builder.font["head"].modified = 3406620153
    builder.save(path)

ROOT.mkdir(parents=True, exist_ok=True)
build(ROOT / "TestOutlines.ttf")
build(ROOT / "TestOutlines.otf", cff=True)
converted = TTFont(ROOT / "TestOutlines.otf", recalcTimestamp=False)
convertCFFToCFF2(converted)
converted.save(ROOT / "TestOutlinesCff2.otf")
build(ROOT / "TestOutlinesWide.ttf", second=True)
collection = TTCollection()
collection.fonts = [TTFont(ROOT / "TestOutlines.ttf", recalcTimestamp=False), TTFont(ROOT / "TestOutlinesWide.ttf", recalcTimestamp=False)]
collection.save(ROOT / "TestOutlines.ttc")
(ROOT / "TestOutlinesWide.ttf").unlink()  # The second face is exercised through its collection.

# A small acyclic graph can expand into many empty components without emitting outline points.
# This fixture ensures the decoder budgets work as well as final output size.
budget = FontBuilder(1000, isTTF=True)
budget_order = [".notdef", "space"] + ["node" + str(i) for i in range(8)]
budget.setupGlyphOrder(budget_order)
budget.setupCharacterMap({32:"space", 65:"node7", 63:"node0"})
budget_glyphs = {}
for name in budget_order:
    pen = TTGlyphPen(budget_glyphs)
    if name.startswith("node"):
        previous = "space" if name == "node0" else "node" + str(int(name[4:]) - 1)
        for _ in range(4): pen.addComponent(previous, (1,0,0,1,0,0))
    budget_glyphs[name] = pen.glyph()
budget.setupGlyf(budget_glyphs)
budget.setupHorizontalMetrics({name:(600,0) for name in budget_order})
budget.setupHorizontalHeader(ascent=800, descent=-200)
budget.setupNameTable({"familyName":"CodeGlyphX Budget Test", "styleName":"Regular", "uniqueFontIdentifier":"BudgetTest", "fullName":"BudgetTest", "psName":"BudgetTest"})
budget.setupOS2(sTypoAscender=800, sTypoDescender=-200, usWinAscent=800, usWinDescent=200)
budget.setupPost()
budget.font["head"].created = budget.font["head"].modified = 3406620153
budget.save(ROOT / "TestOutlineBudget.ttf")

def index_bytes(items):
    offsets = [1]
    for item in items: offsets.append(offsets[-1] + len(item))
    offsize = max(1, (offsets[-1].bit_length() + 7) // 8)
    return struct.pack(">I", len(items)) + bytes([offsize]) + b"".join(x.to_bytes(offsize, "big") for x in offsets) + b"".join(items)

def dict_integer(value): return b"\x1d" + struct.pack(">i", value)

def cff_metadata_fixture(name, dictionaries, subrs, shared):
    font = TTFont(ROOT / "TestOutlinesCff2.otf", recalcTimestamp=False)
    top = font["CFF2"].cff.topDictIndex[0]
    strings = []
    for glyph in font.getGlyphOrder():
        charstring = top.CharStrings[glyph]
        charstring.compile(isCFF2=True)
        strings.append(charstring.bytecode)
    charstrings = index_bytes(strings)
    # Fixed-width DICT integers keep the offset computation independent of data values.
    top_length = 5 + 1 + 5 + 2 + 5 + 2
    charstrings_at = 5 + top_length + 4  # Empty global Subr INDEX
    fdarray_at = charstrings_at + len(charstrings)
    fdarray_length = len(index_bytes([bytes(11)] * dictionaries))
    fdselect_at = fdarray_at + fdarray_length
    private_at = fdselect_at + 1 + len(ORDER)
    private = dict_integer(6) + b"\x13"
    subrindex = struct.pack(">I", subrs) + b"\x01" + bytes([1]) * (subrs + 1)
    records = []
    private_data = bytearray()
    for i in range(dictionaries):
        relative = private_at if shared else private_at + i * (len(private) + len(subrindex))
        records.append(dict_integer(len(private)) + dict_integer(relative) + b"\x12")
        if not shared or i == 0: private_data.extend(private + subrindex)
    topdict = dict_integer(charstrings_at) + b"\x11" + dict_integer(fdarray_at) + b"\x0c\x24" + dict_integer(fdselect_at) + b"\x0c\x25"
    raw = b"\x02\x00\x05" + struct.pack(">H", len(topdict)) + topdict + bytes(4) + charstrings + index_bytes(records) + b"\x00" + bytes(len(ORDER)) + private_data
    table = DefaultTable("CFF2"); table.data = raw; font["CFF2"] = table
    path = ROOT / name
    font.save(path)
    payload = path.read_bytes()
    (ROOT / (name + ".gz")).write_bytes(gzip.compress(payload, mtime=0))
    path.unlink()
    return {"file":name + ".gz", "decodedBytes":len(payload), "dictionaries":dictionaries, "subrsPerIndex":subrs, "sharedIndex":shared}

metadata_fixtures = [cff_metadata_fixture("TestSharedCff2.otf", 256, 16384, True),
                     cff_metadata_fixture("TestCumulativeCff2.otf", 40, 65536, False)]

coverage_font = TTFont(ROOT / "TestOutlines.ttf", recalcTimestamp=False)
script_list = struct.pack(">H4sHHHHHHH", 1, b"DFLT", 8, 4, 0, 0, 0xffff, 1, 0)
feature_list = struct.pack(">H4sHHHH", 1, b"liga", 8, 0, 1, 0)
coverage = struct.pack(">HH", 2, 4096) + struct.pack(">HHH", 20, 20, 0) * 4096
lookup_list = struct.pack(">HHHHHHHHH", 1, 4, 1, 0, 1, 8, 1, 6, 0) + coverage
gsub = struct.pack(">IHHH", 0x10000, 10, 10 + len(script_list), 10 + len(script_list) + len(feature_list)) + script_list + feature_list + lookup_list
raw_gsub = DefaultTable("GSUB"); raw_gsub.data = gsub; coverage_font["GSUB"] = raw_gsub
coverage_path = ROOT / "TestSubstitutionCoverage.ttf"
coverage_font.save(coverage_path)
(ROOT / (coverage_path.name + ".gz")).write_bytes(gzip.compress(coverage_path.read_bytes(), mtime=0))
coverage_path.unlink()

evidence = {"generator":"fontTools " + fontTools.__version__, "source":"Build/Generate-PrintFontFixtures.py", "fonts":[]}
for name in ("TestOutlines.ttf", "TestOutlines.otf", "TestOutlinesCff2.otf", "TestOutlines.ttc", "TestOutlineBudget.ttf"):
    path = ROOT / name
    item = {"file":name, "sha256":hashlib.sha256(path.read_bytes()).hexdigest(), "bytes":path.stat().st_size}
    if name in ("TestOutlines.ttf", "TestOutlines.otf", "TestOutlinesCff2.otf"):
        font = TTFont(path); glyphset = font.getGlyphSet(); pen = BoundsPen(glyphset); glyphset["B"].draw(pen)
        item.update({"unitsPerEm":font["head"].unitsPerEm, "BInkBounds":pen.bounds,
                     "AAdvance":font["hmtx"].metrics["A"][0], "fiAdvance":font["hmtx"].metrics[font.getGlyphOrder()[8]][0]})
    evidence["fonts"].append(item)
evidence["metadataBudgetFixtures"] = metadata_fixtures
(ROOT / "test-font-metadata.json").write_text(json.dumps(evidence, indent=2) + "\n", encoding="utf-8")
print(json.dumps(evidence, indent=2))
