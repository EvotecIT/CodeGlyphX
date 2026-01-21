using System.Text;
using CodeGlyphX;
using System.Threading;
using CodeGlyphX.AustraliaPost;
using CodeGlyphX.DataBar;
using CodeGlyphX.JapanPost;
using CodeGlyphX.Kix;
using CodeGlyphX.Pharmacode;
using CodeGlyphX.Postal;
using CodeGlyphX.Pdf417;
using CodeGlyphX.RoyalMail;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class Barcode2DTests {
    [Fact]
    public void Kix_Size_IsExpected() {
        var matrix = MatrixBarcodeEncoder.EncodeKix("012345");
        Assert.Equal(47, matrix.Width);
        Assert.Equal(8, matrix.Height);
    }

    [Fact]
    public void Kix_RoundTrip_Modules() {
        var matrix = KixEncoder.Encode("AB12CD34");
        Assert.True(KixDecoder.TryDecode(matrix, out var text));
        Assert.Equal("AB12CD34", text);
    }

    [Fact]
    public void RoyalMail4State_RoundTrip_Modules() {
        var matrix = RoyalMailFourStateEncoder.Encode("AB12CD34", includeHeaders: true);
        Assert.True(RoyalMailFourStateDecoder.TryDecode(matrix, out var text));
        Assert.Equal("AB12CD34", text);
    }

    [Fact]
    public void AustraliaPost_Standard_RoundTrip_Modules() {
        var matrix = AustraliaPostEncoder.Encode("12345678");
        Assert.True(AustraliaPostDecoder.TryDecode(matrix, preferredCustomerTable: null, out var decoded));
        Assert.Equal(AustraliaPostFormat.Standard, decoded.Format);
        Assert.Equal("12345678", decoded.Value);
    }

    [Fact]
    public void AustraliaPost_Customer2C_RoundTrip_Modules() {
        var matrix = AustraliaPostEncoder.Encode("12345678ABCDE");
        Assert.True(AustraliaPostDecoder.TryDecode(matrix, preferredCustomerTable: null, out var decoded));
        Assert.Equal(AustraliaPostFormat.Customer2, decoded.Format);
        Assert.Equal(AustraliaPostCustomerEncodingTable.C, decoded.CustomerInfoEncoding);
        Assert.Equal("12345678ABCDE", decoded.Value);
    }

    [Fact]
    public void AustraliaPost_Customer3C_RoundTrip_Modules() {
        var matrix = AustraliaPostEncoder.Encode("12345678AbCdEfGhIj");
        Assert.True(AustraliaPostDecoder.TryDecode(matrix, preferredCustomerTable: null, out var decoded));
        Assert.Equal(AustraliaPostFormat.Customer3, decoded.Format);
        Assert.Equal(AustraliaPostCustomerEncodingTable.C, decoded.CustomerInfoEncoding);
        Assert.Equal("12345678AbCdEfGhIj", decoded.Value);
    }

    [Fact]
    public void JapanPost_RoundTrip_Modules() {
        var matrix = JapanPostEncoder.Encode("ABKUT-123");
        Assert.True(JapanPostDecoder.TryDecode(matrix, out var text));
        Assert.Equal("ABKUT-123", text);
    }

    [Fact]
    public void Gs1DataBarOmni_Size_IsExpected() {
        var matrix = DataBar14Encoder.EncodeOmni("1234567890123");
        Assert.Equal(50, matrix.Width);
        Assert.Equal(5, matrix.Height);
    }

    [Fact]
    public void Gs1DataBarStacked_Size_IsExpected() {
        var matrix = DataBar14Encoder.EncodeStacked("1234567890123");
        Assert.Equal(50, matrix.Width);
        Assert.Equal(3, matrix.Height);
    }

    [Fact]
    public void Gs1DataBarOmni_RoundTrip_Modules() {
        var matrix = DataBar14Encoder.EncodeOmni("0001234567890");
        Assert.True(DataBar14Decoder.TryDecodeOmni(matrix, out var text));
        Assert.Equal("0001234567890", text);
    }

    [Fact]
    public void Gs1DataBarStacked_RoundTrip_Modules() {
        var matrix = DataBar14Encoder.EncodeStacked("0001234567890");
        Assert.True(DataBar14Decoder.TryDecodeStacked(matrix, out var text));
        Assert.Equal("0001234567890", text);
    }

    [Fact]
    public void Gs1DataBarExpandedStacked_RoundTrip_Modules() {
        var input = "(01)98898765432106(3103)000001";
        var matrix = DataBarExpandedEncoder.EncodeExpandedStacked(input, columns: 2);
        Assert.True(DataBarExpandedDecoder.TryDecodeExpandedStacked(matrix, out var text));
        Assert.Equal(Gs1.ElementString(input), text);
    }

    [Fact]
    public void PharmacodeTwoTrack_RoundTrip_Modules() {
        var matrix = PharmacodeTwoTrackEncoder.Encode("12345");
        Assert.True(PharmacodeTwoTrackDecoder.TryDecode(matrix, out var text));
        Assert.Equal("12345", text);
    }

    [Fact]
    public void Postnet_RoundTrip_Modules() {
        var matrix = PostnetEncoder.Encode("12345");
        Assert.True(PostnetDecoder.TryDecode(matrix, out var text));
        Assert.Equal("123455", text);
    }

    [Fact]
    public void Planet_RoundTrip_Modules() {
        var matrix = PlanetEncoder.Encode("12345");
        Assert.True(PlanetDecoder.TryDecode(matrix, out var text));
        Assert.Equal("123455", text);
    }

    [Fact]
    public void DataMatrix_RoundTrip_Modules() {
        var matrix = DataMatrix.DataMatrixEncoder.Encode("DataMatrixExample");
        Assert.True(DataMatrix.DataMatrixDecoder.TryDecode(matrix, out var text));
        Assert.Equal("DataMatrixExample", text);
    }

    [Fact]
    public void DataMatrix_RoundTrip_C40() {
        var matrix = DataMatrix.DataMatrixEncoder.Encode("ABC123", DataMatrix.DataMatrixEncodingMode.C40);
        Assert.True(DataMatrix.DataMatrixDecoder.TryDecode(matrix, out var text));
        Assert.Equal("ABC123", text);
    }

    [Fact]
    public void DataMatrix_RoundTrip_Text() {
        var matrix = DataMatrix.DataMatrixEncoder.Encode("hello123", DataMatrix.DataMatrixEncodingMode.Text);
        Assert.True(DataMatrix.DataMatrixDecoder.TryDecode(matrix, out var text));
        Assert.Equal("hello123", text);
    }

    [Fact]
    public void DataMatrix_RoundTrip_X12() {
        var matrix = DataMatrix.DataMatrixEncoder.Encode("ABC123", DataMatrix.DataMatrixEncodingMode.X12);
        Assert.True(DataMatrix.DataMatrixDecoder.TryDecode(matrix, out var text));
        Assert.Equal("ABC123", text);
    }

    [Fact]
    public void DataMatrix_RoundTrip_Edifact() {
        var matrix = DataMatrix.DataMatrixEncoder.Encode("ABC_", DataMatrix.DataMatrixEncodingMode.Edifact);
        Assert.True(DataMatrix.DataMatrixDecoder.TryDecode(matrix, out var text));
        Assert.Equal("ABC_", text);
    }

    [Fact]
    public void DataMatrix_RoundTrip_Pixels() {
        var matrix = DataMatrix.DataMatrixEncoder.Encode("MatrixTest");
        var pixels = Rendering.Png.MatrixPngRenderer.RenderPixels(
            matrix,
            new Rendering.Png.MatrixPngRenderOptions { ModuleSize = 4, QuietZone = 2 },
            out var width,
            out var height,
            out var stride);

        Assert.True(DataMatrix.DataMatrixDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var text));
        Assert.Equal("MatrixTest", text);
    }

    [Fact]
    public void DataMatrix_Decode_Cancelled_ReturnsFalse() {
        var matrix = DataMatrix.DataMatrixEncoder.Encode("MatrixCancel");
        var pixels = Rendering.Png.MatrixPngRenderer.RenderPixels(
            matrix,
            new Rendering.Png.MatrixPngRenderOptions { ModuleSize = 4, QuietZone = 2 },
            out var width,
            out var height,
            out var stride);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.False(DataMatrix.DataMatrixDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, cts.Token, out _));
    }

    [Fact]
    public void DataMatrix_RoundTrip_Pixels_Mirrored() {
        var matrix = DataMatrix.DataMatrixEncoder.Encode("MatrixMirror");
        var pixels = Rendering.Png.MatrixPngRenderer.RenderPixels(
            matrix,
            new Rendering.Png.MatrixPngRenderOptions { ModuleSize = 4, QuietZone = 2 },
            out var width,
            out var height,
            out var stride);

        var mirrored = MirrorPixels(pixels, width, height);
        Assert.True(DataMatrix.DataMatrixDecoder.TryDecode(mirrored, width, height, stride, PixelFormat.Rgba32, out var text));
        Assert.Equal("MatrixMirror", text);
    }

    [Fact]
    public void Pdf417_RoundTrip_Modules() {
        var matrix = Pdf417.Pdf417Encoder.Encode("Pdf417Example");
        Assert.True(Pdf417.Pdf417Decoder.TryDecode(matrix, out string text));
        Assert.Equal("Pdf417Example", text);
    }

    [Fact]
    public void UspsImb_Encode_MatchesReference_Length20() {
        var matrix = UspsImbEncoder.Encode("12345678901234567890");
        var pattern = ImbPattern(matrix);
        Assert.Equal("AFDADFAAAFTDDTDDTATADDFAADFDDFTAFTDTAFTFTDTATFTFAFDFDFFFFTFFFATDD", pattern);
    }

    [Fact]
    public void UspsImb_RoundTrip_Modules() {
        var matrix = UspsImbEncoder.Encode("12345678901234567890-12345");
        Assert.True(UspsImbDecoder.TryDecode(matrix, out var text));
        Assert.Equal("12345678901234567890-12345", text);
    }

    [Fact]
    public void MicroPdf417_RoundTrip_Modules() {
        var matrix = MicroPdf417Encoder.Encode("Micro417");
        Assert.True(MicroPdf417Decoder.TryDecode(matrix, out var text));
        Assert.Equal("Micro417", text);
    }

    [Fact]
    public void Pdf417_Macro_RoundTrip_Modules() {
        var options = new Pdf417MacroOptions {
            SegmentIndex = 5,
            FileId = "123456",
            IsLastSegment = true,
            SegmentCount = 12,
            FileName = "FILE-A",
            Timestamp = 20250101,
            Sender = "SENDER",
            Addressee = "ADDR",
            FileSize = 98765,
            Checksum = 12345
        };

        var matrix = Pdf417Encoder.EncodeMacro("MacroPayload", options);
        Assert.True(Pdf417Decoder.TryDecode(matrix, out Pdf417Decoded decoded));
        Assert.Equal("MacroPayload", decoded.Text);
        Assert.NotNull(decoded.Macro);
        Assert.Equal(5, decoded.Macro!.SegmentIndex);
        Assert.Equal("123456", decoded.Macro.FileId);
        Assert.True(decoded.Macro.IsLastSegment);
        Assert.Equal(12, decoded.Macro.SegmentCount);
        Assert.Equal("FILE-A", decoded.Macro.FileName);
        Assert.Equal(20250101, decoded.Macro.Timestamp);
        Assert.Equal("SENDER", decoded.Macro.Sender);
        Assert.Equal("ADDR", decoded.Macro.Addressee);
        Assert.Equal(98765, decoded.Macro.FileSize);
        Assert.Equal(12345, decoded.Macro.Checksum);
    }

    [Fact]
    public void Pdf417_RoundTrip_Modules_WithPadding() {
        var matrix = Pdf417.Pdf417Encoder.Encode("Pdf417Example");
        var padded = PadColumns(matrix, left: 4, right: 3);
        Assert.True(Pdf417.Pdf417Decoder.TryDecode(padded, out string text));
        Assert.Equal("Pdf417Example", text);
    }

    [Fact]
    public void Pdf417_RoundTrip_Pixels() {
        var matrix = Pdf417.Pdf417Encoder.Encode("Pdf417Example");
        var pixels = Rendering.Png.MatrixPngRenderer.RenderPixels(
            matrix,
            new Rendering.Png.MatrixPngRenderOptions { ModuleSize = 3, QuietZone = 2 },
            out var width,
            out var height,
            out var stride);

        Assert.True(Pdf417.Pdf417Decoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out string text));
        Assert.Equal("Pdf417Example", text);
    }

    [Fact]
    public void Pdf417_Decode_Cancelled_ReturnsFalse() {
        var matrix = Pdf417.Pdf417Encoder.Encode("Pdf417Cancel");
        var pixels = Rendering.Png.MatrixPngRenderer.RenderPixels(
            matrix,
            new Rendering.Png.MatrixPngRenderOptions { ModuleSize = 3, QuietZone = 2 },
            out var width,
            out var height,
            out var stride);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.False(Pdf417.Pdf417Decoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, cts.Token, out _));
    }

    [Fact]
    public void Pdf417_RoundTrip_Pixels_Mirrored() {
        var matrix = Pdf417.Pdf417Encoder.Encode("Pdf417Mirror");
        var pixels = Rendering.Png.MatrixPngRenderer.RenderPixels(
            matrix,
            new Rendering.Png.MatrixPngRenderOptions { ModuleSize = 3, QuietZone = 2 },
            out var width,
            out var height,
            out var stride);

        var mirrored = MirrorPixels(pixels, width, height);
        Assert.True(Pdf417.Pdf417Decoder.TryDecode(mirrored, width, height, stride, PixelFormat.Rgba32, out string text));
        Assert.Equal("Pdf417Mirror", text);
    }


    private static BitMatrix PadColumns(BitMatrix matrix, int left, int right) {
        var width = matrix.Width + left + right;
        var height = matrix.Height;
        var padded = new BitMatrix(width, height);
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < matrix.Width; x++) {
                padded[left + x, y] = matrix[x, y];
            }
        }
        return padded;
    }

    private static byte[] MirrorPixels(byte[] pixels, int width, int height) {
        var output = new byte[pixels.Length];
        for (var y = 0; y < height; y++) {
            var row = y * width * 4;
            for (var x = 0; x < width; x++) {
                var src = row + x * 4;
                var nx = width - 1 - x;
                var dst = row + nx * 4;
                output[dst + 0] = pixels[src + 0];
                output[dst + 1] = pixels[src + 1];
                output[dst + 2] = pixels[src + 2];
                output[dst + 3] = pixels[src + 3];
            }
        }
        return output;
    }

    private static string ImbPattern(BitMatrix modules) {
        var bars = ExtractImbBars(modules);
        var sb = new StringBuilder(bars.Count);
        for (var i = 0; i < bars.Count; i++) {
            sb.Append(bars[i] switch {
                ImbBarType.Ascender => 'A',
                ImbBarType.Descender => 'D',
                ImbBarType.Full => 'F',
                _ => 'T'
            });
        }
        return sb.ToString();
    }

    private static List<ImbBarType> ExtractImbBars(BitMatrix modules) {
        var bars = new List<ImbBarType>(65);
        var height = modules.Height;

        var first = -1;
        var last = -1;
        for (var x = 0; x < modules.Width; x++) {
            if (HasBar(modules, x)) {
                if (first < 0) first = x;
                last = x;
            }
        }

        if (first < 0 || last < 0) return bars;

        var runs = new List<(bool isBar, int start, int length)>(modules.Width / 2);
        var current = HasBar(modules, first);
        var runStart = first;
        for (var x = first + 1; x <= last; x++) {
            var isBar = HasBar(modules, x);
            if (isBar == current) continue;
            runs.Add((current, runStart, x - runStart));
            current = isBar;
            runStart = x;
        }
        runs.Add((current, runStart, last - runStart + 1));

        foreach (var run in runs) {
            if (!run.isBar) continue;
            var asc = false;
            var desc = false;
            var tracker = false;
            for (var x = run.start; x < run.start + run.length; x++) {
                if (!tracker && (modules[x, 3] || modules[x, 4])) tracker = true;
                if (!asc && (modules[x, 0] || modules[x, 1] || modules[x, 2])) asc = true;
                if (!desc && (modules[x, height - 1] || modules[x, height - 2] || modules[x, height - 3])) desc = true;
            }

            if (!tracker) continue;
            if (asc && desc) {
                bars.Add(ImbBarType.Full);
            } else if (asc) {
                bars.Add(ImbBarType.Ascender);
            } else if (desc) {
                bars.Add(ImbBarType.Descender);
            } else {
                bars.Add(ImbBarType.Tracker);
            }
        }

        return bars;
    }

    private static bool HasBar(BitMatrix modules, int x) {
        for (var y = 0; y < modules.Height; y++) {
            if (modules[x, y]) return true;
        }
        return false;
    }

    private enum ImbBarType {
        Tracker,
        Ascender,
        Descender,
        Full
    }
}
