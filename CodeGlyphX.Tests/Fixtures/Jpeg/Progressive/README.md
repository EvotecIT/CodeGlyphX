# Progressive JPEG pixel references

I copied my two ImagePlayground photographs here to cover the progressive JPEGs
that failed with `Unexpected JPEG end` in the default strict `ImageReader` path:

| JPEG | Dimensions | Encoded bytes | Source |
| --- | --- | ---: | --- |
| `KulekWSluchawkach.jpg` | 400 × 400 | 19,711 | `Sources/ImagePlayground.Tests/Images/KulekWSluchawkach.jpg` |
| `PrzemyslawKlysAndKulkozaurr.jpg` | 1624 × 1624 | 141,189 | `Sources/ImagePlayground.Tests/Images/PrzemyslawKlysAndKulkozaurr.jpg` |

The originals are from [EvotecIT/ImagePlayground at commit
`c21a1e1a967f6d9c62b4d244fbe126862b6804db`](https://github.com/EvotecIT/ImagePlayground/tree/c21a1e1a967f6d9c62b4d244fbe126862b6804db/Sources/ImagePlayground.Tests/Images),
committed by me on 27 December 2022. They retain ImagePlayground's MIT license;
the complete copyright and permission notice is in `ImagePlayground-LICENSE.txt`.
The other JPEG in that directory, `QRCode1.jpg` (21,319 bytes), passed the strict
decode on the pre-fix revision and is not duplicated here.

I generated the synthetic fixtures with libjpeg-turbo 3.2.1 `cjpeg -quality 85
-progressive` from the deterministic pixel formulas in `generate.py`:

| Fixture | Dimensions | Regression covered |
| --- | --- | --- |
| `refinement.jpg` | 32 × 24 grayscale | Spectral bands and successive approximation; newly nonzero AC signs before correction bits, and runs through existing nonzero coefficients |
| `eob-runs.jpg` | 64 × 16 grayscale | EOB runs spanning multiple constant blocks, including negative DC refinement |
| `odd-subsampled.jpg` | 17 × 19, 4:2:0 | Single-component scan dimensions exclude interleaved MCU edge padding; later scans select their own chroma Huffman tables |
| `restart-refinement.jpg` | 32 × 24 grayscale | Refinement with `-restart 2B`, resetting DC predictors and EOB state at restart boundaries |

The matching PNGs are independent decodes of the JPEGs, not the original input
pixels or output from CodeGlyphX. I used the official
[libjpeg-turbo 3.2.1 tools](https://github.com/libjpeg-turbo/libjpeg-turbo/releases/tag/3.2.1)
(Linux x64 build 20261027):

```sh
djpeg -strict -dct int -nosmooth -outfile reference.ppm input.jpg
```

Pillow 11.0.0 converts those already decoded PPM pixels to lossless RGB PNGs.
`-nosmooth` selects nearest-neighbour chroma upsampling, matching the default
managed decoder. Tests compare every RGB channel with an absolute tolerance of
3 on the 0–255 scale for color images and require exact grayscale and alpha. This allows small integer IDCT
rounding differences and their YCbCr-to-RGB propagation; the observed photo
maxima are 2 and 3 respectively. No averaging, downsampling, or excluded edge
pixels can conceal a wrong block. `sha256.json` records the fixture bytes.

To regenerate, put libjpeg-turbo 3.2.1 `cjpeg` and `djpeg` on `PATH`, install
Pillow for that Python interpreter, and run:

```sh
python generate.py --scratch /path/to/temporary/output
```

These tools are used only to regenerate committed fixtures. Builds and tests
require neither Python nor a native JPEG decoder, and the shipped library gains
no dependency.

The decoder rules follow ITU-T T.81, Annex G; useful independent implementation
references are libjpeg-turbo's
[`start_pass_phuff_decoder`, `decode_mcu_DC_refine`, and `decode_mcu_AC_refine`](https://github.com/libjpeg-turbo/libjpeg-turbo/blob/3.2.1/src/jdphuff.c)
and its [noninterleaved scan geometry](https://github.com/libjpeg-turbo/libjpeg-turbo/blob/3.2.1/src/jdinput.c).
The tests also remove the final scan's entropy while retaining its SOS and EOI,
reject malformed successive-approximation parameters and out-of-range Huffman
selectors in later scans, and exercise the existing encoded-byte, pixel-count,
and decoded-buffer limits. Strict progressive decoding requires a complete EOI;
removing one or both EOI bytes is accepted only with explicit `AllowTruncated`.
