using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX;
using CodeGlyphX.Internal;
using CodeGlyphX.Pdf417.Ec;

#if NET8_0_OR_GREATER
using PixelSpan = System.ReadOnlySpan<byte>;
#else
using PixelSpan = byte[];
#endif

namespace CodeGlyphX.Pdf417;

public static partial class Pdf417Decoder {
    private static bool TryDecodeInternal(BitMatrix modules, CancellationToken cancellationToken, Pdf417DecodeDiagnostics diagnostics, out string value) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }

        if (TryDecodeCore(modules, cancellationToken, diagnostics, out value)) { diagnostics.Success = true; return true; }
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryDecodeWithStartPattern(modules, cancellationToken, diagnostics, out value)) { diagnostics.Success = true; return true; }
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        diagnostics.MirroredTried = true;
        var mirror = MirrorX(modules);
        if (TryDecodeCore(mirror, cancellationToken, diagnostics, out value)) { diagnostics.Success = true; return true; }
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryDecodeWithStartPattern(mirror, cancellationToken, diagnostics, out value)) { diagnostics.Success = true; return true; }

        value = string.Empty;
        diagnostics.Failure ??= "No PDF417 decoded.";
        return false;
    }

    private static bool TryDecodeCore(BitMatrix modules, CancellationToken cancellationToken, out string value) {
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
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
                if (DecodeBudget.ShouldAbort(cancellationToken)) return FailDecode(out value);
                var y = height - 1 - rowIndex;
                var cluster = rowIndex % 3;
                var offset = 0;
                offset += StartPatternWidth;

                if (!TryReadCodeword(modules, y, offset, 17, cluster, out _)) {
                    return FailDecode(out value);
                }
                offset += 17;

                for (var x = 0; x < cols; x++) {
                    if ((x & 31) == 0 && DecodeBudget.ShouldAbort(cancellationToken)) return FailDecode(out value);
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
                if (DecodeBudget.ShouldAbort(cancellationToken)) return FailDecode(out value);
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
            if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
            if (TryDecodeFromPixels(pixels, width, height, stride, format, threshold, cancellationToken, out value)) return true;
        }
        value = string.Empty;
        return false;
    }

    private static bool TryDecodePixels(PixelSpan pixels, int width, int height, int stride, PixelFormat format, CancellationToken cancellationToken, out string value, Pdf417DecodeDiagnostics diagnostics) {
        foreach (var threshold in BuildThresholds(pixels, width, height, stride, format)) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
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

        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
        if (TryFindBoundingBox(pixels, width, height, stride, format, threshold, invert: false, out var box)) {
            if (TryDecodeFromBox(pixels, width, height, stride, format, box, threshold, invert: false, cancellationToken, out value)) return true;
        }

        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
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

        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryFindBoundingBox(pixels, width, height, stride, format, threshold, invert: false, out var box)) {
            if (TryDecodeFromBox(pixels, width, height, stride, format, box, threshold, invert: false, cancellationToken, diagnostics, out value)) return true;
        }

        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
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
            if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
            var modules = SampleModules(pixels, width, height, stride, format, box, candidate.WidthModules, candidate.HeightModules, candidate.ModuleSize, threshold, invert, cancellationToken);
            if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
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
            if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
            var modules = SampleModules(pixels, width, height, stride, format, box, candidate.WidthModules, candidate.HeightModules, candidate.ModuleSize, threshold, invert, cancellationToken);
            if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
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

    private static BitMatrix SampleModules(PixelSpan pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, int widthModules, int heightModules, int moduleSize, int threshold, bool invert, CancellationToken cancellationToken) {
        var modules = new BitMatrix(widthModules, heightModules);
        var totalWidth = widthModules * moduleSize;
        var totalHeight = heightModules * moduleSize;
        var offsetX = box.Left + (box.Width - totalWidth) / 2.0;
        var offsetY = box.Top + (box.Height - totalHeight) / 2.0;

        var half = moduleSize / 2.0;
        for (var y = 0; y < heightModules; y++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return modules;
            var sy = (int)Math.Round(offsetY + (y * moduleSize) + half);
            sy = Clamp(sy, 0, height - 1);
            for (var x = 0; x < widthModules; x++) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) return modules;
                var sx = (int)Math.Round(offsetX + (x * moduleSize) + half);
                sx = Clamp(sx, 0, width - 1);
                var dark = IsDark(pixels, width, height, stride, format, sx, sy, threshold);
                modules[x, y] = invert ? !dark : dark;
            }
        }

        return modules;
    }

    private static bool TryDecodeWithShear(PixelSpan pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, Candidate candidate, int threshold, bool invert, CancellationToken cancellationToken, out string value) {
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
        if (TryDecodeWithRowAligned(pixels, width, height, stride, format, box, candidate, threshold, invert, cancellationToken, out value)) return true;

        var shearList = new List<double>(12);
        int? leftEdgeMid = null;
        var midRow = box.Top + box.Height / 2;
        if (TryFindLeftEdge(pixels, width, height, stride, format, box.Left, box.Right, midRow, threshold, invert, cancellationToken, out var leftEdge)) {
            leftEdgeMid = leftEdge;
        }
        if (TryEstimateShear(pixels, width, height, stride, format, box, candidate, threshold, invert, cancellationToken, out var estimated)) {
            shearList.Add(estimated);
            shearList.Add(estimated - 0.12);
            shearList.Add(estimated + 0.12);
            shearList.Add(estimated - 0.24);
            shearList.Add(estimated + 0.24);
        }

        var defaults = new[] { -0.6, -0.4, -0.25, -0.15, -0.08, 0.08, 0.15, 0.25, 0.4, 0.6 };
        for (var i = 0; i < defaults.Length; i++) shearList.Add(defaults[i]);

        for (var i = 0; i < shearList.Count; i++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
            var modules = SampleModulesSheared(pixels, width, height, stride, format, box, candidate.WidthModules, candidate.HeightModules, candidate.ModuleSize, threshold, invert, shearList[i], leftEdgeMid, cancellationToken);
            if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
            if (TryDecodeWithRotations(modules, cancellationToken, out value)) return true;
            var trimmed = TrimModuleBorder(modules);
            if (!ReferenceEquals(trimmed, modules) && TryDecodeWithRotations(trimmed, cancellationToken, out value)) return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryDecodeWithShear(PixelSpan pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, Candidate candidate, int threshold, bool invert, CancellationToken cancellationToken, Pdf417DecodeDiagnostics diagnostics, out string value) {
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryDecodeWithRowAligned(pixels, width, height, stride, format, box, candidate, threshold, invert, cancellationToken, diagnostics, out value)) return true;

        var shearList = new List<double>(12);
        int? leftEdgeMid = null;
        var midRow = box.Top + box.Height / 2;
        if (TryFindLeftEdge(pixels, width, height, stride, format, box.Left, box.Right, midRow, threshold, invert, cancellationToken, out var leftEdge)) {
            leftEdgeMid = leftEdge;
        }
        if (TryEstimateShear(pixels, width, height, stride, format, box, candidate, threshold, invert, cancellationToken, out var estimated)) {
            shearList.Add(estimated);
            shearList.Add(estimated - 0.12);
            shearList.Add(estimated + 0.12);
            shearList.Add(estimated - 0.24);
            shearList.Add(estimated + 0.24);
        }

        var defaults = new[] { -0.6, -0.4, -0.25, -0.15, -0.08, 0.08, 0.15, 0.25, 0.4, 0.6 };
        for (var i = 0; i < defaults.Length; i++) shearList.Add(defaults[i]);

        for (var i = 0; i < shearList.Count; i++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
            var modules = SampleModulesSheared(pixels, width, height, stride, format, box, candidate.WidthModules, candidate.HeightModules, candidate.ModuleSize, threshold, invert, shearList[i], leftEdgeMid, cancellationToken);
            if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
            if (TryDecodeWithRotations(modules, cancellationToken, diagnostics, out value)) return true;
            var trimmed = TrimModuleBorder(modules);
            if (!ReferenceEquals(trimmed, modules) && TryDecodeWithRotations(trimmed, cancellationToken, diagnostics, out value)) return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryDecodeWithRowAligned(PixelSpan pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, Candidate candidate, int threshold, bool invert, CancellationToken cancellationToken, out string value) {
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
        var modules = SampleModulesRowAligned(pixels, width, height, stride, format, box, candidate.WidthModules, candidate.HeightModules, candidate.ModuleSize, threshold, invert, cancellationToken);
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
        if (TryDecodeWithRotations(modules, cancellationToken, out value)) return true;
        var trimmed = TrimModuleBorder(modules);
        if (!ReferenceEquals(trimmed, modules) && TryDecodeWithRotations(trimmed, cancellationToken, out value)) return true;
        value = string.Empty;
        return false;
    }

    private static bool TryDecodeWithRowAligned(PixelSpan pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, Candidate candidate, int threshold, bool invert, CancellationToken cancellationToken, Pdf417DecodeDiagnostics diagnostics, out string value) {
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        var modules = SampleModulesRowAligned(pixels, width, height, stride, format, box, candidate.WidthModules, candidate.HeightModules, candidate.ModuleSize, threshold, invert, cancellationToken);
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryDecodeWithRotations(modules, cancellationToken, diagnostics, out value)) return true;
        var trimmed = TrimModuleBorder(modules);
        if (!ReferenceEquals(trimmed, modules) && TryDecodeWithRotations(trimmed, cancellationToken, diagnostics, out value)) return true;
        value = string.Empty;
        return false;
    }

    private static BitMatrix SampleModulesRowAligned(PixelSpan pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, int widthModules, int heightModules, int moduleSize, int threshold, bool invert, CancellationToken cancellationToken) {
        var modules = new BitMatrix(widthModules, heightModules);
        var totalWidth = widthModules * moduleSize;
        var totalHeight = heightModules * moduleSize;
        var half = moduleSize / 2.0;
        var baseOffsetX = box.Left + (box.Width - totalWidth) / 2.0;
        var offsetY = box.Top + (box.Height - totalHeight) / 2.0;
        var maxShift = moduleSize * 4;
        var minRun = Math.Max(2, moduleSize / 2);

        for (var y = 0; y < heightModules; y++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return modules;
            var sy = (int)Math.Round(offsetY + (y * moduleSize) + half);
            sy = Clamp(sy, 0, height - 1);

            var rowOffsetX = baseOffsetX;
            if (TryFindLeftEdgeRun(pixels, width, height, stride, format, box.Left, box.Right, sy, threshold, invert, minRun, cancellationToken, out var leftEdge)) {
                rowOffsetX = leftEdge - half;
                var delta = rowOffsetX - baseOffsetX;
                if (delta > maxShift) rowOffsetX = baseOffsetX + maxShift;
                else if (delta < -maxShift) rowOffsetX = baseOffsetX - maxShift;
            }

            for (var x = 0; x < widthModules; x++) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) return modules;
                var sx = (int)Math.Round(rowOffsetX + (x * moduleSize) + half);
                sx = Clamp(sx, 0, width - 1);
                var dark = IsDark(pixels, width, height, stride, format, sx, sy, threshold);
                modules[x, y] = invert ? !dark : dark;
            }
        }

        return modules;
    }

    private static bool TryEstimateShear(PixelSpan pixels, int width, int height, int stride, PixelFormat format, BoundingBox box, Candidate candidate, int threshold, bool invert, CancellationToken cancellationToken, out double shearModulesPerRow) {
        shearModulesPerRow = 0;
        var sampleTop = box.Top + (int)(box.Height * 0.2);
        var sampleBottom = box.Bottom - (int)(box.Height * 0.2);

        if (sampleTop < box.Top) sampleTop = box.Top;
        if (sampleBottom > box.Bottom) sampleBottom = box.Bottom;
        if (sampleTop >= sampleBottom) return false;

        if (!TryFindLeftEdge(pixels, width, height, stride, format, box.Left, box.Right, sampleTop, threshold, invert, cancellationToken, out var leftTop)) return false;
        if (!TryFindLeftEdge(pixels, width, height, stride, format, box.Left, box.Right, sampleBottom, threshold, invert, cancellationToken, out var leftBottom)) return false;

        var delta = leftBottom - leftTop;
        if (candidate.HeightModules <= 1 || candidate.ModuleSize <= 0) return false;

        shearModulesPerRow = delta / ((candidate.HeightModules - 1) * (double)candidate.ModuleSize);
        return Math.Abs(shearModulesPerRow) > 0.02;
    }

    private static bool TryFindLeftEdge(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int left, int right, int y, int threshold, bool invert, CancellationToken cancellationToken, out int xFound) {
        xFound = 0;
        if ((uint)y >= (uint)height) return false;
        var row = y * stride;
        for (var x = left; x <= right; x++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
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

    private static bool TryFindLeftEdgeRun(PixelSpan pixels, int width, int height, int stride, PixelFormat format, int left, int right, int y, int threshold, bool invert, int minRun, CancellationToken cancellationToken, out int xFound) {
        xFound = 0;
        if ((uint)y >= (uint)height) return false;
        var row = y * stride;
        var run = 0;
        for (var x = left; x <= right; x++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
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
        if (DecodeBudget.ShouldAbort(cancellationToken)) return false;

        if (!TryFindRowEdges(pixels, width, height, stride, format, threshold, invert, box, candidate, cancellationToken, out var topLeft, out var topRight, out var bottomRight, out var bottomLeft)) {
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
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
            var modules = SampleModulesPerspective(pixels, width, height, stride, format, candidate.WidthModules, candidate.HeightModules, threshold, invert, transform, offsets[i].x, offsets[i].y, cancellationToken);
            if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
            if (TryDecodeWithRotations(modules, cancellationToken, out value)) return true;

            var trimmed = TrimModuleBorder(modules);
            if (!ReferenceEquals(trimmed, modules) && TryDecodeWithRotations(trimmed, cancellationToken, out value)) return true;
        }

        value = string.Empty;
        return false;
    }
#endif

}
