using System;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml.Linq;
using CodeGlyphX.Code49;
using CodeGlyphX.Payloads;
using CodeGlyphX.PosiCode;
using CodeGlyphX.Postal;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class BarcodeBoundaryReviewTests {
    [Fact]
    public void Code49_CheckRowsRetainBoundaryPayloadsAndRejectInsufficientRows() {
        foreach (var fixture in Fixtures().Where(item => item.Bcid == "code49")) {
            var text = fixture.Text;
            Assert.True(Code49Decoder.TryDecode(Matrix(fixture), out var independent));
            Assert.Equal(text, independent.Text);
            var symbol = Code49Encoder.Encode(text);
            Assert.True(Code49Decoder.TryDecode(symbol.Modules, out var decoded));
            Assert.Equal(text, decoded.Text);
            Assert.Equal(independent.Rows, decoded.Rows);
            Assert.Equal(text.Length <= 37 ? 6 : text.Length <= 42 ? 7 : 8, decoded.Rows);
            if (text.Length > 42) Assert.Throws<ArgumentException>(() => Code49Encoder.Encode(text, rows: 7));
        }
        Assert.Throws<ArgumentException>(() => Code49Encoder.Encode(new string('A', 50)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(26)]
    [InlineData(999)]
    public void SwissQr_ExplicitEciPreservesTextAndControlsPaymentClassification(int assignment) {
        var text = SwissText();
        var bytes = Encoding.UTF8.GetBytes(text);
        var symbol = QrCodeEncoder.EncodeBytes(bytes, eciAssignmentNumber: assignment < 0 ? null : assignment);
        Assert.True(QrDecoder.TryDecode(symbol.Modules, out var qr));
        var compatible = assignment < 0 || assignment == 26;
        Assert.Equal(compatible ? text : Encoding.GetEncoding(28591).GetString(bytes), qr.Text);
        Assert.Equal(bytes, qr.Bytes);
        Assert.Equal(compatible ? QrPayloadType.SwissQr : QrPayloadType.Text, qr.Parsed.Type);
        Assert.Equal(compatible, SwissQrCode.TryDecode(symbol.Modules, out var payment));
        if (compatible) Assert.Equal(text, payment.ToString());
        else Assert.Null(payment);
        Assert.Equal(compatible ? SymbolPayloadProfile.SwissQr : SymbolPayloadProfile.None,
            SymbolDecoder.Decode(symbol.Modules, SymbolFormat.QrCode).PayloadProfile);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(25)]
    public void SwissQr_CrossIsSevenMillimetresWithinTheCoreExcludingQuietSpace(int version) {
        Assert.True(SwissQrCodePayload.TryParse(SwissText(), out var payload));
        var qr = QrCodeEncoder.EncodeBytes(Encoding.UTF8.GetBytes(payload.ToString()), minVersion: version, maxVersion: version);
        var symbol = new SwissQrSymbol(qr, payload);
        var svg = XDocument.Parse(symbol.Render(OutputFormat.Svg, moduleSize: 46).GetText());
        var coverage = new SvgCoverageTestReader(svg);
        // At 46 px/module the 46 mm QR core makes a 7 mm logo exactly N * 7 pixels wide.
        var side = qr.Size * 7;
        var offset = (coverage.Width - side) / 2;
        foreach (var ux in new[] { 1, 5, 7, 10, 17, 19, 33, 35, 41, 48, 50, 64, 66, 76, 78, 81 })
            foreach (var uy in new[] { 1, 5, 7, 10, 17, 19, 33, 35, 41, 48, 50, 64, 66, 76, 78, 81 }) {
                var x = ux * side / 83;
                var y = uy * side / 83;
                var unitX = x * 83 / side;
                var unitY = y * 83 / side;
                var square = unitX >= 6 && unitX < 77 && unitY >= 6 && unitY < 77;
                var cross = (unitX >= 34 && unitX < 49 && unitY >= 18 && unitY < 65)
                    || (unitX >= 18 && unitX < 65 && unitY >= 34 && unitY < 49);
                Assert.Equal(square && !cross ? Rgba32.Black : Rgba32.White, coverage.Sample(offset + x, offset + y));
            }
        Assert.Equal((qr.Modules.Width + 8) * 46, double.Parse(svg.Root!.Attribute("width")!.Value, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void LinkedPosiCode_ShortIndependentRowsRemainImageScannable() {
        foreach (var fixture in Fixtures().Where(item => item.Bcid == "posicode")) {
            var text = fixture.Text;
            var variant = (PosiCodeVariant)Enum.Parse(typeof(PosiCodeVariant), fixture.Variant, true);
            var reference = Matrix(fixture);
            var encoded = PosiCodeEncoder.EncodeStacked(text, 2, variant, rowHeight: 6);
            Assert.Equal(reference.Width, encoded.Modules.Width);
            Assert.True(PosiCodeDecoder.TryDecode(reference, out var decoded));
            Assert.Equal(text, decoded.Text);
            var png = MatrixPngRenderer.Render(reference, new MatrixPngRenderOptions { ModuleSize = 4, QuietZone = 8 });
            var result = SymbolScanner.Scan(png, Options(SymbolFormat.PosiCode));
            Assert.Equal(text, Assert.Single(result.Symbols).Text);
        }
    }

    [Fact]
    public void Planet_FourteenDigitReferenceWorksThroughEncoderModulesAndImages() {
        var fixture = Fixtures().Single(item => item.Bcid == "planet");
        var reference = Matrix(fixture);
        var expected = fixture.Expected;
        Assert.True(PlanetDecoder.TryDecode(reference, out var text));
        Assert.Equal(expected, text);
        Assert.False(PostnetDecoder.TryDecode(reference, out _));
        foreach (var input in new[] { fixture.Text, expected }) {
            var encoded = PlanetEncoder.Encode(input);
            Assert.Equal(reference.Width, encoded.Width);
            Assert.Equal(reference.Height, encoded.Height);
            for (var y = 0; y < reference.Height; y++) for (var x = 0; x < reference.Width; x++) Assert.Equal(reference[x, y], encoded[x, y]);
        }
        var png = MatrixPngRenderer.Render(reference, new MatrixPngRenderOptions { ModuleSize = 8, QuietZone = 5 });
        Assert.Equal(expected, Assert.Single(SymbolScanner.Scan(png, Options(SymbolFormat.Planet)).Symbols).Text);
        Assert.Throws<InvalidOperationException>(() => PostnetEncoder.Encode(expected));
        Assert.Throws<InvalidOperationException>(() => PlanetEncoder.Encode(expected.Substring(0, 13) + "3"));
    }

    private static ScanOptions Options(SymbolFormat format) => new() { Formats = new[] { format }, MaxSymbols = 1, EnableTileScan = false, TimeoutMilliseconds = TestBudget.Adjust(10000) };
    private static Fixture[] Fixtures(string name = "bwipp-boundaries.json") {
        using var stream = typeof(BarcodeBoundaryReviewTests).Assembly.GetManifestResourceStream("CodeGlyphX.Tests.Fixtures.Conformance." + name);
        return ((FixtureSet)new DataContractJsonSerializer(typeof(FixtureSet)).ReadObject(stream!)!).Items;
    }
    private static string SwissText() => Fixtures("bwipp-current.json").First(item => item.Bcid == "swissqrcode").Text;
    private static BitMatrix Matrix(Fixture fixture) {
        var matrix = new BitMatrix(fixture.Width, fixture.Height);
        var bits = fixture.Modules;
        for (var y = 0; y < matrix.Height; y++) for (var x = 0; x < matrix.Width; x++) matrix[x, y] = bits[y * matrix.Width + x] == '1';
        return matrix;
    }

    [DataContract]
    private sealed class FixtureSet {
        [DataMember(Name = "fixtures")] public Fixture[] Items { get; set; } = null!;
    }

    [DataContract]
    private sealed class Fixture {
        [DataMember(Name = "bcid")] public string Bcid { get; set; } = null!;
        [DataMember(Name = "text")] public string Text { get; set; } = null!;
        [DataMember(Name = "variant")] public string Variant { get; set; } = null!;
        [DataMember(Name = "expected")] public string Expected { get; set; } = null!;
        [DataMember(Name = "width")] public int Width { get; set; }
        [DataMember(Name = "height")] public int Height { get; set; }
        [DataMember(Name = "modules")] public string Modules { get; set; } = null!;
    }
}
