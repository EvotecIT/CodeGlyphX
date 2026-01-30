using CodeGlyphX.Rendering.Gif;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class GifAnimationEncodeTests {
    [Fact]
    public void Gif_AnimationEncode_RoundTripsFrames() {
        const int width = 2;
        const int height = 2;
        const int stride = width * 4;

        var frame1 = new byte[] {
            0, 0, 0, 255,       255, 0, 0, 255,
            0, 255, 0, 255,     0, 0, 255, 255
        };

        var frame2 = new byte[] {
            0, 0, 0, 0,         255, 255, 0, 255,
            255, 0, 255, 255,   0, 255, 255, 255
        };

        var frames = new[] {
            new GifAnimationFrame(frame1, width, height, stride, durationMs: 120, disposalMethod: GifDisposalMethod.RestoreBackground),
            new GifAnimationFrame(frame2, width, height, stride, durationMs: 50, disposalMethod: GifDisposalMethod.None)
        };

        var gif = GifWriter.WriteAnimationRgba32(width, height, frames, new GifAnimationOptions(loopCount: 3, backgroundRgba: 0x000000FF));

        Assert.True(GifReader.TryDecodeAnimationFrames(gif, out var decodedFrames, out var canvasWidth, out var canvasHeight, out var options));
        Assert.Equal(width, canvasWidth);
        Assert.Equal(height, canvasHeight);
        Assert.Equal(3, options.LoopCount);
        Assert.Equal(2, decodedFrames.Length);

        Assert.Equal(frame1, decodedFrames[0].Rgba);
        Assert.Equal(GifDisposalMethod.RestoreBackground, decodedFrames[0].DisposalMethod);
        Assert.Equal(frame2, decodedFrames[1].Rgba);
    }

    [Fact]
    public void Gif_AnimationEncode_UsesGlobalPalette_WhenFramesMatch() {
        const int width = 3;
        const int height = 1;
        const int stride = width * 4;

        var frame1 = new byte[] {
            0, 0, 0, 255,       255, 255, 255, 255,   0, 0, 0, 255
        };

        var frame2 = new byte[] {
            0, 0, 0, 255,       0, 0, 0, 255,         255, 255, 255, 255
        };

        var frames = new[] {
            new GifAnimationFrame(frame1, width, height, stride, durationMs: 100),
            new GifAnimationFrame(frame2, width, height, stride, durationMs: 100)
        };

        var gif = GifWriter.WriteAnimationRgba32(width, height, frames, new GifAnimationOptions(loopCount: 0, backgroundRgba: 0x000000FF));

        var packedFields = CollectImagePackedFields(gif);
        Assert.Equal(2, packedFields.Length);
        Assert.All(packedFields, packed => Assert.Equal(0, packed & 0x80));
    }

    [Fact]
    public void Gif_AnimationEncode_UsesGlobalPalette_WhenUnionFits() {
        const int width = 2;
        const int height = 1;
        const int stride = width * 4;

        var frame1 = new byte[] {
            255, 0, 0, 255,     0, 255, 0, 255
        };

        var frame2 = new byte[] {
            0, 0, 255, 255,     0, 255, 0, 255
        };

        var frames = new[] {
            new GifAnimationFrame(frame1, width, height, stride, durationMs: 80),
            new GifAnimationFrame(frame2, width, height, stride, durationMs: 80)
        };

        var gif = GifWriter.WriteAnimationRgba32(width, height, frames, new GifAnimationOptions(loopCount: 0, backgroundRgba: 0x00FF00FF));

        var packedFields = CollectImagePackedFields(gif);
        Assert.Equal(2, packedFields.Length);
        Assert.All(packedFields, packed => Assert.Equal(0, packed & 0x80));
    }

    [Fact]
    public void Gif_AnimationEncode_UsesGlobalPalette_WhenUnionExceedsLimit() {
        const int width = 20;
        const int height = 20;
        const int stride = width * 4;
        var pixelCount = width * height;

        var frame1 = new byte[pixelCount * 4];
        var frame2 = new byte[pixelCount * 4];

        FillUniqueColors(frame1, offset: 0);
        FillUniqueColors(frame2, offset: 400);

        var frames = new[] {
            new GifAnimationFrame(frame1, width, height, stride, durationMs: 80),
            new GifAnimationFrame(frame2, width, height, stride, durationMs: 80)
        };

        var gif = GifWriter.WriteAnimationRgba32(width, height, frames, new GifAnimationOptions(loopCount: 0, backgroundRgba: 0x000000FF));

        var packedFields = CollectImagePackedFields(gif);
        Assert.Equal(2, packedFields.Length);
        Assert.All(packedFields, packed => Assert.Equal(0, packed & 0x80));
        Assert.True(GifReader.TryDecodeAnimationFrames(gif, out _, out _, out _, out _));
    }

    private static void FillUniqueColors(byte[] rgba, int offset) {
        var pixelCount = rgba.Length / 4;
        for (var i = 0; i < pixelCount; i++) {
            var value = offset + i;
            var r = (byte)(value & 0xFF);
            var g = (byte)((value >> 8) & 0xFF);
            var b = (byte)((value >> 16) & 0xFF);
            var idx = i * 4;
            rgba[idx] = r;
            rgba[idx + 1] = g;
            rgba[idx + 2] = b;
            rgba[idx + 3] = 255;
        }
    }

    private static byte[] CollectImagePackedFields(ReadOnlySpan<byte> gif) {
        if (gif.Length < 13) throw new InvalidOperationException("GIF data too small.");
        if (gif[0] != (byte)'G' || gif[1] != (byte)'I' || gif[2] != (byte)'F') {
            throw new InvalidOperationException("Not a GIF.");
        }

        var packed = gif[10];
        var gctFlag = (packed & 0x80) != 0;
        var gctSize = 1 << ((packed & 0x07) + 1);
        var offset = 13;
        if (gctFlag) {
            offset += gctSize * 3;
        }

        var packedFields = new System.Collections.Generic.List<byte>();
        while (offset < gif.Length) {
            var block = gif[offset++];
            if (block == 0x3B) break;

            if (block == 0x21) {
                if (offset >= gif.Length) break;
                var label = gif[offset++];
                if (label == 0xF9) {
                    if (offset >= gif.Length) break;
                    var size = gif[offset++];
                    offset += size;
                    if (offset < gif.Length && gif[offset] == 0) offset++;
                    continue;
                }

                if (label == 0xFF) {
                    if (offset >= gif.Length) break;
                    var size = gif[offset++];
                    offset += size;
                    SkipSubBlocks(gif, ref offset);
                    continue;
                }

                if (offset >= gif.Length) break;
                var blockSize = gif[offset++];
                offset += blockSize;
                SkipSubBlocks(gif, ref offset);
                continue;
            }

            if (block == 0x2C) {
                if (offset + 8 >= gif.Length) break;
                offset += 8;
                var packedField = gif[offset++];
                packedFields.Add(packedField);
                var lctFlag = (packedField & 0x80) != 0;
                if (lctFlag) {
                    var lctSize = 1 << ((packedField & 0x07) + 1);
                    offset += lctSize * 3;
                }
                if (offset >= gif.Length) break;
                offset++; // LZW min code size
                SkipSubBlocks(gif, ref offset);
                continue;
            }

            break;
        }

        return packedFields.ToArray();
    }

    private static void SkipSubBlocks(ReadOnlySpan<byte> data, ref int offset) {
        while (offset < data.Length) {
            var size = data[offset++];
            if (size == 0) break;
            offset += size;
        }
    }
}
