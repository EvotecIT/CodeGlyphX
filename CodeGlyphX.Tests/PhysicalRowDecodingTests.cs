using System;
using System.Linq;
using CodeGlyphX.DataBar;
using CodeGlyphX.Pdf417;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class PhysicalRowDecodingTests {
    [Theory]
    [InlineData("(10)1098.1234", false)]
    [InlineData("(10)1098.1234", true)]
    [InlineData("(01)90012345678908(3102)001750(11)100312", false)]
    [InlineData("(01)90012345678908(3102)001750(11)100312", true)]
    public void ExpandedImage_PreservesLightGuardsAndExactGs1Elements(string aiText, bool inverted) {
        var barcode = DataBarExpandedEncoder.EncodeExpanded(aiText);
        var modules = barcode.Segments.SelectMany(s => Enumerable.Repeat(s.IsBar, s.Modules)).ToArray();
        var width = (int)(modules.Length * 2.35) + 40;
        const int height = 80;
        var pixels = White(width, height);
        for (var y = 8; y < height - 8; y++) {
            for (var x = 20; x < width - 20; x++) {
                var module = Math.Min(modules.Length - 1, (int)((x - 20) / 2.35));
                Set(pixels, width, x, y, modules[module]);
            }
        }
        if (inverted) for (var i = 0; i < pixels.Length; i += 4) pixels[i] = pixels[i + 1] = pixels[i + 2] = (byte)(255 - pixels[i]);
        var options = ScanOptions.Robust(TestBudget.Adjust(1500));
        options.Formats = new[] { SymbolFormat.Gs1DataBarExpanded };
        options.MaxSymbols = 1;
        var result = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options);
        Assert.Equal(Gs1.ElementString(aiText), Assert.Single(result.Symbols).Text);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void Pdf417Image_ReadsUnequalPhysicalRowsBesideUnrelatedInk(bool eraseRow, bool transparent, bool damagedEdges) {
        const string text = "DISPATCH-1042-ROW-HEIGHT";
        var symbol = Pdf417Encoder.Encode(text, new Pdf417EncodeOptions {
            ErrorCorrectionLevel = 3, MinColumns = 4, MaxColumns = 4, MinRows = 18, MaxRows = 18
        });
        var other = damagedEdges ? Pdf417Encoder.Encode("ANOTHER--2098-ROW-HEIGHT", new Pdf417EncodeOptions {
            ErrorCorrectionLevel = 3, MinColumns = 4, MaxColumns = 4, MinRows = 18, MaxRows = 18
        }) : null;
        var width = symbol.Width * 3 + 80;
        const int height = 130;
        var pixels = White(width, height);
        var top = 10;
        for (var row = 0; row < symbol.Height; row++) {
            var rowHeight = row % 3 + 2;
            for (var y = top; y < top + rowHeight; y++)
                for (var x = 0; x < symbol.Width * 3; x++)
                    Set(pixels, width, x + 20, y, !(eraseRow && row == 7) && symbol[x / 3, row]);
            if (other is not null) for (var x = 34 * 3; x < 68 * 3; x++)
                Set(pixels, width, x + 20, top + rowHeight - 1, other[x / 3, row]);
            top += rowHeight;
        }
        // Unrelated document ink makes a single dark bounding box a poor grid fit.
        for (var x = 5; x < width - 5; x++) Set(pixels, width, x, 115, true);
        if (transparent) for (var at = 0; at < pixels.Length; at += 4) {
            if (pixels[at] != 255) continue;
            pixels[at] = pixels[at + 1] = pixels[at + 2] = pixels[at + 3] = 0;
        }
        var options = ScanOptions.Robust(TestBudget.Adjust(1500));
        options.Formats = new[] { SymbolFormat.Pdf417 };
        options.MaxSymbols = 1;
        var result = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options);
        Assert.Equal(text, Assert.Single(result.Symbols).Text);
        Assert.True(Pdf417Decoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32,
            out string diagnosticText, out var diagnostics));
        Assert.Equal(text, diagnosticText);
        Assert.True(diagnostics.AttemptCount > 0);
    }

    [Theory]
    [InlineData(1, 2, -4)]
    [InlineData(1, 2, 0)]
    [InlineData(0, 2, 4)]
    public void Pdf417Image_InvalidLayoutReturnsFalseAcrossPixelOverloads(int width, int height, int stride) {
        var pixels = new byte[4];
        Assert.False(Pdf417Decoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out string _));
        Assert.False(Pdf417Decoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32,
            out string _, out var diagnostics));
        Assert.Equal("Invalid image size.", diagnostics.Failure);
        Assert.False(Pdf417Decoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out Pdf417Decoded _));
#if NET8_0_OR_GREATER
        Assert.False(Pdf417Decoder.TryDecode(pixels.AsSpan(), width, height, stride, PixelFormat.Rgba32, out string _));
        Assert.False(Pdf417Decoder.TryDecode(pixels.AsSpan(), width, height, stride, PixelFormat.Rgba32,
            out string _, out _));
        Assert.False(Pdf417Decoder.TryDecode(pixels.AsSpan(), width, height, stride, PixelFormat.Rgba32, out Pdf417Decoded _));
#endif
    }

    [Theory]
    [InlineData(80)]
    [InlineData(4080)]
    public void Pdf417Image_ReadsRowsWithDifferentPerspectiveSlopes(int pagePadding) {
        const string text = "PDF417-FAN-ROWS-1042";
        var symbol = Pdf417Encoder.Encode(text, new Pdf417EncodeOptions {
            ErrorCorrectionLevel = 1, MinColumns = 2, MaxColumns = 2, MinRows = 14, MaxRows = 14
        });
        var inkWidth = symbol.Width * 3;
        var width = inkWidth + pagePadding;
        const int height = 190;
        var pixels = White(width, height);
        for (var x = 0; x < inkWidth; x++) {
            var fraction = x / (double)(inkWidth - 1);
            var top = 12 + 20 * fraction;
            var rowHeight = 9 - 3 * fraction;
            for (var y = 0; y < height; y++) {
                var row = (int)Math.Floor((y - top) / rowHeight);
                if (row < 0 || row >= symbol.Height) continue;
                Set(pixels, width, x + 20, y, symbol[x / 3, row]);
            }
        }
        for (var x = 5; x < width - 5; x++) Set(pixels, width, x, 175, true);
        using var cancellation = new System.Threading.CancellationTokenSource(TestBudget.Adjust(1500));
        Assert.True(Pdf417Decoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32,
            cancellation.Token, out string decoded));
        Assert.Equal(text, decoded);
        var options = ScanOptions.Robust(TestBudget.Adjust(1500));
        options.Image = new ImageDecodeOptions { MaxDimension = 0 };
        options.Formats = new[] { SymbolFormat.Pdf417 };
        options.MaxSymbols = 1;
        var result = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), options);
        Assert.Equal(text, Assert.Single(result.Symbols).Text);
    }

    [Fact]
    public void Pdf417Image_AdjacentRowGroupsRetainIncompleteHeaderEvidence() {
        const string text = "ROW-BIN-1042";
        var symbol = Pdf417Encoder.Encode(text, new Pdf417EncodeOptions {
            ErrorCorrectionLevel = 0, MinColumns = 3, MaxColumns = 3, MinRows = 6, MaxRows = 6
        });
        var width = symbol.Width * 3 + 90;
        const int height = 90;
        var pixels = White(width, height);
        var top = 10;
        for (var row = 0; row < symbol.Height; row++) {
            var rowHeight = row % 3 + 2;
            for (var y = top; y < top + rowHeight; y++)
                for (var x = 0; x < symbol.Width * 3; x++)
                    Set(pixels, width, x + 31 + row, y, symbol[x / 3, row]);
            top += rowHeight;
        }
        for (var x = 5; x < width - 5; x++) Set(pixels, width, x, 80, true);
        using var cancellation = new System.Threading.CancellationTokenSource(TestBudget.Adjust(1500));
        Assert.True(Pdf417Decoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32,
            cancellation.Token, out string decoded));
        Assert.Equal(text, decoded);
    }

    [Fact]
    public void Pdf417Image_CompactSymbolSurvivesSeparateRegularRowFragment() {
        const string text = "COMPACT-INVOICE-1042";
        var compact = Pdf417Encoder.Encode(text, new Pdf417EncodeOptions {
            Compact = true, ErrorCorrectionLevel = 3,
            MinColumns = 4, MaxColumns = 4, MinRows = 18, MaxRows = 18
        });
        var regular = Pdf417Encoder.Encode("REGULAR-FRAGMENT", new Pdf417EncodeOptions {
            ErrorCorrectionLevel = 1, MinColumns = 2, MaxColumns = 2, MinRows = 14, MaxRows = 14
        });
        var width = Math.Max(compact.Width, regular.Width) * 3 + 80;
        const int height = 210;
        var pixels = White(width, height);
        var top = 12;
        for (var row = 0; row < compact.Height; row++) {
            var rowHeight = row % 3 + 2;
            for (var y = top; y < top + rowHeight; y++)
                for (var x = 0; x < compact.Width * 3; x++)
                    Set(pixels, width, x + 20, y, compact[x / 3, row]);
            top += rowHeight;
        }
        for (var y = 120; y < 160; y++)
            for (var x = 0; x < regular.Width * 3; x++)
                Set(pixels, width, x + 20, y, regular[x / 3, 0]);
        for (var x = 5; x < width - 5; x++) Set(pixels, width, x, 195, true);
        Assert.True(Pdf417Decoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32, out string decoded));
        Assert.Equal(text, decoded);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BlankCameraFrame_CompletesDefaultRobustSearch(bool black) {
        const int width = 800, height = 600;
        var pixels = White(width, height);
        if (black) for (var i = 0; i < pixels.Length; i += 4) pixels[i] = pixels[i + 1] = pixels[i + 2] = 0;
        var result = SymbolScanner.Scan(ImageFrame.Packed(pixels, width, height, PixelFormat.Rgba32), ScanOptions.Robust(TestBudget.Adjust(1500)));
        Assert.Empty(result.Symbols);
        Assert.Equal(ScanCompletionReason.Completed, result.CompletionReason);
        Assert.Equal(ScanStatus.NoSymbolFound, result.Status);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void Pdf417Image_ReadsPayloadRunsWithPrintGain(bool thinBars, bool inverted) {
        const string text = "PRINT-GAIN-DISPATCH-1042";
        var symbol = Pdf417Encoder.Encode(text, new Pdf417EncodeOptions {
            ErrorCorrectionLevel = 2, MinColumns = 4, MaxColumns = 4, MinRows = 18, MaxRows = 18
        });
        const int pitch = 4, padding = 20;
        var width = symbol.Width * pitch + padding * 2;
        var height = symbol.Height * 4 + padding * 2;
        var pixels = White(width, height);
        var top = padding;
        for (var y = 0; y < symbol.Height; y++) {
            var rowHeight = y % 3 + 2;
            for (var x = 0; x < symbol.Width * pitch; x++) {
                var dark = symbol[x / pitch, y];
                // Keep the row indicators intact. A localized blur or print defect
                // widens bars (or spaces) in each payload character by half a module.
                if (x >= 34 * pitch && x < (34 + 4 * 17) * pitch) {
                    var before = symbol[(x - 1) / pitch, y];
                    var after = symbol[(x + 1) / pitch, y];
                    dark = thinBars ? dark && before && after : dark || before || after;
                }
                for (var dy = 0; dy < rowHeight; dy++)
                    Set(pixels, width, padding + x, top + dy, dark);
            }
            top += rowHeight;
        }
        for (var x = 5; x < width - 5; x++) Set(pixels, width, x, height - 5, true);
        if (inverted) for (var at = 0; at < pixels.Length; at += 4)
            pixels[at] = pixels[at + 1] = pixels[at + 2] = (byte)(255 - pixels[at]);
        using var cancellation = new System.Threading.CancellationTokenSource(TestBudget.Adjust(1500));
        Assert.True(Pdf417Decoder.TryDecode(pixels, width, height, width * 4, PixelFormat.Rgba32,
            cancellation.Token, out string decoded));
        Assert.Equal(text, decoded);
    }

    private static byte[] White(int width, int height) {
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = 255;
        return pixels;
    }

    private static void Set(byte[] pixels, int width, int x, int y, bool dark) {
        var at = (y * width + x) * 4;
        pixels[at] = pixels[at + 1] = pixels[at + 2] = dark ? (byte)0 : (byte)255;
    }
}
