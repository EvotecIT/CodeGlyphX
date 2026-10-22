using System;
using System.Linq;
using CodeGlyphX.Printing;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class ApplicationPrintGeometryTests {
    [Fact]
    public void Pharmacodes_UseAbsoluteClearanceAndHalfHeightTracks() {
        var single = BarcodePrint.Layout(Barcode.Encode(SymbolFormat.Pharmacode, "12345"));
        Assert.InRange(single.XDimensionMm, .3, .7);
        var bars = single.Paths.Where(p => p.Role == "bars").ToArray();
        Assert.Equal(6, bars.Min(p => p.Bounds.X), 7);
        Assert.Equal(6, single.SymbolBounds.Right - bars.Max(p => p.Bounds.Right), 7);
        Assert.All(bars, p => Assert.Equal(8, p.Bounds.Height, 7));
        var dual = BarcodePrint.Layout(MatrixBarcodeEncoder.EncodeSymbol(SymbolFormat.PharmacodeTwoTrack, "12345"));
        bars = dual.Paths.Where(p => p.Role == "bars").OrderBy(p => p.Bounds.X).ToArray();
        Assert.Equal(6, bars[0].Bounds.X, 7);
        Assert.Equal(2 * dual.XDimensionMm, bars[1].Bounds.X - bars[0].Bounds.X, 7);
        Assert.Contains(bars, p => Math.Abs(p.Bounds.Height - 4) < 1e-7);
        Assert.Contains(bars, p => Math.Abs(p.Bounds.Height - 8) < 1e-7);
        Assert.All(bars, p => Assert.True(p.Bounds.Y == 2 || p.Bounds.Y == 6));
        Assert.Equal("12345", Scan(dual, SymbolFormat.PharmacodeTwoTrack));
    }

    [Theory]
    [InlineData(SymbolFormat.Code16K)]
    [InlineData(SymbolFormat.Code49)]
    [InlineData(SymbolFormat.CodablockF)]
    public void StackedCodeFamilies_ReserveTheirAsymmetricHorizontalClearance(SymbolFormat format) {
        var symbol = MatrixBarcodeEncoder.EncodeSymbol(format, "PRINT ROWS");
        var layout = BarcodePrint.Layout(symbol);
        var bars = layout.Paths.Where(p => p.Role == "bars").ToArray();
        Assert.Equal(10 * layout.XDimensionMm, bars.Min(p => p.Bounds.X), 7);
        var right = format == SymbolFormat.CodablockF ? 10 : 1;
        Assert.Equal((symbol.Width + 10 + right) * layout.XDimensionMm, layout.SymbolBounds.Width, 7);
        Assert.Equal(0, bars.Min(p => p.Bounds.Y), 7);
        Assert.Equal("PRINT ROWS", Scan(layout, format));
    }

    [Fact]
    public void OptionalQuietZones_DoNotInventAnApplicationRequirement() {
        var options = new BarcodePrintOptions { IncludeQuietZones = false };
        var dataBar = BarcodePrint.Layout(Barcode.Encode(SymbolFormat.Gs1DataBarLimited, "0952123454321"), options);
        var daft = BarcodePrint.Layout(MatrixBarcodeEncoder.EncodeSymbol(SymbolFormat.Daft, "DAFT"), options);
        var flat = BarcodePrint.Layout(Barcode.Encode(SymbolFormat.Flattermarken, "123"), options);
        var codeOne = BarcodePrint.Layout(CodeOneEncoder.EncodeText("A"), options);
        foreach (var label in new[] { dataBar, daft, flat, codeOne })
            Assert.DoesNotContain(label.Findings, f => f.Code == "QuietZonesOmitted");
        Assert.InRange(flat.XDimensionMm, 2, 3);
        Assert.All(flat.Paths, p => Assert.Equal(10, p.Bounds.Height, 7));
        var shortCode = CodeOneEncoder.EncodeText("12345", new CodeOneEncodingOptions { Version = CodeOneVersion.S10 });
        Assert.Contains(BarcodePrint.Layout(shortCode, options).Findings, f => f.Code == "QuietZonesOmitted" && f.IsError);
        var withQuiet = BarcodePrint.Layout(shortCode);
        Assert.Equal((shortCode.Width + 2) * withQuiet.XDimensionMm, withQuiet.SymbolBounds.Width, 7);
        Assert.Equal((shortCode.Height + 1) * withQuiet.XDimensionMm, withQuiet.SymbolBounds.Height, 7);
    }

    [Fact]
    public void Itf14_PlateFrameAndReadableClearanceKeepAbsoluteDimensions() {
        const string spaced = "1 0012345 00001 7";
        var code = Barcode.Encode(SymbolFormat.Itf14, spaced, new BarcodeEncodingOptions());
        var layout = BarcodePrint.Layout(code, new BarcodePrintOptions {
            Font = BarcodePrintLayoutTests.Font(), Itf14BearerFrame = true, XDimensionMm = .254
        });
        var bars = layout.Paths.Where(p => p.Role == "bars").ToArray();
        Assert.All(bars, p => Assert.Equal(4.83, p.Bounds.Y, 7));
        Assert.Equal(4, layout.Paths.Count(p => p.Role == "bearer"));
        Assert.Equal(4.83 + 10 * layout.XDimensionMm, bars.Min(p => p.Bounds.X), 7);
        Assert.Equal(spaced, Assert.Single(layout.Text).Text);
        Assert.Equal(1.02, layout.Text[0].InkBounds.Y - layout.SymbolBounds.Bottom, 7);
        Assert.Equal(8D / 3, bars.Max(p => p.Bounds.Width) / layout.XDimensionMm, 7);
        Assert.Equal("10012345000017", Scan(layout, SymbolFormat.Itf14));
        Assert.Contains(layout.Findings, f => f.Code == "ApplicationModuleSize" && !f.IsError);
        Assert.False(layout.HasErrors);
        var thin = BarcodePrint.Layout(code, new BarcodePrintOptions { IncludeHumanReadable = false, Itf14BearerThicknessMm = .1 });
        Assert.Contains(thin.Findings, f => f.Code == "BearerThickness" && f.IsError);
    }

    [Theory]
    [InlineData("12345670")]
    [InlineData("1234565")]
    public void MarksAndSpencer_LeavesCenterGuardsAtDigitHeightAndPlacesLettersOutside(string input) {
        var layout = BarcodePrint.Layout(LinearBarcodeProfile.MarksAndSpencer, input, new BarcodePrintOptions { Font = BarcodePrintLayoutTests.Font() });
        Assert.Equal("M" + input + "S", string.Concat(layout.Text.Select(t => t.Text)));
        var bars = layout.Paths.Where(p => p.Role == "bars").ToArray();
        var left = 12 * layout.XDimensionMm;
        var center = bars.Where(p => p.Bounds.X >= left + 31 * layout.XDimensionMm - 1e-7 && p.Bounds.X < left + 36 * layout.XDimensionMm - 1e-7).ToArray();
        Assert.Equal(2, center.Length);
        Assert.All(center, p => Assert.Equal(bars.Max(other => other.Bounds.Height) - 5 * layout.XDimensionMm, p.Bounds.Height, 7));
        Assert.True(layout.Text[0].InkBounds.Right <= left);
        Assert.True(layout.Text[layout.Text.Count - 1].InkBounds.X > left + 67 * layout.XDimensionMm);
        Assert.Equal(BarcodeProfiles.Prepare(LinearBarcodeProfile.MarksAndSpencer, input), Scan(layout, SymbolFormat.Ean));
    }

    [Theory]
    [InlineData(24D / 29)]
    [InlineData(27D / 29)]
    public void MaxiCode_AcceptsThePermittedLatticeRange(double pitch) {
        var layout = BarcodePrint.Layout(MaxiCodeCode.Encode("MAXI SIZE"), new BarcodePrintOptions { XDimensionMm = pitch });
        Assert.Equal(pitch, layout.XDimensionMm, 9);
        Assert.False(layout.HasErrors);
        Assert.Equal("MAXI SIZE", Scan(layout, SymbolFormat.MaxiCode));
    }

    [Fact]
    public void FullAsciiReadableText_BlanksControlsWithoutChangingThePayload() {
        const string data = "a\0b\u007f";
        var symbol = Barcode.Encode(SymbolFormat.Code39, data, new BarcodeEncodingOptions { Code39FullAscii = true, CheckMode = BarcodeCheckMode.None });
        Assert.Equal("*a b *", symbol.HumanReadableText);
        var original = CodeGlyphX.Code39.Code39Encoder.Encode(data, false, true);
        Assert.Equal(original.Segments.Select(s => (s.IsBar, s.Modules)), symbol.Segments.Select(s => (s.IsBar, s.Modules)));
    }

    [Fact]
    public void Coupon_KeepsFixedRowsAndPrintsItsOfferAboveTheSymbol() {
        var input = NorthAmericanCoupon.Prepare("123456", "123456", "1", "1", '0', "123");
        Assert.True(NorthAmericanCoupon.TryParse(input, out var coupon));
        var layout = BarcodePrint.Layout(coupon, options: new BarcodePrintOptions { Font = BarcodePrintLayoutTests.Font(), BarHeightMm = 100 });
        Assert.InRange(layout.XDimensionMm, .254 - 1e-9, .330 + 1e-9);
        Assert.False(layout.HasErrors);
        Assert.Equal(coupon.HumanReadable, Assert.Single(layout.Text).Text);
        Assert.True(layout.Text[0].InkBounds.Bottom < layout.SymbolBounds.Y);
        var normal = BarcodePrint.Layout(coupon, options: new BarcodePrintOptions { Font = BarcodePrintLayoutTests.Font() });
        Assert.Equal(normal.SymbolBounds, layout.SymbolBounds);
        Assert.Equal(coupon.ElementString, Scan(layout, SymbolFormat.Gs1DataBarExpandedStacked));
    }

    [Fact]
    public void IntelligentMail_GroupsTheCheckedFieldsWithoutChangingBars() {
        var symbol = MatrixBarcodeEncoder.EncodeSymbol(SymbolFormat.UspsIntelligentMail, "01234567094987654321-01234567891");
        var layout = BarcodePrint.Layout(symbol, new BarcodePrintOptions { Font = BarcodePrintLayoutTests.Font() });
        Assert.Equal("01 234 567094 987654321 01234 5678 91", Assert.Single(layout.Text).Text);
        Assert.Equal("01234567094987654321-01234567891", Scan(layout, SymbolFormat.UspsIntelligentMail));
        var concatenated = MatrixBarcodeEncoder.EncodeSymbol(SymbolFormat.UspsIntelligentMail, "0123456709498765432101234567891");
        Assert.Equal(symbol.Width, concatenated.Width); Assert.Equal(symbol.Height, concatenated.Height);
        for (var y = 0; y < symbol.Height; y++) for (var x = 0; x < symbol.Width; x++)
            Assert.Equal(symbol.Modules[x, y], concatenated.Modules[x, y]);
        Assert.True(CodeGlyphX.Postal.UspsImbDecoder.TryDecode(concatenated.Modules, out var decoded));
        Assert.Equal("01234567094987654321-01234567891", decoded);
        Assert.Throws<InvalidOperationException>(() => MatrixBarcodeEncoder.EncodeSymbol(SymbolFormat.UspsIntelligentMail, "05234567094987654321"));
        Assert.Throws<InvalidOperationException>(() => MatrixBarcodeEncoder.EncodeSymbol(SymbolFormat.UspsIntelligentMail, "94123567094987654321"));
        var postal = MatrixBarcodeEncoder.EncodeSymbol(SymbolFormat.UspsIntelligentMail, "94009567094987654321");
        Assert.True(CodeGlyphX.Postal.UspsImbDecoder.TryDecode(postal.Modules, out decoded));
        Assert.Equal("94009567094987654321", decoded);
    }

    [Fact]
    public void HibcPrint_SelectsCarrierNominalSizesAndReportsUndersizedModules() {
        var font = BarcodePrintLayoutTests.Font();
        var options = new BarcodePrintOptions { Font = font, SnapToPrinterPixels = false };
        var linear = BarcodePrint.LayoutHibc(SymbolFormat.Code39, "A999BJC5D6E71", options: options);
        var matrix = BarcodePrint.LayoutHibc(SymbolFormat.DataMatrix, "A999BJC5D6E71", options: options);
        Assert.Equal(.254, linear.XDimensionMm, 7);
        Assert.Equal(.381, matrix.XDimensionMm, 7);
        options.XDimensionMm = .1;
        foreach (var format in new[] { SymbolFormat.Code39, SymbolFormat.Code128, SymbolFormat.DataMatrix })
            Assert.Contains(BarcodePrint.LayoutHibc(format, "A999BJC5D6E71", options: options).Findings, f => f.Code == "ModuleSize" && f.IsError);
    }

    [Fact]
    public void Gs1MatrixPrint_UsesElementLinesAndApplicationModuleRanges() {
        const string input = "(01)09506000134352(10)ABC123";
        var readable = Gs1.HumanReadable(input, multiline: true);
        Assert.Equal("(01)09506000134352\n(10)ABC123", readable);
        var font = BarcodePrintLayoutTests.Font();
        var options = new BarcodePrintOptions { Font = font, TextStyle = BarcodePrintTextStyle.Gs1, HumanReadableText = readable };
        var payload = Gs1.ElementString(input);
        var qr = QR.Encode(payload, new QrEncodingOptions { Fnc1Mode = QrFnc1Mode.FirstPosition });
        var dm = CodeGlyphX.DataMatrix.DataMatrixEncoder.EncodeSymbol(payload, new CodeGlyphX.DataMatrix.DataMatrixEncodingOptions { IsGs1 = true });
        var dot = DotCodeCode.Encode(payload, new DotCodeEncodingOptions { IsGs1 = true });
        var labels = new[] { BarcodePrint.Layout(qr, options), BarcodePrint.Layout(dm, options), BarcodePrint.Layout(dot, options) };
        foreach (var layout in labels) {
            Assert.False(layout.HasErrors);
            Assert.Equal(readable.Split('\n'), layout.Text.Select(t => t.Text));
            Assert.Equal(Gs1.ElementString(input), Scan(layout, layout.Format!.Value));
        }
        Assert.InRange(labels[2].XDimensionMm, .380, .990);
        Assert.Equal((dot.Width + 6) * labels[2].XDimensionMm, labels[2].SymbolBounds.Width, 7);
        options.XDimensionMm = .05;
        foreach (var layout in new[] { BarcodePrint.Layout(qr, options), BarcodePrint.Layout(dm, options), BarcodePrint.Layout(dot, options) })
            Assert.Contains(layout.Findings, f => f.Code == "ModuleSize" && f.IsError);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    public void PrintedDotCode_ExcludesCaptionGlyphsFromItsLatticeAndReportedGeometry(int rotation) {
        const string input = "(01)09506000134352(10)ABC123";
        var symbol = DotCodeEncoder.EncodeGs1(input);
        var layout = BarcodePrint.Layout(symbol, new BarcodePrintOptions {
            Font = BarcodePrintLayoutTests.Font(), TextStyle = BarcodePrintTextStyle.Gs1,
            HumanReadableText = Gs1.HumanReadable(input, true), Caption = "TOBACCO 123", CaptionSizePoints = 6, MinimumCaptionSizePoints = 6
        });
        var placement = new BarcodePrintPlacement { RotationDegrees = rotation };
        var result = SymbolScanner.Scan(layout.Render(OutputFormat.Png, placement).ToArray(), new ScanOptions { Formats = new[] { SymbolFormat.DotCode }, TimeoutMilliseconds = TestBudget.Adjust(5000) });
        var found = Assert.Single(result.Symbols);
        Assert.Equal(Gs1.ElementString(input), found.Text);
        var grid = found.Geometry!.Bounds;
        var measured = placement.Measure(layout.SymbolBounds);
        Assert.InRange(Math.Abs(grid.Width - measured.Width * layout.Dpi / 25.4), 0, 7 * layout.ModulePitchPixels);
        Assert.InRange(Math.Abs(grid.Height - measured.Height * layout.Dpi / 25.4), 0, 7 * layout.ModulePitchPixels);
    }

    private static string Scan(BarcodePrintLayout layout, SymbolFormat format) {
        var result = SymbolScanner.Scan(layout.Render(OutputFormat.Png).ToArray(), new ScanOptions { Formats = new[] { format }, TimeoutMilliseconds = TestBudget.Adjust(5000) });
        Assert.True(result.Symbols.Count == 1, format + ": " + result.Status + " / " + result.Symbols.Count);
        return Assert.Single(result.Symbols).Text;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    public void PrintedCodeOne_KeepsCaptionAndReadableLinesOutsideTheRecoveredGrid(int rotation) {
        const string input = "Package Łódź";
        var code = CodeOneEncoder.EncodeText(input);
        var label = BarcodePrint.Layout(code, new BarcodePrintOptions {
            Font = BarcodePrintLayoutTests.Font(), Caption = "Code One", HumanReadableText = "Lot 123"
        });
        var placement = new BarcodePrintPlacement { RotationDegrees = rotation };
        var scan = SymbolScanner.Scan(label.Render(OutputFormat.Png, placement).ToArray(), new ScanOptions {
            Formats = new[] { SymbolFormat.CodeOne }, TimeoutMilliseconds = TestBudget.Adjust(5000)
        });
        var found = Assert.Single(scan.Symbols);
        Assert.Equal(input, found.Text);
        var expected = placement.Measure(label.SymbolBounds);
        Assert.InRange(Math.Abs(found.Geometry!.Bounds.Width - expected.Width * label.Dpi / 25.4), 0, 2);
        Assert.InRange(Math.Abs(found.Geometry.Bounds.Height - expected.Height * label.Dpi / 25.4), 0, 2);
    }
}
