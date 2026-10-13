#if NET8_0_OR_GREATER
using System;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelDecoder {
    private static bool TryDecodeWithInversion(global::CodeGlyphX.BitMatrix matrix, Func<QrDecoded, bool>? accept, DecodeBudget budget, out QrDecoded result, out global::CodeGlyphX.QrDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        Func<bool>? shouldStop = budget.CanAbort ? () => budget.IsExpired : null;
        var ok = global::CodeGlyphX.QrDecoder.TryDecodeInternal(matrix, shouldStop, out result, out global::CodeGlyphX.QrDecodeDiagnostics moduleDiag);
        var best = moduleDiag;
        if (ok && (accept == null || accept(result))) {
            diagnostics = moduleDiag;
            return true;
        }

        if (budget.Enabled && budget.BudgetMilliseconds <= 800) {
            diagnostics = best;
            return false;
        }

        global::CodeGlyphX.QrDecodeDiagnostics moduleDiagInv;
        matrix.Invert();
        try {
            ok = global::CodeGlyphX.QrDecoder.TryDecodeInternal(matrix, shouldStop, out result, out moduleDiagInv);
        } finally {
            matrix.Invert();
        }
        best = Better(best, moduleDiagInv);

        if (ok && (accept == null || accept(result))) {
            diagnostics = moduleDiagInv;
            return true;
        }

        diagnostics = best;
        return false;
    }

    private static bool TryDecodeByRotations(global::CodeGlyphX.BitMatrix matrix, Func<QrDecoded, bool>? accept, DecodeBudget budget, out QrDecoded result, out global::CodeGlyphX.QrDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        var best = default(global::CodeGlyphX.QrDecodeDiagnostics);

        var rotated = new global::CodeGlyphX.BitMatrix(matrix.Width, matrix.Height);
        RotateInto90(matrix, rotated);
        if (TryDecodeWithInversion(rotated, accept, budget, out result, out var d90)) {
            diagnostics = d90;
            return true;
        }
        best = Better(best, d90);

        RotateInto180(matrix, rotated);
        if (TryDecodeWithInversion(rotated, accept, budget, out result, out var d180)) {
            diagnostics = d180;
            return true;
        }
        best = Better(best, d180);

        RotateInto270(matrix, rotated);
        if (TryDecodeWithInversion(rotated, accept, budget, out result, out var d270)) {
            diagnostics = d270;
            return true;
        }
        best = Better(best, d270);

        diagnostics = best;
        return false;
    }

    private static void RotateInto90(global::CodeGlyphX.BitMatrix source, global::CodeGlyphX.BitMatrix target) {
        target.Clear();
        var width = source.Width;
        var height = source.Height;
        var srcWords = source.Words;
        var dstWords = target.Words;
        for (var y = 0; y < height; y++) {
            var rowBase = y * width;
            for (var x = 0; x < width; x++) {
                var bitIndex = rowBase + x;
                if ((srcWords[bitIndex >> 5] & (1u << (bitIndex & 31))) == 0) continue;
                var rx = height - 1 - y;
                var ry = x;
                var dstIndex = ry * width + rx;
                dstWords[dstIndex >> 5] |= 1u << (dstIndex & 31);
            }
        }
    }

    private static void RotateInto180(global::CodeGlyphX.BitMatrix source, global::CodeGlyphX.BitMatrix target) {
        target.Clear();
        var width = source.Width;
        var height = source.Height;
        var srcWords = source.Words;
        var dstWords = target.Words;
        for (var y = 0; y < height; y++) {
            var rowBase = y * width;
            for (var x = 0; x < width; x++) {
                var bitIndex = rowBase + x;
                if ((srcWords[bitIndex >> 5] & (1u << (bitIndex & 31))) == 0) continue;
                var rx = width - 1 - x;
                var ry = height - 1 - y;
                var dstIndex = ry * width + rx;
                dstWords[dstIndex >> 5] |= 1u << (dstIndex & 31);
            }
        }
    }

    private static void RotateInto270(global::CodeGlyphX.BitMatrix source, global::CodeGlyphX.BitMatrix target) {
        target.Clear();
        var width = source.Width;
        var height = source.Height;
        var srcWords = source.Words;
        var dstWords = target.Words;
        for (var y = 0; y < height; y++) {
            var rowBase = y * width;
            for (var x = 0; x < width; x++) {
                var bitIndex = rowBase + x;
                if ((srcWords[bitIndex >> 5] & (1u << (bitIndex & 31))) == 0) continue;
                var rx = y;
                var ry = width - 1 - x;
                var dstIndex = ry * width + rx;
                dstWords[dstIndex >> 5] |= 1u << (dstIndex & 31);
            }
        }
    }

}
#endif
