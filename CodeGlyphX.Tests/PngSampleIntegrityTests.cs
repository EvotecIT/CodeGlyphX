using System;
using System.Buffers.Binary;
using System.IO;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class PngSampleIntegrityTests {
    // Some tooling (optimizers/audits) uses strict PNG parsing and will reject bad CRCs.
    // Our decoding tests may still pass on platforms/decoders that ignore CRC, so validate
    // the sample corpus explicitly to avoid "works locally, fails in CI/pipeline" drift.
    [Fact]
    public void PngSamples_AreWellFormed_AndHaveValidCrc() {
        var repoRoot = FindRepoRoot();
        var samplesDir = Path.Combine(repoRoot, "Assets", "DecodingSamples");
        Assert.True(Directory.Exists(samplesDir), $"Missing samples directory: {samplesDir}");

        foreach (var file in Directory.EnumerateFiles(samplesDir, "*.png", SearchOption.TopDirectoryOnly)) {
            var bytes = File.ReadAllBytes(file);
            Assert.True(IsPng(bytes), $"Not a PNG file: {file}");
            Assert.True(HasValidPngChunkCrcs(bytes), $"PNG CRC validation failed: {file}");
        }
    }

    private static string FindRepoRoot() {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 12 && dir is not null; i++) {
            var candidate = Path.Combine(dir.FullName, "Assets", "DecodingSamples");
            if (Directory.Exists(candidate)) return dir.FullName;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate repo root from test base directory.");
    }

    private static bool IsPng(ReadOnlySpan<byte> bytes) {
        // PNG signature: 89 50 4E 47 0D 0A 1A 0A
        return bytes.Length >= 8
               && bytes[0] == 0x89
               && bytes[1] == 0x50
               && bytes[2] == 0x4E
               && bytes[3] == 0x47
               && bytes[4] == 0x0D
               && bytes[5] == 0x0A
               && bytes[6] == 0x1A
               && bytes[7] == 0x0A;
    }

    private static bool HasValidPngChunkCrcs(ReadOnlySpan<byte> bytes) {
        if (!IsPng(bytes)) return false;

        var offset = 8;
        while (true) {
            if (offset + 12 > bytes.Length) return false; // len + type + crc (min)

            var length = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(offset, 4));
            offset += 4;

            var type = bytes.Slice(offset, 4);
            offset += 4;

            if (offset + length + 4 > bytes.Length) return false;

            var data = bytes.Slice(offset, (int)length);
            offset += (int)length;

            var expectedCrc = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(offset, 4));
            offset += 4;

            var crcInput = new byte[4 + data.Length];
            type.CopyTo(crcInput.AsSpan(0, 4));
            data.CopyTo(crcInput.AsSpan(4));
            var actualCrc = Crc32(crcInput);
            if (actualCrc != expectedCrc) return false;

            // IEND must be last
            if (type[0] == (byte)'I' && type[1] == (byte)'E' && type[2] == (byte)'N' && type[3] == (byte)'D') {
                return offset == bytes.Length;
            }
        }
    }

    private static uint Crc32(ReadOnlySpan<byte> data) {
        // Standard CRC-32 (IEEE 802.3), polynomial 0xEDB88320.
        var crc = 0xFFFF_FFFFu;
        foreach (var b in data) {
            crc ^= b;
            for (var i = 0; i < 8; i++) {
                var mask = (uint)-(int)(crc & 1);
                crc = (crc >> 1) ^ (0xEDB8_8320u & mask);
            }
        }
        return ~crc;
    }
}
