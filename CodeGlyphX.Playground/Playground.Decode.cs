using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeGlyphX;
using CodeGlyphX.Aztec;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace CodeGlyphX.Playground;

public partial class Playground {

    internal async Task OnDecodeFileChanged(InputFileChangeEventArgs args)
    {
        ResetOutputs();
        DecodeResults.Clear();
        DecodeError = null;
        DecodeImageDataUri = null;
        DecodeStatus = string.Empty;

        var file = args.File;
        if (file is null)
            return;

        try
        {
            _decodeCts?.Cancel();
            _decodeCts?.Dispose();
            _decodeCts = new CancellationTokenSource();
            if (DecodeMaxMilliseconds > 0)
            {
                _decodeCts.CancelAfter(DecodeMaxMilliseconds);
            }
            var token = _decodeCts.Token;
            using var budgetScope = CodeGlyphBudget.Begin(DecodeMaxMilliseconds);

            using var stream = file.OpenReadStream(5 * 1024 * 1024);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var data = ms.ToArray();

            DecodeImageDataUri = $"data:{file.ContentType};base64,{Convert.ToBase64String(data)}";

            await UpdateDecodeStatusAsync("Preparing image...");
            if (!ImageReader.TryDecodeRgba32(data, out var rgba, out var width, out var height))
            {
                DecodeError = "Unsupported image format.";
                return;
            }

            if (DecodeDownscale && (width > DecodeMaxDimension || height > DecodeMaxDimension))
            {
                await UpdateDecodeStatusAsync("Downscaling image...");
                rgba = ResizeNearest(rgba, width, height, DecodeMaxDimension, out width, out height);
            }

            IsDecoding = true;
            await UpdateDecodeStatusAsync("Decoding...");

            var stride = width * 4;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var found = false;

            int RemainingBudget()
            {
                var remaining = DecodeMaxMilliseconds - (int)sw.ElapsedMilliseconds;
                return remaining < 0 ? 0 : remaining;
            }

            if (DecodeQr && !token.IsCancellationRequested)
            {
                await UpdateDecodeStatusAsync("Decoding QR...");
                var qrBudget = RemainingBudget();
                if (qrBudget <= 0)
                {
                    DecodeError = "Time budget exceeded.";
                    return;
                }
                var qrOptions = new QrPixelDecodeOptions
                {
                    Profile = QrDecodeProfile.Robust,
                    AggressiveSampling = true,
                    BudgetMilliseconds = DecodeMaxMilliseconds,
                    MaxDimension = DecodeDownscale ? DecodeMaxDimension : 0,
                    EnableTileScan = !DecodeStopAfterFirst
                };
                if (DecodeStopAfterFirst)
                {
                    if (QrDecoder.TryDecode(rgba, width, height, stride, PixelFormat.Rgba32, out var qr, qrOptions, token))
                    {
                        AddDecodeResult("QR", qr.Text);
                        found = true;
                        DecodeStatus = $"Decode finished in {sw.ElapsedMilliseconds} ms";
                        return;
                    }
                }
                else
                {
                    if (QrDecoder.TryDecodeAll(rgba, width, height, stride, PixelFormat.Rgba32, out var qrAll, qrOptions, token))
                    {
                        for (var i = 0; i < qrAll.Length; i++)
                        {
                            AddDecodeResult("QR", qrAll[i].Text);
                        }
                        found |= qrAll.Length > 0;
                    }
                }
                await Task.Yield();
            }

            if (DecodeBarcode && (!found || !DecodeStopAfterFirst) && !token.IsCancellationRequested)
            {
                await UpdateDecodeStatusAsync("Decoding 1D barcode...");
                var stageBudget = RemainingBudget();
                if (stageBudget <= 0)
                {
                    DecodeError = "Time budget exceeded.";
                    return;
                }
                using var stageCts = CreateStageCts(token, stageBudget);
                var barcodeOptions = new BarcodeDecodeOptions
                {
                    EnableTileScan = !DecodeStopAfterFirst
                };
                if (DecodeStopAfterFirst)
                {
                    if (BarcodeDecoder.TryDecode(rgba, width, height, stride, PixelFormat.Rgba32, null, barcodeOptions, stageCts.Token, out var barcode))
                    {
                        AddDecodeResult($"Barcode ({barcode.Type})", barcode.Text);
                        found = true;
                        DecodeStatus = $"Decode finished in {sw.ElapsedMilliseconds} ms";
                        return;
                    }
                }
                else
                {
                    if (BarcodeDecoder.TryDecodeAll(rgba, width, height, stride, PixelFormat.Rgba32, out var barcodes, null, barcodeOptions, stageCts.Token))
                    {
                        for (var i = 0; i < barcodes.Length; i++)
                        {
                            AddDecodeResult($"Barcode ({barcodes[i].Type})", barcodes[i].Text);
                        }
                        found |= barcodes.Length > 0;
                    }
                }
                await Task.Yield();
            }

            if (DecodeMatrix && (!found || !DecodeStopAfterFirst) && !token.IsCancellationRequested)
            {
                await UpdateDecodeStatusAsync("Decoding 2D matrix codes...");
                var stageBudget = RemainingBudget();
                if (stageBudget <= 0)
                {
                    DecodeError = "Time budget exceeded.";
                    return;
                }
                var matrixMax = DecodeMaxDimension > 0 ? Math.Min(DecodeMaxDimension, 640) : 640;
                var matrixRgba = rgba;
                var matrixWidth = width;
                var matrixHeight = height;
                if (matrixMax > 0 && (matrixWidth > matrixMax || matrixHeight > matrixMax))
                {
                    if (DecodeDownscale)
                    {
                        await UpdateDecodeStatusAsync("Downscaling for matrix decode...");
                        matrixRgba = ResizeNearest(matrixRgba, matrixWidth, matrixHeight, matrixMax, out matrixWidth, out matrixHeight);
                    }
                    else
                    {
                        DecodeStatus = $"Skipped matrix decode (image too large: {matrixWidth}x{matrixHeight}).";
                        return;
                    }
                }

                var pixelCount = matrixWidth * matrixHeight;
                var maxPixels = matrixMax > 0 ? matrixMax * matrixMax : 1_000_000;
                if (pixelCount > maxPixels * 2)
                {
                    DecodeStatus = $"Skipped matrix decode (image too large: {matrixWidth}x{matrixHeight}).";
                    return;
                }
                if (stageBudget < 150)
                {
                    DecodeStatus = "Skipped matrix decode (insufficient time budget).";
                    return;
                }

                using var stageCts = CreateStageCts(token, stageBudget);
                var matrixStride = matrixWidth * 4;
                var sliceBudget = Math.Max(100, stageBudget / 3);

                using (var dmCts = CreateStageCts(stageCts.Token, sliceBudget))
                {
                    if (CodeGlyphX.DataMatrix.DataMatrixDecoder.TryDecode(matrixRgba, matrixWidth, matrixHeight, matrixStride, PixelFormat.Rgba32, dmCts.Token, out var dm))
                    {
                        AddDecodeResult("Data Matrix", dm);
                        found = true;
                        if (DecodeStopAfterFirst)
                        {
                            DecodeStatus = $"Decode finished in {sw.ElapsedMilliseconds} ms";
                            return;
                        }
                    }
                }
                if (!DecodeStopAfterFirst && !stageCts.Token.IsCancellationRequested)
                {
                    var tileBudget = Math.Max(80, sliceBudget / 2);
                    using var tileCts = CreateStageCts(stageCts.Token, tileBudget);
                    ScanTiles(matrixRgba, matrixWidth, matrixHeight, matrixStride, tileCts.Token, (tile, tw, th, tstride) =>
                    {
                        if (CodeGlyphX.DataMatrix.DataMatrixDecoder.TryDecode(tile, tw, th, tstride, PixelFormat.Rgba32, tileCts.Token, out var text))
                        {
                            AddDecodeResult("Data Matrix", text);
                            found = true;
                        }
                        return false;
                    });
                }
                await Task.Yield();
                if (!found || !DecodeStopAfterFirst)
                {
                    using (var pdfCts = CreateStageCts(stageCts.Token, sliceBudget))
                    {
                        if (CodeGlyphX.Pdf417.Pdf417Decoder.TryDecode(matrixRgba, matrixWidth, matrixHeight, matrixStride, PixelFormat.Rgba32, pdfCts.Token, out var pdf417))
                        {
                            AddDecodeResult("PDF417", pdf417);
                            found = true;
                            if (DecodeStopAfterFirst)
                            {
                                DecodeStatus = $"Decode finished in {sw.ElapsedMilliseconds} ms";
                                return;
                            }
                        }
                    }
                    if (!DecodeStopAfterFirst && !stageCts.Token.IsCancellationRequested)
                    {
                        var tileBudget = Math.Max(80, sliceBudget / 2);
                        using var tileCts = CreateStageCts(stageCts.Token, tileBudget);
                        ScanTiles(matrixRgba, matrixWidth, matrixHeight, matrixStride, tileCts.Token, (tile, tw, th, tstride) =>
                        {
                            if (CodeGlyphX.Pdf417.Pdf417Decoder.TryDecode(tile, tw, th, tstride, PixelFormat.Rgba32, tileCts.Token, out var text))
                            {
                                AddDecodeResult("PDF417", text);
                                found = true;
                            }
                            return false;
                        });
                    }
                    await Task.Yield();
                }
                if (!found || !DecodeStopAfterFirst)
                {
                    using (var azCts = CreateStageCts(stageCts.Token, sliceBudget))
                    {
                        if (AztecCode.TryDecode(matrixRgba, matrixWidth, matrixHeight, matrixStride, PixelFormat.Rgba32, azCts.Token, out var aztec))
                        {
                            AddDecodeResult("Aztec", aztec);
                            found = true;
                            if (DecodeStopAfterFirst)
                            {
                                DecodeStatus = $"Decode finished in {sw.ElapsedMilliseconds} ms";
                                return;
                            }
                        }
                    }
                    if (!DecodeStopAfterFirst && !stageCts.Token.IsCancellationRequested)
                    {
                        var tileBudget = Math.Max(80, sliceBudget / 2);
                        using var tileCts = CreateStageCts(stageCts.Token, tileBudget);
                        ScanTiles(matrixRgba, matrixWidth, matrixHeight, matrixStride, tileCts.Token, (tile, tw, th, tstride) =>
                        {
                            if (AztecCode.TryDecode(tile, tw, th, tstride, PixelFormat.Rgba32, tileCts.Token, out var text))
                            {
                                AddDecodeResult("Aztec", text);
                                found = true;
                            }
                            return false;
                        });
                    }
                    await Task.Yield();
                }
            }

            if (token.IsCancellationRequested)
            {
                DecodeError = DecodeMaxMilliseconds > 0 && sw.ElapsedMilliseconds >= DecodeMaxMilliseconds
                    ? "Time budget exceeded."
                    : "Decode cancelled.";
                return;
            }

            DecodeStatus = $"Decode finished in {sw.ElapsedMilliseconds} ms";

            if (!found)
            {
                DecodeError = "No matching symbols found.";
            }
        }
        catch (Exception ex)
        {
            DecodeError = ex.Message;
        }
        finally
        {
            IsDecoding = false;
        }
    }

    internal void AddDecodeResult(string type, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (DecodeResults.Any(r => r.Type == type && r.Text == text)) return;
        DecodeResults.Add(new DecodeResult(type, text));
    }

    internal void AddDecodeResult(CodeGlyphDecoded decoded)
    {
        switch (decoded.Kind)
        {
            case CodeGlyphKind.Qr:
                AddDecodeResult("QR", decoded.Text);
                break;
            case CodeGlyphKind.Barcode1D:
                var type = decoded.Barcode?.Type.ToString() ?? "Barcode";
                AddDecodeResult($"Barcode ({type})", decoded.Text);
                break;
            case CodeGlyphKind.DataMatrix:
                AddDecodeResult("Data Matrix", decoded.Text);
                break;
            case CodeGlyphKind.Pdf417:
                AddDecodeResult("PDF417", decoded.Text);
                break;
            case CodeGlyphKind.Aztec:
                AddDecodeResult("Aztec", decoded.Text);
                break;
        }
    }

    internal void CancelDecode()
    {
        _decodeCts?.Cancel();
        DecodeStatus = "Cancelling...";
    }

    internal static CancellationTokenSource CreateStageCts(CancellationToken token, int milliseconds)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        if (milliseconds > 0)
        {
            cts.CancelAfter(milliseconds);
        }
        return cts;
    }

    internal async Task UpdateDecodeStatusAsync(string status)
    {
        DecodeStatus = status;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();
    }

    internal void ResetOutputs()
    {
        ErrorMessage = null;
        ImageDataUri = null;
        SvgDataUri = null;
        DecodeImageDataUri = null;
        DecodeError = null;
        DecodeResults.Clear();
        DecodeStatus = string.Empty;
    }

    internal static byte[] ResizeNearest(byte[] rgba, int width, int height, int maxDimension, out int newWidth, out int newHeight)
    {
        if (maxDimension <= 0 || (width <= maxDimension && height <= maxDimension))
        {
            newWidth = width;
            newHeight = height;
            return rgba;
        }

        var scale = Math.Min(1.0, maxDimension / (double)Math.Max(width, height));
        newWidth = Math.Max(1, (int)Math.Round(width * scale));
        newHeight = Math.Max(1, (int)Math.Round(height * scale));

        var dst = new byte[newWidth * newHeight * 4];
        var xRatio = width / (double)newWidth;
        var yRatio = height / (double)newHeight;
        var srcStride = width * 4;

        for (var y = 0; y < newHeight; y++)
        {
            var srcY = Math.Min(height - 1, (int)(y * yRatio));
            var srcRow = srcY * srcStride;
            var dstRow = y * newWidth * 4;
            for (var x = 0; x < newWidth; x++)
            {
                var srcX = Math.Min(width - 1, (int)(x * xRatio));
                var srcIndex = srcRow + srcX * 4;
                var dstIndex = dstRow + x * 4;
                dst[dstIndex + 0] = rgba[srcIndex + 0];
                dst[dstIndex + 1] = rgba[srcIndex + 1];
                dst[dstIndex + 2] = rgba[srcIndex + 2];
                dst[dstIndex + 3] = rgba[srcIndex + 3];
            }
        }

        return dst;
    }

    internal static void ScanTiles(byte[] rgba, int width, int height, int stride, CancellationToken token, Func<byte[], int, int, int, bool> onTile)
    {
        if (width <= 0 || height <= 0 || stride < width * 4) return;
        var tiles = Math.Max(width, height) >= 720 ? 3 : 2;
        var pad = Math.Max(8, Math.Min(width, height) / 40);
        var tileW = width / tiles;
        var tileH = height / tiles;

        for (var ty = 0; ty < tiles; ty++)
        {
            for (var tx = 0; tx < tiles; tx++)
            {
                if (token.IsCancellationRequested) return;
                var x0 = tx * tileW;
                var y0 = ty * tileH;
                var x1 = (tx == tiles - 1) ? width : (tx + 1) * tileW;
                var y1 = (ty == tiles - 1) ? height : (ty + 1) * tileH;

                x0 = Math.Max(0, x0 - pad);
                y0 = Math.Max(0, y0 - pad);
                x1 = Math.Min(width, x1 + pad);
                y1 = Math.Min(height, y1 + pad);

                var tw = x1 - x0;
                var th = y1 - y0;
                if (tw < 48 || th < 48) continue;

                var tileStride = tw * 4;
                var tile = new byte[tileStride * th];
                for (var y = 0; y < th; y++)
                {
                    if (token.IsCancellationRequested) return;
                    Buffer.BlockCopy(rgba, (y0 + y) * stride + x0 * 4, tile, y * tileStride, tileStride);
                }

                if (onTile(tile, tw, th, tileStride)) return;
            }
        }
    }
}
