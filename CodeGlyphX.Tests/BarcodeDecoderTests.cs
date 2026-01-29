using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class BarcodeDecoderTests {
    [Fact]
    public void Decode_Code128_FromPixels() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Code128, "CODEMATRIX-123");
        var pixels = BarcodePngRenderer.RenderPixels(barcode, new BarcodePngRenderOptions {
            ModuleSize = 3,
            QuietZone = 10,
            HeightModules = 40
        }, out var width, out var height, out var stride);

        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded));
        Assert.Equal(BarcodeType.Code128, decoded.Type);
        Assert.Equal("CODEMATRIX-123", decoded.Text);
    }

    [Fact]
    public void Decode_Code128_Cancelled_ReturnsFalse() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Code128, "CANCEL-128");
        var pixels = BarcodePngRenderer.RenderPixels(barcode, new BarcodePngRenderOptions {
            ModuleSize = 3,
            QuietZone = 10,
            HeightModules = 40
        }, out var width, out var height, out var stride);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.False(BarcodeDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, BarcodeType.Code128, options: null, cts.Token, out _));
    }

    [Fact]
    public void Decode_Gs1_128_FromPixels() {
        var aiText = "(01)09506000134352(10)ABC123";
        var barcode = BarcodeEncoder.Encode(BarcodeType.GS1_128, aiText);
        var pixels = BarcodePngRenderer.RenderPixels(barcode, new BarcodePngRenderOptions {
            ModuleSize = 3,
            QuietZone = 10,
            HeightModules = 40
        }, out var width, out var height, out var stride);

        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded));
        Assert.Equal(BarcodeType.GS1_128, decoded.Type);
        Assert.Equal(Gs1.ElementString(aiText), decoded.Text);
    }

    [Fact]
    public void Decode_Code128_FromPng() {
        var png = Barcode.Render(BarcodeType.Code128, "CODEMATRIX-123", OutputFormat.Png, new BarcodeOptions {
            ModuleSize = 3,
            QuietZone = 10,
            HeightModules = 40
        }).Data;

        Assert.True(Barcode.TryDecodePng(png, out var decoded));
        Assert.Equal(BarcodeType.Code128, decoded.Type);
        Assert.Equal("CODEMATRIX-123", decoded.Text);
    }

    [Fact]
    public void Decode_Code128_FromPng_Cancelled_ReturnsFalse() {
        var png = Barcode.Render(BarcodeType.Code128, "CANCEL-PNG", OutputFormat.Png, new BarcodeOptions {
            ModuleSize = 3,
            QuietZone = 10,
            HeightModules = 40
        }).Data;

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.False(Barcode.TryDecodePng(png, BarcodeType.Code128, cts.Token, out _));
    }

    [Fact]
    public void Decode_Code39_FromPixels() {
        var barcode = BarcodeEncoder.EncodeCode39("ABC123", includeChecksum: true, fullAsciiMode: false);
        var pixels = BarcodePngRenderer.RenderPixels(barcode, new BarcodePngRenderOptions {
            ModuleSize = 3,
            QuietZone = 10,
            HeightModules = 40
        }, out var width, out var height, out var stride);

        var options = new BarcodeDecodeOptions {
            Code39Checksum = Code39ChecksumPolicy.StripIfValid
        };
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, options, out var decoded));
        Assert.Equal(BarcodeType.Code39, decoded.Type);
        Assert.Equal("ABC123", decoded.Text);
    }

    [Fact]
    public void Decode_Codabar_FromPixels() {
        var barcode = BarcodeEncoder.EncodeCodabar("40156", start: 'A', stop: 'B');
        var pixels = BarcodePngRenderer.RenderPixels(barcode, new BarcodePngRenderOptions {
            ModuleSize = 3,
            QuietZone = 10,
            HeightModules = 40
        }, out var width, out var height, out var stride);

        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded));
        Assert.Equal(BarcodeType.Codabar, decoded.Type);
        Assert.Equal("40156", decoded.Text);
    }

    [Fact]
    public void Decode_Msi_FromPixels() {
        var barcode = BarcodeEncoder.EncodeMsi("123456");
        var pixels = BarcodePngRenderer.RenderPixels(barcode, new BarcodePngRenderOptions {
            ModuleSize = 3,
            QuietZone = 10,
            HeightModules = 40
        }, out var width, out var height, out var stride);

        var options = new BarcodeDecodeOptions {
            MsiChecksum = MsiChecksumPolicy.StripIfValid
        };
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, options, out var decoded));
        Assert.Equal(BarcodeType.MSI, decoded.Type);
        Assert.Equal("123456", decoded.Text);
    }

    [Fact]
    public void Decode_Code11_FromPixels() {
        var barcode = BarcodeEncoder.EncodeCode11("123-45", includeChecksum: true);
        var pixels = BarcodePngRenderer.RenderPixels(barcode, new BarcodePngRenderOptions {
            ModuleSize = 3,
            QuietZone = 10,
            HeightModules = 40
        }, out var width, out var height, out var stride);

        var options = new BarcodeDecodeOptions {
            Code11Checksum = Code11ChecksumPolicy.StripIfValid
        };
        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, options, out var decoded));
        Assert.Equal(BarcodeType.Code11, decoded.Type);
        Assert.Equal("123-45", decoded.Text);
    }

    [Fact]
    public void Decode_Plessey_FromPixels() {
        var barcode = BarcodeEncoder.EncodePlessey("1A2B");
        var pixels = BarcodePngRenderer.RenderPixels(barcode, new BarcodePngRenderOptions {
            ModuleSize = 3,
            QuietZone = 10,
            HeightModules = 40
        }, out var width, out var height, out var stride);

        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded));
        Assert.Equal(BarcodeType.Plessey, decoded.Type);
        Assert.Equal("1A2B", decoded.Text);
    }

    [Fact]
    public void Decode_Telepen_FromPixels() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Telepen, "HELLO-123");
        var pixels = BarcodePngRenderer.RenderPixels(barcode, new BarcodePngRenderOptions {
            ModuleSize = 3,
            QuietZone = 10,
            HeightModules = 40
        }, out var width, out var height, out var stride);

        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded));
        Assert.Equal(BarcodeType.Telepen, decoded.Type);
        Assert.Equal("HELLO-123", decoded.Text);
    }

    [Fact]
    public void Decode_Ean13_FromPixels() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.EAN, "590123412345");
        var pixels = BarcodePngRenderer.RenderPixels(barcode, new BarcodePngRenderOptions {
            ModuleSize = 3,
            QuietZone = 10,
            HeightModules = 40
        }, out var width, out var height, out var stride);

        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded));
        Assert.Equal(BarcodeType.EAN, decoded.Type);
        Assert.Equal("5901234123457", decoded.Text);
    }

    [Fact]
    public void Decode_Ean13_FromModules_Stretched() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.EAN, "590123412345");
        var modules = ExpandModules(barcode);
        var stretched = RepeatModules(modules, 2);

        Assert.True(BarcodeDecoder.TryDecode(stretched, out var decoded));
        Assert.Equal(BarcodeType.EAN, decoded.Type);
        Assert.Equal("5901234123457", decoded.Text);
    }

    [Fact]
    public void Decode_Ean13_WithAddOn_FromModules() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.EAN, "590123412345+12");
        var modules = ExpandModules(barcode);

        Assert.True(BarcodeDecoder.TryDecode(modules, out var decoded));
        Assert.Equal(BarcodeType.EAN, decoded.Type);
        Assert.Equal("5901234123457+12", decoded.Text);
    }

    [Fact]
    public void Decode_Code128_FromModules_Reversed() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Code128, "REVERSED-128");
        var modules = ExpandModules(barcode);
        var reversed = ReverseModules(modules);

        Assert.True(BarcodeDecoder.TryDecode(reversed, out var decoded));
        Assert.Equal(BarcodeType.Code128, decoded.Type);
        Assert.Equal("REVERSED-128", decoded.Text);
    }

    [Fact]
    public void Decode_UpcE_FromModules_Stretched() {
        const string digits = "042100";
        var barcode = BarcodeEncoder.Encode(BarcodeType.UPCE, digits);
        var modules = ExpandModules(barcode);
        var stretched = RepeatModules(modules, 2);

        Assert.True(BarcodeDecoder.TryDecode(stretched, out var decoded));
        Assert.Equal(BarcodeType.UPCE, decoded.Type);
        Assert.Equal(BuildUpcEWithChecksum('0', digits), decoded.Text);
    }

    [Fact]
    public void Decode_Itf_FromModules() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.ITF, "123456");
        var modules = ExpandModules(barcode);

        Assert.True(BarcodeDecoder.TryDecode(modules, out var decoded));
        Assert.Equal(BarcodeType.ITF, decoded.Type);
        Assert.Equal("123456", decoded.Text);
    }

    [Fact]
    public void Decode_Matrix2of5_FromModules() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Matrix2of5, "123456");
        var modules = ExpandModules(barcode);

        Assert.True(BarcodeDecoder.TryDecode(modules, out var decoded));
        Assert.Equal(BarcodeType.Matrix2of5, decoded.Type);
        Assert.Equal("123456", decoded.Text);
    }

    [Fact]
    public void Decode_Industrial2of5_FromModules() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Industrial2of5, "123456");
        var modules = ExpandModules(barcode);

        Assert.True(BarcodeDecoder.TryDecode(modules, out var decoded));
        Assert.Equal(BarcodeType.Industrial2of5, decoded.Type);
        Assert.Equal("123456", decoded.Text);
    }

    [Fact]
    public void Decode_Iata2of5_FromModules() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.IATA2of5, "123456");
        var modules = ExpandModules(barcode);

        Assert.True(BarcodeDecoder.TryDecode(modules, out var decoded));
        Assert.Equal(BarcodeType.IATA2of5, decoded.Type);
        Assert.Equal("123456", decoded.Text);
    }

    [Fact]
    public void Decode_PatchCode_FromModules() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.PatchCode, "T");
        var modules = ExpandModules(barcode);

        Assert.True(BarcodeDecoder.TryDecode(modules, out var decoded));
        Assert.Equal(BarcodeType.PatchCode, decoded.Type);
        Assert.Equal("T", decoded.Text);
    }

    [Fact]
    public void Decode_Telepen_FromModules() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Telepen, "ABC123");
        var modules = ExpandModules(barcode);

        Assert.True(BarcodeDecoder.TryDecode(modules, out var decoded));
        Assert.Equal(BarcodeType.Telepen, decoded.Type);
        Assert.Equal("ABC123", decoded.Text);
    }

    [Fact]
    public void Decode_Pharmacode_FromModules() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Pharmacode, "91");
        var modules = ExpandModules(barcode);

        Assert.True(BarcodeDecoder.TryDecode(modules, BarcodeType.Pharmacode, out var decoded));
        Assert.Equal(BarcodeType.Pharmacode, decoded.Type);
        Assert.Equal("91", decoded.Text);
    }

    [Fact]
    public void Decode_Code32_FromModules() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Code32, "02608901");
        var modules = ExpandModules(barcode);

        Assert.True(BarcodeDecoder.TryDecode(modules, BarcodeType.Code32, out var decoded));
        Assert.Equal(BarcodeType.Code32, decoded.Type);
        Assert.Equal("026089019", decoded.Text);
    }

    [Fact]
    public void Encode_Code32_UsesBase32Alphabet() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Code32, "02608901");
        var modules = ExpandModules(barcode);

        Assert.True(BarcodeDecoder.TryDecode(modules, BarcodeType.Code39, out var decoded));
        Assert.Equal(BarcodeType.Code39, decoded.Type);
        Assert.Equal("0SW5KV", decoded.Text);
    }

    [Fact]
    public void Decode_Itf14_FromPixels() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.ITF14, "1234567890123");
        var pixels = BarcodePngRenderer.RenderPixels(barcode, new BarcodePngRenderOptions {
            ModuleSize = 3,
            QuietZone = 10,
            HeightModules = 40
        }, out var width, out var height, out var stride);

        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded));
        Assert.Equal(BarcodeType.ITF14, decoded.Type);
        Assert.Equal("12345678901231", decoded.Text);
    }

    [Fact]
    public void Decode_Code128_FromPixels_Rotated() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Code128, "ROTATE-128");
        var pixels = BarcodePngRenderer.RenderPixels(barcode, new BarcodePngRenderOptions {
            ModuleSize = 3,
            QuietZone = 10,
            HeightModules = 40
        }, out var width, out var height, out var stride);

        var rotated = RotateClockwise(pixels, width, height, out var rotWidth, out var rotHeight);

        Assert.True(BarcodeDecoder.TryDecode(rotated, rotWidth, rotHeight, rotWidth * 4, PixelFormat.Rgba32, out var decoded));
        Assert.Equal(BarcodeType.Code128, decoded.Type);
        Assert.Equal("ROTATE-128", decoded.Text);
    }

    [Fact]
    public void Decode_Code128_FromPixels_Inverted() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Code128, "INVERT-128");
        var pixels = BarcodePngRenderer.RenderPixels(barcode, new BarcodePngRenderOptions {
            ModuleSize = 3,
            QuietZone = 10,
            HeightModules = 40
        }, out var width, out var height, out var stride);

        InvertPixels(pixels);

        Assert.True(BarcodeDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, out var decoded));
        Assert.Equal(BarcodeType.Code128, decoded.Type);
        Assert.Equal("INVERT-128", decoded.Text);
    }

    [Fact]
    public void Decode_Code128_SetA_FromModules() {
        var input = "\u0001AB\u001F";
        var barcode = BarcodeEncoder.Encode(BarcodeType.Code128, input);
        var modules = ExpandModules(barcode);

        Assert.True(BarcodeDecoder.TryDecode(modules, out var decoded));
        Assert.Equal(BarcodeType.Code128, decoded.Type);
        Assert.Equal(input, decoded.Text);
    }

    [Fact]
    public void Decode_Gs1DataBarTruncated_FromModules() {
        var input = "0001234567890";
        var barcode = BarcodeEncoder.Encode(BarcodeType.GS1DataBarTruncated, input);
        var modules = ExpandModules(barcode);

        Assert.True(BarcodeDecoder.TryDecode(modules, out var decoded));
        Assert.Equal(BarcodeType.GS1DataBarTruncated, decoded.Type);
        Assert.Equal(input, decoded.Text);
    }

    [Fact]
    public void Decode_Gs1DataBarExpanded_FromModules() {
        var input = "(01)98898765432106(3103)000001";
        var barcode = BarcodeEncoder.Encode(BarcodeType.GS1DataBarExpanded, input);
        var modules = ExpandModules(barcode);

        Assert.True(BarcodeDecoder.TryDecode(modules, out var decoded));
        Assert.Equal(BarcodeType.GS1DataBarExpanded, decoded.Type);
        Assert.Equal(Gs1.ElementString(input), decoded.Text);
    }

    private static byte[] RotateClockwise(byte[] pixels, int width, int height, out int outWidth, out int outHeight) {
        outWidth = height;
        outHeight = width;
        var output = new byte[outWidth * outHeight * 4];

        for (var y = 0; y < height; y++) {
            var row = y * width * 4;
            for (var x = 0; x < width; x++) {
                var src = row + x * 4;
                var nx = outWidth - 1 - y;
                var ny = x;
                var dst = (ny * outWidth + nx) * 4;
                output[dst + 0] = pixels[src + 0];
                output[dst + 1] = pixels[src + 1];
                output[dst + 2] = pixels[src + 2];
                output[dst + 3] = pixels[src + 3];
            }
        }

        return output;
    }

    private static void InvertPixels(byte[] pixels) {
        for (var i = 0; i < pixels.Length; i += 4) {
            pixels[i + 0] = (byte)(255 - pixels[i + 0]);
            pixels[i + 1] = (byte)(255 - pixels[i + 1]);
            pixels[i + 2] = (byte)(255 - pixels[i + 2]);
        }
    }

    private static bool[] ExpandModules(Barcode1D barcode) {
        var list = new List<bool>(barcode.TotalModules);
        foreach (var seg in barcode.Segments) {
            for (var i = 0; i < seg.Modules; i++) list.Add(seg.IsBar);
        }
        return list.ToArray();
    }

    private static bool[] RepeatModules(bool[] modules, int factor) {
        var output = new bool[modules.Length * factor];
        var offset = 0;
        for (var i = 0; i < modules.Length; i++) {
            var bit = modules[i];
            for (var f = 0; f < factor; f++) output[offset++] = bit;
        }
        return output;
    }

    private static bool[] ReverseModules(bool[] modules) {
        var output = new bool[modules.Length];
        for (var i = 0; i < modules.Length; i++) {
            output[i] = modules[modules.Length - 1 - i];
        }
        return output;
    }


    private static string BuildUpcEWithChecksum(char numberSystem, string digits) {
        var upcA = ExpandUpcEToUpcA(numberSystem, digits);
        var check = CalcUpcAChecksum(upcA);
        return numberSystem + digits + check;
    }

    private static string ExpandUpcEToUpcA(char numberSystem, string digits) {
        return digits[5] switch {
            '0' or '1' or '2' => $"{numberSystem}{digits.Substring(0, 2)}{digits[5]}0000{digits.Substring(2, 3)}",
            '3' => $"{numberSystem}{digits.Substring(0, 3)}00000{digits.Substring(3, 2)}",
            '4' => $"{numberSystem}{digits.Substring(0, 4)}00000{digits[4]}",
            _ => $"{numberSystem}{digits.Substring(0, 5)}0000{digits[5]}"
        };
    }

    private static char CalcUpcAChecksum(string content) {
        var digits = content.Select(c => c - '0').ToArray();
        var sum = 3 * (digits[0] + digits[2] + digits[4] + digits[6] + digits[8] + digits[10]);
        sum += digits[1] + digits[3] + digits[5] + digits[7] + digits[9];
        sum %= 10;
        sum = sum != 0 ? 10 - sum : 0;
        return (char)(sum + '0');
    }
}
