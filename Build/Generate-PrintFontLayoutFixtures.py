"""Original OpenType layout/variation fixtures, using optional fontTools 4.67.0.

No generated font is shipped with the library. Normal builds do not run this tool.
"""
from pathlib import Path
from tempfile import TemporaryDirectory
import hashlib
import json
import struct
import fontTools
from fontTools.ttLib import TTFont, newTable
from fontTools.ttLib.tables._k_e_r_n import KernTable_format_0
from fontTools.feaLib.builder import addOpenTypeFeaturesFromString
from fontTools.designspaceLib import DesignSpaceDocument, AxisDescriptor, SourceDescriptor
from fontTools.varLib import build
from fontTools.varLib.instancer import instantiateVariableFont
from fontTools.pens.boundsPen import BoundsPen
from fontTools.ttLib.tables import otTables
from fontTools.otlLib.builder import buildCoverage, buildLookup
from fontTools.ttLib.tables._c_m_a_p import CmapSubtable
from fontTools.ttLib.tables.DefaultTable import DefaultTable

ROOT = Path(__file__).resolve().parents[1] / "CodeGlyphX.Tests/Fixtures/Fonts"
SOURCE = ROOT / "TestOutlines.ttf"
SOURCE_SHA = "490e83227fae41636a95fc7d05e1e242513938bbf2732089d7d305b9bfc53fe6"
HEADER = "languagesystem DFLT dflt; languagesystem latn dflt; "


def base():
    font = TTFont(SOURCE, recalcTimestamp=False)
    del font["GSUB"]
    return font


def legacy(font):
    table = newTable("kern")
    table.version = 0
    sub = KernTable_format_0()
    sub.version, sub.coverage = 0, 1
    sub.kernTable = {("A", "B"): -5, ("B", "B"): -25}
    table.kernTables = [sub]
    font["kern"] = table


def layout_fixtures():
    features = """
        feature ss01 { sub A by a; } ss01;
        feature salt { sub B from [a f]; } salt;
        feature ccmp { sub a by f i; } ccmp;
        feature liga { sub f i by fi; } liga;
        lookup CHAIN { sub A a' B by f; } CHAIN;
        feature calt { lookup CHAIN; } calt;
        feature rvrn { rsub A a' B by i; } rvrn;
        lookup EXT useExtension { sub B by A; } EXT;
        feature ss02 { lookup EXT; } ss02;
    """
    font = base()
    addOpenTypeFeaturesFromString(font, HEADER + features)
    # The bounded managed engine supports coverage-based contexts (format 3).
    # Compile these structures independently rather than relying on feaLib's size optimizer.
    table = font["GSUB"].table
    chain = otTables.ChainContextSubst()
    chain.Format = 3
    chain.BacktrackCoverage = [buildCoverage(["A"], font.getReverseGlyphMap())]
    chain.InputCoverage = [buildCoverage(["a"], font.getReverseGlyphMap())]
    chain.LookAheadCoverage = [buildCoverage(["B"], font.getReverseGlyphMap())]
    chain.BacktrackGlyphCount = chain.InputGlyphCount = chain.LookAheadGlyphCount = 1
    record = otTables.SubstLookupRecord()
    record.SequenceIndex, record.LookupListIndex = 0, 5
    chain.SubstLookupRecord, chain.SubstCount = [record], 1
    table.LookupList.Lookup[4].SubTable = [chain]
    context = otTables.ContextSubst()
    context.Format, context.GlyphCount, context.SubstCount = 3, 2, 1
    context.Coverage = [buildCoverage([g], font.getReverseGlyphMap()) for g in ("A", "a")]
    record = otTables.SubstLookupRecord()
    record.SequenceIndex, record.LookupListIndex = 1, 5
    context.SubstLookupRecord = [record]
    context_index = len(table.LookupList.Lookup)
    table.LookupList.Lookup.append(buildLookup([context]))
    table.LookupList.LookupCount += 1
    feature = otTables.FeatureRecord()
    feature.FeatureTag = "ss03"
    feature.Feature = otTables.Feature()
    feature.Feature.FeatureParams = None
    feature.Feature.LookupListIndex, feature.Feature.LookupCount = [context_index], 1
    table.FeatureList.FeatureRecord.append(feature)
    table.FeatureList.FeatureCount += 1
    font.save(ROOT / "TestLayoutSubstitution.ttf")
    for name, feature in (
        ("Pairs", "pos A B -80; pos B <10 0 -40 0> A <20 0 -10 0>;"),
        ("Classes", "pos [A a] [B i] -70;"),
        ("Extension", "lookup EXT useExtension { pos A B -60; } EXT;"),
    ):
        font = base()
        legacy(font)
        addOpenTypeFeaturesFromString(font, HEADER + "feature kern {" + feature + "} kern;")
        font.save(ROOT / ("TestKerning" + name + ".ttf"))
    font = base()
    legacy(font)
    font.save(ROOT / "TestKerningLegacy.ttf")
    font = base()
    tracking = DefaultTable("trak")
    tracking.data = (struct.pack(">IHHHH", 0x10000, 0, 12, 0, 0)
                     + struct.pack(">HHI", 2, 2, 36)
                     + struct.pack(">iHH", -65536, 256, 44) + struct.pack(">iHH", 65536, 257, 48)
                     + struct.pack(">ii", 12 * 65536, 24 * 65536)
                     + struct.pack(">hhhh", -20, -40, 20, 80))
    font["trak"] = tracking
    variation = CmapSubtable.newSubtable(14)
    variation.platformID, variation.platEncID, variation.language = 0, 5, 0
    variation.cmap = {}
    variation.uvsDict = {0xFE0F: [(65, "B"), (97, None)], 0xE0100: [(65, "a")]}
    font["cmap"].tables.append(variation)
    font.save(ROOT / "TestTrackingAndSelectors.ttf")


def variation_fixtures():
    with TemporaryDirectory() as temp:
        work = Path(temp)
        document = DesignSpaceDocument()
        for tag, name, minimum, default, maximum in (
            ("wght", "Weight", 100, 400, 900), ("opsz", "Optical size", 12, 12, 72)
        ):
            axis = AxisDescriptor()
            axis.tag, axis.name = tag, name
            axis.minimum, axis.default, axis.maximum = minimum, default, maximum
            document.addAxis(axis)
        for weight in (100, 400, 900):
            for optical in (12, 72):
                font = base()
                delta = round((weight - 400) / 5 + (optical - 12))
                for glyph in font.getGlyphOrder():
                    width, side = font["hmtx"].metrics[glyph]
                    if width:
                        font["hmtx"].metrics[glyph] = (width + delta, side)
                    outline = font["glyf"][glyph]
                    if outline.numberOfContours > 0:
                        for index, (x, y) in enumerate(outline.coordinates):
                            if x > 100:
                                outline.coordinates[index] = (x + delta, y)
                font["hhea"].ascent = 800 + delta
                font["OS/2"].sTypoAscender = 800 + delta
                font["OS/2"].usWinAscent = 900 + delta
                path = work / f"master-{weight}-{optical}.ttf"
                font.save(path)
                source = SourceDescriptor()
                source.path = str(path)
                source.name = path.stem
                source.location = {"Weight": weight, "Optical size": optical}
                document.addSource(source)
        variable, _, _ = build(document)
        # A non-identity but valid avar map distinguishes design from normalized values.
        avar = newTable("avar")
        avar.segments = {"wght": {-1.0: -1.0, 0.0: 0.0, .5: .75, 1.0: 1.0},
                         "opsz": {-1.0: -1.0, 0.0: 0.0, 1.0: 1.0}}
        variable["avar"] = avar
        variable.recalcTimestamp = False
        variable["head"].created = variable["head"].modified = 3406620153
        variable.save(ROOT / "TestVariable.ttf")
        del variable["HVAR"]
        variable.save(ROOT / "TestVariableGvarMetrics.ttf")
    # Independent fontTools instancing and BoundsPen results are the test oracle.
    samples = []
    for weight, optical in ((100, 12), (400, 12), (650, 12), (900, 12), (900, 72)):
        instance = instantiateVariableFont(TTFont(ROOT / "TestVariable.ttf"), {"wght": weight, "opsz": optical}, inplace=False)
        pen = BoundsPen(instance.getGlyphSet())
        instance.getGlyphSet()["A"].draw(pen)
        samples.append({"weight": weight, "optical": optical, "A": instance["hmtx"].metrics["A"][0],
                        "bounds": pen.bounds, "ascender": instance["hhea"].ascent})
    return samples


def main():
    if fontTools.__version__ != "4.67.0" or hashlib.sha256(SOURCE.read_bytes()).hexdigest() != SOURCE_SHA:
        raise RuntimeError("Use fontTools 4.67.0 and the pinned original TestOutlines.ttf")
    layout_fixtures()
    samples = variation_fixtures()
    paths = sorted(list(ROOT.glob("TestKerning*.ttf")) + list(ROOT.glob("TestVariable*.ttf")) + [ROOT / "TestLayoutSubstitution.ttf", ROOT / "TestTrackingAndSelectors.ttf"])
    manifest = {"tool": "fontTools 4.67.0", "source_sha256": SOURCE_SHA,
                "variable_instances": samples, "outputs": {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in paths}}
    (ROOT / "test-layout-metadata.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
