using System;

namespace CodeGlyphX.DataBar;

/// <summary>
/// Assembles compacted values into GS1 DataBar Expanded finder, data, and guard elements.
/// </summary>
internal sealed class DataBarExpandedSymbol {
    private DataBarExpandedSymbol(string content, int totalCharacters, int[] elements) {
        Content = content;
        TotalCharacters = totalCharacters;
        Elements = elements;
    }

    internal string Content { get; }
    internal int TotalCharacters { get; }
    internal int[] Elements { get; }

    internal static DataBarExpandedSymbol Create(string value, bool requireEvenTotalCharacters) {
        var binary = DataBarExpandedBinaryEncoder.Encode(value, requireEvenTotalCharacters);
        var dataWidths = new int[binary.DataValues.Length][];
        for (var i = 0; i < dataWidths.Length; i++) {
            dataWidths[i] = DataBarExpandedSpecification.GetDataCharacterWidths(binary.DataValues[i]);
        }

        var checkValue = DataBarExpandedSpecification.ComputeCheckCharacter(dataWidths, binary.TotalCharacters);
        var checkWidths = DataBarExpandedSpecification.GetDataCharacterWidths(checkValue);
        var elements = Assemble(binary.TotalCharacters, checkWidths, dataWidths);
        return new DataBarExpandedSymbol(binary.Content, binary.TotalCharacters, elements);
    }

    private static int[] Assemble(int totalCharacters, int[] checkWidths, int[][] dataWidths) {
        var finderCount = (totalCharacters + 1) / 2;
        var elements = new int[4 + (totalCharacters * 8) + (finderCount * 5)];
        elements[0] = 1;
        elements[1] = 1;
        elements[elements.Length - 2] = 1;
        elements[elements.Length - 1] = 1;
        Array.Copy(checkWidths, 0, elements, 2, 8);

        var finderSequence = DataBarExpandedSpecification.GetFinderSequence(totalCharacters);
        for (var finder = 0; finder < finderCount; finder++) {
            var finderStart = 10 + (finder * 21);
            DataBarExpandedSpecification.CopyFinderPattern(finderSequence[finder], (finder & 1) != 0, elements, finderStart);

            var firstData = finder * 2;
            if (firstData < dataWidths.Length) {
                CopyReversed(dataWidths[firstData], elements, finderStart + 5);
            }
            if (firstData + 1 < dataWidths.Length) {
                Array.Copy(dataWidths[firstData + 1], 0, elements, finderStart + 13, 8);
            }
        }

        return elements;
    }

    private static void CopyReversed(int[] source, int[] destination, int destinationIndex) {
        for (var i = 0; i < source.Length; i++) {
            destination[destinationIndex + i] = source[source.Length - 1 - i];
        }
    }
}
