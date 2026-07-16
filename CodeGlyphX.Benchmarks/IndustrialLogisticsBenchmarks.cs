using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

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
public class IndustrialLogisticsBenchmarks {
    private const string Payload = "LOT-2026-0042";
    private BitMatrix _rmQr = null!;
    private BitMatrix _maxiCode = null!;
    private BitMatrix _dotCode = null!;
    private BitMatrix _hanXin = null!;
    private BitMatrix _composite = null!;

    [GlobalSetup]
    public void Setup() {
        _rmQr = RmQrCodeEncoder.EncodeText(Payload).Modules;
        _maxiCode = MaxiCodeEncoder.EncodeText(Payload).Modules;
        _dotCode = DotCodeEncoder.EncodeText(Payload).Modules;
        _hanXin = HanXinEncoder.EncodeText(Payload).Modules;
        _composite = Gs1CompositeEncoder.Encode("(01)09506000134352", "(21)ABC123").Modules;

        if (!RmQrDecoder.TryDecode(_rmQr, out var rmQr) || rmQr.Text != Payload ||
            !MaxiCodeDecoder.TryDecode(_maxiCode, out var maxiCode) || maxiCode != Payload ||
            !DotCodeDecoder.TryDecode(_dotCode, out var dotCode) || dotCode != Payload ||
            !HanXinDecoder.TryDecode(_hanXin, out var hanXin) || hanXin != Payload ||
            !Gs1CompositeDecoder.TryDecode(_composite, out var composite) || composite.CompositeText != Gs1.ElementString("(21)ABC123")) {
            throw new InvalidOperationException("Industrial/logistics benchmark validation failed.");
        }
    }

    [Benchmark(Description = "rMQR encode")]
    public RmQrCode EncodeRmQr() => RmQrCodeEncoder.EncodeText(Payload);

    [Benchmark(Description = "rMQR decode")]
    public bool DecodeRmQr() => RmQrDecoder.TryDecode(_rmQr, out _);

    [Benchmark(Description = "MaxiCode encode")]
    public MaxiCodeSymbol EncodeMaxiCode() => MaxiCodeEncoder.EncodeText(Payload);

    [Benchmark(Description = "MaxiCode decode")]
    public bool DecodeMaxiCode() => MaxiCodeDecoder.TryDecode(_maxiCode, out _);

    [Benchmark(Description = "DotCode encode")]
    public DotCodeSymbol EncodeDotCode() => DotCodeEncoder.EncodeText(Payload);

    [Benchmark(Description = "DotCode decode")]
    public bool DecodeDotCode() => DotCodeDecoder.TryDecode(_dotCode, out _);

    [Benchmark(Description = "Han Xin encode")]
    public HanXinSymbol EncodeHanXin() => HanXinEncoder.EncodeText(Payload);

    [Benchmark(Description = "Han Xin decode")]
    public bool DecodeHanXin() => HanXinDecoder.TryDecode(_hanXin, out _);

    [Benchmark(Description = "GS1 Composite encode")]
    public Gs1CompositeSymbol EncodeGs1Composite() =>
        Gs1CompositeEncoder.Encode("(01)09506000134352", "(21)ABC123");

    [Benchmark(Description = "GS1 Composite decode")]
    public bool DecodeGs1Composite() => Gs1CompositeDecoder.TryDecode(_composite, out _);
}
