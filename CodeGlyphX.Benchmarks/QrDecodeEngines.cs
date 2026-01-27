using System;
using System.Collections.Generic;
using System.Linq;
using CodeGlyphX.Rendering;
#if COMPARE_ZXING
using ZXing;
using ZXing.Common;
#endif

namespace CodeGlyphX.Benchmarks;

internal readonly record struct DecodeEngineResult(bool Success, int Count, string[] Texts);

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
                out _,
                options);
            if (!okAll || decoded.Length == 0) {
                if (QrDecoder.TryDecode(data.Rgba, data.Width, data.Height, data.Stride, PixelFormat.Rgba32, out var single, out _, options)) {
                    decoded = new[] { single };
                } else {
                    decoded = Array.Empty<QrDecoded>();
                }
            }

            if (decoded.Length == 0) {
                return new DecodeEngineResult(false, 0, Array.Empty<string>());
            }

            var texts = decoded
                .Select(d => d.Text)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            return new DecodeEngineResult(texts.Length > 0, decoded.Length, texts);
        }
    }

#if COMPARE_ZXING
    private sealed class ZXingEngine : IQrDecodeEngine {
        private readonly BarcodeReaderGeneric _reader = new() {
            Options = new DecodingOptions { PossibleFormats = new[] { BarcodeFormat.QR_CODE } }
        };

        public string Name => "ZXing.Net";
        public bool IsExternal => true;
        public IReadOnlyList<string> Aliases { get; } = new[] { "zxing", "zxingnet", "zxing.net", "zx" };

        public DecodeEngineResult Decode(QrDecodeScenarioData data, QrPixelDecodeOptions options) {
            var result = _reader.Decode(data.Rgba, data.Width, data.Height, RGBLuminanceSource.BitmapFormat.RGBA32);
            if (result is null || string.IsNullOrWhiteSpace(result.Text)) {
                return new DecodeEngineResult(false, 0, Array.Empty<string>());
            }

            return new DecodeEngineResult(true, 1, new[] { result.Text });
        }
    }
#endif
}
