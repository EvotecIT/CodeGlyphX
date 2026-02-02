# CodeGlyphX PowerForge Migration Backlog

Short, actionable list for the PowerForge-based static site migration (1:1 parity first, then refactor).

## Parity (must not regress)
- Home page layout, CTA, style board, and code examples.
- Docs landing page content + left nav + anchors.
- Benchmarks page + data tables + summary block.
- Showcase page + carousel + download/actions.
- Pricing, FAQ, and Support content.
- API reference at `/api` plus redirect from `/docs/api`.
- Playground remains under `/playground/` (Blazor/WASM).

## Engine tasks
- Confirm PowerForge build outputs match live URLs and assets.
- Ensure nav/footer parity across all pages (no drift).
- Add smoke checks: missing nav links, broken assets, missing data files.
- Add sitemap defaults (auto + custom entries).

## Content/data
- Move any remaining hardcoded HTML into templates or data-driven content.
- Decide FAQ source format (JSON vs Markdown) and document both.
- Decide benchmark source format (JSON vs Markdown) and document both.

## Performance
- Cache headers for static assets (Cloudflare-friendly).
- Avoid hashing for vendor assets with fixed paths (Prism).
- Confirm highlight/copy buttons work on all docs/code blocks.
