using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Benchmarks;

#if BENCH_QUICK
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 1, iterationCount: 3, invocationCount: 1)]
#else
[SimpleJob(RuntimeMoniker.Net80)]
#endif
[MemoryDiagnoser]
[RankColumn]
public class QrDecodeBenchmarks
{
    private const string SampleText = "https://github.com/EvotecIT/CodeGlyphX";
    private byte[] _cleanRgba = Array.Empty<byte>();
    private byte[] _noisyRgba = Array.Empty<byte>();
    private byte[] _screenshotRgba = Array.Empty<byte>();
    private byte[] _antialiasRgba = Array.Empty<byte>();
    private int _cleanWidth;
    private int _cleanHeight;
    private int _noisyWidth;
    private int _noisyHeight;
    private int _screenshotWidth;
    private int _screenshotHeight;
    private int _antialiasWidth;
    private int _antialiasHeight;
    private byte[] _logoRgba = Array.Empty<byte>();
    private byte[] _fancyRgba = Array.Empty<byte>();
    private byte[] _resampledRgba = Array.Empty<byte>();
    private byte[] _noQuietRgba = Array.Empty<byte>();
    private int _logoWidth;
    private int _logoHeight;
    private int _fancyWidth;
    private int _fancyHeight;
    private int _resampledWidth;
    private int _resampledHeight;
    private int _noQuietWidth;
    private int _noQuietHeight;
    private readonly QrPixelDecodeOptions _fast = new() { Profile = QrDecodeProfile.Fast };
    private readonly QrPixelDecodeOptions _balanced = new() { Profile = QrDecodeProfile.Balanced };
    private readonly QrPixelDecodeOptions _robust = new() {
        Profile = QrDecodeProfile.Robust,
        AggressiveSampling = true
    };
    private readonly QrPixelDecodeOptions _robustNoisy = new() {
        Profile = QrDecodeProfile.Robust,
        MaxMilliseconds = 800,
        MaxDimension = 1600,
        AggressiveSampling = true
    };

    [GlobalSetup]
    public void Setup()
    {
        LoadRgba("Assets/DecodingSamples/qr-clean-small.png", out _cleanRgba, out _cleanWidth, out _cleanHeight);
        LoadRgba("Assets/DecodingSamples/qr-noisy-ui.png", out _noisyRgba, out _noisyWidth, out _noisyHeight);
        LoadRgba("Assets/DecodingSamples/qr-screenshot-1.png", out _screenshotRgba, out _screenshotWidth, out _screenshotHeight);
        LoadRgba("Assets/DecodingSamples/qr-dot-aa.png", out _antialiasRgba, out _antialiasWidth, out _antialiasHeight);
        BuildLogoSample(out _logoRgba, out _logoWidth, out _logoHeight);
        BuildFancySample(out _fancyRgba, out _fancyWidth, out _fancyHeight);
        BuildResampledSample(out _resampledRgba, out _resampledWidth, out _resampledHeight);
        BuildNoQuietSample(out _noQuietRgba, out _noQuietWidth, out _noQuietHeight);
    }

    [Benchmark(Description = "QR Decode (clean, fast)")]
    public bool DecodeCleanFast()
    {
        return QrDecoder.TryDecode(_cleanRgba, _cleanWidth, _cleanHeight, _cleanWidth * 4, PixelFormat.Rgba32, out _, _fast);
    }

    [Benchmark(Description = "QR Decode (clean, balanced)")]
    public bool DecodeCleanBalanced()
    {
        return QrDecoder.TryDecode(_cleanRgba, _cleanWidth, _cleanHeight, _cleanWidth * 4, PixelFormat.Rgba32, out _, _balanced);
    }

    [Benchmark(Description = "QR Decode (clean, robust)")]
    public bool DecodeCleanRobust()
    {
        return QrDecoder.TryDecode(_cleanRgba, _cleanWidth, _cleanHeight, _cleanWidth * 4, PixelFormat.Rgba32, out _, _robust);
    }

    [Benchmark(Description = "QR Decode (noisy, robust)")]
    public bool DecodeNoisyRobust()
    {
        return QrDecoder.TryDecode(_noisyRgba, _noisyWidth, _noisyHeight, _noisyWidth * 4, PixelFormat.Rgba32, out _, _robustNoisy);
    }

    [Benchmark(Description = "QR Decode (screenshot, balanced)")]
    public bool DecodeScreenshotBalanced()
    {
        return QrDecoder.TryDecode(_screenshotRgba, _screenshotWidth, _screenshotHeight, _screenshotWidth * 4, PixelFormat.Rgba32, out _, _balanced);
    }

    [Benchmark(Description = "QR Decode (antialias, robust)")]
    public bool DecodeAntialiasRobust()
    {
        return QrDecoder.TryDecode(_antialiasRgba, _antialiasWidth, _antialiasHeight, _antialiasWidth * 4, PixelFormat.Rgba32, out _, _robust);
    }

    [Benchmark(Description = "QR Decode (logo, robust)")]
    public bool DecodeLogoRobust()
    {
        return QrDecoder.TryDecode(_logoRgba, _logoWidth, _logoHeight, _logoWidth * 4, PixelFormat.Rgba32, out _, _robust);
    }

    [Benchmark(Description = "QR Decode (fancy, robust)")]
    public bool DecodeFancyRobust()
    {
        return QrDecoder.TryDecode(_fancyRgba, _fancyWidth, _fancyHeight, _fancyWidth * 4, PixelFormat.Rgba32, out _, _robust);
    }

    [Benchmark(Description = "QR Decode (resampled, balanced)")]
    public bool DecodeResampledBalanced()
    {
        return QrDecoder.TryDecode(_resampledRgba, _resampledWidth, _resampledHeight, _resampledWidth * 4, PixelFormat.Rgba32, out _, _balanced);
    }

    [Benchmark(Description = "QR Decode (no quiet zone, robust)")]
    public bool DecodeNoQuietRobust()
    {
        return QrDecoder.TryDecode(_noQuietRgba, _noQuietWidth, _noQuietHeight, _noQuietWidth * 4, PixelFormat.Rgba32, out _, _robust);
    }

    private static void LoadRgba(string relativePath, out byte[] rgba, out int width, out int height)
    {
        var bytes = ReadRepoFile(relativePath);
        if (!ImageReader.TryDecodeRgba32(bytes, out rgba, out width, out height))
        {
            throw new InvalidOperationException($"Failed to decode image '{relativePath}'.");
        }
    }

    private static void BuildLogoSample(out byte[] rgba, out int width, out int height)
    {
        var logo = LogoBuilder.CreateCirclePng(
            size: 96,
            color: new Rgba32(24, 24, 24, 255),
            accent: new Rgba32(240, 240, 240, 255),
            out _,
            out _);
        var options = QrPresets.Logo(logo);
        var png = QrEasy.RenderPng(SampleText, options);
        DecodePng(png, out rgba, out width, out height);
    }

    private static void BuildFancySample(out byte[] rgba, out int width, out int height)
    {
        var options = new QrEasyOptions { Style = QrRenderStyle.Fancy };
        var png = QrEasy.RenderPng(SampleText, options);
        DecodePng(png, out rgba, out width, out height);
    }

    private static void BuildResampledSample(out byte[] rgba, out int width, out int height)
    {
        var options = new QrEasyOptions { ModuleSize = 8 };
        var png = QrEasy.RenderPng(SampleText, options);
        DecodePng(png, out var baseRgba, out var baseWidth, out var baseHeight);

        var downW = Math.Max(1, (int)Math.Round(baseWidth * 0.62));
        var downH = Math.Max(1, (int)Math.Round(baseHeight * 0.62));
        var down = ResampleBilinear(baseRgba, baseWidth, baseHeight, downW, downH);

        var upW = Math.Max(1, (int)Math.Round(baseWidth * 1.12));
        var upH = Math.Max(1, (int)Math.Round(baseHeight * 1.12));
        var up = ResampleBilinear(down, downW, downH, upW, upH);

        rgba = up;
        width = upW;
        height = upH;
    }

    private static void BuildNoQuietSample(out byte[] rgba, out int width, out int height)
    {
        var options = new QrEasyOptions { QuietZone = 0, ErrorCorrectionLevel = QrErrorCorrectionLevel.H };
        var png = QrEasy.RenderPng(SampleText, options);
        DecodePng(png, out rgba, out width, out height);
    }

    private static void DecodePng(byte[] png, out byte[] rgba, out int width, out int height)
    {
        if (!ImageReader.TryDecodeRgba32(png, out rgba, out width, out height))
        {
            throw new InvalidOperationException("Failed to decode generated QR PNG sample.");
        }
    }

    private static byte[] ResampleBilinear(byte[] src, int srcWidth, int srcHeight, int dstWidth, int dstHeight)
    {
        if (srcWidth <= 0 || srcHeight <= 0 || dstWidth <= 0 || dstHeight <= 0)
        {
            throw new ArgumentOutOfRangeException("Invalid resample dimensions.");
        }

        var dst = new byte[checked(dstWidth * dstHeight * 4)];
        var scaleX = srcWidth / (double)dstWidth;
        var scaleY = srcHeight / (double)dstHeight;

        for (var y = 0; y < dstHeight; y++)
        {
            var sy = (y + 0.5) * scaleY - 0.5;
            if (sy < 0) sy = 0;
            var y0 = (int)sy;
            var y1 = Math.Min(y0 + 1, srcHeight - 1);
            var wy = sy - y0;
            var row0 = y0 * srcWidth * 4;
            var row1 = y1 * srcWidth * 4;

            for (var x = 0; x < dstWidth; x++)
            {
                var sx = (x + 0.5) * scaleX - 0.5;
                if (sx < 0) sx = 0;
                var x0 = (int)sx;
                var x1 = Math.Min(x0 + 1, srcWidth - 1);
                var wx = sx - x0;

                var p00 = row0 + x0 * 4;
                var p10 = row0 + x1 * 4;
                var p01 = row1 + x0 * 4;
                var p11 = row1 + x1 * 4;

                var dstIndex = (y * dstWidth + x) * 4;
                for (var c = 0; c < 4; c++)
                {
                    var v00 = src[p00 + c];
                    var v10 = src[p10 + c];
                    var v01 = src[p01 + c];
                    var v11 = src[p11 + c];

                    var v0 = v00 + (v10 - v00) * wx;
                    var v1 = v01 + (v11 - v01) * wx;
                    var v = v0 + (v1 - v0) * wy;
                    var value = (int)Math.Round(v);
                    if (value < 0) value = 0;
                    else if (value > 255) value = 255;
                    dst[dstIndex + c] = (byte)value;
                }
            }
        }

        return dst;
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
