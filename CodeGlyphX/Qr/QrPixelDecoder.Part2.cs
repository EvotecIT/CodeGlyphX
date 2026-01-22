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
        var pool = new QrGrayImagePool();
        try {
            if (!QrGrayImage.TryCreate(pixels, width, height, stride, fmt, scale, settings.MinContrast, shouldStop, pool, out var baseImage)) {
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

        var candidates = new List<QrFinderPatternDetector.FinderPattern>(8);

            if (TryDecodeWithImage(scale, baseImage, fastSettings, candidates, budget, accept, pool, out result, out var diagFast)) {
                diagnostics = diagFast;
                return true;
            }

            // Track the closest unsuccessful attempt for diagnostics.
            var best = diagFast;

            if (scale == 1 && settings.AllowTransforms) {
                if (TryDecodeWithTransformsFast(scale, baseImage, fastSettings, candidates, budget, accept, pool, out result, out var diagFastTransform)) {
                    diagnostics = diagFastTransform;
                    return true;
                }
                best = Better(best, diagFastTransform);
            }

            if (TryDecodeImageAndStretch(scale, baseImage, settings, candidates, budget, accept, pool, out result, out var diagBase)) {
                diagnostics = diagBase;
                return true;
            }

            best = Better(best, diagBase);

            if (scale == 1 && settings.AllowTransforms) {
                if (TryDecodeWithTransforms(scale, baseImage, settings, candidates, budget, accept, pool, out result, out var diagTransform)) {
                    diagnostics = diagTransform;
                    return true;
                }
                best = Better(best, diagTransform);
            }

            diagnostics = best;
            return false;
        } finally {
            pool.ReturnAll();
        }
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
        List<QrFinderPatternDetector.FinderPattern> candidates,
        DecodeBudget budget,
        Func<QrDecoded, bool>? accept,
        QrGrayImagePool? pool,
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
            var adaptive = baseImage.WithAdaptiveThreshold(windowSize: 15, offset: 8, pool);
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

            var adaptiveSoft = baseImage.WithAdaptiveThreshold(windowSize: 25, offset: 4, pool);
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
                var adaptiveUltra = baseImage.WithAdaptiveThreshold(windowSize: 31, offset: 0, pool);
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
            var blurred = baseImage.WithBoxBlur(radius: 1, pool);
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

            var adaptiveBlur = blurred.WithAdaptiveThreshold(windowSize: 17, offset: 6, pool);
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
                var blurred2 = baseImage.WithBoxBlur(radius: 2, pool);
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
        List<QrFinderPatternDetector.FinderPattern> candidates,
        DecodeBudget budget,
        Func<QrDecoded, bool>? accept,
        QrGrayImagePool? pool,
        out QrDecoded result,
        out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (budget.IsExpired || budget.IsNearDeadline(120)) return false;

        if (TryDecodeWithImage(scale, baseImage, settings, candidates, budget, accept, pool, out result, out var diagBase)) {
            diagnostics = diagBase;
            return true;
        }

        var best = diagBase;

        if (settings.AggressiveSampling) {
            if (budget.IsNearDeadline(150)) {
                diagnostics = best;
                return false;
            }
            var boosted = baseImage.WithBinaryBoost(12, pool);
            if (!ReferenceEquals(boosted.Gray, baseImage.Gray)) {
                if (TryDecodeWithImage(scale, boosted, settings, candidates, budget, accept, pool, out result, out var diagBoost)) {
                    diagnostics = diagBoost;
                    return true;
                }
                best = Better(best, diagBoost);
            }
        }

        if (settings.AllowNormalize) {
            if (budget.IsNearDeadline(150)) {
                diagnostics = best;
                return false;
            }
            var normalized = baseImage.WithLocalNormalize(GetNormalizeWindow(baseImage), pool);
            if (TryDecodeWithImage(scale, normalized, settings, candidates, budget, accept, pool, out result, out var diagNorm)) {
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
                var stretched = baseImage.WithContrastStretch(48, pool);
                if (!ReferenceEquals(stretched.Gray, baseImage.Gray)) {
                    if (TryDecodeWithImage(scale, stretched, settings, candidates, budget, accept, pool, out result, out var diagStretch)) {
                        diagnostics = diagStretch;
                        return true;
                    }
                    best = Better(best, diagStretch);

                    if (settings.AllowNormalize) {
                        var normStretch = stretched.WithLocalNormalize(GetNormalizeWindow(stretched), pool);
                        if (TryDecodeWithImage(scale, normStretch, settings, candidates, budget, accept, pool, out result, out var diagNormStretch)) {
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
        List<QrFinderPatternDetector.FinderPattern> candidates,
        DecodeBudget budget,
        Func<QrDecoded, bool>? accept,
        QrGrayImagePool? pool,
        out QrDecoded result,
        out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        var best = default(QrPixelDecodeDiagnostics);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var rot90 = baseImage.Rotate90(pool);
        if (budget.IsExpired) return false;
        if (TryDecodeImageAndStretch(scale, rot90, settings, candidates, budget, accept, pool, out result, out var d90)) {
            diagnostics = d90;
            return true;
        }
        best = Better(best, d90);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var rot180 = baseImage.Rotate180(pool);
        if (budget.IsExpired) return false;
        if (TryDecodeImageAndStretch(scale, rot180, settings, candidates, budget, accept, pool, out result, out var d180)) {
            diagnostics = d180;
            return true;
        }
        best = Better(best, d180);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var rot270 = baseImage.Rotate270(pool);
        if (budget.IsExpired) return false;
        if (TryDecodeImageAndStretch(scale, rot270, settings, candidates, budget, accept, pool, out result, out var d270)) {
            diagnostics = d270;
            return true;
        }
        best = Better(best, d270);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var mirror = baseImage.MirrorX(pool);
        if (budget.IsExpired) return false;
        if (TryDecodeImageAndStretch(scale, mirror, settings, candidates, budget, accept, pool, out result, out var dm0)) {
            diagnostics = dm0;
            return true;
        }
        best = Better(best, dm0);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var mirror90 = mirror.Rotate90(pool);
        if (budget.IsExpired) return false;
        if (TryDecodeImageAndStretch(scale, mirror90, settings, candidates, budget, accept, pool, out result, out var dm90)) {
            diagnostics = dm90;
            return true;
        }
        best = Better(best, dm90);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var mirror180 = mirror.Rotate180(pool);
        if (budget.IsExpired) return false;
        if (TryDecodeImageAndStretch(scale, mirror180, settings, candidates, budget, accept, pool, out result, out var dm180)) {
            diagnostics = dm180;
            return true;
        }
        best = Better(best, dm180);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var mirror270 = mirror.Rotate270(pool);
        if (budget.IsExpired) return false;
        if (TryDecodeImageAndStretch(scale, mirror270, settings, candidates, budget, accept, pool, out result, out var dm270)) {
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
        List<QrFinderPatternDetector.FinderPattern> candidates,
        DecodeBudget budget,
        Func<QrDecoded, bool>? accept,
        QrGrayImagePool? pool,
        out QrDecoded result,
        out QrPixelDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        var best = default(QrPixelDecodeDiagnostics);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var rot90 = baseImage.Rotate90(pool);
        if (budget.IsExpired) return false;
        if (TryDecodeWithImage(scale, rot90, settings, candidates, budget, accept, pool, out result, out var d90)) {
            diagnostics = d90;
            return true;
        }
        best = Better(best, d90);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var rot180 = baseImage.Rotate180(pool);
        if (budget.IsExpired) return false;
        if (TryDecodeWithImage(scale, rot180, settings, candidates, budget, accept, pool, out result, out var d180)) {
            diagnostics = d180;
            return true;
        }
        best = Better(best, d180);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var rot270 = baseImage.Rotate270(pool);
        if (budget.IsExpired) return false;
        if (TryDecodeWithImage(scale, rot270, settings, candidates, budget, accept, pool, out result, out var d270)) {
            diagnostics = d270;
            return true;
        }
        best = Better(best, d270);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var mirror = baseImage.MirrorX(pool);
        if (budget.IsExpired) return false;
        if (TryDecodeWithImage(scale, mirror, settings, candidates, budget, accept, pool, out result, out var dm0)) {
            diagnostics = dm0;
            return true;
        }
        best = Better(best, dm0);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var mirror90 = mirror.Rotate90(pool);
        if (budget.IsExpired) return false;
        if (TryDecodeWithImage(scale, mirror90, settings, candidates, budget, accept, pool, out result, out var dm90)) {
            diagnostics = dm90;
            return true;
        }
        best = Better(best, dm90);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var mirror180 = mirror.Rotate180(pool);
        if (budget.IsExpired) return false;
        if (TryDecodeWithImage(scale, mirror180, settings, candidates, budget, accept, pool, out result, out var dm180)) {
            diagnostics = dm180;
            return true;
        }
        best = Better(best, dm180);

        if (budget.IsNearDeadline(150)) {
            diagnostics = best;
            return false;
        }
        var mirror270 = mirror.Rotate270(pool);
        if (budget.IsExpired) return false;
        if (TryDecodeWithImage(scale, mirror270, settings, candidates, budget, accept, pool, out result, out var dm270)) {
            diagnostics = dm270;
            return true;
        }
        best = Better(best, dm270);

        diagnostics = best;
        return false;
    }

    private static void CollectAllFromImage(QrGrayImage baseImage, QrProfileSettings settings, PooledList<QrDecoded> list, HashSet<string> seen, Func<QrDecoded, bool>? accept, DecodeBudget budget, QrGrayImagePool? pool) {
        if (budget.IsExpired) return;
        var candidates = new List<QrFinderPatternDetector.FinderPattern>(8);
        CollectAllFromImageCore(baseImage, settings, list, seen, accept, budget, candidates, pool);
        if (settings.AllowNormalize) {
            if (budget.IsNearDeadline(150)) return;
            var normalized = baseImage.WithLocalNormalize(GetNormalizeWindow(baseImage), pool);
            if (!budget.IsExpired) {
                CollectAllFromImageCore(normalized, settings, list, seen, accept, budget, candidates, pool);
            }
        }
    }

    private static void CollectAllFromImageCore(QrGrayImage baseImage, QrProfileSettings settings, PooledList<QrDecoded> list, HashSet<string> seen, Func<QrDecoded, bool>? accept, DecodeBudget budget, List<QrFinderPatternDetector.FinderPattern> candidates, QrGrayImagePool? pool) {
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
            var adaptive = baseImage.WithAdaptiveThreshold(windowSize: 15, offset: 8, pool);
            CollectFromImage(adaptive, invert: false, list, seen, accept, candidates, budget, settings.AggressiveSampling);
            CollectFromImage(adaptive, invert: true, list, seen, accept, candidates, budget, settings.AggressiveSampling);

            if (!budget.IsExpired && settings.AllowBlur && settings.AggressiveSampling) {
                if (budget.IsNearDeadline(150)) return;
                var adaptiveSoft = baseImage.WithAdaptiveThreshold(windowSize: 31, offset: 0, pool);
                CollectFromImage(adaptiveSoft, invert: false, list, seen, accept, candidates, budget, settings.AggressiveSampling);
                CollectFromImage(adaptiveSoft, invert: true, list, seen, accept, candidates, budget, settings.AggressiveSampling);
            }
        }
    }

}
#endif
