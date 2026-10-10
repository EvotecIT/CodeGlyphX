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
                                "correct": f"{min(correct):g}–{max(correct):g}" if len(correct) > 1 else f"{min(correct):g}"})
        domains.append({"id": domain, "label": domain.upper(), "affinity": generation["Metadata"]["processAffinityMask"],
                        "generation": generation_rows, "decoding": decode_rows})
    return {"date": generation["StartedUtc"][:10], "source": qualification["MeasurementSource"],
            "versions": qualification["Versions"], "cpu": qualification["Cpu"]["Name"].strip(),
            "png_wins": png_wins, "png_cases": 6, "module_cpp_wins": module_cpp_wins,
            "png_qrc_allocation_wins": png_qrc_allocation_wins,
            "recognition": recognition, "formats": formats, "domains": domains}


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
