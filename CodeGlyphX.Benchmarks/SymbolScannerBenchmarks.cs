using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace CodeGlyphX.Benchmarks;

#if BENCH_QUICK
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 1, iterationCount: 3, invocationCount: 1)]
#else
[SimpleJob(RuntimeMoniker.Net80)]
#endif
[MemoryDiagnoser]
[RankColumn]
public class SymbolScannerBenchmarks
{
    private const string Payload = "SCANNER-BENCHMARK";
    private byte[] _rgba = Array.Empty<byte>();
    private ImageFrame _frame = null!;
    private readonly QrPixelDecodeOptions _qrOptions = new() {
        Profile = QrDecodeProfile.Fast,
        MaxScale = 1,
        DisableTransforms = true
    };
    private ScanOptions _scanOptions = null!;
    private int _width;
    private int _height;
    private int _stride;

    [GlobalSetup]
    public void Setup()
    {
        _rgba = QrEasy.RenderPixels(Payload, out _width, out _height, out _stride, new QrEasyOptions {
            ModuleSize = 6,
            QuietZone = 4
        });
        _frame = ImageFrame.Packed(_rgba, _width, _height, PixelFormat.Rgba32);
        _scanOptions = new ScanOptions {
            Formats = new[] { SymbolFormat.QrCode },
            MaxSymbols = 1,
            Qr = _qrOptions
        };

        if (!QrImageDecoder.TryDecodeAll(_rgba, _width, _height, _stride, PixelFormat.Rgba32, _qrOptions, out var direct)
            || direct.Length != 1
            || direct[0].Text != Payload) {
            throw new InvalidOperationException("Direct QR decoder benchmark validation failed.");
        }

        var scanned = SymbolScanner.Scan(_frame, _scanOptions);
        if (!scanned.IsSuccess || scanned.Symbols.Count != 1 || scanned.Symbols[0].Text != Payload) {
            throw new InvalidOperationException("Unified scanner benchmark validation failed.");
        }
    }

    [Benchmark(Baseline = true, Description = "Direct QR image decoder")]
    public int DirectQrDecoder()
    {
        return QrImageDecoder.TryDecodeAll(_rgba, _width, _height, _stride, PixelFormat.Rgba32, _qrOptions, out var decoded)
            ? decoded.Length
            : 0;
    }

    [Benchmark(Description = "Unified symbol scanner")]
    public int UnifiedSymbolScanner()
    {
        var result = SymbolScanner.Scan(_frame, _scanOptions);
        return result.IsSuccess ? result.Symbols.Count : 0;
    }
}
