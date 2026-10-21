using System;
using System.Linq;
using CodeGlyphX.CodeOne;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class CodeOneControlBoundaryTests {
    [Fact]
    public void InvalidUtf8Eci_FailsTryDecodeWithoutThrowing() {
        var symbol = CodeOneEncoder.EncodeBytes(new byte[] { 255 }, new CodeOneEncodingOptions { EciAssignmentNumber = 26 });
        Assert.False(CodeOneDecoder.TryDecode(symbol.Modules, out _));
        Assert.False(SymbolDecoder.TryDecode(symbol.Modules, out _, SymbolFormat.CodeOne));
        // Both ECI-transition and terminal flushes must use the same conversion policy.
        var data = new[] { 129, 93, 93, 130, 130, 156, 235, 128, 93, 130, 130, 133, 66 };
        Assert.False(CodeOnePayloadDecoder.TryDecode(data, data.Length, out _));
    }

    [Theory]
    [InlineData(230, 0)]
    [InlineData(230, 1)]
    [InlineData(230, 2)]
    [InlineData(239, 0)]
    [InlineData(239, 1)]
    [InlineData(239, 2)]
    public void FinalTwoTripletValues_PermitEachNormativeShiftPadding(int mode, int padding) {
        var layout = CodeOneLayout.All[(int)CodeOneVersion.B - 1];
        var words = new int[layout.DataCount + layout.EccCount];
        words[0] = mode;
        for (var i = 0; i < 9; i++) {
            var packed = 14 * 1600 + 14 * 40 + (i == 8 ? padding : 14) + 1;
            words[i * 2 + 1] = packed >> 8; words[i * 2 + 2] = packed & 255;
        }
        CodeOneErrorCorrection.Process(words, layout, decode: false);
        Assert.True(CodeOneDecoder.TryDecode(layout.Place(words), out var decoded));
        Assert.Equal(new string(mode == 230 ? 'A' : 'a', 26), decoded.Text);
        var shortWords = new[] { mode, words[17], words[18], 255, 66 };
        Assert.False(CodeOnePayloadDecoder.TryDecode(shortWords, shortWords.Length, out _));
    }

    [Theory]
    [InlineData(CodeOneVersion.A, "X", 18, "A")]
    [InlineData(CodeOneVersion.A, "X", 18, "?")]
    [InlineData(CodeOneVersion.A, "X", 20, "")]
    [InlineData(CodeOneVersion.B, "aaaaa", 30, "Z")]
    public void AlignedDecimalRun_CanLeaveOneFinalAsciiCodeword(CodeOneVersion version, string prefix, int digits, string suffix) {
        var text = prefix + new string('1', digits) + suffix;
        var symbol = CodeOneEncoder.EncodeText(text, new CodeOneEncodingOptions { Version = version });
        Assert.Equal(version, symbol.Version);
        Assert.True(CodeOneDecoder.TryDecode(symbol.Modules, out var decoded));
        Assert.Equal(text, decoded.Text);
        Assert.Equal(version, CodeOneEncoder.EncodeText(text).Version);
    }

    [Fact]
    public void TerminalDecimalGs1Separator_UsesFnc1Control() {
        var text = new string('?', 9) + new string('1', 18) + Gs1.GroupSeparator;
        var bytes = System.Text.Encoding.ASCII.GetBytes(text);
        var symbol = CodeOneEncoder.EncodeBytes(bytes, new CodeOneEncodingOptions { Version = CodeOneVersion.B, IsGs1 = true });
        Assert.True(CodeOneLayout.All[(int)CodeOneVersion.B - 1].TryExtract(new CodeGlyphX.Internal.BitMatrixView(symbol.Modules), out var words));
        Assert.Equal(232, words[18]);
        Assert.True(CodeOneDecoder.TryDecode(symbol.Modules, out var decoded));
        Assert.Equal(text, decoded.Text);
        Assert.True(decoded.IsGs1);
    }

    [Fact]
    public void TerminalEdiGs1Separator_UsesFnc1Control() {
        var text = "91" + new string('>', 22) + Gs1.GroupSeparator;
        var symbol = CodeOneEncoder.EncodeBytes(System.Text.Encoding.ASCII.GetBytes(text),
            new CodeOneEncodingOptions { Version = CodeOneVersion.B, IsGs1 = true });
        Assert.True(CodeOneLayout.All[(int)CodeOneVersion.B - 1].TryExtract(new CodeGlyphX.Internal.BitMatrixView(symbol.Modules), out var words));
        Assert.Equal(238, words[1]);
        Assert.Equal(232, words[18]);
        Assert.True(CodeOneDecoder.TryDecode(symbol.Modules, out var decoded));
        Assert.Equal(text, decoded.Text);
    }
}
