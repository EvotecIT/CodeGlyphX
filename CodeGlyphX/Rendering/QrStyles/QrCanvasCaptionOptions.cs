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

/// <summary>A measured canvas caption shared by PNG and canonical SVG output.</summary>
public sealed class QrCanvasCaptionOptions {
    /// <summary>Optional second line, rendered smaller than the headline.</summary>
    public string Subline { get; set; } = string.Empty;
    /// <summary>Optional camera or phone silhouette before the caption.</summary>
    public QrCanvasCaptionIcon Icon { get; set; }
    /// <summary>Optional fallback URL, placed below the code independently of caption position.</summary>
    public string Url { get; set; } = string.Empty;
    /// <summary>Loads a caller-supplied TTF, OTF or supported font collection without installed fonts.</summary>
    public static QrCanvasCaptionOptions FromFontBytes(byte[] fontData) => new() { Font = BarcodeFont.FromBytes(fontData) };

    /// <summary>Exact headline text, limited to 256 UTF-16 characters without explicit line breaks.</summary>
    public string Text { get; set; } = "SCAN ME";
    /// <summary>Immutable portable font. The bundled Manrope Medium default includes Latin Extended and Polish.
    /// Load a suitable face with BarcodeFont.FromBytes for other supported text.</summary>
    public BarcodeFont Font { get; set; } = BarcodeFont.ManropeMedium;
    /// <summary>Requested em size in pixels, from 8 to 256; zero sizes cap height to 4.5% of the code width. Long text shrinks to a readable floor, then wraps to two lines.</summary>
    public int FontSizePx { get; set; } = 0;
    /// <summary>Outline weight.</summary>
    public QrCanvasCaptionWeight Weight { get; set; } = QrCanvasCaptionWeight.Regular;
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
        if (Subline is null || Subline.Length > 256 || Url is null || Url.Length > 256) throw new ArgumentException("Caption lines must contain at most 256 characters.");
        if (!Enum.IsDefined(typeof(QrCanvasCaptionIcon), Icon)) throw new ArgumentOutOfRangeException(nameof(Icon));
        if (Text.IndexOfAny(new[] { '\r', '\n' }) >= 0 || Subline.IndexOfAny(new[] { '\r', '\n' }) >= 0 || Url.IndexOfAny(new[] { '\r', '\n' }) >= 0) throw new ArgumentException("Use Subline for a second caption line.");
        if (Font is null) throw new ArgumentNullException(nameof(Font));
        if (FontSizePx != 0 && (FontSizePx < 8 || FontSizePx > 256)) throw new ArgumentOutOfRangeException(nameof(FontSizePx));
        if (PaddingPx < 4 || PaddingPx > 256) throw new ArgumentOutOfRangeException(nameof(PaddingPx));
        if (!Enum.IsDefined(typeof(QrCanvasCaptionPosition), Position)) throw new ArgumentOutOfRangeException(nameof(Position));
        if (!Enum.IsDefined(typeof(QrCanvasCaptionWeight), Weight)) throw new ArgumentOutOfRangeException(nameof(Weight));
    }

    internal QrCanvasCaptionOptions Copy(int scale = 1) => new() {
        Text = Text, Subline = Subline, Icon = Icon, Url = Url, Font = Font, FontSizePx = checked(FontSizePx * scale), Weight = Weight,
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

/// <summary>Portable vector silhouettes available before a caption.</summary>
public enum QrCanvasCaptionIcon {
    /// <summary>No icon.</summary>
    None,
    /// <summary>Camera outline and lens.</summary>
    Camera,
    /// <summary>Phone outline and home indicator.</summary>
    Phone
}
