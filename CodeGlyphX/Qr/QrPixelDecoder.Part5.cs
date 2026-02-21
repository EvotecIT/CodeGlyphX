#if NET8_0_OR_GREATER
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using System.IO;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelDecoder {
    private static string? ModuleDumpDirCache;
    private static int ModuleDumpCount;
    private static bool TrySampleWithCorners(
        QrGrayImage image,
        bool invert,
        double phaseX,
        double phaseY,
        int dimension,
        global::CodeGlyphX.BitMatrix scratch,
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
        bool stylized,
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
                scratch,
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
                aggressive,
                stylized,
                loose: false,
                centerSampling: false,
                ringSampling: false,
                out result,
                out moduleDiagnostics)) {
            return true;
        }

        var strictDiag = moduleDiagnostics;
        var looseDiag = default(global::CodeGlyphX.QrDecodeDiagnostics);

        if (aggressive && ShouldTryLooseSampling(strictDiag, moduleSizePx) &&
            TrySampleWithCornersInternal(
                image,
                invert,
                phaseX,
                phaseY,
                dimension,
                scratch,
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
                aggressive,
                stylized,
                loose: true,
                centerSampling: false,
                ringSampling: false,
                out result,
                out looseDiag)) {
            moduleDiagnostics = looseDiag;
            return true;
        }

        var best = Better(strictDiag, looseDiag);
        if (aggressive && ShouldTryCenterSampling(best, moduleSizePx)) {
            if (TrySampleWithCornersInternal(
                    image,
                    invert,
                    phaseX,
                    phaseY,
                    dimension,
                    scratch,
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
                    aggressive,
                    stylized,
                    loose: false,
                    centerSampling: true,
                    ringSampling: false,
                    out result,
                    out var centerDiag)) {
                moduleDiagnostics = centerDiag;
                return true;
            }

            var centerLooseDiag = default(global::CodeGlyphX.QrDecodeDiagnostics);
            if (ShouldTryLooseSampling(centerDiag, moduleSizePx) &&
                TrySampleWithCornersInternal(
                    image,
                    invert,
                    phaseX,
                    phaseY,
                    dimension,
                    scratch,
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
                    aggressive,
                    stylized,
                    loose: true,
                    centerSampling: true,
                    ringSampling: false,
                    out result,
                    out centerLooseDiag)) {
                moduleDiagnostics = centerLooseDiag;
                return true;
            }

            best = Better(best, centerDiag);
            best = Better(best, centerLooseDiag);
        }

        if (stylized && moduleSizePx >= 1.1 && !budget.IsNearDeadline(150)) {
            if (TrySampleWithCornersInternal(
                    image,
                    invert,
                    phaseX,
                    phaseY,
                    dimension,
                    scratch,
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
                    aggressive,
                    stylized,
                    loose: false,
                    centerSampling: false,
                    ringSampling: true,
                    out result,
                    out var ringDiag)) {
                moduleDiagnostics = ringDiag;
                return true;
            }

            var ringLooseDiag = default(global::CodeGlyphX.QrDecodeDiagnostics);
            if (ShouldTryLooseSampling(ringDiag, moduleSizePx) &&
                TrySampleWithCornersInternal(
                    image,
                    invert,
                    phaseX,
                    phaseY,
                    dimension,
                    scratch,
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
                    aggressive,
                    stylized,
                    loose: true,
                    centerSampling: false,
                    ringSampling: true,
                    out result,
                    out ringLooseDiag)) {
                moduleDiagnostics = ringLooseDiag;
                return true;
            }

            best = Better(best, ringDiag);
            best = Better(best, ringLooseDiag);
        }

        moduleDiagnostics = best;
        return false;
    }


    private static bool TrySampleWithCornersInternal(
        QrGrayImage image,
        bool invert,
        double phaseX,
        double phaseY,
        int dimension,
        global::CodeGlyphX.BitMatrix scratch,
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
        bool aggressive,
        bool stylized,
        bool loose,
        bool centerSampling,
        bool ringSampling,
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

        var bm = scratch;
        bm.Clear();
        var clampedLimit = stylized ? dimension * 3 : dimension * 2;
        var useRing = ringSampling && moduleSizePx >= 1.1;
        var preferDenseSampling = stylized && moduleSizePx >= 3.5;
        var mode = (centerSampling && moduleSizePx >= 1.25)
            ? 0
            : preferDenseSampling || moduleSizePx >= 6.0
                ? 1
                : moduleSizePx >= 1.25
                    ? 2
                    : 3;
        var useMeanSampling = stylized && mode == 1 && dimension >= 25;
        var useLooseMeanSampling = useMeanSampling && dimension >= 65 && moduleSizePx >= 8.0;
        var useExtraLooseMeanSampling = useMeanSampling && dimension == 41 && moduleSizePx >= 8.0;
        var useWideMeanDelta = useMeanSampling && stylized && dimension >= 25 && moduleSizePx >= 6.0;
        var delta = useRing
            ? QrPixelSampling.GetSampleDeltaRingForModule(moduleSizePx)
            : mode switch {
                0 => QrPixelSampling.GetSampleDeltaCenterForModule(moduleSizePx),
                1 => useWideMeanDelta
                    ? QrPixelSampling.GetSampleDelta5x5WideForModule(moduleSizePx)
                    : QrPixelSampling.GetSampleDelta5x5ForModule(moduleSizePx),
                _ => QrPixelSampling.GetSampleDeltaForModule(moduleSizePx)
            };

        var sampledOk = useRing
            ? (loose
                ? SampleModules<NinePxLooseSampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _)
                : SampleModules<NinePxSampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _))
            : mode switch {
                0 => loose
                    ? SampleModules<Center3x3LooseSampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _)
                    : SampleModules<Center3x3Sampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _),
                1 => loose
                    ? (useMeanSampling
                        ? (useExtraLooseMeanSampling
                            ? SampleModules<Mean25ExtraLooseSampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _)
                            : SampleModules<Mean25LooseSampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _))
                        : SampleModules<Nearest25LooseSampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _))
                    : (useMeanSampling
                        ? (useExtraLooseMeanSampling
                            ? SampleModules<Mean25ExtraLooseSampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _)
                            : useLooseMeanSampling
                                ? SampleModules<Mean25LooseSampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _)
                                : SampleModules<Mean25Sampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _))
                        : SampleModules<Nearest25Sampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _)),
                2 => loose
                    ? SampleModules<Nearest9LooseSampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _)
                    : SampleModules<Nearest9Sampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _),
                _ => loose
                    ? SampleModules<NinePxLooseSampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _)
                    : SampleModules<NinePxSampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _)
            };

        if (!sampledOk) {
            moduleDiagnostics = new global::CodeGlyphX.QrDecodeDiagnostics(global::CodeGlyphX.QrDecodeFailure.InvalidInput);
            return false;
        }

        if (budget.IsNearDeadline(120)) return false;
        Func<bool>? shouldStop = budget.Enabled || budget.CanCancel ? () => budget.IsExpired : null;
        if (global::CodeGlyphX.QrDecoder.TryDecodeInternal(bm, shouldStop, out result, out global::CodeGlyphX.QrDecodeDiagnostics moduleDiag)) {
            moduleDiagnostics = moduleDiag;
            if (accept == null || accept(result)) return true;
            return false;
        }

        if (stylized &&
            moduleDiag.Failure is global::CodeGlyphX.QrDecodeFailure.Payload or global::CodeGlyphX.QrDecodeFailure.ReedSolomon &&
            !budget.IsNearDeadline(160) &&
            global::CodeGlyphX.QrDecoder.TryDecodeAllFormatCandidates(bm, shouldStop, out result, out var moduleDiagAll)) {
            moduleDiagnostics = moduleDiagAll;
            if (accept == null || accept(result)) return true;
            return false;
        }

        if (stylized &&
            mode == 1 &&
            moduleDiag.Failure is global::CodeGlyphX.QrDecodeFailure.Payload or global::CodeGlyphX.QrDecodeFailure.ReedSolomon &&
            !budget.IsNearDeadline(200)) {
            bm.Clear();
            var resampledOk = useMeanSampling
                ? SampleModules<Mean25ExtraLooseSampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _)
                : SampleModules<Mean25LooseSampler>(image, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _);
            if (resampledOk && global::CodeGlyphX.QrDecoder.TryDecodeInternal(bm, shouldStop, out result, out var moduleDiagMean)) {
                moduleDiagnostics = moduleDiagMean;
                if (accept == null || accept(result)) return true;
                return false;
            }

            if (!budget.IsNearDeadline(160) &&
                global::CodeGlyphX.QrDecoder.TryDecodeAllFormatCandidates(bm, shouldStop, out result, out var moduleDiagMeanAll)) {
                moduleDiagnostics = moduleDiagMeanAll;
                if (accept == null || accept(result)) return true;
                return false;
            }
        }

        if (stylized &&
            dimension >= 33 &&
            moduleSizePx >= 2.0 &&
            moduleDiag.Failure is global::CodeGlyphX.QrDecodeFailure.Payload or global::CodeGlyphX.QrDecodeFailure.ReedSolomon &&
            !budget.IsNearDeadline(220)) {
            if (TryDecodeWithFinderThreshold(image, invert, transform, dimension, phaseX, phaseY, delta, mode, useMeanSampling, loose, accept, budget, shouldStop, out result, out moduleDiagnostics)) {
                return true;
            }
        }

        if (stylized &&
            dimension >= 33 &&
            moduleSizePx >= 2.0 &&
            moduleDiag.Failure is global::CodeGlyphX.QrDecodeFailure.Payload or global::CodeGlyphX.QrDecodeFailure.ReedSolomon &&
            !budget.IsNearDeadline(220)) {
            if (TryDecodeWithPhaseMajority(image, invert, transform, dimension, phaseX, phaseY, delta, mode, useMeanSampling, loose, accept, budget, shouldStop, out result, out moduleDiagnostics)) {
                return true;
            }
        }

        if (stylized &&
            mode == 1 &&
            dimension >= 33 &&
            moduleSizePx >= 2.0 &&
            moduleDiag.Failure is global::CodeGlyphX.QrDecodeFailure.Payload or global::CodeGlyphX.QrDecodeFailure.ReedSolomon &&
            !budget.IsNearDeadline(220)) {
            var window = (int)Math.Round(moduleSizePx * 4.0);
            if (window < 17) window = 17;
            if (window > 51) window = 51;
            if ((window & 1) == 0) window++;

            var adaptive = image.WithAdaptiveThreshold(window, offset: 4);
            bm.Clear();
            var adaptiveOk = useMeanSampling
                ? SampleModules<Mean25LooseSampler>(adaptive, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _)
                : SampleModules<Nearest25LooseSampler>(adaptive, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _);
            if (adaptiveOk && global::CodeGlyphX.QrDecoder.TryDecodeInternal(bm, shouldStop, out result, out var moduleDiagAdaptive)) {
                moduleDiagnostics = moduleDiagAdaptive;
                if (accept == null || accept(result)) return true;
                return false;
            }

            if (!budget.IsNearDeadline(160) &&
                global::CodeGlyphX.QrDecoder.TryDecodeAllFormatCandidates(bm, shouldStop, out result, out var moduleDiagAdaptiveAll)) {
                moduleDiagnostics = moduleDiagAdaptiveAll;
                if (accept == null || accept(result)) return true;
                return false;
            }
        }

        if (stylized &&
            mode == 1 &&
            dimension >= 33 &&
            moduleSizePx >= 2.0 &&
            moduleDiag.Failure is global::CodeGlyphX.QrDecodeFailure.Payload or global::CodeGlyphX.QrDecodeFailure.ReedSolomon &&
            !budget.IsNearDeadline(240)) {
            var stretched = image.WithContrastStretch(minRange: 70);
            if (!ReferenceEquals(stretched.Gray, image.Gray)) {
                bm.Clear();
                var stretchOk = useMeanSampling
                    ? SampleModules<Mean25LooseSampler>(stretched, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _)
                    : SampleModules<Nearest25LooseSampler>(stretched, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _);
                if (stretchOk && global::CodeGlyphX.QrDecoder.TryDecodeInternal(bm, shouldStop, out result, out var moduleDiagStretch)) {
                    moduleDiagnostics = moduleDiagStretch;
                    if (accept == null || accept(result)) return true;
                    return false;
                }

                if (!budget.IsNearDeadline(160) &&
                    global::CodeGlyphX.QrDecoder.TryDecodeAllFormatCandidates(bm, shouldStop, out result, out var moduleDiagStretchAll)) {
                    moduleDiagnostics = moduleDiagStretchAll;
                    if (accept == null || accept(result)) return true;
                    return false;
                }

                if (!budget.IsNearDeadline(200)) {
                    var boosted = stretched.WithBinaryBoost(12);
                    if (!ReferenceEquals(boosted.Gray, stretched.Gray)) {
                        bm.Clear();
                        var boostOk = useMeanSampling
                            ? SampleModules<Mean25LooseSampler>(boosted, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _)
                            : SampleModules<Nearest25LooseSampler>(boosted, invert, transform, dimension, phaseX, phaseY, bm, budget, clampedLimit, delta, out _);
                        if (boostOk && global::CodeGlyphX.QrDecoder.TryDecodeInternal(bm, shouldStop, out result, out var moduleDiagBoost)) {
                            moduleDiagnostics = moduleDiagBoost;
                            if (accept == null || accept(result)) return true;
                            return false;
                        }

                        if (!budget.IsNearDeadline(160) &&
                            global::CodeGlyphX.QrDecoder.TryDecodeAllFormatCandidates(bm, shouldStop, out result, out var moduleDiagBoostAll)) {
                            moduleDiagnostics = moduleDiagBoostAll;
                            if (accept == null || accept(result)) return true;
                            return false;
                        }
                    }
                }
            }
        }

        if (stylized &&
            dimension >= 33 &&
            moduleSizePx >= 2.0 &&
            moduleDiag.Failure is global::CodeGlyphX.QrDecodeFailure.Payload or global::CodeGlyphX.QrDecodeFailure.ReedSolomon &&
            !budget.IsNearDeadline(260)) {
            if (TryDecodeWithDimensionSweep(image, invert, transform, dimension, moduleSizePx, phaseX, phaseY, delta, mode, useMeanSampling, loose, accept, budget, shouldStop, out result, out moduleDiagnostics)) {
                return true;
            }
        }

        if (stylized &&
            mode == 1 &&
            moduleDiag.Failure is global::CodeGlyphX.QrDecodeFailure.Payload or global::CodeGlyphX.QrDecodeFailure.ReedSolomon &&
            !budget.IsNearDeadline(220)) {
            var alt = new global::CodeGlyphX.BitMatrix(dimension, dimension);
            var altOk = useMeanSampling
                ? SampleModules<Nearest25LooseSampler>(image, invert, transform, dimension, phaseX, phaseY, alt, budget, clampedLimit, delta, out _)
                : SampleModules<Mean25LooseSampler>(image, invert, transform, dimension, phaseX, phaseY, alt, budget, clampedLimit, delta, out _);
            if (altOk) {
                var ratioA = ComputeBlackRatio(bm);
                var ratioB = ComputeBlackRatio(alt);
                var preferOr = ratioA < 0.48 || ratioB < 0.48;
                var preferAnd = ratioA > 0.60 || ratioB > 0.60;
                var combineOrFirst = preferOr && !preferAnd;
                if (!preferOr && !preferAnd) {
                    combineOrFirst = ratioA <= ratioB;
                }

                var combined = new global::CodeGlyphX.BitMatrix(dimension, dimension);
                var centerDelta = QrPixelSampling.GetSampleDeltaCenterForModule(moduleSizePx);
                var third = new global::CodeGlyphX.BitMatrix(dimension, dimension);
                var thirdOk = SampleModules<Center3x3LooseSampler>(image, invert, transform, dimension, phaseX, phaseY, third, budget, clampedLimit, centerDelta, out _);
                if (thirdOk &&
                    TryDecodeMajorityMatrices(bm, alt, third, combined, accept, budget, shouldStop, out result, out moduleDiagnostics)) {
                    return true;
                }
                if (TryDecodeCombinedMatrices(bm, alt, combined, combineOrFirst, accept, budget, shouldStop, out result, out moduleDiagnostics) ||
                    TryDecodeCombinedMatrices(bm, alt, combined, !combineOrFirst, accept, budget, shouldStop, out result, out moduleDiagnostics)) {
                    return true;
                }
                if (!budget.IsNearDeadline(220) &&
                    dimension <= 25 &&
                    TryDecodeWithFlipSearch(bm, alt, thirdOk ? third : null, accept, budget, shouldStop, out result, out moduleDiagnostics)) {
                    return true;
                }
            }
        }

        if (stylized &&
            dimension <= 45 &&
            moduleSizePx >= 2.0 &&
            moduleDiag.Failure is global::CodeGlyphX.QrDecodeFailure.Payload or global::CodeGlyphX.QrDecodeFailure.ReedSolomon &&
            !budget.IsNearDeadline(260)) {
            if (TryDecodeWithConfidenceFlips(image, invert, transform, dimension, phaseX, phaseY, moduleSizePx, bm, accept, budget, shouldStop, out result, out moduleDiagnostics)) {
                return true;
            }
        }

        if (stylized &&
            moduleDiag.Failure is global::CodeGlyphX.QrDecodeFailure.Payload or global::CodeGlyphX.QrDecodeFailure.ReedSolomon &&
            !budget.IsNearDeadline(200)) {
            var inferredVersion = (dimension - 17) / 4;
            if (inferredVersion is >= 1 and <= 40) {
                var corrected = new global::CodeGlyphX.BitMatrix(dimension, dimension);
                corrected.CopyFrom(bm);
                ApplyFunctionPatterns(corrected, inferredVersion);
                if (global::CodeGlyphX.QrDecoder.TryDecodeInternal(corrected, shouldStop, out result, out var moduleDiagCorrected)) {
                    moduleDiagnostics = moduleDiagCorrected;
                    if (accept == null || accept(result)) return true;
                    return false;
                }
                if (!budget.IsNearDeadline(160) &&
                    global::CodeGlyphX.QrDecoder.TryDecodeAllFormatCandidates(corrected, shouldStop, out result, out var moduleDiagCorrectedAll)) {
                    moduleDiagnostics = moduleDiagCorrectedAll;
                    if (accept == null || accept(result)) return true;
                    return false;
                }
            }
        }

        if (stylized &&
            dimension >= 25 &&
            moduleSizePx >= 3.5 &&
            moduleDiag.Failure is global::CodeGlyphX.QrDecodeFailure.Payload or global::CodeGlyphX.QrDecodeFailure.ReedSolomon &&
            !budget.IsNearDeadline(200)) {
            var timingRowAlt = CountTimingAlternationsRow(bm);
            var timingColAlt = CountTimingAlternationsCol(bm);
            var minAlt = Math.Max(6, dimension / 3);
            if (timingRowAlt < minAlt || timingColAlt < minAlt) {
                var baseScore = timingRowAlt + timingColAlt;
                var bestScore = -1;
                var bestPhaseX = phaseX;
                var bestPhaseY = phaseY;
                Span<double> phaseOffsets = stackalloc double[] { 0.0, -0.2, 0.2 };
                Span<double> scaleOffsets = stackalloc double[] { -0.02, 0.0, 0.02 };

                var bestScale = 1.0;
                for (var si = 0; si < scaleOffsets.Length; si++) {
                    if (budget.IsNearDeadline(200)) break;
                    var scale = 1.0 + scaleOffsets[si];
                    var deltaScaled = delta * scale;
                    for (var oy = 0; oy < phaseOffsets.Length; oy++) {
                        var phaseYp = phaseY + phaseOffsets[oy];
                        for (var ox = 0; ox < phaseOffsets.Length; ox++) {
                            if (budget.IsNearDeadline(160)) break;
                            var phaseXp = phaseX + phaseOffsets[ox];
                            bm.Clear();
                            var resampledOk = SampleModules<Nearest25ExtraLooseSampler>(
                                image,
                                invert,
                                transform,
                                dimension,
                                phaseXp,
                                phaseYp,
                                bm,
                                budget,
                                clampedLimit,
                                deltaScaled,
                                out _);
                            if (!resampledOk) continue;

                            var score = CountTimingAlternationsRow(bm) + CountTimingAlternationsCol(bm);
                            if (score > bestScore) {
                                bestScore = score;
                                bestPhaseX = phaseXp;
                                bestPhaseY = phaseYp;
                                bestScale = scale;
                            }
                        }
                    }
                }

                if (bestScore > baseScore) {
                    var deltaScaled = delta * bestScale;
                    bm.Clear();
                    var resampledOk = SampleModules<Nearest25ExtraLooseSampler>(
                        image,
                        invert,
                        transform,
                        dimension,
                        bestPhaseX,
                        bestPhaseY,
                        bm,
                        budget,
                        clampedLimit,
                        deltaScaled,
                        out _);
                    if (resampledOk && global::CodeGlyphX.QrDecoder.TryDecodeInternal(bm, shouldStop, out result, out var moduleDiagResample)) {
                        moduleDiagnostics = moduleDiagResample;
                        if (accept == null || accept(result)) return true;
                        return false;
                    }

                    if (!budget.IsNearDeadline(160)) {
                        var adaptive = image.WithAdaptiveThreshold(windowSize: 31, offset: 4);
                        bm.Clear();
                        var adaptiveOk = SampleModules<Nearest25ExtraLooseSampler>(
                            adaptive,
                            invert,
                            transform,
                            dimension,
                            bestPhaseX,
                            bestPhaseY,
                            bm,
                            budget,
                            clampedLimit,
                            deltaScaled,
                            out _);
                        if (adaptiveOk && global::CodeGlyphX.QrDecoder.TryDecodeInternal(bm, shouldStop, out result, out var moduleDiagAdaptive)) {
                            moduleDiagnostics = moduleDiagAdaptive;
                            if (accept == null || accept(result)) return true;
                            return false;
                        }
                    }
                }
            }
        }

        if (stylized && !budget.IsNearDeadline(120)) {
            MaybeDumpModuleMatrix(bm, dimension, moduleDiag, suffix: "raw");
        }

        if (stylized &&
            dimension >= 21 &&
            moduleSizePx >= 3.0 &&
            moduleDiag.Failure is global::CodeGlyphX.QrDecodeFailure.Payload or global::CodeGlyphX.QrDecodeFailure.ReedSolomon &&
            !budget.IsNearDeadline(180)) {
            var cleanupVersion = moduleDiag.Version;
            if (cleanupVersion <= 0) {
                cleanupVersion = (dimension - 17) / 4;
                if (cleanupVersion is < 1 or > 40) cleanupVersion = 0;
            }
            if (cleanupVersion > 0 &&
                TryDecodeWithCleanup(bm, cleanupVersion, accept, budget, shouldStop, out result, out var cleanedDiag)) {
                moduleDiagnostics = cleanedDiag;
                return true;
            }
        }

        if (budget.Enabled && budget.MaxMilliseconds <= 800) {
            moduleDiagnostics = moduleDiag;
            return false;
        }

        global::CodeGlyphX.QrDecodeDiagnostics moduleDiagInv;
        bm.Invert();
        try {
            if (budget.IsNearDeadline(120)) return false;
            if (global::CodeGlyphX.QrDecoder.TryDecodeInternal(bm, shouldStop, out result, out moduleDiagInv)) {
                moduleDiagnostics = moduleDiagInv;
                if (accept == null || accept(result)) return true;
                return false;
            }
        } finally {
            bm.Invert();
        }

        moduleDiagnostics = Better(moduleDiag, moduleDiagInv);
        return false;
    }

    private interface IModuleSampler {
        static abstract bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta);
    }

    private readonly struct Center3x3Sampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModuleCenter3x3WithDelta(image, sx, sy, invert, delta);
    }

    private readonly struct Center3x3LooseSampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModuleCenter3x3LooseWithDelta(image, sx, sy, invert, delta);
    }

    private readonly struct Mean25Sampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModule25MeanWithDelta(image, sx, sy, invert, delta);
    }

    private readonly struct Mean25LooseSampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModule25MeanLooseWithDelta(image, sx, sy, invert, delta);
    }

    private readonly struct Mean25ExtraLooseSampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModule25MeanExtraLooseWithDelta(image, sx, sy, invert, delta);
    }

    private readonly struct Nearest25Sampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModule25NearestWithDelta(image, sx, sy, invert, delta);
    }

    private readonly struct Nearest25LooseSampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModule25NearestLooseWithDelta(image, sx, sy, invert, delta);
    }

    private readonly struct Nearest25ExtraLooseSampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModule25NearestExtraLooseWithDelta(image, sx, sy, invert, delta);
    }

    private readonly struct Nearest9Sampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModule9NearestWithDelta(image, sx, sy, invert, delta);
    }

    private readonly struct Nearest9LooseSampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModule9NearestLooseWithDelta(image, sx, sy, invert, delta);
    }

    private readonly struct NinePxSampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModule9PxWithDelta(image, sx, sy, invert, delta);
    }

    private readonly struct NinePxLooseSampler : IModuleSampler {
        public static bool Sample(QrGrayImage image, double sx, double sy, bool invert, double delta) =>
            QrPixelSampling.SampleModule9PxLooseWithDelta(image, sx, sy, invert, delta);
    }

    private static bool SampleModules<TSampler>(
        QrGrayImage image,
        bool invert,
        in QrPerspectiveTransform transform,
        int dimension,
        double phaseX,
        double phaseY,
        global::CodeGlyphX.BitMatrix bm,
        DecodeBudget budget,
        int clampedLimit,
        double delta,
        out int clamped)
        where TSampler : struct, IModuleSampler {
        clamped = 0;

        var bmWords = bm.Words;
        var bmWidth = dimension;
        var width = image.Width;
        var height = image.Height;
        var maxX = width - 1;
        var maxY = height - 1;
        var checkBudget = budget.Enabled || budget.CanCancel;
        var budgetCounter = 0;
        var xStart = 0.5 + phaseX;

        for (var my = 0; my < dimension; my++) {
            if (checkBudget && budget.IsExpired) return false;
            var myc = my + 0.5 + phaseY;
            transform.GetRowParameters(
                xStart,
                myc,
                out var numX,
                out var numY,
                out var denom,
                out var stepNumX,
                out var stepNumY,
                out var stepDenom);

            if (!double.IsFinite(numX + numY + denom + stepNumX + stepNumY + stepDenom)) {
                return false;
            }

            var denomEnd = denom + stepDenom * (dimension - 1);
            if (!double.IsFinite(denomEnd)) return false;
            var absDenom = denom < 0 ? -denom : denom;
            var absDenomEnd = denomEnd < 0 ? -denomEnd : denomEnd;
            if (absDenom < 1e-12 || absDenomEnd < 1e-12) return false;
            if (denom * denomEnd < 0) return false;

            var rowOffset = my * bmWidth;
            var absStepDenom = stepDenom < 0 ? -stepDenom : stepDenom;
            if (absStepDenom < 1e-12) {
                var inv = 1.0 / denom;
                var sx = numX * inv;
                var sy = numY * inv;
                var sxStep = stepNumX * inv;
                var syStep = stepNumY * inv;
                var wordIndex = rowOffset >> 5;
                var bitMask = 1u << (rowOffset & 31);

                var sxEnd = sx + sxStep * (dimension - 1);
                var syEnd = sy + syStep * (dimension - 1);
                if (sx >= 0 && sx <= maxX && sxEnd >= 0 && sxEnd <= maxX &&
                    sy >= 0 && sy <= maxY && syEnd >= 0 && syEnd <= maxY) {
                    if (!checkBudget) {
                        for (var mx = 0; mx < dimension; mx++) {
                            if (TSampler.Sample(image, sx, sy, invert, delta)) {
                                bmWords[wordIndex] |= bitMask;
                            }

                            bitMask <<= 1;
                            if (bitMask == 0) {
                                bitMask = 1u;
                                wordIndex++;
                            }

                            sx += sxStep;
                            sy += syStep;
                        }
                    } else {
                        for (var mx = 0; mx < dimension; mx++) {
                            if (((budgetCounter++ & 63) == 0) && budget.IsExpired) return false;

                            if (TSampler.Sample(image, sx, sy, invert, delta)) {
                                bmWords[wordIndex] |= bitMask;
                            }

                            bitMask <<= 1;
                            if (bitMask == 0) {
                                bitMask = 1u;
                                wordIndex++;
                            }

                            sx += sxStep;
                            sy += syStep;
                        }
                    }
                } else {
                    if (!checkBudget) {
                        for (var mx = 0; mx < dimension; mx++) {
                            var sampleX = sx;
                            var sampleY = sy;
                            if (sampleX < 0) { sampleX = 0; clamped++; }
                            else if (sampleX > maxX) { sampleX = maxX; clamped++; }

                            if (sampleY < 0) { sampleY = 0; clamped++; }
                            else if (sampleY > maxY) { sampleY = maxY; clamped++; }

                            if (TSampler.Sample(image, sampleX, sampleY, invert, delta)) {
                                bmWords[wordIndex] |= bitMask;
                            }

                            bitMask <<= 1;
                            if (bitMask == 0) {
                                bitMask = 1u;
                                wordIndex++;
                            }

                            sx += sxStep;
                            sy += syStep;
                        }
                    } else {
                        for (var mx = 0; mx < dimension; mx++) {
                            if (((budgetCounter++ & 63) == 0) && budget.IsExpired) return false;

                            var sampleX = sx;
                            var sampleY = sy;
                            if (sampleX < 0) { sampleX = 0; clamped++; }
                            else if (sampleX > maxX) { sampleX = maxX; clamped++; }

                            if (sampleY < 0) { sampleY = 0; clamped++; }
                            else if (sampleY > maxY) { sampleY = maxY; clamped++; }

                            if (TSampler.Sample(image, sampleX, sampleY, invert, delta)) {
                                bmWords[wordIndex] |= bitMask;
                            }

                            bitMask <<= 1;
                            if (bitMask == 0) {
                                bitMask = 1u;
                                wordIndex++;
                            }

                            sx += sxStep;
                            sy += syStep;
                        }
                    }
                }
            } else {
                var wordIndex = rowOffset >> 5;
                var bitMask = 1u << (rowOffset & 31);
                if (!checkBudget) {
                    for (var mx = 0; mx < dimension; mx++) {
                        var inv = 1.0 / denom;
                        var sx = numX * inv;
                        var sy = numY * inv;

                        if (sx < 0) { sx = 0; clamped++; }
                        else if (sx > maxX) { sx = maxX; clamped++; }

                        if (sy < 0) { sy = 0; clamped++; }
                        else if (sy > maxY) { sy = maxY; clamped++; }

                        if (TSampler.Sample(image, sx, sy, invert, delta)) {
                            bmWords[wordIndex] |= bitMask;
                        }

                        bitMask <<= 1;
                        if (bitMask == 0) {
                            bitMask = 1u;
                            wordIndex++;
                        }

                        numX += stepNumX;
                        numY += stepNumY;
                        denom += stepDenom;
                    }
                } else {
                    for (var mx = 0; mx < dimension; mx++) {
                        if (((budgetCounter++ & 63) == 0) && budget.IsExpired) return false;

                        var inv = 1.0 / denom;
                        var sx = numX * inv;
                        var sy = numY * inv;

                        if (sx < 0) { sx = 0; clamped++; }
                        else if (sx > maxX) { sx = maxX; clamped++; }

                        if (sy < 0) { sy = 0; clamped++; }
                        else if (sy > maxY) { sy = maxY; clamped++; }

                        if (TSampler.Sample(image, sx, sy, invert, delta)) {
                            bmWords[wordIndex] |= bitMask;
                        }

                        bitMask <<= 1;
                        if (bitMask == 0) {
                            bitMask = 1u;
                            wordIndex++;
                        }

                        numX += stepNumX;
                        numY += stepNumY;
                        denom += stepDenom;
                    }
                }
            }

            if (clamped > clampedLimit) return false;
        }

        return clamped <= clampedLimit;
    }

    private static bool ShouldTryLooseSampling(global::CodeGlyphX.QrDecodeDiagnostics diag, double moduleSizePx) {
        if (moduleSizePx < 1.0) return false;
        return diag.Failure is global::CodeGlyphX.QrDecodeFailure.ReedSolomon or global::CodeGlyphX.QrDecodeFailure.Payload;
    }

    private static bool ShouldTryCenterSampling(global::CodeGlyphX.QrDecodeDiagnostics diag, double moduleSizePx) {
        if (moduleSizePx < 1.5) return false;
        return diag.Failure is global::CodeGlyphX.QrDecodeFailure.ReedSolomon or global::CodeGlyphX.QrDecodeFailure.Payload;
    }

    private static bool TryDecodeByBoundingBox(
        int scale,
        byte threshold,
        QrGrayImage image,
        bool invert,
        Func<QrDecoded, bool>? accept,
        bool aggressive,
        bool stylized,
        DecodeBudget budget,
        out QrDecoded result,
        out QrPixelDecodeDiagnostics diagnostics,
        int candidateCount = 0,
        int candidateTriplesTried = 0,
        int scanMinX = 0,
        int scanMinY = 0,
        int scanMaxX = -1,
        int scanMaxY = -1,
        bool allowAdaptiveFallback = true) {
        result = null!;
        diagnostics = default;

        var width = image.Width;
        var height = image.Height;
        if (scanMaxX < 0) scanMaxX = width - 1;
        if (scanMaxY < 0) scanMaxY = height - 1;
        if (scanMinX < 0) scanMinX = 0;
        if (scanMinY < 0) scanMinY = 0;
        if (scanMaxX >= width) scanMaxX = width - 1;
        if (scanMaxY >= height) scanMaxY = height - 1;
        if (scanMinX > scanMaxX || scanMinY > scanMaxY) return false;

        var scanWidth = scanMaxX - scanMinX + 1;
        var scanHeight = scanMaxY - scanMinY + 1;
        var canRetryAdaptive = allowAdaptiveFallback &&
            stylized &&
            image.ThresholdMap is null &&
            scanWidth >= 160 &&
            scanHeight >= 160 &&
            !budget.IsNearDeadline(200);

        var minX = scanMaxX;
        var minY = scanMaxY;
        var maxX = -1;
        var maxY = -1;

        var gray = image.Gray;
        var thresholdMap = image.ThresholdMap;
        var imageThreshold = image.Threshold;
        var checkBudget = budget.Enabled || budget.CanCancel;
        var budgetCounter = 0;
        if (checkBudget && budget.IsExpired) return false;

        if (thresholdMap is null) {
            if (!invert) {
                if (!checkBudget) {
                    for (var y = scanMinY; y <= scanMaxY; y++) {
                        var row = y * width;
                        for (int x = scanMinX, idx = row + scanMinX; x <= scanMaxX; x++, idx++) {
                            if (gray[idx] > imageThreshold) continue;
                            if (x < minX) minX = x;
                            if (y < minY) minY = y;
                            if (x > maxX) maxX = x;
                            if (y > maxY) maxY = y;
                        }
                    }
                } else {
                    for (var y = scanMinY; y <= scanMaxY; y++) {
                        if (budget.IsExpired) return false;
                        var row = y * width;
                        for (int x = scanMinX, idx = row + scanMinX; x <= scanMaxX; x++, idx++) {
                            if (((budgetCounter++ & 255) == 0) && budget.IsExpired) return false;
                            if (gray[idx] > imageThreshold) continue;
                            if (x < minX) minX = x;
                            if (y < minY) minY = y;
                            if (x > maxX) maxX = x;
                            if (y > maxY) maxY = y;
                        }
                    }
                }
            } else {
                if (!checkBudget) {
                    for (var y = scanMinY; y <= scanMaxY; y++) {
                        var row = y * width;
                        for (int x = scanMinX, idx = row + scanMinX; x <= scanMaxX; x++, idx++) {
                            if (gray[idx] <= imageThreshold) continue;
                            if (x < minX) minX = x;
                            if (y < minY) minY = y;
                            if (x > maxX) maxX = x;
                            if (y > maxY) maxY = y;
                        }
                    }
                } else {
                    for (var y = scanMinY; y <= scanMaxY; y++) {
                        if (budget.IsExpired) return false;
                        var row = y * width;
                        for (int x = scanMinX, idx = row + scanMinX; x <= scanMaxX; x++, idx++) {
                            if (((budgetCounter++ & 255) == 0) && budget.IsExpired) return false;
                            if (gray[idx] <= imageThreshold) continue;
                            if (x < minX) minX = x;
                            if (y < minY) minY = y;
                            if (x > maxX) maxX = x;
                            if (y > maxY) maxY = y;
                        }
                    }
                }
            }
        } else {
            if (!invert) {
                if (!checkBudget) {
                    for (var y = scanMinY; y <= scanMaxY; y++) {
                        var row = y * width;
                        for (int x = scanMinX, idx = row + scanMinX; x <= scanMaxX; x++, idx++) {
                            if (gray[idx] > thresholdMap[idx]) continue;
                            if (x < minX) minX = x;
                            if (y < minY) minY = y;
                            if (x > maxX) maxX = x;
                            if (y > maxY) maxY = y;
                        }
                    }
                } else {
                    for (var y = scanMinY; y <= scanMaxY; y++) {
                        if (budget.IsExpired) return false;
                        var row = y * width;
                        for (int x = scanMinX, idx = row + scanMinX; x <= scanMaxX; x++, idx++) {
                            if (((budgetCounter++ & 255) == 0) && budget.IsExpired) return false;
                            if (gray[idx] > thresholdMap[idx]) continue;
                            if (x < minX) minX = x;
                            if (y < minY) minY = y;
                            if (x > maxX) maxX = x;
                            if (y > maxY) maxY = y;
                        }
                    }
                }
            } else {
                if (!checkBudget) {
                    for (var y = scanMinY; y <= scanMaxY; y++) {
                        var row = y * width;
                        for (int x = scanMinX, idx = row + scanMinX; x <= scanMaxX; x++, idx++) {
                            if (gray[idx] <= thresholdMap[idx]) continue;
                            if (x < minX) minX = x;
                            if (y < minY) minY = y;
                            if (x > maxX) maxX = x;
                            if (y > maxY) maxY = y;
                        }
                    }
                } else {
                    for (var y = scanMinY; y <= scanMaxY; y++) {
                        if (budget.IsExpired) return false;
                        var row = y * width;
                        for (int x = scanMinX, idx = row + scanMinX; x <= scanMaxX; x++, idx++) {
                            if (((budgetCounter++ & 255) == 0) && budget.IsExpired) return false;
                            if (gray[idx] <= thresholdMap[idx]) continue;
                            if (x < minX) minX = x;
                            if (y < minY) minY = y;
                            if (x > maxX) maxX = x;
                            if (y > maxY) maxY = y;
                        }
                    }
                }
            }
        }

        if (maxX < 0) return false;

        TrimBoundingBox(image, invert, ref minX, ref minY, ref maxX, ref maxY);
        if (maxX < minX || maxY < minY) return false;

        // Expand a touch to counter anti-aliasing that can shrink the detected black bbox.
        if (minX > 0) minX--;
        if (minY > 0) minY--;
        if (maxX < width - 1) maxX++;
        if (maxY < height - 1) maxY++;

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
        Span<double> phases = stackalloc double[3];
        Span<double> boxScales = stackalloc double[3];
        for (var version = 1; version <= maxVersion; version++) {
            if (checkBudget && budget.IsExpired) return false;
            var modulesCount = version * 4 + 17;
            var moduleSizeX = boxW / (double)modulesCount;
            var moduleSizeY = boxH / (double)modulesCount;
            if (moduleSizeX < 1.0 || moduleSizeY < 1.0) continue;

            var relDiff = Math.Abs(moduleSizeX - moduleSizeY) / Math.Max(moduleSizeX, moduleSizeY);
            if (relDiff > 0.20) continue;
            var scaleCount = 1;
            boxScales[0] = 1.0;
            if (stylized &&
                aggressive &&
                moduleSizeX >= 2.0 &&
                moduleSizeY >= 2.0 &&
                !(checkBudget && budget.IsNearDeadline(180))) {
                boxScales[scaleCount++] = 0.97;
                boxScales[scaleCount++] = 1.03;
            }

            for (var s = 0; s < scaleCount; s++) {
                if (checkBudget && budget.IsExpired) return false;
                var boxScale = boxScales[s];
                var scaledModuleSizeX = moduleSizeX * boxScale;
                var scaledModuleSizeY = moduleSizeY * boxScale;
                if (scaledModuleSizeX < 1.0 || scaledModuleSizeY < 1.0) continue;

                var gridW = scaledModuleSizeX * (modulesCount - 1);
                var gridH = scaledModuleSizeY * (modulesCount - 1);
                var padX = (boxW - scaledModuleSizeX * modulesCount) * 0.5;
                var padY = (boxH - scaledModuleSizeY * modulesCount) * 0.5;
                var baseX = minX + padX;
                var baseY = minY + padY;
                if (baseX < 0 || baseY < 0) continue;
                if (baseX + gridW > width - 1 || baseY + gridH > height - 1) continue;

                var moduleSize = (scaledModuleSizeX + scaledModuleSizeY) * 0.5;
                var useRing = stylized && moduleSize >= 1.25 && !(checkBudget && budget.IsNearDeadline(200));
                var ringDelta = useRing ? QrPixelSampling.GetSampleDeltaRingForModule(moduleSize) : 0.0;

                phases[0] = 0.5;
                var phaseCount = 1;
                if (aggressive && moduleSize >= 2.0 && !(checkBudget && budget.IsNearDeadline(150))) {
                    phases[phaseCount++] = 0.42;
                    phases[phaseCount++] = 0.58;
                }

                for (var p = 0; p < phaseCount; p++) {
                    if (checkBudget && budget.IsExpired) return false;
                    var phase = phases[p];
                    var bm = new global::CodeGlyphX.BitMatrix(modulesCount, modulesCount);
                    var bmWords = bm.Words;
                    var bmWidth = modulesCount;

                    for (var my = 0; my < modulesCount; my++) {
                        if (checkBudget && budget.IsExpired) return false;
                        var sy = baseY + (my + phase) * scaledModuleSizeY;

                        var rowOffset = my * bmWidth;
                        var wordIndex = rowOffset >> 5;
                        var bitMask = 1u << (rowOffset & 31);
                        for (var mx = 0; mx < modulesCount; mx++) {
                            var sx = baseX + (mx + phase) * scaledModuleSizeX;
                            var isBlack = moduleSize >= 2.0
                                ? (useRing
                                    ? QrPixelSampling.SampleModule9PxWithDelta(image, sx, sy, invert, ringDelta)
                                    : QrPixelSampling.SampleModule9Px(image, sx, sy, invert, moduleSize))
                                : (useRing
                                    ? QrPixelSampling.SampleModule9PxWithDelta(image, sx, sy, invert, ringDelta)
                                    : QrPixelSampling.SampleModuleMajority3x3(image, sx, sy, invert));
                            if (isBlack) {
                                bmWords[wordIndex] |= bitMask;
                            }

                            bitMask <<= 1;
                            if (bitMask == 0) {
                                bitMask = 1u;
                                wordIndex++;
                            }
                        }
                    }

                    if (checkBudget && budget.IsNearDeadline(120)) return false;
                    if (TryDecodeWithInversion(bm, accept, budget, out result, out var moduleDiag)) {
                        diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, modulesCount, moduleDiag);
                        return true;
                    }
                    best = Better(best, new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, modulesCount, moduleDiag));

                    if (checkBudget && budget.IsNearDeadline(120)) return false;
                    if (TryDecodeByRotations(bm, accept, budget, out result, out var moduleDiagRot)) {
                        diagnostics = new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, modulesCount, moduleDiagRot);
                        return true;
                    }
                    best = Better(best, new QrPixelDecodeDiagnostics(scale, threshold, invert, candidateCount, candidateTriplesTried, modulesCount, moduleDiagRot));

                    MaybeDumpModuleMatrix(bm, modulesCount, moduleDiag, suffix: "bbox");
                }
            }
        }

        if (canRetryAdaptive && !budget.IsExpired) {
            var window = ResolveAdaptiveWindowSize(Math.Min(scanWidth, scanHeight));
            var adaptive = image.WithAdaptiveThreshold(window, offset: 4);
            if (TryDecodeByBoundingBox(
                    scale,
                    threshold,
                    adaptive,
                    invert,
                    accept,
                    aggressive,
                    stylized,
                    budget,
                    out result,
                    out var adaptiveDiag,
                    candidateCount,
                    candidateTriplesTried,
                    scanMinX,
                    scanMinY,
                    scanMaxX,
                    scanMaxY,
                    allowAdaptiveFallback: false)) {
                diagnostics = adaptiveDiag;
                return true;
            }
            best = Better(best, adaptiveDiag);
        }

        diagnostics = best;
        return false;
    }

    private static int ResolveAdaptiveWindowSize(int minDim) {
        if (minDim >= 900) return 41;
        if (minDim >= 600) return 31;
        if (minDim >= 360) return 25;
        if (minDim >= 240) return 21;
        return 15;
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
        var width = maxX - minX + 1;
        if (width <= 0) return 0;

        var gray = image.Gray;
        var start = y * image.Width + minX;
        var end = start + width;
        var thresholdMap = image.ThresholdMap;
        var count = 0;

        if (thresholdMap is null) {
            var threshold = image.Threshold;
            if (Sse2.IsSupported && width >= 16) {
                count = 0;
                var i = start;
                var offset = Vector128.Create((byte)0x80);
                var thresholdVec = Vector128.Create((byte)(threshold ^ 0x80));

                while (i + 16 <= end) {
                    var vec = MemoryMarshal.Read<Vector128<byte>>(gray.AsSpan(i));
                    var signed = Sse2.Xor(vec, offset).AsSByte();
                    var gt = Sse2.CompareGreaterThan(signed, thresholdVec.AsSByte());
                    var mask = (uint)Sse2.MoveMask(gt.AsByte());
                    count += CodeGlyphX.Internal.BitOps.PopCount(mask);
                    i += 16;
                }

                for (; i < end; i++) {
                    if (gray[i] > threshold) count++;
                }

                return invert ? count : width - count;
            }

            count = 0;
            if (!invert) {
                for (var i = start; i < end; i++) {
                    if (gray[i] <= threshold) count++;
                }
            } else {
                for (var i = start; i < end; i++) {
                    if (gray[i] > threshold) count++;
                }
            }
            return count;
        }

        count = 0;
        if (!invert) {
            for (var i = start; i < end; i++) {
                if (gray[i] <= thresholdMap[i]) count++;
            }
        } else {
            for (var i = start; i < end; i++) {
                if (gray[i] > thresholdMap[i]) count++;
            }
        }
        return count;
    }

    private static int CountDarkCol(QrGrayImage image, bool invert, int x, int minY, int maxY) {
        var width = image.Width;
        var gray = image.Gray;
        var thresholdMap = image.ThresholdMap;
        var threshold = image.Threshold;

        var count = 0;
        if (thresholdMap is null) {
            if (!invert) {
                for (var y = minY; y <= maxY; y++) {
                    var idx = y * width + x;
                    if (gray[idx] <= threshold) count++;
                }
            } else {
                for (var y = minY; y <= maxY; y++) {
                    var idx = y * width + x;
                    if (gray[idx] > threshold) count++;
                }
            }
            return count;
        }

        if (!invert) {
            for (var y = minY; y <= maxY; y++) {
                var idx = y * width + x;
                if (gray[idx] <= thresholdMap[idx]) count++;
            }
        } else {
            for (var y = minY; y <= maxY; y++) {
                var idx = y * width + x;
                if (gray[idx] > thresholdMap[idx]) count++;
            }
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double Distance(double x1, double y1, double x2, double y2) {
        var dx = x1 - x2;
        var dy = y1 - y2;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool SampleMajority3x3(QrGrayImage image, int px, int py, bool invert) {
        var width = image.Width;
        var height = image.Height;
        var gray = image.Gray;
        var thresholdMap = image.ThresholdMap;
        var threshold = image.Threshold;

        var x0 = px - 1;
        if (x0 < 0) x0 = 0;
        else if (x0 >= width) x0 = width - 1;
        var x1 = px;
        if (x1 < 0) x1 = 0;
        else if (x1 >= width) x1 = width - 1;
        var x2 = px + 1;
        if (x2 < 0) x2 = 0;
        else if (x2 >= width) x2 = width - 1;

        var y0 = py - 1;
        if (y0 < 0) y0 = 0;
        else if (y0 >= height) y0 = height - 1;
        var y1 = py;
        if (y1 < 0) y1 = 0;
        else if (y1 >= height) y1 = height - 1;
        var y2 = py + 1;
        if (y2 < 0) y2 = 0;
        else if (y2 >= height) y2 = height - 1;

        var row0 = y0 * width;
        var row1 = y1 * width;
        var row2 = y2 * width;
        var black = 0;
        var remaining = 9;
        const int required = 5;
        if (thresholdMap is null) {
            if (!invert) {
                if (gray[row0 + x0] <= threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                if (gray[row0 + x1] <= threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                if (gray[row0 + x2] <= threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                if (gray[row1 + x0] <= threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                if (gray[row1 + x1] <= threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                if (gray[row1 + x2] <= threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                if (gray[row2 + x0] <= threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                if (gray[row2 + x1] <= threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                if (gray[row2 + x2] <= threshold) black++;
            } else {
                if (gray[row0 + x0] > threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                if (gray[row0 + x1] > threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                if (gray[row0 + x2] > threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                if (gray[row1 + x0] > threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                if (gray[row1 + x1] > threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                if (gray[row1 + x2] > threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                if (gray[row2 + x0] > threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                if (gray[row2 + x1] > threshold) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                if (gray[row2 + x2] > threshold) black++;
            }
        } else {
            if (!invert) {
                var idx = row0 + x0;
                if (gray[idx] <= thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                idx = row0 + x1;
                if (gray[idx] <= thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                idx = row0 + x2;
                if (gray[idx] <= thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                idx = row1 + x0;
                if (gray[idx] <= thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                idx = row1 + x1;
                if (gray[idx] <= thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                idx = row1 + x2;
                if (gray[idx] <= thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                idx = row2 + x0;
                if (gray[idx] <= thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                idx = row2 + x1;
                if (gray[idx] <= thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                idx = row2 + x2;
                if (gray[idx] <= thresholdMap[idx]) black++;
            } else {
                var idx = row0 + x0;
                if (gray[idx] > thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                idx = row0 + x1;
                if (gray[idx] > thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                idx = row0 + x2;
                if (gray[idx] > thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                idx = row1 + x0;
                if (gray[idx] > thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                idx = row1 + x1;
                if (gray[idx] > thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                idx = row1 + x2;
                if (gray[idx] > thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                idx = row2 + x0;
                if (gray[idx] > thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                idx = row2 + x1;
                if (gray[idx] > thresholdMap[idx]) black++;
                remaining--;
                if (black >= required) return true;
                if (black + remaining < required) return false;
                idx = row2 + x2;
                if (gray[idx] > thresholdMap[idx]) black++;
            }
        }

        return black >= required;
    }

    private static bool TryDecodeWithInversion(global::CodeGlyphX.BitMatrix matrix, Func<QrDecoded, bool>? accept, DecodeBudget budget, out QrDecoded result, out global::CodeGlyphX.QrDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        Func<bool>? shouldStop = budget.Enabled || budget.CanCancel ? () => budget.IsExpired : null;
        var ok = global::CodeGlyphX.QrDecoder.TryDecodeInternal(matrix, shouldStop, out result, out global::CodeGlyphX.QrDecodeDiagnostics moduleDiag);
        var best = moduleDiag;
        if (ok && (accept == null || accept(result))) {
            diagnostics = moduleDiag;
            return true;
        }

        if (budget.Enabled && budget.MaxMilliseconds <= 800) {
            diagnostics = best;
            return false;
        }

        global::CodeGlyphX.QrDecodeDiagnostics moduleDiagInv;
        matrix.Invert();
        try {
            ok = global::CodeGlyphX.QrDecoder.TryDecodeInternal(matrix, shouldStop, out result, out moduleDiagInv);
        } finally {
            matrix.Invert();
        }
        best = Better(best, moduleDiagInv);

        if (ok && (accept == null || accept(result))) {
            diagnostics = moduleDiagInv;
            return true;
        }

        diagnostics = best;
        return false;
    }

    private static bool TryDecodeWithCleanup(
        global::CodeGlyphX.BitMatrix matrix,
        int version,
        Func<QrDecoded, bool>? accept,
        DecodeBudget budget,
        Func<bool>? shouldStop,
        out QrDecoded result,
        out global::CodeGlyphX.QrDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (version <= 0) return false;
        if (budget.IsNearDeadline(140)) return false;

        var size = matrix.Width;
        if (size <= 0 || size != matrix.Height) return false;

        var functionMask = QrStructureAnalysis.GetFunctionMask(version);
        var blackRatio = ComputeBlackRatio(matrix);

        if (blackRatio < 0.28 && !budget.IsNearDeadline(140)) {
            var filteredLight = new global::CodeGlyphX.BitMatrix(size, size);
            ApplyMajorityFilter(matrix, filteredLight, functionMask, minBlack: 3);
            MaybeDumpModuleMatrix(filteredLight, size, new global::CodeGlyphX.QrDecodeDiagnostics(global::CodeGlyphX.QrDecodeFailure.Payload, version), suffix: "filtered3");

            if (budget.IsNearDeadline(120)) return false;
            if (global::CodeGlyphX.QrDecoder.TryDecodeInternal(filteredLight, shouldStop, out result, out diagnostics)) {
                if (accept == null || accept(result)) return true;
            }
            if (budget.IsNearDeadline(120)) return false;
            if (global::CodeGlyphX.QrDecoder.TryDecodeAllFormatCandidates(filteredLight, shouldStop, out result, out diagnostics)) {
                if (accept == null || accept(result)) return true;
            }
        }
        var filtered = new global::CodeGlyphX.BitMatrix(size, size);
        ApplyMajorityFilter(matrix, filtered, functionMask, minBlack: 4);
        MaybeDumpModuleMatrix(filtered, size, new global::CodeGlyphX.QrDecodeDiagnostics(global::CodeGlyphX.QrDecodeFailure.Payload, version), suffix: "filtered4");

        if (budget.IsNearDeadline(120)) return false;
        if (global::CodeGlyphX.QrDecoder.TryDecodeInternal(filtered, shouldStop, out result, out diagnostics)) {
            if (accept == null || accept(result)) return true;
        }
        if (budget.IsNearDeadline(120)) return false;
        if (global::CodeGlyphX.QrDecoder.TryDecodeAllFormatCandidates(filtered, shouldStop, out result, out diagnostics)) {
            if (accept == null || accept(result)) return true;
        }

        if (budget.IsNearDeadline(120)) return false;
        var filteredStrong = new global::CodeGlyphX.BitMatrix(size, size);
        ApplyMajorityFilter(matrix, filteredStrong, functionMask, minBlack: 5);
        MaybeDumpModuleMatrix(filteredStrong, size, new global::CodeGlyphX.QrDecodeDiagnostics(global::CodeGlyphX.QrDecodeFailure.Payload, version), suffix: "filtered5");
        if (global::CodeGlyphX.QrDecoder.TryDecodeInternal(filteredStrong, shouldStop, out result, out diagnostics)) {
            if (accept == null || accept(result)) return true;
        }
        if (budget.IsNearDeadline(120)) return false;
        if (global::CodeGlyphX.QrDecoder.TryDecodeAllFormatCandidates(filteredStrong, shouldStop, out result, out diagnostics)) {
            if (accept == null || accept(result)) return true;
        }

        if (blackRatio > 0.62 && !budget.IsNearDeadline(140)) {
            var filteredHeavy = new global::CodeGlyphX.BitMatrix(size, size);
            ApplyMajorityFilter(matrix, filteredHeavy, functionMask, minBlack: 6);
            MaybeDumpModuleMatrix(filteredHeavy, size, new global::CodeGlyphX.QrDecodeDiagnostics(global::CodeGlyphX.QrDecodeFailure.Payload, version), suffix: "filtered6");

            if (budget.IsNearDeadline(120)) return false;
            if (global::CodeGlyphX.QrDecoder.TryDecodeInternal(filteredHeavy, shouldStop, out result, out diagnostics)) {
                if (accept == null || accept(result)) return true;
            }
            if (budget.IsNearDeadline(120)) return false;
            if (global::CodeGlyphX.QrDecoder.TryDecodeAllFormatCandidates(filteredHeavy, shouldStop, out result, out diagnostics)) {
                if (accept == null || accept(result)) return true;
            }
        }

        if (!budget.IsNearDeadline(160)) {
            var corrected = new global::CodeGlyphX.BitMatrix(size, size);
            corrected.CopyFrom(filteredStrong);
            ApplyFunctionPatterns(corrected, version);
            MaybeDumpModuleMatrix(corrected, size, new global::CodeGlyphX.QrDecodeDiagnostics(global::CodeGlyphX.QrDecodeFailure.Payload, version), suffix: "patterns");

            if (budget.IsNearDeadline(120)) return false;
            if (global::CodeGlyphX.QrDecoder.TryDecodeInternal(corrected, shouldStop, out result, out diagnostics)) {
                if (accept == null || accept(result)) return true;
            }
            if (!budget.IsNearDeadline(120) &&
                global::CodeGlyphX.QrDecoder.TryDecodeAllFormatCandidates(corrected, shouldStop, out result, out diagnostics)) {
                if (accept == null || accept(result)) return true;
            }
        }

        return false;
    }

    private static void ApplyFunctionPatterns(global::CodeGlyphX.BitMatrix matrix, int version) {
        var size = matrix.Width;
        if (size <= 0 || size != matrix.Height) return;
        if (version <= 0 || version > 40) return;

        ApplyFinder(matrix, 0, 0);
        ApplyFinder(matrix, size - 7, 0);
        ApplyFinder(matrix, 0, size - 7);
        ApplySeparator(matrix, 0, 0);
        ApplySeparator(matrix, size - 7, 0);
        ApplySeparator(matrix, 0, size - 7);

        if (size > 16) {
            for (var i = 8; i <= size - 9; i++) {
                var v = (i & 1) == 0;
                matrix[i, 6] = v;
                matrix[6, i] = v;
            }
        }

        if ((uint)size > 8u) {
            matrix[8, size - 8] = true;
        }

        if (version >= 2) {
            var align = QrTables.GetAlignmentPatternPositions(version);
            if (align.Length > 0) {
                for (var i = 0; i < align.Length; i++) {
                    for (var j = 0; j < align.Length; j++) {
                        var ax = align[i];
                        var ay = align[j];
                        if ((ax <= 7 && ay <= 7) ||
                            (ax >= size - 8 && ay <= 7) ||
                            (ax <= 7 && ay >= size - 8)) {
                            continue;
                        }
                        ApplyAlignment(matrix, ax, ay);
                    }
                }
            }
        }
    }

    private static void ApplyFinder(global::CodeGlyphX.BitMatrix matrix, int startX, int startY) {
        for (var y = 0; y < 7; y++) {
            for (var x = 0; x < 7; x++) {
                var xx = startX + x;
                var yy = startY + y;
                if ((uint)xx >= (uint)matrix.Width || (uint)yy >= (uint)matrix.Height) continue;

                var border = x == 0 || x == 6 || y == 0 || y == 6;
                var center = x >= 2 && x <= 4 && y >= 2 && y <= 4;
                matrix[xx, yy] = border || center;
            }
        }
    }

    private static void ApplySeparator(global::CodeGlyphX.BitMatrix matrix, int startX, int startY) {
        for (var i = -1; i <= 7; i++) {
            var x = startX + i;
            var yTop = startY - 1;
            var yBottom = startY + 7;
            if ((uint)x < (uint)matrix.Width) {
                if ((uint)yTop < (uint)matrix.Height) matrix[x, yTop] = false;
                if ((uint)yBottom < (uint)matrix.Height) matrix[x, yBottom] = false;
            }

            var y = startY + i;
            var xLeft = startX - 1;
            var xRight = startX + 7;
            if ((uint)y < (uint)matrix.Height) {
                if ((uint)xLeft < (uint)matrix.Width) matrix[xLeft, y] = false;
                if ((uint)xRight < (uint)matrix.Width) matrix[xRight, y] = false;
            }
        }
    }

    private static void ApplyAlignment(global::CodeGlyphX.BitMatrix matrix, int centerX, int centerY) {
        for (var dy = -2; dy <= 2; dy++) {
            for (var dx = -2; dx <= 2; dx++) {
                var x = centerX + dx;
                var y = centerY + dy;
                if ((uint)x >= (uint)matrix.Width || (uint)y >= (uint)matrix.Height) continue;

                var border = dx == -2 || dx == 2 || dy == -2 || dy == 2;
                var center = dx == 0 && dy == 0;
                matrix[x, y] = border || center;
            }
        }
    }

    private static bool TryDecodeCombinedMatrices(
        global::CodeGlyphX.BitMatrix primary,
        global::CodeGlyphX.BitMatrix alternate,
        global::CodeGlyphX.BitMatrix combined,
        bool useOr,
        Func<QrDecoded, bool>? accept,
        DecodeBudget budget,
        Func<bool>? shouldStop,
        out QrDecoded result,
        out global::CodeGlyphX.QrDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        var aWords = primary.Words;
        var bWords = alternate.Words;
        var cWords = combined.Words;
        for (var i = 0; i < cWords.Length; i++) {
            cWords[i] = useOr ? (aWords[i] | bWords[i]) : (aWords[i] & bWords[i]);
        }

        if (budget.IsNearDeadline(140)) return false;
        if (global::CodeGlyphX.QrDecoder.TryDecodeInternal(combined, shouldStop, out result, out diagnostics)) {
            if (accept == null || accept(result)) return true;
        }

        if (!budget.IsNearDeadline(140) &&
            global::CodeGlyphX.QrDecoder.TryDecodeAllFormatCandidates(combined, shouldStop, out result, out diagnostics)) {
            if (accept == null || accept(result)) return true;
        }

        return false;
    }

    private static bool TryDecodeMajorityMatrices(
        global::CodeGlyphX.BitMatrix primary,
        global::CodeGlyphX.BitMatrix alternate,
        global::CodeGlyphX.BitMatrix third,
        global::CodeGlyphX.BitMatrix combined,
        Func<QrDecoded, bool>? accept,
        DecodeBudget budget,
        Func<bool>? shouldStop,
        out QrDecoded result,
        out global::CodeGlyphX.QrDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        var aWords = primary.Words;
        var bWords = alternate.Words;
        var cWords = third.Words;
        var dWords = combined.Words;
        for (var i = 0; i < dWords.Length; i++) {
            dWords[i] = (aWords[i] & bWords[i]) | (aWords[i] & cWords[i]) | (bWords[i] & cWords[i]);
        }

        if (budget.IsNearDeadline(140)) return false;
        if (global::CodeGlyphX.QrDecoder.TryDecodeInternal(combined, shouldStop, out result, out diagnostics)) {
            if (accept == null || accept(result)) return true;
        }

        if (!budget.IsNearDeadline(140) &&
            global::CodeGlyphX.QrDecoder.TryDecodeAllFormatCandidates(combined, shouldStop, out result, out diagnostics)) {
            if (accept == null || accept(result)) return true;
        }

        return false;
    }

    private readonly struct FlipCandidate {
        public FlipCandidate(int x, int y, bool value, int score) {
            X = x;
            Y = y;
            Value = value;
            Score = score;
        }

        public int X { get; }
        public int Y { get; }
        public bool Value { get; }
        public int Score { get; }
    }

    private static bool TryDecodeWithCandidateFlips(
        global::CodeGlyphX.BitMatrix primary,
        List<FlipCandidate> candidates,
        Func<QrDecoded, bool>? accept,
        DecodeBudget budget,
        Func<bool>? shouldStop,
        out QrDecoded result,
        out global::CodeGlyphX.QrDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (candidates.Count == 0) return false;
        candidates.Sort(static (a, b) => b.Score.CompareTo(a.Score));

        var scratch = new global::CodeGlyphX.BitMatrix(primary.Width, primary.Height);
        for (var i = 0; i < candidates.Count; i++) {
            if (budget.IsNearDeadline(160) || shouldStop?.Invoke() == true) return false;

            scratch.CopyFrom(primary);
            var c = candidates[i];
            scratch[c.X, c.Y] = c.Value;
            if (global::CodeGlyphX.QrDecoder.TryDecodeAllFormatCandidates(scratch, shouldStop, out result, out diagnostics)) {
                if (diagnostics.FormatBestDistance > 4) continue;
                if (accept == null || accept(result)) return true;
            }
        }

        for (var i = 0; i < candidates.Count; i++) {
            for (var j = i + 1; j < candidates.Count; j++) {
                if (budget.IsNearDeadline(160) || shouldStop?.Invoke() == true) return false;

                scratch.CopyFrom(primary);
                var c0 = candidates[i];
                var c1 = candidates[j];
                scratch[c0.X, c0.Y] = c0.Value;
                scratch[c1.X, c1.Y] = c1.Value;
                if (global::CodeGlyphX.QrDecoder.TryDecodeAllFormatCandidates(scratch, shouldStop, out result, out diagnostics)) {
                    if (diagnostics.FormatBestDistance > 4) continue;
                    if (accept == null || accept(result)) return true;
                }
            }
        }

        diagnostics = new global::CodeGlyphX.QrDecodeDiagnostics(global::CodeGlyphX.QrDecodeFailure.Payload);
        return false;
    }

    private static bool TryDecodeWithFlipSearch(
        global::CodeGlyphX.BitMatrix primary,
        global::CodeGlyphX.BitMatrix alternate,
        global::CodeGlyphX.BitMatrix? third,
        Func<QrDecoded, bool>? accept,
        DecodeBudget budget,
        Func<bool>? shouldStop,
        out QrDecoded result,
        out global::CodeGlyphX.QrDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        var size = primary.Width;
        if (size <= 0 || size != primary.Height) return false;
        if (size != alternate.Width || size != alternate.Height) return false;
        if (third != null && (size != third.Width || size != third.Height)) return false;

        var maxCandidates = third != null ? 12 : 8;
        var candidates = new List<FlipCandidate>(maxCandidates);
        for (var y = 0; y < size; y++) {
            for (var x = 0; x < size; x++) {
                var primaryValue = primary[x, y];
                var altValue = alternate[x, y];
                var score = primaryValue == altValue ? 0 : 1;
                var majorityValue = altValue;

                if (third != null) {
                    var thirdValue = third[x, y];
                    if (primaryValue != thirdValue) score++;
                    var blackCount = 0;
                    if (primaryValue) blackCount++;
                    if (altValue) blackCount++;
                    if (thirdValue) blackCount++;
                    majorityValue = blackCount >= 2;
                }

                if (score == 0 || primaryValue == majorityValue) continue;

                candidates.Add(new FlipCandidate(x, y, majorityValue, score));
            }
        }

        if (candidates.Count == 0) return false;
        if (candidates.Count > maxCandidates) {
            candidates.RemoveRange(maxCandidates, candidates.Count - maxCandidates);
        }

        return TryDecodeWithCandidateFlips(primary, candidates, accept, budget, shouldStop, out result, out diagnostics);
    }

    private static bool TryDecodeWithConfidenceFlips(
        QrGrayImage image,
        bool invert,
        in QrPerspectiveTransform transform,
        int dimension,
        double phaseX,
        double phaseY,
        double moduleSizePx,
        global::CodeGlyphX.BitMatrix primary,
        Func<QrDecoded, bool>? accept,
        DecodeBudget budget,
        Func<bool>? shouldStop,
        out QrDecoded result,
        out global::CodeGlyphX.QrDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        var version = (dimension - 17) / 4;
        if (version is < 1 or > 40 || dimension != version * 4 + 17) return false;
        if (budget.IsNearDeadline(200)) return false;

        var confidenceImage = image;
        if (image.ThresholdMap is null) {
            var window = (int)Math.Round(moduleSizePx * 4.0);
            if (window < 15) window = 15;
            if (window > 41) window = 41;
            if ((window & 1) == 0) window++;
            confidenceImage = image.WithAdaptiveThreshold(window, offset: 4);
        }

        var functionMask = QrStructureAnalysis.GetFunctionMask(version);
        var maxCandidates = dimension <= 25 ? 12 : dimension <= 45 ? 20 : 16;
        var candidates = new List<FlipCandidate>(maxCandidates);
        if (!CollectLowConfidenceCandidates(confidenceImage, transform, dimension, phaseX, phaseY, primary, functionMask, maxCandidates, budget, candidates)) {
            return false;
        }

        if (candidates.Count == 0) return false;
        return TryDecodeWithCandidateFlips(primary, candidates, accept, budget, shouldStop, out result, out diagnostics);
    }

    private static bool CollectLowConfidenceCandidates(
        QrGrayImage image,
        in QrPerspectiveTransform transform,
        int dimension,
        double phaseX,
        double phaseY,
        global::CodeGlyphX.BitMatrix primary,
        global::CodeGlyphX.BitMatrix functionMask,
        int maxCandidates,
        DecodeBudget budget,
        List<FlipCandidate> candidates) {
        var width = image.Width;
        var height = image.Height;
        var maxX = width - 1;
        var maxY = height - 1;
        var checkBudget = budget.Enabled || budget.CanCancel;
        var budgetCounter = 0;
        var xStart = 0.5 + phaseX;

        for (var my = 0; my < dimension; my++) {
            if (checkBudget && budget.IsExpired) return false;
            var myc = my + 0.5 + phaseY;
            transform.GetRowParameters(
                xStart,
                myc,
                out var numX,
                out var numY,
                out var denom,
                out var stepNumX,
                out var stepNumY,
                out var stepDenom);

            if (!double.IsFinite(numX + numY + denom + stepNumX + stepNumY + stepDenom)) return false;

            var denomEnd = denom + stepDenom * (dimension - 1);
            if (!double.IsFinite(denomEnd)) return false;
            var absDenom = denom < 0 ? -denom : denom;
            var absDenomEnd = denomEnd < 0 ? -denomEnd : denomEnd;
            if (absDenom < 1e-12 || absDenomEnd < 1e-12) return false;
            if (denom * denomEnd < 0) return false;

            var absStepDenom = stepDenom < 0 ? -stepDenom : stepDenom;
            if (absStepDenom < 1e-12) {
                var inv = 1.0 / denom;
                var sx = numX * inv;
                var sy = numY * inv;
                var sxStep = stepNumX * inv;
                var syStep = stepNumY * inv;

                for (var mx = 0; mx < dimension; mx++) {
                    if (checkBudget && ((budgetCounter++ & 127) == 0) && budget.IsExpired) return false;
                    if (functionMask[mx, my]) {
                        sx += sxStep;
                        sy += syStep;
                        continue;
                    }

                    var sampleX = sx;
                    var sampleY = sy;
                    if (sampleX < 0) sampleX = 0;
                    else if (sampleX > maxX) sampleX = maxX;

                    if (sampleY < 0) sampleY = 0;
                    else if (sampleY > maxY) sampleY = maxY;

                    QrPixelSampling.SampleLumaWithThreshold(image, sampleX, sampleY, out var lum, out var threshold);
                    var margin = lum > threshold ? lum - threshold : threshold - lum;
                    var score = 255 - margin;
                    var flipValue = !primary[mx, my];
                    AddCandidate(candidates, maxCandidates, mx, my, flipValue, score);

                    sx += sxStep;
                    sy += syStep;
                }
            } else {
                for (var mx = 0; mx < dimension; mx++) {
                    if (checkBudget && ((budgetCounter++ & 127) == 0) && budget.IsExpired) return false;
                    if (functionMask[mx, my]) {
                        numX += stepNumX;
                        numY += stepNumY;
                        denom += stepDenom;
                        continue;
                    }

                    var inv = 1.0 / denom;
                    var sx = numX * inv;
                    var sy = numY * inv;

                    if (sx < 0) sx = 0;
                    else if (sx > maxX) sx = maxX;

                    if (sy < 0) sy = 0;
                    else if (sy > maxY) sy = maxY;

                    QrPixelSampling.SampleLumaWithThreshold(image, sx, sy, out var lum, out var threshold);
                    var margin = lum > threshold ? lum - threshold : threshold - lum;
                    var score = 255 - margin;
                    var flipValue = !primary[mx, my];
                    AddCandidate(candidates, maxCandidates, mx, my, flipValue, score);

                    numX += stepNumX;
                    numY += stepNumY;
                    denom += stepDenom;
                }
            }
        }

        return true;
    }

    private static void AddCandidate(List<FlipCandidate> candidates, int maxCandidates, int x, int y, bool value, int score) {
        if (candidates.Count < maxCandidates) {
            candidates.Add(new FlipCandidate(x, y, value, score));
            return;
        }

        var minIndex = 0;
        var minScore = candidates[0].Score;
        for (var i = 1; i < candidates.Count; i++) {
            var candidateScore = candidates[i].Score;
            if (candidateScore < minScore) {
                minScore = candidateScore;
                minIndex = i;
            }
        }

        if (score > minScore) {
            candidates[minIndex] = new FlipCandidate(x, y, value, score);
        }
    }

    private static bool TryDecodeWithFinderThreshold(
        QrGrayImage image,
        bool invert,
        in QrPerspectiveTransform transform,
        int dimension,
        double phaseX,
        double phaseY,
        double delta,
        int mode,
        bool useMeanSampling,
        bool loose,
        Func<QrDecoded, bool>? accept,
        DecodeBudget budget,
        Func<bool>? shouldStop,
        out QrDecoded result,
        out global::CodeGlyphX.QrDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (!TryComputeFinderThreshold(image, transform, dimension, phaseX, phaseY, out var threshold)) {
            return false;
        }

        var adjusted = image.WithThreshold(threshold);
        var matrix = new global::CodeGlyphX.BitMatrix(dimension, dimension);
        var ok = mode switch {
            0 => loose
                ? SampleModules<Center3x3LooseSampler>(adjusted, invert, transform, dimension, phaseX, phaseY, matrix, budget, int.MaxValue, delta, out _)
                : SampleModules<Center3x3Sampler>(adjusted, invert, transform, dimension, phaseX, phaseY, matrix, budget, int.MaxValue, delta, out _),
            1 => loose
                ? (useMeanSampling
                    ? SampleModules<Mean25LooseSampler>(adjusted, invert, transform, dimension, phaseX, phaseY, matrix, budget, int.MaxValue, delta, out _)
                    : SampleModules<Nearest25LooseSampler>(adjusted, invert, transform, dimension, phaseX, phaseY, matrix, budget, int.MaxValue, delta, out _))
                : (useMeanSampling
                    ? SampleModules<Mean25Sampler>(adjusted, invert, transform, dimension, phaseX, phaseY, matrix, budget, int.MaxValue, delta, out _)
                    : SampleModules<Nearest25Sampler>(adjusted, invert, transform, dimension, phaseX, phaseY, matrix, budget, int.MaxValue, delta, out _)),
            _ => loose
                ? SampleModules<Nearest9LooseSampler>(adjusted, invert, transform, dimension, phaseX, phaseY, matrix, budget, int.MaxValue, delta, out _)
                : SampleModules<Nearest9Sampler>(adjusted, invert, transform, dimension, phaseX, phaseY, matrix, budget, int.MaxValue, delta, out _)
        };

        if (!ok) return false;

        if (global::CodeGlyphX.QrDecoder.TryDecodeInternal(matrix, shouldStop, out result, out diagnostics)) {
            if (accept == null || accept(result)) return true;
        }

        if (!budget.IsNearDeadline(160) &&
            global::CodeGlyphX.QrDecoder.TryDecodeAllFormatCandidates(matrix, shouldStop, out result, out diagnostics)) {
            if (accept == null || accept(result)) return true;
        }

        return false;
    }

    private static bool TryDecodeWithPhaseMajority(
        QrGrayImage image,
        bool invert,
        in QrPerspectiveTransform transform,
        int dimension,
        double phaseX,
        double phaseY,
        double delta,
        int mode,
        bool useMeanSampling,
        bool loose,
        Func<QrDecoded, bool>? accept,
        DecodeBudget budget,
        Func<bool>? shouldStop,
        out QrDecoded result,
        out global::CodeGlyphX.QrDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        if (budget.IsNearDeadline(200)) return false;

        var primary = new global::CodeGlyphX.BitMatrix(dimension, dimension);
        var altA = new global::CodeGlyphX.BitMatrix(dimension, dimension);
        var altB = new global::CodeGlyphX.BitMatrix(dimension, dimension);

        var okPrimary = SampleWithMode(image, invert, transform, dimension, phaseX, phaseY, delta, mode, useMeanSampling, loose, primary, budget);
        if (!okPrimary) return false;

        var okA = SampleWithMode(image, invert, transform, dimension, phaseX - 0.15, phaseY - 0.15, delta, mode, useMeanSampling, loose, altA, budget);
        var okB = SampleWithMode(image, invert, transform, dimension, phaseX + 0.15, phaseY + 0.15, delta, mode, useMeanSampling, loose, altB, budget);
        if (!okA || !okB) return false;

        var combined = new global::CodeGlyphX.BitMatrix(dimension, dimension);
        if (TryDecodeMajorityMatrices(primary, altA, altB, combined, accept, budget, shouldStop, out result, out diagnostics)) {
            return true;
        }

        return false;
    }

    private static bool SampleWithMode(
        QrGrayImage image,
        bool invert,
        in QrPerspectiveTransform transform,
        int dimension,
        double phaseX,
        double phaseY,
        double delta,
        int mode,
        bool useMeanSampling,
        bool loose,
        global::CodeGlyphX.BitMatrix matrix,
        DecodeBudget budget) {
        return mode switch {
            0 => loose
                ? SampleModules<Center3x3LooseSampler>(image, invert, transform, dimension, phaseX, phaseY, matrix, budget, int.MaxValue, delta, out _)
                : SampleModules<Center3x3Sampler>(image, invert, transform, dimension, phaseX, phaseY, matrix, budget, int.MaxValue, delta, out _),
            1 => loose
                ? (useMeanSampling
                    ? SampleModules<Mean25LooseSampler>(image, invert, transform, dimension, phaseX, phaseY, matrix, budget, int.MaxValue, delta, out _)
                    : SampleModules<Nearest25LooseSampler>(image, invert, transform, dimension, phaseX, phaseY, matrix, budget, int.MaxValue, delta, out _))
                : (useMeanSampling
                    ? SampleModules<Mean25Sampler>(image, invert, transform, dimension, phaseX, phaseY, matrix, budget, int.MaxValue, delta, out _)
                    : SampleModules<Nearest25Sampler>(image, invert, transform, dimension, phaseX, phaseY, matrix, budget, int.MaxValue, delta, out _)),
            _ => loose
                ? SampleModules<Nearest9LooseSampler>(image, invert, transform, dimension, phaseX, phaseY, matrix, budget, int.MaxValue, delta, out _)
                : SampleModules<Nearest9Sampler>(image, invert, transform, dimension, phaseX, phaseY, matrix, budget, int.MaxValue, delta, out _)
        };
    }

    private static bool TryComputeFinderThreshold(
        QrGrayImage image,
        in QrPerspectiveTransform transform,
        int dimension,
        double phaseX,
        double phaseY,
        out byte threshold) {
        threshold = 0;
        if (dimension < 21) return false;

        double blackSum = 0;
        double whiteSum = 0;
        var blackCount = 0;
        var whiteCount = 0;

        SampleFinder(0, 0, image, transform, phaseX, phaseY, ref blackSum, ref whiteSum, ref blackCount, ref whiteCount);
        SampleFinder(dimension - 7, 0, image, transform, phaseX, phaseY, ref blackSum, ref whiteSum, ref blackCount, ref whiteCount);
        SampleFinder(0, dimension - 7, image, transform, phaseX, phaseY, ref blackSum, ref whiteSum, ref blackCount, ref whiteCount);

        if (blackCount < 20 || whiteCount < 20) return false;

        var meanBlack = blackSum / blackCount;
        var meanWhite = whiteSum / whiteCount;
        var mid = (meanBlack + meanWhite) * 0.5;
        if (mid < 0) mid = 0;
        if (mid > 255) mid = 255;
        threshold = (byte)(mid + 0.5);
        return true;
    }

    private static void SampleFinder(
        int startX,
        int startY,
        QrGrayImage image,
        in QrPerspectiveTransform transform,
        double phaseX,
        double phaseY,
        ref double blackSum,
        ref double whiteSum,
        ref int blackCount,
        ref int whiteCount) {
        for (var y = 0; y < 7; y++) {
            var my = startY + y;
            for (var x = 0; x < 7; x++) {
                var mx = startX + x;
                var border = x == 0 || x == 6 || y == 0 || y == 6;
                var center = x >= 2 && x <= 4 && y >= 2 && y <= 4;
                var expectBlack = border || center;

                var sx = mx + 0.5 + phaseX;
                var sy = my + 0.5 + phaseY;
                transform.Transform(sx, sy, out var ix, out var iy);
                if (!double.IsFinite(ix + iy)) continue;

                QrPixelSampling.SampleLumaWithThreshold(image, ix, iy, out var lum, out _);
                if (expectBlack) {
                    blackSum += lum;
                    blackCount++;
                } else {
                    whiteSum += lum;
                    whiteCount++;
                }
            }
        }
    }

    private static bool TryDecodeWithDimensionSweep(
        QrGrayImage image,
        bool invert,
        in QrPerspectiveTransform transform,
        int dimension,
        double moduleSizePx,
        double phaseX,
        double phaseY,
        double delta,
        int mode,
        bool useMeanSampling,
        bool loose,
        Func<QrDecoded, bool>? accept,
        DecodeBudget budget,
        Func<bool>? shouldStop,
        out QrDecoded result,
        out global::CodeGlyphX.QrDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        Span<int> offsets = stackalloc int[] { -4, 4, -8, 8 };
        for (var i = 0; i < offsets.Length; i++) {
            if (budget.IsNearDeadline(200) || shouldStop?.Invoke() == true) return false;

            var newDim = dimension + offsets[i];
            if (newDim < 21 || newDim > 97) continue;
            if ((newDim - 17) % 4 != 0) continue;

            var scale = dimension / (double)newDim;
            var newDelta = delta * scale;
            var matrix = new global::CodeGlyphX.BitMatrix(newDim, newDim);

            var ok = mode switch {
                0 => loose
                    ? SampleModules<Center3x3LooseSampler>(image, invert, transform, newDim, phaseX, phaseY, matrix, budget, int.MaxValue, newDelta, out _)
                    : SampleModules<Center3x3Sampler>(image, invert, transform, newDim, phaseX, phaseY, matrix, budget, int.MaxValue, newDelta, out _),
                1 => loose
                    ? (useMeanSampling
                        ? SampleModules<Mean25LooseSampler>(image, invert, transform, newDim, phaseX, phaseY, matrix, budget, int.MaxValue, newDelta, out _)
                        : SampleModules<Nearest25LooseSampler>(image, invert, transform, newDim, phaseX, phaseY, matrix, budget, int.MaxValue, newDelta, out _))
                    : (useMeanSampling
                        ? SampleModules<Mean25Sampler>(image, invert, transform, newDim, phaseX, phaseY, matrix, budget, int.MaxValue, newDelta, out _)
                        : SampleModules<Nearest25Sampler>(image, invert, transform, newDim, phaseX, phaseY, matrix, budget, int.MaxValue, newDelta, out _)),
                _ => loose
                    ? SampleModules<Nearest9LooseSampler>(image, invert, transform, newDim, phaseX, phaseY, matrix, budget, int.MaxValue, newDelta, out _)
                    : SampleModules<Nearest9Sampler>(image, invert, transform, newDim, phaseX, phaseY, matrix, budget, int.MaxValue, newDelta, out _)
            };

            if (!ok) continue;
            if (global::CodeGlyphX.QrDecoder.TryDecodeInternal(matrix, shouldStop, out result, out diagnostics)) {
                if (accept == null || accept(result)) return true;
            }

            if (!budget.IsNearDeadline(160) &&
                global::CodeGlyphX.QrDecoder.TryDecodeAllFormatCandidates(matrix, shouldStop, out result, out diagnostics)) {
                if (accept == null || accept(result)) return true;
            }

            if (!budget.IsNearDeadline(200)) {
                MaybeDumpModuleMatrix(matrix, newDim, diagnostics, suffix: "sweep");
            }
        }

        return false;
    }

    private static void ApplyMajorityFilter(global::CodeGlyphX.BitMatrix source, global::CodeGlyphX.BitMatrix target, global::CodeGlyphX.BitMatrix functionMask, int minBlack) {
        var size = source.Width;
        for (var y = 0; y < size; y++) {
            for (var x = 0; x < size; x++) {
                if (functionMask[x, y]) {
                    target[x, y] = source[x, y];
                    continue;
                }

                var black = 0;
                var y0 = y - 1;
                var y1 = y + 1;
                if (y0 < 0) y0 = 0;
                if (y1 >= size) y1 = size - 1;
                for (var yy = y0; yy <= y1; yy++) {
                    var x0 = x - 1;
                    var x1 = x + 1;
                    if (x0 < 0) x0 = 0;
                    if (x1 >= size) x1 = size - 1;
                    for (var xx = x0; xx <= x1; xx++) {
                        if (source[xx, yy]) black++;
                    }
                }

                target[x, y] = black >= minBlack;
            }
        }
    }

    private static bool ShouldTryAllFormatCandidates() {
        var value = Environment.GetEnvironmentVariable("CODEGLYPHX_QR_FORCE_ALL_FORMAT");
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static void MaybeDumpModuleMatrix(global::CodeGlyphX.BitMatrix matrix, int dimension, global::CodeGlyphX.QrDecodeDiagnostics diagnostics, string suffix) {
        if (!TryGetModuleDumpSettings(out var dir, out var limit)) return;
        if (matrix.Width != matrix.Height || matrix.Width <= 0) return;
        if (TryGetModuleDumpRange(out var minDim, out var maxDim)) {
            if (dimension < minDim || dimension > maxDim) return;
        }

        var count = Interlocked.Increment(ref ModuleDumpCount);
        if (count > limit) return;

        Directory.CreateDirectory(dir);

        var version = diagnostics.Version;
        if (!QrStructureAnalysis.TryGetVersionFromSize(dimension, out var inferred)) {
            inferred = 0;
        }
        if (version <= 0) version = inferred;

        var failure = diagnostics.Failure.ToString().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(failure)) failure = "unknown";
        var fileName = $"qr-modules-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{count:00}-v{version}-dim{dimension}-{failure}-{suffix}.png";

        var opts = new MatrixPngRenderOptions {
            ModuleSize = 6,
            QuietZone = 2,
            PngCompressionLevel = 1
        };
        MatrixPngRenderer.RenderToFile(matrix, opts, dir, fileName);

        var asciiName = Path.ChangeExtension(fileName, ".txt");
        var asciiOptions = new CodeGlyphX.Rendering.Ascii.MatrixAsciiRenderOptions {
            QuietZone = 1,
            ModuleWidth = 1,
            ModuleHeight = 1,
            UseUnicodeBlocks = false
        };
        var ascii = CodeGlyphX.Rendering.Ascii.MatrixAsciiRenderer.Render(matrix, asciiOptions);
        File.WriteAllText(Path.Combine(dir, asciiName), ascii);

        var metaName = Path.ChangeExtension(fileName, ".meta.txt");
        var timingRowAlt = CountTimingAlternationsRow(matrix);
        var timingColAlt = CountTimingAlternationsCol(matrix);
        var blackRatio = ComputeBlackRatio(matrix);
        var meta = $"dim={dimension}{Environment.NewLine}" +
                   $"timingRowAlt={timingRowAlt}{Environment.NewLine}" +
                   $"timingColAlt={timingColAlt}{Environment.NewLine}" +
                   $"blackRatio={blackRatio:P2}{Environment.NewLine}";
        File.WriteAllText(Path.Combine(dir, metaName), meta);
    }

    private static bool TryGetModuleDumpSettings(out string dir, out int limit) {
        dir = Environment.GetEnvironmentVariable("CODEGLYPHX_QR_MODULE_DUMP_DIR") ??
              Environment.GetEnvironmentVariable("CODEGLYPHX_QR_MODULE_DUMP") ??
              string.Empty;
        if (string.IsNullOrWhiteSpace(dir)) {
            limit = 0;
            return false;
        }

        if (string.Equals(dir, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dir, "true", StringComparison.OrdinalIgnoreCase)) {
            dir = Path.Combine(Path.GetTempPath(), "codeglyphx-module-dumps");
        }

        limit = 3;
        var limitRaw = Environment.GetEnvironmentVariable("CODEGLYPHX_QR_MODULE_DUMP_LIMIT");
        if (!string.IsNullOrWhiteSpace(limitRaw) && int.TryParse(limitRaw, out var parsed) && parsed > 0) {
            limit = parsed;
        }

        if (!string.Equals(ModuleDumpDirCache, dir, StringComparison.Ordinal)) {
            ModuleDumpDirCache = dir;
            Interlocked.Exchange(ref ModuleDumpCount, 0);
        }

        return true;
    }

    private static bool TryGetModuleDumpRange(out int minDim, out int maxDim) {
        minDim = 0;
        maxDim = int.MaxValue;
        var minRaw = Environment.GetEnvironmentVariable("CODEGLYPHX_QR_MODULE_DUMP_MIN_DIM");
        var maxRaw = Environment.GetEnvironmentVariable("CODEGLYPHX_QR_MODULE_DUMP_MAX_DIM");
        var hasMin = int.TryParse(minRaw, out minDim);
        var hasMax = int.TryParse(maxRaw, out maxDim);
        if (!hasMin) minDim = 0;
        if (!hasMax) maxDim = int.MaxValue;
        return hasMin || hasMax;
    }

    private static int CountTimingAlternationsRow(global::CodeGlyphX.BitMatrix matrix) {
        var size = matrix.Width;
        if (size <= 16) return 0;
        var start = 8;
        var end = size - 9;
        if (end <= start) return 0;

        var alt = 0;
        var last = matrix[start, 6];
        for (var x = start + 1; x <= end; x++) {
            var v = matrix[x, 6];
            if (v != last) alt++;
            last = v;
        }
        return alt;
    }

    private static int CountTimingAlternationsCol(global::CodeGlyphX.BitMatrix matrix) {
        var size = matrix.Width;
        if (size <= 16) return 0;
        var start = 8;
        var end = size - 9;
        if (end <= start) return 0;

        var alt = 0;
        var last = matrix[6, start];
        for (var y = start + 1; y <= end; y++) {
            var v = matrix[6, y];
            if (v != last) alt++;
            last = v;
        }
        return alt;
    }

    private static double ComputeBlackRatio(global::CodeGlyphX.BitMatrix matrix) {
        var size = matrix.Width;
        if (size <= 0) return 0;
        var total = size * size;
        var black = 0;
        for (var y = 0; y < size; y++) {
            for (var x = 0; x < size; x++) {
                if (matrix[x, y]) black++;
            }
        }
        return total == 0 ? 0 : black / (double)total;
    }

    private static bool TryDecodeByRotations(global::CodeGlyphX.BitMatrix matrix, Func<QrDecoded, bool>? accept, DecodeBudget budget, out QrDecoded result, out global::CodeGlyphX.QrDecodeDiagnostics diagnostics) {
        result = null!;
        diagnostics = default;

        var best = default(global::CodeGlyphX.QrDecodeDiagnostics);

        var rotated = new global::CodeGlyphX.BitMatrix(matrix.Width, matrix.Height);
        RotateInto90(matrix, rotated);
        if (TryDecodeWithInversion(rotated, accept, budget, out result, out var d90)) {
            diagnostics = d90;
            return true;
        }
        best = Better(best, d90);

        RotateInto180(matrix, rotated);
        if (TryDecodeWithInversion(rotated, accept, budget, out result, out var d180)) {
            diagnostics = d180;
            return true;
        }
        best = Better(best, d180);

        RotateInto270(matrix, rotated);
        if (TryDecodeWithInversion(rotated, accept, budget, out result, out var d270)) {
            diagnostics = d270;
            return true;
        }
        best = Better(best, d270);

        diagnostics = best;
        return false;
    }

    private static void RotateInto90(global::CodeGlyphX.BitMatrix source, global::CodeGlyphX.BitMatrix target) {
        target.Clear();
        var width = source.Width;
        var height = source.Height;
        var srcWords = source.Words;
        var dstWords = target.Words;
        for (var y = 0; y < height; y++) {
            var rowBase = y * width;
            for (var x = 0; x < width; x++) {
                var bitIndex = rowBase + x;
                if ((srcWords[bitIndex >> 5] & (1u << (bitIndex & 31))) == 0) continue;
                var rx = height - 1 - y;
                var ry = x;
                var dstIndex = ry * width + rx;
                dstWords[dstIndex >> 5] |= 1u << (dstIndex & 31);
            }
        }
    }

    private static void RotateInto180(global::CodeGlyphX.BitMatrix source, global::CodeGlyphX.BitMatrix target) {
        target.Clear();
        var width = source.Width;
        var height = source.Height;
        var srcWords = source.Words;
        var dstWords = target.Words;
        for (var y = 0; y < height; y++) {
            var rowBase = y * width;
            for (var x = 0; x < width; x++) {
                var bitIndex = rowBase + x;
                if ((srcWords[bitIndex >> 5] & (1u << (bitIndex & 31))) == 0) continue;
                var rx = width - 1 - x;
                var ry = height - 1 - y;
                var dstIndex = ry * width + rx;
                dstWords[dstIndex >> 5] |= 1u << (dstIndex & 31);
            }
        }
    }

    private static void RotateInto270(global::CodeGlyphX.BitMatrix source, global::CodeGlyphX.BitMatrix target) {
        target.Clear();
        var width = source.Width;
        var height = source.Height;
        var srcWords = source.Words;
        var dstWords = target.Words;
        for (var y = 0; y < height; y++) {
            var rowBase = y * width;
            for (var x = 0; x < width; x++) {
                var bitIndex = rowBase + x;
                if ((srcWords[bitIndex >> 5] & (1u << (bitIndex & 31))) == 0) continue;
                var rx = y;
                var ry = width - 1 - x;
                var dstIndex = ry * width + rx;
                dstWords[dstIndex >> 5] |= 1u << (dstIndex & 31);
            }
        }
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

}
#endif
