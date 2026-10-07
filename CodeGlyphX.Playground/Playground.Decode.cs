using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;

namespace CodeGlyphX.Playground;

public partial class Playground {
    internal async Task OnDecodeFileChanged(InputFileChangeEventArgs args) {
        _decodeCts?.Cancel();
        _decodeCts = null;
        IsDecoding = false;
        ResetOutputs();
        IsDropActive = false;
        var file = args.File;
        if (file is null) return;

        var formats = SymbolCapabilities.All
            .Where(capability => capability.CanScanImages &&
                (capability.Format is SymbolFormat.QrCode or SymbolFormat.MicroQrCode ? DecodeQr :
                 capability.Family == SymbolFamily.Linear ? DecodeBarcode : DecodeMatrix))
            .Select(capability => capability.Format)
            .ToArray();
        if (formats.Length == 0) {
            DecodeError = "Choose at least one symbol family to decode.";
            return;
        }

        var operation = new CancellationTokenSource();
        _decodeCts = operation;
        IsDecoding = true;
        try {
            using var stream = file.OpenReadStream(15 * 1024 * 1024, operation.Token);
            using var buffer = new System.IO.MemoryStream();
            await stream.CopyToAsync(buffer, operation.Token);
            var data = buffer.ToArray();
            if (!ReferenceEquals(_decodeCts, operation)) return;
            DecodeImageDataUri = $"data:{file.ContentType};base64,{Convert.ToBase64String(data)}";
            await UpdateDecodeStatusAsync("Decoding...");

            var options = new ScanOptions {
                Formats = formats,
                TimeoutMilliseconds = DecodeBudgetMilliseconds,
                MaxSymbols = DecodeStopAfterFirst ? 1 : 0,
                CancellationToken = operation.Token,
                Profile = DecodeQualityPreset switch {
                    "Speed" => ScanProfile.Fast,
                    "Quality" => ScanProfile.Robust,
                    _ => ScanProfile.Balanced
                },
                EnableTileScan = true,
                Qr = new QrPixelDecodeOptions {
                    Profile = QrDecodeProfile.Robust,
                    AggressiveSampling = true,
                    StylizedSampling = true,
                    EnableTileScan = true,
                    MaxDimension = DecodeDownscale ? DecodeMaxDimension : 0
                },
                Barcode = new BarcodeDecodeOptions { EnableTileScan = !DecodeStopAfterFirst },
                Image = ImageDecodeOptions.Strict(
                    maxBytes: 15 * 1024 * 1024,
                    maxPixels: 16_000_000,
                    maxDimension: DecodeDownscale ? DecodeMaxDimension : 0)
            };
            // Scheduling belongs to the UI; recognition, tiling and deadlines belong to the scanner.
            var scan = await Task.Run(() => SymbolScanner.Scan(data, options));
            if (!ReferenceEquals(_decodeCts, operation)) return;
            foreach (var symbol in scan.Symbols) {
                AddDecodeResult(SymbolCapabilities.Get(symbol.Format).DisplayName, symbol.Text);
            }
            DecodeStatus = scan.CompletionReason switch {
                ScanCompletionReason.Cancelled => $"Decode cancelled after {scan.Elapsed.TotalMilliseconds:0} ms.",
                ScanCompletionReason.DeadlineExceeded => $"Time budget exceeded after {scan.Elapsed.TotalMilliseconds:0} ms.",
                ScanCompletionReason.SymbolLimitReached => $"First match found in {scan.Elapsed.TotalMilliseconds:0} ms.",
                _ => $"Decode finished in {scan.Elapsed.TotalMilliseconds:0} ms."
            };
            if (scan.Symbols.Count == 0) {
                DecodeError = scan.Status switch {
                    ScanStatus.Cancelled => "Decode cancelled.",
                    ScanStatus.DeadlineExceeded => "Time budget exceeded.",
                    ScanStatus.InvalidImage => "The image could not be decoded within the configured image limits.",
                    ScanStatus.UnsupportedFormats => "The selected symbol formats do not support image recognition.",
                    _ => "No matching symbols found."
                };
            }
        }
        catch (OperationCanceledException) when (operation.IsCancellationRequested) {
            if (ReferenceEquals(_decodeCts, operation)) DecodeStatus = "Decode cancelled.";
        }
        catch (System.IO.IOException) {
            if (ReferenceEquals(_decodeCts, operation)) DecodeError = "The image could not be read. Upload a file smaller than 15 MB.";
        }
        finally {
            if (ReferenceEquals(_decodeCts, operation)) {
                _decodeCts = null;
                IsDecoding = false;
            }
            operation.Dispose();
        }
    }

    internal void AddDecodeResult(string type, string text) {
        if (DecodeResults.Any(result => result.Type == type && result.Text == text)) return;
        DecodeResults.Add(new DecodeResult(type, text));
    }

    internal async Task CancelDecode() {
        if (_decodeCts is not null) await _decodeCts.CancelAsync();
        DecodeStatus = "Cancelling...";
    }

    internal async Task UpdateDecodeStatusAsync(string status) {
        DecodeStatus = status;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();
    }

    internal void ResetOutputs() {
        InvalidateArtwork();
        ErrorMessage = null;
        ImageDataUri = null;
        SvgDataUri = null;
        SvgUnavailableReason = null;
        HeuristicReport = null;
        DecodeImageDataUri = null;
        DecodeError = null;
        DecodeResults.Clear();
        DecodeStatus = string.Empty;
    }
}
