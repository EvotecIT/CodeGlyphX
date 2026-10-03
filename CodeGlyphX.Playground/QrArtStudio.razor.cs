using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;
using Microsoft.AspNetCore.Components.Forms;

namespace CodeGlyphX.Playground;

public partial class QrArtStudio : IDisposable {
    private byte[]? _source, _imageSource;
    private string? _sourceUri, _error;
    private string _payload = "https://example.com/art", _status = "Loading sample artwork…";
    private bool _exploreLayouts, _frameEnabled;
    private string _sourceKind = "pattern";
    private bool _procedural => _sourceKind == "pattern";
    private QrPatternSettings _patternSettings = QrPatternSettings.FromPreset(QrArtPattern.Marble);
    private string _resultDescription = "";
    private double _strength = 0.96, _artScale = 0.95, _detail = 0.3;
    private int _moduleSize = 18;
    private QrIllustratedStyle _frameStyle = QrIllustratedStyle.BotanicalBadge;
    private QrImageFinderStyle _finders;
    private QrImageArtStyle _style = QrImageArtStyle.Botanical;
    private bool _busy, _loading, _protect = true, _disposed;
    private double _subjectX = 0.5, _subjectY = 0.5, _radius = 0.25, _cropX = 0.5, _cropY = 0.5, _zoom = 1, _qrX = 0.5, _qrY = 0.5;
    private double _printMm = 40;
    private int _screenSize = 320, _dpi = 150, _sourceWidth = 1, _sourceHeight = 1;
    private QrImageProtectionMask? _mask;
    private QrImageSearchResult? _result;
    private List<QrArtStudioCard> _cards = new();
    private CancellationTokenSource? _cancel;
    private readonly CancellationTokenSource _lifetime = new();
    private static ImageDecodeOptions InputLimits() => new() { MaxBytes = 10 * 1024 * 1024, MaxPixels = 4_000_000 };
    private string FocalStyle => FormattableString.Invariant($"left:{_subjectX * 100}%;top:{_subjectY * 100}%;width:{2 * _radius * Math.Min(_sourceWidth, _sourceHeight) / _sourceWidth * 100}%;height:{2 * _radius * Math.Min(_sourceWidth, _sourceHeight) / _sourceHeight * 100}%");

    protected override async Task OnInitializedAsync() {
        SetPatternSource();
        ApplyPatternTreatment();
        try {
            using var stream = typeof(QrArtStudio).Assembly.GetManifestResourceStream("CodeGlyphX.Playground.Art.Earth.jpg");
            if (stream is null) throw new InvalidOperationException("Sample artwork is unavailable.");
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, _lifetime.Token);
            if (!_disposed && _imageSource is null) {
                _imageSource = memory.ToArray();
                if (!_procedural) SetSource(_imageSource);
            }
        } catch (Exception) { if (!_disposed && !_procedural) _status = "Upload an image to begin."; }
    }
    private void SetPatternSource() {
        var image = QrArt.RenderPatternPng(_patternSettings.ToOptions(), cancellationToken: _lifetime.Token);
        _source = image; _sourceWidth = 512; _sourceHeight = 512;
        _sourceUri = "data:image/png;base64," + Convert.ToBase64String(image);
        _error = null; _status = "Ready to compare.";
    }
    private void ChangePattern(QrPatternSettings settings) {
        var previous = _patternSettings;
        try {
            _patternSettings = settings;
            SetPatternSource();
            if (previous.Pattern != settings.Pattern && !_frameEnabled) ApplyPatternTreatment();
        } catch (Exception ex) { _patternSettings = previous; _error = ex.Message; }
    }
    private void ChangeSource() {
        _result = null; _cards.Clear(); _mask = null;
        if (_sourceKind == "scene") { _error = null; return; }
        if (_procedural) { SetPatternSource(); if (!_frameEnabled) ApplyPatternTreatment(); }
        else if (_imageSource is not null) { SetSource(_imageSource); if (!_frameEnabled) _style = QrImageArtStyle.Botanical; }
        else { _source = null; _sourceUri = null; _status = "Upload an image to begin."; }
    }
    private void ApplyPatternTreatment() {
        var art = QrArtPatternPresets.CreateCompositionOptions(_patternSettings.Pattern).Art!;
        _style = art.Style; _finders = art.Finders;
    }
    private void SetSource(byte[] image) {
        var pixels = ImageReader.DecodeRgba32(image, InputLimits(), out var width, out var height);
        var png = PngImageEncoder.EncodeRgba32(pixels, width, height);
        _sourceWidth = width; _sourceHeight = height; _source = image;
        _sourceUri = "data:image/png;base64," + Convert.ToBase64String(png);
        _result = null; _cards.Clear(); _mask = null; _error = null; _status = "Ready to compare.";
    }
    private async Task LoadImage(InputFileChangeEventArgs args) {
        if (_busy || _loading) return;
        _loading = true;
        try {
            using var stream = OpenUpload(args.File); using var memory = new MemoryStream(); await stream.CopyToAsync(memory, _lifetime.Token);
            if (!_disposed) { var image = memory.ToArray(); SetSource(image); _imageSource = image; }
        }
        catch (Exception ex) { if (!_disposed) _error = ex.Message; }
        finally { _loading = false; }
    }
    private async Task LoadMask(InputFileChangeEventArgs args) {
        if (_busy || _loading) return;
        _loading = true;
        try {
            using var stream = OpenUpload(args.File); using var memory = new MemoryStream(); await stream.CopyToAsync(memory, _lifetime.Token);
            if (_disposed) return;
            var pixels = ImageReader.DecodeRgba32(memory.ToArray(), InputLimits(), out var width, out var height);
            var gray = new byte[width * height];
            for (var i = 0; i < gray.Length; i++) { var p = i * 4; gray[i] = (byte)Math.Round((0.299 * pixels[p] + 0.587 * pixels[p + 1] + 0.114 * pixels[p + 2]) * pixels[p + 3] / 255); }
            _mask = new QrImageProtectionMask(gray, width, height); _error = null;
        } catch (Exception ex) { if (!_disposed) _error = ex.Message; }
        finally { _loading = false; }
    }
    // The intentional 10 MiB encoded limit is paired with a 4 MP decoded limit.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "S5693:Make sure that this file upload is safe", Justification = "The stream enforces a fixed 10 MiB limit; decoding additionally enforces 4 million pixels.")]
    private Stream OpenUpload(IBrowserFile file) => file.OpenReadStream(10 * 1024 * 1024, _lifetime.Token);

    private void ClearMask() => _mask = null;
    private async Task Compare() {
        if (_source is null || _busy || _loading) return;
        if (_moduleSize < 6 || _moduleSize > 64) {
            _error = "Export pixels per module must be between 6 and 64.";
            _status = "Adjust the export size and compare again.";
            return;
        }
        _busy = true; _error = null; _status = "Comparing masks and encoding settings…";
        _cancel = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        var payload = _payload;
        var description = _procedural ? $"{QrPatternControls.Name(_patternSettings.Pattern)}, seed {_patternSettings.Seed}" : "uploaded image";
        try {
            var options = new QrImageSearchOptions {
                ExploreLayouts = _exploreLayouts, AdditionalVersions = 1, ValidationCandidates = 3, Results = 3, DecodeBudgetMilliseconds = 2000,
                Composition = new QrImageCompositionOptions {
                    ModuleSize = _moduleSize, Strength = _strength, ImagePositionX = _cropX, ImagePositionY = _cropY, ImageZoom = _zoom,
                    Canvas = new QrImageCanvasOptions { PaddingModules = _frameEnabled ? 12 : 8, PositionX = _qrX, PositionY = _qrY },
                    Art = new QrImageArtOptions {
                        Style = _style, Finders = _finders, Shape = QrPngModuleShape.ConnectedRounded, Scale = _artScale, DetailProtection = _detail,
                        FunctionalForeground = new Rgba32(21, 26, 47), FunctionalBackground = new Rgba32(255, 249, 239),
                        Subject = !_procedural && _protect ? new QrImageSubjectOptions { X = _subjectX, Y = _subjectY, Radius = _radius, Mask = _mask } : null
                    }
                }
            };
            var progress = new Progress<int>(count => {
                if (_disposed || !_busy) return;
                _status = $"Compared {count} combinations; scan checks follow the visual search…";
                StateHasChanged();
            });
            var result = await QrArt.SearchImageAsync(payload, _source, options, InputLimits(), _cancel.Token, progress);
            if (_disposed) return;
            var cards = new List<QrArtStudioCard>();
            foreach (var candidate in result.Candidates) {
                var illustration = _frameEnabled ? QrIllustratedComposer.Frame(candidate.Image, _frameStyle, _cancel.Token) : null;
                var image = illustration?.Image ?? candidate.Image;
                var png = image.ToPng();
                var validation = illustration is null ? candidate.Validation : await QrArt.ValidateImageAsync(png, payload, 2000, cancellationToken: _cancel.Token);
                cards.Add(new QrArtStudioCard(candidate, image, "data:image/png;base64," + Convert.ToBase64String(png), payload, validation,
                    illustration is null ? null : "data:image/svg+xml;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(illustration.ToSvg()))));
            }
            if (_disposed) return;
            _result = result;
            _resultDescription = description;
            _cards = cards.OrderByDescending(c => c.Validation.Checks.Count(check => check.Passed)).ThenByDescending(c => c.Candidate.Fidelity).ToList();
            _status = "Comparison complete. Download an alternative or check its delivery settings.";
        } catch (OperationCanceledException) { _status = "Comparison cancelled."; }
        catch (Exception ex) { _error = ex.Message; _status = "Comparison could not finish."; }
        finally { _busy = false; _cancel.Dispose(); _cancel = null; }
    }
    private async Task CheckDelivery(QrArtStudioCard card) {
        if (_busy || _loading) return;
        _busy = true; _error = null; _status = "Checking delivery simulations…";
        _cancel = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        try {
            await Task.Delay(1, _cancel.Token);
            card.Delivery = await QrArt.ValidateDeliveryAsync(card.Image.ToPng(), card.Payload,
                new QrImageDeliveryOptions { ScreenSize = _screenSize, PrintMillimeters = _printMm, PrintDpi = _dpi, DecodeBudgetMilliseconds = 2000 }, cancellationToken: _cancel.Token);
            _status = "Delivery checks complete.";
        } catch (OperationCanceledException) { _status = "Delivery checks cancelled."; }
        catch (Exception ex) { _error = ex.Message; }
        finally { _busy = false; _cancel.Dispose(); _cancel = null; }
    }
    private static string FamilyName(QrIllustratedStyle style) => style switch {
        QrIllustratedStyle.EngravedPortrait => "Engraved portrait",
        QrIllustratedStyle.BotanicalBadge => "Botanical badge",
        _ => "Geometric poster"
    };
    private void ApplyIllustration() {
        if (!_frameEnabled) { if (_procedural) ApplyPatternTreatment(); return; }
        var preset = QrIllustratedComposer.CreateOptions(_frameStyle);
        _style = preset.Art!.Style; _finders = preset.Art.Finders;
    }
    private void Cancel() => _cancel?.Cancel();
    public void Dispose() {
        if (_disposed) return;
        _disposed = true; _cancel?.Cancel(); _lifetime.Cancel(); _lifetime.Dispose();
    }
    private static string StyleName(QrImageArtStyle style) => style switch {
        QrImageArtStyle.ModuleShape => "Connected rounded modules", QrImageArtStyle.CrossStitch => "Cross stitch", _ => style.ToString()
    };
}
