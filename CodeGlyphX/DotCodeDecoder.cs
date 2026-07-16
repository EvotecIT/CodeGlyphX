using System;
using CodeGlyphX.DotCode;
using CodeGlyphX.Internal.ReedSolomon;

namespace CodeGlyphX;

/// <summary>Decodes exact sampled DotCode module grids with prime-field Reed-Solomon correction.</summary>
public static class DotCodeDecoder {
    /// <summary>Attempts to decode a DotCode grid and preserve control metadata.</summary>
    public static bool TryDecodeDetailed(BitMatrix modules, out DotCodeDecoded decoded) {
        decoded = null!;
        if (modules is null || modules.Width is < 5 or > 200 || modules.Height is < 5 or > 200 ||
            ((modules.Width + modules.Height) & 1) == 0) return false;
        var lit = 0;
        for (var y = 0; y < modules.Height; y++) {
            for (var x = 0; x < modules.Width; x++) {
                if (!modules[x, y]) continue;
                if (((x + y) & 1) != 0) return false;
                lit++;
            }
        }
        if (lit < 4) return false;
        try {
            var stream = DotCodeMatrix.Unfold(modules);
            var dataLength = ResolveDataLength(stream.Length);
            if (dataLength < 1) return false;
            if (TryDecodeCandidate(stream, dataLength, out decoded)) return true;

            // AIM DotCode masks 4..7 deliberately light all six orientation corners after folding. Those
            // positions contain the tail of the serialized stream, so reverse the small, bounded overwrite
            // before pattern lookup and let Reed-Solomon validate the one genuine candidate.
            if (!DotCodeMatrix.CornersAreLit(modules)) return false;
            var serializedLength = GetSerializedBitLength(dataLength);
            var firstOverwritten = Math.Max(stream.Length - 6, 0);
            var overwrittenDataBits = Math.Max(serializedLength - firstOverwritten, 0);
            if (overwrittenDataBits == 0) return false;
            var candidateStream = (bool[])stream.Clone();
            for (var candidate = 0; candidate < 1 << overwrittenDataBits; candidate++) {
                for (var bit = 0; bit < overwrittenDataBits; bit++) {
                    candidateStream[firstOverwritten + bit] = (candidate & 1 << bit) != 0;
                }
                if (TryDecodeCandidate(candidateStream, dataLength, out decoded)) return true;
            }
            return false;
        } catch (ReedSolomonException) { return false; }
          catch (ArgumentException) { return false; }
          catch (IndexOutOfRangeException) { return false; }
    }

    /// <summary>Attempts to decode a DotCode grid and return only its text.</summary>
    public static bool TryDecode(BitMatrix modules, out string text) {
        if (TryDecodeDetailed(modules, out var decoded)) { text = decoded.Text; return true; }
        text = string.Empty;
        return false;
    }

    private static int ResolveDataLength(int dotCapacity) {
        var best = 0;
        for (var data = 1; ; data++) {
            var required = 2 + 9 * (data + 3 + data / 2);
            if (required > dotCapacity) break;
            best = data;
        }
        return best;
    }

    private static int GetSerializedBitLength(int dataLength) {
        var eccLength = 3 + dataLength / 2;
        return 2 + 9 * (dataLength + eccLength);
    }

    private static bool TryDecodeCandidate(bool[] stream, int dataLength, out DotCodeDecoded decoded) {
        try {
            return TryDecodeStream(stream, dataLength, out decoded);
        } catch (ReedSolomonException) { decoded = null!; return false; }
          catch (ArgumentException) { decoded = null!; return false; }
          catch (IndexOutOfRangeException) { decoded = null!; return false; }
    }

    private static bool TryDecodeStream(bool[] stream, int dataLength, out DotCodeDecoded decoded) {
        decoded = null!;
        var mask = ReadBits(stream, 0, 2);
        var eccLength = 3 + dataLength / 2;
        var words = new int[dataLength + 1 + eccLength];
        words[0] = mask;
        var offset = 2;
        for (var i = 1; i < words.Length; i++) {
            var pattern = ReadBits(stream, offset, 9);
            offset += 9;
            words[i] = DotCodeTables.DecodePattern(pattern);
            if (words[i] < 0) return false;
        }

        PrimeFieldReedSolomon.DecodeInterleaved(words, dataLength + 1, eccLength, DotCodeTables.Prime, DotCodeTables.Primitive);
        if (words[0] != mask || mask is < 0 or > 3) return false;
        var increment = mask == 1 ? 3 : mask == 2 ? 7 : mask == 3 ? 17 : 0;
        var data = new int[dataLength];
        var weight = 0;
        for (var i = 0; i < data.Length; i++) {
            data[i] = Mod(words[i + 1] - weight, DotCodeTables.Prime);
            weight += increment;
        }
        if (!DotCodeHighLevelDecoder.TryDecode(data, out var highLevel)) return false;
        decoded = new DotCodeDecoded(highLevel.Text, highLevel.Bytes, highLevel.HasFnc1, highLevel.ReaderInitialization,
            mask, highLevel.EciAssignments, highLevel.StructuredAppendIndex, highLevel.StructuredAppendCount);
        return true;
    }

    private static int ReadBits(bool[] stream, int offset, int count) {
        if (offset < 0 || count < 0 || offset + count > stream.Length) throw new ArgumentOutOfRangeException(nameof(offset));
        var value = 0;
        for (var i = 0; i < count; i++) value = value << 1 | (stream[offset + i] ? 1 : 0);
        return value;
    }

    private static int Mod(int value, int modulus) { var result = value % modulus; return result < 0 ? result + modulus : result; }
}
