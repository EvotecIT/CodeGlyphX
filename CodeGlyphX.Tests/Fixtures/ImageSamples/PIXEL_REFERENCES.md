The pixel reference cases in `IndependentPngPixelTests` cover packed grayscale,
indexed palettes, palette transparency, 16-bit RGB/RGBA, and Adam7 interlacing.
Each expected value is the SHA-256 of the complete row-major, straight-alpha,
8-bit RGBA buffer decoded by Pillow 12.3.0. The tests use the checked-in PngSuite
fixtures and do not run or require Pillow. Source attribution and permission
are in [README.md](README.md).

To inspect the references with a separately installed Pillow:

```python
from pathlib import Path
from PIL import Image
import hashlib

for path in sorted(Path("CodeGlyphX.Tests/Fixtures/ImageSamples").glob("*.png")):
    with Image.open(path) as image:
        if image.mode.startswith("I"):
            continue  # 16-bit grayscale conversion needs an explicit scaling contract.
        rgba = image.convert("RGBA").tobytes()
        print(path.name, hashlib.sha256(rgba).hexdigest())
```

Reference changes require checking the source fixture and independently decoded
pixels. A decoder change alone is not a reason to regenerate the expected values.
