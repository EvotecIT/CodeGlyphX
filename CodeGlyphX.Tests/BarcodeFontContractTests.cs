using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using CodeGlyphX.Printing;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class BarcodeFontContractTests {
    [Fact]
    public void SuppliedFont_PreservesCaseAndScalesMetricsWithOutlines() {
        var font = LoadFont();
        var mixed = font.Shape("Ab09", 4);
        var upper = font.Shape("AB09", 4);
        var larger = font.Shape("Ab09", 8);
        Assert.Equal("Ab09", mixed.Text);
        Assert.Empty(mixed.MissingGlyphs);
        Assert.NotEmpty(mixed.Contours);
        Assert.NotEqual(mixed.AdvanceWidth, upper.AdvanceWidth);
        Assert.Equal(mixed.AdvanceWidth * 2, larger.AdvanceWidth, 6);
        Assert.Equal(mixed.InkBounds.Width * 2, larger.InkBounds.Width, 6);
        Assert.Equal(mixed.InkBounds.Height * 2, larger.InkBounds.Height, 6);
        Assert.Equal(mixed.BaselineOffset * 2, larger.BaselineOffset, 6);
        Assert.InRange(mixed.AdvanceWidth, 4, 16);
        Assert.InRange(mixed.BaselineOffset, 1, mixed.LineHeight);
    }

    [Fact]
    public void SpacesHaveAdvanceWithoutInventedInk_AndMissingGlyphsAreExplicit() {
        var font = LoadFont();
        var spaces = font.Shape("  ", 4);
        Assert.True(spaces.AdvanceWidth > 0);
        Assert.True(spaces.InkBounds.IsEmpty);
        Assert.Empty(spaces.Contours);
        Assert.Throws<ArgumentException>(() => font.Shape("A世界", 4));
        var replaced = font.Shape("A世界", 4, strict: false);
        Assert.Equal(new[] { 0x4e16, 0x754c }, replaced.MissingGlyphs);
        Assert.Equal(font.Shape("A??", 4).AdvanceWidth, replaced.AdvanceWidth, 6);
        Assert.Equal("A世界", replaced.Text);
    }

    [Fact]
    public void ShapeOwnsContoursAndIncludesOverhangBeyondTheLineBox() {
        var points = new List<BarcodePoint> { new(-1, -2), new(3, -2), new(3, 6), new(-1, 6) };
        var contours = new List<IEnumerable<BarcodePoint>> { points };
        var shape = new BarcodeTextShape("f", 2, 5, 4, contours);
        points.Clear(); contours.Clear();
        Assert.Equal(4, Assert.Single(shape.Contours).Count);
        Assert.Equal(-1, shape.InkBounds.X);
        Assert.Equal(-2, shape.InkBounds.Y);
        Assert.Equal(4, shape.InkBounds.Width);
        Assert.Equal(8, shape.InkBounds.Height);
        Assert.Equal(2, shape.LogicalBounds.Width);
        var italic = LoadFont(italic: true).Shape("fj", 4);
        Assert.NotEqual(italic.LogicalBounds.Width, italic.InkBounds.Width);
    }

    [Fact]
    public void InvalidFontOrShapeInput_IsRejectedBeforeRendering() {
        Assert.Throws<ArgumentException>(() => BarcodeFont.FromBytes(new byte[20]));
        var font = LoadFont();
        Assert.Throws<ArgumentException>(() => font.Shape("two\nlines", 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => font.Shape("text", double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => font.Shape("text", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BarcodeTextShape("A", -1, 1, 1, Array.Empty<BarcodePoint[]>()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BarcodeBounds(0, 0, double.PositiveInfinity, 1));
    }

    [Theory]
    [InlineData("TestOutlines.ttf")]
    [InlineData("TestOutlines.otf")]
    [InlineData("TestOutlinesCff2.otf")]
    public void IndependentTrueTypeAndCffCurves_HaveMatchingMetricsAndRealInkBounds(string fixture) {
        // fontTools BoundsPen independently records B's design bounds as (20,0,600,700).
        var font = BarcodeFont.FromBytes(FontBytes(fixture));
        var shape = font.Shape("B", 10);
        Assert.Equal(6.5, shape.AdvanceWidth, 6);
        Assert.Equal(8, shape.BaselineOffset, 6);
        Assert.Equal(10, shape.LineHeight, 6);
        Assert.Equal(0.2, shape.InkBounds.X, 6);
        Assert.Equal(1, shape.InkBounds.Y, 6);
        Assert.Equal(5.8, shape.InkBounds.Width, 2);
        Assert.Equal(7, shape.InkBounds.Height, 6);
        Assert.True(Assert.Single(shape.Contours).Count > 4);
        Assert.Equal(5, font.Shape("fi", 10).AdvanceWidth, 6); // GSUB ligature, nominal f+i would be 6.
        Assert.Equal(9, font.Shape("\U0001f642", 10).AdvanceWidth, 6);
        var accent = font.Shape("é", 10);
        Assert.True(accent.InkBounds.Y < 0);
        Assert.Equal(3, accent.Contours.Count);
    }

    [Fact]
    public void CollectionSelectionAndOwnedBytes_KeepTheSelectedFaceThroughShaping() {
        var data = FontBytes("TestOutlines.ttc");
        var regular = BarcodeFont.FromBytes(data, 0);
        var wide = BarcodeFont.FromBytes(data, 1);
        Array.Clear(data, 0, data.Length);
        Assert.Equal(6, regular.Shape("A", 10).AdvanceWidth, 6);
        Assert.Equal(8, wide.Shape("A", 10).AdvanceWidth, 6);
        Assert.Equal(5, regular.Shape("fi", 10).AdvanceWidth, 6);
        Assert.Equal(5, wide.Shape("fi", 10).AdvanceWidth, 6);
        Assert.Equal(5, BarcodeFont.FromBytes(FontBytes("TestOutlines.ttc")).Shape("fi", 10).AdvanceWidth, 6);
        Assert.Equal(0, regular.CollectionIndex);
        Assert.Equal(1, wide.CollectionIndex);
        Assert.Throws<ArgumentException>(() => BarcodeFont.FromBytes(FontBytes("TestOutlines.ttc"), 2));
        Assert.Throws<ArgumentException>(() => BarcodeFont.FromBytes(FontBytes("TestOutlines.ttf"), 1));
    }

    [Fact]
    public void ArabicJoiningAndUnsupportedScriptShaping_AreDistinguishedFromGlyphCoverage() {
        var font = BarcodeFont.FromBytes(FontBytes("TestOutlines.ttf"));
        var arabic = font.Shape("با", 10);
        Assert.Equal("با", arabic.Text);
        Assert.Equal(11.5, arabic.AdvanceWidth, 6); // initial beh (700) plus final alef (450).
        Assert.False(arabic.HasIncompleteShaping);
        Assert.Empty(arabic.MissingGlyphs);
        Assert.Throws<ArgumentException>(() => font.Shape("क", 10));
        var incomplete = font.Shape("क", 10, strict: false);
        Assert.True(incomplete.HasIncompleteShaping);
        Assert.Empty(incomplete.MissingGlyphs);
        Assert.NotEmpty(incomplete.Contours);
    }

    [Fact]
    public void FontCanBeUsedConcurrentlyWithoutChangingMetricsOrContours() {
        var font = LoadFont();
        var expected = font.Shape("Ab09 office", 4);
        Parallel.For(0, 64, _ => {
            var actual = font.Shape("Ab09 office", 4);
            Assert.Equal(expected.AdvanceWidth, actual.AdvanceWidth);
            Assert.Equal(expected.InkBounds.X, actual.InkBounds.X);
            Assert.Equal(expected.InkBounds.Width, actual.InkBounds.Width);
            Assert.Equal(expected.Contours.SelectMany(x => x).Select(x => x.X), actual.Contours.SelectMany(x => x).Select(x => x.X));
        });
    }

    [Fact]
    public void MalformedTableExtentsGlyphLocationsAndUtf16_FailAtThePublicBoundary() {
        var outside = FontBytes("TestOutlines.ttf");
        Array.Copy(new byte[] { 0x7f, 0xff, 0xff, 0xff }, 0, outside, 20, 4);
        Assert.Throws<ArgumentException>(() => BarcodeFont.FromBytes(outside));
        var locations = FontBytes("TestOutlines.ttf");
        var loca = TableOffset(locations, "loca");
        locations[loca] = 0xff; locations[loca + 1] = 0xff;
        Assert.Throws<ArgumentException>(() => BarcodeFont.FromBytes(locations));
        var font = LoadFont();
        Assert.Throws<ArgumentException>(() => font.Shape("\ud800", 4));
        Assert.Throws<ArgumentException>(() => font.Shape("\udc00", 4));
        Assert.Throws<ArgumentException>(() => font.Shape(new string('A', 32769), 4));
    }

    [Fact]
    public void TruncatedAndRecursiveGlyphs_AreRejectedInsteadOfReadingAdjacentDataOrLosingInk() {
        var truncated = FontBytes("TestOutlines.ttf");
        var glyph = GlyphOffset(truncated, 3); // A
        truncated[glyph] = 0x7f; truncated[glyph + 1] = 0xff;
        var badContours = BarcodeFont.FromBytes(truncated);
        Assert.Throws<InvalidDataException>(() => badContours.Shape("A", 4));
        var recursive = FontBytes("TestOutlines.ttf");
        glyph = GlyphOffset(recursive, 3);
        recursive[glyph] = recursive[glyph + 1] = 0xff;
        recursive[glyph + 10] = 0; recursive[glyph + 11] = 2; // XY byte offsets
        recursive[glyph + 12] = 0; recursive[glyph + 13] = 3; // references itself
        recursive[glyph + 14] = recursive[glyph + 15] = 0;
        Assert.Throws<InvalidDataException>(() => BarcodeFont.FromBytes(recursive).Shape("A", 4));
    }

    [Fact]
    public void EmptyCompositeExpansion_HasAWorkLimitEvenWhenItProducesNoPoints() {
        var font = BarcodeFont.FromBytes(FontBytes("TestOutlineBudget.ttf"));
        Assert.Throws<InvalidDataException>(() => font.Shape("AAAAAAAA", 4));
    }

    [Theory]
    [InlineData("A\u0301")]
    [InlineData("ب\u064e")]
    public void CombiningMarks_RequiringUnsupportedAttachmentAreReportedInStrictAndPreviewModes(string text) {
        var font = BarcodeFont.FromBytes(FontBytes("TestOutlines.ttf"));
        Assert.Throws<ArgumentException>(() => font.Shape(text, 4));
        var preview = font.Shape(text, 4, strict: false);
        Assert.Empty(preview.MissingGlyphs);
        Assert.True(preview.HasIncompleteShaping);
        Assert.Equal(text, preview.Text);
    }

    [Fact]
    public void RepeatedCff2SubroutineReferences_DoNotMultiplyDecodedStorage() {
        var data = FontBytes("TestSharedCff2.otf.gz");
        // The 256 shared references would exceed the cumulative entry allowance if decoded separately.
        var font = BarcodeFont.FromBytes(data);
        Assert.Equal(2.4, font.Shape("A", 4).AdvanceWidth, 6);
    }

    [Fact]
    public void UniqueCff2Indices_AreSubjectToACumulativeMetadataLimit() {
        Assert.Throws<ArgumentException>(() => BarcodeFont.FromBytes(FontBytes("TestCumulativeCff2.otf.gz")));
    }

    [Fact]
    public void NonmatchingSubstitutionCoverageRanges_CountTowardTheShapingWorkLimit() {
        var font = BarcodeFont.FromBytes(FontBytes("TestSubstitutionCoverage.ttf.gz"));
        Assert.Throws<InvalidDataException>(() => font.Shape(new string('A', 512), 4));
    }

    private static int GlyphOffset(byte[] data, int glyphId) {
        var head = TableOffset(data, "head");
        var loca = TableOffset(data, "loca");
        var format = data[head + 50] * 256 + data[head + 51];
        var offset = format == 0 ? 2 * (data[loca + glyphId * 2] * 256 + data[loca + glyphId * 2 + 1])
            : (data[loca + glyphId * 4] << 24) | (data[loca + glyphId * 4 + 1] << 16) | (data[loca + glyphId * 4 + 2] << 8) | data[loca + glyphId * 4 + 3];
        return TableOffset(data, "glyf") + offset;
    }

    private static int TableOffset(byte[] data, string tag) {
        var count = data[4] * 256 + data[5];
        for (var index = 0; index < count; index++) {
            var record = 12 + index * 16;
            if (System.Text.Encoding.ASCII.GetString(data, record, 4) == tag)
                return (data[record + 8] << 24) | (data[record + 9] << 16) | (data[record + 10] << 8) | data[record + 11];
        }
        throw new InvalidOperationException("Missing fixture table.");
    }

    private static byte[] FontBytes(string name) {
        using var stream = typeof(BarcodeFontContractTests).Assembly.GetManifestResourceStream("CodeGlyphX.Tests.Fixtures.Fonts." + name);
        Assert.NotNull(stream);
        using var output = new MemoryStream();
        if (name.EndsWith(".gz", StringComparison.Ordinal)) {
            using var unzip = new GZipStream(stream!, CompressionMode.Decompress);
            unzip.CopyTo(output);
        } else stream!.CopyTo(output);
        return output.ToArray();
    }

    private static BarcodeFont LoadFont(bool italic = false) {
        using var stream = typeof(BarcodeFontContractTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Fonts.OfficeIMOBaselineSans-" + (italic ? "Italic" : "Regular") + ".ttf");
        Assert.NotNull(stream);
        using var output = new MemoryStream();
        stream!.CopyTo(output);
        return BarcodeFont.FromBytes(output.ToArray());
    }
}
