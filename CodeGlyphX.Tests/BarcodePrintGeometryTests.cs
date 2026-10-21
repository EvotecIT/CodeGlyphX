using System;
using System.Linq;
using CodeGlyphX.Payloads;
using CodeGlyphX.Printing;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class BarcodePrintGeometryTests {
    [Fact]
    public void SpecializedLayouts_RetainMaxiCodeSwissAndUltracodeGeometry() {
        var maxi = BarcodePrint.Layout(MaxiCodeCode.Encode("MAXI PRINT"));
        Assert.Equal(.88, maxi.XDimensionMm, 7);
        Assert.Equal(3, maxi.Paths.Count(p => p.Role == "finder"));
        Assert.Contains(maxi.Paths, p => p.Role == "hexagons" && p.Contours[0].Count == 6);
        Assert.Equal("MAXI PRINT", Scan(maxi, SymbolFormat.MaxiCode).Text);

        Assert.True(SwissQrCodePayload.TryParse(SwissQrCapacityTests.BoundaryText(200, 'e', "\n"), out var payload));
        var swiss = SwissQrCode.Encode(payload);
        var payment = BarcodePrint.Layout(swiss);
        Assert.Equal(46, payment.XDimensionMm * swiss.Modules.Width, 7);
        Assert.Equal(7, payment.Paths.Where(p => p.Role == "swiss-cross").Max(p => p.Bounds.Width), 7);
        var png = payment.Render(OutputFormat.Png).ToArray();
        var bytes = ImageReader.DecodeRgba32(png, out var width, out var height);
        Assert.True(SwissQrCode.TryDecode(bytes, width, height, width * 4, PixelFormat.Rgba32, out var decoded));
        Assert.Equal(payload.ToString(), decoded.ToString());

        var ultra = BarcodePrint.Layout(UltracodeEncoder.EncodeText("COLOUR PRINT"));
        Assert.Equal("COLOUR PRINT", Scan(ultra, SymbolFormat.Ultracode).Text);
        Assert.Contains(ultra.Paths, p => p.Role == "colour-cells");
        Assert.NotEmpty(ultra.Render(OutputFormat.Pdf).ToArray());
        Assert.Throws<ArgumentException>(() => BarcodePrint.Layout(UltracodeEncoder.EncodeText("COLOUR"), new BarcodePrintOptions {
            Foreground = BarcodePaint.Solid(new Rgba32(50, 60, 70))
        }));
    }

    [Theory]
    [InlineData(Gs1CompositeCarrier.Gs1_128)]
    [InlineData(Gs1CompositeCarrier.Ean13)]
    [InlineData(Gs1CompositeCarrier.Ean8)]
    [InlineData(Gs1CompositeCarrier.UpcA)]
    [InlineData(Gs1CompositeCarrier.UpcE)]
    [InlineData(Gs1CompositeCarrier.DataBarOmnidirectional)]
    [InlineData(Gs1CompositeCarrier.DataBarTruncated)]
    [InlineData(Gs1CompositeCarrier.DataBarStacked)]
    [InlineData(Gs1CompositeCarrier.DataBarStackedOmnidirectional)]
    [InlineData(Gs1CompositeCarrier.DataBarLimited)]
    [InlineData(Gs1CompositeCarrier.DataBarExpanded)]
    [InlineData(Gs1CompositeCarrier.DataBarExpandedStacked)]
    public void CompositeLayout_PreservesLinkedImageReadbackAndCarrierHeight(Gs1CompositeCarrier carrier) {
        var primary = carrier switch {
            Gs1CompositeCarrier.Ean13 => "9520123456788",
            Gs1CompositeCarrier.Ean8 => "95200002",
            Gs1CompositeCarrier.UpcA => "012345678905",
            Gs1CompositeCarrier.UpcE => "01234558",
            _ => "(01)09521234543213"
        };
        var symbol = Gs1CompositeEncoder.Encode(primary, "(11)990102", new Gs1CompositeEncodingOptions { Carrier = carrier });
        var layout = BarcodePrint.Layout(symbol, new BarcodePrintOptions { IncludeHumanReadable = false, Dpi = 300 });
        var detected = Scan(layout, SymbolFormat.Gs1Composite, carrier);
        var metadata = Assert.IsType<Gs1CompositeSymbolMetadata>(detected.Metadata);
        Assert.False(metadata.IsStandalone);
        Assert.Equal(symbol.LinearText, metadata.LinearText);
        Assert.Equal(symbol.CompositeText, metadata.CompositeText);
        Assert.Equal(carrier, metadata.Carrier);
        Assert.NotNull(metadata.ComponentGeometry);
        Assert.NotNull(metadata.LinearGeometry);
        var expectedMinimum = carrier == Gs1CompositeCarrier.Gs1_128 ? 31.75
            : carrier is Gs1CompositeCarrier.DataBarOmnidirectional or Gs1CompositeCarrier.DataBarStackedOmnidirectional ? 33 * layout.XDimensionMm
            : 5 * layout.XDimensionMm;
        Assert.True(layout.SymbolBounds.Height > expectedMinimum);
    }

    [Theory]
    [InlineData(Gs1CompositeCarrier.Ean13, false)]
    [InlineData(Gs1CompositeCarrier.Ean13, true)]
    [InlineData(Gs1CompositeCarrier.Ean8, false)]
    [InlineData(Gs1CompositeCarrier.Ean8, true)]
    [InlineData(Gs1CompositeCarrier.UpcA, false)]
    [InlineData(Gs1CompositeCarrier.UpcA, true)]
    [InlineData(Gs1CompositeCarrier.UpcE, false)]
    [InlineData(Gs1CompositeCarrier.UpcE, true)]
    public void ThreePartRetailComposite_RetainsSupplementsGuardsAndReadableText(Gs1CompositeCarrier carrier, bool readable) {
        var primary = carrier switch {
            Gs1CompositeCarrier.Ean13 => "9520123456788+12",
            Gs1CompositeCarrier.Ean8 => "95200002+12345",
            Gs1CompositeCarrier.UpcA => "012345678905+12",
            _ => "01234558+12345"
        };
        var symbol = Gs1CompositeEncoder.Encode(primary, "(11)990102", new Gs1CompositeEncodingOptions { Carrier = carrier });
        var layout = BarcodePrint.Layout(symbol, new BarcodePrintOptions {
            IncludeHumanReadable = readable, Font = BarcodePrintLayoutTests.Font(), Caption = readable ? "COMPOSITE LABEL" : null
        });
        var hit = Scan(layout, SymbolFormat.Gs1Composite, carrier);
        var metadata = Assert.IsType<Gs1CompositeSymbolMetadata>(hit.Metadata);
        Assert.False(metadata.IsStandalone);
        Assert.Equal(primary, metadata.LinearText);
        Assert.Equal("11990102", metadata.CompositeText);
        if (readable) {
            Assert.Contains(layout.Text, line => line.Text == primary.Substring(primary.IndexOf('+') + 1));
            Assert.Contains(layout.Text, line => line.Text == "COMPOSITE LABEL");
            var bounds = metadata.LinearGeometry!.Bounds;
            var pitch = layout.Dpi / 25.4;
            var offsetY = -Math.Min(0, layout.Bounds.Y);
            var bars = layout.Paths.Where(path => path.Role == "bars").ToArray();
            Assert.InRange(Math.Abs(bounds.X + bounds.Width - bars.Max(path => path.Bounds.Right) * pitch), 0, 2);
            Assert.InRange(Math.Abs(bounds.Y + bounds.Height - (bars.Max(path => path.Bounds.Bottom) + offsetY) * pitch), 0, 2);
        }
    }

    [Theory]
    [InlineData(SymbolFormat.Postnet, "12345")]
    [InlineData(SymbolFormat.Planet, "12345678901")]
    [InlineData(SymbolFormat.UspsIntelligentMail, "01234567094987654321-01234567891")]
    [InlineData(SymbolFormat.RoyalMail4State, "LE28HS9Z")]
    [InlineData(SymbolFormat.AustraliaPost, "12345678")]
    [InlineData(SymbolFormat.JapanPost, "15400233-16-4")]
    [InlineData(SymbolFormat.KixCode, "1234AB56")]
    [InlineData(SymbolFormat.Daft, "DAFTDAFT")]
    [InlineData(SymbolFormat.PharmacodeTwoTrack, "12345")]
    public void PostalLayout_SeparatesBarWidthPitchAndHeight_AndReadsBack(SymbolFormat format, string value) {
        var symbol = MatrixBarcodeEncoder.EncodeSymbol(format, value);
        var expected = SymbolDecoder.Decode(symbol.Modules, format).Text;
        var layout = BarcodePrint.Layout(symbol);
        Assert.False(layout.HasErrors, string.Join("; ", layout.Findings.Where(f => f.IsError).Select(f => f.Message)));
        var bars = layout.Paths.Where(p => p.Role == "bars").ToArray();
        Assert.All(bars, bar => Assert.Equal(layout.XDimensionMm, bar.Bounds.Width, 7));
        Assert.True(bars[1].Bounds.X - bars[0].Bounds.X > bars[0].Bounds.Width);
        Assert.True(layout.SymbolBounds.Height >= bars.Max(p => p.Bounds.Height));
        Assert.Equal(expected, Scan(layout, format).Text);
    }

    [Fact]
    public void DataBarStackedAndPdf417_PreservePhysicalRowRatios() {
        var stacked = BarcodePrint.Layout(MatrixBarcodeEncoder.EncodeSymbol(SymbolFormat.Gs1DataBarStacked, "952123454321"));
        Assert.Equal((5 + 1 + 7) * stacked.XDimensionMm, stacked.SymbolBounds.Height, 7);
        Assert.Equal("0952123454321", Scan(stacked, SymbolFormat.Gs1DataBarStacked).Text);
        var pdf = BarcodePrint.Layout(Pdf417.Pdf417Encoder.EncodeSymbol("ROW RATIO"));
        Assert.Equal("ROW RATIO", Scan(pdf, SymbolFormat.Pdf417).Text);
        var dot = BarcodePrint.Layout(DotCodeCode.Encode("ROUND DOTS"));
        Assert.Contains(dot.Paths, p => p.Role == "dots");
        Assert.Equal("ROUND DOTS", Scan(dot, SymbolFormat.DotCode).Text);
    }

    [Fact]
    public void TallerStackedBars_KeepSeparatorThicknessAndReadback_AndRejectTruncation() {
        var symbol = MatrixBarcodeEncoder.EncodeSymbol(SymbolFormat.Gs1DataBarStacked, "952123454321");
        var normal = BarcodePrint.Layout(symbol);
        var taller = BarcodePrint.Layout(symbol, new BarcodePrintOptions { BarHeightMm = 20 });
        Assert.Equal(20, taller.SymbolBounds.Height, 7);
        Assert.Contains(taller.Paths, p => p.Role == "bars" && Math.Abs(p.Bounds.Height - taller.XDimensionMm) < 1e-7);
        Assert.Equal("0952123454321", Scan(taller, SymbolFormat.Gs1DataBarStacked).Text);
        var shortBars = BarcodePrint.Layout(symbol, new BarcodePrintOptions { BarHeightMm = .1 });
        Assert.Contains(shortBars.Findings, f => f.Code == "StackedRowHeight" && f.IsError);
        Assert.Equal(normal.SymbolBounds.Height, shortBars.SymbolBounds.Height, 7);
    }

    private static DetectedSymbol Scan(BarcodePrintLayout layout, SymbolFormat format, Gs1CompositeCarrier? carrier = null) {
        var result = SymbolScanner.Scan(layout.Render(OutputFormat.Png).ToArray(), new ScanOptions {
            Formats = new[] { format }, CompositeCarrier = carrier, TimeoutMilliseconds = 5000,
            EnableTileScan = false, MaxSymbols = 1, Image = new ImageDecodeOptions { MaxDimension = 0 }
        });
        Assert.True(result.Symbols.Count == 1, $"{format}/{carrier}: {result.Status}");
        return result.Symbols[0];
    }
}
