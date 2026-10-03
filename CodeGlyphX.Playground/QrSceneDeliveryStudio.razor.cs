using System.Globalization;
using System.Text;
using System.Security.Cryptography;
using CodeGlyphX.Rendering.Art;
using Microsoft.AspNetCore.Components;

namespace CodeGlyphX.Playground;

public partial class QrSceneDeliveryStudio : IAsyncDisposable {
    [Parameter, EditorRequired] public QrSceneComposition Scene { get; set; } = default!;
    private string _widthText = "100", _dpiText = "300", _screenText = "320", _status = "No physical exports prepared yet.";
    private int _compression = 6;
    private bool _busy, _disposed;
    private string? _error, _pngUri, _svgUri, _pdfUri, _recipeUri, _reportUri;
    private QrSceneExport? _export;
    private QrImageValidationReport? _report;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _cancel;
    private QrSceneExportOptions Settings => new() { WidthMillimeters = double.Parse(_widthText, CultureInfo.InvariantCulture), Dpi = int.Parse(_dpiText, CultureInfo.InvariantCulture), PngCompressionLevel = _compression };
    private bool SettingsValid {
        get {
            if (!double.TryParse(_widthText, NumberStyles.Float, CultureInfo.InvariantCulture, out _) || !int.TryParse(_dpiText, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)) return false;
            try { _ = Settings.PixelSize; return true; } catch (ArgumentException) { return false; }
        }
    }
    private bool ScreenValid => int.TryParse(_screenText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var size) && size >= 32 && size <= 2048;
    private Task Prepare() => Run(false);
    private Task Search() => Run(true);
    private async Task Run(bool search) {
        if (_busy || !SettingsValid || !ScreenValid) return;
        _busy = true; _error = null; _status = search ? "Comparing up to six QR placements against delivery checks…" : "Preparing and checking the exact print PNG…";
        _cancel = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        try {
            var settings = Settings;
            var delivery = new QrImageDeliveryOptions { ScreenSize = int.Parse(_screenText, CultureInfo.InvariantCulture), PrintMillimeters = settings.WidthMillimeters, PrintDpi = settings.Dpi, DecodeBudgetMilliseconds = 2000 };
            await Task.Delay(1, _cancel.Token);
            var export = Scene.Export(settings, _cancel.Token);
            QrImageValidationReport report;
            var selection = "";
            if (search) {
                var result = await QrArt.SearchSceneAsync(export.Scene.Payload, export.Scene.Design, new QrSceneSearchOptions { Delivery = delivery }, _cancel.Token);
                export = result.Best.Scene.Export(settings, _cancel.Token);
                report = await QrArt.ValidateDeliveryAsync(export.ToPng(), export.Scene.Payload, delivery, cancellationToken: _cancel.Token);
                selection = $" Compared {result.ValidatedCandidates} candidates; {result.RejectedPlacements} placements did not fit. Selected QR scale {export.Scene.Design.Qr.Scale:0.##} / {export.Scene.Design.ModuleShape}.";
            } else report = await QrArt.ValidateDeliveryAsync(export.ToPng(), export.Scene.Payload, delivery, cancellationToken: _cancel.Token);
            _cancel.Token.ThrowIfCancellationRequested();
            var pngBytes = export.ToPng();
            var png = BinaryUri("image/png", pngBytes);
            var svg = TextUri("image/svg+xml", export.ToSvg());
            await Task.Delay(1, _cancel.Token);
            var pdf = BinaryUri("application/pdf", export.ToPdf());
            var recipe = TextUri("application/xml", export.Scene.ToRecipe().ToXml());
            var reportText = BuildReport(export, report, delivery, Convert.ToHexString(SHA256.HashData(pngBytes)));
            if (_disposed) return;
            _export = export; _report = report; _pngUri = png; _svgUri = svg; _pdfUri = pdf; _recipeUri = recipe; _reportUri = TextUri("text/plain", reportText);
            _status = (report.AllPassed ? "All software delivery checks passed." : "Some software delivery checks failed. Review the requested sizes.") + selection;
        } catch (OperationCanceledException) { if (!_disposed) _status = "Delivery check cancelled. Previous downloads are retained."; }
        catch (Exception ex) { if (!_disposed) { _error = ex.Message; _status = "Adjust delivery settings and try again. Previous downloads are retained."; } }
        finally { _busy = false; _cancel.Dispose(); _cancel = null; }
    }
    private static string BuildReport(QrSceneExport export, QrImageValidationReport report, QrImageDeliveryOptions delivery, string pngHash) {
        var text = new StringBuilder();
        text.AppendLine("CodeGlyphX scene delivery report").AppendLine("Payload: " + export.Scene.Payload)
            .AppendLine("PNG SHA256: " + pngHash)
            .AppendLine($"Export: {export.Scene.Image.Size} px; {export.WidthMillimeters.ToString(CultureInfo.InvariantCulture)} mm; {export.Dpi} DPI")
            .AppendLine($"Screen: {delivery.ScreenSize} px; JPEG quality: {delivery.JpegQuality}; perspective inset: {delivery.PerspectiveInset.ToString(CultureInfo.InvariantCulture)}");
        foreach (var check in report.Checks) text.AppendLine($"{check.Name}: {(check.Passed ? "PASS" : "FAIL")} ({check.Width} x {check.Height} px), exact payload match required");
        return text.AppendLine("Software simulations only. Physical phone/camera/print qualification remains necessary.").ToString();
    }
    private void Cancel() => _cancel?.Cancel();
    public async ValueTask DisposeAsync() { if (_disposed) return; _disposed = true; _cancel?.Cancel(); await _lifetime.CancelAsync(); _lifetime.Dispose(); }
    private static string BinaryUri(string type, byte[] bytes) => "data:" + type + ";base64," + Convert.ToBase64String(bytes);
    private static string TextUri(string type, string text) => BinaryUri(type, Encoding.UTF8.GetBytes(text));
}
