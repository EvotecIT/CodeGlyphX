using System;
using System.IO;
using System.Linq;
using CodeGlyphX.Printing;
using CodeGlyphX.Rendering;
using CodeGlyphX.Internal.Fonts;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class BarcodeFontCollectionTests {
    [Theory]
    [InlineData("TestMixedCffFirst.ttc")]
    [InlineData("TestMixedTrueTypeFirst.ttc")]
    public void MixedOutlineCollections_RetainSelectedMetricsLigaturesAndOwnedOutlines(string fixture) {
        var data = Bytes(fixture);
        var faces = Enumerable.Range(0, 3).Select(index => BarcodeFont.FromBytes(data, index)).ToArray();
        var automatic = BarcodeFont.FromBytes(data);
        Array.Clear(data, 0, data.Length);
        Assert.Equal(0, automatic.CollectionIndex);
        Assert.Equal(6, automatic.Shape("A", 10).AdvanceWidth, 6);
        Assert.Equal(5, automatic.Shape("fi", 10).AdvanceWidth, 6);
        // Revisit faces in alternating order to expose shared-byte/face cache contamination.
        foreach (var index in new[] { 2, 0, 1, 0, 2 }) {
            var font = faces[index];
            Assert.Equal(index, font.CollectionIndex);
            Assert.Equal(6 + index, font.Shape("A", 10).AdvanceWidth, 6);
            var ligature = font.Shape("fi", 10);
            Assert.Equal(index == 1 ? 6.5 : 5 + index, ligature.AdvanceWidth, 6);
            Assert.Equal(index == 1 ? 5.8 : 4.4, ligature.InkBounds.Width, 2);
            var shape = font.Shape("B", 10);
            Assert.Equal(6.5, shape.AdvanceWidth, 6);
            Assert.Equal(.2, shape.InkBounds.X, 6);
            Assert.Equal(5.8, shape.InkBounds.Width, 2);
            Assert.Equal(7, shape.InkBounds.Height, 6);
            Assert.True(Assert.Single(shape.Contours).Count > 4);
        }
        Assert.Throws<ArgumentException>(() => BarcodeFont.FromBytes(Bytes(fixture), 3));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void CffCollectionFaces_AreUsableInAllPhysicalPrintExports(int index) {
        var font = BarcodeFont.FromBytes(Bytes("TestMixedCffFirst.ttc"), index);
        var layout = BarcodePrint.Layout(BarcodeEncoder.Encode(BarcodeType.Code128, "AB"), new BarcodePrintOptions {
            Font = font, HumanReadableText = "AfiB", Caption = "B"
        });
        Assert.Equal(new[] { "AfiB", "B" }, layout.Text.Select(line => line.Text));
        Assert.All(layout.Text, line => Assert.NotEmpty(line.Shape.Contours));
        foreach (var format in new[] { OutputFormat.Png, OutputFormat.Svg, OutputFormat.Pdf })
            Assert.NotEmpty(layout.Render(format).ToArray());
    }

    internal static byte[] Bytes(string name) {
        using var input = typeof(BarcodeFontCollectionTests).Assembly.GetManifestResourceStream("CodeGlyphX.Tests.Fixtures.Fonts." + name);
        Assert.NotNull(input);
        using var output = new MemoryStream();
        input!.CopyTo(output);
        return output.ToArray();
    }

    [Fact]
    public void CffCollectionFaceIdentityAndUncachedShaping_RetainTheRequestedFace() {
        var data = Bytes("TestMixedCffFirst.ttc");
        var first = ManagedOpenTypeCffFont.TryLoad(data, 0, null, out _);
        var second = ManagedOpenTypeCffFont.TryLoad(data, 1, null, out _);
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(first!.Fingerprint, second!.Fingerprint);
        foreach (var index in new[] { 1, 0, 1 }) {
            var request = new ManagedTextShapingRequest("fi", "Collection", data, true, 1000,
                ManagedTextDirection.Auto, null, default, index, null, cloneFontData: false,
                applyDefaultLatinLigatures: true);
            var shape = ManagedTextShapingProvider.Instance.ShapeText(request);
            Assert.NotNull(shape);
            Assert.Equal(index == 0 ? 8 : 5, Assert.Single(shape!.Glyphs).GlyphId);
            Assert.Equal(index == 0 ? 5 : 6.5, (index == 0 ? first : second).MeasureShapedText("fi", shape, 10), 6);
        }
    }
}
