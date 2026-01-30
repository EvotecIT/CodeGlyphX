using System;
using System.Collections.Generic;
using CodeGlyphX.Rendering.Gif;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class GifDisposalTests {
    [Fact]
    public void Gif_Disposal_RestoreBackground_Clears_For_Next_Frame() {
        var gif = BuildTwoFrameGif();

        var frames = GifReader.DecodeAnimationCanvasFrames(gif, out var width, out var height, out _);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(2, frames.Length);

        var first = frames[0].Rgba;
        Assert.Equal(255, first[0]);
        Assert.Equal(0, first[1]);
        Assert.Equal(0, first[2]);
        Assert.Equal(255, first[3]);

        var second = frames[1].Rgba;
        Assert.Equal(0, second[0]);
        Assert.Equal(0, second[1]);
        Assert.Equal(0, second[2]);
        Assert.Equal(255, second[3]);
    }

    [Fact]
    public void Gif_Disposal_RestorePrevious_Reverts_For_Next_Frame() {
        var gif = BuildRestorePreviousGif();

        var frames = GifReader.DecodeAnimationCanvasFrames(gif, out var width, out var height, out _);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(3, frames.Length);

        var first = frames[0].Rgba;
        Assert.Equal(255, first[0]);
        Assert.Equal(0, first[1]);
        Assert.Equal(0, first[2]);
        Assert.Equal(255, first[3]);

        var second = frames[1].Rgba;
        Assert.Equal(0, second[0]);
        Assert.Equal(255, second[1]);
        Assert.Equal(0, second[2]);
        Assert.Equal(255, second[3]);

        var third = frames[2].Rgba;
        Assert.Equal(255, third[0]);
        Assert.Equal(0, third[1]);
        Assert.Equal(0, third[2]);
        Assert.Equal(255, third[3]);
    }

    private static byte[] BuildTwoFrameGif() {
        var output = new List<byte>(64);

        // Header + Logical Screen Descriptor
        output.AddRange(new byte[] {
            (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a',
            0x01, 0x00, // width
            0x01, 0x00, // height
            0xF0,       // GCT flag + 2 colors
            0x00,       // background index
            0x00        // aspect
        });

        // Global color table: black, red
        output.AddRange(new byte[] {
            0x00, 0x00, 0x00,
            0xFF, 0x00, 0x00
        });

        // Frame 1: red pixel, restore background
        AddGraphicControl(output, disposal: 2, transparent: false, transparentIndex: 0);
        AddImage(output, new byte[] { 0x01 });

        // Frame 2: transparent pixel
        AddGraphicControl(output, disposal: 0, transparent: true, transparentIndex: 0);
        AddImage(output, new byte[] { 0x00 });

        output.Add(0x3B); // Trailer
        return output.ToArray();
    }

    private static byte[] BuildRestorePreviousGif() {
        var output = new List<byte>(96);

        // Header + Logical Screen Descriptor
        output.AddRange(new byte[] {
            (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a',
            0x01, 0x00, // width
            0x01, 0x00, // height
            0xF1,       // GCT flag + 4 colors
            0x00,       // background index
            0x00        // aspect
        });

        // Global color table: black, red, green, blue
        output.AddRange(new byte[] {
            0x00, 0x00, 0x00,
            0xFF, 0x00, 0x00,
            0x00, 0xFF, 0x00,
            0x00, 0x00, 0xFF
        });

        // Frame 1: red pixel, do not dispose
        AddGraphicControl(output, disposal: 1, transparent: false, transparentIndex: 0);
        AddImage(output, new byte[] { 0x01 });

        // Frame 2: green pixel, restore previous
        AddGraphicControl(output, disposal: 3, transparent: false, transparentIndex: 0);
        AddImage(output, new byte[] { 0x02 });

        // Frame 3: transparent pixel (should show restored red)
        AddGraphicControl(output, disposal: 0, transparent: true, transparentIndex: 0);
        AddImage(output, new byte[] { 0x00 });

        output.Add(0x3B); // Trailer
        return output.ToArray();
    }

    private static void AddGraphicControl(List<byte> output, int disposal, bool transparent, byte transparentIndex) {
        output.Add(0x21);
        output.Add(0xF9);
        output.Add(0x04);
        var packed = (byte)((disposal << 2) | (transparent ? 0x01 : 0x00));
        output.Add(packed);
        output.Add(0x00);
        output.Add(0x00);
        output.Add(transparentIndex);
        output.Add(0x00);
    }

    private static void AddImage(List<byte> output, byte[] indices) {
        output.Add(0x2C);
        output.AddRange(new byte[] {
            0x00, 0x00, // left
            0x00, 0x00, // top
            0x01, 0x00, // width
            0x01, 0x00, // height
            0x00        // no local color table
        });

        const int minCodeSize = 2;
        output.Add(minCodeSize);
        var lzw = EncodeLzw(indices, minCodeSize);
        output.Add((byte)lzw.Length);
        output.AddRange(lzw);
        output.Add(0x00);
    }

    private static byte[] EncodeLzw(ReadOnlySpan<byte> indices, int minCodeSize) {
        if (indices.Length == 0) return Array.Empty<byte>();

        var clearCode = 1 << minCodeSize;
        var endCode = clearCode + 1;
        var nextCode = endCode + 1;
        var codeSize = minCodeSize + 1;

        var dict = new Dictionary<int, int>(4096);
        var output = new List<byte>(indices.Length);
        var bitBuffer = 0;
        var bitCount = 0;

        void WriteCode(int code) {
            bitBuffer |= code << bitCount;
            bitCount += codeSize;
            while (bitCount >= 8) {
                output.Add((byte)(bitBuffer & 0xFF));
                bitBuffer >>= 8;
                bitCount -= 8;
            }
        }

        void ResetDictionary() {
            dict.Clear();
            codeSize = minCodeSize + 1;
            nextCode = endCode + 1;
            WriteCode(clearCode);
        }

        ResetDictionary();
        var prefix = (int)indices[0];
        for (var i = 1; i < indices.Length; i++) {
            var c = indices[i];
            var key = (prefix << 8) | c;
            if (dict.TryGetValue(key, out var code)) {
                prefix = code;
                continue;
            }
            WriteCode(prefix);
            if (nextCode < (1 << 12)) {
                dict[key] = nextCode++;
                if (nextCode == (1 << codeSize) && codeSize < 12) {
                    codeSize++;
                }
            } else {
                ResetDictionary();
            }
            prefix = c;
        }

        WriteCode(prefix);
        WriteCode(endCode);
        if (bitCount > 0) {
            output.Add((byte)(bitBuffer & 0xFF));
        }
        return output.ToArray();
    }
}
