using System;
using System.Buffers;
using System.Threading;
using CodeGlyphX.Internal;
using CodeGlyphX.Pdf417.Ec;

namespace CodeGlyphX.Pdf417;

public static partial class Pdf417Decoder {
    private static bool TryDecodeInternal(BitMatrix modules, CancellationToken cancellationToken, Pdf417DecodeDiagnostics diagnostics, out string value) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }

        if (TryDecodeCore(modules, cancellationToken, diagnostics, out value)) return MarkSuccessful(diagnostics);
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryDecodeWithStartPattern(modules, cancellationToken, diagnostics, out value)) return MarkSuccessful(diagnostics);
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        diagnostics.MirroredTried = true;
        var mirror = MirrorX(modules);
        if (TryDecodeCore(mirror, cancellationToken, diagnostics, out value)) return MarkSuccessful(diagnostics);
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryDecodeWithStartPattern(mirror, cancellationToken, diagnostics, out value)) return MarkSuccessful(diagnostics);

        value = string.Empty;
        diagnostics.Failure ??= "No PDF417 decoded.";
        return false;
    }

    private static bool MarkSuccessful(Pdf417DecodeDiagnostics diagnostics) {
        diagnostics.Success = true;
        diagnostics.Failure = null;
        return true;
    }

    private static bool TryDecodeCore(BitMatrix modules, CancellationToken cancellationToken, out string value) {
        return TryDecodeCore(modules, cancellationToken, out value, out _);
    }

    private static bool TryDecodeCore(BitMatrix modules, CancellationToken cancellationToken, out string value, out Pdf417MacroMetadata? macro) {
        if (TryDecodeCore(modules, cancellationToken, topDown: true, out value, out macro)) return true;
        if (DecodeBudget.ShouldAbort(cancellationToken)) return FailDecode(out value);
        // Retain decoding of bottom-up symbols emitted by earlier library versions.
        return TryDecodeCore(modules, cancellationToken, topDown: false, out value, out macro);
    }

    private static bool TryDecodeCore(BitMatrix modules, CancellationToken cancellationToken, bool topDown, out string value, out Pdf417MacroMetadata? macro) {
        macro = null;
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
        var width = modules.Width;
        if (!TryGetDimensions(width, out var cols, out var compact)) {
            value = string.Empty;
            return false;
        }
        if (TryDecodeCore(modules, cancellationToken, topDown, cols, compact, out value, out macro)) return true;
        if (DecodeBudget.ShouldAbort(cancellationToken)) return FailDecode(out value);
        // Regular and compact widths differ by two codewords and can describe the same matrix width.
        if (!compact && width >= 52 && (width - 35) % 17 == 0) {
            return TryDecodeCore(modules, cancellationToken, topDown, (width - 35) / 17, compact: true, out value, out macro);
        }
        return FailDecode(out value);
    }

    private static bool TryDecodeCore(BitMatrix modules, CancellationToken cancellationToken, bool topDown, int cols, bool compact,
        out string value, out Pdf417MacroMetadata? macro) {
        macro = null;
        if (DecodeBudget.ShouldAbort(cancellationToken)) return FailDecode(out value);
        var width = modules.Width;
        var height = modules.Height;

        var capacity = cols * height;
        var rented = ArrayPool<int>.Shared.Rent(capacity);
        var count = 0;
        var rowWidth = width;

        try {
            for (var rowIndex = 0; rowIndex < height; rowIndex++) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) return FailDecode(out value);
                var y = topDown ? rowIndex : height - 1 - rowIndex;
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

            if (!corrected) return FailDecode(out value);
            var lengthDescriptor = received[0];
            if (lengthDescriptor <= 0 || lengthDescriptor > total) {
                return FailDecode(out value);
            }
            if (eccCount > 0 && lengthDescriptor > total - eccCount) {
                return FailDecode(out value);
            }

            var dataCodewords = new int[lengthDescriptor - 1];
            Array.Copy(received, 1, dataCodewords, 0, dataCodewords.Length);
            var decoded = Pdf417DecodedBitStreamParser.Decode(dataCodewords, out macro);
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
        var success = TryDecodeCore(modules, cancellationToken, out value, out var macro);
        if (success) diagnostics.Macro = macro;
        return success;
    }

    private static bool FailDecode(out string value) {
        value = string.Empty;
        return false;
    }

}
