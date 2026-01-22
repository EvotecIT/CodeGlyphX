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


def parse_allocated_bytes(value: str):
    if not value:
        return None
    cleaned = value.strip().replace(",", "")
    if cleaned == "NA":
        return None
    match = re.match(r"^([0-9]+(?:\.[0-9]+)?)\s*(B|KB|MB)$", cleaned)
    if not match:
        return None
    number = float(match.group(1))
    unit = match.group(2)
    if unit == "B":
        return number
    if unit == "KB":
        return number * 1024.0
    if unit == "MB":
        return number * 1024.0 * 1024.0
    return None


def rate_performance(time_ratio: float | None, alloc_ratio: float | None) -> str:
    if time_ratio is None:
        return "unknown"
    if alloc_ratio is not None:
        if time_ratio <= 1.1 and alloc_ratio <= 1.25:
            return "good"
        if time_ratio <= 1.5 and alloc_ratio <= 2.0:
            return "ok"
        return "bad"
    if time_ratio <= 1.1:
        return "good"
    if time_ratio <= 1.5:
        return "ok"
    return "bad"


def resolve_publish_flag(run_mode: str, publish: bool, no_publish: bool) -> bool:
    if publish:
        return True
    if no_publish:
        return False
    return run_mode == "full"


def build_meta(commit: str | None, branch: str | None, dotnet_sdk: str | None, runtime: str | None):
    return {
        "commit": commit or os.environ.get("GIT_COMMIT") or os.environ.get("BUILD_SOURCEVERSION"),
        "branch": branch or os.environ.get("GIT_BRANCH") or os.environ.get("BUILD_SOURCEBRANCH"),
        "dotnetSdk": dotnet_sdk or os.environ.get("DOTNET_SDK"),
        "runtime": runtime,
        "osDescription": platform.platform(),
        "osArchitecture": platform.machine(),
        "processArchitecture": platform.machine(),
        "machineName": platform.node(),
        "processorCount": os.cpu_count(),
    }


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
    lines.append("How to read:")
    lines.append("- Mean: average time per operation. Lower is better.")
    lines.append("- Allocated: managed memory allocated per operation. Lower is better.")
    lines.append("- CodeGlyphX vs Fastest: CodeGlyphX mean divided by the fastest mean for that scenario. 1 x means CodeGlyphX is fastest; 1.5 x means ~50% slower.")
    lines.append("- CodeGlyphX Alloc vs Fastest: CodeGlyphX allocated divided by the fastest allocation for that scenario. 1 x means CodeGlyphX allocates the least; higher is more allocations.")
    lines.append("- Rating: good/ok/bad based on time + allocation ratios (good <=1.1x and <=1.25x alloc, ok <=1.5x and <=2.0x alloc).")
    lines.append("- Quick runs use fewer iterations for fast feedback; Full runs use BenchmarkDotNet defaults and are recommended for publishing.")
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
        summary_items = []
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
                ratio_value = None
                ratio_text = ""
                alloc_ratio_value = None
                alloc_ratio_text = ""
                cgx_mean = ""
                cgx_alloc = ""
                if cgx and cgx.get("meanNs"):
                    ratio_value = round(cgx["meanNs"] / fastest["meanNs"], 2)
                    ratio_text = f"{ratio_value} x"
                    cgx_mean = cgx.get("mean", "")
                    cgx_alloc = cgx.get("allocated", "")
                    fastest_alloc_bytes = parse_allocated_bytes(fastest.get("allocated", ""))
                    cgx_alloc_bytes = parse_allocated_bytes(cgx.get("allocated", ""))
                    if fastest_alloc_bytes and cgx_alloc_bytes:
                        alloc_ratio_value = round(cgx_alloc_bytes / fastest_alloc_bytes, 2)
                        alloc_ratio_text = f"{alloc_ratio_value} x"
                rating = rate_performance(ratio_value, alloc_ratio_value)
                summary_rows.append(
                    f"| {title} | {scenario} | {fastest_vendor} {fastest.get('mean','')} | {ratio_text} | {alloc_ratio_text} | {rating} | {cgx_mean} | {cgx_alloc} |"
                )
                summary_items.append(
                    {
                        "benchmark": title,
                        "scenario": scenario,
                        "fastestVendor": fastest_vendor,
                        "fastestMean": fastest.get("mean", ""),
                        "codeGlyphXMean": cgx_mean,
                        "codeGlyphXAlloc": cgx_alloc,
                        "codeGlyphXVsFastest": ratio_value,
                        "codeGlyphXVsFastestText": ratio_text,
                        "codeGlyphXAllocVsFastest": alloc_ratio_value,
                        "codeGlyphXAllocVsFastestText": alloc_ratio_text,
                        "rating": rating,
                    }
                )

        if summary_rows:
            lines.append("### Summary (Comparisons)")
            lines.append("")
            lines.append("| Benchmark | Scenario | Fastest | CodeGlyphX vs Fastest | CodeGlyphX Alloc vs Fastest | Rating | CodeGlyphX Mean | CodeGlyphX Alloc |")
            lines.append("| --- | --- | --- | --- | --- | --- | --- | --- |")
            lines.extend(summary_rows)
            lines.append("")
        else:
            summary_items = []

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
            blocks["windows_quick"],
            "",
            blocks["windows_full"],
            "",
            blocks["linux_quick"],
            "",
            blocks["linux_full"],
            "",
            blocks["macos_quick"],
            "",
            blocks["macos_full"],
            "",
        ]
    )


def extract_block(text: str, os_name: str, run_mode: str):
    marker = f"BENCHMARK:{os_name.upper()}:{run_mode.upper()}"
    start = f"<!-- {marker}:START -->"
    end = f"<!-- {marker}:END -->"
    pattern = re.compile(re.escape(start) + r"[\s\S]*?" + re.escape(end))
    match = pattern.search(text)
    if match:
        return match.group(0)
    return f"{start}\n_no results yet_\n{end}"


def update_section(path: Path, section: str, os_name: str, run_mode: str):
    marker = f"BENCHMARK:{os_name.upper()}:{run_mode.upper()}"
    start = f"<!-- {marker}:START -->"
    end = f"<!-- {marker}:END -->"
    block = f"{start}\n{section}\n{end}"
    text = path.read_text(encoding="utf-8") if path.exists() else ""
    blocks = {
        "windows_quick": extract_block(text, "windows", "quick"),
        "windows_full": extract_block(text, "windows", "full"),
        "linux_quick": extract_block(text, "linux", "quick"),
        "linux_full": extract_block(text, "linux", "full"),
        "macos_quick": extract_block(text, "macos", "quick"),
        "macos_full": extract_block(text, "macos", "full"),
    }
    blocks[f"{os_name}_{run_mode}"] = block
    path.write_text(build_template(blocks), encoding="utf-8")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--artifacts-path", required=True)
    parser.add_argument("--output", default=None)
    parser.add_argument("--framework", default="net8.0")
    parser.add_argument("--configuration", default="Release")
    parser.add_argument("--run-mode", default=None, choices=["quick", "full"])
    parser.add_argument("--os-name", default=None, choices=["windows", "linux", "macos"])
    parser.add_argument("--commit", default=None)
    parser.add_argument("--branch", default=None)
    parser.add_argument("--dotnet-sdk", default=None)
    parser.add_argument("--runtime", default=None)
    parser.add_argument("--publish", action="store_true")
    parser.add_argument("--no-publish", action="store_true")
    args = parser.parse_args()

    artifacts_path = Path(args.artifacts_path).resolve()
    output_path = Path(args.output).resolve() if args.output else Path(__file__).resolve().parent.parent / "BENCHMARK.md"

    os_name = resolve_os_name(artifacts_path, args.os_name)
    run_mode = resolve_run_mode(args.run_mode)
    publish_flag = resolve_publish_flag(run_mode, args.publish, args.no_publish)
    meta = build_meta(args.commit, args.branch, args.dotnet_sdk, args.runtime)
    section = build_section(artifacts_path, args.framework, args.configuration, run_mode)
    update_section(output_path, section, os_name, run_mode)

    repo_root = Path(__file__).resolve().parent.parent
    json_path = repo_root / "Assets" / "Data" / "benchmark.json"
    json_path.parent.mkdir(parents=True, exist_ok=True)
    write_json(
        json_path,
        artifacts_path,
        args.framework,
        args.configuration,
        os_name,
        run_mode,
        publish_flag,
        meta,
    )
    repo_root = Path(__file__).resolve().parent.parent
    # JSON output is already stored under Assets/Data for website ingestion.


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
    publish: bool,
    meta: dict,
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
        "schemaVersion": 1,
        "os": os_name,
        "framework": framework,
        "configuration": configuration,
        "runMode": run_mode,
        "runModeDetails": format_run_mode(run_mode),
        "publish": publish,
        "artifacts": str(artifacts_path),
        "meta": meta,
        "howToRead": [
            "Mean: average time per operation. Lower is better.",
            "Allocated: managed memory allocated per operation. Lower is better.",
            "CodeGlyphX vs Fastest: CodeGlyphX mean divided by the fastest mean for that scenario. 1 x means CodeGlyphX is fastest; 1.5 x means ~50% slower.",
            "CodeGlyphX Alloc vs Fastest: CodeGlyphX allocated divided by the fastest allocation for that scenario. 1 x means CodeGlyphX allocates the least; higher is more allocations.",
            "Rating: good/ok/bad based on time + allocation ratios (good <=1.1x and <=1.25x alloc, ok <=1.5x and <=2.0x alloc).",
            "Quick runs use fewer iterations for fast feedback; Full runs use BenchmarkDotNet defaults and are recommended for publishing.",
        ],
        "notes": notes,
        "summary": summary_items,
        "baseline": baseline,
        "comparisons": comparisons,
    }

    if not path.exists():
        skeleton = {
            "windows": {"quick": None, "full": None},
            "linux": {"quick": None, "full": None},
            "macos": {"quick": None, "full": None},
        }
        path.write_text(__import__("json").dumps(skeleton, indent=2), encoding="utf-8")

    data = __import__("json").loads(path.read_text(encoding="utf-8"))
    for os_key in ("windows", "linux", "macos"):
        if data.get(os_key) is None:
            data[os_key] = {"quick": None, "full": None}
        elif "quick" not in data[os_key]:
            data[os_key] = {"quick": data[os_key], "full": None}

    data[os_name][run_mode] = payload
    path.write_text(__import__("json").dumps(data, indent=2), encoding="utf-8")

    summary_path = Path(__file__).resolve().parent.parent / "Assets" / "Data" / "benchmark-summary.json"
    summary_path.parent.mkdir(parents=True, exist_ok=True)
    if not summary_path.exists():
        skeleton = {
            "windows": {"quick": None, "full": None},
            "linux": {"quick": None, "full": None},
            "macos": {"quick": None, "full": None},
        }
        summary_path.write_text(__import__("json").dumps(skeleton, indent=2), encoding="utf-8")

    summary_data = __import__("json").loads(summary_path.read_text(encoding="utf-8"))
    for os_key in ("windows", "linux", "macos"):
        if summary_data.get(os_key) is None:
            summary_data[os_key] = {"quick": None, "full": None}
        elif "quick" not in summary_data[os_key]:
            summary_data[os_key] = {"quick": summary_data[os_key], "full": None}

    summary_payload = {
        "generatedUtc": payload["generatedUtc"],
        "schemaVersion": payload["schemaVersion"],
        "os": payload["os"],
        "framework": payload["framework"],
        "configuration": payload["configuration"],
        "runMode": payload["runMode"],
        "runModeDetails": payload["runModeDetails"],
        "publish": payload["publish"],
        "artifacts": payload["artifacts"],
        "meta": payload["meta"],
        "howToRead": payload["howToRead"],
        "notes": payload["notes"],
        "summary": payload["summary"],
    }
    summary_data[os_name][run_mode] = summary_payload
    summary_path.write_text(__import__("json").dumps(summary_data, indent=2), encoding="utf-8")

    index_path = Path(__file__).resolve().parent.parent / "Assets" / "Data" / "benchmark-index.json"
    if not index_path.exists():
        index_path.write_text(__import__("json").dumps({"schemaVersion": 1, "entries": []}, indent=2), encoding="utf-8")

    index_data = __import__("json").loads(index_path.read_text(encoding="utf-8"))
    if index_data.get("entries") is None:
        index_data["entries"] = []
    index_data["entries"] = [
        entry
        for entry in index_data["entries"]
        if not (entry.get("os") == payload["os"] and entry.get("runMode") == payload["runMode"])
    ]
    index_data["entries"].append(
        {
            "os": payload["os"],
            "runMode": payload["runMode"],
            "generatedUtc": payload["generatedUtc"],
            "publish": payload["publish"],
            "framework": payload["framework"],
            "configuration": payload["configuration"],
            "artifacts": payload["artifacts"],
            "meta": payload["meta"],
        }
    )
    index_path.write_text(__import__("json").dumps(index_data, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
