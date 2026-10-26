# Agent Guide (CodeGlyphX Website)

This folder contains the PowerForge.Web website for CodeGlyphX.

Repo layout (maintainer):
- Repository root: `$env:EVOTEC_GITHUB_ROOT` on PowerShell or `$EVOTEC_GITHUB_ROOT` on POSIX shells; use the profile fallback only when it is unset
- Website folder: `<Evotec repo root>/CodeGlyphX/Website`
- Engine repo: `<Evotec repo root>/PSPublishModule`

## How To Build

- Fast dev build:
  - `.\build.ps1 -Dev`
- Serve + watch:
  - `.\build.ps1 -Serve -Watch -Dev`
- CI-equivalent (strict gates enabled by mode):
  - `powerforge-web pipeline --config .\pipeline.json --mode ci`

If you don't have the engine repo next to this repo, set:
- `POWERFORGE_ROOT` = path to `PSPublishModule`

## Key Files

- Site config: `site.json`
  - Features: `docs`, `apiDocs`, `search`, `notFound`
- Pipeline: `pipeline.json`
  - API docs step generates into `./_site/api`
  - API docs nav token injection uses:
    - `config: ./site.json`
    - `nav: ./site.json`
    - `navContextPath: "/"` (keeps API header nav consistent with non-API pages)
- Theme: `themes/codeglyphx/theme.manifest.json`

## One Header and Footer

Every page shares `themes/codeglyphx/partials/header.html` and `footer.html`; do not add another copy.

- The header is built from the `site.json` `main` menu: groups with `Id`, `Title`, `Description` and `Items`. Each item needs an `Id` with a thumbnail in `data/visuals.json` (`nav.thumbs`, drawn by SiteSpecimens). The top-level `Home` item keeps the verify check happy; the brand link carries it.
- API reference and `/powershell/commands/`: the `export-site-chrome` step runs `dotnet run build/SiteChrome.cs -- export ./_site ./_temp/chrome` after the site build, copying the header and footer from the rendered home page; the apidocs steps pass them as `headerHtml`/`footerHtml`. `partials/api-header.html` and `api-footer.html` are only fallbacks for the theme contract.
- Playground: `layouts/playground.html` renders `/playground/` with the site chrome around `#app`. The overlay copies only the Blazor app's `_framework`, `_content` and playground stylesheets, and `compose-playground-shell` (`SiteChrome.cs playground`) fills the import map and fingerprinted `blazor.webassembly` script from the published `index.html`. `CodeGlyphX.Website/Layout/MainLayout.razor` renders only the playground.
- Tabs, filters and toggles use the `.cgx-tabs`/`.cgx-tab` component in `static/css/app.css` (module-dot key, tabular count, viewfinder corners on the selected option); do not add another segmented-control style.

## Inner Page Visuals

Docs figures, the docs map, benchmark charts, navigation thumbnails and the product-page codes are CodeGlyphX output too: `dotnet run --project ./tools/SiteSpecimens -c Release -- .` also writes `static/visuals/**` and `data/visuals.json` (deterministic; commit both). Docs pages place figures with `{{< cgx-figure id="qr-ecc" caption="..." >}}` (ids under `docs.figures`). Hand-maintained inputs: `data/docs-map.json` (the `/docs/` map) and `data/release-highlights.json` (what each release drew, shown on the changelog). Captions only state decoded values that the tool decoded from the rendered image.

## Home Page Data

The home page (`content/pages/index.md`, layout `landing`) reads committed data. Regenerate it when the library or ImagePlayground changes, then review the diff:

- Copy and examples: `data/landing.json` (hand-edited).
- Specimen sheet, payload codes and hero decode results: `data/specimens.json` and `static/specimens/*.svg`, produced by CodeGlyphX itself:
  - `dotnet run --project ./tools/SiteSpecimens -c Release -- .`
  - The hero and gallery art in `static/images/art/` comes from `Assets/Examples`. The tool decodes each image, so the page only claims what CodeGlyphX reads back.
- PowerShell (home section and `/powershell/`): run both scripts against the same ImagePlayground build, so the command list and examples describe one consistent snapshot:
  - `pwsh ./build/Sync-ImagePlayground.ps1 [-ModulePath <ImagePlayground.psd1>]` writes `data/imageplayground.json`.
  - `pwsh ./build/Render-PowerShellExamples.ps1 [-ModulePath <ImagePlayground.psd1>] [-Id wifi,barcodes]` runs every `powershell/<id>.ps1` listed in `powershell/examples.json` and writes the files each one produced to `static/powershell/examples/<id>/`, plus `data/powershell-examples.json` (script text, console output, output files). Pages show scripts exactly as they ran; do not link to example files on GitHub.
  - Pages follow the latest source and do not wait for an ImagePlayground release. To render from source before it reaches the PowerShell Gallery, point `-ModulePath` at an ImagePlayground checkout with `IMAGEPLAYGROUND_DEVELOPMENT=1` and pass `-ModuleVersion` with the version it will ship as. Keep the snapshot version accurate; source validation does not establish Gallery publication.
  - `data/powershell-roadmap.json` is hand-maintained: what CodeGlyphX can do and whether ImagePlayground exposes it (`available`, `partial`, `planned`). Update it when ImagePlayground ships new commands, and test anything marked available.

## Deploy + Recovery

- Production hosting: GitHub Pages at `https://codeglyphx.com`, behind Cloudflare.
- Deployment workflow: `../.github/workflows/website-deploy.yml`, using the pinned shared PowerForge website build and Cloudflare policy actions plus GitHub's Pages deployment action.
- Pushes affecting the site on `master`, published releases, and manual dispatch build and deploy `Website/_site`. GitHub Pages uses the Actions publishing source and the protected `github-pages` environment.
- The protected `production` environment's `CLOUDFLARE_ZONE_ID` variable and existing organization secret `CLOUDFLARE_API_TOKEN` supply the post-deployment Cloudflare policy action. The build receives no Cloudflare credential. Keep cache rules and response headers in `site.json`; GitHub Pages does not interpret `_headers`.
- The site and playground are static. Blazor WebAssembly runs in the visitor's browser; there is no CodeGlyphX server API, database, or scheduled host build.
- Recovery is a deployment from a known good source revision through the same workflow. Do not add repo-local deployment, Cloudflare, or server backup scripts.
- See `HOSTING.md` for the hosting contract and migration rollback boundary.

## Theme Best Practices (Nav Stability)

- Prefer Scriban helpers:
  - `{{ pf.nav_links "main" }}`
  - `{{ pf.nav_actions }}`
- Avoid `navigation.menus[0]` (menu ordering can change across sites/profiles).

