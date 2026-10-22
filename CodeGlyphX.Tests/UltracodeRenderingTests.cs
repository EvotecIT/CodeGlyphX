using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class UltracodeRenderingTests {
    [Theory]
    [InlineData(128, 255)]
    [InlineData(255, 128)]
    [InlineData(128, 128)]
    [InlineData(0, 0)]
    public void SvgAndPngPreserveTheSamePaintAtEveryCellAndQuietZone(int foregroundAlpha, int backgroundAlpha) {
        var symbol = UltracodeEncoder.EncodeText("Colour 123");
        var options = new UltracodeRenderOptions {
            CellSize = 3, QuietZone = 2,
            Foreground = new Rgba32(54, 40, 70, (byte)foregroundAlpha),
            Background = new Rgba32(210, 230, 245, (byte)backgroundAlpha)
        };
        var png = PngReader.DecodeRgba32(symbol.RenderPng(options), out var width, out var height);
        var rectangles = XDocument.Parse(symbol.RenderSvg(options)).Root!.Elements().ToArray();
        for (var y = options.CellSize / 2; y < height; y += options.CellSize)
            for (var x = options.CellSize / 2; x < width; x += options.CellSize) {
                // SVG uses source-over compositing. Keep premultiplied channels until the final comparison.
                var rgba = new double[4];
                foreach (var rectangle in rectangles) {
                    var left = Number(rectangle, "x"); var top = Number(rectangle, "y");
                    if (x < left || y < top || x >= left + Number(rectangle, "width") || y >= top + Number(rectangle, "height")) continue;
                    var alpha = rectangle.Attribute("fill-opacity") is { } opacity
                        ? double.Parse(opacity.Value, CultureInfo.InvariantCulture) : 1;
                    var color = rectangle.Attribute("fill")!.Value;
                    for (var channel = 0; channel < 3; channel++)
                        rgba[channel] = byte.Parse(color.Substring(1 + channel * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                            * alpha + rgba[channel] * (1 - alpha);
                    rgba[3] = alpha + rgba[3] * (1 - alpha);
                }
                var at = (y * width + x) * 4;
                Assert.InRange(Math.Abs(rgba[3] * 255 - png[at + 3]), 0, 1);
                if (rgba[3] == 0) continue; // RGB values of completely transparent pixels have no visible meaning.
                for (var channel = 0; channel < 3; channel++)
                    Assert.InRange(Math.Abs(rgba[channel] / rgba[3] - png[at + channel]), 0, 1);
            }
    }

    private static int Number(XElement rectangle, string attribute) => int.Parse(rectangle.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);
}
