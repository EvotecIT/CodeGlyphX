#!/usr/bin/env python3
import argparse
import csv
import datetime as dt
import json
import os
import platform
import re
import sys
from pathlib import Path


BENCH_PREFIX = "CodeGlyphX.Benchmarks."
REPORT_GLOB = "*-report.csv"
COMPARE_VENDORS = {"CodeGlyphX", "ZXing.Net", "QRCoder", "Barcoder"}

TITLE_MAP = {
    "QrCodeBenchmarks": "QR (Encode)",
    "QrDecodeBenchmarks": "QR (Decode)",
    "BarcodeBenchmarks": "1D Barcodes (Encode)",
    "MatrixCodeBenchmarks": "2D Matrix Codes (Encode)",
    "QrCompareBenchmarks": "QR (Encode)",
    "QrDecodeCleanCompareBenchmarks": "QR Decode (Clean)",
    "QrDecodeNoisyCompareBenchmarks": "QR Decode (Noisy)",
    "QrDecodeStressCompareBenchmarks": "QR Decode (Stress)",
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
        .replace("Âµs", "μs")
        .replace("Âμs", "μs")
    )


def parse_allocated_bytes(value: str):
    if not value:
        return None
    cleaned = value.strip().replace(",", "")
    if cleaned == "NA":
        return None
    match = re.match(r"^(\d+(?:\.\d+)?)\s*(B|KB|MB)$", cleaned)
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


def get_compare_class_name(path: Path) -> str:
    return strip_benchmark_prefix(path.stem)


def strip_benchmark_prefix(name: str) -> str:
    return name.replace(BENCH_PREFIX, "").replace("-report", "")


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
    with path.open(newline="", encoding="utf-8-sig") as f:
        sample = f.read(2048)
        f.seek(0)
        try:
            dialect = csv.Sniffer().sniff(sample, delimiters=";,")
        except csv.Error:
            dialect = csv.get_dialect("excel")
        reader = csv.DictReader(f, dialect=dialect)
        return list(reader)


def list_report_files(results_path: Path):
    files = sorted(results_path.glob(REPORT_GLOB))
    baseline_files = [p for p in files if "Compare" not in p.name]
    compare_files = [p for p in files if "Compare" in p.name]
    return baseline_files, compare_files


def expected_compare_ids():
    return sorted([key for key in TITLE_MAP.keys() if key.endswith("CompareBenchmarks")])


def compute_missing_compare(compare_files):
    expected = expected_compare_ids()
    actual = sorted([get_compare_class_name(p) for p in compare_files])
    missing_titles = [TITLE_MAP.get(name, name) for name in expected if name not in actual]
    missing_ids = [name for name in expected if name not in actual]
    return expected, actual, missing_titles, missing_ids


def parse_vendor_scenario(method: str):
    method = (method or "").strip()
    if not method:
        return "Unknown", method
    parts = method.split(None, 1)
    if len(parts) == 2 and parts[0] in COMPARE_VENDORS:
        return parts[0], parts[1]
    return "Unknown", method


def build_summary(compare_files):
    summary_rows = []
    summary_items = []
    for path in compare_files:
        rows = load_csv_rows(path)
        if not rows:
            continue
        base_name = strip_benchmark_prefix(path.stem)
        title = TITLE_MAP.get(base_name, base_name)

        scenario_map = {}
        for row in rows:
            method = normalize_method(row.get("Method", ""))
            if not method:
                continue
            vendor, scenario = parse_vendor_scenario(method)
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
    return summary_rows, summary_items


def build_baseline_section(lines, baseline_files):
    if not baseline_files:
        return
    lines.append("### Baseline")
    lines.append("")
    for path in baseline_files:
        rows = load_csv_rows(path)
        if not rows:
            continue
        base_name = strip_benchmark_prefix(path.stem)
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


def build_comparison_section(lines, compare_files):
    if not compare_files:
        return
    lines.append("### Comparisons")
    lines.append("")
    for path in compare_files:
        rows = load_csv_rows(path)
        if not rows:
            continue
        base_name = strip_benchmark_prefix(path.stem)
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
            if not method:
                continue
            vendor, scenario = parse_vendor_scenario(method)
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


def build_baseline_payload(baseline_files):
    baseline = []
    for file in baseline_files:
        rows = load_csv_rows(file)
        if not rows:
            continue
        base_name = strip_benchmark_prefix(file.stem)
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
    return baseline


def build_comparisons_payload(compare_files):
    comparisons = []
    for file in compare_files:
        rows = load_csv_rows(file)
        if not rows:
            continue
        base_name = strip_benchmark_prefix(file.stem)
        title = TITLE_MAP.get(base_name, base_name)
        scenario_map = {}
        for row in rows:
            method = normalize_method(row.get("Method", ""))
            if not method:
                continue
            vendor, scenario = parse_vendor_scenario(method)
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
    return comparisons


def format_run_mode(run_mode: str, source: str | None = None, requested: str | None = None) -> str:
    if run_mode == "quick":
        label = "Run mode: Quick (warmupCount=1, iterationCount=3, invocationCount=1)."
    else:
        label = "Run mode: Full (BenchmarkDotNet default job settings)."
    if source in ("inferred", "inferred-mismatch"):
        if requested and requested != run_mode:
            return f"{label} (inferred from artifacts; requested {requested})."
        return f"{label} (inferred from artifacts)."
    return label


def infer_run_mode_from_reports(results_path: Path) -> str | None:
    candidates = list(results_path.glob("*-report-github.md"))
    if not candidates:
        candidates = list(results_path.glob("*-report.md"))
    if not candidates:
        return None

    count_re = re.compile(r"(IterationCount|WarmupCount|InvocationCount)\\s*=\\s*(\\d+)")
    for path in sorted(candidates):
        try:
            text = path.read_text(encoding="utf-8", errors="ignore")
        except OSError:
            continue
        counts = {}
        for match in count_re.finditer(text):
            counts[match.group(1).lower()] = int(match.group(2))
        iteration = counts.get("iterationcount")
        warmup = counts.get("warmupcount")
        invocation = counts.get("invocationcount")
        if iteration is None or warmup is None:
            continue
        if iteration == 3 and warmup == 1 and (invocation is None or invocation == 1):
            return "quick"
        return "full"
    return None


def resolve_run_mode(run_mode: str | None, results_path: Path):
    inferred = infer_run_mode_from_reports(results_path)
    requested = run_mode
    warning = None
    source = "explicit" if run_mode else None

    if inferred:
        if run_mode and inferred != run_mode:
            warning = f"Run mode mismatch: requested {run_mode}, inferred {inferred} from artifacts."
            run_mode = inferred
            source = "inferred-mismatch"
        elif not run_mode:
            run_mode = inferred
            source = "inferred"
    if not run_mode:
        run_mode = "quick" if os.environ.get("BENCH_QUICK") == "true" else "full"
        source = "env-default"

    if warning:
        print(f"WARNING: {warning}", file=sys.stderr)

    details = format_run_mode(run_mode, source=source, requested=requested)
    return run_mode, details, warning, source


def build_section(
    artifacts_path: Path,
    framework: str,
    configuration: str,
    run_mode: str,
    run_mode_details: str,
    run_mode_warning: str | None,
) -> str:
    results_path = artifacts_path / "results"
    if not results_path.exists():
        raise SystemExit(f"Results folder not found: {results_path}")

    baseline_files, compare_files = list_report_files(results_path)
    _, _, missing_compare, _ = compute_missing_compare(compare_files)

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
    lines.append(f"- {run_mode_details}")
    lines.append("- Comparisons target PNG output and include encode+render (not encode-only).")
    lines.append("- Module size and quiet zone are matched to CodeGlyphX defaults where possible; image size is derived from CodeGlyphX modules.")
    lines.append("- ZXing.Net uses ZXing.Net.Bindings.ImageSharp.V3 (ImageSharp 3.x renderer).")
    lines.append("- Barcoder uses Barcoder.Renderer.Image (ImageSharp renderer).")
    lines.append("- QRCoder uses PngByteQRCode (managed PNG output, no external renderer).")
    lines.append("- QR decode comparisons use raw RGBA32 bytes (ZXing via RGBLuminanceSource).")
    lines.append("- QR decode clean uses CodeGlyphX Balanced; noisy uses CodeGlyphX Robust with aggressive sampling/limits; ZXing uses default (clean) and TryHarder (noisy).")
    warnings = []
    if run_mode_warning:
        warnings.append(run_mode_warning)
    if missing_compare:
        warnings.append(f"Missing compare results: {', '.join(missing_compare)}.")
    if warnings:
        lines.append("Warnings:")
        lines.extend(f"- {warning}" for warning in warnings)
    lines.append("")

    summary_rows = []
    summary_items = []
    if compare_files:
        summary_rows, summary_items = build_summary(compare_files)
        if summary_rows:
            lines.append("### Summary (Comparisons)")
            lines.append("")
            lines.append("| Benchmark | Scenario | Fastest | CodeGlyphX vs Fastest | CodeGlyphX Alloc vs Fastest | Rating | CodeGlyphX Mean | CodeGlyphX Alloc |")
            lines.append("| --- | --- | --- | --- | --- | --- | --- | --- |")
            lines.extend(summary_rows)
            lines.append("")
        else:
            summary_items = []

    build_baseline_section(lines, baseline_files)
    build_comparison_section(lines, compare_files)

    return "\n".join(lines).rstrip()


def build_template(blocks):
    return "\n".join(
        [
            "# Benchmarks",
            "",
            "**Data locations**",
            "- Generated files are overwritten on each run (do not edit by hand).",
            "- Human-readable report: `BENCHMARK.md`",
            "- Website JSON: `Assets/Data/benchmark.json`",
            "- Summary JSON: `Assets/Data/benchmark-summary.json`",
            "- Index JSON: `Assets/Data/benchmark-index.json`",
            "",
            "**Publish flag**",
            "- Quick runs default to `publish=false` (draft).",
            "- Full runs default to `publish=true`.",
            "- Override with `-Publish` or `-NoPublish` on the report generator.",
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
    text = path.read_text(encoding="utf-8-sig") if path.exists() else ""
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
    parser.add_argument("--allow-partial", action="store_true")
    parser.add_argument("--publish", action="store_true")
    parser.add_argument("--no-publish", action="store_true")
    parser.add_argument("--fail-on-missing-compare", action="store_true")
    args = parser.parse_args()

    artifacts_path = Path(args.artifacts_path).resolve()
    output_path = Path(args.output).resolve() if args.output else Path(__file__).resolve().parent.parent / "BENCHMARK.md"

    os_name = resolve_os_name(artifacts_path, args.os_name)
    results_path = artifacts_path / "results"
    run_mode, run_mode_details, run_mode_warning, run_mode_source = resolve_run_mode(args.run_mode, results_path)
    fail_on_missing_compare = args.fail_on_missing_compare or not args.allow_partial
    publish_flag = resolve_publish_flag(run_mode, args.publish, args.no_publish)
    meta = build_meta(args.commit, args.branch, args.dotnet_sdk, args.runtime)
    section = build_section(artifacts_path, args.framework, args.configuration, run_mode, run_mode_details, run_mode_warning)
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
        run_mode_details,
        run_mode_source,
        publish_flag,
        meta,
        fail_on_missing_compare,
    )
    # JSON output is already stored under Assets/Data for website ingestion.


def parse_mean_to_ns(value: str):
    if not value:
        return None
    cleaned = value.strip().replace(",", "")
    if cleaned == "NA":
        return None
    match = re.match(r"^(\d+(?:\.\d+)?)\s*(ns|us|μs|ms|s)$", cleaned)
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


def find_pack_runner_report(artifacts_path: Path, run_mode: str):
    pack_dir = artifacts_path / "pack-runner"
    if not pack_dir.exists():
        return None
    preferred = pack_dir / f"qr-decode-packs-{run_mode}.json"
    if preferred.exists():
        return preferred
    candidates = []
    for path in pack_dir.glob(f"qr-decode-packs-*-{run_mode}.json"):
        try:
            mtime = path.stat().st_mtime
        except OSError:
            mtime = 0
        candidates.append((mtime, path))
    if not candidates:
        return None
    candidates.sort(key=lambda item: item[0], reverse=True)
    return candidates[0][1]


def load_pack_runner_payload(artifacts_path: Path, run_mode: str):
    report_path = find_pack_runner_report(artifacts_path, run_mode)
    if not report_path:
        return None

    raw = json.loads(report_path.read_text(encoding="utf-8-sig"))

    def get_field(obj: dict, *names, default=None):
        if not isinstance(obj, dict):
            return default
        for name in names:
            if name in obj:
                return obj[name]
        return default

    packs_raw = get_field(raw, "Packs", "packs", default=[]) or []
    engines_acc: dict[str, dict] = {}
    pack_summaries = []

    for pack in packs_raw:
        pack_name = get_field(pack, "Name", "name", default="unknown")
        scenario_count = int(get_field(pack, "ScenarioCount", "scenarioCount", default=0) or 0)
        engines_raw = get_field(pack, "Engines", "engines", default=[]) or []
        engine_summaries = []

        for engine in engines_raw:
            engine_name = get_field(engine, "Name", "name", default="unknown")
            is_external = bool(get_field(engine, "IsExternal", "isExternal", default=False))
            runs = float(get_field(engine, "Runs", "runs", default=0) or 0)
            decode_rate = float(get_field(engine, "DecodeRate", "decodeRate", default=0) or 0)
            expected_rate = float(get_field(engine, "ExpectedRate", "expectedRate", default=0) or 0)
            median_ms = float(get_field(engine, "MedianMs", "medianMs", default=0) or 0)
            p95_ms = float(get_field(engine, "P95Ms", "p95Ms", default=0) or 0)

            scenarios = get_field(engine, "Scenarios", "scenarios", default=[]) or []
            failing_scenarios = []
            for scenario in scenarios:
                scenario_expected = float(get_field(scenario, "ExpectedRate", "expectedRate", default=1) or 1)
                if scenario_expected >= 0.9999:
                    continue
                scenario_name = get_field(scenario, "Name", "name", default=None)
                if scenario_name:
                    failing_scenarios.append(scenario_name)

            engine_summaries.append(
                {
                    "name": engine_name,
                    "isExternal": is_external,
                    "runs": runs,
                    "decodeRate": decode_rate,
                    "expectedRate": expected_rate,
                    "medianMs": median_ms,
                    "p95Ms": p95_ms,
                    "failingScenarios": failing_scenarios,
                }
            )

            acc = engines_acc.get(engine_name)
            if not acc:
                acc = {
                    "name": engine_name,
                    "isExternal": is_external,
                    "runs": 0.0,
                    "decodeWeighted": 0.0,
                    "expectedWeighted": 0.0,
                    "failingScenarios": set(),
                    "failingPacks": set(),
                }
                engines_acc[engine_name] = acc
            acc["runs"] += runs
            acc["decodeWeighted"] += decode_rate * runs
            acc["expectedWeighted"] += expected_rate * runs
            if failing_scenarios:
                acc["failingScenarios"].update(failing_scenarios)
                acc["failingPacks"].add(pack_name)

        pack_summaries.append(
            {
                "name": pack_name,
                "scenarioCount": scenario_count,
                "engines": engine_summaries,
            }
        )

    engines_summary = []
    for acc in engines_acc.values():
        runs = acc["runs"] or 0.0
        decode_rate = acc["decodeWeighted"] / runs if runs else None
        expected_rate = acc["expectedWeighted"] / runs if runs else None
        engines_summary.append(
            {
                "name": acc["name"],
                "isExternal": acc["isExternal"],
                "runs": runs,
                "decodeRate": decode_rate,
                "expectedRate": expected_rate,
                "failingScenarios": sorted(acc["failingScenarios"]),
                "failingPacks": sorted(acc["failingPacks"]),
            }
        )

    def fmt_pct(value: float | None):
        if value is None:
            return "n/a"
        return f"{value * 100.0:.0f}%"

    engines_for_note = sorted(engines_summary, key=lambda e: (e["isExternal"], e["name"]))
    note_bits = []
    for engine in engines_for_note:
        bit = f"{engine['name']} expected={fmt_pct(engine['expectedRate'])}"
        failing = engine["failingScenarios"][:4]
        if failing:
            bit += " (misses: " + ", ".join(failing) + ")"
        note_bits.append(bit)

    note = None
    if note_bits:
        note = f"QR pack runner ({run_mode}): " + "; ".join(note_bits)

    payload = {
        "reportPath": str(report_path),
        "generatedUtc": get_field(raw, "DateUtc", "dateUtc", default=None),
        "mode": run_mode,
        "packs": pack_summaries,
        "engines": engines_for_note,
        "note": note,
    }
    return payload


def write_json(
    path: Path,
    artifacts_path: Path,
    framework: str,
    configuration: str,
    os_name: str,
    run_mode: str,
    run_mode_details: str,
    run_mode_source: str,
    publish: bool,
    meta: dict,
    fail_on_missing_compare: bool,
):
    results_path = artifacts_path / "results"
    if not results_path.exists():
        raise SystemExit(f"Results folder not found: {results_path}")

    baseline_files, compare_files = list_report_files(results_path)
    _, _, missing_compare, missing_compare_ids = compute_missing_compare(compare_files)

    baseline = build_baseline_payload(baseline_files)
    comparisons = build_comparisons_payload(compare_files)
    summary_rows, summary_items = build_summary(compare_files) if compare_files else ([], [])
    pack_runner = load_pack_runner_payload(artifacts_path, run_mode)

    notes = [
        run_mode_details,
        "Comparisons target PNG output and include encode+render (not encode-only).",
        "Module size and quiet zone are matched to CodeGlyphX defaults where possible; image size is derived from CodeGlyphX modules.",
        "ZXing.Net uses ZXing.Net.Bindings.ImageSharp.V3 (ImageSharp 3.x renderer).",
        "Barcoder uses Barcoder.Renderer.Image (ImageSharp renderer).",
        "QRCoder uses PngByteQRCode (managed PNG output, no external renderer).",
        "QR decode comparisons use raw RGBA32 bytes (ZXing via RGBLuminanceSource).",
        "QR decode clean uses CodeGlyphX Balanced; noisy uses CodeGlyphX Robust with aggressive sampling/limits; ZXing uses default (clean) and TryHarder (noisy).",
    ]
    if pack_runner and pack_runner.get("note"):
        notes.append(pack_runner["note"])

    payload = {
        "generatedUtc": dt.datetime.now(dt.timezone.utc).isoformat(),
        "schemaVersion": 1,
        "os": os_name,
        "framework": framework,
        "configuration": configuration,
        "runMode": run_mode,
        "runModeDetails": run_mode_details,
        "runModeSource": run_mode_source,
        "publish": publish,
        "artifacts": str(artifacts_path),
        "meta": meta,
        "missingComparisons": missing_compare,
        "missingComparisonIds": missing_compare_ids,
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
        "packRunner": pack_runner,
    }

    if not path.exists():
        skeleton = {
            "windows": {"quick": None, "full": None},
            "linux": {"quick": None, "full": None},
            "macos": {"quick": None, "full": None},
        }
        path.write_text(__import__("json").dumps(skeleton, indent=2), encoding="utf-8")

    data = __import__("json").loads(path.read_text(encoding="utf-8-sig"))
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

    summary_data = __import__("json").loads(summary_path.read_text(encoding="utf-8-sig"))
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
        "runModeSource": payload["runModeSource"],
        "publish": payload["publish"],
        "artifacts": payload["artifacts"],
        "meta": payload["meta"],
        "missingComparisons": payload["missingComparisons"],
        "missingComparisonIds": payload["missingComparisonIds"],
        "howToRead": payload["howToRead"],
        "notes": payload["notes"],
        "summary": payload["summary"],
        "packRunner": payload.get("packRunner"),
    }
    summary_data[os_name][run_mode] = summary_payload
    summary_path.write_text(__import__("json").dumps(summary_data, indent=2), encoding="utf-8")

    index_path = Path(__file__).resolve().parent.parent / "Assets" / "Data" / "benchmark-index.json"
    if not index_path.exists():
        index_path.write_text(__import__("json").dumps({"schemaVersion": 1, "entries": []}, indent=2), encoding="utf-8")

    index_data = __import__("json").loads(index_path.read_text(encoding="utf-8-sig"))
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
            "runModeSource": payload["runModeSource"],
            "generatedUtc": payload["generatedUtc"],
            "publish": payload["publish"],
            "framework": payload["framework"],
            "configuration": payload["configuration"],
            "artifacts": payload["artifacts"],
            "meta": payload["meta"],
        }
    )
    index_path.write_text(__import__("json").dumps(index_data, indent=2), encoding="utf-8")

    if fail_on_missing_compare and payload["missingComparisons"]:
        raise SystemExit(f"Missing compare results: {', '.join(payload['missingComparisons'])}.")


if __name__ == "__main__":
    main()
