using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Gif;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Tiff;
using CodeGlyphX.Rendering.Webp;

namespace CodeGlyphX.Fuzz;

public static class Program {
    public static int Main(string[] args) {
        var data = ReadInput(args);
        if (data.Length == 0) return 0;

        RunFuzz(data);
        return 0;
    }

    private static byte[] ReadInput(string[] args) {
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])) {
            return File.ReadAllBytes(args[0]);
        }

        using var stdin = Console.OpenStandardInput();
        using var ms = new MemoryStream();
        stdin.CopyTo(ms);
        return ms.ToArray();
    }

    private static void RunFuzz(byte[] data) {
        Try(() => _ = ImageReader.TryReadInfo(data, out _));
        Try(() => _ = ImageReader.TryDecodeRgba32(data, out _, out _, out _));
        Try(() => _ = PngReader.DecodeRgba32(data, out _, out _));
        Try(() => _ = GifReader.DecodeRgba32(data, out _, out _));
        Try(() => _ = TiffReader.TryDecodeRgba32(data, out _, out _, out _));
        Try(() => _ = WebpReader.TryReadDimensions(data, out _, out _));
        Try(() => _ = WebpReader.DecodeRgba32(data, out _, out _));
    }

    private static void Try(Action action) {
        try {
            action();
        } catch (FormatException) {
        } catch (ArgumentException) {
        } catch (InvalidOperationException) {
        } catch (IOException) {
        }
    }
}
