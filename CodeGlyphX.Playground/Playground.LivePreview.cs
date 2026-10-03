using System;
using System.Threading;
using System.Threading.Tasks;

namespace CodeGlyphX.Playground;

public partial class Playground : IDisposable {
    private CancellationTokenSource? _previewUpdate;
    private bool _previewDisposed;

    internal bool IsPreviewUpdating => _previewUpdate is not null;

    // Coalesce typing and color-picker drags before doing synchronous image rendering.
    internal async Task QueueGenerateAsync() {
        if (_previewDisposed || SelectedMode != "Generate") return;

        CancelPreviewUpdate();
        var update = new CancellationTokenSource();
        _previewUpdate = update;
        StateHasChanged();
        try {
            await Task.Delay(150, update.Token);
            if (!_previewDisposed && ReferenceEquals(_previewUpdate, update)) GenerateCode();
        }
        catch (OperationCanceledException) when (update.IsCancellationRequested) {
            // A newer edit, explicit generation or navigation superseded this update.
        }
        finally {
            if (ReferenceEquals(_previewUpdate, update)) _previewUpdate = null;
            update.Dispose();
        }
    }

    private void CancelPreviewUpdate() {
        var update = _previewUpdate;
        _previewUpdate = null;
        update?.Cancel();
    }

    /// <summary>Cancel queued preview work when the playground leaves the page.</summary>
    public void Dispose() {
        _previewDisposed = true;
        CancelPreviewUpdate();
    }
}
