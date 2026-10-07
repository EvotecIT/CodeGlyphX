using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class PngMalformedInputTests {
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
