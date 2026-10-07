using System;
using System.IO;
using System.Threading.Tasks;
using CodeGlyphX;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class AsyncDecodeTests {
    private const string Payload = "ASYNC";

    [Fact]
    public async Task StreamAdaptersReturnDecodedSymbol() {
        var png = CreatePng(containsSymbol: true);
        var qrOptions = QrPixelDecodeOptions.Fast();
        var options = new CodeGlyphDecodeOptions { Qr = qrOptions, IncludeBarcode = false };

        await AssertStreamOutcome(png, Payload, stream => CodeGlyph.TryDecodePngAsync(stream, qrOptions: qrOptions));
        await AssertStreamOutcome(png, Payload, stream => CodeGlyph.TryDecodeAllPngAsync(stream, includeBarcode: false, qrOptions: qrOptions));
        await AssertStreamOutcome(png, Payload, stream => CodeGlyph.TryDecodeImageAsync(stream, qrOptions: qrOptions));
        await AssertStreamOutcome(png, Payload, stream => CodeGlyph.TryDecodeAllImageAsync(stream, includeBarcode: false, qrOptions: qrOptions));
        await AssertStreamOutcome(png, Payload, stream => CodeGlyph.TryDecodeImageAsync(stream, options));
        await AssertStreamOutcome(png, Payload, stream => CodeGlyph.TryDecodeAllImageAsync(stream, options));

        using var pngStream = new MemoryStream(png);
        using var imageStream = new MemoryStream(png);
        AssertOutcome(await CodeGlyph.DecodePngAsync(pngStream, qrOptions: qrOptions), Payload);
        AssertOutcome(await CodeGlyph.DecodeImageAsync(imageStream, qrOptions: qrOptions), Payload);
        Assert.Equal(pngStream.Length, pngStream.Position);
        Assert.Equal(imageStream.Length, imageStream.Position);
    }

    [Fact]
    public async Task FileAdaptersReturnDecodedSymbol() {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
        var qrOptions = QrPixelDecodeOptions.Fast();
        try {
            await File.WriteAllBytesAsync(path, CreatePng(containsSymbol: true));
            AssertOutcome(await CodeGlyph.TryDecodePngFileAsync(path, qrOptions: qrOptions), Payload);
            AssertOutcome(await CodeGlyph.TryDecodeAllPngFileAsync(path, includeBarcode: false, qrOptions: qrOptions), Payload);
            AssertOutcome(await CodeGlyph.TryDecodeImageFileAsync(path, qrOptions: qrOptions), Payload);
            AssertOutcome(await CodeGlyph.TryDecodeAllImageFileAsync(path, includeBarcode: false, qrOptions: qrOptions), Payload);
        } finally {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task OptionsAdaptersReturnNoSymbolForValidBlankImage() {
        var png = CreatePng(containsSymbol: false);
        var options = new CodeGlyphDecodeOptions {
            Qr = QrPixelDecodeOptions.Fast(),
            IncludeBarcode = false,
            Image = new ImageDecodeOptions { RecognitionBudgetMilliseconds = TestBudget.Adjust(250) }
        };

        await AssertStreamOutcome(png, null, stream => CodeGlyph.TryDecodeImageAsync(stream, options));
        await AssertStreamOutcome(png, null, stream => CodeGlyph.TryDecodeAllImageAsync(stream, options));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ThrowingAdaptersRejectValidImageWithoutSymbol(bool pngOnly) {
        using var stream = new MemoryStream(CreatePng(containsSymbol: false));
        using var budget = CodeGlyphBudget.Begin(TestBudget.Adjust(250));
        var qrOptions = QrPixelDecodeOptions.Fast();

        await Assert.ThrowsAsync<FormatException>(() => pngOnly
            ? CodeGlyph.DecodePngAsync(stream, expectedBarcode: BarcodeType.Code128, qrOptions: qrOptions)
            : CodeGlyph.DecodeImageAsync(stream, expectedBarcode: BarcodeType.Code128, qrOptions: qrOptions));
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public async Task OptionsAdaptersRejectPayloadAbovePerCallByteLimit() {
        var png = CreatePng(containsSymbol: true);
        var options = new CodeGlyphDecodeOptions {
            Image = new ImageDecodeOptions { MaxBytes = png.Length - 1 }
        };
        using var singleStream = new MemoryStream(png);
        using var allStream = new MemoryStream(png);

        Assert.Null(await CodeGlyph.TryDecodeImageAsync(singleStream, options));
        Assert.Empty(await CodeGlyph.TryDecodeAllImageAsync(allStream, options));
        Assert.Equal(0, singleStream.Position);
        Assert.Equal(0, allStream.Position);
        Assert.True(singleStream.CanRead);
        Assert.True(allStream.CanRead);
    }

    private static byte[] CreatePng(bool containsSymbol) {
        if (!containsSymbol) {
            return MatrixPngRenderer.Render(new BitMatrix(1, 1), new MatrixPngRenderOptions { ModuleSize = 1, QuietZone = 0 });
        }
        var qr = QrCodeEncoder.EncodeText(Payload, QrErrorCorrectionLevel.M);
        return QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions { ModuleSize = 4, QuietZone = 4 });
    }

    private static async Task AssertStreamOutcome(byte[] png, string? expected, Func<Stream, Task<CodeGlyphDecoded?>> decode) {
        using var stream = new MemoryStream(png);
        AssertOutcome(await decode(stream), expected);
        Assert.Equal(stream.Length, stream.Position);
    }

    private static async Task AssertStreamOutcome(byte[] png, string? expected, Func<Stream, Task<CodeGlyphDecoded[]>> decode) {
        using var stream = new MemoryStream(png);
        AssertOutcome(await decode(stream), expected);
        Assert.Equal(stream.Length, stream.Position);
    }

    private static void AssertOutcome(CodeGlyphDecoded? decoded, string? expected) {
        if (expected is null) {
            Assert.Null(decoded);
            return;
        }
        Assert.NotNull(decoded);
        Assert.Equal(CodeGlyphKind.Qr, decoded!.Kind);
        Assert.Equal(expected, decoded.Text);
    }

    private static void AssertOutcome(CodeGlyphDecoded[] decoded, string? expected) {
        if (expected is null) {
            Assert.Empty(decoded);
            return;
        }
        AssertOutcome(Assert.Single(decoded), expected);
    }
}
