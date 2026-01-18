using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CodeGlyphX.Website;
using PowerForge.Blazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register documentation service with API docs
builder.Services.AddScoped<DocumentationService>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var docService = new DocumentationService();

    // Add API reference from XML docs
    docService.AddSource(new HttpXmlDocumentationSource(
        httpClient,
        "api-docs/CodeGlyphX.xml",
        new XmlDocSourceOptions
        {
            Id = "api",
            DisplayName = "API Reference",
            Description = "Complete API documentation for CodeGlyphX",
            Order = 100
        }));

    return docService;
});

await builder.Build().RunAsync();
