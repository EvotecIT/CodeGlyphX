using System;
using CodeGlyphX.Printing;

namespace CodeGlyphX.Rendering;

/// <summary>Placement of a caption outside the complete QR quiet zone.</summary>
public enum QrCanvasCaptionPosition {
    /// <summary>Above the code.</summary>
    Top,
    /// <summary>Below the code.</summary>
    Bottom
}

/// <summary>Portable caption weight. Bold strengthens the selected outlines without an installed font.</summary>
public enum QrCanvasCaptionWeight {
    /// <summary>Use the selected face unchanged.</summary>
    Regular,
    /// <summary>Apply the managed print engine's synthetic bold treatment.</summary>
    Bold
}

/// <summary>A single-line canvas caption shared by PNG and canonical SVG output.</summary>
public sealed class QrCanvasCaptionOptions {
    /// <summary>Exact caption text, limited to 256 UTF-16 characters without line breaks.</summary>
    public string Text { get; set; } = "SCAN ME";
    /// <summary>Immutable portable font. The bundled Scan B face provides an ASCII default.
    /// Load a suitable face with BarcodeFont.FromBytes for supported Unicode text.</summary>
    public BarcodeFont Font { get; set; } = BarcodeFont.OcrB;
    /// <summary>Requested em size in pixels, from 8 to 256. Long lines shrink to fit, down to 8 pixels.</summary>
    public int FontSizePx { get; set; } = 24;
    /// <summary>Outline weight.</summary>
    public QrCanvasCaptionWeight Weight { get; set; } = QrCanvasCaptionWeight.Bold;
    /// <summary>Text colour, or null for contrasting dark/light text selected for the template.</summary>
    public Rgba32? Color { get; set; }
    /// <summary>Top or bottom placement.</summary>
    public QrCanvasCaptionPosition Position { get; set; } = QrCanvasCaptionPosition.Bottom;
    /// <summary>Clear space around the text, from 4 to 256 pixels.</summary>
    public int PaddingPx { get; set; } = 16;
    /// <summary>Reject unavailable glyphs or unsupported shaping instead of substituting text.</summary>
    public bool StrictText { get; set; } = true;

    // Internal raster units per final output pixel; copied with supersampled geometry.
    internal int RasterScale { get; private set; } = 1;

    internal void Validate() {
        if (Text is null || Text.Length > 256) throw new ArgumentException("Caption text must contain at most 256 characters.", nameof(Text));
        if (Font is null) throw new ArgumentNullException(nameof(Font));
        if (FontSizePx < 8 || FontSizePx > 256) throw new ArgumentOutOfRangeException(nameof(FontSizePx));
        if (PaddingPx < 4 || PaddingPx > 256) throw new ArgumentOutOfRangeException(nameof(PaddingPx));
        if (!Enum.IsDefined(typeof(QrCanvasCaptionPosition), Position)) throw new ArgumentOutOfRangeException(nameof(Position));
        if (!Enum.IsDefined(typeof(QrCanvasCaptionWeight), Weight)) throw new ArgumentOutOfRangeException(nameof(Weight));
    }

    internal QrCanvasCaptionOptions Copy(int scale = 1) => new() {
        Text = Text, Font = Font, FontSizePx = checked(FontSizePx * scale), Weight = Weight,
        Color = Color, Position = Position, PaddingPx = checked(PaddingPx * scale), StrictText = StrictText,
        RasterScale = checked(RasterScale * scale)
    };
}

/// <summary>Call-to-action treatments outside the reserved QR area.</summary>
public enum QrCanvasTemplate {
    /// <summary>No template; use the independent canvas decorations.</summary>
    None,
    /// <summary>A compact rounded call-to-action capsule.</summary>
    ScanMePill,
    /// <summary>A speech bubble with a pointer towards the code.</summary>
    SpeechBubble,
    /// <summary>A ticket border with side notches and a perforated caption divider.</summary>
    TicketStub,
    /// <summary>A rounded phone silhouette with a speaker and home indicator.</summary>
    PhoneOutline,
    /// <summary>A rounded card with a contrasting caption band.</summary>
    RoundedCardHeader,
    /// <summary>Four framing corners and a plain caption.</summary>
    CornerViewfinder
}
