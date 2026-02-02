using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CodeGlyphX;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Gif;
using CodeGlyphX.Rendering.Ico;
using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Pam;
using CodeGlyphX.Rendering.Pbm;
using CodeGlyphX.Rendering.Pdf;
using CodeGlyphX.Rendering.Pgm;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Ppm;
using CodeGlyphX.Rendering.Psd;
using CodeGlyphX.Rendering.Tga;
using CodeGlyphX.Rendering.Tiff;
using CodeGlyphX.Rendering.Webp;
using CodeGlyphX.Rendering.Xbm;
using CodeGlyphX.Rendering.Xpm;

namespace CodeGlyphX.Fuzz;

public static class Program {
    private static readonly bool LogExpectedExceptions =
        string.Equals(Environment.GetEnvironmentVariable("CODEGLYPHX_FUZZ_LOG"), "1", StringComparison.OrdinalIgnoreCase);

    private static readonly int TimeoutMs = ReadIntEnv("CODEGLYPHX_FUZZ_TIMEOUT_MS", 2000);
    private static readonly long MaxMemoryBytes = ReadLongEnv("CODEGLYPHX_FUZZ_MAX_MB", 256) * 1024L * 1024L;

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
        var options = ImageDecodeOptions.UltraSafe();
        if (TimeoutMs > 0) options.MaxMilliseconds = TimeoutMs;

        Run("ImageReader.TryDetectFormat", () => _ = ImageReader.TryDetectFormat(data, out _));
        Run("ImageReader.TryReadInfo", () => _ = ImageReader.TryReadInfo(data, out _));
        Run("ImageReader.TryReadAnimationInfo", () => _ = ImageReader.TryReadAnimationInfo(data, out _));
        Run("ImageReader.TryReadPageCount", () => _ = ImageReader.TryReadPageCount(data, out _));
        Run("ImageReader.TryDecodeRgba32", () => _ = ImageReader.TryDecodeRgba32(data, options, out _, out _, out _));
        Run("ImageReader.TryDecodeRgba32Composite", () => _ = ImageReader.TryDecodeRgba32Composite(data, options, out _, out _));
        Run("ImageReader.TryDecodeAnimationFrames", () => _ = ImageReader.TryDecodeAnimationFrames(data, options, out _, out _, out _, out _));
        Run("ImageReader.TryDecodeAnimationCanvasFrames", () => _ = ImageReader.TryDecodeAnimationCanvasFrames(data, options, out _, out _, out _, out _));

        Run("BmpReader.DecodeRgba32", () => _ = BmpReader.DecodeRgba32(data, out _, out _));
        Run("GifReader.DecodeRgba32", () => _ = GifReader.DecodeRgba32(data, out _, out _));
        Run("GifReader.DecodeAnimationFrames", () => _ = GifReader.DecodeAnimationFrames(data, out _, out _, out _));
        Run("GifReader.DecodeAnimationCanvasFrames", () => _ = GifReader.DecodeAnimationCanvasFrames(data, out _, out _, out _));
        Run("IcoReader.DecodeRgba32", () => _ = IcoReader.DecodeRgba32(data, out _, out _));
        Run("JpegReader.DecodeRgba32", () => _ = JpegReader.DecodeRgba32(data, out _, out _, default));
        Run("PamReader.DecodeRgba32", () => _ = PamReader.DecodeRgba32(data, out _, out _));
        Run("PbmReader.DecodeRgba32", () => _ = PbmReader.DecodeRgba32(data, out _, out _));
        Run("PdfReader.DecodeRgba32", () => _ = PdfReader.DecodeRgba32(data, out _, out _));
        Run("PgmReader.DecodeRgba32", () => _ = PgmReader.DecodeRgba32(data, out _, out _));
        Run("PngReader.DecodeRgba32", () => _ = PngReader.DecodeRgba32(data, out _, out _));
        Run("PpmReader.DecodeRgba32", () => _ = PpmReader.DecodeRgba32(data, out _, out _));
        Run("PsdReader.DecodeRgba32", () => _ = PsdReader.DecodeRgba32(data, out _, out _));
        Run("TgaReader.DecodeRgba32", () => _ = TgaReader.DecodeRgba32(data, out _, out _));
        Run("TiffReader.TryDecodeRgba32", () => _ = TiffReader.TryDecodeRgba32(data, out _, out _, out _));
        Run("WebpReader.TryReadDimensions", () => _ = WebpReader.TryReadDimensions(data, out _, out _));
        Run("WebpReader.DecodeRgba32", () => _ = WebpReader.DecodeRgba32(data, out _, out _));
        Run("WebpReader.DecodeAnimationFrames", () => _ = WebpReader.DecodeAnimationFrames(data, out _, out _, out _));
        Run("WebpReader.DecodeAnimationCanvasFrames", () => _ = WebpReader.DecodeAnimationCanvasFrames(data, out _, out _, out _));
        Run("XbmReader.DecodeRgba32", () => _ = XbmReader.DecodeRgba32(data, out _, out _));
        Run("XpmReader.DecodeRgba32", () => _ = XpmReader.DecodeRgba32(data, out _, out _));
    }

    private static void Run(string label, Action action) {
        try {
            RunWithTimeout(label, action);
        } catch (Exception ex) when (IsExpectedException(ex)) {
            LogExpectedException(ex);
        }
    }

    private static void RunWithTimeout(string label, Action action) {
        if (TimeoutMs <= 0) {
            action();
            EnforceMemoryLimit(label);
            return;
        }

        try {
            var task = Task.Run(action);
            if (!task.Wait(TimeoutMs)) {
                Environment.FailFast($"[Fuzz] Timeout after {TimeoutMs}ms in {label}.");
            }
            if (task.Exception is not null) throw task.Exception.GetBaseException();
            EnforceMemoryLimit(label);
        } catch (AggregateException ex) {
            throw ex.GetBaseException();
        }
    }

    private static void EnforceMemoryLimit(string label) {
        if (MaxMemoryBytes <= 0) return;
        var bytes = GC.GetTotalMemory(forceFullCollection: false);
        if (bytes <= MaxMemoryBytes) return;
        throw new InvalidOperationException($"[Fuzz] Memory limit exceeded in {label} (got {FormatBytes(bytes)}, max {FormatBytes(MaxMemoryBytes)}).");
    }

    private static bool IsExpectedException(Exception ex) {
        return ex is FormatException || ex is ArgumentException || ex is InvalidOperationException || ex is IOException;
    }

    private static void LogExpectedException(Exception ex) {
        if (!LogExpectedExceptions) return;
        Console.Error.WriteLine($"[Fuzz] {ex.GetType().Name}: {ex.Message}");
    }

    private static int ReadIntEnv(string name, int fallback) {
        var raw = Environment.GetEnvironmentVariable(name);
        return int.TryParse(raw, out var value) ? value : fallback;
    }

    private static long ReadLongEnv(string name, long fallback) {
        var raw = Environment.GetEnvironmentVariable(name);
        return long.TryParse(raw, out var value) ? value : fallback;
    }

    private static string FormatBytes(long bytes) {
        if (bytes < 1024) return $"{bytes} B";
        var kb = bytes / 1024d;
        if (kb < 1024) return $"{kb:0.#} KB";
        var mb = kb / 1024d;
        return $"{mb:0.#} MB";
    }
}
