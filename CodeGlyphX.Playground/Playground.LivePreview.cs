using System;
using System.Threading;
using System.Threading.Tasks;

namespace CodeGlyphX.Playground;

public partial class Playground : IDisposable {
    private CancellationTokenSource? _previewUpdate;
    private bool _previewDisposed;

    internal bool IsPreviewUpdating => _previewUpdate is not null || (IsArtwork && ArtEditor?.Busy == true);

    // Coalesce typing and color-picker drags before doing synchronous image rendering.
    internal async Task QueueGenerateAsync() {
        if (_previewDisposed || SelectedMode != "Generate") return;

        CancelPreviewUpdate();
        if (IsArtwork) {
            ResetOutputs();
        }
        var update = new CancellationTokenSource();
        _previewUpdate = update;
        StateHasChanged();
        try {
            await Task.Delay(150, update.Token);
            if (!_previewDisposed && ReferenceEquals(_previewUpdate, update)) await GenerateCode();
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
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Release queued preview work when disposing managed resources.</summary>
    protected virtual void Dispose(bool disposing) {
        if (!disposing || _previewDisposed) return;
        _previewDisposed = true;
        CancelPreviewUpdate();
        InvalidateArtwork();
        _decodeCts?.Cancel();
    }
}
