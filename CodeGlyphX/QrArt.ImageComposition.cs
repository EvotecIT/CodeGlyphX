using System;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;

namespace CodeGlyphX;

public static partial class QrArt {
    /// <summary>
    /// Encodes text with high error correction and composes a supplied image across its modules.
    /// Uses the first image/frame supported by <see cref="ImageReader"/>. No network access is performed.
    /// Validate the exported image and its final delivery size before distribution.
    /// </summary>
    public static QrImageComposition Compose(string payload, byte[] image, QrImageCompositionOptions? options = null, ImageDecodeOptions? imageOptions = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        if (image is null) throw new ArgumentNullException(nameof(image));
        options ??= new QrImageCompositionOptions();
        options.Validate();
        var qr = QrCode.Encode(payload, new QrEasyOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        var pixels = ImageReader.DecodeRgba32(image, imageOptions, out var width, out var height);
        return QrImageComposer.Render(qr, pixels, width, height, options);
    }
}
