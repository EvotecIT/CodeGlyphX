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
    private const int CoeffUpdateProbability = 128;
    private const int CoefficientsPerBlock = 16;
    private const int TokenScaffoldTokensPerPartition = 8;
    private const int CoeffTokenCount = 12;
    private const int BlockScaffoldBlocksPerPartition = 4;
    private const int BlockScaffoldMaxTokensPerBlock = CoefficientsPerBlock;
    private const int MacroblockSize = 16;
    private const int BlockSize = 4;
    private const int MacroblockScaffoldWidth = 8;
    private const int MacroblockScaffoldHeight = 8;
    private const int MacroblockScaffoldMaxBlocks = 4;
    private const int MacroblockScaffoldChromaWidth = 4;
    private const int MacroblockScaffoldChromaHeight = 4;
    private const int MacroblockScaffoldBlocksPerMacroblock = 4;
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

    internal static bool TryDecode(ReadOnlySpan<byte> payload, out byte[] rgba, out int width, out int height) {
        rgba = Array.Empty<byte>();
        width = 0;
        height = 0;

        if (!TryReadHeader(payload, out var header)) return false;
        _ = TryReadControlHeader(payload, out _);
        _ = TryReadFrameHeader(payload, out _);
        _ = TryReadPartitionLayout(payload, out _);
        _ = TryReadTokenPartitions(payload, out _);
        _ = TryReadTokenScaffold(payload, out _);
        _ = TryReadBlockTokenScaffold(payload, out _);
        _ = TryReadMacroblockHeaderScaffold(payload, out _);
        _ = TryReadMacroblockTokenScaffold(payload, out _);
        _ = TryReadMacroblockScaffold(payload, out _);
        _ = TryReadMacroblockRgbaScaffold(payload, out _);

        // Temporary scaffold decode: succeed only for explicitly marked payloads
        // to avoid shadowing the native fallback for real VP8 images.
        if (!HasScaffoldSignature(payload)) {
            return false;
        }

        if (!TryReadImageRgbaScaffold(payload, header, out rgba)) {
            return false;
        }

        width = header.Width;
        height = header.Height;
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

            macroblocks[index] = new WebpVp8MacroblockHeaderScaffold(index, x, y, segmentId, skipCoefficients);
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
            var tokensPerMacroblock = Math.Max(0, blocks.Partitions[p].TokensRead);
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
                for (var b = 0; b < assignedBlocks.Length; b++) {
                    var blockType = GetScaffoldBlockType(b);
                    var dequantFactor = ComputeScaffoldDequantFactorForSegment(blockType, frameHeader, header.SegmentId);
                    assignedBlocks[b] = BuildSkippedBlockScaffold(b, blockType, dequantFactor);
                }
            } else {
                for (var b = 0; b < assignedBlocks.Length; b++) {
                    var blockIndex = GetNextPartitionBlockIndex(partitionIndex, partition.Blocks.Length, blockCursors);
                    var baseBlock = partition.Blocks[blockIndex];
                    var segmentDequantFactor = ComputeScaffoldDequantFactorForSegment(baseBlock.BlockType, frameHeader, header.SegmentId);
                    var segmentBlock = ApplySegmentDequantization(baseBlock, segmentDequantFactor);
                    assignedBlocks[b] = segmentBlock;
                    macroblockTokensRead += segmentBlock.TokensRead;
                }
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

            var tokensPerMacroblock = Math.Max(0, blocks.Partitions[partitionIndex].TokensRead);
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
        macroblock = BuildMacroblockScaffold(macroblockTokens.Macroblocks[0].Blocks, macroblockTokens.TotalBlocksAssigned);
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

        var macroblockWidth = MacroblockScaffoldWidth;
        var macroblockHeight = MacroblockScaffoldHeight;
        if (macroblockWidth <= 0 || macroblockHeight <= 0) return false;

        var pixelBytes = checked(header.Width * header.Height * 4);
        var output = new byte[pixelBytes];

        var tileCols = (header.Width + macroblockWidth - 1) / macroblockWidth;
        var tileRows = (header.Height + macroblockHeight - 1) / macroblockHeight;

        var macroblockRgbaCache = new byte[macroblockTokens.Macroblocks.Length][];
        for (var i = 0; i < macroblockTokens.Macroblocks.Length; i++) {
            var macroblock = BuildMacroblockScaffold(macroblockTokens.Macroblocks[i].Blocks, macroblockTokens.TotalBlocksAssigned);
            macroblockRgbaCache[i] = ConvertMacroblockScaffoldToRgba(macroblock);
        }

        for (var tileY = 0; tileY < tileRows; tileY++) {
            var dstBlockY = tileY * macroblockHeight;
            var sourceY = ClampIndex(tileY, macroblockTokens.MacroblockRows);
            for (var tileX = 0; tileX < tileCols; tileX++) {
                var dstBlockX = tileX * macroblockWidth;
                var sourceX = ClampIndex(tileX, macroblockTokens.MacroblockCols);
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
            probs[i] = 128;
        }

        var updatedCount = 0;
        for (var blockType = 0; blockType < CoeffBlockTypes; blockType++) {
            for (var band = 0; band < CoeffBands; band++) {
                for (var prev = 0; prev < CoeffPrevContexts; prev++) {
                    for (var node = 0; node < CoeffEntropyNodes; node++) {
                        if (!decoder.TryReadBool(CoeffUpdateProbability, out var updateBit)) return false;
                        if (!updateBit) continue;

                        if (!decoder.TryReadLiteral(8, out var value)) return false;
                        var index = GetCoeffIndex(blockType, band, prev, node);
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

        // Very small inverse-transform placeholder: DC sets the baseline,
        // first AC (if present) adds a tiny alternating influence.
        var baseSample = ClampToByte(128 + (result.DequantDc / 8));
        var acInfluence = result.DequantAc.Length > 0 ? result.DequantAc[0] / 16 : 0;

        for (var i = 0; i < samples.Length; i++) {
            var signedInfluence = (i & 1) == 0 ? acInfluence : -acInfluence;
            samples[i] = ClampToByte(baseSample + signedInfluence);
        }

        return new WebpVp8BlockPixelScaffold(BlockSize, BlockSize, samples, baseSample, acInfluence);
    }

    private static byte ClampToByte(int value) {
        if (value < byte.MinValue) return byte.MinValue;
        if (value > byte.MaxValue) return byte.MaxValue;
        return (byte)value;
    }

    private static (int X, int Y) GetMacroblockBlockOffset(int blockIndex) {
        // Tile up to four 4x4 blocks into an 8x8 scaffold:
        // 0 -> (0,0), 1 -> (4,0), 2 -> (0,4), 3 -> (4,4)
        var x = (blockIndex & 1) * BlockSize;
        var y = ((blockIndex >> 1) & 1) * BlockSize;
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

    internal static WebpVp8MacroblockScaffold BuildMacroblockScaffold(WebpVp8BlockTokenScaffold[] blocks, int blocksAvailable) {
        if (blocks is null || blocks.Length == 0) return default;

        var yPlane = new byte[MacroblockScaffoldWidth * MacroblockScaffoldHeight];
        var uPlane = new byte[MacroblockScaffoldChromaWidth * MacroblockScaffoldChromaHeight];
        var vPlane = new byte[MacroblockScaffoldChromaWidth * MacroblockScaffoldChromaHeight];
        var blocksPlacedY = 0;
        var blocksPlacedU = 0;
        var blocksPlacedV = 0;
        var blocksPlacedTotal = 0;

        for (var b = 0; b < blocks.Length && blocksPlacedTotal < MacroblockScaffoldMaxBlocks; b++) {
            var block = blocks[b];
            switch (block.BlockType) {
                case BlockTypeY2:
                case BlockTypeY: {
                    var (dstX, dstY) = GetMacroblockBlockOffset(blocksPlacedY);
                    CopyBlockToPlane(
                        block.BlockPixels.Samples,
                        block.BlockPixels.Width,
                        block.BlockPixels.Height,
                        yPlane,
                        MacroblockScaffoldWidth,
                        MacroblockScaffoldHeight,
                        dstX,
                        dstY);
                    blocksPlacedY++;
                    blocksPlacedTotal++;
                    break;
                }
                case BlockTypeU: {
                    if (blocksPlacedU == 0) {
                        CopyBlockToPlane(
                            block.BlockPixels.Samples,
                            block.BlockPixels.Width,
                            block.BlockPixels.Height,
                            uPlane,
                            MacroblockScaffoldChromaWidth,
                            MacroblockScaffoldChromaHeight,
                            dstX: 0,
                            dstY: 0);
                        blocksPlacedU++;
                        blocksPlacedTotal++;
                    }

                    break;
                }
                case BlockTypeV: {
                    if (blocksPlacedV == 0) {
                        CopyBlockToPlane(
                            block.BlockPixels.Samples,
                            block.BlockPixels.Width,
                            block.BlockPixels.Height,
                            vPlane,
                            MacroblockScaffoldChromaWidth,
                            MacroblockScaffoldChromaHeight,
                            dstX: 0,
                            dstY: 0);
                        blocksPlacedV++;
                        blocksPlacedTotal++;
                    }

                    break;
                }
            }
        }

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
    public WebpVp8MacroblockHeaderScaffold(int index, int x, int y, int segmentId, bool skipCoefficients) {
        Index = index;
        X = x;
        Y = y;
        SegmentId = segmentId;
        SkipCoefficients = skipCoefficients;
    }

    public int Index { get; }
    public int X { get; }
    public int Y { get; }
    public int SegmentId { get; }
    public bool SkipCoefficients { get; }
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
