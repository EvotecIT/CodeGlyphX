#if NET10_0
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeGlyphX.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed partial class PlaygroundLivePreviewTests {
    [Fact]
    public async Task InputEventsUpdatePayloadColorsAndDownloadArtifacts() {
        await using var renderer = new PlaygroundRenderer();
        await renderer.Start();
        await renderer.Change("oninput", e => e.Attributes.ContainsKey("placeholder") && e.Tag == "textarea", "LIVE-PAYLOAD");
        await renderer.Change("oninput", e => e.Attribute("aria-label") == "Foreground color", "#112244");
        await renderer.Change("oninput", e => e.Attribute("aria-label") == "Background color", "#eeffff");
        var png = renderer.Png();
        Assert.True(QrImageDecoder.TryDecodeImage(png, out var decoded));
        Assert.Equal("LIVE-PAYLOAD", decoded.Text);
        var pixels = ImageReader.DecodeRgba32(png, out _, out _);
        Assert.Equal(new byte[] { 238, 255, 255, 255 }, pixels.Take(4));
        Assert.Contains(Enumerable.Range(0, pixels.Length / 4), p => pixels[p * 4] == 17 && pixels[p * 4 + 1] == 34 && pixels[p * 4 + 2] == 68);
        Assert.Equal(renderer.PreviewUri(), renderer.Elements().Single(e => e.Attribute("download") == "qrcode.png").Attribute("href"));
        var svg = renderer.Elements().Single(e => e.Attribute("download") == "qrcode.svg").Attribute("href")!;
        var xml = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(svg.Split(',')[1]));
        Assert.Contains("#112244", xml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("#EEFFFF", xml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(renderer.Elements(), e => e.Tag == "pre" && e.Text.Contains("Foreground = new Rgba32(17, 34, 68)", StringComparison.Ordinal));
        Assert.Empty(renderer.Errors);
    }

    [Theory]
    [InlineData("QR URL", "textarea")]
    [InlineData("Code 128", "input")]
    [InlineData("Data Matrix", "textarea")]
    public async Task EmptyInputClearsPreviewAndDownloads(string preset, string tag) {
        await using var renderer = new PlaygroundRenderer();
        await renderer.Start();
        await renderer.Click(preset);
        Assert.NotNull(renderer.PreviewUri());
        await renderer.Change("oninput", e => e.Tag == tag && e.Events.ContainsKey("oninput"), "");
        Assert.Null(renderer.PreviewUri());
        Assert.DoesNotContain(renderer.Elements(), e => e.Attributes.ContainsKey("download"));
        Assert.Empty(renderer.Errors);
    }

    [Fact]
    public async Task LowContrastPalettePreservesChosenColorsAndReportsRisk() {
        await using var renderer = new PlaygroundRenderer();
        await renderer.Start();
        await renderer.Check("Enable palette colors");
        await renderer.Change("oninput", e => e.Tag == "input" && e.Attribute("type") == "color" && e.Attribute("value") == "#00ffd5", "#ffeecc");
        var pixels = ImageReader.DecodeRgba32(renderer.Png(), out _, out _);
        Assert.Contains(Enumerable.Range(0, pixels.Length / 4), p => pixels[p * 4] == 255 && pixels[p * 4 + 1] == 238 && pixels[p * 4 + 2] == 204);
        Assert.Contains(renderer.Elements(), e => e.Tag == "li" && e.Text.Contains("contrast", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(renderer.Errors);
    }

    [Fact]
    public async Task RapidEditsPublishLatestPayloadAndPreventPendingDownloads() {
        await using var renderer = new PlaygroundRenderer();
        await renderer.Start();
        var first = renderer.Change("oninput", e => e.Tag == "textarea" && e.Attributes.ContainsKey("placeholder"), "OLDER",
            () => Assert.DoesNotContain(renderer.Elements(), e => e.Attributes.ContainsKey("download")));
        var latest = renderer.Change("oninput", e => e.Tag == "textarea" && e.Attributes.ContainsKey("placeholder"), "LATEST");
        await Task.WhenAll(first, latest);
        Assert.True(QrImageDecoder.TryDecodeImage(renderer.Png(), out var decoded));
        Assert.Equal("LATEST", decoded.Text);
        Assert.Empty(renderer.Errors);
    }

    [Fact]
    public async Task ModeSwitchAndNavigationCancelQueuedGeneration() {
        await using var renderer = new PlaygroundRenderer();
        await renderer.Start();
        var pending = renderer.Change("oninput", e => e.Tag == "textarea" && e.Attributes.ContainsKey("placeholder"), "PENDING");
        await renderer.Change("onchange", e => e.Tag == "select" && e.Attribute("value") == "Generate", "Decode");
        await pending;
        Assert.Null(renderer.PreviewUri());
        Assert.DoesNotContain(renderer.Elements(), e => e.Attributes.ContainsKey("download"));
        await renderer.Change("onchange", e => e.Tag == "select" && e.Attribute("value") == "Decode", "Generate");
        pending = renderer.Change("oninput", e => e.Tag == "textarea" && e.Attributes.ContainsKey("placeholder"), "LEAVING");
        await renderer.Remove();
        await pending;
        Assert.Empty(renderer.Errors);
    }

    // Exercise the actual Blazor event bindings and sibling rendering without browser JS.
    // RenderTree APIs are confined to this net10 UI test boundary.
#pragma warning disable BL0006
    private sealed class PlaygroundRenderer : Renderer {
        private readonly ServiceProvider _services;
        private int _root;
        public List<Exception> Errors { get; } = new();
        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();
        public PlaygroundRenderer() : this(new ServiceCollection().AddSingleton<IJSRuntime>(new NoBrowserJs()).BuildServiceProvider()) { }
        private PlaygroundRenderer(ServiceProvider services) : base(services, NullLoggerFactory.Instance) { _services = services; }
        public Task Start() => Dispatcher.InvokeAsync(async () => {
            _root = AssignRootComponentId(InstantiateComponent(typeof(Playground.Playground)));
            await RenderRootComponentAsync(_root, ParameterView.Empty);
        });
        public Task Remove() => Dispatcher.InvokeAsync(() => RemoveRootComponent(_root));
        public Task Change(string name, Func<Element, bool> match, string value, Action? assertPending = null) => Dispatcher.InvokeAsync(() => {
            var element = Elements().First(match);
            var dispatch = DispatchEventAsync(element.Events[name], new EventFieldInfo { ComponentId = element.ComponentId, FieldValue = value }, new ChangeEventArgs { Value = value });
            assertPending?.Invoke();
            return dispatch;
        });
        public Task Click(string text) => Dispatcher.InvokeAsync(() => {
            var element = Elements().First(e => e.Tag == "button" && e.Text.Trim() == text);
            return DispatchEventAsync(element.Events["onclick"], null, new MouseEventArgs());
        });
        public Task Check(string text) => Dispatcher.InvokeAsync(() => {
            var element = Elements().SkipWhile(e => e.Tag != "label" || !e.Text.Contains(text, StringComparison.Ordinal)).Skip(1).First(e => e.Tag == "input");
            return DispatchEventAsync(element.Events["onchange"], new EventFieldInfo { ComponentId = element.ComponentId, FieldValue = true }, new ChangeEventArgs { Value = true });
        });
        public string? PreviewUri() => Elements().FirstOrDefault(e => e.Attribute("alt") == "Generated code")?.Attribute("src");
        public byte[] Png() => Convert.FromBase64String(PreviewUri()!.Split(',')[1]);
        public IReadOnlyList<Element> Elements() {
            var result = new List<Element>();
            Read(_root, result);
            return result;
        }
        private void Read(int componentId, List<Element> result) {
            var tree = GetCurrentRenderTreeFrames(componentId);
            for (var i = 0; i < tree.Count; i++) {
                var frame = tree.Array[i];
                if (frame.FrameType == RenderTreeFrameType.Component) Read(frame.ComponentId, result);
                if (frame.FrameType != RenderTreeFrameType.Element) continue;
                var element = new Element(componentId, frame.ElementName);
                for (var a = i + 1; a < tree.Count && tree.Array[a].FrameType == RenderTreeFrameType.Attribute; a++) {
                    var attribute = tree.Array[a];
                    element.Attributes[attribute.AttributeName] = attribute.AttributeValue;
                    if (attribute.AttributeEventHandlerId != 0) element.Events[attribute.AttributeName] = attribute.AttributeEventHandlerId;
                }
                element.Text = string.Concat(tree.Array.Skip(i + 1).Take(frame.ElementSubtreeLength - 1).Where(f => f.FrameType is RenderTreeFrameType.Text or RenderTreeFrameType.Markup).Select(f => f.FrameType == RenderTreeFrameType.Text ? f.TextContent : f.MarkupContent));
                result.Add(element);
            }
        }
        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
        protected override void HandleException(Exception exception) => Errors.Add(exception);
        protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) _services.Dispose(); }
    }
#pragma warning restore BL0006

    private sealed class Element(int componentId, string tag) {
        public int ComponentId { get; } = componentId;
        public string Tag { get; } = tag;
        public string Text { get; set; } = "";
        public Dictionary<string, object> Attributes { get; } = new();
        public Dictionary<string, ulong> Events { get; } = new();
        public string? Attribute(string name) => Attributes.TryGetValue(name, out var value) ? value?.ToString() : null;
    }

    private sealed class NoBrowserJs : IJSRuntime {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => new(default(TValue)!);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => new(default(TValue)!);
    }
}
#endif
