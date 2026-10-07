using CodeGlyphX.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svg;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class SvgQrRendererTests {
    [Theory]
    [InlineData(QrModuleShape.ConnectedRounded)]
    [InlineData(QrModuleShape.ConnectedSquircle)]
    public void Render_RejectsConnectedModulesInsteadOfApproximatingThem(QrModuleShape shape) {
        var qr = QR.Encode("SVG-CONNECTED-MODULES");

        var direct = Assert.Throws<NotSupportedException>(() => SvgQrRenderer.Render(qr.Modules, new QrSvgRenderOptions {
            ModuleShape = shape
        }));
        var facade = Assert.Throws<NotSupportedException>(() => qr.Render(OutputFormat.Svg, new QrRenderOptions {
            ModuleShape = shape
        }));

        Assert.Contains(nameof(QrSvgRenderOptions.ModuleShape), direct.Message);
        Assert.Equal(facade.Message, direct.Message);
    }

    [Theory]
    [InlineData("Glow")]
    [InlineData("PerEyeColors")]
    [InlineData("PerEyeGradients")]
    [InlineData("AccentRing")]
    [InlineData("AccentRay")]
    [InlineData("AccentStripe")]
    [InlineData("Sparkle")]
    [InlineData("ConnectedOuterEye")]
    [InlineData("ConnectedInnerEye")]
    public void Render_RejectsEyeAppearanceThatSvgCannotPreserve(string appearance) {
        var qr = QR.Encode("SVG-EYE-APPEARANCE");
        var eyes = appearance switch {
            "Glow" => new QrEyeOptions { UseFrame = true, FrameStyle = QrEyeFrameStyle.Glow, GlowRadiusPx = 4 },
            "PerEyeColors" => new QrEyeOptions { OuterColors = new[] { Rgba32.Black, Rgba32.White, Rgba32.Black } },
            "PerEyeGradients" => new QrEyeOptions { InnerGradients = new[] { new QrGradientOptions(), new QrGradientOptions(), new QrGradientOptions() } },
            "AccentRing" => new QrEyeOptions { AccentRingCount = 1 },
            "AccentRay" => new QrEyeOptions { AccentRayCount = 1 },
            "AccentStripe" => new QrEyeOptions { AccentStripeCount = 1 },
            "Sparkle" => new QrEyeOptions { SparkleCount = 1 },
            "ConnectedOuterEye" => new QrEyeOptions { OuterShape = QrModuleShape.ConnectedRounded },
            "ConnectedInnerEye" => new QrEyeOptions { InnerShape = QrModuleShape.ConnectedSquircle },
            _ => throw new ArgumentOutOfRangeException(nameof(appearance))
        };

        var direct = Assert.Throws<NotSupportedException>(() => SvgQrRenderer.Render(qr.Modules, new QrSvgRenderOptions { Eyes = eyes }));
        var facade = Assert.Throws<NotSupportedException>(() => qr.Render(OutputFormat.Svg, new QrRenderOptions { Eyes = eyes }));

        Assert.Contains("Eyes.", direct.Message);
        Assert.Equal(facade.Message, direct.Message);
    }

    [Fact]
    public void RenderToStream_RejectsUnsupportedAppearanceBeforeWritingOutput() {
        var qr = QR.Encode("SVG-STREAM-APPEARANCE");
        using var stream = new MemoryStream();
        var options = new QrSvgRenderOptions { Eyes = new QrEyeOptions { AccentRingCount = 1 } };

        Assert.Throws<NotSupportedException>(() => SvgQrRenderer.RenderToStream(qr.Modules, options, stream));

        Assert.Equal(0, stream.Length);
        Assert.True(stream.CanWrite);
    }

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
