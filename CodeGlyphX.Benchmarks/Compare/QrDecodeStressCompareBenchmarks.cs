using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
#if COMPARE_ZXING
using ZXing;
using ZXing.Common;
#endif

namespace CodeGlyphX.Benchmarks;

#if BENCH_QUICK
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 1, iterationCount: 3, invocationCount: 1)]
#else
[SimpleJob(RuntimeMoniker.Net80)]
#endif
[MemoryDiagnoser]
[RankColumn]
public class QrDecodeStressCompareBenchmarks
{
    private const string SampleText = "https://github.com/EvotecIT/CodeGlyphX";
#if !BENCH_QUICK
    private byte[] _logoRgba = Array.Empty<byte>();
#endif
    private byte[] _fancyRgba = Array.Empty<byte>();
    private byte[] _resampledRgba = Array.Empty<byte>();
    private byte[] _noQuietRgba = Array.Empty<byte>();
#if !BENCH_QUICK
    private int _logoWidth;
    private int _logoHeight;
#endif
    private int _fancyWidth;
    private int _fancyHeight;
    private int _resampledWidth;
    private int _resampledHeight;
    private int _noQuietWidth;
    private int _noQuietHeight;
    private readonly QrPixelDecodeOptions _balanced = new() { Profile = QrDecodeProfile.Balanced };
    private readonly QrPixelDecodeOptions _robust = new()
    {
        Profile = QrDecodeProfile.Robust,
        AggressiveSampling = true,
        MaxMilliseconds = 800,
        MaxDimension = 1600
    };

#if COMPARE_ZXING
    private readonly BarcodeReaderGeneric _zxingReader = new()
    {
        Options = new DecodingOptions
        {
            PossibleFormats = new[] { BarcodeFormat.QR_CODE },
            TryHarder = true
        }
    };
#endif

    [GlobalSetup]
    public void Setup()
    {
#if !BENCH_QUICK
        BuildLogoSample(out _logoRgba, out _logoWidth, out _logoHeight);
#endif
        BuildFancySample(out _fancyRgba, out _fancyWidth, out _fancyHeight);
        BuildResampledSample(out _resampledRgba, out _resampledWidth, out _resampledHeight);
        BuildNoQuietSample(out _noQuietRgba, out _noQuietWidth, out _noQuietHeight);
    }

#if !BENCH_QUICK
    [Benchmark(Baseline = true, Description = "CodeGlyphX QR Decode (logo)")]
    public bool CodeGlyphX_DecodeLogoRobust()
    {
        return QrDecoder.TryDecode(_logoRgba, _logoWidth, _logoHeight, _logoWidth * 4, PixelFormat.Rgba32, out _, _robust);
    }

#if COMPARE_ZXING
    [Benchmark(Description = "ZXing.Net QR Decode (logo)")]
    public bool ZXing_DecodeLogoTryHarder()
    {
        return _zxingReader.Decode(_logoRgba, _logoWidth, _logoHeight, RGBLuminanceSource.BitmapFormat.RGBA32) is not null;
    }
#endif
#endif

    [Benchmark(Description = "CodeGlyphX QR Decode (fancy)")]
    public bool CodeGlyphX_DecodeFancyRobust()
    {
        return QrDecoder.TryDecode(_fancyRgba, _fancyWidth, _fancyHeight, _fancyWidth * 4, PixelFormat.Rgba32, out _, _robust);
    }

#if COMPARE_ZXING
    [Benchmark(Description = "ZXing.Net QR Decode (fancy)")]
    public bool ZXing_DecodeFancyTryHarder()
    {
        return _zxingReader.Decode(_fancyRgba, _fancyWidth, _fancyHeight, RGBLuminanceSource.BitmapFormat.RGBA32) is not null;
    }
#endif

    [Benchmark(Description = "CodeGlyphX QR Decode (resampled)")]
    public bool CodeGlyphX_DecodeResampledBalanced()
    {
        return QrDecoder.TryDecode(_resampledRgba, _resampledWidth, _resampledHeight, _resampledWidth * 4, PixelFormat.Rgba32, out _, _balanced);
    }

#if COMPARE_ZXING
    [Benchmark(Description = "ZXing.Net QR Decode (resampled)")]
    public bool ZXing_DecodeResampledTryHarder()
    {
        return _zxingReader.Decode(_resampledRgba, _resampledWidth, _resampledHeight, RGBLuminanceSource.BitmapFormat.RGBA32) is not null;
    }
#endif

    [Benchmark(Description = "CodeGlyphX QR Decode (no quiet zone)")]
    public bool CodeGlyphX_DecodeNoQuietRobust()
    {
        return QrDecoder.TryDecode(_noQuietRgba, _noQuietWidth, _noQuietHeight, _noQuietWidth * 4, PixelFormat.Rgba32, out _, _robust);
    }

#if COMPARE_ZXING
    [Benchmark(Description = "ZXing.Net QR Decode (no quiet zone)")]
    public bool ZXing_DecodeNoQuietTryHarder()
    {
        return _zxingReader.Decode(_noQuietRgba, _noQuietWidth, _noQuietHeight, RGBLuminanceSource.BitmapFormat.RGBA32) is not null;
    }
#endif

#if !BENCH_QUICK
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
#endif

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
}
