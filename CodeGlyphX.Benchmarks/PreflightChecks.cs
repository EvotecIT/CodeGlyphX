using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
#if COMPARE_QRCODER
using QRCoder;
#endif
#if COMPARE_ZXING
using ZXing;
using ZXing.Common;
#endif
#if COMPARE_BARCODER
using Barcoder.Ean;
using Barcoder.Pdf417;
#endif

namespace CodeGlyphX.Benchmarks;

internal static class PreflightChecks
{
    private const string EanText = "5901234123457";
    private const string QrText = "CodeGlyphX";

    public static int Run()
    {
        var failures = new List<string>();
        var validatedOutputs = 0;

        void Check(string name, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failures.Add($"{name}: {ex.GetType().Name} - {ex.Message}");
            }
        }

        Check("CodeGlyphX QR PNG", () =>
        {
            var png = QR.Render(QrText, OutputFormat.Png).ToArray();
            if (png.Length == 0) throw new InvalidOperationException("Empty PNG output.");
        });

        Check("CodeGlyphX Barcode PNG", () =>
        {
            var png = Barcode.Render(SymbolFormat.Ean, EanText, OutputFormat.Png, new BarcodeOptions()).ToArray();
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
            var bytes = CompareBenchmarkHelpers.EncodeZxingPng(writer, QrText);
            if (bytes.Length == 0) throw new InvalidOperationException("Empty PNG output.");
        });

        Check("ZXing QR Decode", () =>
        {
            var png = QR.Render(QrText, OutputFormat.Png).ToArray();
            var rgba = PngReader.DecodeRgba32(png, out var width, out var height);
            var reader = new BarcodeReaderGeneric
            {
                Options = new DecodingOptions { PossibleFormats = new[] { BarcodeFormat.QR_CODE } }
            };
            if (reader.Decode(rgba, width, height, RGBLuminanceSource.BitmapFormat.RGBA32)?.Text != QrText)
            {
                throw new InvalidOperationException("Decode returned null.");
            }
        });
#endif

#if COMPARE_BARCODER
        Check("Barcoder EAN PNG", () =>
        {
            var options = new BarcodeOptions();
            var renderer = CompareBenchmarkHelpers.CreateBarcoderBarcodeRenderer(options);
            var barcode = EanEncoder.Encode(EanText);
            var bytes = renderer.Render(barcode);
            if (bytes.Length == 0) throw new InvalidOperationException("Empty PNG output.");
        });

        Check("Barcoder PDF417 pixel fidelity", () =>
        {
            var options = new MatrixOptions();
            var barcode = Pdf417Encoder.Encode("Document ID: 98765 | Invoice: INV-2024-001234 | Amount: $1,234.56", securityLevel: 2);
            var bytes = CompareBenchmarkHelpers.CreateBarcoderMatrixRenderer(options).Render(barcode);
            var pixels = PngReader.DecodeRgba32(bytes, out var width, out var height);
            if (width != (barcode.Bounds.X + options.QuietZone * 2) * options.ModuleSize ||
                height != (barcode.Bounds.Y + options.QuietZone * 2) * options.ModuleSize)
                throw new InvalidOperationException("PNG dimensions do not match the source symbol and quiet zone.");
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var sourceX = x / options.ModuleSize - options.QuietZone;
                    var sourceY = y / options.ModuleSize - options.QuietZone;
                    var dark = sourceX >= 0 && sourceX < barcode.Bounds.X && sourceY >= 0 && sourceY < barcode.Bounds.Y && barcode.At(sourceX, sourceY);
                    var offset = (y * width + x) * 4;
                    var expected = dark ? (byte)0 : (byte)255;
                    if (pixels[offset] != expected || pixels[offset + 1] != expected || pixels[offset + 2] != expected || pixels[offset + 3] != 255)
                        throw new InvalidOperationException("PNG pixel differs from the source symbol.");
                }
            }
        });
        Console.WriteLine("Barcoder PDF417 timing is excluded: upstream output is not payload-qualified; source-pixel fidelity is checked.");
#endif

        // Exercise every enabled encode comparison through its real setup and PNG output.
        foreach (var type in typeof(PreflightChecks).Assembly.GetTypes()
            .Where(type => type.Name.EndsWith("CompareBenchmarks", StringComparison.Ordinal)))
        {
            var methods = type.GetMethods().Where(method => method.ReturnType == typeof(byte[]) &&
                method.GetCustomAttribute<BenchmarkAttribute>() is not null).ToArray();
            if (methods.Length == 0) continue;
            var instance = Activator.CreateInstance(type)!;
            var content = type.GetFields(BindingFlags.NonPublic | BindingFlags.Static)
                .First(field => field.IsLiteral && field.FieldType == typeof(string)).GetRawConstantValue() as string;
            Check(type.Name + " setup", () => type.GetMethod("Setup")!.Invoke(instance, null));
            foreach (var method in methods)
            {
                Check(type.Name + "." + method.Name, () =>
                {
                    var bytes = (byte[])method.Invoke(instance, null)!;
                    var pixels = PngReader.DecodeRgba32(bytes, out var width, out var height);
                    if (width <= 0 || height <= 0 || pixels.Length != checked(width * height * 4))
                        throw new InvalidOperationException("Invalid PNG dimensions.");
                    var hasDark = false;
                    var hasLight = false;
                    for (var offset = 0; offset < pixels.Length; offset += 4)
                    {
                        hasDark |= pixels[offset] < 128;
                        hasLight |= pixels[offset] >= 128;
                    }
                    if (!hasDark || !hasLight) throw new InvalidOperationException("PNG does not contain a symbol.");
#if COMPARE_ZXING
                    var options = new DecodingOptions { TryHarder = true };
                    if (type == typeof(Code39CompareBenchmarks) && !method.Name.StartsWith("ZXing_", StringComparison.Ordinal))
                        options.Hints[DecodeHintType.ASSUME_CODE_39_CHECK_DIGIT] = true;
                    var reader = new BarcodeReaderGeneric { Options = options };
                    var decodedText = reader.Decode(pixels, width, height, RGBLuminanceSource.BitmapFormat.RGBA32)?.Text;
                    if (decodedText is null && type == typeof(Pdf417CompareBenchmarks))
                        decodedText = Pdf417Code.TryDecodePng(bytes, out string text) ? text : null;
                    if (!string.Equals(decodedText, content, StringComparison.Ordinal))
                        throw new InvalidOperationException($"PNG ({width}x{height}) decoded as '{decodedText}' instead of '{content}'.");
#endif
                    validatedOutputs++;
                });
            }
        }

        if (failures.Count == 0)
        {
            Console.WriteLine($"Preflight checks passed; {validatedOutputs} timed PNG outputs validated.");
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
