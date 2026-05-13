using System;
using System.Collections.Generic;
using CodeGlyphX.DataMatrix;
using Xunit;

namespace CodeGlyphX.Tests;

public class DataMatrixDecoderTests {
    [Fact]
    public void DataMatrix_Encode_SingleCharacter_UsesIsoTimingPattern() {
        var matrix = DataMatrixEncoder.Encode("A");

        Assert.Equal(10, matrix.Width);
        Assert.Equal(10, matrix.Height);

        for (var x = 0; x < matrix.Width; x++) {
            Assert.Equal((x & 1) == 0, matrix[x, 0]);
            Assert.True(matrix[x, matrix.Height - 1]);
        }

        for (var y = 1; y < matrix.Height - 1; y++) {
            Assert.True(matrix[0, y]);
            Assert.Equal((y & 1) != 0, matrix[matrix.Width - 1, y]);
        }

        Assert.True(DataMatrixDecoder.TryDecode(matrix, out var text));
        Assert.Equal("A", text);
    }

    [Fact]
    public void DataMatrix_ReedSolomon_ForSingleAsciiSymbol_UsesIso16022Roots() {
        var divisor = DataMatrixReedSolomon.ComputeDivisor(5);
        var ecc = DataMatrixReedSolomon.ComputeRemainder(new byte[] { 66, 129, 71 }, divisor);

        Assert.Equal(new byte[] { 180, 133, 93, 98, 187 }, ecc);
    }

    [Fact]
    public void DataMatrix_Decode_C40_Basic() {
        var codewords = EncodeTripletMode(230, 14, 15, 16, 5, 6, 7); // ABC123
        var text = DataMatrixDecoder.DecodeDataCodewords(codewords);
        Assert.Equal("ABC123", text);
    }

    [Fact]
    public void DataMatrix_Decode_Text_Basic() {
        var codewords = EncodeTripletMode(239, 14, 15, 16); // abc
        var text = DataMatrixDecoder.DecodeDataCodewords(codewords);
        Assert.Equal("abc", text);
    }

    [Fact]
    public void DataMatrix_Decode_X12_Basic() {
        var codewords = EncodeTripletMode(238, 14, 15, 16); // ABC
        var text = DataMatrixDecoder.DecodeDataCodewords(codewords);
        Assert.Equal("ABC", text);
    }

    [Fact]
    public void DataMatrix_Decode_Edifact_Basic() {
        var codewords = EncodeEdifact(240, 33, 34, 35, 31); // ABC + unlatch
        var text = DataMatrixDecoder.DecodeDataCodewords(codewords);
        Assert.Equal("ABC", text);
    }

    private static byte[] EncodeTripletMode(byte latch, params int[] values) {
        if (values.Length == 0 || values.Length % 3 != 0) throw new ArgumentException("Values must be a multiple of 3.", nameof(values));
        var output = new List<byte>(values.Length / 3 * 2 + 2) { latch };
        for (var i = 0; i < values.Length; i += 3) {
            var full = 1600 * values[i] + 40 * values[i + 1] + values[i + 2] + 1;
            output.Add((byte)(full / 256));
            output.Add((byte)(full % 256));
        }
        output.Add(254); // Unlatch
        return output.ToArray();
    }

    private static byte[] EncodeEdifact(byte latch, params int[] values) {
        if (values.Length == 0 || values.Length % 4 != 0) throw new ArgumentException("Values must be a multiple of 4.", nameof(values));
        var output = new List<byte>(values.Length / 4 * 3 + 1) { latch };
        for (var i = 0; i < values.Length; i += 4) {
            var bits = (values[i] << 18) | (values[i + 1] << 12) | (values[i + 2] << 6) | values[i + 3];
            output.Add((byte)((bits >> 16) & 0xFF));
            output.Add((byte)((bits >> 8) & 0xFF));
            output.Add((byte)(bits & 0xFF));
        }
        return output.ToArray();
    }
}
