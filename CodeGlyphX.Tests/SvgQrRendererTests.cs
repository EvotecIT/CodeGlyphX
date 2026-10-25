using CodeGlyphX.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svg;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class SvgQrRendererTests {
    [Theory]
    [InlineData(QrModuleShape.ConnectedRounded)]
    [InlineData(QrModuleShape.ConnectedSquircle)]
    public void Render_ExportsConnectedModulesAsVectorGeometry(QrModuleShape shape) {
        var qr = QR.Encode("SVG-CONNECTED-MODULES");

        var direct = XElement.Parse(SvgQrRenderer.Render(qr.Modules, new QrSvgRenderOptions {
            ModuleShape = shape
        }));
        var facade = XElement.Parse(qr.Render(OutputFormat.Svg, new QrRenderOptions {
            ModuleShape = shape
        }).GetText());

        AssertVectorArtwork(direct);
        AssertVectorArtwork(facade);
        var square = SvgQrRenderer.Render(qr.Modules, new QrSvgRenderOptions());
        Assert.NotEqual(XElement.Parse(square).ToString(), direct.ToString());
        var directPixels = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions { ModuleShape = shape }, out var width, out var height, out _);
        AssertVectorCoverageMatchesPixels(direct, qr.Size, directPixels, width, height);
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
    public void Render_ExportsEyeAppearanceAsVectorGeometry(string appearance) {
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

        // Accent-only options need an allowed drawing surface to produce visible marks.
        eyes.SparkleSeed = 1234;
        eyes.AccentRingSeed = 1234;
        eyes.AccentRaySeed = 1234;
        eyes.AccentStripeSeed = 1234;
        eyes.SparkleAllowOnQrBackground = true;
        eyes.AccentRingAllowOnQrBackground = true;
        eyes.AccentRayAllowOnQrBackground = true;
        eyes.AccentStripeAllowOnQrBackground = true;
        var direct = XElement.Parse(SvgQrRenderer.Render(qr.Modules, new QrSvgRenderOptions { Eyes = eyes }));
        var facade = XElement.Parse(qr.Render(OutputFormat.Svg, new QrRenderOptions { Eyes = eyes }).GetText());

        AssertVectorArtwork(direct);
        AssertVectorArtwork(facade);
        Assert.Equal("http://www.w3.org/2000/svg", direct.Name.NamespaceName);
        Assert.Equal("http://www.w3.org/2000/svg", facade.Name.NamespaceName);
        var directPixels = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions { Eyes = eyes }, out var width, out var height, out _);
        AssertVectorCoverageMatchesPixels(direct, qr.Size, directPixels, width, height);
        var facadePixels = qr.RenderPixels(out width, out height, out _, new QrRenderOptions { Eyes = eyes });
        AssertVectorCoverageMatchesPixels(facade, qr.Size, facadePixels, width, height);
    }

    [Fact]
    public void RenderToStream_ExportsStyledArtwork_AndLeavesCallerStreamOpen() {
        var qr = QR.Encode("SVG-STREAM-APPEARANCE");
        using var stream = new MemoryStream();
        var options = new QrSvgRenderOptions { Eyes = new QrEyeOptions { AccentRingCount = 1 } };

        SvgQrRenderer.RenderToStream(qr.Modules, options, stream);

        var root = XElement.Parse(Encoding.UTF8.GetString(stream.ToArray()).TrimStart('\uFEFF'));
        AssertVectorArtwork(root);
        Assert.True(stream.CanWrite);
        stream.WriteByte(0);
    }

    [Fact]
    public void Render_With_ForegroundGradient_PreservesVectorPaint() {
        var qr = QrCodeEncoder.EncodeText("SVG-GRADIENT");
        var gradient = new QrGradientOptions {
            Type = QrGradientType.Horizontal,
            StartColor = new Rgba32(0, 0, 0),
            EndColor = new Rgba32(255, 0, 0)
        };
        var svg = SvgQrRenderer.Render(qr.Modules, new QrSvgRenderOptions {
            ForegroundGradient = gradient
        });

        var root = XElement.Parse(svg);
        AssertVectorArtwork(root);
        var pixels = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions { ForegroundGradient = gradient },
            out var width, out var height, out _);
        AssertVectorCoverageMatchesPixels(root, qr.Size, pixels, width, height);
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
        Assert.Equal(firstIds.Length, firstIds.Distinct().Count());
        Assert.Equal(secondIds.Length, secondIds.Distinct().Count());
        Assert.Empty(firstIds.Intersect(secondIds));
        // Gradients may be baked into canonical colored vector contours. The caller's changed
        // colors must affect the export, while any live fragment references remain local.
        Assert.NotEqual(first.ToString(), second.ToString());
        foreach (var root in new[] { first, second }) {
            AssertVectorArtwork(root);
            var ids = root.Descendants().Attributes("id").Select(a => a.Value).ToArray();
            Assert.True(ids.Length > 0 || root.Descendants().Attributes("fill").Select(a => a.Value).Distinct().Count() > 2);
            foreach (var fill in root.Descendants().Attributes("fill").Where(a => a.Value.StartsWith("url(#", StringComparison.Ordinal)))
                Assert.Contains(fill.Value.Substring(5, fill.Value.Length - 6), ids);
        }
    }

    private static void AssertVectorArtwork(XElement root) {
        Assert.Contains(root.Descendants(), e => e.Name.LocalName == "path" && !string.IsNullOrWhiteSpace((string?)e.Attribute("d")));
        Assert.DoesNotContain(root.Descendants(), e => e.Name.LocalName is "image" or "foreignObject");
    }

    private static void AssertVectorCoverageMatchesPixels(XElement root, int moduleCount, byte[] expected, int width, int height) {
        var sample = RenderingContractRegressionTests.ReadVectorCoverage(root, moduleCount);
        var moduleSize = width / (double)(moduleCount + 8);
        var actual = new byte[expected.Length];
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            var pixel = sample((x + 0.5) / moduleSize, (y + 0.5) / moduleSize);
            var p = (y * width + x) * 4;
            actual[p] = pixel.R; actual[p + 1] = pixel.G; actual[p + 2] = pixel.B; actual[p + 3] = pixel.A;
        }
        Assert.Equal(expected, actual);
    }

}
