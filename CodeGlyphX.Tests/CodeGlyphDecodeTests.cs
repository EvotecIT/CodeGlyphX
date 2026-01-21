using CodeGlyphX.DataMatrix;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class CodeGlyphDecodeTests {
    [Fact]
    public void Decode_Qr_FromPng() {
        var png = QR.Png("HELLO");

        Assert.True(CodeGlyph.TryDecodePng(png, out var decoded));
        Assert.Equal(CodeGlyphKind.Qr, decoded.Kind);
        Assert.Equal("HELLO", decoded.Text);
    }

    [Fact]
    public void Decode_Barcode_FromPng() {
        var png = Barcode.Png(BarcodeType.Code128, "CODE128-12345", new BarcodeOptions {
            ModuleSize = 3,
            QuietZone = 10,
            HeightModules = 40
        });

        Assert.True(CodeGlyph.TryDecodePng(png, out var decoded));
        Assert.Equal(CodeGlyphKind.Barcode1D, decoded.Kind);
        Assert.Equal("CODE128-12345", decoded.Text);
    }

    [Fact]
    public void Decode_DataMatrix_FromPng() {
        var matrix = DataMatrixEncoder.Encode("DM-1234");
        var png = MatrixPngRenderer.Render(matrix, new MatrixPngRenderOptions {
            ModuleSize = 3,
            QuietZone = 2
        });

        Assert.True(CodeGlyph.TryDecodePng(png, out var decoded));
        Assert.Equal(CodeGlyphKind.DataMatrix, decoded.Kind);
        Assert.Equal("DM-1234", decoded.Text);
    }

    [Fact]
    public void Decode_Pdf417_FromPng() {
        var matrix = Pdf417Encoder.Encode("PDF-417");
        var png = MatrixPngRenderer.Render(matrix, new MatrixPngRenderOptions {
            ModuleSize = 3,
            QuietZone = 2
        });

        Assert.True(CodeGlyph.TryDecodePng(png, out var decoded));
        Assert.Equal(CodeGlyphKind.Pdf417, decoded.Kind);
        Assert.Equal("PDF-417", decoded.Text);
    }

    [Fact]
    public void Decode_Aztec_FromPng() {
        var matrix = AztecCode.Encode("AZTEC-OK");
        var png = MatrixPngRenderer.Render(matrix, new MatrixPngRenderOptions {
            ModuleSize = 3,
            QuietZone = 2
        });

        Assert.True(CodeGlyph.TryDecodePng(png, out var decoded));
        Assert.Equal(CodeGlyphKind.Aztec, decoded.Kind);
        Assert.Equal("AZTEC-OK", decoded.Text);
    }

    [Fact]
    public void DecodeAll_Qr_FromPng() {
        var png = QR.Png("HELLO-ALL");

        Assert.True(CodeGlyph.TryDecodeAllPng(png, out var decoded, includeBarcode: false));
        Assert.Single(decoded);
        Assert.Equal(CodeGlyphKind.Qr, decoded[0].Kind);
        Assert.Equal("HELLO-ALL", decoded[0].Text);
    }

    [Fact]
    public void DecodeAll_Barcode_FromPng() {
        var png = Barcode.Png(BarcodeType.Code128, "CODE128-ALL", new BarcodeOptions {
            ModuleSize = 3,
            QuietZone = 10,
            HeightModules = 40
        });

        Assert.True(CodeGlyph.TryDecodeAllPng(png, out var decoded, expectedBarcode: BarcodeType.Code128, includeBarcode: true, preferBarcode: true));
        Assert.Single(decoded);
        Assert.Equal(CodeGlyphKind.Barcode1D, decoded[0].Kind);
        Assert.Equal("CODE128-ALL", decoded[0].Text);
    }
}
