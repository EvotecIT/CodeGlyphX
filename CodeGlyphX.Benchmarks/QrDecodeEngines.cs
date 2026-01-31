using System;
using System.Collections.Generic;
using System.Linq;
using CodeGlyphX.Rendering;
#if COMPARE_ZXING
using ZXing;
using ZXing.Common;
#endif

namespace CodeGlyphX.Benchmarks;

internal readonly record struct DecodeEngineResult(bool Success, int Count, string[] Texts, QrPixelDecodeInfo? Info);

internal interface IQrDecodeEngine {
    string Name { get; }
    bool IsExternal { get; }
    IReadOnlyList<string> Aliases { get; }
    DecodeEngineResult Decode(QrDecodeScenarioData data, QrPixelDecodeOptions options);
}

internal static class QrDecodeEngines {
    public static IReadOnlyList<IQrDecodeEngine> Create() {
        var engines = new List<IQrDecodeEngine>(4) {
            new CodeGlyphXEngine()
        };
#if COMPARE_ZXING
        engines.Add(new ZXingEngine());
#endif
        return engines;
    }

    private sealed class CodeGlyphXEngine : IQrDecodeEngine {
        public string Name => "CodeGlyphX";
        public bool IsExternal => false;
        public IReadOnlyList<string> Aliases { get; } = new[] { "codeglyphx", "cgx", "self", "ours" };

        public DecodeEngineResult Decode(QrDecodeScenarioData data, QrPixelDecodeOptions options) {
            QrDecoded[] decoded;
            var okAll = QrDecoder.TryDecodeAll(
                data.Rgba,
                data.Width,
                data.Height,
                data.Stride,
                PixelFormat.Rgba32,
                out decoded,
                out var info,
                options);
            if (!okAll || decoded.Length == 0) {
                var okSingle = QrDecoder.TryDecode(
                    data.Rgba,
                    data.Width,
                    data.Height,
                    data.Stride,
                    PixelFormat.Rgba32,
                    out var single,
                    out var infoSingle,
                    options);
                info = infoSingle;
                if (okSingle) {
                    decoded = new[] { single };
                } else {
                    decoded = Array.Empty<QrDecoded>();
                }
            }

            if (decoded.Length == 0) {
                return new DecodeEngineResult(false, 0, Array.Empty<string>(), info);
            }

            var texts = decoded
                .Select(d => d.Text)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            return new DecodeEngineResult(texts.Length > 0, decoded.Length, texts, info);
        }
    }

#if COMPARE_ZXING
    private sealed class ZXingEngine : IQrDecodeEngine {
        private readonly BarcodeReaderGeneric _reader = new();

        public string Name => "ZXing.Net";
        public bool IsExternal => true;
        public IReadOnlyList<string> Aliases { get; } = new[] { "zxing", "zxingnet", "zxing.net", "zx" };

        public DecodeEngineResult Decode(QrDecodeScenarioData data, QrPixelDecodeOptions options) {
            var tryHarder = WantsTryHarder(options);
            _reader.Options = new DecodingOptions {
                PossibleFormats = new[] { BarcodeFormat.QR_CODE },
                TryHarder = tryHarder
            };
            var result = _reader.Decode(data.Rgba, data.Width, data.Height, RGBLuminanceSource.BitmapFormat.RGBA32);
            if (result is null || string.IsNullOrWhiteSpace(result.Text)) {
                return new DecodeEngineResult(false, 0, Array.Empty<string>(), null);
            }

            return new DecodeEngineResult(true, 1, new[] { result.Text }, null);
        }
    }
#endif

    private static bool WantsTryHarder(QrPixelDecodeOptions options) {
        return options.Profile == QrDecodeProfile.Robust ||
               options.AggressiveSampling ||
               options.StylizedSampling ||
               options.AutoCrop ||
               options.EnableTileScan;
    }
}
