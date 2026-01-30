using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("WebpTests")]

public sealed class WebpDimensionsFallbackTests {
    [Fact]
    public void TryReadDimensions_Uses_Anmf_When_No_Vp8x() {
        var data = BuildAnmfOnlyContainer(width: 10, height: 12);

        Assert.True(WebpReader.TryReadDimensions(data, out var width, out var height));
        Assert.Equal(10, width);
        Assert.Equal(12, height);
    }

    private static byte[] BuildAnmfOnlyContainer(int width, int height) {
        var payload = new byte[16];
        WriteU24LE(payload, 6, width - 1);
        WriteU24LE(payload, 9, height - 1);

        var totalSize = 12 + 8 + payload.Length;
        var data = new byte[totalSize];
        WriteFourCc(data, 0, "RIFF");
        WriteU32LE(data, 4, totalSize - 8);
        WriteFourCc(data, 8, "WEBP");
        WriteFourCc(data, 12, "ANMF");
        WriteU32LE(data, 16, payload.Length);
        Buffer.BlockCopy(payload, 0, data, 20, payload.Length);
        return data;
    }

    private static void WriteFourCc(byte[] buffer, int offset, string fourCc) {
        buffer[offset] = (byte)fourCc[0];
        buffer[offset + 1] = (byte)fourCc[1];
        buffer[offset + 2] = (byte)fourCc[2];
        buffer[offset + 3] = (byte)fourCc[3];
    }

    private static void WriteU24LE(byte[] buffer, int offset, int value) {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
    }

    private static void WriteU32LE(byte[] buffer, int offset, int value) {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
        buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
    }
}
