# CodeGlyphX Website (PowerForge.Web)

This repo now uses the PowerForge.Web pipeline in `Website/` as the single source of truth
for the CodeGlyphX website. The legacy website build scripts under `Build/` were removed.

## Build
```powershell
pwsh Website/build.ps1
pwsh Website/build.ps1 -Serve
```

If PowerForge.Web.Cli is not found, set `POWERFORGE_ROOT` to the PSPublishModule repo:
```powershell
$env:POWERFORGE_ROOT = "C:\Support\GitHub\PSPublishModule"
pwsh Website/build.ps1
```

## Structure
- `Website/content/pages/` – homepage + marketing pages (FAQ, pricing, showcase, benchmarks)
- `Website/content/docs/` – documentation pages (one file = one page)
- `Website/data/` – JSON data for sections (FAQ, benchmarks, stats, etc.)
- `Website/themes/codeglyphx/` – theme layouts/partials
- `Website/static/` – assets, API fragments, vendor libs, CNAME

## Pipeline (Website/pipeline.json)
1) Build static pages into `Website/_site`
2) Build CodeGlyphX (Release)
3) Publish Blazor playground into `/playground`
4) Generate API reference into `/api` (docs template + JSON)
5) Generate `llms.txt` and `sitemap.xml`
6) Optimize + audit

## API Reference
The API reference is generated under `/api`. `/docs/api` is a redirect for legacy links.

## Notes
- The Blazor project (`CodeGlyphX.Website`) is still used for the playground publish step.
- Generated outputs live in `Website/_site` and are ignored by git.
