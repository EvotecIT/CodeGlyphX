# WebP regression vectors

The `qualified-vp8l-*.webp` files are representative output from the corrected
CodeGlyphX lossless writer. Pillow 12.3.0 using libwebp 1.6.0 independently decoded
each vector and verified every RGBA byte against the original pixels. The JSON
manifest records the producing revision, encoder input, encoded-file SHA-256 and
independently decoded RGBA SHA-256.

The vectors cover the 840×420 checkerboard failure, a literal/codebook fallback,
and a multichannel image with alpha. `WriteRgba32_MatchesIndependentlyDecodedVp8lVectors`
requires the current writer to reproduce these qualified bitstreams and checks the
pixels against the independent hashes. The semantic size/stride/alpha round-trip
tests remain separate.

Run `python Build/Verify-WebpVectors.py` from the repository root to repeat the
independent qualification. Python and Pillow/libwebp are optional fixture-validation
tools; they are not library dependencies or ordinary build/test requirements.

When an intentional encoder strategy change alters a vector, regenerate it from
the input in `WebpLargeRoundTripTests.CreatePixels`, decode it with libwebp, compare
every decoded RGBA byte with the original input, and update the manifest and test
hashes together. Use the independent decoder's result as the qualification oracle.
The bitstream pins apply to these conformance vectors, not to the public encoder's
choice of compression strategy.

The `independent-vp8-*` fixtures exercise the separate lossy VP8 reader path.
