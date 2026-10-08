# Extending the CodeGlyphX API

CodeGlyphX separates symbol encoding, appearance, output containers and recognition. Add a built-in format at its owning encoder or renderer, then connect it to the shared facades and capability catalogue. Document adapters consume encoded results and keep document placement in the document library.

## Adding a symbol format

| Format geometry | Encoded result and public entry point | Existing consumer route |
| --- | --- | --- |
| Linear bars | A focused encoder returning `Barcode1D`; add its `SymbolFormat` and legacy `BarcodeType` mapping | `Barcode.Encode` and native document `AddBarcode`/`InsertBarcode` methods |
| Orthogonal matrix or stacked rows | A focused encoder and typed result derived from `MatrixSymbol`; add generic matrix dispatch when its input fits that contract | `MatrixBarcode.Encode` and native matrix document methods |
| Another QR variant | A separate encoder and result, following Micro QR or rMQR | The matrix route when its geometry is an orthogonal grid |
| Different geometry | A specialized result and renderer, following MaxiCode | An explicit adapter that preserves that geometry |

`QR.Encode` returns QR Code Model 2. Its version, mask and functional-pattern rules remain specific to that format. Sharing an orthogonal matrix renderer does not make Model-2 QR artwork appropriate for every QR variant. Variant artwork needs its own functional geometry and finder rules.

Existing `SymbolFormat`, `BarcodeType` and `OutputFormat` values are explicitly numbered. Keep those values and existing signatures stable; assign unused values to new members. Catalogue flags describe implemented operations. A matrix family entry with `CanEncode` does not imply that every format accepts the generic `MatrixBarcode.Encode(format, string)` call: Model-2 QR, MaxiCode and GS1 Composite have specialized entry points.

Encoding text must preserve its value or reject a character set that cannot be represented. Keep byte input distinct from text input. PDF417 text uses a supported ECI character-set declaration for byte compaction. Micro QR has no ECI; use representable byte-mode text, `EncodeKanji` for supported Kanji, or an explicit byte payload.

For a new image decoder, update catalogue image support, scanner dispatch, initial and tile attempt counting, and typed result projection together. Keep unsupported module-only selections visible in `UnsupportedFormats`. Explicit `ScanOptions.Formats` fixes the caller's requested set even when catalogue defaults gain another built-in format.

## Adding artwork or output formats

Appearance belongs to `QrRenderOptions`, `BarcodeOptions` or `MatrixOptions`; encoding settings and output-container settings remain separate. A new QR appearance property needs the same contract through option snapshots, builders, presets and relevant rendering adapters.

Each output path must preserve a setting or reject it explicitly. PNG is the complete QR raster appearance owner. SVG/SVGZ and styled HTML implement a defined subset. PDF/EPS use their existing raster fallback when vector drawing cannot preserve an effect. Protected functional modules and finder geometry must retain their intended appearance through each supported route.

For another output format, update `OutputFormat`, `OutputFormatInfo`, output dispatch and the format-specific container settings together. Keep `RenderedOutput` ownership stable: callers receive read-only data and obtain an owned byte copy with `ToArray()`. Caller-provided streams remain open after rendering or saving.

Art themes, procedural patterns, image treatments and scene styles have separate owners. Add a concrete implementation and preset mapping for each new built-in choice. Persisted scene recipes also have a schema version; define the read and migration behavior when that schema changes.

## Recognition completion and limits

Scan success means at least one symbol was recovered. Inspect `CompletionReason` and `IsPartial` separately. The total deadline covers reading, codec work and recognition. A shorter family allowance may stop one recognizer while later families continue; `RecognitionBudgetExceeded` reports that incomplete recognition without claiming that the total deadline elapsed.

`MaxSymbols` limits returned observations. With `Deduplicate = true`, equivalent format-and-payload results are collapsed. With it disabled, retries can observe the same physical symbol more than once. These APIs do not provide an exhaustive physical-instance count.

## Proof for an extension

Exercise the public entry point and an encoded-result route, rather than only the internal engine. Use exact payload roundtrips, independently specified format facts, meaningful invalid-input constraints and owned-result checks. Appearance extensions also need a rendered artifact inspection and representative protected geometry. Test each supported target framework and the normal package boundary for affected consumers.

Check new symbol layouts with an independent reader when one supports the format. Preserve independently specified module or codeword vectors in regression tests: an encoder and decoder can share a coordinate or compaction mistake that their own roundtrip does not expose.

Image recognition also needs proof through physical placement and rasterization. Document sizes can produce fractional module spacing even when the encoded grid is exact. Check the saved document's rendered image at representative sizes and include integer-spacing controls; a module-only roundtrip cannot establish that image sampling preserves the same payload and metadata.

Keep additions within their actual geometry and capability. Adding a built-in format does not require a public runtime plugin interface or a new common options framework. A new dependency, geometry model or incompatible contract needs its own explicit design and compatibility assessment.
