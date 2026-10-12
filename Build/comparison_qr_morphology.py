"""Validate and project the fixed QR morphology comparison for both public views."""
import json


def project_qr_morphology(root, qualification):
    proof = qualification.get("QrMorphologyCostProof")
    if not proof:
        return None
    if (proof["Source"] != qualification["MeasurementSource"]
            or proof["CoreSha256"] != qualification["CandidateCoreSha256"]
            or proof["PixelEquivalence"]["Mismatches"]):
        raise ValueError("QR morphology proof differs from the qualified core")
    rows = {}
    for kind in ("morphology", "negative"):
        projected = []
        for domain in ("ccd0", "ccd1"):
            packet = json.loads((root / f"qr-{kind}-cost-{domain}-result.json").read_text(encoding="utf-8-sig"))
            metadata = packet["Metadata"]
            if (metadata["benchmark.CandidateSource"] != proof["Source"]
                    or metadata["benchmark.CandidateCoreSha256"] != proof["CoreSha256"]
                    or metadata["benchmark.BaselineCoreSha256"] != proof["PreviousCoreSha256"]):
                raise ValueError("QR cost packet binary identity differs")
            lanes = {row["Engine"]: row for row in packet["Summary"]}
            if set(lanes) != {"Baseline", "Candidate"} or any(
                    row["Status"] != "Succeeded" or row["FailureCount"] or row["SampleCount"] != 12
                    for row in lanes.values()):
                raise ValueError("Incomplete QR cost evidence")
            before, current = lanes["Baseline"], lanes["Candidate"]
            row = {"domain": domain.upper(), "before_ms": f'{before["MedianMs"]:.2f}',
                   "current_ms": f'{current["MedianMs"]:.2f}', "ratio": f'{current["MedianMs"] / before["MedianMs"]:.3f}',
                   "before_mib": f'{before["Metrics"]["AllocatedBytes"] / 1048576:.2f}',
                   "current_mib": f'{current["Metrics"]["AllocatedBytes"] / 1048576:.2f}'}
            if kind == "negative":
                for engine, prefix in (("Baseline", "before"), ("Candidate", "current")):
                    samples = [sample for sample in packet["Samples"] if sample["Engine"] == engine and sample["Iteration"] >= 0]
                    if len(samples) != 12:
                        raise ValueError("Incomplete QR negative samples")
                    for metric, name in (("CorrectImages", "correct"), ("InconclusiveImages", "inconclusive")):
                        values = [sample["Metrics"][metric] for sample in samples]
                        row[f"{prefix}_{name}"] = f"{min(values):g}–{max(values):g}" if min(values) != max(values) else f"{min(values):g}"
                        row[f"{prefix}_{name}_mean"] = f"{sum(values) / len(values):.2f}"
            projected.append(row)
        rows[kind] = projected
    return {"changes": proof["Changes"], "scope": proof["Scope"], "limits": proof["Limits"],
            "kernel": rows["morphology"], "negatives": rows["negative"]}
