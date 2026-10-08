using System;
using CodeGlyphX.Rendering;
#if COMPARE_ZXING
using CodeGlyphX.Rendering.Png;
using ZXing.Common;
#endif

namespace CodeGlyphX.Benchmarks;

internal static class CompareBenchmarkHelpers
{
    public static int MatrixWidthPx(BitMatrix modules, MatrixOptions options)
    {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (options is null) throw new ArgumentNullException(nameof(options));
        return (modules.Width + options.QuietZone * 2) * options.ModuleSize;
    }

    public static int MatrixHeightPx(BitMatrix modules, MatrixOptions options)
    {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (options is null) throw new ArgumentNullException(nameof(options));
        return (modules.Height + options.QuietZone * 2) * options.ModuleSize;
    }

    public static int BarcodeWidthPx(BarcodeType type, string content, BarcodeOptions options)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));
        var barcode = BarcodeEncoder.Encode(type, content);
        return (barcode.TotalModules + options.QuietZone * 2) * options.ModuleSize;
    }

    public static int BarcodeHeightPx(BarcodeOptions options)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));
        return options.HeightModules * options.ModuleSize;
    }

#if COMPARE_ZXING
    public static EncodingOptions CreateZxingOptions(int widthPx, int heightPx, int margin)
    {
        return new EncodingOptions
        {
            Width = widthPx,
            Height = heightPx,
            Margin = margin
        };
    }

    public static byte[] EncodeZxingPng(ZXing.BarcodeWriterGeneric writer, string content)
    {
        var matrix = writer.Encode(content);
        var pixels = new byte[checked(matrix.Width * matrix.Height * 4)];
        for (var y = 0; y < matrix.Height; y++)
        {
            for (var x = 0; x < matrix.Width; x++)
            {
                var offset = (y * matrix.Width + x) * 4;
                var value = matrix[x, y] ? (byte)0 : (byte)255;
                pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = value;
                pixels[offset + 3] = 255;
            }
        }
        return PngImageEncoder.EncodeRgba32(pixels, matrix.Width, matrix.Height);
    }
#endif

#if COMPARE_BARCODER
    public static ManagedBarcodeRenderer CreateBarcoderMatrixRenderer(MatrixOptions options)
    {
        return new ManagedBarcodeRenderer(options.ModuleSize, options.QuietZone, 1);
    }

    public static ManagedBarcodeRenderer CreateBarcoderBarcodeRenderer(BarcodeOptions options)
    {
        return new ManagedBarcodeRenderer(options.ModuleSize, options.QuietZone, options.HeightModules);
    }
#endif
}
