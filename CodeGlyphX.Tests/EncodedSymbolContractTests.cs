using System;
using System.IO;
using CodeGlyphX.Aztec;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class EncodedSymbolContractTests {
    [Fact]
    public void FrozenModulesRejectMutationAndCloneRemainsEditable() {
        var source = new BitMatrix(11, 11);
        source[0, 0] = true;
        var symbol = new MicroQrCode(1, QrErrorCorrectionLevel.L, 0, source);
        source.Clear();

        Assert.True(symbol.Modules[0, 0]);
        Assert.True(symbol.Modules.IsReadOnly);
        Assert.Throws<InvalidOperationException>(() => symbol.Modules[0, 0] = false);
        Assert.Throws<InvalidOperationException>(() => symbol.Modules.Set(0, 0, false));
        Assert.Throws<InvalidOperationException>(() => symbol.Modules.Clear());
        var edited = symbol.Modules.Clone();
        Assert.False(edited.IsReadOnly);
        edited.Clear();
        Assert.False(edited[0, 0]);
        Assert.True(symbol.Modules[0, 0]);
        Assert.Same(edited, edited.Freeze());
        Assert.Throws<InvalidOperationException>(() => edited.Clear());
    }

    [Fact]
    public void DataMatrixResultRetainsSelectedCapacityAndRendersFromStableModules() {
        var symbol = DataMatrixCode.Encode("ABC", new DataMatrixEncodingOptions { Rows = 10, Columns = 10 });

        Assert.Equal(SymbolFormat.DataMatrix, symbol.Format);
        Assert.Equal(10, symbol.Rows);
        Assert.Equal(10, symbol.Columns);
        Assert.False(symbol.IsDmre);
        Assert.Equal(3, symbol.DataCodewordCapacity);
        Assert.Equal(5, symbol.ErrorCorrectionCodewordCount);
        Assert.True(symbol.Modules.IsReadOnly);
        using var output = new MemoryStream();
        symbol.Save(output, OutputFormat.Png, new MatrixOptions { ModuleSize = 4 });
        Assert.True(DataMatrixCode.TryDecodeImage(output.ToArray(), out var text));
        Assert.Equal("ABC", text);
    }

    [Fact]
    public void Pdf417ResultRetainsActualAutoSelectedErrorCorrection() {
        var options = new Pdf417EncodeOptions {
            MinColumns = 3,
            MaxColumns = 3,
            MinRows = 3,
            MaxRows = 3,
            ErrorCorrectionLevel = -1
        };
        var symbol = Pdf417Code.Encode("ABC", options);

        Assert.Equal(SymbolFormat.Pdf417, symbol.Format);
        Assert.Equal(3, symbol.Rows);
        Assert.Equal(3, symbol.Columns);
        Assert.InRange(symbol.ErrorCorrectionLevel, 0, 8);
        Assert.NotEqual(options.ErrorCorrectionLevel, symbol.ErrorCorrectionLevel);
        Assert.Equal(120, symbol.Width);
        Assert.True(symbol.Modules.IsReadOnly);
        Assert.True(Pdf417Decoder.TryDecode(symbol.Modules, out var decoded));
        Assert.Equal("ABC", decoded);
    }

    [Fact]
    public void AztecResultRetainsCompactLayerMetadata() {
        var symbol = AztecCode.Encode("ABC", new AztecEncodeOptions { Layers = 2, Compact = true });

        Assert.Equal(SymbolFormat.Aztec, symbol.Format);
        Assert.True(symbol.Compact);
        Assert.Equal(2, symbol.Layers);
        Assert.Equal(19, symbol.Size);
        Assert.True(symbol.DataCodewordCount > 0);
        Assert.True(symbol.Modules.IsReadOnly);
    }

    [Fact]
    public void AztecAutomaticSelectionHonorsExplicitLayoutForTextAndBytes() {
        var options = new AztecEncodeOptions { Compact = false };
        Assert.False(AztecCode.Encode("ABC", options).Compact);
        Assert.False(AztecCode.Encode(new byte[] { 65, 66, 67 }, options).Compact);
        Assert.Throws<ArgumentOutOfRangeException>(() => AztecCode.Encode("ABC", new AztecEncodeOptions { Layers = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => AztecCode.Encode(new byte[] { 65 }, new AztecEncodeOptions { ErrorCorrectionPercent = -1 }));
    }

    [Theory]
    [InlineData(SymbolFormat.MicroQrCode)]
    [InlineData(SymbolFormat.RmQrCode)]
    [InlineData(SymbolFormat.DotCode)]
    [InlineData(SymbolFormat.HanXin)]
    [InlineData(SymbolFormat.DataMatrix)]
    [InlineData(SymbolFormat.Pdf417)]
    [InlineData(SymbolFormat.Aztec)]
    public void GenericMatrixEncodingKeepsFamilyIdentityAndReadOnlyModules(SymbolFormat format) {
        var symbol = MatrixBarcode.Encode(format, "ABC");

        Assert.Equal(format, symbol.Format);
        Assert.True(symbol.Modules.IsReadOnly);
        if (format == SymbolFormat.DataMatrix) Assert.IsType<DataMatrixSymbol>(symbol);
        if (format == SymbolFormat.Pdf417) Assert.IsType<Pdf417Symbol>(symbol);
        if (format == SymbolFormat.Aztec) Assert.IsType<AztecSymbol>(symbol);
        if (format == SymbolFormat.DotCode) Assert.IsType<DotCodeSymbol>(symbol);
        if (format == SymbolFormat.HanXin) Assert.IsType<HanXinSymbol>(symbol);
    }

    [Fact]
    public void MaxiCodeKeepsSpecializedGeometryOutOfOrthogonalRenderer() {
        var symbol = MaxiCodeCode.Encode("ABC");

        Assert.Equal(SymbolFormat.MaxiCode, symbol.Format);
        Assert.Equal(30, symbol.Modules.Width);
        Assert.Equal(33, symbol.Modules.Height);
        Assert.True(symbol.Modules.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => MatrixBarcode.Encode(SymbolFormat.MaxiCode, "ABC"));
    }

    [Fact]
    public void BarcodeFacadeRetainsIdentityAndBuilderOwnsLayoutOptions() {
        var options = new BarcodeOptions { ModuleSize = 2 };
        var builder = Barcode.Create(SymbolFormat.Code128, "ABC", options).WithModuleSize(4);
        options.ModuleSize = 9;
        var symbol = builder.Encode();

        Assert.Equal(4, builder.Options.ModuleSize);
        Assert.Equal(SymbolFormat.Code128, symbol.Format);
        using var output = new MemoryStream();
        symbol.Save(output, OutputFormat.Png, builder.Options);
        Assert.True(Barcode.TryDecodeImage(output.ToArray(), SymbolFormat.Code128, out var decoded));
        Assert.Equal("ABC", decoded.Text);
    }

    [Fact]
    public void MatrixBuildersOwnIncomingPayloadAndOptions() {
        var data = new byte[] { 65, 66, 67 };
        var renderOptions = new MatrixOptions { ModuleSize = 4 };
        var encodeOptions = new Pdf417EncodeOptions { ErrorCorrectionLevel = 1 };
        var dataMatrix = DataMatrixCode.Create(data, options: renderOptions);
        var pdf417 = Pdf417Code.Create(data, encodeOptions, renderOptions);
        data[0] = 90;
        renderOptions.ModuleSize = 9;
        encodeOptions.ErrorCorrectionLevel = 7;

        Assert.True(DataMatrixDecoder.TryDecode(dataMatrix.Encode().Modules, out var dmText));
        Assert.Equal("ABC", dmText);
        var pdfSymbol = pdf417.Encode();
        Assert.Equal(1, pdfSymbol.ErrorCorrectionLevel);
        Assert.True(Pdf417Decoder.TryDecode(pdfSymbol.Modules, out var pdfText));
        Assert.Equal("ABC", pdfText);
    }
}
