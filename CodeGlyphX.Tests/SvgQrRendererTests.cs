using CodeGlyphX.Rendering;
using System;
using System.Linq;
using System.Xml.Linq;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svg;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class SvgQrRendererTests {
    [Fact]
    public void Render_With_ForegroundGradient_Uses_Path() {
        var qr = QrCodeEncoder.EncodeText("SVG-GRADIENT");
        var svg = SvgQrRenderer.Render(qr.Modules, new QrSvgRenderOptions {
            ForegroundGradient = new QrGradientOptions {
                Type = QrGradientType.Horizontal,
                StartColor = new Rgba32(0, 0, 0),
                EndColor = new Rgba32(255, 0, 0)
            }
        });

        Assert.Contains("<path", svg, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, CountSubstring(svg, "<rect"));
    }

    [Fact]
    public void InlineExportsKeepAllGradientReferencesWithinTheirOwnDefinitions() {
        var qr = QrCodeEncoder.EncodeText("SVG-GRADIENT");
        var gradient = new QrGradientOptions { StartColor = new(20, 100, 30), EndColor = new(100, 20, 100) };
        var options = new QrSvgRenderOptions { ForegroundGradient = gradient, Eyes = new() { OuterGradient = gradient, InnerGradient = gradient } };
        var first = XElement.Parse(SvgQrRenderer.Render(qr.Modules, options));
        gradient.StartColor = new(150, 20, 30);
        var second = XElement.Parse(SvgQrRenderer.Render(qr.Modules, options));
        var firstIds = first.Descendants().Attributes("id").Select(a => a.Value).ToArray();
        var secondIds = second.Descendants().Attributes("id").Select(a => a.Value).ToArray();
        Assert.Equal(7, firstIds.Length); Assert.Equal(7, secondIds.Length);
        Assert.Empty(firstIds.Intersect(secondIds));
        foreach (var root in new[] { first, second }) {
            var ids = root.Descendants().Attributes("id").Select(a => a.Value).ToArray();
            foreach (var fill in root.Descendants().Attributes("fill").Where(a => a.Value.StartsWith("url(#", StringComparison.Ordinal)))
                Assert.Contains(fill.Value.Substring(5, fill.Value.Length - 6), ids);
        }
    }

    private static int CountSubstring(string input, string token) {
        var count = 0;
        var index = 0;
        while (true) {
            index = input.IndexOf(token, index, StringComparison.OrdinalIgnoreCase);
            if (index < 0) break;
            count++;
            index += token.Length;
        }
        return count;
    }
}
