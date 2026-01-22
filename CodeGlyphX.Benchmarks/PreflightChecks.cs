using System;
using System.Collections.Generic;
using System.IO;
using CodeGlyphX.Rendering;
#if COMPARE_QRCODER
using QRCoder;
#endif
#if COMPARE_ZXING
using ZXing;
using ZXing.Common;
#endif
#if COMPARE_BARCODER
using Barcoder.Ean;
using Barcoder.Renderer.Image;
#endif
#if COMPARE_ZXING || COMPARE_BARCODER
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace CodeGlyphX.Benchmarks;

internal static class PreflightChecks
{
    private const string EanText = "5901234123457";
    private const string QrText = "CodeGlyphX";

    public static int Run()
    {
        var failures = new List<string>();
        var hasFailures = false;

        void Check(string name, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                hasFailures = true;
                failures.Add($"{name}: {ex.GetType().Name} - {ex.Message}");
            }
        }

        Check("CodeGlyphX QR PNG", () =>
        {
            var png = QrEasy.RenderPng(QrText);
            if (png.Length == 0) throw new InvalidOperationException("Empty PNG output.");
        });

        Check("CodeGlyphX Barcode PNG", () =>
        {
            var png = BarcodeEasy.RenderPng(BarcodeType.EAN, EanText, new BarcodeOptions());
            if (png.Length == 0) throw new InvalidOperationException("Empty PNG output.");
        });

#if COMPARE_QRCODER
        Check("QRCoder QR PNG", () =>
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(QrText, QRCodeGenerator.ECCLevel.M);
            var qrCode = new PngByteQRCode(data);
            var bytes = qrCode.GetGraphic(5);
            if (bytes.Length == 0) throw new InvalidOperationException("Empty PNG output.");
        });
#endif

#if COMPARE_ZXING
        Check("ZXing QR PNG", () =>
        {
            var options = new EncodingOptions { Width = 128, Height = 128, Margin = 2 };
            var writer = new BarcodeWriterGeneric
            {
                Format = BarcodeFormat.QR_CODE,
                Options = options
            };
            using var image = writer.WriteAsImageSharp<Rgba32>(QrText);
            var bytes = ImageSharpBenchmarkHelpers.ToPngBytes(image);
            if (bytes.Length == 0) throw new InvalidOperationException("Empty PNG output.");
        });

        Check("ZXing QR Decode", () =>
        {
            DecodeSampleHelper.LoadRgba("Assets/DecodingSamples/qr-clean-small.png", out var rgba, out var width, out var height);
            var reader = new BarcodeReaderGeneric
            {
                Options = new DecodingOptions { PossibleFormats = new[] { BarcodeFormat.QR_CODE } }
            };
            if (reader.Decode(rgba, width, height, RGBLuminanceSource.BitmapFormat.RGBA32) is null)
            {
                throw new InvalidOperationException("Decode returned null.");
            }
        });
#endif

#if COMPARE_BARCODER
        Check("Barcoder EAN PNG", () =>
        {
            var options = new BarcodeOptions();
            var barcoderOptions = CompareBenchmarkHelpers.CreateBarcoderBarcodeOptions(options);
            var renderer = new ImageRenderer(barcoderOptions);
            var barcode = EanEncoder.Encode(EanText);
            using var stream = new MemoryStream();
            renderer.Render(barcode, stream);
            if (stream.Length == 0) throw new InvalidOperationException("Empty PNG output.");
        });
#endif

        if (!hasFailures)
        {
            Console.WriteLine("Preflight checks passed.");
            return 0;
        }

        Console.Error.WriteLine("Preflight checks failed:");
        foreach (var failure in failures)
        {
            Console.Error.WriteLine($"- {failure}");
        }

        return 1;
    }
}
