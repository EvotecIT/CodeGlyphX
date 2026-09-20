using System;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class DecodedMetadataRegressionTests {
    [Theory]
    [InlineData(SymbolFormat.Code128)]
    [InlineData(SymbolFormat.Aztec)]
    [InlineData(SymbolFormat.DataMatrix)]
    [InlineData(SymbolFormat.Pdf417)]
    [InlineData(SymbolFormat.MicroQrCode)]
    [InlineData(SymbolFormat.QrCode)]
    public void OrdinarySymbols_AreNotGs1(SymbolFormat format) {
        var modules = format switch {
            SymbolFormat.Aztec => AztecCode.Encode("HELLO"),
            SymbolFormat.DataMatrix => DataMatrixEncoder.Encode("HELLO"),
            SymbolFormat.Pdf417 => Pdf417Code.Encode("HELLO"),
            SymbolFormat.MicroQrCode => MicroQrCodeEncoder.EncodeText("HELLO").Modules,
            SymbolFormat.QrCode => QrCodeEncoder.EncodeText("HELLO").Modules,
            _ => null
        };
        var png = modules is null
            ? Barcode.Render(BarcodeType.Code128, "HELLO", OutputFormat.Png).Data
            : MatrixPngRenderer.Render(modules, new MatrixPngRenderOptions { ModuleSize = 5 });
        var result = SymbolScanner.Scan(png, new ScanOptions { Formats = new[] { format }, TimeoutMilliseconds = TestBudget.Adjust(5000) });
        Assert.Equal(ScanStatus.Success, result.Status);
        Assert.Equal(SymbolPayloadProfile.None, Assert.Single(result.Symbols).PayloadProfile);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DataMatrixMetadata_SurvivesUnifiedModuleAndPixelFacades(bool gs1) {
        var options = new DataMatrixEncodingOptions { IsGs1 = gs1 };
        var modules = DataMatrixEncoder.Encode("0109506000134352", options);
        Assert.True(CodeGlyph.TryDecode(modules, out var matrix, BarcodeType.DataMatrix));
        Check(matrix, gs1);
        var pixels = MatrixPngRenderer.RenderPixels(modules, new MatrixPngRenderOptions { ModuleSize = 5 }, out var w, out var h, out var stride);
        Assert.True(CodeGlyph.TryDecode(pixels, w, h, stride, PixelFormat.Rgba32, out var basic, qrOptions: QrPixelDecodeOptions.Screen(100)));
        Check(basic, gs1);
        Assert.True(CodeGlyph.TryDecode(pixels, w, h, stride, PixelFormat.Rgba32, out var diagnostic, out var diag, qrOptions: QrPixelDecodeOptions.Screen(100)));
        Check(diagnostic, gs1);
        Assert.True(diag.DataMatrix!.Success);
        Assert.True(diag.DataMatrix.AttemptCount > 0);
        var span = (ReadOnlySpan<byte>)pixels;
        Assert.True(CodeGlyph.TryDecode(span, w, h, stride, PixelFormat.Rgba32, out var spanResult, qrOptions: QrPixelDecodeOptions.Screen(100)));
        Check(spanResult, gs1);
        Assert.True(CodeGlyph.TryDecode(span, w, h, stride, PixelFormat.Rgba32, out var spanDiagnostic, out _, qrOptions: QrPixelDecodeOptions.Screen(100)));
        Check(spanDiagnostic, gs1);
        var budgeted = new CodeGlyphDecodeOptions { Qr = QrPixelDecodeOptions.Screen(100), Image = new ImageDecodeOptions { RecognitionBudgetMilliseconds = 5000 } };
        Assert.True(CodeGlyph.TryDecode(pixels, w, h, stride, PixelFormat.Rgba32, out var budgetResult, budgeted));
        Check(budgetResult, gs1);
        Assert.True(CodeGlyph.TryDecode(pixels, w, h, stride, PixelFormat.Rgba32, out var budgetDiagnostic, out var budgetDiag, budgeted));
        Check(budgetDiagnostic, gs1);
        Assert.True(budgetDiag.DataMatrix!.Success);
        if (gs1) {
            budgeted.IncludeBarcode = false;
            budgeted.Image!.RecognitionBudgetMilliseconds = 500;
            Assert.True(CodeGlyph.TryDecodeAll(pixels, w, h, stride, PixelFormat.Rgba32, out var all, budgeted));
            Check(Assert.Single(all, x => x.Kind == CodeGlyphKind.DataMatrix), true);
        }
    }

    [Fact]
    public void DataMatrixControlMetadata_IsPreserved() {
        var modules = DataMatrixEncoder.Encode("é", new DataMatrixEncodingOptions { EciAssignmentNumber = 26, StructuredAppend = new DataMatrixStructuredAppend(1, 2, 123) });
        Assert.True(CodeGlyph.TryDecode(modules, out var result, BarcodeType.DataMatrix));
        Assert.Equal("é", result.Text);
        Assert.NotNull(result.DataMatrix);
        Assert.Equal(26, Assert.Single(result.DataMatrix!.EciAssignments));
        Assert.Equal(1, result.DataMatrix.StructuredAppend!.Value.Index);
    }

    private static void Check(CodeGlyphDecoded result, bool gs1) {
        Assert.Equal(CodeGlyphKind.DataMatrix, result.Kind);
        Assert.NotNull(result.DataMatrix);
        Assert.Equal(gs1, result.DataMatrix!.IsGs1);
        Assert.Equal("0109506000134352", result.Text);
    }
}
