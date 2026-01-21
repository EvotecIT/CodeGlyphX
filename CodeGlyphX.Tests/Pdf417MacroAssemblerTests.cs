using CodeGlyphX.Pdf417;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class Pdf417MacroAssemblerTests {
    [Fact]
    public void Pdf417MacroAssembler_AssemblesSegmentsInOrder() {
        var macro0 = new Pdf417MacroOptions {
            FileId = "000123",
            SegmentIndex = 0
        };
        var macro1 = new Pdf417MacroOptions {
            FileId = "000123",
            SegmentIndex = 1,
            IsLastSegment = true,
            SegmentCount = 2,
            FileName = "payload.txt",
            Sender = "sender",
            FileSize = 11
        };

        var matrix0 = Pdf417Encoder.EncodeMacro("Hello ", macro0);
        var matrix1 = Pdf417Encoder.EncodeMacro("World", macro1);
        Assert.True(Pdf417Decoder.TryDecode(matrix0, out Pdf417Decoded decoded0));
        Assert.True(Pdf417Decoder.TryDecode(matrix1, out Pdf417Decoded decoded1));
        Assert.NotNull(decoded0.Macro);
        Assert.NotNull(decoded1.Macro);
        Assert.Equal(0, decoded0.Macro!.SegmentIndex);
        Assert.Equal(1, decoded1.Macro!.SegmentIndex);
        Assert.Null(decoded0.Macro!.SegmentCount);
        Assert.Equal(2, decoded1.Macro!.SegmentCount);
        Assert.True(decoded1.Macro!.IsLastSegment);
        Assert.Equal("Hello ", decoded0.Text);
        Assert.Equal("World", decoded1.Text);

        var assembler = new Pdf417MacroAssembler();
        Assert.True(assembler.TryAdd(decoded1));
        Assert.True(assembler.TryAdd(decoded0));
        Assert.True(assembler.IsComplete);

        Assert.True(assembler.TryAssemble(out var text));
        Assert.Equal("Hello World", text);
        Assert.Equal("000123", assembler.FileId);
        Assert.Equal(2, assembler.SegmentCount);
        Assert.Equal("payload.txt", assembler.FileName);
        Assert.Equal("sender", assembler.Sender);
        Assert.Equal(11, assembler.FileSize);
    }

    [Fact]
    public void Pdf417MacroAssembler_RejectsNonMacroSegments() {
        var matrix = Pdf417Encoder.Encode("Plain");
        Assert.True(Pdf417Decoder.TryDecode(matrix, out Pdf417Decoded decoded));

        var assembler = new Pdf417MacroAssembler();
        Assert.False(assembler.TryAdd(decoded));
        Assert.False(assembler.IsComplete);
    }

    [Fact]
    public void Pdf417MacroAssembler_AcceptsCodeGlyphDecodedSegments() {
        var macro0 = new Pdf417MacroOptions { FileId = "123123", SegmentIndex = 0 };
        var macro1 = new Pdf417MacroOptions { FileId = "123123", SegmentIndex = 1, IsLastSegment = true };

        var matrix0 = Pdf417Encoder.EncodeMacro("Part A ", macro0);
        var matrix1 = Pdf417Encoder.EncodeMacro("Part B", macro1);

        Assert.True(CodeGlyph.TryDecode(matrix0, out var decoded0));
        Assert.True(CodeGlyph.TryDecode(matrix1, out var decoded1));

        var assembler = new Pdf417MacroAssembler();
        Assert.True(assembler.TryAdd(decoded0));
        Assert.True(assembler.TryAdd(decoded1));
        Assert.True(assembler.IsComplete);

        Assert.True(assembler.TryAssemble(out var text));
        Assert.Equal("Part A Part B", text);
    }

    [Fact]
    public void Pdf417MacroAssembler_RejectsMismatchedFileId() {
        var macro0 = new Pdf417MacroOptions { FileId = "111222", SegmentIndex = 0 };
        var macro1 = new Pdf417MacroOptions { FileId = "333444", SegmentIndex = 1, IsLastSegment = true };

        var matrix0 = Pdf417Encoder.EncodeMacro("A", macro0);
        var matrix1 = Pdf417Encoder.EncodeMacro("B", macro1);
        Assert.True(Pdf417Decoder.TryDecode(matrix0, out Pdf417Decoded decoded0));
        Assert.True(Pdf417Decoder.TryDecode(matrix1, out Pdf417Decoded decoded1));

        var assembler = new Pdf417MacroAssembler();
        Assert.True(assembler.TryAdd(decoded0));
        Assert.False(assembler.TryAdd(decoded1));
    }
}
