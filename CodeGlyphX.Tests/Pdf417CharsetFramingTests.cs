using CodeGlyphX.Pdf417;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class Pdf417CharsetFramingTests {
    [Theory]
    [InlineData(26, 0xC3, 0xA9, "é")]
    [InlineData(25, 0x6F, 0x22, "漢")]
    public void DeclaredCharset_ConsecutiveByteShiftsFormOneCharacter(int eci, int first, int second, string expected) {
        Assert.Equal(expected, Pdf417DecodedBitStreamParser.Decode(new[] { 927, eci, 913, first, 913, second }));
    }

    [Theory]
    [InlineData(new[] { 927, 26, 901, 0xC3, 901, 0xA9 }, "é")]
    [InlineData(new[] { 927, 25, 901, 0x6F, 901, 0x22 }, "漢")]
    [InlineData(new[] { 927, 26, 901, 0xC3, 913, 0xA9 }, "é")]
    [InlineData(new[] { 927, 26, 913, 0xC3, 901, 0xA9 }, "é")]
    [InlineData(new[] { 927, 26, 913, 0xC3, 900, 913, 0xA9 }, "é")]
    // The first base-900 group represents the six bytes "ABCDE" followed by 0xC3.
    [InlineData(new[] { 927, 26, 924, 109, 326, 368, 127, 455, 901, 0xA9 }, "ABCDEé")]
    // The next six-byte group starts with 0xA9 followed by "FGHIJ".
    [InlineData(new[] { 927, 26, 924, 109, 326, 368, 127, 455, 924, 283, 607, 624, 316, 154 }, "ABCDEéFGHIJ")]
    public void DeclaredCharset_ByteLatchesAndShiftsSharePendingBytes(int[] codewords, string expected) {
        Assert.Equal(expected, Pdf417DecodedBitStreamParser.Decode(codewords));
    }

    [Theory]
    [InlineData(0, "AAé")] // Alpha.
    [InlineData(810, "aé")] // Lower latch followed by a.
    [InlineData(840, "0é")] // Mixed latch followed by 0.
    [InlineData(865, "é")] // Mixed and punctuation latches.
    [InlineData(29, "Aé")] // Alpha A followed by punctuation-shift padding.
    public void TextSubmodes_ConsecutiveByteShiftsUseTheDeclaredCharset(int prefix, string expected) {
        Assert.Equal(expected, Pdf417DecodedBitStreamParser.Decode(new[] { 927, 26, prefix, 913, 0xC3, 913, 0xA9 }));
    }

    [Fact]
    public void DeclaredCharset_SurrogatePairCanSpanSeparateByteShifts() {
        Assert.Equal("😀", Pdf417DecodedBitStreamParser.Decode(new[] { 927, 25, 913, 0xD8, 913, 0x3D, 913, 0xDE, 913, 0x00 }));
    }

    [Fact]
    public void DecodedBytes_TextAndNumericCharactersKeepTheirOrder() {
        Assert.Equal("éAA2AB", Pdf417DecodedBitStreamParser.Decode(new[] {
            927, 26, 913, 0xC3, 913, 0xA9, 0, 902, 12, 900, 1
        }));
        // Text and numeric compaction already represent characters, including under UTF-16BE ECI.
        Assert.Equal("漢AA2", Pdf417DecodedBitStreamParser.Decode(new[] { 927, 25, 901, 0x6F, 0x22, 900, 0, 902, 12 }));
    }

    [Theory]
    [InlineData(new[] { 927, 26, 913, 0xC3, 0, 913, 0xA9 })]
    [InlineData(new[] { 927, 26, 913, 0xC3, 902, 12, 913, 0xA9 })]
    [InlineData(new[] { 927, 26, 913, 0xC3, 927, 26, 913, 0xA9 })]
    [InlineData(new[] { 927, 26, 913, 0xC3, 927, 3, 913, 0xA9 })]
    [InlineData(new[] { 927, 26, 913, 0xC3, 928, 0, 10, 123, 922, 913, 0xA9 })]
    public void IncompleteCharacters_DoNotJoinAcrossTextNumericEciOrMacroBoundaries(int[] codewords) {
        Assert.Null(Pdf417DecodedBitStreamParser.Decode(codewords));
    }

    [Fact]
    public void CharsetChange_FlushesThePreviousBytesBeforeUsingTheNextCharset() {
        Assert.Equal("é漢é", Pdf417DecodedBitStreamParser.Decode(new[] {
            927, 26, 913, 0xC3, 913, 0xA9,
            927, 25, 901, 0x6F, 901, 0x22,
            927, 3, 913, 0xE9
        }));
    }

    [Theory]
    [InlineData(new[] { 927, 26, 901, 0xC3, 0xA9, 927, 25, 0x6F, 0x22, 927, 3, 0xE9 }, "é漢é")]
    [InlineData(new[] { 901, 927, 26, 0xC3, 0xA9 }, "é")]
    [InlineData(new[] { 924, 109, 326, 368, 127, 330, 927, 25, 186, 215, 603, 445, 606 }, "ABCDEF漢AB")]
    public void CharsetChange_WithinByteCompactionKeepsItsByteMode(int[] codewords, string expected) {
        Assert.Equal(expected, Pdf417DecodedBitStreamParser.Decode(codewords));
    }

    [Theory]
    [InlineData(new[] { 927, 26, 913, 0xC3 })]
    [InlineData(new[] { 927, 25, 913, 0x6F })]
    [InlineData(new[] { 927, 26, 901, 0xC3, 901, 0x20 })]
    [InlineData(new[] { 927, 26, 913, 0xFF })]
    [InlineData(new[] { 927, 25, 913, 0xD8, 913, 0x3D })]
    [InlineData(new[] { 927, 26, 913 })]
    [InlineData(new[] { 927, 26, 0, 913 })]
    [InlineData(new[] { 927, 26, 0, 913, 256 })]
    [InlineData(new[] { 927, 26, 0, 913, -1 })]
    [InlineData(new[] { 927, 26, 901, 0xC3, 927, 3, 0xA9 })]
    [InlineData(new[] { 901, 927, 899, 65 })]
    [InlineData(new[] { 901, 65, 927 })]
    public void MalformedOrTruncatedByteFraming_IsRejectedWithoutReplacement(int[] codewords) {
        Assert.Null(Pdf417DecodedBitStreamParser.Decode(codewords));
    }

    [Fact]
    public void MacroMetadata_HasItsOwnTextInterpretationAndDoesNotConsumePayloadBytes() {
        var decoded = Pdf417DecodedBitStreamParser.Decode(new[] {
            927, 26, 913, 0xC3, 913, 0xA9,
            928, 0, 10, 123, 923, 0, 0, 913, 0xE9, 922
        }, out var macro);

        Assert.Equal("é", decoded);
        Assert.NotNull(macro);
        Assert.Equal("123", macro!.FileId);
        Assert.Equal("AAé", macro.FileName);
        Assert.True(macro.IsLastSegment);
    }

    [Fact]
    public void MacroTextFields_KeepTheirCharsetAndBytesIndependent() {
        var decoded = Pdf417DecodedBitStreamParser.Decode(new[] {
            927, 26, 901, 0xC3, 0xA9,
            928, 0, 10, 123,
            923, 0, 927, 25, 913, 0x6F, 913, 0x22,
            923, 3, 913, 0xE9, 922
        }, out var macro);

        Assert.Equal("é", decoded);
        Assert.NotNull(macro);
        Assert.Equal("漢", macro!.FileName);
        Assert.Equal("é", macro.Sender);
    }

    [Theory]
    [InlineData(new[] { 928, 0, 10, 123, 923, 0, 927, 26, 913, 0xC3, 923, 3, 913, 0xA9, 922 })]
    [InlineData(new[] { 928, 0, 10, 123, 923, 0, 927, 899, 913, 65, 922 })]
    public void MacroTextFields_RejectIncompleteOrUnknownCharsetData(int[] codewords) {
        Assert.Null(Pdf417DecodedBitStreamParser.Decode(codewords));
    }

    [Fact]
    public void SymbolsWithoutCharsetEci_KeepTheirExistingRawByteInterpretation() {
        Assert.Equal("é", Pdf417DecodedBitStreamParser.Decode(new[] { 901, 0xC3, 0xA9 }));
        Assert.Equal("Ã©", Pdf417DecodedBitStreamParser.Decode(new[] { 913, 0xC3, 913, 0xA9 }));
        Assert.Equal("Ã©", Pdf417DecodedBitStreamParser.Decode(new[] { 901, 0xC3, 901, 0xA9 }));
        Assert.Equal("ÿ", Pdf417DecodedBitStreamParser.Decode(new[] { 901, 0xFF }));
    }

    [Theory]
    [InlineData(new[] { 902, 0 })]
    [InlineData(new[] { 0, 902, 0 })]
    [InlineData(new[] { 0, 902, 12, 923 })]
    [InlineData(new[] { 0, 902, 12, 922 })]
    [InlineData(new[] { 0, 902, 12, 903 })]
    [InlineData(new[] { 0, 902, -1 })]
    public void InvalidNumericGroupsOrControls_DoNotReturnPartialText(int[] codewords) {
        Assert.Null(Pdf417DecodedBitStreamParser.Decode(codewords));
    }

    [Fact]
    public void InvalidNumericGroup_AfterAValidFullGroupRejectsThePayload() {
        var codewords = new List<int> { 0, 902 };
        // Fifteen base-900 codewords form the decimal sentinel 12 and therefore the digit 2.
        codewords.AddRange(new int[14]);
        codewords.Add(12);
        codewords.Add(0); // The following group has no decimal sentinel.

        Assert.Null(Pdf417DecodedBitStreamParser.Decode(codewords.ToArray()));
        codewords[codewords.Count - 1] = 13;
        Assert.Equal("AA23", Pdf417DecodedBitStreamParser.Decode(codewords.ToArray()));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(6)]
    public void MacroNumericFields_InvalidGroupAfterValidDataRejectsMetadata(int field) {
        var codewords = new List<int> { 928, 0, 10, 123, 923, field };
        codewords.AddRange(new int[14]);
        codewords.Add(12);
        codewords.Add(0); // A valid prior group must not hide this invalid trailing group.
        codewords.Add(922);

        Assert.Null(Pdf417DecodedBitStreamParser.Decode(codewords.ToArray(), out var macro));
        Assert.Null(macro);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(6)]
    public void MacroNumericFields_TruncatedOrInvalidDataIsRejected(int field) {
        Assert.Null(Pdf417DecodedBitStreamParser.Decode(new[] { 928, 0, 10, 123, 923, field }));
        Assert.Null(Pdf417DecodedBitStreamParser.Decode(new[] { 928, 0, 10, 123, 923, field, 0, 922 }));
    }

    [Theory]
    [InlineData(new[] { 901, 65, 927, 3, 66, 67, 68, 69, 70, 71 }, "ABCDEFG")]
    [InlineData(new[] { 927, 26, 901, 65, 927, 25, 0, 66, 0, 67, 0, 68 }, "ABCD")]
    [InlineData(new[] { 901, 65, 927, 3, 66, 67, 927, 26, 68, 69, 70, 71 }, "ABCDEFG")]
    public void ByteCompaction_ResidualBytesStayResidualAcrossCharsetChanges(int[] codewords, string expected) {
        Assert.Equal(expected, Pdf417DecodedBitStreamParser.Decode(codewords));
    }

    [Theory]
    [InlineData(new[] { 928, 0, 10 })]
    [InlineData(new[] { 928, 0, 10, 922 })]
    [InlineData(new[] { 928, 0, 10, 923, 0, 0, 922 })]
    [InlineData(new[] { 928, 0, 10, -1, 922 })]
    [InlineData(new[] { 928, 1, 900, 123, 922 })]
    public void MacroMandatoryData_RequiresFileIdAndValidNumericCodewords(int[] codewords) {
        Assert.Null(Pdf417DecodedBitStreamParser.Decode(codewords, out var macro));
        Assert.Null(macro);
    }

    [Fact]
    public void MacroFileId_DataCodewordsKeepTheirThreeDigitBoundaries() {
        Assert.Equal(string.Empty, Pdf417DecodedBitStreamParser.Decode(new[] { 928, 0, 10, 0, 899, 922 }, out var macro));
        Assert.NotNull(macro);
        Assert.Equal("000899", macro!.FileId);
    }
}
