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
            SymbolFormat.Aztec => AztecCode.Encode("HELLO").Modules,
            SymbolFormat.DataMatrix => DataMatrixEncoder.Encode("HELLO"),
            SymbolFormat.Pdf417 => Pdf417Code.Encode("HELLO").Modules,
            SymbolFormat.MicroQrCode => MicroQrCodeEncoder.EncodeText("HELLO").Modules,
            SymbolFormat.QrCode => QrCodeEncoder.EncodeText("HELLO").Modules,
            _ => null
        };
        var png = modules is null
            ? Barcode.Render(SymbolFormat.Code128, "HELLO", OutputFormat.Png).Data
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
        Assert.True(SymbolDecoder.TryDecode(modules, out var matrix, SymbolFormat.DataMatrix));
        Check(matrix, gs1);
        Assert.Null(matrix.SearchRegion);
        Assert.False(matrix.HasRawBytes);
        var pixels = MatrixPngRenderer.RenderPixels(modules, new MatrixPngRenderOptions { ModuleSize = 5 }, out var w, out var h, out var stride);
        var scan = SymbolScanner.Scan(new ImageFrame(pixels, w, h, stride, PixelFormat.Rgba32), new ScanOptions {
            Formats = new[] { SymbolFormat.DataMatrix }, MaxSymbols = 1, TimeoutMilliseconds = TestBudget.Adjust(5000)
        });
        Assert.True(scan.IsSuccess, scan.Failure);
        Check(Assert.Single(scan.Symbols), gs1);
    }

    [Fact]
    public void DataMatrixControlMetadata_IsPreserved() {
        var modules = DataMatrixEncoder.Encode("Ã©", new DataMatrixEncodingOptions { EciAssignmentNumber = 26, StructuredAppend = new DataMatrixStructuredAppend(1, 2, 123) });
        Assert.True(SymbolDecoder.TryDecode(modules, out var result, SymbolFormat.DataMatrix));
        Assert.Equal("Ã©", result.Text);
        var metadata = Assert.IsType<DataMatrixSymbolMetadata>(result.Metadata);
        Assert.Equal(26, Assert.Single(metadata.EciAssignments));
        Assert.Equal(1, metadata.StructuredAppend!.Value.Index);
    }

    private static void Check(DetectedSymbol result, bool gs1) {
        Assert.Equal(SymbolFormat.DataMatrix, result.Format);
        Assert.Equal(gs1, Assert.IsType<DataMatrixSymbolMetadata>(result.Metadata).IsGs1);
        Assert.Equal("0109506000134352", result.Text);
    }
}
