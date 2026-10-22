"""Reproduce the approved embedded fonts from pinned, locally supplied inputs.

Optional maintainer tool: Python/fontTools are not build or runtime requirements.
Download the four pinned inputs listed below into --source-directory first.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil

import fontTools
from fontTools.ttLib import TTFont


ROOT = Path(__file__).resolve().parents[1]
INPUTS = {
    "gg-ocra.ttf": (
        "https://raw.githubusercontent.com/fontlabcom/getgo-fonts/c3bb7eac16e0b855355d4e436479ec49fd1d25a2/getgo-fonts/cc0/ocra/ocra.ttf",
        "c9c2658009c0ce6086165610ada58db852a78be0acd89c9870dacffd91b8e979"),
    "GG-Ocra-CC0.txt": (
        "https://raw.githubusercontent.com/fontlabcom/getgo-fonts/c3bb7eac16e0b855355d4e436479ec49fd1d25a2/getgo-fonts/cc0/LICENSE.txt",
        "a2010f343487d3f7618affe54f789f5487602331c0a8d03f49e9a7c547cf0499"),
    "ocr-b-original.ttf": (
        "https://raw.githubusercontent.com/jaycee723/ocr-b/fedeba81519770109925b5bec70e940be5948d8f/dist/OCR-B.ttf",
        "367d876cca948ecd4900851f6e85687cbb6e71de9d0d2f36348edec5655526af"),
    "Raisty-OCR-B-OFL.txt": (
        "https://raw.githubusercontent.com/jaycee723/ocr-b/fedeba81519770109925b5bec70e940be5948d8f/LICENSE.md",
        "035f9ce834c07a9bf88db572f515d2fb494089a9589441ccc056b758805a29ef"),
}


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source-directory", type=Path, required=True)
    args = parser.parse_args()
    if fontTools.__version__ != "4.66.1":
        raise RuntimeError("Reproduction requires fontTools 4.66.1")
    for name, (_, expected) in INPUTS.items():
        if sha256(args.source_directory / name) != expected:
            raise ValueError(f"Unexpected source hash: {name}")

    target = ROOT / "CodeGlyphX/Printing/Fonts"
    target.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(args.source_directory / "gg-ocra.ttf", target / "GGOcra.ttf")
    original = TTFont(args.source_directory / "ocr-b-original.ttf", recalcTimestamp=False)
    names = {1: "CodeGlyphX Scan B", 2: "Regular", 3: "CodeGlyphXScanB-Regular-1.1",
             4: "CodeGlyphX Scan B Regular", 6: "CodeGlyphXScanB-Regular",
             16: "CodeGlyphX Scan B", 17: "Regular", 18: "CodeGlyphX Scan B Regular",
             21: "CodeGlyphX Scan B", 22: "Regular"}
    # Preserve all language/platform records and the original copyright and license.
    for record in original["name"].names:
        if record.nameID in names:
            record.string = names[record.nameID].encode(record.getEncoding())
    output = target / "CodeGlyphXScanB-Regular.ttf"
    original.save(output, reorderTables=False)
    renamed = TTFont(output, recalcTimestamp=False)
    for tag in original.keys():
        if tag not in ("GlyphOrder", "name", "head"):
            if original.getTableData(tag) != renamed.getTableData(tag):
                raise ValueError(f"Renaming changed the {tag} table")
    for name in ("GG-Ocra-CC0.txt", "Raisty-OCR-B-OFL.txt"):
        shutil.copyfile(args.source_directory / name, ROOT / "Licenses" / name)
    manifest = {
        "tool": "fontTools 4.66.1; Prepare-OcrFontAssets.py",
        "inputs": {name: {"url": url, "sha256": digest} for name, (url, digest) in INPUTS.items()},
        "outputs": {name: sha256(target / name) for name in ("GGOcra.ttf", output.name)},
        "modifications": "GG Ocra unchanged. OCR-B primary names changed to CodeGlyphX Scan B; outlines, metrics and layout tables unchanged."
    }
    (target / "provenance.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(manifest["outputs"], indent=2))


if __name__ == "__main__":
    main()
