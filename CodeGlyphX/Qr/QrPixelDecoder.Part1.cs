#if NET8_0_OR_GREATER
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using CodeGlyphX;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelDecoder {
    private readonly struct DecodeBudget {
        public bool Enabled { get; }
        public long Deadline { get; }
        public int MaxMilliseconds { get; }
        private readonly CancellationToken _cancellationToken;
        private readonly bool _hasCancellation;

        public DecodeBudget(int maxMilliseconds, CancellationToken cancellationToken) {
            MaxMilliseconds = maxMilliseconds;
            _cancellationToken = cancellationToken;
            _hasCancellation = cancellationToken.CanBeCanceled;
            if (maxMilliseconds > 0) {
                Enabled = true;
                Deadline = Stopwatch.GetTimestamp() + (long)(maxMilliseconds * (Stopwatch.Frequency / 1000.0));
            } else {
                Enabled = false;
                Deadline = 0;
            }
        }

        public bool IsExpired => (_hasCancellation && _cancellationToken.IsCancellationRequested) ||
                                 (Enabled && Stopwatch.GetTimestamp() > Deadline);

        public bool IsCancelled => _hasCancellation && _cancellationToken.IsCancellationRequested;

        public bool IsNearDeadline(int milliseconds) {
            if (_hasCancellation && _cancellationToken.IsCancellationRequested) return true;
            if (!Enabled) return false;
            var remaining = Deadline - Stopwatch.GetTimestamp();
            if (remaining <= 0) return true;
            return remaining <= (long)(milliseconds * (Stopwatch.Frequency / 1000.0));
        }
    }

    private static int GetBudgetThresholdLimit(DecodeBudget budget) {
        if (!budget.Enabled) return int.MaxValue;
        if (budget.MaxMilliseconds <= 400) return 1;
        if (budget.MaxMilliseconds <= 800) return 3;
        if (budget.MaxMilliseconds <= 1600) return 6;
        return int.MaxValue;
    }

    private readonly struct QrProfileSettings {
        public int MaxScale { get; }
        public int CollectMaxScale { get; }
        public bool AllowTransforms { get; }
        public bool AllowContrastStretch { get; }
        public bool AllowNormalize { get; }
        public bool AllowAdaptiveThreshold { get; }
        public bool AllowBlur { get; }
        public bool AllowExtraThresholds { get; }
        public int MinContrast { get; }
        public bool AggressiveSampling { get; }

        public QrProfileSettings(
            int maxScale,
            int collectMaxScale,
            bool allowTransforms,
            bool allowContrastStretch,
            bool allowNormalize,
            bool allowAdaptiveThreshold,
            bool allowBlur,
            bool allowExtraThresholds,
            int minContrast,
            bool aggressiveSampling) {
            MaxScale = maxScale;
            CollectMaxScale = collectMaxScale;
            AllowTransforms = allowTransforms;
            AllowContrastStretch = allowContrastStretch;
            AllowNormalize = allowNormalize;
            AllowAdaptiveThreshold = allowAdaptiveThreshold;
            AllowBlur = allowBlur;
            AllowExtraThresholds = allowExtraThresholds;
            MinContrast = minContrast;
            AggressiveSampling = aggressiveSampling;
        }
    }

    private static QrProfileSettings GetProfileSettings(QrDecodeProfile profile, int minDim) {
        if (profile == QrDecodeProfile.Fast) {
            return new QrProfileSettings(
                maxScale: 1,
                collectMaxScale: 1,
                allowTransforms: false,
                allowContrastStretch: false,
                allowNormalize: false,
                allowAdaptiveThreshold: false,
                allowBlur: false,
                allowExtraThresholds: false,
                minContrast: 24,
                aggressiveSampling: false);
        }

        if (profile == QrDecodeProfile.Balanced) {
            var maxScale = minDim >= 160 ? 2 : 1;
            var collectScale = minDim >= 160 ? 2 : 1;
            return new QrProfileSettings(
                maxScale,
                collectScale,
                allowTransforms: true,
                allowContrastStretch: true,
                allowNormalize: true,
                allowAdaptiveThreshold: true,
                allowBlur: false,
                allowExtraThresholds: true,
                minContrast: 16,
                aggressiveSampling: false);
        }

        var robustMax = 1;
        if (minDim >= 160) robustMax = 2;
        if (minDim >= 320) robustMax = 3;
        if (minDim >= 640) robustMax = 4;
        if (minDim >= 960) robustMax = 5;

        var collectMax = 1;
        if (minDim >= 160) collectMax = 2;
        if (minDim >= 320) collectMax = 3;

        return new QrProfileSettings(
            robustMax,
            collectMax,
            allowTransforms: true,
            allowContrastStretch: true,
            allowNormalize: true,
            allowAdaptiveThreshold: true,
            allowBlur: true,
            allowExtraThresholds: true,
            minContrast: 8,
            aggressiveSampling: false);
    }

    private static QrProfileSettings ApplyOverrides(QrProfileSettings settings, QrPixelDecodeOptions? options, int scaleStart) {
        if (options is null && scaleStart <= 1) return settings;

        var maxScale = settings.MaxScale;
        var collectMaxScale = settings.CollectMaxScale;
        var allowTransforms = settings.AllowTransforms;
        var allowContrastStretch = settings.AllowContrastStretch;
        var allowNormalize = settings.AllowNormalize;
        var allowAdaptiveThreshold = settings.AllowAdaptiveThreshold;
        var allowBlur = settings.AllowBlur;
        var allowExtraThresholds = settings.AllowExtraThresholds;
        var aggressiveSampling = settings.AggressiveSampling;

        if (options is not null) {
            if (options.MaxScale > 0) {
                maxScale = Math.Min(maxScale, options.MaxScale);
                collectMaxScale = Math.Min(collectMaxScale, options.MaxScale);
            }
            if (options.DisableTransforms) {
                allowTransforms = false;
            }
            if (options.AggressiveSampling) {
                aggressiveSampling = true;
            }
            if (options.MaxMilliseconds > 0) {
                if (options.MaxMilliseconds <= 400) {
                    maxScale = Math.Min(maxScale, 1);
                    collectMaxScale = Math.Min(collectMaxScale, 1);
                    allowTransforms = false;
                    allowContrastStretch = false;
                    allowNormalize = false;
                    allowAdaptiveThreshold = false;
                    allowBlur = false;
                    allowExtraThresholds = false;
                } else if (options.MaxMilliseconds <= 800) {
                    maxScale = Math.Min(maxScale, 2);
                    collectMaxScale = Math.Min(collectMaxScale, 1);
                    allowTransforms = false;
                    allowContrastStretch = false;
                    allowNormalize = false;
                    allowAdaptiveThreshold = false;
                    allowBlur = false;
                }
                if (options.MaxMilliseconds <= 800) {
                    aggressiveSampling = false;
                }
            }
        }

        if (scaleStart > maxScale) maxScale = scaleStart;
        if (scaleStart > collectMaxScale) collectMaxScale = scaleStart;

        if (maxScale == settings.MaxScale &&
            collectMaxScale == settings.CollectMaxScale &&
            allowTransforms == settings.AllowTransforms &&
            allowContrastStretch == settings.AllowContrastStretch &&
            allowNormalize == settings.AllowNormalize &&
            allowAdaptiveThreshold == settings.AllowAdaptiveThreshold &&
            allowBlur == settings.AllowBlur &&
            allowExtraThresholds == settings.AllowExtraThresholds &&
            aggressiveSampling == settings.AggressiveSampling) {
            return settings;
        }

        return new QrProfileSettings(
            maxScale,
            collectMaxScale,
            allowTransforms,
            allowContrastStretch,
            allowNormalize,
            allowAdaptiveThreshold,
            allowBlur,
            allowExtraThresholds,
            settings.MinContrast,
            aggressiveSampling);
    }

    private static int GetScaleStart(QrPixelDecodeOptions? options, int width, int height) {
        if (options is null || options.MaxDimension <= 0 && options.MaxMilliseconds <= 0) return 1;
        var maxDim = Math.Max(width, height);
        var targetMax = options.MaxDimension > 0 ? options.MaxDimension : int.MaxValue;
        if (options.MaxMilliseconds > 0) {
            if (options.MaxMilliseconds <= 400) targetMax = Math.Min(targetMax, 400);
            else if (options.MaxMilliseconds <= 800) targetMax = Math.Min(targetMax, 600);
        }
        if (targetMax == int.MaxValue) return 1;
        if (maxDim <= targetMax) return 1;
        var scale = (int)Math.Ceiling(maxDim / (double)targetMax);
        return Math.Clamp(scale, 1, 8);
    }

    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded result) {
        return TryDecode(pixels, width, height, stride, fmt, options: null, accept: null, cancellationToken: default, out result, out _);
    }

    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        return TryDecode(pixels, width, height, stride, fmt, options: null, accept: null, cancellationToken: default, out result, out diagnostics);
    }

    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, Func<QrDecoded, bool>? accept, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        return TryDecode(pixels, width, height, stride, fmt, options: null, accept, cancellationToken: default, out result, out diagnostics);
    }

    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, QrPixelDecodeOptions? options, out QrDecoded result) {
        return TryDecode(pixels, width, height, stride, fmt, options, accept: null, cancellationToken: default, out result, out _);
    }

    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, QrPixelDecodeOptions? options, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        return TryDecode(pixels, width, height, stride, fmt, options, accept: null, cancellationToken: default, out result, out diagnostics);
    }

    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded result) {
        return TryDecode(pixels, width, height, stride, fmt, options, accept: null, cancellationToken, out result, out _);
    }

    public static bool TryDecode(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        return TryDecode(pixels, width, height, stride, fmt, options, accept: null, cancellationToken, out result, out diagnostics);
    }

    public static bool TryDecode(
        ReadOnlySpan<byte> pixels,
        int width,
        int height,
        int stride,
        PixelFormat fmt,
        QrPixelDecodeOptions? options,
        Func<QrDecoded, bool>? accept,
        CancellationToken cancellationToken,
        out QrDecoded result,
        out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (width <= 0 || height <= 0 || stride < width * 4 || pixels.Length < (height - 1) * stride + width * 4) {
            diagnostics = new QrPixelDecodeDiagnostics(
                scale: 0,
                threshold: 0,
                invert: false,
                candidateCount: 0,
                candidateTriplesTried: 0,
                dimension: 0,
                moduleDiagnostics: new global::CodeGlyphX.QrDecodeDiagnostics(global::CodeGlyphX.QrDecodeFailure.InvalidInput));
            return false;
        }

        var best = default(QrPixelDecodeDiagnostics);
        var scaleStart = GetScaleStart(options, width, height);
        var profile = options?.Profile ?? QrDecodeProfile.Robust;
        if (options?.MaxMilliseconds > 0) {
            if (options.MaxMilliseconds <= 800) profile = QrDecodeProfile.Fast;
            else if (options.MaxMilliseconds <= 1600 && profile == QrDecodeProfile.Robust) profile = QrDecodeProfile.Balanced;
        }
        var settings = GetProfileSettings(profile, Math.Min(width, height));
        settings = ApplyOverrides(settings, options, scaleStart);
        var budgetMilliseconds = options?.BudgetMilliseconds > 0 ? options.BudgetMilliseconds : options?.MaxMilliseconds ?? 0;
        var budget = new DecodeBudget(budgetMilliseconds, cancellationToken);

        if (budget.IsExpired) {
            var failure = budget.IsCancelled ? global::CodeGlyphX.QrDecodeFailure.Cancelled : global::CodeGlyphX.QrDecodeFailure.Payload;
            diagnostics = new QrPixelDecodeDiagnostics(
                scale: scaleStart,
                threshold: 0,
                invert: false,
                candidateCount: 0,
                candidateTriplesTried: 0,
                dimension: 0,
                moduleDiagnostics: new global::CodeGlyphX.QrDecodeDiagnostics(failure));
            return false;
        }

        // Prefer a full finder-pattern based decode (robust to extra background/noise).
        if (TryDecodeAtScale(pixels, width, height, stride, fmt, scaleStart, settings, budget, accept, out result, out var diag1)) {
            diagnostics = diag1;
            return true;
        }
        best = Better(best, diag1);

        for (var scale = scaleStart + 1; scale <= settings.MaxScale; scale++) {
            if (budget.IsExpired) break;
            if (TryDecodeAtScale(pixels, width, height, stride, fmt, scale, settings, budget, accept, out result, out var diagScale)) {
                diagnostics = diagScale;
                return true;
            }
            best = Better(best, diagScale);
        }

        if (budget.IsCancelled) {
            diagnostics = new QrPixelDecodeDiagnostics(
                scale: scaleStart,
                threshold: 0,
                invert: false,
                candidateCount: 0,
                candidateTriplesTried: 0,
                dimension: 0,
                moduleDiagnostics: new global::CodeGlyphX.QrDecodeDiagnostics(global::CodeGlyphX.QrDecodeFailure.Cancelled));
            return false;
        }

        diagnostics = best;
        return false;
    }

    public static bool TryDecodeAll(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded[] results) {
        return TryDecodeAll(pixels, width, height, stride, fmt, options: null, accept: null, cancellationToken: default, out results);
    }

    public static bool TryDecodeAll(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, out QrDecoded[] results, out QrPixelDecodeDiagnostics diagnostics) {
        results = Array.Empty<QrDecoded>();
        diagnostics = default;

        if (width <= 0 || height <= 0 || stride < width * 4 || pixels.Length < (height - 1) * stride + width * 4) {
            diagnostics = new QrPixelDecodeDiagnostics(
                scale: 0,
                threshold: 0,
                invert: false,
                candidateCount: 0,
                candidateTriplesTried: 0,
                dimension: 0,
                moduleDiagnostics: new global::CodeGlyphX.QrDecodeDiagnostics(global::CodeGlyphX.QrDecodeFailure.InvalidInput));
            return false;
        }

        var ok = TryDecodeAll(pixels, width, height, stride, fmt, options: null, accept: null, cancellationToken: default, out results);
        if (!ok) {
            diagnostics = new QrPixelDecodeDiagnostics(
                scale: 1,
                threshold: 0,
                invert: false,
                candidateCount: 0,
                candidateTriplesTried: 0,
                dimension: 0,
                moduleDiagnostics: new global::CodeGlyphX.QrDecodeDiagnostics(global::CodeGlyphX.QrDecodeFailure.Payload));
            return false;
        }

        var first = results.Length > 0 ? results[0] : null;
        var dimension = first is null ? 0 : (first.Version * 4 + 17);
        var moduleDiag = first is null
            ? new global::CodeGlyphX.QrDecodeDiagnostics(global::CodeGlyphX.QrDecodeFailure.Payload)
            : new global::CodeGlyphX.QrDecodeDiagnostics(
                global::CodeGlyphX.QrDecodeFailure.None,
                first.Version,
                first.ErrorCorrectionLevel,
                first.Mask,
                formatBestDistance: -1);

        diagnostics = new QrPixelDecodeDiagnostics(
            scale: 1,
            threshold: 0,
            invert: false,
            candidateCount: results.Length,
            candidateTriplesTried: 0,
            dimension: dimension,
            moduleDiagnostics: moduleDiag);
        return true;
    }

    public static bool TryDecodeAll(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, QrPixelDecodeOptions? options, out QrDecoded[] results) {
        return TryDecodeAll(pixels, width, height, stride, fmt, options, accept: null, cancellationToken: default, out results);
    }

    public static bool TryDecodeAll(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, QrPixelDecodeOptions? options, out QrDecoded[] results, out QrPixelDecodeDiagnostics diagnostics) {
        results = Array.Empty<QrDecoded>();
        diagnostics = default;

        if (width <= 0 || height <= 0 || stride < width * 4 || pixels.Length < (height - 1) * stride + width * 4) {
            diagnostics = new QrPixelDecodeDiagnostics(
                scale: 0,
                threshold: 0,
                invert: false,
                candidateCount: 0,
                candidateTriplesTried: 0,
                dimension: 0,
                moduleDiagnostics: new global::CodeGlyphX.QrDecodeDiagnostics(global::CodeGlyphX.QrDecodeFailure.InvalidInput));
            return false;
        }

        var ok = TryDecodeAll(pixels, width, height, stride, fmt, options, accept: null, cancellationToken: default, out results);
        if (!ok) {
            diagnostics = new QrPixelDecodeDiagnostics(
                scale: 1,
                threshold: 0,
                invert: false,
                candidateCount: 0,
                candidateTriplesTried: 0,
                dimension: 0,
                moduleDiagnostics: new global::CodeGlyphX.QrDecodeDiagnostics(global::CodeGlyphX.QrDecodeFailure.Payload));
            return false;
        }

        var first = results.Length > 0 ? results[0] : null;
        var dimension = first is null ? 0 : (first.Version * 4 + 17);
        var moduleDiag = first is null
            ? new global::CodeGlyphX.QrDecodeDiagnostics(global::CodeGlyphX.QrDecodeFailure.Payload)
            : new global::CodeGlyphX.QrDecodeDiagnostics(
                global::CodeGlyphX.QrDecodeFailure.None,
                first.Version,
                first.ErrorCorrectionLevel,
                first.Mask,
                formatBestDistance: -1);

        diagnostics = new QrPixelDecodeDiagnostics(
            scale: 1,
            threshold: 0,
            invert: false,
            candidateCount: results.Length,
            candidateTriplesTried: 0,
            dimension: dimension,
            moduleDiagnostics: moduleDiag);
        return true;
    }

    public static bool TryDecodeAll(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded[] results) {
        return TryDecodeAll(pixels, width, height, stride, fmt, options, accept: null, cancellationToken, out results);
    }

    public static bool TryDecodeAll(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, QrPixelDecodeOptions? options, CancellationToken cancellationToken, out QrDecoded[] results, out QrPixelDecodeDiagnostics diagnostics) {
        results = Array.Empty<QrDecoded>();
        diagnostics = default;

        if (width <= 0 || height <= 0 || stride < width * 4 || pixels.Length < (height - 1) * stride + width * 4) {
            diagnostics = new QrPixelDecodeDiagnostics(
                scale: 0,
                threshold: 0,
                invert: false,
                candidateCount: 0,
                candidateTriplesTried: 0,
                dimension: 0,
                moduleDiagnostics: new global::CodeGlyphX.QrDecodeDiagnostics(global::CodeGlyphX.QrDecodeFailure.InvalidInput));
            return false;
        }

        var ok = TryDecodeAll(pixels, width, height, stride, fmt, options, accept: null, cancellationToken, out results);
        if (!ok) {
            var failure = cancellationToken.IsCancellationRequested
                ? global::CodeGlyphX.QrDecodeFailure.Cancelled
                : global::CodeGlyphX.QrDecodeFailure.Payload;
            diagnostics = new QrPixelDecodeDiagnostics(
                scale: 1,
                threshold: 0,
                invert: false,
                candidateCount: 0,
                candidateTriplesTried: 0,
                dimension: 0,
                moduleDiagnostics: new global::CodeGlyphX.QrDecodeDiagnostics(failure));
            return false;
        }

        var first = results.Length > 0 ? results[0] : null;
        var dimension = first is null ? 0 : (first.Version * 4 + 17);
        var moduleDiag = first is null
            ? new global::CodeGlyphX.QrDecodeDiagnostics(global::CodeGlyphX.QrDecodeFailure.Payload)
            : new global::CodeGlyphX.QrDecodeDiagnostics(
                global::CodeGlyphX.QrDecodeFailure.None,
                first.Version,
                first.ErrorCorrectionLevel,
                first.Mask,
                formatBestDistance: -1);

        diagnostics = new QrPixelDecodeDiagnostics(
            scale: 1,
            threshold: 0,
            invert: false,
            candidateCount: results.Length,
            candidateTriplesTried: 0,
            dimension: dimension,
            moduleDiagnostics: moduleDiag);
        return true;
    }

    internal static bool TryDecodeAll(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, QrPixelDecodeOptions? options, Func<QrDecoded, bool>? accept, CancellationToken cancellationToken, out QrDecoded[] results) {
        return TryDecodeAll(pixels, width, height, stride, fmt, options, accept, cancellationToken, allowTileScan: true, out results);
    }

    private static bool TryDecodeAll(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, QrPixelDecodeOptions? options, Func<QrDecoded, bool>? accept, CancellationToken cancellationToken, bool allowTileScan, out QrDecoded[] results) {
        results = Array.Empty<QrDecoded>();

        if (width <= 0 || height <= 0) return false;
        if (stride < width * 4) return false;
        if (pixels.Length < (height - 1) * stride + width * 4) return false;

        var scaleStart = GetScaleStart(options, width, height);
        var profile = options?.Profile ?? QrDecodeProfile.Robust;
        if (options?.MaxMilliseconds > 0) {
            if (options.MaxMilliseconds <= 800) profile = QrDecodeProfile.Fast;
            else if (options.MaxMilliseconds <= 1600 && profile == QrDecodeProfile.Robust) profile = QrDecodeProfile.Balanced;
        }
        var settings = GetProfileSettings(profile, Math.Min(width, height));
        settings = ApplyOverrides(settings, options, scaleStart);
        var budgetMilliseconds = options?.BudgetMilliseconds > 0 ? options.BudgetMilliseconds : options?.MaxMilliseconds ?? 0;
        var enableTileScan = allowTileScan && options?.EnableTileScan == true;
        var tileBudgetMs = 0;
        var baseBudgetMs = budgetMilliseconds;
        if (enableTileScan && budgetMilliseconds > 0) {
            tileBudgetMs = Math.Max(300, budgetMilliseconds / 3);
            baseBudgetMs = Math.Max(300, budgetMilliseconds - tileBudgetMs);
        }
        var budget = new DecodeBudget(baseBudgetMs, cancellationToken);
        if (budget.IsExpired || budget.IsNearDeadline(120)) return false;
        DecodeBudget tileBudget = default;
        var useTileBudget = enableTileScan && budgetMilliseconds > 0;

        Func<bool>? shouldStop = budget.Enabled ? () => budget.IsNearDeadline(120) : null;
        var pool = new QrGrayImagePool();
        try {
            if (!QrGrayImage.TryCreate(pixels, width, height, stride, fmt, scale: scaleStart, settings.MinContrast, shouldStop, pool, out var baseImage)) {
                return false;
            }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            using var list = new PooledList<QrDecoded>(4);

            CollectAllFromImage(baseImage, settings, list, seen, accept, budget, pool);
            if (budget.IsExpired) {
                if (list.Count == 0) return false;
                results = list.ToArray();
                return true;
            }

            var skipExtraPasses = enableTileScan && list.Count > 0;
            if (!skipExtraPasses) {
                var range = baseImage.Max - baseImage.Min;
                if (settings.AllowContrastStretch && range < 48) {
                    var stretched = baseImage.WithContrastStretch(48, pool);
                    if (!ReferenceEquals(stretched.Gray, baseImage.Gray)) {
                        CollectAllFromImage(stretched, settings, list, seen, accept, budget, pool);
                        if (budget.IsExpired) {
                            if (list.Count == 0) return false;
                            results = list.ToArray();
                            return true;
                        }
                    }
                }

                for (var scale = scaleStart + 1; scale <= settings.CollectMaxScale; scale++) {
                    if (budget.IsExpired) break;
                    CollectAllAtScale(pixels, width, height, stride, fmt, scale, settings, list, seen, accept, budget, pool);
                }
            }
            if (enableTileScan) {
                if (useTileBudget) {
                    tileBudget = new DecodeBudget(tileBudgetMs, cancellationToken);
                } else {
                    tileBudget = budget;
                }
                if (tileBudget.IsExpired) {
                    if (list.Count == 0) return false;
                    results = list.ToArray();
                    return true;
                }
                var grid = options?.TileGrid > 0 ? options.TileGrid : (Math.Max(width, height) >= 900 ? 3 : 2);
                if (grid < 2) grid = 2;
                if (grid > 4) grid = 4;

                var pad = Math.Max(8, Math.Min(width, height) / 40);
                var tileW = width / grid;
                var tileH = height / grid;
                for (var ty = 0; ty < grid; ty++) {
                    for (var tx = 0; tx < grid; tx++) {
                        if (tileBudget.IsExpired) break;
                        var x0 = tx * tileW;
                        var y0 = ty * tileH;
                        var x1 = (tx == grid - 1) ? width : (tx + 1) * tileW;
                        var y1 = (ty == grid - 1) ? height : (ty + 1) * tileH;

                        x0 = Math.Max(0, x0 - pad);
                        y0 = Math.Max(0, y0 - pad);
                        x1 = Math.Min(width, x1 + pad);
                        y1 = Math.Min(height, y1 + pad);

                        var tw = x1 - x0;
                        var th = y1 - y0;
                        if (tw < 48 || th < 48) continue;

                        var tileStride = tw * 4;
                        var len = tileStride * th;
                        var buffer = ArrayPool<byte>.Shared.Rent(len);
                        try {
                            var tileSpan = buffer.AsSpan(0, len);
                            for (var y = 0; y < th; y++) {
                                if (tileBudget.IsExpired) break;
                                var srcIndex = (y0 + y) * stride + x0 * 4;
                                pixels.Slice(srcIndex, tileStride).CopyTo(tileSpan.Slice(y * tileStride, tileStride));
                            }
                            if (tileBudget.IsExpired) break;

                            if (tileBudget.IsNearDeadline(120)) break;
                            if (TryDecode(tileSpan, tw, th, tileStride, fmt, options, accept, cancellationToken, out var decoded, out _)) {
                                AddResult(list, seen, decoded, accept);
                            }
                        } finally {
                            ArrayPool<byte>.Shared.Return(buffer);
                        }
                    }
                    if (tileBudget.IsExpired) break;
                }
            }

            if (list.Count == 0) return false;
            results = list.ToArray();
            return true;
        } finally {
            pool.ReturnAll();
        }
    }

}
#endif
