using System.Collections.Generic;
using System.Reflection;
using CodeGlyphX.Pdf417;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class Pdf417MacroRangeTests {
    [Theory]
    [InlineData(-1)]
    [InlineData(99999)]
    [InlineData(100000)]
    public void Encoder_RejectsIndexesOutsideThePublishedRange(int index) {
        Assert.Throws<InvalidOperationException>(() => Pdf417Code.EncodeMacro("A",
            new Pdf417MacroOptions { FileId = "123", SegmentIndex = index }));
    }

    [Theory]
    [InlineData(0, null, false)]
    [InlineData(99998, null, true)]
    [InlineData(0, 1, true)]
    [InlineData(0, 2, false)]
    [InlineData(99998, 99999, true)]
    public void Encoder_PreservesValidIndexCountAndLastBoundaries(int index, int? count, bool last) {
        var decoded = EncodeAndDecode("A", index, count, last);
        Assert.Equal("A", decoded.Text);
        Assert.Equal(index, decoded.Macro!.SegmentIndex);
        Assert.Equal(count, decoded.Macro.SegmentCount);
        Assert.Equal(last, decoded.Macro.IsLastSegment);
    }

    [Theory]
    [InlineData(0, -1, true)]
    [InlineData(0, 0, true)]
    [InlineData(0, 100000, true)]
    [InlineData(1, 1, true)]
    [InlineData(0, 2, true)]
    [InlineData(1, 2, false)]
    public void Encoder_RejectsContradictoryCountAndLastMetadata(int index, int count, bool last) {
        Assert.Throws<InvalidOperationException>(() => Pdf417Code.EncodeMacro("A",
            new Pdf417MacroOptions { FileId = "123", SegmentIndex = index, SegmentCount = count, IsLastSegment = last }));
    }

    [Fact]
    public void Encoder_MaximumIndexRequiresALastMarkerWhenTheCountIsOmitted() {
        Assert.Throws<InvalidOperationException>(() => Pdf417Code.EncodeMacro("A",
            new Pdf417MacroOptions { FileId = "123", SegmentIndex = 99998 }));
    }

    [Fact]
    public void Encoder_PermitsGlobalFieldsOnANonLastSegment() {
        var symbol = Pdf417Code.EncodeMacro("A", new Pdf417MacroOptions {
            FileId = "123", SegmentIndex = 0, SegmentCount = 2, FileName = "file.txt"
        });
        Assert.True(Pdf417Decoder.TryDecode(symbol.Modules, out Pdf417Decoded decoded));
        Assert.Equal("file.txt", decoded.Macro!.FileName);
        Assert.Equal(2, decoded.Macro.SegmentCount);
        Assert.False(decoded.Macro.IsLastSegment);
    }

    [Fact]
    public void Encoder_UsesTwoCodewordsForTheSegmentCount() {
        var method = typeof(Pdf417Encoder).GetMethod("EncodeMacroBlock", BindingFlags.NonPublic | BindingFlags.Static)!;
        var words = Assert.IsType<List<int>>(method.Invoke(null, new object[] {
            new Pdf417MacroOptions { FileId = "123", SegmentIndex = 2, SegmentCount = 3, IsLastSegment = true }
        }));
        // Padded 00002/00003 with decimal sentinel 1 yield 111,102 and 111,103.
        Assert.Equal(new[] { 928, 111, 102, 123, 923, 1, 111, 103, 922 }, words);
    }

    [Theory]
    [MemberData(nameof(InvalidMacroStreams))]
    public void PublicDecoders_RejectInvalidMacroRangesAndRelationships(bool micro, int[] macroWords) {
        var modules = BuildModules(micro, macroWords);
        Assert.False(micro
            ? MicroPdf417Decoder.TryDecode(modules, out _)
            : Pdf417Decoder.TryDecode(modules, out string _));
    }

    public static IEnumerable<object[]> InvalidMacroStreams() {
        var words = new[] {
            new[] { 928, 222, 199, 123, 922 }, // Index 99999.
            new[] { 928, 222, 198, 123 }, // Maximum index without the required last flag.
            new[] { 928, 111, 100, 123, 923, 1, 111, 100, 922 }, // Count 0.
            new[] { 928, 111, 100, 123, 923, 1, 1, 322, 200, 922 }, // Count 100000.
            new[] { 928, 111, 101, 123, 923, 1, 111, 101, 922 }, // Index 1, count 1.
            new[] { 928, 111, 100, 123, 923, 1, 111, 102, 922 }, // Early last flag.
            new[] { 928, 111, 101, 123, 923, 1, 111, 102 } // Missing last flag.
        };
        foreach (var macroWords in words) {
            yield return new object[] { false, macroWords };
            yield return new object[] { true, macroWords };
        }
    }

    [Fact]
    public void Decoder_PreservesValidBoundaryAndLegacyShortCounts() {
        Assert.Equal(string.Empty, Pdf417DecodedBitStreamParser.Decode(new[] {
            928, 222, 198, 123, 923, 1, 222, 199, 922
        }, out var maximum));
        Assert.Equal(99998, maximum!.SegmentIndex);
        Assert.Equal(99999, maximum.SegmentCount);
        Assert.Equal(string.Empty, Pdf417DecodedBitStreamParser.Decode(new[] {
            928, 111, 101, 123, 923, 1, 12, 922
        }, out var legacy));
        Assert.Equal(1, legacy!.SegmentIndex);
        Assert.Equal(2, legacy.SegmentCount);
    }

    [Fact]
    public void Assembler_RejectsConflictingCountsWithoutChangingAcceptedState() {
        var assembler = new Pdf417MacroAssembler();
        Assert.True(assembler.TryAdd(EncodeAndDecode("B", 1, 2, true)));
        Assert.False(assembler.TryAdd(EncodeAndDecode("C", 2, 3, true)));
        Assert.Equal(1, assembler.ReceivedCount);
        Assert.Equal(2, assembler.SegmentCount);
        Assert.Equal(1, assembler.LastSegmentIndex);
        Assert.True(assembler.TryAdd(EncodeAndDecode("A", 0, null, false)));
        Assert.Equal("AB", assembler.Assemble());
    }

    [Fact]
    public void Assembler_RejectsCountSmallerThanPreviouslyAcceptedIndexes() {
        var assembler = new Pdf417MacroAssembler();
        Assert.True(assembler.TryAdd(EncodeAndDecode("C", 2, null, false)));
        Assert.False(assembler.TryAdd(EncodeAndDecode("B", 1, 2, true)));
        Assert.Equal(1, assembler.ReceivedCount);
        Assert.Null(assembler.SegmentCount);
        Assert.Null(assembler.LastSegmentIndex);
        Assert.False(assembler.HasLastSegment);
        Assert.False(assembler.IsComplete);
    }

    [Fact]
    public void Assembler_RejectsAnUncountedSegmentOutsideTheKnownCount() {
        var assembler = new Pdf417MacroAssembler();
        Assert.True(assembler.TryAdd(EncodeAndDecode("A", 0, 2, false)));
        Assert.False(assembler.TryAdd(EncodeAndDecode("C", 2, null, false)));
        Assert.Equal(1, assembler.ReceivedCount);
        Assert.Equal(2, assembler.SegmentCount);
        Assert.False(assembler.HasLastSegment);
        Assert.True(assembler.TryAdd(EncodeAndDecode("B", 1, null, true)));
        Assert.Equal("AB", assembler.Assemble());
    }

    [Fact]
    public void Assembler_RejectsAFirstCountThatRevealsAMissingLastMarker() {
        var assembler = new Pdf417MacroAssembler();
        Assert.True(assembler.TryAdd(EncodeAndDecode("B", 1, null, false)));
        Assert.False(assembler.TryAdd(EncodeAndDecode("A", 0, 2, false)));
        Assert.Equal(1, assembler.ReceivedCount);
        Assert.Null(assembler.SegmentCount);
        Assert.False(assembler.HasLastSegment);
    }

    [Fact]
    public void Assembler_RejectsASecondLastMarkerWithoutChangingCompletion() {
        var assembler = new Pdf417MacroAssembler();
        Assert.True(assembler.TryAdd(EncodeAndDecode("A", 0, null, true)));
        Assert.False(assembler.TryAdd(EncodeAndDecode("B", 1, null, true)));
        Assert.Equal(1, assembler.ReceivedCount);
        Assert.Equal(0, assembler.LastSegmentIndex);
        Assert.Equal("A", assembler.Assemble());
    }

    [Fact]
    public void Assembler_AcceptsTheCountOnEverySegment() {
        var assembler = new Pdf417MacroAssembler();
        Assert.True(assembler.TryAdd(EncodeAndDecode("A", 0, 2, false)));
        Assert.True(assembler.TryAdd(EncodeAndDecode("B", 1, 2, true)));
        Assert.Equal("AB", assembler.Assemble());
    }

    [Theory]
    [InlineData("FileName", false)]
    [InlineData("FileName", true)]
    [InlineData("Timestamp", false)]
    [InlineData("Timestamp", true)]
    [InlineData("Sender", false)]
    [InlineData("Sender", true)]
    [InlineData("Addressee", false)]
    [InlineData("Addressee", true)]
    [InlineData("FileSize", false)]
    [InlineData("FileSize", true)]
    [InlineData("Checksum", false)]
    [InlineData("Checksum", true)]
    public void Assembler_RejectsConflictingGlobalFieldsAtomically(string field, bool reverseArrival) {
        var firstIndex = reverseArrival ? 1 : 0;
        var nextIndex = 1 - firstIndex;
        var assembler = new Pdf417MacroAssembler();
        Assert.True(assembler.TryAdd(EncodeAndDecode(firstIndex == 0 ? "A" : "B", CreateGlobalMacroOptions(firstIndex))));
        var conflicting = CreateGlobalMacroOptions(nextIndex);
        switch (field) {
            case "FileName": conflicting.FileName = "A.txt"; break;
            case "Timestamp": conflicting.Timestamp = 1700000001; break;
            case "Sender": conflicting.Sender = "Sender"; break;
            case "Addressee": conflicting.Addressee = "Receiver"; break;
            case "FileSize": conflicting.FileSize = 101; break;
            case "Checksum": conflicting.Checksum = 12346; break;
            default: throw new InvalidOperationException("Unknown metadata field.");
        }

        Assert.False(assembler.TryAdd(EncodeAndDecode(nextIndex == 0 ? "A" : "B", conflicting)));
        Assert.Equal(1, assembler.ReceivedCount);
        Assert.Equal("123", assembler.FileId);
        Assert.Equal(2, assembler.SegmentCount);
        Assert.Equal(reverseArrival, assembler.HasLastSegment);
        Assert.Equal(reverseArrival ? 1 : (int?)null, assembler.LastSegmentIndex);
        Assert.False(assembler.IsComplete);
        AssertGlobalFields(assembler);

        Assert.True(assembler.TryAdd(EncodeAndDecode(nextIndex == 0 ? "A" : "B", CreateGlobalMacroOptions(nextIndex))));
        Assert.Equal("AB", assembler.Assemble());
        AssertGlobalFields(assembler);
    }

    [Theory]
    [InlineData(0)] // Matching fields on both segments.
    [InlineData(1)] // Fields first become known on the last segment.
    [InlineData(2)] // The last segment omits fields already known.
    public void Assembler_AcceptsMatchingOrMissingGlobalFields(int missingFields) {
        var assembler = new Pdf417MacroAssembler();
        var first = missingFields == 1
            ? new Pdf417MacroOptions { FileId = "123", SegmentIndex = 0, SegmentCount = 2 }
            : CreateGlobalMacroOptions(0);
        var last = missingFields == 2
            ? new Pdf417MacroOptions { FileId = "123", SegmentIndex = 1, SegmentCount = 2, IsLastSegment = true }
            : CreateGlobalMacroOptions(1);
        Assert.True(assembler.TryAdd(EncodeAndDecode("A", first)));
        Assert.True(assembler.TryAdd(EncodeAndDecode("B", last)));
        Assert.Equal("AB", assembler.Assemble());
        AssertGlobalFields(assembler);
    }

    private static Pdf417MacroOptions CreateGlobalMacroOptions(int index) => new Pdf417MacroOptions {
        FileId = "123", SegmentIndex = index, SegmentCount = 2, IsLastSegment = index == 1,
        FileName = "a.txt", Timestamp = 1700000000, Sender = "sender", Addressee = "receiver",
        FileSize = 100, Checksum = 12345
    };

    private static void AssertGlobalFields(Pdf417MacroAssembler assembler) {
        Assert.Equal("a.txt", assembler.FileName);
        Assert.Equal(1700000000L, assembler.Timestamp);
        Assert.Equal("sender", assembler.Sender);
        Assert.Equal("receiver", assembler.Addressee);
        Assert.Equal(100L, assembler.FileSize);
        Assert.Equal(12345, assembler.Checksum);
    }

    private static Pdf417Decoded EncodeAndDecode(string text, int index, int? count, bool last) {
        return EncodeAndDecode(text, new Pdf417MacroOptions {
            FileId = "123", SegmentIndex = index, SegmentCount = count, IsLastSegment = last
        });
    }

    private static Pdf417Decoded EncodeAndDecode(string text, Pdf417MacroOptions macro) {
        var symbol = Pdf417Code.EncodeMacro(text, macro);
        Assert.True(Pdf417Decoder.TryDecode(symbol.Modules, out Pdf417Decoded decoded));
        Assert.NotNull(decoded.Macro);
        return decoded;
    }

    private static BitMatrix BuildModules(bool micro, int[] macroWords) {
        if (micro) {
            var words = new List<int> { 0 };
            words.AddRange(macroWords);
            var method = typeof(MicroPdf417Encoder).GetMethod("EncodeCodewords", BindingFlags.NonPublic | BindingFlags.Static)!;
            return Assert.IsType<BitMatrix>(method.Invoke(null, new object[] { words, new MicroPdf417EncodeOptions() }));
        }
        var pdfMethod = typeof(Pdf417Encoder).GetMethod("EncodeCodewords", BindingFlags.NonPublic | BindingFlags.Static)!;
        var symbol = Assert.IsType<Pdf417Symbol>(pdfMethod.Invoke(null, new object[] {
            new List<int> { 0 }, new Pdf417EncodeOptions(), new List<int>(macroWords)
        }));
        return symbol.Modules;
    }
}
