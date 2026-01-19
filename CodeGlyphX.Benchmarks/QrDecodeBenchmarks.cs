using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Benchmarks;

[SimpleJob(RuntimeMoniker.Net80, warmupCount: 1, iterationCount: 3, invocationCount: 1)]
[MemoryDiagnoser]
[RankColumn]
public class QrDecodeBenchmarks
{
    private byte[] _cleanRgba = Array.Empty<byte>();
    private byte[] _noisyRgba = Array.Empty<byte>();
    private int _cleanWidth;
    private int _cleanHeight;
    private int _noisyWidth;
    private int _noisyHeight;
    private readonly QrPixelDecodeOptions _robust = new() {
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
    }

    [Benchmark(Description = "QR Decode (clean)")]
    public bool DecodeClean()
    {
        return QrDecoder.TryDecode(_cleanRgba, _cleanWidth, _cleanHeight, _cleanWidth * 4, PixelFormat.Rgba32, out _);
    }

    [Benchmark(Description = "QR Decode (noisy, robust)")]
    public bool DecodeNoisyRobust()
    {
        return QrDecoder.TryDecode(_noisyRgba, _noisyWidth, _noisyHeight, _noisyWidth * 4, PixelFormat.Rgba32, out _, _robust);
    }

    private static void LoadRgba(string relativePath, out byte[] rgba, out int width, out int height)
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
