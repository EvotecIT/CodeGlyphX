"""Generate a current report from qualified PowerForge packets and every corpus outcome."""
import argparse
import hashlib
import json
from pathlib import Path


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--evidence-root", type=Path, required=True)
    parser.add_argument("--output-root", type=Path, required=True)
    args = parser.parse_args()
    root, output = args.evidence_root, args.output_root
    output.mkdir(parents=True, exist_ok=True)
    baseline = read(root / "qualified-baseline-corpus.json")
    current = read(root / "candidate-corpus-final.json")
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
    evidence = output / "evidence"
    evidence.mkdir(exist_ok=True)
    hashes = {}
    for name in names:
        # Normalize JSON formatting, leaving escaped payload line endings untouched.
        raw = (root / name).read_bytes().replace(b"\r\n", b"\n")
        (evidence / name).write_bytes(raw)
        hashes[name] = hashlib.sha256(raw).hexdigest()
    (evidence / "sha256.json").write_bytes((json.dumps(hashes, indent=2) + "\n").encode("utf-8"))
    lines = ["# Current barcode comparisons", "", "Generated from the complete result packets by `Build/generate-fair-comparison-report.py`.", "",
             "See [methodology](../fair-comparisons.md) for versions, adapter boundaries, settings, corpus selection, scoring and proof limits.", "",
             "## Recognition", "", "The 279-image corpus includes twelve negative images and eighteen images whose formats have no CodeGlyphX image scanner. Exact payload sets are checked after the documented physical representation rules.", "",
             "| Engine | Before: positive correct | Current: positive correct | Unsupported positive images | Unexpected-output images | Negative-image false positives | Inconclusive negative images |",
             "| --- | ---: | ---: | ---: | ---: | ---: | ---: |"]
    for engine in engines:
        old = [x for x in baseline if x["Engine"] == engine]
        new = [x for x in current if x["Engine"] == engine]
        if len(old) != 279 or len(new) != 279 or {x["Id"] for x in old} != {x["Id"] for x in new}:
            raise ValueError("Baseline/candidate corpus identities differ")
        lines.append(f'| {engine} | {sum(bool(x["Expected"]) and x["Status"] == "Correct" for x in old)}/267 | {sum(bool(x["Expected"]) and x["Status"] == "Correct" for x in new)}/267 | {sum(x["Status"] == "Unsupported" for x in new)} | {sum(bool(x["Unexpected"]) for x in new)} | {sum(not x["Expected"] and bool(x["Actual"]) for x in new)} | {sum(x["Status"] == "Inconclusive" for x in new)} |')
    lines += ["", "Unexpected output on a positive image may include an oracle limitation or an encoding-policy difference. Raw text is preserved; these counts must not be presented as a measured false-positive rate.", "",
              "| Format | Images | CodeGlyphX before | CodeGlyphX current | ZXing.Net | ZXing-C++ |", "| --- | ---: | ---: | ---: | ---: | ---: |"]
    for format in sorted({x["Format"] for x in current}):
        counts = [sum(x["Format"] == format and x["Status"] == "Correct" and x["Engine"] == engine for x in current) for engine in engines]
        before = sum(x["Format"] == format and x["Status"] == "Correct" and x["Engine"] == "CodeGlyphX" for x in baseline)
        n = sum(x["Format"] == format and x["Engine"] == "CodeGlyphX" for x in current)
        lines.append(f'| {format} | {n} | {before} | {counts[0]} | {counts[1]} | {counts[2]} |')
    old_by_id = {x["Id"]: x for x in baseline if x["Engine"] == "CodeGlyphX"}
    regressions = [x["Id"] for x in current if x["Engine"] == "CodeGlyphX" and x["Status"] != "Correct" and old_by_id[x["Id"]]["Status"] == "Correct"]
    lines += ["", "Previously correct images now missed: " + (", ".join(regressions) if regressions else "none in this run.")]
    negatives_before = read(root / "negative-baseline-corpus.json")
    negatives_current = read(root / "negative-candidate-corpus.json")
    lines += ["", "## Supplementary negative corpus", "",
              "All 25 images in the two upstream false-positive directories are checked, including the twelve in the main corpus. This supplementary check is reported separately. Empty output counts as a correct rejection only when scanning completes; an exhausted budget remains inconclusive.", "",
              "| Engine | Before: correct rejection | Current: correct rejection | Current: unexpected output | Current: inconclusive |",
              "| --- | ---: | ---: | ---: | ---: |"]
    for engine in engines:
        old = [x for x in negatives_before if x["Engine"] == engine]
        new = [x for x in negatives_current if x["Engine"] == engine]
        if len(old) != 25 or len(new) != 25 or any(x["Expected"] for x in old + new):
            raise ValueError("Supplementary negative corpus differs from its contract")
        lines.append(f'| {engine} | {sum(x["Status"] == "Correct" for x in old)}/25 | {sum(x["Status"] == "Correct" for x in new)}/25 | {sum(bool(x["Unexpected"]) for x in new)} | {sum(x["Status"] == "Inconclusive" for x in new)} |')
    for domain in ("ccd0", "ccd1"):
        packet = runs["generation", domain]
        metadata = packet["Metadata"]
        lines += ["", f"## Generation on {domain}", "", f'Source `{metadata.get("gitSha")}`, affinity `{metadata.get("processAffinityMask")}`, priority `{metadata.get("processPriority")}`. Twelve rotated samples after three warmups; all outliers retained. Each batch materializes 100 outputs.', "",
                  "| Payload | Operation | Engine | Median µs/output | P95 µs/output | Min–max µs/output | Managed bytes/output |",
                  "| --- | --- | --- | ---: | ---: | ---: | ---: |"]
        for x in packet["Summary"]:
            # milliseconds/batch * 1000 microseconds/millisecond / 100 outputs
            lines.append(f'| {x["Scenario"]} | {x["Operation"]} | {x["Engine"]} | {x["MedianMs"] * 10:.2f} | {x["P95Ms"] * 10:.2f} | {x["MinMs"] * 10:.2f}–{x["MaxMs"] * 10:.2f} | {x["Metrics"]["ManagedAllocatedBytes"] / 100:.0f} |')
    lines += ["", "## Decode latency", "", "The nineteen-image subset includes a negative, two unsupported formats and a four-QR image. Loading is excluded. These timings retain recognition failures and do not establish a speed ranking at equal accuracy. Other activity on the shared host can affect wall-clock measurements; read the sample distributions across both domains.", "",
              "| Domain | Engine | Median ms / 19 images | P95 ms | Min–max ms | Correct images range | Unexpected images range |",
              "| --- | --- | ---: | ---: | ---: | ---: | ---: |"]
    for domain in ("ccd0", "ccd1"):
        packet = runs["decoding", domain]
        for x in packet["Summary"]:
            samples = [s for s in packet["Samples"] if s["Engine"] == x["Engine"] and s["Iteration"] >= 0]
            correct = [s["Metrics"]["CorrectImages"] for s in samples]
            unexpected = [s["Metrics"]["UnexpectedImages"] for s in samples]
            lines.append(f'| {domain} | {x["Engine"]} | {x["MedianMs"]:.2f} | {x["P95Ms"]:.2f} | {x["MinMs"]:.2f}–{x["MaxMs"]:.2f} | {min(correct):g}–{max(correct):g} | {min(unexpected):g}–{max(unexpected):g} |')
    lines += ["", "All individual corpus outcomes and benchmark samples are in [evidence](evidence/sha256.json). Those files preserve the original environment/provenance metadata and untrimmed results. JSON formatting uses LF for portable evidence hashes; escaped payload line endings remain unchanged. Recorded run-input hashes identify the original measured files. The frozen manifest and acquisition script permit an independent rerun.", ""]
    (output / "results.md").write_text("\n".join(lines), encoding="utf-8")


if __name__ == "__main__":
    main()
