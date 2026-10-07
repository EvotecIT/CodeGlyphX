using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class CodeGlyphDecodeTests {
    [Theory]
    [InlineData(SymbolFormat.QrCode)]
    [InlineData(SymbolFormat.Code128)]
    [InlineData(SymbolFormat.DataMatrix)]
    [InlineData(SymbolFormat.Pdf417)]
    [InlineData(SymbolFormat.Aztec)]
    public void Scan_GeneratedImage_ReturnsRequestedPhysicalFormat(SymbolFormat format) {
        const string payload = "SCAN-12345";
        var image = format switch {
            SymbolFormat.QrCode => QR.Render(payload, OutputFormat.Png).Data.ToArray(),
            SymbolFormat.Code128 => Barcode.Render(SymbolFormat.Code128, payload, OutputFormat.Png).Data.ToArray(),
            SymbolFormat.DataMatrix => DataMatrixCode.Render(payload, OutputFormat.Png).Data.ToArray(),
            SymbolFormat.Pdf417 => Pdf417Code.Render(payload, OutputFormat.Png).Data.ToArray(),
            SymbolFormat.Aztec => AztecCode.Render(payload, OutputFormat.Png).Data.ToArray(),
            _ => throw new System.ArgumentOutOfRangeException(nameof(format))
        };
        var result = SymbolScanner.Scan(image, new ScanOptions {
            Formats = new[] { format }, MaxSymbols = 1, TimeoutMilliseconds = TestBudget.Adjust(5000)
        });
        Assert.True(result.IsSuccess, result.Failure);
        var symbol = Assert.Single(result.Symbols);
        Assert.Equal(format, symbol.Format);
        Assert.Equal(payload, symbol.Text);
        Assert.NotNull(symbol.SearchRegion);
        Assert.Equal(ScanCompletionReason.SymbolLimitReached, result.CompletionReason);
        Assert.True(result.IsPartial);
    }
}
