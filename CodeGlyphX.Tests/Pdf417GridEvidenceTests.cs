using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class Pdf417GridEvidenceTests {
    [Theory]
    [InlineData(false, 1, false)]
    [InlineData(false, 4, true)]
    [InlineData(true, 1, true)]
    [InlineData(true, 4, false)]
    public void OffsetCandidates_RetainPayloadAndMacroInBothRowOrders(bool compact, int columns, bool bottomUp) {
        const string payload = "PREFIX-0417";
        var encoded = Pdf417Code.EncodeMacro(payload, new Pdf417MacroOptions {
            FileId = "123", SegmentIndex = 0, IsLastSegment = true
        }, new Pdf417EncodeOptions {
            Compact = compact, ErrorCorrectionLevel = 2, MinColumns = columns, MaxColumns = columns
        });
        var source = encoded.Modules;
        var padded = new BitMatrix(source.Width + 28, source.Height);
        for (var y = 0; y < source.Height; y++) for (var x = 0; x < source.Width; x++)
            padded[x + 11, y] = source[x, bottomUp ? source.Height - 1 - y : y];

        Assert.True(Pdf417Decoder.TryDecode(padded, out string text));
        Assert.Equal(payload, text);
        Assert.True(Pdf417Decoder.TryDecode(padded, out text, out var diagnostics));
        Assert.Equal(payload, text);
        Assert.True(diagnostics.Success);
        Assert.True(Pdf417Decoder.TryDecode(padded, out Pdf417Decoded decoded));
        Assert.Equal(payload, decoded.Text);
        Assert.NotNull(decoded.Macro);
        Assert.Equal("123", decoded.Macro!.FileId);
        Assert.Equal(0, decoded.Macro.SegmentIndex);
        Assert.True(decoded.Macro.IsLastSegment);
    }

}
