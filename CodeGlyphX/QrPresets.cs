using System;

namespace CodeGlyphX;

/// <summary>
/// Ready-to-use QR presets with safe defaults for common scenarios.
/// </summary>
public static class QrPresets {
    /// <summary>
    /// Preset for OTP payloads (high error correction).
    /// </summary>
    public static QrEasyOptions Otp() => new QrEasyOptions {
        ErrorCorrectionLevel = QrErrorCorrectionLevel.H
    };

    /// <summary>
    /// Preset for Wi-Fi payloads (more correction for noisy camera scans).
    /// </summary>
    public static QrEasyOptions Wifi() => new QrEasyOptions {
        ErrorCorrectionLevel = QrErrorCorrectionLevel.Q
    };

    /// <summary>
    /// Preset for contact payloads (more correction for dense payloads).
    /// </summary>
    public static QrEasyOptions Contact() => new QrEasyOptions {
        ErrorCorrectionLevel = QrErrorCorrectionLevel.Q
    };

    /// <summary>
    /// Preset for logo overlays (high correction + safe logo defaults).
    /// </summary>
    public static QrEasyOptions Logo(byte[] logoPng, double? logoScale = null) {
        if (logoPng is null) throw new ArgumentNullException(nameof(logoPng));
        return new QrEasyOptions {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.H,
            LogoPng = logoPng,
            LogoScale = logoScale ?? 0.22,
            LogoDrawBackground = true,
            LogoPaddingPx = 6,
            LogoCornerRadiusPx = 8
        };
    }
}
