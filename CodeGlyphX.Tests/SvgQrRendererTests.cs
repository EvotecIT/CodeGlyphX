using System;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svg;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class SvgQrRendererTests {
    [Fact]
    public void Render_With_ForegroundGradient_Uses_Path() {
        var qr = QrCodeEncoder.EncodeText("SVG-GRADIENT");
        var svg = SvgQrRenderer.Render(qr.Modules, new QrSvgRenderOptions {
            ForegroundGradient = new QrPngGradientOptions {
                Type = QrPngGradientType.Horizontal,
                StartColor = new Rgba32(0, 0, 0),
                EndColor = new Rgba32(255, 0, 0)
            }
        });

        Assert.Contains("<path", svg, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, CountSubstring(svg, "<rect"));
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
