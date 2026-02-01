using System;
using System.IO;
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
}
