using System;
using System.Threading;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class CodeGlyphBufferContractTests {
    [Fact]
    public void NullArrays_AreRejectedByEveryRawPixelEntryPoint() {
        Assert.Throws<ArgumentNullException>(() => CodeGlyph.TryDecode((byte[])null!, 1, 1, 4, PixelFormat.Rgba32, out _));
        Assert.Throws<ArgumentNullException>(() => CodeGlyph.TryDecode((byte[])null!, 1, 1, 4, PixelFormat.Rgba32, out _, out _));
        Assert.Throws<ArgumentNullException>(() => CodeGlyph.TryDecodeAll((byte[])null!, 1, 1, 4, PixelFormat.Rgba32, out _));
    }

    [Fact]
    public void Cancellation_PreservesArrayAndSpanResultsAndDiagnostics() {
        var pixels = new byte[4];
        var token = new CancellationToken(true);
        Assert.False(CodeGlyph.TryDecode(pixels, 1, 1, 4, PixelFormat.Rgba32, out var array, cancellationToken: token));
        Assert.Null(array);
        Assert.False(CodeGlyph.TryDecode(pixels.AsSpan(), 1, 1, 4, PixelFormat.Rgba32, out var span, cancellationToken: token));
        Assert.Null(span);
        Assert.False(CodeGlyph.TryDecode(pixels, 1, 1, 4, PixelFormat.Rgba32, out _, out var arrayDiag, cancellationToken: token));
        Assert.False(CodeGlyph.TryDecode(pixels.AsSpan(), 1, 1, 4, PixelFormat.Rgba32, out _, out var spanDiag, cancellationToken: token));
        Assert.Equal(DecodeFailureReason.Cancelled, arrayDiag.FailureReason);
        Assert.Equal(arrayDiag.FailureReason, spanDiag.FailureReason);
        Assert.False(CodeGlyph.TryDecodeAll(pixels, 1, 1, 4, PixelFormat.Rgba32, out var arrays, cancellationToken: token));
        Assert.False(CodeGlyph.TryDecodeAll(pixels.AsSpan(), 1, 1, 4, PixelFormat.Rgba32, out var spans, cancellationToken: token));
        Assert.Empty(arrays);
        Assert.Empty(spans);
    }
}
