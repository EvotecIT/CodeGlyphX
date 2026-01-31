# CodeGlyphX Security Sweep (2026-01-31)

## Scope
- CodeGlyphX core library (image decode/encode, QR/barcode parsing, renderers).
- Dependency check for `CodeGlyphX.csproj`.

## Method
- Manual review of decoding entry points, I/O helpers, renderers, and option surfaces.
- Quick dependency vulnerability scan via `dotnet list package --vulnerable`.

## Key Findings

### 1) Potential memory DoS in image decoders (PNG/GIF/others)
**Severity:** High (when decoding untrusted images)

- Several decoders allocate buffers based on header dimensions without a global cap.
- PNG (`Rendering/Png/PngDecoder.cs`) and GIF (`Rendering/Gif/GifReader.cs`) decode allocate `width * height` buffers before any downscale or budget enforcement.
- `ImageDecodeOptions.MaxDimension` only applies *after* decode (via `ImageDecodeHelper`), so it does not prevent large allocations.

**Impact:** Untrusted images with large dimensions can trigger very large allocations and/or OOM, causing a denial of service.

**Examples:**
- `Rendering/Png/PngDecoder.cs` allocates scanlines + raw buffers based on IHDR width/height with no max pixel check.
- `Rendering/Gif/GifReader.cs` allocates `width * height * 4` buffers based on header dimensions without a cap.

**Recommendations:**
- Introduce a configurable max pixel limit (or max dimension limit) enforced *before* decode, ideally via `ImageDecodeOptions` and `ImageReader.TryReadInfo`.
- Consider a global default limit for all image decoders, with opt-out for trusted inputs.
- If retaining `MaxDimension` behavior (downscale after decode), document that it does not protect against oversized inputs.

### 2) HTML/SVG renderer injection risks via unescaped style/attribute values
**Severity:** Medium (when render options come from untrusted sources)

- HTML and SVG renderers embed color and font values directly into attributes and style blocks without escaping.
- If these values are derived from untrusted input, this can allow HTML/SVG injection (e.g., breaking out of attributes).

**Examples:**
- `Rendering/Html/HtmlBarcodeRenderer.cs` inserts `LabelFontFamily`, `LabelColor`, `BarColor`, `BackgroundColor` directly into HTML.
- `Rendering/Html/HtmlQrRenderer.cs` and `Rendering/Svg/*` insert color strings into attributes without escaping.

**Recommendations:**
- Add a strict validation/sanitization path (e.g., whitelist hex/rgb/rgba/known color names) or encode/escape attribute values.
- Document that render option strings must be trusted or sanitized by callers.
- Optional: provide safe overloads that accept strongly typed colors (`Rgba32`) and map to CSS safely.

### 3) Unbounded reads for streams
**Severity:** Medium

- `RenderIO.ReadBinary(Stream)` reads the entire stream into memory with no size cap.
- Any API that accepts a `Stream` and calls `RenderIO.ReadBinary` can exhaust memory on large streams.

**Recommendations:**
- Add `ReadBinary(Stream, int maxBytes)` overloads with a hard cap.
- For decode entry points, optionally enforce max input size as part of decode options.

### 4) Path handling convenience APIs can be misused
**Severity:** Low (depends on caller usage)

- Methods like `RenderIO.WriteBinary(directory, fileName, ...)` accept arbitrary `fileName` and combine with `Path.Combine` without validation.
- If `fileName` is untrusted, it can include path traversal (`..`) or rooted paths to escape the directory.

**Recommendations:**
- Consider optional filename validation (reject path separators or rooted paths) for "safe" helper overloads.
- Document that these overloads expect trusted file names.

## Dependency Scan
- `dotnet list CodeGlyphX/CodeGlyphX.csproj package --vulnerable` reports **no known vulnerable packages** from `https://api.nuget.org/v3/index.json` as of 2026-01-31.

## Suggested Hardening Roadmap
- **Short term (low risk):**
  - Add pre-decode size limits (max pixels) using `ImageReader.TryReadInfo`.
  - Document that `MaxDimension` downscales post-decode, not pre-decode.
  - Add safe helper overloads for stream reads with byte caps.

- **Medium term:**
  - Sanitize HTML/SVG render options or provide strongly typed color APIs.
  - Add fuzzing tests for image decoders (PNG/GIF/TIFF/WebP/JPEG) to catch parser edge cases.

## Open Questions
- Do you want strict defaults (limits enabled by default) or opt-in limits for callers?
- Are any HTML/SVG renderers used with untrusted inputs in production?
- Should we add a new security policy file (SECURITY.md) for disclosures and supported versions?
