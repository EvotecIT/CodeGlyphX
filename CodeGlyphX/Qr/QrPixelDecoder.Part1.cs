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
using CodeGlyphX.Internal;

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
        public bool CanCancel => _hasCancellation;

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
        public bool StylizedSampling { get; }

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
            bool aggressiveSampling,
            bool stylizedSampling) {
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
            StylizedSampling = stylizedSampling;
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
                aggressiveSampling: false,
                stylizedSampling: false);
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
                aggressiveSampling: false,
                stylizedSampling: false);
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
            aggressiveSampling: false,
            stylizedSampling: false);
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
        var stylizedSampling = settings.StylizedSampling;
        var minContrast = settings.MinContrast;

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
                minContrast = Math.Max(4, minContrast - 4);
                if (collectMaxScale < maxScale) {
                    collectMaxScale = Math.Min(maxScale, collectMaxScale + 2);
                }
            }
            if (options.StylizedSampling) {
                stylizedSampling = true;
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
                if (options.MaxMilliseconds <= 800 && !options.AggressiveSampling) {
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
            aggressiveSampling == settings.AggressiveSampling &&
            stylizedSampling == settings.StylizedSampling &&
            minContrast == settings.MinContrast) {
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
            minContrast,
            aggressiveSampling,
            stylizedSampling);
    }

    private static int GetScaleStart(QrPixelDecodeOptions? options, int width, int height) {
        if (options is null || options.MaxDimension <= 0 && options.MaxMilliseconds <= 0) return 1;
        var maxDim = Math.Max(width, height);
        var targetMax = options.MaxDimension > 0 ? options.MaxDimension : int.MaxValue;
        if (options.MaxMilliseconds > 0) {
            if (options.MaxMilliseconds <= 400) targetMax = Math.Min(targetMax, 400);
            else if (options.MaxMilliseconds <= 800) {
                var budgetMax = options.AggressiveSampling || options.Profile == QrDecodeProfile.Robust ? 1000 : 600;
                targetMax = Math.Min(targetMax, budgetMax);
            }
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

        if (options?.AggressiveSampling == true && scaleStart > 1 && !budget.IsNearDeadline(200)) {
            if (TryDecodeAtScale(pixels, width, height, stride, fmt, scale: 1, settings, budget, accept, out result, out var diagFull)) {
                diagnostics = diagFull;
                return true;
            }
            best = Better(best, diagFull);
        }

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
        if (options?.AggressiveSampling == true && !budget.IsNearDeadline(300) && Math.Max(width, height) <= 900) {
            if (TryDecodeUpscaled(pixels, width, height, stride, fmt, profile, options, accept, cancellationToken, budget, out result, out var diagUpscaled)) {
                diagnostics = diagUpscaled;
                return true;
            }
            diagnostics = Better(diagnostics, diagUpscaled);
        }
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
            var seen = new HashSet<byte[]>(ByteArrayComparer.Instance);
            using var list = new PooledList<QrDecoded>(4);

            CollectAllFromImage(baseImage, settings, list, seen, accept, budget, pool);
            if (budget.IsExpired) {
                if (list.Count == 0) return false;
                results = list.ToArray();
                return true;
            }

            if (options?.AggressiveSampling == true && scaleStart > 1 && list.Count == 0 && !budget.IsNearDeadline(200)) {
                CollectAllAtScale(pixels, width, height, stride, fmt, scale: 1, settings, list, seen, accept, budget, pool);
                if (budget.IsExpired) {
                    if (list.Count == 0) return false;
                    results = list.ToArray();
                    return true;
                }
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
                var maxGrid = options?.AggressiveSampling == true ? 8 : 4;
                if (grid > maxGrid) grid = maxGrid;

                var pad = options?.AggressiveSampling == true
                    ? Math.Max(4, Math.Min(width, height) / 60)
                    : Math.Max(8, Math.Min(width, height) / 40);
                var minTile = options?.AggressiveSampling == true ? 24 : 48;
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
                        if (tw < minTile || th < minTile) continue;

                        var startIndex = (long)y0 * stride + x0 * 4L;
                        var requiredLen = (long)(th - 1) * stride + tw * 4L;
                        if (startIndex < 0 || requiredLen <= 0) continue;
                        if (startIndex + requiredLen > pixels.Length) continue;
                        if (tileBudget.IsNearDeadline(120)) break;

                        var tileSpan = pixels.Slice((int)startIndex, (int)requiredLen);
                        if (TryDecode(tileSpan, tw, th, stride, fmt, options, accept, cancellationToken, out var decoded, out _)) {
                            AddResult(list, seen, decoded, accept);
                        }
                    }
                    if (tileBudget.IsExpired) break;
                }

                if (options?.AggressiveSampling == true && list.Count == 0 && !tileBudget.IsExpired && grid < maxGrid) {
                    for (var extraGrid = grid + 1; extraGrid <= maxGrid; extraGrid++) {
                        if (tileBudget.IsExpired) break;
                        var extraTileW = width / extraGrid;
                        var extraTileH = height / extraGrid;
                        for (var ty = 0; ty < extraGrid; ty++) {
                            for (var tx = 0; tx < extraGrid; tx++) {
                                if (tileBudget.IsExpired) break;
                                var x0 = tx * extraTileW;
                                var y0 = ty * extraTileH;
                                var x1 = (tx == extraGrid - 1) ? width : (tx + 1) * extraTileW;
                                var y1 = (ty == extraGrid - 1) ? height : (ty + 1) * extraTileH;

                                x0 = Math.Max(0, x0 - pad);
                                y0 = Math.Max(0, y0 - pad);
                                x1 = Math.Min(width, x1 + pad);
                                y1 = Math.Min(height, y1 + pad);

                                var tw = x1 - x0;
                                var th = y1 - y0;
                                if (tw < minTile || th < minTile) continue;

                                var startIndex = (long)y0 * stride + x0 * 4L;
                                var requiredLen = (long)(th - 1) * stride + tw * 4L;
                                if (startIndex < 0 || requiredLen <= 0) continue;
                                if (startIndex + requiredLen > pixels.Length) continue;
                                if (tileBudget.IsNearDeadline(120)) break;

                                var tileSpan = pixels.Slice((int)startIndex, (int)requiredLen);
                                if (TryDecode(tileSpan, tw, th, stride, fmt, options, accept, cancellationToken, out var decoded, out _)) {
                                    AddResult(list, seen, decoded, accept);
                                }
                            }
                            if (tileBudget.IsExpired) break;
                        }
                        if (list.Count > 0 || tileBudget.IsExpired) break;
                    }
                }
            }

            if (options?.AggressiveSampling == true && list.Count == 0 && !budget.IsExpired && !budget.IsNearDeadline(200)) {
                CollectFromWhitespaceGrid(pixels, width, height, stride, fmt, baseImage, scaleStart, options, accept, cancellationToken, budget, list, seen);
            }

            if (options?.AggressiveSampling == true && list.Count == 0 && !budget.IsNearDeadline(250)) {
                CollectFromDensityTiles(pixels, width, height, stride, fmt, baseImage, scaleStart, options, accept, cancellationToken, budget, list, seen);
            }

            if (options?.AggressiveSampling == true && list.Count == 0 && !budget.IsNearDeadline(250)) {
                CollectFromOverlappingTiles(pixels, width, height, stride, fmt, baseImage, scaleStart, options, accept, cancellationToken, budget, list, seen);
            }

            if (options?.AggressiveSampling == true && list.Count == 0 && !budget.IsNearDeadline(300) && Math.Max(width, height) <= 900) {
                if (TryDecodeUpscaled(pixels, width, height, stride, fmt, profile, options, accept, cancellationToken, budget, out var decodedUpscaled, out _)) {
                    AddResult(list, seen, decodedUpscaled, accept);
                }
            }

            if (list.Count == 0) return false;
            results = list.ToArray();
            return true;
        } finally {
            pool.ReturnAll();
        }
    }

    private static void CollectFromWhitespaceGrid(
        ReadOnlySpan<byte> pixels,
        int width,
        int height,
        int stride,
        PixelFormat fmt,
        QrGrayImage image,
        int scale,
        QrPixelDecodeOptions? options,
        Func<QrDecoded, bool>? accept,
        CancellationToken cancellationToken,
        DecodeBudget budget,
        PooledList<QrDecoded> results,
        HashSet<byte[]> seen) {
        var w = image.Width;
        var h = image.Height;
        if (w <= 0 || h <= 0) return;

        var rowCounts = ArrayPool<int>.Shared.Rent(h);
        var colCounts = ArrayPool<int>.Shared.Rent(w);
        Array.Clear(rowCounts, 0, h);
        Array.Clear(colCounts, 0, w);

        var gray = image.Gray;
        var thresholdMap = image.ThresholdMap;
        var threshold = image.Threshold;

        try {
            var totalDark = 0;
            for (var y = 0; y < h; y++) {
                if (budget.IsNearDeadline(120)) return;
                var row = y * w;
                var darkCount = 0;
                for (var x = 0; x < w; x++) {
                    var idx = row + x;
                    var t = thresholdMap is null ? threshold : thresholdMap[idx];
                    if (gray[idx] <= t) {
                        darkCount++;
                        colCounts[x]++;
                    }
                }
                rowCounts[y] = darkCount;
                totalDark += darkCount;
            }

            var rowBands = new List<(int Start, int End)>(8);
            var colBands = new List<(int Start, int End)>(8);

            var overallDark = totalDark / (double)Math.Max(1, w * h);
            var gapRatio = 0.02;
            if (options?.AggressiveSampling == true) {
                if (overallDark >= 0.45) gapRatio = 0.12;
                else if (overallDark >= 0.35) gapRatio = 0.10;
                else if (overallDark >= 0.25) gapRatio = 0.08;
                else gapRatio = 0.06;
            }
            var rowGapRatio = gapRatio;
            var colGapRatio = gapRatio;
            var minBand = Math.Max(12, Math.Min(w, h) / (options?.AggressiveSampling == true ? 28 : 20));
            var minGap = Math.Max(2, Math.Min(w, h) / (options?.AggressiveSampling == true ? 200 : 120));

            FindBands(rowCounts, h, w, rowGapRatio, minBand, minGap, rowBands);
            FindBands(colCounts, w, h, colGapRatio, minBand, minGap, colBands);

            if (rowBands.Count < 2 && colBands.Count < 2) return;

            var maxBands = 6;
            if (rowBands.Count > maxBands) rowBands.RemoveRange(maxBands, rowBands.Count - maxBands);
            if (colBands.Count > maxBands) colBands.RemoveRange(maxBands, colBands.Count - maxBands);

            var padPx = Math.Max(4, Math.Min(width, height) / 120);
            var minTile = options?.AggressiveSampling == true ? 36 : 48;
            var tilesTried = 0;

            for (var ry = 0; ry < rowBands.Count; ry++) {
                for (var rx = 0; rx < colBands.Count; rx++) {
                    if (budget.IsExpired || budget.IsNearDeadline(120)) return;
                    var rb = rowBands[ry];
                    var cb = colBands[rx];

                    var x0 = cb.Start * scale;
                    var x1 = (cb.End + 1) * scale - 1;
                    var y0 = rb.Start * scale;
                    var y1 = (rb.End + 1) * scale - 1;

                    x0 = Math.Max(0, x0 - padPx);
                    y0 = Math.Max(0, y0 - padPx);
                    x1 = Math.Min(width - 1, x1 + padPx);
                    y1 = Math.Min(height - 1, y1 + padPx);

                    var tw = x1 - x0 + 1;
                    var th = y1 - y0 + 1;
                    if (tw < minTile || th < minTile) continue;

                    var startIndex = (long)y0 * stride + x0 * 4L;
                    var requiredLen = (long)(th - 1) * stride + tw * 4L;
                    if (startIndex < 0 || requiredLen <= 0) continue;
                    if (startIndex + requiredLen > pixels.Length) continue;

                    var tileSpan = pixels.Slice((int)startIndex, (int)requiredLen);
                    if (TryDecode(tileSpan, tw, th, stride, fmt, options, accept, cancellationToken, out var decoded, out _)) {
                        AddResult(results, seen, decoded, accept);
                        return;
                    }

                    tilesTried++;
                    if (tilesTried >= 36) return;
                }
            }
        } finally {
            ArrayPool<int>.Shared.Return(rowCounts, clearArray: true);
            ArrayPool<int>.Shared.Return(colCounts, clearArray: true);
        }
    }

    private static void CollectFromDensityTiles(
        ReadOnlySpan<byte> pixels,
        int width,
        int height,
        int stride,
        PixelFormat fmt,
        QrGrayImage image,
        int scale,
        QrPixelDecodeOptions? options,
        Func<QrDecoded, bool>? accept,
        CancellationToken cancellationToken,
        DecodeBudget budget,
        PooledList<QrDecoded> results,
        HashSet<byte[]> seen) {
        var w = image.Width;
        var h = image.Height;
        if (w <= 0 || h <= 0) return;

        var grid = Math.Clamp(Math.Min(w, h) / 80, 4, 10);
        var tileW = w / grid;
        var tileH = h / grid;
        if (tileW <= 0 || tileH <= 0) return;

        var thresholds = image.ThresholdMap;
        var threshold = image.Threshold;
        var gray = image.Gray;

        Span<(int X, int Y, int Score)> top = stackalloc (int, int, int)[24];
        var topCount = 0;

        for (var ty = 0; ty < grid; ty++) {
            if (budget.IsNearDeadline(200)) return;
            for (var tx = 0; tx < grid; tx++) {
                var x0 = tx * tileW;
                var y0 = ty * tileH;
                var x1 = (tx == grid - 1) ? w : (tx + 1) * tileW;
                var y1 = (ty == grid - 1) ? h : (ty + 1) * tileH;
                if (x1 <= x0 || y1 <= y0) continue;

                var dark = 0;
                var edges = 0;
                var total = (x1 - x0) * (y1 - y0);
                for (var y = y0; y < y1; y++) {
                    var row = y * w;
                    var prevBlack = false;
                    for (var x = x0; x < x1; x++) {
                        var idx = row + x;
                        var t = thresholds is null ? threshold : thresholds[idx];
                        var black = gray[idx] <= t;
                        if (black) dark++;
                        if (x > x0) {
                            if (black != prevBlack) edges++;
                        }
                        if (y > y0) {
                            var upIdx = idx - w;
                            var upT = thresholds is null ? threshold : thresholds[upIdx];
                            var upBlack = gray[upIdx] <= upT;
                            if (black != upBlack) edges++;
                        }
                        prevBlack = black;
                    }
                }

                if (total <= 0) continue;
                var ratio = dark / (double)total;
                if (ratio < 0.04 || ratio > 0.96) continue;
                var balanceScore = 1.0 - Math.Abs(ratio - 0.5) * 2.0;
                if (balanceScore <= 0.0) continue;
                var edgeScore = edges / Math.Max(1.0, total * 2.0);
                var score = (int)Math.Round((balanceScore * 0.7 + edgeScore * 0.3) * 1000);
                if (score <= 0) continue;

                if (topCount < top.Length) {
                    top[topCount++] = (tx, ty, score);
                } else {
                    var minIdx = 0;
                    var minScore = top[0].Score;
                    for (var i = 1; i < top.Length; i++) {
                        if (top[i].Score < minScore) {
                            minScore = top[i].Score;
                            minIdx = i;
                        }
                    }
                    if (score > minScore) {
                        top[minIdx] = (tx, ty, score);
                    }
                }
            }
        }

        if (topCount == 0) return;

        for (var i = 0; i < topCount - 1; i++) {
            var maxIdx = i;
            var maxScore = top[i].Score;
            for (var j = i + 1; j < topCount; j++) {
                if (top[j].Score > maxScore) {
                    maxScore = top[j].Score;
                    maxIdx = j;
                }
            }
            if (maxIdx != i) {
                (top[i], top[maxIdx]) = (top[maxIdx], top[i]);
            }
        }

        var maxTry = Math.Min(topCount, 20);
        var pad = Math.Max(2, Math.Min(width, height) / 80);

        for (var i = 0; i < maxTry; i++) {
            if (budget.IsNearDeadline(200)) return;
            var tile = top[i];
            var x0 = tile.X * tileW;
            var y0 = tile.Y * tileH;
            var x1 = (tile.X == grid - 1) ? w : (tile.X + 1) * tileW;
            var y1 = (tile.Y == grid - 1) ? h : (tile.Y + 1) * tileH;

            var px0 = Math.Max(0, (x0 * scale) - pad);
            var py0 = Math.Max(0, (y0 * scale) - pad);
            var px1 = Math.Min(width - 1, (x1 * scale) + pad);
            var py1 = Math.Min(height - 1, (y1 * scale) + pad);
            var tw = px1 - px0 + 1;
            var th = py1 - py0 + 1;
            if (tw < 36 || th < 36) continue;

            var startIndex = (long)py0 * stride + px0 * 4L;
            var requiredLen = (long)(th - 1) * stride + tw * 4L;
            if (startIndex < 0 || requiredLen <= 0) continue;
            if (startIndex + requiredLen > pixels.Length) continue;

            var tileSpan = pixels.Slice((int)startIndex, (int)requiredLen);
            if (TryDecodeAll(tileSpan, tw, th, stride, fmt, options, accept, cancellationToken, allowTileScan: false, out var decodedList) && decodedList.Length > 0) {
                AddResult(results, seen, decodedList[0], accept);
                return;
            }
        }
    }

    private static void CollectFromOverlappingTiles(
        ReadOnlySpan<byte> pixels,
        int width,
        int height,
        int stride,
        PixelFormat fmt,
        QrGrayImage image,
        int scale,
        QrPixelDecodeOptions? options,
        Func<QrDecoded, bool>? accept,
        CancellationToken cancellationToken,
        DecodeBudget budget,
        PooledList<QrDecoded> results,
        HashSet<byte[]> seen) {
        var w = image.Width;
        var h = image.Height;
        if (w <= 0 || h <= 0) return;

        var minDim = Math.Min(w, h);
        var tileSize = Math.Clamp(minDim / 3, 64, minDim);
        var step = Math.Max(16, tileSize / 2);
        var thresholds = image.ThresholdMap;
        var threshold = image.Threshold;
        var gray = image.Gray;
        var sampleStep = minDim >= 900 ? 2 : 1;

        Span<(int X, int Y, int Score)> top = stackalloc (int, int, int)[20];
        var topCount = 0;

        for (var y = 0; y + tileSize <= h; y += step) {
            if (budget.IsNearDeadline(200)) return;
            for (var x = 0; x + tileSize <= w; x += step) {
                var dark = 0;
                var edges = 0;
                var samples = 0;
                for (var yy = y; yy < y + tileSize; yy += sampleStep) {
                    var row = yy * w;
                    var prevBlack = false;
                    for (var xx = x; xx < x + tileSize; xx += sampleStep) {
                        var idx = row + xx;
                        var t = thresholds is null ? threshold : thresholds[idx];
                        var black = gray[idx] <= t;
                        if (black) dark++;
                        if (xx > x) {
                            if (black != prevBlack) edges++;
                        }
                        if (yy > y) {
                            var upIdx = idx - w;
                            var upT = thresholds is null ? threshold : thresholds[upIdx];
                            var upBlack = gray[upIdx] <= upT;
                            if (black != upBlack) edges++;
                        }
                        prevBlack = black;
                        samples++;
                    }
                }

                if (samples <= 0) continue;
                var ratio = dark / (double)samples;
                if (ratio < 0.10 || ratio > 0.90) continue;
                var balanceScore = 1.0 - Math.Abs(ratio - 0.5) * 2.0;
                if (balanceScore <= 0.0) continue;
                var edgeScore = edges / Math.Max(1.0, samples * 2.0);
                var score = (int)Math.Round((balanceScore * 0.7 + edgeScore * 0.3) * 1000);
                if (score <= 0) continue;

                if (topCount < top.Length) {
                    top[topCount++] = (x, y, score);
                } else {
                    var minIdx = 0;
                    var minScore = top[0].Score;
                    for (var i = 1; i < top.Length; i++) {
                        if (top[i].Score < minScore) {
                            minScore = top[i].Score;
                            minIdx = i;
                        }
                    }
                    if (score > minScore) {
                        top[minIdx] = (x, y, score);
                    }
                }
            }
        }

        if (topCount == 0) return;

        for (var i = 0; i < topCount - 1; i++) {
            var maxIdx = i;
            var maxScore = top[i].Score;
            for (var j = i + 1; j < topCount; j++) {
                if (top[j].Score > maxScore) {
                    maxScore = top[j].Score;
                    maxIdx = j;
                }
            }
            if (maxIdx != i) {
                (top[i], top[maxIdx]) = (top[maxIdx], top[i]);
            }
        }

        var maxTry = Math.Min(topCount, 16);
        var pad = Math.Max(2, Math.Min(width, height) / 80);

        for (var i = 0; i < maxTry; i++) {
            if (budget.IsNearDeadline(200)) return;
            var tile = top[i];
            var x0 = tile.X;
            var y0 = tile.Y;
            var x1 = tile.X + tileSize;
            var y1 = tile.Y + tileSize;

            var px0 = Math.Max(0, (x0 * scale) - pad);
            var py0 = Math.Max(0, (y0 * scale) - pad);
            var px1 = Math.Min(width - 1, (x1 * scale) + pad);
            var py1 = Math.Min(height - 1, (y1 * scale) + pad);
            var tw = px1 - px0 + 1;
            var th = py1 - py0 + 1;
            if (tw < 48 || th < 48) continue;

            var startIndex = (long)py0 * stride + px0 * 4L;
            var requiredLen = (long)(th - 1) * stride + tw * 4L;
            if (startIndex < 0 || requiredLen <= 0) continue;
            if (startIndex + requiredLen > pixels.Length) continue;

            var tileSpan = pixels.Slice((int)startIndex, (int)requiredLen);
            if (TryDecodeAll(tileSpan, tw, th, stride, fmt, options, accept, cancellationToken, allowTileScan: false, out var decodedList) && decodedList.Length > 0) {
                AddResult(results, seen, decodedList[0], accept);
                return;
            }
        }
    }


    private static bool TryDecodeUpscaled(
        ReadOnlySpan<byte> pixels,
        int width,
        int height,
        int stride,
        PixelFormat fmt,
        QrDecodeProfile profile,
        QrPixelDecodeOptions? options,
        Func<QrDecoded, bool>? accept,
        CancellationToken cancellationToken,
        DecodeBudget budget,
        out QrDecoded result,
        out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (budget.IsNearDeadline(250)) return false;
        if (width <= 0 || height <= 0) return false;

        var upW = width * 2;
        var upH = height * 2;
        if (upW <= 0 || upH <= 0) return false;
        if (upW > 2800 || upH > 2800) return false;

        var upStride = upW * 4;
        var required = (long)upStride * upH;
        if (required > 160_000_000) return false;

        var buffer = ArrayPool<byte>.Shared.Rent((int)required);
        try {
            for (var y = 0; y < height; y++) {
                if (budget.IsExpired || budget.IsNearDeadline(200)) return false;
                var srcRow = y * stride;
                var dstRow = (y * 2) * upStride;
                var dstRow2 = dstRow + upStride;
                for (var x = 0; x < width; x++) {
                    var srcIdx = srcRow + x * 4;
                    var dstIdx = dstRow + x * 8;
                    buffer[dstIdx + 0] = pixels[srcIdx + 0];
                    buffer[dstIdx + 1] = pixels[srcIdx + 1];
                    buffer[dstIdx + 2] = pixels[srcIdx + 2];
                    buffer[dstIdx + 3] = pixels[srcIdx + 3];
                    buffer[dstIdx + 4] = pixels[srcIdx + 0];
                    buffer[dstIdx + 5] = pixels[srcIdx + 1];
                    buffer[dstIdx + 6] = pixels[srcIdx + 2];
                    buffer[dstIdx + 7] = pixels[srcIdx + 3];

                    var dstIdx2 = dstRow2 + x * 8;
                    buffer[dstIdx2 + 0] = pixels[srcIdx + 0];
                    buffer[dstIdx2 + 1] = pixels[srcIdx + 1];
                    buffer[dstIdx2 + 2] = pixels[srcIdx + 2];
                    buffer[dstIdx2 + 3] = pixels[srcIdx + 3];
                    buffer[dstIdx2 + 4] = pixels[srcIdx + 0];
                    buffer[dstIdx2 + 5] = pixels[srcIdx + 1];
                    buffer[dstIdx2 + 6] = pixels[srcIdx + 2];
                    buffer[dstIdx2 + 7] = pixels[srcIdx + 3];
                }
            }

            var scaledSettings = GetProfileSettings(profile, Math.Min(upW, upH));
            scaledSettings = ApplyOverrides(scaledSettings, options, scaleStart: 1);
            return TryDecodeAtScale(buffer, upW, upH, upStride, fmt, scale: 1, scaledSettings, budget, accept, out result, out diagnostics);
        } finally {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: false);
        }
    }

    private static void FindBands(int[] counts, int length, int fullLength, double gapRatio, int minBand, int minGap, List<(int Start, int End)> bands) {
        var gapLimit = Math.Max(1, (int)Math.Round(fullLength * gapRatio));
        var inBand = false;
        var bandStart = 0;
        var gapRun = 0;

        for (var i = 0; i < length; i++) {
            var isGap = counts[i] <= gapLimit;
            if (!inBand) {
                if (!isGap) {
                    inBand = true;
                    bandStart = i;
                    gapRun = 0;
                }
                continue;
            }

            if (isGap) {
                gapRun++;
                if (gapRun >= minGap) {
                    var bandEnd = i - gapRun;
                    if (bandEnd >= bandStart && bandEnd - bandStart + 1 >= minBand) {
                        bands.Add((bandStart, bandEnd));
                    }
                    inBand = false;
                }
            } else {
                gapRun = 0;
            }
        }

        if (inBand) {
            var bandEnd = length - 1;
            if (bandEnd >= bandStart && bandEnd - bandStart + 1 >= minBand) {
                bands.Add((bandStart, bandEnd));
            }
        }
    }

}
#endif
