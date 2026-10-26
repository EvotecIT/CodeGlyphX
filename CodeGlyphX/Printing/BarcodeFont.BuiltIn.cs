using System;
using System.IO;

namespace CodeGlyphX.Printing;

public sealed partial class BarcodeFont {
    private static readonly Lazy<BarcodeFont> OcrAFont = new(() => LoadBundled("GGOcra.ttf"));
    private static readonly Lazy<BarcodeFont> OcrBFont = new(() => LoadBundled("CodeGlyphXScanB-Regular.ttf"));
    private static readonly Lazy<BarcodeFont> ManropeSemiBoldFont = new(() => LoadBundled("Manrope-SemiBold-Latin-Extended.ttf"));

    /// <summary>Gets the bundled GG Ocra regular face, released under CC0 1.0.</summary>
    /// <remarks>Includes printable ASCII. Loaded on first use without installed fonts or font services.
    /// This selects text outlines; it does not certify a printed label or provide OCR recognition.</remarks>
    public static BarcodeFont OcrA => OcrAFont.Value;

    /// <summary>Gets the bundled CodeGlyphX Scan B regular face, derived from Raisty OCR-B under OFL 1.1.</summary>
    /// <remarks>The font is renamed to respect the upstream reserved font name. Includes printable ASCII.
    /// Loaded on first use without installed fonts or font services. This selects text outlines;
    /// it does not certify a printed label or provide OCR recognition.</remarks>
    public static BarcodeFont OcrB => OcrBFont.Value;

    /// <summary>Gets the bundled Manrope SemiBold face, released under OFL 1.1.</summary>
    /// <remarks>Includes Latin, Latin Extended, combining marks and common punctuation, including Polish captions.
    /// Loaded on first use without installed fonts or font services. Strict text shaping rejects unavailable glyphs.</remarks>
    public static BarcodeFont ManropeSemiBold => ManropeSemiBoldFont.Value;

    private static BarcodeFont LoadBundled(string fileName) {
        using var input = typeof(BarcodeFont).Assembly.GetManifestResourceStream("CodeGlyphX.Printing.Fonts." + fileName)
            ?? throw new InvalidOperationException("The bundled barcode font resource is missing.");
        using var bytes = new MemoryStream();
        input.CopyTo(bytes);
        return FromBytes(bytes.ToArray());
    }
}
