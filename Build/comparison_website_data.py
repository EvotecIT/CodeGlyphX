"""Project qualified comparison evidence into the website's read-only view model."""
import argparse
import json
from pathlib import Path
from comparison_qr_morphology import project_qr_morphology
from comparison_databar_tilted import project_databar_tilted
from comparison_retail_fine import project_retail_fine
from comparison_scanner_boundaries import project_scanner_boundaries
from comparison_packet_identity import validate_current_packet, validate_qualified_whole_image_packet
from comparison_decode_allocations import project_decode_allocations
from comparison_decode_deadlines import project_decode_deadlines
from comparison_qr_finder_groups import project_qr_finder_groups
from comparison_dpm_gray8 import project_dpm_gray8
from comparison_dpm_window_means import project_dpm_window_means
from comparison_retained_contexts import project_retained_contexts
from comparison_pdf417_segment_cache import project_pdf417_segment_cache
from comparison_aztec_probe_cache import project_aztec_probe_cache
from comparison_qr_component_budget import project_qr_component_budget
from comparison_qr_foreground_bounds import project_qr_foreground_bounds
from comparison_qr_component_mask import project_qr_component_mask
from comparison_databar_character_recovery import project_databar_character_recovery
from comparison_databar_recovery_buffers import project_databar_recovery_buffers
from comparison_component_coordinates import project_component_coordinates
from comparison_databar_module_runs import project_databar_module_runs
from comparison_databar14_workspace import project_databar14_workspace
from comparison_databar_expanded_workspace import project_databar_expanded_workspace


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
        for kind, packet in (("generation", generation), ("decoding", decoding)):
            validate_current_packet(packet, qualification, kind, domain)
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
    databar14 = qualification.get("DataBar14RunRecoveryProof") or qualification.get("HistoricalDataBar14RunRecoveryProof")
    historical_databar14 = bool(databar14) and not qualification.get("DataBar14RunRecoveryProof")
    databar14_projected = None
    if databar14:
        if not historical_databar14 and (databar14["Source"] != qualification["MeasurementSource"] or databar14["CoreSha256"] != qualification["CandidateCoreSha256"]):
            raise ValueError("DataBar-14 recovery differs from the qualified core")
        databar14_projected = {"historical": historical_databar14, "source": databar14["Source"], "changes": databar14["Changes"], "scope": databar14["Scope"], "cost_scope": databar14["CostScope"],
            "rows": [{"engine": engine, "before": databar14["Engines"][engine]["PreviousCorrect"],
                      "current": databar14["Engines"][engine]["Correct"], "unexpected": databar14["Engines"][engine]["Unexpected"]}
                     for engine in engines],
            "cost": [{"domain": row["Domain"].upper(), "before_ms": f'{row["BaselineMedianMs"]:.2f}',
                      "current_ms": f'{row["CurrentMedianMs"]:.2f}', "ratio": f'{row["LatencyRatio"]:.3f}',
                      "before_correct": f'{row["PreviousCorrectRequests"]:g}', "current_correct": f'{row["CurrentCorrectRequests"]:g}',
                      "before_mib": f'{row["BaselineAllocatedBytes"]/1048576:.2f}', "current_mib": f'{row["CurrentAllocatedBytes"]/1048576:.2f}'}
                     for row in databar14["Cost"]]}
    retail = qualification.get("RetailRecoveryProof") or qualification.get("HistoricalRetailRecoveryProof")
    historical_retail = bool(retail) and not qualification.get("RetailRecoveryProof")
    retail_rows, retail_cost, retail_positive_cost = [], [], []
    if retail:
        if not historical_retail and retail["Source"] != qualification["MeasurementSource"]:
            raise ValueError("Retail diagnostics differ from the qualified source")
        for engine in engines:
            positive, negative = (retail["Engines"][engine][kind] for kind in ("positive", "negative"))
            retail_rows.append({"engine": engine, "positive_before": positive["PreviousCorrect"],
                                "positive": positive["Correct"], "positive_unexpected": positive["Unexpected"],
                                "negative_before": negative["PreviousCorrect"], "negative": negative["Correct"],
                                "negative_unexpected": negative["Unexpected"], "inconclusive": negative["Inconclusive"]})
        for key, projected in (("Cost", retail_cost), ("PositiveCost", retail_positive_cost)):
            for row in retail.get(key, []):
                projected.append({"domain": row["Domain"].upper(), "before_ms": f'{row["BaselineMedianMs"]:.2f}',
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
            validate_qualified_whole_image_packet(packet, qualification, domain)
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
    maxicode_image = {}
    maxi = qualification.get("MaxiCodeImageProof")
    if maxi:
        if maxi["Source"] != qualification["MeasurementSource"] or maxi["CoreSha256"] != qualification["CandidateCoreSha256"]:
            raise ValueError("MaxiCode proof differs from the qualified source")
        maxicode_image = {"correct": maxi["Correct"], "images": maxi["Images"], "before": maxi["PreviousCorrect"],
                          "limits": maxi["Limits"], "evidence": maxi["Evidence"], "text_contract": maxi["TextContract"]}
    pdf417_cache = {}
    historical_cache = not qualification.get("Pdf417CacheCostProof")
    cache = qualification.get("Pdf417CacheCostProof") or qualification.get("HistoricalPdf417CacheCostProof")
    if cache:
        decisions = cache["DecisionEquivalence"]
        if (decisions["Mismatches"] != 0 or (not historical_cache and
                (cache["Source"] != qualification["MeasurementSource"]
                 or cache["CoreSha256"] != qualification["CandidateCoreSha256"]))):
            raise ValueError("PDF417 cache proof differs from the qualified source")
        old_cache = cache["InstrumentedBaseline"]["LargeCaches"][0]
        new_cache = cache["InstrumentedCandidate"]["LargeCaches"][0]
        pdf417_cache = {"historical": historical_cache, "source": cache["Source"], "source_short": cache["Source"][:8], "cases": f'{decisions["Cases"]:,}', "previous_lines": f'{old_cache["Entries"]:,}',
                        "current_lines": f'{new_cache["Entries"]:,}', "previous_rejections": f'{old_cache["EntryRejections"]:,}',
                        "previous_pixels": f'{old_cache["SampledPixels"]:,}', "current_pixels": f'{new_cache["SampledPixels"]:,}',
                        "limits": cache["Limits"],
                        "cost": [{"domain": row["Domain"].upper(), "previous_ms": f'{row["PreviousMedianMs"]:.3f}',
                                  "current_ms": f'{row["CurrentMedianMs"]:.3f}', "ratio": f'{row["LatencyRatio"]:.3f}',
                                  "previous_mib": f'{row["PreviousMeanAllocatedBytes"] / 1048576:.2f}',
                                  "current_mib": f'{row["CurrentMeanAllocatedBytes"] / 1048576:.2f}',
                                  "previous_correct": f'{row["PreviousMetrics"]["CorrectImages"]:.2f}',
                                  "current_correct": f'{row["CurrentMetrics"]["CorrectImages"]:.2f}',
                                  "previous_unfinished": f'{row["PreviousMetrics"]["InconclusiveImages"]:.2f}',
                                  "current_unfinished": f'{row["CurrentMetrics"]["InconclusiveImages"]:.2f}'} for row in cache["PairedControls"]]}
    codabar_run_recovery = {}
    codabar = qualification.get("CodabarRunRecoveryProof") or qualification.get("HistoricalCodabarRunRecoveryProof")
    historical_codabar = codabar is not None and not qualification.get("CodabarRunRecoveryProof")
    if codabar:
        if not historical_codabar and (codabar["Source"] != qualification["MeasurementSource"] or codabar["CoreSha256"] != qualification["CandidateCoreSha256"]):
            raise ValueError("Codabar proof differs from the qualified source")
        codabar_run_recovery = {"historical": historical_codabar, "source": codabar["Source"], "source_short": codabar["Source"][:8], "before": codabar["MainCorpus"]["PreviousCorrect"], "correct": codabar["MainCorpus"]["Correct"],
                               "images": codabar["MainCorpus"]["Images"], "limits": codabar["Limits"], "rows": []}
        for engine, row in codabar["Diagnostics"]["diagnostic"].items():
            old, now = row["Previous"], row["Current"]
            sup = codabar["Diagnostics"]["supplemental"][engine]
            codabar_run_recovery["rows"].append({"engine": engine, "before": old["CorrectPositive"], "correct": now["CorrectPositive"],
                "negative_before": old["CorrectNegative"], "negative_correct": now["CorrectNegative"],
                "supplemental_before": sup["Previous"]["CorrectPositive"], "supplemental_correct": sup["Current"]["CorrectPositive"],
                "unexpected": now["UnexpectedImages"], "supplemental_unexpected": sup["Current"]["UnexpectedImages"]})
    qr_exact_grid = {}
    exact = qualification.get("QrExactGridRecoveryProof") or qualification.get("HistoricalQrExactGridRecoveryProof")
    historical_exact = exact is not None and not qualification.get("QrExactGridRecoveryProof")
    if exact:
        if not historical_exact and (exact["Source"] != qualification["MeasurementSource"] or exact["CoreSha256"] != qualification["CandidateCoreSha256"]):
            raise ValueError("QR exact-grid proof differs from the qualified source")
        qr_exact_grid = {"historical": historical_exact, "source": exact["Source"], "source_short": exact["Source"][:8],
                         "rechecked": qualification.get("QrExactGridRecheck", {}).get("Correct", exact["Correct"]),
                         "before": exact["PreviousCorrect"], "correct": exact["Correct"], "images": len(exact["Images"]),
                         "focused_cases": exact["FocusedCases"], "limits": exact["Limits"], "legacy_scope": exact["LegacyScope"]}
    pdf417_negative_completion = {}
    completion = qualification.get("Pdf417NegativeCompletionProof") or qualification.get("HistoricalPdf417NegativeCompletionProof")
    historical_completion = completion is not None and not qualification.get("Pdf417NegativeCompletionProof")
    if completion:
        if (not historical_completion and (completion["Source"] != qualification["MeasurementSource"]
                or completion["CoreSha256"] != qualification["CandidateCoreSha256"])) or completion["DecisionEquivalence"]["mismatches"] != 0:
            raise ValueError("PDF417 negative-search proof differs from the qualified source")
        pdf417_negative_completion = {"historical": historical_completion, "source": completion["Source"], "checks": f'{completion["DecisionEquivalence"]["checks"]:,}',
            "changes": completion["Changes"], "notes": completion["CompletionNotes"], "scope": completion["CostScope"], "limits": completion["Limits"],
            "cost": [{"domain": row["Domain"].upper(), "previous_ms": f'{row["BaselineMedianMs"]:.3f}', "current_ms": f'{row["CurrentMedianMs"]:.3f}',
                      "ratio": f'{row["LatencyRatio"]:.3f}', "previous_mib": f'{row["BaselineAllocatedBytes"] / 1048576:.2f}', "current_mib": f'{row["CurrentAllocatedBytes"] / 1048576:.2f}',
                      "previous_correct": f'{row["PreviousCorrectRequests"]:.3f}', "current_correct": f'{row["CurrentCorrectRequests"]:.3f}'} for row in completion["Cost"]]}
    negative_search_cost = {}
    negative_cost = qualification.get("NegativeSearchCostProof")
    if negative_cost:
        decisions = negative_cost["DecisionEquivalence"]
        if (negative_cost["Source"] != qualification["MeasurementSource"]
                or negative_cost["CoreSha256"] != qualification["CandidateCoreSha256"]
                or decisions["Mismatches"] != 0):
            raise ValueError("Negative-search cost proof differs from the qualified source")
        negative_search_cost = {"outputs": decisions["PreprocessingOutputs"],
                               "configurations": decisions["PreprocessingConfigurations"],
                               "guard_cases": f'{decisions["GuardCases"]:,}',
                               "accepted_guards": f'{decisions["AcceptedGuards"]:,}'}
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
    itf_recovery = {}
    recovery = qualification.get("ItfRunRecoveryProof") or qualification.get("HistoricalItfRunRecoveryProof")
    historical_itf = bool(recovery) and not qualification.get("ItfRunRecoveryProof")
    if recovery:
        if (not historical_itf and (recovery["Source"] != qualification["MeasurementSource"] or recovery["CoreSha256"] != qualification["CandidateCoreSha256"])) or recovery["WidthModelEquivalence"]["Mismatches"]:
            raise ValueError("ITF proof differs from the qualified source")
        itf_recovery = {"historical": historical_itf, "source": recovery["Source"], "changes": recovery["Changes"], "scope": recovery["Scope"], "limits": recovery["Limits"],
            "cases": f'{recovery["WidthModelEquivalence"]["Cases"]:,}',
            "outcomes": [{"engine": engine, "previous_positive": values["positive"]["previous"]["Correct"], "current_positive": values["positive"]["current"]["Correct"],
                          "previous_negative": values["negative"]["previous"]["Correct"], "current_negative": values["negative"]["current"]["Correct"], "unexpected": values["negative"]["current"]["Unexpected"]} for engine, values in recovery["Engines"].items()],
            "cost": [{"domain": row["Domain"].upper(), "previous_ms": f'{row["PreviousMedianMs"]:.3f}', "current_ms": f'{row["CurrentMedianMs"]:.3f}',
                      "ratio": f'{row["LatencyRatio"]:.3f}', "previous_mib": f'{row["PreviousAllocatedBytes"]/1048576:.2f}', "current_mib": f'{row["CurrentAllocatedBytes"]/1048576:.2f}',
                      "previous_correct": f'{row["PreviousCorrect"]:.3f}', "current_correct": f'{row["CurrentCorrect"]:.3f}'} for row in recovery["Cost"]]}
    code39_recovery = {}
    recovery = qualification.get("Code39RunRecoveryProof") or qualification.get("HistoricalCode39RunRecoveryProof")
    historical_code39 = bool(recovery) and not qualification.get("Code39RunRecoveryProof")
    if recovery:
        if (not historical_code39 and (recovery["Source"] != qualification["MeasurementSource"] or recovery["CoreSha256"] != qualification["CandidateCoreSha256"])) or recovery["NormalizerEquivalence"]["Mismatches"]:
            raise ValueError("Code 39 proof differs from the qualified source")
        code39_recovery = {"historical": historical_code39, "source": recovery["Source"], "changes": recovery["Changes"], "scope": recovery["Scope"], "limits": recovery["Limits"],
            "cases": f'{recovery["NormalizerEquivalence"]["Cases"]:,}',
            "outcomes": [{"engine": engine, "previous_positive": values["positive"]["previous"]["Correct"], "current_positive": values["positive"]["current"]["Correct"],
                          "previous_negative": values["negative"]["previous"]["Correct"], "current_negative": values["negative"]["current"]["Correct"], "unexpected": values["negative"]["current"]["Unexpected"]} for engine, values in recovery["Engines"].items()],
            "cost": [{"domain": row["Domain"].upper(), "previous_ms": f'{row["PreviousMedianMs"]:.3f}', "current_ms": f'{row["CurrentMedianMs"]:.3f}',
                      "ratio": f'{row["LatencyRatio"]:.3f}', "previous_mib": f'{row["PreviousAllocatedBytes"]/1048576:.2f}', "current_mib": f'{row["CurrentAllocatedBytes"]/1048576:.2f}',
                      "previous_correct": f'{row["PreviousCorrect"]:.3f}', "current_correct": f'{row["CurrentCorrect"]:.3f}'} for row in recovery["Cost"]]}
    datamatrix_spatial = {}
    spatial = qualification.get("DataMatrixSpatialSearchProof") or qualification.get("HistoricalDataMatrixSpatialSearchProof")
    historical_spatial = spatial is not None and not qualification.get("DataMatrixSpatialSearchProof")
    if spatial:
        if (not historical_spatial and (spatial["Source"] != qualification["MeasurementSource"]
                or spatial["CoreSha256"] != qualification["CandidateCoreSha256"])) or spatial["BoundsEquivalence"]["Mismatches"] or spatial["ComponentEquivalence"]["Mismatches"]:
            raise ValueError("Data Matrix spatial proof differs from the qualified source")
        datamatrix_spatial = {"historical": historical_spatial, "source": spatial["Source"], "changes": spatial["Changes"], "bounds_cases": f'{spatial["BoundsEquivalence"]["Cases"]:,}',
            "component_cases": f'{spatial["ComponentEquivalence"]["Cases"]:,}', "scope": spatial["CostScope"], "selected_scope": spatial["SelectedNegativeScope"], "limits": spatial["Limits"],
            "cost": [{"domain": row["Domain"].upper(), "previous_ms": f'{row["BaselineMedianMs"]:.3f}', "current_ms": f'{row["CurrentMedianMs"]:.3f}',
                      "ratio": f'{row["LatencyRatio"]:.3f}', "previous_mib": f'{row["BaselineAllocatedBytes"]/1048576:.2f}', "current_mib": f'{row["CurrentAllocatedBytes"]/1048576:.2f}',
                      "previous_correct": f'{row["PreviousCorrectRequests"]:.3f}', "current_correct": f'{row["CurrentCorrectRequests"]:.3f}'} for row in spatial["Cost"]],
            "selected_notes": [f'{row["Domain"].upper()}: the three additional negatives take {row["BaselineMedianMs"]:.3f} → {row["CurrentMedianMs"]:.3f} ms (ratio {row["LatencyRatio"]:.3f}), with mean completed images {row["PreviousCorrectRequests"]:.3f} → {row["CurrentCorrectRequests"]:.3f}/3 and managed allocation {row["BaselineAllocatedBytes"]/1048576:.2f} → {row["CurrentAllocatedBytes"]/1048576:.2f} MiB.' for row in spatial["SelectedNegativeCost"]]}
    return {"date": generation["StartedUtc"][:10], "source": qualification["MeasurementSource"],
            "versions": qualification["Versions"], "cpu": qualification["Cpu"]["Name"].strip(),
            "host_load_note": qualification.get("CurrentHostLoadNote", ""),
            "png_wins": png_wins, "png_cases": 6, "module_cpp_wins": module_cpp_wins,
            "png_qrc_allocation_wins": png_qrc_allocation_wins,
            "recognition": recognition, "formats": formats, "domains": domains,
            "format_scope_notes": [qualification["DataBarStackedDiscovery"]["PublicNote"]] if qualification.get("DataBarStackedDiscovery") else [],
            "negative_followup": negative_followup, "supplemental_pdf417": supplemental_rows,
            "supplemental_pdf417_limit": supplemental["Limit"] if supplemental else "",
            "retail": retail_rows, "retail_cost": retail_cost, "retail_positive_cost": retail_positive_cost,
            "retail_historical": historical_retail, "retail_source": retail["Source"] if retail else "",
            "qr_morphology": project_qr_morphology(root, qualification),
            "databar_tilted": project_databar_tilted(root, qualification),
            "retail_fine": project_retail_fine(root, qualification),
            "scanner_boundaries": project_scanner_boundaries(root, qualification),
            "databar_expanded_workspace": project_databar_expanded_workspace(root, qualification),
            "databar14_workspace": project_databar14_workspace(root, qualification),
            "databar_module_runs": project_databar_module_runs(root, qualification),
            "component_coordinates": project_component_coordinates(root, qualification),
            "databar_recovery_buffers": project_databar_recovery_buffers(root, qualification),
            "databar_character_recovery": project_databar_character_recovery(root, qualification),
            "qr_component_mask": project_qr_component_mask(root, qualification),
            "qr_foreground_bounds": project_qr_foreground_bounds(root, qualification),
            "qr_component_budget": project_qr_component_budget(root, qualification),
            "aztec_probe_cache": project_aztec_probe_cache(root, qualification),
            "pdf417_segment_cache": project_pdf417_segment_cache(root, qualification),
            "retained_contexts": project_retained_contexts(root, qualification),
            "paired_decode_scope": qualification.get("PairedDecodeCost", {}).get("Scope", ""),
            "dpm_window_means": project_dpm_window_means(root, qualification),
            "dpm_gray8": project_dpm_gray8(root, qualification),
            "qr_finder_groups": project_qr_finder_groups(root, qualification),
            "decode_deadlines": project_decode_deadlines(root, qualification),
            "decode_allocations": project_decode_allocations(root, qualification),
            "retail_changes": retail.get("Changes", "") if retail else "",
            "databar14_recovery": databar14_projected,
            "retail_extrema_reuse": ({"masks": f'{retail["ExtremaReuse"]["Masks"]:,}',
                                      "pixels": f'{retail["ExtremaReuse"]["PixelDecisions"]:,}',
                                      "rows": f'{retail["ExtremaReuse"]["Rows"]:,}'}
                                     if retail and retail.get("ExtremaReuse") else None),
            "retail_positive_cost_scope": retail.get("PositiveCostScope", "") if retail else "", "paired_decode_cost": paired_decode_rows,
            "pdf417_cost": pdf417_cost, "pdf417_cost_historical": historical_pdf417,
            "pdf417_cost_source": proof["Source"] if proof else "",
            "qr_alignment": qr_alignment, "datamatrix_deadline": datamatrix_deadline, "maxicode_image": maxicode_image,
            "negative_search_cost": negative_search_cost, "pdf417_negative_completion": pdf417_negative_completion, "datamatrix_spatial": datamatrix_spatial, "code39_recovery": code39_recovery, "itf_recovery": itf_recovery,
            "pdf417_cache": pdf417_cache, "codabar_run_recovery": codabar_run_recovery, "qr_exact_grid": qr_exact_grid,
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
