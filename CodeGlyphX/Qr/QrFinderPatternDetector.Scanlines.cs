#if NET8_0_OR_GREATER
using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace CodeGlyphX.Qr;

internal static partial class QrFinderPatternDetector {
    private static void FindCandidatesWithStep(QrGrayImage image, bool invert, int rowStep,
        PooledList<FinderPattern> possibleCenters, bool aggressive, Func<bool>? shouldStop,
        int maxCandidates, bool requireDiagonalCheck) {
        var width = image.Width;
        var height = image.Height;
        var maxCandidatesLimit = maxCandidates > 0 ? maxCandidates : int.MaxValue;
        if (rowStep < 1) rowStep = 1;
        var buffer = ArrayPool<byte>.Shared.Rent(width);
        try {
            var row = buffer.AsSpan(0, width);
            for (var y = 0; y < height; y += rowStep) {
                if (shouldStop?.Invoke() == true) return;
                if (possibleCenters.Count >= maxCandidatesLimit) return;
                image.WriteBinaryRow(y, invert, row);
                var s0 = 0;
                var s1 = 0;
                var s2 = 0;
                var s3 = 0;
                var s4 = 0;
                var currentState = 0;
                var x = 0;
                while (x < width) {
                    var black = row[x] != 0;
                    var length = row[x..].IndexOf(black ? (byte)0 : (byte)1);
                    if (length < 0) length = width - x;
                    if (black) {
                        if ((currentState & 1) == 1) currentState++;
                        if (currentState > 4) {
                            ShiftCounts(ref s0, ref s1, ref s2, ref s3, ref s4);
                            currentState = 3;
                        }
                        AddRun(currentState, length, ref s0, ref s1, ref s2, ref s3, ref s4);
                    } else if (currentState == 4) {
                        if (FoundPatternCross(s0, s1, s2, s3, s4, aggressive) &&
                            HandlePossibleCenter(image, invert, possibleCenters, s0, s1, s2, s3, s4, x, y, aggressive, requireDiagonalCheck)) {
                            if (possibleCenters.Count >= maxCandidatesLimit) return;
                            ResetCounts(ref s0, ref s1, ref s2, ref s3, ref s4);
                            // The original pixel pass consumes the first white pixel
                            // while accepting a finder, before starting its next run.
                            currentState = length > 1 ? 1 : 0;
                            s1 = length - 1;
                        } else {
                            // ShiftCounts already counts the first white pixel.
                            ShiftCounts(ref s0, ref s1, ref s2, ref s3, ref s4);
                            currentState = 3;
                            s3 += length - 1;
                        }
                    } else {
                        if ((currentState & 1) == 0) currentState++;
                        AddRun(currentState, length, ref s0, ref s1, ref s2, ref s3, ref s4);
                    }
                    x += length;
                }

                if (FoundPatternCross(s0, s1, s2, s3, s4, aggressive)) {
                    HandlePossibleCenter(image, invert, possibleCenters, s0, s1, s2, s3, s4, width, y, aggressive, requireDiagonalCheck);
                }
            }
        } finally {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddRun(int state, int length, ref int s0, ref int s1, ref int s2, ref int s3, ref int s4) {
        switch (state) {
            case 0: s0 += length; break;
            case 1: s1 += length; break;
            case 2: s2 += length; break;
            case 3: s3 += length; break;
            default: s4 += length; break;
        }
    }
}
#endif
