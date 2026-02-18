# Agent Guide (CodeGlyphX Website)

This folder contains the PowerForge.Web website for CodeGlyphX.

Repo layout (maintainer):
- Website folder: `C:\Support\GitHub\CodeMatrix\Website`
- Engine repo: `C:\Support\GitHub\PSPublishModule`

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

## Deploy + Cloudflare Cache

- GitHub Pages deploy workflow: `../.github/workflows/pages.yml`
- Cloudflare secrets used by deploy:
  - `CLOUDFLARE_API_TOKEN`
  - `CLOUDFLARE_ZONE_ID_CODEGLYPHX`
- Post-deploy cache commands are standardized and route-driven from site config:
  - `cloudflare purge --site-config "Website/site.json"`
  - `cloudflare verify --site-config "Website/site.json" --warmup 1`
- Canonical cache-rule guidance lives in:
  - `C:\Support\GitHub\PSPublishModule\Docs\PowerForge.Web.Cloudflare.md`

## Theme Best Practices (Nav Stability)

- Prefer Scriban helpers:
  - `{{ pf.nav_links "main" }}`
  - `{{ pf.nav_actions }}`
- Avoid `navigation.menus[0]` (menu ordering can change across sites/profiles).

