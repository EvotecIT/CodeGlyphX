using System;
using System.Text;
using CodeGlyphX;
using CodeGlyphX.Internal;
using CodeGlyphX.Qr;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrEncoderStandardsTests {
    [Fact]
    public void EncodeText_OptimalSegments_UseSmallerSymbolThanForcedByteMode() {
        var text = "123456789012345678901234567890123456789012345678901234567890";

        var optimized = QrCodeEncoder.EncodeText(text, new QrEncodingOptions {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.M,
            OptimizeSegments = true
        });
        var forcedByte = QrCodeEncoder.EncodeText(text, new QrEncodingOptions {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.M,
            OptimizeSegments = false
        });

        Assert.True(optimized.Version < forcedByte.Version);
        Assert.True(QrDecoder.TryDecode(optimized.Modules, out var decoded));
        Assert.Equal(text, decoded.Text);
    }

    [Fact]
    public void EncodeText_DefaultUtf8_EmitsEciWhenNeeded() {
        const string text = "Zażółć gęślą jaźń 😀";

        var qr = QrCodeEncoder.EncodeText(text);

        Assert.True(QrDecoder.TryDecode(qr.Modules, out var decoded));
        Assert.Equal(text, decoded.Text);
        Assert.Equal(Encoding.UTF8.GetBytes(text), decoded.Bytes);
    }

    [Fact]
    public void EncodeText_MixedKanjiAndAscii_RoundTripsAcrossOptimizedSegments() {
        const string text = "注文-2026-漢字-abc";

        var qr = QrCodeEncoder.EncodeText(text);

        Assert.True(QrDecoder.TryDecode(qr.Modules, out var decoded));
        Assert.Equal(text, decoded.Text);
    }

    [Fact]
    public void EncodeGs1_RoundTripsSeparatorsAndLiteralPercent() {
        const string elementString = "010590123412345710ABC%123\u001D2112345";

        var qr = QrCodeEncoder.EncodeGs1(elementString);

        Assert.True(QrDecoder.TryDecode(qr.Modules, out var decoded));
        Assert.Equal(QrFnc1Mode.FirstPosition, decoded.Fnc1Mode);
        Assert.Null(decoded.Fnc1ApplicationIndicator);
        Assert.Equal(elementString, decoded.Text);
        Assert.Equal(Encoding.ASCII.GetBytes(elementString), decoded.Bytes);
    }

    [Fact]
    public void EncodeText_Fnc1SecondPosition_RoundTripsApplicationIndicator() {
        const string text = "INDUSTRY-42";
        var qr = QrCodeEncoder.EncodeText(text, new QrEncodingOptions {
            TextEncoding = QrTextEncoding.Latin1,
            EciMode = QrEciMode.Never,
            Fnc1Mode = QrFnc1Mode.SecondPosition,
            Fnc1ApplicationIndicator = 37
        });

        Assert.True(QrDecoder.TryDecode(qr.Modules, out var decoded));
        Assert.Equal(text, decoded.Text);
        Assert.Equal(QrFnc1Mode.SecondPosition, decoded.Fnc1Mode);
        Assert.Equal(37, decoded.Fnc1ApplicationIndicator);
    }

    [Fact]
    public void EncodeStructuredAppend_UsesStandardSequenceFieldsAndSharedParity() {
        var parts = new[] { "FIRST-", "SECOND-", "THIRD" };
        var expectedParity = 0;
        foreach (var part in parts) {
            foreach (var value in Encoding.UTF8.GetBytes(part)) expectedParity ^= value;
        }

        var symbols = QrCodeEncoder.EncodeStructuredAppend(parts);

        Assert.Equal(parts.Length, symbols.Length);
        for (var i = 0; i < symbols.Length; i++) {
            Assert.True(QrDecoder.TryDecode(symbols[i].Modules, out var decoded));
            Assert.Equal(parts[i], decoded.Text);
            Assert.True(decoded.StructuredAppend.HasValue);
            Assert.Equal(i + 1, decoded.StructuredAppend!.Value.Index);
            Assert.Equal(parts.Length, decoded.StructuredAppend.Value.Total);
            Assert.Equal(expectedParity, decoded.StructuredAppend.Value.Parity);
        }
    }

    [Fact]
    public void EncodeStructuredAppend_BinaryParts_RoundTripWithoutTextConversion() {
        var parts = new[] {
            new byte[] { 0x00, 0x80, 0xFF },
            new byte[] { 0x10, 0x20, 0x30 }
        };
        var expectedParity = 0;
        foreach (var part in parts) foreach (var value in part) expectedParity ^= value;

        var symbols = QrCodeEncoder.EncodeStructuredAppend(parts);

        for (var i = 0; i < symbols.Length; i++) {
            Assert.True(QrDecoder.TryDecode(symbols[i].Modules, out var decoded));
            Assert.Equal(parts[i], decoded.Bytes);
            Assert.Equal(i + 1, decoded.StructuredAppend!.Value.Index);
            Assert.Equal(parts.Length, decoded.StructuredAppend.Value.Total);
            Assert.Equal(expectedParity, decoded.StructuredAppend.Value.Parity);
        }
    }

    [Fact]
    public void EncodeText_RejectsInvalidFnc1SecondPositionOptions() {
        Assert.Throws<ArgumentOutOfRangeException>(() => QrCodeEncoder.EncodeText("ABC", new QrEncodingOptions {
            Fnc1Mode = QrFnc1Mode.SecondPosition
        }));
        Assert.Throws<ArgumentException>(() => QrCodeEncoder.EncodeText("ABC", new QrEncodingOptions {
            Fnc1Mode = QrFnc1Mode.FirstPosition,
            Fnc1ApplicationIndicator = 1
        }));
    }

    [Theory]
    [InlineData("ASCII-123", QrTextEncoding.Ascii)]
    [InlineData("Zażółć", QrTextEncoding.Utf8)]
    [InlineData("A😀B", QrTextEncoding.Utf8)]
    [InlineData("ĄĆĘŁŃÓŚŹŻ", QrTextEncoding.Iso8859_2)]
    [InlineData("漢字ABC", QrTextEncoding.ShiftJis)]
    public void ByteCount_MatchesEncodedPayload(string text, QrTextEncoding encoding) {
        Assert.True(QrEncoding.CanEncode(text, encoding));
        Assert.Equal(QrEncoding.Encode(text, encoding).Length, QrEncoding.GetByteCount(text, 0, text.Length, encoding));
    }

    [Fact]
    public void KanjiPlannerPrefilter_CoversEverySupportedJisCharacter() {
        for (ushort value = 0; value < 0x2000; value++) {
            if (QrKanjiTable.TryGetUnicode(value, out var character)) {
                Assert.True(QrSegmentPlanner.CouldBeQrKanji(character), $"U+{(int)character:X4} was excluded by the planner prefilter.");
            }
        }
    }
}
