#if NET8_0_OR_GREATER
using System;

namespace CodeGlyphX.Qr;

/// <summary>
/// Minimal perspective transform (ported from common QR grid-sampling approaches).
/// </summary>
internal readonly struct QrPerspectiveTransform {
    private readonly double _a11;
    private readonly double _a12;
    private readonly double _a13;
    private readonly double _a21;
    private readonly double _a22;
    private readonly double _a23;
    private readonly double _a31;
    private readonly double _a32;
    private readonly double _a33;

    private QrPerspectiveTransform(
        double a11, double a21, double a31,
        double a12, double a22, double a32,
        double a13, double a23, double a33) {
        _a11 = a11;
        _a12 = a12;
        _a13 = a13;
        _a21 = a21;
        _a22 = a22;
        _a23 = a23;
        _a31 = a31;
        _a32 = a32;
        _a33 = a33;
    }

    public static QrPerspectiveTransform QuadrilateralToQuadrilateral(
        double x0, double y0,
        double x1, double y1,
        double x2, double y2,
        double x3, double y3,
        double x0p, double y0p,
        double x1p, double y1p,
        double x2p, double y2p,
        double x3p, double y3p) {
        var qToS = QuadrilateralToSquare(x0, y0, x1, y1, x2, y2, x3, y3);
        var sToQ = SquareToQuadrilateral(x0p, y0p, x1p, y1p, x2p, y2p, x3p, y3p);
        return sToQ.Times(qToS);
    }

    public void Transform(double x, double y, out double outX, out double outY) {
        var denominator = _a13 * x + _a23 * y + _a33;
        if (Math.Abs(denominator) < 1e-12) {
            outX = double.NaN;
            outY = double.NaN;
            return;
        }

        outX = (_a11 * x + _a21 * y + _a31) / denominator;
        outY = (_a12 * x + _a22 * y + _a32) / denominator;
    }

    public void GetRowParameters(
        double xStart,
        double y,
        out double numX,
        out double numY,
        out double denom,
        out double stepNumX,
        out double stepNumY,
        out double stepDenom) {
        stepNumX = _a11;
        stepNumY = _a12;
        stepDenom = _a13;

        var baseNumX = _a21 * y + _a31;
        var baseNumY = _a22 * y + _a32;
        var baseDenom = _a23 * y + _a33;

        numX = _a11 * xStart + baseNumX;
        numY = _a12 * xStart + baseNumY;
        denom = _a13 * xStart + baseDenom;
    }

    private QrPerspectiveTransform Times(QrPerspectiveTransform other) {
        return new QrPerspectiveTransform(
            _a11 * other._a11 + _a21 * other._a12 + _a31 * other._a13,
            _a11 * other._a21 + _a21 * other._a22 + _a31 * other._a23,
            _a11 * other._a31 + _a21 * other._a32 + _a31 * other._a33,

            _a12 * other._a11 + _a22 * other._a12 + _a32 * other._a13,
            _a12 * other._a21 + _a22 * other._a22 + _a32 * other._a23,
            _a12 * other._a31 + _a22 * other._a32 + _a32 * other._a33,

            _a13 * other._a11 + _a23 * other._a12 + _a33 * other._a13,
            _a13 * other._a21 + _a23 * other._a22 + _a33 * other._a23,
            _a13 * other._a31 + _a23 * other._a32 + _a33 * other._a33);
    }

    private QrPerspectiveTransform BuildAdjoint() {
        return new QrPerspectiveTransform(
            _a22 * _a33 - _a23 * _a32,
            _a23 * _a31 - _a21 * _a33,
            _a21 * _a32 - _a22 * _a31,

            _a13 * _a32 - _a12 * _a33,
            _a11 * _a33 - _a13 * _a31,
            _a12 * _a31 - _a11 * _a32,

            _a12 * _a23 - _a13 * _a22,
            _a13 * _a21 - _a11 * _a23,
            _a11 * _a22 - _a12 * _a21);
    }

    private static QrPerspectiveTransform QuadrilateralToSquare(
        double x0, double y0,
        double x1, double y1,
        double x2, double y2,
        double x3, double y3) {
        return SquareToQuadrilateral(x0, y0, x1, y1, x2, y2, x3, y3).BuildAdjoint();
    }

    private static QrPerspectiveTransform SquareToQuadrilateral(
        double x0, double y0,
        double x1, double y1,
        double x2, double y2,
        double x3, double y3) {
        var dx3 = x0 - x1 + x2 - x3;
        var dy3 = y0 - y1 + y2 - y3;
        if (Math.Abs(dx3) < 1e-12 && Math.Abs(dy3) < 1e-12) {
            return new QrPerspectiveTransform(
                x1 - x0, x2 - x1, x0,
                y1 - y0, y2 - y1, y0,
                0, 0, 1);
        }

        var dx1 = x1 - x2;
        var dx2 = x3 - x2;
        var dy1 = y1 - y2;
        var dy2 = y3 - y2;
        var denominator = dx1 * dy2 - dx2 * dy1;
        if (Math.Abs(denominator) < 1e-12) {
            // Degenerate; fall back to affine.
            return new QrPerspectiveTransform(
                x1 - x0, x2 - x1, x0,
                y1 - y0, y2 - y1, y0,
                0, 0, 1);
        }

        var a13 = (dx3 * dy2 - dx2 * dy3) / denominator;
        var a23 = (dx1 * dy3 - dx3 * dy1) / denominator;

        return new QrPerspectiveTransform(
            x1 - x0 + a13 * x1, x3 - x0 + a23 * x3, x0,
            y1 - y0 + a13 * y1, y3 - y0 + a23 * y3, y0,
            a13, a23, 1);
    }
}
#endif
