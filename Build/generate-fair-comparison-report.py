"""Generate a current report from qualified PowerForge packets and every corpus outcome."""
import argparse
import hashlib
import json
from pathlib import Path
from comparison_website_data import write_website_data
from comparison_qr_morphology import project_qr_morphology
from comparison_databar_tilted import project_databar_tilted
from comparison_retail_fine import project_retail_fine
from comparison_scanner_boundaries import project_scanner_boundaries, append_scanner_boundary_section
from comparison_packet_identity import validate_current_packet, validate_qualified_whole_image_packet
from comparison_decode_allocations import project_decode_allocations, append_decode_allocation_section
from comparison_decode_deadlines import project_decode_deadlines, append_decode_deadline_section
from comparison_qr_finder_groups import project_qr_finder_groups, append_qr_finder_group_section
from comparison_dpm_gray8 import project_dpm_gray8, append_dpm_gray8_section
from comparison_dpm_window_means import project_dpm_window_means, append_dpm_window_means_section
from comparison_retained_contexts import project_retained_contexts, append_retained_context_section
from comparison_pdf417_segment_cache import project_pdf417_segment_cache
from comparison_aztec_probe_cache import project_aztec_probe_cache


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--evidence-root", type=Path, required=True)
    parser.add_argument("--output-root", type=Path, required=True)
    parser.add_argument("--website-root", type=Path, help="Also refresh the website view from this evidence")
    args = parser.parse_args()
    root, output = args.evidence_root, args.output_root
    output.mkdir(parents=True, exist_ok=True)
    baseline = read(root / "qualified-baseline-corpus.json")
    current = read(root / "candidate-corpus-final.json")
    qualification = read(root / "qualification.json")
    engines = ["CodeGlyphX", "ZXing.Net", "ZXing-C++"]
    runs = {}
    for kind in ("generation", "decoding"):
        for domain in ("ccd0", "ccd1"):
            path = root / f"{kind}-final-{domain}-result.json"
            packet = read(path)
            validate_current_packet(packet, qualification, kind, domain)
            runs[kind, domain] = packet
    # Keep every sample, failure, unsupported format and returned payload available for audit.
    names = ["qualified-baseline-corpus.json", "candidate-corpus-final.json",
             "negative-baseline-corpus.json", "negative-candidate-corpus.json", "negative-manifest.json",
             "qualification.json"]
    names += [f"{kind}-final-{domain}-result.json" for kind, domain in runs]
    names += [name for name in ("previous-candidate-corpus.json", "previous-negative-corpus.json", "previous-qualification.json") if (root / name).exists()]
    names += sorted(path.name for path in root.glob("negative-rotation-*.json"))
    names += sorted(path.name for path in root.glob("historical-qr-morphology-*.json"))
    names += sorted(path.name for path in root.glob("historical-databar-tilt-*.json"))
    names += sorted(path.name for path in root.glob("historical-retail-fine-*.json"))
    names += sorted(path.name for path in root.glob("historical-scanner-boundary-*.json"))
    names += sorted(path.name for path in root.glob("historical-decode-allocation-*.json"))
    names += sorted(path.name for path in root.glob("historical-deadline-polling-*.json"))
    names += sorted(path.name for path in root.glob("historical-qr-finder-groups-*.json"))
    names += sorted(path.name for path in root.glob("historical-dpm-gray8-*.json"))
    names += sorted(path.name for path in root.glob("historical-dpm-window-means-*.json"))
    names += sorted(path.name for path in root.glob("historical-retained-contexts-*.json"))
    names += sorted(path.name for path in root.glob("retained-contexts-*.json"))
    names += sorted(path.name for path in root.glob("pdf417-segment-cache-*.json"))
    names += sorted(path.name for path in root.glob("aztec-probe-cache-*.json"))
    names += sorted(path.name for path in root.glob("historical-aztec-probe-cache-*.json"))
    if qualification.get("DpmWindowMeansProof") or qualification.get("HistoricalDpmWindowMeansProof"):
        names += sorted(path.name for path in root.glob("dpm-window-means-*.json"))
    if qualification.get("DpmGray8Proof") or qualification.get("HistoricalDpmGray8Proof"):
        names += sorted(path.name for path in root.glob("dpm-gray8-*.json"))
    if qualification.get("QrFinderGroupProof") or qualification.get("HistoricalQrFinderGroupProof"):
        names += sorted(path.name for path in root.glob("qr-finder-groups-*.json"))
    if qualification.get("DecodeDeadlineProof") or qualification.get("HistoricalDecodeDeadlineProof"):
        names += sorted(path.name for path in root.glob("decode-deadline-*.json"))
    if qualification.get("DecodeAllocationProof") or qualification.get("HistoricalDecodeAllocationProof"):
        names += sorted(path.name for path in root.glob("decode-allocation-*.json"))
    if qualification.get("ScannerBoundaryProof") or qualification.get("HistoricalScannerBoundaryProof"):
        names += sorted(path.name for path in root.glob("scanner-*.json"))
        names += ["frozen-input-inventory.json", "scanline-closure-review.json"]
    if qualification.get("RetailFineContrastProof") or qualification.get("HistoricalRetailFineContrastProof"):
        names += sorted(path.name for path in root.glob("retail-fine-*.json"))
    if qualification.get("IndustrialTileBoundaryProof") or qualification.get("HistoricalIndustrialTileBoundaryProof"):
        names += sorted(path.name for path in root.glob("industrial-tile-*.json"))
    if qualification.get("DataBarTiltedSeparatorProof") or qualification.get("HistoricalDataBarTiltedSeparatorProof"):
        names += sorted(path.name for path in root.glob("databar-tilted-*.json"))
    if qualification.get("QrMorphologyCostProof") or qualification.get("HistoricalQrMorphologyCostProof"):
        names += sorted(path.name for path in root.glob("qr-morphology-*.json"))
        names += sorted(path.name for path in root.glob("qr-negative-cost-*.json"))
    names += sorted(path.name for path in root.glob("pdf417-supplemental-*.json"))
    if qualification.get("Pdf417FramingProof") or qualification.get("HistoricalPdf417FramingProof"):
        names += sorted(path.name for path in root.glob("pdf417-framing-*.json"))
    if qualification.get("Pdf417CostProof") or qualification.get("HistoricalPdf417CostProof"):
        names += sorted(path.name for path in root.glob("pdf417-cost-*.json"))
    if qualification.get("QrAlignmentProof") or qualification.get("HistoricalQrAlignmentProof"):
        names.append("qr-alignment-equivalence.json")
    if qualification.get("Pdf417CacheCostProof") or qualification.get("HistoricalPdf417CacheCostProof"):
        names += ["pdf417-cache-cost-proof.json", "pdf417-cache-controls-manifest.json", "pdf417-cache-checks-previous.json", "pdf417-cache-checks-current.json", "pdf417-cache-pressure-previous.json", "pdf417-cache-pressure-current.json"]
        names += sorted(p.name for p in root.glob("pdf417-cache-cost-*-result.json"))
    if qualification.get("CodabarRunRecoveryProof") or qualification.get("HistoricalCodabarRunRecoveryProof"):
        names += sorted(p.name for p in root.glob("codabar-*.json"))
    if qualification.get("QrExactGridRecoveryProof") or qualification.get("HistoricalQrExactGridRecoveryProof"):
        names += ["qr-exact-grid-recovery-proof.json"]
    if qualification.get("Pdf417NegativeCompletionProof") or qualification.get("HistoricalPdf417NegativeCompletionProof"):
        names += ["pdf417-negative-completion-proof.json", "pdf417-codeword-cache-equivalence.json"]
        names += sorted(p.name for p in root.glob("pdf-negative-cost-*-result.json"))
    names += sorted(p.name for p in root.glob("historical-pdf417-*.json"))
    names += sorted(p.name for p in root.glob("historical-datamatrix-*.json"))
    if qualification.get("Code39RunRecoveryProof") or qualification.get("HistoricalCode39RunRecoveryProof"):
        names += sorted(p.name for p in root.glob("code39-*.json"))
    names += sorted(p.name for p in root.glob("historical-code39-*.json"))
    names += sorted(p.name for p in root.glob("historical-itf-*.json"))
    if qualification.get("ItfRunRecoveryProof") or qualification.get("HistoricalItfRunRecoveryProof"):
        names += sorted(p.name for p in root.glob("itf-*.json"))
    if qualification.get("DataMatrixSpatialSearchProof") or qualification.get("HistoricalDataMatrixSpatialSearchProof"):
        names += sorted(p.name for p in root.glob("datamatrix-spatial-*.json"))
        names += sorted(p.name for p in root.glob("datamatrix-components-*.json"))
        names += sorted(p.name for p in root.glob("datamatrix-texture-*.json"))
        names += sorted(p.name for p in root.glob("datamatrix-selected-*.json"))
    if qualification.get("NegativeSearchCostProof") or qualification.get("HistoricalNegativeSearchCostProof"):
        names.append("negative-search-cost-proof.json")
    if qualification.get("MaxiCodeImageProof"):
        names.append("maxicode-image-proof.json")
    if qualification.get("DataMatrixDeadlineProof") or qualification.get("HistoricalDataMatrixDeadlineProof"):
        names.append("datamatrix-box-equivalence.json")
        names += sorted(path.name for path in root.glob("datamatrix-box-cost-*-result.json"))
    if qualification.get("RetailRecoveryProof") or qualification.get("HistoricalRetailRecoveryProof"):
        names += sorted(path.name for path in root.glob("retail-*.json"))
    names += sorted(path.name for path in root.glob("historical-retail-*.json"))
    if qualification.get("DataBar14RunRecoveryProof") or qualification.get("HistoricalDataBar14RunRecoveryProof"):
        names += sorted(path.name for path in root.glob("databar14-*.json"))
        names += sorted(path.name for path in root.glob("historical-databar14-*.json"))
    if qualification.get("PairedDecodeCost"):
        names += sorted(path.name for path in root.glob("paired-decode-cost-*.json"))
    if qualification.get("QrVariation"):
        names += sorted(path.name for path in root.glob("qr-variation-*.json"))
    evidence = output / "evidence"
    evidence.mkdir(exist_ok=True)
    hashes = {}
    for name in names:
        # Normalize JSON formatting, leaving escaped payload line endings untouched.
        raw = (root / name).read_bytes().replace(b"\r\n", b"\n")
        (evidence / name).write_bytes(raw)
        hashes[name] = hashlib.sha256(raw).hexdigest()
    (evidence / "sha256.json").write_bytes((json.dumps(hashes, indent=2) + "\n").encode("utf-8"))
    unsupported = sum(x["Engine"] == "CodeGlyphX" and x["Status"] == "Unsupported" for x in current)
    scanner_coverage = (f"{unsupported} images use formats without a CodeGlyphX image scanner."
                        if unsupported else "Every positive format has a CodeGlyphX image scanner.")
    lines = ["# Current barcode comparisons", "", "Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.", "",
             "See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.", "",
             qualification.get("CurrentHostLoadNote", ""), "", "## Recognition", "", f"The 279-image corpus includes twelve negative images. {scanner_coverage} Exact payload sets are checked after the documented physical representation rules.", "",
             "| Engine | Original source: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |",
             "| --- | ---: | ---: | ---: | ---: | ---: | ---: |"]
    for engine in engines:
        old = [x for x in baseline if x["Engine"] == engine]
        new = [x for x in current if x["Engine"] == engine]
        if len(old) != 279 or len(new) != 279 or {x["Id"] for x in old} != {x["Id"] for x in new}:
            raise ValueError("Baseline/candidate corpus identities differ")
        lines.append(f'| {engine} | {sum(bool(x["Expected"]) and x["Status"] == "Correct" for x in old)}/267 | {sum(bool(x["Expected"]) and x["Status"] == "Correct" for x in new)}/267 | {sum(x["Status"] == "Unsupported" for x in new)} | {sum(bool(x["Unexpected"]) for x in new)} | {sum(not x["Expected"] and bool(x["Actual"]) for x in new)} | {sum(x["Status"] == "Inconclusive" for x in new)} |')
    lines += ["", "Unexpected output on a positive image may include an oracle limitation or an encoding-policy difference. Raw text is preserved; these counts must not be presented as a measured false-positive rate.", "",
              "| Format | Images | CodeGlyphX original source | CodeGlyphX current | ZXing.Net | ZXing-C++ |", "| --- | ---: | ---: | ---: | ---: | ---: |"]
    for format in sorted({x["Format"] for x in current}):
        counts = [sum(x["Format"] == format and x["Status"] == "Correct" and x["Engine"] == engine for x in current) for engine in engines]
        before = sum(x["Format"] == format and x["Status"] == "Correct" and x["Engine"] == "CodeGlyphX" for x in baseline)
        n = sum(x["Format"] == format and x["Engine"] == "CodeGlyphX" for x in current)
        lines.append(f'| {format} | {n} | {before} | {counts[0]} | {counts[1]} | {counts[2]} |')
    old_by_id = {x["Id"]: x for x in baseline if x["Engine"] == "CodeGlyphX"}
    regressions = [x["Id"] for x in current if x["Engine"] == "CodeGlyphX" and x["Status"] != "Correct" and old_by_id[x["Id"]]["Status"] == "Correct"]
    lines += ["", "Previously correct images now missed: " + (", ".join(regressions) if regressions else "none in this run.")]
    if (root / "previous-candidate-corpus.json").exists():
        previous = read(root / "previous-candidate-corpus.json")
        old_correct = sum(x["Engine"] == "CodeGlyphX" and bool(x["Expected"]) and x["Status"] == "Correct" for x in previous)
        new_correct = sum(x["Engine"] == "CodeGlyphX" and bool(x["Expected"]) and x["Status"] == "Correct" for x in current)
        previous_by_id = {x["Id"]: x for x in previous if x["Engine"] == "CodeGlyphX"}
        lost = [x["Id"] for x in current if x["Engine"] == "CodeGlyphX" and x["Expected"] and x["Status"] != "Correct" and previous_by_id[x["Id"]]["Status"] == "Correct"]
        incomplete = [x["Id"] for x in current if x["Engine"] == "CodeGlyphX" and not x["Expected"] and x["Status"] != "Correct" and previous_by_id[x["Id"]]["Status"] == "Correct"]
        lines += ["", f"This decoding follow-up records {new_correct}/267 correct positive images, compared with {old_correct}/267 in the [previous pass](evidence/previous-candidate-corpus.json). Previously correct positive images lost in this follow-up: " + (", ".join(lost) if lost else "none in this run."), "",
                  "Previously completed main-corpus negatives now inconclusive: " + (", ".join(incomplete) if incomplete else "none in this run.")]
    if qualification.get("SupplementalPdf417"):
        supplemental = qualification["SupplementalPdf417"]
        lines += ["", "## Supplemental PDF417 images", "", supplemental["Selection"], "", supplemental["Limit"], "",
                  "| Engine | Previous pass | Current pass | Unexpected-output images |",
                  "| --- | ---: | ---: | ---: |"]
        for engine in engines:
            result = supplemental["Engines"][engine]
            lines.append(f'| {engine} | {result["PreviousCorrect"]}/{supplemental["Images"]} | {result["Correct"]}/{supplemental["Images"]} | {result["UnexpectedImages"]} |')
        lines += ["", "CodeGlyphX gains: " + (", ".join(supplemental["Gained"]) or "none") + ". Lost previous successes: " + (", ".join(supplemental["Lost"]) or "none") + ".",
                  "", "The [manifest](evidence/pdf417-supplemental-manifest.json), [current outcomes](evidence/pdf417-supplemental-current.json), [previous outcomes](evidence/pdf417-supplemental-previous.json) and [pinned image/oracle provenance](evidence/pdf417-supplemental-provenance.json) retain the complete set."]
        framing = qualification.get("Pdf417FramingProof")
        if framing:
            if framing["Source"] != qualification["MeasurementSource"]:
                raise ValueError("PDF417 framing proof differs from the measured source")
            lines += ["", f'The four damaged-framing regression images change from {framing["PreviousIndependentCorrect"]}/4 to {framing["CurrentIndependentCorrect"]}/4 exact payloads. The previous binary also fails {framing["PreviousBinaryContractCheck"]["failed"]} of {framing["PreviousBinaryContractCheck"]["total"]} selected contract checks; the candidate passes all {framing["CurrentFramingContracts"]["passed"]} recovery checks and {framing["OwnerContracts"]["passed"]} focused owner/budget/scanner checks.',
                      "", "A separate PDF417-hinted check completes all 31 distinct negative images with no unexpected output for each reader. This diagnostic does not replace all-format negative completion. Its [unchanged inputs](evidence/pdf417-framing-negative-manifest.json), [outcomes](evidence/pdf417-framing-negative-outcomes.json), [four regression inputs](evidence/pdf417-framing-regression-manifest.json), [previous results](evidence/pdf417-framing-previous-misses.json) and [current results](evidence/pdf417-framing-current-misses.json) are retained."]
        if supplemental.get("TimingSensitiveGains"):
            lines += ["", "Timing-sensitive changes: " + "; ".join(supplemental["TimingSensitiveGains"]) + ". These outcomes do not establish a decoder accuracy gain."]
    alignment = qualification.get("QrAlignmentProof")
    if alignment:
        decisions = alignment["DecisionEquivalence"]
        if (alignment["Source"] != qualification["MeasurementSource"]
                or alignment["CoreSha256"] != qualification["CandidateCoreSha256"]
                or decisions["Mismatches"] != 0):
            raise ValueError("QR alignment proof differs from the measured candidate")
        lines += ["", "## QR alignment scoring", "",
                  "On .NET 8 and later, alignment scoring stops after four mismatches in its 25-position pattern: that candidate cannot reach the existing 22/25 acceptance threshold. Accepted scores, candidate order, selected centers and first-best ties remain unchanged. Recognition allowances, format hints, payload oracles and scoring are unchanged.", "",
                  f'The actual previous and current private methods agree on {decisions["ScoreCases"]:,} scoring cases, including {decisions["AcceptedScores"]:,} accepted scores, and {decisions["FinderCases"]:,} complete finder searches, including {decisions["AcceptedFinders"]:,} successful searches. There are zero decision or selected-center mismatches. Boundary patterns, inversion, global and adaptive thresholds, fractional coordinates, transformed and edge patterns, noise and first-perfect ties are included.', "",
                  alignment["Limits"], "",
                  "The [decision diagnostic](evidence/qr-alignment-equivalence.json) records the exact baseline and candidate core hashes. Current whole-image timings and completion counts are reported in the paired decoding comparison below."]
    maxi = qualification.get("MaxiCodeImageProof")
    if maxi:
        if maxi["Source"] != qualification["MeasurementSource"] or maxi["CoreSha256"] != qualification["CandidateCoreSha256"]:
            raise ValueError("MaxiCode proof differs from the measured candidate")
        lines += ["", "## MaxiCode image recognition", "",
                  (f'Both previous and current binaries decode the same six MaxiCode images correctly ({maxi["Correct"]}/{maxi["Images"]}).' if maxi["Correct"] == maxi["PreviousCorrect"] else f'The same six MaxiCode images decode correctly ({maxi["Correct"]}/{maxi["Images"]}), versus {maxi["PreviousCorrect"]}/{maxi["Images"]} previously.') + ' Image bytes, format hints and payload oracles are unchanged.', "",
                  "The scanner verifies circular finder bands, samples the staggered module grid and uses the existing primary and secondary error correction. It accepts planar rotation and inverted polarity, and can recover small damage to fixed orientation modules. The existing result carries secondary text and bytes, typed carrier metadata and the searched source region.", "",
                  maxi["TextContract"], "", maxi["Evidence"], "", maxi["Limits"], "",
                  "The [MaxiCode proof](evidence/maxicode-image-proof.json) records source hashes and search bounds. The capability catalog advertises one-symbol image scanning without geometry. Independent fixture tests do not establish coverage of every numeric MaxiCode mode; fixture filenames are not encoding-mode evidence."]
    historical_cache = not qualification.get("Pdf417CacheCostProof")
    cache = qualification.get("Pdf417CacheCostProof") or qualification.get("HistoricalPdf417CacheCostProof")
    if cache:
        decisions = cache["DecisionEquivalence"]
        if (decisions["Mismatches"] != 0 or (not historical_cache and
                (cache["Source"] != qualification["MeasurementSource"]
                 or cache["CoreSha256"] != qualification["CandidateCoreSha256"]))):
            raise ValueError("PDF417 cache proof differs from the measured candidate")
        old_cache = cache["InstrumentedBaseline"]["LargeCaches"][0]
        new_cache = cache["InstrumentedCandidate"]["LargeCaches"][0]
        lines += ["", "## PDF417 scanline reuse", "",
                  "PDF417 stores short scanline runs in 16-bit arrays and retains 32-bit widths for longer lines. More tilted lines fit within the same 8 MiB estimated cache allowance. Search order, acceptance rules and recognition deadlines are unchanged.", "",
                  f'All {decisions["Cases"]:,} cache reads match independently sampled pixels, including reversed, inverted and tilted lines, width boundaries, entry saturation and byte pressure. The checks also cover pre-cancelled and expired calls.', "",
                  f'On the existing difficult negative image, the instrumented PDF417 search retains {new_cache["Entries"]:,} lines instead of {old_cache["Entries"]:,}. Capacity rejections fall from {old_cache["EntryRejections"]:,} to {new_cache["EntryRejections"]:,}; sampled pixels fall from {old_cache["SampledPixels"]:,} to {new_cache["SampledPixels"]:,}. These diagnostic counts use a longer allowance to complete all work; their timings are unranked.', "",
                  "The paired control batch contains the same two negative images with PDF417-only and all-format hints: four requests, each with the original 1,500 ms allowance. Correctness and completion accompany cost.", "",
                  "| Domain | Previous median ms | Current median ms | Current/previous | Previous/current mean managed bytes | Previous/current mean correct requests | Previous/current mean inconclusive requests |",
                  "|---|---:|---:|---:|---:|---:|---:|"]
        if historical_cache:
            lines.insert(len(lines) - 2, f'Historical diagnostic and paired-control measurements from source `{cache["Source"]}`. These counts and costs were not rerun for the current candidate; the current corpus and whole-image costs below are rerun separately.')
            lines.insert(len(lines) - 2, "")
        for row in cache["PairedControls"]:
            old, now = row["PreviousMetrics"], row["CurrentMetrics"]
            lines.append(f'| {row["Domain"].upper()} | {row["PreviousMedianMs"]:.3f} | {row["CurrentMedianMs"]:.3f} | {row["LatencyRatio"]:.3f} | {row["PreviousMeanAllocatedBytes"]:,.0f} / {row["CurrentMeanAllocatedBytes"]:,.0f} | {old["CorrectImages"]:.3f} / {now["CorrectImages"]:.3f} | {old["InconclusiveImages"]:.3f} / {now["InconclusiveImages"]:.3f} |')
        lines += ["", cache["Limits"], "", "[Cache checks, counts and paired costs](evidence/pdf417-cache-cost-proof.json). Whole-image comparison results remain the practical measure of scanner cost and completion."]
    recovery = qualification.get("ItfRunRecoveryProof") or qualification.get("HistoricalItfRunRecoveryProof")
    historical_itf = bool(recovery) and not qualification.get("ItfRunRecoveryProof")
    if recovery:
        if (not historical_itf and (recovery["Source"] != qualification["MeasurementSource"] or recovery["CoreSha256"] != qualification["CandidateCoreSha256"])) or recovery["WidthModelEquivalence"]["Mismatches"]:
            raise ValueError("ITF proof differs from the measured candidate")
        notice = [f'These diagnostics and paired costs belong to source `{recovery["Source"]}`. They describe the earlier ITF change; current corpus, retail and whole-image measurements are separate.', ""] if historical_itf else []
        lines += ["", "## Historical ITF complete-symbol recovery" if historical_itf else "## ITF complete-symbol recovery", "", *notice, recovery["Changes"], "", recovery["Scope"], "",
                  f'The actual previous and current Codabar validators agree on {recovery["WidthModelEquivalence"]["Cases"]:,} decisions and exact fit outputs across printing bias, rounding, framing, direction and cancellation. Two independent ITF images and direction, polarity, framing and ITF-14 checksum regressions pass through public entrypoints.', "",
                  "| Reader | Previous → current positives / 12 | Previous → current completed negative rejections / 31 | Current unexpected negative images |",
                  "|---|---:|---:|---:|"]
        for engine, values in recovery["Engines"].items():
            a,b=values["positive"],values["negative"]
            lines.append(f'| {engine} | {a["previous"]["Correct"]} → {a["current"]["Correct"]} | {b["previous"]["Correct"]} → {b["current"]["Correct"]} | {b["current"]["Unexpected"]} |')
        lines += ["", "Paired cost for the same twelve ITF images. Both binaries use preloaded RGBA32, identical hints and 1500 ms allowances; 3 warmups and 12 rotated samples per engine and domain. Input loading and outcome evaluation are excluded.", "",
                  "| Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current mean correct / 12 |",
                  "|---|---:|---:|---:|---:|"]
        for row in recovery["Cost"]:
            lines.append(f'| {row["Domain"].upper()} | {row["PreviousMedianMs"]:.3f} → {row["CurrentMedianMs"]:.3f} | {row["LatencyRatio"]:.3f} | {row["PreviousAllocatedBytes"]/1048576:.2f} → {row["CurrentAllocatedBytes"]/1048576:.2f} | {row["PreviousCorrect"]:.3f} → {row["CurrentCorrect"]:.3f} |')
        lines += ["", recovery["Limits"], "", "[ITF proof, outcomes and every timing sample](evidence/itf-run-recovery-proof.json) · [Codabar width-model compatibility](evidence/itf-codabar-model-equivalence.json)."]
    recovery = qualification.get("Code39RunRecoveryProof") or qualification.get("HistoricalCode39RunRecoveryProof")
    historical_code39 = bool(recovery) and not qualification.get("Code39RunRecoveryProof")
    if recovery:
        if (not historical_code39 and (recovery["Source"] != qualification["MeasurementSource"] or recovery["CoreSha256"] != qualification["CandidateCoreSha256"])) or recovery["NormalizerEquivalence"]["Mismatches"]:
            raise ValueError("Code 39 proof differs from the measured candidate")
        notice = [f'These diagnostics and paired costs belong to source `{recovery["Source"]}`. They describe the earlier Code 39 change; current corpus and whole-image measurements are separate.', ""] if historical_code39 else []
        lines += ["", "## Historical Code 39 clipped and reversed scanlines" if historical_code39 else "## Code 39 clipped and reversed scanlines", "", *notice, recovery["Changes"], "", recovery["Scope"], "",
                  f'The actual previous and current width normalizers agree on {recovery["NormalizerEquivalence"]["Cases"]:,} decisions, including reversed run lists. Three independent images and direction, polarity, framing and short-checksum regressions pass through public entrypoints.', "",
                  "| Reader | Previous → current positives / 12 | Previous → current completed negative rejections / 31 | Current unexpected negative images |",
                  "|---|---:|---:|---:|"]
        for engine, values in recovery["Engines"].items():
            a,b=values["positive"],values["negative"]
            lines.append(f'| {engine} | {a["previous"]["Correct"]} → {a["current"]["Correct"]} | {b["previous"]["Correct"]} → {b["current"]["Correct"]} | {b["current"]["Unexpected"]} |')
        lines += ["", "Paired cost for the same twelve Code 39 images. Both binaries use preloaded RGBA32, identical hints and 1500 ms allowances; 3 warmups and 12 rotated samples per engine and domain. Input loading and outcome evaluation are excluded.", "",
                  "| Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current mean correct / 12 |",
                  "|---|---:|---:|---:|---:|"]
        for row in recovery["Cost"]:
            lines.append(f'| {row["Domain"].upper()} | {row["PreviousMedianMs"]:.3f} → {row["CurrentMedianMs"]:.3f} | {row["LatencyRatio"]:.3f} | {row["PreviousAllocatedBytes"]/1048576:.2f} → {row["CurrentAllocatedBytes"]/1048576:.2f} | {row["PreviousCorrect"]:.3f} → {row["CurrentCorrect"]:.3f} |')
        lines += ["", recovery["Limits"], "", "[Code 39 proof, outcomes and every timing sample](evidence/code39-run-recovery-proof.json) · [Normalizer equivalence](evidence/code39-normalizer-equivalence.json)."]
    spatial = qualification.get("DataMatrixSpatialSearchProof") or qualification.get("HistoricalDataMatrixSpatialSearchProof")
    historical_spatial = spatial is not None and not qualification.get("DataMatrixSpatialSearchProof")
    if spatial:
        if (not historical_spatial and (spatial["Source"] != qualification["MeasurementSource"]
                or spatial["CoreSha256"] != qualification["CandidateCoreSha256"])) or spatial["BoundsEquivalence"]["Mismatches"] or spatial["ComponentEquivalence"]["Mismatches"]:
            raise ValueError("Data Matrix spatial proof differs from the measured candidate")
        notice = [f'These spatial-search checks and costs belong to source `{spatial["Source"]}`. They describe the earlier Data Matrix change. Current corpus and whole-image costs are measured separately on the current binary.', ""] if historical_spatial else []
        lines += ["", "## Historical Data Matrix image-search cost" if historical_spatial else "## Data Matrix image-search cost", "", *notice, spatial["Changes"], "",
                  f'The production bounding-box method matches a full-raster reference in {spatial["BoundsEquivalence"]["Cases"]:,} cases. Another {spatial["BoundsEquivalence"]["CanceledCases"]:,} cases reject cancelled tokens, and live and expired ambient deadlines are checked. Ordered component area and corner coordinates match the retained binary in {spatial["ComponentEquivalence"]["Cases"]:,} cases, including equal-area ties.', "",
                  spatial["CostScope"], "",
                  "| Domain | Previous → current median ms | Current / previous | Previous → current managed MiB | Previous → current mean completed / 1 |",
                  "|---|---:|---:|---:|---:|"]
        for row in spatial["Cost"]:
            lines.append(f'| {row["Domain"].upper()} | {row["BaselineMedianMs"]:.3f} → {row["CurrentMedianMs"]:.3f} | {row["LatencyRatio"]:.3f} | {row["BaselineAllocatedBytes"]/1048576:.2f} → {row["CurrentAllocatedBytes"]/1048576:.2f} | {row["PreviousCorrectRequests"]:.3f} → {row["CurrentCorrectRequests"]:.3f} |')
        lines += ["", spatial["SelectedNegativeScope"], "",
                  "| Domain | Previous → current median ms | Current / previous | Previous → current managed MiB | Previous → current mean completed / 3 |",
                  "|---|---:|---:|---:|---:|"]
        for row in spatial["SelectedNegativeCost"]:
            lines.append(f'| {row["Domain"].upper()} | {row["BaselineMedianMs"]:.3f} → {row["CurrentMedianMs"]:.3f} | {row["LatencyRatio"]:.3f} | {row["BaselineAllocatedBytes"]/1048576:.2f} → {row["CurrentAllocatedBytes"]/1048576:.2f} | {row["PreviousCorrectRequests"]:.3f} → {row["CurrentCorrectRequests"]:.3f} |')
        lines += ["", spatial["Limits"], "", "[Spatial-search proof and raw cost references](evidence/datamatrix-spatial-search-proof.json) · [Bounds equivalence](evidence/datamatrix-spatial-bounds-equivalence.json) · [Ordered component checks](evidence/datamatrix-components-current.json)."]
    completion = qualification.get("Pdf417NegativeCompletionProof") or qualification.get("HistoricalPdf417NegativeCompletionProof")
    historical_completion = completion is not None and not qualification.get("Pdf417NegativeCompletionProof")
    if completion:
        if (not historical_completion and (completion["Source"] != qualification["MeasurementSource"]
                or completion["CoreSha256"] != qualification["CandidateCoreSha256"])) or completion["DecisionEquivalence"]["mismatches"] != 0:
            raise ValueError("PDF417 negative-search proof differs from the measured candidate")
        notice = [f'These completion and cost measurements belong to source `{completion["Source"]}`. They describe the earlier PDF417 change; current corpus, alternating negatives and whole-image costs are measured separately on the current binary.', ""] if historical_completion else []
        lines += ["", "## Historical PDF417 negative-search completion" if historical_completion else "## PDF417 negative-search completion", "", *notice, completion["Changes"], "",
                  f'The cached and uncached fitters agree on {completion["DecisionEquivalence"]["checks"]:,} decisions, including failed fits, wide runs and cache capacity pressure. Four module-entrypoint controls preserve offset symbols and macro metadata in both row orders.', "",
                  completion["CompletionNotes"], "", completion["CostScope"], "",
                  "| Domain | Previous median ms | Current median ms | Current / previous | Previous managed bytes | Current managed bytes | Previous / current mean completed images |",
                  "|---|---:|---:|---:|---:|---:|---:|"]
        for row in completion["Cost"]:
            lines.append(f'| {row["Domain"].upper()} | {row["BaselineMedianMs"]:.3f} | {row["CurrentMedianMs"]:.3f} | {row["LatencyRatio"]:.3f} | {row["BaselineAllocatedBytes"]:,.0f} | {row["CurrentAllocatedBytes"]:,.0f} | {row["PreviousCorrectRequests"]:.3f} / {row["CurrentCorrectRequests"]:.3f} |')
        lines += ["", completion["Limits"], "", "[Completion proof](evidence/pdf417-negative-completion-proof.json) · [Fitter equivalence](evidence/pdf417-codeword-cache-equivalence.json)."]
    exact = qualification.get("QrExactGridRecoveryProof") or qualification.get("HistoricalQrExactGridRecoveryProof")
    historical_exact = exact is not None and not qualification.get("QrExactGridRecoveryProof")
    if exact:
        if not historical_exact and (exact["Source"] != qualification["MeasurementSource"] or exact["CoreSha256"] != qualification["CandidateCoreSha256"]):
            raise ValueError("QR exact-grid proof differs from the measured candidate")
        notice = [f'The discovery and focused checks below belong to source `{exact["Source"]}`. The current corpus recheck preserves {qualification["QrExactGridRecheck"]["Correct"]}/2 correct payloads on the current core.', ""] if historical_exact else []
        lines += ["", "## Historical exact QR raster recovery" if historical_exact else "## Exact QR raster recovery", "", *notice,
                  f'The two dense version-40 images decode correctly ({exact["Correct"]}/{len(exact["Images"])}), versus {exact["PreviousCorrect"]}/{len(exact["Images"])} previously. Both have one pixel per module. The single-image API and unrestricted unified scanner preserve their exact payloads.', "",
                  exact["Changes"][0], "",
                  f'Four independent image-entrypoint regression cases fail before the fix. {exact["FocusedCases"]} focused cases cover integer and anisotropic spacing, padding, rotations, inversion, cancellation, inherited deadlines, damaged codewords and acceptance filtering. Full modern suites rerun on the measured core.', "",
                  exact["LegacyScope"], "", exact["Limits"], "", "[QR raster proof and provenance](evidence/qr-exact-grid-recovery-proof.json)."]
    codabar = qualification.get("CodabarRunRecoveryProof") or qualification.get("HistoricalCodabarRunRecoveryProof")
    historical_codabar = codabar is not None and not qualification.get("CodabarRunRecoveryProof")
    if codabar:
        if not historical_codabar and (codabar["Source"] != qualification["MeasurementSource"] or codabar["CoreSha256"] != qualification["CandidateCoreSha256"]):
            raise ValueError("Codabar proof differs from the measured candidate")
        main = codabar["MainCorpus"]
        notice = [f'The Codabar discovery below belongs to source `{codabar["Source"]}`. Its gain and diagnostic table describe that earlier change. Current corpus results and costs are measured separately on the current core.', ""] if historical_codabar else []
        lines += ["", "## Historical Codabar pixel-run recovery" if historical_codabar else "## Codabar pixel-run recovery", "", *notice,
                  f'The same six Codabar images decode correctly ({main["Correct"]}/{main["Images"]}), versus {main["PreviousCorrect"]}/{main["Images"]} previously. Two previously failing photographs now pass the hinted decoder, unrestricted decoder and unified scanner.', "",
                  codabar["Changes"][0], "",
                  "| Engine | Previous/current main diagnostic positives / 6 | Previous/current completed negatives / 31 | Previous/current supplemental positives / 5 | Current unexpected diagnostic/supplemental images |",
                  "|---|---:|---:|---:|---:|"]
        for engine, row in codabar["Diagnostics"]["diagnostic"].items():
            old, now = row["Previous"], row["Current"]
            sup = codabar["Diagnostics"]["supplemental"][engine]
            lines.append(f'| {engine} | {old["CorrectPositive"]} / {now["CorrectPositive"]} | {old["CorrectNegative"]} / {now["CorrectNegative"]} | {sup["Previous"]["CorrectPositive"]} / {sup["Current"]["CorrectPositive"]} | {now["UnexpectedImages"]} / {sup["Current"]["UnexpectedImages"]} |')
        lines += ["", "Six new image-entrypoint regressions fail on the old core and pass on the candidate. Sixteen focused cases per framework additionally cover print gain, punctuation, orientation, polarity and malformed symbols.", "",
                  codabar["Limits"], "", "[Codabar proof and provenance](evidence/codabar-run-recovery-proof.json) · [Supplemental input provenance](evidence/codabar-supplemental-provenance.json)."]
    negative_cost = qualification.get("NegativeSearchCostProof")
    if negative_cost:
        decisions = negative_cost["DecisionEquivalence"]
        if (negative_cost["Source"] != qualification["MeasurementSource"]
                or negative_cost["CoreSha256"] != qualification["CandidateCoreSha256"]
                or decisions["Mismatches"] != 0):
            raise ValueError("Negative-search cost proof differs from the measured candidate")
        lines += ["", "## Negative-image decoding work", "",
                  "Direct-mark retries reuse one local-intensity table across dark and light adaptive masks. PDF417 scanlines reject guard ratios that cannot satisfy the existing fit. Search bounds, variant order, cancellation checks, recognition allowances and payload scoring are unchanged.", "",
                  f'The actual previous and current private methods produce identical bytes for {decisions["PreprocessingOutputs"]} preprocessing outputs across {decisions["PreprocessingConfigurations"]} configurations. All {decisions["GuardCases"]:,} guard decisions match, including {decisions["AcceptedGuards"]:,} accepted fits.', "",
                  negative_cost["Limits"], "",
                  "The [decision and allocation diagnostic](evidence/negative-search-cost-proof.json) records source hashes, output checks and allocation observations. Paired whole-image costs and negative completion below determine the measured outcome."]
    matrix = qualification.get("DataMatrixDeadlineProof")
    if matrix:
        decisions = matrix["DecisionEquivalence"]
        if (matrix["Source"] != qualification["MeasurementSource"]
                or matrix["CoreSha256"] != qualification["CandidateCoreSha256"]
                or decisions["Mismatches"] != 0):
            raise ValueError("Data Matrix deadline proof differs from the measured candidate")
        lines += ["", "## Data Matrix deadline lookup", "",
                  "Bounding-box search captures its fixed enclosing deadline once per synchronous call. It still checks caller cancellation and the monotonic clock at every original row and pixel position. Pixel arithmetic, polarity and bounds are unchanged, as are recognition allowances, format hints, payload oracles and scoring.", "",
                  f'The actual previous and current private methods agree on {decisions["Cases"]:,} cases, with zero mismatches. The diagnostic covers RGBA and BGRA pixels, transparency, padded strides, inversion, blank, solid and random images, live and expired ambient clocks, and cancelled tokens.', "",
                  matrix["Limits"], "",
                  "The [decision diagnostic](evidence/datamatrix-box-equivalence.json) records the exact baseline and candidate core hashes. Current whole-image timings and completion counts are reported in the paired decoding comparison below.", "",
                  matrix["CostScope"], "",
                  "| Domain | Previous kernel ms | Current kernel ms | Current / previous | Previous managed bytes | Current managed bytes |",
                  "| --- | ---: | ---: | ---: | ---: | ---: |"]
        for row in matrix["Cost"]:
            lines.append(f'| {row["Domain"].upper()} | {row["PreviousMedianMs"]:.3f} | {row["CurrentMedianMs"]:.3f} | {row["Ratio"]:.3f} | {row["PreviousAllocatedBytes"]:,.0f} | {row["CurrentAllocatedBytes"]:,.0f} |')
    pdf_cost = qualification.get("Pdf417CostProof") or qualification.get("HistoricalPdf417CostProof")
    if pdf_cost:
        historical = not qualification.get("Pdf417CostProof")
        if not historical and pdf_cost["Source"] != qualification["MeasurementSource"]:
            raise ValueError("PDF417 cost source differs from the measured candidate")
        for domain in ("ccd0", "ccd1"):
            metadata = read(root / f"pdf417-cost-{domain}-result.json")["Metadata"]
            if (metadata["benchmark.CandidateSource"] != pdf_cost["Source"]
                    or metadata["benchmark.CandidateCoreSha256"] != pdf_cost["CoreSha256"]):
                raise ValueError("PDF417 cost packet differs from its recorded source")
        heading = "## Historical PDF417 search cost" if historical else "## PDF417 search cost"
        source_note = (f'These retained measurements compare source `{pdf_cost["PreviousSource"]}` with `{pdf_cost["Source"]}`. They qualify the earlier PDF417 optimization and do not measure the current candidate.'
                       if historical else f'Measured candidate: `{pdf_cost["Source"]}`.')
        lines += ["", heading, "", source_note, "", pdf_cost["Scope"], "", pdf_cost["Limits"], "",
                  "| Domain | Before ms | After ms | After / before | Before completed /31 | After completed /31 | Before unfinished batches /12 | After unfinished batches /12 | Before managed MiB | After managed MiB |",
                  "| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |"]
        for row in pdf_cost["Cost"]:
            ranges = [str(int(v[0])) if v[0] == v[1] else f"{v[0]:g}–{v[1]:g}" for v in (row["PreviousCorrectRange"], row["CurrentCorrectRange"])]
            lines.append(f'| {row["Domain"].upper()} | {row["BaselineMedianMs"]:.2f} | {row["CurrentMedianMs"]:.2f} | {row["LatencyRatio"]:.3f} | {ranges[0]} | {ranges[1]} | {row["PreviousInconclusiveBatches"]} | {row["CurrentInconclusiveBatches"]} | {row["BaselineAllocatedBytes"]/1048576:.2f} | {row["CurrentAllocatedBytes"]/1048576:.2f} |')
        guard = pdf_cost["GuardEquivalence"]
        lines += ["", "Completed counts show the range across measured batches; neither binary returns an unexpected payload. An unfinished negative search remains inconclusive.", "",
                  f'The actual old and new guard matchers return identical decisions across {guard["Cases"]:,} deterministic cases, including {guard["Accepted"]:,} accepted fits. Existing focused PDF417 and budget checks pass; no public API or production dependency changes.', "",
                  "Raw [CCD0 samples](evidence/pdf417-cost-ccd0-result.json), [CCD1 samples](evidence/pdf417-cost-ccd1-result.json), [unchanged negative inputs](evidence/pdf417-cost-negative-manifest.json), [separate diagnostic outcomes](evidence/pdf417-cost-negative-outcomes.json) and [guard equivalence evidence](evidence/pdf417-cost-guard-equivalence.json) are retained."]
    if qualification.get("RetailRecoveryProof") or qualification.get("HistoricalRetailRecoveryProof"):
        retail = qualification.get("RetailRecoveryProof") or qualification["HistoricalRetailRecoveryProof"]
        historical_retail = not qualification.get("RetailRecoveryProof")
        if not historical_retail and retail["Source"] != qualification["MeasurementSource"]:
            raise ValueError("Retail diagnostics differ from the measured source")
        lines += ["", ("## Historical retail recovery and search cost" if historical_retail else "## Retail recovery and search cost"), "", (f'The diagnostics and paired costs in this section belong to source {retail["Source"]}; they retain their original binary identities.' if historical_retail else ""), "", retail.get("Changes", ""), "", retail["Scope"], "",
                  "The 87 positive requests reuse the frozen retail images. These diagnostics help isolate the changed behavior and do not add independent images to the recognition ranking.", "",
                  "| Reader | Previous positive correct | Current positive correct | Current unexpected-output images |",
                  "| --- | ---: | ---: | ---: |"]
        for engine in engines:
            row = retail["Engines"][engine]["positive"]
            lines.append(f'| {engine} | {row["PreviousCorrect"]}/87 | {row["Correct"]}/87 | {row["Unexpected"]} |')
        lines += ["", "Each of 31 fixed negative images is scanned with EAN, UPC-A and UPC-E hints: 93 requests, rather than 93 independent images.", "",
                  "| Reader | Previous correct rejection | Current correct rejection | Current unexpected output | Current inconclusive |",
                  "| --- | ---: | ---: | ---: | ---: |"]
        for engine in engines:
            row = retail["Engines"][engine]["negative"]
            lines.append(f'| {engine} | {row["PreviousCorrect"]}/93 | {row["Correct"]}/93 | {row["Unexpected"]} | {row["Inconclusive"]} |')
        lines += ["", retail["CostLimits"], "",
                  "| Domain | Previous median ms / nine requests | Current median ms / nine requests | Latency ratio | Previous mean managed MiB | Current mean managed MiB | Allocation ratio | Previous/current correct requests |",
                  "| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |"]
        for row in retail["Cost"]:
            lines.append(f'| {row["Domain"]} | {row["BaselineMedianMs"]:.2f} | {row["CurrentMedianMs"]:.2f} | {row["LatencyRatio"]:.2f}x | {row["BaselineAllocatedBytes"] / 1048576:.2f} | {row["CurrentAllocatedBytes"] / 1048576:.2f} | {row["AllocationRatio"]:.2f}x | {row["PreviousCorrectRequests"]:g}/9 → {row["CurrentCorrectRequests"]:g}/9 |')
        lines += ["", "Correct-request counts accompany these costs. Unexpected outputs and unfinished scans remain in the raw samples. Shared-host measurements do not establish portable speed or a population false-positive rate.", "",
                  "[Positive requests](evidence/retail-positive-diagnostic.json) · [negative requests](evidence/retail-negative-diagnostic.json) · [current outcomes](evidence/retail-negative-current.json) · [CCD0 raw cost samples](evidence/retail-cost-ccd0-result.json) · [CCD1 raw cost samples](evidence/retail-cost-ccd1-result.json)."]
        if retail.get("PositiveCost"):
            lines += ["", retail["PositiveCostScope"], "",
                      "| Domain | Previous → current median ms / 87 positives | Current / previous | Previous → current mean managed MiB | Previous → current mean correct / 87 |",
                      "| --- | ---: | ---: | ---: | ---: |"]
            for row in retail["PositiveCost"]:
                lines.append(f'| {row["Domain"].upper()} | {row["BaselineMedianMs"]:.3f} → {row["CurrentMedianMs"]:.3f} | {row["LatencyRatio"]:.3f} | {row["BaselineAllocatedBytes"]/1048576:.2f} → {row["CurrentAllocatedBytes"]/1048576:.2f} | {row["PreviousCorrectRequests"]:.3f} → {row["CurrentCorrectRequests"]:.3f} |')
            if retail.get("FocusedCases"):
                lines += ["", f'Three independent EAN photographs pass in both orientations and polarities. {retail["ExactBaselineFailures"]} of {retail["FocusedCases"]} new cases fail against the exact preceding binary; all pass on the current core. Lone-row and invalid-checksum inputs remain rejected.']
            lines += ["", "[Retail recovery and cost proof](evidence/retail-row-recovery-proof.json) · [CCD0 positive cost samples](evidence/retail-positive-cost-ccd0-result.json) · [CCD1 positive cost samples](evidence/retail-positive-cost-ccd1-result.json)."]
        reuse = retail.get("ExtremaReuse")
        if reuse:
            if reuse["CandidateCoreSha256"] != retail["CoreSha256"] or reuse["Mismatches"]:
                raise ValueError("Retail extrema proof differs from the measured core")
            lines += ["", f'Retail thresholding reuses sliding minima and maxima across adjacent cutoffs. It preserves all eight binary hypotheses and their order. The actual previous and current binarizers agree on {reuse["Masks"]:,} masks and {reuse["PixelDecisions"]:,} pixel decisions across {reuse["Rows"]:,} fixed-image and seeded diagnostic rows. Independent direct extrema checks cover {reuse["IndependentExtremaPoints"]:,} positions. Precancelled and expired-budget calls abort. These deterministic checks establish output equivalence on their inputs; they are not an accuracy survey.', "",
                      "[Extrema equivalence proof](evidence/retail-extrema-equivalence.json). Earlier photograph recovery and its exact preceding-binary failures remain in the [historical row-recovery proof](evidence/historical-retail-row-recovery-proof.json); those gains are not attributed to the current optimization."]
    if qualification.get("DataBar14RunRecoveryProof") or qualification.get("HistoricalDataBar14RunRecoveryProof"):
        recovery = qualification.get("DataBar14RunRecoveryProof") or qualification["HistoricalDataBar14RunRecoveryProof"]
        historical_databar14 = not qualification.get("DataBar14RunRecoveryProof")
        if not historical_databar14 and (recovery["Source"] != qualification["MeasurementSource"] or recovery["CoreSha256"] != qualification["CandidateCoreSha256"]):
            raise ValueError("DataBar-14 recovery differs from the measured source")
        lines += ["", ("## Historical GS1 DataBar-14 character pitch recovery" if historical_databar14 else "## GS1 DataBar-14 character pitch recovery"), "", (f'The diagnostics and paired costs in this section belong to source {recovery["Source"]}; current whole-image qualification appears separately.' if historical_databar14 else ""), "", recovery["Changes"], "", recovery["Scope"], "",
                  "| Reader | Previous correct / 12 | Current correct / 12 | Current unexpected output |",
                  "| --- | ---: | ---: | ---: |"]
        for engine in engines:
            row = recovery["Engines"][engine]
            lines.append(f'| {engine} | {row["PreviousCorrect"]} | {row["Correct"]} | {row["Unexpected"]} |')
        lines += ["", f'All {recovery["FocusedCases"]} new photograph cases fail against the exact preceding binary and pass on the current core. They cover both axes and polarities through single-result pixels, multi-result pixels and the scanner. The {recovery["NegativeRequests"]} family-hinted negative requests remain separate from the main ranking.', "",
                  recovery["CostScope"], "", "| Domain | Previous → current median ms / 12 images | Current / previous | Previous → current correct / 12 | Previous → current managed MiB |",
                  "| --- | ---: | ---: | ---: | ---: |"]
        for row in recovery["Cost"]:
            lines.append(f'| {row["Domain"].upper()} | {row["BaselineMedianMs"]:.2f} → {row["CurrentMedianMs"]:.2f} | {row["LatencyRatio"]:.3f} | {row["PreviousCorrectRequests"]:g} → {row["CurrentCorrectRequests"]:g} | {row["BaselineAllocatedBytes"]/1048576:.2f} → {row["CurrentAllocatedBytes"]/1048576:.2f} |')
        lines += ["", "[DataBar-14 recovery proof](evidence/databar14-run-recovery-proof.json) · [Positive requests](evidence/databar14-positive-manifest.json) · [Negative requests](evidence/databar14-negative-manifest.json) · [CCD0 samples](evidence/databar14-cost-ccd0-result.json) · [CCD1 samples](evidence/databar14-cost-ccd1-result.json)."]
    tilted = project_databar_tilted(root, qualification)
    if tilted:
        lines += ["", "## Historical GS1 DataBar tilted photographs" if tilted["historical"] else "## GS1 DataBar tilted photographs", "", (f'These retained recovery and paired costs belong to source {tilted["source"]}; current whole-image qualification appears separately.' if tilted["historical"] else ""), "", tilted["changes"], "", tilted["scope"], "",
                  "| Engine | Previous correct / 12 | Current correct / 12 | Unexpected images |",
                  "| --- | ---: | ---: | ---: |"]
        for row in tilted["engines"]:
            lines.append(f'| {row["engine"]} | {row["before"]} | {row["correct"]} | {row["unexpected"]} |')
        lines += ["", tilted["cost_scope"], "",
                  "| Batch | Domain | Previous → current median ms | Current / previous | Previous → current correct range | Previous → current inconclusive range | Previous → current mean managed MiB |",
                  "| --- | --- | ---: | ---: | ---: | ---: | ---: |"]
        for row in tilted["costs"]:
            lines.append(f'| {row["kind"]}: {row["requests"]} | {row["domain"]} | {row["before_ms"]} → {row["current_ms"]} | {row["ratio"]} | {row["before_correct"]} → {row["current_correct"]} | {row["before_inconclusive"]} → {row["current_inconclusive"]} | {row["before_mib"]} → {row["current_mib"]} |')
        lines += ["", tilted["limits"], "", "[Recovery and regression proof](evidence/databar-tilted-separator-proof.json) · [Positive inputs](evidence/databar14-positive-manifest.json) · [Negative inputs](evidence/databar14-negative-manifest.json) · [CCD0 positive samples](evidence/databar-tilted-positive-ccd0-result.json) · [CCD1 positive samples](evidence/databar-tilted-positive-ccd1-result.json) · [CCD0 negative samples](evidence/databar-tilted-negative-ccd0-result.json) · [CCD1 negative samples](evidence/databar-tilted-negative-ccd1-result.json)."]
    append_retained_context_section(lines, project_aztec_probe_cache(root, qualification))
    append_retained_context_section(lines, project_pdf417_segment_cache(root, qualification))
    append_retained_context_section(lines, project_retained_contexts(root, qualification))
    append_dpm_window_means_section(lines, project_dpm_window_means(root, qualification))
    append_dpm_gray8_section(lines, project_dpm_gray8(root, qualification))
    append_qr_finder_group_section(lines, project_qr_finder_groups(root, qualification))
    append_decode_deadline_section(lines, project_decode_deadlines(root, qualification))
    append_decode_allocation_section(lines, project_decode_allocations(root, qualification))
    append_scanner_boundary_section(lines, project_scanner_boundaries(root, qualification))
    fine = project_retail_fine(root, qualification)
    if fine:
        lines += ["", "## Historical retail photographs and tile boundary safety" if fine["historical"] else "## Retail photographs and tile boundary safety", "", (f'These retained diagnostics and costs belong to source {fine["source"]}; the current scanner contracts and costs appear separately. The current scanner repair above covers the inverted single-digit and joined-Pharmacode gaps described in this historical packet.' if fine["historical"] else ""), "", fine["changes"], "", fine["scope"], "",
                  "| Engine | Previous correct / 87 | Current correct / 87 | Unexpected images |",
                  "| --- | ---: | ---: | ---: |"]
        for row in fine["engines"]:
            lines.append(f'| {row["engine"]} | {row["before"]} | {row["correct"]} | {row["unexpected"]} |')
        lines += ["", fine["cost_scope"], "",
                  "| Batch | Domain | Previous → current median ms | Current / previous | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range | Previous → current mean managed MiB |",
                  "| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |"]
        for row in fine["costs"]:
            lines.append(f'| {row["kind"]}: {row["requests"]} | {row["domain"]} | {row["before_ms"]} → {row["current_ms"]} | {row["ratio"]} | {row["before_correct"]} → {row["current_correct"]} | {row["before_inconclusive"]} → {row["current_inconclusive"]} | {row["before_unexpected"]} → {row["current_unexpected"]} | {row["before_mib"]} → {row["current_mib"]} |')
        lines += ["", "The 31 distinct negative images are also scanned separately with each retail hint: 93 requests. This diagnostic does not replace all-format negative completion.", "",
                  "| Engine | Completed / 93 | Inconclusive | Unexpected requests |", "| --- | ---: | ---: | ---: |"]
        for row in fine["negative_diagnostic"]:
            lines.append(f'| {row["engine"]} | {row["completed"]} | {row["inconclusive"]} | {row["unexpected"]} |')
        lines += ["", "Eight default all-format tile-scanner cases prohibit retail payloads on the perforated negative fixture in four rotations and both polarities. The four non-inverted cases return no symbol. Each case also exhausts typed EAN/UPC tile decoding. An unrelated format can reach its recognition limit on a legacy runtime, so safety and completion remain separate. All eight fail with the exact initial candidate before the full-length restriction.", "", fine["negative_variant_limits"], "", "[Additional negative variants before](evidence/retail-fine-negative-variants-baseline.json) · [Additional negative variants current](evidence/retail-fine-negative-variants-current.json).", "", fine["limits"], "", "[Recovery and regression proof](evidence/retail-fine-contrast-proof.json) · [Positive requests](evidence/retail-fine-positive-manifest.json) · [Negative cost requests](evidence/retail-fine-negative-manifest.json) · [Hinted negative outcomes](evidence/retail-fine-negative-diagnostic.json) · [CCD0 positive samples](evidence/retail-fine-positive-ccd0-result.json) · [CCD1 positive samples](evidence/retail-fine-positive-ccd1-result.json) · [CCD0 negative samples](evidence/retail-fine-negative-ccd0-result.json) · [CCD1 negative samples](evidence/retail-fine-negative-ccd1-result.json)."]
        lines += ["", "### Complete margins for Industrial 2 of 5 tiles", "", fine["industrial"]["changes"], "", fine["industrial"]["scope"], "", fine["industrial"]["limits"], "", "[Tile boundary and single-digit regression proof](evidence/industrial-tile-boundary-proof.json) · [Tile geometry before correction](evidence/industrial-tile-boundary-diagnosis.json) · [Safety contracts before correction](evidence/industrial-tile-before-proof.json) · [Current short-symbol diagnostic](evidence/industrial-tile-boundary-current-diagnostic.json) · [Transparent-margin contracts before correction](evidence/industrial-tile-alpha-before-proof.json) · [Current transparent-margin diagnostic](evidence/industrial-tile-alpha-current-diagnostic.json)."]

    morphology = project_qr_morphology(root, qualification)
    if morphology:
        lines += ["", "## Historical QR image cleanup and negative-search cost" if morphology["historical"] else "## QR image cleanup and negative-search cost", "", (f'These retained filter and whole-image costs belong to source {morphology["source"]}; current whole-image qualification appears separately.' if morphology["historical"] else ""), "", morphology["changes"], "", morphology["scope"], "",
                  "Every output pixel matches the preceding binary on 24 closing/opening filters over four corpus images. Eight direct-neighborhood cases pass on each modern framework, covering solid and mixed images, clipped borders, narrow dimensions and adaptive thresholds.", "",
                  "| Domain | Previous → current median ms / 24 filters | Current / previous | Previous → current mean managed MiB |",
                  "| --- | ---: | ---: | ---: |"]
        for row in morphology["kernel"]:
            lines.append(f'| {row["domain"]} | {row["before_ms"]} → {row["current_ms"]} | {row["ratio"]} | {row["before_mib"]} → {row["current_mib"]} |')
        lines += ["", "The same four difficult negatives are also measured through the complete scanner. Completed-image ranges retain every measured batch; an unfinished empty search remains inconclusive.", "",
                  "| Domain | Previous → current median ms / 4 images | Current / previous | Previous → current completed / 4 range (mean) | Previous → current inconclusive range (mean) | Previous → current mean managed MiB |",
                  "| --- | ---: | ---: | ---: | ---: | ---: |"]
        for row in morphology["negatives"]:
            lines.append(f'| {row["domain"]} | {row["before_ms"]} → {row["current_ms"]} | {row["ratio"]} | {row["before_correct"]} ({row["before_correct_mean"]}) → {row["current_correct"]} ({row["current_correct_mean"]}) | {row["before_inconclusive"]} ({row["before_inconclusive_mean"]}) → {row["current_inconclusive"]} ({row["current_inconclusive_mean"]}) | {row["before_mib"]} → {row["current_mib"]} |')
        lines += ["", morphology["limits"], "", "[Proof and output hashes](evidence/qr-morphology-cost-proof.json) · [Unchanged images and oracles](evidence/qr-morphology-negative-manifest.json) · [CCD0 kernel samples](evidence/qr-morphology-cost-ccd0-result.json) · [CCD1 kernel samples](evidence/qr-morphology-cost-ccd1-result.json) · [CCD0 whole-image samples](evidence/qr-negative-cost-ccd0-result.json) · [CCD1 whole-image samples](evidence/qr-negative-cost-ccd1-result.json)."]
    if qualification.get("AttributionLimits"):
        lines += ["", qualification["AttributionLimits"]]
    if "CorpusHostCpuBeforeScans" in qualification:
        host = qualification["CorpusHostCpuBeforeScans"]
        lines += ["", "Host CPU samples before the main corpus pass: " + ", ".join(f"{x:.1f}%" for x in host["corpus"]) + ". Before the supplementary negative pass: " + ", ".join(f"{x:.1f}%" for x in host["negatives"]) + ". These are separate passes; overlapping images can have different completion outcomes."]
    negatives_before = read(root / "negative-baseline-corpus.json")
    negatives_current = read(root / "negative-candidate-corpus.json")
    lines += ["", "## Supplementary negative corpus", "",
              "The fixed supplementary set contains all 25 images in upstream falsepositives-2; six also appear in the main corpus. This supplementary check is reported separately. Empty output counts as a correct rejection only when scanning completes; an exhausted budget remains inconclusive.", "",
              "| Engine | Original source: correct rejection | Current: correct rejection | Current: unexpected output | Current: inconclusive |",
              "| --- | ---: | ---: | ---: | ---: |"]
    for engine in engines:
        old = [x for x in negatives_before if x["Engine"] == engine]
        new = [x for x in negatives_current if x["Engine"] == engine]
        if len(old) != 25 or len(new) != 25 or any(x["Expected"] for x in old + new):
            raise ValueError("Supplementary negative corpus differs from its contract")
        lines.append(f'| {engine} | {sum(x["Status"] == "Correct" for x in old)}/25 | {sum(x["Status"] == "Correct" for x in new)}/25 | {sum(bool(x["Unexpected"]) for x in new)} | {sum(x["Status"] == "Inconclusive" for x in new)} |')
    if (root / "previous-negative-corpus.json").exists():
        previous_negatives = [x for x in read(root / "previous-negative-corpus.json") if x["Engine"] == "CodeGlyphX"]
        now = [x for x in negatives_current if x["Engine"] == "CodeGlyphX"]
        if len(previous_negatives) != 25 or {x["Id"] for x in now} != {x["Id"] for x in previous_negatives} or any(x["Expected"] for x in previous_negatives):
            raise ValueError("Previous supplementary negative identities differ")
        previously_completed = {x["Id"] for x in previous_negatives if x["Status"] == "Correct"}
        incomplete = [x["Id"] for x in now if x["Id"] in previously_completed and x["Status"] != "Correct"]
        lines += ["", f'Compared with the [previous decoding pass](evidence/previous-negative-corpus.json), CodeGlyphX completes {sum(x["Status"] == "Correct" for x in now)}/25 supplementary negatives versus {len(previously_completed)}/25. Previously completed images now unfinished: ' + (", ".join(incomplete) if incomplete else "none in this run.")]
    if (root / "negative-rotation-manifest.json").exists():
        ids = {sample["Id"] for sample in read(root / "negative-rotation-manifest.json")["Samples"]}
        expected_ids = {row["Id"] for row in current + negatives_current if not row["Expected"]}
        if ids != expected_ids or len(ids) != 31:
            raise ValueError("Paired negative inputs differ from the fixed corpus union")
        lines += ["", "## Paired negative completion", "",
                  "The previous and current binaries scan the same 31 distinct negative IDs from the two fixed manifests. Order alternates on each cache domain, with Normal priority and the same 1,500 ms per-image budget. Every pass is retained. CPU readings are three host samples before each pass; they do not establish an idle machine. These repeated passes expose budget sensitivity and do not replace the main or supplementary results above.", "",
                  "| Domain | Pass | Binary | Correct rejection | Inconclusive | Unexpected output | Mean host CPU before pass |",
                  "| --- | ---: | --- | ---: | ---: | ---: | ---: |"]
        for domain in ("ccd0", "ccd1"):
            summaries = read(root / f"negative-rotation-{domain}-summary.json")
            if len(summaries) != 4:
                raise ValueError("Incomplete paired negative passes")
            for row in summaries:
                outcomes = [x for x in read(root / row["File"]) if x["Engine"] == "CodeGlyphX"]
                if len(outcomes) != 31 or {x["Id"] for x in outcomes} != ids or any(x["Expected"] for x in outcomes):
                    raise ValueError("Paired negative output identities differ")
                counts = [sum(x["Status"] == "Correct" for x in outcomes), sum(x["Status"] == "Inconclusive" for x in outcomes), sum(bool(x["Unexpected"]) for x in outcomes)]
                if counts != [row["Completed"], row["Inconclusive"], row["Unexpected"]]:
                    raise ValueError("Paired negative summary differs from raw outcomes")
                cpu = sum(row["Cpu"]) / len(row["Cpu"])
                lines.append(f'| {domain} | {row["Index"] + 1} | {row["Source"]} | {row["Completed"]}/31 | {row["Inconclusive"]} | {row["Unexpected"]} | {cpu:.1f}% |')
    if qualification.get("PlacementCpuBeforePasses"):
        lines += ["", "## Initial host load", "", qualification["PlacementCpuLimits"], "",
                  "| Pass | Mean host CPU | Mean CCD0 CPU | Mean CCD1 CPU |",
                  "| --- | ---: | ---: | ---: |"]
        for name, capture in sorted(qualification["PlacementCpuBeforePasses"].items()):
            samples = capture["Samples"]
            if len(samples) != 3 or capture["Masks"] != ["0xFFFF", "0xFFFF0000"]:
                raise ValueError("Incomplete CPU placement capture")
            means = [sum(sample[key] for sample in samples) / len(samples) for key in ("Global", "Ccd0", "Ccd1")]
            label = name.removesuffix("-placement-cpu.json")
            lines.append(f'| {label} | {means[0]:.1f}% | {means[1]:.1f}% | {means[2]:.1f}% |')
    for domain in ("ccd0", "ccd1"):
        packet = runs["generation", domain]
        metadata = packet["Metadata"]
        lines += ["", f"## Generation on {domain}", "", f'Source `{metadata.get("gitSha")}`, affinity `{metadata.get("processAffinityMask")}`, priority `{metadata.get("processPriority")}`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.', "",
                  "| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |",
                  "| --- | --- | --- | ---: | ---: | ---: | ---: |"]
        for x in packet["Summary"]:
            # milliseconds/batch * 1000 microseconds/millisecond / 100 outputs
            lines.append(f'| {x["Scenario"]} | {x["Operation"]} | {x["Engine"]} | {x["MedianMs"] * 10:.2f} | {x["P95Ms"] * 10:.2f} | {x["MinMs"] * 10:.2f}–{x["MaxMs"] * 10:.2f} | {x["Metrics"]["ManagedAllocatedBytes"] / 100:.0f} |')
    lines += ["", "## Decode latency", "", "The nineteen-image subset includes a negative, a MaxiCode image and a four-QR image. Loading is excluded. Missed and unfinished decodes remain included, with correctness reported alongside timing. The timing comparison applies to this fixed subset; accuracy on the full corpus differs. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.", "",
              "Managed allocation is the mean process-wide allocation during each complete batch operation, including runner invocation overhead. It excludes image loading, result evaluation and native allocations. It does not measure peak or total memory use.", "",
              "| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB / batch | Correct images range | Unexpected images range |",
              "| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |"]
    for domain in ("ccd0", "ccd1"):
        packet = runs["decoding", domain]
        for x in packet["Summary"]:
            samples = [s for s in packet["Samples"] if s["Engine"] == x["Engine"] and s["Iteration"] >= 0]
            correct = [s["Metrics"]["CorrectImages"] for s in samples]
            unexpected = [s["Metrics"]["UnexpectedImages"] for s in samples]
            lines.append(f'| {domain} | {x["Engine"]} | {x["MedianMs"]:.2f} | {x["P95Ms"]:.2f} | {x["MinMs"]:.2f}–{x["MaxMs"]:.2f} | {x["Metrics"]["AllocatedBytes"] / 1048576:.2f} | {min(correct):g}–{max(correct):g} | {min(unexpected):g}–{max(unexpected):g} |')
    if qualification.get("PairedDecodeCost"):
        paired = qualification["PairedDecodeCost"]
        if paired["Source"] != qualification["MeasurementSource"]:
            raise ValueError("Paired whole-image cost source differs")
        lines += ["", "## Paired whole-image decoding cost", "", paired["Scope"]]
        for note in paired.get("FollowupNotes", []):
            lines += ["", note]
        lines += ["", "Lower current/previous ratios mean lower elapsed time. Correctness and unfinished-search ranges retain every measured sample; a faster incomplete search is not a successful negative scan.", "",
                  "| Domain | Binary | Median ms / 19 images | P95 ms | Min–max ms | Mean managed MiB | Correct / 19 | Unexpected | Inconclusive | Current / previous median |",
                  "| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |"]
        for domain in ("ccd0", "ccd1"):
            packet = read(root / f"paired-decode-cost-{domain}-result.json")
            validate_qualified_whole_image_packet(packet, qualification, domain)
            lanes = {row["Engine"]: row for row in packet["Summary"]}
            ratio = lanes["Candidate"]["MedianMs"] / lanes["Baseline"]["MedianMs"]
            for engine in ("Baseline", "Candidate"):
                lane = lanes[engine]
                samples = [s for s in packet["Samples"] if s["Engine"] == engine and s["Iteration"] >= 0]
                ranges = []
                for metric in ("CorrectImages", "UnexpectedImages", "InconclusiveImages"):
                    values = [s["Metrics"][metric] for s in samples]
                    ranges.append(f"{min(values):g}–{max(values):g}")
                lines.append(f'| {domain} | {engine} | {lane["MedianMs"]:.2f} | {lane["P95Ms"]:.2f} | {lane["MinMs"]:.2f}–{lane["MaxMs"]:.2f} | {lane["Metrics"]["AllocatedBytes"] / 1048576:.2f} | {ranges[0]} | {ranges[1]} | {ranges[2]} | {ratio:.3f} |')
        lines += ["", "[CCD0 paired samples](evidence/paired-decode-cost-ccd0-result.json) · [CCD1 paired samples](evidence/paired-decode-cost-ccd1-result.json)."]
    lines += ["", "All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.", ""]
    (output / "results.md").write_text("\n".join(lines), encoding="utf-8")
    if args.website_root:
        write_website_data(evidence, args.website_root)


if __name__ == "__main__":
    main()
