using System;
using System.IO;
using System.Text;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class PngMalformedInputTests {
    [Theory]
    [InlineData("IHDR")]
    [InlineData("tEXt")]
    public void OversizedChunkReturnsInvalidImageInsteadOfOverflowingTheParser(string chunkType) {
        // A PNG signature, a declared Int32.MaxValue payload, a chunk type and four CRC bytes.
        var png = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 127, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0 };
        Encoding.ASCII.GetBytes(chunkType).CopyTo(png, 12);

        Assert.Throws<FormatException>(() => PngReader.DecodeRgba32(png, out _, out _));
        var container = new byte[png.Length + 5];
        png.CopyTo(container, 3);
        Assert.Throws<FormatException>(() => PngReader.DecodeRgba32(new ReadOnlyMemory<byte>(container, 3, png.Length), out _, out _));
        Assert.False(ImageReader.TryDecodeRgba32(png, out var rgba, out var width, out var height));
        Assert.Empty(rgba);
        Assert.Equal(0, width);
        Assert.Equal(0, height);

        var scan = SymbolScanner.Scan(png, new ScanOptions { Formats = new[] { SymbolFormat.QrCode } });
        Assert.Equal(ScanStatus.InvalidImage, scan.Status);
        Assert.Empty(scan.Symbols);
        using var stream = new MemoryStream(png);
        Assert.Equal(ScanStatus.InvalidImage, SymbolScanner.Scan(stream).Status);
    }

    [Fact]
    public void CorruptDeflateReturnsFalseFromTryDecodeAndFormatExceptionFromExpertDecoder() {
        const string resource = "CodeGlyphX.Tests.Fixtures.ImageSamples.pngsuite-basn6a16.png";
        using var fixture = typeof(PngMalformedInputTests).Assembly.GetManifestResourceStream(resource);
        Assert.NotNull(fixture);
        using var input = new MemoryStream();
        fixture!.CopyTo(input);
        var png = input.ToArray();
        // The deterministic fuzz corpus found this invalid compressed-data mutation.
        png[(19 * 104729) % png.Length] ^= 8;

        var error = Assert.Throws<FormatException>(() => PngReader.DecodeRgba32(png, out _, out _));
        Assert.IsType<InvalidDataException>(error.InnerException);
        Assert.False(ImageReader.TryDecodeRgba32(png, out var rgba, out var width, out var height));
        Assert.Empty(rgba);
        Assert.Equal(0, width);
        Assert.Equal(0, height);
        using var stream = new MemoryStream(png);
        Assert.False(ImageReader.TryDecodeRgba32(stream, out _, out _, out _));
    }
}
