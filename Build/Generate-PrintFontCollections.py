"""Build original mixed-outline collections from the pinned test fixtures.

Optional test-only maintainer tool; normal builds do not require Python/fontTools.
"""
from pathlib import Path
import hashlib
import json
import fontTools
from fontTools.ttLib import TTCollection, TTFont

ROOT = Path(__file__).resolve().parents[1] / "CodeGlyphX.Tests/Fixtures/Fonts"
INPUTS = {
    "TestOutlines.otf": "96e0728fe8f5d3fb3572c437d0195134f2d6fa113aed44a8045112834e3774ee",
    "TestOutlinesCff2.otf": "aeee9f788c034a01a6a3ac2135090d79e508ea7dfe03d34396cbad5661bd5eb4",
    "TestOutlines.ttf": "490e83227fae41636a95fc7d05e1e242513938bbf2732089d7d305b9bfc53fe6",
}


def main():
    if fontTools.__version__ != "4.67.0":
        raise RuntimeError("Reproduction requires fontTools 4.67.0")
    for name, expected in INPUTS.items():
        if hashlib.sha256((ROOT / name).read_bytes()).hexdigest() != expected:
            raise ValueError(f"Unexpected fixture hash: {name}")
    manifest = {"inputs": INPUTS, "outputs": {}}
    for name, sources in (
        ("TestMixedCffFirst.ttc", list(INPUTS)),
        ("TestMixedTrueTypeFirst.ttc", ["TestOutlines.ttf", "TestOutlines.otf", "TestOutlinesCff2.otf"]),
    ):
        collection = TTCollection()
        collection.fonts = []
        faces = []
        for index, source in enumerate(sources):
            font = TTFont(ROOT / source, recalcTimestamp=False)
            # Different nominal AND ligature metrics expose wrong-face shaping caches.
            order = font.getGlyphOrder()  # CFF2's post table deliberately has no glyph names.
            font["hmtx"].metrics[order[3]] = (600 + 100 * index, 20)
            font["hmtx"].metrics[order[8]] = (500 + 100 * index, 20)
            if index == 1:
                # A distinct substitution target catches wrong-face shaping even when
                # nominal metrics are later read from the correctly selected font.
                ligatures = font["GSUB"].table.LookupList.Lookup[0].SubTable[0].ligatures
                ligatures[order[6]][0].LigGlyph = order[5]  # f+i becomes curved B.
            collection.fonts.append(font)
            faces.append({"source": source, "A": 600 + 100 * index,
                          "fi": 650 if index == 1 else 500 + 100 * index,
                          "fi_glyph_id": 5 if index == 1 else 8})
        path = ROOT / name
        collection.save(path)
        manifest["outputs"][name] = {"sha256": hashlib.sha256(path.read_bytes()).hexdigest(), "faces": faces}
    (ROOT / "test-collection-metadata.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
