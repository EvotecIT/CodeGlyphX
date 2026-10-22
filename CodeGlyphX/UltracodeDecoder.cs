using System;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Internal;
using CodeGlyphX.Ultracode;

namespace CodeGlyphX;

/// <summary>Decodes sampled Ultracode colour cells in any orthogonal rotation.</summary>
public static class UltracodeDecoder {
    /// <summary>Checks the structure and Reed-Solomon parity before returning an eight-bit payload.</summary>
    /// <remarks>Supply the symbol grid without its quiet zone. Unknown data cells become tile erasures.
    /// Structural finder and timing cells must be readable. Compressed and linked-message modes are not accepted by this overload.</remarks>
    public static bool TryDecode(UltracodeGrid cells, out UltracodeDecoded? result, CancellationToken cancellationToken = default) {
        if (cells is null) throw new ArgumentNullException(nameof(cells));
        result = null;
        if (cells.Width > 66 || cells.Height > 66) return false;
        var rotated = cells;
        for (var rotation = 0; rotation < 4; rotation++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
            var layout = UltracodeLayout.FromDimensions(rotated.Width, rotated.Height);
            if (layout != null && layout.HasFrame(rotated) && TryDecodeCore(rotated, layout, out result, cancellationToken)) {
                result = result!.WithRotation((4 - rotation) % 4 * 90);
                return true;
            }
            if (rotation < 3) rotated = Rotate(rotated);
        }
        return false;
    }

    private static bool TryDecodeCore(UltracodeGrid cells, UltracodeLayout layout, out UltracodeDecoded? result, CancellationToken token) {
        result = null;
        var tiles = layout.ReadTiles(cells);
        for (var parityCount = 6; parityCount <= 101; parityCount++) {
            if (DecodeBudget.ShouldAbort(token)) return false;
            if (parityCount + 5 >= tiles.Length || tiles[parityCount + 3] != 283 && tiles[tiles.Length - 1] != parityCount) continue;
            for (var pads = 0; pads < layout.Rows; pads++) {
                var messageCount = tiles.Length - parityCount - 3 - pads;
                if (messageCount < 3 || messageCount + parityCount > 282) continue;
                UltracodeErrorCorrectionLevel? level = null;
                for (var candidate = 1; candidate <= 5; candidate++)
                    if (UltracodeLayout.ParityCount(messageCount, (UltracodeErrorCorrectionLevel)candidate) == parityCount)
                        level = (UltracodeErrorCorrectionLevel)candidate;
                if (!level.HasValue) continue;
                var expected = new UltracodeLayout(messageCount, parityCount);
                if (expected.Width != layout.Width || expected.Height != layout.Height) continue;
                var words = new int[messageCount + parityCount];
                var erasures = new List<int>();
                SetWord(words, erasures, 0, tiles[0]);
                SetWord(words, erasures, 1, tiles[1]);
                SetWord(words, erasures, 2, tiles[parityCount + 4]);
                for (var i = 3; i < messageCount; i++) SetWord(words, erasures, i, tiles[parityCount + 2 + i]);
                for (var i = 0; i < parityCount; i++) SetWord(words, erasures, messageCount + i, tiles[2 + i]);
                var original = (int[])words.Clone();
                if (!UltracodeErrorCorrection.Correct(words, parityCount, erasures.ToArray(), token)
                    || words[0] != 257 || words[1] != messageCount || words[2] != parityCount - 3) continue;
                var bytes = new byte[messageCount - 3];
                var valid = true;
                for (var i = 0; i < bytes.Length; i++) {
                    if (words[3 + i] > 255) { valid = false; break; }
                    bytes[i] = (byte)words[3 + i];
                }
                if (!valid) continue;
                var corrected = erasures.Count;
                for (var i = 0; i < words.Length; i++) if (words[i] != original[i] && !erasures.Contains(i)) corrected++;
                result = new UltracodeDecoded(bytes, level.Value, corrected);
                return true;
            }
        }
        return false;
    }

    private static void SetWord(int[] words, List<int> erasures, int position, int value) {
        if (value < 0 || value >= 283) { erasures.Add(position); words[position] = 0; }
        else words[position] = value;
    }

    private static UltracodeGrid Rotate(UltracodeGrid source) {
        var result = new UltracodeGrid(source.Height, source.Width);
        for (var y = 0; y < source.Height; y++) for (var x = 0; x < source.Width; x++)
            result[source.Height - y - 1, x] = source[x, y];
        return result;
    }
}
