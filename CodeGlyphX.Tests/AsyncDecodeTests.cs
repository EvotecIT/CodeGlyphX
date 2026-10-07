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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task StreamAdaptersPreserveRecognitionOutcome(bool containsSymbol) {
        var png = CreatePng(containsSymbol);
        var expected = containsSymbol ? Payload : null;
        var options = new CodeGlyphDecodeOptions { IncludeBarcode = false };

        await AssertStreamOutcome(png, expected, stream => CodeGlyph.TryDecodePngAsync(stream));
        await AssertStreamOutcome(png, expected, stream => CodeGlyph.TryDecodeAllPngAsync(stream, includeBarcode: false));
        await AssertStreamOutcome(png, expected, stream => CodeGlyph.TryDecodeImageAsync(stream));
        await AssertStreamOutcome(png, expected, stream => CodeGlyph.TryDecodeAllImageAsync(stream, includeBarcode: false));
        await AssertStreamOutcome(png, expected, stream => CodeGlyph.TryDecodeImageAsync(stream, options));
        await AssertStreamOutcome(png, expected, stream => CodeGlyph.TryDecodeAllImageAsync(stream, options));

        using var pngStream = new MemoryStream(png);
        using var imageStream = new MemoryStream(png);
        if (containsSymbol) {
            AssertOutcome(await CodeGlyph.DecodePngAsync(pngStream), expected);
            AssertOutcome(await CodeGlyph.DecodeImageAsync(imageStream), expected);
        } else {
            await Assert.ThrowsAsync<FormatException>(() => CodeGlyph.DecodePngAsync(pngStream));
            await Assert.ThrowsAsync<FormatException>(() => CodeGlyph.DecodeImageAsync(imageStream));
        }
        Assert.Equal(pngStream.Length, pngStream.Position);
        Assert.Equal(imageStream.Length, imageStream.Position);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FileAdaptersPreserveRecognitionOutcome(bool containsSymbol) {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
        var expected = containsSymbol ? Payload : null;
        try {
            await File.WriteAllBytesAsync(path, CreatePng(containsSymbol));
            AssertOutcome(await CodeGlyph.TryDecodePngFileAsync(path), expected);
            AssertOutcome(await CodeGlyph.TryDecodeAllPngFileAsync(path, includeBarcode: false), expected);
            AssertOutcome(await CodeGlyph.TryDecodeImageFileAsync(path), expected);
            AssertOutcome(await CodeGlyph.TryDecodeAllImageFileAsync(path, includeBarcode: false), expected);
        } finally {
            File.Delete(path);
        }
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
