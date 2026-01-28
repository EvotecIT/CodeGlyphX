using System;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// Canonical prefix code with LSB-first decoding support.
/// </summary>
internal sealed class WebpPrefixCode {
    private const int InvalidSymbol = -1;

    private readonly int[] _left;
    private readonly int[] _right;
    private readonly int[] _symbol;
    private readonly int _singleSymbol;

    private WebpPrefixCode(int[] left, int[] right, int[] symbol, int singleSymbol) {
        _left = left;
        _right = right;
        _symbol = symbol;
        _singleSymbol = singleSymbol;
    }

    /// <summary>
    /// Builds a canonical prefix code from code lengths.
    /// </summary>
    public static bool TryBuild(ReadOnlySpan<byte> codeLengths, int maxBits, out WebpPrefixCode code) {
        code = null!;
        if (codeLengths.Length == 0) return false;
        if (maxBits <= 0 || maxBits > 24) return false;

        var counts = new int[maxBits + 1];
        var nonZero = 0;
        var singleSymbol = InvalidSymbol;
        for (var i = 0; i < codeLengths.Length; i++) {
            var len = codeLengths[i];
            if (len == 0) continue;
            if (len > maxBits) return false;
            counts[len]++;
            nonZero++;
            singleSymbol = i;
        }

        if (nonZero == 0) {
            // Treat an empty code as a single-symbol code for symbol 0 (per VP8L spec).
            code = new WebpPrefixCode(
                left: Array.Empty<int>(),
                right: Array.Empty<int>(),
                symbol: Array.Empty<int>(),
                singleSymbol: 0);
            return true;
        }

        if (!IsValidTree(counts, maxBits, nonZero)) return false;

        if (nonZero == 1) {
            code = new WebpPrefixCode(
                left: Array.Empty<int>(),
                right: Array.Empty<int>(),
                symbol: Array.Empty<int>(),
                singleSymbol);
            return true;
        }

        var nextCode = new int[maxBits + 1];
        var canonical = 0;
        for (var bits = 1; bits <= maxBits; bits++) {
            canonical = (canonical + counts[bits - 1]) << 1;
            nextCode[bits] = canonical;
        }

        var capacity = checked(codeLengths.Length * 2 + 1);
        var left = new int[capacity];
        var right = new int[capacity];
        var symbol = new int[capacity];
        for (var i = 0; i < capacity; i++) {
            left[i] = InvalidSymbol;
            right[i] = InvalidSymbol;
            symbol[i] = InvalidSymbol;
        }

        var nodeCount = 1; // root
        for (var sym = 0; sym < codeLengths.Length; sym++) {
            var len = codeLengths[sym];
            if (len == 0) continue;

            var codeValue = nextCode[len]++;
            var reversed = ReverseBits(codeValue, len);
            if (!InsertCode(left, right, symbol, ref nodeCount, reversed, len, sym)) return false;
        }

        code = new WebpPrefixCode(left, right, symbol, InvalidSymbol);
        return true;
    }

    /// <summary>
    /// Decodes the next symbol from the bitstream.
    /// </summary>
    public int DecodeSymbol(ref WebpBitReader reader) {
        if (_singleSymbol != InvalidSymbol) return _singleSymbol;

        var node = 0;
        while (true) {
            var bit = reader.ReadBits(1);
            if (bit < 0) return InvalidSymbol;

            node = bit == 0 ? _left[node] : _right[node];
            if (node < 0 || node >= _symbol.Length) return InvalidSymbol;

            var sym = _symbol[node];
            if (sym != InvalidSymbol) return sym;
        }
    }

    private static bool InsertCode(
        int[] left,
        int[] right,
        int[] symbol,
        ref int nodeCount,
        int code,
        int length,
        int sym) {
        var node = 0;
        for (var i = 0; i < length; i++) {
            var bit = (code >> i) & 1;
            var next = bit == 0 ? left[node] : right[node];
            if (next == InvalidSymbol) {
                next = nodeCount++;
                if (next >= symbol.Length) return false;
                left[next] = InvalidSymbol;
                right[next] = InvalidSymbol;
                symbol[next] = InvalidSymbol;
                if (bit == 0) left[node] = next;
                else right[node] = next;
            }
            node = next;
        }

        if (symbol[node] != InvalidSymbol) return false;
        symbol[node] = sym;
        return true;
    }

    private static int ReverseBits(int value, int length) {
        var result = 0;
        for (var i = 0; i < length; i++) {
            result = (result << 1) | (value & 1);
            value >>= 1;
        }
        return result;
    }

    private static bool IsValidTree(int[] counts, int maxBits, int nonZero) {
        if (nonZero == 1) return true;

        long weight = 0;
        var full = 1L << maxBits;
        for (var bits = 1; bits <= maxBits; bits++) {
            var count = counts[bits];
            if (count == 0) continue;
            weight += (long)count << (maxBits - bits);
            if (weight > full) return false;
        }
        // WebP allows incomplete trees as long as they are not over-subscribed.
        return weight > 0 && weight <= full;
    }
}
