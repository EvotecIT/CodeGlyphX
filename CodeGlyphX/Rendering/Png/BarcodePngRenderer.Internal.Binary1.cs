using System;
using System.Buffers;
using System.IO;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Png;

public static partial class BarcodePngRenderer {
    private enum LabelDrawKind {
        None,
        Foreground
    }

    private static bool TryRenderGray1(Barcode1D barcode, BarcodePngRenderOptions opts, out byte[] png) {
        png = Array.Empty<byte>();
        if (!CanRenderAsGray1(opts)) return false;
        EnsureBasicInputs(barcode, opts);

        var labelText = BarcodeLabelText.Normalize(opts.LabelText);
        var hasLabel = !string.IsNullOrEmpty(labelText);
        var labelKind = GetLabelKind(opts, hasLabel);
        if (labelKind == LabelDrawKind.None && hasLabel && !IsLabelInvisible(opts)) return false;
        var barHeightPx = opts.HeightModules * opts.ModuleSize;
        var widthPx = (barcode.TotalModules + opts.QuietZone * 2) * opts.ModuleSize;
        var labelLayout = hasLabel ? BuildLabelLayout(opts, labelText, widthPx, barHeightPx) : default;
        var heightPx = barHeightPx + labelLayout.HeightPx;
        var rowBytes = (widthPx + 7) / 8;
        var length = heightPx * (rowBytes + 1);

        var invert = IsBlack(opts.Foreground) && IsWhite(opts.Background);
        var scanlines = ArrayPool<byte>.Shared.Rent(length);
        try {
            RenderBinaryScanlines(barcode, opts, scanlines, length, widthPx, heightPx, barHeightPx, invert, labelText, labelLayout, labelKind);
            png = PngWriter.WriteGray1(widthPx, heightPx, scanlines, length);
            return true;
        } finally {
            ArrayPool<byte>.Shared.Return(scanlines);
        }
    }

    private static bool TryRenderGray1ToStream(Barcode1D barcode, BarcodePngRenderOptions opts, Stream stream) {
        if (!CanRenderAsGray1(opts)) return false;
        EnsureBasicInputs(barcode, opts);

        var labelText = BarcodeLabelText.Normalize(opts.LabelText);
        var hasLabel = !string.IsNullOrEmpty(labelText);
        var labelKind = GetLabelKind(opts, hasLabel);
        if (labelKind == LabelDrawKind.None && hasLabel && !IsLabelInvisible(opts)) return false;
        var barHeightPx = opts.HeightModules * opts.ModuleSize;
        var widthPx = (barcode.TotalModules + opts.QuietZone * 2) * opts.ModuleSize;
        var labelLayout = hasLabel ? BuildLabelLayout(opts, labelText, widthPx, barHeightPx) : default;
        var heightPx = barHeightPx + labelLayout.HeightPx;
        var rowBytes = (widthPx + 7) / 8;
        var length = heightPx * (rowBytes + 1);

        var invert = IsBlack(opts.Foreground) && IsWhite(opts.Background);
        var scanlines = ArrayPool<byte>.Shared.Rent(length);
        try {
            RenderBinaryScanlines(barcode, opts, scanlines, length, widthPx, heightPx, barHeightPx, invert, labelText, labelLayout, labelKind);
            PngWriter.WriteGray1(stream, widthPx, heightPx, scanlines, length);
            return true;
        } finally {
            ArrayPool<byte>.Shared.Return(scanlines);
        }
    }

    private static bool TryRenderIndexed1(Barcode1D barcode, BarcodePngRenderOptions opts, out byte[] png) {
        png = Array.Empty<byte>();
        if (!CanRenderAsIndexed1(opts)) return false;
        EnsureBasicInputs(barcode, opts);

        var labelText = BarcodeLabelText.Normalize(opts.LabelText);
        var hasLabel = !string.IsNullOrEmpty(labelText);
        var labelKind = GetLabelKind(opts, hasLabel);
        if (labelKind == LabelDrawKind.None && hasLabel && !IsLabelInvisible(opts)) return false;
        var barHeightPx = opts.HeightModules * opts.ModuleSize;
        var widthPx = (barcode.TotalModules + opts.QuietZone * 2) * opts.ModuleSize;
        var labelLayout = hasLabel ? BuildLabelLayout(opts, labelText, widthPx, barHeightPx) : default;
        var heightPx = barHeightPx + labelLayout.HeightPx;
        var rowBytes = (widthPx + 7) / 8;
        var length = heightPx * (rowBytes + 1);

        var palette = BuildPalette(opts.Background, opts.Foreground);
        var scanlines = ArrayPool<byte>.Shared.Rent(length);
        try {
            RenderBinaryScanlines(barcode, opts, scanlines, length, widthPx, heightPx, barHeightPx, invert: false, labelText, labelLayout, labelKind);
            png = PngWriter.WriteIndexed1(widthPx, heightPx, scanlines, length, palette);
            return true;
        } finally {
            ArrayPool<byte>.Shared.Return(scanlines);
        }
    }

    private static bool TryRenderIndexed1ToStream(Barcode1D barcode, BarcodePngRenderOptions opts, Stream stream) {
        if (!CanRenderAsIndexed1(opts)) return false;
        EnsureBasicInputs(barcode, opts);

        var labelText = BarcodeLabelText.Normalize(opts.LabelText);
        var hasLabel = !string.IsNullOrEmpty(labelText);
        var labelKind = GetLabelKind(opts, hasLabel);
        if (labelKind == LabelDrawKind.None && hasLabel && !IsLabelInvisible(opts)) return false;
        var barHeightPx = opts.HeightModules * opts.ModuleSize;
        var widthPx = (barcode.TotalModules + opts.QuietZone * 2) * opts.ModuleSize;
        var labelLayout = hasLabel ? BuildLabelLayout(opts, labelText, widthPx, barHeightPx) : default;
        var heightPx = barHeightPx + labelLayout.HeightPx;
        var rowBytes = (widthPx + 7) / 8;
        var length = heightPx * (rowBytes + 1);

        var palette = BuildPalette(opts.Background, opts.Foreground);
        var scanlines = ArrayPool<byte>.Shared.Rent(length);
        try {
            RenderBinaryScanlines(barcode, opts, scanlines, length, widthPx, heightPx, barHeightPx, invert: false, labelText, labelLayout, labelKind);
            PngWriter.WriteIndexed1(stream, widthPx, heightPx, scanlines, length, palette);
            return true;
        } finally {
            ArrayPool<byte>.Shared.Return(scanlines);
        }
    }

    private readonly struct LabelLayout {
        public int Scale { get; }
        public int Spacing { get; }
        public int XStart { get; }
        public int YStart { get; }
        public int HeightPx { get; }

        public LabelLayout(int scale, int spacing, int xStart, int yStart, int heightPx) {
            Scale = scale;
            Spacing = spacing;
            XStart = xStart;
            YStart = yStart;
            HeightPx = heightPx;
        }
    }

    private static void EnsureBasicInputs(Barcode1D barcode, BarcodePngRenderOptions opts) {
        if (barcode is null) throw new ArgumentNullException(nameof(barcode));
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        if (opts.ModuleSize <= 0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleSize));
        if (opts.QuietZone < 0) throw new ArgumentOutOfRangeException(nameof(opts.QuietZone));
        if (opts.HeightModules <= 0) throw new ArgumentOutOfRangeException(nameof(opts.HeightModules));
    }

    private static void RenderBinaryScanlines(Barcode1D barcode, BarcodePngRenderOptions opts, byte[] scanlines, int length, int widthPx, int heightPx, int barHeightPx, bool invert, string? labelText, LabelLayout labelLayout, LabelDrawKind labelKind) {
        if (scanlines is null) throw new ArgumentNullException(nameof(scanlines));
        if (length < 0 || length > scanlines.Length) throw new ArgumentOutOfRangeException(nameof(length));

        var moduleSize = opts.ModuleSize;
        var quiet = opts.QuietZone;
        var rowBytes = (widthPx + 7) / 8;
        var rowStride = rowBytes + 1;
        var fill = invert ? (byte)0xFF : (byte)0x00;

        var offset = 0;
        for (var y = 0; y < heightPx; y++) {
            scanlines[offset++] = 0;
            for (var i = 0; i < rowBytes; i++) {
                scanlines[offset + i] = fill;
            }
            offset += rowBytes;
        }

        var xModules = quiet;
        for (var i = 0; i < barcode.Segments.Count; i++) {
            var seg = barcode.Segments[i];
            if (!seg.IsBar) {
                xModules += seg.Modules;
                continue;
            }

            var x0 = xModules * moduleSize;
            var x1 = (xModules + seg.Modules) * moduleSize;
            for (var y = 0; y < barHeightPx; y++) {
                var rowStart = y * rowStride + 1;
                ApplyRun(scanlines, rowStart, x0, x1, invert);
            }
            xModules += seg.Modules;
        }

        if (labelKind == LabelDrawKind.Foreground && !string.IsNullOrEmpty(labelText)) {
            DrawLabelBinary(scanlines, widthPx, heightPx, rowStride, labelLayout.XStart, labelLayout.YStart, labelText!, labelLayout.Scale, labelLayout.Spacing, invert);
        }
    }

    private static void ApplyRun(byte[] scanlines, int rowStart, int x0, int x1, bool invert) {
        if (x1 <= x0) return;

        var startByte = x0 >> 3;
        var endByte = (x1 - 1) >> 3;
        var startBit = x0 & 7;
        var endBit = (x1 - 1) & 7;

        if (startByte == endByte) {
            var mask = (byte)((0xFF >> startBit) & (0xFF << (7 - endBit)));
            if (invert) {
                scanlines[rowStart + startByte] &= (byte)~mask;
            } else {
                scanlines[rowStart + startByte] |= mask;
            }
            return;
        }

        var startMask = (byte)(0xFF >> startBit);
        var endMask = (byte)(0xFF << (7 - endBit));
        if (invert) {
            scanlines[rowStart + startByte] &= (byte)~startMask;
            scanlines[rowStart + endByte] &= (byte)~endMask;
        } else {
            scanlines[rowStart + startByte] |= startMask;
            scanlines[rowStart + endByte] |= endMask;
        }

        var fill = invert ? (byte)0x00 : (byte)0xFF;
        for (var i = startByte + 1; i < endByte; i++) {
            scanlines[rowStart + i] = fill;
        }
    }

    private static void DrawLabelBinary(byte[] scanlines, int widthPx, int heightPx, int rowStride, int xStart, int yStart, string text, int scale, int spacing, bool invert) {
        var x = xStart;
        for (var i = 0; i < text.Length; i++) {
            var glyph = BarcodeLabelFont.GetGlyph(text[i]);
            for (var row = 0; row < BarcodeLabelFont.GlyphHeight; row++) {
                var bits = glyph[row];
                for (var col = 0; col < BarcodeLabelFont.GlyphWidth; col++) {
                    if ((bits & (1 << (BarcodeLabelFont.GlyphWidth - 1 - col))) == 0) continue;
                    var px = x + col * scale;
                    var py = yStart + row * scale;
                    for (var sy = 0; sy < scale; sy++) {
                        var y = py + sy;
                        if ((uint)y >= (uint)heightPx) continue;
                        var rowStart = y * rowStride + 1;
                        for (var sx = 0; sx < scale; sx++) {
                            var px2 = px + sx;
                            if ((uint)px2 >= (uint)widthPx) continue;
                            var byteIndex = rowStart + (px2 >> 3);
                            var mask = (byte)(1 << (7 - (px2 & 7)));
                            if (invert) {
                                scanlines[byteIndex] &= (byte)~mask;
                            } else {
                                scanlines[byteIndex] |= mask;
                            }
                        }
                    }
                }
            }
            x += BarcodeLabelFont.GlyphWidth * scale + spacing;
        }
    }

    private static bool CanRenderAsGray1(BarcodePngRenderOptions opts) {
        if (!IsOpaque(opts.Foreground) || !IsOpaque(opts.Background)) return false;
        return (IsBlack(opts.Foreground) && IsWhite(opts.Background))
            || (IsWhite(opts.Foreground) && IsBlack(opts.Background));
    }

    private static bool CanRenderAsIndexed1(BarcodePngRenderOptions opts) {
        if (!IsOpaque(opts.Foreground) || !IsOpaque(opts.Background)) return false;
        return true;
    }

    private static bool IsOpaque(Rgba32 color) => color.A == 255;

    private static bool IsBlack(Rgba32 color) => color.R == 0 && color.G == 0 && color.B == 0;

    private static bool IsWhite(Rgba32 color) => color.R == 255 && color.G == 255 && color.B == 255;

    private static byte[] BuildPalette(Rgba32 background, Rgba32 foreground) {
        return new[] {
            background.R, background.G, background.B,
            foreground.R, foreground.G, foreground.B
        };
    }

    private static bool IsLabelInvisible(BarcodePngRenderOptions opts) {
        return opts.LabelColor.R == opts.Background.R
            && opts.LabelColor.G == opts.Background.G
            && opts.LabelColor.B == opts.Background.B
            && opts.LabelColor.A == opts.Background.A;
    }

    private static LabelDrawKind GetLabelKind(BarcodePngRenderOptions opts, bool hasLabel) {
        if (!hasLabel) return LabelDrawKind.None;
        if (!IsOpaque(opts.LabelColor)) return LabelDrawKind.None;
        if (opts.LabelColor.R == opts.Foreground.R
            && opts.LabelColor.G == opts.Foreground.G
            && opts.LabelColor.B == opts.Foreground.B
            && opts.LabelColor.A == opts.Foreground.A) {
            return LabelDrawKind.Foreground;
        }
        if (opts.LabelColor.R == opts.Background.R
            && opts.LabelColor.G == opts.Background.G
            && opts.LabelColor.B == opts.Background.B
            && opts.LabelColor.A == opts.Background.A) {
            return LabelDrawKind.None;
        }
        return LabelDrawKind.None;
    }

    private static LabelLayout BuildLabelLayout(BarcodePngRenderOptions opts, string labelText, int widthPx, int barHeightPx) {
        var labelFontPx = Math.Max(1, opts.LabelFontSize);
        var labelMarginPx = Math.Max(0, opts.LabelMargin);
        var labelScale = Math.Max(1, (int)Math.Round(labelFontPx / (double)BarcodeLabelFont.GlyphHeight));
        var spacing = labelScale;
        var labelHeightPx = labelMarginPx + BarcodeLabelFont.GlyphHeight * labelScale;
        var textWidth = BarcodeLabelFont.MeasureTextWidth(labelText, labelScale, spacing);
        var xStart = 0;
        if (textWidth > 0) {
            xStart = (widthPx - textWidth) / 2;
            if (xStart < 0) xStart = 0;
        }
        return new LabelLayout(labelScale, spacing, xStart, barHeightPx + labelMarginPx, labelHeightPx);
    }
}
