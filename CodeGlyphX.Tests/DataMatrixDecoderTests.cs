using System;
using System.Collections.Generic;
using CodeGlyphX.DataMatrix;
using Xunit;

namespace CodeGlyphX.Tests;

public class DataMatrixDecoderTests {
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
