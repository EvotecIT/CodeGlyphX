using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Tiff;

/// <summary>
/// Renders matrix codes to a TIFF image (baseline, uncompressed).
/// </summary>
public static class MatrixTiffRenderer {
    /// <summary>
    /// Renders the module matrix to a TIFF byte array.
    /// </summary>
    public static byte[] Render(BitMatrix modules, MatrixPngRenderOptions opts) {
        var pixels = MatrixPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
        return TiffWriter.WriteRgba32(widthPx, heightPx, pixels, stride);
    }

    /// <summary>
    /// Renders the module matrix to a TIFF byte array with compression selection.
    /// </summary>
    public static byte[] Render(BitMatrix modules, MatrixPngRenderOptions opts, TiffCompressionMode compression) {
        var pixels = MatrixPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
        return TiffWriter.WriteRgba32(widthPx, heightPx, pixels, stride, compression);
    }

    /// <summary>
    /// Renders the module matrix to a bilevel (1-bit) TIFF byte array.
    /// </summary>
    public static byte[] RenderBilevel(
        BitMatrix modules,
        MatrixPngRenderOptions opts,
        int rowsPerStrip = 0,
        TiffCompression compression = TiffCompression.None,
        byte threshold = 128,
        ushort photometric = 1) {
        var pixels = MatrixPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
        using var ms = new MemoryStream();
        TiffWriter.WriteBilevelFromRgba(ms, widthPx, heightPx, pixels, stride, rowsPerStrip <= 0 ? heightPx : rowsPerStrip, compression, threshold, photometric);
        return ms.ToArray();
    }

    /// <summary>
    /// Renders the module matrix to a bilevel (1-bit) TIFF byte array using direct module packing.
    /// </summary>
    public static byte[] RenderBilevelFromModules(
        BitMatrix modules,
        MatrixPngRenderOptions opts,
        int rowsPerStrip = 0,
        TiffCompression compression = TiffCompression.None,
        ushort photometric = 1) {
        var packed = PackBilevelModules(modules, opts, photometric, out var widthPx, out var heightPx, out var packedStride);
        using var ms = new MemoryStream();
        TiffWriter.WriteBilevel(ms, widthPx, heightPx, packed, packedStride, rowsPerStrip <= 0 ? heightPx : rowsPerStrip, compression, photometric);
        return ms.ToArray();
    }

    /// <summary>
    /// Renders the module matrix to a bilevel (1-bit) tiled TIFF byte array.
    /// </summary>
    public static byte[] RenderBilevelTiled(
        BitMatrix modules,
        MatrixPngRenderOptions opts,
        int tileWidth,
        int tileHeight,
        TiffCompression compression = TiffCompression.None,
        byte threshold = 128,
        ushort photometric = 1) {
        var pixels = MatrixPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
        using var ms = new MemoryStream();
        TiffWriter.WriteBilevelTiledFromRgba(ms, widthPx, heightPx, pixels, stride, tileWidth, tileHeight, compression, threshold, photometric);
        return ms.ToArray();
    }

    /// <summary>
    /// Renders the module matrix to a bilevel (1-bit) tiled TIFF byte array using direct module packing.
    /// </summary>
    public static byte[] RenderBilevelTiledFromModules(
        BitMatrix modules,
        MatrixPngRenderOptions opts,
        int tileWidth,
        int tileHeight,
        TiffCompression compression = TiffCompression.None,
        ushort photometric = 1) {
        var packed = PackBilevelModules(modules, opts, photometric, out var widthPx, out var heightPx, out var packedStride);
        using var ms = new MemoryStream();
        TiffWriter.WriteBilevelTiled(ms, widthPx, heightPx, packed, packedStride, tileWidth, tileHeight, compression, photometric);
        return ms.ToArray();
    }

    /// <summary>
    /// Renders the module matrix to a TIFF stream.
    /// </summary>
    public static void RenderToStream(BitMatrix modules, MatrixPngRenderOptions opts, Stream stream) {
        var pixels = MatrixPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
        TiffWriter.WriteRgba32(stream, widthPx, heightPx, pixels, stride);
    }

    /// <summary>
    /// Renders the module matrix to a bilevel (1-bit) TIFF stream.
    /// </summary>
    public static void RenderBilevelToStream(
        BitMatrix modules,
        MatrixPngRenderOptions opts,
        Stream stream,
        int rowsPerStrip = 0,
        TiffCompression compression = TiffCompression.None,
        byte threshold = 128,
        ushort photometric = 1) {
        var pixels = MatrixPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
        TiffWriter.WriteBilevelFromRgba(stream, widthPx, heightPx, pixels, stride, rowsPerStrip <= 0 ? heightPx : rowsPerStrip, compression, threshold, photometric);
    }

    /// <summary>
    /// Renders the module matrix to a bilevel (1-bit) TIFF stream using direct module packing.
    /// </summary>
    public static void RenderBilevelFromModulesToStream(
        BitMatrix modules,
        MatrixPngRenderOptions opts,
        Stream stream,
        int rowsPerStrip = 0,
        TiffCompression compression = TiffCompression.None,
        ushort photometric = 1) {
        var packed = PackBilevelModules(modules, opts, photometric, out var widthPx, out var heightPx, out var packedStride);
        TiffWriter.WriteBilevel(stream, widthPx, heightPx, packed, packedStride, rowsPerStrip <= 0 ? heightPx : rowsPerStrip, compression, photometric);
    }

    /// <summary>
    /// Renders the module matrix to a bilevel (1-bit) tiled TIFF stream.
    /// </summary>
    public static void RenderBilevelTiledToStream(
        BitMatrix modules,
        MatrixPngRenderOptions opts,
        Stream stream,
        int tileWidth,
        int tileHeight,
        TiffCompression compression = TiffCompression.None,
        byte threshold = 128,
        ushort photometric = 1) {
        var pixels = MatrixPngRenderer.RenderPixels(modules, opts, out var widthPx, out var heightPx, out var stride);
        TiffWriter.WriteBilevelTiledFromRgba(stream, widthPx, heightPx, pixels, stride, tileWidth, tileHeight, compression, threshold, photometric);
    }

    /// <summary>
    /// Renders the module matrix to a bilevel (1-bit) tiled TIFF stream using direct module packing.
    /// </summary>
    public static void RenderBilevelTiledFromModulesToStream(
        BitMatrix modules,
        MatrixPngRenderOptions opts,
        Stream stream,
        int tileWidth,
        int tileHeight,
        TiffCompression compression = TiffCompression.None,
        ushort photometric = 1) {
        var packed = PackBilevelModules(modules, opts, photometric, out var widthPx, out var heightPx, out var packedStride);
        TiffWriter.WriteBilevelTiled(stream, widthPx, heightPx, packed, packedStride, tileWidth, tileHeight, compression, photometric);
    }

    /// <summary>
    /// Renders the module matrix to a TIFF file.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string path) {
        var tiff = Render(modules, opts);
        return RenderIO.WriteBinary(path, tiff);
    }

    /// <summary>
    /// Renders the module matrix to a bilevel (1-bit) TIFF file.
    /// </summary>
    public static string RenderBilevelToFile(
        BitMatrix modules,
        MatrixPngRenderOptions opts,
        string path,
        int rowsPerStrip = 0,
        TiffCompression compression = TiffCompression.None,
        byte threshold = 128,
        ushort photometric = 1) {
        var tiff = RenderBilevel(modules, opts, rowsPerStrip, compression, threshold, photometric);
        return RenderIO.WriteBinary(path, tiff);
    }

    /// <summary>
    /// Renders the module matrix to a bilevel (1-bit) TIFF file using direct module packing.
    /// </summary>
    public static string RenderBilevelFromModulesToFile(
        BitMatrix modules,
        MatrixPngRenderOptions opts,
        string path,
        int rowsPerStrip = 0,
        TiffCompression compression = TiffCompression.None,
        ushort photometric = 1) {
        var tiff = RenderBilevelFromModules(modules, opts, rowsPerStrip, compression, photometric);
        return RenderIO.WriteBinary(path, tiff);
    }

    /// <summary>
    /// Renders the module matrix to a bilevel (1-bit) tiled TIFF file.
    /// </summary>
    public static string RenderBilevelTiledToFile(
        BitMatrix modules,
        MatrixPngRenderOptions opts,
        string path,
        int tileWidth,
        int tileHeight,
        TiffCompression compression = TiffCompression.None,
        byte threshold = 128,
        ushort photometric = 1) {
        var tiff = RenderBilevelTiled(modules, opts, tileWidth, tileHeight, compression, threshold, photometric);
        return RenderIO.WriteBinary(path, tiff);
    }

    /// <summary>
    /// Renders the module matrix to a bilevel (1-bit) tiled TIFF file using direct module packing.
    /// </summary>
    public static string RenderBilevelTiledFromModulesToFile(
        BitMatrix modules,
        MatrixPngRenderOptions opts,
        string path,
        int tileWidth,
        int tileHeight,
        TiffCompression compression = TiffCompression.None,
        ushort photometric = 1) {
        var tiff = RenderBilevelTiledFromModules(modules, opts, tileWidth, tileHeight, compression, photometric);
        return RenderIO.WriteBinary(path, tiff);
    }

    /// <summary>
    /// Renders the module matrix to a TIFF file under the specified directory.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, MatrixPngRenderOptions opts, string directory, string fileName) {
        var tiff = Render(modules, opts);
        return RenderIO.WriteBinary(directory, fileName, tiff);
    }

    /// <summary>
    /// Renders the module matrix to a bilevel (1-bit) TIFF file under the specified directory.
    /// </summary>
    public static string RenderBilevelToFile(
        BitMatrix modules,
        MatrixPngRenderOptions opts,
        string directory,
        string fileName,
        int rowsPerStrip = 0,
        TiffCompression compression = TiffCompression.None,
        byte threshold = 128,
        ushort photometric = 1) {
        var tiff = RenderBilevel(modules, opts, rowsPerStrip, compression, threshold, photometric);
        return RenderIO.WriteBinary(directory, fileName, tiff);
    }

    /// <summary>
    /// Renders the module matrix to a bilevel (1-bit) TIFF file under the specified directory using direct module packing.
    /// </summary>
    public static string RenderBilevelFromModulesToFile(
        BitMatrix modules,
        MatrixPngRenderOptions opts,
        string directory,
        string fileName,
        int rowsPerStrip = 0,
        TiffCompression compression = TiffCompression.None,
        ushort photometric = 1) {
        var tiff = RenderBilevelFromModules(modules, opts, rowsPerStrip, compression, photometric);
        return RenderIO.WriteBinary(directory, fileName, tiff);
    }

    /// <summary>
    /// Renders the module matrix to a bilevel (1-bit) tiled TIFF file under the specified directory.
    /// </summary>
    public static string RenderBilevelTiledToFile(
        BitMatrix modules,
        MatrixPngRenderOptions opts,
        string directory,
        string fileName,
        int tileWidth,
        int tileHeight,
        TiffCompression compression = TiffCompression.None,
        byte threshold = 128,
        ushort photometric = 1) {
        var tiff = RenderBilevelTiled(modules, opts, tileWidth, tileHeight, compression, threshold, photometric);
        return RenderIO.WriteBinary(directory, fileName, tiff);
    }

    /// <summary>
    /// Renders the module matrix to a bilevel (1-bit) tiled TIFF file under the specified directory using direct module packing.
    /// </summary>
    public static string RenderBilevelTiledFromModulesToFile(
        BitMatrix modules,
        MatrixPngRenderOptions opts,
        string directory,
        string fileName,
        int tileWidth,
        int tileHeight,
        TiffCompression compression = TiffCompression.None,
        ushort photometric = 1) {
        var tiff = RenderBilevelTiledFromModules(modules, opts, tileWidth, tileHeight, compression, photometric);
        return RenderIO.WriteBinary(directory, fileName, tiff);
    }

    private static byte[] PackBilevelModules(
        BitMatrix modules,
        MatrixPngRenderOptions opts,
        ushort photometric,
        out int widthPx,
        out int heightPx,
        out int packedStride) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        if (opts.ModuleSize <= 0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleSize));
        if (opts.QuietZone < 0) throw new ArgumentOutOfRangeException(nameof(opts.QuietZone));

        var moduleSize = opts.ModuleSize;
        var quiet = opts.QuietZone;
        widthPx = checked((modules.Width + quiet * 2) * moduleSize);
        heightPx = checked((modules.Height + quiet * 2) * moduleSize);
        packedStride = (widthPx + 7) / 8;

        var fgLuma = (opts.Foreground.R * 77 + opts.Foreground.G * 150 + opts.Foreground.B * 29) >> 8;
        var bgLuma = (opts.Background.R * 77 + opts.Background.G * 150 + opts.Background.B * 29) >> 8;
        var foregroundIsDark = fgLuma <= bgLuma;

        var packed = new byte[packedStride * heightPx];
        for (var y = 0; y < heightPx; y++) {
            var moduleY = y / moduleSize - quiet;
            var inY = moduleY >= 0 && moduleY < modules.Height;
            var rowOffset = y * packedStride;
            for (var x = 0; x < widthPx; x++) {
                var moduleX = x / moduleSize - quiet;
                var inX = moduleX >= 0 && moduleX < modules.Width;
                var moduleOn = inX && inY && modules[moduleX, moduleY];
                var isBlack = moduleOn ? foregroundIsDark : !foregroundIsDark;
                var bit = photometric == 0 ? (isBlack ? 1 : 0) : (isBlack ? 0 : 1);
                if (bit != 0) {
                    var dst = rowOffset + (x >> 3);
                    packed[dst] |= (byte)(1 << (7 - (x & 7)));
                }
            }
        }

        return packed;
    }
}
