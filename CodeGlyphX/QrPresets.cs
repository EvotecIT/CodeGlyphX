using System;

namespace CodeGlyphX;

/// <summary>
/// Ready-to-use QR presets with conservative defaults for common scenarios.
/// </summary>
public static class QrPresets {
    /// <summary>
    /// Preset for OTP payloads (high error correction).
    /// </summary>
    public static QrEncodingOptions Otp() => new QrEncodingOptions {
        ErrorCorrectionLevel = QrErrorCorrectionLevel.H
    };

    /// <summary>
    /// Preset for Wi-Fi payloads (more correction for noisy camera scans).
    /// </summary>
    public static QrEncodingOptions Wifi() => new QrEncodingOptions {
        ErrorCorrectionLevel = QrErrorCorrectionLevel.Q
    };

    /// <summary>
    /// Preset for contact payloads (more correction for dense payloads).
    /// </summary>
    public static QrEncodingOptions Contact() => new QrEncodingOptions {
        ErrorCorrectionLevel = QrErrorCorrectionLevel.Q
    };

    /// <summary>
    /// Appearance preset for logo overlays. Select error correction and version separately with QrEncodingOptions.
    /// </summary>
    public static QrRenderOptions Logo(byte[] logoPng, double? logoScale = null) {
        if (logoPng is null) throw new ArgumentNullException(nameof(logoPng));
        return new QrRenderOptions {
            LogoPng = (byte[])logoPng.Clone(),
            LogoScale = logoScale ?? 0.22,
            LogoDrawBackground = true,
            LogoPaddingPx = 6,
            LogoCornerRadiusPx = 8
        };
    }
}
