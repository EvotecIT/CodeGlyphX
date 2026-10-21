using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using CodeGlyphX.Printing;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class BarcodePrintLayoutTests {
    [Theory]
    [InlineData(SymbolFormat.Ean, "1234567", "12345670")]
    [InlineData(SymbolFormat.Ean, "590123412345", "5901234123457")]
    [InlineData(SymbolFormat.UpcA, "03600029145", "036000291452")]
    [InlineData(SymbolFormat.UpcE, "425261", "04252614")]
    [InlineData(SymbolFormat.Ean, "590123412345+12", "5901234123457+12")]
    public void RetailBuilderAndExplicitControls_KeepComputedDigitsInTheirReservedGroups(SymbolFormat format, string input, string canonical) {
        var options = new BarcodePrintOptions { Font = Font() };
        var layouts = new[] {
            Barcode.Create(format, input).LayoutPrint(options),
            BarcodePrint.Layout(Barcode.Encode(format, input, new BarcodeEncodingOptions()), options)
        };
        foreach (var layout in layouts) {
            Assert.Equal(canonical.Replace("+", string.Empty), string.Concat(layout.Text.Select(t => t.Text)));
            Assert.True(layout.Text.Count > 1, "Retail groups must not be replaced by a single raw-input line.");
            if (canonical.Contains("+")) {
                var addon = Assert.Single(layout.Text, t => t.Text == "12");
                Assert.True(addon.InkBounds.Bottom < layout.Text[0].InkBounds.Y);
            }
        }
    }

    [Theory]
    [InlineData(203)]
    [InlineData(300)]
    [InlineData(600)]
    public void RetailLayout_ResolvesPitchQuietZonesGuardExtensionsAndDigitGroups(int dpi) {
        var layout = BarcodePrint.Layout(Barcode.Encode(SymbolFormat.Ean, "5901234123457"), new BarcodePrintOptions {
            Dpi = dpi, Font = Font()
        });
        var pitch = Math.Round(.330 * dpi / 25.4);
        var x = pitch * 25.4 / dpi;
        Assert.Equal(pitch, layout.ModulePitchPixels, 7);
        Assert.Equal(113 * x, layout.SymbolBounds.Width, 7); // GS1: 95 encoded modules + 11/7 clear modules.
        var bars = layout.Paths.Where(p => p.Role == "bars").ToArray();
        Assert.Equal(11 * x, bars.Min(p => p.Bounds.X), 7);
        Assert.Equal(106 * x, bars.Max(p => p.Bounds.Right), 7);
        Assert.Equal((22.85 / .330 + 5) * x, bars.Max(p => p.Bounds.Bottom), 7);
        Assert.Equal(new[] { "5", "901234", "123457" }, layout.Text.Select(t => t.Text));
        Assert.Equal(layout.Text[0].Origin.Y + layout.Text[0].Shape.BaselineOffset,
            layout.Text[2].Origin.Y + layout.Text[2].Shape.BaselineOffset, 7);
        Assert.False(layout.HasErrors);
        Assert.True(layout.Bounds.Bottom >= layout.Text.Max(t => t.InkBounds.Bottom));
    }

    [Fact]
    public void RetailSupplement_GetsSeparateTopTextAndPermittedPhysicalGap() {
        var layout = BarcodePrint.Layout(Barcode.Encode(SymbolFormat.Ean, "5901234123457+12"), new BarcodePrintOptions { Font = Font() });
        var addon = Assert.Single(layout.Text, t => t.Text == "12");
        var ordinary = Assert.Single(layout.Text, t => t.Text == "901234");
        Assert.True(addon.InkBounds.Bottom < ordinary.InkBounds.Y);
        Assert.Equal((11 + 95 + 7 + 20 + 5) * layout.XDimensionMm, layout.SymbolBounds.Width, 7);
        var addonBars = layout.Paths.Where(p => p.Role == "bars" && p.Bounds.X >= (11 + 95 + 7) * layout.XDimensionMm - 1e-7).ToArray();
        Assert.NotEmpty(addonBars);
        Assert.All(addonBars, p => Assert.Equal(9 * layout.XDimensionMm, p.Bounds.Y, 7));
    }

    [Fact]
    public void Exports_KeepPhysicalUnitsAndPngDpi_AndQrReadsBack() {
        const string value = "PRINT SIZE Ab09";
        var layout = BarcodePrint.Layout(QrCodeEncoder.EncodeText(value), new BarcodePrintOptions { Dpi = 300 });
        var png = layout.Render(OutputFormat.Png).ToArray();
        Assert.Equal(11811, PngPixelsPerMetre(png));
        var scan = SymbolScanner.Scan(png, new ScanOptions { Formats = new[] { SymbolFormat.QrCode }, TimeoutMilliseconds = 5000 });
        Assert.Equal(value, Assert.Single(scan.Symbols).Text);
        var svg = XDocument.Parse(layout.Render(OutputFormat.Svg).GetText()).Root!;
        Assert.EndsWith("mm", svg.Attribute("width")!.Value);
        Assert.Equal(layout.Bounds.Width, double.Parse(svg.Attribute("width")!.Value.Replace("mm", string.Empty), CultureInfo.InvariantCulture), 6);
        Assert.DoesNotContain(svg.Descendants(), e => e.Name.LocalName == "text");
        var pdf = Encoding.ASCII.GetString(layout.Render(OutputFormat.Pdf).ToArray());
        Assert.Contains("/MediaBox [0 0 " + (layout.Bounds.Width * 72 / 25.4).ToString("0.########", CultureInfo.InvariantCulture), pdf);
        Assert.DoesNotContain("/Subtype /Image", pdf);
        using var stream = new MemoryStream();
        layout.Save(stream, OutputFormat.Pdf);
        Assert.True(stream.CanWrite);
        Assert.True(stream.Length > 100);
    }

    [Fact]
    public void SuppliedFontOutlines_KeepGlyphHolesAndCaseInTheRaster() {
        var layout = BarcodePrint.Layout(Barcode.Encode(SymbolFormat.Code128, "A"), new BarcodePrintOptions {
            Font = Font(), HumanReadableText = "Oo", FontSizePoints = 30, Dpi = 300
        });
        var line = Assert.Single(layout.Text);
        Assert.Equal("Oo", line.Text);
        var pixels = layout.RenderPixels(out var width, out var height);
        var capital = Font().Shape("O", 30 * 25.4 / 72);
        var centerX = line.Origin.X + capital.InkBounds.X + capital.InkBounds.Width / 2;
        var centerY = line.Origin.Y + capital.InkBounds.Y + capital.InkBounds.Height / 2;
        var px = (int)(centerX * 300 / 25.4); var py = (int)(centerY * 300 / 25.4);
        Assert.InRange(px, 0, width - 1); Assert.InRange(py, 0, height - 1);
        Assert.Equal(255, pixels[(py * width + px) * 4]); // Actual glyph counter remains clear.
        var edgeX = (int)((line.Origin.X + capital.InkBounds.X + capital.InkBounds.Width * .08) * 300 / 25.4);
        Assert.True(pixels[(py * width + edgeX) * 4] < 100);
        Assert.Throws<ArgumentException>(() => CompactBarcodeFont.Instance.Shape("lowercase", 4));
        Assert.NotEmpty(CompactBarcodeFont.Instance.Shape("lowercase", 4, false).MissingGlyphs);
    }

    [Fact]
    public void CaptionMinimumAndOverhangingMultilineText_AreMeasuredBeforeExport() {
        var layout = BarcodePrint.Layout(Barcode.Encode(SymbolFormat.Code128, "A"), new BarcodePrintOptions {
            Font = Font(true), HumanReadableText = "fj\nAb09", FontSizePoints = 14,
            Caption = new string('W', 60), CaptionSizePoints = 10, MinimumCaptionSizePoints = 9
        });
        Assert.Equal(3, layout.Text.Count);
        Assert.Contains(layout.Findings, f => f.Code == "CaptionDoesNotFit" && f.IsError);
        Assert.True(layout.LogicalBounds.Y < 0);
        Assert.True(layout.Bounds.X <= layout.InkBounds.X);
        Assert.True(layout.Bounds.Right >= layout.InkBounds.Right);
        Assert.True(layout.Bounds.Bottom >= layout.Text.Where(t => t.Text != new string('W', 60)).Max(t => t.InkBounds.Bottom));
        var caption = Assert.Single(layout.Text, t => t.Text.Length == 60);
        Assert.Equal(9 * 25.4 / 72, caption.EmSizeMm, 7);
    }

    [Fact]
    public void Placement_MeasuresRotatedBoundsAndRequiresExplicitClipping() {
        var layout = BarcodePrint.Layout(QrCodeEncoder.EncodeText("PLACED"));
        var p = new BarcodePrintPlacement { X = 30, Y = 5, RotationDegrees = 90, CanvasWidthMm = 40, CanvasHeightMm = 45 };
        var bounds = p.Measure(layout.Bounds);
        Assert.Equal(30 - layout.Bounds.Height, bounds.X, 7);
        Assert.Equal(5, bounds.Y, 7);
        Assert.Equal(layout.Bounds.Height, bounds.Width, 7);
        var pixels = layout.RenderPixels(out var width, out var height, p);
        var scan = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), new ScanOptions {
            Formats = new[] { SymbolFormat.QrCode }, TimeoutMilliseconds = 5000
        });
        Assert.Equal("PLACED", Assert.Single(scan.Symbols).Text);
        p.CanvasWidthMm = 5;
        Assert.Contains(layout.Preflight(p), f => f.Code == "CanvasClipsLayout" && f.IsError);
        Assert.Throws<ArgumentException>(() => layout.Render(OutputFormat.Svg, p));
        p.AllowClipping = true;
        Assert.NotEmpty(layout.Render(OutputFormat.Svg, p).GetText());
    }

    [Fact]
    public void Paints_OwnTilesAndReportRasterOrUnsupportedExportPaths() {
        var tile = new byte[] { 255, 0, 0, 255, 0, 0, 255, 255 };
        var paint = BarcodePaint.Pattern(tile, 2, 1, 2, 1);
        Array.Clear(tile, 0, tile.Length);
        var layout = BarcodePrint.Layout(new Barcode1D(new[] { new BarSegment(true, 2) }), new BarcodePrintOptions {
            Foreground = paint, XDimensionMm = 1, BarHeightMm = 2, Dpi = 254, SnapToPrinterPixels = false, IncludeHumanReadable = false
        });
        var pixels = layout.RenderPixels(out var width, out _);
        Assert.Equal(255, pixels[(5 * width + 102) * 4]);
        Assert.Equal(0, pixels[(5 * width + 102) * 4 + 2]);
        Assert.Equal(255, pixels[(5 * width + 112) * 4 + 2]);
        Assert.Contains(layout.Preflight(format: OutputFormat.Pdf), f => f.Code == "PdfRasterFallback" && !f.IsError);
        Assert.Contains("/Subtype /Image", Encoding.ASCII.GetString(layout.Render(OutputFormat.Pdf).ToArray()));
        Assert.Contains(layout.Preflight(format: OutputFormat.Eps), f => f.Code == "EpsPaintUnsupported" && f.IsError);
        Assert.Throws<NotSupportedException>(() => layout.Render(OutputFormat.Eps));
        Assert.Contains(layout.Preflight(format: OutputFormat.Webp), f => f.Code == "PhysicalMetadataUnavailable");
        Assert.Contains("data:image/png;base64,", layout.Render(OutputFormat.Svg).GetText());
        Assert.Throws<NotSupportedException>(() => layout.Render(OutputFormat.Pgm));
    }

    [Fact]
    public void PackageText_PreflightsCapHeightAndDeclaredWeight() {
        var options = new BarcodePrintOptions { Font = Font(), HumanReadableText = "9400 1000 0000", TextStyle = BarcodePrintTextStyle.UspsPackage, FontSizePoints = 10 };
        var symbol = Barcode.Encode(SymbolFormat.Gs1Code128, "(01)09506000134352");
        var regular = BarcodePrint.Layout(symbol, options);
        Assert.Contains(regular.Findings, f => f.Code == "PackageTextWeight" && f.IsError);
        options.SyntheticBold = true;
        var synthetic = BarcodePrint.Layout(symbol, options);
        Assert.DoesNotContain(synthetic.Findings, f => f.Code == "PackageTextWeight");
        Assert.True(Assert.Single(synthetic.Text).InkBounds.Width > Assert.Single(regular.Text).InkBounds.Width);
        options.FontSizePoints = 2;
        Assert.Contains(BarcodePrint.Layout(symbol, options).Findings, f => f.Code == "PackageCapHeight" && f.IsError);
    }

    [Fact]
    public void Itf14Bearer_ContainsTheLinearBarsAndPreservesReservedSpace() {
        var layout = BarcodePrint.Layout(Barcode.Encode(SymbolFormat.Itf14, "10012345000017"), new BarcodePrintOptions { Itf14BearerFrame = true });
        var bearers = layout.Paths.Where(p => p.Role == "bearer").ToArray();
        Assert.Equal(4, bearers.Length);
        Assert.Equal(31.75 + 2 * 4.83, layout.SymbolBounds.Height, 7);
        var bars = layout.Paths.Where(p => p.Role == "bars").ToArray();
        Assert.All(bars, p => Assert.Equal(4.83, p.Bounds.Y, 7));
        Assert.Equal(4.83 + 10 * layout.XDimensionMm, bars.Min(p => p.Bounds.X), 7);
    }

    [Fact]
    public void ImpossiblePrinterPitchAndOversizedRaster_AreReportedBeforeAllocation() {
        var impossible = BarcodePrint.Layout(Barcode.Encode(SymbolFormat.Ean, "5901234123457"), new BarcodePrintOptions {
            Dpi = 24, IncludeHumanReadable = false
        });
        Assert.Contains(impossible.Findings, f => f.Code == "ModuleSize" && f.IsError);
        var large = BarcodePrint.Layout(QrCodeEncoder.EncodeText("LARGE"), new BarcodePrintOptions { XDimensionMm = 10, Dpi = 2400 });
        Assert.Contains(large.Preflight(format: OutputFormat.Png), f => f.Code == "RasterPixelLimit" && f.IsError);
        Assert.Throws<ArgumentException>(() => large.Render(OutputFormat.Png));
        Assert.NotEmpty(large.Render(OutputFormat.Svg).GetText());
        Assert.Throws<ArgumentOutOfRangeException>(() => BarcodePrint.Layout(QrCodeEncoder.EncodeText("BAD"), new BarcodePrintOptions { Dpi = 0 }));
        Assert.Throws<ArgumentException>(() => BarcodePrint.Layout(Barcode.Encode(SymbolFormat.Code128, "A"), new BarcodePrintOptions { Format = SymbolFormat.Code39 }));
        var noContrast = BarcodePrint.Layout(QrCodeEncoder.EncodeText("COLOURS"), new BarcodePrintOptions {
            Foreground = BarcodePaint.Solid(Rgba32.White)
        });
        Assert.Contains(noContrast.Findings, f => f.Code == "NoPrintContrast" && f.IsError);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void FractionalPlacement_PreservesContinuousInkAcrossTouchingModules(bool matrix, bool translucent) {
        var paint = BarcodePaint.Solid(new Rgba32(0, 0, 0, translucent ? (byte)128 : (byte)255));
        var options = new BarcodePrintOptions {
            Dpi = 254, XDimensionMm = .33, SnapToPrinterPixels = false,
            Foreground = paint, IncludeHumanReadable = false
        };
        BarcodePrintLayout layout;
        if (matrix) {
            var modules = new BitMatrix(2, 2);
            for (var y = 0; y < 2; y++) for (var x = 0; x < 2; x++) modules[x, y] = true;
            layout = BarcodePrint.Layout(modules, SymbolFormat.QrCode, options);
        } else {
            layout = BarcodePrint.Layout(Barcode.Encode(SymbolFormat.Ean, "5901234123457"), options);
        }
        var placement = new BarcodePrintPlacement { X = .05, Y = .025 };
        var pixels = layout.RenderPixels(out var width, out _, placement);
        var bars = layout.Paths.Where(p => p.Role == "bars").ToArray();
        var first = matrix ? bars[0] : bars.First(p => bars.Any(next => Math.Abs(p.Bounds.Right - next.Bounds.X) < 1e-8));
        var px = (int)Math.Floor(((matrix ? first.Bounds.X + .33 : first.Bounds.Right) + placement.X) * 10);
        var py = (int)Math.Floor(((matrix ? first.Bounds.Bottom : first.Bounds.Height / 2) + placement.Y) * 10);
        var expected = translucent ? (byte)127 : (byte)0;
        Assert.Equal(expected, pixels[(py * width + px) * 4]);
        Assert.Equal(expected, pixels[(py * width + px) * 4 + 1]);
        Assert.Equal(expected, pixels[(py * width + px) * 4 + 2]);
        Assert.Equal(255, pixels[(py * width + px) * 4 + 3]);
    }

    [Fact]
    public void DecorativePdf_PreflightsTheSameRasterLimitAsPng() {
        var layout = BarcodePrint.Layout(QrCodeEncoder.EncodeText("LARGE"), new BarcodePrintOptions {
            XDimensionMm = 10, Dpi = 2400,
            Foreground = BarcodePaint.LinearGradient(new BarcodePoint(0, 0), new BarcodePoint(10, 0), Rgba32.Black, Rgba32.White)
        });
        Assert.Contains(layout.Preflight(format: OutputFormat.Pdf), f => f.Code == "PdfRasterFallback");
        Assert.Contains(layout.Preflight(format: OutputFormat.Pdf), f => f.Code == "RasterPixelLimit" && f.IsError);
        Assert.Throws<ArgumentException>(() => layout.Render(OutputFormat.Pdf));
        Assert.DoesNotContain(layout.Preflight(format: OutputFormat.Svg), f => f.Code == "RasterPixelLimit");
    }

    [Fact]
    public void FractionalColourCells_IntegrateTheirBoundaryWithoutExposingTheBackground() {
        var symbol = UltracodeEncoder.EncodeText("COLOUR SEAMS");
        var layout = BarcodePrint.Layout(symbol, new BarcodePrintOptions { Dpi = 254, XDimensionMm = .3 });
        var placement = new BarcodePrintPlacement { X = .05, Y = .05 };
        var pixels = layout.RenderPixels(out var width, out _, placement);
        var found = false;
        for (var y = 0; y < symbol.Cells.Height && !found; y++) for (var x = 0; x + 1 < symbol.Cells.Width; x++) {
            var a = symbol.Cells[x, y]; var b = symbol.Cells[x + 1, y];
            if (a == b || a == UltracodeColor.White || b == UltracodeColor.White) continue;
            var ca = CellColor(a); var cb = CellColor(b);
            var px = (int)Math.Floor((x + 2) * 3 + .5);
            var py = (int)Math.Floor((y + 1.5) * 3 + .5);
            var at = (py * width + px) * 4;
            Assert.InRange(pixels[at], (ca.R + cb.R) / 2, (ca.R + cb.R + 1) / 2);
            Assert.InRange(pixels[at + 1], (ca.G + cb.G) / 2, (ca.G + cb.G + 1) / 2);
            Assert.InRange(pixels[at + 2], (ca.B + cb.B) / 2, (ca.B + cb.B + 1) / 2);
            Assert.Equal(255, pixels[at + 3]);
            found = true;
            break;
        }
        Assert.True(found, "The independent palette-boundary assertion needs two different adjacent non-white cells.");
    }

    private static Rgba32 CellColor(UltracodeColor color) => color switch {
        UltracodeColor.Black => Rgba32.Black, UltracodeColor.Cyan => new Rgba32(0, 255, 255),
        UltracodeColor.Magenta => new Rgba32(255, 0, 255), UltracodeColor.Yellow => new Rgba32(255, 255, 0),
        UltracodeColor.Green => new Rgba32(0, 255, 0), _ => throw new ArgumentOutOfRangeException(nameof(color))
    };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OverhangingMultilineText_PreservesIndependentSourceOverLayers(bool transparentBackground) {
        var layout = BarcodePrint.Layout(new Barcode1D(new[] { new BarSegment(true, 1) }), new BarcodePrintOptions {
            Dpi = 254, XDimensionMm = .1, BarHeightMm = 1, HumanReadableText = "A\nB",
            Font = new OverhangingFont(), FontSizePoints = 2 * 72 / 25.4,
            TextPaint = BarcodePaint.Solid(new Rgba32(0, 0, 0, 128)),
            Background = BarcodePaint.Solid(new Rgba32(255, 255, 255, transparentBackground ? (byte)0 : (byte)255))
        });
        var pixels = layout.RenderPixels(out var width, out _);
        var first = layout.Text[0];
        var x = (int)Math.Floor((first.InkBounds.X + 1) * 10);
        var single = ((int)Math.Floor((first.Origin.Y + .5) * 10) * width + x) * 4;
        var overlap = ((int)Math.Floor((first.Origin.Y + 1.5) * 10) * width + x) * 4;
        Assert.Equal(transparentBackground ? 0 : 127, pixels[single]);
        Assert.Equal(transparentBackground ? 0 : 63, pixels[overlap]);
        Assert.Equal(transparentBackground ? 128 : 255, pixels[single + 3]);
        Assert.Equal(transparentBackground ? 192 : 255, pixels[overlap + 3]);
        var svg = XDocument.Parse(layout.Render(OutputFormat.Svg).GetText());
        Assert.Equal(2, svg.Descendants().Count(e => e.Name.LocalName == "path" && e.Attribute("fill-opacity") is { } opacity
            && double.Parse(opacity.Value, CultureInfo.InvariantCulture) < 1));
    }

    private sealed class OverhangingFont : IBarcodeFont {
        public string Name => "Ink extends beyond line box";
        public BarcodeTextShape Shape(string text, double emSize, bool strict = true) => new(text, emSize, emSize / 2, emSize / 2,
            new[] { new[] { new BarcodePoint(0, 0), new BarcodePoint(emSize, 0), new BarcodePoint(emSize, emSize), new BarcodePoint(0, emSize) } });
    }

    internal static BarcodeFont Font(bool italic = false) {
        using var stream = typeof(BarcodePrintLayoutTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Fonts.OfficeIMOBaselineSans-" + (italic ? "Italic" : "Regular") + ".ttf");
        Assert.NotNull(stream);
        using var bytes = new MemoryStream(); stream!.CopyTo(bytes);
        return BarcodeFont.FromBytes(bytes.ToArray());
    }

    private static int PngPixelsPerMetre(byte[] png) {
        for (var offset = 8; offset + 12 <= png.Length;) {
            var length = BigEndian(png, offset);
            if (Encoding.ASCII.GetString(png, offset + 4, 4) == "pHYs") {
                Assert.Equal(1, png[offset + 16]);
                Assert.Equal(BigEndian(png, offset + 8), BigEndian(png, offset + 12));
                return BigEndian(png, offset + 8);
            }
            offset += length + 12;
        }
        throw new InvalidOperationException("PNG has no physical resolution.");
    }
    private static int BigEndian(byte[] bytes, int offset) => bytes[offset] << 24 | bytes[offset + 1] << 16 | bytes[offset + 2] << 8 | bytes[offset + 3];
}
