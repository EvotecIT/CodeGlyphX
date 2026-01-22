#!/usr/bin/env python3
import argparse
import csv
import datetime as dt
import os
import platform
import re
from pathlib import Path


TITLE_MAP = {
    "QrCodeBenchmarks": "QR (Encode)",
    "QrDecodeBenchmarks": "QR (Decode)",
    "BarcodeBenchmarks": "1D Barcodes (Encode)",
    "MatrixCodeBenchmarks": "2D Matrix Codes (Encode)",
    "QrCompareBenchmarks": "QR (Encode)",
    "QrDecodeCleanCompareBenchmarks": "QR Decode (Clean)",
    "QrDecodeNoisyCompareBenchmarks": "QR Decode (Noisy)",
    "Code128CompareBenchmarks": "Code 128 (Encode)",
    "Code39CompareBenchmarks": "Code 39 (Encode)",
    "Code93CompareBenchmarks": "Code 93 (Encode)",
    "EanCompareBenchmarks": "EAN-13 (Encode)",
    "UpcACompareBenchmarks": "UPC-A (Encode)",
    "DataMatrixCompareBenchmarks": "Data Matrix (Encode)",
    "Pdf417CompareBenchmarks": "PDF417 (Encode)",
    "AztecCompareBenchmarks": "Aztec (Encode)",
}

VENDOR_ORDER = ["CodeGlyphX", "ZXing.Net", "QRCoder", "Barcoder"]


def normalize_method(value: str) -> str:
    value = (value or "").strip()
    if len(value) >= 2 and value[0] == "'" and value[-1] == "'":
        return value[1:-1]
    return value


def normalize_mean_text(value: str) -> str:
    if not value:
        return value
    return (
        value.replace("µs", "μs")
        .replace("�s", "μs")
    )


def normalize_compare_scenario(value: str) -> str:
    mapping = {
        "EAN PNG": "EAN-13 PNG",
        "QR Decode (clean, balanced)": "QR Decode (clean)",
        "QR Decode (noisy, robust)": "QR Decode (noisy)",
        "QR Decode (noisy, try harder)": "QR Decode (noisy)",
    }
    return mapping.get(value, value)


def detect_os_name() -> str:
    name = platform.system().lower()
    if name.startswith("win"):
        return "windows"
    if name.startswith("linux"):
        return "linux"
    if name.startswith("darwin"):
        return "macos"
    return "unknown"


def resolve_os_name(artifacts_path: Path, override: str | None) -> str:
    if override:
        return override.lower()
    leaf = artifacts_path.name.lower()
    for candidate in ("windows", "linux", "macos"):
        if leaf.startswith(f"{candidate}-"):
            return candidate
    return detect_os_name()


def load_csv_rows(path: Path):
    with path.open(newline="", encoding="utf-8") as f:
        sample = f.read(2048)
        f.seek(0)
        try:
            dialect = csv.Sniffer().sniff(sample, delimiters=";,")
        except csv.Error:
            dialect = csv.get_dialect("excel")
        reader = csv.DictReader(f, dialect=dialect)
        return list(reader)


def format_run_mode(run_mode: str) -> str:
    if run_mode == "quick":
        return "Run mode: Quick (warmupCount=1, iterationCount=3, invocationCount=1)."
    return "Run mode: Full (BenchmarkDotNet default job settings)."


def resolve_run_mode(run_mode: str | None) -> str:
    if run_mode:
        return run_mode
    return "quick" if os.environ.get("BENCH_QUICK") == "true" else "full"


def build_section(artifacts_path: Path, framework: str, configuration: str, run_mode: str) -> str:
    results_path = artifacts_path / "results"
    if not results_path.exists():
        raise SystemExit(f"Results folder not found: {results_path}")

    baseline_files = sorted(results_path.glob("*-report.csv"))
    baseline_files = [p for p in baseline_files if "Compare" not in p.name]
    compare_files = sorted(results_path.glob("*-report.csv"))
    compare_files = [p for p in compare_files if "Compare" in p.name]

    os_name = detect_os_name()
    timestamp = dt.datetime.now(dt.timezone.utc).strftime("%Y-%m-%d %H:%M:%S UTC")

    lines = []
    lines.append(f"## {os_name.upper()}")
    lines.append("")
    lines.append(f"Updated: {timestamp}")
    lines.append(f"Framework: {framework}")
    lines.append(f"Configuration: {configuration}")
    lines.append(f"Artifacts: {artifacts_path}")
    lines.append("Notes:")
    lines.append(f"- {format_run_mode(run_mode)}")
    lines.append("- Comparisons target PNG output and include encode+render (not encode-only).")
    lines.append("- Module size and quiet zone are matched to CodeGlyphX defaults where possible; image size is derived from CodeGlyphX modules.")
    lines.append("- ZXing.Net uses ZXing.Net.Bindings.ImageSharp.V3 (ImageSharp 3.x renderer).")
    lines.append("- Barcoder uses Barcoder.Renderer.Image (ImageSharp renderer).")
    lines.append("- QRCoder uses PngByteQRCode (managed PNG output, no external renderer).")
    lines.append("- QR decode comparisons use raw RGBA32 bytes (ZXing via RGBLuminanceSource).")
    lines.append("- QR decode clean uses CodeGlyphX Balanced; noisy uses CodeGlyphX Robust with aggressive sampling/limits; ZXing uses default (clean) and TryHarder (noisy).")
    lines.append("")

    if compare_files:
        summary_rows = []
        for path in compare_files:
            rows = load_csv_rows(path)
            if not rows:
                continue
            base_name = path.stem.replace("CodeGlyphX.Benchmarks.", "").replace("-report", "")
            title = TITLE_MAP.get(base_name, base_name)

            scenario_map = {}
            for row in rows:
                method = normalize_method(row.get("Method", ""))
                if not method:
                    continue
                vendor = "Unknown"
                scenario = method
                match = re.match(r"^(CodeGlyphX|ZXing\.Net|QRCoder|Barcoder)\s+(.*)$", method)
                if match:
                    vendor = match.group(1)
                    scenario = match.group(2)
                scenario = normalize_compare_scenario(scenario)
                mean_text = normalize_mean_text(row.get("Mean", ""))
                scenario_map.setdefault(scenario, {})[vendor] = {
                    "mean": mean_text,
                    "meanNs": parse_mean_to_ns(mean_text),
                    "allocated": row.get("Allocated", ""),
                }

            for scenario in sorted(scenario_map.keys()):
                vendors = scenario_map[scenario]
                fastest_vendor = None
                fastest = None
                for vendor, entry in vendors.items():
                    mean_ns = entry.get("meanNs")
                    if not mean_ns:
                        continue
                    if fastest is None or mean_ns < fastest["meanNs"]:
                        fastest = entry
                        fastest_vendor = vendor
                if not fastest_vendor:
                    continue
                cgx = vendors.get("CodeGlyphX")
                ratio_text = ""
                cgx_mean = ""
                cgx_alloc = ""
                if cgx and cgx.get("meanNs"):
                    ratio_text = f"{round(cgx['meanNs'] / fastest['meanNs'], 2)} x"
                    cgx_mean = cgx.get("mean", "")
                    cgx_alloc = cgx.get("allocated", "")
                summary_rows.append(
                    f"| {title} | {scenario} | {fastest_vendor} {fastest.get('mean','')} | {ratio_text} | {cgx_mean} | {cgx_alloc} |"
                )

        if summary_rows:
            lines.append("### Summary (Comparisons)")
            lines.append("")
            lines.append("| Benchmark | Scenario | Fastest | CodeGlyphX vs Fastest | CodeGlyphX Mean | CodeGlyphX Alloc |")
            lines.append("| --- | --- | --- | --- | --- | --- |")
            lines.extend(summary_rows)
            lines.append("")

    if baseline_files:
        lines.append("### Baseline")
        lines.append("")
        for path in baseline_files:
            rows = load_csv_rows(path)
            if not rows:
                continue
            base_name = path.stem.replace("CodeGlyphX.Benchmarks.", "").replace("-report", "")
            title = TITLE_MAP.get(base_name, base_name)
            lines.append(f"#### {title}")
            lines.append("")
            lines.append("| Scenario | Mean | Allocated |")
            lines.append("| --- | --- | --- |")
            for row in rows:
                scenario = normalize_method(row.get("Method", ""))
                mean_text = normalize_mean_text(row.get("Mean", ""))
                lines.append(f"| {scenario} | {mean_text} | {row.get('Allocated','')} |")
            lines.append("")

    if compare_files:
        lines.append("### Comparisons")
        lines.append("")
        for path in compare_files:
            rows = load_csv_rows(path)
            if not rows:
                continue
            base_name = path.stem.replace("CodeGlyphX.Benchmarks.", "").replace("-report", "")
            title = TITLE_MAP.get(base_name, base_name)
            lines.append(f"#### {title}")
            lines.append("")
            lines.append(
                "| Scenario | CodeGlyphX (Mean / Alloc) | ZXing.Net (Mean / Alloc) | QRCoder (Mean / Alloc) | Barcoder (Mean / Alloc) |"
            )
            lines.append("| --- | --- | --- | --- | --- |")

            scenarios = {}
            for row in rows:
                method = normalize_method(row.get("Method", ""))
                vendor = "Unknown"
                scenario = method
                match = re.match(r"^(CodeGlyphX|ZXing\.Net|QRCoder|Barcoder)\s+(.*)$", method)
                if match:
                    vendor = match.group(1)
                    scenario = match.group(2)
                scenario = normalize_compare_scenario(scenario)
                scenarios.setdefault(scenario, {})[vendor] = row

            for scenario in sorted(scenarios.keys()):
                group = scenarios[scenario]
                def cell(vendor):
                    item = group.get(vendor)
                    if not item:
                        return ""
                    mean = normalize_mean_text(item.get("Mean", ""))
                    allocated = item.get("Allocated", "")
                    return f"{mean}<br>{allocated}"
                lines.append(
                    f"| {scenario} | {cell('CodeGlyphX')} | {cell('ZXing.Net')} | {cell('QRCoder')} | {cell('Barcoder')} |"
                )
            lines.append("")

    return "\n".join(lines).rstrip()


def build_template(blocks):
    return "\n".join(
        [
            "# Benchmarks",
            "",
            blocks["windows"],
            "",
            blocks["linux"],
            "",
            blocks["macos"],
            "",
        ]
    )


def extract_block(text: str, os_name: str):
    marker = f"BENCHMARK:{os_name.upper()}"
    start = f"<!-- {marker}:START -->"
    end = f"<!-- {marker}:END -->"
    pattern = re.compile(re.escape(start) + r"[\s\S]*?" + re.escape(end))
    match = pattern.search(text)
    if match:
        return match.group(0)
    return f"{start}\n_no results yet_\n{end}"


def update_section(path: Path, section: str, os_name: str):
    marker = f"BENCHMARK:{os_name.upper()}"
    start = f"<!-- {marker}:START -->"
    end = f"<!-- {marker}:END -->"
    block = f"{start}\n{section}\n{end}"
    text = path.read_text(encoding="utf-8") if path.exists() else ""
    blocks = {
        "windows": extract_block(text, "windows"),
        "linux": extract_block(text, "linux"),
        "macos": extract_block(text, "macos"),
    }
    blocks[os_name] = block
    path.write_text(build_template(blocks), encoding="utf-8")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--artifacts-path", required=True)
    parser.add_argument("--output", default=None)
    parser.add_argument("--framework", default="net8.0")
    parser.add_argument("--configuration", default="Release")
    parser.add_argument("--run-mode", default=None, choices=["quick", "full"])
    parser.add_argument("--os-name", default=None, choices=["windows", "linux", "macos"])
    args = parser.parse_args()

    artifacts_path = Path(args.artifacts_path).resolve()
    output_path = Path(args.output).resolve() if args.output else Path(__file__).resolve().parent.parent / "BENCHMARK.md"

    os_name = resolve_os_name(artifacts_path, args.os_name)
    run_mode = resolve_run_mode(args.run_mode)
    section = build_section(artifacts_path, args.framework, args.configuration, run_mode)
    update_section(output_path, section, os_name)

    json_path = output_path.parent / "BENCHMARK.json"
    write_json(json_path, artifacts_path, args.framework, args.configuration, os_name, run_mode)
    repo_root = Path(__file__).resolve().parent.parent
    assets_json = repo_root / "Assets" / "Data" / "benchmark.json"
    assets_json.parent.mkdir(parents=True, exist_ok=True)
    assets_json.write_text(json_path.read_text(encoding="utf-8"), encoding="utf-8")


def parse_mean_to_ns(value: str):
    if not value:
        return None
    cleaned = value.strip().replace(",", "")
    if cleaned == "NA":
        return None
    match = re.match(r"^([0-9]+(?:\\.[0-9]+)?)\\s*(ns|us|μs|ms|s)$", cleaned)
    if not match:
        return None
    number = float(match.group(1))
    unit = match.group(2)
    scale = {
        "ns": 1.0,
        "us": 1000.0,
        "μs": 1000.0,
        "ms": 1_000_000.0,
        "s": 1_000_000_000.0,
    }.get(unit, 1.0)
    return number * scale


def write_json(
    path: Path,
    artifacts_path: Path,
    framework: str,
    configuration: str,
    os_name: str,
    run_mode: str,
):
    results_path = artifacts_path / "results"
    if not results_path.exists():
        raise SystemExit(f"Results folder not found: {results_path}")

    baseline_files = sorted(results_path.glob("*-report.csv"))
    baseline_files = [p for p in baseline_files if "Compare" not in p.name]
    compare_files = sorted(results_path.glob("*-report.csv"))
    compare_files = [p for p in compare_files if "Compare" in p.name]

    baseline = []
    for file in baseline_files:
        rows = load_csv_rows(file)
        if not rows:
            continue
        base_name = file.stem.replace("CodeGlyphX.Benchmarks.", "").replace("-report", "")
        title = TITLE_MAP.get(base_name, base_name)
        items = []
        for row in rows:
            mean = row.get("Mean", "")
            items.append(
                {
                    "name": normalize_method(row.get("Method", "")),
                    "mean": mean,
                    "meanNs": parse_mean_to_ns(mean),
                    "allocated": row.get("Allocated", ""),
                }
            )
        baseline.append({"id": base_name, "title": title, "scenarios": items})

    comparisons = []
    for file in compare_files:
        rows = load_csv_rows(file)
        if not rows:
            continue
        base_name = file.stem.replace("CodeGlyphX.Benchmarks.", "").replace("-report", "")
        title = TITLE_MAP.get(base_name, base_name)
        scenario_map = {}
        for row in rows:
            method = normalize_method(row.get("Method", ""))
            vendor = "Unknown"
            scenario = method
            match = re.match(r"^(CodeGlyphX|ZXing\.Net|QRCoder|Barcoder)\s+(.*)$", method)
            if match:
                vendor = match.group(1)
                scenario = match.group(2)
            scenario = normalize_compare_scenario(scenario)
            mean_text = normalize_mean_text(row.get("Mean", ""))
            scenario_map.setdefault(scenario, {})[vendor] = {
                "mean": mean_text,
                "meanNs": parse_mean_to_ns(mean_text),
                "allocated": row.get("Allocated", ""),
            }

        scenarios = []
        for scenario in sorted(scenario_map.keys()):
            vendors = scenario_map[scenario]
            entry = {"name": scenario, "vendors": vendors}
            cgx = vendors.get("CodeGlyphX")
            if cgx and cgx.get("meanNs"):
                ratios = {}
                for key, value in vendors.items():
                    if key == "CodeGlyphX":
                        continue
                    mean_ns = value.get("meanNs")
                    if mean_ns:
                        ratios[key] = round(mean_ns / cgx["meanNs"], 3)
                entry["ratios"] = ratios
            scenarios.append(entry)
        comparisons.append({"id": base_name, "title": title, "scenarios": scenarios})

    notes = [
        format_run_mode(run_mode),
        "Comparisons target PNG output and include encode+render (not encode-only).",
        "Module size and quiet zone are matched to CodeGlyphX defaults where possible; image size is derived from CodeGlyphX modules.",
        "ZXing.Net uses ZXing.Net.Bindings.ImageSharp.V3 (ImageSharp 3.x renderer).",
        "Barcoder uses Barcoder.Renderer.Image (ImageSharp renderer).",
        "QRCoder uses PngByteQRCode (managed PNG output, no external renderer).",
        "QR decode comparisons use raw RGBA32 bytes (ZXing via RGBLuminanceSource).",
        "QR decode clean uses CodeGlyphX Balanced; noisy uses CodeGlyphX Robust with aggressive sampling/limits; ZXing uses default (clean) and TryHarder (noisy).",
    ]

    payload = {
        "generatedUtc": dt.datetime.now(dt.timezone.utc).isoformat(),
        "os": os_name,
        "framework": framework,
        "configuration": configuration,
        "runMode": run_mode,
        "runModeDetails": format_run_mode(run_mode),
        "artifacts": str(artifacts_path),
        "notes": notes,
        "baseline": baseline,
        "comparisons": comparisons,
    }

    if not path.exists():
        skeleton = {"windows": None, "linux": None, "macos": None}
        path.write_text(
            __import__("json").dumps(skeleton, indent=2),
            encoding="utf-8",
        )

    data = __import__("json").loads(path.read_text(encoding="utf-8"))
    data[os_name] = payload
    path.write_text(__import__("json").dumps(data, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
