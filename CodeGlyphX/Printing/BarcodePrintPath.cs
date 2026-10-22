using System;
using System.Collections.Generic;

namespace CodeGlyphX.Printing;

/// <summary>A filled compound outline in millimetres. Nonzero winding preserves glyph holes.</summary>
public sealed class BarcodePrintPath {
    /// <summary>Gets the closed fill contours.</summary>
    public IReadOnlyList<IReadOnlyList<BarcodePoint>> Contours { get; }
    /// <summary>Gets the fill paint.</summary>
    public BarcodePaint Paint { get; }
    /// <summary>Gets the exact outline's axis-aligned bounds.</summary>
    public BarcodeBounds Bounds { get; }
    /// <summary>Gets the semantic purpose, such as bars, text, bearer or recognition pattern.</summary>
    public string Role { get; }
    internal BarcodePrintPath(IReadOnlyList<IReadOnlyList<BarcodePoint>> contours, BarcodePaint paint, string role) {
        Contours = contours; Paint = paint; Role = role;
        var left = double.PositiveInfinity; var top = double.PositiveInfinity;
        var right = double.NegativeInfinity; var bottom = double.NegativeInfinity;
        foreach (var contour in contours) foreach (var p in contour) {
            left = Math.Min(left, p.X); right = Math.Max(right, p.X);
            top = Math.Min(top, p.Y); bottom = Math.Max(bottom, p.Y);
        }
        Bounds = double.IsPositiveInfinity(left) ? default : new BarcodeBounds(left, top, right - left, bottom - top);
    }
}

/// <summary>A positioned readable line, retaining the measured shape used by every export.</summary>
public sealed class BarcodePrintText {
    /// <summary>Gets the exact source text.</summary>
    public string Text => Shape.Text;
    /// <summary>Gets the scoped font's display name.</summary>
    public string FontName { get; }
    /// <summary>Gets the resolved em size in millimetres.</summary>
    public double EmSizeMm { get; }
    /// <summary>Gets the position of the line-box origin.</summary>
    public BarcodePoint Origin { get; }
    /// <summary>Gets the owned outline and its original metrics.</summary>
    public BarcodeTextShape Shape { get; }
    /// <summary>Gets the positioned logical line box.</summary>
    public BarcodeBounds LogicalBounds => Shape.LogicalBounds.Translate(Origin.X, Origin.Y);
    /// <summary>Gets the positioned actual ink extent.</summary>
    public BarcodeBounds InkBounds => Shape.InkBounds.Translate(Origin.X, Origin.Y);
    internal BarcodePrintText(BarcodeTextShape shape, string fontName, double em, double x, double y) {
        Shape = shape; FontName = fontName; EmSizeMm = em; Origin = new BarcodePoint(x, y);
    }
}

/// <summary>An actionable print-preflight observation, independent of printer certification.</summary>
public sealed class BarcodePrintFinding {
    /// <summary>Gets a stable code for programmatic handling.</summary>
    public string Code { get; }
    /// <summary>Gets whether the selected physical rule cannot be satisfied.</summary>
    public bool IsError { get; }
    /// <summary>Gets the explanation and corrective action.</summary>
    public string Message { get; }
    internal BarcodePrintFinding(string code, bool error, string message) { Code = code; IsError = error; Message = message; }
}
