using System;
using CodeGlyphX.Aztec.Internal;

namespace CodeGlyphX.Aztec;

internal sealed class AztecDetector {
    private const int MaxLayers = 32;
    private const int MaxCompactLayers = 4;

    private readonly BitMatrix _matrix;
    private bool _compact;
    private int _nbLayers;
    private int _nbDataBlocks;
    private int _nbCenterLayers;
    private int _shift;

    private AztecDetector(BitMatrix matrix) {
        _matrix = matrix;
    }

    public static bool TryDetect(BitMatrix matrix, out AztecDetectorResult result) {
        result = null!;
        if (matrix is null) return false;
        if (matrix.Width != matrix.Height) return false;

        try {
            var detector = new AztecDetector(matrix);
            return detector.TryDetectInternal(out result);
        } catch {
            result = null!;
            return false;
        }
    }

    private bool TryDetectInternal(out AztecDetectorResult result) {
        result = null!;
        if (TryDetectBySize(out result)) return true;

        var centerX = _matrix.Width / 2;
        var centerY = _matrix.Height / 2;
        if (!TryGetBullsEyeCorners(centerX, centerY, out var bullsEyeCorners)) return false;
        if (!ExtractParameters(bullsEyeCorners)) return false;

        var bits = _matrix;
        if (_shift != 0) {
            bits = Rotate(bits, _shift);
        }

        result = new AztecDetectorResult(bits, _compact, _nbDataBlocks, _nbLayers);
        return true;
    }

    private bool TryDetectBySize(out AztecDetectorResult result) {
        result = null!;
        var size = _matrix.Width;
        if (size != _matrix.Height) return false;

        var compact = false;
        var layers = 0;
        var baseMatrixSize = 0;

        for (var l = 1; l <= MaxCompactLayers; l++) {
            var candidate = 11 + l * 4;
            if (candidate == size) {
                compact = true;
                layers = l;
                baseMatrixSize = candidate;
                break;
            }
        }

        if (layers == 0) {
            for (var l = 1; l <= MaxLayers; l++) {
                var baseSize = 14 + l * 4;
                var matrixSize = baseSize + 1 + 2 * ((baseSize / 2 - 1) / 15);
                if (matrixSize == size) {
                    compact = false;
                    layers = l;
                    baseMatrixSize = baseSize;
                    break;
                }
            }
        }

        if (layers == 0) return false;

        if (!TryReadModeMessage(size, compact, out var modeBits)) return false;
        if (!TryDecodeParameters(modeBits, compact, out var nbLayers, out var nbDataBlocks)) return false;

        if (nbLayers != layers) {
            // Size and mode disagree; reject for now.
            return false;
        }

        result = new AztecDetectorResult(_matrix, compact, nbDataBlocks, nbLayers);
        return true;
    }

    private bool TryGetBullsEyeCorners(int centerX, int centerY, out AztecPoint[] corners) {
        corners = Array.Empty<AztecPoint>();

        var pointA = new AztecPoint(centerX, centerY);
        var pointB = pointA;
        var pointC = pointA;
        var pointD = pointA;
        var color = true;

        for (_nbCenterLayers = 1; _nbCenterLayers < 9; _nbCenterLayers++) {
            var pointA1 = GetFirstDifferent(pointA, color, 1, -1);
            var pointB1 = GetFirstDifferent(pointB, color, 1, 1);
            var pointC1 = GetFirstDifferent(pointC, color, -1, 1);
            var pointD1 = GetFirstDifferent(pointD, color, -1, -1);

            if (_nbCenterLayers > 2) {
                var ratio = Distance(pointD1, pointA1) * _nbCenterLayers / (Distance(pointD, pointA) * (_nbCenterLayers + 2));
                if (ratio < 0.75f || ratio > 1.25f || !IsWhiteOrBlackRectangle(pointA1, pointB1, pointC1, pointD1)) {
                    break;
                }
            }

            pointA = pointA1;
            pointB = pointB1;
            pointC = pointC1;
            pointD = pointD1;
            color = !color;
        }

        _nbCenterLayers--;
        if (_nbCenterLayers is not (5 or 7)) return false;
        _compact = _nbCenterLayers == 5;

        corners = ExpandSquare(pointA, pointB, pointC, pointD, _nbCenterLayers * 2, _nbCenterLayers * 2);
        return true;
    }

    private bool ExtractParameters(AztecPoint[] bullsEyeCorners) {
        if (!IsValid(bullsEyeCorners[0]) || !IsValid(bullsEyeCorners[1]) || !IsValid(bullsEyeCorners[2]) || !IsValid(bullsEyeCorners[3])) {
            return false;
        }

        var length = (_compact ? 7 : 11) + _nbCenterLayers * 2;
        var sides = new int[4];
        for (var i = 0; i < 4; i++) {
            sides[i] = SampleLine(bullsEyeCorners[i], bullsEyeCorners[(i + 1) % 4], length);
        }
        _shift = GetRotation(sides, length);

        long parameterData = 0;
        for (var i = 0; i < 4; i++) {
            var side = sides[(_shift + i) % 4];
            if (_compact) {
                parameterData = (parameterData << 7) + ((side >> 1) & 0x7F);
            } else {
                parameterData = (parameterData << 10) + ((side >> 2) & 0x3FF);
            }
        }

        var nbBits = _compact ? 7 : 10;
        var nbDataBlocks = _compact ? 2 : 4;
        var parameterWords = new int[nbBits];
        for (var i = 0; i < nbBits; i++) {
            var offset = (nbBits - i - 1) * 4;
            parameterWords[i] = (int)(parameterData >> offset) & 0xF;
        }

        try {
            var rsDecoder = new ReedSolomonDecoder(GenericGf.AztecParam);
            rsDecoder.Decode(parameterWords, nbBits - nbDataBlocks);
        } catch (ReedSolomonException) {
            return false;
        }

        if (_compact) {
            _nbLayers = (parameterWords[0] >> 2) + 1;
            _nbDataBlocks = ((parameterWords[0] & 0x3) << 4) + parameterWords[1] + 1;
        } else {
            _nbLayers = (parameterWords[0] << 1) + (parameterWords[1] >> 3) + 1;
            _nbDataBlocks = ((parameterWords[1] & 0x7) << 8) + (parameterWords[2] << 4) + parameterWords[3] + 1;
        }

        return true;
    }

    private bool TryReadModeMessage(int matrixSize, bool compact, out bool[] modeBits) {
        modeBits = Array.Empty<bool>();
        var center = matrixSize / 2;

        if (compact) {
            modeBits = new bool[28];
            for (var i = 0; i < 7; i++) {
                var offset = center - 3 + i;
                modeBits[i] = _matrix.Get(offset, center - 5);
                modeBits[i + 7] = _matrix.Get(center + 5, offset);
                modeBits[20 - i] = _matrix.Get(offset, center + 5);
                modeBits[27 - i] = _matrix.Get(center - 5, offset);
            }
            return true;
        }

        modeBits = new bool[40];
        for (var i = 0; i < 10; i++) {
            var offset = center - 5 + i + i / 5;
            modeBits[i] = _matrix.Get(offset, center - 7);
            modeBits[i + 10] = _matrix.Get(center + 7, offset);
            modeBits[29 - i] = _matrix.Get(offset, center + 7);
            modeBits[39 - i] = _matrix.Get(center - 7, offset);
        }
        return true;
    }

    private static bool TryDecodeParameters(bool[] modeBits, bool compact, out int layers, out int dataBlocks) {
        layers = 0;
        dataBlocks = 0;

        long parameterData = 0;
        for (var i = 0; i < modeBits.Length; i++) {
            parameterData = (parameterData << 1) + (modeBits[i] ? 1 : 0);
        }

        var nbBits = compact ? 7 : 10;
        var nbDataBlocks = compact ? 2 : 4;
        var parameterWords = new int[nbBits];
        for (var i = 0; i < nbBits; i++) {
            var offset = (nbBits - i - 1) * 4;
            parameterWords[i] = (int)(parameterData >> offset) & 0xF;
        }

        try {
            var rsDecoder = new ReedSolomonDecoder(GenericGf.AztecParam);
            rsDecoder.Decode(parameterWords, nbBits - nbDataBlocks);
        } catch (ReedSolomonException) {
            return false;
        }

        if (compact) {
            layers = (parameterWords[0] >> 2) + 1;
            dataBlocks = ((parameterWords[0] & 0x3) << 4) + parameterWords[1] + 1;
        } else {
            layers = (parameterWords[0] << 1) + (parameterWords[1] >> 3) + 1;
            dataBlocks = ((parameterWords[1] & 0x7) << 8) + (parameterWords[2] << 4) + parameterWords[3] + 1;
        }

        return true;
    }

    private AztecPoint GetFirstDifferent(AztecPoint init, bool color, int dx, int dy) {
        var x = (int)init.X + dx;
        var y = (int)init.Y + dy;

        while (IsValid(x, y) && _matrix.Get(x, y) == color) {
            x += dx;
            y += dy;
        }

        x -= dx;
        y -= dy;

        while (IsValid(x, y) && _matrix.Get(x, y) == color) x += dx;
        x -= dx;

        while (IsValid(x, y) && _matrix.Get(x, y) == color) y += dy;
        y -= dy;

        return new AztecPoint(x, y);
    }

    private bool IsWhiteOrBlackRectangle(AztecPoint pointA, AztecPoint pointB, AztecPoint pointC, AztecPoint pointD) {
        var delta = 3.0f;
        var px = new AztecPoint(pointA.X - delta, pointA.Y + delta);
        var py = new AztecPoint(pointB.X - delta, pointB.Y - delta);
        var pz = new AztecPoint(pointC.X + delta, pointC.Y - delta);
        var pw = new AztecPoint(pointD.X + delta, pointD.Y + delta);

        var color = GetColor(pw, px);
        if (color == 0) return false;

        var colorB = GetColor(px, py);
        if (colorB != color) return false;
        var colorC = GetColor(py, pz);
        if (colorC != color) return false;
        var colorD = GetColor(pz, pw);
        if (colorD != color) return false;

        return true;
    }

    private int GetColor(AztecPoint from, AztecPoint to) {
        var distance = Distance(from, to);
        var dx = (to.X - from.X) / distance;
        var dy = (to.Y - from.Y) / distance;
        var x = from.X;
        var y = from.Y;

        var color = _matrix.Get((int)from.X, (int)from.Y);
        var transitions = 0;
        var points = (int)distance;

        for (var i = 0; i < points; i++) {
            if (_matrix.Get((int)x, (int)y) != color) {
                transitions++;
                color = !color;
            }
            x += dx;
            y += dy;
        }

        var ratio = transitions / distance;
        if (ratio > 0.1f && ratio < 0.9f) return 0;
        return color ? 1 : -1;
    }

    private int SampleLine(AztecPoint p1, AztecPoint p2, int size) {
        var result = 0;
        var distance = Distance(p1, p2);
        var moduleSize = distance / (size - 1);
        var dx = (p2.X - p1.X) / distance;
        var dy = (p2.Y - p1.Y) / distance;

        var x = p1.X;
        var y = p1.Y;
        for (var i = 0; i < size; i++) {
            if (_matrix.Get(RoundToInt(x), RoundToInt(y))) {
                result |= 1 << (size - 1 - i);
            }
            x += dx * moduleSize;
            y += dy * moduleSize;
        }

        return result;
    }

    private int GetRotation(int[] sides, int length) {
        var cornerBits = 0;
        for (var i = 0; i < 4; i++) {
            var side = sides[i];
            cornerBits = (cornerBits << 3) + ((side >> (length - 2)) << 1) + (side & 1);
        }

        cornerBits = ((cornerBits & 1) << 11) + (cornerBits >> 1);

        for (var shift = 0; shift < 4; shift++) {
            if ((cornerBits & 0xEEE) == 0xAAA) {
                return shift;
            }
            cornerBits = ((cornerBits << 3) & 0xFFF) + (cornerBits >> 9);
        }

        return 0;
    }

    private bool IsValid(AztecPoint point) {
        return IsValid((int)point.X, (int)point.Y);
    }

    private bool IsValid(int x, int y) {
        return x >= 0 && x < _matrix.Width && y >= 0 && y < _matrix.Height;
    }

    private static AztecPoint[] ExpandSquare(AztecPoint p0, AztecPoint p1, AztecPoint p2, AztecPoint p3, int oldSide, int newSide) {
        var ratio = newSide / (float)(oldSide * 2);
        var dx = p0.X - p2.X;
        var dy = p0.Y - p2.Y;

        var centerX = (p0.X + p2.X) / 2.0f;
        var centerY = (p0.Y + p2.Y) / 2.0f;

        var pA = new AztecPoint(centerX + dx * ratio, centerY + dy * ratio);
        var pB = new AztecPoint(centerX + (p1.X - p3.X) * ratio, centerY + (p1.Y - p3.Y) * ratio);
        var pC = new AztecPoint(centerX - dx * ratio, centerY - dy * ratio);
        var pD = new AztecPoint(centerX - (p1.X - p3.X) * ratio, centerY - (p1.Y - p3.Y) * ratio);

        return new[] { pA, pB, pC, pD };
    }

    private static BitMatrix Rotate(BitMatrix matrix, int rotation) {
        rotation &= 3;
        if (rotation == 0) return matrix;
        if (rotation == 1) return Rotate90(matrix);
        if (rotation == 2) return Rotate180(matrix);
        return Rotate270(matrix);
    }

    private static BitMatrix Rotate90(BitMatrix matrix) {
        var width = matrix.Width;
        var height = matrix.Height;
        var rotated = new BitMatrix(height, width);
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                if (matrix.Get(x, y)) rotated.Set(height - 1 - y, x, true);
            }
        }
        return rotated;
    }

    private static BitMatrix Rotate180(BitMatrix matrix) {
        var width = matrix.Width;
        var height = matrix.Height;
        var rotated = new BitMatrix(width, height);
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                if (matrix.Get(x, y)) rotated.Set(width - 1 - x, height - 1 - y, true);
            }
        }
        return rotated;
    }

    private static BitMatrix Rotate270(BitMatrix matrix) {
        var width = matrix.Width;
        var height = matrix.Height;
        var rotated = new BitMatrix(height, width);
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                if (matrix.Get(x, y)) rotated.Set(y, width - 1 - x, true);
            }
        }
        return rotated;
    }

    private static float Distance(AztecPoint a, AztecPoint b) {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }

    private static int RoundToInt(float value) {
        return (int)Math.Floor(value + 0.5f);
    }

    private readonly struct AztecPoint {
        public readonly float X;
        public readonly float Y;

        public AztecPoint(float x, float y) {
            X = x;
            Y = y;
        }
    }
}
