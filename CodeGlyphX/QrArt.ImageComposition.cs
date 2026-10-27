using System;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;

namespace CodeGlyphX;

public static partial class QrArt {
    /// <summary>
    /// Encodes text with high error correction and composes a supplied image across its modules.
    /// Uses the first image/frame supported by <see cref="ImageReader"/>. No network access is performed.
    /// The QR uses at least <see cref="QrImageCompositionOptions.MinimumVersion"/> (version 6 by default), so short
    /// payloads still have enough modules to carry the image. With <see cref="QrImageCompositionOptions.VerifyDecode"/>
    /// (the default) the composed pixels must decode with the native decoder, otherwise <see cref="InvalidOperationException"/>
    /// is thrown. Validate the exported image and its final delivery size before distribution.
    /// </summary>
    public static QrImageComposition Compose(string payload, byte[] image, QrImageCompositionOptions? options = null, ImageDecodeOptions? imageOptions = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        if (image is null) throw new ArgumentNullException(nameof(image));
        options ??= new QrImageCompositionOptions();
        options.Validate();
        var qr = QR.Encode(payload, new QrEncodingOptions {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.H, MinVersion = options.MinimumVersion
        });
        var pixels = ImageReader.DecodeRgba32(image, imageOptions, out var width, out var height);
        var composition = QrImageComposer.Render(qr, pixels, width, height, options);
        if (options.VerifyDecode) VerifyComposition(qr, composition);
        return composition;
    }

    // The same native check as styled rendering: standard profile, no transforms, exported pixels only.
    internal static void VerifyComposition(QrCode qr, QrImageComposition composition) {
        var decodeOptions = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Balanced,
            DisableTransforms = true,
            BudgetMilliseconds = 2000,
        };
        if (QrDecoder.TryDecode(qr.Modules, out var expected)
            && QrImageDecoder.TryDecode(composition.GetPixels(), composition.Size, composition.Size, composition.Size * 4,
                PixelFormat.Rgba32, decodeOptions, out var actual)
            && string.Equals(expected.Text, actual.Text, StringComparison.Ordinal)
            && expected.Bytes.AsSpan().SequenceEqual(actual.Bytes)) return;
        throw new InvalidOperationException("The photo composition did not pass native decode validation. "
            + "Lower Strength, enlarge CenterSize, raise ModuleSize or MinimumVersion, or crop to a calmer area of the image. "
            + "For decorative output, set VerifyDecode to false and verify the exported image separately.");
    }
}
