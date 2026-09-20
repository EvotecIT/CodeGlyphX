using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;
using Microsoft.AspNetCore.Components.Forms;

namespace CodeGlyphX.Playground;

public partial class QrArtStudio : IDisposable {
    private byte[]? _source;
    private string? _sourceUri, _error;
    private string _payload = "https://example.com/art", _status = "Loading sample artwork…";
    private QrImageArtStyle _style = QrImageArtStyle.Botanical;
    private bool _busy, _loading, _protect = true, _disposed;
    private double _subjectX = 0.5, _subjectY = 0.5, _radius = 0.25, _cropX = 0.5, _cropY = 0.5, _zoom = 1, _qrX = 0.5, _qrY = 0.5;
    private double _printMm = 40;
    private int _screenSize = 320, _dpi = 150, _sourceWidth = 1, _sourceHeight = 1;
    private QrImageProtectionMask? _mask;
    private QrImageSearchResult? _result;
    private List<ArtCard> _cards = new();
    private CancellationTokenSource? _cancel;
    private readonly CancellationTokenSource _lifetime = new();
    private static ImageDecodeOptions InputLimits() => new() { MaxBytes = 10 * 1024 * 1024, MaxPixels = 4_000_000 };
    private string FocalStyle => FormattableString.Invariant($"left:{_subjectX * 100}%;top:{_subjectY * 100}%;width:{2 * _radius * Math.Min(_sourceWidth, _sourceHeight) / _sourceWidth * 100}%;height:{2 * _radius * Math.Min(_sourceWidth, _sourceHeight) / _sourceHeight * 100}%");

    protected override async Task OnInitializedAsync() {
        try {
            using var stream = typeof(QrArtStudio).Assembly.GetManifestResourceStream("CodeGlyphX.Playground.Art.Earth.jpg");
            if (stream is null) throw new InvalidOperationException("Sample artwork is unavailable.");
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, _lifetime.Token);
            if (!_disposed && _source is null) SetSource(memory.ToArray());
        } catch (Exception) { if (!_disposed) _status = "Upload an image to begin."; }
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
        try { using var stream = OpenUpload(args.File); using var memory = new MemoryStream(); await stream.CopyToAsync(memory, _lifetime.Token); if (!_disposed) SetSource(memory.ToArray()); }
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
        _busy = true; _error = null; _status = "Comparing masks and encoding settings…";
        _cancel = new CancellationTokenSource();
        try {
            var options = new QrImageSearchOptions {
                AdditionalVersions = 1, ValidationCandidates = 3, Results = 3, DecodeBudgetMilliseconds = 2000,
                Composition = new QrImageCompositionOptions {
                    ModuleSize = 12, Strength = 0.95, ImagePositionX = _cropX, ImagePositionY = _cropY, ImageZoom = _zoom,
                    Canvas = new QrImageCanvasOptions { PaddingModules = 8, PositionX = _qrX, PositionY = _qrY },
                    Art = new QrImageArtOptions {
                        Style = _style, FunctionalForeground = new Rgba32(20, 35, 40), FunctionalBackground = new Rgba32(245, 240, 225),
                        Subject = _protect ? new QrImageSubjectOptions { X = _subjectX, Y = _subjectY, Radius = _radius, Mask = _mask } : null
                    }
                }
            };
            var progress = new Progress<int>(count => {
                if (_disposed || !_busy) return;
                _status = $"Compared {count} combinations; scan checks follow the visual search…";
                StateHasChanged();
            });
            var result = await QrArt.SearchImageAsync(_payload, _source, options, InputLimits(), _cancel.Token, progress);
            if (_disposed) return;
            _result = result;
            _cards = result.Candidates.Select(c => new ArtCard(c, "data:image/png;base64," + Convert.ToBase64String(c.Image.ToPng()), _payload)).ToList();
            _status = "Comparison complete. Download an alternative or check its delivery settings.";
        } catch (OperationCanceledException) { _status = "Comparison cancelled."; }
        catch (Exception ex) { _error = ex.Message; _status = "Comparison could not finish."; }
        finally { _busy = false; _cancel.Dispose(); _cancel = null; }
    }
    private async Task CheckDelivery(ArtCard card) {
        if (_busy) return;
        _busy = true; _error = null; _status = "Checking delivery simulations…";
        _cancel = new CancellationTokenSource();
        try {
            await Task.Delay(1, _cancel.Token);
            card.Delivery = await QrArt.ValidateDeliveryAsync(card.Candidate.Image.ToPng(), card.Payload,
                new QrImageDeliveryOptions { ScreenSize = _screenSize, PrintMillimeters = _printMm, PrintDpi = _dpi, DecodeBudgetMilliseconds = 2000 }, cancellationToken: _cancel.Token);
            _status = "Delivery checks complete.";
        } catch (OperationCanceledException) { _status = "Delivery checks cancelled."; }
        catch (Exception ex) { _error = ex.Message; }
        finally { _busy = false; _cancel.Dispose(); _cancel = null; }
    }
    private void Cancel() => _cancel?.Cancel();
    public void Dispose() {
        if (_disposed) return;
        _disposed = true; _cancel?.Cancel(); _lifetime.Cancel(); _lifetime.Dispose();
    }
    private static string StyleName(QrImageArtStyle style) => style == QrImageArtStyle.ModuleShape ? "Rounded modules" : style.ToString();
    private static string CheckSummary(QrImageValidationReport report) => $"{report.Checks.Count(c => c.Passed)} / {report.Checks.Count} scan checks passed";
    private sealed class ArtCard(QrImageCandidate candidate, string uri, string payload) {
        internal string Payload { get; } = payload;
        internal QrImageCandidate Candidate { get; } = candidate;
        internal string Uri { get; } = uri;
        internal QrImageValidationReport? Delivery { get; set; }
    }
}
