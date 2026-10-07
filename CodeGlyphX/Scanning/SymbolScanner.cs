using System;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Aztec;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Internal;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

/// <summary>
/// Unified scanner for raw frames and encoded images.
/// </summary>
public static partial class SymbolScanner {
    /// <summary>
    /// Scans a raw image frame for all requested symbols.
    /// </summary>
    public static ScanResult Scan(ImageFrame frame, ScanOptions? options = null) {
        if (frame is null) throw new ArgumentNullException(nameof(frame));
        options = SnapshotOptions(options);
        using (var deadline = new ScanDeadline(options.CancellationToken, options.TimeoutMilliseconds)) {
            return ScanFrame(frame, options, deadline);
        }
    }

    /// <summary>
    /// Decodes an encoded image and scans it for all requested symbols.
    /// </summary>
    public static ScanResult Scan(byte[] encodedImage, ScanOptions? options = null) {
        if (encodedImage is null) throw new ArgumentNullException(nameof(encodedImage));
        options = SnapshotOptions(options);

        using (var deadline = new ScanDeadline(options.CancellationToken, options.TimeoutMilliseconds)) {
            return ScanEncodedImage(encodedImage, options, deadline);
        }
    }

    private static ScanResult ScanEncodedImage(byte[] encodedImage, ScanOptions options, ScanDeadline deadline) {
        if (deadline.ShouldStop) return Cancelled(deadline, new List<SymbolFormat>());
        try {
            var imageOptions = ResolveSourceImageDecodeOptions(options);
            if (!ImageReader.TryDecodeRgba32(encodedImage, imageOptions, out var rgba, out var width, out var height)) {
                return Result(ScanStatus.InvalidImage, deadline, new List<DetectedSymbol>(), new List<SymbolFormat>(), "The encoded image could not be decoded or exceeded its configured limits.");
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
        if (deadline.ShouldStop) return Cancelled(deadline, new List<SymbolFormat>());
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
        var frameRegion = region ?? fullRegion;
        var searchRegion = frameRegion;

        if (deadline.ShouldStop) return Cancelled(deadline, unsupported);
        var rgba = ImageFrameConverter.ToRgba32(frame, frameRegion, out var width, out var height);
        if (deadline.ShouldStop) return Cancelled(deadline, unsupported);
        if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options.Image, deadline.Token))
            return Cancelled(deadline, unsupported);
        if (deadline.ShouldStop) return Cancelled(deadline, unsupported);

        var results = new List<DetectedSymbol>();
        var seen = options.Deduplicate ? new HashSet<string>(StringComparer.Ordinal) : null;
        var requestedSet = new HashSet<SymbolFormat>(requested);
        var remainingAttempts = CountInitialAttempts(requestedSet, options, width, height);

        ScanQr(rgba, width, height, searchRegion, options, deadline, requestedSet, results, seen, ref remainingAttempts);
        if (!ShouldStop(options, deadline, results)) ScanMicroQr(rgba, width, height, searchRegion, options, deadline, requestedSet, results, seen, ref remainingAttempts);
        if (!ShouldStop(options, deadline, results)) ScanDataMatrix(rgba, width, height, searchRegion, options, deadline, requestedSet, results, seen, ref remainingAttempts);
        if (!ShouldStop(options, deadline, results)) ScanAztec(rgba, width, height, searchRegion, options, deadline, requestedSet, results, seen, ref remainingAttempts);
        if (!ShouldStop(options, deadline, results)) ScanPdf417(rgba, width, height, searchRegion, options, deadline, requestedSet, results, seen, ref remainingAttempts);
        if (!ShouldStop(options, deadline, results)) ScanLinear(rgba, width, height, searchRegion, options, deadline, requestedSet, results, seen, ref remainingAttempts);

        if (!ShouldStop(options, deadline, results) && options.EnableTileScan) {
            ScanMatrixTiles(rgba, width, height, searchRegion, options, deadline, requestedSet, results, seen);
        }
        TrimToMaximum(options, results);
        if (results.Count > 0) {
            var completion = deadline.ShouldStop
                ? (deadline.CallerCancelled ? ScanCompletionReason.Cancelled : ScanCompletionReason.DeadlineExceeded)
                : ReachedMaximum(options, results) ? ScanCompletionReason.SymbolLimitReached : ScanCompletionReason.Completed;
            var failure = completion == ScanCompletionReason.Cancelled ? "Cancelled after partial results."
                : completion == ScanCompletionReason.DeadlineExceeded ? "Deadline exceeded after partial results." : null;
            return Result(ScanStatus.Success, deadline, results, unsupported, failure, completion);
        }
        if (deadline.ShouldStop) return Cancelled(deadline, unsupported);
        return Result(ScanStatus.NoSymbolFound, deadline, results, unsupported);
    }

    private static void ScanMicroQr(
        byte[] rgba,
        int width,
        int height,
        ImageRegion searchRegion,
        ScanOptions options,
        ScanDeadline deadline,
        ISet<SymbolFormat> requested,
        List<DetectedSymbol> results,
        HashSet<string>? seen,
        ref int remainingAttempts) {
        if (!requested.Contains(SymbolFormat.MicroQrCode)) return;
        using var attempt = deadline.CreateAttempt(remainingAttempts--, options.Image?.RecognitionBudgetMilliseconds ?? 0);
        deadline = attempt;
        if (!MicroQrDecoder.TryDecode(
                rgba,
                width,
                height,
                width * 4,
                PixelFormat.Rgba32,
                deadline.Token,
                out var decoded,
                out var info)) return;

        Add(results, seen, SymbolResultFactory.From(decoded, searchRegion,
            MapGeometryToSource(info.Geometry, searchRegion, width, height), info.IsInverted, info.IsMirrored));
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
        HashSet<string>? seen,
        ref int remainingAttempts) {
        if (!requested.Contains(SymbolFormat.QrCode)) return;
        using var attempt = deadline.CreateAttempt(remainingAttempts--, options.Image?.RecognitionBudgetMilliseconds ?? 0);
        deadline = attempt;
        var qrOptions = ResolveQrOptions(options, deadline);
        if (options.MaxSymbols == 1) {
            if (QrImageDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, qrOptions, deadline.Token, out var single)) {
                Add(results, seen, SymbolResultFactory.From(single, searchRegion));
                return;
            }
            if (deadline.ShouldStop) return;
        }
        if (!QrImageDecoder.TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, qrOptions, deadline.Token, out var decoded)) return;
        for (var i = 0; i < decoded.Length; i++) {
            Add(results, seen, SymbolResultFactory.From(decoded[i], searchRegion));
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
        HashSet<string>? seen,
        ref int remainingAttempts) {
        if (!requested.Contains(SymbolFormat.DataMatrix)) return;
        using var attempt = deadline.CreateAttempt(remainingAttempts--, options.Image?.RecognitionBudgetMilliseconds ?? 0);
        deadline = attempt;
        if (DataMatrixDecoder.TryDecodeDetailed(rgba, width, height, width * 4, PixelFormat.Rgba32, deadline.Token, out var decoded)) {
            Add(results, seen, SymbolResultFactory.From(decoded, searchRegion));
            return;
        }
        if (options.DirectPartMarking is null || deadline.ShouldStop) return;
        var dpm = options.DirectPartMarking.Clone();
        var variants = DirectPartMarkPreprocessor.CreateVariants(rgba, width, height, dpm, deadline.Token);
        for (var i = 0; i < variants.Count && !deadline.ShouldStop; i++) {
            if (!DataMatrixDecoder.TryDecodeDetailed(variants[i], width, height, width * 4, PixelFormat.Rgba32, deadline.Token, out decoded)) continue;
            Add(results, seen, SymbolResultFactory.From(decoded, searchRegion, dpm.Profile));
            return;
        }
    }

    private static void ScanAztec(
        byte[] rgba,
        int width,
        int height,
        ImageRegion searchRegion,
        ScanOptions options,
        ScanDeadline deadline,
        ISet<SymbolFormat> requested,
        List<DetectedSymbol> results,
        HashSet<string>? seen,
        ref int remainingAttempts) {
        if (!requested.Contains(SymbolFormat.Aztec)) return;
        using var attempt = deadline.CreateAttempt(remainingAttempts--, options.Image?.RecognitionBudgetMilliseconds ?? 0);
        deadline = attempt;
        if (AztecDecoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, deadline.Token, out var text)) {
            Add(results, seen, new DetectedSymbol(SymbolFormat.Aztec, text, searchRegion: searchRegion));
        }
    }

    private static void ScanPdf417(
        byte[] rgba,
        int width,
        int height,
        ImageRegion searchRegion,
        ScanOptions options,
        ScanDeadline deadline,
        ISet<SymbolFormat> requested,
        List<DetectedSymbol> results,
        HashSet<string>? seen,
        ref int remainingAttempts) {
        if (!requested.Contains(SymbolFormat.Pdf417)) return;
        using var attempt = deadline.CreateAttempt(remainingAttempts--, options.Image?.RecognitionBudgetMilliseconds ?? 0);
        deadline = attempt;
        if (Pdf417Decoder.TryDecode(rgba, width, height, width * 4, PixelFormat.Rgba32, deadline.Token, out Pdf417Decoded decoded)) {
            Add(results, seen, SymbolResultFactory.From(decoded, searchRegion));
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
        HashSet<string>? seen,
        ref int remainingAttempts) {
        var expectedTypes = new List<BarcodeType>();
        foreach (var format in requested) {
            var capability = SymbolCapabilities.Get(format);
            if (capability.Family == SymbolFamily.Linear && capability.LegacyBarcodeType.HasValue && capability.CanScanImages) {
                expectedTypes.Add(capability.LegacyBarcodeType.Value);
            }
        }
        if (expectedTypes.Count == 0) return;
        using var attempt = deadline.CreateAttempt(remainingAttempts--, options.Image?.RecognitionBudgetMilliseconds ?? 0);
        deadline = attempt;

        var barcodeOptions = CloneBarcodeOptions(options.Barcode) ?? new BarcodeDecodeOptions { EnableTileScan = options.EnableTileScan, TileGrid = options.TileGrid };
        var classifyDataBarHeight = expectedTypes.Contains(BarcodeType.GS1DataBarTruncated)
            && expectedTypes.Contains(BarcodeType.GS1DataBarOmni);

        if (classifyDataBarHeight) {
            var expected = RequestsEveryDefaultLinearFormat(requested)
                ? (BarcodeType?)null
                : BarcodeType.GS1DataBarTruncated;
            // The unrestricted pass already tries every linear type; only a typed pass
            // shares its family budget with the per-type attempts that follow it.
            using var locatedAttempt = expected.HasValue ? deadline.CreateAttempt(expectedTypes.Count) : null;
            ScanLocatedLinear(
                rgba,
                width,
                height,
                searchRegion,
                options,
                locatedAttempt ?? deadline,
                requested,
                expectedTypes,
                expected,
                barcodeOptions,
                results,
                seen);
            if (!expected.HasValue || ShouldStop(options, deadline, results)) return;
        }

        for (var i = 0; i < expectedTypes.Count && !ShouldStop(options, deadline, results); i++) {
            var expected = expectedTypes[i];
            if (classifyDataBarHeight && (expected == BarcodeType.GS1DataBarTruncated || expected == BarcodeType.GS1DataBarOmni)) continue;
            using var typeAttempt = deadline.CreateAttempt(expectedTypes.Count - i);
            AddLinearResults(rgba, width, height, searchRegion, options, typeAttempt, requested, expectedTypes, results, seen, expected, barcodeOptions);
        }
    }

    private static void AddLinearResults(
        byte[] rgba,
        int width,
        int height,
        ImageRegion searchRegion,
        ScanOptions options,
        ScanDeadline deadline,
        ISet<SymbolFormat> requested,
        List<BarcodeType> expectedTypes,
        List<DetectedSymbol> results,
        HashSet<string>? seen,
        BarcodeType expectedType,
        BarcodeDecodeOptions? barcodeOptions) {
        if (expectedType == BarcodeType.Pharmacode) {
            ScanLocatedLinear(rgba, width, height, searchRegion, options, deadline, requested, expectedTypes,
                expectedType, barcodeOptions, results, seen);
            return;
        }
        if (!BarcodeDecoder.TryDecodeAll(rgba, width, height, width * 4, PixelFormat.Rgba32, out var decoded, expectedType, barcodeOptions, deadline.Token)) return;
        for (var i = 0; i < decoded.Length; i++) {
            var hit = ResolveRequestedLinearIdentity(decoded[i], expectedTypes, rgba, width, height, candidate: null, cancellationToken: deadline.Token);
            if (!SymbolCapabilities.TryFromLegacy(hit.Type, out var format) || !requested.Contains(format)) continue;
            Add(results, seen, SymbolResultFactory.From(hit, searchRegion));
            if (ReachedMaximum(options, results)) return;
        }
    }

    private static void ScanLocatedLinear(
        byte[] rgba,
        int width,
        int height,
        ImageRegion searchRegion,
        ScanOptions options,
        ScanDeadline deadline,
        ISet<SymbolFormat> requested,
        List<BarcodeType> expectedTypes,
        BarcodeType? expected,
        BarcodeDecodeOptions? barcodeOptions,
        List<DetectedSymbol> results,
        HashSet<string>? seen) {
        if (!BarcodeDecoder.TryDecodeAllLocated(
                rgba,
                width,
                height,
                width * 4,
                PixelFormat.Rgba32,
                out var decoded,
                expected,
                barcodeOptions,
                deadline.Token)) return;

        // BarcodeDecoder's public multi-result contract deduplicates by physical type and payload. Preserve
        // that behavior after classifying each located DataBar candidate independently.
        var decodedSeen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < decoded.Length; i++) {
            if (IsBoundaryClippedPharmacode(decoded[i], rgba, width, height, deadline.Token)) continue;
            var hit = ResolveRequestedLinearIdentity(decoded[i].Decoded, expectedTypes, rgba, width, height, decoded[i], deadline.Token);
            var key = hit.Type + "\u001f" + hit.Text;
            if (!decodedSeen.Add(key)) continue;
            if (!SymbolCapabilities.TryFromLegacy(hit.Type, out var format) || !requested.Contains(format)) continue;
            Add(results, seen, SymbolResultFactory.From(hit, searchRegion));
            if (ReachedMaximum(options, results)) return;
        }
    }

    private static bool IsBoundaryClippedPharmacode(BarcodeImageCandidate candidate, byte[] rgba,
        int width, int height, CancellationToken cancellationToken) {
        if (candidate.Decoded.Type != BarcodeType.Pharmacode) return false;
        var region = candidate.SearchRegion;
        if (region.X == 0 && region.Y == 0 && region.Width == width && region.Height == height) return false;

        // Pharmacode has no checksum or start/stop pattern. A tile boundary through a bar can
        // turn part of a different barcode into a valid number. Require background at both ends
        // of this actual sampled scanline, wider than Pharmacode's single-module internal spaces.
        var vertical = candidate.Scanline.IsVertical;
        var position = vertical ? region.X + candidate.Scanline.Position : region.Y + candidate.Scanline.Position;
        var start = vertical ? region.Y : region.X;
        var end = vertical ? region.Bottom : region.Right;
        int Luminance(int at) {
            var x = vertical ? position : at;
            var y = vertical ? at : position;
            var offset = (y * width + x) * 4;
            return (rgba[offset] * 54 + rgba[offset + 1] * 183 + rgba[offset + 2] * 19) >> 8;
        }
        var minimum = 255;
        var maximum = 0;
        for (var at = start; at < end; at++) {
            if ((at & 127) == 0 && cancellationToken.IsCancellationRequested) return true;
            var luminance = Luminance(at);
            minimum = Math.Min(minimum, luminance);
            maximum = Math.Max(maximum, luminance);
        }
        var threshold = (minimum + maximum) / 2;
        if (minimum >= maximum) return true;
        var first = start;
        while (first < end && Luminance(first) >= threshold) first++;
        var last = end - 1;
        while (last > first && Luminance(last) >= threshold) last--;
        var quietPixels = Math.Max(2, 2 * (last - first + 1) / candidate.Scanline.Modules.Length);
        return first - start < quietPixels || end - last - 1 < quietPixels;
    }

    private static BarcodeDecoded ResolveRequestedLinearIdentity(
        BarcodeDecoded decoded,
        List<BarcodeType> expectedTypes,
        byte[] rgba,
        int width,
        int height,
        BarcodeImageCandidate? candidate,
        CancellationToken cancellationToken) {
        // DataBar Omnidirectional and Truncated have the same horizontal module sequence; only bar height
        // distinguishes them. An Omni-only request supplies the caller's physical identity. When both are
        // requested, use the standards-defined 33X Omnidirectional height boundary and otherwise preserve
        // the scanline decoder's conservative Truncated identity.
        if (decoded.Type == BarcodeType.GS1DataBarTruncated
            && expectedTypes.Contains(BarcodeType.GS1DataBarOmni)) {
            if (!expectedTypes.Contains(BarcodeType.GS1DataBarTruncated)
                || candidate is not null
                && DataBar14ImageClassifier.TryIsOmnidirectional(rgba, width, height, candidate, cancellationToken, out var isOmnidirectional)
                && isOmnidirectional) {
                return new BarcodeDecoded(BarcodeType.GS1DataBarOmni, decoded.Text);
            }
        }
        return decoded;
    }

    private static bool RequestsEveryDefaultLinearFormat(ISet<SymbolFormat> requested) {
        for (var i = 0; i < SymbolCapabilities.ImageScannableFormats.Count; i++) {
            var format = SymbolCapabilities.ImageScannableFormats[i];
            var capability = SymbolCapabilities.Get(format);
            if (capability.IsDefaultScanFormat && capability.Family == SymbolFamily.Linear
                && capability.LegacyBarcodeType.HasValue && !requested.Contains(format)) return false;
        }
        return true;
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
        if (options.Qr is null) { result.EnableTileScan = options.EnableTileScan; result.TileGrid = options.TileGrid; }
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

    private static ImageDecodeOptions? ResolveSourceImageDecodeOptions(ScanOptions options) {
        var source = options.Image;
        if (source is null || source.MaxDimension <= 0) return source;

        // Decode at source dimensions so reported regions and geometry remain in the encoded image's
        // coordinate space. ScanEncodedRegion applies MaxDimension immediately before recognition.
        return new ImageDecodeOptions {
            MaxDimension = 0,
            MaxPixels = source.MaxPixels,
            MaxBytes = source.MaxBytes,
            RecognitionBudgetMilliseconds = source.RecognitionBudgetMilliseconds,
            MaxAnimationFrames = source.MaxAnimationFrames,
            MaxAnimationDurationMs = source.MaxAnimationDurationMs,
            MaxAnimationFramePixels = source.MaxAnimationFramePixels,
            MaxDecodedBytes = source.MaxDecodedBytes,
            JpegOptions = source.JpegOptions
        };
    }

    private static List<SymbolFormat> ResolveRequestedFormats(SymbolFormat[]? formats, List<SymbolFormat> unsupported) {
        var requested = new List<SymbolFormat>();
        var seen = new HashSet<SymbolFormat>();
        if (formats is null || formats.Length == 0) {
            for (var i = 0; i < SymbolCapabilities.ImageScannableFormats.Count; i++) {
                var format = SymbolCapabilities.ImageScannableFormats[i];
                if (SymbolCapabilities.Get(format).IsDefaultScanFormat) requested.Add(format);
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

    private static SymbolGeometry MapGeometryToSource(SymbolGeometry geometry, ImageRegion sourceRegion, int decodedWidth, int decodedHeight) {
        if (sourceRegion.X == 0 && sourceRegion.Y == 0 && sourceRegion.Width == decodedWidth && sourceRegion.Height == decodedHeight) return geometry;
        return new SymbolGeometry(
            MapPointToSource(geometry.TopLeft, sourceRegion, decodedWidth, decodedHeight),
            MapPointToSource(geometry.TopRight, sourceRegion, decodedWidth, decodedHeight),
            MapPointToSource(geometry.BottomRight, sourceRegion, decodedWidth, decodedHeight),
            MapPointToSource(geometry.BottomLeft, sourceRegion, decodedWidth, decodedHeight));
    }

    private static SymbolPoint MapPointToSource(SymbolPoint point, ImageRegion sourceRegion, int decodedWidth, int decodedHeight) {
        return new SymbolPoint(
            sourceRegion.X + point.X * sourceRegion.Width / decodedWidth,
            sourceRegion.Y + point.Y * sourceRegion.Height / decodedHeight);
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
        return Result(status, deadline, new List<DetectedSymbol>(), unsupported, failure,
            deadline.CallerCancelled ? ScanCompletionReason.Cancelled : ScanCompletionReason.DeadlineExceeded);
    }

    private static ScanResult Result(
        ScanStatus status,
        ScanDeadline deadline,
        List<DetectedSymbol> symbols,
        List<SymbolFormat> unsupported,
        string? failure = null,
        ScanCompletionReason completionReason = ScanCompletionReason.Completed) {
        return new ScanResult(status, symbols, unsupported, deadline.Elapsed, failure, completionReason);
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
        ValidateTileGrid(options.TileGrid, nameof(options.TileGrid));
        ValidateImageOptions(options.Image);
        if (options.Qr is not null) {
            ValidateTileGrid(options.Qr.TileGrid, nameof(options.Qr.TileGrid));
            if (!Enum.IsDefined(typeof(QrDecodeProfile), options.Qr.Profile) || options.Qr.MaxDimension < 0 ||
                options.Qr.MaxScale < 0 || options.Qr.MaxScale > 8 || options.Qr.BudgetMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(options.Qr));
        }
        if (options.Barcode is not null) {
            ValidateTileGrid(options.Barcode.TileGrid, nameof(options.Barcode.TileGrid));
            if (!Enum.IsDefined(typeof(Code39ChecksumPolicy), options.Barcode.Code39Checksum) ||
                !Enum.IsDefined(typeof(MsiChecksumPolicy), options.Barcode.MsiChecksum) ||
                !Enum.IsDefined(typeof(Code11ChecksumPolicy), options.Barcode.Code11Checksum) ||
                !Enum.IsDefined(typeof(PlesseyChecksumPolicy), options.Barcode.PlesseyChecksum))
                throw new ArgumentOutOfRangeException(nameof(options.Barcode));
        }
        if (options.DirectPartMarking is not null) DirectPartMarkPreprocessor.Validate(options.DirectPartMarking);
    }
}
