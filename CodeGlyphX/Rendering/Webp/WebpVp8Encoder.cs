using System;
using System.IO;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// Managed VP8 lossy encoder (minimal intra-only encoder).
/// </summary>
internal static class WebpVp8Encoder {
    private const int BlockSize = 4;
    private const int MacroblockSize = 16;
    private const int MacroblockSubBlockCount = 16;
    private const int MacroblockChromaBlocks = 4;
    private const int Intra4x4ModeCount = 10;
    private const int IntraUvModeCount = 4;
    private const int SegmentCount = 4;
    private const int SegmentProbCount = 3;
    private const int YModeBPred = 4;
    private const int BModeDcPred = 0;
    private const int ModeDcPred = 0;
    private const int BlockTypeY2 = 0;
    private const int BlockTypeY = 1;
    private const int BlockTypeU = 2;
    private const int BlockTypeV = 3;
    private const int CoeffBlockTypes = 4;
    private const int CoeffBands = 8;
    private const int CoeffPrevContexts = 3;
    private const int CoeffEntropyNodes = 11;
    private const int CoefficientsPerBlock = 16;
    private const int IdctCospi8Sqrt2Minus1 = 20091;
    private const int IdctSinpi8Sqrt2 = 35468;
    private const int MaxCoefficientMagnitude = 2047;

    // Precompute a forward transform that matches the decoder's inverse transform.
    private static readonly double[,] ForwardTransform = BuildForwardTransformMatrix();
    private static readonly double[,] ForwardWalshTransform = BuildForwardWalshTransformMatrix();

    private static readonly int[] CoeffBandTable =
    {
        0, 1, 2, 3,
        6, 4, 5, 6,
        6, 6, 6, 6,
        6, 7, 7, 7,
    };

    private static readonly int[] ZigZagToNaturalOrder =
    {
        0, 1, 4, 8,
        5, 2, 3, 6,
        9, 12, 13, 10,
        7, 11, 14, 15,
    };

    private static readonly int[] CoeffTokenExtraBits =
    {
        0, 0, 0, 0, 0, 0,
        1, 2, 3, 4, 5, 11,
    };

    private static readonly int[] CoeffTokenBaseMagnitude =
    {
        0, 0, 1, 2, 3, 4,
        5, 7, 11, 19, 35, 67,
    };

    private static readonly int[] CoeffTokenTree =
    {
        -1, 2,
        -2, 4,
        -3, 6,
        -4, 8,
        -5, 10,
        -6, 12,
        -7, 14,
        -8, 16,
        -9, 18,
        -10, 20,
        -11, -12,
    };

    public static bool TryEncodeLossyRgba32(
        ReadOnlySpan<byte> rgba,
        int width,
        int height,
        int stride,
        int quality,
        out byte[] webp,
        out string reason) {
        webp = Array.Empty<byte>();
        reason = string.Empty;

        if (width <= 0 || height <= 0) {
            reason = "Width and height must be positive.";
            return false;
        }

        if (quality is < 0 or > 100) {
            reason = "Quality must be in the 0-100 range.";
            return false;
        }

        var minStride = checked(width * 4);
        if (stride < minStride) {
            reason = "Stride is smaller than width * 4.";
            return false;
        }

        var requiredBytes = checked((height - 1) * stride + minStride);
        if (rgba.Length < requiredBytes) {
            reason = "Input RGBA buffer is too small for the provided dimensions/stride.";
            return false;
        }

        var alphaUsed = ComputeAlphaUsed(rgba, width, height, stride);

        if (!TryEncodeVp8Payload(rgba, width, height, stride, quality, out var payload, out reason)) {
            return false;
        }

        var alphPayload = alphaUsed ? BuildAlphPayload(rgba, width, height, stride) : Array.Empty<byte>();
        webp = WriteWebpContainer(payload, alphPayload, width, height);
        return true;
    }

    private static bool TryEncodeVp8Payload(
        ReadOnlySpan<byte> rgba,
        int width,
        int height,
        int stride,
        int quality,
        out byte[] payload,
        out string reason) {
        payload = Array.Empty<byte>();
        reason = string.Empty;

        var baseQIndex = QualityToBaseQIndex(quality);

        ConvertRgbaToYuv420(rgba, width, height, stride, out var yPlane, out var uPlane, out var vPlane);

        var reconY = new byte[yPlane.Length];
        var reconU = new byte[uPlane.Length];
        var reconV = new byte[vPlane.Length];

        var macroblockCols = GetMacroblockDimension(width);
        var macroblockRows = GetMacroblockDimension(height);
        if (macroblockCols <= 0 || macroblockRows <= 0) {
            reason = "Invalid macroblock geometry.";
            return false;
        }

        var segmentIds = BuildSegmentMap(yPlane, width, height, macroblockCols, macroblockRows, baseQIndex, out var segmentationEnabled, out var quantizerDeltas, out var segmentProbabilities);
        var dequantSegments = BuildDequantFactors(baseQIndex, quantizerDeltas, segmentationEnabled);

        var headerWriter = new WebpVp8BoolEncoder(expectedSize: 4096);
        WriteControlHeader(headerWriter);
        WriteSegmentation(headerWriter, segmentationEnabled, quantizerDeltas, segmentProbabilities);
        WriteLoopFilter(headerWriter, quality);
        headerWriter.WriteLiteral(0, 2); // one DCT partition
        WriteQuantization(headerWriter, baseQIndex);
        WriteCoefficientProbabilityUpdates(headerWriter);
        headerWriter.WriteBool(128, false); // refresh entropy probs
        headerWriter.WriteBool(128, false); // no coefficient skip

        var tokenWriter = new WebpVp8BoolEncoder(expectedSize: 4096);

        var aboveSubModes = new int[macroblockCols * MacroblockSubBlockCount];
        var currentSubModes = new int[macroblockCols * MacroblockSubBlockCount];
        var yNzAbove = new byte[macroblockCols * MacroblockSubBlockCount];
        var yNzCurrent = new byte[macroblockCols * MacroblockSubBlockCount];
        var y2NzAbove = new byte[macroblockCols];
        var y2NzCurrent = new byte[macroblockCols];
        var uNzAbove = new byte[macroblockCols * MacroblockChromaBlocks];
        var uNzCurrent = new byte[macroblockCols * MacroblockChromaBlocks];
        var vNzAbove = new byte[macroblockCols * MacroblockChromaBlocks];
        var vNzCurrent = new byte[macroblockCols * MacroblockChromaBlocks];

        var probabilities = BuildDefaultProbabilities();

        for (var row = 0; row < macroblockRows; row++) {
            for (var col = 0; col < macroblockCols; col++) {
                var macroblockIndex = (row * macroblockCols) + col;
                var segmentId = segmentationEnabled && segmentIds.Length > macroblockIndex ? segmentIds[macroblockIndex] : 0;
                EncodeMacroblock(
                    headerWriter,
                    tokenWriter,
                    probabilities,
                    yPlane,
                    uPlane,
                    vPlane,
                    reconY,
                    reconU,
                    reconV,
                    width,
                    height,
                    col,
                    row,
                    dequantSegments,
                    segmentId,
                    y2NzAbove,
                    y2NzCurrent,
                    yNzAbove,
                    yNzCurrent,
                    uNzAbove,
                    uNzCurrent,
                    vNzAbove,
                    vNzCurrent,
                    aboveSubModes,
                    currentSubModes,
                    segmentationEnabled,
                    segmentProbabilities);
            }

            SwapRowModes(ref aboveSubModes, ref currentSubModes);
            SwapRowContexts(ref y2NzAbove, ref y2NzCurrent);
            SwapRowContexts(ref yNzAbove, ref yNzCurrent);
            SwapRowContexts(ref uNzAbove, ref uNzCurrent);
            SwapRowContexts(ref vNzAbove, ref vNzCurrent);
        }

        var headerBytes = headerWriter.Finish();
        if (headerBytes.Length < 2) headerBytes = PadToLength(headerBytes, 2);
        var tokenBytes = tokenWriter.Finish();
        if (tokenBytes.Length < 2) tokenBytes = PadToLength(tokenBytes, 2);

        var keyframeHeader = BuildKeyframeHeader(width, height);
        var firstPartition = new byte[keyframeHeader.Length + headerBytes.Length];
        Buffer.BlockCopy(keyframeHeader, 0, firstPartition, 0, keyframeHeader.Length);
        Buffer.BlockCopy(headerBytes, 0, firstPartition, keyframeHeader.Length, headerBytes.Length);

        var firstPartitionSize = firstPartition.Length;
        if (firstPartitionSize > 0x7FFFF) {
            reason = "VP8 first partition is too large.";
            return false;
        }

        var frameTag = BuildFrameTag(firstPartitionSize, version: 0, showFrame: true, keyframe: true);

        payload = new byte[frameTag.Length + firstPartition.Length + tokenBytes.Length];
        Buffer.BlockCopy(frameTag, 0, payload, 0, frameTag.Length);
        Buffer.BlockCopy(firstPartition, 0, payload, frameTag.Length, firstPartition.Length);
        Buffer.BlockCopy(tokenBytes, 0, payload, frameTag.Length + firstPartition.Length, tokenBytes.Length);

        return true;
    }

    private static void EncodeMacroblock(
        WebpVp8BoolEncoder headerWriter,
        WebpVp8BoolEncoder tokenWriter,
        int[] probabilities,
        byte[] yPlane,
        byte[] uPlane,
        byte[] vPlane,
        byte[] reconY,
        byte[] reconU,
        byte[] reconV,
        int width,
        int height,
        int mbX,
        int mbY,
        DequantFactors[] dequantSegments,
        int segmentId,
        byte[] y2NzAbove,
        byte[] y2NzCurrent,
        byte[] yNzAbove,
        byte[] yNzCurrent,
        byte[] uNzAbove,
        byte[] uNzCurrent,
        byte[] vNzAbove,
        byte[] vNzCurrent,
        int[] aboveSubModes,
        int[] currentSubModes,
        bool segmentationEnabled,
        int[] segmentProbabilities) {
        var macroblockOffsetX = mbX * MacroblockSize;
        var macroblockOffsetY = mbY * MacroblockSize;
        var chromaWidth = (width + 1) >> 1;
        var chromaHeight = (height + 1) >> 1;
        var chromaOffsetX = mbX * (MacroblockSize / 2);
        var chromaOffsetY = mbY * (MacroblockSize / 2);

        if (segmentationEnabled) {
            WriteSegmentId(headerWriter, segmentProbabilities, segmentId);
        }

        if ((uint)segmentId >= SegmentCount) segmentId = 0;
        var dequant = (dequantSegments != null && dequantSegments.Length > segmentId)
            ? dequantSegments[segmentId]
            : dequantSegments?[0] ?? BuildDequantFactors(0);

        var yBlockBase = mbX * MacroblockSubBlockCount;

        long bestBPredCost = 0;
        for (var blockIndex = 0; blockIndex < MacroblockSubBlockCount; blockIndex++) {
            var subX = blockIndex & 3;
            var subY = blockIndex >> 2;
            var dstX = macroblockOffsetX + (subX * BlockSize);
            var dstY = macroblockOffsetY + (subY * BlockSize);
            ChooseBestBlockMode(yPlane, reconY, width, height, dstX, dstY, out var cost);
            bestBPredCost += cost;
        }

        var bestY16Mode = ModeDcPred;
        long bestY16Cost = long.MaxValue;
        Span<byte> predicted = stackalloc byte[BlockSize * BlockSize];
        for (var mode = 0; mode <= 3; mode++) {
            long cost = 0;
            for (var blockIndex = 0; blockIndex < MacroblockSubBlockCount; blockIndex++) {
                var subX = blockIndex & 3;
                var subY = blockIndex >> 2;
                var dstX = macroblockOffsetX + (subX * BlockSize);
                var dstY = macroblockOffsetY + (subY * BlockSize);
                PredictBlock(reconY, width, height, dstX, dstY, mode, predicted);
                cost += ComputePredictionCost(yPlane, width, height, dstX, dstY, predicted);
            }

            if (cost < bestY16Cost) {
                bestY16Cost = cost;
                bestY16Mode = mode;
            }
        }

        var useY16 = bestY16Cost < bestBPredCost;
        if (useY16) {
            WriteKeyframeYMode(headerWriter, bestY16Mode);
            for (var i = 0; i < MacroblockSubBlockCount; i++) {
                currentSubModes[yBlockBase + i] = BModeDcPred;
            }

            EncodeLumaMacroblockY16(
                tokenWriter,
                probabilities,
                yPlane,
                reconY,
                width,
                height,
                mbX,
                mbY,
                bestY16Mode,
                dequant,
                y2NzAbove,
                y2NzCurrent,
                yNzAbove,
                yNzCurrent);
        } else {
            WriteKeyframeYMode(headerWriter, YModeBPred);
            y2NzCurrent[mbX] = 0;

            for (var blockIndex = 0; blockIndex < MacroblockSubBlockCount; blockIndex++) {
                var subX = blockIndex & 3;
                var subY = blockIndex >> 2;
                var initialContext = 0;

                if (subY == 0) {
                    if (mbY > 0) initialContext += yNzAbove[yBlockBase + 12 + subX];
                } else {
                    initialContext += yNzCurrent[yBlockBase + ((subY - 1) * 4) + subX];
                }

                if (subX == 0) {
                    if (mbX > 0) initialContext += yNzCurrent[yBlockBase - MacroblockSubBlockCount + (subY * 4) + 3];
                } else {
                    initialContext += yNzCurrent[yBlockBase + (subY * 4) + subX - 1];
                }

                if (initialContext > 2) initialContext = 2;

                var aboveMode = BModeDcPred;
                if (subY == 0) {
                    if (mbY > 0) {
                        aboveMode = aboveSubModes[yBlockBase + 12 + subX];
                    }
                } else {
                    aboveMode = currentSubModes[yBlockBase + ((subY - 1) * 4) + subX];
                }

                var leftMode = BModeDcPred;
                if (subX == 0) {
                    if (mbX > 0) {
                        leftMode = currentSubModes[yBlockBase - MacroblockSubBlockCount + (subY * 4) + 3];
                    }
                } else {
                    leftMode = currentSubModes[yBlockBase + (subY * 4) + subX - 1];
                }

                var dstX = macroblockOffsetX + (subX * BlockSize);
                var dstY = macroblockOffsetY + (subY * BlockSize);
                var mode = ChooseBestBlockMode(yPlane, reconY, width, height, dstX, dstY, out _);

                WriteKeyframeBMode(headerWriter, aboveMode, leftMode, mode);
                currentSubModes[yBlockBase + blockIndex] = mode;

                var hasNonZero = EncodeBlock(
                    tokenWriter,
                    probabilities,
                    BlockTypeY,
                    initialContext,
                    yPlane,
                    reconY,
                    width,
                    height,
                    dstX,
                    dstY,
                    mode,
                    dequant.Y1Dc,
                    dequant.Y1Ac);

                yNzCurrent[yBlockBase + blockIndex] = hasNonZero ? (byte)1 : (byte)0;
            }
        }

        var uvMode = ChooseBestUvMode(
            uPlane,
            vPlane,
            reconU,
            reconV,
            chromaWidth,
            chromaHeight,
            chromaOffsetX,
            chromaOffsetY);

        WriteKeyframeUvMode(headerWriter, uvMode);

        var chromaBlockBase = mbX * MacroblockChromaBlocks;
        for (var blockIndex = 0; blockIndex < MacroblockChromaBlocks; blockIndex++) {
            var subX = blockIndex & 1;
            var subY = blockIndex >> 1;
            var initialContext = 0;

            if (subY == 0) {
                if (mbY > 0) initialContext += uNzAbove[chromaBlockBase + 2 + subX];
            } else {
                initialContext += uNzCurrent[chromaBlockBase + ((subY - 1) * 2) + subX];
            }

            if (subX == 0) {
                if (mbX > 0) initialContext += uNzCurrent[chromaBlockBase - MacroblockChromaBlocks + (subY * 2) + 1];
            } else {
                initialContext += uNzCurrent[chromaBlockBase + (subY * 2) + subX - 1];
            }

            if (initialContext > 2) initialContext = 2;

            var dstX = chromaOffsetX + (subX * BlockSize);
            var dstY = chromaOffsetY + (subY * BlockSize);

            var hasNonZeroU = EncodeBlock(
                tokenWriter,
                probabilities,
                BlockTypeU,
                initialContext,
                uPlane,
                reconU,
                chromaWidth,
                chromaHeight,
                dstX,
                dstY,
                uvMode,
                dequant.UvDc,
                dequant.UvAc);

            uNzCurrent[chromaBlockBase + blockIndex] = hasNonZeroU ? (byte)1 : (byte)0;
        }

        for (var blockIndex = 0; blockIndex < MacroblockChromaBlocks; blockIndex++) {
            var subX = blockIndex & 1;
            var subY = blockIndex >> 1;
            var initialContext = 0;

            if (subY == 0) {
                if (mbY > 0) initialContext += vNzAbove[chromaBlockBase + 2 + subX];
            } else {
                initialContext += vNzCurrent[chromaBlockBase + ((subY - 1) * 2) + subX];
            }

            if (subX == 0) {
                if (mbX > 0) initialContext += vNzCurrent[chromaBlockBase - MacroblockChromaBlocks + (subY * 2) + 1];
            } else {
                initialContext += vNzCurrent[chromaBlockBase + (subY * 2) + subX - 1];
            }

            if (initialContext > 2) initialContext = 2;

            var dstX = chromaOffsetX + (subX * BlockSize);
            var dstY = chromaOffsetY + (subY * BlockSize);

            var hasNonZeroV = EncodeBlock(
                tokenWriter,
                probabilities,
                BlockTypeV,
                initialContext,
                vPlane,
                reconV,
                chromaWidth,
                chromaHeight,
                dstX,
                dstY,
                uvMode,
                dequant.UvDc,
                dequant.UvAc);

            vNzCurrent[chromaBlockBase + blockIndex] = hasNonZeroV ? (byte)1 : (byte)0;
        }
    }

    private static void EncodeLumaMacroblockY16(
        WebpVp8BoolEncoder tokenWriter,
        int[] probabilities,
        byte[] yPlane,
        byte[] reconY,
        int width,
        int height,
        int mbX,
        int mbY,
        int mode,
        DequantFactors dequant,
        byte[] y2NzAbove,
        byte[] y2NzCurrent,
        byte[] yNzAbove,
        byte[] yNzCurrent) {
        var macroblockOffsetX = mbX * MacroblockSize;
        var macroblockOffsetY = mbY * MacroblockSize;
        var yBlockBase = mbX * MacroblockSubBlockCount;

        var dcValues = new double[MacroblockSubBlockCount];
        var yQuant = new int[MacroblockSubBlockCount * CoefficientsPerBlock];

        Span<byte> predicted = stackalloc byte[BlockSize * BlockSize];
        Span<int> residual = stackalloc int[CoefficientsPerBlock];
        Span<double> coeffs = stackalloc double[CoefficientsPerBlock];

        for (var blockIndex = 0; blockIndex < MacroblockSubBlockCount; blockIndex++) {
            var subX = blockIndex & 3;
            var subY = blockIndex >> 2;
            var dstX = macroblockOffsetX + (subX * BlockSize);
            var dstY = macroblockOffsetY + (subY * BlockSize);

            PredictBlock(reconY, width, height, dstX, dstY, mode, predicted);

            FillResidual(yPlane, width, height, dstX, dstY, predicted, residual);

            ComputeCoefficients(residual, coeffs);

            dcValues[blockIndex] = coeffs[0];
            var offset = blockIndex * CoefficientsPerBlock;
            yQuant[offset] = 0;
            for (var i = 1; i < CoefficientsPerBlock; i++) {
                yQuant[offset + i] = ClampCoefficient(QuantizeDouble(coeffs[i], dequant.Y1Ac));
            }

            var qdc = ClampCoefficient(QuantizeDouble(coeffs[0], dequant.Y1Dc));
            var dequantCoeffs = new int[CoefficientsPerBlock];
            dequantCoeffs[0] = qdc * dequant.Y1Dc;
            for (var i = 1; i < CoefficientsPerBlock; i++) {
                dequantCoeffs[i] = yQuant[offset + i] * dequant.Y1Ac;
            }

            var residualDecoded = InverseTransform4x4(dequantCoeffs);
            UpdateReconstruction(reconY, width, height, dstX, dstY, predicted, residualDecoded);
        }

        Span<double> y2Coeff = stackalloc double[CoefficientsPerBlock];
        ComputeWalshCoefficients(dcValues, y2Coeff);

        var y2Quant = new int[CoefficientsPerBlock];
        for (var i = 0; i < CoefficientsPerBlock; i++) {
            var dequantFactor = i == 0 ? dequant.Y2Dc : dequant.Y2Ac;
            y2Quant[i] = ClampCoefficient(QuantizeDouble(y2Coeff[i], dequantFactor));
        }

        var y2InitialContext = 0;
        if (mbY > 0) y2InitialContext += y2NzAbove[mbX];
        if (mbX > 0) y2InitialContext += y2NzCurrent[mbX - 1];
        if (y2InitialContext > 2) y2InitialContext = 2;

        var hasNonZeroY2 = EncodeBlockCoefficients(
            tokenWriter,
            probabilities,
            BlockTypeY2,
            y2InitialContext,
            y2Quant);
        y2NzCurrent[mbX] = hasNonZeroY2 ? (byte)1 : (byte)0;

        for (var blockIndex = 0; blockIndex < MacroblockSubBlockCount; blockIndex++) {
            var subX = blockIndex & 3;
            var subY = blockIndex >> 2;
            var initialContext = 0;

            if (subY == 0) {
                if (mbY > 0) initialContext += yNzAbove[yBlockBase + 12 + subX];
            } else {
                initialContext += yNzCurrent[yBlockBase + ((subY - 1) * 4) + subX];
            }

            if (subX == 0) {
                if (mbX > 0) initialContext += yNzCurrent[yBlockBase - MacroblockSubBlockCount + (subY * 4) + 3];
            } else {
                initialContext += yNzCurrent[yBlockBase + (subY * 4) + subX - 1];
            }

            if (initialContext > 2) initialContext = 2;

            var offset = blockIndex * CoefficientsPerBlock;
            var coeffTokens = new int[CoefficientsPerBlock];
            coeffTokens[0] = 0;
            for (var i = 1; i < CoefficientsPerBlock; i++) {
                coeffTokens[i] = yQuant[offset + i];
            }

            var hasNonZero = EncodeBlockCoefficients(
                tokenWriter,
                probabilities,
                BlockTypeY,
                initialContext,
                coeffTokens);

            yNzCurrent[yBlockBase + blockIndex] = hasNonZero ? (byte)1 : (byte)0;
        }

        var y2Dequant = new int[CoefficientsPerBlock];
        for (var i = 0; i < CoefficientsPerBlock; i++) {
            var dequantFactor = i == 0 ? dequant.Y2Dc : dequant.Y2Ac;
            y2Dequant[i] = y2Quant[i] * dequantFactor;
        }

        var dcOverride = InverseWalshTransform4x4(y2Dequant);
        for (var blockIndex = 0; blockIndex < MacroblockSubBlockCount; blockIndex++) {
            var subX = blockIndex & 3;
            var subY = blockIndex >> 2;
            var dstX = macroblockOffsetX + (subX * BlockSize);
            var dstY = macroblockOffsetY + (subY * BlockSize);

            PredictBlock(reconY, width, height, dstX, dstY, mode, predicted);

            var offset = blockIndex * CoefficientsPerBlock;
            var dequantCoeffs = new int[CoefficientsPerBlock];
            dequantCoeffs[0] = dcOverride[blockIndex];
            for (var i = 1; i < CoefficientsPerBlock; i++) {
                dequantCoeffs[i] = yQuant[offset + i] * dequant.Y1Ac;
            }

            var residualDecoded = InverseTransform4x4(dequantCoeffs);
            UpdateReconstruction(reconY, width, height, dstX, dstY, predicted, residualDecoded);
        }
    }

    private static bool EncodeBlock(
        WebpVp8BoolEncoder tokenWriter,
        int[] probabilities,
        int blockType,
        int initialContext,
        byte[] source,
        byte[] recon,
        int planeWidth,
        int planeHeight,
        int dstX,
        int dstY,
        int mode,
        int dequantDc,
        int dequantAc) {
        Span<byte> predicted = stackalloc byte[BlockSize * BlockSize];
        PredictBlock(recon, planeWidth, planeHeight, dstX, dstY, mode, predicted);

        Span<int> residual = stackalloc int[CoefficientsPerBlock];
        FillResidual(source, planeWidth, planeHeight, dstX, dstY, predicted, residual);

        Span<double> dequantized = stackalloc double[CoefficientsPerBlock];
        ComputeCoefficients(residual, dequantized);

        var coefficients = new int[CoefficientsPerBlock];
        for (var i = 0; i < CoefficientsPerBlock; i++) {
            var dequant = i == 0 ? dequantDc : dequantAc;
            coefficients[i] = ClampCoefficient(QuantizeDouble(dequantized[i], dequant));
        }

        var hasNonZero = EncodeBlockCoefficients(
            tokenWriter,
            probabilities,
            blockType,
            initialContext,
            coefficients);

        var dequantCoeffs = new int[CoefficientsPerBlock];
        for (var i = 0; i < CoefficientsPerBlock; i++) {
            var dequant = i == 0 ? dequantDc : dequantAc;
            dequantCoeffs[i] = coefficients[i] * dequant;
        }

        var residualDecoded = InverseTransform4x4(dequantCoeffs);
        UpdateReconstruction(recon, planeWidth, planeHeight, dstX, dstY, predicted, residualDecoded);

        return hasNonZero;
    }

    private static bool EncodeBlockCoefficients(
        WebpVp8BoolEncoder encoder,
        int[] probabilities,
        int blockType,
        int initialContext,
        int[] coefficientsNatural) {
        var prevContext = initialContext;
        var hasNonZero = false;

        for (var coefficientIndex = 0; coefficientIndex < CoefficientsPerBlock; coefficientIndex++) {
            var band = CoeffBandTable[coefficientIndex];
            var naturalIndex = ZigZagToNaturalOrder[coefficientIndex];
            var value = coefficientsNatural[naturalIndex];
            var hasLater = HasNonZeroAfter(coefficientsNatural, coefficientIndex + 1);

            int token;
            int extraBits;
            if (value == 0) {
                token = hasLater ? 1 : 0;
                extraBits = 0;
            } else {
                token = GetTokenForMagnitude(Math.Abs(value), out extraBits);
            }

            WriteCoefficientToken(encoder, probabilities, blockType, band, prevContext, token);

            if (token == 0) {
                break;
            }

            if (token > 1) {
                if (extraBits > 0) {
                    encoder.WriteLiteral(extraBits, CoeffTokenExtraBits[token]);
                }

                encoder.WriteBool(128, value < 0);
                hasNonZero = true;
            }

            prevContext = GetPrevContextAfter(token);
        }

        return hasNonZero;
    }

    private static void WriteCoefficientToken(
        WebpVp8BoolEncoder encoder,
        int[] probabilities,
        int blockType,
        int band,
        int prevContext,
        int token) {
        if ((uint)blockType >= CoeffBlockTypes) blockType = BlockTypeY;
        if ((uint)band >= CoeffBands) band = 0;
        if ((uint)prevContext >= CoeffPrevContexts) prevContext = 0;

        var node = 0;
        while (true) {
            var probabilityIndex = node >> 1;
            var coeffIndex = GetCoeffIndex(blockType, band, prevContext, probabilityIndex);
            var probability = probabilities[coeffIndex];

            var left = CoeffTokenTree[node];
            var right = CoeffTokenTree[node + 1];

            if (ContainsToken(left, token)) {
                encoder.WriteBool(probability, false);
                if (left <= 0) return;
                node = left;
            } else {
                encoder.WriteBool(probability, true);
                if (right <= 0) return;
                node = right;
            }
        }
    }

    private static int GetTokenForMagnitude(int magnitude, out int extraBits) {
        extraBits = 0;
        if (magnitude <= 1) return 2;
        if (magnitude == 2) return 3;
        if (magnitude == 3) return 4;
        if (magnitude == 4) return 5;

        for (var token = 6; token < CoeffTokenBaseMagnitude.Length; token++) {
            var baseMagnitude = CoeffTokenBaseMagnitude[token];
            var bitCount = CoeffTokenExtraBits[token];
            var max = baseMagnitude + ((1 << bitCount) - 1);
            if (magnitude <= max) {
                extraBits = magnitude - baseMagnitude;
                return token;
            }
        }

        extraBits = 0;
        return 11;
    }

    private static int GetPrevContextAfter(int token) {
        return token switch {
            0 or 1 => 0,
            2 => 1,
            _ => 2,
        };
    }

    private static bool HasNonZeroAfter(int[] coefficientsNatural, int zigZagStartIndex) {
        for (var i = zigZagStartIndex; i < CoefficientsPerBlock; i++) {
            var naturalIndex = ZigZagToNaturalOrder[i];
            if (coefficientsNatural[naturalIndex] != 0) return true;
        }
        return false;
    }

    private static bool ContainsToken(int nodeValue, int token) {
        if (nodeValue <= 0) {
            return -nodeValue - 1 == token;
        }

        var left = CoeffTokenTree[nodeValue];
        var right = CoeffTokenTree[nodeValue + 1];
        return ContainsToken(left, token) || ContainsToken(right, token);
    }

    private static void WriteControlHeader(WebpVp8BoolEncoder writer) {
        writer.WriteBool(128, false); // color space
        writer.WriteBool(128, false); // clamp type
    }

    private static void WriteSegmentation(
        WebpVp8BoolEncoder writer,
        bool enabled,
        int[] quantizerDeltas,
        int[] segmentProbabilities) {
        writer.WriteBool(128, enabled);
        if (!enabled) return;

        writer.WriteBool(128, true); // update map
        writer.WriteBool(128, true); // update data
        writer.WriteBool(128, false); // absolute deltas disabled

        for (var i = 0; i < SegmentCount; i++) {
            writer.WriteBool(128, true); // update quantizer for segment
            var delta = (quantizerDeltas != null && i < quantizerDeltas.Length) ? quantizerDeltas[i] : 0;
            WriteSignedLiteral(writer, ClampSegmentDelta(delta), 7);
        }

        for (var i = 0; i < SegmentCount; i++) {
            writer.WriteBool(128, false); // no filter delta updates
        }

        for (var i = 0; i < SegmentProbCount; i++) {
            writer.WriteBool(128, true);
            var prob = (segmentProbabilities != null && i < segmentProbabilities.Length)
                ? NormalizeSegmentProbability(segmentProbabilities[i])
                : 128;
            writer.WriteLiteral(prob, 8);
        }
    }

    private static void WriteSegmentId(WebpVp8BoolEncoder writer, int[] probabilities, int segmentId) {
        if ((uint)segmentId >= SegmentCount) segmentId = 0;
        var prob0 = NormalizeSegmentProbability((probabilities != null && probabilities.Length > 0) ? probabilities[0] : 128);
        if (segmentId == 0) {
            writer.WriteBool(prob0, false);
            return;
        }

        writer.WriteBool(prob0, true);
        var prob1 = NormalizeSegmentProbability((probabilities != null && probabilities.Length > 1) ? probabilities[1] : 128);
        if (segmentId == 1) {
            writer.WriteBool(prob1, false);
            return;
        }

        writer.WriteBool(prob1, true);
        var prob2 = NormalizeSegmentProbability((probabilities != null && probabilities.Length > 2) ? probabilities[2] : 128);
        var bit2 = segmentId == 3;
        writer.WriteBool(prob2, bit2);
    }

    private static int NormalizeSegmentProbability(int probability) {
        if (probability < 0 || probability > 255) return 128;
        return probability;
    }

    private static void WriteSignedLiteral(WebpVp8BoolEncoder writer, int value, int bits) {
        if (value < 0) {
            writer.WriteLiteral(-value, bits);
            writer.WriteBool(128, true);
        } else {
            writer.WriteLiteral(value, bits);
            writer.WriteBool(128, false);
        }
    }

    private static void WriteLoopFilter(WebpVp8BoolEncoder writer, int quality) {
        var level = (100 - quality) * 63 / 100;
        if (level < 0) level = 0;
        if (level > 63) level = 63;

        var sharpness = quality >= 75 ? 4 : 0;
        if (sharpness > 7) sharpness = 7;

        writer.WriteBool(128, false); // filter type (normal)
        writer.WriteLiteral(level, 6);
        writer.WriteLiteral(sharpness, 3);
        writer.WriteBool(128, false); // delta enabled
    }

    private static void WriteQuantization(WebpVp8BoolEncoder writer, int baseQIndex) {
        writer.WriteLiteral(baseQIndex, 7);
        for (var i = 0; i < 5; i++) {
            writer.WriteBool(128, false); // no delta updates
        }
    }

    private static void WriteCoefficientProbabilityUpdates(WebpVp8BoolEncoder writer) {
        var totalCount = CoeffBlockTypes * CoeffBands * CoeffPrevContexts * CoeffEntropyNodes;
        for (var i = 0; i < totalCount; i++) {
            var prob = WebpVp8Tables.CoeffUpdateProbs[i];
            writer.WriteBool(prob, false);
        }
    }

    private static void WriteKeyframeYMode(WebpVp8BoolEncoder writer, int mode) {
        WriteTree(writer, WebpVp8Tables.KeyframeYModeTree, WebpVp8Tables.KeyframeYModeProbs, mode);
    }

    private static void WriteKeyframeUvMode(WebpVp8BoolEncoder writer, int mode) {
        if ((uint)mode >= IntraUvModeCount) mode = ModeDcPred;
        WriteTree(writer, WebpVp8Tables.UvModeTree, WebpVp8Tables.KeyframeUvModeProbs, mode);
    }

    private static void WriteKeyframeBMode(WebpVp8BoolEncoder writer, int aboveMode, int leftMode, int mode) {
        if ((uint)aboveMode >= Intra4x4ModeCount) aboveMode = BModeDcPred;
        if ((uint)leftMode >= Intra4x4ModeCount) leftMode = BModeDcPred;
        if ((uint)mode >= Intra4x4ModeCount) mode = BModeDcPred;

        var node = 0;
        while (true) {
            var probIndex = node >> 1;
            var prob = WebpVp8Tables.KeyframeBModeProbs[((aboveMode * Intra4x4ModeCount) + leftMode) * 9 + probIndex];
            var left = WebpVp8Tables.BModeTree[node];
            var right = WebpVp8Tables.BModeTree[node + 1];

            if (ContainsTreeValue(WebpVp8Tables.BModeTree, left, mode)) {
                writer.WriteBool(prob, false);
                if (left <= 0) return;
                node = left;
            } else {
                writer.WriteBool(prob, true);
                if (right <= 0) return;
                node = right;
            }
        }
    }

    private static void WriteTree(WebpVp8BoolEncoder writer, ReadOnlySpan<int> tree, ReadOnlySpan<byte> probs, int value) {
        var node = 0;
        while (true) {
            var probIndex = node >> 1;
            if ((uint)probIndex >= (uint)probs.Length) return;
            var left = tree[node];
            var right = tree[node + 1];

            if (ContainsTreeValue(tree, left, value)) {
                writer.WriteBool(probs[probIndex], false);
                if (left <= 0) return;
                node = left;
            } else {
                writer.WriteBool(probs[probIndex], true);
                if (right <= 0) return;
                node = right;
            }
        }
    }

    private static bool ContainsTreeValue(ReadOnlySpan<int> tree, int nodeValue, int value) {
        if (nodeValue <= 0) {
            return -nodeValue == value;
        }
        var left = tree[nodeValue];
        var right = tree[nodeValue + 1];
        return ContainsTreeValue(tree, left, value) || ContainsTreeValue(tree, right, value);
    }

    private static int[] BuildDefaultProbabilities() {
        var totalCount = CoeffBlockTypes * CoeffBands * CoeffPrevContexts * CoeffEntropyNodes;
        var probs = new int[totalCount];
        for (var i = 0; i < totalCount; i++) {
            probs[i] = WebpVp8Tables.DefaultCoeffProbs[i];
        }
        return probs;
    }

    private static void ConvertRgbaToYuv420(
        ReadOnlySpan<byte> rgba,
        int width,
        int height,
        int stride,
        out byte[] yPlane,
        out byte[] uPlane,
        out byte[] vPlane) {
        var chromaWidth = (width + 1) >> 1;
        var chromaHeight = (height + 1) >> 1;

        yPlane = new byte[checked(width * height)];
        uPlane = new byte[checked(chromaWidth * chromaHeight)];
        vPlane = new byte[checked(chromaWidth * chromaHeight)];

        var sumU = new int[chromaWidth * chromaHeight];
        var sumV = new int[chromaWidth * chromaHeight];
        var counts = new int[chromaWidth * chromaHeight];

        for (var y = 0; y < height; y++) {
            var srcOffset = y * stride;
            var dstOffset = y * width;
            for (var x = 0; x < width; x++) {
                var r = rgba[srcOffset];
                var g = rgba[srcOffset + 1];
                var b = rgba[srcOffset + 2];

                var yVal = (77 * r + 150 * g + 29 * b + 128) >> 8;
                yPlane[dstOffset + x] = ClampToByte(yVal);

                var uVal = 128 + ((-43 * r - 85 * g + 128 * b + 128) >> 8);
                var vVal = 128 + ((128 * r - 107 * g - 21 * b + 128) >> 8);

                var chromaIndex = (y >> 1) * chromaWidth + (x >> 1);
                sumU[chromaIndex] += uVal;
                sumV[chromaIndex] += vVal;
                counts[chromaIndex]++;

                srcOffset += 4;
            }
        }

        for (var i = 0; i < uPlane.Length; i++) {
            var count = counts[i];
            if (count <= 0) {
                uPlane[i] = 128;
                vPlane[i] = 128;
                continue;
            }

            uPlane[i] = ClampToByte((sumU[i] + (count >> 1)) / count);
            vPlane[i] = ClampToByte((sumV[i] + (count >> 1)) / count);
        }
    }

    private static int ChooseBestBlockMode(
        byte[] source,
        byte[] recon,
        int planeWidth,
        int planeHeight,
        int dstX,
        int dstY,
        out int bestCost) {
        var bestMode = ModeDcPred;
        bestCost = int.MaxValue;
        Span<byte> predicted = stackalloc byte[BlockSize * BlockSize];

        for (var mode = 0; mode < Intra4x4ModeCount; mode++) {
            PredictBlock(recon, planeWidth, planeHeight, dstX, dstY, mode, predicted);
            var cost = ComputePredictionCost(source, planeWidth, planeHeight, dstX, dstY, predicted);
            if (cost < bestCost) {
                bestCost = cost;
                bestMode = mode;
            }
        }

        return bestMode;
    }

    private static int ChooseBestUvMode(
        byte[] uPlane,
        byte[] vPlane,
        byte[] reconU,
        byte[] reconV,
        int chromaWidth,
        int chromaHeight,
        int chromaOffsetX,
        int chromaOffsetY) {
        var bestMode = ModeDcPred;
        var bestCost = long.MaxValue;
        Span<byte> predicted = stackalloc byte[BlockSize * BlockSize];

        for (var mode = 0; mode <= 3; mode++) {
            long cost = 0;
            for (var blockIndex = 0; blockIndex < MacroblockChromaBlocks; blockIndex++) {
                var subX = blockIndex & 1;
                var subY = blockIndex >> 1;
                var dstX = chromaOffsetX + (subX * BlockSize);
                var dstY = chromaOffsetY + (subY * BlockSize);

                PredictBlock(reconU, chromaWidth, chromaHeight, dstX, dstY, mode, predicted);
                cost += ComputePredictionCost(uPlane, chromaWidth, chromaHeight, dstX, dstY, predicted);

                PredictBlock(reconV, chromaWidth, chromaHeight, dstX, dstY, mode, predicted);
                cost += ComputePredictionCost(vPlane, chromaWidth, chromaHeight, dstX, dstY, predicted);
            }

            if (cost < bestCost) {
                bestCost = cost;
                bestMode = mode;
            }
        }

        return bestMode;
    }

    private static int ComputePredictionCost(
        byte[] source,
        int planeWidth,
        int planeHeight,
        int dstX,
        int dstY,
        ReadOnlySpan<byte> predicted) {
        var sum = 0;
        for (var y = 0; y < BlockSize; y++) {
            var py = dstY + y;
            if ((uint)py >= (uint)planeHeight) continue;
            var rowOffset = py * planeWidth;
            var predRow = y * BlockSize;
            for (var x = 0; x < BlockSize; x++) {
                var px = dstX + x;
                if ((uint)px >= (uint)planeWidth) continue;
                var diff = source[rowOffset + px] - predicted[predRow + x];
                if (diff < 0) diff = -diff;
                sum += diff;
            }
        }

        return sum;
    }

    private static void PredictBlock(
        byte[] plane,
        int planeWidth,
        int planeHeight,
        int dstX,
        int dstY,
        int mode,
        Span<byte> predicted) {
        Span<byte> top = stackalloc byte[BlockSize];
        Span<byte> left = stackalloc byte[BlockSize];

        var hasTop = dstY > 0;
        var hasLeft = dstX > 0;

        for (var i = 0; i < BlockSize; i++) {
            top[i] = GetPlaneSampleOrDefault(plane, planeWidth, planeHeight, dstX + i, dstY - 1, 128);
            left[i] = GetPlaneSampleOrDefault(plane, planeWidth, planeHeight, dstX - 1, dstY + i, 128);
        }

        var topLeft = GetPlaneSampleOrDefault(plane, planeWidth, planeHeight, dstX - 1, dstY - 1, 128);
        var predictionKind = mode;
        if (predictionKind < 0 || predictionKind >= Intra4x4ModeCount) predictionKind = ModeDcPred;

        var dc = 128;
        if (predictionKind == ModeDcPred) {
            var sum = 0;
            var count = 0;
            if (hasTop) {
                sum += top[0] + top[1] + top[2] + top[3];
                count += BlockSize;
            }
            if (hasLeft) {
                sum += left[0] + left[1] + left[2] + left[3];
                count += BlockSize;
            }
            if (count > 0) {
                dc = (sum + (count >> 1)) / count;
            }
        }

        Span<byte> topExt = stackalloc byte[BlockSize * 2];
        Span<byte> leftExt = stackalloc byte[BlockSize * 2];
        for (var i = 0; i < topExt.Length; i++) {
            topExt[i] = i < BlockSize ? top[i] : top[BlockSize - 1];
            leftExt[i] = i < BlockSize ? left[i] : left[BlockSize - 1];
        }

        for (var y = 0; y < BlockSize; y++) {
            var rowOffset = y * BlockSize;
            for (var x = 0; x < BlockSize; x++) {
                byte predictedSample;
                if (predictionKind <= 3) {
                    predictedSample = predictionKind switch {
                        1 => top[x],
                        2 => left[y],
                        3 => ClampToByte(left[y] + top[x] - topLeft),
                        _ => (byte)dc,
                    };
                } else {
                    predictedSample = predictionKind switch {
                        4 => PredictDownRight(topExt, leftExt, topLeft, x, y),
                        5 => PredictVerticalRight(topExt, topLeft, x, y),
                        6 => PredictDownLeft(topExt, x, y),
                        7 => PredictVerticalLeft(topExt, x, y),
                        8 => PredictHorizontalDown(leftExt, topLeft, x, y),
                        9 => PredictHorizontalUp(leftExt, x, y),
                        _ => (byte)dc,
                    };
                }
                predicted[rowOffset + x] = predictedSample;
            }
        }
    }

    private static void FillResidual(
        byte[] source,
        int planeWidth,
        int planeHeight,
        int dstX,
        int dstY,
        ReadOnlySpan<byte> predicted,
        Span<int> residual) {
        residual.Clear();
        for (var y = 0; y < BlockSize; y++) {
            var py = dstY + y;
            if ((uint)py >= (uint)planeHeight) continue;
            var rowOffset = py * planeWidth;
            var predRow = y * BlockSize;
            var resRow = y * BlockSize;
            for (var x = 0; x < BlockSize; x++) {
                var px = dstX + x;
                if ((uint)px >= (uint)planeWidth) continue;
                residual[resRow + x] = source[rowOffset + px] - predicted[predRow + x];
            }
        }
    }

    private static void UpdateReconstruction(
        byte[] recon,
        int planeWidth,
        int planeHeight,
        int dstX,
        int dstY,
        ReadOnlySpan<byte> predicted,
        ReadOnlySpan<int> residual) {
        for (var y = 0; y < BlockSize; y++) {
            var py = dstY + y;
            if ((uint)py >= (uint)planeHeight) continue;
            var rowOffset = py * planeWidth;
            var predRow = y * BlockSize;
            var resRow = y * BlockSize;
            for (var x = 0; x < BlockSize; x++) {
                var px = dstX + x;
                if ((uint)px >= (uint)planeWidth) continue;
                recon[rowOffset + px] = ClampToByte(predicted[predRow + x] + residual[resRow + x]);
            }
        }
    }

    private static void ComputeCoefficients(ReadOnlySpan<int> residual, Span<double> coefficients) {
        for (var i = 0; i < CoefficientsPerBlock; i++) {
            var sum = 0.0;
            for (var j = 0; j < CoefficientsPerBlock; j++) {
                sum += ForwardTransform[i, j] * residual[j];
            }
            coefficients[i] = sum;
        }
    }

    private static void ComputeWalshCoefficients(ReadOnlySpan<double> dcValues, Span<double> coefficients) {
        for (var i = 0; i < CoefficientsPerBlock; i++) {
            var sum = 0.0;
            for (var j = 0; j < CoefficientsPerBlock; j++) {
                sum += ForwardWalshTransform[i, j] * dcValues[j];
            }
            coefficients[i] = sum;
        }
    }

    private static int QuantizeDouble(double value, int dequant) {
        if (dequant <= 0) return 0;
        var scaled = value / dequant;
        if (scaled >= 0) return (int)Math.Floor(scaled + 0.5);
        return (int)Math.Ceiling(scaled - 0.5);
    }

    private static int ClampCoefficient(int value) {
        if (value < -MaxCoefficientMagnitude) return -MaxCoefficientMagnitude;
        if (value > MaxCoefficientMagnitude) return MaxCoefficientMagnitude;
        return value;
    }

    private static byte PredictDownRight(ReadOnlySpan<byte> top, ReadOnlySpan<byte> left, byte topLeft, int x, int y) {
        if (x == y) return topLeft;
        if (x > y) {
            var index = x - y - 1;
            return GetExtendedSample(top, index, topLeft);
        }

        var leftIndex = y - x - 1;
        return GetExtendedSample(left, leftIndex, topLeft);
    }

    private static byte PredictVerticalRight(ReadOnlySpan<byte> top, byte topLeft, int x, int y) {
        var shift = y >> 1;
        if ((y & 1) == 0) {
            var a = GetExtendedSample(top, x - shift - 1, topLeft);
            var b = GetExtendedSample(top, x - shift, topLeft);
            return (byte)((a + b + 1) >> 1);
        }

        var a0 = GetExtendedSample(top, x - shift - 2, topLeft);
        var a1 = GetExtendedSample(top, x - shift - 1, topLeft);
        var a2 = GetExtendedSample(top, x - shift, topLeft);
        return (byte)((a0 + (2 * a1) + a2 + 2) >> 2);
    }

    private static byte PredictDownLeft(ReadOnlySpan<byte> top, int x, int y) {
        var shift = y >> 1;
        var baseIndex = x + shift + 1;
        if ((y & 1) == 0) {
            var a = GetExtendedSample(top, baseIndex, top[0]);
            var b = GetExtendedSample(top, baseIndex + 1, top[0]);
            return (byte)((a + b + 1) >> 1);
        }

        var a0 = GetExtendedSample(top, baseIndex, top[0]);
        var a1 = GetExtendedSample(top, baseIndex + 1, top[0]);
        var a2 = GetExtendedSample(top, baseIndex + 2, top[0]);
        return (byte)((a0 + (2 * a1) + a2 + 2) >> 2);
    }

    private static byte PredictVerticalLeft(ReadOnlySpan<byte> top, int x, int y) {
        var shift = y >> 1;
        var baseIndex = x + shift;
        if ((y & 1) == 0) {
            var a = GetExtendedSample(top, baseIndex, top[0]);
            var b = GetExtendedSample(top, baseIndex + 1, top[0]);
            return (byte)((a + b + 1) >> 1);
        }

        var a0 = GetExtendedSample(top, baseIndex, top[0]);
        var a1 = GetExtendedSample(top, baseIndex + 1, top[0]);
        var a2 = GetExtendedSample(top, baseIndex + 2, top[0]);
        return (byte)((a0 + (2 * a1) + a2 + 2) >> 2);
    }

    private static byte PredictHorizontalDown(ReadOnlySpan<byte> left, byte topLeft, int x, int y) {
        var shift = x >> 1;
        if ((x & 1) == 0) {
            var a = GetExtendedSample(left, y + shift - 1, topLeft);
            var b = GetExtendedSample(left, y + shift, topLeft);
            return (byte)((a + b + 1) >> 1);
        }

        var a0 = GetExtendedSample(left, y + shift - 2, topLeft);
        var a1 = GetExtendedSample(left, y + shift - 1, topLeft);
        var a2 = GetExtendedSample(left, y + shift, topLeft);
        return (byte)((a0 + (2 * a1) + a2 + 2) >> 2);
    }

    private static byte PredictHorizontalUp(ReadOnlySpan<byte> left, int x, int y) {
        var shift = x >> 1;
        var baseIndex = y + shift + 1;
        if ((x & 1) == 0) {
            var a = GetExtendedSample(left, baseIndex, left[0]);
            var b = GetExtendedSample(left, baseIndex + 1, left[0]);
            return (byte)((a + b + 1) >> 1);
        }

        var a0 = GetExtendedSample(left, baseIndex, left[0]);
        var a1 = GetExtendedSample(left, baseIndex + 1, left[0]);
        var a2 = GetExtendedSample(left, baseIndex + 2, left[0]);
        return (byte)((a0 + (2 * a1) + a2 + 2) >> 2);
    }

    private static int[] InverseTransform4x4(int[] input) {
        var output = new int[CoefficientsPerBlock];
        var temp = new int[CoefficientsPerBlock];

        for (var i = 0; i < BlockSize; i++) {
            var ip0 = input[i];
            var ip4 = input[i + 4];
            var ip8 = input[i + 8];
            var ip12 = input[i + 12];

            var a1 = ip0 + ip8;
            var b1 = ip0 - ip8;
            var temp1 = (ip4 * IdctSinpi8Sqrt2) >> 16;
            var temp2 = ip12 + ((ip12 * IdctCospi8Sqrt2Minus1) >> 16);
            var c1 = temp1 - temp2;
            temp1 = ip4 + ((ip4 * IdctCospi8Sqrt2Minus1) >> 16);
            temp2 = (ip12 * IdctSinpi8Sqrt2) >> 16;
            var d1 = temp1 + temp2;

            temp[i] = a1 + d1;
            temp[i + 12] = a1 - d1;
            temp[i + 4] = b1 + c1;
            temp[i + 8] = b1 - c1;
        }

        for (var i = 0; i < BlockSize; i++) {
            var baseIndex = i * BlockSize;
            var t0 = temp[baseIndex];
            var t1 = temp[baseIndex + 1];
            var t2 = temp[baseIndex + 2];
            var t3 = temp[baseIndex + 3];

            var a1 = t0 + t2;
            var b1 = t0 - t2;
            var temp1 = (t1 * IdctSinpi8Sqrt2) >> 16;
            var temp2 = t3 + ((t3 * IdctCospi8Sqrt2Minus1) >> 16);
            var c1 = temp1 - temp2;
            temp1 = t1 + ((t1 * IdctCospi8Sqrt2Minus1) >> 16);
            temp2 = (t3 * IdctSinpi8Sqrt2) >> 16;
            var d1 = temp1 + temp2;

            output[baseIndex] = (a1 + d1 + 4) >> 3;
            output[baseIndex + 3] = (a1 - d1 + 4) >> 3;
            output[baseIndex + 1] = (b1 + c1 + 4) >> 3;
            output[baseIndex + 2] = (b1 - c1 + 4) >> 3;
        }

        return output;
    }

    private static int[] InverseWalshTransform4x4(int[] input) {
        var temp = new int[CoefficientsPerBlock];
        var output = new int[CoefficientsPerBlock];

        for (var i = 0; i < BlockSize; i++) {
            var ip0 = input[i];
            var ip4 = input[i + 4];
            var ip8 = input[i + 8];
            var ip12 = input[i + 12];

            var a1 = ip0 + ip12;
            var b1 = ip4 + ip8;
            var c1 = ip4 - ip8;
            var d1 = ip0 - ip12;

            temp[i] = a1 + b1;
            temp[i + 4] = c1 + d1;
            temp[i + 8] = a1 - b1;
            temp[i + 12] = d1 - c1;
        }

        for (var i = 0; i < BlockSize; i++) {
            var baseIndex = i * BlockSize;
            var t0 = temp[baseIndex];
            var t1 = temp[baseIndex + 1];
            var t2 = temp[baseIndex + 2];
            var t3 = temp[baseIndex + 3];

            var a1 = t0 + t3;
            var b1 = t1 + t2;
            var c1 = t1 - t2;
            var d1 = t0 - t3;

            var a2 = a1 + b1;
            var b2 = c1 + d1;
            var c2 = a1 - b1;
            var d2 = d1 - c1;

            output[baseIndex] = (a2 + 3) >> 3;
            output[baseIndex + 1] = (b2 + 3) >> 3;
            output[baseIndex + 2] = (c2 + 3) >> 3;
            output[baseIndex + 3] = (d2 + 3) >> 3;
        }

        return output;
    }

    private static int QualityToBaseQIndex(int quality) {
        if (quality <= 0) return 127;
        if (quality >= 100) return 0;
        return (100 - quality) * 127 / 100;
    }

    private static byte[] BuildSegmentMap(
        byte[] yPlane,
        int width,
        int height,
        int macroblockCols,
        int macroblockRows,
        int baseQIndex,
        out bool segmentationEnabled,
        out int[] quantizerDeltas,
        out int[] segmentProbabilities) {
        segmentationEnabled = false;
        quantizerDeltas = new int[SegmentCount];
        segmentProbabilities = new int[SegmentProbCount] { 128, 128, 128 };

        var macroblockCountLong = (long)macroblockCols * macroblockRows;
        if (macroblockCountLong <= 0 || macroblockCountLong > int.MaxValue) {
            return Array.Empty<byte>();
        }

        var macroblockCount = (int)macroblockCountLong;
        var segmentIds = new byte[macroblockCount];

        if (macroblockCount <= 1 || baseQIndex < 12) {
            return segmentIds;
        }

        var variances = new int[macroblockCount];
        for (var mbY = 0; mbY < macroblockRows; mbY++) {
            for (var mbX = 0; mbX < macroblockCols; mbX++) {
                var index = (mbY * macroblockCols) + mbX;
                var startX = mbX * MacroblockSize;
                var startY = mbY * MacroblockSize;

                long sum = 0;
                long sumSq = 0;
                var count = 0;

                for (var y = 0; y < MacroblockSize; y++) {
                    var srcY = startY + y;
                    if (srcY >= height) break;
                    var rowOffset = srcY * width;
                    for (var x = 0; x < MacroblockSize; x++) {
                        var srcX = startX + x;
                        if (srcX >= width) break;
                        var sample = yPlane[rowOffset + srcX];
                        sum += sample;
                        sumSq += (long)sample * sample;
                        count++;
                    }
                }

                if (count <= 0) {
                    variances[index] = 0;
                    continue;
                }

                var numerator = (sumSq * count) - (sum * sum);
                if (numerator < 0) numerator = 0;
                variances[index] = (int)(numerator / (count * (long)count));
            }
        }

        var sorted = (int[])variances.Clone();
        Array.Sort(sorted);
        var p25 = sorted[(macroblockCount * 1) / 4];
        var p50 = sorted[(macroblockCount * 2) / 4];
        var p75 = sorted[(macroblockCount * 3) / 4];

        if (p75 - p25 < 8) {
            return segmentIds;
        }

        var counts = new int[SegmentCount];
        for (var i = 0; i < variances.Length; i++) {
            var variance = variances[i];
            var segmentId = variance <= p25 ? 0
                : variance <= p50 ? 1
                : variance <= p75 ? 2
                : 3;
            segmentIds[i] = (byte)segmentId;
            counts[segmentId]++;
        }

        var usedSegments = 0;
        for (var i = 0; i < counts.Length; i++) {
            if (counts[i] > 0) usedSegments++;
        }

        if (usedSegments < 2) {
            return segmentIds;
        }

        var step = baseQIndex / 16;
        if (step < 2) step = 2;
        if (step > 12) step = 12;

        quantizerDeltas[0] = ClampSegmentDelta(step * 2);
        quantizerDeltas[1] = ClampSegmentDelta(step);
        quantizerDeltas[2] = 0;
        quantizerDeltas[3] = ClampSegmentDelta(-step);

        segmentProbabilities = BuildSegmentProbabilities(counts);
        segmentationEnabled = true;
        return segmentIds;
    }

    private static int[] BuildSegmentProbabilities(int[] counts) {
        var probabilities = new int[SegmentProbCount] { 128, 128, 128 };
        if (counts == null || counts.Length < SegmentCount) return probabilities;

        var total = 0;
        for (var i = 0; i < SegmentCount; i++) {
            total += counts[i];
        }

        if (total <= 0) return probabilities;

        probabilities[0] = ComputeProbability(counts[0], total);
        var remaining = total - counts[0];
        probabilities[1] = remaining > 0 ? ComputeProbability(counts[1], remaining) : 128;
        var remaining2 = remaining - counts[1];
        probabilities[2] = remaining2 > 0 ? ComputeProbability(counts[2], remaining2) : 128;
        return probabilities;
    }

    private static int ComputeProbability(int countFalse, int total) {
        if (total <= 0) return 128;
        var numerator = (long)countFalse * 255 + (total / 2);
        var prob = (int)(numerator / total);
        if (prob < 0) return 0;
        if (prob > 255) return 255;
        return prob;
    }

    private static DequantFactors[] BuildDequantFactors(int baseQIndex, int[] quantizerDeltas, bool segmentationEnabled) {
        var factors = new DequantFactors[SegmentCount];
        if (!segmentationEnabled) {
            var baseFactors = BuildDequantFactors(baseQIndex);
            for (var i = 0; i < SegmentCount; i++) {
                factors[i] = baseFactors;
            }
            return factors;
        }

        for (var i = 0; i < SegmentCount; i++) {
            var delta = (quantizerDeltas != null && i < quantizerDeltas.Length) ? quantizerDeltas[i] : 0;
            var qIndex = ClampQIndex(baseQIndex + delta);
            factors[i] = BuildDequantFactors(qIndex);
        }

        return factors;
    }

    private static DequantFactors BuildDequantFactors(int baseQIndex) {
        var q = ClampQIndex(baseQIndex);
        var y1Dc = GetDcQuant(q);
        var y1Ac = GetAcQuant(q);
        var y2Dc = GetDcQuant(q) * 2;
        var y2Ac = (GetAcQuant(q) * 155) / 100;
        if (y2Ac < 8) y2Ac = 8;
        var uvDc = GetDcQuant(q) * 2;
        if (uvDc > 132) uvDc = 132;
        var uvAc = GetAcQuant(q);
        return new DequantFactors(y1Dc, y1Ac, y2Dc, y2Ac, uvDc, uvAc);
    }

    private static int GetDcQuant(int qIndex) {
        var clamped = ClampQIndex(qIndex);
        if ((uint)clamped >= (uint)WebpVp8Tables.DcQlookup.Length) return 0;
        return WebpVp8Tables.DcQlookup[clamped];
    }

    private static int GetAcQuant(int qIndex) {
        var clamped = ClampQIndex(qIndex);
        if ((uint)clamped >= (uint)WebpVp8Tables.AcQlookup.Length) return 0;
        return WebpVp8Tables.AcQlookup[clamped];
    }

    private static int ClampQIndex(int qIndex) {
        if (qIndex < 0) return 0;
        if (qIndex > 127) return 127;
        return qIndex;
    }

    private static int ClampSegmentDelta(int delta) {
        if (delta < -127) return -127;
        if (delta > 127) return 127;
        return delta;
    }

    private static int GetCoeffIndex(int blockType, int band, int prev, int node) {
        return (((blockType * CoeffBands) + band) * CoeffPrevContexts + prev) * CoeffEntropyNodes + node;
    }

    private static byte GetPlaneSampleOrDefault(byte[] plane, int width, int height, int x, int y, byte fallback) {
        if ((uint)x >= (uint)width || (uint)y >= (uint)height) return fallback;
        return plane[(y * width) + x];
    }

    private static byte GetExtendedSample(ReadOnlySpan<byte> samples, int index, byte fallback) {
        if (index < 0) return fallback;
        if (index >= samples.Length) return samples[samples.Length - 1];
        return samples[index];
    }

    private static int GetMacroblockDimension(int pixels) {
        if (pixels <= 0) return 0;
        return (pixels + MacroblockSize - 1) / MacroblockSize;
    }

    private static byte ClampToByte(int value) {
        if (value < byte.MinValue) return byte.MinValue;
        if (value > byte.MaxValue) return byte.MaxValue;
        return (byte)value;
    }

    private static bool ComputeAlphaUsed(ReadOnlySpan<byte> rgba, int width, int height, int stride) {
        var alphaOffset = 3;
        for (var y = 0; y < height; y++) {
            var offset = y * stride + alphaOffset;
            for (var x = 0; x < width; x++) {
                if (rgba[offset] != 255) return true;
                offset += 4;
            }
        }
        return false;
    }

    private static byte[] BuildAlphPayload(ReadOnlySpan<byte> rgba, int width, int height, int stride) {
        var alpha = new byte[checked(width * height)];
        var dst = 0;
        for (var y = 0; y < height; y++) {
            var offset = y * stride + 3;
            for (var x = 0; x < width; x++) {
                alpha[dst++] = rgba[offset];
                offset += 4;
            }
        }

        var filter = ChooseAlphaFilter(alpha, width, height);
        var payload = new byte[alpha.Length + 1];
        payload[0] = (byte)((filter & 0x3) << 2); // compression=0 (raw), filter, preprocessing=0
        if (filter == 0) {
            Buffer.BlockCopy(alpha, 0, payload, 1, alpha.Length);
            return payload;
        }

        ApplyAlphaFilterEncode(alpha, width, height, filter, payload, 1);
        return payload;
    }

    private static int ChooseAlphaFilter(byte[] alpha, int width, int height) {
        var bestFilter = 0;
        var bestCost = ComputeAlphaFilterCost(alpha, width, height, filter: 0);
        for (var filter = 1; filter <= 3; filter++) {
            var cost = ComputeAlphaFilterCost(alpha, width, height, filter);
            if (cost < bestCost) {
                bestCost = cost;
                bestFilter = filter;
            }
        }

        return bestFilter;
    }

    private static long ComputeAlphaFilterCost(byte[] alpha, int width, int height, int filter) {
        if (filter == 0) {
            long sum = 0;
            for (var i = 0; i < alpha.Length; i++) {
                sum += alpha[i];
            }
            return sum;
        }

        long cost = 0;
        for (var y = 0; y < height; y++) {
            var row = y * width;
            for (var x = 0; x < width; x++) {
                var index = row + x;
                var predictor = GetAlphaPredictor(alpha, width, x, y, filter);
                var diff = alpha[index] - predictor;
                if (diff < 0) diff = -diff;
                cost += diff;
            }
        }

        return cost;
    }

    private static void ApplyAlphaFilterEncode(byte[] alpha, int width, int height, int filter, byte[] output, int offset) {
        for (var y = 0; y < height; y++) {
            var row = y * width;
            for (var x = 0; x < width; x++) {
                var index = row + x;
                var predictor = GetAlphaPredictor(alpha, width, x, y, filter);
                output[offset + index] = unchecked((byte)(alpha[index] - predictor));
            }
        }
    }

    private static byte GetAlphaPredictor(byte[] alpha, int width, int x, int y, int filter) {
        var index = (y * width) + x;
        var left = x > 0 ? alpha[index - 1] : (byte)0;
        var up = y > 0 ? alpha[index - width] : (byte)0;
        var upLeft = (x > 0 && y > 0) ? alpha[index - width - 1] : (byte)0;

        return filter switch {
            1 => left,
            2 => up,
            3 => ClampToByte(left + up - upLeft),
            _ => (byte)0
        };
    }

    private static byte[] BuildKeyframeHeader(int width, int height) {
        var header = new byte[7];
        header[0] = 0x9D;
        header[1] = 0x01;
        header[2] = 0x2A;
        WriteU16LE(header, 3, width & 0x3FFF);
        WriteU16LE(header, 5, height & 0x3FFF);
        return header;
    }

    private static byte[] BuildFrameTag(int partitionSize, int version, bool showFrame, bool keyframe) {
        var tag = (partitionSize << 5) | ((showFrame ? 1 : 0) << 4) | (version << 1) | (keyframe ? 0 : 1);
        var bytes = new byte[3];
        bytes[0] = (byte)(tag & 0xFF);
        bytes[1] = (byte)((tag >> 8) & 0xFF);
        bytes[2] = (byte)((tag >> 16) & 0xFF);
        return bytes;
    }

    private static byte[] WriteWebpContainer(byte[] vp8Payload, byte[] alphPayload, int width, int height) {
        using var ms = new MemoryStream();
        WriteAscii(ms, "RIFF");
        WriteU32LE(ms, 0);
        WriteAscii(ms, "WEBP");

        if (alphPayload is { Length: > 0 }) {
            var vp8x = new byte[10];
            vp8x[0] = 0x02; // alpha
            WriteU24LE(vp8x, 4, width - 1);
            WriteU24LE(vp8x, 7, height - 1);
            WriteChunk(ms, "VP8X", vp8x);
            WriteChunk(ms, "ALPH", alphPayload);
        }

        WriteChunk(ms, "VP8 ", vp8Payload);

        var bytes = ms.ToArray();
        var riffSize = checked((uint)(bytes.Length - 8));
        WriteU32LE(bytes, 4, riffSize);
        return bytes;
    }

    private static void WriteChunk(Stream stream, string fourCc, ReadOnlySpan<byte> payload) {
        WriteAscii(stream, fourCc);
        WriteU32LE(stream, (uint)payload.Length);
        if (!payload.IsEmpty) {
            var buffer = new byte[payload.Length];
            payload.CopyTo(buffer);
            stream.Write(buffer, 0, buffer.Length);
        }
        if ((payload.Length & 1) != 0) {
            stream.WriteByte(0);
        }
    }

    private static void WriteAscii(Stream stream, string text) {
        var bytes = System.Text.Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static void WriteU16LE(byte[] buffer, int offset, int value) {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
    }

    private static void WriteU24LE(byte[] buffer, int offset, int value) {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
    }

    private static void WriteU32LE(Stream stream, uint value) {
        stream.WriteByte((byte)(value & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
        stream.WriteByte((byte)((value >> 16) & 0xFF));
        stream.WriteByte((byte)((value >> 24) & 0xFF));
    }

    private static void WriteU32LE(byte[] buffer, int offset, uint value) {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
        buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
    }

    private static double[,] BuildForwardTransformMatrix() {
        var inverseMatrix = new double[CoefficientsPerBlock, CoefficientsPerBlock];
        for (var j = 0; j < CoefficientsPerBlock; j++) {
            var coeffs = new int[CoefficientsPerBlock];
            coeffs[j] = 1;
            var residual = InverseTransform4x4(coeffs);
            for (var i = 0; i < CoefficientsPerBlock; i++) {
                inverseMatrix[i, j] = residual[i];
            }
        }

        return InvertMatrix(inverseMatrix);
    }

    private static double[,] BuildForwardWalshTransformMatrix() {
        var inverseMatrix = new double[CoefficientsPerBlock, CoefficientsPerBlock];
        for (var j = 0; j < CoefficientsPerBlock; j++) {
            var coeffs = new int[CoefficientsPerBlock];
            coeffs[j] = 1;
            var residual = InverseWalshTransform4x4(coeffs);
            for (var i = 0; i < CoefficientsPerBlock; i++) {
                inverseMatrix[i, j] = residual[i];
            }
        }

        return InvertMatrix(inverseMatrix);
    }

    private static double[,] InvertMatrix(double[,] matrix) {
        var size = CoefficientsPerBlock;
        var augmented = new double[size, size * 2];

        for (var row = 0; row < size; row++) {
            for (var col = 0; col < size; col++) {
                augmented[row, col] = matrix[row, col];
            }
            augmented[row, size + row] = 1.0;
        }

        for (var col = 0; col < size; col++) {
            var pivotRow = col;
            var pivot = Math.Abs(augmented[pivotRow, col]);
            for (var row = col + 1; row < size; row++) {
                var candidate = Math.Abs(augmented[row, col]);
                if (candidate > pivot) {
                    pivot = candidate;
                    pivotRow = row;
                }
            }

            if (pivot < 1e-9) {
                var identity = new double[size, size];
                for (var i = 0; i < size; i++) {
                    identity[i, i] = 1.0;
                }
                return identity;
            }

            if (pivotRow != col) {
                for (var swapCol = 0; swapCol < size * 2; swapCol++) {
                    var temp = augmented[col, swapCol];
                    augmented[col, swapCol] = augmented[pivotRow, swapCol];
                    augmented[pivotRow, swapCol] = temp;
                }
            }

            var scale = augmented[col, col];
            for (var scaleCol = 0; scaleCol < size * 2; scaleCol++) {
                augmented[col, scaleCol] /= scale;
            }

            for (var row = 0; row < size; row++) {
                if (row == col) continue;
                var factor = augmented[row, col];
                if (Math.Abs(factor) < 1e-12) continue;
                for (var reduceCol = 0; reduceCol < size * 2; reduceCol++) {
                    augmented[row, reduceCol] -= factor * augmented[col, reduceCol];
                }
            }
        }

        var inverse = new double[size, size];
        for (var row = 0; row < size; row++) {
            for (var col = 0; col < size; col++) {
                inverse[row, col] = augmented[row, size + col];
            }
        }

        return inverse;
    }

    private static void SwapRowContexts(ref byte[] above, ref byte[] current) {
        var temp = above;
        above = current;
        current = temp;
        Array.Clear(current, 0, current.Length);
    }

    private static void SwapRowModes(ref int[] above, ref int[] current) {
        var temp = above;
        above = current;
        current = temp;
        Array.Clear(current, 0, current.Length);
    }

    private static byte[] PadToLength(byte[] data, int length) {
        if (data.Length >= length) return data;
        var padded = new byte[length];
        Buffer.BlockCopy(data, 0, padded, 0, data.Length);
        return padded;
    }

    private readonly struct DequantFactors {
        public DequantFactors(int y1Dc, int y1Ac, int y2Dc, int y2Ac, int uvDc, int uvAc) {
            Y1Dc = y1Dc;
            Y1Ac = y1Ac;
            Y2Dc = y2Dc;
            Y2Ac = y2Ac;
            UvDc = uvDc;
            UvAc = uvAc;
        }

        public int Y1Dc { get; }
        public int Y1Ac { get; }
        public int Y2Dc { get; }
        public int Y2Ac { get; }
        public int UvDc { get; }
        public int UvAc { get; }
    }
}
