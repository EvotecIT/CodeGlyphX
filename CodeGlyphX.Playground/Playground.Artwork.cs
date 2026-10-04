using CodeGlyphX.Rendering.Art;

namespace CodeGlyphX.Playground;

public partial class Playground {
    internal string QrDesign { get; set; } = "Styled";
    internal bool IsQr => SelectedCategory is "QR" or "SpecialQR";
    internal bool ArtworkAvailable => SelectedCategory != "SpecialQR" || SpecialPayloadType != "Girocode";
    internal bool IsArtwork => SelectedMode == "Generate" && IsQr && ArtworkAvailable && QrDesign == "Artwork";
    internal QrArtStudio? ArtEditor;
    internal int ArtRevision { get; private set; }
    internal PlaygroundArtwork? Artwork { get; private set; }

    internal string CurrentQrPayload() => SelectedCategory == "SpecialQR" ? GetSpecialPayloadData()?.Text ?? "" : Content;

    // Each edit invalidates the active export before any asynchronous search can publish.
    internal void InvalidateArtwork() {
        ArtRevision++;
        Artwork = null;
        ArtEditor?.CancelWork();
    }

    internal void PublishArtwork(PlaygroundArtwork result, int revision) {
        if (_previewDisposed || !IsArtwork || revision != ArtRevision) return;
        Artwork = result;
        ImageDataUri = result.PngUri;
        SvgDataUri = result.SvgUri;
        ErrorMessage = null;
        StateHasChanged();
    }

    internal Task ArtworkEdited() => QueueGenerateAsync();
    internal void ArtworkWorkChanged() { if (!_previewDisposed) StateHasChanged(); }

    internal void UseScenePayload(string payload) {
        // Recipes contain a plain payload. Opening one selects that data path explicitly.
        SelectedCategory = "QR";
        Content = payload;
    }
}

/// <summary>The current design and the exact payload retained by its downloadable artifacts.</summary>
public sealed record PlaygroundArtwork(string PngUri, string? SvgUri, string Payload, string Description,
    QrImageValidationReport? Validation = null, QrSceneComposition? Scene = null, string? RecipeUri = null);
