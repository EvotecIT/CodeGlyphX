using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX;
using CodeGlyphX.Pdf417.Ec;

#if NET8_0_OR_GREATER
using PixelSpan = System.ReadOnlySpan<byte>;
#else
using PixelSpan = byte[];
#endif

namespace CodeGlyphX.Pdf417;

/// <summary>
/// Decodes PDF417 barcodes.
/// </summary>
public static class Pdf417Decoder {
    private const int StartPatternWidth = 17;
    private const int StopPatternWidth = 18;
    private const int StartPattern = 0x1fea8; // 17 bits
    private const int StopPattern = 0x3fa29;  // 18 bits
    private const int DefaultThreshold = 128;

    private static readonly Dictionary<int, int>[] PatternToCodeword = BuildPatternMaps();

    /// <summary>
    /// Attempts to decode a PDF417 symbol from a module matrix.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out string value) {
        return TryDecode(modules, CancellationToken.None, out value);
    }

    /// <summary>
    /// Attempts to decode a PDF417 symbol from a module matrix, with cancellation.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, CancellationToken cancellationToken, out string value) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }

        if (TryDecodeCore(modules, cancellationToken, out value)) return true;
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
        if (TryDecodeWithStartPattern(modules, cancellationToken, out value)) return true;
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
        var mirror = MirrorX(modules);
        if (TryDecodeCore(mirror, cancellationToken, out value)) return true;
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
        if (TryDecodeWithStartPattern(mirror, cancellationToken, out value)) return true;

        value = string.Empty;
        return false;
    }

    /// <summary>
    /// Attempts to decode a PDF417 symbol from a module matrix, with diagnostics.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out string value, out Pdf417DecodeDiagnostics diagnostics) {
        return TryDecode(modules, CancellationToken.None, out value, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode a PDF417 symbol from a module matrix, with cancellation and diagnostics.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, CancellationToken cancellationToken, out string value, out Pdf417DecodeDiagnostics diagnostics) {
        diagnostics = new Pdf417DecodeDiagnostics();
        return TryDecodeInternal(modules, cancellationToken, diagnostics, out value);
    }

    private static bool TryDecodeInternal(BitMatrix modules, CancellationToken cancellationToken, Pdf417DecodeDiagnostics diagnostics, out string value) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }

        if (TryDecodeCore(modules, cancellationToken, diagnostics, out value)) { diagnostics.Success = true; return true; }
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryDecodeWithStartPattern(modules, cancellationToken, diagnostics, out value)) { diagnostics.Success = true; return true; }
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        diagnostics.MirroredTried = true;
        var mirror = MirrorX(modules);
        if (TryDecodeCore(mirror, cancellationToken, diagnostics, out value)) { diagnostics.Success = true; return true; }
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryDecodeWithStartPattern(mirror, cancellationToken, diagnostics, out value)) { diagnostics.Success = true; return true; }

        value = string.Empty;
        diagnostics.Failure ??= "No PDF417 decoded.";
        return false;
    }

    private static bool TryDecodeCore(BitMatrix modules, CancellationToken cancellationToken, out string value) {
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
        var width = modules.Width;
        var height = modules.Height;

        if (!TryGetDimensions(width, out var cols, out var compact)) {
            value = string.Empty;
            return false;
        }

        var capacity = cols * height;
        var rented = ArrayPool<int>.Shared.Rent(capacity);
        var count = 0;
        var rowWidth = width;

        try {
            for (var rowIndex = 0; rowIndex < height; rowIndex++) {
                if (cancellationToken.IsCancellationRequested) return FailDecode(out value);
                var y = height - 1 - rowIndex;
                var cluster = rowIndex % 3;
                var offset = 0;
                offset += StartPatternWidth;

                if (!TryReadCodeword(modules, y, offset, 17, cluster, out _)) {
                    return FailDecode(out value);
                }
                offset += 17;

                for (var x = 0; x < cols; x++) {
                    if ((x & 31) == 0 && cancellationToken.IsCancellationRequested) return FailDecode(out value);
                    if (!TryReadCodeword(modules, y, offset, 17, cluster, out var cw)) {
                        return FailDecode(out value);
                    }
                    if (count >= rented.Length) {
                        return FailDecode(out value);
                    }
                    rented[count++] = cw;
                    offset += 17;
                }

                if (!compact) {
                    if (!TryReadCodeword(modules, y, offset, 17, cluster, out _)) {
                        return FailDecode(out value);
                    }
                    offset += 17;
                }

                offset += compact ? 1 : StopPatternWidth;
                if (offset != rowWidth) {
                    return FailDecode(out value);
                }
            }

            if (count == 0) {
                return FailDecode(out value);
            }

            var received = new int[count];
            Array.Copy(rented, 0, received, 0, count);

            var total = received.Length;
            var eccCount = 0;
            var corrected = false;

            var ec = new ErrorCorrection();
            for (var level = 0; level <= 8; level++) {
                if (cancellationToken.IsCancellationRequested) return FailDecode(out value);
                var k = 1 << (level + 1);
                if (total <= k) continue;
                var expectedLength = total - k;
                var candidate = (int[])received.Clone();
                if (!ec.Decode(candidate, k)) continue;
                if (candidate[0] != expectedLength) continue;
                received = candidate;
                eccCount = k;
                corrected = true;
                break;
            }

            var lengthDescriptor = received[0];
            if (lengthDescriptor <= 0 || lengthDescriptor > total) {
                return FailDecode(out value);
            }
            if (!corrected) {
                eccCount = total - lengthDescriptor;
                if (IsPowerOfTwo(eccCount) && eccCount is >= 2 and <= 512) {
                    var candidate = (int[])received.Clone();
                    if (ec.Decode(candidate, eccCount)) {
                        received = candidate;
                        lengthDescriptor = received[0];
                    }
                }
            }

            if (eccCount > 0 && lengthDescriptor > total - eccCount) {
                return FailDecode(out value);
            }

            var dataCodewords = new int[lengthDescriptor - 1];
            Array.Copy(received, 1, dataCodewords, 0, dataCodewords.Length);
            var decoded = Pdf417DecodedBitStreamParser.Decode(dataCodewords);
            if (decoded is null) {
                return FailDecode(out value);
            }
            value = decoded;
            return true;
        } finally {
            ArrayPool<int>.Shared.Return(rented);
        }
    }

    private static bool TryDecodeCore(BitMatrix modules, CancellationToken cancellationToken, Pdf417DecodeDiagnostics diagnostics, out string value) {
        diagnostics.AttemptCount++;
        return TryDecodeCore(modules, cancellationToken, out value);
    }

    private static bool FailDecode(out string value) {
        value = string.Empty;
        return false;
    }

    private static bool IsPowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;

#if NET8_0_OR_GREATER
    /// <summary>
    /// Attempts to decode a PDF417 symbol from pixels.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out string value) {
        return TryDecodePixels(pixels, width, height, stride, format, CancellationToken.None, out value);
    }

    /// <summary>
    /// Attempts to decode a PDF417 symbol from pixels, with cancellation.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, CancellationToken cancellationToken, out string value) {
        return TryDecodePixels(pixels, width, height, stride, format, cancellationToken, out value);
    }

    /// <summary>
    /// Attempts to decode a PDF417 symbol from pixels, with diagnostics.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, out string value, out Pdf417DecodeDiagnostics diagnostics) {
        return TryDecode(pixels, width, height, stride, format, CancellationToken.None, out value, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode a PDF417 symbol from pixels, with cancellation and diagnostics.
    /// </summary>
    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat format, CancellationToken cancellationToken, out string value, out Pdf417DecodeDiagnostics diagnostics) {
        diagnostics = new Pdf417DecodeDiagnostics();
        return TryDecodePixels(pixels, width, height, stride, format, cancellationToken, out value, diagnostics);
    }
#endif

    /// <summary>
    /// Attempts to decode a PDF417 symbol from pixels.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, out string value) {
        return TryDecode(pixels, width, height, stride, format, CancellationToken.None, out value);
    }

    /// <summary>
    /// Attempts to decode a PDF417 symbol from pixels, with cancellation.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, CancellationToken cancellationToken, out string value) {
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        return TryDecodePixels(pixels, width, height, stride, format, cancellationToken, out value);
    }

    /// <summary>
    /// Attempts to decode a PDF417 symbol from pixels, with diagnostics.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, out string value, out Pdf417DecodeDiagnostics diagnostics) {
        return TryDecode(pixels, width, height, stride, format, CancellationToken.None, out value, out diagnostics);
    }

    /// <summary>
    /// Attempts to decode a PDF417 symbol from pixels, with cancellation and diagnostics.
    /// </summary>
    public static bool TryDecode(byte[] pixels, int width, int height, int stride, PixelFormat format, CancellationToken cancellationToken, out string value, out Pdf417DecodeDiagnostics diagnostics) {
        diagnostics = new Pdf417DecodeDiagnostics();
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        return TryDecodePixels(pixels, width, height, stride, format, cancellationToken, out value, diagnostics);
    }

    private static bool TryDecodePixels(PixelSpan pixels, int width, int height, int stride, PixelFormat format, CancellationToken cancellationToken, out string value) {
        foreach (var threshold in BuildThresholds(pixels, width, height, stride, format)) {
            if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
            if (TryDecodeFromPixels(pixels, width, height, stride, format, threshold, cancellationToken, out value)) return true;
        }
        value = string.Empty;
        return false;
    }

    private static bool TryDecodePixels(PixelSpan pixels, int width, int height, int stride, PixelFormat format, CancellationToken cancellationToken, out string value, Pdf417DecodeDiagnostics diagnostics) {
        foreach (var threshold in BuildThresholds(pixels, width, height, stride, format)) {
            if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
            if (TryDecodeFromPixels(pixels, width, height, stride, format, threshold, cancellationToken, diagnostics, out value)) {
                diagnostics.Success = true;
                return true;
            }
        }
        value = string.Empty;
        diagnostics.Failure ??= "No PDF417 decoded.";
        return false;
    }

    private static bool TryDecodeFromPixels(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int threshold, CancellationToken cancellationToken, out string value) {
        if (width <= 0 || height <= 0 || stride <= 0) {
            value = string.Empty;
            return false;
        }

        if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
        if (TryFindBoundingBox(pixels, width, height, stride, format, threshold, invert: false, out var box)) {
            if (TryDecodeFromBox(pixels, width, height, stride, format, box, threshold, invert: false, cancellationToken, out value)) return true;
        }

        if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
        if (TryFindBoundingBox(pixels, width, height, stride, format, threshold, invert: true, out box)) {
            if (TryDecodeFromBox(pixels, width, height, stride, format, box, threshold, invert: true, cancellationToken, out value)) return true;
        }

        // Fallback to legacy extractor.
        if (TryExtractModules(pixels, width, height, stride, format, threshold, out var modules)) {
            if (TryDecodeWithRotations(modules, cancellationToken, out value)) return true;
            var trimmed = TrimModuleBorder(modules);
            if (!ReferenceEquals(trimmed, modules) && TryDecodeWithRotations(trimmed, cancellationToken, out value)) return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryDecodeFromPixels(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int threshold, CancellationToken cancellationToken, Pdf417DecodeDiagnostics diagnostics, out string value) {
        if (width <= 0 || height <= 0 || stride <= 0) {
            value = string.Empty;
            diagnostics.Failure ??= "Invalid image size.";
            return false;
        }

        if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryFindBoundingBox(pixels, width, height, stride, format, threshold, invert: false, out var box)) {
            if (TryDecodeFromBox(pixels, width, height, stride, format, box, threshold, invert: false, cancellationToken, diagnostics, out value)) return true;
        }

        if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryFindBoundingBox(pixels, width, height, stride, format, threshold, invert: true, out box)) {
            if (TryDecodeFromBox(pixels, width, height, stride, format, box, threshold, invert: true, cancellationToken, diagnostics, out value)) return true;
        }

        // Fallback to legacy extractor.
        if (TryExtractModules(pixels, width, height, stride, format, threshold, out var modules)) {
            if (TryDecodeWithRotations(modules, cancellationToken, diagnostics, out value)) return true;
            var trimmed = TrimModuleBorder(modules);
            if (!ReferenceEquals(trimmed, modules) && TryDecodeWithRotations(trimmed, cancellationToken, diagnostics, out value)) return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryDecodeFromBox(PixelSpan pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, int threshold, bool invert, CancellationToken cancellationToken, out string value) {
        foreach (var candidate in BuildCandidates(pixels, width, height, stride, format, threshold, box, invert)) {
            if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
            var modules = SampleModules(pixels, width, height, stride, format, box, candidate.WidthModules, candidate.HeightModules, candidate.ModuleSize, threshold, invert);
            if (TryDecodeWithRotations(modules, cancellationToken, out value)) return true;

            var trimmed = TrimModuleBorder(modules);
            if (!ReferenceEquals(trimmed, modules) && TryDecodeWithRotations(trimmed, cancellationToken, out value)) return true;

            if (TryDecodeWithShear(pixels, width, height, stride, format, box, candidate, threshold, invert, cancellationToken, out value)) return true;

#if NET8_0_OR_GREATER
            if (TryDecodeWithPerspective(pixels, width, height, stride, format, box, candidate, threshold, invert, cancellationToken, out value)) return true;
#endif
        }

        value = string.Empty;
        return false;
    }

    private static bool TryDecodeFromBox(PixelSpan pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, int threshold, bool invert, CancellationToken cancellationToken, Pdf417DecodeDiagnostics diagnostics, out string value) {
        foreach (var candidate in BuildCandidates(pixels, width, height, stride, format, threshold, box, invert)) {
            if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
            var modules = SampleModules(pixels, width, height, stride, format, box, candidate.WidthModules, candidate.HeightModules, candidate.ModuleSize, threshold, invert);
            if (TryDecodeWithRotations(modules, cancellationToken, diagnostics, out value)) return true;

            var trimmed = TrimModuleBorder(modules);
            if (!ReferenceEquals(trimmed, modules) && TryDecodeWithRotations(trimmed, cancellationToken, diagnostics, out value)) return true;

            if (TryDecodeWithShear(pixels, width, height, stride, format, box, candidate, threshold, invert, cancellationToken, diagnostics, out value)) return true;

#if NET8_0_OR_GREATER
            if (TryDecodeWithPerspective(pixels, width, height, stride, format, box, candidate, threshold, invert, cancellationToken, diagnostics, out value)) return true;
#endif
        }

        value = string.Empty;
        return false;
    }

    private static List<Candidate> BuildCandidates(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int threshold, BoundingBox box, bool invert) {
        var seen = new HashSet<(int module, int width, int height)>();

        if (TryEstimateModuleSize(pixels, width, height, stride, format, threshold, box, invert, out var estimated)) {
            for (var delta = -2; delta <= 2; delta++) {
                var candidate = estimated + delta;
                if (candidate <= 0) continue;
                AddCandidateFromModuleSize(box, candidate, seen);
            }
        }

        for (var compact = 0; compact <= 1; compact++) {
            var offset = compact == 1 ? 35 : 69;
            for (var cols = 1; cols <= 30; cols++) {
                var widthModules = cols * 17 + offset;
                var moduleSize = (int)Math.Round(box.Width / (double)widthModules);
                if (moduleSize <= 0) continue;
                AddCandidate(box, moduleSize, widthModules, seen);
            }
        }

        var candidates = new List<Candidate>(seen.Count);
        foreach (var entry in seen) {
            candidates.Add(new Candidate(entry.module, entry.width, entry.height));
        }

        return candidates;
    }

    private static void AddCandidateFromModuleSize(BoundingBox box, int moduleSize, HashSet<(int module, int width, int height)> seen) {
        var widthModules = (int)Math.Round(box.Width / (double)moduleSize);
        var heightModules = (int)Math.Round(box.Height / (double)moduleSize);
        if (widthModules <= 0 || heightModules <= 0) return;
        AddCandidate(box, moduleSize, widthModules, seen);
    }

    private static void AddCandidate(BoundingBox box, int moduleSize, int widthModules, HashSet<(int module, int width, int height)> seen) {
        var heightModules = (int)Math.Round(box.Height / (double)moduleSize);
        if (heightModules < 3 || heightModules > 90) return;

        var widthPx = widthModules * moduleSize;
        var heightPx = heightModules * moduleSize;
        if (Math.Abs(widthPx - box.Width) > moduleSize * 4) return;
        if (Math.Abs(heightPx - box.Height) > moduleSize * 4) return;

        if (!TryGetDimensions(widthModules, out var cols, out _)) return;
        if (cols < 1 || cols > 30) return;

        seen.Add((moduleSize, widthModules, heightModules));
    }

    private static BitMatrix SampleModules(PixelSpan pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, int widthModules, int heightModules, int moduleSize, int threshold, bool invert) {
        var modules = new BitMatrix(widthModules, heightModules);
        var totalWidth = widthModules * moduleSize;
        var totalHeight = heightModules * moduleSize;
        var offsetX = box.Left + (box.Width - totalWidth) / 2.0;
        var offsetY = box.Top + (box.Height - totalHeight) / 2.0;

        var half = moduleSize / 2.0;
        for (var y = 0; y < heightModules; y++) {
            var sy = (int)Math.Round(offsetY + (y * moduleSize) + half);
            sy = Clamp(sy, 0, height - 1);
            for (var x = 0; x < widthModules; x++) {
                var sx = (int)Math.Round(offsetX + (x * moduleSize) + half);
                sx = Clamp(sx, 0, width - 1);
                var dark = IsDark(pixels, width, height, stride, format, sx, sy, threshold);
                modules[x, y] = invert ? !dark : dark;
            }
        }

        return modules;
    }

    private static bool TryDecodeWithShear(PixelSpan pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, Candidate candidate, int threshold, bool invert, CancellationToken cancellationToken, out string value) {
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
        if (TryDecodeWithRowAligned(pixels, width, height, stride, format, box, candidate, threshold, invert, cancellationToken, out value)) return true;

        var shearList = new List<double>(12);
        int? leftEdgeMid = null;
        var midRow = box.Top + box.Height / 2;
        if (TryFindLeftEdge(pixels, width, height, stride, format, box.Left, box.Right, midRow, threshold, invert, out var leftEdge)) {
            leftEdgeMid = leftEdge;
        }
        if (TryEstimateShear(pixels, width, height, stride, format, box, candidate, threshold, invert, out var estimated)) {
            shearList.Add(estimated);
            shearList.Add(estimated - 0.12);
            shearList.Add(estimated + 0.12);
            shearList.Add(estimated - 0.24);
            shearList.Add(estimated + 0.24);
        }

        var defaults = new[] { -0.6, -0.4, -0.25, -0.15, -0.08, 0.08, 0.15, 0.25, 0.4, 0.6 };
        for (var i = 0; i < defaults.Length; i++) shearList.Add(defaults[i]);

        for (var i = 0; i < shearList.Count; i++) {
            if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
            var modules = SampleModulesSheared(pixels, width, height, stride, format, box, candidate.WidthModules, candidate.HeightModules, candidate.ModuleSize, threshold, invert, shearList[i], leftEdgeMid);
            if (TryDecodeWithRotations(modules, cancellationToken, out value)) return true;
            var trimmed = TrimModuleBorder(modules);
            if (!ReferenceEquals(trimmed, modules) && TryDecodeWithRotations(trimmed, cancellationToken, out value)) return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryDecodeWithShear(PixelSpan pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, Candidate candidate, int threshold, bool invert, CancellationToken cancellationToken, Pdf417DecodeDiagnostics diagnostics, out string value) {
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryDecodeWithRowAligned(pixels, width, height, stride, format, box, candidate, threshold, invert, cancellationToken, diagnostics, out value)) return true;

        var shearList = new List<double>(12);
        int? leftEdgeMid = null;
        var midRow = box.Top + box.Height / 2;
        if (TryFindLeftEdge(pixels, width, height, stride, format, box.Left, box.Right, midRow, threshold, invert, out var leftEdge)) {
            leftEdgeMid = leftEdge;
        }
        if (TryEstimateShear(pixels, width, height, stride, format, box, candidate, threshold, invert, out var estimated)) {
            shearList.Add(estimated);
            shearList.Add(estimated - 0.12);
            shearList.Add(estimated + 0.12);
            shearList.Add(estimated - 0.24);
            shearList.Add(estimated + 0.24);
        }

        var defaults = new[] { -0.6, -0.4, -0.25, -0.15, -0.08, 0.08, 0.15, 0.25, 0.4, 0.6 };
        for (var i = 0; i < defaults.Length; i++) shearList.Add(defaults[i]);

        for (var i = 0; i < shearList.Count; i++) {
            if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
            var modules = SampleModulesSheared(pixels, width, height, stride, format, box, candidate.WidthModules, candidate.HeightModules, candidate.ModuleSize, threshold, invert, shearList[i], leftEdgeMid);
            if (TryDecodeWithRotations(modules, cancellationToken, diagnostics, out value)) return true;
            var trimmed = TrimModuleBorder(modules);
            if (!ReferenceEquals(trimmed, modules) && TryDecodeWithRotations(trimmed, cancellationToken, diagnostics, out value)) return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryDecodeWithRowAligned(PixelSpan pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, Candidate candidate, int threshold, bool invert, CancellationToken cancellationToken, out string value) {
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
        var modules = SampleModulesRowAligned(pixels, width, height, stride, format, box, candidate.WidthModules, candidate.HeightModules, candidate.ModuleSize, threshold, invert);
        if (TryDecodeWithRotations(modules, cancellationToken, out value)) return true;
        var trimmed = TrimModuleBorder(modules);
        if (!ReferenceEquals(trimmed, modules) && TryDecodeWithRotations(trimmed, cancellationToken, out value)) return true;
        value = string.Empty;
        return false;
    }

    private static bool TryDecodeWithRowAligned(PixelSpan pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, Candidate candidate, int threshold, bool invert, CancellationToken cancellationToken, Pdf417DecodeDiagnostics diagnostics, out string value) {
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        var modules = SampleModulesRowAligned(pixels, width, height, stride, format, box, candidate.WidthModules, candidate.HeightModules, candidate.ModuleSize, threshold, invert);
        if (TryDecodeWithRotations(modules, cancellationToken, diagnostics, out value)) return true;
        var trimmed = TrimModuleBorder(modules);
        if (!ReferenceEquals(trimmed, modules) && TryDecodeWithRotations(trimmed, cancellationToken, diagnostics, out value)) return true;
        value = string.Empty;
        return false;
    }

    private static BitMatrix SampleModulesRowAligned(PixelSpan pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, int widthModules, int heightModules, int moduleSize, int threshold, bool invert) {
        var modules = new BitMatrix(widthModules, heightModules);
        var totalWidth = widthModules * moduleSize;
        var totalHeight = heightModules * moduleSize;
        var half = moduleSize / 2.0;
        var baseOffsetX = box.Left + (box.Width - totalWidth) / 2.0;
        var offsetY = box.Top + (box.Height - totalHeight) / 2.0;
        var maxShift = moduleSize * 4;
        var minRun = Math.Max(2, moduleSize / 2);

        for (var y = 0; y < heightModules; y++) {
            var sy = (int)Math.Round(offsetY + (y * moduleSize) + half);
            sy = Clamp(sy, 0, height - 1);

            var rowOffsetX = baseOffsetX;
            if (TryFindLeftEdgeRun(pixels, width, height, stride, format, box.Left, box.Right, sy, threshold, invert, minRun, out var leftEdge)) {
                rowOffsetX = leftEdge - half;
                var delta = rowOffsetX - baseOffsetX;
                if (delta > maxShift) rowOffsetX = baseOffsetX + maxShift;
                else if (delta < -maxShift) rowOffsetX = baseOffsetX - maxShift;
            }

            for (var x = 0; x < widthModules; x++) {
                var sx = (int)Math.Round(rowOffsetX + (x * moduleSize) + half);
                sx = Clamp(sx, 0, width - 1);
                var dark = IsDark(pixels, width, height, stride, format, sx, sy, threshold);
                modules[x, y] = invert ? !dark : dark;
            }
        }

        return modules;
    }

    private static bool TryEstimateShear(PixelSpan pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, Candidate candidate, int threshold, bool invert, out double shearModulesPerRow) {
        shearModulesPerRow = 0;
        var sampleTop = box.Top + (int)(box.Height * 0.2);
        var sampleBottom = box.Bottom - (int)(box.Height * 0.2);

        if (sampleTop < box.Top) sampleTop = box.Top;
        if (sampleBottom > box.Bottom) sampleBottom = box.Bottom;
        if (sampleTop >= sampleBottom) return false;

        if (!TryFindLeftEdge(pixels, width, height, stride, format, box.Left, box.Right, sampleTop, threshold, invert, out var leftTop)) return false;
        if (!TryFindLeftEdge(pixels, width, height, stride, format, box.Left, box.Right, sampleBottom, threshold, invert, out var leftBottom)) return false;

        var delta = leftBottom - leftTop;
        if (candidate.HeightModules <= 1 || candidate.ModuleSize <= 0) return false;

        shearModulesPerRow = delta / ((candidate.HeightModules - 1) * (double)candidate.ModuleSize);
        return Math.Abs(shearModulesPerRow) > 0.02;
    }

    private static bool TryFindLeftEdge(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int left, int right, int y, int threshold, bool invert, out int xFound) {
        xFound = 0;
        if ((uint)y >= (uint)height) return false;
        var row = y * stride;
        for (var x = left; x <= right; x++) {
            if ((uint)x >= (uint)width) break;
            var dark = IsDarkAt(pixels, row, x, format, threshold);
            if (invert) dark = !dark;
            if (dark) {
                xFound = x;
                return true;
            }
        }
        return false;
    }

    private static bool TryFindLeftEdgeRun(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int left, int right, int y, int threshold, bool invert, int minRun, out int xFound) {
        xFound = 0;
        if ((uint)y >= (uint)height) return false;
        var row = y * stride;
        var run = 0;
        for (var x = left; x <= right; x++) {
            if ((uint)x >= (uint)width) break;
            var dark = IsDarkAt(pixels, row, x, format, threshold);
            if (invert) dark = !dark;
            if (dark) {
                run++;
                if (run >= minRun) {
                    xFound = x - run + 1;
                    return true;
                }
            } else {
                run = 0;
            }
        }
        return false;
    }

#if NET8_0_OR_GREATER
    private static bool TryDecodeWithPerspective(PixelSpan pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, Candidate candidate, int threshold, bool invert, CancellationToken cancellationToken, out string value) {
        value = string.Empty;
        if (cancellationToken.IsCancellationRequested) return false;

        if (!TryFindRowEdges(pixels, width, height, stride, format, threshold, invert, box, candidate, out var topLeft, out var topRight, out var bottomRight, out var bottomLeft)) {
            return false;
        }

        var transform = Qr.QrPerspectiveTransform.QuadrilateralToQuadrilateral(
            0, 0,
            candidate.WidthModules - 1, 0,
            candidate.WidthModules - 1, candidate.HeightModules - 1,
            0, candidate.HeightModules - 1,
            topLeft.x, topLeft.y,
            topRight.x, topRight.y,
            bottomRight.x, bottomRight.y,
            bottomLeft.x, bottomLeft.y);

        var offsets = new (double x, double y)[] {
            (0, 0),
            (0.25, 0),
            (-0.25, 0),
            (0, 0.25),
            (0, -0.25),
            (0.25, 0.25),
            (-0.25, 0.25),
            (0.25, -0.25),
            (-0.25, -0.25),
        };

        for (var i = 0; i < offsets.Length; i++) {
            if (cancellationToken.IsCancellationRequested) return false;
            var modules = SampleModulesPerspective(pixels, width, height, stride, format, candidate.WidthModules, candidate.HeightModules, threshold, invert, transform, offsets[i].x, offsets[i].y);
            if (TryDecodeWithRotations(modules, cancellationToken, out value)) return true;

            var trimmed = TrimModuleBorder(modules);
            if (!ReferenceEquals(trimmed, modules) && TryDecodeWithRotations(trimmed, cancellationToken, out value)) return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryDecodeWithPerspective(PixelSpan pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, Candidate candidate, int threshold, bool invert, CancellationToken cancellationToken, Pdf417DecodeDiagnostics diagnostics, out string value) {
        value = string.Empty;
        if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }

        if (!TryFindRowEdges(pixels, width, height, stride, format, threshold, invert, box, candidate, out var topLeft, out var topRight, out var bottomRight, out var bottomLeft)) {
            return false;
        }

        var transform = Qr.QrPerspectiveTransform.QuadrilateralToQuadrilateral(
            0, 0,
            candidate.WidthModules - 1, 0,
            candidate.WidthModules - 1, candidate.HeightModules - 1,
            0, candidate.HeightModules - 1,
            topLeft.x, topLeft.y,
            topRight.x, topRight.y,
            bottomRight.x, bottomRight.y,
            bottomLeft.x, bottomLeft.y);

        var offsets = new (double x, double y)[] {
            (0, 0),
            (0.25, 0),
            (-0.25, 0),
            (0, 0.25),
            (0, -0.25),
            (0.25, 0.25),
            (-0.25, 0.25),
            (0.25, -0.25),
            (-0.25, -0.25),
        };

        for (var i = 0; i < offsets.Length; i++) {
            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; return false; }
            var modules = SampleModulesPerspective(pixels, width, height, stride, format, candidate.WidthModules, candidate.HeightModules, threshold, invert, transform, offsets[i].x, offsets[i].y);
            if (TryDecodeWithRotations(modules, cancellationToken, diagnostics, out value)) return true;

            var trimmed = TrimModuleBorder(modules);
            if (!ReferenceEquals(trimmed, modules) && TryDecodeWithRotations(trimmed, cancellationToken, diagnostics, out value)) return true;
        }

        value = string.Empty;
        return false;
    }

    private static BitMatrix SampleModulesPerspective(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int widthModules, int heightModules, int threshold, bool invert, Qr.QrPerspectiveTransform transform, double offsetX, double offsetY) {
        var modules = new BitMatrix(widthModules, heightModules);
        for (var y = 0; y < heightModules; y++) {
            for (var x = 0; x < widthModules; x++) {
                transform.Transform(x + 0.5 + offsetX, y + 0.5 + offsetY, out var sx, out var sy);
                if (double.IsNaN(sx) || double.IsNaN(sy)) continue;
                var dark = IsDarkAverage(pixels, width, height, stride, format, sx, sy, threshold);
                modules[x, y] = invert ? !dark : dark;
            }
        }
        return modules;
    }

    private static bool IsDarkAverage(PixelSpan pixels, int width, int height, int stride, PixelFormat format, double x, double y, int threshold) {
        var cx = (int)Math.Round(x);
        var cy = (int)Math.Round(y);
        var sum = 0;
        var count = 0;
        for (var dy = -1; dy <= 1; dy++) {
            var yy = cy + dy;
            if ((uint)yy >= (uint)height) continue;
            var row = yy * stride;
            for (var dx = -1; dx <= 1; dx++) {
                var xx = cx + dx;
                if ((uint)xx >= (uint)width) continue;
                var p = row + xx * 4;
                byte r;
                byte g;
                byte b;
                if (format == PixelFormat.Bgra32) {
                    b = pixels[p + 0];
                    g = pixels[p + 1];
                    r = pixels[p + 2];
                } else {
                    r = pixels[p + 0];
                    g = pixels[p + 1];
                    b = pixels[p + 2];
                }
                var lum = (r * 77 + g * 150 + b * 29) >> 8;
                sum += lum;
                count++;
            }
        }

        if (count == 0) return false;
        var avg = sum / count;
        return avg < threshold;
    }

    private static bool TryFindRowEdges(
        PixelSpan pixels,
        int width,
        int height,
        int stride,
        PixelFormat format,
        int threshold,
        bool invert,
        BoundingBox box,
        Candidate candidate,
        out (double x, double y) topLeft,
        out (double x, double y) topRight,
        out (double x, double y) bottomRight,
        out (double x, double y) bottomLeft) {
        topLeft = topRight = bottomRight = bottomLeft = default;

        var minRun = Math.Max(1, candidate.ModuleSize / 3);
        var topY = box.Top + Math.Max(1, box.Height / 8);
        var bottomY = box.Bottom - Math.Max(1, box.Height / 8);
        var slack = Math.Max(candidate.ModuleSize * 2, box.Height / 6);

        if (!TryFindEdgeAtRowWithSlack(pixels, width, height, stride, format, threshold, invert, box.Left, box.Right, topY, minRun, slack, out var leftTop, out var rightTop, out topY)) {
            if (!TryFindEdgeAtRowWithSlack(pixels, width, height, stride, format, threshold, invert, box.Left, box.Right, topY, minRun, box.Height / 3, out leftTop, out rightTop, out topY)) return false;
        }
        if (!TryFindEdgeAtRowWithSlack(pixels, width, height, stride, format, threshold, invert, box.Left, box.Right, bottomY, minRun, slack, out var leftBottom, out var rightBottom, out bottomY)) {
            if (!TryFindEdgeAtRowWithSlack(pixels, width, height, stride, format, threshold, invert, box.Left, box.Right, bottomY, minRun, box.Height / 3, out leftBottom, out rightBottom, out bottomY)) return false;
        }

        // Fall back to nearby rows if edges are noisy.
        if (Math.Abs(leftTop - leftBottom) > candidate.ModuleSize * 8 || Math.Abs(rightTop - rightBottom) > candidate.ModuleSize * 8) {
            var midY = box.Top + box.Height / 2;
            if (!TryFindEdgeAtRow(pixels, width, height, stride, format, threshold, invert, box.Left, box.Right, midY, minRun, out var leftMid, out var rightMid)) return false;
            leftTop = (leftTop + leftMid) / 2.0;
            rightTop = (rightTop + rightMid) / 2.0;
            leftBottom = (leftBottom + leftMid) / 2.0;
            rightBottom = (rightBottom + rightMid) / 2.0;
        }

        topLeft = (leftTop, topY);
        topRight = (rightTop, topY);
        bottomLeft = (leftBottom, bottomY);
        bottomRight = (rightBottom, bottomY);
        return true;
    }

    private static bool TryFindEdgeAtRowWithSlack(
        PixelSpan pixels,
        int width,
        int height,
        int stride,
        PixelFormat format,
        int threshold,
        bool invert,
        int left,
        int right,
        int y,
        int minRun,
        int slack,
        out double leftEdge,
        out double rightEdge,
        out int yUsed) {
        leftEdge = 0;
        rightEdge = 0;
        yUsed = y;
        var start = Math.Max(0, y - slack);
        var end = Math.Min(height - 1, y + slack);
        for (var yy = start; yy <= end; yy++) {
            if (TryFindEdgeAtRow(pixels, width, height, stride, format, threshold, invert, left, right, yy, minRun, out leftEdge, out rightEdge)) {
                yUsed = yy;
                return true;
            }
        }
        return false;
    }

    private static bool TryFindEdgeAtRow(
        PixelSpan pixels,
        int width,
        int height,
        int stride,
        PixelFormat format,
        int threshold,
        bool invert,
        int left,
        int right,
        int y,
        int minRun,
        out double leftEdge,
        out double rightEdge) {
        leftEdge = 0;
        rightEdge = 0;
        if ((uint)y >= (uint)height) return false;

        if (!TryFindLeftEdgeRun(pixels, width, height, stride, format, left, right, y, threshold, invert, minRun, out var leftX) ||
            !TryFindRightEdgeRun(pixels, width, height, stride, format, left, right, y, threshold, invert, minRun, out var rightX)) {
            return TryFindEdgeAtRowLoose(pixels, width, height, stride, format, threshold, invert, left, right, y, out leftEdge, out rightEdge);
        }

        leftEdge = leftX;
        rightEdge = rightX;
        return rightEdge > leftEdge;
    }

    private static bool TryFindEdgeAtRowLoose(
        PixelSpan pixels,
        int width,
        int height,
        int stride,
        PixelFormat format,
        int threshold,
        bool invert,
        int left,
        int right,
        int y,
        out double leftEdge,
        out double rightEdge) {
        leftEdge = 0;
        rightEdge = 0;
        if ((uint)y >= (uint)height) return false;
        var row = y * stride;
        var foundLeft = false;
        for (var x = left; x <= right; x++) {
            if ((uint)x >= (uint)width) continue;
            var dark = IsDarkAt(pixels, row, x, format, threshold);
            if (invert) dark = !dark;
            if (dark) {
                leftEdge = x;
                foundLeft = true;
                break;
            }
        }
        if (!foundLeft) return false;
        for (var x = right; x >= left; x--) {
            if ((uint)x >= (uint)width) continue;
            var dark = IsDarkAt(pixels, row, x, format, threshold);
            if (invert) dark = !dark;
            if (dark) {
                rightEdge = x;
                break;
            }
        }

        return rightEdge > leftEdge;
    }

    private static bool TryFindRightEdgeRun(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int left, int right, int y, int threshold, bool invert, int minRun, out int xFound) {
        xFound = 0;
        if ((uint)y >= (uint)height) return false;
        var row = y * stride;
        var run = 0;
        for (var x = right; x >= left; x--) {
            if ((uint)x >= (uint)width) continue;
            var dark = IsDarkAt(pixels, row, x, format, threshold);
            if (invert) dark = !dark;
            if (dark) {
                run++;
                if (run >= minRun) {
                    xFound = x + run - 1;
                    return true;
                }
            } else {
                run = 0;
            }
        }
        return false;
    }
#endif

    private static BitMatrix SampleModulesSheared(PixelSpan pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, int widthModules, int heightModules, int moduleSize, int threshold, bool invert, double shearModulesPerRow, int? leftEdgeMid) {
        var modules = new BitMatrix(widthModules, heightModules);
        var totalWidth = widthModules * moduleSize;
        var totalHeight = heightModules * moduleSize;
        var half = moduleSize / 2.0;
        var offsetX = leftEdgeMid.HasValue
            ? leftEdgeMid.Value - half
            : box.Left + (box.Width - totalWidth) / 2.0;
        var offsetY = box.Top + (box.Height - totalHeight) / 2.0;

        var midRow = (heightModules - 1) / 2.0;

        for (var y = 0; y < heightModules; y++) {
            var rowShift = (y - midRow) * shearModulesPerRow * moduleSize;
            var sy = (int)Math.Round(offsetY + (y * moduleSize) + half);
            sy = Clamp(sy, 0, height - 1);
            for (var x = 0; x < widthModules; x++) {
                var sx = (int)Math.Round(offsetX + rowShift + (x * moduleSize) + half);
                sx = Clamp(sx, 0, width - 1);
                var dark = IsDark(pixels, width, height, stride, format, sx, sy, threshold);
                modules[x, y] = invert ? !dark : dark;
            }
        }

        return modules;
    }

    private static bool TryGetDimensions(int width, out int cols, out bool compact) {
        if (width >= 69 && (width - 69) % 17 == 0) {
            cols = (width - 69) / 17;
            compact = false;
            return cols > 0;
        }
        if (width >= 35 && (width - 35) % 17 == 0) {
            cols = (width - 35) / 17;
            compact = true;
            return cols > 0;
        }
        cols = 0;
        compact = false;
        return false;
    }

    private static bool TryDecodeWithRotations(BitMatrix modules, CancellationToken cancellationToken, out string value) {
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
        if (TryDecode(modules, cancellationToken, out value)) return true;
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
        if (TryDecode(Rotate90(modules), cancellationToken, out value)) return true;
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
        if (TryDecode(Rotate180(modules), cancellationToken, out value)) return true;
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
        if (TryDecode(Rotate270(modules), cancellationToken, out value)) return true;
        value = string.Empty;
        return false;
    }

    private static bool TryDecodeWithRotations(BitMatrix modules, CancellationToken cancellationToken, Pdf417DecodeDiagnostics diagnostics, out string value) {
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryDecodeInternal(modules, cancellationToken, diagnostics, out value)) return true;
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryDecodeInternal(Rotate90(modules), cancellationToken, diagnostics, out value)) return true;
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryDecodeInternal(Rotate180(modules), cancellationToken, diagnostics, out value)) return true;
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryDecodeInternal(Rotate270(modules), cancellationToken, diagnostics, out value)) return true;
        value = string.Empty;
        return false;
    }

    private static bool TryDecodeWithStartPattern(BitMatrix modules, CancellationToken cancellationToken, out string value) {
        var height = modules.Height;
        if (height == 0) {
            value = string.Empty;
            return false;
        }

        var offsets = new Dictionary<int, OffsetScore>();
        var rows = new[] { height / 2, height / 3, (height * 2) / 3 };
        for (var i = 0; i < rows.Length; i++) {
            if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
            var row = rows[i];
            if ((uint)row >= (uint)height) continue;
            foreach (var hit in FindPatternOffsets(modules, row, StartPattern, StartPatternWidth, maxErrors: 2)) {
                if (!offsets.TryGetValue(hit.Offset, out var score)) {
                    score = new OffsetScore { Count = 0, MinErrors = hit.Errors };
                }
                score.Count++;
                if (hit.Errors < score.MinErrors) score.MinErrors = hit.Errors;
                offsets[hit.Offset] = score;
            }
        }

        if (offsets.Count == 0) {
            value = string.Empty;
            return false;
        }

        var ordered = new List<OffsetCandidate>(offsets.Count);
        foreach (var kvp in offsets) {
            ordered.Add(new OffsetCandidate(kvp.Key, kvp.Value.Count, kvp.Value.MinErrors));
        }
        ordered.Sort(OffsetCandidateComparer.Instance);

        for (var i = 0; i < ordered.Count; i++) {
            if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
            if (TryDecodeWithOffset(modules, ordered[i].Offset, cancellationToken, out value)) return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryDecodeWithStartPattern(BitMatrix modules, CancellationToken cancellationToken, Pdf417DecodeDiagnostics diagnostics, out string value) {
        var height = modules.Height;
        if (height == 0) {
            value = string.Empty;
            diagnostics.Failure ??= "Invalid module size.";
            return false;
        }

        var offsets = new Dictionary<int, OffsetScore>();
        var rows = new[] { height / 2, height / 3, (height * 2) / 3 };
        for (var i = 0; i < rows.Length; i++) {
            if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
            var row = rows[i];
            if ((uint)row >= (uint)height) continue;
            foreach (var hit in FindPatternOffsets(modules, row, StartPattern, StartPatternWidth, maxErrors: 2)) {
                if (!offsets.TryGetValue(hit.Offset, out var score)) {
                    score = new OffsetScore { Count = 0, MinErrors = hit.Errors };
                }
                score.Count++;
                if (hit.Errors < score.MinErrors) score.MinErrors = hit.Errors;
                offsets[hit.Offset] = score;
            }
        }

        if (offsets.Count == 0) {
            value = string.Empty;
            diagnostics.Failure ??= "Start pattern not found.";
            return false;
        }

        var ordered = new List<OffsetCandidate>(offsets.Count);
        foreach (var kvp in offsets) {
            ordered.Add(new OffsetCandidate(kvp.Key, kvp.Value.Count, kvp.Value.MinErrors));
        }
        ordered.Sort(OffsetCandidateComparer.Instance);
        diagnostics.StartPatternCandidates += ordered.Count;

        for (var i = 0; i < ordered.Count; i++) {
            if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
            diagnostics.StartPatternAttempts++;
            if (TryDecodeWithOffset(modules, ordered[i].Offset, cancellationToken, diagnostics, out value)) return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryDecodeWithOffset(BitMatrix modules, int startOffset, CancellationToken cancellationToken, out string value) {
        var maxWidth = modules.Width - startOffset;
        if (maxWidth < StartPatternWidth + 17 + 17 + 1) {
            value = string.Empty;
            return false;
        }

        for (var compact = 0; compact <= 1; compact++) {
            if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
            var baseOffset = compact == 1 ? 35 : 69;
            for (var cols = 1; cols <= 30; cols++) {
                if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
                var widthModules = cols * 17 + baseOffset;
                if (widthModules > maxWidth) break;

                var cropped = CropColumns(modules, startOffset, widthModules);
                if (TryDecodeCore(cropped, cancellationToken, out value)) return true;
            }
        }

        value = string.Empty;
        return false;
    }

    private static bool TryDecodeWithOffset(BitMatrix modules, int startOffset, CancellationToken cancellationToken, Pdf417DecodeDiagnostics diagnostics, out string value) {
        var maxWidth = modules.Width - startOffset;
        if (maxWidth < StartPatternWidth + 17 + 17 + 1) {
            value = string.Empty;
            diagnostics.Failure ??= "Invalid start offset.";
            return false;
        }

        for (var compact = 0; compact <= 1; compact++) {
            if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
            var baseOffset = compact == 1 ? 35 : 69;
            for (var cols = 1; cols <= 30; cols++) {
                if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
                var widthModules = cols * 17 + baseOffset;
                if (widthModules > maxWidth) break;

                var cropped = CropColumns(modules, startOffset, widthModules);
                if (TryDecodeCore(cropped, cancellationToken, diagnostics, out value)) return true;
            }
        }

        value = string.Empty;
        return false;
    }

    private static IEnumerable<OffsetMatch> FindPatternOffsets(BitMatrix modules, int row, int pattern, int length, int maxErrors) {
        var width = modules.Width;
        if (length <= 0 || width < length) yield break;

        for (var x = 0; x <= width - length; x++) {
            var bits = 0;
            for (var i = 0; i < length; i++) {
                bits = (bits << 1) | (modules[x + i, row] ? 1 : 0);
            }
            var diff = bits ^ pattern;
            var errors = PopCount(diff);
            if (errors <= maxErrors) yield return new OffsetMatch(x, errors);
        }
    }

    private readonly struct OffsetMatch {
        public int Offset { get; }
        public int Errors { get; }

        public OffsetMatch(int offset, int errors) {
            Offset = offset;
            Errors = errors;
        }
    }

    private struct OffsetScore {
        public int Count;
        public int MinErrors;
    }

    private readonly struct OffsetCandidate {
        public int Offset { get; }
        public int Count { get; }
        public int Errors { get; }

        public OffsetCandidate(int offset, int count, int errors) {
            Offset = offset;
            Count = count;
            Errors = errors;
        }
    }

    private sealed class OffsetCandidateComparer : IComparer<OffsetCandidate> {
        public static readonly OffsetCandidateComparer Instance = new();
        public int Compare(OffsetCandidate x, OffsetCandidate y) {
            var cmp = x.Errors.CompareTo(y.Errors);
            if (cmp != 0) return cmp;
            cmp = y.Count.CompareTo(x.Count);
            if (cmp != 0) return cmp;
            return x.Offset.CompareTo(y.Offset);
        }
    }

    private static int PopCount(int value) {
        var count = 0;
        while (value != 0) {
            count += value & 1;
            value >>= 1;
        }
        return count;
    }

    private static BitMatrix CropColumns(BitMatrix modules, int left, int width) {
        var height = modules.Height;
        var cropped = new BitMatrix(width, height);
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                cropped[x, y] = modules[left + x, y];
            }
        }
        return cropped;
    }

    private static BitMatrix TrimModuleBorder(BitMatrix modules) {
        var width = modules.Width;
        var height = modules.Height;
        if (width <= 2 || height <= 2) return modules;

        var left = 0;
        var right = width - 1;
        var top = 0;
        var bottom = height - 1;

        var rowThreshold = Math.Max(1, width / 40);
        var colThreshold = Math.Max(1, height / 40);

        while (top <= bottom && CountDarkRow(modules, top, left, right) <= rowThreshold) top++;
        while (top <= bottom && CountDarkRow(modules, bottom, left, right) <= rowThreshold) bottom--;
        while (left <= right && CountDarkCol(modules, left, top, bottom) <= colThreshold) left++;
        while (left <= right && CountDarkCol(modules, right, top, bottom) <= colThreshold) right--;

        if (left <= 0 && right >= width - 1 && top <= 0 && bottom >= height - 1) return modules;
        if (right < left || bottom < top) return modules;

        var trimmed = new BitMatrix(right - left + 1, bottom - top + 1);
        for (var y = top; y <= bottom; y++) {
            for (var x = left; x <= right; x++) {
                trimmed[x - left, y - top] = modules[x, y];
            }
        }

        return trimmed;
    }

    private static int CountDarkRow(BitMatrix modules, int y, int left, int right) {
        var count = 0;
        for (var x = left; x <= right; x++) {
            if (modules[x, y]) count++;
        }
        return count;
    }

    private static int CountDarkCol(BitMatrix modules, int x, int top, int bottom) {
        var count = 0;
        for (var y = top; y <= bottom; y++) {
            if (modules[x, y]) count++;
        }
        return count;
    }

    private static bool TryReadCodeword(BitMatrix modules, int row, int offset, int length, int cluster, out int codeword) {
        var pattern = 0;
        for (var i = 0; i < length; i++) {
            pattern <<= 1;
            if (modules[offset + i, row]) pattern |= 1;
        }
        if (PatternToCodeword[cluster].TryGetValue(pattern, out var cw)) {
            codeword = cw;
            return true;
        }
        codeword = 0;
        return false;
    }

    private static Dictionary<int, int>[] BuildPatternMaps() {
        var maps = new Dictionary<int, int>[3];
        for (var i = 0; i < 3; i++) {
            var map = new Dictionary<int, int>(Pdf417CodewordTable.Table[i].Length);
            for (var cw = 0; cw < Pdf417CodewordTable.Table[i].Length; cw++) {
                map[Pdf417CodewordTable.Table[i][cw]] = cw;
            }
            maps[i] = map;
        }
        return maps;
    }

    private static bool TryExtractModules(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int threshold, out BitMatrix modules) {
        modules = null!;
        if (width <= 0 || height <= 0 || stride <= 0) return false;

        var invert = false;
        if (!TryFindBoundingBox(pixels, width, height, stride, format, threshold, invert: false, out var box)) {
            if (!TryFindBoundingBox(pixels, width, height, stride, format, threshold, invert: true, out box)) return false;
            invert = true;
        }

        if (box.Width <= 1 || box.Height <= 1) return false;
        if (!TryEstimateModuleSize(pixels, width, height, stride, format, threshold, box, invert, out var moduleSize)) return false;

        var cols = (int)Math.Round((double)box.Width / moduleSize);
        var rows = (int)Math.Round((double)box.Height / moduleSize);
        if (cols <= 0 || rows <= 0) return false;

        modules = new BitMatrix(cols, rows);
        var half = moduleSize / 2.0;
        for (var y = 0; y < rows; y++) {
            var sy = (int)Math.Round(box.Top + (y * moduleSize) + half);
            sy = Clamp(sy, 0, height - 1);
            for (var x = 0; x < cols; x++) {
                var sx = (int)Math.Round(box.Left + (x * moduleSize) + half);
                sx = Clamp(sx, 0, width - 1);
                var dark = IsDark(pixels, width, height, stride, format, sx, sy, threshold);
                modules[x, y] = invert ? !dark : dark;
            }
        }

        return true;
    }

    private static bool TryEstimateModuleSize(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int threshold, BoundingBox box, bool invert, out int moduleSize) {
        moduleSize = 0;
        var midY = box.Top + box.Height / 2;
        var midX = box.Left + box.Width / 2;

        if (!TryFindMinRun(pixels, width, height, stride, format, threshold, box.Left, box.Right, midY, horizontal: true, invert, out var hMin)) return false;
        if (!TryFindMinRun(pixels, width, height, stride, format, threshold, box.Top, box.Bottom, midX, horizontal: false, invert, out var vMin)) return false;

        moduleSize = Math.Min(hMin, vMin);
        return moduleSize > 0;
    }

    private static bool TryFindMinRun(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int threshold, int start, int end, int fixedPos, bool horizontal, bool invert, out int minRun) {
        minRun = int.MaxValue;
        var prev = false;
        var run = 0;
        var sawAny = false;

        for (var i = start; i <= end; i++) {
            var x = horizontal ? i : fixedPos;
            var y = horizontal ? fixedPos : i;
            var dark = IsDark(pixels, width, height, stride, format, x, y, threshold);
            var bit = invert ? !dark : dark;

            if (!sawAny) {
                prev = bit;
                run = 1;
                sawAny = true;
                continue;
            }

            if (bit == prev) {
                run++;
            } else {
                if (run > 0 && run < minRun) minRun = run;
                prev = bit;
                run = 1;
            }
        }

        if (run > 0 && run < minRun) minRun = run;
        return minRun != int.MaxValue;
    }

    private static bool TryFindBoundingBox(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int threshold, bool invert, out BoundingBox box) {
        var left = width;
        var right = -1;
        var top = height;
        var bottom = -1;

        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var dark = IsDarkAt(pixels, row, x, format, threshold);
                if (invert) dark = !dark;
                if (!dark) continue;

                if (x < left) left = x;
                if (x > right) right = x;
                if (y < top) top = y;
                if (y > bottom) bottom = y;
            }
        }

        if (right < left || bottom < top) {
            box = default;
            return false;
        }

        var found = new BoundingBox(left, top, right, bottom);
        var trimmed = TrimBoundingBox(pixels, width, height, stride, format, threshold, found, invert);
        box = trimmed.Width >= 3 && trimmed.Height >= 3 ? trimmed : found;
        return true;
    }

    private static BoundingBox TrimBoundingBox(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int threshold, BoundingBox box, bool invert) {
        var left = box.Left;
        var right = box.Right;
        var top = box.Top;
        var bottom = box.Bottom;

        var rowThreshold = Math.Max(2, (right - left + 1) / 40);
        var colThreshold = Math.Max(2, (bottom - top + 1) / 40);

        while (top <= bottom && CountDarkRow(pixels, width, height, stride, format, threshold, left, right, top, invert) <= rowThreshold) top++;
        while (top <= bottom && CountDarkRow(pixels, width, height, stride, format, threshold, left, right, bottom, invert) <= rowThreshold) bottom--;
        while (left <= right && CountDarkCol(pixels, width, height, stride, format, threshold, left, top, bottom, invert) <= colThreshold) left++;
        while (left <= right && CountDarkCol(pixels, width, height, stride, format, threshold, right, top, bottom, invert) <= colThreshold) right--;

        if (right < left || bottom < top) return box;
        return new BoundingBox(left, top, right, bottom);
    }

    private static int CountDarkRow(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int threshold, int left, int right, int y, bool invert) {
        if ((uint)y >= (uint)height) return 0;
        var row = y * stride;
        var count = 0;
        for (var x = left; x <= right; x++) {
            if ((uint)x >= (uint)width) continue;
            var dark = IsDarkAt(pixels, row, x, format, threshold);
            if (invert) dark = !dark;
            if (dark) count++;
        }

        return count;
    }

    private static int CountDarkCol(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int threshold, int x, int top, int bottom, bool invert) {
        if ((uint)x >= (uint)width) return 0;
        var count = 0;
        for (var y = top; y <= bottom; y++) {
            if ((uint)y >= (uint)height) continue;
            var row = y * stride;
            var dark = IsDarkAt(pixels, row, x, format, threshold);
            if (invert) dark = !dark;
            if (dark) count++;
        }

        return count;
    }

    private static bool IsDark(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int x, int y, int threshold) {
        if ((uint)x >= (uint)width || (uint)y >= (uint)height) return false;
        var row = y * stride;
        return IsDarkAt(pixels, row, x, format, threshold);
    }

    private static bool IsDarkAt(PixelSpan pixels, int row, int x, PixelFormat format, int threshold) {
        var p = row + x * 4;
        byte r;
        byte g;
        byte b;
        if (format == PixelFormat.Bgra32) {
            b = pixels[p + 0];
            g = pixels[p + 1];
            r = pixels[p + 2];
        } else {
            r = pixels[p + 0];
            g = pixels[p + 1];
            b = pixels[p + 2];
        }

        var lum = (r * 77 + g * 150 + b * 29) >> 8;
        return lum < threshold;
    }

    private static IEnumerable<int> BuildThresholds(PixelSpan pixels, int width, int height, int stride, PixelFormat format) {
        var list = new List<int>(4) { DefaultThreshold };
        if (TryGetLuminanceRange(pixels, width, height, stride, format, out var min, out var max)) {
            var range = max - min;
            if (range > 8) {
                list.Add(min + range / 2);
                list.Add(min + range / 3);
                list.Add(min + (range * 2) / 3);
            }
        }

        for (var i = 0; i < list.Count; i++) {
            var t = list[i];
            if (t < 0) t = 0;
            if (t > 255) t = 255;
            list[i] = t;
        }

        list.Sort();
        for (var i = list.Count - 1; i > 0; i--) {
            if (list[i] == list[i - 1]) list.RemoveAt(i);
        }

        return list;
    }

    private static bool TryGetLuminanceRange(PixelSpan pixels, int width, int height, int stride, PixelFormat format, out int min, out int max) {
        min = 255;
        max = 0;

        if (width <= 0 || height <= 0 || stride <= 0) return false;
        var stepX = Math.Max(1, width / 160);
        var stepY = Math.Max(1, height / 160);

        for (var y = 0; y < height; y += stepY) {
            var row = y * stride;
            for (var x = 0; x < width; x += stepX) {
                var p = row + x * 4;
                byte r;
                byte g;
                byte b;
                if (format == PixelFormat.Bgra32) {
                    b = pixels[p + 0];
                    g = pixels[p + 1];
                    r = pixels[p + 2];
                } else {
                    r = pixels[p + 0];
                    g = pixels[p + 1];
                    b = pixels[p + 2];
                }
                var lum = (r * 77 + g * 150 + b * 29) >> 8;
                if (lum < min) min = lum;
                if (lum > max) max = lum;
            }
        }

        return max >= min;
    }

    private static BitMatrix Rotate90(BitMatrix matrix) {
        var result = new BitMatrix(matrix.Height, matrix.Width);
        for (var y = 0; y < matrix.Height; y++) {
            for (var x = 0; x < matrix.Width; x++) {
                result[matrix.Height - 1 - y, x] = matrix[x, y];
            }
        }
        return result;
    }

    private static BitMatrix Rotate180(BitMatrix matrix) {
        var result = new BitMatrix(matrix.Width, matrix.Height);
        for (var y = 0; y < matrix.Height; y++) {
            for (var x = 0; x < matrix.Width; x++) {
                result[matrix.Width - 1 - x, matrix.Height - 1 - y] = matrix[x, y];
            }
        }
        return result;
    }

    private static BitMatrix Rotate270(BitMatrix matrix) {
        var result = new BitMatrix(matrix.Height, matrix.Width);
        for (var y = 0; y < matrix.Height; y++) {
            for (var x = 0; x < matrix.Width; x++) {
                result[y, matrix.Width - 1 - x] = matrix[x, y];
            }
        }
        return result;
    }

    private static BitMatrix MirrorX(BitMatrix matrix) {
        var result = new BitMatrix(matrix.Width, matrix.Height);
        for (var y = 0; y < matrix.Height; y++) {
            for (var x = 0; x < matrix.Width; x++) {
                result[matrix.Width - 1 - x, y] = matrix[x, y];
            }
        }
        return result;
    }

    private static int Clamp(int value, int min, int max) {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private readonly struct BoundingBox {
        public int Left { get; }
        public int Top { get; }
        public int Right { get; }
        public int Bottom { get; }
        public int Width => Right - Left + 1;
        public int Height => Bottom - Top + 1;

        public BoundingBox(int left, int top, int right, int bottom) {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }

    private readonly struct Candidate {
        public int ModuleSize { get; }
        public int WidthModules { get; }
        public int HeightModules { get; }

        public Candidate(int moduleSize, int widthModules, int heightModules) {
            ModuleSize = moduleSize;
            WidthModules = widthModules;
            HeightModules = heightModules;
        }
    }
}
