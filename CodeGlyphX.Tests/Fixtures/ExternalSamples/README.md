External Samples (optional)

Place real-world barcode/QR images here. For each image, add a sidecar text file
with the same base name and a .txt extension that contains the expected decoded
text. For multi-code images, put one expected payload per line. Example:
  sample.png
  sample.txt

You can also use the manifest:
  manifest.json
The download script reads it and writes the sidecars for you.
Use "required": false to mark optional samples (failures only log).

Optional sidecars:
- sample.kind : CodeGlyphKind (Qr, Barcode1D, DataMatrix, Pdf417, Aztec)
- sample.type : BarcodeType (Code128, EAN, UPCA, etc.) for 1D barcodes

The tests will also look at CODEGLYPHX_EXTERNAL_SAMPLES if you prefer to store
samples outside the repo.
