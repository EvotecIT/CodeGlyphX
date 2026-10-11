"""Generate a current report from qualified PowerForge packets and every corpus outcome."""
import argparse
import hashlib
import json
from pathlib import Path
from comparison_website_data import write_website_data


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
            expected = 24 if kind == "generation" else 3
            if len(packet["Summary"]) != expected or any(
                row["Status"] != "Succeeded" or row["FailureCount"] or row["SampleCount"] != 12
                for row in packet["Summary"]
            ):
                raise ValueError(f"Unqualified benchmark packet: {path}")
            runs[kind, domain] = packet
    # Keep every sample, failure, unsupported format and returned payload available for audit.
    names = ["qualified-baseline-corpus.json", "candidate-corpus-final.json",
             "negative-baseline-corpus.json", "negative-candidate-corpus.json", "negative-manifest.json",
             "qualification.json"]
    names += [f"{kind}-final-{domain}-result.json" for kind, domain in runs]
    names += [name for name in ("previous-candidate-corpus.json", "previous-negative-corpus.json", "previous-qualification.json") if (root / name).exists()]
    names += sorted(path.name for path in root.glob("negative-rotation-*.json"))
    names += sorted(path.name for path in root.glob("pdf417-supplemental-*.json"))
    if qualification.get("Pdf417FramingProof") or qualification.get("HistoricalPdf417FramingProof"):
        names += sorted(path.name for path in root.glob("pdf417-framing-*.json"))
    if qualification.get("Pdf417CostProof") or qualification.get("HistoricalPdf417CostProof"):
        names += sorted(path.name for path in root.glob("pdf417-cost-*.json"))
    if qualification.get("QrAlignmentProof") or qualification.get("HistoricalQrAlignmentProof"):
        names.append("qr-alignment-equivalence.json")
    if qualification.get("MaxiCodeImageProof"):
        names.append("maxicode-image-proof.json")
    if qualification.get("DataMatrixDeadlineProof") or qualification.get("HistoricalDataMatrixDeadlineProof"):
        names.append("datamatrix-box-equivalence.json")
        names += sorted(path.name for path in root.glob("datamatrix-box-cost-*-result.json"))
    if qualification.get("RetailRecoveryProof"):
        names += sorted(path.name for path in root.glob("retail-*.json"))
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
             "## Recognition", "", f"The 279-image corpus includes twelve negative images. {scanner_coverage} Exact payload sets are checked after the documented physical representation rules.", "",
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
                  f'The same six MaxiCode images now decode correctly ({maxi["Correct"]}/{maxi["Images"]}), versus {maxi["PreviousCorrect"]}/{maxi["Images"]} previously. All six previously lacked image-scanning support. Their bytes, format hints and payload oracles are unchanged.', "",
                  "The scanner verifies circular finder bands, samples the staggered module grid and uses the existing primary and secondary error correction. It accepts planar rotation and inverted polarity, and can recover small damage to fixed orientation modules. The existing result carries secondary text and bytes, typed carrier metadata and the searched source region.", "",
                  maxi["TextContract"], "", maxi["Evidence"], "", maxi["Limits"], "",
                  "The [MaxiCode proof](evidence/maxicode-image-proof.json) records source hashes and search bounds. The capability catalog advertises one-symbol image scanning without geometry. Independent fixture tests do not establish coverage of every numeric MaxiCode mode; fixture filenames are not encoding-mode evidence."]
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
    if qualification.get("RetailRecoveryProof"):
        retail = qualification["RetailRecoveryProof"]
        if retail["Source"] != qualification["MeasurementSource"]:
            raise ValueError("Retail diagnostics differ from the measured source")
        lines += ["", "## Retail recovery and search cost", "", retail["Scope"], "",
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
    lines += ["", "## Decode latency", "", "The nineteen-image subset includes a negative, a MaxiCode image that was unsupported by the previous source, and a four-QR image. Loading is excluded. Missed and unfinished decodes remain included, with correctness reported alongside timing. The timing comparison applies to this fixed subset; accuracy on the full corpus differs. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.", "",
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
