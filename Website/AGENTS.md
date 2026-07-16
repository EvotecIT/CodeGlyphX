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

## Deploy + Recovery

- Production deploy workflow: `../.github/workflows/website-deploy.yml`
- Encrypted recovery workflow: `../.github/workflows/server-backup.yml`
- Host recovery manifest: `../deploy/linux/codeglyphx.serverrecovery.json`
- The protected `production` environment owns host coordinates, deployment identities, the exact Cloudflare zone id, and backup credentials.
- Shared PowerForge actions own checkout, artifact packaging, cache policy, promotion, purge, provenance checks, rollback, and backup publication. Do not add repo-local deployment or Cloudflare scripts.
- Canonical deployment and cache guidance lives in `<Evotec repo root>/PSPublishModule/Docs` and `Deployment/Linux`.

## Theme Best Practices (Nav Stability)

- Prefer Scriban helpers:
  - `{{ pf.nav_links "main" }}`
  - `{{ pf.nav_actions }}`
- Avoid `navigation.menus[0]` (menu ordering can change across sites/profiles).

