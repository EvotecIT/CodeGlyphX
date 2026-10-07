using CodeGlyphX;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrDecodeInfoTests {
    [Fact]
    public void UninitializedDiagnosticsDoNotReportSuccessfulRecognition() {
        var module = default(QrDecodeInfo);
        var pixel = default(QrPixelDecodeInfo);

        Assert.False(module.IsSuccess);
        Assert.Equal(QrDecodeFailureReason.Uninitialized, module.Failure);
        Assert.False(string.IsNullOrEmpty(module.Message));
        Assert.False(pixel.IsSuccess);
        Assert.Equal(0, pixel.Confidence);
    }

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
