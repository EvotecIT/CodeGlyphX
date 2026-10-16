# Independent barcode fixtures

The JSON files contain module grids or bar runs produced by independent encoders. Normal builds and tests consume the committed files and do not install or execute those encoders.

| File | Generator | Regeneration entrypoint |
| --- | --- | --- |
| `bwipp-linear.json` | bwip-js 4.11.4, embedding BWIPP 2026-05-28 | `Build/Generate-BarcodeConformanceFixtures.mjs` |
| `bwipp-current.json` | BWIPP revision `4a678ddb7a9fb59f6b3ba64eba81e2ef1f71ca77` with Ghostscript | `Build/Generate-BwippPostScriptFixtures.py` |
| `zint-micropdf.json` | Zint 2.13.0 HIBC MicroPDF417, all four column counts | `Build/Generate-ZintConformanceFixtures.py` |

BWIPP metadata records the built engine SHA-256. Payload bytes are passed as PostScript hex strings. The generators preserve raw oracle output; comparison tests account for quiet margins and repeated row heights without changing data modules.

The pinned bwip-js engine predates the PosiCode corrections used by the current PostScript fixtures. Code 16K's even-block guards in the pinned BWIPP disagree with USS-16K Table C-2 at rows 7, 8, 15 and 16. That output is retained as a negative vector with `invalidRowGuards`, and the encoder is checked separately against the published table. Code 49 reserved FNC3 is likewise retained as a rejection vector.

MicroPDF417 starts in byte compaction. The Zint text vectors protect its required initial text latch; BWIPP raw vectors protect implicit byte decoding. They catch a shared encoder/decoder assumption that same-library round trips would miss. Six-byte groups with no trailing literal byte use mode 924; the implicit-byte vectors deliberately include a trailing byte after a compacted group.

Some symbologies permit multiple valid encodations. Fixtures marked `exact: false` validate independently generated input without claiming that every encoder makes the same compaction choices. Other fixtures compare the complete emitted modules, including guards and checks. See [third-party notices](../../../THIRD-PARTY-NOTICES.md) for attribution and license texts.
