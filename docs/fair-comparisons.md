# Barcode comparison methodology

CodeGlyphX's current comparison uses matched QR generation settings and an independent, frozen image corpus. Historical QR-pack percentages used different result cardinalities across engines and do not establish a competitive ranking.

The [current results and complete evidence](comparisons/results.md) report the repaired source against the original source, both cache domains, and every miss. Recognition improves from 73 to 121 of 267 positive images in this run; ZXing.Net matches 220 and ZXing-C++ matches 224. No previously correct image is lost. CodeGlyphX has the lowest PNG medians for the three tested payloads on both domains, while the native engine leads module generation and QRCoder uses fewer managed bytes for PNG output. These are shared-host measurements with all outliers retained.

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

Expected and returned payloads are retained verbatim. Comparison applies only these physical representation rules: Codabar start/stop letters, UPC-A's optional leading zero, compact versus expanded UPC-E, and GS1 DataBar's equivalent GTIN/AI presentation. Whitespace, Unicode, line endings and empty payloads remain significant. Every missing or unexpected payload remains in the result packet. A single expected oracle can omit an additional real symbol, so an unexpected result is not automatically a false positive; negative-image results are reported separately.

An empty negative-image result counts as a correct rejection only when scanning completes. Deadline and recognition-budget exhaustion remain inconclusive. The supplementary negative corpus contains all 25 images from both upstream false-positive directories, including the twelve in the main corpus. CodeGlyphX returns no payload but exhausts its budget on all 25; that does not establish a lower false-positive rate. The frozen supplementary manifest is retained with the evidence and can be acquired using `Get-DecodeCorpus.ps1 -ManifestPath`.

The nineteen-image timing subset is locked separately: the first selected image of each format plus the four-QR image. Correct-image and unexpected-image metrics accompany each timing sample. Failed and unsupported decodes are retained; no timing-only decoder ranking is produced.

## Reproduce

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

Remaining corpus misses are recorded individually in the generated results report. Detection robustness, formats without an image scanner, and undeclared character-encoding policies are separate findings. A corrected comparison does not establish that CodeGlyphX is universally faster or more accurate.

The largest remaining recognition gaps are PDF417 (2/18 correct), GS1 DataBar Expanded (0/18), QR photographs (22/37) and retail photographs. Expanded Stacked and MaxiCode have module decoders but no image scanners, accounting for eighteen unsupported images. One decoded QR has undeclared Shift-JIS byte text: the strict default treats those bytes as Latin-1, while the other readers infer an encoding. Changing that default requires a deliberate text-policy decision rather than trimming or rewriting the comparison result.

The nineteen-image latency subset is consistently 13/19 correct for CodeGlyphX and 19/19 for both other readers. CodeGlyphX takes about 4.1 seconds per subset with its Robust profile, compared with 196–218 ms for ZXing.Net and 30–31 ms for the native reader. Search completion and locating difficult symbols remain substantial gaps despite the repaired linear and Data Matrix paths.
