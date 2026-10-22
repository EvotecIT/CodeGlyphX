using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodeGlyphX.Printing;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class RawLinearTests {
    [Theory]
    [InlineData("1")]
    [InlineData("9")]
    [InlineData("1212")]
    [InlineData("9123456789")]
    public void ExactRawRuns_PreserveTheirWidthsAndDeclaredTrailingSpace(string value) {
        var encoded = RawLinear.Encode(value);
        Assert.Equal(value, RawLinear.GetWidths(encoded));
        Assert.Equal(value.Sum(character => character - '0'), encoded.TotalModules);
        Assert.Null(encoded.Format);
        Assert.Equal((value.Length & 1) != 0, encoded.Segments[encoded.Segments.Count - 1].IsBar);
        // These samples are the raw definition itself, independent of the encoder's segment storage.
        var modules = value.SelectMany((character, index) => Enumerable.Repeat((index & 1) == 0, character - '0')).ToArray();
        Assert.True(RawLinear.TryDecode(modules, out var decoded));
        Assert.Equal(value, RawLinear.GetWidths(decoded));
    }

    public static IEnumerable<object[]> CalibratedFrames() {
        var formats = new[] { PixelFormat.Gray8, PixelFormat.Gray16LittleEndian, PixelFormat.Rgb565LittleEndian,
            PixelFormat.Rgb24, PixelFormat.Bgr24, PixelFormat.Rgba32, PixelFormat.Bgra32, PixelFormat.Argb32, PixelFormat.Abgr32 };
        for (var i = 0; i < formats.Length; i++) {
            yield return new object[] { formats[i], ImageRowOrder.TopDown, i % 4 * 90 };
            yield return new object[] { formats[i], ImageRowOrder.BottomUp, (i + 1) % 4 * 90 };
        }
    }

    [Theory]
    [MemberData(nameof(CalibratedFrames))]
    public void CalibratedImages_PreserveAllRunsAcrossPixelFormatsStridesRowsAndRotations(PixelFormat format, ImageRowOrder order, int rotation) {
        const string widths = "9123456789";
        var pixels = Fixture(widths, format, order, rotation, out var frame, out var bounds);
        var original = (byte[])pixels.Clone();
        Assert.True(RawLinear.TryDecode(frame, bounds, widths.Sum(c => c - '0'), out var decoded, rotation));
        Assert.Equal(widths, RawLinear.GetWidths(decoded));
        Assert.Equal(original, pixels);
    }

    [Fact]
    public void RawPrintLayout_ReservesTrailingWhiteWithoutInventingQuietZonesOrText() {
        var layout = BarcodePrint.LayoutRaw(RawLinear.Encode("1212"), new BarcodePrintOptions {
            Dpi = 254, XDimensionMm = .3, BarHeightMm = 2, IncludeQuietZones = false
        });
        Assert.Equal(1.8, layout.SymbolBounds.Width, 7);
        Assert.Equal(1.2, layout.InkBounds.Right, 7);
        Assert.Empty(layout.Text);
        Assert.False(layout.HasErrors);
        Assert.Null(layout.Format);
        var rgba = layout.RenderPixels(out var width, out var height);
        Assert.True(RawLinear.TryDecode(ImageFrame.Packed(rgba, width, height, PixelFormat.Rgba32),
            new ImageRegion(0, 0, width, height), 6, out var decoded));
        Assert.Equal("1212", RawLinear.GetWidths(decoded));
        var nominal = BarcodePrint.LayoutRaw(RawLinear.Encode("1212"));
        Assert.Equal(nominal.SymbolBounds.Width * .15, nominal.SymbolBounds.Height, 7);
    }

    [Fact]
    public void RawLimitsAndCalibration_RejectInvalidRunsClippingAndCancellation() {
        var maximum = new string('9', RawLinear.MaximumLength);
        Assert.Equal(maximum, RawLinear.GetWidths(RawLinear.Encode(maximum)));
        var maximumModules = maximum.SelectMany((character, index) => Enumerable.Repeat((index & 1) == 0, character - '0')).ToArray();
        Assert.True(RawLinear.TryDecode(maximumModules, out var maximumDecoded));
        Assert.Equal(maximum, RawLinear.GetWidths(maximumDecoded));
        Assert.Throws<ArgumentException>(() => RawLinear.Encode(maximum + "1"));
        Assert.Throws<ArgumentException>(() => RawLinear.Encode(""));
        Assert.Throws<ArgumentException>(() => RawLinear.Encode("102"));
        Assert.Throws<ArgumentException>(() => RawLinear.Encode("１２"));
        Assert.False(RawLinear.TryDecode(new bool[4], out _));
        Assert.False(RawLinear.TryDecode(Enumerable.Repeat(true, 10).ToArray(), out _));
        Fixture("1212", PixelFormat.Gray8, ImageRowOrder.TopDown, 0, out var frame, out var bounds);
        Assert.False(RawLinear.TryDecode(frame, bounds, 6, out _, cancellationToken: new CancellationToken(true)));
        Assert.Throws<ArgumentOutOfRangeException>(() => RawLinear.TryDecode(frame, new ImageRegion(0, 0, frame.Width + 1, frame.Height), 6, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => RawLinear.TryDecode(frame, bounds, 0, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => RawLinear.TryDecode(frame, bounds, 6, out _, rotationDegrees: 45));
    }

    [Fact]
    public void CalibratedInvertedRawBars_RequireTheDeclaredPolarity() {
        Fixture("1212", PixelFormat.Gray8, ImageRowOrder.TopDown, 270, out var frame, out var bounds, inverted: true);
        Assert.False(RawLinear.TryDecode(frame, bounds, 6, out _, rotationDegrees: 270));
        Assert.True(RawLinear.TryDecode(frame, bounds, 6, out var decoded, rotationDegrees: 270, inverted: true));
        Assert.Equal("1212", RawLinear.GetWidths(decoded));
    }

    private static byte[] Fixture(string widths, PixelFormat format, ImageRowOrder order, int rotation, out ImageFrame frame, out ImageRegion bounds, bool inverted = false) {
        var bits = widths.SelectMany((character, index) => Enumerable.Repeat((index & 1) == 0, character - '0')).ToArray();
        var canonicalWidth = bits.Length * 3; const int canonicalHeight = 18;
        var rotated = rotation % 180 != 0;
        var inkWidth = rotated ? canonicalHeight : canonicalWidth; var inkHeight = rotated ? canonicalWidth : canonicalHeight;
        var width = inkWidth + 17; var height = inkHeight + 19;
        var bytes = ImageFrame.GetBytesPerPixel(format); var stride = width * bytes + 7;
        var pixels = Enumerable.Repeat((byte)255, 5 + stride * height).ToArray();
        for (var y = 0; y < canonicalHeight; y++) for (var x = 0; x < canonicalWidth; x++) {
            var px = rotation switch { 90 => canonicalHeight - 1 - y, 180 => canonicalWidth - 1 - x, 270 => y, _ => x };
            var py = rotation switch { 90 => x, 180 => canonicalHeight - 1 - y, 270 => canonicalWidth - 1 - x, _ => y };
            px += 9; py += 11;
            var sourceY = order == ImageRowOrder.TopDown ? py : height - 1 - py;
            var at = 5 + sourceY * stride + px * bytes;
            var value = bits[x / 3] ^ inverted ? (byte)0 : (byte)255;
            for (var channel = 0; channel < bytes; channel++) pixels[at + channel] = value;
            if (bytes == 4) pixels[at + (format is PixelFormat.Argb32 or PixelFormat.Abgr32 ? 0 : 3)] = 255;
        }
        frame = new ImageFrame(new ReadOnlyMemory<byte>(pixels, 5, pixels.Length - 5), width, height, stride, format, order);
        bounds = new ImageRegion(9, 11, inkWidth, inkHeight);
        return pixels;
    }
}
