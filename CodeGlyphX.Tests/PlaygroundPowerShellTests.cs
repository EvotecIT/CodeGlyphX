#if NET10_0
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed partial class PlaygroundLivePreviewTests {
    [Fact]
    public async Task PowerShellQrExamplePreservesCorrectionAndOutputSize() {
        await using var renderer = new PlaygroundRenderer();
        await renderer.Start();
        await renderer.Click("PowerShell");
        await renderer.Change("onchange", e => e.Tag == "select" && e.Attribute("value") == "M", "H");
        await renderer.Change("onchange", e => e.Tag == "select" && e.Attribute("value") == "Auto", "1200");
        await renderer.Change("onchange", e => e.Attribute("id") == "qr-design", "Styled");
        await renderer.Change("onchange", e => e.Tag == "select" && e.Attribute("value") == "Square", "Circle");
        var script = renderer.Elements().Single(e => e.Tag == "pre" && e.Attribute("class")!.Contains("language-powershell", StringComparison.Ordinal)).Text;
        Assert.Contains("ErrorCorrection", script);
        Assert.Contains("'H'", script);
        Assert.Contains("Size", script);
        Assert.Contains("1200", script);
        Assert.Contains("ModuleShape", script);
        Assert.Contains("'Circle'", script);
        Assert.DoesNotContain("Not yet available", script);
        Assert.Empty(renderer.Errors);
    }

    [Theory]
    [InlineData("Barcode", "Code128", "GS1128", "Gs1Code128")]
    [InlineData("Matrix", "DataMatrix", "Aztec", "Aztec")]
    public async Task PowerShellUsesSupportedSymbolNames(string category, string initialType, string selectedType, string expectedType) {
        await using var renderer = new PlaygroundRenderer();
        await renderer.Start();
        await renderer.Change("onchange", e => e.Tag == "select" && e.Attribute("value") == "QR", category);
        await renderer.Change("onchange", e => e.Tag == "select" && e.Attribute("value") == initialType, selectedType);
        await renderer.Click("PowerShell");
        var script = renderer.Elements().Single(e => e.Tag == "pre" && e.Attribute("class")!.Contains("language-powershell", StringComparison.Ordinal)).Text;
        Assert.Contains("New-ImageBarCode", script);
        Assert.Contains("'" + expectedType + "'", script);
        Assert.DoesNotContain("Not yet available", script);
        Assert.Empty(renderer.Errors);
    }

    [Fact]
    public async Task PowerShellDecodeKeepsQrAndTheConfiguredDeadline() {
        await using var renderer = new PlaygroundRenderer();
        await renderer.Start();
        await renderer.Change("onchange", e => e.Tag == "select" && e.Attribute("value") == "Generate", "Decode");
        await renderer.Click("PowerShell");
        var script = renderer.Elements().Single(e => e.Tag == "pre" && e.Attribute("class")!.Contains("language-powershell", StringComparison.Ordinal)).Text;
        Assert.Contains("'QrCode'", script);
        Assert.Contains("'Code128'", script);
        Assert.Contains("'DataMatrix'", script);
        Assert.Contains("TimeoutMilliseconds = 2000", script);
        Assert.Contains("[CodeGlyphX.SymbolScanner]::ScanFile('image.png', $options)", script);
        Assert.Contains("$scan.Symbols | Select-Object Format, Text", script);
        Assert.Empty(renderer.Errors);
    }

    [Fact]
    public async Task PowerShellAztecExplainsManualSettingsThatTheCommandCannotRepresent() {
        await using var renderer = new PlaygroundRenderer();
        await renderer.Start();
        await renderer.Change("onchange", e => e.Tag == "select" && e.Attribute("value") == "QR", "Matrix");
        await renderer.Change("onchange", e => e.Tag == "select" && e.Attribute("value") == "DataMatrix", "Aztec");
        await renderer.Change("oninput", e => e.Tag == "input" && e.Attribute("max") == "32", "4");
        await renderer.Click("PowerShell");
        var script = renderer.Elements().Single(e => e.Tag == "pre" && e.Attribute("class")!.Contains("language-powershell", StringComparison.Ordinal)).Text;
        Assert.Contains("Aztec error correction and explicit layers", script);
        Assert.Contains("New-ImageBarCode", script);
        Assert.Empty(renderer.Errors);
    }
}
#endif
