using System;
using System.Collections.Generic;
using System.Globalization;

namespace CodeGlyphX.ChannelCode;

/// <summary>Encodes Channel Code numbers using three through eight channels.</summary>
public static class ChannelCodeEncoder {
    /// <summary>Encodes two through seven digits. Automatic channel selection preserves the supplied digit width.</summary>
    /// <param name="value">Two through seven decimal digits whose number fits the selected channel capacity.</param>
    /// <param name="channels">Three through eight channels, or null to select the digit count plus one. An explicitly wider channel count decodes with leading zeros to that count minus one.</param>
    public static Barcode1D Encode(string value, int? channels = null) => Encode(value, channels, false);

    /// <summary>Encodes using the full five-bar or short three-bar finder, with automatic channel selection.</summary>
    public static Barcode1D Encode(string value, bool shortFinder) => Encode(value, null, shortFinder);

    /// <summary>Encodes with explicit channel count and finder length. The existing full-finder default is preserved.</summary>
    public static Barcode1D Encode(string value, int? channels, bool shortFinder) {
        if (value is null) throw new ArgumentNullException(nameof(value));
        if (value.Length < 2 || value.Length > 7) throw new ArgumentException("Channel Code accepts two through seven decimal digits.", nameof(value));
        for (var i = 0; i < value.Length; i++)
            if ((uint)(value[i] - '0') > 9) throw new ArgumentException("Channel Code accepts decimal digits only.", nameof(value));
        var number = int.Parse(value, CultureInfo.InvariantCulture);
        var count = channels ?? Math.Max(3, value.Length + 1);
        if (count < 3 || count > 8) throw new ArgumentOutOfRangeException(nameof(channels));
        if (value.Length >= count || number > ChannelCodeRank.Capacities[count - 3])
            throw new ArgumentException("The value exceeds the selected Channel Code capacity.", nameof(value));
        var body = new ChannelCodeRank(count).Unrank(number);
        var finder = shortFinder ? 5 : 9;
        var segments = new List<BarSegment>(finder + body.Length);
        for (var i = 0; i < finder; i++) segments.Add(new BarSegment((i & 1) == 0, 1));
        for (var i = 0; i < body.Length; i++) segments.Add(new BarSegment((i & 1) != 0, body[i]));
        return new Barcode1D(segments, SymbolFormat.ChannelCode);
    }
}

/// <summary>Ranks valid Channel Code width sequences using suffix counts rather than enumerating millions of symbols.</summary>
internal sealed class ChannelCodeRank {
    internal static readonly int[] Capacities = { 26, 292, 3493, 44072, 576688, 7742862 };
    private readonly int _channels;
    private readonly Dictionary<int, int> _counts = new();

    internal ChannelCodeRank(int channels) => _channels = channels;

    internal int[] Unrank(int value) {
        var result = new int[_channels * 2];
        var spaces = 2 * _channels - 1;
        var bars = spaces;
        var previousBar = 1;
        var previousSpace = 1;
        var earlierBar = 1;
        for (var at = 0; at < _channels; at++) {
            var selected = false;
            var remaining = _channels - at - 1;
            for (var space = 1; space <= spaces - remaining && !selected; space++) {
                for (var bar = 1; bar <= bars - remaining; bar++) {
                    if (!Allowed(at, earlierBar, previousSpace, previousBar, space, bar)) continue;
                    var count = Count(at + 1, spaces - space, bars - bar, previousBar, space, bar);
                    if (value >= count) { value -= count; continue; }
                    result[at * 2] = space;
                    result[at * 2 + 1] = bar;
                    spaces -= space;
                    bars -= bar;
                    earlierBar = previousBar;
                    previousSpace = space;
                    previousBar = bar;
                    selected = true;
                    break;
                }
            }
            if (!selected) throw new InvalidOperationException("Channel Code rank is out of range.");
        }
        return result;
    }

    internal bool TryRank(ReadOnlySpan<int> widths, out int value) {
        value = 0;
        if (widths.Length != _channels * 2) return false;
        var spaces = 2 * _channels - 1;
        var bars = spaces;
        var previousBar = 1;
        var previousSpace = 1;
        var earlierBar = 1;
        for (var at = 0; at < _channels; at++) {
            var wantedSpace = widths[at * 2];
            var wantedBar = widths[at * 2 + 1];
            var remaining = _channels - at - 1;
            if (wantedSpace <= 0 || wantedSpace > spaces - remaining || wantedBar <= 0 || wantedBar > bars - remaining
                || !Allowed(at, earlierBar, previousSpace, previousBar, wantedSpace, wantedBar)) return false;
            for (var space = 1; space <= wantedSpace; space++) {
                for (var bar = 1; bar <= bars - remaining; bar++) {
                    if (space == wantedSpace && bar >= wantedBar) break;
                    if (!Allowed(at, earlierBar, previousSpace, previousBar, space, bar)) continue;
                    value += Count(at + 1, spaces - space, bars - bar, previousBar, space, bar);
                }
            }
            spaces -= wantedSpace;
            bars -= wantedBar;
            earlierBar = previousBar;
            previousSpace = wantedSpace;
            previousBar = wantedBar;
        }
        return spaces == 0 && bars == 0;
    }

    private int Count(int at, int spaces, int bars, int earlierBar, int previousSpace, int previousBar) {
        if (at == _channels) return spaces == 0 && bars == 0 ? 1 : 0;
        var key = at | spaces << 4 | bars << 9 | earlierBar << 14 | previousSpace << 18 | previousBar << 22;
        if (_counts.TryGetValue(key, out var cached)) return cached;
        var remaining = _channels - at - 1;
        var result = 0;
        for (var space = 1; space <= spaces - remaining; space++) {
            for (var bar = 1; bar <= bars - remaining; bar++) {
                if (!Allowed(at, earlierBar, previousSpace, previousBar, space, bar)) continue;
                result += Count(at + 1, spaces - space, bars - bar, previousBar, space, bar);
            }
        }
        _counts.Add(key, result);
        return result;
    }

    private static bool Allowed(int at, int earlierBar, int previousSpace, int previousBar, int space, int bar) =>
        at == 0 ? space + bar != 2 : at == 1 ? previousSpace + previousBar + space + bar != 4
            : earlierBar + previousSpace + previousBar + space + bar != 5;
}
