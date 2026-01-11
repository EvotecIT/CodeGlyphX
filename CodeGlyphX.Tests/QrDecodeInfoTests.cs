using CodeGlyphX;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrDecodeInfoTests {
    [Fact]
    public void QrDecodeInfo_InvalidInput_ReturnsDiagnostics() {
        BitMatrix? modules = null;
        Assert.False(QrDecoder.TryDecode(modules!, out var _, out var info));
        Assert.Equal(QrDecodeFailureReason.InvalidInput, info.Failure);
        Assert.False(info.IsSuccess);
    }

    [Fact]
    public void QrDecodeInfo_InvalidSize_ReturnsDiagnostics() {
        var modules = new BitMatrix(20, 20);
        Assert.False(QrDecoder.TryDecode(modules, out var _, out var info));
        Assert.Equal(QrDecodeFailureReason.InvalidSize, info.Failure);
        Assert.False(info.IsSuccess);
    }
}
