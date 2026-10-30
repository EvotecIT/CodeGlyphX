using System;
using System.Collections.Generic;
using System.Linq;
using CodeGlyphX;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;

namespace CodeGlyphX.Examples;

/// <summary>
/// The looks on the home-page style board: the first variant of every look in the website gallery
/// (<see cref="QrGalleryLooks"/>), so the board only shows looks that passed the gallery's scan matrix.
/// The scan tests render every look through <see cref="RenderPng"/> and decode the exact payload with
/// two decoders.
/// </summary>
internal static class QrStyleBoardLooks {
    internal const string Payload = QrGalleryLooks.ShortPayload;

    /// <summary>Code size including the quiet zone; canvases and templates add their own margin.</summary>
    internal const int TargetSizePx = 480;

    internal sealed record Look(string Name, string Summary, Func<QrRenderOptions> CreateOptions, Func<byte[]>? CreateLogo = null) {
        internal string Slug => Slugify(Name);
    }

    internal static IReadOnlyList<Look> All { get; } = QrGalleryLooks.All.Select(look => {
        var variant = look.Default;
        QrRenderOptions Create() {
            var options = variant.Create();
            options.TargetSizePx = TargetSizePx;
            return options;
        }
        return new Look(look.Name, look.Summary, Create, variant.ShowsLogo ? () => QrGalleryLooks.Logo(variant, variant.Create()) : null);
    }).ToList();

    internal static QrEncodingOptions Encoding() => QrGalleryLooks.Encoding();

    /// <summary>Renders a look exactly as the website ships it.</summary>
    internal static byte[] RenderPng(Look look) {
        var options = look.CreateOptions();
        if (look.CreateLogo is null) return QR.Render(Payload, OutputFormat.Png, options, Encoding()).Data.ToArray();
        // RenderLogo places and validates the logo, and throws instead of silently dropping it.
        return QrArt.RenderLogo(Payload, look.CreateLogo(), new QrLogoRenderOptions { Appearance = options }, Encoding()).GetPng();
    }

    private static string Slugify(string value) {
        var buffer = new char[value.Length];
        var length = 0;
        foreach (var ch in value) {
            if (ch >= 'A' && ch <= 'Z') buffer[length++] = (char)(ch + 32);
            else if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9')) buffer[length++] = ch;
            else if (ch is ' ' or '-' or '_' or '.' && length > 0 && buffer[length - 1] != '-') buffer[length++] = '-';
        }
        return length == 0 ? "style" : new string(buffer, 0, length).TrimEnd('-');
    }
}
