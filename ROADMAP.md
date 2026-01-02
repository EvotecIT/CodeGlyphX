# ROADMAP

## Current focus

- Validate Phase 4 on Windows (real monitor capture) and decide whether Phase 5 robustness (rotation/thresholding) is needed.

---

## Phase 0 — Repo scaffolding & conventions

- [x] Flat folder structure created (no src/tests)
- [x] `CodeMatrix.sln` in root, projects added
- [x] Coding conventions applied (nullable, analyzers if desired but no extra deps)
- [x] Public API namespaces chosen and consistent
- [x] README skeleton created

Done definition:
- Solution builds, empty stubs compile, ROADMAP exists.

---

## Phase 1 — Basics (Core encoding + rendering + payloads)

- [x] Port QR encoder from HtmlForgeX (compiles, public API exposed)
- [x] Port Code128 encoder from HtmlForgeX (compiles, public API exposed)
- [x] Implement domain model (`BitMatrix`, `QrCode`, `Barcode1D`, `BarSegment`)
- [x] Implement payloads (WiFi, URL, mailto/tel/sms, vCard basic)
- [x] Implement OTP (AuthIMO critical):
  - [x] Base32 encode/decode (deterministic)
  - [x] `otpauth://totp` builder with correct escaping and stable output
- [x] Implement SVG renderers (QR + barcode)
- [x] Implement HTML renderers (generic + email-safe table with row compression)
- [x] Implement PNG writer + QR/Barcode PNG renderers (zlib stored blocks)

Done definition:
- Can encode QR/Code128 and render SVG/HTML/PNG from `CodeMatrix` only.
- OTP builder produces valid otpauth URL and can be encoded into a QR.

---

## Phase 2 — Tests (stability + regression)

- [x] Add Tests project
- [x] QR golden vectors with forced version+mask and SHA256 bit-packing
- [x] Code128 invariants tests
- [x] OTP builder tests (escaping, ordering, Base32)
- [x] Round-trip tests: Encode -> PNG -> decode pixels

Done definition:
- Tests run green and guard against regressions.

---

## Phase 3 — WPF display (AuthIMO integration gate)

- [x] `CodeMatrix.Wpf` project created and builds
- [x] `QrCodeControl` implemented (bindable, crisp rendering)
- [x] `Barcode128Control` implemented
- [x] Demo app shows both controls + exports
- [x] Performance sanity: rendering not allocating excessively in idle UI

Handoff gate (MUST):
- [x] **WPF display controls ready for AuthIMO agent**
  - Namespaces:
    - `CodeMatrix.Wpf` (`QrCodeControl`, `Barcode128Control`)
  - XAML usage:
    - `xmlns:wpf="clr-namespace:CodeMatrix.Wpf;assembly=CodeMatrix.Wpf"`
    - `<wpf:QrCodeControl Text="{Binding ...}" Ecc="M" ModuleSize="6" QuietZone="4" />`
    - `<wpf:Barcode128Control Value="{Binding ...}" ModuleSize="2" QuietZone="10" />`
  - References:
    - Add a project or NuGet reference to `CodeMatrix.Wpf` (which references `CodeMatrix`)

Done definition:
- WPF controls render correctly and are ready to be used in AuthIMO.

---

## Phase 4 — Screen scan feasibility (WPF read of screen)

- [x] Screen capture via BitBlt to BGRA buffer
- [x] Throttled scan loop (2–5 fps) with cancellation
- [x] Region selection UI (simple and reliable)
- [x] QR finder detection (1:1:3:1:1 scanline ratios)
- [x] Grid sampling -> `BitMatrix`
- [x] Decode clean on-screen QR and display payload

Done definition:
- Can decode a clean QR shown on screen in a selected region and copy result.

---

## Phase 5 — Improvements (more symbologies + stronger decode)

- [ ] Add Code39 encoder
- [ ] Add EAN-13/EAN-8 encoder
- [ ] Add UPC-A/UPC-E encoder
- [ ] Add Code93 encoder
- [ ] Consider 2D:
  - [ ] DataMatrix encoder (bigger effort)
  - [ ] PDF417 encoder (bigger effort)
- [ ] Improve pixel decode robustness (still “clean images” scope):
  - [ ] rotation handling
  - [ ] better thresholding
  - [ ] faster finder search

Done definition:
- Additional encoders added without breaking public API; tests updated.
