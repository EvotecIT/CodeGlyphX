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

    internal static bool TryDecode(ReadOnlySpan<byte> payload, out byte[] rgba, out int width, out int height) {
        rgba = Array.Empty<byte>();
        width = 0;
        height = 0;

        if (!TryReadHeader(payload, out var header)) return false;
        _ = TryReadControlHeader(payload, out _);
        _ = TryReadFrameHeader(payload, out _);
        _ = TryReadPartitionLayout(payload, out _);

        // Parsing-only scaffold for now.
        width = header.Width;
        height = header.Height;
        return false;
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

internal readonly struct WebpVp8FrameHeader {
    public WebpVp8FrameHeader(
        WebpVp8ControlHeader controlHeader,
        WebpVp8Segmentation segmentation,
        WebpVp8LoopFilter loopFilter,
        WebpVp8Quantization quantization,
        int dctPartitionCount,
        bool refreshEntropyProbs,
        bool noCoefficientSkip,
        int skipProbability,
        int bytesConsumed) {
        ControlHeader = controlHeader;
        Segmentation = segmentation;
        LoopFilter = loopFilter;
        Quantization = quantization;
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
