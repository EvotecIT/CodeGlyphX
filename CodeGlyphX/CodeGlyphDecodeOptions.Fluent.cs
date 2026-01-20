using System;
using System.Threading;

namespace CodeGlyphX;

public sealed partial class CodeGlyphDecodeOptions {
    /// <summary>
    /// Configures barcode decode options fluently.
    /// </summary>
    public CodeGlyphDecodeOptions WithBarcode(Action<BarcodeDecodeOptions> configure) {
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        configure(EnsureBarcode());
        return this;
    }

    /// <summary>
    /// Configures QR decode options fluently.
    /// </summary>
    public CodeGlyphDecodeOptions WithQr(Action<QrPixelDecodeOptions> configure) {
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        configure(EnsureQr());
        return this;
    }

    /// <summary>
    /// Configures image decode options fluently.
    /// </summary>
    public CodeGlyphDecodeOptions WithImage(Action<ImageDecodeOptions> configure) {
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        configure(EnsureImage());
        return this;
    }

    /// <summary>
    /// Sets the expected barcode type.
    /// </summary>
    public CodeGlyphDecodeOptions WithExpectedBarcode(BarcodeType? barcodeType) {
        ExpectedBarcode = barcodeType;
        return this;
    }

    /// <summary>
    /// Prefer trying barcodes before 2D codes.
    /// </summary>
    public CodeGlyphDecodeOptions PreferBarcodeFirst(bool enabled = true) {
        PreferBarcode = enabled;
        return this;
    }

    /// <summary>
    /// Include 1D barcode results when decoding multiple symbols.
    /// </summary>
    public CodeGlyphDecodeOptions IncludeBarcodeResults(bool enabled = true) {
        IncludeBarcode = enabled;
        return this;
    }

    /// <summary>
    /// Sets a cancellation token for decoding.
    /// </summary>
    public CodeGlyphDecodeOptions WithCancellation(CancellationToken token) {
        CancellationToken = token;
        return this;
    }

    /// <summary>
    /// Applies a QR decode budget (milliseconds + optional max dimension).
    /// </summary>
    public CodeGlyphDecodeOptions WithQrBudget(int maxMilliseconds, int maxDimension = 0) {
        var qr = EnsureQr();
        qr.MaxMilliseconds = maxMilliseconds < 0 ? 0 : maxMilliseconds;
        if (maxDimension > 0) {
            qr.MaxDimension = maxDimension;
        }
        return this;
    }

    /// <summary>
    /// Applies a non-QR image decode budget (milliseconds + optional max dimension).
    /// </summary>
    public CodeGlyphDecodeOptions WithImageBudget(int maxMilliseconds, int maxDimension = 0) {
        var image = EnsureImage();
        image.MaxMilliseconds = maxMilliseconds < 0 ? 0 : maxMilliseconds;
        if (maxDimension > 0) {
            image.MaxDimension = maxDimension;
        }
        return this;
    }

    /// <summary>
    /// Sets the QR profile for this decode.
    /// </summary>
    public CodeGlyphDecodeOptions WithQrProfile(QrDecodeProfile profile) {
        EnsureQr().Profile = profile;
        return this;
    }

    /// <summary>
    /// Overrides maximum QR scale.
    /// </summary>
    public CodeGlyphDecodeOptions WithQrMaxScale(int maxScale) {
        EnsureQr().MaxScale = maxScale;
        return this;
    }

    /// <summary>
    /// Enables aggressive QR sampling.
    /// </summary>
    public CodeGlyphDecodeOptions WithAggressiveQrSampling(bool enabled = true) {
        EnsureQr().AggressiveSampling = enabled;
        return this;
    }
}
