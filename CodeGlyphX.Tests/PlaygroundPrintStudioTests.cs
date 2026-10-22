#if NET10_0
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed partial class PlaygroundLivePreviewTests {
    [Theory]
    [InlineData("OcrA")]
    [InlineData("OcrB")]
    public async Task PrintStudio_BundledFontsNeedNoUploadAndFontEditsClearExports(string font) {
        await using var renderer = new PlaygroundRenderer();
        await renderer.Start();
        await renderer.Change("onchange", e => e.Attribute("id") == "print-font-choice", font);
        await renderer.Click("Prepare print label");
        var png = Convert.FromBase64String(renderer.Elements().Single(e => e.Attribute("download") == "label.png").Attribute("href")!.Split(',')[1]);
        var scan = SymbolScanner.Scan(png, new ScanOptions { Formats = new[] { SymbolFormat.Ean }, TimeoutMilliseconds = 5000 });
        Assert.Equal("5901234123457", Assert.Single(scan.Symbols).Text);
        Assert.Contains(renderer.Elements(), e => e.Attribute("download") == "label.svg");
        Assert.Contains(renderer.Elements(), e => e.Attribute("download") == "label.pdf");
        await renderer.Change("onchange", e => e.Attribute("id") == "print-font-choice", "None");
        Assert.DoesNotContain(renderer.Elements(), e => e.Attribute("download")?.StartsWith("label.", StringComparison.Ordinal) == true);
        await renderer.Click("Prepare print label");
        Assert.Contains(renderer.Elements(), e => e.Attribute("role") == "alert");
        Assert.Empty(renderer.Errors);
    }

    [Theory]
    [InlineData("Package", "(420")]
    [InlineData("Gs1Code128", "(01)09506000134353")]
    public async Task PrintStudio_InvalidGs1ShowsAnAlertAndKeepsExportsCleared(string format, string input) {
        await using var renderer = new PlaygroundRenderer();
        await renderer.Start();
        await renderer.Change("onchange", e => e.Attribute("id") == "print-format", format);
        await renderer.Change("oninput", e => e.Attribute("id") == "print-content", input);
        await renderer.Click("Prepare print label");
        Assert.Contains(renderer.Elements(), e => e.Attribute("role") == "alert" && !string.IsNullOrWhiteSpace(e.Text));
        Assert.DoesNotContain(renderer.Elements(), e => e.Attribute("download")?.StartsWith("label.", StringComparison.Ordinal) == true);
        Assert.Empty(renderer.Errors);
    }

    [Fact]
    public async Task PrintStudio_UsesApplicationOwnersForProfileAndHibcCarriers() {
        await using var renderer = new PlaygroundRenderer();
        await renderer.Start();
        await renderer.Change("onchange", e => e.Attribute("id") == "print-format", "Profile:Ean14");
        await renderer.Change("onchange", e => e.Attribute("type") == "checkbox" && e.Attribute("id") == "print-readable", false);
        await renderer.Click("Prepare print label");
        Assert.Contains(renderer.Elements(), e => e.Attribute("download") == "label.png");
        await renderer.Change("onchange", e => e.Attribute("id") == "print-format", "Hibc");
        await renderer.Change("onchange", e => e.Attribute("id") == "print-hibc-carrier", "QrCode");
        await renderer.Change("oninput", e => e.Attribute("id") == "print-hibc-version", "2");
        await renderer.Click("Prepare print label");
        var png = Convert.FromBase64String(renderer.Elements().Single(e => e.Attribute("download") == "label.png").Attribute("href")!.Split(',')[1]);
        var decoded = SymbolScanner.Scan(png, new ScanOptions { Formats = new[] { SymbolFormat.QrCode }, TimeoutMilliseconds = 5000 });
        Assert.Equal(Hibc.Prepare("A999BJC5D6E71"), Assert.Single(decoded.Symbols).Text);
        Assert.Empty(renderer.Errors);
    }

    [Fact]
    public async Task PrintStudio_DimensionInputClearsExportsAndRejectsAnEvenDotGrid() {
        await using var renderer = new PlaygroundRenderer();
        await renderer.Start();
        await renderer.Change("onchange", e => e.Attribute("id") == "print-format", "DotCode");
        await renderer.Change("oninput", e => e.Attribute("id") == "print-content", "DIMENSIONS");
        await renderer.Change("oninput", e => e.Attribute("id") == "print-dot-width", "31");
        await renderer.Change("oninput", e => e.Attribute("id") == "print-dot-height", "20");
        await renderer.Click("Prepare print label");
        var png = Convert.FromBase64String(renderer.Elements().Single(e => e.Attribute("download") == "label.png").Attribute("href")!.Split(',')[1]);
        var decoded = SymbolScanner.Scan(png, new ScanOptions { Formats = new[] { SymbolFormat.DotCode }, TimeoutMilliseconds = 5000 });
        Assert.Equal("DIMENSIONS", Assert.Single(decoded.Symbols).Text);
        await renderer.Change("oninput", e => e.Attribute("id") == "print-dot-width", "20");
        Assert.DoesNotContain(renderer.Elements(), e => e.Attribute("download")?.StartsWith("label.", StringComparison.Ordinal) == true);
        await renderer.Click("Prepare print label");
        Assert.Contains(renderer.Elements(), e => e.Attribute("role") == "alert" && e.Text.Contains("sum to an odd number", StringComparison.Ordinal));
        Assert.DoesNotContain(renderer.Elements(), e => e.Attribute("download")?.StartsWith("label.", StringComparison.Ordinal) == true);
        Assert.Empty(renderer.Errors);
    }

    [Fact]
    public async Task PrintStudio_UsesCurrentInputAndClearsOnlyItsExportsWhenEdited() {
        await using var renderer = new PlaygroundRenderer();
        await renderer.Start();
        await renderer.Change("onchange", e => e.Attribute("id") == "print-format", "CodeOne");
        await renderer.Change("onchange", e => e.Attribute("id") == "print-version", "S10");
        await renderer.Change("oninput", e => e.Attribute("id") == "print-content", "123456");
        await renderer.Click("Prepare print label");
        var png = Convert.FromBase64String(renderer.Elements().Single(e => e.Attribute("download") == "label.png").Attribute("href")!.Split(',')[1]);
        var decoded = SymbolScanner.Scan(png, new ScanOptions { Formats = new[] { SymbolFormat.CodeOne }, TimeoutMilliseconds = 5000 });
        Assert.Equal("123456", Assert.Single(decoded.Symbols).Text);
        var pdf = Convert.FromBase64String(renderer.Elements().Single(e => e.Attribute("download") == "label.pdf").Attribute("href")!.Split(',')[1]);
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(pdf));
        Assert.Contains(renderer.Elements(), e => e.Attribute("download") == "label.svg");
        await renderer.Change("oninput", e => e.Attribute("id") == "print-content", "invalid numeric input");
        Assert.DoesNotContain(renderer.Elements(), e => e.Attribute("download")?.StartsWith("label.", StringComparison.Ordinal) == true);
        Assert.Contains(renderer.Elements(), e => e.Attribute("download") == "qrcode.png");
        await renderer.Click("Prepare print label");
        Assert.Contains(renderer.Elements(), e => e.Attribute("role") == "alert" && e.Text.Contains("integer exceeds", StringComparison.Ordinal));
        Assert.DoesNotContain(renderer.Elements(), e => e.Attribute("download")?.StartsWith("label.", StringComparison.Ordinal) == true);
        Assert.Empty(renderer.Errors);
    }
}
#endif
