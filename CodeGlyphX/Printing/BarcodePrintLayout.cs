using System;
using System.Collections.Generic;
using System.IO;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Printing;

/// <summary>An immutable, measured barcode scene in millimetres, shared by raster and vector exports.</summary>
public sealed class BarcodePrintLayout {
    /// <summary>Gets the physical format, when known.</summary>
    public SymbolFormat? Format { get; }
    /// <summary>Gets the target printer resolution.</summary>
    public int Dpi { get; }
    /// <summary>Gets the resolved narrow module width in millimetres.</summary>
    public double XDimensionMm { get; }
    /// <summary>Gets the resolved module pitch in printer pixels. Fixed-geometry formats may be fractional.</summary>
    public double ModulePitchPixels => XDimensionMm * Dpi / 25.4;
    /// <summary>Gets the barcode and its reserved quiet zones before optional text overhang.</summary>
    public BarcodeBounds SymbolBounds { get; }
    /// <summary>Gets the union of reserved symbol space and text line boxes.</summary>
    public BarcodeBounds LogicalBounds { get; }
    /// <summary>Gets the union of drawn bar, recognition-pattern and text outlines, excluding the background.</summary>
    public BarcodeBounds InkBounds { get; }
    /// <summary>Gets the complete renderable extent, including required space and overhanging ink.</summary>
    public BarcodeBounds Bounds => LogicalBounds.Union(InkBounds);
    /// <summary>Gets all positioned fill paths in painting order.</summary>
    public IReadOnlyList<BarcodePrintPath> Paths { get; }
    /// <summary>Gets the positioned text and the exact glyph runs used for painting.</summary>
    public IReadOnlyList<BarcodePrintText> Text { get; }
    /// <summary>Gets preflight findings. Absence of findings does not certify a printed label.</summary>
    public IReadOnlyList<BarcodePrintFinding> Findings { get; }
    /// <summary>Gets whether a selected print rule could not be satisfied.</summary>
    public bool HasErrors { get; }
    /// <summary>Gets the scene background paint.</summary>
    public BarcodePaint Background { get; }

    /// <summary>Combines physical-layout findings with placement and export limitations before allocating an image.</summary>
    public IReadOnlyList<BarcodePrintFinding> Preflight(BarcodePrintPlacement? placement = null, OutputFormat? format = null) {
        var findings = new List<BarcodePrintFinding>(Findings);
        PrintTransform.Resolve(this, placement, out var width, out var height, out var clipped, rejectClipping: false);
        if (clipped) findings.Add(new BarcodePrintFinding("CanvasClipsLayout", true, "The canvas clips text, symbol ink or required clear space. Increase the canvas or move the barcode."));
        if (placement is not null && Math.Abs(placement.Scale - 1) > 1e-9)
            findings.Add(new BarcodePrintFinding("ScaledPhysicalRules", false, "The barcode is scaled after physical layout. Revalidate module and text dimensions at the final size."));
        if (format is OutputFormat.Bmp or OutputFormat.Webp or OutputFormat.Pam or OutputFormat.Tga)
            findings.Add(new BarcodePrintFinding("PhysicalMetadataUnavailable", false, "This raster fallback preserves pixels but does not record the print layout's DPI. Set the physical size explicitly when placing it."));
        var vectorPaints = BarcodePrintRenderer.HasOnlyOpaqueSolids(this);
        if (format == OutputFormat.Pdf && !vectorPaints)
            findings.Add(new BarcodePrintFinding("PdfRasterFallback", false, "Decorative or translucent PDF paints rasterize the complete scene at the declared DPI and flatten it onto white. Use SVG to retain these vector paints."));
        if (format == OutputFormat.Eps && !vectorPaints)
            findings.Add(new BarcodePrintFinding("EpsPaintUnsupported", true, "EPS requires opaque solid paints. Select SVG, PNG or PDF to preserve these paints."));
        if (format.HasValue && (format == OutputFormat.Pdf ? !vectorPaints : format is not (OutputFormat.Svg or OutputFormat.Svgz or OutputFormat.Eps))
            && Math.Ceiling(width * Dpi / 25.4 - 1e-9) * Math.Ceiling(height * Dpi / 25.4 - 1e-9) > 16000000)
            findings.Add(new BarcodePrintFinding("RasterPixelLimit", true, "The raster would exceed 16 million pixels. Reduce DPI or use vector output."));
        return findings.AsReadOnly();
    }

    internal BarcodePrintLayout(SymbolFormat? format, int dpi, double xDimension, BarcodeBounds symbol,
        List<BarcodePrintPath> paths, List<BarcodePrintText> text, List<BarcodePrintFinding> findings, BarcodePaint background) {
        Format = format; Dpi = dpi; XDimensionMm = xDimension; SymbolBounds = symbol; Background = background;
        Paths = paths.AsReadOnly(); Text = text.AsReadOnly(); Findings = findings.AsReadOnly();
        var logical = symbol; var ink = default(BarcodeBounds);
        foreach (var line in text) logical = logical.Union(line.LogicalBounds);
        foreach (var path in paths) ink = ink.Union(path.Bounds);
        LogicalBounds = logical; InkBounds = ink;
        foreach (var finding in findings) if (finding.IsError) HasErrors = true;
    }

    /// <summary>Exports the same measured scene. PNG, SVG/SVGZ, PDF and EPS retain physical sizing.</summary>
    /// <remarks>Opaque solid PDF/EPS use vector outlines. Decorative PDF paints use an explicit raster fallback;
    /// EPS rejects paints or transparency that its vector route cannot preserve.</remarks>
    public RenderedOutput Render(OutputFormat format, BarcodePrintPlacement? placement = null) => BarcodePrintRenderer.Render(this, format, placement);
    /// <summary>Renders tightly packed RGBA pixels at the declared resolution.</summary>
    public byte[] RenderPixels(out int width, out int height, BarcodePrintPlacement? placement = null) =>
        BarcodePrintRasterizer.Render(this, placement, out width, out height);
    /// <summary>Saves using the file extension. The caller owns streams and receives no hidden font dependency.</summary>
    public string Save(string path, BarcodePrintPlacement? placement = null) =>
        OutputWriter.Write(path, Render(OutputFormatInfo.Resolve(path, OutputFormat.Png), placement));
    /// <summary>Writes an export to a caller-owned stream without closing it.</summary>
    public void Save(Stream stream, OutputFormat format, BarcodePrintPlacement? placement = null) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        OutputWriter.Write(stream, Render(format, placement));
    }
}

/// <summary>Places a barcode on a larger physical canvas. Rotation is clockwise about the layout's origin.</summary>
/// <remarks>A cropped quiet zone or a scale change can invalidate print rules. Use <see cref="BarcodePrintLayout.Bounds"/>
/// and the transformed <see cref="Measure"/> result before selecting a canvas.</remarks>
public sealed class BarcodePrintPlacement {
    /// <summary>Gets or sets the origin's horizontal position in millimetres.</summary>
    public double X { get; set; }
    /// <summary>Gets or sets the origin's vertical position in millimetres.</summary>
    public double Y { get; set; }
    /// <summary>Gets or sets uniform scale. Physical print rules are defined at scale one.</summary>
    public double Scale { get; set; } = 1;
    /// <summary>Gets or sets clockwise rotation in degrees.</summary>
    public double RotationDegrees { get; set; }
    /// <summary>Gets or sets an explicit canvas width. Both canvas dimensions must be supplied together.</summary>
    public double? CanvasWidthMm { get; set; }
    /// <summary>Gets or sets an explicit canvas height.</summary>
    public double? CanvasHeightMm { get; set; }
    /// <summary>Gets or sets whether clipping to an explicitly sized canvas is permitted.</summary>
    public bool AllowClipping { get; set; }
    /// <summary>Returns the transformed bounds before any canvas clipping.</summary>
    public BarcodeBounds Measure(BarcodeBounds bounds) => new PrintTransform(this).Map(bounds);
}
