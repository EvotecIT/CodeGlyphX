using CodeGlyphX.Rendering.Art;

namespace CodeGlyphX.Playground;

/// <summary>A completed comparison, retaining its payload and export separately from editable controls.</summary>
public sealed class QrArtStudioCard(QrImageCandidate candidate, QrImageComposition image, string uri, string payload,
    QrImageValidationReport validation, string? svgUri) {
    public QrImageComposition Image { get; } = image;
    public QrImageValidationReport Validation { get; } = validation;
    public string? SvgUri { get; } = svgUri;
    public string Payload { get; } = payload;
    public QrImageCandidate Candidate { get; } = candidate;
    public string Uri { get; } = uri;
    public QrImageValidationReport? Delivery { get; set; }
}
