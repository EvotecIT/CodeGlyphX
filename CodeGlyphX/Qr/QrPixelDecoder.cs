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

internal static class QrPixelDecoder {
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
        var budget = new DecodeBudget(options?.MaxMilliseconds ?? 0, cancellationToken);

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
        var budget = new DecodeBudget(options?.MaxMilliseconds ?? 0, cancellationToken);
        if (budget.IsExpired || budget.IsNearDeadline(120)) return false;

        Func<bool>? shouldStop = budget.Enabled ? () => budget.IsNearDeadline(120) : null;
        if (!QrGrayImage.TryCreate(pixels, width, height, stride, fmt, scale: scaleStart, settings.MinContrast, shouldStop, out var baseImage)) {
            return false;
        }
        var seen = new HashSet<string>(StringComparer.Ordinal);
        using var list = new PooledList<QrDecoded>(4);

        CollectAllFromImage(baseImage, settings, list, seen, accept, budget);
        if (budget.IsExpired) {
            if (list.Count == 0) return false;
            results = list.ToArray();
            return true;
        }

        var range = baseImage.Max - baseImage.Min;
        if (settings.AllowContrastStretch && range < 48) {
            var stretched = baseImage.WithContrastStretch(48);
            if (!ReferenceEquals(stretched.Gray, baseImage.Gray)) {
                CollectAllFromImage(stretched, settings, list, seen, accept, budget);
                if (budget.IsExpired) {
                    if (list.Count == 0) return false;
                    results = list.ToArray();
                    return true;
                }
            }
        }

        for (var scale = scaleStart + 1; scale <= settings.CollectMaxScale; scale++) {
            if (budget.IsExpired) break;
            CollectAllAtScale(pixels, width, height, stride, fmt, scale, settings, list, seen, accept, budget);
        }

        if (list.Count == 0) return false;
        results = list.ToArray();
        return true;
    }

    private static bool TryDecodeAtScale(ReadOnlySpan<byte> pixels, int width, int height, int stride, PixelFormat fmt, int scale, QrProfileSettings settings, DecodeBudget budget, Func<QrDecoded, bool>? accept, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (budget.IsExpired) {
            diagnostics = new QrPixelDecodeDiagnostics(
                scale,
                threshold: 0,
                invert: false,
                candidateCount: 0,
                candidateTriplesTried: 0,
                dimension: 0,
                moduleDiagnostics: new global::CodeGlyphX.QrDecodeDiagnostics(global::CodeGlyphX.QrDecodeFailure.Payload));
            return false;
        }

        Func<bool>? shouldStop = budget.Enabled ? () => budget.IsNearDeadline(120) : null;
        if (!QrGrayImage.TryCreate(pixels, width, height, stride, fmt, scale, settings.MinContrast, shouldStop, out var baseImage)) {
            diagnostics = new QrPixelDecodeDiagnostics(
                scale,
                threshold: 0,
                invert: false,
                candidateCount: 0,
                candidateTriplesTried: 0,
                dimension: 0,
                moduleDiagnostics: new global::CodeGlyphX.QrDecodeDiagnostics(global::CodeGlyphX.QrDecodeFailure.InvalidInput));
            return false;
        }

        var fastSettings = new QrProfileSettings(
            settings.MaxScale,
            settings.CollectMaxScale,
            settings.AllowTransforms,
            allowContrastStretch: false,
            allowNormalize: false,
            allowAdaptiveThreshold: false,
            allowBlur: false,
            allowExtraThresholds: false,
            settings.MinContrast,
            settings.AggressiveSampling);

        if (TryDecodeWithImage(scale, baseImage, fastSettings, budget, accept, out result, out var diagFast)) {
            diagnostics = diagFast;
            return true;
        }

        // Track the closest unsuccessful attempt for diagnostics.
        var best = diagFast;

        if (scale == 1 && settings.AllowTransforms) {
            if (TryDecodeWithTransformsFast(scale, baseImage, fastSettings, budget, accept, out result, out var diagFastTransform)) {
                diagnostics = diagFastTransform;
                return true;
            }
            best = Better(best, diagFastTransform);
        }

        if (TryDecodeImageAndStretch(scale, baseImage, settings, budget, accept, out result, out var diagBase)) {
            diagnostics = diagBase;
            return true;
        }

        best = Better(best, diagBase);

        if (scale == 1 && settings.AllowTransforms) {
            if (TryDecodeWithTransforms(scale, baseImage, settings, budget, accept, out result, out var diagTransform)) {
                diagnostics = diagTransform;
                return true;
            }
            best = Better(best, diagTransform);
        }

        diagnostics = best;
        return false;
    }

    private static void AddThresholdCandidate(ref Span<byte> list, ref int count, int threshold) {
        if (count >= list.Length) return;
        if (threshold < 0) threshold = 0;
        else if (threshold > 255) threshold = 255;
        var t = (byte)threshold;
        for (var i = 0; i < count; i++) {
            if (list[i] == t) return;
        }
        list[count++] = t;
    }

    private static bool TryDecodeWithImage(
        int scale,
        QrGrayImage baseImage,
        QrProfileSettings settings,
        DecodeBudget budget,
        Func<QrDecoded, bool>? accept,
        out QrDecoded result,
        out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (budget.IsExpired || budget.IsNearDeadline(120)) return false;

        Span<byte> thresholds = stackalloc byte[16];
        var thresholdCount = 0;
        if (settings.AllowExtraThresholds) {
            var mid = (baseImage.Min + baseImage.Max) / 2;
            var range = baseImage.Max - baseImage.Min;

            AddThresholdCandidate(ref thresholds, ref thresholdCount, mid);
            AddThresholdCandidate(ref thresholds, ref thresholdCount, baseImage.Threshold);
            AddThresholdCandidate(ref thresholds, ref thresholdCount, mid - 16);
            AddThresholdCandidate(ref thresholds, ref thresholdCount, mid + 16);
            AddThresholdCandidate(ref thresholds, ref thresholdCount, baseImage.Threshold - 16);
            AddThresholdCandidate(ref thresholds, ref thresholdCount, baseImage.Threshold + 16);
            if (settings.AggressiveSampling) {
                AddThresholdCandidate(ref thresholds, ref thresholdCount, mid - 32);
                AddThresholdCandidate(ref thresholds, ref thresholdCount, mid + 32);
                AddThresholdCandidate(ref thresholds, ref thresholdCount, baseImage.Threshold - 32);
                AddThresholdCandidate(ref thresholds, ref thresholdCount, baseImage.Threshold + 32);
            }
            AddPercentileThresholds(baseImage, ref thresholds, ref thresholdCount);

            if (range > 0) {
                AddThresholdCandidate(ref thresholds, ref thresholdCount, baseImage.Min + range / 3);
                AddThresholdCandidate(ref thresholds, ref thresholdCount, baseImage.Min + (range * 2) / 3);
            }
            AddThresholdCandidate(ref thresholds, ref thresholdCount, baseImage.Min + 12);
            AddThresholdCandidate(ref thresholds, ref thresholdCount, baseImage.Max - 12);
        } else {
            AddThresholdCandidate(ref thresholds, ref thresholdCount, baseImage.Threshold);
        }

        var best = default(QrPixelDecodeDiagnostics);
        var tightBudget = budget.Enabled && budget.MaxMilliseconds <= 800;
        var candidates = new List<QrFinderPatternDetector.FinderPattern>(8);
        var thresholdLimit = GetBudgetThresholdLimit(budget);
        if (thresholdCount > thresholdLimit) {
            thresholdCount = thresholdLimit;
        }
        if (tightBudget && thresholdCount > 1) {
            thresholdCount = 1;
        }

        for (var i = 0; i < thresholdCount; i++) {
            if (budget.IsExpired) {
                diagnostics = best;
                return false;
            }
            var image = baseImage.WithThreshold(thresholds[i]);
            if (TryDecodeFromGray(scale, thresholds[i], image, invert: false, candidates, accept, settings.AggressiveSampling, budget, out result, out var diagN)) {
                diagnostics = diagN;
                return true;
            }
            best = Better(best, diagN);

            if (!tightBudget) {
                if (TryDecodeFromGray(scale, thresholds[i], image, invert: true, candidates, accept, settings.AggressiveSampling, budget, out result, out var diagI)) {
                    diagnostics = diagI;
                    return true;
                }
                best = Better(best, diagI);
            }
            if (tightBudget) {
                diagnostics = best;
                return false;
            }
        }

        if (settings.AllowAdaptiveThreshold) {
            if (budget.IsNearDeadline(150)) {
                diagnostics = best;
                return false;
            }
            if (budget.IsExpired) {
                diagnostics = best;
                return false;
            }
            var adaptive = baseImage.WithAdaptiveThreshold(windowSize: 15, offset: 8);
            if (TryDecodeFromGray(scale, baseImage.Threshold, adaptive, invert: false, candidates, accept, settings.AggressiveSampling, budget, out result, out var diagA)) {
                diagnostics = diagA;
                return true;
            }
            best = Better(best, diagA);

            if (TryDecodeFromGray(scale, baseImage.Threshold, adaptive, invert: true, candidates, accept, settings.AggressiveSampling, budget, out result, out var diagAI)) {
                diagnostics = diagAI;
                return true;
            }
            best = Better(best, diagAI);

            var adaptiveSoft = baseImage.WithAdaptiveThreshold(windowSize: 25, offset: 4);
            if (TryDecodeFromGray(scale, baseImage.Threshold, adaptiveSoft, invert: false, candidates, accept, settings.AggressiveSampling, budget, out result, out var diagAS)) {
                diagnostics = diagAS;
                return true;
            }
            best = Better(best, diagAS);

            if (TryDecodeFromGray(scale, baseImage.Threshold, adaptiveSoft, invert: true, candidates, accept, settings.AggressiveSampling, budget, out result, out var diagASI)) {
                diagnostics = diagASI;
                return true;
            }
            best = Better(best, diagASI);

            if (settings.AllowBlur && settings.AggressiveSampling) {
                var adaptiveUltra = baseImage.WithAdaptiveThreshold(windowSize: 31, offset: 0);
                if (TryDecodeFromGray(scale, baseImage.Threshold, adaptiveUltra, invert: false, candidates, accept, settings.AggressiveSampling, budget, out result, out var diagAU)) {
                    diagnostics = diagAU;
                    return true;
                }
                best = Better(best, diagAU);

                if (TryDecodeFromGray(scale, baseImage.Threshold, adaptiveUltra, invert: true, candidates, accept, settings.AggressiveSampling, budget, out result, out var diagAUI)) {
                    diagnostics = diagAUI;
                    return true;
                }
                best = Better(best, diagAUI);
            }
        }

        if (settings.AllowBlur) {
            if (budget.IsNearDeadline(150)) {
                diagnostics = best;
                return false;
            }
            if (budget.IsExpired) {
                diagnostics = best;
                return false;
            }
            var blurred = baseImage.WithBoxBlur(radius: 1);
            if (TryDecodeFromGray(scale, blurred.Threshold, blurred, invert: false, candidates, accept, settings.AggressiveSampling, budget, out result, out var diagB0)) {
                diagnostics = diagB0;
                return true;
            }
            best = Better(best, diagB0);

            if (TryDecodeFromGray(scale, blurred.Threshold, blurred, invert: true, candidates, accept, settings.AggressiveSampling, budget, out result, out var diagB1)) {
                diagnostics = diagB1;
                return true;
            }
            best = Better(best, diagB1);

            var adaptiveBlur = blurred.WithAdaptiveThreshold(windowSize: 17, offset: 6);
            if (TryDecodeFromGray(scale, blurred.Threshold, adaptiveBlur, invert: false, candidates, accept, settings.AggressiveSampling, budget, out result, out var diagB2)) {
                diagnostics = diagB2;
                return true;
            }
            best = Better(best, diagB2);

            if (TryDecodeFromGray(scale, blurred.Threshold, adaptiveBlur, invert: true, candidates, accept, settings.AggressiveSampling, budget, out result, out var diagB3)) {
                diagnostics = diagB3;
                return true;
            }
            best = Better(best, diagB3);

            if (settings.AggressiveSampling) {
                var blurred2 = baseImage.WithBoxBlur(radius: 2);
                if (TryDecodeFromGray(scale, blurred2.Threshold, blurred2, invert: false, candidates, accept, settings.AggressiveSampling, budget, out result, out var diagB4)) {
                    diagnostics = diagB4;
                    return true;
                }
                best = Better(best, diagB4);

                if (TryDecodeFromGray(scale, blurred2.Threshold, blurred2, invert: true, candidates, accept, settings.AggressiveSampling, budget, out result, out var diagB5)) {
                    diagnostics = diagB5;
                    return true;
                }
                best = Better(best, diagB5);
            }
        }

        diagnostics = best;
        return false;
    }

    private static bool TryDecodeImageAndStretch(
        int scale,
        QrGrayImage baseImage,
        QrProfileSettings settings,
        DecodeBudget budget,
        Func<QrDecoded, bool>? accept,
        out QrDecoded result,
        out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (budget.IsExpired || budget.IsNearDeadline(120)) return false;

        if (TryDecodeWithImage(scale, baseImage, settings, budget, accept, out result, out var diagBase)) {
            diagnostics = diagBase;
            return true;
        }

        var best = diagBase;

        if (settings.AllowNormalize) {
            if (budget.IsNearDeadline(150)) {
                diagnostics = best;
                return false;
            }
            var normalized = baseImage.WithLocalNormalize(GetNormalizeWindow(baseImage));
            if (TryDecodeWithImage(scale, normalized, settings, budget, accept, out result, out var diagNorm)) {
                diagnostics = diagNorm;
                return true;
            }
            best = Better(best, diagNorm);
        }
        if (settings.AllowContrastStretch) {
            if (budget.IsNearDeadline(150)) {
                diagnostics = best;
                return false;
            }
            var range = baseImage.Max - baseImage.Min;
            if (range < 48) {
                var stretched = baseImage.WithContrastStretch(48);
                if (!ReferenceEquals(stretched.Gray, baseImage.Gray)) {
                    if (TryDecodeWithImage(scale, stretched, settings, budget, accept, out result, out var diagStretch)) {
                        diagnostics = diagStretch;
                        return true;
                    }
                    best = Better(best, diagStretch);

                    if (settings.AllowNormalize) {
                        var normStretch = stretched.WithLocalNormalize(GetNormalizeWindow(stretched));
                        if (TryDecodeWithImage(scale, normStretch, settings, budget, accept, out result, out var diagNormStretch)) {
                            diagnostics = diagNormStretch;
                            return true;
                        }
                        best = Better(best, diagNormStretch);
                    }
                }
            }
        }

        diagnostics = best;
        return false;
    }

    private static bool TryDecodeWithTransforms(
        int scale,
        QrGrayImage baseImage,
        QrProfileSettings settings,
        DecodeBudget budget,
        Func<QrDecoded, bool>? accept,
        out QrDecoded result,
        out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        var best = default(QrPixelDecodeDiagnostics);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var rot90 = baseImage.Rotate90();
        if (budget.IsExpired) return false;
        if (TryDecodeImageAndStretch(scale, rot90, settings, budget, accept, out result, out var d90)) {
            diagnostics = d90;
            return true;
        }
        best = Better(best, d90);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var rot180 = baseImage.Rotate180();
        if (budget.IsExpired) return false;
        if (TryDecodeImageAndStretch(scale, rot180, settings, budget, accept, out result, out var d180)) {
            diagnostics = d180;
            return true;
        }
        best = Better(best, d180);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var rot270 = baseImage.Rotate270();
        if (budget.IsExpired) return false;
        if (TryDecodeImageAndStretch(scale, rot270, settings, budget, accept, out result, out var d270)) {
            diagnostics = d270;
            return true;
        }
        best = Better(best, d270);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var mirror = baseImage.MirrorX();
        if (budget.IsExpired) return false;
        if (TryDecodeImageAndStretch(scale, mirror, settings, budget, accept, out result, out var dm0)) {
            diagnostics = dm0;
            return true;
        }
        best = Better(best, dm0);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var mirror90 = mirror.Rotate90();
        if (budget.IsExpired) return false;
        if (TryDecodeImageAndStretch(scale, mirror90, settings, budget, accept, out result, out var dm90)) {
            diagnostics = dm90;
            return true;
        }
        best = Better(best, dm90);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var mirror180 = mirror.Rotate180();
        if (budget.IsExpired) return false;
        if (TryDecodeImageAndStretch(scale, mirror180, settings, budget, accept, out result, out var dm180)) {
            diagnostics = dm180;
            return true;
        }
        best = Better(best, dm180);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var mirror270 = mirror.Rotate270();
        if (budget.IsExpired) return false;
        if (TryDecodeImageAndStretch(scale, mirror270, settings, budget, accept, out result, out var dm270)) {
            diagnostics = dm270;
            return true;
        }
        best = Better(best, dm270);

        diagnostics = best;
        return false;
    }

    private static bool TryDecodeWithTransformsFast(
        int scale,
        QrGrayImage baseImage,
        QrProfileSettings settings,
        DecodeBudget budget,
        Func<QrDecoded, bool>? accept,
        out QrDecoded result,
        out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        var best = default(QrPixelDecodeDiagnostics);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var rot90 = baseImage.Rotate90();
        if (budget.IsExpired) return false;
        if (TryDecodeWithImage(scale, rot90, settings, budget, accept, out result, out var d90)) {
            diagnostics = d90;
            return true;
        }
        best = Better(best, d90);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var rot180 = baseImage.Rotate180();
        if (budget.IsExpired) return false;
        if (TryDecodeWithImage(scale, rot180, settings, budget, accept, out result, out var d180)) {
            diagnostics = d180;
            return true;
        }
        best = Better(best, d180);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var rot270 = baseImage.Rotate270();
        if (budget.IsExpired) return false;
        if (TryDecodeWithImage(scale, rot270, settings, budget, accept, out result, out var d270)) {
            diagnostics = d270;
            return true;
        }
        best = Better(best, d270);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var mirror = baseImage.MirrorX();
        if (budget.IsExpired) return false;
        if (TryDecodeWithImage(scale, mirror, settings, budget, accept, out result, out var dm0)) {
            diagnostics = dm0;
            return true;
        }
        best = Better(best, dm0);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var mirror90 = mirror.Rotate90();
        if (budget.IsExpired) return false;
        if (TryDecodeWithImage(scale, mirror90, settings, budget, accept, out result, out var dm90)) {
            diagnostics = dm90;
            return true;
        }
        best = Better(best, dm90);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var mirror180 = mirror.Rotate180();
        if (budget.IsExpired) return false;
        if (TryDecodeWithImage(scale, mirror180, settings, budget, accept, out result, out var dm180)) {
            diagnostics = dm180;
            return true;
        }
        best = Better(best, dm180);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var mirror270 = mirror.Rotate270();
        if (budget.IsExpired) return false;
        if (TryDecodeWithImage(scale, mirror270, settings, budget, accept, out result, out var dm270)) {
            diagnostics = dm270;
            return true;
        }
        best = Better(best, dm270);

        diagnostics = best;
        return false;
    }

    private static void CollectAllFromImage(QrGrayImage baseImage, QrProfileSettings settings, PooledList<QrDecoded> list, HashSet<string> seen, Func<QrDecoded, bool>? accept, DecodeBudget budget) {
        if (budget.IsExpired) return;
        CollectAllFromImageCore(baseImage, settings, list, seen, accept, budget);
        if (settings.AllowNormalize) {
            if (budget.IsNearDeadline(150)) return;
            var normalized = baseImage.WithLocalNormalize(GetNormalizeWindow(baseImage));
            if (!budget.IsExpired) {
                CollectAllFromImageCore(normalized, settings, list, seen, accept, budget);
            }
        }
    }

    private static void CollectAllFromImageCore(QrGrayImage baseImage, QrProfileSettings settings, PooledList<QrDecoded> list, HashSet<string> seen, Func<QrDecoded, bool>? accept, DecodeBudget budget) {
        Span<byte> thresholds = stackalloc byte[12];
        var thresholdCount = 0;
        if (settings.AllowExtraThresholds) {
            var mid = (baseImage.Min + baseImage.Max) / 2;
            var range = baseImage.Max - baseImage.Min;

            AddThresholdCandidate(ref thresholds, ref thresholdCount, mid);
            AddThresholdCandidate(ref thresholds, ref thresholdCount, baseImage.Threshold);
            AddThresholdCandidate(ref thresholds, ref thresholdCount, mid - 16);
            AddThresholdCandidate(ref thresholds, ref thresholdCount, mid + 16);
            if (settings.AggressiveSampling) {
                AddThresholdCandidate(ref thresholds, ref thresholdCount, mid - 32);
                AddThresholdCandidate(ref thresholds, ref thresholdCount, mid + 32);
            }
            AddPercentileThresholds(baseImage, ref thresholds, ref thresholdCount);
            if (range > 0) {
                AddThresholdCandidate(ref thresholds, ref thresholdCount, baseImage.Min + range / 3);
                AddThresholdCandidate(ref thresholds, ref thresholdCount, baseImage.Min + (range * 2) / 3);
            }
        } else {
            AddThresholdCandidate(ref thresholds, ref thresholdCount, baseImage.Threshold);
        }

        var candidates = new List<QrFinderPatternDetector.FinderPattern>(8);
        var thresholdLimit = GetBudgetThresholdLimit(budget);
        if (thresholdCount > thresholdLimit) {
            thresholdCount = thresholdLimit;
        }
        for (var i = 0; i < thresholdCount; i++) {
            if (budget.IsExpired) return;
            var image = baseImage.WithThreshold(thresholds[i]);
            CollectFromImage(image, invert: false, list, seen, accept, candidates, budget, settings.AggressiveSampling);
            CollectFromImage(image, invert: true, list, seen, accept, candidates, budget, settings.AggressiveSampling);
        }

        if (settings.AllowAdaptiveThreshold) {
            if (budget.IsNearDeadline(150)) return;
            var adaptive = baseImage.WithAdaptiveThreshold(windowSize: 15, offset: 8);
            CollectFromImage(adaptive, invert: false, list, seen, accept, candidates, budget, settings.AggressiveSampling);
            CollectFromImage(adaptive, invert: true, list, seen, accept, candidates, budget, settings.AggressiveSampling);

            if (!budget.IsExpired && settings.AllowBlur && settings.AggressiveSampling) {
                if (budget.IsNearDeadline(150)) return;
                var adaptiveSoft = baseImage.WithAdaptiveThreshold(windowSize: 31, offset: 0);
                CollectFromImage(adaptiveSoft, invert: false, list, seen, accept, candidates, budget, settings.AggressiveSampling);
                CollectFromImage(adaptiveSoft, invert: true, list, seen, accept, candidates, budget, settings.AggressiveSampling);
            }
        }
    }

    private static int GetNormalizeWindow(QrGrayImage image) {
        var minDim = Math.Min(image.Width, image.Height);
        var window = minDim / 8;
        if (window < 15) window = 15;
        if (window > 51) window = 51;
        if ((window & 1) == 0) window++;
        return window;
    }

    private static void CollectAllAtScale(
        ReadOnlySpan<byte> pixels,
        int width,
        int height,
        int stride,
        PixelFormat fmt,
        int scale,
        QrProfileSettings settings,
        PooledList<QrDecoded> list,
        HashSet<string> seen,
        Func<QrDecoded, bool>? accept,
        DecodeBudget budget) {
        if (budget.IsExpired) return;
        Func<bool>? shouldStop = budget.Enabled ? () => budget.IsNearDeadline(120) : null;
        if (!QrGrayImage.TryCreate(pixels, width, height, stride, fmt, scale, settings.MinContrast, shouldStop, out var image)) {
            return;
        }

        CollectAllFromImage(image, settings, list, seen, accept, budget);
        if (budget.IsExpired) return;

        if (settings.AllowContrastStretch) {
            var range = image.Max - image.Min;
            if (range < 48) {
                var stretched = image.WithContrastStretch(48);
                if (!ReferenceEquals(stretched.Gray, image.Gray)) {
                    CollectAllFromImage(stretched, settings, list, seen, accept, budget);
                }
            }
        }
    }

    private static void AddPercentileThresholds(QrGrayImage image, ref Span<byte> list, ref int count) {
        var total = image.Gray.Length;
        if (total == 0) return;

        Span<int> histogram = stackalloc int[256];
        var gray = image.Gray;
        for (var i = 0; i < gray.Length; i++) {
            histogram[gray[i]]++;
        }

        var q25 = FindQuantile(histogram, total * 25 / 100);
        var q50 = FindQuantile(histogram, total / 2);
        var q75 = FindQuantile(histogram, total * 75 / 100);

        AddThresholdCandidate(ref list, ref count, q25);
        AddThresholdCandidate(ref list, ref count, q50);
        AddThresholdCandidate(ref list, ref count, q75);
    }

    private static byte FindQuantile(Span<int> histogram, int target) {
        if (target <= 0) return 0;
        var sum = 0;
        for (var i = 0; i < histogram.Length; i++) {
            sum += histogram[i];
            if (sum >= target) return (byte)i;
        }
        return 255;
    }

    private static void CollectFromImage(
        QrGrayImage image,
        bool invert,
        PooledList<QrDecoded> results,
        HashSet<string> seen,
        Func<QrDecoded, bool>? accept,
        List<QrFinderPatternDetector.FinderPattern> candidates,
        DecodeBudget budget,
        bool aggressive) {
        if (budget.IsExpired) return;
        Func<bool>? shouldStop = budget.Enabled ? () => budget.IsExpired : null;
        var rowStepOverride = budget.Enabled && budget.MaxMilliseconds <= 800 ? 2 : 0;
        QrFinderPatternDetector.FindCandidates(image, invert, candidates, aggressive, shouldStop, rowStepOverride);
        if (budget.Enabled && candidates.Count > 64) {
            candidates.Sort(static (a, b) => b.Count.CompareTo(a.Count));
            candidates.RemoveRange(64, candidates.Count - 64);
        }
        if (budget.IsExpired) return;
        if (candidates.Count >= 3) {
            CollectFromFinderCandidates(image, invert, candidates, results, seen, accept, budget, aggressive);
        }

        if (!budget.IsExpired && (!budget.Enabled || !budget.IsNearDeadline(200))) {
            CollectFromComponents(image, invert, results, seen, accept, budget);
        }
    }

    private static void CollectFromFinderCandidates(
        QrGrayImage image,
        bool invert,
        List<QrFinderPatternDetector.FinderPattern> candidates,
        PooledList<QrDecoded> results,
        HashSet<string> seen,
        Func<QrDecoded, bool>? accept,
        DecodeBudget budget,
        bool aggressive) {
        candidates.Sort(static (a, b) => b.Count.CompareTo(a.Count));
        var n = Math.Min(candidates.Count, 10);
        var triedTriples = 0;
        var maxTriples = 48;
        if (budget.Enabled) {
            maxTriples = 24;
        }

        var stop = false;
        for (var i = 0; i < n - 2 && !stop; i++) {
            for (var j = i + 1; j < n - 1 && !stop; j++) {
                for (var k = j + 1; k < n; k++) {
                    if (budget.IsExpired) return;
                    if (triedTriples++ >= maxTriples) { stop = true; break; }

                    var a = candidates[i];
                    var b = candidates[j];
                    var c = candidates[k];

                    var msMin = Math.Min(a.ModuleSize, Math.Min(b.ModuleSize, c.ModuleSize));
                    var msMax = Math.Max(a.ModuleSize, Math.Max(b.ModuleSize, c.ModuleSize));
                    if (msMin <= 0) continue;
                    if (msMax > msMin * 1.75) continue;

                    if (!TryOrderAsTlTrBl(a, b, c, out var tl, out var tr, out var bl)) continue;
                    if (TrySampleAndDecode(
                            scale: 1,
                            threshold: image.Threshold,
                            image,
                            invert,
                            tl,
                            tr,
                            bl,
                            candidates.Count,
                            triedTriples,
                            accept,
                            aggressive,
                            budget,
                            out var decoded,
                            out _)) {
                        AddResult(results, seen, decoded, accept);
                    }
                }
            }
        }
    }

    private static void CollectFromComponents(
        QrGrayImage image,
        bool invert,
        PooledList<QrDecoded> results,
        HashSet<string> seen,
        Func<QrDecoded, bool>? accept,
        DecodeBudget budget) {
        if (budget.IsExpired) return;
        Func<bool>? shouldStop = budget.Enabled ? () => budget.IsExpired : null;
        using var comps = FindComponents(image, invert, shouldStop);
        if (comps.Count == 0) return;

        comps.Sort(static (a, b) => b.Area.CompareTo(a.Area));
        var maxTry = Math.Min(comps.Count, 12);

        for (var i = 0; i < maxTry; i++) {
            if (budget.IsExpired || budget.IsNearDeadline(120)) return;
            var c = comps[i];
            var pad = Math.Max(2, (int)Math.Round(Math.Min(c.Width, c.Height) * 0.05));
            var bminX = c.MinX - pad;
            var bminY = c.MinY - pad;
            var bmaxX = c.MaxX + pad;
            var bmaxY = c.MaxY + pad;

            if (bminX < 0) bminX = 0;
            if (bminY < 0) bminY = 0;
            if (bmaxX >= image.Width) bmaxX = image.Width - 1;
            if (bmaxY >= image.Height) bmaxY = image.Height - 1;

            if (TryDecodeByBoundingBox(scale: 1, threshold: image.Threshold, image, invert, accept, aggressive: false, budget, out var decoded, out _, candidateCount: 0, candidateTriplesTried: 0, bminX, bminY, bmaxX, bmaxY)) {
                AddResult(results, seen, decoded, accept);
            }
        }
    }

    private static void AddResult(PooledList<QrDecoded> results, HashSet<string> seen, QrDecoded decoded, Func<QrDecoded, bool>? accept) {
        if (accept is not null && !accept(decoded)) return;
        var key = Convert.ToBase64String(decoded.Bytes);
        if (!seen.Add(key)) return;
        results.Add(decoded);
    }

    private sealed class PooledList<T> : IDisposable {
        private T[] _buffer;
        public int Count { get; private set; }

        public PooledList(int capacity) {
            if (capacity < 1) capacity = 1;
            _buffer = ArrayPool<T>.Shared.Rent(capacity);
        }

        public void Add(T item) {
            if (Count == _buffer.Length) Grow();
            _buffer[Count++] = item;
        }

        public T this[int index] {
            get => _buffer[index];
            set => _buffer[index] = value;
        }

        public void Sort(Comparison<T> comparison) {
            Array.Sort(_buffer, 0, Count, Comparer<T>.Create(comparison));
        }

        public T[] ToArray() {
            if (Count == 0) return Array.Empty<T>();
            var result = new T[Count];
            Array.Copy(_buffer, 0, result, 0, Count);
            return result;
        }

        public void Dispose() {
            ArrayPool<T>.Shared.Return(_buffer, clearArray: true);
            _buffer = Array.Empty<T>();
            Count = 0;
        }

        private void Grow() {
            var next = ArrayPool<T>.Shared.Rent(_buffer.Length * 2);
            Array.Copy(_buffer, 0, next, 0, _buffer.Length);
            ArrayPool<T>.Shared.Return(_buffer, clearArray: true);
            _buffer = next;
        }
    }

    private static bool TryDecodeFromGray(int scale, byte threshold, QrGrayImage image, bool invert, List<QrFinderPatternDetector.FinderPattern> candidates, Func<QrDecoded, bool>? accept, bool aggressive, DecodeBudget budget, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (budget.IsExpired) return false;

        // Finder-based sampling (robust to extra background/noise). Try multiple triples when the region contains UI/text.
        Func<bool>? shouldStop = budget.Enabled ? () => budget.IsExpired : null;
        var rowStepOverride = budget.Enabled && budget.MaxMilliseconds <= 800 ? 2 : 0;
        QrFinderPatternDetector.FindCandidates(image, invert, candidates, aggressive, shouldStop, rowStepOverride);
        if (budget.Enabled && candidates.Count > 64) {
            candidates.Sort(static (a, b) => b.Count.CompareTo(a.Count));
            candidates.RemoveRange(64, candidates.Count - 64);
        }
        if (budget.Enabled && budget.MaxMilliseconds <= 800 && candidates.Count > 30) {
            diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidates.Count, candidateTriplesTried: 0, dimension: 0,
                moduleDiagnostics: new global::CodeGlyphX.QrDecodeDiagnostics(global::CodeGlyphX.QrDecodeFailure.Payload));
            return false;
        }

        if (candidates.Count >= 3) {
            if (TryDecodeFromFinderCandidates(scale, threshold, image, invert, candidates, accept, aggressive, budget, out result, out var diagF)) {
                diagnostics = diagF;
                return true;
            }
            diagnostics = Better(diagnostics, diagF);

            if (TryDecodeByCandidateBounds(scale, threshold, image, invert, candidates, accept, aggressive, budget, out result, out var diagC)) {
                diagnostics = diagC;
                return true;
            }
            diagnostics = Better(diagnostics, diagC);
        }

        if (candidates.Count > 0) {
            if (budget.Enabled && budget.MaxMilliseconds <= 800) {
                return false;
            }
            if (TryDecodeBySingleFinder(scale, threshold, image, invert, candidates, accept, aggressive, budget, out result, out var diagS)) {
                diagnostics = diagS;
                return true;
            }
            diagnostics = Better(diagnostics, diagS);
        }

        if (budget.Enabled && budget.MaxMilliseconds <= 800) {
            return false;
        }

        // Connected-components fallback (helps when finder detection fails but a clean symbol exists).
        var diagCC = default(QrPixelDecodeDiagnostics);
        if (!budget.IsExpired && TryDecodeByConnectedComponents(scale, threshold, image, invert, accept, aggressive, budget, out result, out diagCC)) {
            diagnostics = diagCC;
            return true;
        }
        diagnostics = Better(diagnostics, diagCC);

        // Fallback: bounding box exact-fit (works for perfectly cropped/generated images).
        var diagB = default(QrPixelDecodeDiagnostics);
        if (!budget.IsExpired && TryDecodeByBoundingBox(scale, threshold, image, invert, accept, aggressive, budget, out result, out diagB)) {
            diagnostics = diagB;
            return true;
        }
        diagnostics = Better(diagnostics, diagB);

        return false;
    }

    private static bool TryDecodeByCandidateBounds(
        int scale,
        byte threshold,
        QrGrayImage image,
        bool invert,
        List<QrFinderPatternDetector.FinderPattern> candidates,
        Func<QrDecoded, bool>? accept,
        bool aggressive,
        DecodeBudget budget,
        out QrDecoded result,
        out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (candidates.Count == 0) return false;
        if (budget.IsExpired || budget.IsNearDeadline(120)) return false;

        var minX = double.PositiveInfinity;
        var minY = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var maxY = double.NegativeInfinity;
        var maxModule = 0.0;

        for (var i = 0; i < candidates.Count; i++) {
            var c = candidates[i];
            if (c.ModuleSize > maxModule) maxModule = c.ModuleSize;
            if (c.X < minX) minX = c.X;
            if (c.Y < minY) minY = c.Y;
            if (c.X > maxX) maxX = c.X;
            if (c.Y > maxY) maxY = c.Y;
        }

        if (double.IsInfinity(minX) || double.IsInfinity(minY)) return false;

        var pad = maxModule > 0 ? maxModule * 6.0 : 12.0;

        static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;

        var bminX = Clamp(QrMath.RoundToInt(minX - pad), 0, image.Width - 1);
        var bminY = Clamp(QrMath.RoundToInt(minY - pad), 0, image.Height - 1);
        var bmaxX = Clamp(QrMath.RoundToInt(maxX + pad), 0, image.Width - 1);
        var bmaxY = Clamp(QrMath.RoundToInt(maxY + pad), 0, image.Height - 1);

        if (bmaxX <= bminX || bmaxY <= bminY) return false;

        return TryDecodeByBoundingBox(scale, threshold, image, invert, accept, aggressive, budget, out result, out diagnostics, candidates.Count, candidateTriplesTried: 0, bminX, bminY, bmaxX, bmaxY);
    }

    private static bool TryDecodeBySingleFinder(
        int scale,
        byte threshold,
        QrGrayImage image,
        bool invert,
        List<QrFinderPatternDetector.FinderPattern> candidates,
        Func<QrDecoded, bool>? accept,
        bool aggressive,
        DecodeBudget budget,
        out QrDecoded result,
        out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (candidates.Count == 0) return false;
        if (budget.IsExpired) return false;

        candidates.Sort(static (a, b) => b.Count.CompareTo(a.Count));
        var n = Math.Min(candidates.Count, 4);

        var orientationsBuf = new SingleFinderOrientation[3];
        var dimsBuf = new int[8];

        for (var i = 0; i < n; i++) {
            var c = candidates[i];
            var moduleSize = c.ModuleSize;
            if (moduleSize <= 0) continue;

            var orientations = orientationsBuf.AsSpan();
            var oCount = 0;
            var fx = c.X / image.Width;
            var fy = c.Y / image.Height;

            if (fx <= 0.55 && fy <= 0.55) orientations[oCount++] = SingleFinderOrientation.TopLeft;
            if (fx >= 0.45 && fy <= 0.55) orientations[oCount++] = SingleFinderOrientation.TopRight;
            if (fx <= 0.55 && fy >= 0.45) orientations[oCount++] = SingleFinderOrientation.BottomLeft;
            if (oCount == 0) {
                orientations[oCount++] = SingleFinderOrientation.TopLeft;
                orientations[oCount++] = SingleFinderOrientation.TopRight;
                orientations[oCount++] = SingleFinderOrientation.BottomLeft;
            }

            for (var oi = 0; oi < oCount; oi++) {
                var orientation = orientations[oi];
                if (!TryGetMaxDimension(image, c, moduleSize, orientation, out var maxDim)) continue;

                var dims = dimsBuf.AsSpan();
                var dimsCount = 0;
                var baseDim = NearestValidDimension(maxDim);
                AddDimensionCandidate(ref dims, ref dimsCount, baseDim);
                AddDimensionCandidate(ref dims, ref dimsCount, baseDim - 4);
                AddDimensionCandidate(ref dims, ref dimsCount, baseDim - 8);
                AddDimensionCandidate(ref dims, ref dimsCount, baseDim - 12);
                AddDimensionCandidate(ref dims, ref dimsCount, baseDim - 16);
                AddDimensionCandidate(ref dims, ref dimsCount, baseDim - 20);

                var maxDims = budget.Enabled ? Math.Min(dimsCount, 3) : dimsCount;
                for (var di = 0; di < maxDims; di++) {
                    if (budget.IsExpired) return false;
                    var dim = dims[di];
                    if (!TryGetBoundingBoxFromSingleFinder(image, c, moduleSize, orientation, dim, out var minX, out var minY, out var maxX, out var maxY)) {
                        continue;
                    }

                    if (TryDecodeByBoundingBox(scale, threshold, image, invert, accept, aggressive, budget, out result, out diagnostics, candidates.Count, candidateTriplesTried: 0, minX, minY, maxX, maxY)) {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private enum SingleFinderOrientation {
        TopLeft,
        TopRight,
        BottomLeft
    }

    private static bool TryGetMaxDimension(
        QrGrayImage image,
        QrFinderPatternDetector.FinderPattern candidate,
        double moduleSize,
        SingleFinderOrientation orientation,
        out int maxDim) {
        maxDim = 0;
        if (moduleSize <= 0) return false;

        var halfFinder = moduleSize * 3.5;
        var left = candidate.X - halfFinder;
        var right = candidate.X + halfFinder;
        var top = candidate.Y - halfFinder;
        var bottom = candidate.Y + halfFinder;

        double availX;
        double availY;
        switch (orientation) {
            case SingleFinderOrientation.TopRight:
                availX = right;
                availY = image.Height - top;
                break;
            case SingleFinderOrientation.BottomLeft:
                availX = image.Width - left;
                availY = bottom;
                break;
            default:
                availX = image.Width - left;
                availY = image.Height - top;
                break;
        }

        var dim = (int)Math.Floor(Math.Min(availX, availY) / moduleSize);
        if (dim < 21) return false;
        if (dim > 177) dim = 177;
        maxDim = dim;
        return true;
    }

    private static bool TryGetBoundingBoxFromSingleFinder(
        QrGrayImage image,
        QrFinderPatternDetector.FinderPattern candidate,
        double moduleSize,
        SingleFinderOrientation orientation,
        int dimension,
        out int minX,
        out int minY,
        out int maxX,
        out int maxY) {
        minX = minY = maxX = maxY = 0;
        if (dimension is < 21 or > 177) return false;
        if ((dimension & 3) != 1) return false;

        var halfFinder = moduleSize * 3.5;
        var dimPx = dimension * moduleSize;

        double minXf;
        double minYf;
        double maxXf;
        double maxYf;

        switch (orientation) {
            case SingleFinderOrientation.TopRight:
                maxXf = candidate.X + halfFinder;
                minXf = maxXf - dimPx;
                minYf = candidate.Y - halfFinder;
                maxYf = minYf + dimPx;
                break;
            case SingleFinderOrientation.BottomLeft:
                minXf = candidate.X - halfFinder;
                maxXf = minXf + dimPx;
                maxYf = candidate.Y + halfFinder;
                minYf = maxYf - dimPx;
                break;
            default:
                minXf = candidate.X - halfFinder;
                maxXf = minXf + dimPx;
                minYf = candidate.Y - halfFinder;
                maxYf = minYf + dimPx;
                break;
        }

        var slack = moduleSize * 4.0;
        if (minXf < -slack || minYf < -slack || maxXf > image.Width - 1 + slack || maxYf > image.Height - 1 + slack) return false;

        minX = Math.Clamp(QrMath.RoundToInt(minXf), 0, image.Width - 1);
        maxX = Math.Clamp(QrMath.RoundToInt(maxXf), 0, image.Width - 1);
        minY = Math.Clamp(QrMath.RoundToInt(minYf), 0, image.Height - 1);
        maxY = Math.Clamp(QrMath.RoundToInt(maxYf), 0, image.Height - 1);

        return minX < maxX && minY < maxY;
    }

    private static bool TryDecodeByConnectedComponents(
        int scale,
        byte threshold,
        QrGrayImage image,
        bool invert,
        Func<QrDecoded, bool>? accept,
        bool aggressive,
        DecodeBudget budget,
        out QrDecoded result,
        out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        var w = image.Width;
        var h = image.Height;

        if (budget.IsExpired) return false;
        Func<bool>? shouldStop = budget.Enabled ? () => budget.IsExpired : null;
        using var comps = FindComponents(image, invert, shouldStop);
        if (comps.Count == 0) return false;

        comps.Sort(static (a, b) => b.Area.CompareTo(a.Area));
        var maxTry = Math.Min(comps.Count, 6);

        for (var i = 0; i < maxTry; i++) {
            if (budget.IsExpired || budget.IsNearDeadline(120)) return false;
            var c = comps[i];
            var pad = Math.Max(2, (int)Math.Round(Math.Min(c.Width, c.Height) * 0.05));
            var bminX = c.MinX - pad;
            var bminY = c.MinY - pad;
            var bmaxX = c.MaxX + pad;
            var bmaxY = c.MaxY + pad;

            if (bminX < 0) bminX = 0;
            if (bminY < 0) bminY = 0;
            if (bmaxX >= w) bmaxX = w - 1;
            if (bmaxY >= h) bmaxY = h - 1;

            if (TryDecodeByBoundingBox(scale, threshold, image, invert, accept, aggressive, budget, out result, out diagnostics, candidateCount: 0, candidateTriplesTried: 0, bminX, bminY, bmaxX, bmaxY)) {
                return true;
            }
        }

        return false;
    }

    private static PooledList<Component> FindComponents(QrGrayImage image, bool invert, Func<bool>? shouldStop) {
        var w = image.Width;
        var h = image.Height;
        var comps = new PooledList<Component>(8);
        if (w <= 0 || h <= 0) return comps;

        var total = w * h;
        var minArea = Math.Max(16, total / 400); // ~0.25% of image area
        var visited = ArrayPool<bool>.Shared.Rent(total);
        Array.Clear(visited, 0, total);
        var stack = ArrayPool<int>.Shared.Rent(Math.Max(64, total / 16));

        try {
            for (var y = 0; y < h; y++) {
                if (shouldStop?.Invoke() == true) break;
                var row = y * w;
                for (var x = 0; x < w; x++) {
                    var idx = row + x;
                    if (visited[idx]) continue;
                    if (!image.IsBlack(x, y, invert)) {
                        visited[idx] = true;
                        continue;
                    }

                    var minX = x;
                    var maxX = x;
                    var minY = y;
                    var maxY = y;
                    var area = 0;

                    var sp = 0;
                    stack[sp++] = idx;

                    while (sp > 0) {
                        var cur = stack[--sp];
                        if (visited[cur]) continue;
                        visited[cur] = true;

                        var cy = cur / w;
                        var cx = cur - cy * w;
                        if (!image.IsBlack(cx, cy, invert)) continue;

                        area++;
                        if (cx < minX) minX = cx;
                        if (cx > maxX) maxX = cx;
                        if (cy < minY) minY = cy;
                        if (cy > maxY) maxY = cy;

                        if (cx > 0) {
                            var ni = cur - 1;
                            if (!visited[ni]) {
                                if (sp >= stack.Length) {
                                    GrowStack(ref stack, sp);
                                }
                                stack[sp++] = ni;
                            }
                        }
                        if (cx + 1 < w) {
                            var ni = cur + 1;
                            if (!visited[ni]) {
                                if (sp >= stack.Length) {
                                    GrowStack(ref stack, sp);
                                }
                                stack[sp++] = ni;
                            }
                        }
                        if (cy > 0) {
                            var ni = cur - w;
                            if (!visited[ni]) {
                                if (sp >= stack.Length) {
                                    GrowStack(ref stack, sp);
                                }
                                stack[sp++] = ni;
                            }
                        }
                        if (cy + 1 < h) {
                            var ni = cur + w;
                            if (!visited[ni]) {
                                if (sp >= stack.Length) {
                                    GrowStack(ref stack, sp);
                                }
                                stack[sp++] = ni;
                            }
                        }
                    }

                    if (area < minArea) continue;
                    var cw = maxX - minX + 1;
                    var ch = maxY - minY + 1;
                    if (cw < 21 || ch < 21) continue;

                    var ratio = cw > ch ? (double)cw / ch : (double)ch / cw;
                    if (ratio > 2.2) continue;

                    comps.Add(new Component(minX, minY, maxX, maxY, area));
                }
            }
        } finally {
            ArrayPool<bool>.Shared.Return(visited);
            ArrayPool<int>.Shared.Return(stack);
        }

        return comps;
    }

    private static void GrowStack(ref int[] stack, int size) {
        var newSize = stack.Length * 2;
        if (newSize < size + 8) newSize = size + 8;
        var next = ArrayPool<int>.Shared.Rent(newSize);
        Array.Copy(stack, next, stack.Length);
        ArrayPool<int>.Shared.Return(stack);
        stack = next;
    }

    private static bool TryDecodeFromFinderCandidates(int scale, byte threshold, QrGrayImage image, bool invert, List<QrFinderPatternDetector.FinderPattern> candidates, Func<QrDecoded, bool>? accept, bool aggressive, DecodeBudget budget, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        candidates.Sort(static (a, b) => b.Count.CompareTo(a.Count));
        var n = Math.Min(candidates.Count, budget.Enabled ? 8 : 12);
        var triedTriples = 0;
        var bboxAttempts = 0;
        var maxTriples = aggressive ? 80 : 40;
        if (budget.Enabled) {
            maxTriples = Math.Min(maxTriples, aggressive ? 40 : 20);
        }

        for (var i = 0; i < n - 2; i++) {
            for (var j = i + 1; j < n - 1; j++) {
                for (var k = j + 1; k < n; k++) {
                    if (budget.IsExpired || budget.IsNearDeadline(120)) return false;
                    triedTriples++;
                    if (triedTriples > maxTriples) return false;
                    var a = candidates[i];
                    var b = candidates[j];
                    var c = candidates[k];

                    var msMin = Math.Min(a.ModuleSize, Math.Min(b.ModuleSize, c.ModuleSize));
                    var msMax = Math.Max(a.ModuleSize, Math.Max(b.ModuleSize, c.ModuleSize));
                    if (msMin <= 0) continue;
                    if (msMax > msMin * 1.75) continue;

                    if (!TryOrderAsTlTrBl(a, b, c, out var tl, out var tr, out var bl)) continue;
                    if (TrySampleAndDecode(scale, threshold, image, invert, tl, tr, bl, candidates.Count, triedTriples, accept, aggressive, budget, out result, out var diag)) {
                        diagnostics = diag;
                        return true;
                    }
                    diagnostics = Better(diagnostics, diag);

                    // If the finder triple looks reasonable but decoding fails (false positives are common in UI),
                    // try a bounded bbox decode around the candidate region before moving on.
                    if (bboxAttempts < 4 && TryGetCandidateBounds(tl, tr, bl, image.Width, image.Height, out var bminX, out var bminY, out var bmaxX, out var bmaxY)) {
                        bboxAttempts++;
                        if (TryDecodeByBoundingBox(scale, threshold, image, invert, accept, aggressive, budget, out result, out var diagB, candidates.Count, triedTriples, bminX, bminY, bmaxX, bmaxY)) {
                            diagnostics = diagB;
                            return true;
                        }
                        diagnostics = Better(diagnostics, diagB);
                    }
                }
            }
        }

        return false;
    }

    private readonly struct Component {
        public int MinX { get; }
        public int MinY { get; }
        public int MaxX { get; }
        public int MaxY { get; }
        public int Area { get; }
        public int Width => MaxX - MinX + 1;
        public int Height => MaxY - MinY + 1;

        public Component(int minX, int minY, int maxX, int maxY, int area) {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
            Area = area;
        }
    }

    private static bool TryOrderAsTlTrBl(
        QrFinderPatternDetector.FinderPattern a,
        QrFinderPatternDetector.FinderPattern b,
        QrFinderPatternDetector.FinderPattern c,
        out QrFinderPatternDetector.FinderPattern tl,
        out QrFinderPatternDetector.FinderPattern tr,
        out QrFinderPatternDetector.FinderPattern bl) {
        tl = default;
        tr = default;
        bl = default;

        var dAB = Dist2(a, b);
        var dAC = Dist2(a, c);
        var dBC = Dist2(b, c);

        // Side lengths are the two smaller distances; they should be similar for a QR finder triangle.
        var maxD = dAB;
        var maxP = 0;
        if (dAC > maxD) { maxD = dAC; maxP = 1; }
        if (dBC > maxD) { maxD = dBC; maxP = 2; }

        double side1, side2;
        if (maxP == 0) { side1 = dAC; side2 = dBC; }
        else if (maxP == 1) { side1 = dAB; side2 = dBC; }
        else { side1 = dAB; side2 = dAC; }

        if (side1 <= 0 || side2 <= 0) return false;
        var ratio = side1 > side2 ? side1 / side2 : side2 / side1;
        if (ratio > 1.8) return false;

        // The point shared by the two shorter distances is top-left.
        if (maxP == 0) {
            tl = c;
            tr = a;
            bl = b;
        } else if (maxP == 1) {
            tl = b;
            tr = a;
            bl = c;
        } else {
            tl = a;
            tr = b;
            bl = c;
        }

        // Ensure clockwise orientation: tr should be to the right and bl below.
        var cross = Cross(tr.X - tl.X, tr.Y - tl.Y, bl.X - tl.X, bl.Y - tl.Y);
        if (cross < 0) (tr, bl) = (bl, tr);

        return true;
    }

    private static double Dist2(QrFinderPatternDetector.FinderPattern a, QrFinderPatternDetector.FinderPattern b) {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    private static double Cross(double ax, double ay, double bx, double by) => ax * by - ay * bx;

    private static bool TrySampleAndDecode(int scale, byte threshold, QrGrayImage image, bool invert, QrFinderPatternDetector.FinderPattern tl, QrFinderPatternDetector.FinderPattern tr, QrFinderPatternDetector.FinderPattern bl, int candidateCount, int candidateTriplesTried, Func<QrDecoded, bool>? accept, bool aggressive, DecodeBudget budget, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (budget.IsExpired) return false;

        var moduleSize = (tl.ModuleSize + tr.ModuleSize + bl.ModuleSize) / 3.0;
        if (moduleSize <= 0) return false;

        var distX = Distance(tl.X, tl.Y, tr.X, tr.Y);
        var distY = Distance(tl.X, tl.Y, bl.X, bl.Y);
        if (distX <= 0 || distY <= 0) return false;

        var dimH = QrMath.RoundToInt(distX / moduleSize) + 7;
        var dimV = QrMath.RoundToInt(distY / moduleSize) + 7;

        // Try a few nearby dimensions (estimation can be off on UI-scaled QR).
        var baseDim = NearestValidDimension((dimH + dimV) / 2);
        Span<int> candidates = stackalloc int[10];
        var candidatesCount = 0;
        AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim);
        AddDimensionCandidate(ref candidates, ref candidatesCount, NearestValidDimension(dimH));
        AddDimensionCandidate(ref candidates, ref candidatesCount, NearestValidDimension(dimV));
        AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim - 4);
        AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim - 8);
        AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim - 12);
        AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim + 4);
        AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim + 8);
        AddDimensionCandidate(ref candidates, ref candidatesCount, baseDim + 12);

        for (var i = 0; i < candidatesCount; i++) {
            if (budget.IsExpired) return false;
            var dimension = candidates[i];
            if (dimension is < 21 or > 177) continue;
            if (TrySampleAndDecodeDimension(scale, threshold, image, invert, tl, tr, bl, dimension, candidateCount, candidateTriplesTried, accept, aggressive, budget, out result, out diagnostics)) return true;
        }

        return false;
    }

    private static void AddDimensionCandidate(ref Span<int> list, ref int count, int dimension) {
        if (dimension is < 21 or > 177) return;
        if ((dimension & 3) != 1) return;
        for (var i = 0; i < count; i++) {
            if (list[i] == dimension) return;
        }
        list[count++] = dimension;
    }

    private static bool TrySampleAndDecodeDimension(int scale, byte threshold, QrGrayImage image, bool invert, QrFinderPatternDetector.FinderPattern tl, QrFinderPatternDetector.FinderPattern tr, QrFinderPatternDetector.FinderPattern bl, int dimension, int candidateCount, int candidateTriplesTried, Func<QrDecoded, bool>? accept, bool aggressive, DecodeBudget budget, out QrDecoded result, out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (budget.IsExpired) return false;

        var modulesBetweenCenters = dimension - 7;
        if (modulesBetweenCenters <= 0) return false;

        // Use affine mapping based on the three finder centers (no perspective correction yet).
        var vxX = (tr.X - tl.X) / modulesBetweenCenters;
        var vxY = (tr.Y - tl.Y) / modulesBetweenCenters;
        var vyX = (bl.X - tl.X) / modulesBetweenCenters;
        var vyY = (bl.Y - tl.Y) / modulesBetweenCenters;

        // First try sampling using only the raw finder centers (fast path for clean, axis-aligned on-screen QR).
        const double finderCenterToCorner = 3.5;

        var moduleSize0 = (Math.Sqrt(vxX * vxX + vxY * vxY) + Math.Sqrt(vyX * vyX + vyY * vyY)) / 2.0;

        var cornerTlX0 = tl.X - (vxX + vyX) * finderCenterToCorner;
        var cornerTlY0 = tl.Y - (vxY + vyY) * finderCenterToCorner;

        var cornerTrX0 = cornerTlX0 + vxX * dimension;
        var cornerTrY0 = cornerTlY0 + vxY * dimension;

        var cornerBlX0 = cornerTlX0 + vyX * dimension;
        var cornerBlY0 = cornerTlY0 + vyY * dimension;

        var cornerBrX0 = cornerTlX0 + (vxX + vyX) * dimension;
        var cornerBrY0 = cornerTlY0 + (vxY + vyY) * dimension;

        if (TrySampleWithCorners(image, invert, phaseX: 0, phaseY: 0, dimension, cornerTlX0, cornerTlY0, cornerTrX0, cornerTrY0, cornerBrX0, cornerBrY0, cornerBlX0, cornerBlY0, moduleSize0, accept, aggressive, budget, out result, out var moduleDiag0)) {
            diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, dimension, moduleDiag0);
            return true;
        }

        var best = moduleDiag0;

        // Refine sampling using format/timing patterns as a score:
        // - phase (sub-module offsets) + small scale adjustment of vx/vy (finder centers can be slightly off).
        QrPixelSampling.RefineTransform(image, invert, tl.X, tl.Y, vxX, vxY, vyX, vyY, dimension, out vxX, out vxY, out vyX, out vyY, out var phaseX, out var phaseY);

        var moduleSize = (Math.Sqrt(vxX * vxX + vxY * vxY) + Math.Sqrt(vyX * vyX + vyY * vyY)) / 2.0;

        // Build an initial estimate of the QR outer corners from the three finder centers and the per-module vectors.
        var cornerTlX = tl.X - (vxX + vyX) * finderCenterToCorner;
        var cornerTlY = tl.Y - (vxY + vyY) * finderCenterToCorner;

        var cornerTrX = cornerTlX + vxX * dimension;
        var cornerTrY = cornerTlY + vxY * dimension;

        var cornerBlX = cornerTlX + vyX * dimension;
        var cornerBlY = cornerTlY + vyY * dimension;

        var cornerBrXr0 = cornerTlX + (vxX + vyX) * dimension;
        var cornerBrYr0 = cornerTlY + (vxY + vyY) * dimension;

        // Then try with refined phase/scale. Alignment pattern detection can produce false positives on busy UI regions;
        // use it as a fallback only.
        if (TrySampleWithCorners(image, invert, phaseX, phaseY, dimension, cornerTlX, cornerTlY, cornerTrX, cornerTrY, cornerBrXr0, cornerBrYr0, cornerBlX, cornerBlY, moduleSize, accept, aggressive, budget, out result, out var moduleDiagR)) {
            diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, dimension, moduleDiagR);
            return true;
        }

        best = Better(best, moduleDiagR);

        // Small perspective tuning: jitter the bottom-right corner in module-space to handle mild skew.
        if (TrySampleWithCornerJitter(
                image,
                invert,
                phaseX,
                phaseY,
                dimension,
                cornerTlX,
                cornerTlY,
                cornerTrX,
                cornerTrY,
                cornerBlX,
                cornerBlY,
                cornerBrXr0,
                cornerBrYr0,
                vxX,
                vxY,
                vyX,
                vyY,
                moduleSize,
                accept,
                aggressive,
                budget,
                out result,
                out var moduleDiagJ)) {
            diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, dimension, moduleDiagJ);
            return true;
        }

        best = Better(best, moduleDiagJ);

        // Try to find the bottom-right alignment pattern (helps a lot on UI-scaled QR where module pitch isn't perfectly uniform).
        var version = (dimension - 17) / 4;
        if (version >= 2) {
            var align = QrTables.GetAlignmentPatternPositions(version);
            if (align.Length > 0) {
                var a = align[align.Length - 1]; // bottom-right
                var dxA = a - 3 + phaseX;
                var dyA = a - 3 + phaseY;
                var predX = tl.X + vxX * dxA + vyX * dyA;
                var predY = tl.Y + vxY * dxA + vyY * dyA;

                if (QrAlignmentPatternFinder.TryFind(image, invert, predX, predY, vxX, vxY, vyX, vyY, moduleSize, out var ax, out var ay)) {
                    // Convert the alignment center back into an estimated outer bottom-right corner.
                    // The bottom-right alignment center is at (dimension-6.5, dimension-6.5) in module-center coordinates,
                    // i.e. 6.5 modules inward from the outer corner along both axes.
                    var cornerBrXA = ax + (vxX + vyX) * 6.5;
                    var cornerBrYA = ay + (vxY + vyY) * 6.5;

                    if (TrySampleWithCorners(image, invert, phaseX, phaseY, dimension, cornerTlX, cornerTlY, cornerTrX, cornerTrY, cornerBrXA, cornerBrYA, cornerBlX, cornerBlY, moduleSize, accept, aggressive, budget, out result, out var moduleDiagA)) {
                        diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, dimension, moduleDiagA);
                        return true;
                    }

                    best = Better(best, moduleDiagA);
                }
            }
        }

        var moduleDiagP = default(global::CodeGlyphX.QrDecodeDiagnostics);
        if (aggressive && TrySampleWithPhaseJitter(
                image,
                invert,
                phaseX,
                phaseY,
                dimension,
                cornerTlX,
                cornerTlY,
                cornerTrX,
                cornerTrY,
                cornerBrXr0,
                cornerBrYr0,
                cornerBlX,
                cornerBlY,
                moduleSize,
                accept,
                aggressive,
                budget,
                out result,
                out moduleDiagP)) {
            diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, dimension, moduleDiagP);
            return true;
        }

        if (aggressive) {
            best = Better(best, moduleDiagP);
        }

        diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, dimension, best);
        return false;
    }

    private static bool TrySampleWithCornerJitter(
        QrGrayImage image,
        bool invert,
        double phaseX,
        double phaseY,
        int dimension,
        double cornerTlX,
        double cornerTlY,
        double cornerTrX,
        double cornerTrY,
        double cornerBlX,
        double cornerBlY,
        double cornerBrX,
        double cornerBrY,
        double vxX,
        double vxY,
        double vyX,
        double vyY,
        double moduleSizePx,
        Func<QrDecoded, bool>? accept,
        bool aggressive,
        DecodeBudget budget,
        out QrDecoded result,
        out global::CodeGlyphX.QrDecodeDiagnostics moduleDiagnostics) {
        result = null!;
        moduleDiagnostics = default;

        if (budget.IsExpired) return false;

        Span<double> offsets = stackalloc double[3];
        offsets[0] = -0.60;
        offsets[1] = 0.0;
        offsets[2] = 0.60;

        var best = default(global::CodeGlyphX.QrDecodeDiagnostics);

        for (var yi = 0; yi < offsets.Length; yi++) {
            if (budget.IsExpired) return false;
            var oy = offsets[yi];
            for (var xi = 0; xi < offsets.Length; xi++) {
                if (budget.IsExpired) return false;
                var ox = offsets[xi];
                if (ox == 0 && oy == 0) continue;

                var jx = cornerBrX + vxX * ox + vyX * oy;
                var jy = cornerBrY + vxY * ox + vyY * oy;

                if (TrySampleWithCorners(image, invert, phaseX, phaseY, dimension, cornerTlX, cornerTlY, cornerTrX, cornerTrY, jx, jy, cornerBlX, cornerBlY, moduleSizePx, accept, aggressive, budget, out result, out var diag)) {
                    moduleDiagnostics = diag;
                    return true;
                }

                best = Better(best, diag);
            }
        }

        moduleDiagnostics = best;
        return false;
    }

    private static bool TrySampleWithPhaseJitter(
        QrGrayImage image,
        bool invert,
        double phaseX,
        double phaseY,
        int dimension,
        double cornerTlX,
        double cornerTlY,
        double cornerTrX,
        double cornerTrY,
        double cornerBrX,
        double cornerBrY,
        double cornerBlX,
        double cornerBlY,
        double moduleSizePx,
        Func<QrDecoded, bool>? accept,
        bool aggressive,
        DecodeBudget budget,
        out QrDecoded result,
        out global::CodeGlyphX.QrDecodeDiagnostics moduleDiagnostics) {
        result = null!;
        moduleDiagnostics = default;

        if (budget.IsExpired) return false;

        Span<double> offsets = stackalloc double[3];
        offsets[0] = -0.35;
        offsets[1] = 0.0;
        offsets[2] = 0.35;

        var best = default(global::CodeGlyphX.QrDecodeDiagnostics);

        for (var yi = 0; yi < offsets.Length; yi++) {
            if (budget.IsExpired) return false;
            var oy = phaseY + offsets[yi];
            for (var xi = 0; xi < offsets.Length; xi++) {
                if (budget.IsExpired) return false;
                var ox = phaseX + offsets[xi];
                if (ox == phaseX && oy == phaseY) continue;

                if (TrySampleWithCorners(
                        image,
                        invert,
                        ox,
                        oy,
                        dimension,
                        cornerTlX,
                        cornerTlY,
                        cornerTrX,
                        cornerTrY,
                        cornerBrX,
                        cornerBrY,
                        cornerBlX,
                        cornerBlY,
                        moduleSizePx,
                        accept,
                        aggressive,
                        budget,
                        out result,
                        out var diag)) {
                    moduleDiagnostics = diag;
                    return true;
                }

                best = Better(best, diag);
            }
        }

        moduleDiagnostics = best;
        return false;
    }

    private static bool TrySampleWithCorners(
        QrGrayImage image,
        bool invert,
        double phaseX,
        double phaseY,
        int dimension,
        double cornerTlX,
        double cornerTlY,
        double cornerTrX,
        double cornerTrY,
        double cornerBrX,
        double cornerBrY,
        double cornerBlX,
        double cornerBlY,
        double moduleSizePx,
        Func<QrDecoded, bool>? accept,
        bool aggressive,
        DecodeBudget budget,
        out QrDecoded result,
        out global::CodeGlyphX.QrDecodeDiagnostics moduleDiagnostics) {
        if (budget.IsExpired) {
            result = null!;
            moduleDiagnostics = default;
            return false;
        }

        if (TrySampleWithCornersInternal(
                image,
                invert,
                phaseX,
                phaseY,
                dimension,
                cornerTlX,
                cornerTlY,
                cornerTrX,
                cornerTrY,
                cornerBrX,
                cornerBrY,
                cornerBlX,
                cornerBlY,
                moduleSizePx,
                accept,
                budget,
                loose: false,
                out result,
                out moduleDiagnostics)) {
            return true;
        }

        var strictDiag = moduleDiagnostics;
        var looseDiag = default(global::CodeGlyphX.QrDecodeDiagnostics);
        if (aggressive && ShouldTryLooseSampling(moduleDiagnostics, moduleSizePx) &&
            TrySampleWithCornersInternal(
                image,
                invert,
                phaseX,
                phaseY,
                dimension,
                cornerTlX,
                cornerTlY,
                cornerTrX,
                cornerTrY,
                cornerBrX,
                cornerBrY,
                cornerBlX,
                cornerBlY,
                moduleSizePx,
                accept,
                budget,
                loose: true,
                out result,
                out looseDiag)) {
            moduleDiagnostics = looseDiag;
            return true;
        }

        moduleDiagnostics = Better(strictDiag, looseDiag);
        return false;
    }

    private static bool TrySampleWithCornersInternal(
        QrGrayImage image,
        bool invert,
        double phaseX,
        double phaseY,
        int dimension,
        double cornerTlX,
        double cornerTlY,
        double cornerTrX,
        double cornerTrY,
        double cornerBrX,
        double cornerBrY,
        double cornerBlX,
        double cornerBlY,
        double moduleSizePx,
        Func<QrDecoded, bool>? accept,
        DecodeBudget budget,
        bool loose,
        out QrDecoded result,
        out global::CodeGlyphX.QrDecodeDiagnostics moduleDiagnostics) {
        result = null!;
        moduleDiagnostics = default;

        // Build a perspective transform from module-corner space (0..dimension) to image-space and sample using it.
        var transform = QrPerspectiveTransform.QuadrilateralToQuadrilateral(
            0, 0,
            dimension, 0,
            dimension, dimension,
            0, dimension,
            cornerTlX, cornerTlY,
            cornerTrX, cornerTrY,
            cornerBrX, cornerBrY,
            cornerBlX, cornerBlY);

        var bm = new global::CodeGlyphX.BitMatrix(dimension, dimension);

        var clamped = 0;

        for (var my = 0; my < dimension; my++) {
            if (budget.IsExpired) return false;
            for (var mx = 0; mx < dimension; mx++) {
                if (budget.IsExpired) return false;
                var mxc = mx + 0.5 + phaseX;
                var myc = my + 0.5 + phaseY;

                transform.Transform(mxc, myc, out var sx, out var sy);
                if (double.IsNaN(sx) || double.IsNaN(sy)) return false;

                if (sx < 0) { sx = 0; clamped++; }
                else if (sx > image.Width - 1) { sx = image.Width - 1; clamped++; }

                if (sy < 0) { sy = 0; clamped++; }
                else if (sy > image.Height - 1) { sy = image.Height - 1; clamped++; }

                // When modules are reasonably large, nearest-neighbor majority sampling is more stable than bilinear
                // (bilinear can blur binary UI edges into mid-gray values around the threshold).
                // Prefer a tighter sampling pattern for typical UI-rendered QRs (3–6 px/module).
                // 5x5 sampling is more sensitive to small transform errors; use it only when modules are large.
                if (moduleSizePx >= 6.0) {
                    bm[mx, my] = loose
                        ? QrPixelSampling.SampleModule25NearestLoose(image, sx, sy, invert, moduleSizePx)
                        : QrPixelSampling.SampleModule25Nearest(image, sx, sy, invert, moduleSizePx);
                } else if (moduleSizePx >= 1.25) {
                    bm[mx, my] = loose
                        ? QrPixelSampling.SampleModule9NearestLoose(image, sx, sy, invert, moduleSizePx)
                        : QrPixelSampling.SampleModule9Nearest(image, sx, sy, invert, moduleSizePx);
                } else {
                    bm[mx, my] = loose
                        ? QrPixelSampling.SampleModule9PxLoose(image, sx, sy, invert, moduleSizePx)
                        : QrPixelSampling.SampleModule9Px(image, sx, sy, invert, moduleSizePx);
                }
            }
        }

        // If we had to clamp too many samples, the region is likely cropped too tight or the estimate is wrong.
        if (clamped > dimension * 2) return false;

        if (budget.IsNearDeadline(120)) return false;
        Func<bool>? shouldStop = budget.Enabled || budget.IsCancelled ? () => budget.IsExpired : null;
        if (global::CodeGlyphX.QrDecoder.TryDecodeInternal(bm, shouldStop, out result, out global::CodeGlyphX.QrDecodeDiagnostics moduleDiag)) {
            moduleDiagnostics = moduleDiag;
            if (accept == null || accept(result)) return true;
            return false;
        }

        var inv = bm.Clone();
        Invert(inv);
        if (budget.IsNearDeadline(120)) return false;
        if (global::CodeGlyphX.QrDecoder.TryDecodeInternal(inv, shouldStop, out result, out global::CodeGlyphX.QrDecodeDiagnostics moduleDiagInv)) {
            moduleDiagnostics = moduleDiagInv;
            if (accept == null || accept(result)) return true;
            return false;
        }

        moduleDiagnostics = Better(moduleDiag, moduleDiagInv);
        return false;
    }

    private static bool ShouldTryLooseSampling(global::CodeGlyphX.QrDecodeDiagnostics diag, double moduleSizePx) {
        if (moduleSizePx < 1.0) return false;
        return diag.Failure is global::CodeGlyphX.QrDecodeFailure.ReedSolomon or global::CodeGlyphX.QrDecodeFailure.Payload;
    }

    private static bool TryDecodeByBoundingBox(
        int scale,
        byte threshold,
        QrGrayImage image,
        bool invert,
        Func<QrDecoded, bool>? accept,
        bool aggressive,
        DecodeBudget budget,
        out QrDecoded result,
        out QrPixelDecodeDiagnostics diagnostics,
        int candidateCount = 0,
        int candidateTriplesTried = 0,
        int scanMinX = 0,
        int scanMinY = 0,
        int scanMaxX = -1,
        int scanMaxY = -1) {
        result = null!;
        diagnostics = default;

        if (budget.IsExpired) return false;
        if (scanMaxX < 0) scanMaxX = image.Width - 1;
        if (scanMaxY < 0) scanMaxY = image.Height - 1;
        if (scanMinX < 0) scanMinX = 0;
        if (scanMinY < 0) scanMinY = 0;
        if (scanMaxX >= image.Width) scanMaxX = image.Width - 1;
        if (scanMaxY >= image.Height) scanMaxY = image.Height - 1;
        if (scanMinX > scanMaxX || scanMinY > scanMaxY) return false;

        var minX = scanMaxX;
        var minY = scanMaxY;
        var maxX = -1;
        var maxY = -1;

        for (var y = scanMinY; y <= scanMaxY; y++) {
            if (budget.IsExpired) return false;
            for (var x = scanMinX; x <= scanMaxX; x++) {
                if (!image.IsBlack(x, y, invert)) continue;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
        }

        if (maxX < 0) return false;

        TrimBoundingBox(image, invert, ref minX, ref minY, ref maxX, ref maxY);
        if (maxX < minX || maxY < minY) return false;

        // Expand a touch to counter anti-aliasing that can shrink the detected black bbox.
        if (minX > 0) minX--;
        if (minY > 0) minY--;
        if (maxX < image.Width - 1) maxX++;
        if (maxY < image.Height - 1) maxY++;

        var boxW = maxX - minX + 1;
        var boxH = maxY - minY + 1;
        if (boxW <= 0 || boxH <= 0) return false;

        var maxModules = Math.Min(boxW, boxH);
        var maxVersion = Math.Min(40, (maxModules - 17) / 4);
        if (budget.Enabled && maxVersion > 10) {
            maxVersion = 10;
        }
        if (maxVersion < 1) return false;

        // Try smaller versions first (more likely for OTP QR), but accept non-integer module sizes.
        var best = default(QrPixelDecodeDiagnostics);
        for (var version = 1; version <= maxVersion; version++) {
            if (budget.IsExpired) return false;
            var modulesCount = version * 4 + 17;
            var moduleSizeX = boxW / (double)modulesCount;
            var moduleSizeY = boxH / (double)modulesCount;
            if (moduleSizeX < 1.0 || moduleSizeY < 1.0) continue;

            var relDiff = Math.Abs(moduleSizeX - moduleSizeY) / Math.Max(moduleSizeX, moduleSizeY);
            if (relDiff > 0.20) continue;

            var bm = new global::CodeGlyphX.BitMatrix(modulesCount, modulesCount);
            for (var my = 0; my < modulesCount; my++) {
                if (budget.IsExpired) return false;
                var sy = minY + (my + 0.5) * moduleSizeY;
                var py = QrMath.RoundToInt(sy);
                if (py < 0) py = 0;
                else if (py >= image.Height) py = image.Height - 1;

                for (var mx = 0; mx < modulesCount; mx++) {
                    var sx = minX + (mx + 0.5) * moduleSizeX;
                    var px = QrMath.RoundToInt(sx);
                    if (px < 0) px = 0;
                    else if (px >= image.Width) px = image.Width - 1;

                    bm[mx, my] = SampleMajority3x3(image, px, py, invert);
                }
            }

            if (budget.IsNearDeadline(120)) return false;
            if (TryDecodeWithInversion(bm, accept, budget, out result, out var moduleDiag)) {
                diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, modulesCount, moduleDiag);
                return true;
            }
            best = Better(best, new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, modulesCount, moduleDiag));

            if (budget.IsNearDeadline(120)) return false;
            if (TryDecodeByRotations(bm, accept, budget, out result, out var moduleDiagRot)) {
                diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, modulesCount, moduleDiagRot);
                return true;
            }
            best = Better(best, new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, modulesCount, moduleDiagRot));
        }

        diagnostics = best;
        return false;
    }

    private static bool TryGetCandidateBounds(
        QrFinderPatternDetector.FinderPattern tl,
        QrFinderPatternDetector.FinderPattern tr,
        QrFinderPatternDetector.FinderPattern bl,
        int width,
        int height,
        out int minX,
        out int minY,
        out int maxX,
        out int maxY) {
        var moduleSize = (tl.ModuleSize + tr.ModuleSize + bl.ModuleSize) / 3.0;
        if (moduleSize <= 0) {
            minX = minY = maxX = maxY = 0;
            return false;
        }

        var pad = moduleSize * 8.0;
        var minXF = Math.Min(tl.X, Math.Min(tr.X, bl.X)) - pad;
        var maxXF = Math.Max(tl.X, Math.Max(tr.X, bl.X)) + pad;
        var minYF = Math.Min(tl.Y, Math.Min(tr.Y, bl.Y)) - pad;
        var maxYF = Math.Max(tl.Y, Math.Max(tr.Y, bl.Y)) + pad;

        minX = Math.Clamp(QrMath.RoundToInt(minXF), 0, width - 1);
        maxX = Math.Clamp(QrMath.RoundToInt(maxXF), 0, width - 1);
        minY = Math.Clamp(QrMath.RoundToInt(minYF), 0, height - 1);
        maxY = Math.Clamp(QrMath.RoundToInt(maxYF), 0, height - 1);

        return minX < maxX && minY < maxY;
    }

    private static void TrimBoundingBox(QrGrayImage image, bool invert, ref int minX, ref int minY, ref int maxX, ref int maxY) {
        var width = maxX - minX + 1;
        var height = maxY - minY + 1;
        if (width <= 0 || height <= 0) return;

        var rowThreshold = Math.Max(2, width / 40);
        var colThreshold = Math.Max(2, height / 40);

        while (minY <= maxY && CountDarkRow(image, invert, minX, maxX, minY) <= rowThreshold) minY++;
        while (minY <= maxY && CountDarkRow(image, invert, minX, maxX, maxY) <= rowThreshold) maxY--;
        while (minX <= maxX && CountDarkCol(image, invert, minX, minY, maxY) <= colThreshold) minX++;
        while (minX <= maxX && CountDarkCol(image, invert, maxX, minY, maxY) <= colThreshold) maxX--;
    }

    private static int CountDarkRow(QrGrayImage image, bool invert, int minX, int maxX, int y) {
        var count = 0;
        var width = maxX - minX + 1;
        if (width <= 0) return 0;

        if (image.ThresholdMap is null && Sse2.IsSupported && width >= 16) {
            var gray = image.Gray;
            var start = y * image.Width + minX;
            var end = start + width;
            var i = start;
            var offset = Vector128.Create((byte)0x80);
            var threshold = Vector128.Create((byte)(image.Threshold ^ 0x80));

            while (i + 16 <= end) {
                var vec = MemoryMarshal.Read<Vector128<byte>>(gray.AsSpan(i));
                var signed = Sse2.Xor(vec, offset).AsSByte();
                var gt = Sse2.CompareGreaterThan(signed, threshold.AsSByte());
                var mask = (uint)Sse2.MoveMask(gt.AsByte());
                count += BitOperations.PopCount(mask);
                i += 16;
            }

            for (; i < end; i++) {
                if (gray[i] > image.Threshold) count++;
            }

            return invert ? count : width - count;
        }

        for (var x = minX; x <= maxX; x++) {
            if (image.IsBlack(x, y, invert)) count++;
        }
        return count;
    }

    private static int CountDarkCol(QrGrayImage image, bool invert, int x, int minY, int maxY) {
        var count = 0;
        for (var y = minY; y <= maxY; y++) {
            if (image.IsBlack(x, y, invert)) count++;
        }
        return count;
    }

    private static int NearestValidDimension(int dimension) {
        // Valid sizes are 17 + 4*version => dimension mod 4 == 1.
        var best = -1;
        var bestDiff = int.MaxValue;

        for (var delta = -2; delta <= 2; delta++) {
            var d = dimension + delta;
            if (d < 21 || d > 177) continue;
            if ((d & 3) != 1) continue;

            var diff = Math.Abs(delta);
            if (diff < bestDiff) {
                bestDiff = diff;
                best = d;
            }
        }

        return best;
    }

    private static double Distance(double x1, double y1, double x2, double y2) {
        var dx = x1 - x2;
        var dy = y1 - y2;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static bool SampleMajority3x3(QrGrayImage image, int px, int py, bool invert) {
        var black = 0;

        for (var dy = -1; dy <= 1; dy++) {
            var y = py + dy;
            if (y < 0) y = 0;
            else if (y >= image.Height) y = image.Height - 1;

            for (var dx = -1; dx <= 1; dx++) {
                var x = px + dx;
                if (x < 0) x = 0;
                else if (x >= image.Width) x = image.Width - 1;

                if (image.IsBlack(x, y, invert)) black++;
            }
        }

        return black >= 5;
    }

    private static void Invert(global::CodeGlyphX.BitMatrix matrix) {
        for (var y = 0; y < matrix.Height; y++) {
            for (var x = 0; x < matrix.Width; x++) {
                matrix[x, y] = !matrix[x, y];
            }
        }
    }

    private static bool TryDecodeWithInversion(global::CodeGlyphX.BitMatrix matrix, Func<QrDecoded, bool>? accept, DecodeBudget budget, out QrDecoded result, out global::CodeGlyphX.QrDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        Func<bool>? shouldStop = budget.Enabled || budget.IsCancelled ? () => budget.IsExpired : null;
        var ok = global::CodeGlyphX.QrDecoder.TryDecodeInternal(matrix, shouldStop, out result, out global::CodeGlyphX.QrDecodeDiagnostics moduleDiag);
        var best = moduleDiag;
        if (ok && (accept == null || accept(result))) {
            diagnostics = moduleDiag;
            return true;
        }

        var inv = matrix.Clone();
        Invert(inv);
        ok = global::CodeGlyphX.QrDecoder.TryDecodeInternal(inv, shouldStop, out result, out global::CodeGlyphX.QrDecodeDiagnostics moduleDiagInv);
        best = Better(best, moduleDiagInv);

        if (ok && (accept == null || accept(result))) {
            diagnostics = moduleDiagInv;
            return true;
        }

        diagnostics = best;
        return false;
    }

    private static bool TryDecodeByRotations(global::CodeGlyphX.BitMatrix matrix, Func<QrDecoded, bool>? accept, DecodeBudget budget, out QrDecoded result, out global::CodeGlyphX.QrDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        var best = default(global::CodeGlyphX.QrDecodeDiagnostics);

        var rot90 = Rotate90(matrix);
        if (TryDecodeWithInversion(rot90, accept, budget, out result, out var d90)) {
            diagnostics = d90;
            return true;
        }
        best = Better(best, d90);

        var rot180 = Rotate180(matrix);
        if (TryDecodeWithInversion(rot180, accept, budget, out result, out var d180)) {
            diagnostics = d180;
            return true;
        }
        best = Better(best, d180);

        var rot270 = Rotate270(matrix);
        if (TryDecodeWithInversion(rot270, accept, budget, out result, out var d270)) {
            diagnostics = d270;
            return true;
        }
        best = Better(best, d270);

        diagnostics = best;
        return false;
    }

    private static global::CodeGlyphX.BitMatrix Rotate90(global::CodeGlyphX.BitMatrix matrix) {
        var result = new global::CodeGlyphX.BitMatrix(matrix.Height, matrix.Width);
        for (var y = 0; y < matrix.Height; y++) {
            for (var x = 0; x < matrix.Width; x++) {
                result[matrix.Height - 1 - y, x] = matrix[x, y];
            }
        }
        return result;
    }

    private static global::CodeGlyphX.BitMatrix Rotate180(global::CodeGlyphX.BitMatrix matrix) {
        var result = new global::CodeGlyphX.BitMatrix(matrix.Width, matrix.Height);
        for (var y = 0; y < matrix.Height; y++) {
            for (var x = 0; x < matrix.Width; x++) {
                result[matrix.Width - 1 - x, matrix.Height - 1 - y] = matrix[x, y];
            }
        }
        return result;
    }

    private static global::CodeGlyphX.BitMatrix Rotate270(global::CodeGlyphX.BitMatrix matrix) {
        var result = new global::CodeGlyphX.BitMatrix(matrix.Height, matrix.Width);
        for (var y = 0; y < matrix.Height; y++) {
            for (var x = 0; x < matrix.Width; x++) {
                result[y, matrix.Width - 1 - x] = matrix[x, y];
            }
        }
        return result;
    }

    private static QrPixelDecodeDiagnostics Better(QrPixelDecodeDiagnostics a, QrPixelDecodeDiagnostics b) {
        if (IsEmpty(a)) return b;
        if (IsEmpty(b)) return a;

        // Pick the attempt that got "furthest" (format ok > RS > payload), then the one with lower format distance.
        var sa = Score(a.ModuleDiagnostics);
        var sb = Score(b.ModuleDiagnostics);
        if (sb > sa) return b;
        if (sa > sb) return a;

        var da = a.ModuleDiagnostics.FormatBestDistance;
        var db = b.ModuleDiagnostics.FormatBestDistance;
        if (db >= 0 && (da < 0 || db < da)) return b;
        return a;
    }

    private static global::CodeGlyphX.QrDecodeDiagnostics Better(global::CodeGlyphX.QrDecodeDiagnostics a, global::CodeGlyphX.QrDecodeDiagnostics b) {
        if (IsEmpty(a)) return b;
        if (IsEmpty(b)) return a;

        var sa = Score(a);
        var sb = Score(b);
        if (sb > sa) return b;
        if (sa > sb) return a;

        var da = a.FormatBestDistance;
        var db = b.FormatBestDistance;
        if (db >= 0 && (da < 0 || db < da)) return b;
        return a;
    }

    private static bool IsEmpty(QrPixelDecodeDiagnostics d) {
        return d.Scale == 0 && d.Dimension == 0 && d.CandidateCount == 0 && d.CandidateTriplesTried == 0 &&
               d.ModuleDiagnostics.Version == 0 && d.ModuleDiagnostics.Failure == global::CodeGlyphX.QrDecodeFailure.None;
    }

    private static bool IsEmpty(global::CodeGlyphX.QrDecodeDiagnostics d) {
        return d.Version == 0 && d.Failure == global::CodeGlyphX.QrDecodeFailure.None;
    }

    private static int Score(global::CodeGlyphX.QrDecodeDiagnostics d) {
        return d.Failure switch {
            global::CodeGlyphX.QrDecodeFailure.None => 5,
            global::CodeGlyphX.QrDecodeFailure.Payload => 4,
            global::CodeGlyphX.QrDecodeFailure.ReedSolomon => 3,
            global::CodeGlyphX.QrDecodeFailure.FormatInfo => 2,
            global::CodeGlyphX.QrDecodeFailure.InvalidSize => 1,
            _ => 0,
        };
    }
}
#endif
