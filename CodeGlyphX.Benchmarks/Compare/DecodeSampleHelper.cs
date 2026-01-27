using System;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Benchmarks;

internal static class DecodeSampleHelper
{
    public static void LoadRgba(string relativePath, out byte[] rgba, out int width, out int height)
    {
        var bytes = RepoFiles.ReadRepoFile(relativePath);
        if (!ImageReader.TryDecodeRgba32(bytes, out rgba, out width, out height))
        {
            throw new InvalidOperationException($"Failed to decode image '{relativePath}'.");
        }
    }
}
