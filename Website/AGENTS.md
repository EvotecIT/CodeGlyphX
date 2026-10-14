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

## Home Page Data

The home page (`content/pages/index.md`, layout `landing`) reads committed data. Regenerate it when the library or ImagePlayground changes, then review the diff:

- Copy and examples: `data/landing.json` (hand-edited).
- Specimen sheet, payload codes and hero decode results: `data/specimens.json` and `static/specimens/*.svg`, produced by CodeGlyphX itself:
  - `dotnet run --project ./tools/SiteSpecimens -c Release -- .`
  - The hero and gallery art in `static/images/art/` comes from `Assets/Examples`. The tool decodes each image, so the page only claims what CodeGlyphX reads back.
- PowerShell (home section and `/powershell/`): run both scripts against the same published ImagePlayground version, so the command list and the examples describe one release:
  - `pwsh ./build/Sync-ImagePlayground.ps1 [-ModulePath <ImagePlayground.psd1>]` writes `data/imageplayground.json`.
  - `pwsh ./build/Render-PowerShellExamples.ps1 [-ModulePath <ImagePlayground.psd1>] [-Id wifi,barcodes]` runs every `powershell/<id>.ps1` listed in `powershell/examples.json` and writes the files each one produced to `static/powershell/examples/<id>/`, plus `data/powershell-examples.json` (script text, console output, output files). Pages show scripts exactly as they ran; do not link to example files on GitHub.
  - `data/powershell-roadmap.json` is hand-maintained: what CodeGlyphX can do and whether ImagePlayground exposes it (`available`, `partial`, `planned`). Update it when ImagePlayground ships new commands, and test anything marked available.

## Deploy + Recovery

- Production pull timer: `../deploy/linux/systemd/codeglyphx-site-pull.timer`
- Host pull configuration: `../deploy/linux/codeglyphx.site-pull.env.example`
- Build and Cloudflare policy workflow: `../.github/workflows/website-deploy.yml`
- Encrypted recovery workflow: `../.github/workflows/server-backup.yml`
- Host recovery manifest: `../deploy/linux/codeglyphx.serverrecovery.json`
- The OVH host fetches `master` over HTTPS, checks public GitHub release changes, builds with the pinned PowerForge revision, and promotes the site through the shared `powerforge-site-deploy` runtime. The root-owned host configuration holds the Cloudflare purge token; encrypted recovery capture includes it.
- The protected `production` environment retains Cloudflare policy credentials and backup credentials. The backup job runs from the fixed-egress self-hosted runner and keeps its repository write key off OVH.
- Shared PowerForge actions and runtimes own cache policy, promotion, purge, provenance checks, rollback, and backup publication. Do not add repo-local deployment or Cloudflare scripts.
- Canonical deployment and cache guidance lives in `<Evotec repo root>/PSPublishModule/Docs` and `Deployment/Linux`.

## Theme Best Practices (Nav Stability)

- Prefer Scriban helpers:
  - `{{ pf.nav_links "main" }}`
  - `{{ pf.nav_actions }}`
- Avoid `navigation.menus[0]` (menu ordering can change across sites/profiles).

