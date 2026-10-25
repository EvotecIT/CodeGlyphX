using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Tests;

// Independent reader for the rectangular coverage contours emitted by styled SVG exports.
// Tests sample actual geometry rather than requiring a particular SVG primitive or embedded image.
internal sealed class SvgCoverageTestReader {
    private readonly List<(int X, int Y, int W, int H, Rgba32 Color)> _rectangles = new();
    internal int Width { get; }
    internal int Height { get; }

    internal SvgCoverageTestReader(XDocument svg) {
        var root = svg.Root ?? throw new ArgumentException("Missing SVG root.");
        Width = int.Parse(root.Attribute("width")!.Value, CultureInfo.InvariantCulture);
        Height = int.Parse(root.Attribute("height")!.Value, CultureInfo.InvariantCulture);
        foreach (var path in root.Elements(XName.Get("path", "http://www.w3.org/2000/svg"))) {
            var rgb = uint.Parse(path.Attribute("fill")!.Value.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var opacity = path.Attribute("fill-opacity")?.Value;
            var alpha = opacity is null ? 255 : (int)Math.Round(double.Parse(opacity, CultureInfo.InvariantCulture) * 255);
            var color = new Rgba32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, (byte)alpha);
            var data = path.Attribute("d")!.Value;
            var consumed = 0;
            foreach (Match match in Regex.Matches(data, @"M(\d+) (\d+)h(\d+)v(\d+)h-\d+z")) {
                if (match.Index != consumed) throw new ArgumentException("Unexpected SVG coverage command.");
                var x = Number(1); var y = Number(2); var w = Number(3); var h = Number(4);
                if (x < 0 || y < 0 || x + w > Width || y + h > Height)
                    throw new ArgumentException("Coverage extends outside the SVG viewport.");
                _rectangles.Add((x, y, w, h, color));
                consumed += match.Length;
                int Number(int group) => int.Parse(match.Groups[group].Value, CultureInfo.InvariantCulture);
            }
            if (consumed != data.Length) throw new ArgumentException("Unparsed SVG coverage data.");
        }
        if (_rectangles.Count == 0) throw new ArgumentException("No SVG coverage contours.");
    }

    internal Rgba32 Sample(int x, int y) {
        foreach (var rectangle in _rectangles)
            if (x >= rectangle.X && x < rectangle.X + rectangle.W && y >= rectangle.Y && y < rectangle.Y + rectangle.H)
                return rectangle.Color;
        return Rgba32.Transparent;
    }
}
