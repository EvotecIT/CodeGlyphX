#if NET10_0
using System;
using System.Linq;
using System.Threading.Tasks;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed partial class PlaygroundLivePreviewTests {
    [Fact]
    public async Task StructuredPayloadUsesTheSameStylingAndDataAsArtwork() {
        await using var renderer = new PlaygroundRenderer();
        await renderer.Start();
        await renderer.Click("WiFi");
        await renderer.Change("oninput", e => e.Attribute("aria-label") == "Foreground color", "#112244");
        Assert.True(QrImageDecoder.TryDecodeImage(renderer.Png(), out var styled));
        Assert.Equal("WIFI:T:WPA;S:Office-WiFi;P:Password123;;", styled.Text);
        var pixels = ImageReader.DecodeRgba32(renderer.Png(), out _, out _);
        Assert.Contains(Enumerable.Range(0, pixels.Length / 4), p => pixels[p * 4] == 17 && pixels[p * 4 + 1] == 34 && pixels[p * 4 + 2] == 68);
        await renderer.Change("onchange", e => e.Attribute("id") == "qr-design", "Artwork");
        Assert.Contains(renderer.Elements(), e => e.Tag == "code" && e.Text == styled.Text);
        Assert.Equal(renderer.PreviewUri(), renderer.Elements().Single(e => e.Attribute("download") == "qr-wifi.png").Attribute("href"));
        Assert.Empty(renderer.Errors);
    }

    [Fact]
    public async Task ArtworkEditsUseSharedPayloadAndMainPreview() {
        await using var renderer = new PlaygroundRenderer();
        await renderer.Start();
        await renderer.Change("oninput", e => e.Tag == "textarea" && e.Attributes.ContainsKey("placeholder"), "https://example.com/shared");
        await renderer.Change("onchange", e => e.Attribute("id") == "qr-design", "Artwork");
        var before = renderer.PreviewUri();
        await renderer.Click("Tidal ribbons");
        Assert.NotEqual(before, renderer.PreviewUri());
        Assert.Single(renderer.Elements(), e => e.Tag == "textarea");
        Assert.Contains(renderer.Elements(), e => e.Tag == "code" && e.Text == "https://example.com/shared");
        Assert.Equal(renderer.PreviewUri(), renderer.Elements().Single(e => e.Attribute("download") == "qrcode.png").Attribute("href"));
        await renderer.Change("oninput", e => e.Tag == "textarea" && e.Attributes.ContainsKey("placeholder"), "https://example.com/latest");
        Assert.Contains(renderer.Elements(), e => e.Tag == "code" && e.Text == "https://example.com/latest");
        Assert.Empty(renderer.Errors);
    }

    [Fact]
    public async Task SceneEditsAndUndoRetainSharedPayloadAndRecipe() {
        await using var renderer = new PlaygroundRenderer();
        await renderer.Start();
        await renderer.Change("oninput", e => e.Tag == "textarea" && e.Attributes.ContainsKey("placeholder"), "SCENE-SHARED");
        await renderer.Change("onchange", e => e.Attribute("id") == "qr-design", "Artwork");
        await renderer.Change("onchange", e => e.Tag == "select" && e.Attribute("value") == "pattern", "scene");
        await renderer.Click("Electric city");
        await renderer.Change("oninput", e => e.Tag == "textarea" && e.Attributes.ContainsKey("placeholder"), "SCENE-LATEST");
        await renderer.Click("Undo");
        var uri = renderer.Elements().Single(e => e.Attribute("download") == "qr-scene.cgxart").Attribute("href")!;
        var recipe = QrSceneRecipe.FromXml(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(uri.Split(',')[1])));
        Assert.Equal("SCENE-LATEST", recipe.Payload);
        Assert.Equal(QrSceneStyle.TropicalGarden, recipe.Design.Style);
        Assert.True(QrImageDecoder.TryDecodeImage(renderer.Png(), out var decoded));
        Assert.Equal(recipe.Payload, decoded.Text);
        await renderer.Change("oninput", e => e.Tag == "textarea" && e.Attributes.ContainsKey("placeholder"), "");
        Assert.Null(renderer.PreviewUri());
        Assert.DoesNotContain(renderer.Elements(), e => e.Attribute("download") is "qrcode.png" or "qr-scene.cgxart");
        Assert.Empty(renderer.Errors);
    }

    [Fact]
    public async Task InvalidSceneSeedSuppressesStaleOutputAndRecovers() {
        await using var renderer = new PlaygroundRenderer();
        await renderer.Start();
        await renderer.Change("onchange", e => e.Attribute("id") == "qr-design", "Artwork");
        await renderer.Change("onchange", e => e.Tag == "select" && e.Attribute("value") == "pattern", "scene");
        await renderer.Change("oninput", e => e.Tag == "input" && e.Attribute("value") == "17", "");
        Assert.Null(renderer.PreviewUri());
        await renderer.Change("oninput", e => e.Tag == "input" && e.Attribute("aria-invalid") == "true", "18");
        Assert.NotNull(renderer.PreviewUri());
        Assert.Empty(renderer.Errors);
    }

    [Fact]
    public async Task ADesignSwitchCancelsPendingArtworkWithoutLosingData() {
        await using var renderer = new PlaygroundRenderer();
        await renderer.Start();
        await renderer.Change("onchange", e => e.Attribute("id") == "qr-design", "Artwork");
        var pending = renderer.Change("oninput", e => e.Tag == "textarea" && e.Attributes.ContainsKey("placeholder"), "LATEST-DESIGN");
        await renderer.Change("onchange", e => e.Attribute("id") == "qr-design", "Styled");
        await pending;
        Assert.True(QrImageDecoder.TryDecodeImage(renderer.Png(), out var decoded));
        Assert.Equal("LATEST-DESIGN", decoded.Text);
        Assert.DoesNotContain(renderer.Elements(), e => e.Attribute("aria-label") == "Current artwork");
        Assert.Empty(renderer.Errors);
    }

    [Fact]
    public async Task ANewPayloadCancelsSearchAndPreventsObsoleteCandidates() {
        await using var renderer = new PlaygroundRenderer();
        await renderer.Start();
        await renderer.Change("onchange", e => e.Attribute("id") == "qr-design", "Artwork");
        var comparison = renderer.Click("Compare alternatives");
        await renderer.Change("oninput", e => e.Tag == "textarea" && e.Attributes.ContainsKey("placeholder"), "LATEST-SEARCH");
        await comparison;
        Assert.Contains(renderer.Elements(), e => e.Tag == "code" && e.Text == "LATEST-SEARCH");
        Assert.DoesNotContain(renderer.Elements(), e => e.Tag == "button" && e.Text == "Use this design");
        Assert.Equal(renderer.PreviewUri(), renderer.Elements().Single(e => e.Attribute("download") == "qrcode.png").Attribute("href"));
        Assert.Empty(renderer.Errors);
    }

    [Fact]
    public async Task SelectingComparedArtworkReplacesSharedDownloadsAndDecodeClearsIt() {
        await using var renderer = new PlaygroundRenderer();
        await renderer.Start();
        await renderer.Change("oninput", e => e.Tag == "textarea" && e.Attributes.ContainsKey("placeholder"), "https://example.com/choice");
        await renderer.Change("onchange", e => e.Attribute("id") == "qr-design", "Artwork");
        await renderer.Click("Tidal ribbons");
        await renderer.Click("Compare alternatives");
        var first = renderer.PreviewUri();
        await renderer.Click("Use this design");
        Assert.NotEqual(first, renderer.PreviewUri());
        Assert.Equal(renderer.PreviewUri(), renderer.Elements().Single(e => e.Attribute("download") == "qrcode.png").Attribute("href"));
        Assert.True(QrImageDecoder.TryDecodeImage(renderer.Png(), out var decoded));
        Assert.Equal("https://example.com/choice", decoded.Text);
        var selected = renderer.PreviewUri();
        await renderer.Change("onchange", e => e.Tag == "input" && e.Attribute("value") == "320", "400");
        Assert.Equal(selected, renderer.PreviewUri());
        Assert.Contains(renderer.Elements(), e => e.Tag == "button" && e.Text == "Selected design");
        await renderer.Change("onchange", e => e.Tag == "select" && e.Attribute("value") == "Generate", "Decode");
        Assert.Null(renderer.PreviewUri());
        Assert.DoesNotContain(renderer.Elements(), e => e.Attribute("aria-label") == "Current artwork");
        Assert.Empty(renderer.Errors);
    }

    [Fact]
    public async Task OversizedScenePayloadReportsFailureWithoutBreakingTheForm() {
        await using var renderer = new PlaygroundRenderer();
        await renderer.Start();
        await renderer.Change("onchange", e => e.Attribute("id") == "qr-design", "Artwork");
        await renderer.Change("onchange", e => e.Tag == "select" && e.Attribute("value") == "pattern", "scene");
        await renderer.Change("oninput", e => e.Tag == "textarea" && e.Attributes.ContainsKey("placeholder"), new string('x', 5000));
        Assert.Null(renderer.PreviewUri());
        Assert.Empty(renderer.Errors);
        await renderer.Upload("Open scene recipe", new SceneBrowserFile(new QrSceneRecipe("RECOVERED", QrScenePresets.Create(QrSceneStyle.TropicalGarden)).ToXml()));
        Assert.NotNull(renderer.PreviewUri());
        Assert.Contains(renderer.Elements(), e => e.Tag == "textarea" && e.Attribute("value") == "RECOVERED");
        Assert.Empty(renderer.Errors);
    }

    [Fact]
    public async Task GirocodeRetainsPaymentEncodingAndOffersSupportedDesigns() {
        await using var renderer = new PlaygroundRenderer();
        await renderer.Start();
        await renderer.Click("WiFi");
        await renderer.Change("onchange", e => e.Attribute("id") == "qr-design", "Artwork");
        await renderer.Change("onchange", e => e.Tag == "select" && e.Attribute("value") == "WiFi", "Girocode");
        await renderer.Change("oninput", e => e.Tag == "input" && e.Attribute("value") == "Acme Corp", "Müller");
        Assert.True(QrImageDecoder.TryDecodeImage(renderer.Png(), out var decoded));
        var payload = Payloads.QrPayloads.Girocode("DE89370400440532013000", "COBADEFFXXX", "Müller", 99.99m, "Invoice-2024-001");
        Assert.Equal(payload.Text, decoded.Text);
        Assert.Equal(System.Text.Encoding.Latin1.GetBytes(payload.Text), decoded.Bytes);
        Assert.Equal(QrErrorCorrectionLevel.M, decoded.ErrorCorrectionLevel);
        Assert.Equal("Styled", renderer.Elements().Single(e => e.Attribute("id") == "qr-design").Attribute("value"));
        Assert.Empty(renderer.Errors);
    }
}
#endif
