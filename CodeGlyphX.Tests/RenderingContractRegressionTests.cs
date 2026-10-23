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
        if (format != OutputFormat.Html || HasCanonicalContours(svg)) {
            var coverage = ReadVectorCoverage(svg, size);
            foreach (var origin in new[] { (4, 4), (size - 3, 4), (4, size - 3) }) {
                for (var y = 0; y < 7; y++) for (var x = 0; x < 7; x++) {
                    var dark = x == 0 || y == 0 || x == 6 || y == 6 || (x >= 2 && x <= 4 && y >= 2 && y <= 4);
                    foreach (var fraction in new[] { 0.25, 0.75 })
                        Assert.Equal(dark ? Rgba32.Black : Rgba32.White,
                            coverage(origin.Item1 + x + fraction, origin.Item2 + y + fraction));
                }
                Assert.Equal(Rgba32.White, coverage(origin.Item1 - 0.5, origin.Item2 + 3.5));
                Assert.Equal(Rgba32.White, coverage(origin.Item1 + 3.5, origin.Item2 - 0.5));
            }
            return;
        }
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
        var qr = QR.Encode("FANCY-FINDER");
        var svg = ReadSvg(qr.Render(format, new QrRenderOptions {
            ModuleSize = 8, QuietZone = 4, Style = QrRenderStyle.Fancy
        }));
        if (format != OutputFormat.Html || HasCanonicalContours(svg)) {
            var coverage = ReadVectorCoverage(svg, qr.Size);
            Assert.Equal(Rgba32.White, coverage(7.5, 3.5));
            Assert.Equal(Rgba32.White, coverage(5.5, 7.5));
            Assert.Equal(Rgba32.White, coverage(9.5, 7.5));
            Assert.Equal(Rgba32.Black, coverage(7.5, 7.5));
            Assert.NotEqual(Rgba32.White, coverage(7.5, 4.5));
            return;
        }
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
                OuterGradient = new QrGradientOptions { Type = QrGradientType.Horizontal, EndColor = new Rgba32(128, 0, 0) },
                InnerGradient = new QrGradientOptions { Type = QrGradientType.Vertical, EndColor = new Rgba32(0, 0, 128) }
            }
        }));
        var coverage = ReadVectorCoverage(svg, qr.Size);
        var size = qr.Modules.Width;
        for (var i = 0; i < 3; i++) {
            var x = i == 1 ? size - 3 : 4;
            var y = i == 2 ? size - 3 : 4;
            Assert.True(coverage(x + 0.5, y + 3.5).R < coverage(x + 6.5, y + 3.5).R);
            Assert.True(coverage(x + 3.5, y + 2.1).B < coverage(x + 3.5, y + 4.9).B);
            Assert.Equal(Rgba32.White, coverage(x + 1.5, y + 3.5));
            Assert.Equal(Rgba32.White, coverage(x - 0.5, y + 3.5));
        }
    }

    [Theory]
    [InlineData(OutputFormat.Svg)]
    [InlineData(OutputFormat.Svgz)]
    [InlineData(OutputFormat.Html)]
    public void StyledExportsPreserveProtectedMetadataAndSupportedPatternGeometry(OutputFormat format) {
        var qr = CreateVersionSeven();
        var options = new QrRenderOptions {
            ModuleSize = 8, QuietZone = 4, ModuleShape = QrModuleShape.Circle, ModuleScale = 0.5,
            ArtGuardrailsEnabled = false,
            ForegroundGradient = new QrGradientOptions { StartColor = new Rgba32(255, 0, 0), EndColor = new Rgba32(0, 0, 255) }
        };
        var protectedSvg = ReadSvg(qr.Render(format, options));
        if (format != OutputFormat.Html || HasCanonicalContours(protectedSvg)) {
            var coverage = ReadVectorCoverage(protectedSvg, qr.Size);
            // Encoded format/version/fixed-dark cells stay solid; timing and alignment are
            // deliberately styled in Wave1, so their distinct geometry is checked separately.
            foreach (var cell in FunctionalCells(qr).Skip(3))
                foreach (var fraction in new[] { 0.1, 0.5, 0.9 })
                    Assert.Equal(Rgba32.Black, coverage(cell.x + 4 + fraction, cell.y + 4 + fraction));
            Assert.NotEqual(Rgba32.White, coverage(12.5, 10.5));
            Assert.Equal(Rgba32.White, coverage(12.05, 10.05));
            Assert.NotEqual(Rgba32.White, coverage(26.5, 26.5));
            Assert.Equal(Rgba32.White, coverage(25.5, 26.5));
            options.ProtectFunctionalPatterns = false;
            var unprotected = ReadVectorCoverage(ReadSvg(qr.Render(format, options)), qr.Size);
            var formatCell = FunctionalCells(qr)[4];
            Assert.Equal(Rgba32.White, unprotected(formatCell.x + 4.05, formatCell.y + 4.05));
            return;
        }
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
            QuietZone = 4, ArtGuardrailsEnabled = false, ForegroundGradient = new QrGradientOptions {
                StartColor = new Rgba32(255, 0, 0), EndColor = new Rgba32(0, 0, 255)
            }
        }));
        if (format != OutputFormat.Html || HasCanonicalContours(svg)) {
            var coverage = ReadVectorCoverage(svg, qr.Size);
            foreach (var cell in FunctionalCells(qr).Skip(3))
                Assert.Equal(Rgba32.Black, coverage(cell.x + 4.5, cell.y + 4.5));
            // The timing center retains its intended gradient instead of becoming plain black.
            var coloredTiming = coverage(12.5, 10.5);
            Assert.True(coloredTiming.R > 0 || coloredTiming.B > 0);
            Assert.Equal(Rgba32.White, coverage(12.5, 11.5));
            return;
        }
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

    private static bool HasCanonicalContours(XElement svg) {
        var viewBox = ((string?)svg.Attribute("viewBox") ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return viewBox.Length == 4 && viewBox[2] == (string?)svg.Attribute("width")
            && svg.Descendants().Where(e => e.Name.LocalName == "path")
                .All(e => Regex.IsMatch((string?)e.Attribute("fill") ?? "", @"^#[0-9a-fA-F]{6}$"));
    }

    // Interpret the complete rectangular subpaths emitted by canonical SVG contours. Requiring
    // every command to be consumed prevents a missing/unsupported path from becoming white pixels
    // and passing a negative assertion. Sampling the exported geometry avoids XML element coupling.
    internal static Func<double, double, Rgba32> ReadVectorCoverage(XElement svg, int moduleCount) {
        var width = int.Parse((string)svg.Attribute("width")!, CultureInfo.InvariantCulture);
        var height = int.Parse((string)svg.Attribute("height")!, CultureInfo.InvariantCulture);
        var pixels = new Rgba32[width * height];
        var paths = svg.Descendants().Where(e => e.Name.LocalName == "path").ToArray();
        Assert.NotEmpty(paths);
        foreach (var path in paths) {
            var fill = (string)path.Attribute("fill")!;
            Assert.Matches(@"^#[0-9a-fA-F]{6}$", fill);
            var color = uint.Parse(fill.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var alpha = (string?)path.Attribute("fill-opacity");
            var ink = new Rgba32((byte)(color >> 16), (byte)(color >> 8), (byte)color,
                alpha is null ? (byte)255 : (byte)Math.Round(double.Parse(alpha, CultureInfo.InvariantCulture) * 255));
            var data = (string)path.Attribute("d")!;
            var rectangles = Regex.Matches(data, @"M(\d+) (\d+)h(\d+)v(\d+)h-(\d+)z");
            Assert.Equal(data, string.Concat(rectangles.Cast<Match>().Select(m => m.Value)));
            foreach (Match rectangle in rectangles) {
                int Value(int group) => int.Parse(rectangle.Groups[group].Value, CultureInfo.InvariantCulture);
                var x = Value(1); var y = Value(2); var w = Value(3); var h = Value(4);
                Assert.Equal(w, Value(5));
                Assert.InRange(x + w, 1, width);
                Assert.InRange(y + h, 1, height);
                for (var yy = y; yy < y + h; yy++) for (var xx = x; xx < x + w; xx++) pixels[yy * width + xx] = ink;
            }
        }
        var scale = width / (double)(moduleCount + 8);
        return (x, y) => pixels[(int)(y * scale) * width + (int)(x * scale)];
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
