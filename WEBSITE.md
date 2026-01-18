# CodeGlyphX Website Improvement Plan

This document outlines the planned improvements for the CodeGlyphX website. Tasks are organized by priority and can be assigned to different agents/contributors.

## Current Status

### What's Working Well
- **Home Page**: Hero, stats, features grid, supported formats, code examples, CTA
- **Playground**: QR codes with styling, Special QR payloads, basic 1D barcodes
- **Documentation**: Good coverage of all symbologies with code examples
- **API Reference**: Collapsible sidebar, type details, navigation

### Available Assets
Located in `Assets/Logo/`:
- `codeglyphx-qr-icon.png` - App icon (use for favicon, header)
- `codeglyphx-qr-icon.ico` - Windows icon format
- `Logo-evotec.png` - Evotec company logo
- `Logo-evotec-wide.png` - Wide Evotec logo

---

## Priority 1: Quick Wins (Polish)

### 1.1 Add Favicon and App Icons
**File**: `CodeGlyphX.Website/wwwroot/`
**Task**:
- Copy `Assets/Logo/codeglyphx-qr-icon.png` to `wwwroot/`
- Add favicon link in `index.html`
- Add apple-touch-icon for iOS

### 1.2 Add Copy Button to Install Command
**File**: `CodeGlyphX.Website/Pages/Index.razor`
**Task**: Add a click-to-copy button next to `dotnet add package CodeGlyphX`
```html
<div class="install-command">
    <code>dotnet add package CodeGlyphX</code>
    <button onclick="copyToClipboard('dotnet add package CodeGlyphX')">Copy</button>
</div>
```

### 1.3 Add Proper Footer
**File**: `CodeGlyphX.Website/Layout/MainLayout.razor` or new `Footer.razor`
**Task**: Create a footer component with:
- Copyright notice
- GitHub link with stars badge
- NuGet link with downloads badge
- Version number
- Links: Docs, API, Playground, GitHub Issues

### 1.4 Update Header Logo
**File**: `CodeGlyphX.Website/Layout/MainLayout.razor`
**Task**: Replace the CSS QR icon with actual `codeglyphx-qr-icon.png`

---

## Priority 2: Playground Enhancements

### 2.1 Add 2D Matrix Codes to Playground
**File**: `CodeGlyphX.Website/Pages/Playground.razor`
**Task**: Add new category tabs:
- Data Matrix (with size options)
- PDF417 (with error correction)
- Aztec (with error correction %)

**Implementation**:
```csharp
// Add to SelectedCategory options
<option value="DataMatrix">Data Matrix</option>
<option value="PDF417">PDF417</option>
<option value="Aztec">Aztec</option>
```

### 2.2 Add More 1D Barcode Types
**File**: `CodeGlyphX.Website/Pages/Playground.razor`
**Task**: Expand barcode dropdown to include:
- [ ] Codabar
- [ ] MSI (with checksum options)
- [ ] Plessey
- [ ] ITF / ITF-14
- [ ] UPC-A
- [ ] UPC-E
- [ ] Code 11

### 2.3 Add Image Decoder Demo
**File**: `CodeGlyphX.Website/Pages/Playground.razor` (new section)
**Task**: Add "Decode" tab that allows:
- Upload image (drag & drop or file picker)
- Display decoded result (text, format type)
- Show confidence/metadata

### 2.4 Add Preset Examples
**File**: `CodeGlyphX.Website/Pages/Playground.razor`
**Task**: Add quick-select buttons for common use cases:
- "WiFi Network" - Pre-fills WiFi form
- "Business Card" - Pre-fills vCard
- "Product Barcode" - EAN-13 example
- "Event Ticket" - Aztec with sample data
- "Shipping Label" - Code 128 example

### 2.5 Add Size/Scale Control
**File**: `CodeGlyphX.Website/Pages/Playground.razor`
**Task**: Add module size slider (1-20px) for QR codes

---

## Priority 3: Home Page Enhancements

### 3.1 Add Visual Showcase in Hero
**File**: `CodeGlyphX.Website/Pages/Index.razor`
**Task**: Add animated or static showcase of generated codes
- Option A: Carousel of QR/barcode examples
- Option B: Live-generating QR code animation
- Option C: Grid of different code types

### 3.2 Add Comparison Table
**File**: `CodeGlyphX.Website/Pages/Index.razor` (new section)
**Task**: Create comparison table vs other libraries:

| Feature | CodeGlyphX | ZXing.Net | QRCoder | BarcodeLib |
|---------|------------|-----------|---------|------------|
| Dependencies | 0 | Many | System.Drawing | System.Drawing |
| NuGet Size | ~500KB | ~2MB | ~200KB | ~150KB |
| AOT Support | Yes | No | No | No |
| 2D Codes | 5 types | 4 types | QR only | None |
| 1D Barcodes | 13 types | 8 types | None | 10 types |
| Decode Support | Yes | Yes | No | No |

**Note**: Verify these numbers before publishing

### 3.3 Add GitHub Stars Badge
**File**: `CodeGlyphX.Website/Pages/Index.razor`
**Task**: Add shields.io badge showing GitHub stars
```html
<img src="https://img.shields.io/github/stars/EvotecIT/CodeGlyphX?style=social" />
```

### 3.4 Add NuGet Downloads Badge
**File**: `CodeGlyphX.Website/Pages/Index.razor`
**Task**: Add shields.io badge showing NuGet downloads
```html
<img src="https://img.shields.io/nuget/dt/CodeGlyphX" />
```

---

## Priority 4: Documentation Improvements

### 4.1 Add Search Functionality
**File**: `CodeGlyphX.Website/Pages/Docs.razor`
**Task**: Implement client-side search across all doc pages
- Search input in sidebar
- Filter/highlight matching content
- Consider using a search index

### 4.2 Add "Edit on GitHub" Links
**File**: `CodeGlyphX.Website/Pages/Docs.razor`
**Task**: Add link to edit documentation on GitHub (when docs are in repo)

### 4.3 Add Code Copy Buttons
**File**: All doc sections with `<pre class="code-block">`
**Task**: Add copy button to all code blocks (similar to 1.2)

### 4.4 Add Missing Barcode Documentation
**File**: `CodeGlyphX.Website/Pages/Docs.razor`
**Task**: Add documentation pages for:
- [ ] Codabar (uses, character set)
- [ ] MSI/Plessey (checksum algorithms)
- [ ] ITF-14 (shipping containers)
- [ ] Code 11 (telecommunications)

### 4.5 Add Tutorials Section
**File**: New docs sections
**Task**: Create step-by-step tutorials:
- "Building a Ticket System with Aztec Codes"
- "Inventory Management with Code 128"
- "Creating WiFi QR Codes for Your Office"
- "Setting Up 2FA with OTP QR Codes"

---

## Priority 5: Technical Improvements

### 5.1 Light Mode Support
**File**: `CodeGlyphX.Website/wwwroot/css/app.css`
**Task**: Verify and fix light mode theme:
- Test all components in light mode
- Fix any contrast issues
- Ensure code blocks are readable

### 5.2 Mobile Responsiveness
**File**: Various CSS files
**Task**: Test and fix mobile layout:
- [ ] Header navigation (hamburger menu?)
- [ ] Playground forms on small screens
- [ ] Documentation sidebar on mobile
- [ ] API Reference sidebar on mobile

### 5.3 Performance Optimization
**Task**:
- Enable Brotli compression
- Optimize images (WebP format)
- Lazy load below-fold content

### 5.4 SEO Improvements
**File**: `CodeGlyphX.Website/wwwroot/index.html`
**Task**:
- Add meta description
- Add Open Graph tags
- Add structured data (JSON-LD)

---

## Priority 6: Future Enhancements

### 6.1 Blog/Changelog Section
**Task**: Add a blog or changelog page to announce:
- New features
- Version releases
- Usage tips

### 6.2 Examples Gallery
**Task**: Create a gallery page showing real-world examples:
- WiFi QR codes
- Business cards
- Product labels
- Event tickets
- Payment codes

### 6.3 API Playground
**Task**: Interactive API explorer where users can:
- Try different API methods
- See request/response
- Generate code snippets

### 6.4 Community Showcase
**Task**: Page featuring projects built with CodeGlyphX

---

## File Structure Reference

```
CodeGlyphX.Website/
├── wwwroot/
│   ├── css/
│   │   └── app.css              # Main styles
│   ├── index.html               # Entry point, add favicon here
│   └── [icons go here]
├── Layout/
│   └── MainLayout.razor         # Header, footer
├── Pages/
│   ├── Index.razor              # Home page
│   ├── Playground.razor         # Interactive playground
│   ├── Docs.razor               # Documentation (all sections)
│   └── ApiReference.razor       # API reference browser
└── Components/
    └── [shared components]
```

---

## Task Assignment Guidelines

When assigning tasks to other agents:

1. **For UI/Styling tasks**: Provide the specific file path and CSS class names
2. **For Code tasks**: Provide the file path, method names, and expected behavior
3. **For Content tasks**: Specify the section name and content requirements
4. **For Research tasks**: Specify what data to gather and where to put results

### Example Task Assignment

```
TASK: Add Data Matrix to Playground
FILE: CodeGlyphX.Website/Pages/Playground.razor
REQUIREMENTS:
1. Add "Data Matrix" option to SelectedCategory dropdown
2. Add form fields for Data Matrix content and size selection
3. Call DataMatrixCode.RenderPng() for generation
4. Update GetCodeExample() to show Data Matrix code sample
REFERENCE: Look at existing "QR" category implementation as template
```

---

## Notes

- All changes should maintain the dark theme aesthetic
- Code examples should be tested before adding
- New playground features should generate matching code examples
- Documentation should include both simple and advanced usage
