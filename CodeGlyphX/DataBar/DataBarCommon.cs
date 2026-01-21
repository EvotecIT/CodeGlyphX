using System;

namespace CodeGlyphX.DataBar;

internal static class DataBarCommon {
    internal static int[] GetWidths(int value, int n, int elements, int maxWidth, int noNarrow) {
        var widths = new int[elements];
        var narrowMask = 0;
        var bar = 0;

        for (bar = 0; bar < elements - 1; bar++) {
            var elmWidth = 1;
            var mask = narrowMask | (1 << bar);
            for (;; elmWidth++, mask &= ~(1 << bar)) {
                var subVal = GetCombinations(n - elmWidth - 1, elements - bar - 2);
                if ((noNarrow == 0) && (mask == 0)
                    && (n - elmWidth - (elements - bar - 1) >= elements - bar - 1)) {
                    subVal -= GetCombinations(n - elmWidth - (elements - bar), elements - bar - 2);
                }
                if (elements - bar - 1 > 1) {
                    var lessVal = 0;
                    for (var mxwElement = n - elmWidth - (elements - bar - 2);
                         mxwElement > maxWidth; mxwElement--) {
                        lessVal += GetCombinations(n - elmWidth - mxwElement - 1, elements - bar - 3);
                    }
                    subVal -= lessVal * (elements - 1 - bar);
                } else if (n - elmWidth > maxWidth) {
                    subVal--;
                }

                value -= subVal;
                if (value < 0) {
                    value += subVal;
                    break;
                }
            }
            widths[bar] = elmWidth;
            n -= elmWidth;
            narrowMask = mask;
        }

        widths[bar] = n;
        return widths;
    }

    internal static int GetValue(ReadOnlySpan<int> widths, int n, int elements, int maxWidth, int noNarrow) {
        if (widths.Length < elements) return -1;
        var total = 0;
        for (var i = 0; i < elements; i++) {
            var w = widths[i];
            if (w < 1 || w > maxWidth) return -1;
            total += w;
        }
        if (total != n) return -1;

        var value = 0;
        var narrowMask = 0;
        var remaining = n;

        for (var bar = 0; bar < elements - 1; bar++) {
            var current = widths[bar];
            for (var elmWidth = 1; elmWidth < current; elmWidth++) {
                var mask = elmWidth == 1 ? (narrowMask | (1 << bar)) : narrowMask;
                var subVal = GetCombinations(remaining - elmWidth - 1, elements - bar - 2);
                if ((noNarrow == 0) && (mask == 0)
                    && (remaining - elmWidth - (elements - bar - 1) >= elements - bar - 1)) {
                    subVal -= GetCombinations(remaining - elmWidth - (elements - bar), elements - bar - 2);
                }
                if (elements - bar - 1 > 1) {
                    var lessVal = 0;
                    for (var mxwElement = remaining - elmWidth - (elements - bar - 2);
                         mxwElement > maxWidth;
                         mxwElement--) {
                        lessVal += GetCombinations(remaining - elmWidth - mxwElement - 1, elements - bar - 3);
                    }
                    subVal -= lessVal * (elements - 1 - bar);
                } else if (remaining - elmWidth > maxWidth) {
                    subVal--;
                }
                value += subVal;
            }

            remaining -= current;
            if (current == 1) {
                narrowMask |= 1 << bar;
            } else {
                narrowMask &= ~(1 << bar);
            }
        }

        return value;
    }

    internal static int GetCombinations(int n, int r) {
        var maxDenom = 0;
        var minDenom = 0;

        if (n - r > r) {
            minDenom = r;
            maxDenom = n - r;
        } else {
            minDenom = n - r;
            maxDenom = r;
        }

        var val = 1;
        var j = 1;

        for (var i = n; i > maxDenom; i--) {
            val *= i;
            if (j <= minDenom) {
                val /= j;
                j++;
            }
        }

        for (; j <= minDenom; j++) {
            val /= j;
        }

        return val;
    }
}
