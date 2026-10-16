"""Independently qualify the checked-in lossless WebP regression vectors.

Pillow/libwebp is used only for this optional test-fixture qualification step.
The library and ordinary test runs do not depend on it.
"""

import hashlib
import io
import json
from pathlib import Path

from PIL import Image, features
import PIL


def main():
    root = Path(__file__).resolve().parent.parent / "CodeGlyphX.Tests/Fixtures/Webp"
    manifest = json.loads((root / "qualified-vp8l-vectors.json").read_text(encoding="utf-8"))
    print(f"Independent decoder: Pillow {PIL.__version__}, libwebp {features.version('webp')}")
    for vector in manifest["vectors"]:
        path = root / vector["file"]
        encoded = path.read_bytes()
        if encoded[12:16] != b"VP8L":
            raise ValueError(f"Expected a lossless VP8L stream: {path.name}")
        if hashlib.sha256(encoded).hexdigest() != vector["encodedSha256"]:
            raise ValueError(f"Encoded-byte hash differs: {path.name}")
        with Image.open(io.BytesIO(encoded)) as image:
            if image.size != (vector["width"], vector["height"]):
                raise ValueError(f"Decoded dimensions differ: {path.name}")
            decoded = image.convert("RGBA").tobytes()
        if hashlib.sha256(decoded).hexdigest() != vector["decodedRgbaSha256"]:
            raise ValueError(f"Independently decoded pixels differ: {path.name}")
        print(f"PASS {path.name}: {len(encoded)} encoded bytes, {len(decoded)} decoded bytes")


if __name__ == "__main__":
    main()
