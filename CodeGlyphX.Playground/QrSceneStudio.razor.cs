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
    private string _seedText = "17", _status = "Ready to render.";
    private string? _error;
    private bool _busy, _disposed;
    private readonly List<QrSceneOptions> _history = new();
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _cancel;
    private bool SeedValid => int.TryParse(_seedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
    [CascadingParameter] public Playground? Host { get; set; }
    private string _payload => Host?.CurrentQrPayload() ?? "";
    internal bool Busy => _busy;
    private string? RecipeUri {
        get {
            try { return DataUri("application/xml", new QrSceneRecipe(_payload, _design).ToXml()); }
            catch (ArgumentException) { return null; }
        }
    }

    private void Remember() {
        _history.Add(_design.Clone());
        if (_history.Count > 20) _history.RemoveAt(0);
    }
    private async Task Edit(Action<QrSceneOptions> change) {
        if (_busy) return;
        try {
            var next = _design.Clone(); change(next); next = next.Clone();
            Remember(); _design = next; _seedText = next.Seed.ToString(CultureInfo.InvariantCulture); _error = null;
            _status = "Design edited.";
            await Edited();
        } catch (ArgumentException ex) { _error = ex.Message; }
    }
    private Task ChoosePreset(QrSceneStyle style) => Edit(d => CopyPreset(d, style));
    private static void CopyPreset(QrSceneOptions target, QrSceneStyle style) {
        var preset = QrScenePresets.Create(style, target.Size);
        target.Style = style; target.Colors = preset.Colors; target.Paper = preset.Paper; target.Ink = preset.Ink;
        target.Caption = preset.Caption; target.ModuleShape = preset.ModuleShape; target.Backdrop = preset.Backdrop; target.Motifs = preset.Motifs;
        target.Qr = preset.Qr; target.CaptionLayer = preset.CaptionLayer; target.Logo = preset.Logo; target.LogoImage = null;
    }
    private Task ChangeCaption(ChangeEventArgs e) => Edit(d => d.Caption = e.Value?.ToString() ?? "");
    private async Task ChangeSeed(ChangeEventArgs e) {
        _seedText = e.Value?.ToString() ?? "";
        if (int.TryParse(_seedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seed)) await Edit(d => d.Seed = seed);
        else await Edited();
    }
    private Task NextSeed() => Edit(d => d.Seed = d.Seed == int.MaxValue ? int.MinValue : d.Seed + 1);
    private Task ChangeSize(ChangeEventArgs e) => Edit(d => d.Size = int.Parse(e.Value!.ToString()!, CultureInfo.InvariantCulture));
    private Task ChangeShape(ChangeEventArgs e) => Edit(d => d.ModuleShape = Enum.Parse<QrModuleShape>(e.Value!.ToString()!));
    private Task ChangeColor(int index, ChangeEventArgs e) => Edit(d => d.Colors[index] = ParseColor(e));
    private Task ChangePaper(ChangeEventArgs e) => Edit(d => d.Paper = ParseColor(e));
    private Task ChangeInk(ChangeEventArgs e) => Edit(d => d.Ink = ParseColor(e));
    private Task ChangeQr(QrSceneLayerOptions value) => Edit(d => d.Qr = value);
    private Task ChangeBackdrop(QrSceneLayerOptions value) => Edit(d => d.Backdrop = value);
    private Task ChangeMotifs(QrSceneLayerOptions value) => Edit(d => d.Motifs = value);
    private Task ChangeCaptionLayer(QrSceneLayerOptions value) => Edit(d => d.CaptionLayer = value);
    private Task ChangeLogo(QrSceneLayerOptions value) => Edit(d => d.Logo = value);
    private Task ClearLogo() => Edit(d => d.LogoImage = null);
    private async Task Undo() {
        if (_busy || _history.Count == 0) return;
        var previous = _history[^1]; _history.RemoveAt(_history.Count - 1);
        _design = previous; _seedText = _design.Seed.ToString(CultureInfo.InvariantCulture); _error = null;
        await Edited();
    }
    private async Task Reset() {
        if (_busy) return;
        Remember(); _design = QrScenePresets.Create(_design.Style, _design.Size); _seedText = "17"; _error = null;
        _status = "Scene reset. QR data is retained.";
        await Edited();
    }
    private void ApplyRecipe(QrSceneRecipe recipe) {
        Host?.UseScenePayload(recipe.Payload); _design = recipe.Design; _seedText = _design.Seed.ToString(CultureInfo.InvariantCulture); _error = null;
    }
    private async Task LoadRecipe(InputFileChangeEventArgs args) {
        if (_busy || Host is null || !Host.IsArtwork) return;
        var revision = Host.ArtRevision;
        using var upload = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _cancel = upload;
        _busy = true; _error = null;
        Host.ArtworkWorkChanged();
        var loaded = false;
        try {
            using var stream = OpenUpload(args.File, QrSceneRecipe.MaxCharacters * 4, upload.Token);
            using var reader = new StreamReader(stream, new UTF8Encoding(false, true), true);
            var xml = await reader.ReadToEndAsync(upload.Token);
            if (!RecipeContextMatches(revision)) return;
            var recipe = QrSceneRecipe.FromXml(xml);
            var design = recipe.Design;
            // The core renderer validates QR capacity and visible logo pixels before shared state changes.
            _ = QrArt.ComposeScene(recipe.Payload, design, upload.Token);
            if (design.LogoImage is { } logo && !design.Logo.Visible)
                _ = ImageReader.DecodeRgba32(logo, new ImageDecodeOptions { MaxBytes = 1024 * 1024, MaxPixels = 1_000_000, MaxDecodedBytes = 16_000_000 }, out _, out _);
            if (!RecipeContextMatches(revision)) return;
            Remember(); ApplyRecipe(recipe); loaded = true; _status = "Recipe opened.";
        } catch (OperationCanceledException) { if (RecipeContextMatches(revision)) _status = "Recipe open cancelled."; }
        catch (Exception ex) { if (RecipeContextMatches(revision)) _error = ex.Message; }
        finally { _busy = false; _cancel = null; if (!_disposed) Host?.ArtworkWorkChanged(); }
        if (!_disposed && loaded) await Edited();
    }
    private bool RecipeContextMatches(int revision) => !_disposed && Host?.IsArtwork == true && revision == Host.ArtRevision;
    private async Task LoadLogo(InputFileChangeEventArgs args) {
        if (_busy) return;
        _busy = true; _error = null;
        var loaded = false;
        try {
            using var stream = OpenUpload(args.File, 1024 * 1024, _lifetime.Token); using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, _lifetime.Token);
            var logo = memory.ToArray();
            _ = ImageReader.DecodeRgba32(logo, new ImageDecodeOptions { MaxBytes = 1024 * 1024, MaxPixels = 1_000_000, MaxDecodedBytes = 16_000_000 }, out _, out _);
            if (_disposed) return;
            Remember(); _design.LogoImage = logo; loaded = true; _status = "Logo added.";
        } catch (Exception ex) { if (!_disposed) _error = ex.Message; }
        finally { _busy = false; }
        if (!_disposed && loaded) await Edited();
    }
    // Encoded stream limits are paired with core recipe and decoded-image limits.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "S5693:Make sure that this file upload is safe", Justification = "Upload sizes are fixed by the two callers; parsing and image decoding enforce independent limits.")]
    private static Stream OpenUpload(IBrowserFile file, long limit, CancellationToken cancellationToken) => file.OpenReadStream(limit, cancellationToken);

    private async Task Render() {
        if (_busy || !SeedValid || Host is null || !Host.IsArtwork || string.IsNullOrWhiteSpace(_payload)) return;
        Host.InvalidateArtwork();
        var revision = Host.ArtRevision;
        _busy = true; _error = null; _status = "Rendering and checking the exact PNG…";
        _cancel = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        Host.ArtworkWorkChanged();
        try {
            await Task.Delay(1, _cancel.Token);
            var result = QrArt.ComposeScene(_payload, _design, _cancel.Token);
            var png = result.ToPng();
            var validation = await QrArt.ValidateImageAsync(png, result.Payload, 2000, cancellationToken: _cancel.Token);
            if (_disposed || revision != Host.ArtRevision) return;
            Publish(result, validation, revision);
            _status = "Scene checked. The main preview and downloads use this design.";
        } catch (OperationCanceledException) { if (!_disposed) _status = "Render cancelled."; }
        catch (Exception ex) { if (!_disposed) { _error = ex.Message; _status = "Adjust the design and render again."; } }
        finally { _busy = false; _cancel.Dispose(); _cancel = null; if (!_disposed) Host?.ArtworkWorkChanged(); }
    }
    private void Cancel() => _cancel?.Cancel();
    internal void CancelWork() => _cancel?.Cancel();
    private Task Edited() => Host?.ArtworkEdited() ?? Task.CompletedTask;

    internal Task RenderPreviewAsync(string payload, int revision) {
        if (_disposed || !SeedValid || Host is null) return Task.CompletedTask;
        var result = QrArt.ComposeScene(payload, _design, _lifetime.Token);
        _error = null;
        _status = "Preview updated. Check scene to test scanning.";
        Publish(result, null, revision);
        return Task.CompletedTask;
    }

    private void Publish(QrSceneComposition result, QrImageValidationReport? validation, int revision) {
        Host?.PublishArtwork(new PlaygroundArtwork("data:image/png;base64," + Convert.ToBase64String(result.ToPng()), null, result.Payload,
            $"{Name(result.Design.Style)} · seed {result.Design.Seed}", validation, result, DataUri("application/xml", result.ToRecipe().ToXml())), revision);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (firstRender && Host?.IsArtwork == true) await Host.GenerateCode();
    }
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
