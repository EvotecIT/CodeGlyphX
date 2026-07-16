using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Benchmarks;

#if BENCH_QUICK
[InProcess]
[WarmupCount(1)]
[IterationCount(3)]
[InvocationCount(1)]
#else
[SimpleJob(RuntimeMoniker.Net80)]
#endif
[MemoryDiagnoser]
[RankColumn]
public class MicroQrScanningBenchmarks {
    private byte[] _microPixels = Array.Empty<byte>();
    private byte[] _model2Pixels = Array.Empty<byte>();
    private ImageFrame _microFrame = null!;
    private ScanOptions _scanOptions = null!;
    private int _microWidth;
    private int _microHeight;
    private int _microStride;
    private int _model2Width;
    private int _model2Height;
    private int _model2Stride;

    [GlobalSetup]
    public void Setup() {
        var micro = MicroQrCodeEncoder.EncodeAlphanumeric("MICRO-BENCH", minVersion: 4, maxVersion: 4);
        _microPixels = MatrixPngRenderer.RenderPixels(
            micro.Modules,
            new MatrixPngRenderOptions { ModuleSize = 7, QuietZone = 2 },
            out _microWidth,
            out _microHeight,
            out _microStride);
        _microFrame = ImageFrame.Packed(_microPixels, _microWidth, _microHeight, PixelFormat.Rgba32);
        _scanOptions = new ScanOptions { Formats = new[] { SymbolFormat.MicroQrCode }, MaxSymbols = 1 };
        _model2Pixels = QrEasy.RenderPixels("MODEL2-REJECTION", out _model2Width, out _model2Height, out _model2Stride);

        if (!MicroQrDecoder.TryDecode(
                _microPixels, _microWidth, _microHeight, _microStride, PixelFormat.Rgba32, out var decoded) ||
            decoded.Text != "MICRO-BENCH") {
            throw new InvalidOperationException("Direct Micro QR benchmark validation failed.");
        }
        var scan = SymbolScanner.Scan(_microFrame, _scanOptions);
        if (!scan.IsSuccess || scan.Symbols.Count != 1 || scan.Symbols[0].Text != "MICRO-BENCH") {
            throw new InvalidOperationException("Unified Micro QR benchmark validation failed.");
        }
        if (MicroQrDecoder.TryDecode(
                _model2Pixels, _model2Width, _model2Height, _model2Stride, PixelFormat.Rgba32, out _)) {
            throw new InvalidOperationException("Model 2 rejection benchmark validation failed.");
        }
    }

    [Benchmark(Baseline = true, Description = "Direct Micro QR pixel decoder")]
    public bool DirectMicroQrDecoder() {
        return MicroQrDecoder.TryDecode(
            _microPixels, _microWidth, _microHeight, _microStride, PixelFormat.Rgba32, out _);
    }

    [Benchmark(Description = "Unified Micro QR scanner")]
    public int UnifiedMicroQrScanner() {
        var result = SymbolScanner.Scan(_microFrame, _scanOptions);
        return result.IsSuccess ? result.Symbols.Count : 0;
    }

    [Benchmark(Description = "Reject Model 2 QR as Micro QR")]
    public bool RejectModel2Qr() {
        return MicroQrDecoder.TryDecode(
            _model2Pixels, _model2Width, _model2Height, _model2Stride, PixelFormat.Rgba32, out _);
    }
}
