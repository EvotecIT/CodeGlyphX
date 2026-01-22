using System;
using System.IO;
using CodeGlyphX.Rendering;
#if COMPARE_ZXING || COMPARE_BARCODER
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
#endif
#if COMPARE_ZXING
using ZXing.Common;
#endif
#if COMPARE_BARCODER
using Barcoder.Renderer.Image;
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
        var barcode = Barcode.Encode(type, content);
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
#endif

#if COMPARE_BARCODER
    public static ImageRendererOptions CreateBarcoderMatrixOptions(MatrixOptions options)
    {
        return new ImageRendererOptions
        {
            ImageFormat = Barcoder.Renderer.Image.ImageFormat.Png,
            PixelSize = options.ModuleSize,
            CustomMargin = options.QuietZone * options.ModuleSize
        };
    }

    public static ImageRendererOptions CreateBarcoderBarcodeOptions(BarcodeOptions options)
    {
        return new ImageRendererOptions
        {
            ImageFormat = Barcoder.Renderer.Image.ImageFormat.Png,
            PixelSize = options.ModuleSize,
            CustomMargin = options.QuietZone * options.ModuleSize,
            BarHeightFor1DBarcode = options.HeightModules * options.ModuleSize,
            IncludeEanContentAsText = false
        };
    }
#endif
}

#if COMPARE_ZXING || COMPARE_BARCODER
internal static class ImageSharpBenchmarkHelpers
{
    public static byte[] ToPngBytes(Image<Rgba32> image)
    {
        using var stream = new MemoryStream();
        image.Save(stream, new PngEncoder());
        return stream.ToArray();
    }
}
#endif
