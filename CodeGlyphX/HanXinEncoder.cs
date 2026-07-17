using System;
using CodeGlyphX.HanXin;

namespace CodeGlyphX;

/// <summary>Encodes ISO/IEC 20830 Han Xin Code symbols across versions 1 through 84.</summary>
public static class HanXinEncoder {
    /// <summary>Encodes text using automatic compaction and the smallest fitting version.</summary>
    public static HanXinSymbol EncodeText(string text) => EncodeText(text, new HanXinEncodingOptions());

    /// <summary>Encodes text with explicit version, ECC, mask, ECI, and compaction options.</summary>
    public static HanXinSymbol EncodeText(string text, HanXinEncodingOptions options) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        if (options is null) throw new ArgumentNullException(nameof(options));
        var effective = options.Clone();
        Validate(effective);
        var data = HanXinPayloadCodec.EncodeText(text, effective, out _);
        return EncodePrepared(data, effective);
    }

    /// <summary>Encodes arbitrary bytes using Han Xin binary compaction.</summary>
    public static HanXinSymbol EncodeBytes(byte[] bytes, HanXinEncodingOptions? options = null) {
        if (bytes is null) throw new ArgumentNullException(nameof(bytes));
        var effective = (options ?? new HanXinEncodingOptions()).Clone();
        effective.Mode = HanXinEncodingMode.Binary;
        Validate(effective);
        var data = HanXinPayloadCodec.EncodeBytes((byte[])bytes.Clone(), effective, out _);
        return EncodePrepared(data, effective);
    }

    private static HanXinSymbol EncodePrepared(byte[] data, HanXinEncodingOptions options) {
        var required = data.Length;
        var version = options.Version ?? 0;
        if (version == 0) {
            for (var candidate = 1; candidate <= 84; candidate++) {
                if (HanXinTables.DataCodewords(candidate, options.ErrorCorrectionLevel) >= required) { version = candidate; break; }
            }
            if (version == 0) throw new ArgumentException("The Han Xin payload exceeds Version 84 capacity.", nameof(data));
        } else if (HanXinTables.DataCodewords(version, options.ErrorCorrectionLevel) < required) {
            throw new ArgumentException($"The payload requires {required} data codewords, exceeding Version {version} ECC {options.ErrorCorrectionLevel} capacity.", nameof(data));
        }
        var mask = options.Mask ?? 0;
        var matrix = HanXinMatrixCodec.Encode(data, version, options.ErrorCorrectionLevel, mask);
        if (!options.Mask.HasValue) {
            var bestScore = HanXinMatrixCodec.Evaluate(matrix);
            for (var candidate = 1; candidate < 4; candidate++) {
                var candidateMatrix = HanXinMatrixCodec.Encode(data, version, options.ErrorCorrectionLevel, candidate);
                var score = HanXinMatrixCodec.Evaluate(candidateMatrix);
                if (score < bestScore) { bestScore = score; mask = candidate; matrix = candidateMatrix; }
            }
        }
        return new HanXinSymbol(matrix, version, options.ErrorCorrectionLevel, mask);
    }

    private static void Validate(HanXinEncodingOptions options) {
        if (options.Mode is < HanXinEncodingMode.Auto or > HanXinEncodingMode.Binary) throw new ArgumentOutOfRangeException(nameof(options), options.Mode, "Unknown Han Xin compaction mode.");
        if (options.Version is < 1 or > 84) throw new ArgumentOutOfRangeException(nameof(options), options.Version, "Han Xin version must be between 1 and 84.");
        if (options.ErrorCorrectionLevel is < 1 or > 4) throw new ArgumentOutOfRangeException(nameof(options), options.ErrorCorrectionLevel, "Han Xin ECC level must be between 1 and 4.");
        if (options.Mask is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(options), options.Mask, "Han Xin mask must be between 0 and 3.");
        if (options.EciAssignmentNumber is < 0 or > 999999) throw new ArgumentOutOfRangeException(nameof(options), options.EciAssignmentNumber, "Han Xin ECI assignment must be between 0 and 999999.");
    }
}
