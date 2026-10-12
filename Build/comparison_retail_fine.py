"""Validate and project guarded retail recovery for the report and website."""
import json


def project_retail_fine(root, qualification):
    proof = qualification.get("RetailFineContrastProof")
    if not proof:
        return None
    if (proof["Source"] != qualification["MeasurementSource"]
            or proof["CoreSha256"] != qualification["CandidateCoreSha256"]):
        raise ValueError("Fine retail proof differs from the qualified core")
    industrial = qualification["IndustrialTileBoundaryProof"]
    if (industrial["Source"] != proof["Source"] or industrial["CoreSha256"] != proof["CoreSha256"]
            or industrial["RegressionBefore"]["Failed"] != 6
            or industrial["NegativeCases"] != {"net10": 8, "net8": 8, "net472": 8}
            or industrial["SingleDigitCases"] != {"net10": 6, "net8": 6, "net472": 6}
            or industrial["AlphaTileCases"] != {"net10": 12, "net8": 12, "net472": 12}
            or industrial["AlphaMargins"]["CoreSha256"] != proof["CoreSha256"]
            or industrial["AlphaMargins"]["CurrentOriginalPayloadContractsPassed"] != 12):
        raise ValueError("Industrial tile boundary proof differs from the qualified core")
    costs = []
    for kind, requests in (("positive", 87), ("negative", 9)):
        for domain in ("ccd0", "ccd1"):
            packet = json.loads((root / f"retail-fine-{kind}-{domain}-result.json").read_text(encoding="utf-8-sig"))
            metadata = packet["Metadata"]
            if (metadata["benchmark.CandidateSource"] != proof["Source"]
                    or metadata["benchmark.CandidateCoreSha256"] != proof["CoreSha256"]
                    or metadata["benchmark.BaselineCoreSha256"] != proof["PreviousCoreSha256"]
                    or metadata["processAffinityMask"] != ("0xFFFF" if domain == "ccd0" else "0xFFFF0000")
                    or metadata["processPriority"] != "Normal"):
                raise ValueError("Fine retail cost identity or placement differs")
            lanes = {r["Engine"]: r for r in packet["Summary"]}
            if set(lanes) != {"Baseline", "Candidate"} or any(
                    r["Status"] != "Succeeded" or r["FailureCount"] or r["SampleCount"] != 12
                    for r in lanes.values()):
                raise ValueError("Incomplete fine retail cost evidence")
            before, now = lanes["Baseline"], lanes["Candidate"]
            row = {"kind": kind.title(), "requests": requests, "domain": domain.upper(),
                   "before_ms": f'{before["MedianMs"]:.2f}', "current_ms": f'{now["MedianMs"]:.2f}',
                   "ratio": f'{now["MedianMs"] / before["MedianMs"]:.3f}',
                   "before_mib": f'{before["Metrics"]["AllocatedBytes"] / 1048576:.2f}',
                   "current_mib": f'{now["Metrics"]["AllocatedBytes"] / 1048576:.2f}'}
            for engine, prefix in (("Baseline", "before"), ("Candidate", "current")):
                samples = [r for r in packet["Samples"] if r["Engine"] == engine and r["Iteration"] >= 0]
                if len(samples) != 12 or any(r["Metrics"]["Requests"] != requests for r in samples):
                    raise ValueError("Incomplete fine retail request samples")
                for metric, name in (("CorrectImages", "correct"), ("InconclusiveImages", "inconclusive"),
                                     ("UnexpectedImages", "unexpected")):
                    values = [r["Metrics"][metric] for r in samples]
                    row[f"{prefix}_{name}"] = (f"{min(values):g}–{max(values):g}"
                                                if min(values) != max(values) else f"{min(values):g}")
            costs.append(row)
    return {"changes": proof["Changes"], "scope": proof["Scope"], "limits": proof["Limits"],
            "negative_variant_limits": proof["AdditionalNegativeVariants"]["Scope"],
            "industrial": {"changes": industrial["Changes"], "scope": industrial["Scope"], "limits": industrial["Limits"]},
            "cost_scope": proof["CostScope"], "costs": costs,
            "engines": [{"engine": name, "before": r["PreviousCorrect"], "correct": r["Correct"],
                         "unexpected": r["Unexpected"]} for name, r in proof["Engines"].items()],
            "negative_diagnostic": [{"engine": name, "completed": r["Completed"],
                                     "inconclusive": r["Inconclusive"], "unexpected": r["Unexpected"]}
                                    for name, r in proof["HintedNegativeDiagnostic"]["Engines"].items()]}
