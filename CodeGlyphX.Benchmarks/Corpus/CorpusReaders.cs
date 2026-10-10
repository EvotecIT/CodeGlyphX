using System.Runtime.InteropServices;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Benchmarks;

// Product adapters only. Loading, expected payloads and measurement policy live outside these operations.
internal static class CorpusReaders {
    internal static string[] Decode(byte[] rgba, int width, int height, string format, string engine, bool robust, bool multiple) {
        if (engine == "CodeGlyphX") {
            var options = robust ? ScanOptions.Robust(1500) : ScanOptions.Balanced(500);
            options.Formats = format == "All" ? null : new[] { Enum.Parse<SymbolFormat>(format == "Ean8" ? "Ean" : format) };
            options.MaxSymbols = multiple ? 32 : 1;
            options.Image = new ImageDecodeOptions { MaxDimension = 0 };
            var result = SymbolScanner.Scan(ImageFrame.Packed(rgba, width, height, PixelFormat.Rgba32), options);
            return result.Symbols.Select(symbol => symbol.Text).Distinct(StringComparer.Ordinal).ToArray();
        }
#if COMPARE_ZXING
        if (engine == "ZXing.Net") {
            var reader = new ZXing.BarcodeReaderGeneric { AutoRotate = true };
            reader.Options.TryHarder = robust;
            reader.Options.TryInverted = true;
            reader.Options.PossibleFormats = format == "All" ? null : new[] { NetFormat(format) };
            if (!multiple) {
                var result = reader.Decode(rgba, width, height, ZXing.RGBLuminanceSource.BitmapFormat.RGBA32);
                return result is null ? Array.Empty<string>() : new[] { result.Text };
            }
            return (reader.DecodeMultiple(rgba, width, height, ZXing.RGBLuminanceSource.BitmapFormat.RGBA32) ?? Array.Empty<ZXing.Result>())
                .Select(result => result.Text).Distinct(StringComparer.Ordinal).ToArray();
        }
#endif
#if COMPARE_ZXINGCPP
        if (engine == "ZXing-C++") return DecodeCpp(rgba, width, height, format, robust, multiple);
#endif
        throw new NotSupportedException($"Comparison engine '{engine}' is not compiled into this executable.");
    }

#if COMPARE_ZXING
    private static ZXing.BarcodeFormat NetFormat(string format) => format switch {
        "QrCode" => ZXing.BarcodeFormat.QR_CODE,
        "DataMatrix" => ZXing.BarcodeFormat.DATA_MATRIX,
        "Aztec" => ZXing.BarcodeFormat.AZTEC,
        "Pdf417" => ZXing.BarcodeFormat.PDF_417,
        "Code128" => ZXing.BarcodeFormat.CODE_128,
        "Code39" => ZXing.BarcodeFormat.CODE_39,
        "Code93" => ZXing.BarcodeFormat.CODE_93,
        "Codabar" => ZXing.BarcodeFormat.CODABAR,
        "Ean" => ZXing.BarcodeFormat.EAN_13,
        "Ean8" => ZXing.BarcodeFormat.EAN_8,
        "UpcA" => ZXing.BarcodeFormat.UPC_A,
        "UpcE" => ZXing.BarcodeFormat.UPC_E,
        "Itf" => ZXing.BarcodeFormat.ITF,
        "Gs1DataBarOmnidirectional" => ZXing.BarcodeFormat.RSS_14,
        "Gs1DataBarExpanded" or "Gs1DataBarExpandedStacked" => ZXing.BarcodeFormat.RSS_EXPANDED,
        "MaxiCode" => ZXing.BarcodeFormat.MAXICODE,
        _ => throw new NotSupportedException($"ZXing.Net format mapping missing: {format}.")
    };
#endif

#if COMPARE_ZXINGCPP
    internal static string[] DecodeCpp(byte[] rgba, int width, int height, string format, bool robust, bool multiple = true) {
        using var reader = new ZXingCpp.BarcodeReader {
            Formats = CppFormat(format), TryHarder = robust, TryRotate = true, TryInvert = true,
            TextMode = format.StartsWith("Gs1DataBar", StringComparison.Ordinal) ? ZXingCpp.TextMode.HRI : ZXingCpp.TextMode.Plain,
            MaxNumberOfSymbols = multiple ? 32 : 1
        };
        // ImageView is non-owning. The official wrapper's byte-span constructor does not retain a pin.
        var pin = GCHandle.Alloc(rgba, GCHandleType.Pinned);
        try {
            var view = new ZXingCpp.ImageView(pin.AddrOfPinnedObject(), width, height, ZXingCpp.ImageFormat.RGBA, width * 4);
            var barcodes = reader.From(view);
            try {
                return barcodes.Where(barcode => barcode.IsValid).Select(barcode => barcode.Text).Distinct(StringComparer.Ordinal).ToArray();
            } finally {
                foreach (var barcode in barcodes) barcode.Dispose();
                GC.KeepAlive(view);
            }
        } finally { pin.Free(); }
    }

    private static ZXingCpp.BarcodeFormat CppFormat(string format) => ZXingCpp.BarcodeFormat.Parse(format switch {
        "All" => "AllReadable", "QrCode" => "QRCode", "Pdf417" => "PDF417", "Ean" => "EAN13", "Ean8" => "EAN8",
        "UpcA" => "UPCA", "UpcE" => "UPCE", "Itf" => "ITF",
        "Gs1DataBarOmnidirectional" => "DataBar", "Gs1DataBarExpanded" => "DataBarExp",
        "Gs1DataBarExpandedStacked" => "DataBarExpStk", _ => format
    });
#endif
}

/// <summary>Resolves comparison-only native assets in hosts without the executable's deps.json resolver.</summary>
public static class CorpusNativeLibrary {
    private static readonly object ResolverLock = new();
    private static string? _configuredPath;
    /// <summary>Uses the binding's platform asset without relying on a PowerShell host's deps.json.</summary>
    public static void ConfigureFromDirectory(string assemblyDirectory) {
        var architecture = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
        var platform = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux";
        var library = platform == "win" ? "ZXing.dll" : platform == "osx" ? "libZXing.dylib" : "libZXing.so";
        Configure(Path.Combine(assemblyDirectory, "runtimes", platform + "-" + architecture, "native", library));
    }

    public static void Configure(string absoluteLibraryPath) {
#if COMPARE_ZXINGCPP
        var path = Path.GetFullPath(absoluteLibraryPath);
        lock (ResolverLock) {
            if (_configuredPath is not null) {
                var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                if (!_configuredPath.Equals(path, comparison)) throw new InvalidOperationException("Run a different native engine in a fresh host.");
                return;
            }
            var handle = NativeLibrary.Load(path);
            try {
                NativeLibrary.GetExport(handle, "ZXing_ReaderOptions_delete");
                NativeLibrary.SetDllImportResolver(typeof(ZXingCpp.Barcode).Assembly, (name, assembly, searchPath) => name == "ZXing" ? handle : IntPtr.Zero);
                _configuredPath = path;
            } catch { NativeLibrary.Free(handle); throw; }
        }
#else
        throw new NotSupportedException("ZXing-C++ was not compiled in.");
#endif
    }
}
