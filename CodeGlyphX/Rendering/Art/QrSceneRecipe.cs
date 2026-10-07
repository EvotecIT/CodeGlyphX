using CodeGlyphX.Rendering;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Art;

/// <summary>A versioned local scene recipe (.cgxart). Parsing prohibits DTDs and external resources.
/// Settings, payload and any logo are copied. Recipes contain no rendered pixels or scan certification.</summary>
public sealed class QrSceneRecipe {
    /// <summary>Maximum serialized input length, including an optional base64 logo.</summary>
    public const int MaxCharacters = 2 * 1024 * 1024;
    /// <summary>The exact payload to encode.</summary>
    public string Payload { get; }
    private readonly QrSceneOptions _design;
    private static readonly Encoding PayloadEncoding = new UTF8Encoding(false, true);
    /// <summary>Returns an independent editable copy.</summary>
    public QrSceneOptions Design => _design.Clone();

    /// <summary>Snapshots a payload and design.</summary>
    public QrSceneRecipe(string payload, QrSceneOptions design) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        if (payload.Length > 4096) throw new ArgumentException("Recipe payload exceeds 4096 characters.", nameof(payload));
        // Strict UTF-8 supports QR text and control characters without XML text normalization.
        PayloadEncoding.GetByteCount(payload);
        if (design is null) throw new ArgumentNullException(nameof(design));
        Payload = payload; _design = design.Clone();
    }

    /// <summary>Returns the version-one, culture-independent recipe document.</summary>
    public string ToXml() {
        var d = _design;
        var root = new XElement("cgx-scene", new XAttribute("version", 1), new XAttribute("style", d.Style),
            new XAttribute("size", d.Size), new XAttribute("seed", d.Seed), new XAttribute("shape", d.ModuleShape),
            new XAttribute("paper", Color(d.Paper)), new XAttribute("ink", Color(d.Ink)),
            new XElement("payload", new XAttribute("encoding", "base64-utf8"), Convert.ToBase64String(PayloadEncoding.GetBytes(Payload))), new XElement("caption", d.Caption),
            new XElement("palette", d.Colors.Select(c => new XElement("color", Color(c)))));
        root.Add(Layer("backdrop", d.Backdrop), Layer("motifs", d.Motifs), Layer("qr", d.Qr), Layer("caption", d.CaptionLayer), Layer("logo", d.Logo));
        if (d.LogoImage is not null) root.Add(new XElement("logo-image", Convert.ToBase64String(d.LogoImage)));
        // XML normally normalizes CR/CRLF to LF. Entitize CR so reopening keeps the exact QR payload.
        var output = new StringBuilder();
        using (var writer = XmlWriter.Create(output, new XmlWriterSettings { OmitXmlDeclaration = true, NewLineHandling = NewLineHandling.Entitize })) root.WriteTo(writer);
        return output.ToString();
    }

    /// <summary>Reads a bounded version-one recipe. Unknown/duplicate fields, invalid settings, DTDs and
    /// external resources are rejected with <see cref="FormatException"/>. Does not decode optional logo pixels.</summary>
    public static QrSceneRecipe FromXml(string xml) {
        if (xml is null) throw new ArgumentNullException(nameof(xml));
        if (xml.Length > MaxCharacters) throw new FormatException("Scene recipe exceeds its input limit.");
        try {
            using var input = new StringReader(xml);
            using var reader = XmlReader.Create(input, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxCharacters });
            var root = XElement.Load(reader);
            if (root.Name != "cgx-scene" || Required(root, "version") != "1") throw new FormatException("Unsupported scene recipe version.");
            Attributes(root, "version", "style", "size", "seed", "shape", "paper", "ink");
            foreach (var element in root.Elements()) {
                if (element.Name != "payload" && element.Name != "caption" && element.Name != "palette" && element.Name != "layer" && element.Name != "logo-image")
                    throw new FormatException("Unknown scene recipe field.");
            }
            var palette = Element(root, "palette"); Attributes(palette);
            if (palette.Elements().Any(e => e.Name != "color") || palette.Elements().Count() != 4) throw new FormatException("Recipe palettes require four colors.");
            var design = new QrSceneOptions {
                Style = EnumValue<QrSceneStyle>(Required(root, "style")), Size = Integer(Required(root, "size")), Seed = Integer(Required(root, "seed")),
                ModuleShape = EnumValue<QrModuleShape>(Required(root, "shape")), Paper = ParseColor(Required(root, "paper")), Ink = ParseColor(Required(root, "ink")),
                Colors = palette.Elements().Select(ReadColor).ToArray(), Caption = Text(Element(root, "caption")),
                Backdrop = ReadLayer(root, "backdrop"), Motifs = ReadLayer(root, "motifs"), Qr = ReadLayer(root, "qr"),
                CaptionLayer = ReadLayer(root, "caption"), Logo = ReadLayer(root, "logo")
            };
            if (root.Elements("layer").Count() != 5) throw new FormatException("Recipe requires exactly five named layers.");
            var logos = root.Elements("logo-image").ToArray();
            if (logos.Length > 1) throw new FormatException("Duplicate logo image.");
            if (logos.Length == 1) design.LogoImage = Convert.FromBase64String(Text(logos[0]));
            design.Validate();
            return new QrSceneRecipe(ReadPayload(Element(root, "payload")), design);
        } catch (Exception ex) when (ex is XmlException || ex is ArgumentException || ex is OverflowException) {
            throw new FormatException("Invalid scene recipe: " + ex.Message, ex);
        }
    }
    /// <summary>Saves the bounded recipe document and returns its path.</summary>
    public string Save(string path) => RenderIO.WriteText(path, ToXml());

    private static XElement Layer(string name, QrSceneLayerOptions layer) => new("layer", new XAttribute("name", name), new XAttribute("visible", layer.Visible),
        new XAttribute("x", Number(layer.X)), new XAttribute("y", Number(layer.Y)), new XAttribute("scale", Number(layer.Scale)), new XAttribute("rotation", Number(layer.RotationDegrees)));
    private static QrSceneLayerOptions ReadLayer(XElement root, string name) {
        var values = root.Elements("layer").Where(e => (string?)e.Attribute("name") == name).ToArray();
        if (values.Length != 1) throw new FormatException("Missing or duplicate layer: " + name);
        var layer = values[0]; Attributes(layer, "name", "visible", "x", "y", "scale", "rotation");
        if (layer.HasElements || !string.IsNullOrWhiteSpace(layer.Value)) throw new FormatException("Unexpected layer content.");
        if (!bool.TryParse(Required(layer, "visible"), out var visible)) throw new FormatException("Invalid layer visibility.");
        return new() { Visible = visible, X = Real(Required(layer, "x")), Y = Real(Required(layer, "y")), Scale = Real(Required(layer, "scale")), RotationDegrees = Real(Required(layer, "rotation")) };
    }
    private static XElement Element(XElement root, string name) {
        var values = root.Elements(name).ToArray();
        if (values.Length != 1) throw new FormatException("Missing or duplicate field: " + name);
        return values[0];
    }
    private static string Text(XElement value) {
        Attributes(value); if (value.HasElements) throw new FormatException("Unexpected nested recipe content."); return value.Value;
    }
    private static string ReadPayload(XElement value) {
        Attributes(value, "encoding");
        if (Required(value, "encoding") != "base64-utf8" || value.HasElements) throw new FormatException("Unsupported payload encoding.");
        return PayloadEncoding.GetString(Convert.FromBase64String(value.Value));
    }
    private static Rgba32 ReadColor(XElement value) => ParseColor(Text(value));
    private static void Attributes(XElement value, params string[] names) {
        if (value.Attributes().Any(a => !names.Contains(a.Name.LocalName) || a.Name.Namespace != XNamespace.None)) throw new FormatException("Unknown recipe attribute.");
    }
    private static string Required(XElement value, string name) => (string?)value.Attribute(name) ?? throw new FormatException("Missing recipe attribute: " + name);
    private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static double Real(string value) => double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    private static int Integer(string value) => int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
    private static T EnumValue<T>(string value) where T : struct {
        if (!Enum.TryParse(value, out T result) || !Enum.IsDefined(typeof(T), result)) throw new FormatException("Unknown recipe style."); return result;
    }
    private static string Color(Rgba32 value) => "#" + value.R.ToString("X2") + value.G.ToString("X2") + value.B.ToString("X2");
    private static Rgba32 ParseColor(string value) {
        if (value.Length != 7 || value[0] != '#') throw new FormatException("Colors must use #RRGGBB.");
        return new(byte.Parse(value.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(value.Substring(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture), byte.Parse(value.Substring(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }
}
