using System;
using System.IO;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Svg;

namespace CodeGlyphX.Rendering.Svgz;

/// <summary>
/// Renders matrix modules to SVGZ.
/// </summary>
public static class MatrixSvgzRenderer {
    /// <summary>
    /// Renders the module matrix to SVGZ bytes.
    /// </summary>
    public static byte[] Render(BitMatrix modules, MatrixSvgRenderOptions opts) {
        var svg = MatrixSvgRenderer.Render(modules, opts);
        return SvgzWriter.WriteSvg(svg);
    }

    /// <summary>
    /// Renders the module matrix to an SVGZ stream.
    /// </summary>
    public static void RenderToStream(BitMatrix modules, MatrixSvgRenderOptions opts, Stream stream) {
        var svg = MatrixSvgRenderer.Render(modules, opts);
        SvgzWriter.WriteSvg(stream, svg);
    }

    /// <summary>
    /// Renders the module matrix to an SVGZ file.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, MatrixSvgRenderOptions opts, string path) {
        var svgz = Render(modules, opts);
        return RenderIO.WriteBinary(path, svgz);
    }

    /// <summary>
    /// Renders the module matrix to an SVGZ file under the specified directory.
    /// </summary>
    public static string RenderToFile(BitMatrix modules, MatrixSvgRenderOptions opts, string directory, string fileName) {
        var svgz = Render(modules, opts);
        return RenderIO.WriteBinary(directory, fileName, svgz);
    }
}
