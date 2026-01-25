Image Samples (optional)

This folder is the **image format corpus** for ImageReader coverage tests
(PNG/TIFF edge cases, interlace, packed bit-depths, palettes, etc.). The files
are external and not stored in the repo. Use the download script to fetch them.

Download:
  pwsh Build/Download-ImageSamples.ps1

If you want to store samples outside the repo, set:
  CODEGLYPHX_IMAGE_SAMPLES

Tests will skip if no image files are present. Once samples exist, missing
required entries will fail the test.

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
    See the PNG Suite README on the source site for usage permissions.
  - libtiff pic samples: https://download.osgeo.org/libtiff/pics-3.8.0.tar.gz
    See libtiffpic/README inside the archive for descriptions.
