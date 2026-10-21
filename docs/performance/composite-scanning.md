# Composite image recognition

Status: active qualification, 21 October 2026. Owner: CodeGlyphX barcode decoding.

The quality target is to recover both messages of a complete Composite label, retain a readable
upper component when the carrier cannot be read, and return component corners in source-image
coordinates. A partial result must not preempt a complete result in a later sampling or projection
candidate. Existing explicit selection, cancellation and total scan deadlines remain binding.

The starting candidate is `4de04c26`, on Win-PC. Its independent fixtures cover all eleven added
retail/DataBar carriers and CC-A/B, including retail supplements. Right-angle image scans pass;
separate component geometry, cropped carriers and perspective recovery were absent.

Run the focused quality checks from the repository root with an explicit task-owned output path:

```powershell
dotnet test CodeGlyphX.Tests/CodeGlyphX.Tests.csproj -c Release -f net8.0 `
    --artifacts-path <scratch>/artifacts `
    --filter 'FullyQualifiedName~CompositeCarrierImageTests|FullyQualifiedName~CompositeCarrierConformanceTests' `
    --logger trx --results-directory <scratch>/results
```

Build subprocesses disable MSBuild node reuse and the CLI build server as required by the shared
development policy. Repeat the stable contract checks on .NET 10 and .NET Framework 4.7.2.

| Attempt | Observation | Disposition |
| --- | --- | --- |
| Affine rectification alone | Independent EAN-13 Composite at three pixels per module passes ±10-degree rotation; 3% perspective fails. | Retained as the existing inexpensive first path. |
| Projective fit from printed horizontal edges and long outer bars | Recovers the common horizontal lattice without assuming the upper and lower components have the same width. | Retained as a bounded Composite fallback. |
| Extra horizontal sampling phases at ±0.2 modules | Did not close the perspective failures. | Removed; do not repeat unchanged. |
| Stable row voting and limited row-address recovery | Removes raster edge flicker; the observed upper-component errors were in row-address marks. Direct module validation remains exact. | Retained; require at least two thirds of address marks to match exactly and at most two changed cells per remaining mark. |
| Affine followed by projective resampling | Adds quantization to the already transformed image. | Project the original mask once. |
| Central long-bar fitting and adjoining long-band consolidation | Avoids rotated rectangle ends and recognizes a single bar row split by pixel flicker. Thin component/separator rows remain distinct. | Retained with framing rejection tests. |
| Immediate partial-component return | Can hide a complete linked result available through a later candidate. | Retain partial candidates while complete-label paths run. |

The focused skew/perspective envelope uses an independently produced EAN-13 CC-A image, three
pixels per module, ±10-degree rotation, 3% perspective and their positive-angle combination.
The tests allow a five-second caller deadline to verify recognition independently of host load.
This is a quality check, not a speed comparison or evidence for the default 500 ms deadline.
The wider suite also covers source regions, image downscaling, all right-angle rotations of 42
independent carrier images, cropped/damaged carriers, unrelated retail bars and bounded CC-A/B
framing damage.

Stop the current correction loop once these contracts and the affected scanner regressions pass.
Do not rerun removed ideas without a material change to their inputs. The next qualification work
is the remaining carrier/component perspective envelope and matched end-to-end default-budget
measurements on the combined stable candidate; physical print examples provide additional inputs.
Do not claim arbitrary photographs, postal acceptance or certified print grading from generated
transformation fixtures.

## Physical labels with readable text

The print-layout packet adds retail guard descent, physical row ratios and readable digits to the
generated-input envelope. It exposed two distinct sampling boundaries: an Expanded Stacked
carrier whose visible extent removes only its leading light module, and retail lettering that
splits the lower carrier into many short bands. These inputs differ from the earlier perspective
fixtures and justify the additional bounded recovery below.

| Attempt | Observation | Disposition |
| --- | --- | --- |
| Existing light-margin restoration | Expanded Stacked physical rows matched the encoded grid, but a leading-only crop could not be restored because every candidate also added a trailing module. | Add the defined zero-trailing alternative; retain complete framing/linkage validation. |
| Whole printed retail band collapse | Guard descent adds a band, and readable digits/supplement text add short varying bands. A checked upper component remains available. | Normalize exact guard-only tails; retain their physical geometry in image metadata. |
| Checked component anchor with an extrapolated projection width | Recovers the main retail carrier, but fractional whole-region bounds can accumulate enough horizontal error to lose a wider five-digit supplement. | Measure the upper component's own printed horizontal edges before extrapolating its lattice. Revalidate the sampled component and its payload. |
| Main-carrier-first result | A valid main symbol can hide a readable supplement. A five-digit add-on may also have a valid two-digit prefix. | Try supplements first and require their observed trailing clear space. |
| Anchored middle-bar observations | Three bounded middle-row positions, each corroborated four modules below, recover retail bars without treating glyph strokes as data. The three linkage separators and complete carrier decoder remain mandatory. | Retained for checked retail Composite components; bounded to sixteen distinct anchors, with the caller deadline observed. |

The focused inputs cover all twelve linked carriers, both supplement lengths, captions/readable
digits, MaxiCode/Swiss QR/Ultracode output and postal geometry. The print tests compare reported
retail carrier extents with the actual printed bars within two source pixels. Corrupted separators
and unrelated components remain negative controls. Public sampled-module validation remains
exact; none of these image normalizations changes that contract.

```powershell
dotnet test CodeGlyphX.Tests/CodeGlyphX.Tests.csproj -c Release -f net8.0 `
    --filter 'FullyQualifiedName~BarcodePrint|FullyQualifiedName~BarcodeFontContract|FullyQualifiedName~Composite|FullyQualifiedName~SwissQr|FullyQualifiedName~RenderingContract'
```

Repeat on .NET 10 and .NET Framework 4.7.2. This is recognition and output proof with a five-second
test deadline, not a benchmark. Stop this print-input correction loop when these contracts pass;
the default-budget, wider perspective and held-out physical-capture gates remain separate work.
