using System;
using System.Collections.Generic;
using System.Text;

namespace CodeGlyphX.DataBar;

/// <summary>
/// Decodes GS1 DataBar Expanded symbols (linear and stacked).
/// </summary>
public static class DataBarExpandedDecoder {
    private const char GroupSeparator = Gs1.GroupSeparator;

    /// <summary>
    /// Attempts to decode a GS1 DataBar Expanded symbol from a <see cref="Barcode1D"/>.
    /// </summary>
    public static bool TryDecodeExpanded(Barcode1D barcode, out string content) {
        content = string.Empty;
        if (barcode is null) return false;
        if (!TryExtractWidths(barcode, out var widths)) return false;
        return TryDecodeFromWidths(widths, out content);
    }

    /// <summary>
    /// Attempts to decode a GS1 DataBar Expanded symbol from module data.
    /// </summary>
    public static bool TryDecodeExpanded(bool[] modules, out string content) {
        content = string.Empty;
        if (modules is null || modules.Length == 0) return false;
        if (!TryExtractWidths(modules, out var widths)) return false;
        return TryDecodeFromWidths(widths, out content);
    }

    /// <summary>
    /// Attempts to decode a GS1 DataBar Expanded Stacked symbol from a <see cref="BitMatrix"/>.
    /// </summary>
    public static bool TryDecodeExpandedStacked(BitMatrix modules, out string content) {
        content = string.Empty;
        if (modules is null || modules.Width <= 0 || modules.Height <= 0) return false;
        if ((modules.Height - 1) % 4 != 0) return false;

        if (!TryExtractStackedRows(modules, out var rowRuns, out var maxBlocks)) return false;
        if (!TryBuildStackedElements(rowRuns, maxBlocks, out var elements)) return false;
        if (!TryTrimElements(elements, out var trimmed)) return false;
        return TryDecodeFromWidths(trimmed, out content);
    }

    private static bool TryDecodeFromWidths(int[] widths, out string content) {
        content = string.Empty;
        if (widths.Length < 20) return false;

        var candidates = GetTotalCharsCandidates(widths.Length);
        if (candidates.Count == 0) return false;

        foreach (var totalChars in candidates) {
            if (TryDecodeWithTotalChars(widths, totalChars, out content)) return true;
        }
        return false;
    }

    private static List<int> GetTotalCharsCandidates(int patternWidth) {
        var candidates = new List<int>(2);
        for (var totalChars = 2; totalChars <= 21; totalChars++) {
            var finderCount = (totalChars / 2) + (totalChars & 1);
            var expectedWidth = (finderCount * 5) + (totalChars * 8) + 4;
            if (expectedWidth == patternWidth) candidates.Add(totalChars);
        }
        return candidates;
    }

    private static bool TryDecodeWithTotalChars(int[] elements, int totalChars, out string content) {
        content = string.Empty;
        var dataChars = totalChars - 1;
        if (dataChars <= 0) return false;

        if (!HasValidGuards(elements)) return false;
        if (!TryBuildDataWidths(elements, dataChars, out var dataWidths, out var checkWidths)) return false;
        if (!TryDecodeDataValues(dataWidths, out var dataValues)) return false;
        if (!TryDecodeDataCharacter(checkWidths, out var checkChar)) return false;
        if (!HasValidChecksum(dataWidths, dataChars, checkChar)) return false;
        var bits = BuildDataBits(dataValues);
        return TryDecodePayload(bits, out content);
    }

    private static bool TryDecodePayload(bool[] bits, out string content) {
        content = string.Empty;
        if (bits.Length < 4) return false;

        if (!TryGetEncodingMethod(bits, out var encodingMethod, out var headerSize)) return false;

        var sb = new StringBuilder();
        var pos = headerSize;

        if (!TryDecodeByMethod(bits, encodingMethod, ref pos, sb)) return false;
        content = sb.ToString();
        return true;
    }

    private static bool TryDecodeGtinWithFirstDigit(bool[] bits, ref int pos, StringBuilder sb, bool includeAi) {
        if (pos + 4 + 40 > bits.Length) return false;
        if (includeAi) sb.Append("01");
        var firstDigit = ReadBits(bits, pos, 4);
        if (firstDigit < 0 || firstDigit > 9) return false;
        pos += 4;

        var digits = new int[13];
        digits[0] = firstDigit;
        if (!ReadGtinGroups(bits, ref pos, digits, 1)) return false;

        AppendDigits(sb, digits, 13);
        var check = ComputeGtinCheckDigit(digits, 13);
        sb.Append((char)('0' + check));
        return true;
    }

    private static bool TryDecodeGtinConstantNine(bool[] bits, ref int pos, StringBuilder sb, bool includeAi) {
        if (pos + 40 > bits.Length) return false;
        if (includeAi) sb.Append("01");
        var digits = new int[13];
        digits[0] = 9;
        if (!ReadGtinGroups(bits, ref pos, digits, 1)) return false;

        AppendDigits(sb, digits, 13);
        var check = ComputeGtinCheckDigit(digits, 13);
        sb.Append((char)('0' + check));
        return true;
    }

    private static bool ReadGtinGroups(bool[] bits, ref int pos, int[] digitsOut, int startIndex) {
        if (digitsOut.Length - startIndex < 12) return false;
        for (var i = 0; i < 4; i++) {
            var group = ReadBits(bits, pos, 10);
            if (group < 0 || group > 999) return false;
            pos += 10;
            var offset = startIndex + (i * 3);
            digitsOut[offset] = group / 100;
            digitsOut[offset + 1] = (group / 10) % 10;
            digitsOut[offset + 2] = group % 10;
        }
        return true;
    }

    private static int ComputeGtinCheckDigit(int[] digits, int length) {
        var sum = 0;
        for (var i = 0; i < length; i++) {
            var digit = digits[i];
            sum += (i % 2 == 0) ? (3 * digit) : digit;
        }
        var check = 10 - (sum % 10);
        return check == 10 ? 0 : check;
    }

    private static void AppendDigits(StringBuilder sb, int[] digits, int length) {
        for (var i = 0; i < length; i++) {
            sb.Append((char)('0' + digits[i]));
        }
    }

    private static bool TryDecodeWeight3103(bool[] bits, ref int pos, StringBuilder sb) {
        if (pos + 15 > bits.Length) return false;
        var weight = ReadBits(bits, pos, 15);
        pos += 15;
        sb.Append("3103");
        AppendFixedDigits(sb, weight, 6);
        return true;
    }

    private static bool TryDecodeWeight320x(bool[] bits, ref int pos, StringBuilder sb) {
        if (pos + 15 > bits.Length) return false;
        var weight = ReadBits(bits, pos, 15);
        pos += 15;
        if (weight < 10000) {
            sb.Append("3202");
            AppendFixedDigits(sb, weight, 6);
        } else {
            sb.Append("3203");
            AppendFixedDigits(sb, weight - 10000, 6);
        }
        return true;
    }

    private static bool TryDecodeAi392x(bool[] bits, ref int pos, StringBuilder sb) {
        if (pos + 2 > bits.Length) return false;
        var lastDigit = ReadBits(bits, pos, 2);
        pos += 2;
        sb.Append("392");
        sb.Append((char)('0' + lastDigit));
        return true;
    }

    private static bool TryDecodeAi393x(bool[] bits, ref int pos, StringBuilder sb) {
        if (pos + 12 > bits.Length) return false;
        var lastDigit = ReadBits(bits, pos, 2);
        pos += 2;
        sb.Append("393");
        sb.Append((char)('0' + lastDigit));
        var currency = ReadBits(bits, pos, 10);
        pos += 10;
        AppendFixedDigits(sb, currency, 3);
        return true;
    }

    private static bool TryDecodeWeightAndDate(bool[] bits, ref int pos, StringBuilder sb, int encodingMethod) {
        if (pos + 36 > bits.Length) return false;
        var firstAi = encodingMethod switch {
            7 or 9 or 11 or 13 => "310",
            8 or 10 or 12 or 14 => "320",
            _ => null
        };
        var dateAi = encodingMethod switch {
            7 or 8 => "11",
            9 or 10 => "13",
            11 or 12 => "15",
            13 or 14 => "17",
            _ => null
        };
        if (firstAi is null || dateAi is null) return false;

        var weight = ReadBits(bits, pos, 20);
        pos += 20;
        var lastAiDigit = weight / 100000;
        var weightValue = weight % 100000;
        sb.Append(firstAi);
        sb.Append((char)('0' + lastAiDigit));
        AppendFixedDigits(sb, weightValue, 6);

        var dateValue = ReadBits(bits, pos, 16);
        pos += 16;
        if (dateValue != 38400) {
            var day = dateValue % 32;
            dateValue /= 32;
            var month = (dateValue % 12) + 1;
            var year = dateValue / 12;
            sb.Append(dateAi);
            AppendFixedDigits(sb, year, 2);
            AppendFixedDigits(sb, month, 2);
            AppendFixedDigits(sb, day, 2);
        }
        return true;
    }

    private static bool TryDecodeGeneralField(bool[] bits, int pos, StringBuilder sb) {
        var mode = GeneralFieldMode.Numeric;
        var size = bits.Length;

        while (pos < size) {
            switch (mode) {
                case GeneralFieldMode.Numeric:
                    if (!TryDecodeNumeric(bits, ref pos, sb, size, ref mode)) return false;
                    break;
                case GeneralFieldMode.Alpha:
                    if (!TryDecodeAlpha(bits, ref pos, sb, size, ref mode)) return false;
                    break;
                default:
                    if (!TryDecodeIso(bits, ref pos, sb, size, ref mode)) return false;
                    break;
            }
        }

        return true;
    }

    private static bool TryExtractStackedRows(BitMatrix modules, out List<int[]> rowRuns, out int maxBlocks) {
        rowRuns = new List<int[]>();
        maxBlocks = 0;
        var dataRows = new List<int>();
        for (var row = 0; row < modules.Height; row += 4) dataRows.Add(row);
        if (dataRows.Count == 0) return false;

        rowRuns = new List<int[]>(dataRows.Count);
        foreach (var rowIndex in dataRows) {
            if (!TryExtractWidths(GetRow(modules, rowIndex), out var widths)) return false;
            if (widths.Length < 4) return false;
            var blocks = (widths.Length - 4) / 21;
            if ((widths.Length - 4) % 21 != 0) return false;
            if (blocks > maxBlocks) maxBlocks = blocks;
            rowRuns.Add(widths);
        }
        return maxBlocks > 0;
    }

    private static bool TryBuildStackedElements(List<int[]> rowRuns, int maxBlocks, out int[] elements) {
        elements = new int[2 + (maxBlocks * rowRuns.Count * 21) + 2];
        Array.Clear(elements, 0, elements.Length);

        var currentBlock = 0;
        for (var rowIndex = 0; rowIndex < rowRuns.Count; rowIndex++) {
            var runs = rowRuns[rowIndex];
            var blocks = (runs.Length - 4) / 21;
            var currentRow = rowIndex + 1;
            var leftToRight = (currentRow % 2 == 1) || (maxBlocks % 2 == 1);
            if (runs[0] == 2) leftToRight = true;

            for (var blockIndex = 0; blockIndex < blocks; blockIndex++) {
                var blockStart = 2 + (blockIndex * 21);
                var targetBlock = leftToRight ? currentBlock + blockIndex : currentBlock + (blocks - 1 - blockIndex);
                var targetStart = 2 + (targetBlock * 21);
                if (targetStart + 20 >= elements.Length) return false;
                CopyBlock(elements, runs, blockStart, targetStart, leftToRight);
            }
            currentBlock += blocks;
        }
        return true;
    }

    private static void CopyBlock(int[] elements, int[] runs, int blockStart, int targetStart, bool leftToRight) {
        if (leftToRight) {
            for (var j = 0; j < 21; j++) {
                elements[targetStart + j] = runs[blockStart + j];
            }
        } else {
            for (var j = 0; j < 21; j++) {
                elements[targetStart + j] = runs[blockStart + (20 - j)];
            }
        }
    }

    private static bool TryTrimElements(int[] elements, out int[] trimmed) {
        trimmed = Array.Empty<int>();
        var lastDataIndex = FindLastNonZeroIndex(elements);
        if (lastDataIndex < 0) return false;
        var patternWidth = lastDataIndex + 3;
        if (patternWidth < 4) return false;
        trimmed = new int[patternWidth];
        Array.Copy(elements, trimmed, Math.Min(elements.Length, trimmed.Length));
        trimmed[0] = 1;
        trimmed[1] = 1;
        trimmed[trimmed.Length - 2] = 1;
        trimmed[trimmed.Length - 1] = 1;
        return true;
    }

    private static int FindLastNonZeroIndex(int[] elements) {
        for (var i = elements.Length - 1; i >= 0; i--) {
            if (elements[i] != 0) return i;
        }
        return -1;
    }

    private static bool HasValidGuards(int[] elements) {
        return elements.Length >= 10
            && elements[0] == 1
            && elements[1] == 1
            && elements[elements.Length - 1] == 1
            && elements[elements.Length - 2] == 1;
    }

    private static bool TryBuildDataWidths(int[] elements, int dataChars, out int[][] dataWidths, out int[] checkWidths) {
        dataWidths = new int[dataChars][];
        for (var i = 0; i < dataChars; i++) dataWidths[i] = new int[8];
        checkWidths = new int[8];
        for (var i = 0; i < 8; i++) checkWidths[i] = elements[i + 2];

        for (var i = 1; i < dataChars; i += 2) {
            var baseIndex = (((i - 1) / 2) * 21) + 23;
            if (baseIndex + 7 >= elements.Length) return false;
            for (var j = 0; j < 8; j++) {
                dataWidths[i][j] = elements[baseIndex + j];
            }
        }

        for (var i = 0; i < dataChars; i += 2) {
            var baseIndex = ((i / 2) * 21) + 15;
            if (baseIndex + 7 >= elements.Length) return false;
            for (var j = 0; j < 8; j++) {
                dataWidths[i][j] = elements[baseIndex + (7 - j)];
            }
        }
        return true;
    }

    private static bool TryDecodeDataValues(int[][] dataWidths, out int[] dataValues) {
        dataValues = new int[dataWidths.Length];
        for (var i = 0; i < dataWidths.Length; i++) {
            if (!TryDecodeDataCharacter(dataWidths[i], out var value)) return false;
            dataValues[i] = value;
        }
        return true;
    }

    private static bool HasValidChecksum(int[][] dataWidths, int dataChars, int checkChar) {
        var checksum = 0;
        for (var i = 0; i < dataChars; i++) {
            var row = DataBarExpandedTables.WEIGHT_ROWS[(((dataChars - 2) / 2) * 21) + i];
            for (var j = 0; j < 8; j++) {
                checksum += dataWidths[i][j] * DataBarExpandedTables.CHECKSUM_WEIGHT_EXP[(row * 8) + j];
            }
        }
        var expectedCheck = (211 * ((dataChars + 1) - 4)) + (checksum % 211);
        return checkChar == expectedCheck;
    }

    private static bool[] BuildDataBits(int[] dataValues) {
        var bits = new bool[dataValues.Length * 12];
        var pos = 0;
        for (var i = 0; i < dataValues.Length; i++) {
            var value = dataValues[i];
            for (var bit = 11; bit >= 0; bit--) {
                bits[pos++] = ((value >> bit) & 1) == 1;
            }
        }
        return bits;
    }

    private static bool TryGetEncodingMethod(bool[] bits, out int encodingMethod, out int headerSize) {
        encodingMethod = 0;
        headerSize = 0;
        if (bits.Length > 1 && bits[1]) {
            encodingMethod = 1;
            headerSize = 4;
            return true;
        }
        if (bits.Length > 2 && !bits[2]) {
            encodingMethod = 2;
            headerSize = 5;
            return true;
        }
        if (bits.Length > 3 && !bits[3]) {
            var value = ReadBits(bits, 1, 4);
            encodingMethod = value == 4 ? 3 : 4;
            headerSize = 5;
            return true;
        }
        var five = ReadBits(bits, 1, 5);
        if (five == 12) {
            encodingMethod = 5;
            headerSize = 8;
            return true;
        }
        if (five == 13) {
            encodingMethod = 6;
            headerSize = 8;
            return true;
        }
        var seven = ReadBits(bits, 1, 7);
        if (seven < 56 || seven > 63) return false;
        encodingMethod = 7 + (seven - 56);
        headerSize = 8;
        return true;
    }

    private static bool TryDecodeByMethod(bool[] bits, int encodingMethod, ref int pos, StringBuilder sb) {
        switch (encodingMethod) {
            case 1:
                return TryDecodeGtinWithFirstDigit(bits, ref pos, sb, includeAi: true)
                    && TryDecodeGeneralField(bits, pos, sb);
            case 2:
                return TryDecodeGeneralField(bits, pos, sb);
            case 3:
                return TryDecodeGtinConstantNine(bits, ref pos, sb, includeAi: true)
                    && TryDecodeWeight3103(bits, ref pos, sb);
            case 4:
                return TryDecodeGtinConstantNine(bits, ref pos, sb, includeAi: true)
                    && TryDecodeWeight320x(bits, ref pos, sb);
            case 5:
                return TryDecodeGtinConstantNine(bits, ref pos, sb, includeAi: true)
                    && TryDecodeAi392x(bits, ref pos, sb)
                    && TryDecodeGeneralField(bits, pos, sb);
            case 6:
                return TryDecodeGtinConstantNine(bits, ref pos, sb, includeAi: true)
                    && TryDecodeAi393x(bits, ref pos, sb)
                    && TryDecodeGeneralField(bits, pos, sb);
            default:
                return TryDecodeGtinConstantNine(bits, ref pos, sb, includeAi: true)
                    && TryDecodeWeightAndDate(bits, ref pos, sb, encodingMethod);
        }
    }

    private static bool TryDecodeNumeric(bool[] bits, ref int pos, StringBuilder sb, int size, ref GeneralFieldMode mode) {
        if (Match(bits, pos, "0000")) {
            if (pos + 4 >= size) {
                pos = size;
                return true;
            }
            mode = GeneralFieldMode.Alpha;
            pos += 4;
            return true;
        }

        var remaining = size - pos;
        if (remaining < 7) {
            if (remaining >= 4) {
                var v4 = ReadBits(bits, pos, 4);
                if (v4 == 0) {
                    pos = size;
                    return true;
                }
                sb.Append(v4 == 10 ? GroupSeparator : (char)('0' + v4 - 1));
            }
            pos = size;
            return true;
        }

        var v7 = ReadBits(bits, pos, 7);
        var value = v7 - 8;
        if (value < 0) {
            pos = size;
            return true;
        }
        var d1 = value / 11;
        var d2 = value % 11;
        sb.Append(d1 == 10 ? GroupSeparator : (char)('0' + d1));
        sb.Append(d2 == 10 ? GroupSeparator : (char)('0' + d2));
        pos += 7;
        return true;
    }

    private static bool TryDecodeAlpha(bool[] bits, ref int pos, StringBuilder sb, int size, ref GeneralFieldMode mode) {
        if (Match(bits, pos, "000")) {
            if (pos + 3 >= size) {
                pos = size;
                return true;
            }
            mode = GeneralFieldMode.Numeric;
            pos += 3;
            return true;
        }
        if (Match(bits, pos, "00100")) {
            if (pos + 5 >= size) {
                pos = size;
                return true;
            }
            mode = GeneralFieldMode.Iso;
            pos += 5;
            return true;
        }

        if (size - pos < 5) {
            pos = size;
            return true;
        }
        var v5 = ReadBits(bits, pos, 5);
        if (v5 == 15) {
            sb.Append(GroupSeparator);
            pos += 5;
            return true;
        }
        if (v5 >= 5 && v5 <= 14) {
            sb.Append((char)('0' + (v5 - 5)));
            pos += 5;
            return true;
        }

        if (size - pos < 6) {
            pos = size;
            return true;
        }
        var v6 = ReadBits(bits, pos, 6);
        if (v6 >= 32 && v6 <= 57) {
            sb.Append((char)('A' + (v6 - 32)));
            pos += 6;
            return true;
        }
        var alphaChar = v6 switch {
            58 => '*',
            59 => ',',
            60 => '-',
            61 => '.',
            62 => '/',
            _ => '\0'
        };
        if (alphaChar == '\0') {
            pos = size;
            return true;
        }
        sb.Append(alphaChar);
        pos += 6;
        return true;
    }

    private static bool TryDecodeIso(bool[] bits, ref int pos, StringBuilder sb, int size, ref GeneralFieldMode mode) {
        if (Match(bits, pos, "000")) {
            if (pos + 3 >= size) {
                pos = size;
                return true;
            }
            mode = GeneralFieldMode.Numeric;
            pos += 3;
            return true;
        }
        if (Match(bits, pos, "00100")) {
            if (pos + 5 >= size) {
                pos = size;
                return true;
            }
            mode = GeneralFieldMode.Alpha;
            pos += 5;
            return true;
        }

        if (size - pos < 5) {
            pos = size;
            return true;
        }
        var iso5 = ReadBits(bits, pos, 5);
        if (iso5 == 15) {
            sb.Append(GroupSeparator);
            pos += 5;
            return true;
        }
        if (iso5 >= 5 && iso5 <= 14) {
            sb.Append((char)('0' + (iso5 - 5)));
            pos += 5;
            return true;
        }

        if (size - pos >= 7) {
            var v7 = ReadBits(bits, pos, 7);
            if (v7 >= 64 && v7 <= 89) {
                sb.Append((char)(v7 + 1));
                pos += 7;
                return true;
            }
            if (v7 >= 90 && v7 <= 115) {
                sb.Append((char)(v7 + 7));
                pos += 7;
                return true;
            }
        }

        if (size - pos < 8) {
            pos = size;
            return true;
        }
        var v8 = ReadBits(bits, pos, 8);
        var isoChar = v8 switch {
            232 => '!',
            233 => '"',
            234 => '%',
            235 => '&',
            236 => '\'',
            237 => '(',
            238 => ')',
            239 => '*',
            240 => '+',
            241 => ',',
            242 => '-',
            243 => '.',
            244 => '/',
            245 => ':',
            246 => ';',
            247 => '<',
            248 => '=',
            249 => '>',
            250 => '?',
            251 => '_',
            252 => ' ',
            _ => '\0'
        };
        if (isoChar == '\0') {
            pos = size;
            return true;
        }
        sb.Append(isoChar);
        pos += 8;
        return true;
    }

    private static bool Match(bool[] bits, int pos, string pattern) {
        if (pos + pattern.Length > bits.Length) return false;
        for (var i = 0; i < pattern.Length; i++) {
            var bit = bits[pos + i];
            if ((pattern[i] == '1') != bit) return false;
        }
        return true;
    }

    private static int ReadBits(bool[] bits, int pos, int count) {
        var value = 0;
        for (var i = 0; i < count; i++) {
            value <<= 1;
            if (pos + i < bits.Length && bits[pos + i]) value |= 1;
        }
        return value;
    }

    private static void AppendFixedDigits(StringBuilder sb, int value, int width) {
        var buffer = new char[width];
        for (var i = width - 1; i >= 0; i--) {
            buffer[i] = (char)('0' + (value % 10));
            value /= 10;
        }
        sb.Append(buffer);
    }

    private enum GeneralFieldMode {
        Numeric,
        Alpha,
        Iso
    }

    private static bool TryDecodeDataCharacter(int[] widths, out int value) {
        value = 0;
        Span<int> odd = stackalloc int[4];
        Span<int> even = stackalloc int[4];
        odd[0] = widths[0];
        odd[1] = widths[2];
        odd[2] = widths[4];
        odd[3] = widths[6];
        even[0] = widths[1];
        even[1] = widths[3];
        even[2] = widths[5];
        even[3] = widths[7];

        for (var group = 1; group <= 5; group++) {
            var vOdd = DataBarCommon.GetValue(odd, DataBarExpandedTables.MODULES_ODD_EXP[group - 1], 4, DataBarExpandedTables.WIDEST_ODD_EXP[group - 1], 0);
            var vEven = DataBarCommon.GetValue(even, DataBarExpandedTables.MODULES_EVEN_EXP[group - 1], 4, DataBarExpandedTables.WIDEST_EVEN_EXP[group - 1], 1);
            if (vOdd < 0 || vEven < 0) continue;
            var candidate = (vOdd * DataBarExpandedTables.T_EVEN_EXP[group - 1]) + vEven + DataBarExpandedTables.G_SUM_EXP[group - 1];
            if (candidate >= DataBarExpandedTables.GroupMin(group) && candidate <= DataBarExpandedTables.GroupMax(group)) {
                value = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool TryExtractWidths(Barcode1D barcode, out int[] widths) {
        widths = Array.Empty<int>();
        if (barcode.Segments.Count < 10) return false;
        widths = new int[barcode.Segments.Count];
        for (var i = 0; i < barcode.Segments.Count; i++) {
            widths[i] = barcode.Segments[i].Modules;
        }
        return true;
    }

    private static bool TryExtractWidths(bool[] modules, out int[] widths) {
        widths = Array.Empty<int>();
        if (modules.Length < 2) return false;
        var runs = new List<int>();
        var current = modules[0];
        var count = 1;
        for (var i = 1; i < modules.Length; i++) {
            if (modules[i] == current) {
                count++;
            } else {
                runs.Add(count);
                current = modules[i];
                count = 1;
            }
        }
        runs.Add(count);
        widths = runs.ToArray();
        return true;
    }

    private static bool[] GetRow(BitMatrix matrix, int row) {
        var rowData = new bool[matrix.Width];
        for (var x = 0; x < matrix.Width; x++) {
            rowData[x] = matrix[x, row];
        }
        return rowData;
    }
}
