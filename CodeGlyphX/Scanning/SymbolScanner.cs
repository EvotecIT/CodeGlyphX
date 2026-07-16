using System;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Aztec;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

/// <summary>
/// Unified scanner for raw frames and encoded images.
/// </summary>
public static class SymbolScanner {
    /// <summary>
    /// Scans a raw image frame for all requested symbols.
    /// </summary>
    public static ScanResult Scan(ImageFrame frame, ScanOptions? options = null) {
        if (frame is null) throw new ArgumentNullException(nameof(frame));
        options ??= new ScanOptions();
        ValidateOptions(options);
        using (var deadline = new ScanDeadline(options.CancellationToken, options.TimeoutMilliseconds)) {
            return ScanFrame(frame, options, deadline);
        }
    }

    /// <summary>
    /// Decodes an encoded image and scans it for all requested symbols.
    /// </summary>
    public static ScanResult Scan(byte[] encodedImage, ScanOptions? options = null) {
        if (encodedImage is null) throw new ArgumentNullException(nameof(encodedImage));
        options ??= new ScanOptions();
        ValidateOptions(options);

        using (var deadline = new ScanDeadline(options.CancellationToken, options.TimeoutMilliseconds)) {
            if (deadline.ShouldStop) return Cancelled(deadline, new List<SymbolFormat>());
            try {
                if (!ImageReader.TryDecodeRgba32(encodedImage, options.Image, out var rgba, out var width, out var height)) {
                    return Result(ScanStatus.InvalidImage, deadline, new List<DetectedSymbol>(), new List<SymbolFormat>(), "The encoded image could not be decoded.");
                }
                if (deadline.ShouldStop) return Cancelled(deadline, new List<SymbolFormat>());
                return ScanFrame(ImageFrame.Packed(rgba, width, height, PixelFormat.Rgba32), options, deadline);
            } catch (ArgumentException ex) {
                return Result(ScanStatus.InvalidImage, deadline, new List<DetectedSymbol>(), new List<SymbolFormat>(), ex.Message);
            } catch (InvalidOperationException ex) {
                return Result(ScanStatus.InvalidImage, deadline, new List<DetectedSymbol>(), new List<SymbolFormat>(), ex.Message);
            } catch (NotSupportedException ex) {
                return Result(ScanStatus.InvalidImage, deadline, new List<DetectedSymbol>(), new List<SymbolFormat>(), ex.Message);
            }
        }
    }

    /// <summary>
    /// Attempts to scan a raw frame and returns decoded symbols.
    /// </summary>
    public static bool TryScan(ImageFrame frame, out DetectedSymbol[] symbols, ScanOptions? options = null) {
        var result = Scan(frame, options);
        symbols = CopySymbols(result.Symbols);
        return result.IsSuccess;
    }

    /// <summary>
    /// Attempts to decode and scan an encoded image and returns decoded symbols.
    /// </summary>
    public static bool TryScan(byte[] encodedImage, out DetectedSymbol[] symbols, ScanOptions? options = null) {
        var result = Scan(encodedImage, options);
        symbols = CopySymbols(result.Symbols);
        return result.IsSuccess;
    }

    private static ScanResult ScanFrame(ImageFrame frame, ScanOptions options, ScanDeadline deadline) {
        var unsupported = new List<SymbolFormat>();
        var requested = ResolveRequestedFormats(options.Formats, unsupported);
        if (requested.Count == 0) {
            return Result(ScanStatus.UnsupportedFormats, deadline, new List<DetectedSymbol>(), unsupported, "None of the requested formats support image scanning.");
        }

        var fullRegion = new ImageRegion(0, 0, frame.Width, frame.Height);
        var region = options.Region?.ClipTo(frame.Width, frame.Height);
        if (options.Region.HasValue && !region.HasValue) {
            return Result(ScanStatus.InvalidImage, deadline, new List<DetectedSymbol>(), unsupported, "The scan region does not overlap the image.");
        }
        var searchRegion = region ?? fullRegion;

        if (deadline.ShouldStop) return Cancelled(deadline, unsupported);
        var rgba = ImageFrameConverter.ToRgba32(frame, searchRegion, out var width, out var height);
        if (deadline.ShouldStop) return Cancelled(deadline, unsupported);

        var results = new List<DetectedSymbol>();
        var seen = options.Deduplicate ? new HashSet<string>(StringComparer.Ordinal) : null;
        var requestedSet = new HashSet<SymbolFormat>(requested);

        ScanQr(rgba, width, height, searchRegion, options, deadline, requestedSet, results, seen);
        if (!ShouldStop(options, deadline, results)) ScanMicroQr(rgba, width, height, searchRegion, deadline, requestedSet, results, seen);
        if (!ShouldStop(options, deadline, results)) ScanDataMatrix(rgba, width, height, searchRegion, options, deadline, requestedSet, results, seen);
        if (!ShouldStop(options, deadline, results)) ScanAztec(rgba, width, height, searchRegion, deadline, requestedSet, results, seen);
        if (!ShouldStop(options, deadline, results)) ScanPdf417(rgba, width, height, searchRegion, deadline, requestedSet, results, seen);
        if (!ShouldStop(options, deadline, results)) ScanLinear(rgba, width, height, searchRegion, options, deadline, requestedSet, results, seen);

        TrimToMaximum(options, results);
        if (results.Count > 0) {
            var partial = deadline.ShouldStop;
            var failure = partial ? (deadline.CallerCancelled ? "Cancelled after partial results." : "Deadline exceeded after partial results.") : null;
            return Result(ScanStatus.Success, deadline, results, unsupported, failure, partial);
        }
        if (deadline.ShouldStop) return Cancelled(deadline, unsupported);
        return Result(ScanStatus.NoSymbolFound, deadline, results, unsupported);
    }

    private static void ScanMicroQr(
        byte[] rgba,
        int width,
        int height,
        ImageRegion searchRegion,
        ScanDeadline deadline,
        ISet<SymbolFormat> requested,
        List<DetectedSymbol> results,
        HashSet<string>? seen) {
        if (!requested.Contains(SymbolFormat.MicroQrCode)) return;
        if (!MicroQrDecoder.TryDecode(
                rgba,
                width,
                height,
                width * 4,
                PixelFormat.Rgba32,
                deadline.Token,
                out var decoded,
                out var info)) return;

        Add(results, seen, new DetectedSymbol(
            SymbolFormat.MicroQrCode,
            new CodeGlyphDecoded(decoded),
            searchRegion,
            TranslateGeometry(info.Geometry, searchRegion.X, searchRegion.Y),
            isInverted: info.IsInverted,
            isMirrored: info.IsMirrored));
    }

    private static void ScanQr(
        byte[] rgba,
        int width,
        int height,
        ImageRegion searchRegion,
        ScanOptions options,
        ScanDeadline deadline,
        ISet<SymbolFormat> requested,
        List<DetectedSymbol> results,
        HashSet<string>? seen) {
        if (!requested.Contains(SymbolFormat.QrCode)) return;
        var qrOptions = ResolveQrOptions(options, deadline);
        if (!QrImageDecoder.TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, qrOptions, deadline.Token, out var decoded)) return;
        for (var i = 0; i < decoded.Length; i++) {
            Add(results, seen, new DetectedSymbol(SymbolFormat.QrCode, new CodeGlyphDecoded(decoded[i]), searchRegion));
            if (ReachedMaximum(options, results)) return;
        }
    }

    private static void ScanDataMatrix(
        byte[] rgba,
        int width,
        int height,
        ImageRegion searchRegion,
        ScanOptions options,
        ScanDeadline deadline,
        ISet<SymbolFormat> requested,
        List<DetectedSymbol> results,
        HashSet<string>? seen) {
        if (!requested.Contains(SymbolFormat.DataMatrix)) return;
        if (DataMatrixDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, deadline.Token, out var text)) {
            Add(results, seen, new DetectedSymbol(SymbolFormat.DataMatrix, new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, text), searchRegion));
            return;
        }
        if (options.DirectPartMarking is null || deadline.ShouldStop) return;
        var dpm = options.DirectPartMarking.Clone();
        var variants = DirectPartMarkPreprocessor.CreateVariants(rgba, width, height, dpm, deadline.Token);
        for (var i = 0; i < variants.Count && !deadline.ShouldStop; i++) {
            if (!DataMatrixDecoder.TryDecode(variants[i], width, height, width * 4, PixelFormat.Rgba32, deadline.Token, out text)) continue;
            Add(results, seen, new DetectedSymbol(SymbolFormat.DataMatrix,
                new CodeGlyphDecoded(CodeGlyphKind.DataMatrix, text), searchRegion, directPartMarkProfile: dpm.Profile));
            return;
        }
    }

    private static void ScanAztec(
        byte[] rgba,
        int width,
        int height,
        ImageRegion searchRegion,
        ScanDeadline deadline,
        ISet<SymbolFormat> requested,
        List<DetectedSymbol> results,
        HashSet<string>? seen) {
        if (!requested.Contains(SymbolFormat.Aztec)) return;
        if (AztecDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, deadline.Token, out var text)) {
            Add(results, seen, new DetectedSymbol(SymbolFormat.Aztec, new CodeGlyphDecoded(CodeGlyphKind.Aztec, text), searchRegion));
        }
    }

    private static void ScanPdf417(
        byte[] rgba,
        int width,
        int height,
        ImageRegion searchRegion,
        ScanDeadline deadline,
        ISet<SymbolFormat> requested,
        List<DetectedSymbol> results,
        HashSet<string>? seen) {
        if (!requested.Contains(SymbolFormat.Pdf417)) return;
        if (Pdf417Decoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, deadline.Token, out Pdf417Decoded decoded)) {
            Add(results, seen, new DetectedSymbol(SymbolFormat.Pdf417, new CodeGlyphDecoded(decoded), searchRegion));
        }
    }

    private static void ScanLinear(
        byte[] rgba,
        int width,
        int height,
        ImageRegion searchRegion,
        ScanOptions options,
        ScanDeadline deadline,
        ISet<SymbolFormat> requested,
        List<DetectedSymbol> results,
        HashSet<string>? seen) {
        var expectedTypes = new List<BarcodeType>();
        foreach (var format in requested) {
            var capability = SymbolCapabilities.Get(format);
            if (capability.Family == SymbolFamily.Linear && capability.LegacyBarcodeType.HasValue && capability.CanScanImages) {
                expectedTypes.Add(capability.LegacyBarcodeType.Value);
            }
        }
        if (expectedTypes.Count == 0) return;

        BarcodeType? expected = expectedTypes.Count == 1 ? expectedTypes[0] : (BarcodeType?)null;
        var barcodeOptions = CloneBarcodeOptions(options.Barcode);
        if (!BarcodeDecoder.TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out var decoded, expected, barcodeOptions, deadline.Token)) return;
        for (var i = 0; i < decoded.Length; i++) {
            var hit = ResolveRequestedLinearIdentity(decoded[i], expectedTypes, rgba, width, height, deadline.Token);
            if (!SymbolCapabilities.TryFromLegacy(hit.Type, out var format) || !requested.Contains(format)) continue;
            Add(results, seen, new DetectedSymbol(format, new CodeGlyphDecoded(hit), searchRegion));
            if (ReachedMaximum(options, results)) return;
        }
    }

    private static BarcodeDecoded ResolveRequestedLinearIdentity(
        BarcodeDecoded decoded,
        List<BarcodeType> expectedTypes,
        byte[] rgba,
        int width,
        int height,
        CancellationToken cancellationToken) {
        // DataBar Omnidirectional and Truncated have the same horizontal module sequence; only bar height
        // distinguishes them. An Omni-only request supplies the caller's physical identity. When both are
        // requested, use the standards-defined 33X Omnidirectional height boundary and otherwise preserve
        // the scanline decoder's conservative Truncated identity.
        if (decoded.Type == BarcodeType.GS1DataBarTruncated
            && expectedTypes.Contains(BarcodeType.GS1DataBarOmni)) {
            if (!expectedTypes.Contains(BarcodeType.GS1DataBarTruncated)
                || DataBar14ImageClassifier.TryIsOmnidirectional(rgba, width, height, cancellationToken, out var isOmnidirectional)
                && isOmnidirectional) {
                return new BarcodeDecoded(BarcodeType.GS1DataBarOmni, decoded.Text);
            }
        }
        return decoded;
    }

    private static QrPixelDecodeOptions ResolveQrOptions(ScanOptions options, ScanDeadline deadline) {
        var source = options.Qr ?? CreateQrProfile(options.Profile, options.TimeoutMilliseconds);
        var result = new QrPixelDecodeOptions {
            Profile = source.Profile,
            MaxDimension = source.MaxDimension,
            MaxScale = source.MaxScale,
            BudgetMilliseconds = source.BudgetMilliseconds,
            AutoCrop = source.AutoCrop,
            EnableTileScan = source.EnableTileScan,
            TileGrid = source.TileGrid,
            DisableTransforms = source.DisableTransforms,
            AggressiveSampling = source.AggressiveSampling,
            StylizedSampling = source.StylizedSampling
        };
        if (deadline.TimeoutMilliseconds > 0 && (result.BudgetMilliseconds <= 0 || result.BudgetMilliseconds > deadline.RemainingMilliseconds)) {
            result.BudgetMilliseconds = deadline.RemainingMilliseconds;
        }
        return result;
    }

    private static QrPixelDecodeOptions CreateQrProfile(ScanProfile profile, int timeoutMilliseconds) {
        switch (profile) {
            case ScanProfile.Fast:
                return QrPixelDecodeOptions.Fast();
            case ScanProfile.Balanced:
                return QrPixelDecodeOptions.Balanced();
            case ScanProfile.Robust:
                return QrPixelDecodeOptions.Robust();
            case ScanProfile.Screen:
                return QrPixelDecodeOptions.Screen(timeoutMilliseconds > 0 ? timeoutMilliseconds : 300);
            default:
                throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unknown scan profile.");
        }
    }

    private static BarcodeDecodeOptions? CloneBarcodeOptions(BarcodeDecodeOptions? source) {
        if (source is null) return null;
        return new BarcodeDecodeOptions {
            Code39Checksum = source.Code39Checksum,
            MsiChecksum = source.MsiChecksum,
            Code11Checksum = source.Code11Checksum,
            PlesseyChecksum = source.PlesseyChecksum,
            EnableTileScan = source.EnableTileScan,
            TileGrid = source.TileGrid
        };
    }

    private static List<SymbolFormat> ResolveRequestedFormats(SymbolFormat[]? formats, List<SymbolFormat> unsupported) {
        var requested = new List<SymbolFormat>();
        var seen = new HashSet<SymbolFormat>();
        if (formats is null || formats.Length == 0) {
            for (var i = 0; i < SymbolCapabilities.ImageScannableFormats.Count; i++) {
                requested.Add(SymbolCapabilities.ImageScannableFormats[i]);
            }
            return requested;
        }

        for (var i = 0; i < formats.Length; i++) {
            if (!seen.Add(formats[i])) continue;
            if (!SymbolCapabilities.TryGet(formats[i], out var capability) || !capability.CanScanImages) {
                unsupported.Add(formats[i]);
            } else {
                requested.Add(formats[i]);
            }
        }
        return requested;
    }

    private static void Add(List<DetectedSymbol> results, HashSet<string>? seen, DetectedSymbol symbol) {
        if (seen is not null && !seen.Add(CreateKey(symbol))) return;
        results.Add(symbol);
    }

    private static SymbolGeometry TranslateGeometry(SymbolGeometry geometry, int offsetX, int offsetY) {
        if (offsetX == 0 && offsetY == 0) return geometry;
        return new SymbolGeometry(
            TranslatePoint(geometry.TopLeft, offsetX, offsetY),
            TranslatePoint(geometry.TopRight, offsetX, offsetY),
            TranslatePoint(geometry.BottomRight, offsetX, offsetY),
            TranslatePoint(geometry.BottomLeft, offsetX, offsetY));
    }

    private static SymbolPoint TranslatePoint(SymbolPoint point, int offsetX, int offsetY) {
        return new SymbolPoint(point.X + offsetX, point.Y + offsetY);
    }

    private static string CreateKey(DetectedSymbol symbol) {
        var bytes = symbol.HasRawBytes ? Convert.ToBase64String(symbol.RawBytes.ToArray()) : string.Empty;
        return symbol.Format + "\u001f" + symbol.Text + "\u001f" + bytes;
    }

    private static bool ShouldStop(ScanOptions options, ScanDeadline deadline, List<DetectedSymbol> results) {
        return deadline.ShouldStop || ReachedMaximum(options, results);
    }

    private static bool ReachedMaximum(ScanOptions options, List<DetectedSymbol> results) {
        return options.MaxSymbols > 0 && results.Count >= options.MaxSymbols;
    }

    private static void TrimToMaximum(ScanOptions options, List<DetectedSymbol> results) {
        if (options.MaxSymbols > 0 && results.Count > options.MaxSymbols) {
            results.RemoveRange(options.MaxSymbols, results.Count - options.MaxSymbols);
        }
    }

    private static ScanResult Cancelled(ScanDeadline deadline, List<SymbolFormat> unsupported) {
        var status = deadline.CallerCancelled ? ScanStatus.Cancelled : ScanStatus.DeadlineExceeded;
        var failure = deadline.CallerCancelled ? "The scan was cancelled." : "The total scan deadline elapsed.";
        return Result(status, deadline, new List<DetectedSymbol>(), unsupported, failure);
    }

    private static ScanResult Result(
        ScanStatus status,
        ScanDeadline deadline,
        List<DetectedSymbol> symbols,
        List<SymbolFormat> unsupported,
        string? failure = null,
        bool partial = false) {
        return new ScanResult(status, symbols, unsupported, deadline.Elapsed, failure, partial);
    }

    private static DetectedSymbol[] CopySymbols(IReadOnlyList<DetectedSymbol> symbols) {
        var result = new DetectedSymbol[symbols.Count];
        for (var i = 0; i < result.Length; i++) result[i] = symbols[i];
        return result;
    }

    private static void ValidateOptions(ScanOptions options) {
        if (options.TimeoutMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(options.TimeoutMilliseconds));
        if (options.MaxSymbols < 0) throw new ArgumentOutOfRangeException(nameof(options.MaxSymbols));
        if (!Enum.IsDefined(typeof(ScanProfile), options.Profile)) throw new ArgumentOutOfRangeException(nameof(options.Profile));
        if (options.DirectPartMarking is not null) DirectPartMarkPreprocessor.Validate(options.DirectPartMarking);
    }
}
