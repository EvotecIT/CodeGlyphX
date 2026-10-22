# Independent barcode fixtures

The JSON files contain module grids or bar runs produced by independent encoders. Normal builds and tests consume the committed files and do not install or execute those encoders.

| File | Generator | Regeneration entrypoint |
| --- | --- | --- |
| `bwipp-aztec-runes.txt` | bwip-js 4.11.4, all 256 Rune values; three independent PNGs in the Aztec fixture folder | `Build/Generate-AztecRuneFixtures.mjs` |
| `bwipp-hibc-aztec.txt` | bwip-js 4.11.4 HIBC primary, secondary, concatenated and supplemental Aztec payloads | `Build/Generate-HibcAztecFixtures.mjs` |
| `bwipp-composite-compaction.txt` | bwip-js 4.11.4 date/lot and AI 90 compaction across CC-A, CC-B and CC-C | `Build/Generate-CompositeCompactionFixtures.mjs` |
| `bwipp-composite-carriers.txt` | bwip-js 4.11.4 EAN/UPC and DataBar linked carriers, retail add-ons and GS1 mode transitions | `Build/Generate-CompositeCarrierFixtures.mjs` |
| `zint-composite-carriers.txt` | Published Zint 2.15.0 regression grids for Limited and Expanded Stacked | `Build/Generate-ZintCompositeFixtures.py` |
| `zint-wide-composite.txt` | Isolated Zint 2.15.0 CLI, six wide Expanded and Expanded Stacked CC-A/B inputs | `Build/Generate-ZintWideCompositeFixtures.py` |
| `bwipp-code-one.txt` | bwip-js 4.11.4 all 14 Code One sizes, compaction modes, capacity boundaries, GS1 and ECI | `Build/Generate-CodeOneFixtures.mjs` |
| `bwipp-linear.json` | bwip-js 4.11.4, embedding BWIPP 2026-05-28 | `Build/Generate-BarcodeConformanceFixtures.mjs` |
| `bwipp-current.json` | BWIPP revision `4a678ddb7a9fb59f6b3ba64eba81e2ef1f71ca77` with Ghostscript | `Build/Generate-BwippPostScriptFixtures.py` |
| `zint-micropdf.json` | Zint 2.13.0 HIBC MicroPDF417, all four column counts | `Build/Generate-ZintConformanceFixtures.py` |
| `bwipp-specialist-images.json` | bwip-js 4.11.4 specialist PNG manifest | `Build/Generate-SpecialistImageFixtures.mjs` |
| `bwipp-swiss-capacity.json` | bwip-js 4.11.4 Swiss QR UTF-8 capacity vectors | `Build/Generate-SwissQrCapacityFixtures.mjs` |
| `bwipp-boundaries.json` | Pinned BWIPP Code 49 row capacities, short linked PosiCode rows and fourteen-digit PLANET | `Build/Generate-BarcodeBoundaryFixtures.py` |
| `bwipp-linear-controls.txt` | bwip-js 4.11.4 short Channel Code capacities, ISSN issue variants and six MSI check conventions, including check value ten | `Build/Generate-LinearControlFixtures.mjs` |
| `bwipp-australia-controls.txt` | bwip-js 4.11.4 FCC 11/59/62, C/N fields at empty/interior/capacity lengths and ambiguous trailing z/padding | `Build/Generate-AustraliaPostControlFixtures.mjs` |
| `bwipp-optional-checks.txt` | bwip-js 4.11.4 optional linear checks, Code 39 Full ASCII literal check symbols, Code 11 C/K boundary, Plessey CRC and SEMI BC412 second-position checks | `Build/Generate-OptionalCheckFixtures.mjs` |

BWIPP metadata records the built engine SHA-256. Payload bytes are passed as PostScript hex strings. The generators preserve raw oracle output; comparison tests account for quiet margins and repeated row heights without changing data modules.

The pinned bwip-js engine predates the PosiCode corrections used by the current PostScript fixtures. Code 16K's even-block guards in the pinned BWIPP disagree with USS-16K Table C-2 at rows 7, 8, 15 and 16. That output is retained as a negative vector with `invalidRowGuards`, and the encoder is checked separately against the published table. Code 49 reserved FNC3 is likewise retained as a rejection vector.

MicroPDF417 starts in byte compaction. The Zint text vectors protect its required initial text latch; BWIPP raw vectors protect implicit byte decoding. They catch a shared encoder/decoder assumption that same-library round trips would miss. Six-byte groups with no trailing literal byte use mode 924; the implicit-byte vectors deliberately include a trailing byte after a compacted group.

The linked Composite vectors preserve both engines' original grids. Zint and BWIPP differ at column
3 of the DataBar Limited separator: Zint leaves it blank and BWIPP inverts the carrier module there.
Encoding follows Zint; decoding accepts both complete layouts. The checked-in Zint vectors protect
the chosen output, and the BWIPP vectors protect the alternate input. Retail add-on fixtures use a
nine-module gap and include their separate lower band. EAN-8 add-ons are explicitly marked with
BWIPP's `permitaddon` option and are not a general retail compliance claim.

For wide Expanded carriers, Zint 2.15.0 adjusts the Composite separator at the first four finder
blocks; BWIPP adjusts every finder. The additional wide vectors preserve Zint's complete input
shape. The decoder accepts either complete separator pattern and rejects arbitrary mixtures or
damage. Encoded output keeps the full finder adjustment checked by BWIPP.

Some symbologies permit multiple valid encodations. Fixtures marked `exact: false` validate independently generated input without claiming that every encoder makes the same compaction choices. Other fixtures compare the complete emitted modules, including guards and checks. See [third-party notices](../../../THIRD-PARTY-NOTICES.md) for attribution and license texts.
