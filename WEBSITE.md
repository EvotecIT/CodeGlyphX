# CodeGlyphX website

`Website/` is a PowerForge.Web site. Its Markdown, JSON data, theme, generated XML API reference, and Blazor playground are validated together by the website pipeline.

## Repository locations

Use `$env:EVOTEC_GITHUB_ROOT` in PowerShell or `$EVOTEC_GITHUB_ROOT` on POSIX shells when set. Otherwise use the profile fallback for the current machine.

- Website: `<Evotec repo root>/CodeGlyphX/Website`
- PowerForge/PSPublishModule owner: `<Evotec repo root>/PSPublishModule`

Set `POWERFORGE_ROOT` only when the engine is not discoverable next to the repository.

```powershell
$repoRoot = if ($env:EVOTEC_GITHUB_ROOT) { $env:EVOTEC_GITHUB_ROOT } else { 'C:\Support\GitHub' }
$env:POWERFORGE_ROOT = Join-Path $repoRoot 'PSPublishModule'
pwsh Website/build.ps1
```

Development and CI-equivalent modes:

```powershell
pwsh Website/build.ps1 -Dev
powerforge-web pipeline --config Website/pipeline.json --mode ci
```

## Sources of truth

- `Website/content/pages/`: marketing and product pages
- `Website/content/docs/`: user documentation, including the 2.0 migration guide
- `Website/data/`: FAQ, feature, release, benchmark, and navigation data
- `Website/themes/codeglyphx/`: layouts and partials
- `Website/static/`: owned static assets and API fragments
- `CodeGlyphX/bin/Release/net10.0/CodeGlyphX.xml`: generated API documentation input
- `CodeGlyphX.Website`: Blazor playground published into the site

Generated output is written under `Website/_site` and is ignored. Do not hand-edit generated API pages or copied static output.

## Pipeline contract

The CI pipeline must:

1. build the current CodeGlyphX release target and XML documentation;
2. render Markdown pages and data-driven sections;
3. publish the Blazor playground;
4. generate the API reference from the current assembly/XML pair;
5. generate search, sitemap, and agent-readable resources;
6. run route, link, budget, accessibility, SEO, and doctor checks configured by the site pipeline.

When a public API example changes, update the owned compiled example first, update the Markdown source, then run the pipeline. Never repair generated HTML directly.
