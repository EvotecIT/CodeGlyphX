using System;
using System.IO;
using System.Threading;
using CodeGlyphX.Internal;
using CodeGlyphX.MaxiCode;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class MaxiCodeImageTests {
    private const string Mixed = "ABCDE12abcde1ÀÁÂÃ¢£¤¥1àáâãabcde123A123456789àáâã¢£¤¥abcdÚÛÜÝ123456789¾";
    private const string Controls = "\rABCDEFGHIJKLMNOPQRSTUVWXYZ\u001c\u001d\u001e \"#$%&'()*+,-./01234:56789";

    [Theory]
    [InlineData("1.png", Controls)]
    [InlineData("with-errors.png", Controls)]
    [InlineData("mode4-mixed-sets.png", Mixed)]
    [InlineData("MODE3.png", "[)>\u001e01\u001d96MODE3")]
    [InlineData("MODE5.png", "[)>\u001e01\u001d96MODE5")]
    [InlineData("MODE6.png", "[)>\u001e01\u001d96MODE6")]
    public void IndependentImages_PreserveSecondaryTextBytesAndCarrierFields(string name, string expected) {
        var scan = SymbolScanner.Scan(ReadFixture(name), Options());
        var symbol = Assert.Single(scan.Symbols);
        Assert.Equal(SymbolFormat.MaxiCode, symbol.Format);
        Assert.Equal(expected, symbol.Text);
        Assert.True(symbol.HasRawBytes);
        Assert.Equal(System.Text.Encoding.GetEncoding(28591).GetBytes(expected), symbol.RawBytes.ToArray());
        Assert.False(symbol.IsInverted);
        Assert.Null(symbol.Geometry);
        Assert.NotNull(symbol.SearchRegion);
        var metadata = Assert.IsType<MaxiCodeSymbolMetadata>(symbol.Metadata);
        if (name.StartsWith("MODE", StringComparison.Ordinal)) {
            Assert.Equal("123450000", metadata.PostalCode);
            Assert.Equal(222, metadata.CountryCode);
            Assert.Equal(111, metadata.ServiceClass);
            Assert.Equal("]U1", symbol.SymbologyIdentifier);
        } else {
            Assert.Null(metadata.PostalCode);
            Assert.Equal("]U0", symbol.SymbologyIdentifier);
        }
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(90, true)]
    [InlineData(180, false)]
    [InlineData(270, false)]
    [InlineData(17, false)]
    [InlineData(333, true)]
    public void RotatedSymbolOnLargerLabel_PreservesPayloadAndPolarity(int angle, bool inverted) {
        var source = ImageReader.DecodeRgba32(ReadFixture("MODE3.png"), out var sw, out var sh);
        const int width = 520, height = 460;
        var pixels = new byte[width * height * 4];
        var sine = Math.Sin(angle * Math.PI / 180);
        var cosine = Math.Cos(angle * Math.PI / 180);
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            var dx = x - 280; var dy = y - 220;
            var sx = (int)Math.Floor(dx * cosine + dy * sine + sw / 2.0);
            var sy = (int)Math.Floor(-dx * sine + dy * cosine + sh / 2.0);
            var at = (y * width + x) * 4;
            var value = (uint)sx < (uint)sw && (uint)sy < (uint)sh ? source[(sy * sw + sx) * 4] : (byte)255;
            // Separate text-like bars make the full foreground bounds unrelated to the symbol bounds.
            if (x > 15 && x < 70 && y > 30 && y < 400 && y % 17 < 4) value = 0;
            if (inverted) value = (byte)(255 - value);
            pixels[at] = pixels[at + 1] = pixels[at + 2] = value;
            pixels[at + 3] = 255;
        }
        var before = (byte[])pixels.Clone();
        var scan = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), Options());
        var symbol = Assert.Single(scan.Symbols);
        Assert.Equal("[)>\u001e01\u001d96MODE3", symbol.Text);
        Assert.Equal(inverted, symbol.IsInverted);
        Assert.Equal(before, pixels);
    }

    [Fact]
    public void CroppedBottomUpBgraFrame_PreservesSourceRegion() {
        var source = ImageReader.DecodeRgba32(ReadFixture("MODE3.png"), out var sw, out var sh);
        var width = sw + 40; var height = sh + 50; var stride = width * 4 + 12;
        var pixels = new byte[stride * height];
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            var sx = x - 20; var sy = y - 25;
            var at = (height - 1 - y) * stride + x * 4;
            for (var c = 0; c < 4; c++) pixels[at + c] = (uint)sx < (uint)sw && (uint)sy < (uint)sh
                ? source[(sy * sw + sx) * 4 + (c < 3 ? 2 - c : c)] : (byte)255;
        }
        var options = Options();
        options.Region = new ImageRegion(20, 25, sw, sh);
        var scan = SymbolScanner.Scan(new ImageFrame(pixels, width, height, stride, PixelFormat.Bgra32, ImageRowOrder.BottomUp), options);
        var symbol = Assert.Single(scan.Symbols);
        Assert.Equal("[)>\u001e01\u001d96MODE3", symbol.Text);
        Assert.Equal(options.Region, symbol.SearchRegion);
    }

    [Fact]
    public void FinderWithoutValidData_IsRejected() {
        const int width = 320, height = 320;
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            var radius = Math.Sqrt((x - 160) * (x - 160) + (y - 160) * (y - 160));
            var dark = radius >= 6 && radius < 45 && ((int)((radius - 6) / 7.8) & 1) == 0;
            var at = (y * width + x) * 4;
            pixels[at] = pixels[at + 1] = pixels[at + 2] = dark ? (byte)0 : (byte)255;
            pixels[at + 3] = 255;
        }
        var scan = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), Options());
        Assert.Equal(ScanStatus.NoSymbolFound, scan.Status);
        Assert.Empty(scan.Symbols);
        Assert.Empty(scan.UnsupportedFormats);
        Assert.True(SymbolCapabilities.Get(SymbolFormat.MaxiCode).IsDefaultScanFormat);
        Assert.False(SymbolCapabilities.Get(SymbolFormat.MaxiCode).CanScanMultiple);
    }

    [Fact]
    public void DefaultFormatSelection_FindsCarrierSymbol() {
        var options = ScanOptions.Robust(5000);
        options.MaxSymbols = 1;
        options.Image = new ImageDecodeOptions { MaxDimension = 0 };
        var scan = SymbolScanner.Scan(ReadFixture("MODE3.png"), options);
        var symbol = Assert.Single(scan.Symbols);
        Assert.Equal(SymbolFormat.MaxiCode, symbol.Format);
        Assert.Equal("[)>\u001e01\u001d96MODE3", symbol.Text);
    }

    [Fact]
    public void ImageRecognition_ObservesCallerCancellationAndAmbientClock() {
        var pixels = ImageReader.DecodeRgba32(ReadFixture("1.png"), out var width, out var height);
        using (var cancelled = new CancellationTokenSource()) {
            cancelled.Cancel();
            Assert.False(MaxiCodeImageDecoder.TryDecode(pixels, width, height, cancelled.Token, out _, out _));
        }
        using var budget = DecodeBudget.Begin(1);
        Assert.True(SpinWait.SpinUntil(() => DecodeBudget.IsExpired, 1000));
        Assert.False(MaxiCodeImageDecoder.TryDecode(pixels, width, height, CancellationToken.None, out _, out _));
    }

    private static ScanOptions Options() => new() {
        Formats = new[] { SymbolFormat.MaxiCode }, TimeoutMilliseconds = 5000, MaxSymbols = 1,
        EnableTileScan = false, Image = new ImageDecodeOptions { MaxDimension = 0 }
    };
    private static byte[] ReadFixture(string name) {
        using var stream = typeof(MaxiCodeImageTests).Assembly.GetManifestResourceStream("CodeGlyphX.Tests.Fixtures.MaxiCode." + name);
        Assert.NotNull(stream);
        using var buffer = new MemoryStream();
        stream!.CopyTo(buffer);
        return buffer.ToArray();
    }
}
