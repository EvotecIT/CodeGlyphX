Image Samples

This folder is the **image format corpus** for ImageReader coverage tests
(PNG/TIFF edge cases, interlace, packed bit-depths, palettes, etc.). The files
include twelve small PngSuite PNG fixtures stored in the repo. Their original
hashes remain in the manifest and the download script verifies them before
skipping network access. Larger TIFF samples remain external; use the download
script to fetch them.

Download:
  pwsh Build/Download-ImageSamples.ps1

If you want to store samples outside the repo, set:
  CODEGLYPHX_IMAGE_SAMPLES

CI downloads the hash-pinned corpus before running tests. Missing required
entries fail the suite instead of being reported as passing tests.

Manifest:
  manifest.json
Fields are intentionally simple:
  - downloadUrl or archiveUrl + archivePath
  - fileName
  - format / width / height
  - sha256
  - source / license

Sources / attribution:
  - PNG Suite (libpng): https://libpng.org/pub/png/PngSuite/
    Copyright (c) Willem van Schaik. PngSuite permits use, copying and distribution
    for any purpose without fee; see the [upstream permission notice](https://github.com/pnggroup/libpng/blob/d76d5106f041b97b3462d15bce738f2a4412de47/contrib/pngsuite/README).
  - libtiff pic samples: https://download.osgeo.org/libtiff/pics-3.8.0.tar.gz
    See libtiffpic/README inside the archive for descriptions.
