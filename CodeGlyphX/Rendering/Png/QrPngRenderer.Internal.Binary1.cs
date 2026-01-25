using System;
using System.Buffers;
using System.IO;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Png;

public static partial class QrPngRenderer {
    private static bool TryRenderGray1(BitMatrix modules, QrPngRenderOptions opts, out byte[] png) {
        png = Array.Empty<byte>();
        if (!CanRenderAsGray1(opts)) return false;
        EnsureBasicInputs(modules, opts);

        var size = modules.Width;
        var outModules = size + opts.QuietZone * 2;
        var widthPx = outModules * opts.ModuleSize;
        var heightPx = widthPx;
        var rowBytes = (widthPx + 7) / 8;
        var length = heightPx * (rowBytes + 1);

        var invert = IsBlack(opts.Foreground) && IsWhite(opts.Background);
        var scanlines = ArrayPool<byte>.Shared.Rent(length);
        try {
            RenderBinaryScanlines(modules, opts, scanlines, length, widthPx, heightPx, invert);
            png = PngWriter.WriteGray1(widthPx, heightPx, scanlines, length);
            return true;
        } finally {
            ArrayPool<byte>.Shared.Return(scanlines);
        }
    }

    private static bool TryRenderGray1ToStream(BitMatrix modules, QrPngRenderOptions opts, Stream stream) {
        if (!CanRenderAsGray1(opts)) return false;
        EnsureBasicInputs(modules, opts);

        var size = modules.Width;
        var outModules = size + opts.QuietZone * 2;
        var widthPx = outModules * opts.ModuleSize;
        var heightPx = widthPx;
        var rowBytes = (widthPx + 7) / 8;
        var length = heightPx * (rowBytes + 1);

        var invert = IsBlack(opts.Foreground) && IsWhite(opts.Background);
        var scanlines = ArrayPool<byte>.Shared.Rent(length);
        try {
            RenderBinaryScanlines(modules, opts, scanlines, length, widthPx, heightPx, invert);
            PngWriter.WriteGray1(stream, widthPx, heightPx, scanlines, length);
            return true;
        } finally {
            ArrayPool<byte>.Shared.Return(scanlines);
        }
    }

    private static bool TryRenderIndexed1(BitMatrix modules, QrPngRenderOptions opts, out byte[] png) {
        png = Array.Empty<byte>();
        if (!CanRenderAsIndexed1(opts)) return false;
        EnsureBasicInputs(modules, opts);

        var size = modules.Width;
        var outModules = size + opts.QuietZone * 2;
        var widthPx = outModules * opts.ModuleSize;
        var heightPx = widthPx;
        var rowBytes = (widthPx + 7) / 8;
        var length = heightPx * (rowBytes + 1);

        var palette = BuildPalette(opts.Background, opts.Foreground);
        var scanlines = ArrayPool<byte>.Shared.Rent(length);
        try {
            RenderBinaryScanlines(modules, opts, scanlines, length, widthPx, heightPx, invert: false);
            png = PngWriter.WriteIndexed1(widthPx, heightPx, scanlines, length, palette);
            return true;
        } finally {
            ArrayPool<byte>.Shared.Return(scanlines);
        }
    }

    private static bool TryRenderIndexed1ToStream(BitMatrix modules, QrPngRenderOptions opts, Stream stream) {
        if (!CanRenderAsIndexed1(opts)) return false;
        EnsureBasicInputs(modules, opts);

        var size = modules.Width;
        var outModules = size + opts.QuietZone * 2;
        var widthPx = outModules * opts.ModuleSize;
        var heightPx = widthPx;
        var rowBytes = (widthPx + 7) / 8;
        var length = heightPx * (rowBytes + 1);

        var palette = BuildPalette(opts.Background, opts.Foreground);
        var scanlines = ArrayPool<byte>.Shared.Rent(length);
        try {
            RenderBinaryScanlines(modules, opts, scanlines, length, widthPx, heightPx, invert: false);
            PngWriter.WriteIndexed1(stream, widthPx, heightPx, scanlines, length, palette);
            return true;
        } finally {
            ArrayPool<byte>.Shared.Return(scanlines);
        }
    }

    private static void EnsureBasicInputs(BitMatrix modules, QrPngRenderOptions opts) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        if (modules.Width != modules.Height) throw new ArgumentException("Matrix must be square.", nameof(modules));
        if (opts.ModuleSize <= 0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleSize));
        if (opts.QuietZone < 0) throw new ArgumentOutOfRangeException(nameof(opts.QuietZone));
        if (opts.ModuleScale is <= 0 or > 1.0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleScale));
    }

    private static void RenderBinaryScanlines(BitMatrix modules, QrPngRenderOptions opts, byte[] scanlines, int length, int widthPx, int heightPx, bool invert) {
        if (scanlines is null) throw new ArgumentNullException(nameof(scanlines));
        if (length < 0 || length > scanlines.Length) throw new ArgumentOutOfRangeException(nameof(length));

        var size = modules.Width;
        var moduleSize = opts.ModuleSize;
        var quiet = opts.QuietZone;
        var rowBytes = (widthPx + 7) / 8;
        var rowStride = rowBytes + 1;
        var fillByte = invert ? (byte)0xFF : (byte)0x00;

        var rowBuffer = ArrayPool<byte>.Shared.Rent(rowBytes);
        try {
            var offset = 0;
            var y = 0;
            while (y < heightPx) {
                var moduleY = y / moduleSize - quiet;
                var rowSpan = rowBuffer.AsSpan(0, rowBytes);
                if (fillByte == 0) {
                    rowSpan.Clear();
                } else {
                    rowSpan.Fill(fillByte);
                }

                if (moduleY >= 0 && moduleY < size) {
                    var moduleX = 0;
                    while (moduleX < size) {
                        if (!modules[moduleX, moduleY]) {
                            moduleX++;
                            continue;
                        }

                        var runStart = moduleX;
                        moduleX++;
                        while (moduleX < size && modules[moduleX, moduleY]) moduleX++;

                        var x0 = (runStart + quiet) * moduleSize;
                        var x1 = (moduleX + quiet) * moduleSize;
                        ApplyRun(rowBuffer, 0, x0, x1, invert);
                    }
                }

                var repeat = Math.Min(moduleSize - (y % moduleSize), heightPx - y);
                for (var r = 0; r < repeat; r++) {
                    scanlines[offset] = 0;
                    Buffer.BlockCopy(rowBuffer, 0, scanlines, offset + 1, rowBytes);
                    offset += rowStride;
                }
                y += repeat;
            }
        } finally {
            ArrayPool<byte>.Shared.Return(rowBuffer);
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

    private static bool CanRenderAsBinary(QrPngRenderOptions opts) {
        if (opts.BackgroundGradient is not null) return false;
        if (opts.ForegroundGradient is not null) return false;
        if (opts.ForegroundPalette is not null) return false;
        if (opts.ForegroundPaletteZones is not null) return false;
        if (opts.ModuleScaleMap is not null) return false;
        if (opts.Eyes is not null) return false;
        if (opts.Logo is not null) return false;
        if (opts.Canvas is not null) return false;
        if (opts.ModuleShape != QrPngModuleShape.Square) return false;
        if (!IsUnitScale(opts.ModuleScale)) return false;
        if (opts.ModuleCornerRadiusPx != 0) return false;
        return true;
    }

    private static bool CanRenderAsGray1(QrPngRenderOptions opts) {
        if (!CanRenderAsBinary(opts)) return false;
        if (!IsOpaque(opts.Foreground) || !IsOpaque(opts.Background)) return false;
        return (IsBlack(opts.Foreground) && IsWhite(opts.Background))
            || (IsWhite(opts.Foreground) && IsBlack(opts.Background));
    }

    private static bool CanRenderAsIndexed1(QrPngRenderOptions opts) {
        if (!CanRenderAsBinary(opts)) return false;
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
}
