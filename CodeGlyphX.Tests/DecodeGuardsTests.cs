using System;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class DecodeGuardsTests {
    [Fact]
    public void EnsurePayloadWithinLimits_Includes_Byte_Details() {
        var maxBytes = ImageReader.MaxImageBytes;
        var length = maxBytes + 1;
        var expected = GuardMessages.ForBytes("Input exceeds size limits.", length, maxBytes);
        var ex = Assert.Throws<FormatException>(() => DecodeGuards.EnsurePayloadWithinLimits(length, "Input exceeds size limits."));
        Assert.Equal(expected, ex.Message);
    }

    [Fact]
    public void EnsurePixelCount_Includes_Dimension_Details() {
        const int width = 100_000;
        const int height = 100_000;
        var expected = GuardMessages.ForPixels("Image dimensions exceed limits.", width, height, (long)width * height, ImageReader.MaxPixels);
        var ex = Assert.Throws<FormatException>(() => DecodeGuards.EnsurePixelCount(width, height, "Image dimensions exceed limits."));
        Assert.Equal(expected, ex.Message);
    }

    [Fact]
    public void EnsureByteCount_Reports_Int32_Limit() {
        var bytes = (long)int.MaxValue + (1024 * 1024);
        var ex = Assert.Throws<FormatException>(() => DecodeGuards.EnsureByteCount(bytes, "Row exceeds size limits."));
        Assert.Contains("max 2048 MB", ex.Message);
    }

    [Fact]
    public void EnsureOutputBytes_Reports_MaxBytes() {
        var maxBytes = RenderGuards.MaxOutputBytes;
        var bytes = (long)maxBytes + (1024 * 1024);
        var expected = GuardMessages.ForBytes("Output exceeds size limits.", bytes, maxBytes);
        var ex = Assert.Throws<ArgumentException>(() => RenderGuards.EnsureOutputBytes(bytes, "Output exceeds size limits."));
        Assert.Equal(expected, ex.Message);
    }

    [Fact]
    public void EnsureOutputPixels_Reports_MaxPixels() {
        const int width = 100_000;
        const int height = 100_000;
        var expected = GuardMessages.ForPixels("Output exceeds size limits.", width, height, (long)width * height, RenderGuards.MaxOutputPixels);
        var ex = Assert.Throws<ArgumentException>(() => RenderGuards.EnsureOutputPixels(width, height, "Output exceeds size limits."));
        Assert.Equal(expected, ex.Message);
    }
}
