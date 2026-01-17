using System;
using System.IO;
using System.Text;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

/// <summary>
/// Builds and writes diagnostic dumps for QR pixel decoding attempts.
/// </summary>
public static class QrDiagnosticsDump {
    /// <summary>
    /// Builds a diagnostic summary string for a decode attempt.
    /// </summary>
    public static string Build(QrPixelDecodeInfo info, string? label = null, string? source = null) {
        var sb = new StringBuilder(256);
        if (!string.IsNullOrWhiteSpace(label)) {
            sb.AppendLine(label);
        }
        if (!string.IsNullOrWhiteSpace(source)) {
            sb.AppendLine($"Source: {source}");
        }
        sb.AppendLine($"Status: {(info.IsSuccess ? "Success" : "Failure")}");
        sb.AppendLine($"Scale: {info.Scale}");
        sb.AppendLine($"Threshold: {info.Threshold}");
        sb.AppendLine($"Invert: {info.Invert}");
        sb.AppendLine($"Candidates: {info.CandidateCount}");
        sb.AppendLine($"TriplesTried: {info.CandidateTriplesTried}");
        if (info.Dimension > 0) sb.AppendLine($"Dimension: {info.Dimension}");
        sb.AppendLine($"Module: {info.Module.Message}");
        sb.AppendLine($"Confidence: {info.Confidence:0.###}");
        return sb.ToString();
    }

    /// <summary>
    /// Writes diagnostics to a text file.
    /// </summary>
    public static string WriteText(string path, QrPixelDecodeInfo info, string? label = null, string? source = null) {
        var text = Build(info, label, source);
        return RenderIO.WriteText(path, text);
    }

    /// <summary>
    /// Writes diagnostics to a text file in the provided directory.
    /// </summary>
    public static string WriteText(string directory, string fileName, QrPixelDecodeInfo info, string? label = null, string? source = null) {
        var text = Build(info, label, source);
        return RenderIO.WriteText(directory, fileName, text);
    }

    /// <summary>
    /// Writes diagnostics to a text stream.
    /// </summary>
    public static void WriteText(Stream stream, QrPixelDecodeInfo info, string? label = null, string? source = null) {
        var text = Build(info, label, source);
        RenderIO.WriteText(stream, text);
    }
}
