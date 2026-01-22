using System;
using System.IO;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Benchmarks;

internal static class DecodeSampleHelper
{
    public static void LoadRgba(string relativePath, out byte[] rgba, out int width, out int height)
    {
        var bytes = ReadRepoFile(relativePath);
        if (!ImageReader.TryDecodeRgba32(bytes, out rgba, out width, out height))
        {
            throw new InvalidOperationException($"Failed to decode image '{relativePath}'.");
        }
    }

    private static byte[] ReadRepoFile(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Path is required.", nameof(relativePath));
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 10 && dir is not null; i++)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return File.ReadAllBytes(candidate);
            }
            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not locate sample file '{relativePath}'.");
    }
}
