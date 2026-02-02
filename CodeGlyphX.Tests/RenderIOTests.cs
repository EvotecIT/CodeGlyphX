using System;
using System.IO;
using System.Threading.Tasks;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class RenderIOTests {
    [Fact]
    public void WriteBinarySafe_Rejects_UnsafeFileNames() {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var data = new byte[] { 1, 2, 3 };

        var root = Path.GetPathRoot(Environment.CurrentDirectory) ?? Path.DirectorySeparatorChar.ToString();
        var rooted = Path.Combine(root, "file.bin");
        var nested = Path.Combine("sub", "file.bin");

        var badNames = new[] { "..", ".", rooted, nested };
        foreach (var name in badNames) {
            Assert.Throws<ArgumentException>(() => RenderIO.WriteBinarySafe(dir, name, data));
        }
    }

    [Fact]
    public void WriteTextSafe_Writes_WithSafeFileName() {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        var path = RenderIO.WriteTextSafe(dir, "ok.txt", "hello", encoding: null);

        Assert.True(File.Exists(path));
        Assert.Equal("hello", File.ReadAllText(path));
    }

    [Fact]
    public void ReadBinary_Rejects_AboveMaxBytes() {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "data.bin");
        File.WriteAllBytes(path, new byte[16]);

        try {
            var ex = Assert.Throws<FormatException>(() => RenderIO.ReadBinary(path, maxBytes: 8));
            Assert.Contains("got 16 B", ex.Message);
            Assert.Contains("max 8 B", ex.Message);
        } finally {
            if (File.Exists(path)) File.Delete(path);
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ReadBinaryAsync_Rejects_AboveMaxBytes() {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "data.bin");
        await File.WriteAllBytesAsync(path, new byte[16]);

        try {
            var ex = await Assert.ThrowsAsync<FormatException>(() => RenderIO.ReadBinaryAsync(path, maxBytes: 8));
            Assert.Contains("got 16 B", ex.Message);
            Assert.Contains("max 8 B", ex.Message);
        } finally {
            if (File.Exists(path)) File.Delete(path);
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }
}
