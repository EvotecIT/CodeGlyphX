"""Regenerate independent JPEG fixtures/PNG references; not required by the tests.

Requires Python, Pillow, and libjpeg-turbo 3.2.1 cjpeg/djpeg on PATH.
Run from any directory: python generate.py --scratch /path/to/temporary/output
"""

import argparse
import hashlib
import json
import subprocess
from pathlib import Path

from PIL import Image


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--scratch", type=Path, required=True)
    args = parser.parse_args()
    args.scratch.mkdir(parents=True, exist_ok=True)
    fixtures = Path(__file__).resolve().parent
    patterns = [
        ("refinement", 32, 24, "L", lambda x, y: (x * 17 + y * 29 + (x * y) % 71) % 256),
        ("eob-runs", 64, 16, "L", lambda x, y: 75 if x < 32 else 149),
        ("odd-subsampled", 17, 19, "RGB", lambda x, y: ((x * 17 + y * 3) % 256, (y * 19) % 256, (x * 11 + y * 7) % 256)),
        ("restart-refinement", 32, 24, "L", lambda x, y: (x * 17 + y * 29 + (x * y) % 71) % 256),
    ]
    for name, width, height, mode, pixel in patterns:
        image = Image.new(mode, (width, height))
        image.putdata([pixel(x, y) for y in range(height) for x in range(width)])
        source = args.scratch / (name + ".ppm")
        image.save(source)
        command = ["cjpeg", "-quality", "85", "-progressive"]
        if name.startswith("restart"):
            command += ["-restart", "2B"]
        subprocess.run(command + ["-outfile", str(fixtures / (name + ".jpg")), str(source)], check=True)

    hashes = {}
    for jpeg in sorted(fixtures.glob("*.jpg")):
        ppm = args.scratch / (jpeg.stem + "-reference.ppm")
        # Match the managed reader's default nearest-neighbour chroma upsampling.
        subprocess.run(["djpeg", "-strict", "-dct", "int", "-nosmooth", "-outfile", str(ppm), str(jpeg)], check=True)
        png = jpeg.with_suffix(".png")
        with Image.open(ppm) as image:
            image.convert("RGB").save(png, optimize=True)
        hashes[jpeg.name] = hashlib.sha256(jpeg.read_bytes()).hexdigest()
        hashes[png.name] = hashlib.sha256(png.read_bytes()).hexdigest()
    (fixtures / "sha256.json").write_text(json.dumps(hashes, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
