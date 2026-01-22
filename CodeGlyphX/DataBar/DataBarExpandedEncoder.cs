using System;
using System.Collections.Generic;
using System.Text;

namespace CodeGlyphX.DataBar;

// Portions adapted from OkapiBarcode (Apache-2.0) DataBarExpanded implementation.

/// <summary>
/// Encodes GS1 DataBar Expanded symbols (linear and stacked).
/// </summary>
public static class DataBarExpandedEncoder {
    private const string PadPattern = "00100";
    /// <summary>
    /// Encodes a GS1 DataBar Expanded symbol into a <see cref="Barcode1D"/>.
    /// </summary>
    public static Barcode1D EncodeExpanded(string content) {
        var elements = BuildElements(content, stacked: false, preferredColumns: 2, out _);
        var segments = new List<BarSegment>(elements.Length);
        var isBar = false;
        for (var i = 0; i < elements.Length; i++) {
            segments.Add(new BarSegment(isBar, elements[i]));
            isBar = !isBar;
        }
        return new Barcode1D(segments);
    }

    /// <summary>
    /// Encodes a GS1 DataBar Expanded Stacked symbol into a <see cref="BitMatrix"/>.
    /// </summary>
    public static BitMatrix EncodeExpandedStacked(string content, int columns = 2) {
        if (columns < 1) throw new ArgumentOutOfRangeException(nameof(columns), "Column count must be at least 1.");
        var elements = BuildElements(content, stacked: true, preferredColumns: columns, out var dataChars);
        return BuildStackedMatrix(elements, dataChars, columns);
    }

    private static int[] BuildElements(string content, bool stacked, int preferredColumns, out int dataChars) {
        if (content is null) throw new ArgumentNullException(nameof(content));
        content = content.Trim();
        if (content.Length == 0) throw new InvalidOperationException("GS1 DataBar Expanded content cannot be empty.");

        if (content.IndexOf('(') >= 0) {
            content = Gs1.ElementString(content);
        }

        for (var i = 0; i < content.Length; i++) {
            if (content[i] > 127) {
                throw new InvalidOperationException("GS1 DataBar Expanded supports ASCII input only.");
            }
        }

        var inputBytes = Encoding.ASCII.GetBytes(content);
        var inputData = new int[inputBytes.Length];
        for (var i = 0; i < inputBytes.Length; i++) {
            inputData[i] = inputBytes[i];
        }

        var binaryString = new StringBuilder(inputData.Length * 8);
        binaryString.Append('0'); // linkage flag (composite) unsupported for now

        var encodingMethod = CalculateBinaryString(stacked, preferredColumns, inputData, binaryString);
        _ = encodingMethod; // suppress unused warning

        dataChars = binaryString.Length / 12;
        var vs = new int[dataChars];
        for (var i = 0; i < dataChars; i++) {
            var value = 0;
            for (var j = 0; j < 12; j++) {
                if (binaryString[(i * 12) + j] == '1') {
                    value += 2048 >> j;
                }
            }
            vs[i] = value;
        }

        var charWidths = new int[dataChars][];
        for (var i = 0; i < dataChars; i++) {
            charWidths[i] = new int[8];
            var group = GetGroup(vs[i]);
            var vOdd = (vs[i] - DataBarExpandedTables.G_SUM_EXP[group - 1]) / DataBarExpandedTables.T_EVEN_EXP[group - 1];
            var vEven = (vs[i] - DataBarExpandedTables.G_SUM_EXP[group - 1]) % DataBarExpandedTables.T_EVEN_EXP[group - 1];

            var widths = DataBarCommon.GetWidths(vOdd, DataBarExpandedTables.MODULES_ODD_EXP[group - 1], 4, DataBarExpandedTables.WIDEST_ODD_EXP[group - 1], 0);
            charWidths[i][0] = widths[0];
            charWidths[i][2] = widths[1];
            charWidths[i][4] = widths[2];
            charWidths[i][6] = widths[3];

            widths = DataBarCommon.GetWidths(vEven, DataBarExpandedTables.MODULES_EVEN_EXP[group - 1], 4, DataBarExpandedTables.WIDEST_EVEN_EXP[group - 1], 1);
            charWidths[i][1] = widths[0];
            charWidths[i][3] = widths[1];
            charWidths[i][5] = widths[2];
            charWidths[i][7] = widths[3];
        }

        var checksum = 0;
        for (var i = 0; i < dataChars; i++) {
            var row = DataBarExpandedTables.WEIGHT_ROWS[(((dataChars - 2) / 2) * 21) + i];
            for (var j = 0; j < 8; j++) {
                checksum += charWidths[i][j] * DataBarExpandedTables.CHECKSUM_WEIGHT_EXP[(row * 8) + j];
            }
        }

        var checkChar = (211 * ((dataChars + 1) - 4)) + (checksum % 211);
        var cGroup = GetGroup(checkChar);
        var cOdd = (checkChar - DataBarExpandedTables.G_SUM_EXP[cGroup - 1]) / DataBarExpandedTables.T_EVEN_EXP[cGroup - 1];
        var cEven = (checkChar - DataBarExpandedTables.G_SUM_EXP[cGroup - 1]) % DataBarExpandedTables.T_EVEN_EXP[cGroup - 1];
        var checkWidths = new int[8];
        var cWidths = DataBarCommon.GetWidths(cOdd, DataBarExpandedTables.MODULES_ODD_EXP[cGroup - 1], 4, DataBarExpandedTables.WIDEST_ODD_EXP[cGroup - 1], 0);
        checkWidths[0] = cWidths[0];
        checkWidths[2] = cWidths[1];
        checkWidths[4] = cWidths[2];
        checkWidths[6] = cWidths[3];
        cWidths = DataBarCommon.GetWidths(cEven, DataBarExpandedTables.MODULES_EVEN_EXP[cGroup - 1], 4, DataBarExpandedTables.WIDEST_EVEN_EXP[cGroup - 1], 1);
        checkWidths[1] = cWidths[0];
        checkWidths[3] = cWidths[1];
        checkWidths[5] = cWidths[2];
        checkWidths[7] = cWidths[3];

        var totalChars = dataChars + 1;
        var finderCount = (totalChars / 2) + (totalChars & 1);
        var patternWidth = (finderCount * 5) + (totalChars * 8) + 4;
        var elements = new int[patternWidth];

        for (var i = 0; i < finderCount; i++) {
            var k = ((((totalChars - 2) / 2) + (totalChars & 1)) - 1) * 11 + i;
            for (var j = 0; j < 5; j++) {
                var sequence = DataBarExpandedTables.FINDER_SEQUENCE[k];
                if (sequence == 0) continue;
                elements[(21 * i) + j + 10] = DataBarExpandedTables.FINDER_PATTERN_EXP[((sequence - 1) * 5) + j];
            }
        }

        for (var i = 0; i < 8; i++) {
            elements[i + 2] = checkWidths[i];
        }

        for (var i = 1; i < dataChars; i += 2) {
            for (var j = 0; j < 8; j++) {
                elements[(((i - 1) / 2) * 21) + 23 + j] = charWidths[i][j];
            }
        }

        for (var i = 0; i < dataChars; i += 2) {
            for (var j = 0; j < 8; j++) {
                elements[((i / 2) * 21) + 15 + j] = charWidths[i][7 - j];
            }
        }

        if (!stacked) {
            elements[0] = 1;
            elements[1] = 1;
            elements[patternWidth - 2] = 1;
            elements[patternWidth - 1] = 1;
        }

        return elements;
    }

    private static int GetGroup(int value) {
        if (value <= 347) return 1;
        if (value <= 1387) return 2;
        if (value <= 2947) return 3;
        if (value <= 3987) return 4;
        return 5;
    }

    private static BitMatrix BuildStackedMatrix(int[] elements, int dataChars, int preferredColumns) {
        var totalChars = dataChars + 1;
        var codeblocks = (totalChars / 2) + (totalChars & 1);
        var blocksPerRow = Math.Max(preferredColumns, 1);
        var stackRows = (codeblocks + blocksPerRow - 1) / blocksPerRow;
        var rows = new List<bool[]>((stackRows * 4) - 3);

        var currentBlock = 0;
        var v2 = false;

        for (var currentRow = 1; currentRow <= stackRows; currentRow++) {
            var subElements = BuildStackedSubElements(elements, codeblocks, blocksPerRow, currentRow, stackRows, ref currentBlock, out var reader, out var leftToRight, out var specialCaseRow);
            var dataPattern = BuildPatternString(subElements, (currentRow % 2 == 1) || specialCaseRow);
            rows.Add(PatternToModules(dataPattern));

            AddStackedSeparators(rows, dataPattern, reader, currentRow, stackRows, dataChars, blocksPerRow, leftToRight, specialCaseRow, ref v2);
        }

        return BuildMatrix(rows);
    }

    private static int[] BuildStackedSubElements(
        int[] elements,
        int codeblocks,
        int blocksPerRow,
        int currentRow,
        int stackRows,
        ref int currentBlock,
        out int reader,
        out bool leftToRight,
        out bool specialCaseRow) {
        var numColumns = currentRow < stackRows ? blocksPerRow : codeblocks - currentBlock;
        leftToRight = (currentRow % 2 == 1) || (blocksPerRow % 2 == 1);
        specialCaseRow = false;

        var subElements = new int[2 + (numColumns * 21) + 2];
        subElements[0] = 1;
        subElements[1] = 1;

        if (!leftToRight && currentRow == stackRows && numColumns != blocksPerRow && (numColumns % 2 == 1)) {
            specialCaseRow = true;
            leftToRight = true;
            subElements[0] = 2;
        }

        reader = 0;
        do {
            var i = 2 + (currentBlock * 21);
            for (var j = 0; j < 21; j++) {
                if (i + j < elements.Length) {
                    if (leftToRight) {
                        subElements[j + (reader * 21) + 2] = elements[i + j];
                    } else {
                        subElements[(20 - j) + (numColumns - 1 - reader) * 21 + 2] = elements[i + j];
                    }
                }
            }
            reader++;
            currentBlock++;
        } while (reader < blocksPerRow && currentBlock < codeblocks);

        var stopIndex = 2 + (numColumns * 21);
        subElements[stopIndex] = 1;
        subElements[stopIndex + 1] = 1;
        return subElements;
    }

    private static void AddStackedSeparators(
        List<bool[]> rows,
        string dataPattern,
        int reader,
        int currentRow,
        int stackRows,
        int dataChars,
        int blocksPerRow,
        bool leftToRight,
        bool specialCaseRow,
        ref bool v2) {
        if (currentRow != 1) {
            var middleSep = BuildMiddleSeparator(blocksPerRow);
            rows.Insert(rows.Count - 1, PatternToModules(middleSep));
            var oddLastRow = (currentRow == stackRows) && (dataChars % 2 == 0);
            rows.Insert(rows.Count - 1, PatternToModules(BuildSeparator(dataPattern, reader, below: false, specialCaseRow, leftToRight, oddLastRow, ref v2)));
        }

        if (currentRow != stackRows) {
            rows.Add(PatternToModules(BuildSeparator(dataPattern, reader, below: true, specialCaseRow: false, leftToRight, oddLastRow: false, ref v2)));
        }
    }

    private static BitMatrix BuildMatrix(List<bool[]> rows) {
        var width = 0;
        for (var i = 0; i < rows.Count; i++) {
            if (rows[i].Length > width) width = rows[i].Length;
        }

        var matrix = new BitMatrix(width, rows.Count);
        for (var y = 0; y < rows.Count; y++) {
            var row = rows[y];
            for (var x = 0; x < row.Length; x++) {
                if (row[x]) matrix[x, y] = true;
            }
        }

        return matrix;
    }

    private static string BuildPatternString(int[] widths, bool leadingWhite) {
        var sb = new StringBuilder(widths.Length + 1);
        if (leadingWhite) sb.Append('0');
        for (var i = 0; i < widths.Length; i++) {
            sb.Append((char)('0' + widths[i]));
        }
        return sb.ToString();
    }

    private static string BuildMiddleSeparator(int blocksPerRow) {
        var sep = new StringBuilder("05");
        for (var j = 5; j < (49 * blocksPerRow); j += 2) {
            sep.Append("11");
        }
        return sep.ToString();
    }

    private static string BuildSeparator(string pattern, int cols, bool below, bool specialCaseRow, bool leftToRight, bool oddLastRow, ref bool v2State) {
        var linearBin = new StringBuilder(pattern.Length * 4);
        var separator = new StringBuilder(pattern.Length * 4);
        BuildLinearAndSeparator(pattern, linearBin, separator);
        ZeroSeparatorEdges(separator);

        var space = false;
        var v2 = v2State;
        for (var j = 0; j < cols; j++) {
            var k = (49 * j) + 19 + (specialCaseRow ? 1 : 0);
            if (!leftToRight && oddLastRow) {
                k -= 17;
            }
            ApplySeparatorColumn(linearBin, separator, k, v2, leftToRight, ref space);
            v2 = !v2;
        }

        if (below) {
            v2State = v2;
        }

        return BinToPattern(separator.ToString());
    }

    private static void BuildLinearAndSeparator(string pattern, StringBuilder linearBin, StringBuilder separator) {
        var black = true;
        for (var i = 0; i < pattern.Length; i++) {
            var count = pattern[i] - '0';
            for (var j = 0; j < count; j++) {
                linearBin.Append(black ? '1' : '0');
                separator.Append(black ? '0' : '1');
            }
            black = !black;
        }
    }

    private static void ZeroSeparatorEdges(StringBuilder separator) {
        for (var i = 0; i < 4; i++) {
            if (i < separator.Length) separator[i] = '0';
            var idx = separator.Length - 1 - i;
            if (idx >= 0) separator[idx] = '0';
        }
    }

    private static void ApplySeparatorColumn(StringBuilder linearBin, StringBuilder separator, int k, bool v2, bool leftToRight, ref bool space) {
        if (leftToRight) {
            var start = v2 ? 2 : 0;
            var end = v2 ? 15 : 13;
            for (var i = start; i < end; i++) {
                UpdateSeparatorAt(linearBin, separator, i + k, ref space);
            }
            return;
        }

        var startRev = v2 ? 14 : 12;
        var endRev = v2 ? 2 : 0;
        for (var i = startRev; i >= endRev; i--) {
            UpdateSeparatorAt(linearBin, separator, i + k, ref space);
        }
    }

    private static void UpdateSeparatorAt(StringBuilder linearBin, StringBuilder separator, int idx, ref bool space) {
        if ((uint)idx >= (uint)linearBin.Length) return;
        if (linearBin[idx] == '1') {
            separator[idx] = '0';
            space = false;
        } else {
            separator[idx] = space ? '0' : '1';
            space = !space;
        }
    }

    private static string BinToPattern(string bin) {
        var len = 0;
        var black = true;
        var pattern = new StringBuilder(bin.Length);
        for (var i = 0; i < bin.Length; i++) {
            if (black) {
                if (bin[i] == '1') {
                    len++;
                } else {
                    black = false;
                    pattern.Append((char)('0' + len));
                    len = 1;
                }
            } else {
                if (bin[i] == '0') {
                    len++;
                } else {
                    black = true;
                    pattern.Append((char)('0' + len));
                    len = 1;
                }
            }
        }
        pattern.Append((char)('0' + len));
        return pattern.ToString();
    }

    private static bool[] PatternToModules(string pattern) {
        var total = 0;
        for (var i = 0; i < pattern.Length; i++) {
            total += pattern[i] - '0';
        }
        var modules = new bool[total];
        var black = true;
        var pos = 0;
        for (var i = 0; i < pattern.Length; i++) {
            var count = pattern[i] - '0';
            for (var j = 0; j < count; j++) {
                modules[pos++] = black;
            }
            black = !black;
        }
        return modules;
    }

    private enum EncodeMode {
        Numeric,
        Alpha,
        IsoIec,
        AnyEnc,
        AlphaOrIso
    }

    private static int CalculateBinaryString(bool stacked, int blocksPerRow, int[] inputData, StringBuilder binaryString) {
        var encodingMethod = SelectEncodingMethod(inputData);
        var readPosn = AppendEncodingHeader(binaryString, encodingMethod, inputData.Length);

        ValidateCompressedDigits(inputData, readPosn);
        AppendCompressedFields(binaryString, encodingMethod, inputData);

        var lastMode = AppendGeneralField(binaryString, inputData, readPosn, stacked, blocksPerRow);
        ApplyPaddingAndPatch(binaryString, encodingMethod, lastMode, stacked, blocksPerRow);

        return encodingMethod;
    }

    private static int SelectEncodingMethod(int[] inputData) {
        var encodingMethod = 2;
        if (inputData.Length >= 16 && inputData[0] == '0' && inputData[1] == '1') {
            encodingMethod = 1;
        }

        if (inputData.Length >= 20 && encodingMethod == 1 && inputData[2] == '9' && inputData[16] == '3') {
            if (inputData.Length >= 26 && inputData[17] == '1' && inputData[18] == '0') {
                var weight = 0.0;
                for (var i = 0; i < 6; i++) {
                    weight *= 10;
                    weight += (inputData[20 + i] - '0');
                }
                if (weight < 99999) {
                    if (inputData[19] == '3' && inputData.Length == 26) {
                        weight /= 1000.0;
                        if (weight <= 32.767) encodingMethod = 3;
                    }
                    if (inputData.Length == 34) {
                        if (inputData[26] == '1' && inputData[27] == '1') encodingMethod = 7;
                        if (inputData[26] == '1' && inputData[27] == '3') encodingMethod = 9;
                        if (inputData[26] == '1' && inputData[27] == '5') encodingMethod = 11;
                        if (inputData[26] == '1' && inputData[27] == '7') encodingMethod = 13;
                    }
                }
            }

            if (inputData.Length >= 26 && inputData[17] == '2' && inputData[18] == '0') {
                var weight = 0.0;
                for (var i = 0; i < 6; i++) {
                    weight *= 10;
                    weight += (inputData[20 + i] - '0');
                }
                if (weight < 99999) {
                    if ((inputData[19] == '2' || inputData[19] == '3') && inputData.Length == 26) {
                        if (inputData[19] == '3') {
                            weight /= 1000.0;
                            if (weight <= 22.767) encodingMethod = 4;
                        } else {
                            weight /= 100.0;
                            if (weight <= 99.99) encodingMethod = 4;
                        }
                    }
                    if (inputData.Length == 34) {
                        if (inputData[26] == '1' && inputData[27] == '1') encodingMethod = 8;
                        if (inputData[26] == '1' && inputData[27] == '3') encodingMethod = 10;
                        if (inputData[26] == '1' && inputData[27] == '5') encodingMethod = 12;
                        if (inputData[26] == '1' && inputData[27] == '7') encodingMethod = 14;
                    }
                }
            }

            if (inputData[17] == '9') {
                if (inputData[18] == '2' && inputData[19] >= '0' && inputData[19] <= '3') encodingMethod = 5;
                if (inputData[18] == '3' && inputData[19] >= '0' && inputData[19] <= '3') encodingMethod = 6;
            }
        }

        return encodingMethod;
    }

    private static int AppendEncodingHeader(StringBuilder binaryString, int encodingMethod, int inputLength) {
        switch (encodingMethod) {
            case 1:
                binaryString.Append("1XX");
                return 16;
            case 2:
                binaryString.Append("00XX");
                return 0;
            case 3:
                binaryString.Append("0100");
                return inputLength;
            case 4:
                binaryString.Append("0101");
                return inputLength;
            case 5:
                binaryString.Append("01100XX");
                return 20;
            case 6:
                binaryString.Append("01101XX");
                return 23;
            default:
                binaryString.Append('0');
                binaryAppend(binaryString, 56 + encodingMethod - 7, 6);
                return inputLength;
        }
    }

    private static void ValidateCompressedDigits(int[] inputData, int readPosn) {
        for (var i = 0; i < readPosn; i++) {
            if (inputData[i] < '0' || inputData[i] > '9') {
                throw new InvalidOperationException("GS1 DataBar Expanded requires numeric data for compressed fields.");
            }
        }
    }

    private static void AppendCompressedFields(StringBuilder binaryString, int encodingMethod, int[] inputData) {
        if (encodingMethod == 1) {
            binaryAppend(binaryString, inputData[2] - '0', 4);
            for (var i = 1; i < 5; i++) {
                var group = ParseInt(inputData, i * 3, 3);
                binaryAppend(binaryString, group, 10);
            }
            return;
        }

        if (encodingMethod == 3 || encodingMethod == 4) {
            for (var i = 1; i < 5; i++) {
                var group = ParseInt(inputData, i * 3, 3);
                binaryAppend(binaryString, group, 10);
            }
            var weight = ParseInt(inputData, 20, 6);
            if (encodingMethod == 4 && inputData[19] == '3') weight += 10000;
            binaryAppend(binaryString, weight, 15);
            return;
        }

        if (encodingMethod == 5 || encodingMethod == 6) {
            for (var i = 1; i < 5; i++) {
                var group = ParseInt(inputData, i * 3, 3);
                binaryAppend(binaryString, group, 10);
            }
            binaryAppend(binaryString, inputData[19] - '0', 2);
            if (encodingMethod == 6) {
                var currency = ParseInt(inputData, 20, 3);
                binaryAppend(binaryString, currency, 10);
            }
            return;
        }

        if (encodingMethod >= 7 && encodingMethod <= 14) {
            for (var i = 1; i < 5; i++) {
                var group = ParseInt(inputData, i * 3, 3);
                binaryAppend(binaryString, group, 10);
            }
            var weight = inputData[19] - '0';
            for (var i = 0; i < 5; i++) {
                weight *= 10;
                weight += inputData[21 + i] - '0';
            }
            binaryAppend(binaryString, weight, 20);
            var date = 38400;
            if (inputData.Length == 34) {
                date = ParseInt(inputData, 28, 2) * 384;
                date += (ParseInt(inputData, 30, 2) - 1) * 32;
                date += ParseInt(inputData, 32, 2);
            }
            binaryAppend(binaryString, date, 16);
        }
    }

    private static EncodeMode AppendGeneralField(StringBuilder binaryString, int[] inputData, int readPosn, bool stacked, int blocksPerRow) {
        var lastMode = EncodeMode.Numeric;
        var generalField = new int[inputData.Length - readPosn];
        Array.Copy(inputData, readPosn, generalField, 0, generalField.Length);
        if (generalField.Length == 0) return lastMode;

        var generalFieldType = GetInitialEncodeModes(generalField);
        var trailingDigit = ApplyGeneralFieldRules(generalFieldType);
        lastMode = AppendToBinaryString(generalField, generalFieldType, trailingDigit, false, binaryString);
        var remainder = CalculateRemainder(binaryString.Length, stacked, blocksPerRow);

        if (trailingDigit) {
            var i = generalField.Length - 1;
            if (lastMode == EncodeMode.Numeric) {
                if (remainder >= 4 && remainder <= 6) {
                    var value = generalField[i] - '0';
                    value++;
                    binaryAppend(binaryString, value, 4);
                } else {
                    var d1 = generalField[i] - '0';
                    var d2 = 10;
                    var value = (11 * d1) + d2 + 8;
                    binaryAppend(binaryString, value, 7);
                }
            } else {
                var value = generalField[i] - 43;
                binaryAppend(binaryString, value, 5);
            }
        }

        return lastMode;
    }

    private static void ApplyPaddingAndPatch(StringBuilder binaryString, int encodingMethod, EncodeMode lastMode, bool stacked, int blocksPerRow) {
        if (binaryString.Length > 252) {
            throw new InvalidOperationException("GS1 DataBar Expanded content is too long.");
        }

        var remainder = CalculateRemainder(binaryString.Length, stacked, blocksPerRow);
        var padBuilder = new StringBuilder();
        var remaining = remainder;
        if (lastMode == EncodeMode.Numeric) {
            padBuilder.Append("0000");
            remaining -= 4;
        }
        for (var i = remaining; i > 0; i -= 5) {
            padBuilder.Append(PadPattern);
        }
        var padstring = padBuilder.ToString();
        binaryString.Append(padstring, 0, remainder);

        var patchEvenOdd = (((binaryString.Length / 12) + 1) & 1) == 0 ? '0' : '1';
        var patchSize = binaryString.Length <= 156 ? '0' : '1';

        if (encodingMethod == 1) {
            binaryString[2] = patchEvenOdd;
            binaryString[3] = patchSize;
        }
        if (encodingMethod == 2) {
            binaryString[3] = patchEvenOdd;
            binaryString[4] = patchSize;
        }
        if (encodingMethod == 5 || encodingMethod == 6) {
            binaryString[6] = patchEvenOdd;
            binaryString[7] = patchSize;
        }
    }

    private static int CalculateRemainder(int binaryStringLength, bool stacked, int blocksPerRow) {
        var remainder = 12 - (binaryStringLength % 12);
        if (remainder == 12) remainder = 0;
        if (binaryStringLength < 36) remainder = 36 - binaryStringLength;
        if (stacked) {
            var symbolChars = ((binaryStringLength + remainder) / 12) + 1;
            var symbolCharsInLastRow = symbolChars % (blocksPerRow * 2);
            if (symbolCharsInLastRow == 1) remainder += 12;
        }
        return remainder;
    }

    private static EncodeMode[] GetInitialEncodeModes(int[] generalField) {
        var generalFieldType = new EncodeMode[generalField.Length];
        for (var i = 0; i < generalField.Length; i++) {
            var c = generalField[i];
            EncodeMode mode;
            if (c == Gs1.GroupSeparator) {
                mode = EncodeMode.AnyEnc;
            } else if (c >= '0' && c <= '9') {
                mode = EncodeMode.AnyEnc;
            } else if ((c >= 'A' && c <= 'Z') || c == '*' || c == ',' || c == '-' || c == '.' || c == '/') {
                mode = EncodeMode.AlphaOrIso;
            } else if ((c >= 'a' && c <= 'z') || c == '!' || c == '"' || c == '%' || c == '&' || c == '\'' ||
                       c == '(' || c == ')' || c == '+' || c == ':' || c == ';' || c == '<' || c == '=' ||
                       c == '>' || c == '?' || c == '_' || c == ' ') {
                mode = EncodeMode.IsoIec;
            } else {
                throw new InvalidOperationException("GS1 DataBar Expanded contains characters not encodable in general field.");
            }
            generalFieldType[i] = mode;
        }

        for (var i = 0; i < generalField.Length - 1; i++) {
            if (generalFieldType[i] == EncodeMode.IsoIec && generalField[i + 1] == Gs1.GroupSeparator) {
                generalFieldType[i + 1] = EncodeMode.IsoIec;
            }
        }

        for (var i = 0; i < generalField.Length - 1; i++) {
            if (generalFieldType[i] == EncodeMode.AlphaOrIso && generalField[i + 1] == Gs1.GroupSeparator) {
                generalFieldType[i + 1] = EncodeMode.AlphaOrIso;
            }
        }

        return generalFieldType;
    }

    private static bool ApplyGeneralFieldRules(EncodeMode[] generalFieldType) {
        var blockLength = new int[200];
        var blockType = new EncodeMode[200];
        var blockCount = BuildGeneralFieldBlocks(generalFieldType, blockLength, blockType);

        ApplyBlockTransitionRules(blockLength, blockType, blockCount);
        blockCount = MergeAdjacentBlocks(blockLength, blockType, blockCount);
        AdjustNumericBlocks(blockLength, blockType, blockCount);
        WriteBlocksToField(generalFieldType, blockLength, blockType, blockCount);

        return blockType[blockCount - 1] == EncodeMode.Numeric && (blockLength[blockCount - 1] & 1) != 0;
    }

    private static int BuildGeneralFieldBlocks(EncodeMode[] generalFieldType, int[] blockLength, EncodeMode[] blockType) {
        var blockCount = 0;
        blockLength[blockCount] = 1;
        blockType[blockCount] = generalFieldType[0];

        for (var i = 1; i < generalFieldType.Length; i++) {
            var current = generalFieldType[i];
            var last = generalFieldType[i - 1];

            if (current == last) {
                blockLength[blockCount] = blockLength[blockCount] + 1;
            } else {
                blockCount++;
                blockLength[blockCount] = 1;
                blockType[blockCount] = current;
            }
        }

        return blockCount + 1;
    }

    private static void ApplyBlockTransitionRules(int[] blockLength, EncodeMode[] blockType, int blockCount) {
        for (var i = 0; i < blockCount; i++) {
            var current = blockType[i];
            var next = blockType[i + 1];

            if (current == EncodeMode.IsoIec && i != (blockCount - 1)) {
                if (next == EncodeMode.AnyEnc && blockLength[i + 1] >= 4) blockType[i + 1] = EncodeMode.Numeric;
                if (next == EncodeMode.AnyEnc && blockLength[i + 1] < 4) blockType[i + 1] = EncodeMode.IsoIec;
                if (next == EncodeMode.AlphaOrIso && blockLength[i + 1] >= 5) blockType[i + 1] = EncodeMode.Alpha;
                if (next == EncodeMode.AlphaOrIso && blockLength[i + 1] < 5) blockType[i + 1] = EncodeMode.IsoIec;
            }

            if (current == EncodeMode.AlphaOrIso) {
                blockType[i] = EncodeMode.Alpha;
                current = EncodeMode.Alpha;
            }

            if (current == EncodeMode.Alpha && i != (blockCount - 1)) {
                if (next == EncodeMode.AnyEnc && blockLength[i + 1] >= 6) blockType[i + 1] = EncodeMode.Numeric;
                if (next == EncodeMode.AnyEnc && blockLength[i + 1] < 6) {
                    if (i == blockCount - 2 && blockLength[i + 1] >= 4) {
                        blockType[i + 1] = EncodeMode.Numeric;
                    } else {
                        blockType[i + 1] = EncodeMode.Alpha;
                    }
                }
            }

            if (current == EncodeMode.AnyEnc) blockType[i] = EncodeMode.Numeric;
        }
    }

    private static int MergeAdjacentBlocks(int[] blockLength, EncodeMode[] blockType, int blockCount) {
        if (blockCount <= 1) return blockCount;
        var i = 1;
        while (i < blockCount) {
            if (blockType[i - 1] == blockType[i]) {
                blockLength[i - 1] = blockLength[i - 1] + blockLength[i];
                for (var j = i + 1; j < blockCount; j++) {
                    blockLength[j - 1] = blockLength[j];
                    blockType[j - 1] = blockType[j];
                }
                blockCount--;
                i--;
            }
            i++;
        }
        return blockCount;
    }

    private static void AdjustNumericBlocks(int[] blockLength, EncodeMode[] blockType, int blockCount) {
        for (var i = 0; i < blockCount - 1; i++) {
            if (blockType[i] == EncodeMode.Numeric && (blockLength[i] & 1) != 0) {
                blockLength[i] = blockLength[i] - 1;
                blockLength[i + 1] = blockLength[i + 1] + 1;
            }
        }
    }

    private static void WriteBlocksToField(EncodeMode[] generalFieldType, int[] blockLength, EncodeMode[] blockType, int blockCount) {
        var index = 0;
        for (var i = 0; i < blockCount; i++) {
            for (var k = 0; k < blockLength[i]; k++) {
                generalFieldType[index] = blockType[i];
                index++;
            }
        }
    }

    private static EncodeMode AppendToBinaryString(int[] generalField, EncodeMode[] generalFieldType, bool trailingDigit, bool treatFnc1AsNumericLatch, StringBuilder binaryString) {
        var lastMode = EncodeMode.Numeric;
        if (generalFieldType[0] == EncodeMode.Alpha) {
            binaryString.Append("0000");
            lastMode = EncodeMode.Alpha;
        }
        if (generalFieldType[0] == EncodeMode.IsoIec) {
            binaryString.Append("0000");
            binaryString.Append("00100");
            lastMode = EncodeMode.IsoIec;
        }

        var i = 0;
        var currentLength = i;
        if (trailingDigit) currentLength++;

        while (currentLength < generalField.Length) {
            switch (generalFieldType[i]) {
                case EncodeMode.Numeric:
                    i = AppendNumericToken(binaryString, generalField, i, ref lastMode);
                    break;
                case EncodeMode.Alpha:
                    i = AppendAlphaToken(binaryString, generalField, i, ref lastMode, treatFnc1AsNumericLatch);
                    break;
                case EncodeMode.IsoIec:
                    i = AppendIsoToken(binaryString, generalField, i, ref lastMode, treatFnc1AsNumericLatch);
                    break;
            }

            currentLength = i;
            if (trailingDigit) currentLength++;
        }

        return lastMode;
    }

    private static int AppendNumericToken(StringBuilder binaryString, int[] generalField, int index, ref EncodeMode lastMode) {
        if (lastMode != EncodeMode.Numeric) binaryString.Append("000");
        var d1 = generalField[index] == Gs1.GroupSeparator ? 10 : generalField[index] - '0';
        var d2 = generalField[index + 1] == Gs1.GroupSeparator ? 10 : generalField[index + 1] - '0';
        var value = (11 * d1) + d2 + 8;
        binaryAppend(binaryString, value, 7);
        lastMode = EncodeMode.Numeric;
        return index + 2;
    }

    private static int AppendAlphaToken(StringBuilder binaryString, int[] generalField, int index, ref EncodeMode lastMode, bool treatFnc1AsNumericLatch) {
        if (index != 0) {
            if (lastMode == EncodeMode.Numeric) binaryString.Append("0000");
            if (lastMode == EncodeMode.IsoIec) binaryString.Append("00100");
        }
        AppendAlphaChar(binaryString, generalField[index]);
        lastMode = EncodeMode.Alpha;
        if (generalField[index] == Gs1.GroupSeparator) {
            binaryString.Append("01111");
            if (treatFnc1AsNumericLatch) lastMode = EncodeMode.Numeric;
        }
        return index + 1;
    }

    private static int AppendIsoToken(StringBuilder binaryString, int[] generalField, int index, ref EncodeMode lastMode, bool treatFnc1AsNumericLatch) {
        if (index != 0) {
            if (lastMode == EncodeMode.Numeric) {
                binaryString.Append("0000");
                binaryString.Append("00100");
            }
            if (lastMode == EncodeMode.Alpha) binaryString.Append("00100");
        }
        AppendIsoChar(binaryString, generalField[index]);
        lastMode = EncodeMode.IsoIec;
        if (generalField[index] == Gs1.GroupSeparator) {
            binaryString.Append("01111");
            if (treatFnc1AsNumericLatch) lastMode = EncodeMode.Numeric;
        }
        return index + 1;
    }

    private static void AppendAlphaChar(StringBuilder binaryString, int value) {
        if (value >= '0' && value <= '9') {
            binaryAppend(binaryString, value - 43, 5);
            return;
        }
        if (value >= 'A' && value <= 'Z') {
            binaryAppend(binaryString, value - 33, 6);
            return;
        }
        switch (value) {
            case Gs1.GroupSeparator:
                return;
            case '*':
                binaryString.Append("111010");
                return;
            case ',':
                binaryString.Append("111011");
                return;
            case '-':
                binaryString.Append("111100");
                return;
            case '.':
                binaryString.Append("111101");
                return;
            case '/':
                binaryString.Append("111110");
                return;
        }
    }

    private static void AppendIsoChar(StringBuilder binaryString, int value) {
        if (value >= '0' && value <= '9') {
            binaryAppend(binaryString, value - 43, 5);
            return;
        }
        if (value >= 'A' && value <= 'Z') {
            binaryAppend(binaryString, value - 1, 7);
            return;
        }
        if (value >= 'a' && value <= 'z') {
            binaryAppend(binaryString, value - 7, 7);
            return;
        }
        switch (value) {
            case Gs1.GroupSeparator:
                return;
            case '!':
                binaryString.Append("11101000");
                return;
            case '"':
                binaryString.Append("11101001");
                return;
            case '%':
                binaryString.Append("11101010");
                return;
            case '&':
                binaryString.Append("11101011");
                return;
            case '\'':
                binaryString.Append("11101100");
                return;
            case '(':
                binaryString.Append("11101101");
                return;
            case ')':
                binaryString.Append("11101110");
                return;
            case '*':
                binaryString.Append("11101111");
                return;
            case '+':
                binaryString.Append("11110000");
                return;
            case ',':
                binaryString.Append("11110001");
                return;
            case '-':
                binaryString.Append("11110010");
                return;
            case '.':
                binaryString.Append("11110011");
                return;
            case '/':
                binaryString.Append("11110100");
                return;
            case ':':
                binaryString.Append("11110101");
                return;
            case ';':
                binaryString.Append("11110110");
                return;
            case '<':
                binaryString.Append("11110111");
                return;
            case '=':
                binaryString.Append("11111000");
                return;
            case '>':
                binaryString.Append("11111001");
                return;
            case '?':
                binaryString.Append("11111010");
                return;
            case '_':
                binaryString.Append("11111011");
                return;
            case ' ':
                binaryString.Append("11111100");
                return;
        }
    }

    private static int ParseInt(int[] chars, int index, int length) {
        var val = 0;
        var pow = (int)Math.Pow(10, length - 1);
        for (var i = 0; i < length; i++) {
            val += (chars[index + i] - '0') * pow;
            pow /= 10;
        }
        return val;
    }

    private static void binaryAppend(StringBuilder sb, int value, int bits) {
        for (var i = bits - 1; i >= 0; i--) {
            sb.Append(((value >> i) & 1) == 1 ? '1' : '0');
        }
    }
}
