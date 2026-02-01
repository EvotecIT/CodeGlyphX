using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Internal;

internal static class DecodeResultHelpers {
    public delegate bool PixelDecodeString(byte[] rgba, int width, int height, CancellationToken token, out string text);
    public delegate bool PixelDecodeWithDiagnostics<TDiag>(byte[] rgba, int width, int height, CancellationToken token, out string text, out TDiag diagnostics);
    public delegate bool PixelDecodeWithStride(byte[] rgba, int width, int height, int stride, CancellationToken token, out string text);
    public delegate bool TryDecodeBytes(byte[] data, ImageDecodeOptions? options, CancellationToken token, out string text);
    public delegate bool TryDecodeBytesWithDiagnostics<TDiag>(byte[] data, ImageDecodeOptions? options, CancellationToken token, out string text, out TDiag diagnostics);
    public delegate bool TryDecodeAllBytes(byte[] data, ImageDecodeOptions? options, CancellationToken token, out string[] texts);
    private delegate bool TryGetRgba(CancellationToken token, out byte[] rgba, out int width, out int height, out string? failure);

    internal const string FailureUnsupportedImageFormat = "Unsupported image format.";

    internal readonly struct DecodeFailureMessages {
        public readonly string FailureCancelled;
        public readonly string FailureDownscale;
        public readonly string FailureNoDecoded;
        public readonly string FailureInvalid;
        public readonly string FailureUnsupported;

        private DecodeFailureMessages(
            string failureCancelled,
            string failureDownscale,
            string failureNoDecoded,
            string failureInvalid,
            string failureUnsupported) {
            FailureCancelled = failureCancelled;
            FailureDownscale = failureDownscale;
            FailureNoDecoded = failureNoDecoded;
            FailureInvalid = failureInvalid;
            FailureUnsupported = failureUnsupported;
        }

        public static DecodeFailureMessages ForPng(
            string failureInvalid,
            string failureCancelled,
            string failureDownscale,
            string failureNoDecoded) {
            return new DecodeFailureMessages(failureCancelled, failureDownscale, failureNoDecoded, failureInvalid, string.Empty);
        }

        public static DecodeFailureMessages ForImage(
            string failureCancelled,
            string failureDownscale,
            string failureUnsupported,
            string failureNoDecoded) {
            return new DecodeFailureMessages(failureCancelled, failureDownscale, failureNoDecoded, string.Empty, failureUnsupported);
        }
    }

    private struct DecodeAllContext {
        public CancellationToken Token;
        public PixelDecodeWithStride Decode;
        public List<string> List;
        public HashSet<string> Seen;
    }

    private struct TileContext {
        public int Width;
        public int Height;
        public int Stride;
        public int Pad;
        public int Grid;
        public int TileW;
        public int TileH;
        public int Tx;
        public int Ty;
        public CancellationToken Token;
    }

    public static bool TryGetImageInfo(ReadOnlySpan<byte> image, out ImageInfo info, out bool formatKnown) {
        formatKnown = ImageReader.TryDetectFormat(image, out var format);
        if (formatKnown && ImageReader.TryReadInfo(image, out info)) return true;
        info = formatKnown ? new ImageInfo(format, 0, 0) : default;
        return false;
    }

    public static ImageInfo EnsureDimensions(ImageInfo info, bool formatKnown, int width, int height) {
        if (info.IsValid) return info;
        var format = formatKnown ? info.Format : ImageFormat.Unknown;
        return new ImageInfo(format, width, height);
    }

    public static DecodeFailureReason FailureForImageRead(ReadOnlySpan<byte> image, bool formatKnown, CancellationToken token) {
        if (token.IsCancellationRequested) return DecodeFailureReason.Cancelled;
        if (image.IsEmpty) return DecodeFailureReason.InvalidInput;
        return formatKnown ? DecodeFailureReason.InvalidInput : DecodeFailureReason.UnsupportedFormat;
    }

    public static DecodeFailureReason FailureForDecode(CancellationToken token) {
        return token.IsCancellationRequested ? DecodeFailureReason.Cancelled : DecodeFailureReason.NoResult;
    }

    public static bool TryCheckImageLimits(ReadOnlySpan<byte> image, ImageDecodeOptions? options, out ImageInfo info, out bool formatKnown, out string? message) {
        message = null;
        _ = TryGetImageInfo(image, out info, out formatKnown);

        var maxBytes = options?.MaxBytes ?? 0;
        if (maxBytes <= 0) maxBytes = ImageReader.MaxImageBytes;
        if (maxBytes > 0 && image.Length > maxBytes) {
            message = GuardMessages.ForBytes("image payload exceeds size limits", image.Length, maxBytes);
            return false;
        }

        var maxPixels = options?.MaxPixels ?? 0;
        if (maxPixels <= 0) maxPixels = ImageReader.MaxPixels;
        if (maxPixels > 0 && info.IsValid) {
            var pixels = (long)info.Width * info.Height;
            if (pixels > maxPixels) {
                message = GuardMessages.ForPixels("image dimensions exceed size limits", info.Width, info.Height, pixels, maxPixels);
                return false;
            }
        }

        return true;
    }

    public static int ResolveMaxBytes(ImageDecodeOptions? options) {
        if (options is not null && options.MaxBytes > 0) return options.MaxBytes;
        return ImageReader.MaxImageBytes;
    }

    public static bool TryReadBinary(string path, ImageDecodeOptions? options, out byte[] data) {
        return RenderIO.TryReadBinary(path, ResolveMaxBytes(options), out data);
    }

    public static bool TryReadBinary(Stream stream, ImageDecodeOptions? options, out byte[] data) {
        return RenderIO.TryReadBinary(stream, ResolveMaxBytes(options), out data);
    }

    public static bool TryReadImageBytes(Stream stream, ImageDecodeOptions? options, CancellationToken cancellationToken, out byte[] data, out bool cancelled) {
        data = Array.Empty<byte>();
        cancelled = false;
        if (cancellationToken.IsCancellationRequested) {
            cancelled = true;
            return false;
        }
        return TryReadBinary(stream, options, out data);
    }

    public static bool TryDecodeImage(ReadOnlySpan<byte> image, ImageDecodeOptions? options, CancellationToken cancellationToken, PixelDecodeString decode, out string text) {
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) { text = string.Empty; return false; }
            if (!ImageReader.TryDecodeRgba32(image, options, out var rgba, out var width, out var height)) { text = string.Empty; return false; }
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) { text = string.Empty; return false; }
            return decode(rgba, width, height, token, out text);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    public static bool TryDecodeImage(Stream stream, ImageDecodeOptions? options, CancellationToken cancellationToken, PixelDecodeString decode, out string text) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) { text = string.Empty; return false; }
            if (!TryReadBinary(stream, options, out var data)) { text = string.Empty; return false; }
            if (!ImageReader.TryDecodeRgba32(data, options, out var rgba, out var width, out var height)) { text = string.Empty; return false; }
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) { text = string.Empty; return false; }
            return decode(rgba, width, height, token, out text);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    public static bool TryDecodeBinaryStream(
        Stream stream,
        ImageDecodeOptions? options,
        CancellationToken cancellationToken,
        TryDecodeBytes decode,
        out string text) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (cancellationToken.IsCancellationRequested) { text = string.Empty; return false; }
        if (!TryReadBinary(stream, options, out var data)) { text = string.Empty; return false; }
        return decode(data, options, cancellationToken, out text);
    }

    public static bool TryDecodeBinaryStreamWithDiagnostics<TDiag>(
        Stream stream,
        ImageDecodeOptions? options,
        CancellationToken cancellationToken,
        string failureInvalid,
        string failureCancelled,
        TryDecodeBytesWithDiagnostics<TDiag> decode,
        out string text,
        out TDiag diagnostics)
        where TDiag : class, IDecodeDiagnostics, new() {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (cancellationToken.IsCancellationRequested) {
            text = string.Empty;
            diagnostics = new TDiag();
            diagnostics.SetFailure(failureCancelled);
            return false;
        }
        if (!TryReadBinary(stream, options, out var data)) {
            text = string.Empty;
            diagnostics = new TDiag();
            diagnostics.SetFailure(failureInvalid);
            return false;
        }
        return decode(data, options, cancellationToken, out text, out diagnostics);
    }

    public static bool TryDecodeImageStreamWithDiagnostics<TDiag>(
        Stream stream,
        ImageDecodeOptions? options,
        CancellationToken cancellationToken,
        string failureInvalid,
        string failureCancelled,
        TryDecodeBytesWithDiagnostics<TDiag> decode,
        out string text,
        out TDiag diagnostics)
        where TDiag : class, IDecodeDiagnostics, new() {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (!TryReadImageBytes(stream, options, cancellationToken, out var data, out var cancelled)) {
            text = string.Empty;
            diagnostics = new TDiag();
            diagnostics.SetFailure(cancelled ? failureCancelled : failureInvalid);
            return false;
        }
        return decode(data, options, cancellationToken, out text, out diagnostics);
    }

    public static bool TryDecodeAllImageStream(
        Stream stream,
        ImageDecodeOptions? options,
        CancellationToken cancellationToken,
        TryDecodeAllBytes decode,
        out string[] texts) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (!TryReadImageBytes(stream, options, cancellationToken, out var data, out _)) { texts = Array.Empty<string>(); return false; }
        return decode(data, options, cancellationToken, out texts);
    }

    private static bool TryDecodeWithDiagnosticsCore<TDiag>(
        ImageDecodeOptions? options,
        CancellationToken cancellationToken,
        DecodeFailureMessages failures,
        TryGetRgba getRgba,
        PixelDecodeWithDiagnostics<TDiag> decode,
        out string text,
        out TDiag diagnostics)
        where TDiag : class, IDecodeDiagnostics, new() {
        var defaultDiagnostics = new TDiag();
        diagnostics = defaultDiagnostics;
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) {
                text = string.Empty;
                diagnostics.SetFailure(failures.FailureCancelled);
                return false;
            }
            if (!getRgba(token, out var rgba, out var width, out var height, out var failure)) {
                text = string.Empty;
                diagnostics.SetFailure(failure ?? failures.FailureCancelled);
                return false;
            }
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) {
                text = string.Empty;
                diagnostics.SetFailure(token.IsCancellationRequested ? failures.FailureCancelled : failures.FailureDownscale);
                return false;
            }
            if (decode(rgba, width, height, token, out text, out var diag)) {
                diagnostics = diag ?? defaultDiagnostics;
                return true;
            }
            diagnostics = diag ?? defaultDiagnostics;
            if (diagnostics.Failure is null) diagnostics.SetFailure(failures.FailureNoDecoded);
            text = string.Empty;
            return false;
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    public static bool TryDecodePngWithDiagnostics<TDiag>(
        byte[] png,
        ImageDecodeOptions? options,
        CancellationToken cancellationToken,
        DecodeFailureMessages failures,
        PixelDecodeWithDiagnostics<TDiag> decode,
        out string text,
        out TDiag diagnostics)
        where TDiag : class, IDecodeDiagnostics, new() {
        if (png is null) throw new ArgumentNullException(nameof(png));
        return TryDecodeWithDiagnosticsCore(
            options,
            cancellationToken,
            failures,
            (CancellationToken token, out byte[] rgba, out int width, out int height, out string? failure) => {
                if (!TryCheckImageLimits(png, options, out _, out _, out var limitMessage)) {
                    failure = limitMessage ?? failures.FailureInvalid;
                    rgba = Array.Empty<byte>();
                    width = 0;
                    height = 0;
                    return false;
                }
                rgba = PngReader.DecodeRgba32(png, out width, out height);
                failure = null;
                return true;
            },
            decode,
            out text,
            out diagnostics);
    }

    public static bool TryDecodeImageWithDiagnostics<TDiag>(
        byte[] image,
        ImageDecodeOptions? options,
        CancellationToken cancellationToken,
        DecodeFailureMessages failures,
        PixelDecodeWithDiagnostics<TDiag> decode,
        out string text,
        out TDiag diagnostics)
        where TDiag : class, IDecodeDiagnostics, new() {
        if (image is null) throw new ArgumentNullException(nameof(image));
        return TryDecodeWithDiagnosticsCore(
            options,
            cancellationToken,
            failures,
            (CancellationToken token, out byte[] rgba, out int width, out int height, out string? failure) => {
                if (!ImageReader.TryDecodeRgba32(image, options, out rgba, out width, out height)) {
                    failure = failures.FailureUnsupported;
                    return false;
                }
                failure = null;
                return true;
            },
            decode,
            out text,
            out diagnostics);
    }

    public static bool TryDecodeAllImage(
        byte[] image,
        ImageDecodeOptions? options,
        CancellationToken cancellationToken,
        PixelDecodeWithStride decode,
        out string[] texts) {
        texts = Array.Empty<string>();
        if (image is null) throw new ArgumentNullException(nameof(image));
        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) return false;
            if (!ImageReader.TryDecodeRgba32(image, options, out var rgba, out var width, out var height)) return false;
            var original = rgba;
            var originalWidth = width;
            var originalHeight = height;
            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) return false;

            var list = new List<string>(4);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var context = new DecodeAllContext {
                Token = token,
                Decode = decode,
                List = list,
                Seen = seen
            };
            var found = CollectAllFromRgba(rgba, width, height, width * 4, context);
            if (!ReferenceEquals(rgba, original) && !token.IsCancellationRequested) {
                found |= CollectAllFromRgba(original, originalWidth, originalHeight, originalWidth * 4, context);
            }

            if (!found) return false;
            texts = list.ToArray();
            return true;
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    private static bool CollectAllFromRgba(
        byte[] rgba,
        int width,
        int height,
        int stride,
        DecodeAllContext context) {
        if (context.Token.IsCancellationRequested) return false;
        var found = false;
        if (context.Decode(rgba, width, height, stride, context.Token, out var text)) {
            found |= AddUnique(context.List, context.Seen, text);
        }
        ScanTiles(rgba, width, height, stride, context.Token, (tile, tw, th, tstride) => {
            if (context.Decode(tile, tw, th, tstride, context.Token, out var value)) {
                found |= AddUnique(context.List, context.Seen, value);
            }
        });
        return found;
    }

    private static bool AddUnique(List<string> list, HashSet<string> seen, string text) {
        if (string.IsNullOrEmpty(text)) return false;
        if (seen.Add(text)) {
            list.Add(text);
            return true;
        }
        return false;
    }

    private static void ScanTiles(byte[] rgba, int width, int height, int stride, CancellationToken token, Action<byte[], int, int, int> onTile) {
        if (!TryGetTileGrid(width, height, stride, out var grid, out var pad, out var tileW, out var tileH)) return;

        for (var ty = 0; ty < grid; ty++) {
            for (var tx = 0; tx < grid; tx++) {
                if (token.IsCancellationRequested) return;
                var context = new TileContext {
                    Width = width,
                    Height = height,
                    Stride = stride,
                    Pad = pad,
                    Grid = grid,
                    TileW = tileW,
                    TileH = tileH,
                    Tx = tx,
                    Ty = ty,
                    Token = token
                };
                if (!TryBuildTile(rgba, context, out var tile, out var tw, out var th, out var tileStride)) {
                    continue;
                }
                onTile(tile, tw, th, tileStride);
            }
        }
    }

    private static bool TryGetTileGrid(int width, int height, int stride, out int grid, out int pad, out int tileW, out int tileH) {
        if (width <= 0 || height <= 0 || stride < width * 4) {
            grid = 0;
            pad = 0;
            tileW = 0;
            tileH = 0;
            return false;
        }
        grid = Math.Max(width, height) >= 720 ? 3 : 2;
        pad = Math.Max(8, Math.Min(width, height) / 40);
        tileW = width / grid;
        tileH = height / grid;
        if (tileW <= 0 || tileH <= 0) return false;
        return true;
    }

    private static bool TryBuildTile(
        byte[] rgba,
        TileContext context,
        out byte[] tile,
        out int tw,
        out int th,
        out int tileStride) {
        var x0 = context.Tx * context.TileW;
        var y0 = context.Ty * context.TileH;
        var x1 = (context.Tx == context.Grid - 1) ? context.Width : (context.Tx + 1) * context.TileW;
        var y1 = (context.Ty == context.Grid - 1) ? context.Height : (context.Ty + 1) * context.TileH;

        x0 = Math.Max(0, x0 - context.Pad);
        y0 = Math.Max(0, y0 - context.Pad);
        x1 = Math.Min(context.Width, x1 + context.Pad);
        y1 = Math.Min(context.Height, y1 + context.Pad);

        tw = x1 - x0;
        th = y1 - y0;
        tileStride = 0;
        tile = Array.Empty<byte>();
        if (tw < 48 || th < 48) return false;

        tileStride = tw * 4;
        tile = new byte[tileStride * th];
        for (var y = 0; y < th; y++) {
            if (context.Token.IsCancellationRequested) return false;
            Buffer.BlockCopy(rgba, (y0 + y) * context.Stride + x0 * 4, tile, y * tileStride, tileStride);
        }
        return true;
    }

    public static DecodeResult<string> DecodeImageResult(ReadOnlySpan<byte> image, ImageDecodeOptions? options, CancellationToken cancellationToken, PixelDecodeString decode) {
        var stopwatch = Stopwatch.StartNew();
        if (!TryCheckImageLimits(image, options, out var info, out var formatKnown, out var limitMessage)) {
            return new DecodeResult<string>(DecodeFailureReason.InvalidInput, info, stopwatch.Elapsed, limitMessage);
        }

        var token = ImageDecodeHelper.ApplyBudget(cancellationToken, options, out var budgetCts, out var budgetScope);
        try {
            if (token.IsCancellationRequested) {
                return new DecodeResult<string>(DecodeFailureReason.Cancelled, info, stopwatch.Elapsed);
            }
            if (!ImageReader.TryDecodeRgba32(image, options, out var rgba, out var width, out var height)) {
                var imageFailure = FailureForImageRead(image, formatKnown, token);
                return new DecodeResult<string>(imageFailure, info, stopwatch.Elapsed);
            }

            info = EnsureDimensions(info, formatKnown, width, height);

            if (!ImageDecodeHelper.TryDownscale(ref rgba, ref width, ref height, options, token)) {
                return new DecodeResult<string>(DecodeFailureReason.Cancelled, info, stopwatch.Elapsed);
            }
            if (decode(rgba, width, height, token, out var text)) {
                return new DecodeResult<string>(text, info, stopwatch.Elapsed);
            }
            var failure = FailureForDecode(token);
            return new DecodeResult<string>(failure, info, stopwatch.Elapsed);
        } catch (Exception ex) {
            return new DecodeResult<string>(DecodeFailureReason.Error, info, stopwatch.Elapsed, ex.Message);
        } finally {
            budgetCts?.Dispose();
            budgetScope?.Dispose();
        }
    }

    public static DecodeResult<string> DecodeImageResult(Stream stream, ImageDecodeOptions? options, CancellationToken cancellationToken, PixelDecodeString decode) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (stream is MemoryStream memory && memory.TryGetBuffer(out var buffer)) {
            return DecodeImageResult(buffer.AsSpan(), options, cancellationToken, decode);
        }
        var maxBytes = options?.MaxBytes > 0 ? options.MaxBytes : ImageReader.MaxImageBytes;
        if (!RenderIO.TryReadBinary(stream, maxBytes, out var data)) {
            return new DecodeResult<string>(DecodeFailureReason.InvalidInput, default, TimeSpan.Zero, "image payload exceeds size limits");
        }
        return DecodeImageResult(data, options, cancellationToken, decode);
    }
}
