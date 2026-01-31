# CodeGlyphX Security Sweep (2026-01-31)

## Scope
- CodeGlyphX core library (image decode/encode, QR/barcode parsing, renderers).
- Dependency check for `CodeGlyphX.csproj`.

## Method
- Manual review of decoding entry points, I/O helpers, renderers, and option surfaces.
- Quick dependency vulnerability scan via `dotnet list package --vulnerable`.

## Mitigations Applied (this branch)
- Enforced image size limits (max pixels + max bytes) before decode for ImageReader-based paths.
- Added PNG pre-decode limits for Aztec/DataMatrix/PDF417 `TryDecodePng(...)` entry points.
- Added size-capped stream reads in decode entry points (sync + async).
- Added decoder guardrails (checked arithmetic + size caps) in PNG/GIF/TIFF/WebP decoders.
- Added payload caps + size guards for BMP/JPEG/PSD/TGA/NetPBM/XBM/XPM/PDF decoders.
- Added PNG chunk length validation for PLTE/tRNS + capped total IDAT payload size.
- Added animation safety limits (max frames, max duration, max frame pixels) for GIF/WebP.
- Added per-call animation limit overrides via `ImageDecodeOptions` for ImageReader animation paths.
- Added safe `ImageDecodeOptions` preset for untrusted inputs and tightened default decode caps.
- Added output-size guards for encoders/writers (PNG/BMP/JPEG/GIF/TIFF/TGA/WebP/NetPBM/PDF/EPS/XBM/XPM).
- Sanitized HTML/SVG colors + font family and HTML document title.
- Added consistent “Invalid input” failure messages when size limits reject input.
- Added safe filename overloads for RenderIO write helpers.
- Added `SECURITY.md` and a fuzzing harness (`CodeGlyphX.Fuzz`) with usage notes.
- Added CodeQL workflow for static analysis in CI.

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

**Status:** Mitigated for ImageReader-based decode paths and PNG-specific barcode entry points with pre-decode limits.  
**Status update:** Guardrails now apply to BMP/JPEG/PSD/TGA/NetPBM/XBM/XPM/PDF decoders and animation frames (GIF/WebP).
**Remaining gap:** Any new format-specific entry points should apply the same pre-decode limits.

### 2) HTML/SVG renderer injection risks via unescaped style/attribute values
**Severity:** Medium (when render options come from untrusted sources)

- HTML and SVG renderers embed color and font values directly into attributes and style blocks without escaping.
- If these values are derived from untrusted input, this can allow HTML/SVG injection (e.g., breaking out of attributes).

**Examples:**
- `Rendering/Html/HtmlBarcodeRenderer.cs` inserts `LabelFontFamily`, `LabelColor`, `BarColor`, `BackgroundColor` directly into HTML.
- `Rendering/Html/HtmlQrRenderer.cs` and `Rendering/Svg/*` insert color strings into attributes without escaping.

**Status:** Sanitization added for colors and font family; HTML title is now encoded when wrapping output.  
**Remaining gap:** If future options add raw CSS/class/style strings, they should be sanitized or explicitly documented as trusted-only.

### 3) Unbounded reads for streams
**Severity:** Medium

- `RenderIO.ReadBinary(Stream)` reads the entire stream into memory with no size cap.
- Any API that accepts a `Stream` and calls `RenderIO.ReadBinary` can exhaust memory on large streams.

**Status:** `ReadBinary` now supports maxBytes and decode entry points enforce caps (sync + async).

### 4) Path handling convenience APIs can be misused
**Severity:** Low (depends on caller usage)

- Methods like `RenderIO.WriteBinary(directory, fileName, ...)` accept arbitrary `fileName` and combine with `Path.Combine` without validation.
- If `fileName` is untrusted, it can include path traversal (`..`) or rooted paths to escape the directory.

**Status:** Mitigated via new safe filename overloads; existing helpers remain unsafe if given untrusted names.  
**Recommendations:**
- Prefer safe overloads when file names come from untrusted sources.
- Document that the existing helpers expect trusted file names.

## Dependency Scan
- `dotnet list CodeGlyphX/CodeGlyphX.csproj package --vulnerable` reports **no known vulnerable packages** from `https://api.nuget.org/v3/index.json` as of 2026-01-31.

## Suggested Hardening Roadmap
- **Short term (low risk):**
  - Document that `MaxDimension` downscales post-decode, not pre-decode.
  - Document animation limits (`ImageReader.MaxAnimationFrames/MaxAnimationDurationMs/MaxAnimationFramePixels`).

- **Medium term:**
  - Sanitize HTML/SVG render options or provide strongly typed color APIs.
  - Add fuzzing tests for image decoders (PNG/GIF/TIFF/WebP/JPEG) to catch parser edge cases.
  - Consider enforcing output-size limits for any new encoders/writers when width/height are caller-provided.

## Remaining Gaps / Follow-ups
- Consider pre-decode max-pixel checks for any other format-specific decode methods that bypass `ImageReader`.
- Hook the fuzzing harness into CI (or a scheduled job) with corpus inputs and time limits.
- Consider a lightweight fuzzing pipeline for decoder inputs.

## Open Questions
- Do you want strict defaults (limits enabled by default) or opt-in limits for callers?
- Are any HTML/SVG renderers used with untrusted inputs in production?
- Should we add a new security policy file (SECURITY.md) for disclosures and supported versions?
