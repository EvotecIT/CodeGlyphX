using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Jpeg;
using Xunit;
using Xunit.Abstractions;

namespace CodeGlyphX.Tests;

public sealed class JpegProgressiveReferenceTests {
    private readonly ITestOutputHelper _output;

    public JpegProgressiveReferenceTests(ITestOutputHelper output) => _output = output;

    [Theory]
    [InlineData("KulekWSluchawkach", 400, 400, 3)]
    [InlineData("PrzemyslawKlysAndKulkozaurr", 1624, 1624, 3)]
    [InlineData("refinement", 32, 24, 0)]
    [InlineData("eob-runs", 64, 16, 0)]
    [InlineData("odd-subsampled", 17, 19, 3)]
    [InlineData("restart-refinement", 32, 24, 0)]
    public void StrictDecodeMatchesLibjpegPixels(string name, int expectedWidth, int expectedHeight, int rgbTolerance) {
        var jpeg = ReadFixture(name + ".jpg");
        var reference = ImageReader.DecodeRgba32(ReadFixture(name + ".png"), out var referenceWidth, out var referenceHeight);
        var actual = ImageReader.DecodeRgba32(jpeg, out var width, out var height);

        Assert.Equal(expectedWidth, referenceWidth);
        Assert.Equal(expectedHeight, referenceHeight);
        Assert.Equal(referenceWidth, width);
        Assert.Equal(referenceHeight, height);
        Assert.Equal(reference.Length, actual.Length);

        // libjpeg-turbo integer IDCT + nearest chroma; allow at most 3/255 per RGB channel.
        // Alpha is exact. Every pixel is compared; an average cannot hide a corrupt block.
        var maxDifference = 0;
        long totalDifference = 0;
        for (var i = 0; i < actual.Length; i++) {
            var difference = Math.Abs(actual[i] - reference[i]);
            var tolerance = i % 4 == 3 ? 0 : rgbTolerance;
            Assert.True(difference <= tolerance,
                $"{name}: pixel {i / 4}, channel {i % 4}: expected {reference[i]}, actual {actual[i]}, tolerance {tolerance}.");
            maxDifference = Math.Max(maxDifference, difference);
            totalDifference += difference;
        }
        _output.WriteLine($"{name}: max RGB difference {maxDifference}; mean {totalDifference / (width * (double)height * 3):F6}.");
    }

    [Theory]
    [InlineData("refinement")]
    [InlineData("restart-refinement")]
    public void TruncatedProgressiveEntropyFailsCleanly(string name) {
        var data = ReadFixture(name + ".jpg");
        var scan = FindLastScan(data);
        // Preserve the complete last SOS header, remove its entropy, and retain a real EOI.
        var entropyStart = scan + 2 + (data[scan + 2] << 8) + data[scan + 3];
        var truncated = new byte[entropyStart + 2];
        Array.Copy(data, truncated, entropyStart);
        truncated[entropyStart] = 0xFF;
        truncated[entropyStart + 1] = 0xD9;

        Assert.Throws<FormatException>(() => ImageReader.DecodeRgba32(truncated, out _, out _));
        Assert.False(ImageReader.TryDecodeRgba32(truncated, out var pixels, out _, out _));
        Assert.Empty(pixels);
    }

    [Theory]
    [InlineData(0x20)] // A refinement scan must have Al == Ah - 1.
    [InlineData(0x1E)] // Successive approximation exceeds the supported precision.
    public void MalformedProgressiveScanFailsCleanly(byte approximation) {
        var data = ReadFixture("refinement.jpg");
        var scan = FindLastScan(data);
        var headerLength = (data[scan + 2] << 8) + data[scan + 3];
        data[scan + 1 + headerLength] = approximation;

        Assert.Throws<FormatException>(() => ImageReader.DecodeRgba32(data, out _, out _));
        Assert.False(ImageReader.TryDecodeRgba32(data, out _, out _, out _));
    }

    [Theory]
    [InlineData(0x04)]
    [InlineData(0x40)]
    [InlineData(0xFF)]
    public void LaterScanRejectsInvalidHuffmanSelectors(byte selector) {
        var data = ReadFixture("refinement.jpg");
        var scan = FindLastScan(data);
        data[scan + 6] = selector;

        Assert.Throws<FormatException>(() => ImageReader.DecodeRgba32(data, out _, out _));
        Assert.False(ImageReader.TryDecodeRgba32(data, out var pixels, out var width, out var height));
        Assert.Empty(pixels);
        Assert.Equal(0, width);
        Assert.Equal(0, height);
    }

    [Theory]
    [InlineData(1)] // Incomplete FF D9 marker.
    [InlineData(2)] // Entire EOI marker removed, with all scan entropy intact.
    public void MissingEndMarkerRequiresExplicitBestEffort(int removedBytes) {
        var complete = ReadFixture("eob-runs.jpg");
        var truncated = new byte[complete.Length - removedBytes];
        Array.Copy(complete, truncated, truncated.Length);

        Assert.Throws<FormatException>(() => ImageReader.DecodeRgba32(truncated, out _, out _));
        Assert.False(ImageReader.TryDecodeRgba32(truncated, out var failedPixels, out _, out _));
        Assert.Empty(failedPixels);

        var expected = ImageReader.DecodeRgba32(complete, out var expectedWidth, out var expectedHeight);
        var options = new ImageDecodeOptions { JpegOptions = new JpegDecodeOptions(allowTruncated: true) };
        var actual = ImageReader.DecodeRgba32(truncated, options, out var width, out var height);
        Assert.Equal(expectedWidth, width);
        Assert.Equal(expectedHeight, height);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("bytes")]
    [InlineData("pixels")]
    [InlineData("working-buffer")]
    public void ProgressiveDecodePreservesResourceLimits(string limit) {
        var data = ReadFixture("odd-subsampled.jpg");
        var options = new ImageDecodeOptions();
        if (limit == "bytes") options.MaxBytes = data.Length - 1;
        if (limit == "pixels") options.MaxPixels = 17 * 19 - 1;
        if (limit == "working-buffer") options.MaxDecodedBytes = 17 * 19 * 4;

        Assert.Throws<FormatException>(() => ImageReader.DecodeRgba32(data, options, out _, out _));
        Assert.False(ImageReader.TryDecodeRgba32(data, options, out var pixels, out _, out _));
        Assert.Empty(pixels);
    }

    private static int FindLastScan(byte[] data) {
        for (var i = data.Length - 2; i >= 0; i--) {
            if (data[i] == 0xFF && data[i + 1] == 0xDA) return i;
        }
        throw new InvalidDataException("Missing JPEG scan in fixture.");
    }

    private static byte[] ReadFixture(string name) {
        using var stream = typeof(JpegProgressiveReferenceTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Jpeg.Progressive." + name);
        Assert.NotNull(stream);
        using var buffer = new MemoryStream();
        stream!.CopyTo(buffer);
        return buffer.ToArray();
    }
}
