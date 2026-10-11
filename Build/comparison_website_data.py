"""Project qualified comparison evidence into the website's read-only view model."""
import argparse
import json
from pathlib import Path


def load(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def build_website_data(root):
    qualification = load(root / "qualification.json")
    baseline = load(root / "qualified-baseline-corpus.json")
    current = load(root / "candidate-corpus-final.json")
    negatives = load(root / "negative-candidate-corpus.json")
    engines = ["CodeGlyphX", "ZXing.Net", "ZXing-C++"]
    recognition = []
    formats = []
    for engine in engines:
        old = [x for x in baseline if x["Engine"] == engine]
        new = [x for x in current if x["Engine"] == engine]
        negative = [x for x in negatives if x["Engine"] == engine]
        if len(new) != 279 or len(old) != 279 or len(negative) != 25:
            raise ValueError("Incomplete website corpus evidence")
        positives = sum(bool(x["Expected"]) for x in new)
        correct = sum(bool(x["Expected"]) and x["Status"] == "Correct" for x in new)
        recognition.append({
            "engine": engine, "before": sum(bool(x["Expected"]) and x["Status"] == "Correct" for x in old),
            "correct": correct, "positives": positives, "percent": f"{correct / positives * 100:.1f}",
            "unsupported": sum(x["Status"] == "Unsupported" for x in new),
            "rejected": sum(x["Status"] == "Correct" for x in negative),
            "unexpected": sum(bool(x["Unexpected"]) for x in negative),
            "inconclusive": sum(x["Status"] == "Inconclusive" for x in negative),
            "main_rejected": sum(not x["Expected"] and x["Status"] == "Correct" for x in new),
            "main_unexpected": sum(not x["Expected"] and bool(x["Unexpected"]) for x in new),
            "main_inconclusive": sum(not x["Expected"] and x["Status"] == "Inconclusive" for x in new),
        })
    for symbol in sorted({x["Format"] for x in current if x["Expected"]}):
        rows = [x for x in current if x["Format"] == symbol]
        formats.append({"format": symbol, "images": len([x for x in rows if x["Engine"] == "CodeGlyphX"]),
                        "correct": [sum(x["Engine"] == engine and x["Status"] == "Correct" for x in rows) for engine in engines]})
    negative_followup = None
    previous_negative_path = root / "previous-negative-corpus.json"
    if previous_negative_path.exists():
        previous_negatives = [x for x in load(previous_negative_path) if x["Engine"] == "CodeGlyphX"]
        now = [x for x in negatives if x["Engine"] == "CodeGlyphX"]
        if len(previous_negatives) != 25 or {x["Id"] for x in previous_negatives} != {x["Id"] for x in now} or any(x["Expected"] for x in previous_negatives):
            raise ValueError("Previous website negative evidence differs")
        before_completed = sum(x["Status"] == "Correct" for x in previous_negatives)
        now_completed = sum(x["Status"] == "Correct" for x in now)
        passes = [x for rows in qualification["NegativeRotation"].values() for x in rows]
        completion = {source: [x["Completed"] for x in passes if x["Source"] == source] for source in ("previous", "current")}
        by_domain = {domain: {source: [row["Completed"] for row in rows if row["Source"] == source]
                              for source in ("previous", "current")}
                     for domain, rows in qualification["NegativeRotation"].items()}
        domain_notes = " ".join(
            f'{domain.upper()}: current {min(counts["current"])}–{max(counts["current"])}/31, '
            f'previous {min(counts["previous"])}–{max(counts["previous"])}/31.'
            for domain, counts in by_domain.items())
        domain_losses = [domain.upper() for domain, counts in by_domain.items()
                         if max(counts["current"]) < min(counts["previous"])]
        negative_followup = (
            f'Compared with the previous decoding pass, CodeGlyphX completes {now_completed}/25 supplementary negatives versus {before_completed}/25. '
            'Alternating scans reuse the same 31 distinct negative IDs. ' + domain_notes + ' '
            'Every pass is retained in the downloadable report; unfinished scans remain inconclusive.'
            + (' The current binary completes fewer negatives throughout the ' + ', '.join(domain_losses)
               + ' passes. This measured completion loss remains open.' if domain_losses
               else ' The current binary records a lower minimum completion count in the alternating passes. This measured loss remains open.'
               if min(completion['current']) < min(completion['previous']) else '')
        )
    domains = []
    png_wins = 0
    module_cpp_wins = 0
    png_qrc_allocation_wins = 0
    generation_engines = engines + ["QRCoder"]
    for domain in ("ccd0", "ccd1"):
        generation = load(root / f"generation-final-{domain}-result.json")
        decoding = load(root / f"decoding-final-{domain}-result.json")
        for packet, count in ((generation, 24), (decoding, 3)):
            if len(packet["Summary"]) != count or any(
                x["Status"] != "Succeeded" or x["FailureCount"] or x["SampleCount"] != 12
                for x in packet["Summary"]
            ):
                raise ValueError("Incomplete website timing evidence")
        generation_rows = []
        for operation in ("Png", "Modules"):
            for scenario in ("Url", "Numeric", "Unicode"):
                lanes = [x for x in generation["Summary"] if x["Operation"].lower() == operation.lower() and x["Scenario"] == scenario]
                if len(lanes) != 4:
                    raise ValueError(f"Missing generation lanes: {operation}/{scenario}")
                fastest = min(lanes, key=lambda x: x["MedianMs"])["Engine"]
                least_managed = min(lanes, key=lambda x: x["Metrics"]["ManagedAllocatedBytes"])["Engine"]
                if operation == "Png":
                    png_wins += fastest == "CodeGlyphX"
                    png_qrc_allocation_wins += least_managed == "QRCoder"
                else:
                    module_cpp_wins += fastest == "ZXing-C++"
                cells = []
                for engine in generation_engines:
                    lane = next(x for x in lanes if x["Engine"] == engine)
                    cells.append({"engine": engine, "median": f'{lane["MedianMs"] * 10:.2f}',
                                  "p95": f'{lane["P95Ms"] * 10:.2f}',
                                  "min": f'{lane["MinMs"] * 10:.2f}', "max": f'{lane["MaxMs"] * 10:.2f}',
                                  "bytes": f'{lane["Metrics"]["ManagedAllocatedBytes"] / 100:,.0f}'})
                generation_rows.append({"operation": operation, "payload": scenario, "cells": cells})
        decode_rows = []
        for engine in engines:
            lane = next(x for x in decoding["Summary"] if x["Engine"] == engine)
            samples = [x for x in decoding["Samples"] if x["Engine"] == engine and x["Iteration"] >= 0]
            correct = {x["Metrics"]["CorrectImages"] for x in samples}
            decode_rows.append({"engine": engine, "median": f'{lane["MedianMs"]:.2f}',
                                "p95": f'{lane["P95Ms"]:.2f}', "min": f'{lane["MinMs"]:.2f}', "max": f'{lane["MaxMs"]:.2f}',
                                "managed_mib": f'{lane["Metrics"]["AllocatedBytes"] / 1048576:.2f}',
                                "correct": f"{min(correct):g}–{max(correct):g}" if len(correct) > 1 else f"{min(correct):g}"})
        domains.append({"id": domain, "label": domain.upper(), "affinity": generation["Metadata"]["processAffinityMask"],
                        "generation": generation_rows, "decoding": decode_rows})
    supplemental = qualification.get("SupplementalPdf417")
    supplemental_rows = []
    if supplemental:
        if supplemental["CurrentSource"] != qualification["MeasurementSource"]:
            raise ValueError("Supplemental PDF417 source differs from the qualified candidate")
        for engine in engines:
            result = supplemental["Engines"][engine]
            supplemental_rows.append({"engine": engine, "before": result["PreviousCorrect"],
                                      "correct": result["Correct"], "images": supplemental["Images"],
                                      "unexpected": result["UnexpectedImages"]})
    retail = qualification.get("RetailRecoveryProof")
    retail_rows, retail_cost = [], []
    if retail:
        if retail["Source"] != qualification["MeasurementSource"]:
            raise ValueError("Retail diagnostics differ from the qualified source")
        for engine in engines:
            positive, negative = (retail["Engines"][engine][kind] for kind in ("positive", "negative"))
            retail_rows.append({"engine": engine, "positive_before": positive["PreviousCorrect"],
                                "positive": positive["Correct"], "positive_unexpected": positive["Unexpected"],
                                "negative_before": negative["PreviousCorrect"], "negative": negative["Correct"],
                                "negative_unexpected": negative["Unexpected"], "inconclusive": negative["Inconclusive"]})
        for row in retail["Cost"]:
            retail_cost.append({"domain": row["Domain"].upper(), "before_ms": f'{row["BaselineMedianMs"]:.2f}',
                                "current_ms": f'{row["CurrentMedianMs"]:.2f}', "latency_ratio": f'{row["LatencyRatio"]:.2f}',
                                "before_mib": f'{row["BaselineAllocatedBytes"] / 1048576:.2f}',
                                "current_mib": f'{row["CurrentAllocatedBytes"] / 1048576:.2f}',
                                "allocation_ratio": f'{row["AllocationRatio"]:.2f}',
                                "before_correct": f'{row["PreviousCorrectRequests"]:g}', "current_correct": f'{row["CurrentCorrectRequests"]:g}'})
    paired_decode = qualification.get("PairedDecodeCost")
    paired_decode_rows = []
    if paired_decode:
        if paired_decode["Source"] != qualification["MeasurementSource"]:
            raise ValueError("Paired decoding source differs from the qualified candidate")
        for domain in ("ccd0", "ccd1"):
            packet = load(root / f"paired-decode-cost-{domain}-result.json")
            if packet["Metadata"]["benchmark.CandidateSource"] != qualification["MeasurementSource"]:
                raise ValueError("Paired decoding packet source differs")
            lanes = {row["Engine"]: row for row in packet["Summary"]}
            if set(lanes) != {"Baseline", "Candidate"} or any(
                row["Status"] != "Succeeded" or row["FailureCount"] or row["SampleCount"] != 12
                for row in lanes.values()
            ):
                raise ValueError("Incomplete paired decoding evidence")
            row = {"domain": domain.upper(),
                   "previous_ms": f'{lanes["Baseline"]["MedianMs"]:.2f}',
                   "current_ms": f'{lanes["Candidate"]["MedianMs"]:.2f}',
                   "ratio": f'{lanes["Candidate"]["MedianMs"] / lanes["Baseline"]["MedianMs"]:.3f}'}
            for engine, prefix in (("Baseline", "previous"), ("Candidate", "current")):
                samples = [s for s in packet["Samples"] if s["Engine"] == engine and s["Iteration"] >= 0]
                for metric, name in (("CorrectImages", "correct"), ("UnexpectedImages", "unexpected"), ("InconclusiveImages", "inconclusive")):
                    values = [s["Metrics"][metric] for s in samples]
                    row[f"{prefix}_{name}"] = f"{min(values):g}–{max(values):g}" if min(values) != max(values) else f"{min(values):g}"
                row[f"{prefix}_managed_mib"] = f'{lanes[engine]["Metrics"]["AllocatedBytes"] / 1048576:.2f}'
            paired_decode_rows.append(row)
    pdf417_cost = []
    proof = qualification.get("Pdf417CostProof") or qualification.get("HistoricalPdf417CostProof")
    historical_pdf417 = bool(proof) and not qualification.get("Pdf417CostProof")
    if proof:
        if (not historical_pdf417 and proof["Source"] != qualification["MeasurementSource"]) or len(proof["Cost"]) != 2:
            raise ValueError("PDF417 cost evidence differs from the qualified source")
        for domain in ("ccd0", "ccd1"):
            metadata = load(root / f"pdf417-cost-{domain}-result.json")["Metadata"]
            if (metadata["benchmark.CandidateSource"] != proof["Source"]
                    or metadata["benchmark.CandidateCoreSha256"] != proof["CoreSha256"]):
                raise ValueError("PDF417 cost packet differs from its recorded source")
        for cost in proof["Cost"]:
            row = {"domain": cost["Domain"].upper(), "previous_ms": f'{cost["BaselineMedianMs"]:.2f}',
                   "current_ms": f'{cost["CurrentMedianMs"]:.2f}', "ratio": f'{cost["LatencyRatio"]:.3f}',
                   "previous_managed_mib": f'{cost["BaselineAllocatedBytes"]/1048576:.2f}',
                   "current_managed_mib": f'{cost["CurrentAllocatedBytes"]/1048576:.2f}'}
            for source, prefix in (("Previous", "previous"), ("Current", "current")):
                values = cost[f"{source}CorrectRange"]
                row[f"{prefix}_correct"] = f"{values[0]:g}–{values[1]:g}" if values[0] != values[1] else f"{values[0]:g}"
                row[f"{prefix}_unfinished"] = cost[f"{source}InconclusiveBatches"]
            pdf417_cost.append(row)
    qr_alignment = {}
    alignment = qualification.get("QrAlignmentProof")
    if alignment:
        decisions = alignment["DecisionEquivalence"]
        if (alignment["Source"] != qualification["MeasurementSource"]
                or alignment["CoreSha256"] != qualification["CandidateCoreSha256"]
                or decisions["Mismatches"] != 0):
            raise ValueError("QR alignment proof differs from the qualified source")
        qr_alignment = {"score_cases": f'{decisions["ScoreCases"]:,}',
                        "accepted_scores": f'{decisions["AcceptedScores"]:,}',
                        "finder_cases": f'{decisions["FinderCases"]:,}',
                        "accepted_finders": f'{decisions["AcceptedFinders"]:,}',
                        "limits": alignment["Limits"]}
    datamatrix_deadline = {}
    matrix = qualification.get("DataMatrixDeadlineProof")
    if matrix:
        decisions = matrix["DecisionEquivalence"]
        if (matrix["Source"] != qualification["MeasurementSource"]
                or matrix["CoreSha256"] != qualification["CandidateCoreSha256"]
                or decisions["Mismatches"] != 0):
            raise ValueError("Data Matrix deadline proof differs from the qualified source")
        datamatrix_deadline = {"cases": f'{decisions["Cases"]:,}', "limits": matrix["Limits"], "cost_scope": matrix["CostScope"],
            "cost": [{"domain": row["Domain"].upper(), "previous_ms": f'{row["PreviousMedianMs"]:.3f}',
                      "current_ms": f'{row["CurrentMedianMs"]:.3f}', "ratio": f'{row["Ratio"]:.3f}',
                      "previous_bytes": f'{row["PreviousAllocatedBytes"]:,.0f}', "current_bytes": f'{row["CurrentAllocatedBytes"]:,.0f}'} for row in matrix["Cost"]]}
    return {"date": generation["StartedUtc"][:10], "source": qualification["MeasurementSource"],
            "versions": qualification["Versions"], "cpu": qualification["Cpu"]["Name"].strip(),
            "png_wins": png_wins, "png_cases": 6, "module_cpp_wins": module_cpp_wins,
            "png_qrc_allocation_wins": png_qrc_allocation_wins,
            "recognition": recognition, "formats": formats, "domains": domains,
            "format_scope_notes": [qualification["DataBarStackedDiscovery"]["PublicNote"]] if qualification.get("DataBarStackedDiscovery") else [],
            "negative_followup": negative_followup, "supplemental_pdf417": supplemental_rows,
            "supplemental_pdf417_limit": supplemental["Limit"] if supplemental else "",
            "retail": retail_rows, "retail_cost": retail_cost, "paired_decode_cost": paired_decode_rows,
            "pdf417_cost": pdf417_cost, "pdf417_cost_historical": historical_pdf417,
            "pdf417_cost_source": proof["Source"] if proof else "",
            "qr_alignment": qr_alignment, "datamatrix_deadline": datamatrix_deadline,
            "paired_decode_notes": qualification.get("PairedDecodeCost", {}).get("FollowupNotes", []),
            "supplemental_pdf417_timing_changes": ", ".join(supplemental.get("TimingSensitiveGains", [])) if supplemental else ""}


def write_website_data(evidence_root, website_root, check=False):
    path = website_root / "data" / "comparisons.json"
    encoded = (json.dumps(build_website_data(evidence_root), ensure_ascii=False, indent=2) + "\n").encode("utf-8")
    if check:
        if path.read_bytes() != encoded:
            raise ValueError("Website comparisons are stale; regenerate from the frozen evidence")
    else:
        path.write_bytes(encoded)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--evidence-root", type=Path, required=True)
    parser.add_argument("--website-root", type=Path, required=True)
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    write_website_data(args.evidence_root, args.website_root, args.check)
