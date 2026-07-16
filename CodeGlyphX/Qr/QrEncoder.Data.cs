using System;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Qr;

internal static partial class QrEncoder {
    public static CodeGlyphX.QrCode EncodeText(string text, QrEncodingOptions options, QrStructuredAppend? structuredAppend = null) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        if (options is null) throw new ArgumentNullException(nameof(options));
        ValidateCommonOptions(options.ErrorCorrectionLevel, options.MinVersion, options.MaxVersion, options.ForceMask);
        ValidateFnc1(options.Fnc1Mode, options.Fnc1ApplicationIndicator);
        if (options.EciMode is < QrEciMode.Auto or > QrEciMode.Never) throw new ArgumentOutOfRangeException(nameof(options.EciMode));
        if (options.TextEncoding is < QrTextEncoding.Latin1 or > QrTextEncoding.ShiftJis) throw new ArgumentOutOfRangeException(nameof(options.TextEncoding));
        if (structuredAppend is not null && !structuredAppend.Value.IsValid)
            throw new ArgumentOutOfRangeException(nameof(structuredAppend));
        if (!QrEncoding.CanEncode(text, options.TextEncoding))
            throw new ArgumentException($"Text cannot be encoded as {options.TextEncoding}.", nameof(text));

        var eciAssignmentNumber = ResolveEciAssignment(text, options.TextEncoding, options.EciMode);
        QrSegmentPlan? plan = null;
        var planGroup = -1;
        var version = 0;

        for (var candidate = options.MinVersion; candidate <= options.MaxVersion; candidate++) {
            var group = candidate <= 9 ? 0 : candidate <= 26 ? 1 : 2;
            if (plan is null || group != planGroup) {
                plan = options.OptimizeSegments
                    ? QrSegmentPlanner.Plan(text, options.TextEncoding, candidate, options.Fnc1Mode, eciAssignmentNumber)
                    : QrSegmentPlanner.CreateBytePlan(QrEncoding.Encode(text, options.TextEncoding), candidate, eciAssignmentNumber);
                planGroup = group;
            }

            var requiredBits = plan.TotalBitLength
                + GetStructuredAppendBitLength(structuredAppend)
                + GetFnc1BitLength(options.Fnc1Mode);
            if (requiredBits <= QrTables.GetNumDataCodewords(candidate, options.ErrorCorrectionLevel) * 8) {
                version = candidate;
                break;
            }
        }

        if (version == 0)
            throw new ArgumentException($"Data too long for QR version range {options.MinVersion}..{options.MaxVersion} at ECC {options.ErrorCorrectionLevel}.", nameof(text));

        return EncodeSegments(
            plan!.Segments,
            version,
            options.ErrorCorrectionLevel,
            options.ForceMask,
            plan.HasByteSegment ? eciAssignmentNumber : null,
            structuredAppend,
            options.Fnc1Mode,
            options.Fnc1ApplicationIndicator);
    }

    public static CodeGlyphX.QrCode EncodeByteMode(byte[] data, QrErrorCorrectionLevel ecc, int minVersion, int maxVersion, int? forceMask, int? eciAssignmentNumber = null, QrStructuredAppend? structuredAppend = null) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        ValidateCommonOptions(ecc, minVersion, maxVersion, forceMask);
        ValidateEciAssignment(eciAssignmentNumber);
        if (structuredAppend is not null && !structuredAppend.Value.IsValid)
            throw new ArgumentOutOfRangeException(nameof(structuredAppend));

        for (var version = minVersion; version <= maxVersion; version++) {
            var plan = QrSegmentPlanner.CreateBytePlan(data, version, eciAssignmentNumber);
            if (plan.TotalBitLength + GetStructuredAppendBitLength(structuredAppend) <= QrTables.GetNumDataCodewords(version, ecc) * 8)
                return EncodeSegments(plan.Segments, version, ecc, forceMask, eciAssignmentNumber, structuredAppend, QrFnc1Mode.None, null);
        }

        throw new ArgumentException($"Data too long for QR version range {minVersion}..{maxVersion} at ECC {ecc}.", nameof(data));
    }

    public static CodeGlyphX.QrCode EncodeKanjiMode(string text, QrErrorCorrectionLevel ecc, int minVersion, int maxVersion, int? forceMask) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        if (text.Length == 0) throw new ArgumentException("QR Kanji mode requires at least one character.", nameof(text));
        ValidateCommonOptions(ecc, minVersion, maxVersion, forceMask);
        var segment = QrSegment.CreateKanji(text);

        for (var version = minVersion; version <= maxVersion; version++) {
            var countBits = QrSegment.GetCharacterCountBitLength(QrSegmentMode.Kanji, version);
            if (segment.CharacterCount >= (1 << countBits)) continue;
            if (segment.GetTotalBitLength(version) <= QrTables.GetNumDataCodewords(version, ecc) * 8)
                return EncodeSegments(new[] { segment }, version, ecc, forceMask, null, null, QrFnc1Mode.None, null);
        }

        throw new ArgumentException($"Data too long for QR version range {minVersion}..{maxVersion} at ECC {ecc}.", nameof(text));
    }

    private static CodeGlyphX.QrCode EncodeSegments(
        QrSegment[] segments,
        int version,
        QrErrorCorrectionLevel ecc,
        int? forceMask,
        int? eciAssignmentNumber,
        QrStructuredAppend? structuredAppend,
        QrFnc1Mode fnc1Mode,
        int? fnc1ApplicationIndicator) {
        var dataCodewords = EncodeData(segments, version, ecc, eciAssignmentNumber, structuredAppend, fnc1Mode, fnc1ApplicationIndicator);
        var allCodewords = AddEccAndInterleave(dataCodewords, version, ecc);
        return Render(version, ecc, forceMask, allCodewords);
    }

    private static byte[] EncodeData(
        QrSegment[] segments,
        int version,
        QrErrorCorrectionLevel ecc,
        int? eciAssignmentNumber,
        QrStructuredAppend? structuredAppend,
        QrFnc1Mode fnc1Mode,
        int? fnc1ApplicationIndicator) {
        var dataCapacityBits = QrTables.GetNumDataCodewords(version, ecc) * 8;
        var buffer = new QrBitBuffer((dataCapacityBits + 7) / 8);

        if (structuredAppend is not null) {
            var metadata = structuredAppend.Value;
            buffer.AppendBits(0b0011, 4);
            buffer.AppendBits(((metadata.Index - 1) << 4) | (metadata.Total - 1), 8);
            buffer.AppendBits(metadata.Parity, 8);
        }

        if (fnc1Mode == QrFnc1Mode.FirstPosition) {
            buffer.AppendBits(0b0101, 4);
        } else if (fnc1Mode == QrFnc1Mode.SecondPosition) {
            buffer.AppendBits(0b1001, 4);
            buffer.AppendBits(fnc1ApplicationIndicator!.Value, 8);
        }

        if (eciAssignmentNumber is not null) {
            buffer.AppendBits(0b0111, 4);
            AppendEciAssignmentNumber(buffer, eciAssignmentNumber.Value);
        }

        for (var i = 0; i < segments.Length; i++) segments[i].AppendTo(buffer, version);

        var remaining = dataCapacityBits - buffer.LengthBits;
        buffer.AppendBits(0, Math.Min(4, Math.Max(0, remaining)));
        while ((buffer.LengthBits & 7) != 0) buffer.AppendBit(false);

        var codewords = buffer.ToByteArray();
        var dataCodewordCount = QrTables.GetNumDataCodewords(version, ecc);
        if (codewords.Length > dataCodewordCount) throw new InvalidOperationException("Encoded data exceeds capacity.");
        if (codewords.Length == dataCodewordCount) return codewords;

        var result = new byte[dataCodewordCount];
        Array.Copy(codewords, result, codewords.Length);
        var pad = 0xEC;
        for (var i = codewords.Length; i < result.Length; i++) {
            result[i] = (byte)pad;
            pad = pad == 0xEC ? 0x11 : 0xEC;
        }
        return result;
    }

    private static CodeGlyphX.QrCode Render(int version, QrErrorCorrectionLevel ecc, int? forceMask, byte[] allCodewords) {
        var size = version * 4 + 17;
        var modules = new BitMatrix(size, size);
        var isFunction = new BitMatrix(size, size);
        DrawFunctionPatterns(version, ecc, modules, isFunction);
        DrawCodewords(allCodewords, modules, isFunction);

        if (forceMask is not null) {
            ApplyMask(forceMask.Value, modules, isFunction);
            DrawFormatBits(ecc, forceMask.Value, modules, isFunction);
            return new CodeGlyphX.QrCode(version, ecc, forceMask.Value, modules);
        }

        var bestMask = -1;
        var scratch = modules.Clone();
        var bestPenalty = int.MaxValue;
        for (var mask = 0; mask <= 7; mask++) {
            scratch.CopyFrom(modules);
            ApplyMask(mask, scratch, isFunction);
            DrawFormatBits(ecc, mask, scratch, isFunction);
            var penalty = QrMask.ComputePenalty(scratch);
            if (penalty < bestPenalty) {
                bestPenalty = penalty;
                bestMask = mask;
            }
        }

        if (bestMask < 0) throw new InvalidOperationException("Failed to choose mask.");
        ApplyMask(bestMask, modules, isFunction);
        DrawFormatBits(ecc, bestMask, modules, isFunction);
        return new CodeGlyphX.QrCode(version, ecc, bestMask, modules);
    }

    private static int? ResolveEciAssignment(string text, QrTextEncoding encoding, QrEciMode mode) {
        if (mode == QrEciMode.Never) return null;
        if (mode == QrEciMode.Auto) {
            if (encoding == QrTextEncoding.Latin1 || !ContainsNonAscii(text)) return null;
        }
        return QrEncoding.TryGetEciAssignment(encoding, out var assignment) ? assignment : null;
    }

    private static bool ContainsNonAscii(string text) {
        for (var i = 0; i < text.Length; i++) if (text[i] > 0x7F) return true;
        return false;
    }

    private static int GetStructuredAppendBitLength(QrStructuredAppend? structuredAppend) => structuredAppend is null ? 0 : 20;

    private static int GetFnc1BitLength(QrFnc1Mode fnc1Mode) {
        return fnc1Mode switch {
            QrFnc1Mode.None => 0,
            QrFnc1Mode.FirstPosition => 4,
            QrFnc1Mode.SecondPosition => 12,
            _ => throw new ArgumentOutOfRangeException(nameof(fnc1Mode))
        };
    }

    private static void AppendEciAssignmentNumber(QrBitBuffer buffer, int assignmentNumber) {
        if (assignmentNumber <= 0x7F) {
            buffer.AppendBits(assignmentNumber, 8);
            return;
        }
        if (assignmentNumber <= 0x3FFF) {
            buffer.AppendBits(0b1000_0000 | ((assignmentNumber >> 8) & 0b0011_1111), 8);
            buffer.AppendBits(assignmentNumber & 0xFF, 8);
            return;
        }
        buffer.AppendBits(0b1100_0000 | ((assignmentNumber >> 16) & 0b0001_1111), 8);
        buffer.AppendBits((assignmentNumber >> 8) & 0xFF, 8);
        buffer.AppendBits(assignmentNumber & 0xFF, 8);
    }

    private static void ValidateCommonOptions(QrErrorCorrectionLevel ecc, int minVersion, int maxVersion, int? forceMask) {
        if (ecc is < QrErrorCorrectionLevel.L or > QrErrorCorrectionLevel.H) throw new ArgumentOutOfRangeException(nameof(ecc));
        if (minVersion is < 1 or > 40) throw new ArgumentOutOfRangeException(nameof(minVersion));
        if (maxVersion is < 1 or > 40) throw new ArgumentOutOfRangeException(nameof(maxVersion));
        if (minVersion > maxVersion) throw new ArgumentOutOfRangeException(nameof(minVersion));
        if (forceMask is not null && forceMask.Value is < 0 or > 7) throw new ArgumentOutOfRangeException(nameof(forceMask));
    }

    private static void ValidateEciAssignment(int? eciAssignmentNumber) {
        if (eciAssignmentNumber is < 0 or > 999999) throw new ArgumentOutOfRangeException(nameof(eciAssignmentNumber));
    }

    private static void ValidateFnc1(QrFnc1Mode fnc1Mode, int? applicationIndicator) {
        if (fnc1Mode is < QrFnc1Mode.None or > QrFnc1Mode.SecondPosition) throw new ArgumentOutOfRangeException(nameof(fnc1Mode));
        if (fnc1Mode == QrFnc1Mode.SecondPosition) {
            if (applicationIndicator is null or < 0 or > 255)
                throw new ArgumentOutOfRangeException(nameof(applicationIndicator), "FNC1 second position requires an application indicator from 0 through 255.");
        } else if (applicationIndicator is not null) {
            throw new ArgumentException("An FNC1 application indicator is valid only in second position.", nameof(applicationIndicator));
        }
    }
}
