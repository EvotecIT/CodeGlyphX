using System.Globalization;
using System.Text;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace CodeGlyphX.Playground;

public partial class QrSceneStudio : IAsyncDisposable {
    private QrSceneOptions _design = QrScenePresets.Create(QrSceneStyle.TropicalGarden);
    private string _payload = "https://example.com/scenes", _seedText = "17", _status = "Ready to render.";
    private string? _error, _pngUri;
    private bool _busy, _disposed;
    private QrSceneComposition? _result;
    private QrImageValidationReport? _validation;
    private readonly List<QrSceneRecipe> _history = new();
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _cancel;
    private bool SeedValid => int.TryParse(_seedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
    private string RecipeUri => DataUri("application/xml", new QrSceneRecipe(_payload, _design).ToXml());
    private string? ResultRecipeUri => _result is null ? null : DataUri("application/xml", _result.ToRecipe().ToXml());

    private void Remember() {
        _history.Add(new QrSceneRecipe(_payload, _design));
        if (_history.Count > 20) _history.RemoveAt(0);
    }
    private void Edit(Action<QrSceneOptions> change) {
        if (_busy) return;
        try {
            var next = _design.Clone(); change(next); next = next.Clone();
            Remember(); _design = next; _seedText = next.Seed.ToString(CultureInfo.InvariantCulture); _error = null;
            _status = "Design edited. Render to update the preview.";
        } catch (ArgumentException ex) { _error = ex.Message; }
    }
    private void ChoosePreset(QrSceneStyle style) => Edit(d => CopyPreset(d, style));
    private static void CopyPreset(QrSceneOptions target, QrSceneStyle style) {
        var preset = QrScenePresets.Create(style, target.Size);
        target.Style = style; target.Colors = preset.Colors; target.Paper = preset.Paper; target.Ink = preset.Ink;
        target.Caption = preset.Caption; target.ModuleShape = preset.ModuleShape; target.Backdrop = preset.Backdrop; target.Motifs = preset.Motifs;
        target.Qr = preset.Qr; target.CaptionLayer = preset.CaptionLayer; target.Logo = preset.Logo; target.LogoImage = null;
    }
    private void ChangePayload(ChangeEventArgs args) {
        if (_busy) return;
        try { var value = args.Value?.ToString() ?? ""; _ = new QrSceneRecipe(value, _design); Remember(); _payload = value; _error = null; }
        catch (ArgumentException ex) { _error = ex.Message; }
    }
    private void ChangeCaption(ChangeEventArgs e) => Edit(d => d.Caption = e.Value?.ToString() ?? "");
    private void ChangeSeed(ChangeEventArgs e) {
        _seedText = e.Value?.ToString() ?? "";
        if (int.TryParse(_seedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seed)) Edit(d => d.Seed = seed);
    }
    private void NextSeed() => Edit(d => d.Seed = d.Seed == int.MaxValue ? int.MinValue : d.Seed + 1);
    private void ChangeSize(ChangeEventArgs e) => Edit(d => d.Size = int.Parse(e.Value!.ToString()!, CultureInfo.InvariantCulture));
    private void ChangeShape(ChangeEventArgs e) => Edit(d => d.ModuleShape = Enum.Parse<QrPngModuleShape>(e.Value!.ToString()!));
    private void ChangeColor(int index, ChangeEventArgs e) => Edit(d => d.Colors[index] = ParseColor(e));
    private void ChangePaper(ChangeEventArgs e) => Edit(d => d.Paper = ParseColor(e));
    private void ChangeInk(ChangeEventArgs e) => Edit(d => d.Ink = ParseColor(e));
    private void ChangeQr(QrSceneLayerOptions value) => Edit(d => d.Qr = value);
    private void ChangeBackdrop(QrSceneLayerOptions value) => Edit(d => d.Backdrop = value);
    private void ChangeMotifs(QrSceneLayerOptions value) => Edit(d => d.Motifs = value);
    private void ChangeCaptionLayer(QrSceneLayerOptions value) => Edit(d => d.CaptionLayer = value);
    private void ChangeLogo(QrSceneLayerOptions value) => Edit(d => d.Logo = value);
    private void ClearLogo() => Edit(d => d.LogoImage = null);
    private void Undo() {
        if (_busy || _history.Count == 0) return;
        var previous = _history[^1]; _history.RemoveAt(_history.Count - 1);
        ApplyRecipe(previous); _status = "Previous edit restored. Render to update the preview.";
    }
    private void Reset() {
        if (_busy) return;
        Remember(); _design = QrScenePresets.Create(_design.Style, _design.Size); _seedText = "17"; _error = null;
        _status = "Scene reset. QR text is retained.";
    }
    private void ApplyRecipe(QrSceneRecipe recipe) {
        _payload = recipe.Payload; _design = recipe.Design; _seedText = _design.Seed.ToString(CultureInfo.InvariantCulture); _error = null;
    }
    private async Task LoadRecipe(InputFileChangeEventArgs args) {
        if (_busy) return;
        _busy = true; _error = null;
        try {
            using var stream = OpenUpload(args.File, QrSceneRecipe.MaxCharacters * 4);
            using var reader = new StreamReader(stream, new UTF8Encoding(false, true), true);
            var xml = await reader.ReadToEndAsync(_lifetime.Token);
            var recipe = QrSceneRecipe.FromXml(xml);
            if (_disposed) return;
            Remember(); ApplyRecipe(recipe); _status = "Recipe opened. Render to update the preview.";
        } catch (Exception ex) { if (!_disposed) _error = ex.Message; }
        finally { _busy = false; }
    }
    private async Task LoadLogo(InputFileChangeEventArgs args) {
        if (_busy) return;
        _busy = true; _error = null;
        try {
            using var stream = OpenUpload(args.File, 1024 * 1024); using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, _lifetime.Token);
            var logo = memory.ToArray();
            _ = ImageReader.DecodeRgba32(logo, new ImageDecodeOptions { MaxBytes = 1024 * 1024, MaxPixels = 1_000_000, MaxDecodedBytes = 16_000_000 }, out _, out _);
            if (_disposed) return;
            Remember(); _design.LogoImage = logo; _status = "Logo added. Render to update the preview.";
        } catch (Exception ex) { if (!_disposed) _error = ex.Message; }
        finally { _busy = false; }
    }
    // Encoded stream limits are paired with core recipe and decoded-image limits.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "S5693:Make sure that this file upload is safe", Justification = "Upload sizes are fixed by the two callers; parsing and image decoding enforce independent limits.")]
    private Stream OpenUpload(IBrowserFile file, long limit) => file.OpenReadStream(limit, _lifetime.Token);

    private async Task Render() {
        if (_busy || !SeedValid) return;
        _busy = true; _error = null; _status = "Rendering and checking the exact PNG…";
        _cancel = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        try {
            await Task.Delay(1, _cancel.Token);
            var result = QrArt.ComposeScene(_payload, _design, _cancel.Token);
            var png = result.ToPng();
            var validation = await QrArt.ValidateImageAsync(png, result.Payload, 2000, cancellationToken: _cancel.Token);
            if (_disposed) return;
            _result = result; _pngUri = "data:image/png;base64," + Convert.ToBase64String(png); _validation = validation;
            _status = "Scene rendered. Downloads retain this design and QR text.";
        } catch (OperationCanceledException) { if (!_disposed) _status = "Render cancelled."; }
        catch (Exception ex) { if (!_disposed) { _error = ex.Message; _status = "Adjust the design and render again."; } }
        finally { _busy = false; _cancel.Dispose(); _cancel = null; }
    }
    private void Cancel() => _cancel?.Cancel();
    public async ValueTask DisposeAsync() {
        if (_disposed) return;
        _disposed = true; _cancel?.Cancel(); await _lifetime.CancelAsync(); _lifetime.Dispose();
    }
    private static string DataUri(string type, string text) => "data:" + type + ";base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
    private static string Color(Rgba32 color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    private static Rgba32 ParseColor(ChangeEventArgs args) {
        var color = args.Value?.ToString() ?? "";
        return new(byte.Parse(color.AsSpan(1, 2), NumberStyles.HexNumber), byte.Parse(color.AsSpan(3, 2), NumberStyles.HexNumber), byte.Parse(color.AsSpan(5, 2), NumberStyles.HexNumber));
    }
    internal static string Name(QrSceneStyle style) => style switch {
        QrSceneStyle.TropicalGarden => "Tropical garden", QrSceneStyle.ElectricCity => "Electric city", QrSceneStyle.MusicFestival => "Music festival",
        QrSceneStyle.OceanReef => "Ocean reef", QrSceneStyle.CosmicOrbit => "Cosmic orbit", _ => "Retro arcade"
    };
}
