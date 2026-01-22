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

        var offset = 0;
        for (var y = 0; y < heightPx; y++) {
            scanlines[offset++] = 0;
            var moduleY = y / moduleSize - quiet;
            var inQrY = moduleY >= 0 && moduleY < size;
            var bitPos = 7;
            var current = (byte)0;

            for (var x = 0; x < widthPx; x++) {
                var moduleX = x / moduleSize - quiet;
                var isDark = inQrY && moduleX >= 0 && moduleX < size && modules[moduleX, moduleY];
                var bit = invert ? !isDark : isDark;
                if (bit) current |= (byte)(1 << bitPos);
                bitPos--;
                if (bitPos < 0) {
                    scanlines[offset++] = current;
                    current = 0;
                    bitPos = 7;
                }
            }

            if (bitPos != 7) {
                if (invert) {
                    current |= (byte)((1 << (bitPos + 1)) - 1);
                }
                scanlines[offset++] = current;
            }
        }
    }

    private static bool CanRenderAsBinary(QrPngRenderOptions opts) {
        if (opts.ForegroundGradient is not null) return false;
        if (opts.Eyes is not null) return false;
        if (opts.Logo is not null) return false;
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
