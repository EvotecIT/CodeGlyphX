using System;
using System.Globalization;

namespace CodeGlyphX.ChannelCode;

/// <summary>Decodes Channel Code's finder and ranked width sequence.</summary>
public static class ChannelCodeDecoder {
    /// <summary>Decodes exact module widths, preserving the channel count as leading decimal zeros.</summary>
    public static bool TryDecode(ReadOnlySpan<bool> modules, out string text) {
        text = string.Empty;
        if (modules.Length < 15 || modules.Length > 39 || !modules[0] || !modules[modules.Length - 1]) return false;
        var runs = new int[25];
        var count = 0;
        var current = true;
        foreach (var module in modules) {
            if (module != current) {
                count++;
                if (count == runs.Length) return false;
                current = module;
            }
            runs[count]++;
        }
        count++;
        foreach (var finder in new[] { 9, 5 }) {
            var channels = (count - finder) / 2;
            if (channels < 3 || channels > 8 || count != channels * 2 + finder || modules.Length != channels * 4 + finder - 2) continue;
            for (var i = 0; i < finder; i++) if (runs[i] != 1) return false;
            if (!new ChannelCodeRank(channels).TryRank(runs.AsSpan(finder, channels * 2), out var rank)) return false;
            text = rank.ToString(CultureInfo.InvariantCulture).PadLeft(channels - 1, '0');
            return true;
        }
        return false;
    }
}
