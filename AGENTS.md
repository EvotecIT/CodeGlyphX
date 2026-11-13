# CodeGlyphX

Pure-managed .NET toolkit for QR codes, linear barcodes, Data Matrix, PDF417,
Aztec and other symbologies, QR payloads, and image rendering and decoding.
No System.Drawing, SkiaSharp, ImageSharp or native codec library.

## Layout

- `CodeGlyphX/`: the library (encoders, decoders, payloads, rendering, image codecs).
- `CodeGlyphX.Tests/`: xUnit tests. `Fixtures/` holds committed and downloaded corpora.
- `CodeGlyphX.Examples/`: runnable examples, also the Native AOT smoke.
- `CodeGlyphX.Fuzz/`: fuzz harness (see `FUZZING.md`). `CodeGlyphX.Benchmarks/`
  (see `BENCHMARK.md`).
- `CodeGlyphX.Wpf/`, `CodeGlyphX.Demo.Wpf/`, `CodeGlyphX.ScreenScan.Wpf/`:
  Windows-only WPF control and demos.
- `CodeGlyphX.Playground/`, `CodeGlyphX.Website/`: Blazor playground for the site.
  `CodeGlyphX.CatalogGenerator/`: catalog data for the website pipeline.
- `Build/`: packaging, package checks, sample downloads, fixture and table
  generators, benchmark and comparison scripts. `Build/PackageConsumer/`:
  package-consumer check.
- `Website/`: codeglyphx.com (PowerForge.Web). It has its own
  `Website/AGENTS.md`; see also `WEBSITE.md`.
- `docs/`: design and qualification notes.

## Build and test

```powershell
dotnet build CodeGlyphX.sln -c Release
./Build/Download-ImageSamples.ps1 -NoUpdateManifest        # hash-pinned test corpora, once
./Build/Download-ExternalSamples.ps1 -NoUpdateManifest
dotnet test CodeGlyphX.Tests/CodeGlyphX.Tests.csproj -c Release -f net8.0
dotnet test CodeGlyphX.Tests/CodeGlyphX.Tests.csproj -c Release -f net10.0
dotnet test CodeGlyphX.Tests/CodeGlyphX.Tests.csproj -c Release -f net472   # Windows only
dotnet pack CodeGlyphX/CodeGlyphX.csproj -c Release -o artifacts/packages
./Build/Assert-Package.ps1 -PackageDirectory artifacts/packages
dotnet publish CodeGlyphX.Examples/CodeGlyphX.Examples.csproj -c Release -f net8.0 -r linux-x64 -p:PublishAot=true --self-contained true -o artifacts/aot-smoke
```

Targets: `netstandard2.0`, `net472`, `net8.0` and `net10.0`. `net8.0`/`net10.0`
get the full QR pixel pipeline, trim/AOT analyzers and no runtime package
dependency. `netstandard2.0`/`net472` depend on `System.Memory` and have a
less capable QR image fallback.

Release-only: `Build/Build-Project.ps1` (via `Build/project.build.json`).
Before a release, the full solution (including fuzz and examples) builds,
`net8.0`, `net10.0` and Windows `net472` tests pass, `.nupkg`/`.snupkg`
contents cover all four targets, a NativeAOT consumer runs, and the website
API docs are built from current XML output.

## Generated files

- `CodeGlyphX/**/*.Generated.cs` (GS1 catalog and code lists, Code 49 and
  Ultracode tables) come from `Build/Generate-Gs1Catalog.ps1`,
  `Build/Generate-Code49Tables.py` and `Build/Generate-UltracodeTables.py`.
  Regenerate instead of editing.
- Conformance fixtures come from the `Build/Generate-*Fixtures.*` scripts,
  some using external comparison tools (Zint, BWIPP). Those tools are never
  product or build dependencies.
- `CodeGlyphX/CompatibilitySuppressions.xml`: refresh with
  `Build/Update-ApiCompatibility.ps1` only after reviewing an intended API break.
- `WebsiteArtifacts/project-manifest.json`: `Build/Export-ProjectManifest.ps1`.

## Conventions

- K&R braces (`csharp_new_line_before_open_brace = none`); follow `.editorconfig`.
- Public API shape: files go through extension-based `Save`
  (`QR.Save(...)`, `Barcode.Save(SymbolFormat.Code128, ...)`); memory and
  streams use `Render(..., OutputFormat.X)` and `OutputWriter.Write`. Builders
  end with `Render(format)`, `Save(path)` or `Save(stream, format)`. Don't add
  per-format facade methods (`RenderPng`, `ToPng`, `.Png()`, `SaveSvg`); they
  were removed. Migration guides live in `Website/content/docs/migration-*.md`.
- Decoding: `QrImageDecoder` for QR-only input, `SymbolScanner` for unified
  recognition. For limits, `null` inherits the `ImageReader` global, `0`
  disables the per-call limit and positive values are explicit.
  `MaxDimension` resizes after validation and is not a memory limit. Budgets
  are cooperative, not hard deadlines.
- Never describe every symbology or codec path as equally robust; use the
  documented format matrix and known limits. `QR.EvaluateScanHeuristics` is a
  static check, not proof that output scans.
- Unsupported input (for example WebP animation interframes) fails rather than
  returning fabricated pixels.

## Gotchas

- Sample-corpus tests need the downloaded corpora under
  `CodeGlyphX.Tests/Fixtures/ImageSamples` and `ExternalSamples` (ignored by
  git), or `CODEGLYPHX_IMAGE_SAMPLES` / `CODEGLYPHX_EXTERNAL_SAMPLES` pointing elsewhere.
- Slow runners: `CODEGLYPHX_TEST_BUDGET_MULTIPLIER=<n>` scales time budgets.
- `net472` tests and the WPF projects build and run only on Windows.
- Fuzzing uses `CODEGLYPHX_FUZZ_TIMEOUT_MS`, `CODEGLYPHX_FUZZ_MAX_MB` and
  `CODEGLYPHX_FUZZ_MAX_INPUT_MB`; `Build/Invoke-FuzzReplay.ps1` replays a corpus.
