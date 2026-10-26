"""Reproduce the bundled Manrope SemiBold subset from pinned, locally supplied inputs.

Optional maintainer tool; Python and fontTools are not build or runtime requirements.
Download the two inputs in INPUTS into --source-directory before running this script.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil

import fontTools
from fontTools import subset
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont

ROOT = Path(__file__).resolve().parents[1]
PREFIX = "https://raw.githubusercontent.com/google/fonts/e7db6ca603fd4862010383ff17ffd6cc4ffac1c0/ofl/manrope/"
INPUTS = {
    "Manrope-source.ttf": (PREFIX + "Manrope%5Bwght%5D.ttf",
                           "3ae11c49db0455a3cc33e37d380f20fdb8c7f8b41dc07625c177e3d87a9d6ae6"),
    "Manrope-OFL.txt": (PREFIX + "OFL.txt",
                        "58172e0c0fac2cda8a37b348164bb55e44b0e69051e557e92b1d3f6910141f7b"),
}
RANGES = [(0x20, 0x24f), (0x300, 0x36f), (0x2000, 0x206f), (0x20ac, 0x20ac), (0x1e00, 0x1eff)]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source-directory", type=Path, required=True)
    args = parser.parse_args()
    if fontTools.__version__ != "4.66.1":
        raise RuntimeError("Reproduction requires fontTools 4.66.1")
    for name, (_, expected) in INPUTS.items():
        if hashlib.sha256((args.source_directory / name).read_bytes()).hexdigest() != expected:
            raise ValueError(f"Unexpected source hash: {name}")
    face = TTFont(args.source_directory / "Manrope-source.ttf", recalcTimestamp=False)
    instantiateVariableFont(face, {"wght": 600}, inplace=True, updateFontNames=True)
    notice = (args.source_directory / "Manrope-OFL.txt").read_text(encoding="utf-8")
    # Keep the full copyright and OFL with embedded assembly-only copies as well.
    for record in face["name"].names:
        if record.nameID == 13:
            record.string = notice.encode(record.getEncoding())
    options = subset.Options()
    options.name_IDs = ["*"]
    options.name_legacy = True
    options.name_languages = ["*"]
    options.layout_features = ["*"]
    selection = subset.Subsetter(options=options)
    selection.populate(unicodes={value for first, last in RANGES for value in range(first, last + 1)})
    selection.subset(face)
    if any(ord(char) not in face.getBestCmap() for char in "Zażółć gęślą jaźń"):
        raise ValueError("The caption subset must retain Polish glyphs")
    target = ROOT / "CodeGlyphX/Printing/Fonts/Manrope-SemiBold-Latin-Extended.ttf"
    face.save(target)
    digest = hashlib.sha256(target.read_bytes()).hexdigest()
    if digest != "04c53e9409bad005e3b0e019075c28759c02dbb4501784fd665dddb10b5992e9":
        raise ValueError("Unexpected caption subset hash")
    shutil.copyfile(args.source_directory / "Manrope-OFL.txt", ROOT / "Licenses/Manrope-OFL.txt")
    manifest = {
        "tool": "fontTools 4.66.1; Prepare-CaptionFontAsset.py",
        "inputs": {name: {"url": url, "sha256": sha} for name, (url, sha) in INPUTS.items()},
        "outputs": {target.name: digest},
        "face": "Manrope SemiBold; static weight 600",
        "unicode_ranges": ",".join(f"U+{a:04X}-{b:04X}" for a, b in RANGES),
        "modifications": "Instantiated at weight 600 with static SemiBold names; subset to Latin, Latin Extended, combining marks and common punctuation. Full upstream OFL retained in name ID 13."
    }
    (target.parent / "caption-provenance.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(manifest["outputs"], indent=2))


if __name__ == "__main__":
    main()
