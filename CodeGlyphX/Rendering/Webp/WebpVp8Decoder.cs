using System;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// Managed VP8 (lossy) decoder scaffold. Currently parses only the keyframe header.
/// </summary>
internal static class WebpVp8Decoder {
    private const int FrameTagBytes = 3;
    private const int StartCodeBytes = 3;
    private const int DimensionBytes = 4;
    private const int MinimumHeaderBytes = FrameTagBytes + StartCodeBytes + DimensionBytes; // 10
    private const int KeyframeHeaderBytes = StartCodeBytes + DimensionBytes; // 7
    private const int SegmentCount = 4;
    private const int SegmentProbCount = 3;
    private const int LoopFilterDeltaCount = 4;
    private const int QuantDeltaCount = 5;
    private const int CoeffBlockTypes = 4;
    private const int CoeffBands = 8;
    private const int CoeffPrevContexts = 3;
    private const int CoeffEntropyNodes = 11;
    private const int CoefficientsPerBlock = 16;
    private const int TokenScaffoldTokensPerPartition = 8;
    private const int CoeffTokenCount = 12;
    private const int BlockScaffoldBlocksPerPartition = 4;
    private const int BlockScaffoldMaxTokensPerBlock = CoefficientsPerBlock;
    private const int IdctCospi8Sqrt2Minus1 = 20091;
    private const int IdctSinpi8Sqrt2 = 35468;
    private const int Intra16x16ModeCount = 4;
    private const int IntraUvModeCount = 4;
    private const int Intra4x4ModeCount = 10;
    private const int YModeBPred = 4;
    private const int ModeDcPred = 0;
    private const int ModeVPred = 1;
    private const int ModeHPred = 2;
    private const int ModeTMPred = 3;
    private const int BModeDcPred = 0;
    private const int BModeTMPred = 1;
    private const int BModeVEPred = 2;
    private const int BModeHEPred = 3;
    private const int BModeRDPred = 4;
    private const int BModeVRPred = 5;
    private const int BModeLDPred = 6;
    private const int BModeVLPred = 7;
    private const int BModeHDPred = 8;
    private const int BModeHUPred = 9;
    private const int MacroblockSubBlockCount = 16;
    private const int MacroblockSize = 16;
    private const int BlockSize = 4;
    private const int MacroblockScaffoldWidth = MacroblockSize;
    private const int MacroblockScaffoldHeight = MacroblockSize;
    private const int MacroblockScaffoldSourceWidth = 16;
    private const int MacroblockScaffoldSourceHeight = 16;
    private const int MacroblockScaffoldMaxBlocks = MacroblockScaffoldBlocksPerMacroblock;
    private const int MacroblockScaffoldChromaWidth = MacroblockSize / 2;
    private const int MacroblockScaffoldChromaHeight = MacroblockSize / 2;
    private const int MacroblockScaffoldSourceChromaWidth = 8;
    private const int MacroblockScaffoldSourceChromaHeight = 8;
    private const int MacroblockScaffoldBlocksPerMacroblock = 24;
    private const int MacroblockScaffoldBlockScale = MacroblockScaffoldBlocksPerMacroblock / BlockScaffoldBlocksPerPartition;
    private const int MacroblockScaffoldLumaBlocks = 16;
    private const int MacroblockScaffoldChromaBlocks = 4;
    private static readonly byte[] ScaffoldSignature = new byte[] { (byte)'S', (byte)'C', (byte)'F', (byte)'0' };
    private const int BlockTypeY2 = 0;
    private const int BlockTypeY = 1;
    private const int BlockTypeU = 2;
    private const int BlockTypeV = 3;
    private static readonly int[] CoeffBandTable = new[]
    {
        0, 1, 2, 3,
        6, 4, 5, 6,
        6, 6, 6, 6,
        6, 7, 7, 7,
    };
    private static readonly int[] ZigZagToNaturalOrder = new[]
    {
        0,  1,  4,  8,
        5,  2,  3,  6,
        9, 12, 13, 10,
        7, 11, 14, 15,
    };
    private static readonly int[] CoeffTokenExtraBits = new[]
    {
        0, 0, 0, 0, 0, 0,
        1, 2, 3, 4, 5, 11,
    };
    private static readonly int[] CoeffTokenBaseMagnitude = new[]
    {
        0, 0, 1, 2, 3, 4,
        5, 7, 11, 19, 35, 67,
    };
    private static readonly int[] CoeffTokenTree = new[]
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
    private static readonly int[] EmptySubblockModes = Array.Empty<int>();
    private static readonly int[] ZeroCoefficients = new int[CoefficientsPerBlock];

    internal static bool TryDecode(ReadOnlySpan<byte> payload, out byte[] rgba, out int width, out int height) {
        rgba = Array.Empty<byte>();
        width = 0;
        height = 0;

        if (!TryReadHeader(payload, out var header)) return false;
        if (!TryDecodeKeyframe(payload, header, out rgba)) return false;

        width = header.Width;
        height = header.Height;
        return true;
    }

    private static bool TryDecodeKeyframe(ReadOnlySpan<byte> payload, WebpVp8Header header, out byte[] rgba) {
        rgba = Array.Empty<byte>();

        if (!TryGetBoolCodedData(payload, out var boolData)) return false;
        var headerDecoder = new WebpVp8BoolDecoder(boolData);
        if (!TryReadFrameHeader(headerDecoder, out var frameHeader)) return false;

        if (!TryReadMacroblockHeadersKeyframe(headerDecoder, header, frameHeader, out var macroblocks)) return false;

        if (!TryReadPartitionLayout(payload, out var layout)) return false;
        var partitionCount = layout.DctPartitionSizes.Length;
        if (partitionCount <= 0) return false;
        if (macroblocks.Length == 0) return false;

        var tokenDecoders = new WebpVp8BoolDecoder[partitionCount];
        var offset = layout.DctDataOffset;
        for (var i = 0; i < partitionCount; i++) {
            var size = layout.DctPartitionSizes[i];
            if (size < 0) return false;
            if (offset < 0 || offset + size > payload.Length) return false;
            tokenDecoders[i] = new WebpVp8BoolDecoder(payload.Slice(offset, size));
            offset += size;
        }

        var width = header.Width;
        var height = header.Height;
        var yPlane = new byte[checked(width * height)];
        var chromaWidth = (width + 1) >> 1;
        var chromaHeight = (height + 1) >> 1;
        var uPlane = new byte[checked(chromaWidth * chromaHeight)];
        var vPlane = new byte[checked(chromaWidth * chromaHeight)];

        var macroblockCols = GetMacroblockDimension(width);
        var macroblockRows = GetMacroblockDimension(height);
        if (macroblockCols <= 0 || macroblockRows <= 0) return false;
        if (macroblocks.Length != macroblockCols * macroblockRows) return false;

        var dequant = BuildDequantFactors(frameHeader.Quantization, frameHeader.Segmentation);

        var y2NzAbove = new byte[macroblockCols];
        var y2NzCurrent = new byte[macroblockCols];
        var yNzAbove = new byte[macroblockCols * MacroblockSubBlockCount];
        var yNzCurrent = new byte[macroblockCols * MacroblockSubBlockCount];
        var uNzAbove = new byte[macroblockCols * MacroblockScaffoldChromaBlocks];
        var uNzCurrent = new byte[macroblockCols * MacroblockScaffoldChromaBlocks];
        var vNzAbove = new byte[macroblockCols * MacroblockScaffoldChromaBlocks];
        var vNzCurrent = new byte[macroblockCols * MacroblockScaffoldChromaBlocks];

        var macroblockHasCoefficients = new bool[macroblocks.Length];

        for (var row = 0; row < macroblockRows; row++) {
            var partitionIndex = GetTokenPartitionForRow(row, partitionCount);
            if ((uint)partitionIndex >= (uint)tokenDecoders.Length) return false;
            var tokenDecoder = tokenDecoders[partitionIndex];

            for (var col = 0; col < macroblockCols; col++) {
                var macroblockIndex = (row * macroblockCols) + col;
                var macroblock = macroblocks[macroblockIndex];
                var segmentId = macroblock.SegmentId;
                if ((uint)segmentId >= (uint)dequant.Length) segmentId = 0;

                var hasCoefficients = false;
                if (!TryDecodeMacroblock(
                    tokenDecoder,
                    frameHeader.CoefficientProbabilities,
                    macroblock,
                    dequant[segmentId],
                    col,
                    row,
                    width,
                    height,
                    yPlane,
                    uPlane,
                    vPlane,
                    chromaWidth,
                    chromaHeight,
                    y2NzAbove,
                    y2NzCurrent,
                    yNzAbove,
                    yNzCurrent,
                    uNzAbove,
                    uNzCurrent,
                    vNzAbove,
                    vNzCurrent,
                    out hasCoefficients)) {
                    return false;
                }

                macroblockHasCoefficients[macroblockIndex] = hasCoefficients;
            }

            SwapRowContexts(ref y2NzAbove, ref y2NzCurrent);
            SwapRowContexts(ref yNzAbove, ref yNzCurrent);
            SwapRowContexts(ref uNzAbove, ref uNzCurrent);
            SwapRowContexts(ref vNzAbove, ref vNzCurrent);
        }

        ApplyLoopFilter(
            frameHeader.LoopFilter,
            frameHeader.Segmentation,
            macroblocks,
            macroblockHasCoefficients,
            width,
            height,
            yPlane,
            uPlane,
            vPlane,
            chromaWidth,
            chromaHeight,
            isKeyframe: true);

        rgba = ConvertPlanesToRgba(width, height, yPlane, uPlane, vPlane, chromaWidth, chromaHeight);
        return true;
    }

    internal static bool TryReadHeader(ReadOnlySpan<byte> payload, out WebpVp8Header header) {
        header = default;
        if (payload.Length < MinimumHeaderBytes) return false;

        var frameTag = ReadU24LE(payload, 0);
        var frameType = frameTag & 1;
        if (frameType != 0) return false; // interframes are not valid for WebP stills

        var version = (frameTag >> 1) & 0x7;
        var showFrame = (frameTag >> 4) & 1;
        var partitionSize = frameTag >> 5;
        if (partitionSize < KeyframeHeaderBytes) return false;

        // Keyframe start code.
        if (payload[3] != 0x9D || payload[4] != 0x01 || payload[5] != 0x2A) return false;

        var widthRaw = ReadU16LE(payload, 6);
        var heightRaw = ReadU16LE(payload, 8);
        var width = widthRaw & 0x3FFF;
        var height = heightRaw & 0x3FFF;
        var horizontalScale = (widthRaw >> 14) & 0x3;
        var verticalScale = (heightRaw >> 14) & 0x3;
        if (width <= 0 || height <= 0) return false;

        header = new WebpVp8Header(
            width,
            height,
            version,
            showFrame != 0,
            partitionSize,
            horizontalScale,
            verticalScale,
            bitsConsumed: MinimumHeaderBytes * 8);
        return true;
    }

    internal static bool TryGetFirstPartition(ReadOnlySpan<byte> payload, out ReadOnlySpan<byte> firstPartition) {
        firstPartition = default;
        if (!TryReadHeader(payload, out var header)) return false;

        var offset = FrameTagBytes;
        var length = header.PartitionSize;
        if (offset < 0 || length < 0) return false;
        if (offset + length > payload.Length) return false;

        firstPartition = payload.Slice(offset, length);
        return true;
    }

    internal static bool TryGetBoolCodedData(ReadOnlySpan<byte> payload, out ReadOnlySpan<byte> boolData) {
        boolData = default;
        if (!TryGetFirstPartition(payload, out var firstPartition)) return false;
        if (firstPartition.Length < KeyframeHeaderBytes) return false;

        boolData = firstPartition.Slice(KeyframeHeaderBytes);
        return true;
    }

    internal static bool TryReadControlHeader(ReadOnlySpan<byte> payload, out WebpVp8ControlHeader controlHeader) {
        controlHeader = default;
        if (!TryGetBoolCodedData(payload, out var boolData)) return false;
        if (boolData.Length < 2) return false;

        var decoder = new WebpVp8BoolDecoder(boolData);
        return TryReadControlHeader(decoder, out controlHeader);
    }

    internal static bool TryReadFrameHeader(ReadOnlySpan<byte> payload, out WebpVp8FrameHeader frameHeader) {
        frameHeader = default;
        if (!TryGetBoolCodedData(payload, out var boolData)) return false;
        if (boolData.Length < 2) return false;

        var decoder = new WebpVp8BoolDecoder(boolData);
        return TryReadFrameHeader(decoder, out frameHeader);
    }

    private static bool TryReadFrameHeader(WebpVp8BoolDecoder decoder, out WebpVp8FrameHeader frameHeader) {
        frameHeader = default;
        if (!TryReadControlHeader(decoder, out var controlHeader)) return false;
        if (!TryReadSegmentation(decoder, out var segmentation)) return false;
        if (!TryReadLoopFilter(decoder, out var loopFilter)) return false;

        if (!decoder.TryReadLiteral(2, out var partitionBits)) return false;
        if (partitionBits < 0 || partitionBits > 3) return false;
        var dctPartitionCount = 1 << partitionBits;

        if (!TryReadQuantization(decoder, out var quantization)) return false;
        if (!TryReadCoefficientProbabilities(decoder, out var coefficientProbabilities)) return false;
        if (!decoder.TryReadBool(128, out var refreshEntropyProbsBit)) return false;
        if (!decoder.TryReadBool(128, out var noCoefficientSkipBit)) return false;

        var skipProbability = 0;
        if (noCoefficientSkipBit) {
            if (!decoder.TryReadLiteral(8, out skipProbability)) return false;
        }

        frameHeader = new WebpVp8FrameHeader(
            controlHeader,
            segmentation,
            loopFilter,
            quantization,
            coefficientProbabilities,
            dctPartitionCount,
            refreshEntropyProbsBit,
            noCoefficientSkipBit,
            skipProbability,
            decoder.BytesConsumed);
        return true;
    }

    private static bool TryReadMacroblockHeadersKeyframe(
        WebpVp8BoolDecoder decoder,
        WebpVp8Header header,
        WebpVp8FrameHeader frameHeader,
        out WebpVp8MacroblockHeaderScaffold[] macroblocks) {
        macroblocks = Array.Empty<WebpVp8MacroblockHeaderScaffold>();

        var macroblockCols = GetMacroblockDimension(header.Width);
        var macroblockRows = GetMacroblockDimension(header.Height);
        if (macroblockCols <= 0 || macroblockRows <= 0) return false;

        var macroblockCountLong = (long)macroblockCols * macroblockRows;
        if (macroblockCountLong <= 0 || macroblockCountLong > int.MaxValue) return false;
        var macroblockCount = (int)macroblockCountLong;

        macroblocks = new WebpVp8MacroblockHeaderScaffold[macroblockCount];

        var aboveSubModes = new int[macroblockCols * MacroblockSubBlockCount];
        var currentSubModes = new int[macroblockCols * MacroblockSubBlockCount];

        for (var row = 0; row < macroblockRows; row++) {
            for (var col = 0; col < macroblockCols; col++) {
                var index = (row * macroblockCols) + col;

                var segmentId = 0;
                if (frameHeader.Segmentation.Enabled && frameHeader.Segmentation.UpdateMap) {
                    if (!TryReadSegmentId(decoder, frameHeader.Segmentation.SegmentProbabilities, out segmentId)) {
                        return false;
                    }
                }

                var skipCoefficients = false;
                if (frameHeader.NoCoefficientSkip) {
                    var probability = frameHeader.SkipProbability;
                    if ((uint)probability > 255) return false;
                    if (!decoder.TryReadBool(probability, out skipCoefficients)) return false;
                }

                if (!TryReadKeyframeYMode(decoder, out var yMode)) return false;
                var is4x4 = yMode == YModeBPred;

                var subblockModes = EmptySubblockModes;
                var baseIndex = col * MacroblockSubBlockCount;
                if (is4x4) {
                    subblockModes = new int[MacroblockSubBlockCount];
                    for (var subY = 0; subY < 4; subY++) {
                        for (var subX = 0; subX < 4; subX++) {
                            var blockIndex = (subY * 4) + subX;
                            var aboveMode = BModeDcPred;
                            if (subY == 0) {
                                if (row > 0) {
                                    aboveMode = aboveSubModes[baseIndex + 12 + subX];
                                }
                            } else {
                                aboveMode = subblockModes[((subY - 1) * 4) + subX];
                            }

                            var leftMode = BModeDcPred;
                            if (subX == 0) {
                                if (col > 0) {
                                    leftMode = currentSubModes[baseIndex - MacroblockSubBlockCount + (subY * 4) + 3];
                                }
                            } else {
                                leftMode = subblockModes[(subY * 4) + subX - 1];
                            }

                            if (!TryReadKeyframeBMode(decoder, aboveMode, leftMode, out var mode)) return false;
                            subblockModes[blockIndex] = mode;
                        }
                    }

                    Array.Copy(subblockModes, 0, currentSubModes, baseIndex, MacroblockSubBlockCount);
                } else {
                    for (var i = 0; i < MacroblockSubBlockCount; i++) {
                        currentSubModes[baseIndex + i] = BModeDcPred;
                    }
                }

                if (!TryReadKeyframeUvMode(decoder, out var uvMode)) return false;

                macroblocks[index] = new WebpVp8MacroblockHeaderScaffold(
                    index,
                    col,
                    row,
                    segmentId,
                    skipCoefficients,
                    yMode,
                    uvMode,
                    is4x4,
                    subblockModes);
            }

            SwapRowModes(ref aboveSubModes, ref currentSubModes);
        }

        return true;
    }

    private static bool TryReadKeyframeYMode(WebpVp8BoolDecoder decoder, out int mode) {
        return TryReadTree(decoder, WebpVp8Tables.KeyframeYModeTree, WebpVp8Tables.KeyframeYModeProbs, out mode);
    }

    private static bool TryReadKeyframeUvMode(WebpVp8BoolDecoder decoder, out int mode) {
        return TryReadTree(decoder, WebpVp8Tables.UvModeTree, WebpVp8Tables.KeyframeUvModeProbs, out mode);
    }

    private static bool TryReadKeyframeBMode(WebpVp8BoolDecoder decoder, int aboveMode, int leftMode, out int mode) {
        mode = 0;
        if ((uint)aboveMode >= Intra4x4ModeCount) aboveMode = BModeDcPred;
        if ((uint)leftMode >= Intra4x4ModeCount) leftMode = BModeDcPred;

        var node = 0;
        while (true) {
            var probIndex = node >> 1;
            if ((uint)probIndex >= 9) return false;
            var prob = WebpVp8Tables.KeyframeBModeProbs[((aboveMode * Intra4x4ModeCount) + leftMode) * 9 + probIndex];
            if (!decoder.TryReadBool(prob, out var bit)) return false;
            var nextIndex = node + (bit ? 1 : 0);
            if ((uint)nextIndex >= (uint)WebpVp8Tables.BModeTree.Length) return false;
            var next = WebpVp8Tables.BModeTree[nextIndex];
            if (next <= 0) {
                mode = -next;
                return true;
            }
            node = next;
        }
    }

    private static bool TryReadTree(
        WebpVp8BoolDecoder decoder,
        ReadOnlySpan<int> tree,
        ReadOnlySpan<byte> probs,
        out int value) {
        value = 0;
        var node = 0;
        while (true) {
            var probIndex = node >> 1;
            if ((uint)probIndex >= (uint)probs.Length) return false;
            if (!decoder.TryReadBool(probs[probIndex], out var bit)) return false;
            var nextIndex = node + (bit ? 1 : 0);
            if ((uint)nextIndex >= (uint)tree.Length) return false;
            var next = tree[nextIndex];
            if (next <= 0) {
                value = -next;
                return true;
            }
            node = next;
        }
    }

    internal static bool TryReadPartitionLayout(ReadOnlySpan<byte> payload, out WebpVp8PartitionLayout layout) {
        layout = default;
        if (!TryReadHeader(payload, out var header)) return false;
        if (!TryReadFrameHeader(payload, out var frameHeader)) return false;

        var firstPartitionOffset = FrameTagBytes;
        var firstPartitionSize = header.PartitionSize;
        if (firstPartitionSize < KeyframeHeaderBytes) return false;

        var firstPartitionEndLong = (long)firstPartitionOffset + firstPartitionSize;
        if (firstPartitionEndLong > payload.Length) return false;
        if (firstPartitionEndLong > int.MaxValue) return false;
        var firstPartitionEnd = (int)firstPartitionEndLong;

        var dctPartitionCount = frameHeader.DctPartitionCount;
        if (dctPartitionCount <= 0) return false;

        var sizeTableBytesLong = 3L * (dctPartitionCount - 1);
        if (sizeTableBytesLong > int.MaxValue) return false;
        var sizeTableBytes = (int)sizeTableBytesLong;
        var sizeTableOffset = firstPartitionEnd;
        var dctDataOffsetLong = (long)sizeTableOffset + sizeTableBytes;
        if (dctDataOffsetLong > payload.Length) return false;
        if (dctDataOffsetLong > int.MaxValue) return false;
        var dctDataOffset = (int)dctDataOffsetLong;

        var dctSizes = new int[dctPartitionCount];
        long sum = 0;
        for (var i = 0; i < dctPartitionCount - 1; i++) {
            var entryOffset = sizeTableOffset + (i * 3);
            var size = ReadU24LE(payload, entryOffset);
            if (size < 0) return false;
            dctSizes[i] = size;
            sum += size;
            if (sum > payload.Length) return false;
        }

        var remainingLong = payload.Length - dctDataOffset - sum;
        if (remainingLong < 0) return false;
        if (remainingLong > int.MaxValue) return false;
        dctSizes[dctPartitionCount - 1] = (int)remainingLong;

        layout = new WebpVp8PartitionLayout(
            firstPartitionOffset,
            firstPartitionSize,
            sizeTableOffset,
            sizeTableBytes,
            dctDataOffset,
            dctSizes,
            payload.Length - dctDataOffset);
        return true;
    }

    internal static bool TryReadTokenPartitions(ReadOnlySpan<byte> payload, out WebpVp8TokenPartitions partitions) {
        partitions = default;
        if (!TryReadPartitionLayout(payload, out var layout)) return false;
        if (layout.DctPartitionSizes.Length == 0) return false;

        var infos = new WebpVp8TokenPartitionInfo[layout.DctPartitionSizes.Length];
        var offset = layout.DctDataOffset;
        long sum = 0;

        for (var i = 0; i < infos.Length; i++) {
            var size = layout.DctPartitionSizes[i];
            if (size < 0) return false;

            var endLong = (long)offset + size;
            if (endLong > payload.Length) return false;
            if (endLong > int.MaxValue) return false;

            var slice = payload.Slice(offset, size);
            if (slice.Length < 2) return false;

            var decoder = new WebpVp8BoolDecoder(slice);
            if (!decoder.TryReadBool(128, out _)) return false;

            infos[i] = new WebpVp8TokenPartitionInfo(offset, size, decoder.BytesConsumed);

            offset = (int)endLong;
            sum += size;
        }

        partitions = new WebpVp8TokenPartitions(layout.DctDataOffset, infos, (int)sum);
        return true;
    }

    internal static bool TryReadTokenScaffold(ReadOnlySpan<byte> payload, out WebpVp8TokenScaffold scaffold) {
        scaffold = default;
        if (!TryReadFrameHeader(payload, out var frameHeader)) return false;
        if (!TryReadTokenPartitions(payload, out var tokenPartitions)) return false;

        var partitionScaffolds = new WebpVp8TokenPartitionScaffold[tokenPartitions.Partitions.Length];
        var totalTokensRead = 0;
        var totalBytesConsumed = 0;

        for (var i = 0; i < tokenPartitions.Partitions.Length; i++) {
            var info = tokenPartitions.Partitions[i];
            if (info.Size < 2) return false;

            var slice = payload.Slice(info.Offset, info.Size);
            var decoder = new WebpVp8BoolDecoder(slice);
            if (!decoder.TryReadBool(128, out _)) return false;

            var tokens = new int[TokenScaffoldTokensPerPartition];
            var prevContexts = new int[TokenScaffoldTokensPerPartition];
            var tokenInfos = new WebpVp8TokenInfo[TokenScaffoldTokensPerPartition];
            var tokensRead = 0;
            var prevContext = 0;
            for (var t = 0; t < tokens.Length; t++) {
                var coefficientIndex = t % CoefficientsPerBlock;
                var band = CoeffBandTable[coefficientIndex];
                var prevBefore = prevContext;
                if (!TryReadScaffoldCoefficientToken(
                    decoder,
                    frameHeader.CoefficientProbabilities,
                    blockType: BlockTypeY2,
                    band,
                    prevContext,
                    out var tokenCode)) {
                    return false;
                }

                tokens[t] = tokenCode;
                prevContexts[t] = prevContext;
                tokensRead++;

                var tokenInfo = ClassifyToken(tokenCode, band, prevBefore);
                tokenInfos[t] = tokenInfo;
                prevContext = tokenInfo.PrevContextAfter;
            }

            partitionScaffolds[i] = new WebpVp8TokenPartitionScaffold(
                info.Offset,
                info.Size,
                decoder.BytesConsumed,
                tokens,
                prevContexts,
                tokenInfos,
                tokensRead);

            totalTokensRead += tokensRead;
            totalBytesConsumed += decoder.BytesConsumed;
        }

        scaffold = new WebpVp8TokenScaffold(
            tokenPartitions.DataOffset,
            partitionScaffolds,
            totalTokensRead,
            totalBytesConsumed);
        return true;
    }

    internal static bool TryReadBlockTokenScaffold(ReadOnlySpan<byte> payload, out WebpVp8BlockTokenScaffoldSet scaffold) {
        scaffold = default;
        if (!TryReadFrameHeader(payload, out var frameHeader)) return false;
        if (!TryReadTokenPartitions(payload, out var tokenPartitions)) return false;

        var partitionScaffolds = new WebpVp8BlockPartitionScaffold[tokenPartitions.Partitions.Length];
        var totalBlocksRead = 0;
        var totalTokensRead = 0;
        var totalBytesConsumed = 0;

        for (var i = 0; i < tokenPartitions.Partitions.Length; i++) {
            var info = tokenPartitions.Partitions[i];
            if (info.Size < 2) return false;

            var slice = payload.Slice(info.Offset, info.Size);
            var decoder = new WebpVp8BoolDecoder(slice);
            if (!decoder.TryReadBool(128, out _)) return false;

            var blocks = new WebpVp8BlockTokenScaffold[BlockScaffoldBlocksPerPartition];
            var blocksRead = 0;
            var partitionTokensRead = 0;

            for (var blockIndex = 0; blockIndex < blocks.Length; blockIndex++) {
                var blockType = GetScaffoldBlockType(blockIndex);
                var dequantFactor = ComputeScaffoldDequantFactor(blockType, frameHeader.Quantization);
                var tokenInfos = new WebpVp8BlockTokenInfo[BlockScaffoldMaxTokensPerBlock];
                var coefficients = new int[CoefficientsPerBlock];
                var dequantizedCoefficients = new int[CoefficientsPerBlock];
                var coefficientsNaturalOrder = new int[CoefficientsPerBlock];
                var dequantizedCoefficientsNaturalOrder = new int[CoefficientsPerBlock];
                var tokensRead = 0;
                var coefficientIndex = 0;
                var prevContext = 0;
                var reachedEob = false;

                while (coefficientIndex < CoefficientsPerBlock && tokensRead < tokenInfos.Length) {
                    var band = CoeffBandTable[coefficientIndex];
                    var prevBefore = prevContext;
                    if (!TryReadScaffoldCoefficientToken(
                        decoder,
                        frameHeader.CoefficientProbabilities,
                        blockType,
                        band,
                        prevContext,
                        out var tokenCode)) {
                        return false;
                    }

                    var tokenInfo = ClassifyToken(tokenCode, band, prevBefore);
                    if (!TryReadTokenExtraBits(decoder, tokenCode, out var extraBitsValue)) return false;
                    var magnitude = ComputeTokenMagnitude(tokenCode, extraBitsValue);
                    if (!TryReadSignedMagnitude(decoder, magnitude, out var coefficientValue)) return false;
                    coefficients[coefficientIndex] = coefficientValue;
                    dequantizedCoefficients[coefficientIndex] = coefficientValue * dequantFactor;
                    var naturalIndex = MapZigZagToNaturalIndex(coefficientIndex);
                    coefficientsNaturalOrder[naturalIndex] = coefficientValue;
                    dequantizedCoefficientsNaturalOrder[naturalIndex] = coefficientValue * dequantFactor;
                    tokenInfos[tokensRead] = new WebpVp8BlockTokenInfo(
                        coefficientIndex,
                        tokenInfo.TokenCode,
                        blockType,
                        tokenInfo.Band,
                        tokenInfo.PrevContextBefore,
                        tokenInfo.PrevContextAfter,
                        tokenInfo.HasMore,
                        tokenInfo.IsNonZero,
                        extraBitsValue,
                        coefficientValue,
                        naturalIndex);

                    tokensRead++;
                    partitionTokensRead++;
                    prevContext = tokenInfo.PrevContextAfter;

                    if (!tokenInfo.HasMore) {
                        reachedEob = true;
                        break;
                    }

                    coefficientIndex++;
                }

                var blockResult = BuildScaffoldBlockResult(
                    blockType,
                    dequantFactor,
                    coefficientsNaturalOrder,
                    dequantizedCoefficientsNaturalOrder,
                    reachedEob,
                    tokensRead);
                var blockPixels = BuildScaffoldBlockPixels(blockResult);

                blocks[blockIndex] = new WebpVp8BlockTokenScaffold(
                    blockIndex,
                    blockType,
                    dequantFactor,
                    coefficients,
                    dequantizedCoefficients,
                    coefficientsNaturalOrder,
                    dequantizedCoefficientsNaturalOrder,
                    tokenInfos,
                    tokensRead,
                    reachedEob,
                    blockResult,
                    blockPixels);
                blocksRead++;
            }

            partitionScaffolds[i] = new WebpVp8BlockPartitionScaffold(
                info.Offset,
                info.Size,
                decoder.BytesConsumed,
                blocks,
                blocksRead,
                partitionTokensRead);

            totalBlocksRead += blocksRead;
            totalTokensRead += partitionTokensRead;
            totalBytesConsumed += decoder.BytesConsumed;
        }

        scaffold = new WebpVp8BlockTokenScaffoldSet(
            tokenPartitions.DataOffset,
            partitionScaffolds,
            totalBlocksRead,
            totalTokensRead,
            totalBytesConsumed);
        return true;
    }

    internal static bool TryReadMacroblockHeaderScaffold(ReadOnlySpan<byte> payload, out WebpVp8MacroblockHeaderScaffoldSet headers) {
        headers = default;
        if (!TryReadHeader(payload, out var header)) return false;
        if (!TryReadFrameHeader(payload, out var frameHeader)) return false;
        if (!TryGetBoolCodedData(payload, out var boolData)) return false;
        if ((uint)frameHeader.BytesConsumed >= (uint)boolData.Length) return false;

        var macroblockCols = GetMacroblockDimension(header.Width);
        var macroblockRows = GetMacroblockDimension(header.Height);
        if (macroblockCols <= 0 || macroblockRows <= 0) return false;

        var macroblockCountLong = (long)macroblockCols * macroblockRows;
        if (macroblockCountLong <= 0 || macroblockCountLong > int.MaxValue) return false;
        var macroblockCount = (int)macroblockCountLong;

        var slice = boolData.Slice(frameHeader.BytesConsumed);
        if (slice.Length < 2) return false;

        var decoder = new WebpVp8BoolDecoder(slice);
        var macroblocks = new WebpVp8MacroblockHeaderScaffold[macroblockCount];
        var skipCount = 0;
        var segmentCounts = new int[SegmentCount];

        for (var index = 0; index < macroblocks.Length; index++) {
            var x = index % macroblockCols;
            var y = index / macroblockCols;

            var segmentId = 0;
            if (frameHeader.Segmentation.Enabled && frameHeader.Segmentation.UpdateMap) {
                if (!TryReadSegmentId(decoder, frameHeader.Segmentation.SegmentProbabilities, out segmentId)) {
                    return false;
                }
            }

            var skipCoefficients = false;
            if (frameHeader.NoCoefficientSkip) {
                var probability = frameHeader.SkipProbability;
                if ((uint)probability > 255) return false;
                if (!decoder.TryReadBool(probability, out skipCoefficients)) return false;
            }

            if (skipCoefficients) skipCount++;
            if ((uint)segmentId < segmentCounts.Length) {
                segmentCounts[segmentId]++;
            }

            if (!TryReadScaffoldMode(decoder, Intra16x16ModeCount + 1, out var yMode)) {
                return false;
            }

            var is4x4 = yMode == YModeBPred;
            var subblockModes = EmptySubblockModes;
            if (is4x4) {
                subblockModes = new int[MacroblockSubBlockCount];
                for (var b = 0; b < subblockModes.Length; b++) {
                    if (!TryReadScaffoldMode(decoder, Intra4x4ModeCount, out subblockModes[b])) {
                        return false;
                    }
                }
            }

            if (!TryReadScaffoldMode(decoder, IntraUvModeCount, out var uvMode)) {
                return false;
            }

            macroblocks[index] = new WebpVp8MacroblockHeaderScaffold(
                index,
                x,
                y,
                segmentId,
                skipCoefficients,
                yMode,
                uvMode,
                is4x4,
                subblockModes);
        }

        headers = new WebpVp8MacroblockHeaderScaffoldSet(
            macroblockCols,
            macroblockRows,
            macroblocks,
            decoder.BytesConsumed,
            skipCount,
            segmentCounts);
        return true;
    }

    internal static bool TryReadMacroblockTokenScaffold(ReadOnlySpan<byte> payload, out WebpVp8MacroblockTokenScaffoldSet scaffold) {
        scaffold = default;
        if (!TryReadFrameHeader(payload, out var frameHeader)) return false;
        if (!TryReadMacroblockHeaderScaffold(payload, out var headers)) return false;
        if (!TryReadTokenPartitions(payload, out var tokenPartitions)) return false;
        if (!TryReadBlockTokenScaffold(payload, out var blocks)) return false;

        var partitionCount = tokenPartitions.Partitions.Length;
        if (partitionCount <= 0) return false;
        if (blocks.Partitions.Length != partitionCount) return false;
        if (headers.Macroblocks.Length == 0) return false;

        var rowsPerPartition = new int[partitionCount];
        for (var row = 0; row < headers.MacroblockRows; row++) {
            var partitionIndex = GetTokenPartitionForRow(row, partitionCount);
            if ((uint)partitionIndex < (uint)rowsPerPartition.Length) {
                rowsPerPartition[partitionIndex]++;
            }
        }

        var partitionRowIndexByGlobalRow = new int[headers.MacroblockRows];
        var partitionRowCursor = new int[partitionCount];
        for (var row = 0; row < headers.MacroblockRows; row++) {
            var partitionIndex = GetTokenPartitionForRow(row, partitionCount);
            if ((uint)partitionIndex >= (uint)partitionRowCursor.Length) return false;
            partitionRowIndexByGlobalRow[row] = partitionRowCursor[partitionIndex];
            partitionRowCursor[partitionIndex]++;
        }

        var macroblocksPerPartition = new int[partitionCount];
        for (var i = 0; i < headers.Macroblocks.Length; i++) {
            var partitionIndex = GetTokenPartitionForRow(headers.Macroblocks[i].Y, partitionCount);
            if ((uint)partitionIndex < (uint)macroblocksPerPartition.Length) {
                macroblocksPerPartition[partitionIndex]++;
            }
        }

        var blockCursors = new int[partitionCount];
        var partitionTokenTotals = new int[partitionCount];
        var partitionByteTotals = new int[partitionCount];
        var partitionRowByteBudgets = new int[partitionCount][];
        var partitionRowNonSkippedMacroblocks = new int[partitionCount][];
        var partitionTokensConsumed = new int[partitionCount];
        var partitionBytesConsumed = new int[partitionCount];
        var partitionCurrentRowIndex = new int[partitionCount];
        var partitionRowTokensConsumed = new int[partitionCount];
        var partitionRowBytesConsumed = new int[partitionCount];
        for (var p = 0; p < partitionCurrentRowIndex.Length; p++) {
            partitionCurrentRowIndex[p] = -1;
        }

        for (var p = 0; p < partitionCount; p++) {
            var tokensPerMacroblock = GetScaffoldTokensPerMacroblock(blocks.Partitions[p]);
            var macroblockCount = macroblocksPerPartition[p];
            partitionTokenTotals[p] = tokensPerMacroblock * Math.Max(1, macroblockCount);
            var totalBytes = Math.Max(0, blocks.Partitions[p].BytesConsumed);
            partitionByteTotals[p] = totalBytes;

            var rowCount = rowsPerPartition[p];
            if (rowCount <= 0) {
                partitionRowByteBudgets[p] = Array.Empty<int>();
                partitionRowNonSkippedMacroblocks[p] = Array.Empty<int>();
                continue;
            }

            var budgets = new int[rowCount];
            var baseBytesPerRow = totalBytes / rowCount;
            var remainder = totalBytes % rowCount;
            for (var r = 0; r < budgets.Length; r++) {
                budgets[r] = baseBytesPerRow + (r < remainder ? 1 : 0);
            }

            partitionRowByteBudgets[p] = budgets;
            partitionRowNonSkippedMacroblocks[p] = new int[rowCount];
        }

        for (var i = 0; i < headers.Macroblocks.Length; i++) {
            var header = headers.Macroblocks[i];
            var partitionIndex = GetTokenPartitionForRow(header.Y, partitionCount);
            if ((uint)partitionIndex >= (uint)partitionRowNonSkippedMacroblocks.Length) return false;
            var rowIndex = partitionRowIndexByGlobalRow[header.Y];
            var rowNonSkipped = partitionRowNonSkippedMacroblocks[partitionIndex];
            if ((uint)rowIndex >= (uint)rowNonSkipped.Length) return false;
            if (!header.SkipCoefficients) {
                rowNonSkipped[rowIndex]++;
            }
        }

        var macroblocks = new WebpVp8MacroblockTokenScaffold[headers.Macroblocks.Length];
        var totalBlocksAssigned = 0;
        var totalTokensRead = 0;
        var totalBytesConsumed = 0;

        for (var i = 0; i < headers.Macroblocks.Length; i++) {
            var header = headers.Macroblocks[i];
            var partitionIndex = GetTokenPartitionForRow(header.Y, partitionCount);
            if ((uint)partitionIndex >= (uint)partitionCount) return false;

            var partition = blocks.Partitions[partitionIndex];
            if (partition.Blocks.Length == 0) return false;

            var assignedBlocks = new WebpVp8BlockTokenScaffold[MacroblockScaffoldBlocksPerMacroblock];
            var macroblockTokensRead = 0;
            if (header.SkipCoefficients) {
                var cursor = 0;
                cursor = FillSkippedBlocks(
                    assignedBlocks,
                    cursor,
                    MacroblockScaffoldLumaBlocks,
                    BlockTypeY,
                    frameHeader,
                    header.SegmentId);
                cursor = FillSkippedBlocks(
                    assignedBlocks,
                    cursor,
                    MacroblockScaffoldChromaBlocks,
                    BlockTypeU,
                    frameHeader,
                    header.SegmentId);
                _ = FillSkippedBlocks(
                    assignedBlocks,
                    cursor,
                    MacroblockScaffoldChromaBlocks,
                    BlockTypeV,
                    frameHeader,
                    header.SegmentId);
            } else {
                var cursor = 0;
                cursor = FillPartitionBlocks(
                    assignedBlocks,
                    cursor,
                    MacroblockScaffoldLumaBlocks,
                    BlockTypeY,
                    partitionIndex,
                    partition,
                    blockCursors,
                    frameHeader,
                    header.SegmentId,
                    ref macroblockTokensRead);
                cursor = FillPartitionBlocks(
                    assignedBlocks,
                    cursor,
                    MacroblockScaffoldChromaBlocks,
                    BlockTypeU,
                    partitionIndex,
                    partition,
                    blockCursors,
                    frameHeader,
                    header.SegmentId,
                    ref macroblockTokensRead);
                _ = FillPartitionBlocks(
                    assignedBlocks,
                    cursor,
                    MacroblockScaffoldChromaBlocks,
                    BlockTypeV,
                    partitionIndex,
                    partition,
                    blockCursors,
                    frameHeader,
                    header.SegmentId,
                    ref macroblockTokensRead);
            }

            if (header.X == 0) {
                partitionCurrentRowIndex[partitionIndex]++;
                partitionRowTokensConsumed[partitionIndex] = 0;
                partitionRowBytesConsumed[partitionIndex] = 0;
            }

            var partitionRowIndex = partitionCurrentRowIndex[partitionIndex];
            var rowBudgets = partitionRowByteBudgets[partitionIndex];
            if ((uint)partitionRowIndex >= (uint)rowBudgets.Length) return false;

            var expectedRowIndex = partitionRowIndexByGlobalRow[header.Y];
            if (expectedRowIndex != partitionRowIndex) return false;

            var tokensPerMacroblock = GetScaffoldTokensPerMacroblock(blocks.Partitions[partitionIndex]);
            var rowNonSkipped = partitionRowNonSkippedMacroblocks[partitionIndex];
            if ((uint)partitionRowIndex >= (uint)rowNonSkipped.Length) return false;
            var nonSkippedCount = rowNonSkipped[partitionRowIndex];
            var rowTokenTotal = tokensPerMacroblock * nonSkippedCount;
            var rowByteTotal = rowBudgets[partitionRowIndex];

            var partitionTokensBefore = partitionTokensConsumed[partitionIndex];
            var partitionBytesBefore = partitionBytesConsumed[partitionIndex];
            var macroblockBytesConsumed = 0;
            if (!header.SkipCoefficients && rowTokenTotal > 0) {
                macroblockBytesConsumed = ComputePartitionByteShare(
                    rowByteTotal,
                    partitionRowBytesConsumed[partitionIndex],
                    rowTokenTotal,
                    partitionRowTokensConsumed[partitionIndex],
                    macroblockTokensRead);
                partitionRowTokensConsumed[partitionIndex] += macroblockTokensRead;
                partitionRowBytesConsumed[partitionIndex] += macroblockBytesConsumed;
                partitionTokensConsumed[partitionIndex] += macroblockTokensRead;
                partitionBytesConsumed[partitionIndex] += macroblockBytesConsumed;
            }
            var partitionTokensAfter = partitionTokensConsumed[partitionIndex];
            var partitionBytesAfter = partitionBytesConsumed[partitionIndex];

            macroblocks[i] = new WebpVp8MacroblockTokenScaffold(
                header,
                partitionIndex,
                partitionBytesBefore,
                macroblockBytesConsumed,
                partitionBytesAfter,
                partitionTokensBefore,
                partitionTokensAfter,
                assignedBlocks,
                macroblockTokensRead);

            totalBlocksAssigned += assignedBlocks.Length;
            totalTokensRead += macroblockTokensRead;
            totalBytesConsumed += macroblockBytesConsumed;
        }

        scaffold = new WebpVp8MacroblockTokenScaffoldSet(
            headers.MacroblockCols,
            headers.MacroblockRows,
            partitionCount,
            macroblocks,
            totalBlocksAssigned,
            totalTokensRead,
            totalBytesConsumed);
        return true;
    }

    internal static bool TryReadMacroblockScaffold(ReadOnlySpan<byte> payload, out WebpVp8MacroblockScaffold macroblock) {
        macroblock = default;
        if (!TryReadMacroblockTokenScaffold(payload, out var macroblockTokens)) return false;
        if (macroblockTokens.Macroblocks.Length == 0) return false;
        macroblock = BuildMacroblockScaffold(macroblockTokens.Macroblocks[0], macroblockTokens.TotalBlocksAssigned);
        return macroblock.Width > 0 && macroblock.Height > 0;
    }

    internal static bool TryReadMacroblockRgbaScaffold(ReadOnlySpan<byte> payload, out WebpVp8RgbaScaffold rgbaScaffold) {
        rgbaScaffold = default;
        if (!TryReadMacroblockScaffold(payload, out var macroblock)) return false;
        var rgba = ConvertMacroblockScaffoldToRgba(macroblock);
        rgbaScaffold = new WebpVp8RgbaScaffold(macroblock.Width, macroblock.Height, rgba, macroblock.BlocksPlacedTotal);
        return true;
    }

    private static bool TryReadImageRgbaScaffold(ReadOnlySpan<byte> payload, WebpVp8Header header, out byte[] rgba) {
        rgba = Array.Empty<byte>();
        if (header.Width <= 0 || header.Height <= 0) return false;
        if (!TryReadMacroblockTokenScaffold(payload, out var macroblockTokens)) return false;
        if (macroblockTokens.Macroblocks.Length == 0) return false;
        if (macroblockTokens.MacroblockCols <= 0 || macroblockTokens.MacroblockRows <= 0) return false;

        var macroblockWidth = MacroblockSize;
        var macroblockHeight = MacroblockSize;
        if (macroblockWidth <= 0 || macroblockHeight <= 0) return false;

        var pixelBytes = checked(header.Width * header.Height * 4);
        var output = new byte[pixelBytes];

        var tileCols = (header.Width + macroblockWidth - 1) / macroblockWidth;
        var tileRows = (header.Height + macroblockHeight - 1) / macroblockHeight;
        var gridMatchesMacroblocks = tileCols == macroblockTokens.MacroblockCols && tileRows == macroblockTokens.MacroblockRows;

        var macroblockRgbaCache = new byte[macroblockTokens.Macroblocks.Length][];
        for (var i = 0; i < macroblockTokens.Macroblocks.Length; i++) {
            var macroblock = BuildMacroblockScaffold(macroblockTokens.Macroblocks[i], macroblockTokens.TotalBlocksAssigned);
            var macroblockRgba = ConvertMacroblockScaffoldToRgba(macroblock);
            macroblockRgbaCache[i] = UpscaleRgbaNearest(
                macroblockRgba,
                macroblock.Width,
                macroblock.Height,
                macroblockWidth,
                macroblockHeight);
        }

        for (var tileY = 0; tileY < tileRows; tileY++) {
            var dstBlockY = tileY * macroblockHeight;
            var sourceY = gridMatchesMacroblocks ? tileY : ClampIndex(tileY, macroblockTokens.MacroblockRows);
            for (var tileX = 0; tileX < tileCols; tileX++) {
                var dstBlockX = tileX * macroblockWidth;
                var sourceX = gridMatchesMacroblocks ? tileX : ClampIndex(tileX, macroblockTokens.MacroblockCols);
                var sourceIndex = (sourceY * macroblockTokens.MacroblockCols) + sourceX;
                if ((uint)sourceIndex >= (uint)macroblockRgbaCache.Length) return false;

                CopyMacroblockRgba(
                    macroblockRgbaCache[sourceIndex],
                    macroblockWidth,
                    macroblockHeight,
                    output,
                    header.Width,
                    header.Height,
                    dstBlockX,
                    dstBlockY);
            }
        }

        rgba = output;
        return true;
    }

    private static bool TryDecodeMacroblock(
        WebpVp8BoolDecoder decoder,
        WebpVp8CoefficientProbabilities probabilities,
        WebpVp8MacroblockHeaderScaffold macroblock,
        DequantFactors dequant,
        int macroblockX,
        int macroblockY,
        int width,
        int height,
        byte[] yPlane,
        byte[] uPlane,
        byte[] vPlane,
        int chromaWidth,
        int chromaHeight,
        byte[] y2NzAbove,
        byte[] y2NzCurrent,
        byte[] yNzAbove,
        byte[] yNzCurrent,
        byte[] uNzAbove,
        byte[] uNzCurrent,
        byte[] vNzAbove,
        byte[] vNzCurrent,
        out bool hasCoefficients) {
        hasCoefficients = false;
        var mbX = macroblockX;
        var mbY = macroblockY;
        var macroblockOffsetX = mbX * MacroblockSize;
        var macroblockOffsetY = mbY * MacroblockSize;
        var chromaOffsetX = mbX * MacroblockScaffoldChromaWidth;
        var chromaOffsetY = mbY * MacroblockScaffoldChromaHeight;

        var skipCoefficients = macroblock.SkipCoefficients;
        var hasY2 = !macroblock.Is4x4;

        var y2Dc = ZeroCoefficients;
        if (hasY2) {
            var initialContext = 0;
            if (mbY > 0) initialContext += y2NzAbove[mbX];
            if (mbX > 0) initialContext += y2NzCurrent[mbX - 1];
            if (initialContext > 2) initialContext = 2;

            var y2NonZero = false;
            var y2Coeffs = ZeroCoefficients;
            if (!skipCoefficients) {
                if (!TryDecodeBlockCoefficients(
                    decoder,
                    probabilities,
                    BlockTypeY2,
                    initialContext,
                    dequant.Y2Dc,
                    dequant.Y2Ac,
                    out y2Coeffs,
                    out y2NonZero)) {
                    return false;
                }
            }

            y2NzCurrent[mbX] = y2NonZero ? (byte)1 : (byte)0;
            if (y2NonZero) {
                y2Dc = InverseWalshTransform4x4(y2Coeffs);
            }

            hasCoefficients |= y2NonZero;
        } else {
            y2NzCurrent[mbX] = 0;
        }

        var yBlockBase = mbX * MacroblockSubBlockCount;
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

            var hasNonZero = false;
            var coefficients = ZeroCoefficients;
            if (!skipCoefficients) {
                if (!TryDecodeBlockCoefficients(
                    decoder,
                    probabilities,
                    BlockTypeY,
                    initialContext,
                    dequant.Y1Dc,
                    dequant.Y1Ac,
                    out coefficients,
                    out hasNonZero)) {
                    return false;
                }
            }

            yNzCurrent[yBlockBase + blockIndex] = hasNonZero ? (byte)1 : (byte)0;
            hasCoefficients |= hasNonZero;
            var mode = macroblock.Is4x4
                ? GetSubblockMode(macroblock.SubblockModes, blockIndex)
                : macroblock.YMode;

            var dstX = macroblockOffsetX + (subX * BlockSize);
            var dstY = macroblockOffsetY + (subY * BlockSize);
            var dcOverride = hasY2 && y2Dc.Length > blockIndex ? y2Dc[blockIndex] : 0;

            ApplyPredictionBlock(
                yPlane,
                width,
                height,
                dstX,
                dstY,
                coefficients,
                macroblock.Is4x4,
                mode,
                hasY2,
                dcOverride);
        }

        var chromaBlockBase = mbX * MacroblockScaffoldChromaBlocks;
        for (var blockIndex = 0; blockIndex < MacroblockScaffoldChromaBlocks; blockIndex++) {
            var subX = blockIndex & 1;
            var subY = blockIndex >> 1;
            var initialContext = 0;
            if (subY == 0) {
                if (mbY > 0) initialContext += uNzAbove[chromaBlockBase + 2 + subX];
            } else {
                initialContext += uNzCurrent[chromaBlockBase + ((subY - 1) * 2) + subX];
            }

            if (subX == 0) {
                if (mbX > 0) initialContext += uNzCurrent[chromaBlockBase - MacroblockScaffoldChromaBlocks + (subY * 2) + 1];
            } else {
                initialContext += uNzCurrent[chromaBlockBase + (subY * 2) + subX - 1];
            }

            if (initialContext > 2) initialContext = 2;

            var hasNonZero = false;
            var coefficients = ZeroCoefficients;
            if (!skipCoefficients) {
                if (!TryDecodeBlockCoefficients(
                    decoder,
                    probabilities,
                    BlockTypeU,
                    initialContext,
                    dequant.UvDc,
                    dequant.UvAc,
                    out coefficients,
                    out hasNonZero)) {
                    return false;
                }
            }

            uNzCurrent[chromaBlockBase + blockIndex] = hasNonZero ? (byte)1 : (byte)0;
            hasCoefficients |= hasNonZero;

            var dstX = chromaOffsetX + (subX * BlockSize);
            var dstY = chromaOffsetY + (subY * BlockSize);
            ApplyPredictionBlock(
                uPlane,
                chromaWidth,
                chromaHeight,
                dstX,
                dstY,
                coefficients,
                is4x4: false,
                macroblock.UvMode,
                overrideDc: false,
                dcValue: 0);
        }

        for (var blockIndex = 0; blockIndex < MacroblockScaffoldChromaBlocks; blockIndex++) {
            var subX = blockIndex & 1;
            var subY = blockIndex >> 1;
            var initialContext = 0;
            if (subY == 0) {
                if (mbY > 0) initialContext += vNzAbove[chromaBlockBase + 2 + subX];
            } else {
                initialContext += vNzCurrent[chromaBlockBase + ((subY - 1) * 2) + subX];
            }

            if (subX == 0) {
                if (mbX > 0) initialContext += vNzCurrent[chromaBlockBase - MacroblockScaffoldChromaBlocks + (subY * 2) + 1];
            } else {
                initialContext += vNzCurrent[chromaBlockBase + (subY * 2) + subX - 1];
            }

            if (initialContext > 2) initialContext = 2;

            var hasNonZero = false;
            var coefficients = ZeroCoefficients;
            if (!skipCoefficients) {
                if (!TryDecodeBlockCoefficients(
                    decoder,
                    probabilities,
                    BlockTypeV,
                    initialContext,
                    dequant.UvDc,
                    dequant.UvAc,
                    out coefficients,
                    out hasNonZero)) {
                    return false;
                }
            }

            vNzCurrent[chromaBlockBase + blockIndex] = hasNonZero ? (byte)1 : (byte)0;
            hasCoefficients |= hasNonZero;

            var dstX = chromaOffsetX + (subX * BlockSize);
            var dstY = chromaOffsetY + (subY * BlockSize);
            ApplyPredictionBlock(
                vPlane,
                chromaWidth,
                chromaHeight,
                dstX,
                dstY,
                coefficients,
                is4x4: false,
                macroblock.UvMode,
                overrideDc: false,
                dcValue: 0);
        }

        return true;
    }

    private static bool TryDecodeBlockCoefficients(
        WebpVp8BoolDecoder decoder,
        WebpVp8CoefficientProbabilities probabilities,
        int blockType,
        int initialContext,
        int dequantDc,
        int dequantAc,
        out int[] dequantizedCoefficients,
        out bool hasNonZero) {
        dequantizedCoefficients = new int[CoefficientsPerBlock];
        hasNonZero = false;

        var prevContext = initialContext;
        var coefficientIndex = 0;

        while (coefficientIndex < CoefficientsPerBlock) {
            var band = CoeffBandTable[coefficientIndex];
            if (!TryReadScaffoldCoefficientToken(
                decoder,
                probabilities,
                blockType,
                band,
                prevContext,
                out var tokenCode)) {
                return false;
            }

            if (tokenCode == 0) {
                break;
            }

            var coeffValue = 0;
            if (tokenCode > 1) {
                if (!TryReadTokenExtraBits(decoder, tokenCode, out var extraBitsValue)) return false;
                var magnitude = ComputeTokenMagnitude(tokenCode, extraBitsValue);
                if (!TryReadSignedMagnitude(decoder, magnitude, out coeffValue)) return false;
            }

            if (coeffValue != 0) {
                hasNonZero = true;
            }

            var dequantFactor = coefficientIndex == 0 ? dequantDc : dequantAc;
            var naturalIndex = MapZigZagToNaturalIndex(coefficientIndex);
            dequantizedCoefficients[naturalIndex] = coeffValue * dequantFactor;

            var tokenInfo = ClassifyToken(tokenCode, band, prevContext);
            prevContext = tokenInfo.PrevContextAfter;
            coefficientIndex++;
        }

        return true;
    }

    private static DequantFactors[] BuildDequantFactors(WebpVp8Quantization quantization, WebpVp8Segmentation segmentation) {
        var factors = new DequantFactors[SegmentCount];
        var segmentCount = segmentation.Enabled ? SegmentCount : 1;

        for (var i = 0; i < segmentCount; i++) {
            var q = quantization.BaseQIndex;
            if (segmentation.Enabled) {
                var delta = (i < segmentation.QuantizerDeltas.Length) ? segmentation.QuantizerDeltas[i] : 0;
                q = segmentation.AbsoluteDeltas ? delta : q + delta;
            }

            var y1Dc = GetDcQuant(q + GetDelta(quantization, 0));
            var y1Ac = GetAcQuant(q);
            var y2Dc = GetDcQuant(q + GetDelta(quantization, 1)) * 2;
            var y2Ac = (GetAcQuant(q + GetDelta(quantization, 2)) * 155) / 100;
            if (y2Ac < 8) y2Ac = 8;
            var uvDc = GetDcQuant(q + GetDelta(quantization, 3)) * 2;
            if (uvDc > 132) uvDc = 132;
            var uvAc = GetAcQuant(q + GetDelta(quantization, 4));

            factors[i] = new DequantFactors(y1Dc, y1Ac, y2Dc, y2Ac, uvDc, uvAc);
        }

        if (!segmentation.Enabled) {
            for (var i = 1; i < factors.Length; i++) {
                factors[i] = factors[0];
            }
        }

        return factors;
    }

    private static int GetDelta(WebpVp8Quantization quantization, int index) {
        if (quantization.Deltas is null || (uint)index >= (uint)quantization.Deltas.Length) return 0;
        return quantization.Deltas[index];
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

    private static byte[] ConvertPlanesToRgba(
        int width,
        int height,
        byte[] yPlane,
        byte[] uPlane,
        byte[] vPlane,
        int chromaWidth,
        int chromaHeight) {
        var rgba = new byte[checked(width * height * 4)];

        for (var y = 0; y < height; y++) {
            var chromaY = y >> 1;
            for (var x = 0; x < width; x++) {
                var yIndex = (y * width) + x;
                var ySample = yPlane[yIndex];
                var chromaX = x >> 1;
                var uSample = GetChromaSampleNearest(uPlane, chromaWidth, chromaHeight, chromaX, chromaY);
                var vSample = GetChromaSampleNearest(vPlane, chromaWidth, chromaHeight, chromaX, chromaY);

                var (r, g, b) = ConvertYuvToRgb(ySample, uSample, vSample);
                var dst = yIndex * 4;
                rgba[dst + 0] = r;
                rgba[dst + 1] = g;
                rgba[dst + 2] = b;
                rgba[dst + 3] = 255;
            }
        }

        return rgba;
    }

    private static void ApplyLoopFilter(
        WebpVp8LoopFilter loopFilter,
        WebpVp8Segmentation segmentation,
        WebpVp8MacroblockHeaderScaffold[] macroblocks,
        bool[] macroblockHasCoefficients,
        int width,
        int height,
        byte[] yPlane,
        byte[] uPlane,
        byte[] vPlane,
        int chromaWidth,
        int chromaHeight,
        bool isKeyframe) {
        if (macroblocks.Length == 0) return;
        if (loopFilter.Level <= 0 && (!segmentation.Enabled || segmentation.FilterDeltas.Length == 0)) {
            return;
        }

        var macroblockCols = GetMacroblockDimension(width);
        var macroblockRows = GetMacroblockDimension(height);
        if (macroblockCols <= 0 || macroblockRows <= 0) return;

        var useSimple = loopFilter.FilterType != 0;
        var strideY = width;
        var strideUv = chromaWidth;

        for (var row = 0; row < macroblockRows; row++) {
            for (var col = 0; col < macroblockCols; col++) {
                var index = (row * macroblockCols) + col;
                if ((uint)index >= (uint)macroblocks.Length) continue;
                var macroblock = macroblocks[index];
                var hasCoefficients = (uint)index < (uint)macroblockHasCoefficients.Length && macroblockHasCoefficients[index];
                var filterSubblocks = hasCoefficients || macroblock.Is4x4;

                if (!TryCalculateLoopFilterParameters(
                    loopFilter,
                    segmentation,
                    macroblock,
                    isKeyframe,
                    out var edgeLimit,
                    out var interiorLimit,
                    out var hevThreshold)) {
                    continue;
                }

                if (edgeLimit <= 0) continue;

                var yX = col * MacroblockSize;
                var yY = row * MacroblockSize;
                var uvX = col * MacroblockScaffoldChromaWidth;
                var uvY = row * MacroblockScaffoldChromaHeight;

                if (useSimple) {
                    var mbLimit = ((edgeLimit + 2) * 2) + interiorLimit;
                    var bLimit = (edgeLimit * 2) + interiorLimit;

                    if (col > 0) {
                        FilterVEdgeSimple(yPlane, width, height, strideY, yX, yY, mbLimit);
                    }

                    if (filterSubblocks) {
                        FilterVEdgeSimple(yPlane, width, height, strideY, yX + 4, yY, bLimit);
                        FilterVEdgeSimple(yPlane, width, height, strideY, yX + 8, yY, bLimit);
                        FilterVEdgeSimple(yPlane, width, height, strideY, yX + 12, yY, bLimit);
                    }

                    if (row > 0) {
                        FilterHEdgeSimple(yPlane, width, height, strideY, yX, yY, mbLimit);
                    }

                    if (filterSubblocks) {
                        FilterHEdgeSimple(yPlane, width, height, strideY, yX, yY + 4, bLimit);
                        FilterHEdgeSimple(yPlane, width, height, strideY, yX, yY + 8, bLimit);
                        FilterHEdgeSimple(yPlane, width, height, strideY, yX, yY + 12, bLimit);
                    }
                } else {
                    if (col > 0) {
                        FilterMbVEdge(yPlane, width, height, strideY, yX, yY, edgeLimit + 2, interiorLimit, hevThreshold, size: 2);
                        FilterMbVEdge(uPlane, chromaWidth, chromaHeight, strideUv, uvX, uvY, edgeLimit + 2, interiorLimit, hevThreshold, size: 1);
                        FilterMbVEdge(vPlane, chromaWidth, chromaHeight, strideUv, uvX, uvY, edgeLimit + 2, interiorLimit, hevThreshold, size: 1);
                    }

                    if (filterSubblocks) {
                        FilterSubblockVEdge(yPlane, width, height, strideY, yX + 4, yY, edgeLimit, interiorLimit, hevThreshold, size: 2);
                        FilterSubblockVEdge(yPlane, width, height, strideY, yX + 8, yY, edgeLimit, interiorLimit, hevThreshold, size: 2);
                        FilterSubblockVEdge(yPlane, width, height, strideY, yX + 12, yY, edgeLimit, interiorLimit, hevThreshold, size: 2);
                        FilterSubblockVEdge(uPlane, chromaWidth, chromaHeight, strideUv, uvX + 4, uvY, edgeLimit, interiorLimit, hevThreshold, size: 1);
                        FilterSubblockVEdge(vPlane, chromaWidth, chromaHeight, strideUv, uvX + 4, uvY, edgeLimit, interiorLimit, hevThreshold, size: 1);
                    }

                    if (row > 0) {
                        FilterMbHEdge(yPlane, width, height, strideY, yX, yY, edgeLimit + 2, interiorLimit, hevThreshold, size: 2);
                        FilterMbHEdge(uPlane, chromaWidth, chromaHeight, strideUv, uvX, uvY, edgeLimit + 2, interiorLimit, hevThreshold, size: 1);
                        FilterMbHEdge(vPlane, chromaWidth, chromaHeight, strideUv, uvX, uvY, edgeLimit + 2, interiorLimit, hevThreshold, size: 1);
                    }

                    if (filterSubblocks) {
                        FilterSubblockHEdge(yPlane, width, height, strideY, yX, yY + 4, edgeLimit, interiorLimit, hevThreshold, size: 2);
                        FilterSubblockHEdge(yPlane, width, height, strideY, yX, yY + 8, edgeLimit, interiorLimit, hevThreshold, size: 2);
                        FilterSubblockHEdge(yPlane, width, height, strideY, yX, yY + 12, edgeLimit, interiorLimit, hevThreshold, size: 2);
                        FilterSubblockHEdge(uPlane, chromaWidth, chromaHeight, strideUv, uvX, uvY + 4, edgeLimit, interiorLimit, hevThreshold, size: 1);
                        FilterSubblockHEdge(vPlane, chromaWidth, chromaHeight, strideUv, uvX, uvY + 4, edgeLimit, interiorLimit, hevThreshold, size: 1);
                    }
                }
            }
        }
    }

    internal static void ApplyLoopFilterForTest(
        WebpVp8LoopFilter loopFilter,
        WebpVp8Segmentation segmentation,
        WebpVp8MacroblockHeaderScaffold[] macroblocks,
        bool[] macroblockHasCoefficients,
        int width,
        int height,
        byte[] yPlane,
        byte[] uPlane,
        byte[] vPlane,
        int chromaWidth,
        int chromaHeight,
        bool isKeyframe) {
        ApplyLoopFilter(
            loopFilter,
            segmentation,
            macroblocks,
            macroblockHasCoefficients,
            width,
            height,
            yPlane,
            uPlane,
            vPlane,
            chromaWidth,
            chromaHeight,
            isKeyframe);
    }

    private static bool TryCalculateLoopFilterParameters(
        WebpVp8LoopFilter loopFilter,
        WebpVp8Segmentation segmentation,
        WebpVp8MacroblockHeaderScaffold macroblock,
        bool isKeyframe,
        out int edgeLimit,
        out int interiorLimit,
        out int hevThreshold) {
        edgeLimit = 0;
        interiorLimit = 0;
        hevThreshold = 0;

        var filterLevel = loopFilter.Level;
        if (segmentation.Enabled && segmentation.FilterDeltas.Length > 0) {
            var segmentId = macroblock.SegmentId;
            if ((uint)segmentId >= SegmentCount) segmentId = 0;
            var delta = segmentation.FilterDeltas[segmentId];
            filterLevel = segmentation.AbsoluteDeltas ? delta : filterLevel + delta;
        }

        filterLevel = ClampQIndex(filterLevel);

        if (loopFilter.DeltaEnabled) {
            var refDelta = loopFilter.RefDeltas.Length > 0 ? loopFilter.RefDeltas[0] : 0;
            filterLevel += refDelta;
            if (macroblock.Is4x4 && loopFilter.ModeDeltas.Length > 0) {
                filterLevel += loopFilter.ModeDeltas[0];
            }
        }

        filterLevel = ClampQIndex(filterLevel);
        if (filterLevel == 0) return false;

        interiorLimit = filterLevel;
        if (loopFilter.Sharpness > 0) {
            interiorLimit >>= loopFilter.Sharpness > 4 ? 2 : 1;
            var clamp = 9 - loopFilter.Sharpness;
            if (interiorLimit > clamp) interiorLimit = clamp;
        }
        if (interiorLimit < 1) interiorLimit = 1;

        hevThreshold = filterLevel >= 15 ? 1 : 0;
        if (filterLevel >= 40) hevThreshold++;
        if (!isKeyframe && filterLevel >= 20) hevThreshold++;

        edgeLimit = filterLevel;
        return true;
    }

    private static void FilterMbVEdge(
        byte[] plane,
        int width,
        int height,
        int stride,
        int x,
        int y,
        int edgeLimit,
        int interiorLimit,
        int hevThreshold,
        int size) {
        if (x < 4 || x + 3 >= width) return;
        var rows = Math.Min(8 * size, height - y);
        if (rows <= 0) return;

        for (var i = 0; i < rows; i++) {
            var py = y + i;
            var index = (py * stride) + x;
            if (NormalThreshold(plane, index, step: 1, edgeLimit, interiorLimit)) {
                if (HighEdgeVariance(plane, index, step: 1, hevThreshold)) {
                    FilterCommon(plane, index, step: 1, useOuterTaps: true);
                } else {
                    FilterMbEdge(plane, index, step: 1);
                }
            }
        }
    }

    private static void FilterSubblockVEdge(
        byte[] plane,
        int width,
        int height,
        int stride,
        int x,
        int y,
        int edgeLimit,
        int interiorLimit,
        int hevThreshold,
        int size) {
        if (x < 4 || x + 3 >= width) return;
        var rows = Math.Min(8 * size, height - y);
        if (rows <= 0) return;

        for (var i = 0; i < rows; i++) {
            var py = y + i;
            var index = (py * stride) + x;
            if (NormalThreshold(plane, index, step: 1, edgeLimit, interiorLimit)) {
                var useOuter = HighEdgeVariance(plane, index, step: 1, hevThreshold);
                FilterCommon(plane, index, step: 1, useOuterTaps: useOuter);
            }
        }
    }

    private static void FilterMbHEdge(
        byte[] plane,
        int width,
        int height,
        int stride,
        int x,
        int y,
        int edgeLimit,
        int interiorLimit,
        int hevThreshold,
        int size) {
        if (y < 4 || y + 3 >= height) return;
        var cols = Math.Min(8 * size, width - x);
        if (cols <= 0) return;

        for (var i = 0; i < cols; i++) {
            var px = x + i;
            var index = (y * stride) + px;
            if (NormalThreshold(plane, index, step: stride, edgeLimit, interiorLimit)) {
                if (HighEdgeVariance(plane, index, step: stride, hevThreshold)) {
                    FilterCommon(plane, index, step: stride, useOuterTaps: true);
                } else {
                    FilterMbEdge(plane, index, step: stride);
                }
            }
        }
    }

    private static void FilterSubblockHEdge(
        byte[] plane,
        int width,
        int height,
        int stride,
        int x,
        int y,
        int edgeLimit,
        int interiorLimit,
        int hevThreshold,
        int size) {
        if (y < 4 || y + 3 >= height) return;
        var cols = Math.Min(8 * size, width - x);
        if (cols <= 0) return;

        for (var i = 0; i < cols; i++) {
            var px = x + i;
            var index = (y * stride) + px;
            if (NormalThreshold(plane, index, step: stride, edgeLimit, interiorLimit)) {
                var useOuter = HighEdgeVariance(plane, index, step: stride, hevThreshold);
                FilterCommon(plane, index, step: stride, useOuterTaps: useOuter);
            }
        }
    }

    private static void FilterVEdgeSimple(
        byte[] plane,
        int width,
        int height,
        int stride,
        int x,
        int y,
        int filterLimit) {
        if (x < 2 || x + 1 >= width) return;
        var rows = Math.Min(16, height - y);
        if (rows <= 0) return;

        for (var i = 0; i < rows; i++) {
            var py = y + i;
            var index = (py * stride) + x;
            if (SimpleThreshold(plane, index, step: 1, filterLimit)) {
                FilterCommon(plane, index, step: 1, useOuterTaps: true);
            }
        }
    }

    private static void FilterHEdgeSimple(
        byte[] plane,
        int width,
        int height,
        int stride,
        int x,
        int y,
        int filterLimit) {
        if (y < 2 || y + 1 >= height) return;
        var cols = Math.Min(16, width - x);
        if (cols <= 0) return;

        for (var i = 0; i < cols; i++) {
            var px = x + i;
            var index = (y * stride) + px;
            if (SimpleThreshold(plane, index, step: stride, filterLimit)) {
                FilterCommon(plane, index, step: stride, useOuterTaps: true);
            }
        }
    }

    private static bool SimpleThreshold(byte[] plane, int index, int step, int filterLimit) {
        var p0 = plane[index - step];
        var q0 = plane[index];
        var p1 = plane[index - (2 * step)];
        var q1 = plane[index + step];
        var value = (Abs(p0 - q0) * 2) + (Abs(p1 - q1) >> 1);
        return value <= filterLimit;
    }

    private static bool NormalThreshold(byte[] plane, int index, int step, int edgeLimit, int interiorLimit) {
        var limit = (2 * edgeLimit) + interiorLimit;
        if (!SimpleThreshold(plane, index, step, limit)) return false;

        var p0 = plane[index - step];
        var q0 = plane[index];
        var p1 = plane[index - (2 * step)];
        var q1 = plane[index + step];
        var p2 = plane[index - (3 * step)];
        var q2 = plane[index + (2 * step)];
        var p3 = plane[index - (4 * step)];
        var q3 = plane[index + (3 * step)];

        return Abs(p3 - p2) <= interiorLimit
            && Abs(p2 - p1) <= interiorLimit
            && Abs(p1 - p0) <= interiorLimit
            && Abs(q3 - q2) <= interiorLimit
            && Abs(q2 - q1) <= interiorLimit
            && Abs(q1 - q0) <= interiorLimit;
    }

    private static bool HighEdgeVariance(byte[] plane, int index, int step, int hevThreshold) {
        var p0 = plane[index - step];
        var q0 = plane[index];
        var p1 = plane[index - (2 * step)];
        var q1 = plane[index + step];
        return Abs(p1 - p0) > hevThreshold || Abs(q1 - q0) > hevThreshold;
    }

    private static void FilterCommon(byte[] plane, int index, int step, bool useOuterTaps) {
        var p1 = plane[index - (2 * step)];
        var p0 = plane[index - step];
        var q0 = plane[index];
        var q1 = plane[index + step];

        var a = 3 * (q0 - p0);
        if (useOuterTaps) {
            a += SaturateInt8(p1 - q1);
        }
        a = SaturateInt8(a);

        var f1 = ((a + 4 > 127) ? 127 : a + 4) >> 3;
        var f2 = ((a + 3 > 127) ? 127 : a + 3) >> 3;

        plane[index - step] = SaturateUInt8(p0 + f2);
        plane[index] = SaturateUInt8(q0 - f1);

        if (!useOuterTaps) {
            var adjust = (f1 + 1) >> 1;
            plane[index - (2 * step)] = SaturateUInt8(p1 + adjust);
            plane[index + step] = SaturateUInt8(q1 - adjust);
        }
    }

    private static void FilterMbEdge(byte[] plane, int index, int step) {
        var p2 = plane[index - (3 * step)];
        var p1 = plane[index - (2 * step)];
        var p0 = plane[index - step];
        var q0 = plane[index];
        var q1 = plane[index + step];
        var q2 = plane[index + (2 * step)];

        var w = SaturateInt8(SaturateInt8(p1 - q1) + (3 * (q0 - p0)));

        var a = (27 * w + 63) >> 7;
        plane[index - step] = SaturateUInt8(p0 + a);
        plane[index] = SaturateUInt8(q0 - a);

        a = (18 * w + 63) >> 7;
        plane[index - (2 * step)] = SaturateUInt8(p1 + a);
        plane[index + step] = SaturateUInt8(q1 - a);

        a = (9 * w + 63) >> 7;
        plane[index - (3 * step)] = SaturateUInt8(p2 + a);
        plane[index + (2 * step)] = SaturateUInt8(q2 - a);
    }

    private static int SaturateInt8(int value) {
        if (value < -128) return -128;
        if (value > 127) return 127;
        return value;
    }

    private static byte SaturateUInt8(int value) {
        if (value < 0) return 0;
        if (value > 255) return 255;
        return (byte)value;
    }

    private static bool TryReadControlHeader(WebpVp8BoolDecoder decoder, out WebpVp8ControlHeader controlHeader) {
        controlHeader = default;
        if (!decoder.TryReadBool(probability: 128, out var colorSpaceBit)) return false;
        if (!decoder.TryReadBool(probability: 128, out var clampTypeBit)) return false;

        controlHeader = new WebpVp8ControlHeader(
            colorSpaceBit ? 1 : 0,
            clampTypeBit ? 1 : 0,
            decoder.BytesConsumed);
        return true;
    }

    private static bool TryReadSegmentation(WebpVp8BoolDecoder decoder, out WebpVp8Segmentation segmentation) {
        segmentation = default;
        if (!decoder.TryReadBool(128, out var segmentationEnabledBit)) return false;

        var enabled = segmentationEnabledBit;
        var updateMap = false;
        var updateData = false;
        var absoluteDeltas = false;
        var quantizerDeltas = new int[SegmentCount];
        var filterDeltas = new int[SegmentCount];
        var segmentProbabilities = new int[SegmentProbCount];

        for (var i = 0; i < segmentProbabilities.Length; i++) {
            segmentProbabilities[i] = -1;
        }

        if (enabled) {
            if (!decoder.TryReadBool(128, out var updateMapBit)) return false;
            if (!decoder.TryReadBool(128, out var updateDataBit)) return false;
            updateMap = updateMapBit;
            updateData = updateDataBit;

            if (updateData) {
                if (!decoder.TryReadBool(128, out var absoluteDeltasBit)) return false;
                absoluteDeltas = absoluteDeltasBit;

                for (var i = 0; i < SegmentCount; i++) {
                    if (!decoder.TryReadBool(128, out var updateQuantizerBit)) return false;
                    if (updateQuantizerBit) {
                        if (!decoder.TryReadSignedLiteral(7, out quantizerDeltas[i])) return false;
                    }
                }

                for (var i = 0; i < SegmentCount; i++) {
                    if (!decoder.TryReadBool(128, out var updateFilterBit)) return false;
                    if (updateFilterBit) {
                        if (!decoder.TryReadSignedLiteral(6, out filterDeltas[i])) return false;
                    }
                }
            }

            if (updateMap) {
                for (var i = 0; i < SegmentProbCount; i++) {
                    if (!decoder.TryReadBool(128, out var updateProbabilityBit)) return false;
                    if (updateProbabilityBit) {
                        if (!decoder.TryReadLiteral(8, out segmentProbabilities[i])) return false;
                    }
                }
            }
        }

        segmentation = new WebpVp8Segmentation(
            enabled,
            updateMap,
            updateData,
            absoluteDeltas,
            quantizerDeltas,
            filterDeltas,
            segmentProbabilities);
        return true;
    }

    private static bool TryReadLoopFilter(WebpVp8BoolDecoder decoder, out WebpVp8LoopFilter loopFilter) {
        loopFilter = default;
        if (!decoder.TryReadBool(128, out var filterTypeBit)) return false;
        if (!decoder.TryReadLiteral(6, out var level)) return false;
        if (!decoder.TryReadLiteral(3, out var sharpness)) return false;
        if (level < 0 || level > 63) return false;
        if (sharpness < 0 || sharpness > 7) return false;

        if (!decoder.TryReadBool(128, out var deltaEnabledBit)) return false;
        var deltaEnabled = deltaEnabledBit;
        var deltaUpdate = false;
        var refDeltas = new int[LoopFilterDeltaCount];
        var refDeltasUpdated = new bool[LoopFilterDeltaCount];
        var modeDeltas = new int[LoopFilterDeltaCount];
        var modeDeltasUpdated = new bool[LoopFilterDeltaCount];

        if (deltaEnabled) {
            if (!decoder.TryReadBool(128, out var deltaUpdateBit)) return false;
            deltaUpdate = deltaUpdateBit;

            if (deltaUpdate) {
                for (var i = 0; i < LoopFilterDeltaCount; i++) {
                    if (!decoder.TryReadBool(128, out var updateRefDeltaBit)) return false;
                    if (updateRefDeltaBit) {
                        if (!decoder.TryReadSignedLiteral(6, out refDeltas[i])) return false;
                        refDeltasUpdated[i] = true;
                    }
                }

                for (var i = 0; i < LoopFilterDeltaCount; i++) {
                    if (!decoder.TryReadBool(128, out var updateModeDeltaBit)) return false;
                    if (updateModeDeltaBit) {
                        if (!decoder.TryReadSignedLiteral(6, out modeDeltas[i])) return false;
                        modeDeltasUpdated[i] = true;
                    }
                }
            }
        }

        loopFilter = new WebpVp8LoopFilter(
            filterTypeBit ? 1 : 0,
            level,
            sharpness,
            deltaEnabled,
            deltaUpdate,
            refDeltas,
            refDeltasUpdated,
            modeDeltas,
            modeDeltasUpdated);
        return true;
    }

    private static bool TryReadQuantization(WebpVp8BoolDecoder decoder, out WebpVp8Quantization quantization) {
        quantization = default;
        if (!decoder.TryReadLiteral(7, out var baseQIndex)) return false;
        if (baseQIndex < 0 || baseQIndex > 127) return false;

        var deltas = new int[QuantDeltaCount];
        var deltasUpdated = new bool[QuantDeltaCount];

        for (var i = 0; i < QuantDeltaCount; i++) {
            if (!decoder.TryReadBool(128, out var updateDeltaBit)) return false;
            if (updateDeltaBit) {
                if (!decoder.TryReadSignedLiteral(4, out deltas[i])) return false;
                deltasUpdated[i] = true;
            }
        }

        quantization = new WebpVp8Quantization(baseQIndex, deltas, deltasUpdated);
        return true;
    }

    private static bool TryReadCoefficientProbabilities(WebpVp8BoolDecoder decoder, out WebpVp8CoefficientProbabilities probabilities) {
        probabilities = default;
        var totalCount = CoeffBlockTypes * CoeffBands * CoeffPrevContexts * CoeffEntropyNodes;
        var probs = new int[totalCount];
        var updated = new bool[totalCount];

        for (var i = 0; i < probs.Length; i++) {
            probs[i] = WebpVp8Tables.DefaultCoeffProbs[i];
        }

        var updatedCount = 0;
        for (var blockType = 0; blockType < CoeffBlockTypes; blockType++) {
            for (var band = 0; band < CoeffBands; band++) {
                for (var prev = 0; prev < CoeffPrevContexts; prev++) {
                    for (var node = 0; node < CoeffEntropyNodes; node++) {
                        var index = GetCoeffIndex(blockType, band, prev, node);
                        var updateProbability = WebpVp8Tables.CoeffUpdateProbs[index];
                        if (!decoder.TryReadBool(updateProbability, out var updateBit)) return false;
                        if (!updateBit) continue;

                        if (!decoder.TryReadLiteral(8, out var value)) return false;
                        probs[index] = value;
                        updated[index] = true;
                        updatedCount++;
                    }
                }
            }
        }

        probabilities = new WebpVp8CoefficientProbabilities(probs, updated, updatedCount, decoder.BytesConsumed);
        return true;
    }

    private static bool TryReadScaffoldCoefficientToken(
        WebpVp8BoolDecoder decoder,
        WebpVp8CoefficientProbabilities probabilities,
        int blockType,
        int band,
        int prevContext,
        out int tokenCode) {
        tokenCode = 0;
        if ((uint)blockType >= CoeffBlockTypes) return false;
        if ((uint)band >= CoeffBands) return false;
        if ((uint)prevContext >= CoeffPrevContexts) return false;

        // Spec-shaped token tree traversal using the 11 coefficient probabilities.
        var node = 0;
        while (node >= 0) {
            var probabilityIndex = node >> 1;
            if ((uint)probabilityIndex >= CoeffEntropyNodes) return false;

            var coeffIndex = GetCoeffIndex(blockType, band, prevContext, node: probabilityIndex);
            var probability = probabilities.Probabilities[coeffIndex];
            if ((uint)probability > 255) return false;
            if (!decoder.TryReadBool(probability, out var bit)) return false;

            var nextIndex = node + (bit ? 1 : 0);
            if ((uint)nextIndex >= CoeffTokenTree.Length) return false;
            node = CoeffTokenTree[nextIndex];
        }

        var token = -node - 1;
        if ((uint)token >= CoeffTokenCount) return false;
        tokenCode = token;
        return true;
    }

    private static WebpVp8TokenInfo ClassifyToken(int tokenCode, int band, int prevContextBefore) {
        var hasMore = tokenCode != 0;
        var isNonZero = tokenCode >= 2;
        var prevContextAfter = tokenCode switch {
            0 or 1 => 0,
            2 => 1,
            _ => 2,
        };

        return new WebpVp8TokenInfo(tokenCode, band, prevContextBefore, prevContextAfter, hasMore, isNonZero);
    }

    private static int GetScaffoldBlockType(int blockIndex) {
        var normalized = blockIndex % CoeffBlockTypes;
        if (normalized < 0) normalized += CoeffBlockTypes;
        return normalized;
    }

    private static int ComputeScaffoldDequantFactor(int blockType, WebpVp8Quantization quantization) {
        var baseFactor = Math.Max(1, quantization.BaseQIndex + 1);
        return blockType switch {
            BlockTypeY2 => baseFactor + 8,
            BlockTypeY => baseFactor + 4,
            BlockTypeU => baseFactor + 2,
            BlockTypeV => baseFactor + 2,
            _ => baseFactor + 2,
        };
    }

    private static int MapZigZagToNaturalIndex(int zigZagIndex) {
        if ((uint)zigZagIndex >= ZigZagToNaturalOrder.Length) return 0;
        return ZigZagToNaturalOrder[zigZagIndex];
    }

    private static bool TryReadTokenExtraBits(WebpVp8BoolDecoder decoder, int tokenCode, out int extraBitsValue) {
        extraBitsValue = 0;
        if ((uint)tokenCode >= CoeffTokenExtraBits.Length) return false;

        var bitCount = CoeffTokenExtraBits[tokenCode];
        if (bitCount == 0) return true;
        return decoder.TryReadLiteral(bitCount, out extraBitsValue);
    }

    private static int ComputeTokenMagnitude(int tokenCode, int extraBitsValue) {
        if ((uint)tokenCode >= CoeffTokenBaseMagnitude.Length) return 0;
        if (tokenCode <= 1) return 0;
        if (tokenCode <= 5) return CoeffTokenBaseMagnitude[tokenCode];
        return CoeffTokenBaseMagnitude[tokenCode] + extraBitsValue;
    }

    private static bool TryReadScaffoldMode(WebpVp8BoolDecoder decoder, int modeCount, out int mode) {
        mode = 0;
        if (modeCount <= 0) return false;

        var bitsNeeded = 0;
        var maxValue = 1;
        while (maxValue < modeCount && bitsNeeded < 8) {
            bitsNeeded++;
            maxValue <<= 1;
        }

        var value = 0;
        for (var i = 0; i < bitsNeeded; i++) {
            if (!decoder.TryReadBool(128, out var bit)) return false;
            value = (value << 1) | (bit ? 1 : 0);
        }

        mode = value % modeCount;
        return true;
    }

    private static bool TryReadSignedMagnitude(WebpVp8BoolDecoder decoder, int magnitude, out int coefficientValue) {
        coefficientValue = magnitude;
        if (magnitude == 0) return true;
        if (!decoder.TryReadBool(128, out var signBit)) return false;
        coefficientValue = signBit ? -magnitude : magnitude;
        return true;
    }

    private static WebpVp8BlockResult BuildScaffoldBlockResult(
        int blockType,
        int dequantFactor,
        int[] coefficientsNaturalOrder,
        int[] dequantizedCoefficientsNaturalOrder,
        bool reachedEob,
        int tokensRead) {
        var ac = new int[CoefficientsPerBlock - 1];
        var dequantAc = new int[CoefficientsPerBlock - 1];
        var hasNonZeroAc = false;

        for (var i = 1; i < CoefficientsPerBlock; i++) {
            var acIndex = i - 1;
            var value = coefficientsNaturalOrder[i];
            var dequantValue = dequantizedCoefficientsNaturalOrder[i];
            ac[acIndex] = value;
            dequantAc[acIndex] = dequantValue;
            if (value != 0) hasNonZeroAc = true;
        }

        return new WebpVp8BlockResult(
            blockType,
            dequantFactor,
            coefficientsNaturalOrder[0],
            dequantizedCoefficientsNaturalOrder[0],
            ac,
            dequantAc,
            hasNonZeroAc,
            reachedEob,
            tokensRead);
    }

    private static WebpVp8BlockPixelScaffold BuildScaffoldBlockPixels(WebpVp8BlockResult result) {
        var samples = new byte[BlockSize * BlockSize];
        var dequantizedCoefficients = new int[CoefficientsPerBlock];
        dequantizedCoefficients[0] = result.DequantDc;
        for (var i = 1; i < CoefficientsPerBlock; i++) {
            dequantizedCoefficients[i] = result.DequantAc[i - 1];
        }

        var residual = InverseTransform4x4(dequantizedCoefficients);
        var baseSample = 128;
        for (var i = 0; i < samples.Length; i++) {
            samples[i] = ClampToByte(baseSample + residual[i]);
        }

        var acInfluence = residual.Length > 1 ? residual[1] : 0;
        return new WebpVp8BlockPixelScaffold(BlockSize, BlockSize, samples, (byte)baseSample, acInfluence);
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

    private static byte ClampToByte(int value) {
        if (value < byte.MinValue) return byte.MinValue;
        if (value > byte.MaxValue) return byte.MaxValue;
        return (byte)value;
    }

    private static (int X, int Y) GetMacroblockLumaOffset(int blockIndex) {
        // 16 luma blocks in a 4x4 grid (each 4x4) over an 8x8 source scaffold.
        var x = (blockIndex & 3) * BlockSize;
        var y = (blockIndex >> 2) * BlockSize;
        return (x, y);
    }

    private static (int X, int Y) GetMacroblockChromaOffset(int blockIndex) {
        // 4 chroma blocks in a 2x2 grid (each 4x4) over a 4x4 source scaffold.
        var x = (blockIndex & 1) * BlockSize;
        var y = (blockIndex >> 1) * BlockSize;
        return (x, y);
    }

    private static void CopyBlockToPlane(
        byte[] block,
        int blockWidth,
        int blockHeight,
        byte[] plane,
        int planeWidth,
        int planeHeight,
        int dstX,
        int dstY) {
        for (var y = 0; y < blockHeight; y++) {
            var py = dstY + y;
            if ((uint)py >= planeHeight) continue;
            var blockRowOffset = y * blockWidth;
            var planeRowOffset = py * planeWidth;

            for (var x = 0; x < blockWidth; x++) {
                var px = dstX + x;
                if ((uint)px >= planeWidth) continue;
                plane[planeRowOffset + px] = block[blockRowOffset + x];
            }
        }
    }

    private static int GetSubblockMode(int[] modes, int index) {
        if (modes is null || modes.Length == 0) return 0;
        if ((uint)index >= (uint)modes.Length) return 0;
        return modes[index];
    }

    private static int GetPredictionKind(bool is4x4, int mode) {
        if (is4x4) return mode;

        return mode switch {
            0 => 0, // DC_PRED
            1 => 1, // V_PRED
            2 => 2, // H_PRED
            3 => 3, // TM_PRED
            _ => 0,
        };
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

    private static int[] BuildBlockResidual(WebpVp8BlockTokenScaffold block, bool overrideDc, int dcValue) {
        var coefficients = new int[CoefficientsPerBlock];
        coefficients[0] = overrideDc ? dcValue : block.Result.DequantDc;
        for (var i = 1; i < CoefficientsPerBlock; i++) {
            coefficients[i] = block.Result.DequantAc[i - 1];
        }

        return InverseTransform4x4(coefficients);
    }

    private static void ApplyPredictionBlock(
        byte[] plane,
        int planeWidth,
        int planeHeight,
        int dstX,
        int dstY,
        WebpVp8BlockTokenScaffold block,
        bool is4x4,
        int mode,
        bool overrideDc,
        int dcValue) {
        var residual = BuildBlockResidual(block, overrideDc, dcValue);
        Span<byte> top = stackalloc byte[BlockSize];
        Span<byte> left = stackalloc byte[BlockSize];

        var hasTop = dstY > 0;
        var hasLeft = dstX > 0;

        for (var i = 0; i < BlockSize; i++) {
            top[i] = GetPlaneSampleOrDefault(plane, planeWidth, planeHeight, dstX + i, dstY - 1, 128);
            left[i] = GetPlaneSampleOrDefault(plane, planeWidth, planeHeight, dstX - 1, dstY + i, 128);
        }

        var topLeft = GetPlaneSampleOrDefault(plane, planeWidth, planeHeight, dstX - 1, dstY - 1, 128);
        var predictionKind = GetPredictionKind(is4x4, mode);

        var dc = 128;
        if (predictionKind == 0) {
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

        Span<byte> topExt = stackalloc byte[8];
        Span<byte> leftExt = stackalloc byte[8];
        for (var i = 0; i < topExt.Length; i++) {
            topExt[i] = i < BlockSize ? top[i] : top[BlockSize - 1];
            leftExt[i] = i < BlockSize ? left[i] : left[BlockSize - 1];
        }

        for (var y = 0; y < BlockSize; y++) {
            var py = dstY + y;
            if ((uint)py >= (uint)planeHeight) continue;
            var rowOffset = py * planeWidth;
            var residualRow = y * BlockSize;
            for (var x = 0; x < BlockSize; x++) {
                var px = dstX + x;
                if ((uint)px >= (uint)planeWidth) continue;
                byte predicted;
                if (predictionKind <= 3) {
                    predicted = predictionKind switch {
                        1 => top[x],
                        2 => left[y],
                        3 => ClampToByte(left[y] + top[x] - topLeft),
                        _ => (byte)dc,
                    };
                } else {
                    predicted = predictionKind switch {
                        4 => PredictDownRight(topExt, leftExt, topLeft, x, y),
                        5 => PredictVerticalRight(topExt, topLeft, x, y),
                        6 => PredictDownLeft(topExt, x, y),
                        7 => PredictVerticalLeft(topExt, x, y),
                        8 => PredictHorizontalDown(leftExt, topLeft, x, y),
                        9 => PredictHorizontalUp(leftExt, x, y),
                        _ => (byte)dc,
                    };
                }
                var value = predicted + residual[residualRow + x];
                plane[rowOffset + px] = ClampToByte(value);
            }
        }
    }

    private static void ApplyPredictionBlock(
        byte[] plane,
        int planeWidth,
        int planeHeight,
        int dstX,
        int dstY,
        int[] dequantCoefficients,
        bool is4x4,
        int mode,
        bool overrideDc,
        int dcValue) {
        if (dequantCoefficients is null || dequantCoefficients.Length < CoefficientsPerBlock) {
            return;
        }

        var coeffs = dequantCoefficients;
        if (overrideDc && coeffs[0] != dcValue) {
            coeffs = new int[CoefficientsPerBlock];
            Array.Copy(dequantCoefficients, coeffs, CoefficientsPerBlock);
            coeffs[0] = dcValue;
        }

        var residual = InverseTransform4x4(coeffs);
        Span<byte> top = stackalloc byte[BlockSize];
        Span<byte> left = stackalloc byte[BlockSize];

        var hasTop = dstY > 0;
        var hasLeft = dstX > 0;

        for (var i = 0; i < BlockSize; i++) {
            top[i] = GetPlaneSampleOrDefault(plane, planeWidth, planeHeight, dstX + i, dstY - 1, 128);
            left[i] = GetPlaneSampleOrDefault(plane, planeWidth, planeHeight, dstX - 1, dstY + i, 128);
        }

        var topLeft = GetPlaneSampleOrDefault(plane, planeWidth, planeHeight, dstX - 1, dstY - 1, 128);
        var predictionKind = GetPredictionKind(is4x4, mode);

        var dc = 128;
        if (predictionKind == 0) {
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

        Span<byte> topExt = stackalloc byte[8];
        Span<byte> leftExt = stackalloc byte[8];
        for (var i = 0; i < topExt.Length; i++) {
            topExt[i] = i < BlockSize ? top[i] : top[BlockSize - 1];
            leftExt[i] = i < BlockSize ? left[i] : left[BlockSize - 1];
        }

        for (var y = 0; y < BlockSize; y++) {
            var py = dstY + y;
            if ((uint)py >= (uint)planeHeight) continue;
            var rowOffset = py * planeWidth;
            var residualRow = y * BlockSize;
            for (var x = 0; x < BlockSize; x++) {
                var px = dstX + x;
                if ((uint)px >= (uint)planeWidth) continue;
                byte predicted;
                if (predictionKind <= 3) {
                    predicted = predictionKind switch {
                        1 => top[x],
                        2 => left[y],
                        3 => ClampToByte(left[y] + top[x] - topLeft),
                        _ => (byte)dc,
                    };
                } else {
                    predicted = predictionKind switch {
                        4 => PredictDownRight(topExt, leftExt, topLeft, x, y),
                        5 => PredictVerticalRight(topExt, topLeft, x, y),
                        6 => PredictDownLeft(topExt, x, y),
                        7 => PredictVerticalLeft(topExt, x, y),
                        8 => PredictHorizontalDown(leftExt, topLeft, x, y),
                        9 => PredictHorizontalUp(leftExt, x, y),
                        _ => (byte)dc,
                    };
                }
                var value = predicted + residual[residualRow + x];
                plane[rowOffset + px] = ClampToByte(value);
            }
        }
    }

    private static int[] BuildY2DcOverrides(WebpVp8BlockTokenScaffold[] blocks) {
        if (blocks is null || blocks.Length == 0) return Array.Empty<int>();

        var dcCoefficients = new int[MacroblockSubBlockCount];
        var count = 0;
        for (var i = 0; i < blocks.Length && count < dcCoefficients.Length; i++) {
            if (blocks[i].BlockType != BlockTypeY) continue;
            dcCoefficients[count] = blocks[i].Result.DequantDc;
            count++;
        }

        if (count < dcCoefficients.Length) return Array.Empty<int>();
        return InverseTransform4x4(dcCoefficients);
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

    internal static WebpVp8MacroblockScaffold BuildMacroblockScaffold(WebpVp8MacroblockTokenScaffold macroblock, int blocksAvailable) {
        var blocks = macroblock.Blocks;
        if (blocks is null || blocks.Length == 0) return default;

        var sourceYPlane = new byte[MacroblockScaffoldSourceWidth * MacroblockScaffoldSourceHeight];
        var sourceUPlane = new byte[MacroblockScaffoldSourceChromaWidth * MacroblockScaffoldSourceChromaHeight];
        var sourceVPlane = new byte[MacroblockScaffoldSourceChromaWidth * MacroblockScaffoldSourceChromaHeight];
        var blocksPlacedY = 0;
        var blocksPlacedU = 0;
        var blocksPlacedV = 0;
        var blocksPlacedTotal = 0;
        var header = macroblock.Header;
        var y2Overrides = header.Is4x4 ? Array.Empty<int>() : BuildY2DcOverrides(blocks);

        for (var b = 0; b < blocks.Length && blocksPlacedTotal < (MacroblockScaffoldLumaBlocks + (MacroblockScaffoldChromaBlocks * 2)); b++) {
            var block = blocks[b];
            switch (block.BlockType) {
                case BlockTypeY2:
                case BlockTypeY: {
                    if (blocksPlacedY >= MacroblockScaffoldLumaBlocks) {
                        break;
                    }

                    var (dstX, dstY) = GetMacroblockLumaOffset(blocksPlacedY);
                    var yMode = header.Is4x4
                        ? GetSubblockMode(header.SubblockModes, blocksPlacedY)
                        : header.YMode;
                    ApplyPredictionBlock(
                        sourceYPlane,
                        MacroblockScaffoldSourceWidth,
                        MacroblockScaffoldSourceHeight,
                        dstX,
                        dstY,
                        block,
                        header.Is4x4,
                        yMode,
                        !header.Is4x4 && blocksPlacedY < y2Overrides.Length,
                        blocksPlacedY < y2Overrides.Length ? y2Overrides[blocksPlacedY] : 0);
                    blocksPlacedY++;
                    blocksPlacedTotal++;
                    break;
                }
                case BlockTypeU: {
                    if (blocksPlacedU < MacroblockScaffoldChromaBlocks) {
                        var (dstX, dstY) = GetMacroblockChromaOffset(blocksPlacedU);
                        ApplyPredictionBlock(
                            sourceUPlane,
                            MacroblockScaffoldSourceChromaWidth,
                            MacroblockScaffoldSourceChromaHeight,
                            dstX,
                            dstY,
                            block,
                            is4x4: false,
                            header.UvMode,
                            overrideDc: false,
                            dcValue: 0);
                        blocksPlacedU++;
                        blocksPlacedTotal++;
                    }

                    break;
                }
                case BlockTypeV: {
                    if (blocksPlacedV < MacroblockScaffoldChromaBlocks) {
                        var (dstX, dstY) = GetMacroblockChromaOffset(blocksPlacedV);
                        ApplyPredictionBlock(
                            sourceVPlane,
                            MacroblockScaffoldSourceChromaWidth,
                            MacroblockScaffoldSourceChromaHeight,
                            dstX,
                            dstY,
                            block,
                            is4x4: false,
                            header.UvMode,
                            overrideDc: false,
                            dcValue: 0);
                        blocksPlacedV++;
                        blocksPlacedTotal++;
                    }

                    break;
                }
            }
        }

        var yPlane = UpscalePlaneNearest(
            sourceYPlane,
            MacroblockScaffoldSourceWidth,
            MacroblockScaffoldSourceHeight,
            MacroblockScaffoldWidth,
            MacroblockScaffoldHeight);
        var uPlane = UpscalePlaneNearest(
            sourceUPlane,
            MacroblockScaffoldSourceChromaWidth,
            MacroblockScaffoldSourceChromaHeight,
            MacroblockScaffoldChromaWidth,
            MacroblockScaffoldChromaHeight);
        var vPlane = UpscalePlaneNearest(
            sourceVPlane,
            MacroblockScaffoldSourceChromaWidth,
            MacroblockScaffoldSourceChromaHeight,
            MacroblockScaffoldChromaWidth,
            MacroblockScaffoldChromaHeight);

        return new WebpVp8MacroblockScaffold(
            MacroblockScaffoldWidth,
            MacroblockScaffoldHeight,
            yPlane,
            MacroblockScaffoldChromaWidth,
            MacroblockScaffoldChromaHeight,
            uPlane,
            vPlane,
            blocksPlacedY,
            blocksPlacedU,
            blocksPlacedV,
            blocksPlacedTotal,
            blocksAvailable);
    }

    internal static byte[] ConvertMacroblockScaffoldToRgba(WebpVp8MacroblockScaffold macroblock) {
        var width = macroblock.Width;
        var height = macroblock.Height;
        if (width <= 0 || height <= 0) return Array.Empty<byte>();

        var rgba = new byte[width * height * 4];

        for (var y = 0; y < height; y++) {
            var chromaY = y >> 1;
            for (var x = 0; x < width; x++) {
                var yIndex = (y * width) + x;
                var ySample = macroblock.YPlane[yIndex];

                var chromaX = x >> 1;
                var uSample = GetChromaSampleNearest(macroblock.UPlane, macroblock.ChromaWidth, macroblock.ChromaHeight, chromaX, chromaY);
                var vSample = GetChromaSampleNearest(macroblock.VPlane, macroblock.ChromaWidth, macroblock.ChromaHeight, chromaX, chromaY);

                var (r, g, b) = ConvertYuvToRgb(ySample, uSample, vSample);
                var dst = yIndex * 4;
                rgba[dst + 0] = r;
                rgba[dst + 1] = g;
                rgba[dst + 2] = b;
                rgba[dst + 3] = 255;
            }
        }

        return rgba;
    }

    internal static byte[] UpscaleRgbaNearest(byte[] rgba, int width, int height, int targetWidth, int targetHeight) {
        if (rgba is null || rgba.Length == 0) return Array.Empty<byte>();
        if (width <= 0 || height <= 0) return Array.Empty<byte>();
        if (targetWidth <= 0 || targetHeight <= 0) return Array.Empty<byte>();
        if (width == targetWidth && height == targetHeight) return (byte[])rgba.Clone();

        var output = new byte[targetWidth * targetHeight * 4];

        for (var y = 0; y < targetHeight; y++) {
            var srcY = (y * height) / targetHeight;
            if (srcY >= height) srcY = height - 1;
            var srcRowOffset = srcY * width;
            var dstRowOffset = y * targetWidth;

            for (var x = 0; x < targetWidth; x++) {
                var srcX = (x * width) / targetWidth;
                if (srcX >= width) srcX = width - 1;

                var srcIndex = (srcRowOffset + srcX) * 4;
                var dstIndex = (dstRowOffset + x) * 4;
                output[dstIndex + 0] = rgba[srcIndex + 0];
                output[dstIndex + 1] = rgba[srcIndex + 1];
                output[dstIndex + 2] = rgba[srcIndex + 2];
                output[dstIndex + 3] = rgba[srcIndex + 3];
            }
        }

        return output;
    }

    private static byte[] UpscalePlaneNearest(byte[] plane, int width, int height, int targetWidth, int targetHeight) {
        if (plane is null || plane.Length == 0) return Array.Empty<byte>();
        if (width <= 0 || height <= 0) return Array.Empty<byte>();
        if (targetWidth <= 0 || targetHeight <= 0) return Array.Empty<byte>();
        if (width == targetWidth && height == targetHeight) return (byte[])plane.Clone();

        var output = new byte[targetWidth * targetHeight];

        for (var y = 0; y < targetHeight; y++) {
            var srcY = (y * height) / targetHeight;
            if (srcY >= height) srcY = height - 1;
            var srcRowOffset = srcY * width;
            var dstRowOffset = y * targetWidth;

            for (var x = 0; x < targetWidth; x++) {
                var srcX = (x * width) / targetWidth;
                if (srcX >= width) srcX = width - 1;

                output[dstRowOffset + x] = plane[srcRowOffset + srcX];
            }
        }

        return output;
    }

    private static void CopyMacroblockRgba(
        byte[] macroblockRgba,
        int macroblockWidth,
        int macroblockHeight,
        byte[] output,
        int outputWidth,
        int outputHeight,
        int dstBlockX,
        int dstBlockY) {
        for (var y = 0; y < macroblockHeight; y++) {
            var dstY = dstBlockY + y;
            if ((uint)dstY >= outputHeight) break;

            var macroblockRowOffset = y * macroblockWidth;
            var outputRowOffset = dstY * outputWidth;

            for (var x = 0; x < macroblockWidth; x++) {
                var dstX = dstBlockX + x;
                if ((uint)dstX >= outputWidth) break;

                var srcIndex = (macroblockRowOffset + x) * 4;
                var dstIndex = (outputRowOffset + dstX) * 4;
                output[dstIndex + 0] = macroblockRgba[srcIndex + 0];
                output[dstIndex + 1] = macroblockRgba[srcIndex + 1];
                output[dstIndex + 2] = macroblockRgba[srcIndex + 2];
                output[dstIndex + 3] = macroblockRgba[srcIndex + 3];
            }
        }
    }

    private static int ClampIndex(int index, int maxExclusive) {
        if (maxExclusive <= 0) return 0;
        if (index < 0) return 0;
        if (index >= maxExclusive) return maxExclusive - 1;
        return index;
    }

    private static bool TryReadSegmentId(WebpVp8BoolDecoder decoder, int[] probabilities, out int segmentId) {
        segmentId = 0;
        if (probabilities.Length < SegmentProbCount) return false;

        // VP8 segmentation tree for four segments using three probabilities:
        // prob0 -> (0) seg0, (1) go to prob1
        // prob1 -> (0) seg1, (1) go to prob2
        // prob2 -> (0) seg2, (1) seg3
        var prob0 = NormalizeSegmentProbability(probabilities[0]);
        if (!decoder.TryReadBool(prob0, out var bit0)) return false;
        if (!bit0) {
            segmentId = 0;
            return true;
        }

        var prob1 = NormalizeSegmentProbability(probabilities[1]);
        if (!decoder.TryReadBool(prob1, out var bit1)) return false;
        if (!bit1) {
            segmentId = 1;
            return true;
        }

        var prob2 = NormalizeSegmentProbability(probabilities[2]);
        if (!decoder.TryReadBool(prob2, out var bit2)) return false;
        segmentId = bit2 ? 3 : 2;
        return true;
    }

    private static int NormalizeSegmentProbability(int probability) {
        // -1 means "not present" in our parse scaffold; use a neutral default.
        if (probability < 0 || probability > 255) return 128;
        return probability;
    }

    internal static int ComputeScaffoldDequantFactorForSegment(int blockType, WebpVp8FrameHeader frameHeader, int segmentId) {
        var segmentBaseQIndex = ComputeSegmentBaseQIndex(frameHeader, segmentId);
        var baseFactor = Math.Max(1, segmentBaseQIndex + 1);
        return blockType switch {
            BlockTypeY2 => baseFactor + 8,
            BlockTypeY => baseFactor + 4,
            BlockTypeU => baseFactor + 2,
            BlockTypeV => baseFactor + 2,
            _ => baseFactor + 2,
        };
    }

    internal static int ComputeSegmentBaseQIndex(WebpVp8FrameHeader frameHeader, int segmentId) {
        var baseQIndex = frameHeader.Quantization.BaseQIndex;
        if (!frameHeader.Segmentation.Enabled || !frameHeader.Segmentation.UpdateData) {
            return ClampQIndex(baseQIndex);
        }

        if ((uint)segmentId >= SegmentCount) segmentId = 0;
        var segmentDeltaOrAbsolute = frameHeader.Segmentation.QuantizerDeltas[segmentId];
        var candidate = frameHeader.Segmentation.AbsoluteDeltas
            ? segmentDeltaOrAbsolute
            : baseQIndex + segmentDeltaOrAbsolute;
        return ClampQIndex(candidate);
    }

    private static int ClampQIndex(int qIndex) {
        if (qIndex < 0) return 0;
        if (qIndex > 127) return 127;
        return qIndex;
    }

    private static int GetMacroblockDimension(int pixels) {
        if (pixels <= 0) return 0;
        return (pixels + MacroblockSize - 1) / MacroblockSize;
    }

    private static int GetTokenPartitionForRow(int macroblockRow, int partitionCount) {
        if (partitionCount <= 0) return -1;
        if (macroblockRow < 0) macroblockRow = 0;
        // VP8 uses power-of-two partition counts; modulo keeps the scaffold robust.
        return macroblockRow % partitionCount;
    }

    private static int ComputePartitionByteShare(
        int totalBytes,
        int bytesConsumed,
        int totalTokens,
        int tokensConsumed,
        int tokensForMacroblock) {
        if (totalBytes <= 0) return 0;
        if (bytesConsumed >= totalBytes) return 0;
        if (tokensForMacroblock <= 0) return 0;

        var remainingBytes = totalBytes - bytesConsumed;
        if (remainingBytes <= 0) return 0;
        if (totalTokens <= 0) return remainingBytes;

        var remainingTokens = totalTokens - tokensConsumed;
        if (remainingTokens <= 0) return 0;
        if (tokensForMacroblock >= remainingTokens) return remainingBytes;

        var proportional = (int)((long)remainingBytes * tokensForMacroblock / remainingTokens);
        if (proportional <= 0 && remainingBytes > 0) proportional = 1;
        if (proportional > remainingBytes) proportional = remainingBytes;
        if (proportional < 0) proportional = 0;
        return proportional;
    }

    private static int GetNextPartitionBlockIndex(int partitionIndex, int blockCount, int[] cursors) {
        if ((uint)partitionIndex >= (uint)cursors.Length) return 0;
        if (blockCount <= 0) return 0;
        var cursor = cursors[partitionIndex];
        var blockIndex = cursor % blockCount;
        cursors[partitionIndex] = cursor + 1;
        return blockIndex;
    }

    private static int GetScaffoldTokensPerMacroblock(WebpVp8BlockPartitionScaffold partition) {
        var tokensPerPartition = Math.Max(0, partition.TokensRead);
        return tokensPerPartition * MacroblockScaffoldBlockScale;
    }

    private static int FillSkippedBlocks(
        WebpVp8BlockTokenScaffold[] blocks,
        int startIndex,
        int count,
        int blockType,
        WebpVp8FrameHeader frameHeader,
        int segmentId) {
        var cursor = startIndex;
        var dequantFactor = ComputeScaffoldDequantFactorForSegment(blockType, frameHeader, segmentId);
        for (var i = 0; i < count && cursor < blocks.Length; i++) {
            blocks[cursor] = BuildSkippedBlockScaffold(cursor, blockType, dequantFactor);
            cursor++;
        }

        return cursor;
    }

    private static int FillPartitionBlocks(
        WebpVp8BlockTokenScaffold[] blocks,
        int startIndex,
        int count,
        int blockType,
        int partitionIndex,
        WebpVp8BlockPartitionScaffold partition,
        int[] blockCursors,
        WebpVp8FrameHeader frameHeader,
        int segmentId,
        ref int macroblockTokensRead) {
        var cursor = startIndex;
        for (var i = 0; i < count && cursor < blocks.Length; i++) {
            var blockIndex = GetNextPartitionBlockIndex(partitionIndex, partition.Blocks.Length, blockCursors);
            var baseBlock = partition.Blocks[blockIndex];
            var dequantFactor = ComputeScaffoldDequantFactorForSegment(blockType, frameHeader, segmentId);
            var segmentBlock = ApplySegmentDequantization(
                new WebpVp8BlockTokenScaffold(
                    cursor,
                    blockType,
                    baseBlock.DequantFactor,
                    baseBlock.Coefficients,
                    baseBlock.DequantizedCoefficients,
                    baseBlock.CoefficientsNaturalOrder,
                    baseBlock.DequantizedCoefficientsNaturalOrder,
                    baseBlock.Tokens,
                    baseBlock.TokensRead,
                    baseBlock.ReachedEob,
                    baseBlock.Result,
                    baseBlock.BlockPixels),
                dequantFactor);
            blocks[cursor] = segmentBlock;
            macroblockTokensRead += segmentBlock.TokensRead;
            cursor++;
        }

        return cursor;
    }

    private static WebpVp8BlockTokenScaffold BuildSkippedBlockScaffold(int blockIndex, int blockType, int dequantFactor) {
        if (dequantFactor <= 0) dequantFactor = 1;

        var coefficients = new int[CoefficientsPerBlock];
        var dequantizedCoefficients = new int[CoefficientsPerBlock];
        var coefficientsNaturalOrder = new int[CoefficientsPerBlock];
        var dequantizedCoefficientsNaturalOrder = new int[CoefficientsPerBlock];
        var tokens = new WebpVp8BlockTokenInfo[BlockScaffoldMaxTokensPerBlock];

        var blockResult = BuildScaffoldBlockResult(
            blockType,
            dequantFactor,
            coefficientsNaturalOrder,
            dequantizedCoefficientsNaturalOrder,
            reachedEob: true,
            tokensRead: 0);
        var blockPixels = BuildScaffoldBlockPixels(blockResult);

        return new WebpVp8BlockTokenScaffold(
            blockIndex,
            blockType,
            dequantFactor,
            coefficients,
            dequantizedCoefficients,
            coefficientsNaturalOrder,
            dequantizedCoefficientsNaturalOrder,
            tokens,
            tokensRead: 0,
            reachedEob: true,
            blockResult,
            blockPixels);
    }

    private static WebpVp8BlockTokenScaffold ApplySegmentDequantization(WebpVp8BlockTokenScaffold block, int dequantFactor) {
        if (dequantFactor <= 0) dequantFactor = 1;

        var coefficients = block.Coefficients;
        var dequantizedCoefficients = new int[CoefficientsPerBlock];
        var coefficientsNaturalOrder = new int[CoefficientsPerBlock];
        var dequantizedCoefficientsNaturalOrder = new int[CoefficientsPerBlock];

        for (var i = 0; i < CoefficientsPerBlock; i++) {
            var value = coefficients[i];
            var dequantized = value * dequantFactor;
            dequantizedCoefficients[i] = dequantized;

            var naturalIndex = MapZigZagToNaturalIndex(i);
            coefficientsNaturalOrder[naturalIndex] = value;
            dequantizedCoefficientsNaturalOrder[naturalIndex] = dequantized;
        }

        var blockResult = BuildScaffoldBlockResult(
            block.BlockType,
            dequantFactor,
            coefficientsNaturalOrder,
            dequantizedCoefficientsNaturalOrder,
            block.ReachedEob,
            block.TokensRead);
        var blockPixels = BuildScaffoldBlockPixels(blockResult);

        return new WebpVp8BlockTokenScaffold(
            block.BlockIndex,
            block.BlockType,
            dequantFactor,
            coefficients,
            dequantizedCoefficients,
            coefficientsNaturalOrder,
            dequantizedCoefficientsNaturalOrder,
            block.Tokens,
            block.TokensRead,
            block.ReachedEob,
            blockResult,
            blockPixels);
    }

    private static byte GetChromaSampleNearest(byte[] chromaPlane, int width, int height, int x, int y) {
        if (chromaPlane.Length == 0 || width <= 0 || height <= 0) return 128;
        if (x < 0) x = 0;
        if (y < 0) y = 0;
        if (x >= width) x = width - 1;
        if (y >= height) y = height - 1;
        return chromaPlane[(y * width) + x];
    }

    private static (byte R, byte G, byte B) ConvertYuvToRgb(byte y, byte u, byte v) {
        var yy = y;
        var uu = u - 128;
        var vv = v - 128;

        var r = yy + (int)(1.402 * vv);
        var g = yy - (int)(0.344136 * uu) - (int)(0.714136 * vv);
        var b = yy + (int)(1.772 * uu);

        return (ClampToByte(r), ClampToByte(g), ClampToByte(b));
    }

    private static bool HasScaffoldSignature(ReadOnlySpan<byte> payload) {
        if (!TryGetBoolCodedData(payload, out var boolData)) return false;
        if (boolData.Length < ScaffoldSignature.Length) return false;
        return boolData.Slice(0, ScaffoldSignature.Length).SequenceEqual(ScaffoldSignature);
    }

    private static int GetCoeffIndex(int blockType, int band, int prev, int node) {
        return (((blockType * CoeffBands) + band) * CoeffPrevContexts + prev) * CoeffEntropyNodes + node;
    }

    private static int ReadU16LE(ReadOnlySpan<byte> data, int offset) {
        if (offset < 0 || offset + 2 > data.Length) return 0;
        return data[offset] | (data[offset + 1] << 8);
    }

    private static int ReadU24LE(ReadOnlySpan<byte> data, int offset) {
        if (offset < 0 || offset + 3 > data.Length) return 0;
        return data[offset]
            | (data[offset + 1] << 8)
            | (data[offset + 2] << 16);
    }

    private static int Abs(int value) => value < 0 ? -value : value;

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

internal readonly struct WebpVp8Header {
    public WebpVp8Header(
        int width,
        int height,
        int version,
        bool showFrame,
        int partitionSize,
        int horizontalScale,
        int verticalScale,
        int bitsConsumed) {
        Width = width;
        Height = height;
        Version = version;
        ShowFrame = showFrame;
        PartitionSize = partitionSize;
        HorizontalScale = horizontalScale;
        VerticalScale = verticalScale;
        BitsConsumed = bitsConsumed;
    }

    public int Width { get; }
    public int Height { get; }
    public int Version { get; }
    public bool ShowFrame { get; }
    public int PartitionSize { get; }
    public int HorizontalScale { get; }
    public int VerticalScale { get; }
    public int BitsConsumed { get; }
}

internal readonly struct WebpVp8ControlHeader {
    public WebpVp8ControlHeader(int colorSpace, int clampType, int bytesConsumed) {
        ColorSpace = colorSpace;
        ClampType = clampType;
        BytesConsumed = bytesConsumed;
    }

    public int ColorSpace { get; }
    public int ClampType { get; }
    public int BytesConsumed { get; }
}

internal readonly struct WebpVp8Segmentation {
    public WebpVp8Segmentation(
        bool enabled,
        bool updateMap,
        bool updateData,
        bool absoluteDeltas,
        int[] quantizerDeltas,
        int[] filterDeltas,
        int[] segmentProbabilities) {
        Enabled = enabled;
        UpdateMap = updateMap;
        UpdateData = updateData;
        AbsoluteDeltas = absoluteDeltas;
        QuantizerDeltas = quantizerDeltas;
        FilterDeltas = filterDeltas;
        SegmentProbabilities = segmentProbabilities;
    }

    public bool Enabled { get; }
    public bool UpdateMap { get; }
    public bool UpdateData { get; }
    public bool AbsoluteDeltas { get; }
    public int[] QuantizerDeltas { get; }
    public int[] FilterDeltas { get; }
    public int[] SegmentProbabilities { get; }
}

internal readonly struct WebpVp8LoopFilter {
    public WebpVp8LoopFilter(
        int filterType,
        int level,
        int sharpness,
        bool deltaEnabled,
        bool deltaUpdate,
        int[] refDeltas,
        bool[] refDeltasUpdated,
        int[] modeDeltas,
        bool[] modeDeltasUpdated) {
        FilterType = filterType;
        Level = level;
        Sharpness = sharpness;
        DeltaEnabled = deltaEnabled;
        DeltaUpdate = deltaUpdate;
        RefDeltas = refDeltas;
        RefDeltasUpdated = refDeltasUpdated;
        ModeDeltas = modeDeltas;
        ModeDeltasUpdated = modeDeltasUpdated;
    }

    public int FilterType { get; }
    public int Level { get; }
    public int Sharpness { get; }
    public bool DeltaEnabled { get; }
    public bool DeltaUpdate { get; }
    public int[] RefDeltas { get; }
    public bool[] RefDeltasUpdated { get; }
    public int[] ModeDeltas { get; }
    public bool[] ModeDeltasUpdated { get; }
}

internal readonly struct WebpVp8Quantization {
    public WebpVp8Quantization(int baseQIndex, int[] deltas, bool[] deltasUpdated) {
        BaseQIndex = baseQIndex;
        Deltas = deltas;
        DeltasUpdated = deltasUpdated;
    }

    public int BaseQIndex { get; }
    public int[] Deltas { get; }
    public bool[] DeltasUpdated { get; }
}

internal readonly struct WebpVp8CoefficientProbabilities {
    public WebpVp8CoefficientProbabilities(int[] probabilities, bool[] updated, int updatedCount, int bytesConsumed) {
        Probabilities = probabilities;
        Updated = updated;
        UpdatedCount = updatedCount;
        BytesConsumed = bytesConsumed;
    }

    public int[] Probabilities { get; }
    public bool[] Updated { get; }
    public int UpdatedCount { get; }
    public int BytesConsumed { get; }
}

internal readonly struct WebpVp8FrameHeader {
    public WebpVp8FrameHeader(
        WebpVp8ControlHeader controlHeader,
        WebpVp8Segmentation segmentation,
        WebpVp8LoopFilter loopFilter,
        WebpVp8Quantization quantization,
        WebpVp8CoefficientProbabilities coefficientProbabilities,
        int dctPartitionCount,
        bool refreshEntropyProbs,
        bool noCoefficientSkip,
        int skipProbability,
        int bytesConsumed) {
        ControlHeader = controlHeader;
        Segmentation = segmentation;
        LoopFilter = loopFilter;
        Quantization = quantization;
        CoefficientProbabilities = coefficientProbabilities;
        DctPartitionCount = dctPartitionCount;
        RefreshEntropyProbs = refreshEntropyProbs;
        NoCoefficientSkip = noCoefficientSkip;
        SkipProbability = skipProbability;
        BytesConsumed = bytesConsumed;
    }

    public WebpVp8ControlHeader ControlHeader { get; }
    public WebpVp8Segmentation Segmentation { get; }
    public WebpVp8LoopFilter LoopFilter { get; }
    public WebpVp8Quantization Quantization { get; }
    public WebpVp8CoefficientProbabilities CoefficientProbabilities { get; }
    public int DctPartitionCount { get; }
    public bool RefreshEntropyProbs { get; }
    public bool NoCoefficientSkip { get; }
    public int SkipProbability { get; }
    public int BytesConsumed { get; }
}

internal readonly struct WebpVp8PartitionLayout {
    public WebpVp8PartitionLayout(
        int firstPartitionOffset,
        int firstPartitionSize,
        int sizeTableOffset,
        int sizeTableBytes,
        int dctDataOffset,
        int[] dctPartitionSizes,
        int dctBytesAvailable) {
        FirstPartitionOffset = firstPartitionOffset;
        FirstPartitionSize = firstPartitionSize;
        SizeTableOffset = sizeTableOffset;
        SizeTableBytes = sizeTableBytes;
        DctDataOffset = dctDataOffset;
        DctPartitionSizes = dctPartitionSizes;
        DctBytesAvailable = dctBytesAvailable;
    }

    public int FirstPartitionOffset { get; }
    public int FirstPartitionSize { get; }
    public int SizeTableOffset { get; }
    public int SizeTableBytes { get; }
    public int DctDataOffset { get; }
    public int[] DctPartitionSizes { get; }
    public int DctBytesAvailable { get; }
}

internal readonly struct WebpVp8TokenPartitionInfo {
    public WebpVp8TokenPartitionInfo(int offset, int size, int headerBytesConsumed) {
        Offset = offset;
        Size = size;
        HeaderBytesConsumed = headerBytesConsumed;
    }

    public int Offset { get; }
    public int Size { get; }
    public int HeaderBytesConsumed { get; }
}

internal readonly struct WebpVp8TokenPartitions {
    public WebpVp8TokenPartitions(int dataOffset, WebpVp8TokenPartitionInfo[] partitions, int totalBytes) {
        DataOffset = dataOffset;
        Partitions = partitions;
        TotalBytes = totalBytes;
    }

    public int DataOffset { get; }
    public WebpVp8TokenPartitionInfo[] Partitions { get; }
    public int TotalBytes { get; }
}

internal readonly struct WebpVp8TokenPartitionScaffold {
    public WebpVp8TokenPartitionScaffold(
        int offset,
        int size,
        int bytesConsumed,
        int[] tokens,
        int[] prevContexts,
        WebpVp8TokenInfo[] tokenInfos,
        int tokensRead) {
        Offset = offset;
        Size = size;
        BytesConsumed = bytesConsumed;
        Tokens = tokens;
        PrevContexts = prevContexts;
        TokenInfos = tokenInfos;
        TokensRead = tokensRead;
    }

    public int Offset { get; }
    public int Size { get; }
    public int BytesConsumed { get; }
    public int[] Tokens { get; }
    public int[] PrevContexts { get; }
    public WebpVp8TokenInfo[] TokenInfos { get; }
    public int TokensRead { get; }
}

internal readonly struct WebpVp8TokenScaffold {
    public WebpVp8TokenScaffold(int dataOffset, WebpVp8TokenPartitionScaffold[] partitions, int totalTokensRead, int totalBytesConsumed) {
        DataOffset = dataOffset;
        Partitions = partitions;
        TotalTokensRead = totalTokensRead;
        TotalBytesConsumed = totalBytesConsumed;
    }

    public int DataOffset { get; }
    public WebpVp8TokenPartitionScaffold[] Partitions { get; }
    public int TotalTokensRead { get; }
    public int TotalBytesConsumed { get; }
}

internal readonly struct WebpVp8TokenInfo {
    public WebpVp8TokenInfo(
        int tokenCode,
        int band,
        int prevContextBefore,
        int prevContextAfter,
        bool hasMore,
        bool isNonZero) {
        TokenCode = tokenCode;
        Band = band;
        PrevContextBefore = prevContextBefore;
        PrevContextAfter = prevContextAfter;
        HasMore = hasMore;
        IsNonZero = isNonZero;
    }

    public int TokenCode { get; }
    public int Band { get; }
    public int PrevContextBefore { get; }
    public int PrevContextAfter { get; }
    public bool HasMore { get; }
    public bool IsNonZero { get; }
}

internal readonly struct WebpVp8BlockTokenInfo {
    public WebpVp8BlockTokenInfo(
        int coefficientIndex,
        int tokenCode,
        int blockType,
        int band,
        int prevContextBefore,
        int prevContextAfter,
        bool hasMore,
        bool isNonZero,
        int extraBitsValue,
        int coefficientValue,
        int naturalIndex) {
        CoefficientIndex = coefficientIndex;
        TokenCode = tokenCode;
        BlockType = blockType;
        Band = band;
        PrevContextBefore = prevContextBefore;
        PrevContextAfter = prevContextAfter;
        HasMore = hasMore;
        IsNonZero = isNonZero;
        ExtraBitsValue = extraBitsValue;
        CoefficientValue = coefficientValue;
        NaturalIndex = naturalIndex;
    }

    public int CoefficientIndex { get; }
    public int TokenCode { get; }
    public int BlockType { get; }
    public int Band { get; }
    public int PrevContextBefore { get; }
    public int PrevContextAfter { get; }
    public bool HasMore { get; }
    public bool IsNonZero { get; }
    public int ExtraBitsValue { get; }
    public int CoefficientValue { get; }
    public int NaturalIndex { get; }
}

internal readonly struct WebpVp8BlockTokenScaffold {
    public WebpVp8BlockTokenScaffold(
        int blockIndex,
        int blockType,
        int dequantFactor,
        int[] coefficients,
        int[] dequantizedCoefficients,
        int[] coefficientsNaturalOrder,
        int[] dequantizedCoefficientsNaturalOrder,
        WebpVp8BlockTokenInfo[] tokens,
        int tokensRead,
        bool reachedEob,
        WebpVp8BlockResult result,
        WebpVp8BlockPixelScaffold blockPixels) {
        BlockIndex = blockIndex;
        BlockType = blockType;
        DequantFactor = dequantFactor;
        Coefficients = coefficients;
        DequantizedCoefficients = dequantizedCoefficients;
        CoefficientsNaturalOrder = coefficientsNaturalOrder;
        DequantizedCoefficientsNaturalOrder = dequantizedCoefficientsNaturalOrder;
        Tokens = tokens;
        TokensRead = tokensRead;
        ReachedEob = reachedEob;
        Result = result;
        BlockPixels = blockPixels;
    }

    public int BlockIndex { get; }
    public int BlockType { get; }
    public int DequantFactor { get; }
    public int[] Coefficients { get; }
    public int[] DequantizedCoefficients { get; }
    public int[] CoefficientsNaturalOrder { get; }
    public int[] DequantizedCoefficientsNaturalOrder { get; }
    public WebpVp8BlockTokenInfo[] Tokens { get; }
    public int TokensRead { get; }
    public bool ReachedEob { get; }
    public WebpVp8BlockResult Result { get; }
    public WebpVp8BlockPixelScaffold BlockPixels { get; }
}

internal readonly struct WebpVp8BlockResult {
    public WebpVp8BlockResult(
        int blockType,
        int dequantFactor,
        int dc,
        int dequantDc,
        int[] ac,
        int[] dequantAc,
        bool hasNonZeroAc,
        bool reachedEob,
        int tokensRead) {
        BlockType = blockType;
        DequantFactor = dequantFactor;
        Dc = dc;
        DequantDc = dequantDc;
        Ac = ac;
        DequantAc = dequantAc;
        HasNonZeroAc = hasNonZeroAc;
        ReachedEob = reachedEob;
        TokensRead = tokensRead;
    }

    public int BlockType { get; }
    public int DequantFactor { get; }
    public int Dc { get; }
    public int DequantDc { get; }
    public int[] Ac { get; }
    public int[] DequantAc { get; }
    public bool HasNonZeroAc { get; }
    public bool ReachedEob { get; }
    public int TokensRead { get; }
}

internal readonly struct WebpVp8BlockPixelScaffold {
    public WebpVp8BlockPixelScaffold(int width, int height, byte[] samples, byte baseSample, int acInfluence) {
        Width = width;
        Height = height;
        Samples = samples;
        BaseSample = baseSample;
        AcInfluence = acInfluence;
    }

    public int Width { get; }
    public int Height { get; }
    public byte[] Samples { get; }
    public byte BaseSample { get; }
    public int AcInfluence { get; }
}

internal readonly struct WebpVp8BlockPartitionScaffold {
    public WebpVp8BlockPartitionScaffold(
        int offset,
        int size,
        int bytesConsumed,
        WebpVp8BlockTokenScaffold[] blocks,
        int blocksRead,
        int tokensRead) {
        Offset = offset;
        Size = size;
        BytesConsumed = bytesConsumed;
        Blocks = blocks;
        BlocksRead = blocksRead;
        TokensRead = tokensRead;
    }

    public int Offset { get; }
    public int Size { get; }
    public int BytesConsumed { get; }
    public WebpVp8BlockTokenScaffold[] Blocks { get; }
    public int BlocksRead { get; }
    public int TokensRead { get; }
}

internal readonly struct WebpVp8BlockTokenScaffoldSet {
    public WebpVp8BlockTokenScaffoldSet(
        int dataOffset,
        WebpVp8BlockPartitionScaffold[] partitions,
        int totalBlocksRead,
        int totalTokensRead,
        int totalBytesConsumed) {
        DataOffset = dataOffset;
        Partitions = partitions;
        TotalBlocksRead = totalBlocksRead;
        TotalTokensRead = totalTokensRead;
        TotalBytesConsumed = totalBytesConsumed;
    }

    public int DataOffset { get; }
    public WebpVp8BlockPartitionScaffold[] Partitions { get; }
    public int TotalBlocksRead { get; }
    public int TotalTokensRead { get; }
    public int TotalBytesConsumed { get; }
}

internal readonly struct WebpVp8MacroblockHeaderScaffold {
    public WebpVp8MacroblockHeaderScaffold(
        int index,
        int x,
        int y,
        int segmentId,
        bool skipCoefficients,
        int yMode,
        int uvMode,
        bool is4x4,
        int[] subblockModes) {
        Index = index;
        X = x;
        Y = y;
        SegmentId = segmentId;
        SkipCoefficients = skipCoefficients;
        YMode = yMode;
        UvMode = uvMode;
        Is4x4 = is4x4;
        SubblockModes = subblockModes ?? Array.Empty<int>();
    }

    public int Index { get; }
    public int X { get; }
    public int Y { get; }
    public int SegmentId { get; }
    public bool SkipCoefficients { get; }
    public int YMode { get; }
    public int UvMode { get; }
    public bool Is4x4 { get; }
    public int[] SubblockModes { get; }
}

internal readonly struct WebpVp8MacroblockHeaderScaffoldSet {
    public WebpVp8MacroblockHeaderScaffoldSet(
        int macroblockCols,
        int macroblockRows,
        WebpVp8MacroblockHeaderScaffold[] macroblocks,
        int boolBytesConsumed,
        int skipCount,
        int[] segmentCounts) {
        MacroblockCols = macroblockCols;
        MacroblockRows = macroblockRows;
        Macroblocks = macroblocks;
        BoolBytesConsumed = boolBytesConsumed;
        SkipCount = skipCount;
        SegmentCounts = segmentCounts;
    }

    public int MacroblockCols { get; }
    public int MacroblockRows { get; }
    public WebpVp8MacroblockHeaderScaffold[] Macroblocks { get; }
    public int BoolBytesConsumed { get; }
    public int SkipCount { get; }
    public int[] SegmentCounts { get; }
}

internal readonly struct WebpVp8MacroblockTokenScaffold {
    public WebpVp8MacroblockTokenScaffold(
        WebpVp8MacroblockHeaderScaffold header,
        int partitionIndex,
        int partitionBytesBefore,
        int partitionBytesConsumed,
        int partitionBytesAfter,
        int partitionTokensBefore,
        int partitionTokensAfter,
        WebpVp8BlockTokenScaffold[] blocks,
        int tokensRead) {
        Header = header;
        PartitionIndex = partitionIndex;
        PartitionBytesBefore = partitionBytesBefore;
        PartitionBytesConsumed = partitionBytesConsumed;
        PartitionBytesAfter = partitionBytesAfter;
        PartitionTokensBefore = partitionTokensBefore;
        PartitionTokensAfter = partitionTokensAfter;
        Blocks = blocks;
        TokensRead = tokensRead;
    }

    public WebpVp8MacroblockHeaderScaffold Header { get; }
    public int PartitionIndex { get; }
    public int PartitionBytesBefore { get; }
    public int PartitionBytesConsumed { get; }
    public int PartitionBytesAfter { get; }
    public int PartitionTokensBefore { get; }
    public int PartitionTokensAfter { get; }
    public WebpVp8BlockTokenScaffold[] Blocks { get; }
    public int TokensRead { get; }
}

internal readonly struct WebpVp8MacroblockTokenScaffoldSet {
    public WebpVp8MacroblockTokenScaffoldSet(
        int macroblockCols,
        int macroblockRows,
        int partitionCount,
        WebpVp8MacroblockTokenScaffold[] macroblocks,
        int totalBlocksAssigned,
        int totalTokensRead,
        int totalBytesConsumed) {
        MacroblockCols = macroblockCols;
        MacroblockRows = macroblockRows;
        PartitionCount = partitionCount;
        Macroblocks = macroblocks;
        TotalBlocksAssigned = totalBlocksAssigned;
        TotalTokensRead = totalTokensRead;
        TotalBytesConsumed = totalBytesConsumed;
    }

    public int MacroblockCols { get; }
    public int MacroblockRows { get; }
    public int PartitionCount { get; }
    public WebpVp8MacroblockTokenScaffold[] Macroblocks { get; }
    public int TotalBlocksAssigned { get; }
    public int TotalTokensRead { get; }
    public int TotalBytesConsumed { get; }
}

internal readonly struct WebpVp8MacroblockScaffold {
    public WebpVp8MacroblockScaffold(
        int width,
        int height,
        byte[] yPlane,
        int chromaWidth,
        int chromaHeight,
        byte[] uPlane,
        byte[] vPlane,
        int blocksPlacedY,
        int blocksPlacedU,
        int blocksPlacedV,
        int blocksPlacedTotal,
        int blocksAvailable) {
        Width = width;
        Height = height;
        YPlane = yPlane;
        ChromaWidth = chromaWidth;
        ChromaHeight = chromaHeight;
        UPlane = uPlane;
        VPlane = vPlane;
        BlocksPlacedY = blocksPlacedY;
        BlocksPlacedU = blocksPlacedU;
        BlocksPlacedV = blocksPlacedV;
        BlocksPlacedTotal = blocksPlacedTotal;
        BlocksAvailable = blocksAvailable;
    }

    public int Width { get; }
    public int Height { get; }
    public byte[] YPlane { get; }
    public int ChromaWidth { get; }
    public int ChromaHeight { get; }
    public byte[] UPlane { get; }
    public byte[] VPlane { get; }
    public int BlocksPlacedY { get; }
    public int BlocksPlacedU { get; }
    public int BlocksPlacedV { get; }
    public int BlocksPlacedTotal { get; }
    public int BlocksAvailable { get; }
}

internal readonly struct WebpVp8RgbaScaffold {
    public WebpVp8RgbaScaffold(int width, int height, byte[] rgba, int blocksPlaced) {
        Width = width;
        Height = height;
        Rgba = rgba;
        BlocksPlaced = blocksPlaced;
    }

    public int Width { get; }
    public int Height { get; }
    public byte[] Rgba { get; }
    public int BlocksPlaced { get; }
}
