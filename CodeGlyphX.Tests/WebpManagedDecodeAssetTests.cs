using System;
using System.IO;
using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("WebpTests")]

public sealed class WebpManagedDecodeAssetTests {
    [Theory]
    [InlineData("Assets/DecodingSamples/qr-template-grid.webp", 1536, 864)]
    [InlineData("CodeGlyphX.Website/wwwroot/codeglyphx-qr-icon.webp", 259, 259)]
    public void Webp_ManagedDecode_RepoAssets_Decodes(string relativePath, int expectedWidth, int expectedHeight) {
        var data = ReadRepoFile(relativePath);

        Assert.True(
            TryDecodeManagedWithDiagnostics(data, out var rgba, out var width, out var height, out var reason),
            reason);
        Assert.Equal(expectedWidth, width);
        Assert.Equal(expectedHeight, height);
        Assert.Equal(width * height * 4, rgba.Length);
        Assert.True(HasNonZeroByte(rgba));
    }

    private static bool HasNonZeroByte(byte[] rgba) {
        for (var i = 0; i < rgba.Length; i++) {
            if (rgba[i] != 0) return true;
        }

        return false;
    }

    private static bool TryDecodeManagedWithDiagnostics(
        byte[] data,
        out byte[] rgba,
        out int width,
        out int height,
        out string reason) {
        rgba = Array.Empty<byte>();
        width = 0;
        height = 0;
        reason = string.Empty;

        if (WebpManagedDecoder.TryDecodeRgba32(data, out rgba, out width, out height)) {
            return true;
        }

        if (TryFindChunk(data, FourCcVp8L, out var vp8lPayload)) {
            if (!WebpVp8lDecoder.TryDecodeWithReason(vp8lPayload, out rgba, out width, out height, out reason)) {
                return false;
            }

            return true;
        }

        if (TryFindChunk(data, FourCcVp8, out _)) {
            reason = "Managed VP8 decode failed for lossy payload.";
            return false;
        }

        reason = "No VP8/VP8L payload found in container.";
        return false;
    }

    private static bool TryFindChunk(byte[] data, uint targetFourCc, out ReadOnlySpan<byte> payload) {
        payload = default;
        if (data.Length < 12) return false;
        var riffSize = ReadU32LE(data, 4);
        var riffLimit = data.Length;
        var declaredLimit = 8L + riffSize;
        if (declaredLimit > 0 && declaredLimit < riffLimit) {
            riffLimit = (int)declaredLimit;
        }
        if (riffLimit < 12) return false;

        var offset = 12;
        while (offset + 8 <= riffLimit) {
            var fourCc = ReadU32LE(data, offset);
            var chunkSize = ReadU32LE(data, offset + 4);
            var dataOffset = offset + 8;

            if (chunkSize > int.MaxValue) return false;
            var chunkLength = (int)chunkSize;
            if (dataOffset < 0 || dataOffset > riffLimit) return false;
            if (dataOffset + chunkLength > riffLimit) return false;

            if (fourCc == targetFourCc) {
                payload = data.AsSpan(dataOffset, chunkLength);
                return true;
            }

            var padded = chunkLength + (chunkLength & 1);
            offset = dataOffset + padded;
        }

        return false;
    }

    private static uint ReadU32LE(byte[] data, int offset) {
        if (offset < 0 || offset + 4 > data.Length) return 0;
        return (uint)(data[offset]
            | (data[offset + 1] << 8)
            | (data[offset + 2] << 16)
            | (data[offset + 3] << 24));
    }

    private const uint FourCcVp8L = 0x4C385056; // "VP8L"
    private const uint FourCcVp8 = 0x20385056;  // "VP8 "

    private static byte[] ReadRepoFile(string relativePath) {
        if (string.IsNullOrWhiteSpace(relativePath)) {
            throw new ArgumentException("Path is required.", nameof(relativePath));
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 10 && dir is not null; i++) {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate)) {
                return File.ReadAllBytes(candidate);
            }
            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not locate sample file '{relativePath}'.");
    }
}
