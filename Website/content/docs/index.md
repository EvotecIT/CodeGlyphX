---
title: Documentation - CodeGlyphX
description: CodeGlyphX documentation - learn how to generate and decode QR codes, barcodes, and 2D matrix codes.
slug: index
collection: docs
layout: docshome
---

# CodeGlyphX Documentation

## About CodeGlyphX

CodeGlyphX is a pure-managed .NET library for generating and decoding 65 physical QR, matrix, stacked, linear, colour and postal formats. It also supplies application profiles, portable text fonts and measured print layouts without native graphics libraries.

- **No native graphics dependencies** - No System.Drawing, SkiaSharp, or ImageSharp required
- **Encode and decode** - Supported symbologies have explicit APIs and documented limits
- **Multiple output formats** - PNG, SVG, PDF, EPS, HTML, and many more
- **Cross-platform** - Windows, Linux, macOS
- **AOT validated** - CI publishes and executes a `net8.0` NativeAOT consumer

The [capability matrix](/docs/symbol-capabilities/) lists every physical format and separates encoding, module or colour-cell decoding, image scanning, default selection and multiple results. [Profiles and controls](/docs/barcode-profiles/) cover checked identifiers, GS1, HIBC, linked Composite and specialist options. These profiles use existing carriers rather than adding duplicate format counts.

For labels, start with [fonts](/docs/fonts/) and [physical printing](/docs/printing/): bundled OCR-A/OCR-B, supplied font files, millimetres and printer DPI, quiet zones, readable text and matching PNG/SVG/PDF exports. Try these controls in the [playground's print studio](/playground/).

The [API reference](/api/) documents every public type. Upgrading? Read the [3.0 migration guide](/docs/migration-3/); the [2.0 guide](/docs/migration-2/) is kept for earlier upgrades.

## Performance and recognition

For measured generation speed and image recognition quality, read the [current comparisons](/benchmarks/). The summary below includes the remaining decoding gaps.

{{< comparison-summary >}}

## Getting Help

If you encounter issues or have questions, visit the
[GitHub Issues](https://github.com/EvotecIT/CodeGlyphX/issues) page or read the [FAQ](/faq/).

For planned work and known gaps, see the
[ROADMAP](https://github.com/EvotecIT/CodeGlyphX/blob/master/ROADMAP.md).
