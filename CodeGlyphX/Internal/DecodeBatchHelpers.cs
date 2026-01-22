using System;
using System.Collections.Generic;
using System.Threading;

namespace CodeGlyphX.Internal;

internal static class DecodeBatchHelpers {
    public static DecodeBatchResult<T> Run<T>(IEnumerable<byte[]> images, Func<byte[], DecodeResult<T>> decode, CancellationToken cancellationToken) {
        if (images is null) throw new ArgumentNullException(nameof(images));
        if (decode is null) throw new ArgumentNullException(nameof(decode));

        var results = new List<DecodeResult<T>>();
        var success = 0;
        var invalidInput = 0;
        var unsupportedFormat = 0;
        var cancelled = 0;
        var noResult = 0;
        var error = 0;
        long elapsedTicks = 0;

        foreach (var image in images) {
            DecodeResult<T> result;
            if (cancellationToken.IsCancellationRequested) {
                result = new DecodeResult<T>(DecodeFailureReason.Cancelled, default, TimeSpan.Zero);
            } else if (image is null) {
                result = new DecodeResult<T>(DecodeFailureReason.InvalidInput, default, TimeSpan.Zero, "image buffer is null");
            } else {
                result = decode(image);
            }

            results.Add(result);
            elapsedTicks += result.Elapsed.Ticks;

            switch (result.Failure) {
                case DecodeFailureReason.None:
                    success++;
                    break;
                case DecodeFailureReason.InvalidInput:
                    invalidInput++;
                    break;
                case DecodeFailureReason.UnsupportedFormat:
                    unsupportedFormat++;
                    break;
                case DecodeFailureReason.Cancelled:
                    cancelled++;
                    break;
                case DecodeFailureReason.NoResult:
                    noResult++;
                    break;
                case DecodeFailureReason.Error:
                    error++;
                    break;
            }
        }

        var counts = new DecodeBatchCounts {
            Total = results.Count,
            Success = success,
            InvalidInput = invalidInput,
            UnsupportedFormat = unsupportedFormat,
            Cancelled = cancelled,
            NoResult = noResult,
            Error = error
        };

        var diagnostics = new DecodeBatchDiagnostics(
            counts,
            TimeSpan.FromTicks(elapsedTicks));

        return new DecodeBatchResult<T>(results.ToArray(), diagnostics);
    }
}
