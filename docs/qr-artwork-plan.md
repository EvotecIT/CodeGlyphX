# CodeGlyphX QR artwork plan

Status: active direction. Prepared 26 October 2026 and updated 30 October 2026 after the polish pass (#311), the benchmarks redesign (#312) and the curated gallery (#314) merged.

The goal is the best-looking QR artwork a developer can ship with confidence. Visual range and quality come first; every style we present must also be shown to read reliably. Lots of tools make decorative codes. Few prove that each of their styles scans across the payloads, logos and sizes people actually use. That combination is the product position.

## What already exists

The engine already covers most of what styled-QR studios offer:

- Image and illustrated art composition (`QrImageComposer`, `QrIllustratedComposer`, `QrSceneComposition`) with full-bleed pictures and woven logos.
- Art themes and variants (`QrArt.Theme`, `QrArtTheme`), module shapes, eye styles, gradients, palette zones, foreground and background patterns.
- Canvas decoration: frames, halos, badges, bands, splashes, grain and vignettes.
- Delivery simulations (`QrImageDeliveryOptions`): screen size, JPEG compression, perspective and print raster.
- Static heuristics (`QR.EvaluateScanHeuristics`) and the editable `.cgxart` scene recipe described in [Qualifying an illustrated QR scene](qr-scene-qualification.md).
- Vector and document output: SVG, PDF and EPS.

The gaps are curation, guidance, depth of verification and print colour, not basic capability.

## Current state (26 October 2026)

An audit of 194 renders from 3.1.0 covered the 12 hero presets (light and dark), themes, call-to-action frames, photo treatments, scenes and patterns, with short and long URLs and with and without a logo.

- **Scanning is solid.** Every render passes the native validator. ZXing-C++ reads every render at full size; the only half-size failures are the Botanical hero with a long URL.
- **The looks are not yet premium.** Format and version modules ignore the body style, dark variants are the light design inside a black border, gradients go muddy, the logo plate is always a plain white square, and several hero and theme names promise effects that do not render. Caption frames use a barcode font and long captions can collide with frame decorations. Scenes and patterns still place a plain code card on clip-art or cut the art into rectangles.

The next work fixes these shared defects and the existing heroes first, then adds new hero looks, then reworks photo treatments, scenes and patterns. Each step ships only after the full scan matrix and a visual review of real renders.

## After QR art v3 (28 October 2026)

QR art v3 merged in #308, which also includes #309 and #310. It fixed the shared defects above: format and version bits follow the body style, dark variants are designed as dark looks, gradients stay clean, logos sit on whole-module plates, and every legacy theme passes strict ZXing. The theme, Wave 1, photo and scene/pattern matrices all pass both decoders.

A visual review of master against the best commercial generators found:

- **Hero presets (24): ready to ship.** They're coherent and modern, with eyes that belong to the body. Botanical, MinimalLine and Sunset are the strongest. Remaining defects:
  - MinimalLine data dots overlap the bottom-right alignment ring.
  - The Brand plate has a hairline outline, and its red mark reads as a record button.
  - Neon light is almost the same image as Neon dark.
  - MonoClassy and PaperCutout are near-duplicates (brown on cream).
- **Website style board (44): stale.** The landing page still shows images generated in February. About 70% of them are the same dense dark body on a white card.
- **Expressive art (9): pleasant but not integrated.** The code sits on an opaque card, so the illustration is only a frame. Earth isn't publishable: the card is off-centre and the photo-filled modules speckle. Landscape and Geometric read as colour faults.
- **Scenes (6): charming, but the code inside is plain squares.** The committed images predate the scene rework.

Scanning is no longer the gap. Curation, presentation and integration between art and code are.

## Progress (30 October 2026)

- **Polish pass: done in #311.** The hero defects above are fixed, the style board is regenerated from current code, Earth is dropped and the scene images are re-rendered.
- **Curated gallery: done in #314.** The `/gallery/` page shows 20 looks in 29 variants, generated from current code, with recipes and scan badges recorded from a 48-check matrix per variant. Four mockups show looks on a poster, business card, package and table tent. Sunset dark, Ocean dark, Minimal Line dark and Soft Diamond failed the matrix and were dropped.
- Follow-ups from that work: the legacy theme set has several near-identical looks, the README's colourful gallery image predates the gallery, and the website specimens need regenerating for 3.1.0.

## Priorities

1. **Halftone and image-embedded QR (L).** The flagship family in workstream 2.
2. **Brand kit from a logo (M).** Workstream 7.
3. **Recommendations and a live scan-safety meter (M).** Workstreams 3 and 9.
4. **Call-to-action frame library (M).** Workstream 8.
5. **Print-ready colour and print-aware presets (S–M).** Workstream 5.
6. **Organic body shapes, scene-styled codes and seasonal packs (M, then S each).** Workstream 2.
7. **SVG-native colour animation (M).** Workstream 10, once the static gallery meets its bar.
8. **Deeper scan simulation and reusable recipes.** Workstreams 4 and 6, continuing alongside.

## Workstreams

Start with the gallery. It exposes where the current art is weak and gives the other workstreams their test set.

### 1. Curated style gallery with a quality bar

- Select 20 to 30 strong presets instead of exposing more raw options as the main entry point.
- Qualify each preset across a fixed matrix: short and long URLs, with and without a logo, light and dark logos, small and large print sizes, and the delivery simulations.
- A preset enters the gallery only when it passes the matrix. Fix or drop failures rather than documenting around them.
- Publish the gallery on the website with each preset's rendered sample, its recipe and its measured results.
- Generate every website gallery image from current code during the website build or a checked script, so a gallery can't go stale again.
- Show each preset in context (poster, business card, package, table tent) as large single images rather than composite sheets.
- Each look must change body shape, palette and background together. Merge presets that differ only in palette.

### 2. More and better art families

- Halftone and image-embedded codes where the picture forms the code rather than sitting behind a card. The photo is dithered into module sub-cells while each module centre keeps decode contrast, with controllable tone and dot shape. This is the flagship family, and it needs no ML.
- Illustrated scenes and pattern-filled modules beyond the current themes. Inside a scene, the code takes the scene's palette and module style instead of plain squares.
- Organic body shapes (blob, brush stroke, stitch) with matching eyes.
- Additional finder (eye) styles that keep finder geometry intact.
- Seasonal and themed packs (holidays, sports, food, conferences) built from scenes and recipes.
- Every new family joins the gallery through the same quality bar.

### 3. Recommendations instead of trial and error

- Given a payload, logo and target print size or scan distance, recommend version, error correction and module size.
- When a combination cannot work, say so plainly with a concrete fix: a larger size, a higher detail level, a simpler logo or a different preset.
- Report the largest version that remains practical at each print size.
- Keep recommendations distinct from guarantees in the API and documentation.

### 4. Deeper scan simulation

- Extend the delivery checks from a handful of fixed probes to a seeded set of several dozen simulated captures: blur, glare, rotation, perspective, low light, moiré, noise and distance.
- Report a pass rate and the failing conditions, with reproducible seeds.
- Describe the result as a software simulation. It never replaces scanning a printed proof on real devices.

### 5. Print-ready colour

- Add pure-black (K only) and CMYK colour output to PDF and EPS. Today only the PDF reader understands DeviceCMYK.
- Consider spot-colour output after CMYK, if print users ask for it.
- Verify output in the viewers and RIP-facing tools printers actually use.

### 6. Reusable style recipes

- Extend the `.cgxart` recipe idea from scenes to every render style, so a complete look can be saved, versioned and reapplied to a new payload.
- Use recipes as the source for the website gallery and for consumers' brand styles.

### 7. Brand kit from a logo

- Extract the logo's palette and choose contrast-safe dark and light colours.
- Size and shape the logo plate automatically.
- Generate a small set of matched looks (around six), each qualified like a gallery preset.
- Save the result as a recipe so it can be reapplied to any payload.

### 8. Call-to-action frames

- Extend the existing canvas captions into a frame library: "Scan me" labels, arrows, badges and phone frames, with proper text layout for long and localized captions.
- Frames never crowd the quiet zone, and every frame is checked across the scan matrix.

### 9. Live scan-safety meter

- Show contrast, quiet-zone, logo-coverage and module-size gauges in the Playground while a look is edited, using `QR.EvaluateScanHeuristics` and the recommendations from workstream 3.
- Present it as guidance, never as proof that a code will scan.

### 10. SVG-native colour animation

- Animate colour only (eye pulse, shimmer, gradient sweep) in SVG and CSS over a fixed code. Read back sampled frames.
- Starts only after the static gallery meets its quality bar. See Decisions.

## Decisions

- **No QR video output.** Video use cases (Stories, Reels, stream overlays, signage, event screens) belong to design and streaming tools rather than a .NET library, and moving codes read less reliably than still ones. Developers who need frames can render images and assemble them with their own video tooling.
- **No proprietary round code.** A non-QR circular format with its own reader conflicts with the roadmap rule that new symbologies need a public specification and an independent decoder.
- **Animated light effects wait.** Effects that move only colour over a fixed code are possible later, with every frame read back, once the static gallery meets its quality bar. Workstream 10 describes that later form: SVG and CSS only, never raster video.
- **No runtime ML.** Image integration, brand kits and style extraction use deterministic analysis inside the managed renderer.
- **No hosted redirects, analytics or accounts.** These are service features outside the library.

## Evidence for completion

- Each gallery preset has a recorded matrix result from the current build.
- Every gallery image on the website is generated from the release commit.
- Each new family and each brand-kit look has a visual review of real renders, not only a scan result.
- Recommendation and simulation outputs are covered by tests with fixed seeds.
- CMYK and K-only output is checked in at least two independent PDF/EPS consumers.
- Physical device and print checks follow [Qualifying an illustrated QR scene](qr-scene-qualification.md) for a representative subset of presets before the gallery is promoted as qualified.
