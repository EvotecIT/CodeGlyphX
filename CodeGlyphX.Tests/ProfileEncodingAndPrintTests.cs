using System;
using System.Linq;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Printing;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class ProfileEncodingAndPrintTests {
    private const string HibcData = "A999BJC5D6E71";

    [Fact]
    public void PackagePrint_QualifiesCaptionWeightIndependentlyOfReadableWeight() {
        Assert.True(IntelligentMailPackage.TryParse("9212391234567812345670", out var package));
        var font = BarcodePrintLayoutTests.Font();
        var options = new BarcodePrintOptions { Font = font, FontIsBold = true, CaptionFont = font, CaptionFontIsBold = true, FontSizePoints = 11, CaptionSizePoints = 11 };
        var declared = BarcodePrint.Layout(package, options: options);
        options.CaptionFontIsBold = false;
        var strengthened = BarcodePrint.Layout(package, options: options);
        Assert.True(strengthened.Text[0].InkBounds.Width > declared.Text[0].InkBounds.Width);
        Assert.Equal(declared.Text[1].InkBounds, strengthened.Text[1].InkBounds);
        Assert.False(strengthened.HasErrors, string.Join("; ", strengthened.Findings.Where(f => f.IsError).Select(f => f.Message)));
    }

    [Fact]
    public void Bc412CheckedInput_RejectsAChangedSecondCharacter() {
        var checkedValue = Barcode.Encode(SymbolFormat.Bc412, "ZXYZ0LPV", new BarcodeEncodingOptions()).HumanReadableText!.ToCharArray();
        checkedValue[1] = checkedValue[1] == '0' ? '1' : '0';
        Assert.Throws<ArgumentException>(() => Barcode.Encode(SymbolFormat.Bc412, new string(checkedValue), new BarcodeEncodingOptions { CheckMode = BarcodeCheckMode.Validate }));
    }

    [Theory]
    [InlineData(LinearBarcodeProfile.Identcode, "01234567890", "01.234 567.890 5", 203)]
    [InlineData(LinearBarcodeProfile.Identcode, "01234567890", "01.234 567.890 5", 300)]
    [InlineData(LinearBarcodeProfile.Identcode, "01234567890", "01.234 567.890 5", 600)]
    [InlineData(LinearBarcodeProfile.Leitcode, "0123456789012", "01234.567.890.12 8", 203)]
    [InlineData(LinearBarcodeProfile.Leitcode, "0123456789012", "01234.567.890.12 8", 300)]
    [InlineData(LinearBarcodeProfile.Leitcode, "0123456789012", "01234.567.890.12 8", 600)]
    public void GermanPostalProfiles_KeepFieldGroupingAndSelectAFeasibleWholePixelPitch(LinearBarcodeProfile profile, string value, string text, int dpi) {
        var layout = BarcodePrint.Layout(profile, value, new BarcodePrintOptions { Font = BarcodePrintLayoutTests.Font(), Dpi = dpi });
        Assert.False(layout.HasErrors, string.Join(";", layout.Findings.Select(f => f.Message)));
        Assert.Equal(text, Assert.Single(layout.Text).Text);
        Assert.InRange(layout.XDimensionMm, .375, .5);
        Assert.Equal(Math.Round(layout.ModulePitchPixels), layout.ModulePitchPixels, 7);
        var bars = layout.Paths.Where(path => path.Role == "bars").ToArray();
        Assert.Equal(25, bars.Max(path => path.Bounds.Bottom), 7);
        Assert.True(bars.Min(path => path.Bounds.X) >= Math.Max(5, 10 * layout.XDimensionMm));
    }

    [Fact]
    public void PhysicalProfileDefaults_PreserveMandatoryReadableCharactersAndReportImpossibleSizes() {
        var font = BarcodePrintLayoutTests.Font();
        Assert.Equal("PZN - 12345678", Assert.Single(BarcodePrint.Layout(LinearBarcodeProfile.Pzn8, "1234567", new BarcodePrintOptions { Font = font }).Text).Text);
        Assert.Equal("A123456788", Assert.Single(Barcode.Create(SymbolFormat.Code32, "12345678").LayoutPrint(new BarcodePrintOptions { Font = font }).Text).Text);
        Assert.Equal("PRODUCT-123", Assert.Single(Barcode.Create(SymbolFormat.Code128, "PRODUCT-123").LayoutPrint(new BarcodePrintOptions { Font = font }).Text).Text);
        var impossible = BarcodePrint.Layout(LinearBarcodeProfile.Leitcode, "0123456789012", new BarcodePrintOptions { Font = font, Dpi = 30 });
        Assert.Contains(impossible.Findings, f => f.Code == "ModuleSize" && f.IsError);
        var outside = BarcodePrint.Layout(LinearBarcodeProfile.Leitcode, "0123456789012", new BarcodePrintOptions { Font = font, XDimensionMm = 1 });
        Assert.Contains(outside.Findings, f => f.Code == "ModuleSize" && f.IsError);
    }

    [Theory]
    [InlineData(SymbolFormat.Code39)]
    [InlineData(SymbolFormat.Code128)]
    [InlineData(SymbolFormat.QrCode)]
    [InlineData(SymbolFormat.Aztec)]
    [InlineData(SymbolFormat.DataMatrix)]
    [InlineData(SymbolFormat.Pdf417)]
    [InlineData(SymbolFormat.MicroPdf417)]
    [InlineData(SymbolFormat.CodablockF)]
    public void HibcCheckedInput_EncodesOneFlagAndCheckAndDecodesOriginalData(SymbolFormat carrier) {
        var checkedPayload = Hibc.PrepareStructured(HibcData);
        var options = new HibcEncodingOptions { InputMode = HibcInputMode.CheckedPayload, ValidateStructure = true };
        if (carrier is SymbolFormat.Code39 or SymbolFormat.Code128) {
            var symbol = Hibc.EncodeLinear(carrier, checkedPayload, options);
            var modules = symbol.Segments.SelectMany(run => Enumerable.Repeat(run.IsBar, run.Modules)).ToArray();
            Assert.True(Hibc.TryDecodeLinear(carrier, modules, out var decoded));
            Assert.Equal(HibcData, decoded);
            Assert.Equal(Hibc.HumanReadable(checkedPayload), symbol.HumanReadableText);
        } else {
            var symbol = Hibc.EncodeMatrix(carrier, checkedPayload, options);
            Assert.True(Hibc.TryDecodeMatrix(carrier, symbol.Modules, out var decoded));
            Assert.Equal(HibcData, decoded);
        }
        var layout = BarcodePrint.LayoutHibc(carrier, checkedPayload, options,
            new BarcodePrintOptions { Font = BarcodePrintLayoutTests.Font() });
        Assert.Equal(Hibc.HumanReadable(checkedPayload), Assert.Single(layout.Text).Text);
        Assert.Throws<ArgumentException>(() => BarcodePrint.LayoutHibc(carrier, checkedPayload.Substring(0, checkedPayload.Length - 1) + "?", options));
    }

    [Fact]
    public void HibcCarrierSettings_PreserveQrVersionRectanglesAndPdfColumnChoices() {
        var qr = Hibc.EncodeMatrix(SymbolFormat.QrCode, HibcData, new HibcEncodingOptions {
            Qr = new QrEncodingOptions { MinVersion = 2, MaxVersion = 2, ErrorCorrectionLevel = QrErrorCorrectionLevel.H }
        });
        Assert.Equal(25, qr.Width);
        var rectangle = Hibc.EncodeMatrix(SymbolFormat.DataMatrix, HibcData, new HibcEncodingOptions {
            DataMatrix = new DataMatrixEncodingOptions { Shape = DataMatrixShape.Rectangular, Rows = 12, Columns = 36 }
        });
        Assert.Equal(36, rectangle.Width); Assert.Equal(12, rectangle.Height);
        Assert.True(Hibc.TryDecodeMatrix(SymbolFormat.DataMatrix, rectangle.Modules, out var decoded));
        Assert.Equal(HibcData, decoded);
        var pdf = Assert.IsType<Pdf417Symbol>(Hibc.EncodeMatrix(SymbolFormat.Pdf417, HibcData, new HibcEncodingOptions {
            Pdf417 = new Pdf417EncodeOptions { MinColumns = 4, MaxColumns = 4, ErrorCorrectionLevel = 2 }
        }));
        Assert.Equal(4, pdf.Columns); Assert.Equal(2, pdf.ErrorCorrectionLevel);
        foreach (var columns in new[] { 1, 4 }) {
            var micro = Hibc.EncodeMatrix(SymbolFormat.MicroPdf417, HibcData, new HibcEncodingOptions { MicroPdf417 = new MicroPdf417EncodeOptions { Columns = columns } });
            Assert.True(Hibc.TryDecodeMatrix(SymbolFormat.MicroPdf417, micro.Modules, out decoded));
            Assert.Equal(HibcData, decoded);
        }
        Assert.Throws<ArgumentException>(() => Hibc.EncodeMatrix(SymbolFormat.Aztec, HibcData, new HibcEncodingOptions { Qr = new QrEncodingOptions() }));
        Assert.Throws<ArgumentException>(() => Hibc.EncodeMatrix(SymbolFormat.QrCode, HibcData, new HibcEncodingOptions { Qr = new QrEncodingOptions { Fnc1Mode = QrFnc1Mode.FirstPosition } }));
        Assert.Throws<ArgumentException>(() => Hibc.EncodeMatrix(SymbolFormat.DataMatrix, HibcData, new HibcEncodingOptions { DataMatrix = new DataMatrixEncodingOptions { ReaderProgramming = true } }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Hibc.EncodeLinear(SymbolFormat.Code39, HibcData, new HibcEncodingOptions { InputMode = (HibcInputMode)99 }));
    }

    [Theory]
    [InlineData(203)]
    [InlineData(300)]
    [InlineData(600)]
    public void PackagePrint_IncludesBannerGroupedPicIdentificationBarsAndMeasuredClearances(int dpi) {
        Assert.True(IntelligentMailPackage.TryParse("42012345" + Gs1.GroupSeparator + "9212391234567812345670", out var package));
        var options = new BarcodePrintOptions { Dpi = dpi, Font = BarcodePrintLayoutTests.Font(), XDimensionMm = .508 };
        var layout = BarcodePrint.Layout(package, options: options);
        Assert.False(layout.HasErrors, string.Join(";", layout.Findings.Select(f => f.Message)));
        Assert.Equal(new[] { "USPS TRACKING #", "9212 3912 3456 7812 3456 70" }, layout.Text.Select(t => t.Text));
        var upper = layout.Text[0].InkBounds; var lower = layout.Text[1].InkBounds;
        var bars = layout.Paths.Where(path => path.Role == "bars").ToArray();
        Assert.Equal(.75 * 25.4, bars.Max(path => path.Bounds.Bottom), 7);
        Assert.Equal(.1875 * 25.4, -upper.Bottom, 7);
        Assert.Equal((.75 + .1875) * 25.4, lower.Y, 7);
        var identification = layout.Paths.Where(path => path.Role == "identification").OrderBy(path => path.Bounds.Y).ToArray();
        Assert.Equal(2, identification.Length);
        Assert.Equal(.047 * 25.4, identification[0].Bounds.Height, 7);
        Assert.Equal(.031 * 25.4, upper.Y - identification[0].Bounds.Bottom, 7);
        Assert.Equal(.031 * 25.4, identification[1].Bounds.Y - lower.Bottom, 7);
        Assert.Null(options.HumanReadableText); Assert.False(options.SyntheticBold);
        var scan = SymbolScanner.Scan(layout.Render(OutputFormat.Png).ToArray(), new ScanOptions { Formats = new[] { SymbolFormat.Gs1Code128 }, TimeoutMilliseconds = 5000 });
        Assert.Equal(package.ElementString, Assert.Single(scan.Symbols).Text);
    }

    [Fact]
    public void PackagePrint_ReportsInvalidSizeAndPaintAndRequiresCorrectTypography() {
        Assert.True(IntelligentMailPackage.TryParse("9212391234567812345670", out var package));
        Assert.Throws<ArgumentException>(() => BarcodePrint.Layout(package));
        var options = new BarcodePrintOptions { Font = BarcodePrintLayoutTests.Font(), BarHeightMm = 10, FontSizePoints = 4, CaptionSizePoints = 4,
            Foreground = BarcodePaint.Solid(new Rgba32(20, 20, 20)) };
        var layout = BarcodePrint.Layout(package, options: options);
        Assert.Contains(layout.Findings, f => f.Code == "PackageBarHeight" && f.IsError);
        Assert.Contains(layout.Findings, f => f.Code == "PackageCapHeight" && f.IsError);
        Assert.Contains(layout.Findings, f => f.Code == "PackageBlackOnWhite" && f.IsError);
        Assert.Throws<ArgumentException>(() => BarcodePrint.Layout(package, "lowercase banner", options));
    }

    [Fact]
    public void Gs1ReadableText_UsesAiParenthesesAndOmitsOnlyMachineUrl() {
        const string message = "(01)09506000134352(10)ABC123(8200)https://example.org/";
        Assert.Equal("(01)09506000134352(10)ABC123", Gs1.HumanReadable(message));
        Assert.Contains("8200https://example.org/", Gs1.ElementString(message));
        Assert.Throws<FormatException>(() => Gs1.HumanReadable("(01)09506000134353"));
        var font = BarcodePrintLayoutTests.Font();
        var layout = Barcode.Create(SymbolFormat.Gs1Code128, message).LayoutPrint(new BarcodePrintOptions { Font = font });
        Assert.Equal("(01)09506000134352(10)ABC123", Assert.Single(layout.Text).Text);
        var small = Barcode.Create(SymbolFormat.Gs1Code128, message).LayoutPrint(new BarcodePrintOptions { Font = font, FontSizePoints = 4 });
        Assert.Contains(small.Findings, finding => finding.Code == "TextTooSmall" && finding.IsError);
    }
}
