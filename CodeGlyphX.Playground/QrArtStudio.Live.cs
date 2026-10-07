using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;
using Microsoft.AspNetCore.Components;

namespace CodeGlyphX.Playground;

public partial class QrArtStudio {
    [CascadingParameter] public Playground? Host { get; set; }
    private QrSceneStudio? _scene;
    internal bool Busy => _busy || _scene?.Busy == true;

    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (firstRender && Host?.IsArtwork == true) await Host.GenerateCode();
    }

    internal void CancelWork() {
        _cancel?.Cancel();
        _scene?.CancelWork();
        _cards.Clear();
        _result = null;
    }

    private Task Edited() => Host?.ArtworkEdited() ?? Task.CompletedTask;

    internal async Task RenderPreviewAsync(string payload, int revision) {
        if (_disposed || Host is null) return;
        if (_sourceKind == "scene") {
            if (_scene is not null) await _scene.RenderPreviewAsync(payload, revision);
            return;
        }
        if (_procedural && _patternDirty) SetPatternSource();
        if (_source is null) return;
        if (_moduleSize < 6 || _moduleSize > 64) throw new ArgumentException("Export pixels per module must be between 6 and 64.");
        var image = QrArt.Compose(payload, _source, CompositionOptions(), InputLimits());
        var illustration = _frameEnabled ? QrIllustratedComposer.Frame(image, _frameStyle, _lifetime.Token) : null;
        image = illustration?.Image ?? image;
        _error = null;
        _status = "Preview updated. Compare alternatives to check masks and scanning.";
        Host.PublishArtwork(new PlaygroundArtwork("data:image/png;base64," + Convert.ToBase64String(image.ToPng()),
            illustration is null ? null : "data:image/svg+xml;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(illustration.ToSvg())),
            payload, Description()), revision);
    }

    private string Description() => _procedural ? $"{QrPatternControls.Name(_patternSettings.Pattern)} · seed {_patternSettings.Seed}" : "Image artwork";

    private QrImageCompositionOptions CompositionOptions() => new() {
        ModuleSize = _moduleSize, Strength = _strength, ImagePositionX = _cropX, ImagePositionY = _cropY, ImageZoom = _zoom,
        Canvas = new QrImageCanvasOptions { PaddingModules = _frameEnabled ? 12 : 8, PositionX = _qrX, PositionY = _qrY },
        Art = new QrImageArtOptions {
            Style = _style, Finders = _finders, Shape = QrModuleShape.ConnectedRounded, Scale = _artScale, DetailProtection = _detail,
            FunctionalForeground = new(21, 26, 47), FunctionalBackground = new(255, 249, 239),
            Subject = !_procedural && _protect ? new QrImageSubjectOptions { X = _subjectX, Y = _subjectY, Radius = _radius, Mask = _mask } : null
        }
    };

    private void Select(QrArtStudioCard card) {
        if (Host is null || Busy || card.Payload != Host.CurrentQrPayload()) return;
        SelectCompleted(card, Host.ArtRevision);
    }

    private void SelectCompleted(QrArtStudioCard card, int revision) {
        Host?.PublishArtwork(new PlaygroundArtwork(card.Uri, card.SvgUri, card.Payload, Description(), card.Validation), revision);
    }
}
