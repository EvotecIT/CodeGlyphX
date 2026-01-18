using System;
using System.Collections.Generic;

namespace CodeGlyphX.Aztec;

internal sealed class AztecState {
    public static readonly AztecState InitialState = new(AztecToken.Empty, AztecHighLevelEncoder.ModeUpper, 0, 0);
    private const int MaxBinaryShiftBytes = 2047 + 31;

    private readonly int _mode;
    private readonly AztecToken _token;
    private readonly int _binaryShiftByteCount;
    private readonly int _bitCount;
    private readonly int _binaryShiftCost;

    public AztecState(AztecToken token, int mode, int binaryBytes, int bitCount) {
        _token = token ?? throw new ArgumentNullException(nameof(token));
        _mode = mode;
        _binaryShiftByteCount = binaryBytes;
        _bitCount = bitCount;
        _binaryShiftCost = CalculateBinaryShiftCost(binaryBytes);
    }

    public int Mode => _mode;
    public AztecToken Token => _token;
    public int BinaryShiftByteCount => _binaryShiftByteCount;
    public int BitCount => _bitCount;

    public AztecState LatchAndAppend(int mode, int value) {
        var bitCount = _bitCount;
        var token = _token;
        if (mode != _mode) {
            var latch = AztecHighLevelEncoder.LatchTable[_mode][mode];
            token = token.Add(latch & 0xFFFF, latch >> 16);
            bitCount += latch >> 16;
        }

        var latchModeBitCount = mode == AztecHighLevelEncoder.ModeDigit ? 4 : 5;
        token = token.Add(value, latchModeBitCount);
        return new AztecState(token, mode, 0, bitCount + latchModeBitCount);
    }

    public AztecState ShiftAndAppend(int mode, int value) {
        var token = _token;
        var thisModeBitCount = _mode == AztecHighLevelEncoder.ModeDigit ? 4 : 5;
        token = token.Add(AztecHighLevelEncoder.ShiftTable[_mode][mode], thisModeBitCount);
        token = token.Add(value, 5);
        return new AztecState(token, _mode, 0, _bitCount + thisModeBitCount + 5);
    }

    public AztecState AddBinaryShiftChar(int index) {
        var token = _token;
        var mode = _mode;
        var bitCount = _bitCount;
        if (_mode == AztecHighLevelEncoder.ModePunct || _mode == AztecHighLevelEncoder.ModeDigit) {
            var latch = AztecHighLevelEncoder.LatchTable[mode][AztecHighLevelEncoder.ModeUpper];
            token = token.Add(latch & 0xFFFF, latch >> 16);
            bitCount += latch >> 16;
            mode = AztecHighLevelEncoder.ModeUpper;
        }

        var deltaBitCount = (_binaryShiftByteCount == 0 || _binaryShiftByteCount == 31)
            ? 18
            : (_binaryShiftByteCount == 62 ? 9 : 8);

        var result = new AztecState(token, mode, _binaryShiftByteCount + 1, bitCount + deltaBitCount);
        if (result._binaryShiftByteCount == MaxBinaryShiftBytes) {
            result = result.EndBinaryShift(index + 1);
        }
        return result;
    }

    public AztecState EndBinaryShift(int index) {
        if (_binaryShiftByteCount == 0) return this;
        var token = _token.AddBinaryShift(index - _binaryShiftByteCount, _binaryShiftByteCount);
        return new AztecState(token, _mode, 0, _bitCount);
    }

    public bool IsBetterThanOrEqualTo(AztecState other) {
        var newModeBitCount = _bitCount + (AztecHighLevelEncoder.LatchTable[_mode][other._mode] >> 16);
        if (_binaryShiftByteCount < other._binaryShiftByteCount) {
            newModeBitCount += other._binaryShiftCost - _binaryShiftCost;
        } else if (_binaryShiftByteCount > other._binaryShiftByteCount && other._binaryShiftByteCount > 0) {
            newModeBitCount += 10;
        }
        return newModeBitCount <= other._bitCount;
    }

    public AztecBitBuffer ToBitBuffer(byte[] text) {
        var tokens = new List<AztecToken>();
        for (var token = EndBinaryShift(text.Length).Token; token != null; token = token.Previous) {
            tokens.Add(token);
        }

        var bitBuffer = new AztecBitBuffer();
        for (var i = tokens.Count - 1; i >= 0; i--) {
            tokens[i].AppendTo(bitBuffer, text);
        }
        return bitBuffer;
    }

    private static int CalculateBinaryShiftCost(int binaryShiftByteCount) {
        if (binaryShiftByteCount == 0) return 0;
        if (binaryShiftByteCount <= 31) return 18;
        if (binaryShiftByteCount <= 62) return 9;
        return 8;
    }
}
