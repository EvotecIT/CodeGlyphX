using System.Collections.Generic;

namespace CodeMatrix.Internal;

internal static class BarcodeSegments {
    public static void AppendBit(List<BarSegment> segments, bool isBar) {
        if (segments.Count > 0) {
            var last = segments[segments.Count - 1];
            if (last.IsBar == isBar) {
                segments[segments.Count - 1] = new BarSegment(isBar, checked(last.Modules + 1));
                return;
            }
        }
        segments.Add(new BarSegment(isBar, 1));
    }

    public static void AppendBits(List<BarSegment> segments, bool[] bits) {
        for (var i = 0; i < bits.Length; i++) {
            AppendBit(segments, bits[i]);
        }
    }

    public static void AppendBits(List<BarSegment> segments, uint bits, int bitCount) {
        for (var i = bitCount - 1; i >= 0; i--) {
            AppendBit(segments, ((bits >> i) & 1u) != 0);
        }
    }
}
