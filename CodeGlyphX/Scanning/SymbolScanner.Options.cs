using System;

namespace CodeGlyphX;

public static partial class SymbolScanner {
    private static ScanOptions SnapshotOptions(ScanOptions? source) {
        source ??= new ScanOptions();
        ValidateOptions(source);
        return new ScanOptions {
            Formats = source.Formats is null ? null : (SymbolFormat[])source.Formats.Clone(),
            Region = source.Region, TimeoutMilliseconds = source.TimeoutMilliseconds,
            MaxSymbols = source.MaxSymbols, Deduplicate = source.Deduplicate,
            EnableTileScan = source.EnableTileScan, TileGrid = source.TileGrid, Profile = source.Profile,
            CancellationToken = source.CancellationToken, Barcode = CloneBarcodeOptions(source.Barcode),
            Qr = source.Qr is null ? null : new QrPixelDecodeOptions {
                Profile = source.Qr.Profile, MaxDimension = source.Qr.MaxDimension, MaxScale = source.Qr.MaxScale,
                BudgetMilliseconds = source.Qr.BudgetMilliseconds, AutoCrop = source.Qr.AutoCrop,
                EnableTileScan = source.Qr.EnableTileScan, TileGrid = source.Qr.TileGrid,
                DisableTransforms = source.Qr.DisableTransforms, AggressiveSampling = source.Qr.AggressiveSampling,
                StylizedSampling = source.Qr.StylizedSampling
            },
            Image = source.Image is null ? null : new ImageDecodeOptions {
                MaxDimension = source.Image.MaxDimension, MaxPixels = source.Image.MaxPixels,
                MaxBytes = source.Image.MaxBytes, MaxDecodedBytes = source.Image.MaxDecodedBytes,
                RecognitionBudgetMilliseconds = source.Image.RecognitionBudgetMilliseconds,
                MaxAnimationFrames = source.Image.MaxAnimationFrames, MaxAnimationDurationMs = source.Image.MaxAnimationDurationMs,
                MaxAnimationFramePixels = source.Image.MaxAnimationFramePixels, JpegOptions = source.Image.JpegOptions
            },
            DirectPartMarking = source.DirectPartMarking?.Clone()
        };
    }

    private static void ValidateTileGrid(int value, string name) {
        if (value != 0 && (value < 2 || value > 4)) throw new ArgumentOutOfRangeException(name, "Tile grid must be zero or between two and four.");
    }

    private static void ValidateImageOptions(ImageDecodeOptions? options) {
        if (options is null) return;
        if (options.MaxDimension < 0 || options.MaxPixels < 0 || options.MaxBytes < 0 || options.MaxDecodedBytes < 0 ||
            options.RecognitionBudgetMilliseconds < 0 || options.MaxAnimationFrames < 0 ||
            options.MaxAnimationDurationMs < 0 || options.MaxAnimationFramePixels < 0)
            throw new ArgumentOutOfRangeException(nameof(options), "Image limits must be zero, positive, or null when inherited.");
    }
}
