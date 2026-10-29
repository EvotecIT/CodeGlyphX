using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CodeGlyphX;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;

namespace CodeGlyphX.Examples;

/// <summary>
/// The curated looks on the website QR gallery (<c>/gallery/</c>). Each look has one or two variants
/// (light and dark panels count as one look). Appearances come from <see cref="QrGalleryRecipes"/>, whose
/// recipe blocks are also the C# shown on the page. A look belongs here only while every variant passes
/// the scan matrix in <see cref="Cases"/>: short and long URLs, with and without a logo, at a small and a
/// large size, read by strict ZXing and the CodeGlyphX decoder (QrGalleryScanTests).
/// </summary>
internal static class QrGalleryLooks {
    internal const string ShortPayload = "https://codeglyphx.com";
    internal const string LongPayload = "https://codeglyphx.com/docs/qr/?utm_source=print&utm_medium=poster&utm_campaign=autumn-launch-2026&ref=gallery";

    /// <summary>Code size, including the quiet zone, of the images the gallery page shows.</summary>
    internal const int GallerySizePx = 560;

    /// <summary>Code sizes the matrix renders: a small print or screen size and a large one.</summary>
    internal static IReadOnlyList<int> MatrixSizesPx { get; } = new[] { 320, 1024 };

    internal static IReadOnlyList<(string Name, string Text)> MatrixPayloads { get; } = new[] {
        ("short", ShortPayload),
        ("long", LongPayload),
    };

    internal sealed record Variant(string Id, string Label, Func<QrRenderOptions> Create, bool ShowsLogo = false,
        Rgba32? LogoTile = null, Rgba32? LogoMark = null);

    internal sealed record Look(string Id, string Name, string Summary, string BestFor, string Family, IReadOnlyList<Variant> Variants) {
        internal Variant Default => Variants[0];
    }

    internal sealed record MatrixCase(string PayloadName, string Payload, bool Logo, int SizePx) {
        public override string ToString() => $"{PayloadName}/{(Logo ? "logo" : "plain")}/{SizePx}px";
    }

    private static Variant Light(string id, Func<QrRenderOptions> create) => new(id, "Light", create);
    private static Variant Dark(string id, Func<QrRenderOptions> create) => new(id, "Dark", create);
    private static Variant Single(string id, Func<QrRenderOptions> create) => new(id, "", create);

    internal static IReadOnlyList<Look> All { get; } = new List<Look> {
        new("sunset", "Sunset", "Connected body with an orange-to-pink gradient on warm paper.",
            "Events, menus and hospitality.", "Colour",
            new[] { Single("sunset", QrGalleryRecipes.Sunset) }),

        new("minimal-line", "Minimal Line", "Round dots, a drawn timing line and ring eyes.",
            "Quiet labels and minimal layouts.", "Minimal",
            new[] { Single("minimal-line", QrGalleryRecipes.MinimalLine) }),

        new("botanical", "Botanical", "Leaf modules and mirrored leaf eyes in forest green.",
            "Natural products and outdoor experiences.", "Colour",
            new[] { Light("botanical", QrGalleryRecipes.Botanical), Dark("botanical-dark", QrGalleryRecipes.BotanicalDark) }),

        new("neon", "Neon", "Vertical bars and magenta eyes on a glowing panel.",
            "Evening events and screen displays.", "Framed",
            new[] { Dark("neon-dark", QrGalleryRecipes.NeonDark), Light("neon", QrGalleryRecipes.Neon) }),

        new("ink", "Ink", "Liquid black body with round eyes.",
            "Expressive monochrome marks and print.", "Minimal",
            new[] { Light("ink", QrGalleryRecipes.Ink), Dark("ink-dark", QrGalleryRecipes.InkDark) }),

        new("gold-sparkle", "Gold Sparkle", "Connected bronze body and sparkle eyes on a black-and-gold canvas.",
            "Premium invitations and gift cards.", "Framed",
            new[] { Single("gold-sparkle", QrGalleryRecipes.GoldSparkle) }),

        new("retro-pixel", "Retro Pixel", "Square pixels and square eyes on mustard.",
            "Playful packaging and retro graphics.", "Colour",
            new[] { Light("retro-pixel", QrGalleryRecipes.RetroPixel), Dark("retro-pixel-dark", QrGalleryRecipes.RetroPixelDark) }),

        new("ticket", "Ticket", "Connected navy body inside a coral ticket stub with a caption.",
            "Admission, coupons and boarding passes.", "Caption",
            new[] { Single("ticket", QrGalleryRecipes.Ticket) }),

        new("mono-classy", "Mono Classy", "Square body and diagonal-corner eyes on a charcoal panel.",
            "Editorial layouts and premium print.", "Minimal",
            new[] { Dark("mono-classy-dark", QrGalleryRecipes.MonoClassyDark), Light("mono-classy", QrGalleryRecipes.MonoClassy) }),

        new("ocean", "Ocean", "Horizontal bars with a navy-to-teal gradient on pale aqua.",
            "Calm digital products and packaging.", "Colour",
            new[] { Single("ocean", QrGalleryRecipes.Ocean) }),

        new("confetti", "Confetti", "Rounded modules in a random three-colour palette on cream.",
            "Parties, kids' products and celebrations.", "Colour",
            new[] { Single("confetti", QrGalleryRecipes.Confetti) }),

        new("scan-me", "Scan Me", "Rounded navy body with a SCAN ME pill on yellow.",
            "Counters, windows and point-of-sale.", "Caption",
            new[] { Single("scan-me", QrGalleryRecipes.ScanMe) }),

        new("berry", "Berry", "Rounded body with a plum-to-raspberry gradient and squircle eyes.",
            "Invitations and personal products.", "Colour",
            new[] { Light("berry", QrGalleryRecipes.Berry), Dark("berry-dark", QrGalleryRecipes.BerryDark) }),

        new("glow-frame", "Glow Frame", "The ConnectedSquircleGlow art theme in its pastel variant.",
            "Apps, onboarding screens and slides.", "Framed",
            new[] { Single("glow-frame", QrGalleryRecipes.GlowFrame) }),

        new("paper-cutout", "Paper Cutout", "Liquid cocoa body on a cream card stacked over kraft paper.",
            "Cards, invitations and stationery.", "Framed",
            new[] { Light("paper-cutout", QrGalleryRecipes.PaperCutout), Dark("paper-cutout-dark", QrGalleryRecipes.PaperCutoutDark) }),

        new("brand-logo", "Brand Logo", "Rounded body, maroon eyes and a centred logo.",
            "Your own mark on business cards and packaging.", "Logo",
            new[] {
                new Variant("brand-logo", "Light", QrGalleryRecipes.BrandLogo, ShowsLogo: true, LogoTile: R(80, 32, 42), LogoMark: R(255, 248, 246)),
                new Variant("brand-logo-dark", "Dark", QrGalleryRecipes.BrandLogoDark, ShowsLogo: true, LogoTile: R(80, 32, 42), LogoMark: R(255, 248, 246)),
            }),


        new("wi-fi-card", "Wi-Fi Card", "Connected squircle body in a blue card with a caption band.",
            "Guest Wi-Fi, lobbies and meeting rooms.", "Caption",
            new[] { Single("wi-fi-card", QrGalleryRecipes.WifiCard) }),

        new("viewfinder", "Viewfinder", "Extra-rounded crimson body framed by viewfinder corners.",
            "Signage that tells people what to do.", "Caption",
            new[] { Single("viewfinder", QrGalleryRecipes.Viewfinder) }),

        new("dot-matrix", "Dot Matrix", "Navy dots with rounded eyes on pale blue.",
            "Technology, inventory and compact labels.", "Minimal",
            new[] { Light("dot-matrix", QrGalleryRecipes.DotMatrix), Dark("dot-matrix-dark", QrGalleryRecipes.DotMatrixDark) }),

        new("mint-logo", "Mint Logo", "Squircle modules in two greens with a logo and a green border.",
            "Health, finance and fresh-food brands.", "Logo",
            new[] { new Variant("mint-logo", "", QrGalleryRecipes.MintLogo, ShowsLogo: true, LogoTile: R(0, 112, 86), LogoMark: R(234, 251, 243)) }),
    };

    internal static IEnumerable<Variant> AllVariants => All.SelectMany(look => look.Variants);

    internal static QrEncodingOptions Encoding() => new() { ErrorCorrectionLevel = QrErrorCorrectionLevel.H };

    /// <summary>The image the gallery page shows for a variant: the short URL at the gallery size,
    /// with the logo only on looks that are about a logo.</summary>
    internal static byte[] RenderGalleryPng(Variant variant) => RenderPng(variant, ShortPayload, variant.ShowsLogo, GallerySizePx);

    /// <summary>Renders a variant through the same calls its published recipe makes.</summary>
    internal static byte[] RenderPng(Variant variant, string payload, bool logo, int sizePx) {
        var options = variant.Create();
        options.TargetSizePx = sizePx;
        if (!logo) return QR.Render(payload, OutputFormat.Png, options, Encoding()).Data.ToArray();
        // RenderLogo places and validates the logo, and throws instead of silently dropping it.
        return QrArt.RenderLogo(payload, Logo(variant, options), new QrLogoRenderOptions { Appearance = options }, Encoding()).GetPng();
    }

    /// <summary>The sample mark used for the logo cases: the look's own logo colours, or its foreground on its background.</summary>
    internal static byte[] Logo(Variant variant, QrRenderOptions options) =>
        QrSampleBrandMark.Render(160, variant.LogoTile ?? options.Foreground, variant.LogoMark ?? options.Background);

    /// <summary>Every case of the scan matrix for one variant.</summary>
    internal static IEnumerable<MatrixCase> Cases() {
        foreach (var payload in MatrixPayloads)
            foreach (var logo in new[] { false, true })
                foreach (var size in MatrixSizesPx)
                    yield return new MatrixCase(payload.Name, payload.Text, logo, size);
    }

    /// <summary>The published C# for a variant: its recipe block from <see cref="QrGalleryRecipes"/> and the save call.</summary>
    internal static string Snippet(Variant variant) {
        var body = RecipeBlock(variant.Id);
        var sb = new StringBuilder();
        sb.Append("using CodeGlyphX;\nusing CodeGlyphX.Rendering;\n");
        if (variant.ShowsLogo) sb.Append("using CodeGlyphX.Rendering.Art;\n");
        sb.Append('\n').Append(body).Append("\n\n");
        if (variant.ShowsLogo) {
            sb.Append("var result = QrArt.RenderLogo(\"").Append(ShortPayload).Append("\", File.ReadAllBytes(\"logo.png\"),\n");
            sb.Append("    new QrLogoRenderOptions { Appearance = options },\n");
            sb.Append("    new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });\n");
            sb.Append("File.WriteAllBytes(\"").Append(variant.Id).Append(".png\", result.GetPng());");
        } else {
            sb.Append("QR.Save(\"").Append(ShortPayload).Append("\", \"").Append(variant.Id).Append(".png\", options,\n");
            sb.Append("    new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });");
        }
        return sb.ToString();
    }

    /// <summary>The verbatim text of one <c>#region recipe</c> block, dedented.</summary>
    internal static string RecipeBlock(string id) {
        if (!RecipeBlocks.Value.TryGetValue(id, out var block)) throw new InvalidOperationException($"No recipe block for the {id} gallery look.");
        return block;
    }

    internal static IReadOnlyDictionary<string, string> AllRecipeBlocks => RecipeBlocks.Value;

    private static readonly Lazy<Dictionary<string, string>> RecipeBlocks = new(() => {
        using var stream = typeof(QrGalleryLooks).Assembly.GetManifestResourceStream("QrGalleryRecipes.cs")
            ?? throw new InvalidOperationException("QrGalleryRecipes.cs is not embedded in " + typeof(QrGalleryLooks).Assembly.GetName().Name + ".");
        using var reader = new StreamReader(stream);
        var source = reader.ReadToEnd().Replace("\r\n", "\n");
        var blocks = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match match in Regex.Matches(source, @"^[ \t]*#region recipe ([a-z0-9-]+)\n(.*?)\n[ \t]*#endregion", RegexOptions.Multiline | RegexOptions.Singleline)) {
            var lines = match.Groups[2].Value.Split('\n');
            var indent = lines.Where(l => l.Trim().Length > 0).Min(l => l.Length - l.TrimStart().Length);
            blocks.Add(match.Groups[1].Value, string.Join("\n", lines.Select(l => l.Length >= indent ? l.Substring(indent) : l.TrimStart()).ToArray()).TrimEnd());
        }
        return blocks;
    });

    private static Rgba32 R(byte r, byte g, byte b) => new(r, g, b);
}
