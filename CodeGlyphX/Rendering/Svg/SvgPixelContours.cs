using CodeGlyphX.Rendering;
using System;
using System.Collections.Generic;
using System.Text;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Svg;

// Pixel-edge vector contours preserve the canonical QR renderer's shape masks, joins and functional patterns.
// Runs which continue on successive rows become one rectangle. No bitmap is embedded in the SVG.
internal static class SvgPixelContours {
    internal static void Append(StringBuilder output, ReadOnlySpan<byte> pixels, int stride, int left, int top, int size, Rgba32 ink) {
        var active = new Dictionary<long, Rectangle>();
        for (var y = 0; y <= size; y++) {
            var next = new Dictionary<long, Rectangle>();
            if (y < size) {
                for (var x = 0; x < size;) {
                    if (!IsInk(pixels, (top + y) * stride + (left + x) * 4, ink)) { x++; continue; }
                    var start = x++;
                    while (x < size && IsInk(pixels, (top + y) * stride + (left + x) * 4, ink)) x++;
                    var key = ((long)start << 32) | (uint)x;
                    if (active.TryGetValue(key, out var rect)) { rect.Height++; active.Remove(key); }
                    else rect = new Rectangle(left + start, top + y, x - start);
                    next.Add(key, rect);
                }
            }
            foreach (var rect in active.Values) {
                output.Append('M').Append(rect.X).Append(' ').Append(rect.Y).Append('h').Append(rect.Width)
                    .Append('v').Append(rect.Height).Append("h-").Append(rect.Width).Append('z');
            }
            active = next;
        }
    }
    private static bool IsInk(ReadOnlySpan<byte> pixels, int p, Rgba32 ink) => pixels[p] == ink.R && pixels[p + 1] == ink.G && pixels[p + 2] == ink.B;
    private sealed class Rectangle {
        internal readonly int X, Y, Width;
        internal int Height = 1;
        internal Rectangle(int x, int y, int width) { X = x; Y = y; Width = width; }
    }
}
