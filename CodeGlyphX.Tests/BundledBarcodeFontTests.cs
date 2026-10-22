using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CodeGlyphX.Printing;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class BundledBarcodeFontTests {
    private static BarcodeFont Font(bool ocrB) => ocrB ? BarcodeFont.OcrB : BarcodeFont.OcrA;

    [Theory]
    [InlineData(false, "GG Ocra", 7.15, 1.07, 4.72, 7.4)]
    [InlineData(true, "CodeGlyphX Scan B", 8.65234375, 1.259765625, 6.1328125, 8.5546875)]
    public void BundledFace_PreservesAsciiCaseAndIndependentDesignMetrics(bool ocrB, string name,
        double advance, double left, double width, double height) {
        var font = Font(ocrB);
        Assert.StartsWith(name, font.Name);
        var ascii = new string(Enumerable.Range(32, 95).Select(value => (char)value).ToArray());
        var all = font.Shape(ascii, 10);
        Assert.Equal(ascii, all.Text);
        Assert.Empty(all.MissingGlyphs);
        Assert.False(all.HasIncompleteShaping);
        // FontTools BoundsPen and hmtx, independently measured from the pinned upstream fonts.
        var b = font.Shape("B", 10);
        Assert.Equal(advance, b.AdvanceWidth, 6);
        Assert.Equal(left, b.InkBounds.X, 6);
        Assert.Equal(width, b.InkBounds.Width, 3);
        Assert.Equal(height, b.InkBounds.Height, 3);
        Assert.True(font.Shape("a", 10).InkBounds.Height < font.Shape("A", 10).InkBounds.Height);
        Assert.True(font.Shape(" ", 10).AdvanceWidth > 0);
        Assert.Empty(font.Shape(" ", 10).Contours);
        Assert.Throws<ArgumentException>(() => font.Shape("\u4e16", 10));
        Assert.Equal(new[] { 0x4e16 }, font.Shape("\u4e16", 10, strict: false).MissingGlyphs);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BundledFace_IsSharedAndCanShapeConcurrently(bool ocrB) {
        var font = Font(ocrB);
        var expected = font.Shape("Batch aB09", 4);
        Parallel.For(0, 16, _ => {
            Assert.Same(font, Font(ocrB));
            var actual = font.Shape("Batch aB09", 4);
            Assert.Equal(expected.AdvanceWidth, actual.AdvanceWidth);
            Assert.Equal(expected.InkBounds, actual.InkBounds);
            Assert.NotSame(expected.Contours, actual.Contours);
        });
    }

    [Theory]
    [InlineData(false, 203)]
    [InlineData(false, 300)]
    [InlineData(false, 600)]
    [InlineData(true, 203)]
    [InlineData(true, 300)]
    [InlineData(true, 600)]
    public void BundledFace_RetailSupplementExportsRealOutlinesAndReadsBack(bool ocrB, int dpi) {
        const string payload = "5901234123457+12";
        var layout = BarcodePrint.Layout(Barcode.Encode(SymbolFormat.Ean, payload), new BarcodePrintOptions {
            Dpi = dpi, Font = Font(ocrB), FontSizePoints = 8, IncludeHumanReadable = true
        });
        Assert.False(layout.HasErrors);
        Assert.Equal("590123412345712", string.Concat(layout.Text.Select(line => line.Text)));
        Assert.All(layout.Text, line => Assert.NotEmpty(line.Shape.Contours));
        var png = layout.Render(OutputFormat.Png).ToArray();
        var scan = SymbolScanner.Scan(png, new ScanOptions { Formats = new[] { SymbolFormat.Ean }, TimeoutMilliseconds = TestBudget.Adjust(5000) });
        Assert.Contains(scan.Symbols, symbol => symbol.Text == payload);
        var svg = XDocument.Parse(layout.Render(OutputFormat.Svg).GetText());
        Assert.DoesNotContain(svg.Descendants(), node => node.Name.LocalName == "text");
        Assert.Contains(svg.Descendants(), node => node.Name.LocalName == "path");
        var pdf = Encoding.ASCII.GetString(layout.Render(OutputFormat.Pdf).ToArray());
        Assert.StartsWith("%PDF-", pdf);
        Assert.DoesNotContain("/Subtype /Image", pdf);
        Assert.DoesNotContain("/FontFile", pdf);
    }
}
