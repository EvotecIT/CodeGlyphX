using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Tiff;

/// <summary>
/// Renders 1D barcodes to a TIFF image (baseline, uncompressed).
/// </summary>
public static class BarcodeTiffRenderer {
    /// <summary>
    /// Renders the barcode to a TIFF byte array.
    /// </summary>
    public static byte[] Render(Barcode1D barcode, BarcodePngRenderOptions opts) {
        var pixels = BarcodePngRenderer.RenderPixels(barcode, opts, out var widthPx, out var heightPx, out var stride);
        return TiffWriter.WriteRgba32(widthPx, heightPx, pixels, stride);
    }

    /// <summary>
    /// Renders the barcode to a bilevel (1-bit) TIFF byte array.
    /// </summary>
    public static byte[] RenderBilevel(
        Barcode1D barcode,
        BarcodePngRenderOptions opts,
        int rowsPerStrip = 0,
        TiffCompression compression = TiffCompression.None,
        byte threshold = 128,
        ushort photometric = 1) {
        var pixels = BarcodePngRenderer.RenderPixels(barcode, opts, out var widthPx, out var heightPx, out var stride);
        using var ms = new MemoryStream();
        TiffWriter.WriteBilevelFromRgba(ms, widthPx, heightPx, pixels, stride, rowsPerStrip <= 0 ? heightPx : rowsPerStrip, compression, threshold, photometric);
        return ms.ToArray();
    }

    /// <summary>
    /// Renders the barcode to a bilevel (1-bit) TIFF byte array using direct bar packing (label is ignored).
    /// </summary>
    public static byte[] RenderBilevelFromBars(
        Barcode1D barcode,
        BarcodePngRenderOptions opts,
        int rowsPerStrip = 0,
        TiffCompression compression = TiffCompression.None,
        ushort photometric = 1) {
        var packed = PackBilevelBarcode(barcode, opts, photometric, out var widthPx, out var heightPx, out var packedStride);
        using var ms = new MemoryStream();
        TiffWriter.WriteBilevel(ms, widthPx, heightPx, packed, packedStride, rowsPerStrip <= 0 ? heightPx : rowsPerStrip, compression, photometric);
        return ms.ToArray();
    }

    /// <summary>
    /// Renders the barcode to a bilevel (1-bit) tiled TIFF byte array.
    /// </summary>
    public static byte[] RenderBilevelTiled(
        Barcode1D barcode,
        BarcodePngRenderOptions opts,
        int tileWidth,
        int tileHeight,
        TiffCompression compression = TiffCompression.None,
        byte threshold = 128,
        ushort photometric = 1) {
        var pixels = BarcodePngRenderer.RenderPixels(barcode, opts, out var widthPx, out var heightPx, out var stride);
        using var ms = new MemoryStream();
        TiffWriter.WriteBilevelTiledFromRgba(ms, widthPx, heightPx, pixels, stride, tileWidth, tileHeight, compression, threshold, photometric);
        return ms.ToArray();
    }

    /// <summary>
    /// Renders the barcode to a bilevel (1-bit) tiled TIFF byte array using direct bar packing (label is ignored).
    /// </summary>
    public static byte[] RenderBilevelTiledFromBars(
        Barcode1D barcode,
        BarcodePngRenderOptions opts,
        int tileWidth,
        int tileHeight,
        TiffCompression compression = TiffCompression.None,
        ushort photometric = 1) {
        var packed = PackBilevelBarcode(barcode, opts, photometric, out var widthPx, out var heightPx, out var packedStride);
        using var ms = new MemoryStream();
        TiffWriter.WriteBilevelTiled(ms, widthPx, heightPx, packed, packedStride, tileWidth, tileHeight, compression, photometric);
        return ms.ToArray();
    }

    /// <summary>
    /// Renders the barcode to a TIFF stream.
    /// </summary>
    public static void RenderToStream(Barcode1D barcode, BarcodePngRenderOptions opts, Stream stream) {
        var pixels = BarcodePngRenderer.RenderPixels(barcode, opts, out var widthPx, out var heightPx, out var stride);
        TiffWriter.WriteRgba32(stream, widthPx, heightPx, pixels, stride);
    }

    /// <summary>
    /// Renders the barcode to a bilevel (1-bit) TIFF stream.
    /// </summary>
    public static void RenderBilevelToStream(
        Barcode1D barcode,
        BarcodePngRenderOptions opts,
        Stream stream,
        int rowsPerStrip = 0,
        TiffCompression compression = TiffCompression.None,
        byte threshold = 128,
        ushort photometric = 1) {
        var pixels = BarcodePngRenderer.RenderPixels(barcode, opts, out var widthPx, out var heightPx, out var stride);
        TiffWriter.WriteBilevelFromRgba(stream, widthPx, heightPx, pixels, stride, rowsPerStrip <= 0 ? heightPx : rowsPerStrip, compression, threshold, photometric);
    }

    /// <summary>
    /// Renders the barcode to a bilevel (1-bit) TIFF stream using direct bar packing (label is ignored).
    /// </summary>
    public static void RenderBilevelFromBarsToStream(
        Barcode1D barcode,
        BarcodePngRenderOptions opts,
        Stream stream,
        int rowsPerStrip = 0,
        TiffCompression compression = TiffCompression.None,
        ushort photometric = 1) {
        var packed = PackBilevelBarcode(barcode, opts, photometric, out var widthPx, out var heightPx, out var packedStride);
        TiffWriter.WriteBilevel(stream, widthPx, heightPx, packed, packedStride, rowsPerStrip <= 0 ? heightPx : rowsPerStrip, compression, photometric);
    }

    /// <summary>
    /// Renders the barcode to a bilevel (1-bit) tiled TIFF stream.
    /// </summary>
    public static void RenderBilevelTiledToStream(
        Barcode1D barcode,
        BarcodePngRenderOptions opts,
        Stream stream,
        int tileWidth,
        int tileHeight,
        TiffCompression compression = TiffCompression.None,
        byte threshold = 128,
        ushort photometric = 1) {
        var pixels = BarcodePngRenderer.RenderPixels(barcode, opts, out var widthPx, out var heightPx, out var stride);
        TiffWriter.WriteBilevelTiledFromRgba(stream, widthPx, heightPx, pixels, stride, tileWidth, tileHeight, compression, threshold, photometric);
    }

    /// <summary>
    /// Renders the barcode to a bilevel (1-bit) tiled TIFF stream using direct bar packing (label is ignored).
    /// </summary>
    public static void RenderBilevelTiledFromBarsToStream(
        Barcode1D barcode,
        BarcodePngRenderOptions opts,
        Stream stream,
        int tileWidth,
        int tileHeight,
        TiffCompression compression = TiffCompression.None,
        ushort photometric = 1) {
        var packed = PackBilevelBarcode(barcode, opts, photometric, out var widthPx, out var heightPx, out var packedStride);
        TiffWriter.WriteBilevelTiled(stream, widthPx, heightPx, packed, packedStride, tileWidth, tileHeight, compression, photometric);
    }

    /// <summary>
    /// Renders the barcode to a TIFF file.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string path) {
        var tiff = Render(barcode, opts);
        return RenderIO.WriteBinary(path, tiff);
    }

    /// <summary>
    /// Renders the barcode to a bilevel (1-bit) TIFF file.
    /// </summary>
    public static string RenderBilevelToFile(
        Barcode1D barcode,
        BarcodePngRenderOptions opts,
        string path,
        int rowsPerStrip = 0,
        TiffCompression compression = TiffCompression.None,
        byte threshold = 128,
        ushort photometric = 1) {
        var tiff = RenderBilevel(barcode, opts, rowsPerStrip, compression, threshold, photometric);
        return RenderIO.WriteBinary(path, tiff);
    }

    /// <summary>
    /// Renders the barcode to a bilevel (1-bit) TIFF file using direct bar packing (label is ignored).
    /// </summary>
    public static string RenderBilevelFromBarsToFile(
        Barcode1D barcode,
        BarcodePngRenderOptions opts,
        string path,
        int rowsPerStrip = 0,
        TiffCompression compression = TiffCompression.None,
        ushort photometric = 1) {
        var tiff = RenderBilevelFromBars(barcode, opts, rowsPerStrip, compression, photometric);
        return RenderIO.WriteBinary(path, tiff);
    }

    /// <summary>
    /// Renders the barcode to a bilevel (1-bit) tiled TIFF file.
    /// </summary>
    public static string RenderBilevelTiledToFile(
        Barcode1D barcode,
        BarcodePngRenderOptions opts,
        string path,
        int tileWidth,
        int tileHeight,
        TiffCompression compression = TiffCompression.None,
        byte threshold = 128,
        ushort photometric = 1) {
        var tiff = RenderBilevelTiled(barcode, opts, tileWidth, tileHeight, compression, threshold, photometric);
        return RenderIO.WriteBinary(path, tiff);
    }

    /// <summary>
    /// Renders the barcode to a bilevel (1-bit) tiled TIFF file using direct bar packing (label is ignored).
    /// </summary>
    public static string RenderBilevelTiledFromBarsToFile(
        Barcode1D barcode,
        BarcodePngRenderOptions opts,
        string path,
        int tileWidth,
        int tileHeight,
        TiffCompression compression = TiffCompression.None,
        ushort photometric = 1) {
        var tiff = RenderBilevelTiledFromBars(barcode, opts, tileWidth, tileHeight, compression, photometric);
        return RenderIO.WriteBinary(path, tiff);
    }

    /// <summary>
    /// Renders the barcode to a TIFF file under the specified directory.
    /// </summary>
    public static string RenderToFile(Barcode1D barcode, BarcodePngRenderOptions opts, string directory, string fileName) {
        var tiff = Render(barcode, opts);
        return RenderIO.WriteBinary(directory, fileName, tiff);
    }

    /// <summary>
    /// Renders the barcode to a bilevel (1-bit) TIFF file under the specified directory.
    /// </summary>
    public static string RenderBilevelToFile(
        Barcode1D barcode,
        BarcodePngRenderOptions opts,
        string directory,
        string fileName,
        int rowsPerStrip = 0,
        TiffCompression compression = TiffCompression.None,
        byte threshold = 128,
        ushort photometric = 1) {
        var tiff = RenderBilevel(barcode, opts, rowsPerStrip, compression, threshold, photometric);
        return RenderIO.WriteBinary(directory, fileName, tiff);
    }

    /// <summary>
    /// Renders the barcode to a bilevel (1-bit) TIFF file under the specified directory using direct bar packing (label is ignored).
    /// </summary>
    public static string RenderBilevelFromBarsToFile(
        Barcode1D barcode,
        BarcodePngRenderOptions opts,
        string directory,
        string fileName,
        int rowsPerStrip = 0,
        TiffCompression compression = TiffCompression.None,
        ushort photometric = 1) {
        var tiff = RenderBilevelFromBars(barcode, opts, rowsPerStrip, compression, photometric);
        return RenderIO.WriteBinary(directory, fileName, tiff);
    }

    /// <summary>
    /// Renders the barcode to a bilevel (1-bit) tiled TIFF file under the specified directory.
    /// </summary>
    public static string RenderBilevelTiledToFile(
        Barcode1D barcode,
        BarcodePngRenderOptions opts,
        string directory,
        string fileName,
        int tileWidth,
        int tileHeight,
        TiffCompression compression = TiffCompression.None,
        byte threshold = 128,
        ushort photometric = 1) {
        var tiff = RenderBilevelTiled(barcode, opts, tileWidth, tileHeight, compression, threshold, photometric);
        return RenderIO.WriteBinary(directory, fileName, tiff);
    }

    /// <summary>
    /// Renders the barcode to a bilevel (1-bit) tiled TIFF file under the specified directory using direct bar packing (label is ignored).
    /// </summary>
    public static string RenderBilevelTiledFromBarsToFile(
        Barcode1D barcode,
        BarcodePngRenderOptions opts,
        string directory,
        string fileName,
        int tileWidth,
        int tileHeight,
        TiffCompression compression = TiffCompression.None,
        ushort photometric = 1) {
        var tiff = RenderBilevelTiledFromBars(barcode, opts, tileWidth, tileHeight, compression, photometric);
        return RenderIO.WriteBinary(directory, fileName, tiff);
    }

    private static byte[] PackBilevelBarcode(
        Barcode1D barcode,
        BarcodePngRenderOptions opts,
        ushort photometric,
        out int widthPx,
        out int heightPx,
        out int packedStride) {
        if (barcode is null) throw new ArgumentNullException(nameof(barcode));
        if (opts is null) throw new ArgumentNullException(nameof(opts));
        if (opts.ModuleSize <= 0) throw new ArgumentOutOfRangeException(nameof(opts.ModuleSize));
        if (opts.QuietZone < 0) throw new ArgumentOutOfRangeException(nameof(opts.QuietZone));
        if (opts.HeightModules <= 0) throw new ArgumentOutOfRangeException(nameof(opts.HeightModules));

        var moduleSize = opts.ModuleSize;
        var quiet = opts.QuietZone;
        widthPx = checked((barcode.TotalModules + quiet * 2) * moduleSize);
        heightPx = checked(opts.HeightModules * moduleSize);
        packedStride = (widthPx + 7) / 8;

        var fgLuma = (opts.Foreground.R * 77 + opts.Foreground.G * 150 + opts.Foreground.B * 29) >> 8;
        var bgLuma = (opts.Background.R * 77 + opts.Background.G * 150 + opts.Background.B * 29) >> 8;
        var foregroundIsDark = fgLuma <= bgLuma;

        var packed = new byte[packedStride * heightPx];
        var barMap = new bool[barcode.TotalModules];
        var idx = 0;
        foreach (var segment in barcode.Segments) {
            for (var i = 0; i < segment.Modules && idx < barMap.Length; i++) {
                barMap[idx++] = segment.IsBar;
            }
        }

        for (var y = 0; y < heightPx; y++) {
            var rowOffset = y * packedStride;
            for (var x = 0; x < widthPx; x++) {
                var moduleX = x / moduleSize - quiet;
                var isBar = moduleX >= 0 && moduleX < barMap.Length && barMap[moduleX];
                var isBlack = isBar ? foregroundIsDark : !foregroundIsDark;
                var bit = photometric == 0 ? (isBlack ? 1 : 0) : (isBlack ? 0 : 1);
                if (bit != 0) {
                    packed[rowOffset + (x >> 3)] |= (byte)(1 << (7 - (x & 7)));
                }
            }
        }

        return packed;
    }
}
