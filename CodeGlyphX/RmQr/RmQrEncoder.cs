// Portions adapted from the Zint backend, Copyright (c) Robin Stuart and contributors.
// Licensed under BSD-3-Clause; see THIRD-PARTY-NOTICES.md.

using System;
using CodeGlyphX.Internal;
using CodeGlyphX.Qr;

namespace CodeGlyphX.RmQr;

internal static class RmQrEncoder {
    internal static RmQrCode EncodeText(string text, RmQrEncodingOptions options) {
        ValidateOptions(options);
        var mode = ResolveMode(text, options);
        var textEncoding = ResolveTextEncoding(options, mode);
        if (mode == RmQrMode.Byte && !QrEncoding.CanEncode(text, textEncoding)) {
            throw new ArgumentException($"Text cannot be represented by {textEncoding}.", nameof(text));
        }
        var segment = mode switch {
            RmQrMode.Numeric => RmQrSegment.CreateNumeric(text),
            RmQrMode.Alphanumeric => RmQrSegment.CreateAlphanumeric(text),
            RmQrMode.Kanji => RmQrSegment.CreateKanji(text),
            _ => RmQrSegment.CreateBytes(QrEncoding.Encode(text, textEncoding))
        };
        var eci = ResolveEci(text, options, mode);
        return Encode(segment, options, eci);
    }

    internal static RmQrCode EncodeBytes(byte[] data, RmQrEncodingOptions options) {
        ValidateOptions(options);
        if (options.Mode is not RmQrEncodingMode.Auto and not RmQrEncodingMode.Byte) {
            throw new ArgumentException("A binary payload requires byte mode.", nameof(options));
        }
        return Encode(RmQrSegment.CreateBytes(data), options, options.EciAssignmentNumber);
    }

    private static RmQrCode Encode(RmQrSegment segment, RmQrEncodingOptions options, int? eciAssignmentNumber) {
        ValidateEci(eciAssignmentNumber);
        var headerBits = (options.IsGs1 ? 3 : 0) + GetEciBitLength(eciAssignmentNumber);
        var selectedVersion = 0;
        var selectedArea = int.MaxValue;
        for (var version = options.MinimumVersion; version <= options.MaximumVersion; version++) {
            var segmentBits = segment.GetBitLength(version);
            if (segmentBits == int.MaxValue) continue;
            if (headerBits + segmentBits > RmQrTables.GetDataCodewords(version, options.ErrorCorrectionLevel) * 8) continue;
            var area = RmQrTables.GetWidth(version) * RmQrTables.GetHeight(version);
            if (area >= selectedArea) continue;
            selectedVersion = version;
            selectedArea = area;
        }
        if (selectedVersion == 0) throw new ArgumentException("Input exceeds the selected rMQR version range and error correction capacity.");

        var data = BuildDataCodewords(segment, selectedVersion, options, eciAssignmentNumber);
        var full = AddEccAndInterleave(data, selectedVersion, options.ErrorCorrectionLevel);
        var modules = BuildMatrix(full, selectedVersion, options.ErrorCorrectionLevel);
        return new RmQrCode(
            selectedVersion,
            RmQrTables.GetVersionName(selectedVersion),
            options.ErrorCorrectionLevel,
            options.IsGs1,
            modules);
    }

    private static byte[] BuildDataCodewords(
        RmQrSegment segment,
        int version,
        RmQrEncodingOptions options,
        int? eciAssignmentNumber) {
        var targetBytes = RmQrTables.GetDataCodewords(version, options.ErrorCorrectionLevel);
        var buffer = new QrBitBuffer(targetBytes);
        if (options.IsGs1) buffer.AppendBits(5, 3);
        if (eciAssignmentNumber.HasValue) {
            buffer.AppendBits(7, 3);
            AppendEci(buffer, eciAssignmentNumber.Value);
        }
        segment.AppendTo(buffer, version);

        var remaining = targetBytes * 8 - buffer.LengthBits;
        buffer.AppendBits(0, Math.Min(3, remaining));
        var alignment = (8 - (buffer.LengthBits & 7)) & 7;
        if (alignment > 0) buffer.AppendBits(0, alignment);
        var data = buffer.ToByteArray();
        if (data.Length == targetBytes) return data;
        var result = new byte[targetBytes];
        Buffer.BlockCopy(data, 0, result, 0, data.Length);
        for (var i = data.Length; i < result.Length; i++) result[i] = ((i - data.Length) & 1) == 0 ? (byte)0xEC : (byte)0x11;
        return result;
    }

    private static byte[] AddEccAndInterleave(byte[] data, int version, QrErrorCorrectionLevel ecc) {
        var total = RmQrTables.GetTotalCodewords(version);
        var blocks = RmQrTables.GetBlocks(version, ecc);
        var totalEcc = total - data.Length;
        var eccPerBlock = totalEcc / blocks;
        if (eccPerBlock * blocks != totalEcc) throw new InvalidOperationException("Invalid rMQR ECC block table.");

        var shortDataLength = data.Length / blocks;
        var shortBlocks = blocks - data.Length % blocks;
        var divisor = QrReedSolomon.ComputeDivisor(eccPerBlock);
        var result = new byte[total];
        var dataOffset = 0;
        var eccStart = data.Length;
        for (var block = 0; block < blocks; block++) {
            var dataLength = shortDataLength + (block >= shortBlocks ? 1 : 0);
            for (var i = 0; i < shortDataLength; i++) result[i * blocks + block] = data[dataOffset + i];
            if (dataLength > shortDataLength) {
                result[shortDataLength * blocks + block - shortBlocks] = data[dataOffset + shortDataLength];
            }
            var remainder = QrReedSolomon.ComputeRemainder(Slice(data, dataOffset, dataLength), divisor);
            for (var i = 0; i < remainder.Length; i++) result[eccStart + i * blocks + block] = remainder[i];
            dataOffset += dataLength;
        }
        return result;
    }

    private static byte[] Slice(byte[] source, int offset, int length) {
        var result = new byte[length];
        Buffer.BlockCopy(source, offset, result, 0, length);
        return result;
    }

    private static BitMatrix BuildMatrix(byte[] codewords, int version, QrErrorCorrectionLevel ecc) {
        var width = RmQrTables.GetWidth(version);
        var height = RmQrTables.GetHeight(version);
        var modules = new BitMatrix(width, height);
        var function = new BitMatrix(width, height);
        RmQrMatrix.SetupFunctionPatterns(modules, function);
        RmQrMatrix.PopulateData(modules, function, codewords);
        RmQrMatrix.ApplyMask(modules, function);
        RmQrMatrix.DrawFormatInformation(modules, version, ecc);
        return modules;
    }

    private static RmQrMode ResolveMode(string text, RmQrEncodingOptions options) {
        if (options.IsGs1) return RmQrMode.Byte;
        return options.Mode switch {
            RmQrEncodingMode.Numeric => RmQrMode.Numeric,
            RmQrEncodingMode.Alphanumeric => RmQrMode.Alphanumeric,
            RmQrEncodingMode.Byte => RmQrMode.Byte,
            RmQrEncodingMode.Kanji => RmQrMode.Kanji,
            RmQrEncodingMode.Auto when RmQrSegment.IsNumeric(text) => RmQrMode.Numeric,
            RmQrEncodingMode.Auto when RmQrSegment.IsAlphanumeric(text) => RmQrMode.Alphanumeric,
            RmQrEncodingMode.Auto when RmQrSegment.IsKanji(text) => RmQrMode.Kanji,
            RmQrEncodingMode.Auto => RmQrMode.Byte,
            _ => throw new ArgumentOutOfRangeException(nameof(options.Mode))
        };
    }

    private static int? ResolveEci(string text, RmQrEncodingOptions options, RmQrMode mode) {
        if (options.EciAssignmentNumber.HasValue) return options.EciAssignmentNumber;
        if (mode != RmQrMode.Byte || options.TextEncoding == QrTextEncoding.Latin1) return null;
        if (IsAscii(text)) return null;
        return QrEncoding.TryGetEciAssignment(options.TextEncoding, out var assignment) ? assignment : null;
    }

    private static QrTextEncoding ResolveTextEncoding(RmQrEncodingOptions options, RmQrMode mode) {
        if (mode != RmQrMode.Byte || !options.EciAssignmentNumber.HasValue) return options.TextEncoding;
        if (QrEncoding.TryGetTextEncoding(options.EciAssignmentNumber.Value, out var encoding)) return encoding;
        throw new InvalidOperationException(
            $"ECI {options.EciAssignmentNumber.Value} has no known rMQR text encoding. Encode bytes directly for custom ECI assignments.");
    }

    private static bool IsAscii(string text) {
        for (var i = 0; i < text.Length; i++) if (text[i] > 0x7F) return false;
        return true;
    }

    private static int GetEciBitLength(int? assignment) {
        if (!assignment.HasValue) return 0;
        return 3 + (assignment.Value <= 0x7F ? 8 : assignment.Value <= 0x3FFF ? 16 : 24);
    }

    private static void AppendEci(QrBitBuffer buffer, int assignment) {
        if (assignment <= 0x7F) buffer.AppendBits(assignment, 8);
        else if (assignment <= 0x3FFF) buffer.AppendBits(0x8000 | assignment, 16);
        else buffer.AppendBits(0xC00000 | assignment, 24);
    }

    private static void ValidateOptions(RmQrEncodingOptions options) {
        if (options is null) throw new ArgumentNullException(nameof(options));
        RmQrTables.ValidateEcc(options.ErrorCorrectionLevel);
        if (options.MinimumVersion is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(options.MinimumVersion));
        if (options.MaximumVersion is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(options.MaximumVersion));
        if (options.MinimumVersion > options.MaximumVersion) throw new ArgumentOutOfRangeException(nameof(options.MinimumVersion));
        if (!Enum.IsDefined(typeof(RmQrEncodingMode), options.Mode)) throw new ArgumentOutOfRangeException(nameof(options.Mode));
        if (!Enum.IsDefined(typeof(QrTextEncoding), options.TextEncoding)) throw new ArgumentOutOfRangeException(nameof(options.TextEncoding));
        ValidateEci(options.EciAssignmentNumber);
    }

    private static void ValidateEci(int? assignment) {
        if (assignment.HasValue && assignment.Value is < 0 or > 999999) {
            throw new ArgumentOutOfRangeException(nameof(assignment), "ECI assignment numbers must be between 0 and 999999.");
        }
    }
}
