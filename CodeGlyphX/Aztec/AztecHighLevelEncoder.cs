using System;
using System.Collections.Generic;

namespace CodeGlyphX.Aztec;

internal sealed class AztecHighLevelEncoder {
    internal const int ModeUpper = 0;
    internal const int ModeLower = 1;
    internal const int ModeDigit = 2;
    internal const int ModeMixed = 3;
    internal const int ModePunct = 4;

    internal static readonly int[][] LatchTable = {
        new[] {
            0,
            (5 << 16) + 28,
            (5 << 16) + 30,
            (5 << 16) + 29,
            (10 << 16) + (29 << 5) + 30,
        },
        new[] {
            (9 << 16) + (30 << 4) + 14,
            0,
            (5 << 16) + 30,
            (5 << 16) + 29,
            (10 << 16) + (29 << 5) + 30,
        },
        new[] {
            (4 << 16) + 14,
            (9 << 16) + (14 << 5) + 28,
            0,
            (9 << 16) + (14 << 5) + 29,
            (14 << 16) + (14 << 10) + (29 << 5) + 30,
        },
        new[] {
            (5 << 16) + 29,
            (5 << 16) + 28,
            (10 << 16) + (29 << 5) + 30,
            0,
            (5 << 16) + 30,
        },
        new[] {
            (5 << 16) + 31,
            (10 << 16) + (31 << 5) + 28,
            (10 << 16) + (31 << 5) + 30,
            (10 << 16) + (31 << 5) + 29,
            0,
        },
    };

    private static readonly int[][] CharMap = new int[5][];
    internal static readonly int[][] ShiftTable = new int[6][];

    private static readonly int[] MixedTable = {
        '\0', ' ', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005', '\u0006', '\u0007', '\b', '\t', '\n',
        '\u000B', '\f', '\r', '\u001B', '\u001C', '\u001D', '\u001E', '\u001F', '@', '\\', '^', '_', '`', '|', '~', '\u007F'
    };

    private static readonly int[] PunctTable = {
        '\0', '\r', '\0', '\0', '\0', '\0', '!', '\'', '#', '$', '%', '&', '\'',
        '(', ')', '*', '+', ',', '-', '.', '/', ':', ';', '<', '=', '>', '?',
        '[', ']', '{', '}'
    };

    private readonly byte[] _text;

    static AztecHighLevelEncoder() {
        for (var i = 0; i < CharMap.Length; i++) {
            CharMap[i] = new int[256];
        }

        CharMap[ModeUpper][' '] = 1;
        for (var c = 'A'; c <= 'Z'; c++) {
            CharMap[ModeUpper][c] = c - 'A' + 2;
        }

        CharMap[ModeLower][' '] = 1;
        for (var c = 'a'; c <= 'z'; c++) {
            CharMap[ModeLower][c] = c - 'a' + 2;
        }

        CharMap[ModeDigit][' '] = 1;
        for (var c = '0'; c <= '9'; c++) {
            CharMap[ModeDigit][c] = c - '0' + 2;
        }
        CharMap[ModeDigit][','] = 12;
        CharMap[ModeDigit]['.'] = 13;

        for (var i = 0; i < MixedTable.Length; i++) {
            CharMap[ModeMixed][MixedTable[i]] = i;
        }
        for (var i = 0; i < PunctTable.Length; i++) {
            if (PunctTable[i] > 0) {
                CharMap[ModePunct][PunctTable[i]] = i;
            }
        }

        for (var i = 0; i < ShiftTable.Length; i++) {
            var table = new int[6];
            for (var j = 0; j < table.Length; j++) table[j] = -1;
            ShiftTable[i] = table;
        }
        ShiftTable[ModeUpper][ModePunct] = 0;

        ShiftTable[ModeLower][ModePunct] = 0;
        ShiftTable[ModeLower][ModeUpper] = 28;

        ShiftTable[ModeMixed][ModePunct] = 0;

        ShiftTable[ModeDigit][ModePunct] = 0;
        ShiftTable[ModeDigit][ModeUpper] = 15;
    }

    public AztecHighLevelEncoder(byte[] text) {
        _text = text ?? throw new ArgumentNullException(nameof(text));
    }

    public AztecBitBuffer Encode() {
        var states = new List<AztecState> { AztecState.InitialState };
        for (var index = 0; index < _text.Length; index++) {
            var pairCode = 0;
            var nextChar = index + 1 < _text.Length ? _text[index + 1] : 0;

            switch (_text[index]) {
                case (byte)'\r':
                    pairCode = nextChar == '\n' ? 2 : 0;
                    break;
                case (byte)'.':
                    pairCode = nextChar == ' ' ? 3 : 0;
                    break;
                case (byte)',':
                    pairCode = nextChar == ' ' ? 4 : 0;
                    break;
                case (byte)':':
                    pairCode = nextChar == ' ' ? 5 : 0;
                    break;
                default:
                    pairCode = 0;
                    break;
            }

            if (pairCode > 0) {
                states = UpdateStateListForPair(states, index, pairCode);
                index++;
            } else {
                states = UpdateStateListForChar(states, index);
            }
        }

        AztecState? minState = null;
        for (var i = 0; i < states.Count; i++) {
            var state = states[i];
            if (minState is null || state.BitCount < minState.BitCount) {
                minState = state;
            }
        }

        return (minState ?? AztecState.InitialState).ToBitBuffer(_text);
    }

    private List<AztecState> UpdateStateListForChar(IEnumerable<AztecState> states, int index) {
        var result = new List<AztecState>();
        foreach (var state in states) {
            UpdateStateForChar(state, index, result);
        }
        return SimplifyStates(result);
    }

    private void UpdateStateForChar(AztecState state, int index, ICollection<AztecState> result) {
        var ch = (char)(_text[index] & 0xFF);
        var charInCurrentTable = CharMap[state.Mode][ch] > 0;
        AztecState? stateNoBinary = null;

        for (var mode = 0; mode <= ModePunct; mode++) {
            var charInMode = CharMap[mode][ch];
            if (charInMode > 0) {
                stateNoBinary ??= state.EndBinaryShift(index);
                if (!charInCurrentTable || mode == state.Mode || mode == ModeDigit) {
                    result.Add(stateNoBinary.LatchAndAppend(mode, charInMode));
                }
                if (!charInCurrentTable && ShiftTable[state.Mode][mode] >= 0) {
                    result.Add(stateNoBinary.ShiftAndAppend(mode, charInMode));
                }
            }
        }

        if (state.BinaryShiftByteCount > 0 || CharMap[state.Mode][ch] == 0) {
            result.Add(state.AddBinaryShiftChar(index));
        }
    }

    private static List<AztecState> UpdateStateListForPair(IEnumerable<AztecState> states, int index, int pairCode) {
        var result = new List<AztecState>();
        foreach (var state in states) {
            UpdateStateForPair(state, index, pairCode, result);
        }
        return SimplifyStates(result);
    }

    private static void UpdateStateForPair(AztecState state, int index, int pairCode, ICollection<AztecState> result) {
        var stateNoBinary = state.EndBinaryShift(index);
        result.Add(stateNoBinary.LatchAndAppend(ModePunct, pairCode));
        if (state.Mode != ModePunct) {
            result.Add(stateNoBinary.ShiftAndAppend(ModePunct, pairCode));
        }
        if (pairCode == 3 || pairCode == 4) {
            var digitState = stateNoBinary
                .LatchAndAppend(ModeDigit, 16 - pairCode)
                .LatchAndAppend(ModeDigit, 1);
            result.Add(digitState);
        }
        if (state.BinaryShiftByteCount > 0) {
            var binaryState = state.AddBinaryShiftChar(index).AddBinaryShiftChar(index + 1);
            result.Add(binaryState);
        }
    }

    private static List<AztecState> SimplifyStates(IEnumerable<AztecState> states) {
        var result = new List<AztecState>();
        foreach (var newState in states) {
            var add = true;
            for (var i = result.Count - 1; i >= 0; i--) {
                var oldState = result[i];
                if (oldState.IsBetterThanOrEqualTo(newState)) {
                    add = false;
                    break;
                }
                if (newState.IsBetterThanOrEqualTo(oldState)) {
                    result.RemoveAt(i);
                }
            }
            if (add) result.Add(newState);
        }
        return result;
    }
}
