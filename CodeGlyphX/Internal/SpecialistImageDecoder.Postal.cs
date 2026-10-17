using System;
using System.Collections.Generic;
using System.Threading;

namespace CodeGlyphX.Internal;

internal static partial class SpecialistImageDecoder {
    private static bool IsBarState(SymbolFormat format) => SymbolCapabilities.Get(format).Family == SymbolFamily.Postal
        || format == SymbolFormat.PharmacodeTwoTrack;

    private static bool TryBarStates(MaskView view, ImageRegion region, SymbolFormat format, CancellationToken token,
        out DetectedSymbol symbol, out int rotation, out bool mirrored) {
        symbol = null!; rotation = 0; mirrored = false;
        var bars = new List<(int left, int right, int top, int bottom)>();
        for (var x = region.X; x < region.X + region.Width;) {
            if (DecodeBudget.ShouldAbort(token)) return false;
            var top = region.Y + region.Height; var bottom = -1;
            for (var y = region.Y; y < region.Y + region.Height; y++) if (view.Get(x, y)) { top = Math.Min(top, y); bottom = y; }
            if (bottom < 0) { x++; continue; }
            var left = x++;
            while (x < region.X + region.Width) {
                var nextTop = region.Y + region.Height; var nextBottom = -1;
                for (var y = region.Y; y < region.Y + region.Height; y++) if (view.Get(x, y)) { nextTop = Math.Min(nextTop, y); nextBottom = y; }
                if (nextBottom < 0) break;
                top = Math.Min(top, nextTop); bottom = Math.Max(bottom, nextBottom); x++;
            }
            bars.Add((left, x - 1, top, bottom));
            if (bars.Count > 512) return false;
        }
        if (bars.Count < 2) return false;
        var widths = new List<int>(); var pitches = new List<int>();
        for (var i = 0; i < bars.Count; i++) {
            widths.Add(bars[i].right - bars[i].left + 1);
            if (i > 0) pitches.Add(bars[i].left - bars[i - 1].left);
        }
        widths.Sort(); pitches.Sort();
        var barWidth = widths[widths.Count / 2]; var pitch = pitches[pitches.Count / 2];
        if (pitch < barWidth * 1.25 || pitch > barWidth * 8) return false;
        for (var i = 0; i < bars.Count; i++) {
            if (Math.Abs(bars[i].right - bars[i].left + 1 - barWidth) > Math.Max(1, barWidth * .35)) return false;
            if (i > 0 && Math.Abs(bars[i].left - bars[i - 1].left - pitch) > Math.Max(1, pitch * .2)) return false;
        }
        var rows = format == SymbolFormat.Daft || format == SymbolFormat.Mailmark4State ? 3
            : format == SymbolFormat.PharmacodeTwoTrack || format == SymbolFormat.Postnet || format == SymbolFormat.Planet ? 2 : 8;
        var modules = new BitMatrix(bars.Count * 2 - 1, rows);
        for (var i = 0; i < bars.Count; i++) {
            var bar = bars[i];
            var upper = bar.top < region.Y + region.Height * .22;
            var lower = bar.bottom >= region.Y + region.Height * .78;
            for (var y = 0; y < rows; y++) {
                modules[i * 2, y] = rows == 2 ? (y == 0 ? upper : lower)
                    : rows == 3 ? (y == 0 ? upper : y == 2 ? lower : true)
                    : y < 3 ? upper : y > 4 ? lower : true;
            }
        }
        return TryOrientations(modules, format, token, out symbol, out rotation, out mirrored);
    }
}
