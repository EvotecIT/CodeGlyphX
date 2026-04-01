using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrImageDecodeParitySharedTests {
    [Theory]
    [Trait("Category", "QrParity")]
    [MemberData(nameof(QrImageDecodeParityData.CleanGeneratedCases), MemberType = typeof(QrImageDecodeParityData))]
    public void QrImageDecoder_Decodes_CleanGeneratedPngSamples(QrImageDecodeParityCase testCase) {
        var png = QrImageDecodeParityData.RenderPng(testCase);

        var ok = QrImageDecoder.TryDecodeImage(
            png,
            imageOptions: null,
            options: new QrPixelDecodeOptions {
                MaxDimension = 4096
            },
            out var decoded);

        Assert.True(ok);
        Assert.Equal(testCase.Payload.Text, decoded.Text);
    }

#if NET8_0_OR_GREATER
    [Theory]
    [Trait("Category", "QrParity")]
    [MemberData(nameof(QrImageDecodeParityData.CleanGeneratedCases), MemberType = typeof(QrImageDecodeParityData))]
    public void ForcedFallback_Matches_PrimaryDecode_On_CleanGeneratedPngSamples(QrImageDecodeParityCase testCase) {
        var png = QrImageDecodeParityData.RenderPng(testCase);

        var primaryOk = QrImageDecoder.TryDecodeImage(
            png,
            imageOptions: null,
            options: new QrPixelDecodeOptions {
                MaxDimension = 4096
            },
            out var primary);

        QrDecoded fallback = null!;
        var fallbackOk = false;
        WithForcedFallback(() => {
            fallbackOk = QrImageDecoder.TryDecodeImage(
                png,
                imageOptions: null,
                options: new QrPixelDecodeOptions {
                    MaxDimension = 4096
                },
                out fallback);
        });

        Assert.True(primaryOk);
        Assert.True(fallbackOk);
        Assert.Equal(primary.Text, fallback.Text);
    }

    private static void WithForcedFallback(System.Action action) {
        var previous = CodeGlyphXFeatures.ForceQrFallbackForTests;
        CodeGlyphXFeatures.ForceQrFallbackForTests = true;
        try {
            action();
        } finally {
            CodeGlyphXFeatures.ForceQrFallbackForTests = previous;
        }
    }
#endif
}
