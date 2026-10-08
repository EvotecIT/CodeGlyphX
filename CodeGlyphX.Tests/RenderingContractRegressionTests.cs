using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Eps;
using CodeGlyphX.Rendering.Pdf;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svg;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class RenderingContractRegressionTests {
    [Theory]
    [InlineData(OutputFormat.Svg)]
    [InlineData(OutputFormat.Svgz)]
    [InlineData(OutputFormat.Html)]
    public void FramedFindersPreserveQuietZoneAndFiveModuleOpening(OutputFormat format) {
        var qr = QR.Encode("FRAMED-FINDER");
        var svg = ReadSvg(qr.Render(format, new QrRenderOptions {
            ModuleSize = 8, QuietZone = 4, Eyes = new QrEyeOptions { UseFrame = true }
        }));
        var size = qr.Modules.Width;
        foreach (var origin in new[] { (4, 4), (size - 3, 4), (4, size - 3) }) {
            AssertRect(svg, origin.Item1, origin.Item2, 7, "#000000");
            AssertRect(svg, origin.Item1 + 1, origin.Item2 + 1, 5, "#FFFFFF");
            AssertRect(svg, origin.Item1 + 2, origin.Item2 + 2, 3, "#000000");
        }
    }

    [Theory]
    [InlineData(OutputFormat.Svg)]
    [InlineData(OutputFormat.Html)]
    public void FancyFindersKeepTheOpeningAtTheQuietZoneOffset(OutputFormat format) {
        var svg = ReadSvg(QR.Encode("FANCY-FINDER").Render(format, new QrRenderOptions {
            ModuleSize = 8, QuietZone = 4, Style = QrRenderStyle.Fancy
        }));
        var opening = svg.Descendants().First(e => e.Name.LocalName == "rect"
            && (string?)e.Attribute("fill") == "#FFFFFF" && (string?)e.Attribute("width") == "5");
        Assert.Equal("5", (string?)opening.Attribute("x"));
        Assert.Equal("5", (string?)opening.Attribute("y"));
        var dot = svg.Descendants().First(e => e.Name.LocalName == "circle");
        Assert.Equal("7.5", (string?)dot.Attribute("cx"));
        Assert.Equal("7.5", (string?)dot.Attribute("cy"));
    }

    [Fact]
    public void EyeGradientsUseTheSameQuietZoneCoordinatesAsTheirShapes() {
        var qr = QR.Encode("FINDER-GRADIENT");
        var svg = XElement.Parse(SvgQrRenderer.Render(qr.Modules, new QrSvgRenderOptions {
            QuietZone = 4,
            Eyes = new QrEyeOptions {
                UseFrame = true,
                OuterGradient = new QrGradientOptions { Type = QrGradientType.Horizontal },
                InnerGradient = new QrGradientOptions { Type = QrGradientType.Vertical }
            }
        }));
        var gradients = svg.Descendants().Where(e => e.Name.LocalName == "linearGradient").ToArray();
        var size = qr.Modules.Width;
        for (var i = 0; i < 3; i++) {
            var x = i == 1 ? size - 3 : 4;
            var y = i == 2 ? size - 3 : 4;
            Assert.Equal(x.ToString(CultureInfo.InvariantCulture), (string?)gradients[i * 2].Attribute("x1"));
            Assert.Equal(y.ToString(CultureInfo.InvariantCulture), (string?)gradients[i * 2].Attribute("y1"));
            Assert.Equal((x + 7).ToString(CultureInfo.InvariantCulture), (string?)gradients[i * 2].Attribute("x2"));
            Assert.Equal((x + 2).ToString(CultureInfo.InvariantCulture), (string?)gradients[i * 2 + 1].Attribute("x1"));
            Assert.Equal((y + 2).ToString(CultureInfo.InvariantCulture), (string?)gradients[i * 2 + 1].Attribute("y1"));
            Assert.Equal((y + 5).ToString(CultureInfo.InvariantCulture), (string?)gradients[i * 2 + 1].Attribute("y2"));
        }
    }

    [Theory]
    [InlineData(OutputFormat.Svg)]
    [InlineData(OutputFormat.Svgz)]
    [InlineData(OutputFormat.Html)]
    public void StyledExportsKeepTimingAlignmentFormatAndVersionCellsSolid(OutputFormat format) {
        var qr = CreateVersionSeven();
        var options = new QrRenderOptions {
            ModuleSize = 8, QuietZone = 4, ModuleShape = QrModuleShape.Circle, ModuleScale = 0.5,
            ForegroundGradient = new QrGradientOptions { StartColor = new Rgba32(255, 0, 0), EndColor = new Rgba32(0, 0, 255) }
        };
        var protectedSvg = ReadSvg(qr.Render(format, options));
        foreach (var cell in FunctionalCells(qr)) {
            Assert.True(HasSolidSvgModule(protectedSvg, cell.x + 4, cell.y + 4), $"Unprotected functional cell ({cell.x}, {cell.y}).");
        }
        Assert.Contains(protectedSvg.Descendants(), e => e.Name.LocalName == "circle");

        options.ProtectFunctionalPatterns = false;
        var decorativeSvg = ReadSvg(qr.Render(format, options));
        Assert.False(HasSolidSvgModule(decorativeSvg, 12, 10));
    }

    [Theory]
    [InlineData(OutputFormat.Svg)]
    [InlineData(OutputFormat.Html)]
    public void PlainGeometryGradientKeepsProtectedCellsInSolidForeground(OutputFormat format) {
        var qr = CreateVersionSeven();
        var svg = ReadSvg(qr.Render(format, new QrRenderOptions {
            QuietZone = 4, ForegroundGradient = new QrGradientOptions {
                StartColor = new Rgba32(255, 0, 0), EndColor = new Rgba32(0, 0, 255)
            }
        }));
        foreach (var cell in FunctionalCells(qr)) {
            Assert.True(HasSolidSvgModule(svg, cell.x + 4, cell.y + 4));
        }
        Assert.Contains(svg.Descendants(), e => e.Name.LocalName == "path" && ((string?)e.Attribute("fill"))?.StartsWith("url(#", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void PdfAndEpsVectorGeometryKeepFunctionalCellsAtFullModuleSize() {
        var qr = CreateVersionSeven();
        var options = new QrPngRenderOptions {
            ModuleSize = 8, QuietZone = 4, ModuleShape = QrModuleShape.Circle, ModuleScale = 0.5
        };
        var pdf = Encoding.ASCII.GetString(QrPdfRenderer.Render(qr.Modules, options));
        var eps = QrEpsRenderer.Render(qr.Modules, options);
        Assert.DoesNotContain("/Subtype /Image", pdf);
        Assert.DoesNotContain("colorimage", eps);
        var height = (qr.Modules.Width + 8) * 8;
        foreach (var cell in FunctionalCells(qr)) {
            var rect = $"{(cell.x + 4) * 8} {height - (cell.y + 5) * 8} 8 8";
            Assert.Contains(rect + " re f", pdf);
            Assert.Contains(rect + " rectfill", eps);
        }
        options.ProtectFunctionalPatterns = false;
        var unprotectedPdf = Encoding.ASCII.GetString(QrPdfRenderer.Render(qr.Modules, options));
        Assert.DoesNotContain($"96 {height - 88} 8 8 re f", unprotectedPdf);
    }

    [Theory]
    [InlineData("ForegroundPattern")]
    [InlineData("OuterColors")]
    [InlineData("InnerColors")]
    [InlineData("OuterGradients")]
    [InlineData("InnerGradients")]
    [InlineData("Glow")]
    [InlineData("Sparkle")]
    [InlineData("Ring")]
    [InlineData("Ray")]
    [InlineData("Stripe")]
    public void PdfAndEpsUseRasterFallbackForEffectsTheirVectorSinksCannotRepresent(string effect) {
        var qr = QR.Encode("RASTER-EFFECT");
        var options = new QrPngRenderOptions { ModuleSize = 4, Eyes = new QrEyeOptions { UseFrame = true } };
        var eyes = options.Eyes;
        switch (effect) {
            case "ForegroundPattern": options.ForegroundPattern = new QrForegroundPatternOptions { Color = new Rgba32(100, 0, 0) }; break;
            case "OuterColors": eyes.OuterColors = new[] { new Rgba32(100, 0, 0), Rgba32.Black, Rgba32.Black }; break;
            case "InnerColors": eyes.InnerColors = new[] { new Rgba32(100, 0, 0), Rgba32.Black, Rgba32.Black }; break;
            case "OuterGradients": eyes.OuterGradients = new[] { new QrGradientOptions(), new QrGradientOptions(), new QrGradientOptions() }; break;
            case "InnerGradients": eyes.InnerGradients = new[] { new QrGradientOptions(), new QrGradientOptions(), new QrGradientOptions() }; break;
            case "Glow": eyes.FrameStyle = QrEyeFrameStyle.Single; eyes.GlowRadiusPx = 2; break;
            case "Sparkle": eyes.SparkleCount = 1; break;
            case "Ring": eyes.AccentRingCount = 1; break;
            case "Ray": eyes.AccentRayCount = 1; break;
            case "Stripe": eyes.AccentStripeCount = 1; break;
            default: throw new ArgumentOutOfRangeException(nameof(effect));
        }
        Assert.Contains("/Subtype /Image", Encoding.ASCII.GetString(QrPdfRenderer.Render(qr.Modules, options)));
        Assert.Contains("colorimage", QrEpsRenderer.Render(qr.Modules, options));
        using var pdfStream = new MemoryStream();
        QrPdfRenderer.RenderToStream(qr.Modules, options, pdfStream);
        Assert.Contains("/Subtype /Image", Encoding.ASCII.GetString(pdfStream.ToArray()));
        using var epsStream = new MemoryStream();
        QrEpsRenderer.RenderToStream(qr.Modules, options, epsStream);
        Assert.Contains("colorimage", Encoding.ASCII.GetString(epsStream.ToArray()));
    }

    [Fact]
    public void BarcodeAsciiUsesFacadeQuietZoneAndPreservesExplicitTextOptions() {
        var barcode = Barcode.Encode(SymbolFormat.Code128, "ABC");
        var compact = Barcode.Render(barcode, OutputFormat.Ascii, new BarcodeOptions { QuietZone = 0, HeightModules = 99 }).GetText();
        var padded = Barcode.Render(barcode, OutputFormat.Ascii, new BarcodeOptions { QuietZone = 10 }).GetText();
        var compactRows = compact.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        var paddedRows = padded.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        Assert.Equal(barcode.TotalModules, compactRows[0].Length);
        Assert.Equal(compactRows[0].Length + 20, paddedRows[0].Length);
        Assert.Equal(new string(' ', 10) + compactRows[0] + new string(' ', 10), paddedRows[0]);
        Assert.Equal(new BarcodeAsciiRenderOptions().Height, compactRows.Length);

        var explicitText = new BarcodeAsciiRenderOptions { QuietZone = 2, Height = 2, ModuleWidth = 2, Dark = "|", Light = ".", NewLine = "\n" };
        var expected = BarcodeAsciiRenderer.Render(barcode, explicitText);
        var actual = Barcode.Render(barcode, OutputFormat.Ascii, new BarcodeOptions { QuietZone = 10 }, new OutputOptions { BarcodeAscii = explicitText }).GetText();
        Assert.Equal(expected, actual);
        Assert.Equal(2, explicitText.QuietZone);
    }

    private static QrCode CreateVersionSeven() => QR.Encode("PROTECTED-STRUCTURE", new QrEncodingOptions { MinVersion = 7, MaxVersion = 7 });

    private static (int x, int y)[] FunctionalCells(QrCode qr) {
        var format = Enumerable.Range(0, 6).Select(y => (x: 8, y)).First(c => qr.Modules[c.x, c.y]);
        var version = Enumerable.Range(0, 18).Select(i => (x: 34 + i % 3, y: i / 3)).First(c => qr.Modules[c.x, c.y]);
        var cells = new[] { (x: 8, y: 6), (x: 6, y: 8), (x: 22, y: 22), (x: 8, y: 37), format, version };
        Assert.All(cells, cell => Assert.True(qr.Modules[cell.x, cell.y]));
        return cells;
    }

    private static XElement ReadSvg(RenderedOutput output) {
        if (output.Format != OutputFormat.Svgz) return XElement.Parse(output.GetText());
        using var compressed = new MemoryStream(output.ToArray());
        using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);
        return XElement.Parse(reader.ReadToEnd());
    }

    private static void AssertRect(XElement svg, int x, int y, int size, string fill) {
        Assert.Contains(svg.Descendants(), e => e.Name.LocalName == "rect"
            && (string?)e.Attribute("x") == x.ToString(CultureInfo.InvariantCulture)
            && (string?)e.Attribute("y") == y.ToString(CultureInfo.InvariantCulture)
            && (string?)e.Attribute("width") == size.ToString(CultureInfo.InvariantCulture)
            && (string?)e.Attribute("height") == size.ToString(CultureInfo.InvariantCulture)
            && (string?)e.Attribute("fill") == fill);
    }

    private static bool HasSolidSvgModule(XElement svg, int x, int y) {
        foreach (var shape in svg.Descendants().Where(e => (string?)e.Attribute("fill") == "#000000")) {
            if (shape.Name.LocalName == "rect" && (string?)shape.Attribute("x") == x.ToString(CultureInfo.InvariantCulture)
                && (string?)shape.Attribute("y") == y.ToString(CultureInfo.InvariantCulture)
                && (string?)shape.Attribute("width") == "1" && (string?)shape.Attribute("height") == "1"
                && shape.Attribute("rx") is null) return true;
            if (shape.Name.LocalName != "path") continue;
            foreach (Match run in Regex.Matches((string?)shape.Attribute("d") ?? "", @"M(\d+) (\d+)h(\d+)v1h-\d+z")) {
                var start = int.Parse(run.Groups[1].Value, CultureInfo.InvariantCulture);
                var row = int.Parse(run.Groups[2].Value, CultureInfo.InvariantCulture);
                var length = int.Parse(run.Groups[3].Value, CultureInfo.InvariantCulture);
                if (row == y && start <= x && x < start + length) return true;
            }
        }
        return false;
    }
}
