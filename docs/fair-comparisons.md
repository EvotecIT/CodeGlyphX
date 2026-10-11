# Barcode comparison methodology

CodeGlyphX's current comparison uses matched QR generation settings and an independent, frozen image corpus. Historical QR-pack percentages used different result cardinalities across engines and do not establish a competitive ranking.

The [current results and complete evidence](comparisons/results.md) compare the repaired source with the original source and the previous decoding pass. They report every miss, completed negative scan, inconclusive search and timing sample across both cache domains. Recognition, latency and managed allocation are separate results. These are shared-host measurements with all outliers retained.

The comparison engines are opt-in dependencies of the non-packable benchmark executable. CodeGlyphX's shipped library does not reference them. QRCoder participates in generation only.

## Engine and input boundaries

| Engine | Package | Role | License |
| --- | --- | --- | --- |
| CodeGlyphX | Local source | Generation and decoding | Apache-2.0 |
| ZXing.Net | 0.16.11 | Generation and decoding | Apache-2.0 |
| ZXing-C++ | ZXingCpp 0.5.3, package reports native engine 3.1.1 | Generation and decoding through the official binding | Apache-2.0 |
| QRCoder | 1.8.0 | Generation | MIT |

The ZXingCpp adapter pins input pixels for the complete native call and disposes returned native barcodes. Its generation adapter uses explicit UTF-8 bytes because the binding's string constructor passes a UTF-16 character count to a UTF-8 length parameter. The package's native library hash is retained with the result evidence; its native version is the publisher's reported version.

Generation fixes error correction at M, version at 4 for a URL, 2 for 32 numeric digits and 16 for a multilingual payload, with eight pixels per module and a four-module quiet zone. ASCII inputs may use compact QR numeric/alphanumeric modes. Multilingual byte segments declare UTF-8 through ECI; QRCoder's `forceUtf8` alone does not declare the encoding. Every artifact is materialized. Each measured batch produces 100 deterministic outputs; validation checks every output against the first and checks its version, error correction, dimensions and full payload outside timing.

The Modules lane includes each binding's materialization into the same one-byte-per-module representation. The PNG lane includes encoding and rendering. CodeGlyphX and QRCoder use their own PNG APIs; both ZXing adapters use the same CodeGlyphX RGBA PNG codec because those bindings have no PNG encoder. These PNG results measure the complete adapter workflow. They do not isolate QR encoding speed or independent PNG codec implementations. Managed allocated bytes exclude native allocations.

The image corpus has 279 upstream ZXing images: deterministic SHA-256 path ordering selects up to six eligible UTF-8-oracle images per black-box directory, plus twelve negative images and one four-QR image. Each file has a locked hash. Binary-oracle samples are excluded. The selection does not filter out failures. This is a reproducible sample of that project's corpus, with possible corpus bias in favor of ZXing, rather than a representative survey of all camera images.

All readers receive the same preloaded RGBA32 pixels, with image loading outside decode timing. Positive images supply the expected symbology as a hint. A single-result API is used for one expected payload; multi-result APIs are used for multiple payloads or negative images. All rotations and inversions are enabled where supported. CodeGlyphX uses its Robust scan profile with a 1,500 ms image budget; ZXing.Net uses TryHarder and the C++ binding uses TryHarder. The other APIs do not expose an equivalent cooperative deadline, so effort is documented rather than treated as identical. The results count distinct payloads, not physical instances with identical payloads.

Expected and returned payloads are retained verbatim. Comparison applies only these physical representation rules: Codabar start/stop letters, UPC-A's optional leading zero, compact versus expanded UPC-E, and GS1 DataBar's equivalent GTIN/AI presentation. Expanded and Expanded Stacked normalize valid parenthesized GS1 application identifiers to their element-string representation; invalid GS1 oracles remain verbatim. This scoring correction accompanies the Expanded pixel-decoder repair, so its improvement includes both decoding and equivalent-presentation scoring. Whitespace, Unicode, line endings and empty payloads remain significant. Every missing or unexpected payload remains in the result packet. A single expected oracle can omit an additional real symbol, so an unexpected result is not automatically a false positive; negative-image results are reported separately.

An empty negative-image result counts as a correct rejection only when scanning completes. Deadline and recognition-budget exhaustion remain inconclusive. The unchanged supplementary corpus contains all 25 images in upstream `falsepositives-2`; six overlap with the twelve main-corpus negatives. The two sets are reported separately. Earlier prose described a different folder selection and overlap; the retained IDs and hashes identify the actual measured set. The frozen supplementary manifest is retained with the evidence and can be acquired using `Get-DecodeCorpus.ps1 -ManifestPath`. Results on these small sets do not establish a population false-positive rate.

The nineteen-image timing subset is locked separately: the first selected image of each format plus the four-QR image. Correct-image and unexpected-image metrics accompany each timing sample. Failed and unsupported decodes are retained; no timing-only decoder ranking is produced.

## Reproduce

The website reads `Website/data/comparisons.json`, generated from the checked-in evidence. Its page and quick summary share the same data and shortcode. Refresh or verify that projection without rerunning measurements:

```text
python Build/comparison_website_data.py --evidence-root docs/comparisons/evidence --website-root Website
python Build/comparison_website_data.py --evidence-root docs/comparisons/evidence --website-root Website --check
```

`Build/generate-fair-comparison-report.py --website-root Website` also refreshes the projection when generating a new report. PowerForge copies `docs/comparisons` into the site for raw evidence downloads. Historical January data remains an archive; the active page does not use its rankings.

Use PowerShell 7 with PSPublishModule's shared benchmark runner. Choose a mounted development scratch root, then run from the repository root:

```powershell
$scratch = Join-Path $env:EVOTEC_SCRATCH_ROOT 'barcode-comparisons'
dotnet build CodeGlyphX.Benchmarks -c Release -f net10.0 `
    -p:CompareZXing=true -p:CompareZXingCpp=true -p:CompareQRCoder=true `
    -p:CompareBarcoder=false --artifacts-path (Join-Path $scratch 'build')
./Build/Get-DecodeCorpus.ps1 -OutputRoot (Join-Path $scratch 'corpus')
$assembly = Join-Path $scratch 'build/bin/CodeGlyphX.Benchmarks/release_net10.0/CodeGlyphX.Benchmarks.dll'
dotnet $assembly --corpus ./CodeGlyphX.Benchmarks/Corpus/corpus-manifest.json `
    (Join-Path $scratch 'corpus') (Join-Path $scratch 'corpus-results.json')
Import-Module PSPublishModule
Invoke-BenchmarkSuite -Path ./Build/fair-comparisons.benchmark.ps1 -Variable @{
    AssemblyPath = $assembly; OutputRoot = (Join-Path $scratch 'generation')
    Iterations = '12'; Warmup = '3'
}
Invoke-BenchmarkSuite -Path ./Build/decode-corpus.benchmark.ps1 -Variable @{
    AssemblyPath = $assembly
    ManifestPath = (Resolve-Path ./CodeGlyphX.Benchmarks/Corpus/latency-manifest.json).Path
    ImageRoot = (Join-Path $scratch 'corpus'); OutputRoot = (Join-Path $scratch 'decoding')
    Iterations = '12'; Warmup = '3'
}
```

Use the same verified CPU domain and process priority for every engine on machines with multiple cache domains. Keep source unchanged during a suite: the shared runner rejects changed provenance. It owns warmup, rotated engine order, timing, environment metadata and JSON/CSV/Markdown reports. Retain all samples and outliers. Run unrelated tests and builds before the timing suites.

## Decoder regressions covered

The corpus exposed an implicit Data Matrix unlatch that discarded the last ASCII codeword after C40, Text and X12 segments. Those three paths preserve the final character. An invalid EAN first-digit parity now returns a decode failure instead of throwing from a dictionary lookup.

Linear image decoding locates guarded symbols inside scanlines rather than requiring all foreground runs to belong to the barcode. It estimates local digit/character widths for retail barcodes, Code 128 and Code 39 while retaining existing parity, stop and checksum validation. Independent photograph fixtures cover complete payloads, including significant spaces. Ordinary ITF scanning rejects partial scan regions without the ten-module quiet zones needed to prevent a clipped barcode fragment from becoming a different number. Full-frame and low-level module decoding retain their existing contracts.

Code 93 image decoding locates symbols inside framed scanlines in either direction and polarity. Localized candidates require both Code 93 checksums and the termination bar, within the existing recognition budget. Complete checksum-free symbols retain their low-level decoding behavior. An unmodified independent framed diagram covers single-result decoding, multi-result decoding and scanning. Generated cases cover reversed, vertical and inverted symbols, damaged checksums, missing termination and checksum-free compatibility.

Retail scanlines add two bounded local-contrast thresholds for uneven lighting. Sliding minimum and maximum windows keep this work linear in the scanline length, with periodic cancellation checks and pooled scratch buffers. These hypotheses pass the existing guard, digit-parity and checksum checks. Validated global payloads and supplements retain priority. A local result must also match its complete base symbol's module count and payload length, preventing a longer striped pattern from being accepted as a shorter UPC-E prefix. Regression fixtures cover shaded EAN and UPC photographs, generated gradients, vertical and inverted symbols, an invalid checksum, and two striped negative images.

PDF417 pixel decoding reads local codeword widths and row indicators rather than assuming equal physical row heights. Repeated observations vote for complete row data; a bounded slope search combines rows from a perspective symbol at a common physical anchor. Compact symbols and separate regular fragments retain independent candidates. Adjacent scanline groups can contribute partial header evidence after their rows pass the complete header's indicator checks. A second pass compensates for consistent bar widening or thinning after the ordinary row reader fails; repeated consistent row indicators are required before this retry. Existing error correction, length-descriptor and payload validation still determine success. Tests cover unequal row heights, damaged and missing rows, transparent backgrounds, perspective symbols near a page edge, compact symbols beside fragments, adjacent-group boundaries, and payload print gain in normal and inverted images.

PDF417 accepts the reader-initialization marker only in the first data position and returns the following payload without applying device settings. Pixel rows can retain their final readable character when its space reaches a cropped image edge. Rows with a visible right indicator and a missing stop retain ordinary indicator and header validation. Scanline run buffers and guard patterns are reused without changing search decisions.

At each pixel threshold, PDF417 tries additional shallow angles after its ordinary scanline and print-gain searches fail. Complete data rows with damaged right framing can contribute only after at least three distinct framed rows validate a regular symbol; their left indicators and physical order must match that assembly.

PDF417 image recovery also preserves readable prefixes from damaged row tails and uses known missing codeword positions as erasures. Partial rows require an independently validated row assembly; complete framed observations take priority. Ordinary decoding runs across all slopes before recovery tries at most 128 retained candidates per orientation. Mixed correction requires twice the unknown-error count plus the erasure count to fit within the parity count, followed by zero-syndrome, length-descriptor and payload checks. Correction loops retain cancellation and the scan deadline. Generated missing-row and damaged-tail images, unmodified damaged photographs, and seeded codeword vectors cover recovery within capacity and rejection of malformed or excessive damage. Enough readable framing remains necessary; these checks do not establish recovery from arbitrary occlusion or perspective.

Data Matrix pixel decoding locates a connected solid finder L after the ordinary axis-aligned grids fail, then checks a bounded set of perspective grids against the timing edges. Detection uses at most one megapixel and retains only a small fixed number of components and grid dimensions. Sampled modules still require region framing, error correction and payload validation. Detailed results preserve GS1, ECI and rectangular DMRE metadata. Independent photograph fixtures cover perspective and low contrast; generated fixtures cover rotations, reflection, inversion and damaged internal framing. Nonpositive pixel strides retain the existing failure return across the plain, diagnostic and detailed APIs.

Aztec mode decoding recognizes the reader-initialization count flag only where the layer count and symbol capacity permit it. It returns the encoded payload without changing reader settings. Ordinary large symbols retain their high count bit, and both parameter-reading paths share the same validation.

Aztec pixel detection reads oriented samples through a lazy grid view. It checks framing and mode parameters before sampling a candidate's complete payload grid, while retaining the same rotations, reflections, threshold comparisons and clamped majority samples. Periodic checks preserve cancellation and the monotonic recognition deadline. Packed matrix transforms avoid repeated checked cell copies in both Aztec and PDF417 searches. PDF417 also reuses normalized codeword runs, scans zero-slope rows directly and slides the header window rather than rereading every overlapping bit. Its alpha handling and matching tolerances remain the same; rotations narrower than the existing minimum symbol width are skipped.

GS1 DataBar Expanded image sampling preserves the light guards and normalizes each character's local pixel runs. Odd and even character layouts, inverted images and fractional module pitches retain checksum validation and exact GS1 element strings.

PDF417 pixel decoding indexes character candidates before applying the complete eight-run fit, preserving acceptance tolerances and codeword tie order. It reuses sampled lines across direction, polarity and print-gain retries within one thresholded image. Cache accounting is limited to 8 MiB and 8,192 entries; lines that do not fit the cache are sampled normally. This is a cache reservation limit, not a limit on total decoder allocations. Cached reads check cancellation and the same monotonic deadline. Compact symbols retain their shorter row framing, including one-column images and 69-module two-column symbols.

Expanded Stacked row patterns that share a first finder reuse that fit. Each pattern retains its original observation order, character normalization, guard checks, geometry and checksum assembly. Differential checks retain the complete observations from all twelve frozen images; the full corpus and timing passes assess recognition and completion separately.

Expanded Stacked image scanning fits physical rows against templates from the encoder's canonical layout and reuses the same character normalization, row assembly, finder sequence and checksum validation. It samples both image axes and line directions, retains inversion across rows, and checks cancellation and the monotonic deadline during collection and assembly. Each orientation samples at most 256 lines, each row pattern retains at most 64 observations, and assembly tries at most 4,096 search nodes. The scanner returns one result; arbitrary perspective and multiple-symbol localization are not established. Mixed/default requests require multiple rows before reporting the stacked format; explicit Stacked-only requests also accept one-row layouts. Compact rotations, independent module fixtures, truncated rows, budget interruption and subsequent decoding have focused regression coverage. The long AI (3922) oracle in `rssexpandedstacked-2/13.png` fails GS1 field-length validation. Its raw digits remain available, while its invalid parenthesized oracle remains unchanged and the comparison records a failure.

Negative-search work avoids large QR grid allocations without plausible finder evidence and bounds Aztec offset retries. Opaque uniform frames complete immediately; transparent frames retain their normal image search. Scanner attempt budgeting gives QR three shares and Data Matrix two because they search additional scales or preprocessing variants; other families and the tile stage receive one share each. Tiles use the same family weights. These shares divide the same total deadline, and explicit recognition limits still cap each attempt. Cancellation and diagnostics remain observable through the public pixel and scanner APIs. Empty results from unfinished searches still remain inconclusive in the comparison.

Data Matrix checks each candidate orientation through a view over the existing module grid. Unsupported dimensions and invalid finder borders are rejected before a data region is copied for codeword reading and error correction. The original and mirrored rotation order, per-region border allowances, payload rules and recognition budgets are preserved. Successful mirrored module decoding clears the failure message from its earlier unsuccessful attempt. Independent square, original rectangular and DMRE orientation checks protect exact GS1 payloads and symbol metadata.

Before sampling an axis-aligned Data Matrix grid, the decoder checks whether its estimated dimensions match a supported symbol in either orientation. An unsupported estimate first attempts clock-track recovery; if that fails, perspective recovery remains available. Supported estimates retain priority over clock-derived alternatives. Fractional-pitch square, rotated rectangular and DMRE cases protect that recovery path. This avoids allocating and sampling grids that cannot decode, without increasing the recognition allowance.

QR component discovery groups broad foreground regions into horizontal runs and uses pixel traversal for narrow strokes and fragmented noise. Both paths retain four-connected geometry, exact area and bounds, row-major component order, and the existing candidate filters. Cancellation checks run inside large components, and an interrupted component is never admitted as a complete finder candidate. Differential checks against the previous implementation and an independent reference complement the public image-decoding tests. Component costs vary with image structure; the complete-image corpus and timing results remain the basis for comparisons.

On small images with plausible finder evidence, modern QR recognition tries local normalization and hybrid thresholding before broad transform retries. One ordered finder triple receives the existing perspective sampling precision, within the caller's original recognition allowance. Recovered symbols retain structural, error-correction and payload validation. Single-result decoding, multi-result decoding and the scanner share this recovery; the legacy implementation retains its existing search. An independently encoded shaded photograph protects all three public paths. This regression establishes recovery of that image, while broader photograph accuracy and negative-search completion remain measured separately.

Modern QR finder validation accounts for rotated square geometry. A diagonal pixel step advances along both finder axes, so its run count can be half the horizontal count. The diagonal check accepts that lower bound while preserving its upper mismatch bound, all five run ratios, and the horizontal and vertical checks. Sampling, error correction, payload validation, search limits and recognition allowances remain unchanged. Three unmodified, independently encoded small QR images at different angles protect single-result decoding, multi-result decoding and scanning. Their recovery is regression evidence; accuracy on unseen images remains unproved.

Modern QR perspective sampling fits the three detected finder centers and an alignment center to the module grid. It uses the existing perspective transform and sampler, with the earlier approximate alignment fallback still available. Bounds, error correction, payload checks and recognition allowances remain unchanged. An unmodified perspective photograph protects the exact bookmark payload through single-result decoding, multi-result decoding and scanning. The frozen corpus records additional gains separately; this regression does not establish recovery from arbitrary perspective.

Direct-part-mark preprocessing composites transparent pixels against the same white background as ordinary decoding. It cannot reveal a barcode stored only in invisible RGB values. Preprocessing variants are generated as needed and stop after a successful decode, an attempt limit, cancellation or the monotonic recognition deadline. The clock check also works on single-threaded hosts whose cancellation timers cannot run during recognition.

Negative completion is sensitive to shared-host contention. The main twelve negatives and supplementary twenty-five negatives are reported separately, including the six overlapping IDs. A paired diagnostic compares the previous and current binaries on all 31 distinct negative IDs in alternating order across both cache domains. Every outcome and the CPU samples before each pass are retained with the generated report; a favorable pass does not erase an inconclusive result from a busier pass.

Remaining corpus misses are recorded individually in the generated results report. Detection robustness, formats without an image scanner, and undeclared character-encoding policies are separate findings. A corrected comparison does not establish that CodeGlyphX is universally faster or more accurate.

Supplemental PDF417 photographs, Aztec, QR photographs and retail photographs retain recognition misses. The supplemental PDF417 set comes from the same upstream collection and informed a permanent regression test; it is separate from the frozen ranking and is not an unseen holdout for the final candidate. MaxiCode has a module decoder and no image scanner, accounting for six unsupported images. QR samples with undeclared Shift-JIS byte text illustrate a text-policy difference: the strict default treats those bytes as Latin-1, while the other readers infer an encoding. Changing that default requires a deliberate text-policy decision rather than trimming or rewriting the comparison result.

The nineteen-image latency table reports accuracy alongside each engine's retained timing samples. It includes an unsupported MaxiCode image, so a faster batch alone does not show equivalent decoding capability. Search completion and locating difficult symbols remain separate from generation speed.
