#!/usr/bin/env bash
set -euo pipefail

FRAMEWORK="net8.0"
CONFIGURATION="Release"
ARTIFACTS_ROOT=""
NO_BASE=0
NO_COMPARE=0
COMPARE_ZXING=0
COMPARE_QRCODER=0
COMPARE_BARCODER=0
BASE_FILTER="*"
COMPARE_FILTER="*Compare*"
BASE_FILTER_SET=0
COMPARE_FILTER_SET=0
BENCH_QUICK=1
ALLOW_PARTIAL=0
SKIP_PREFLIGHT=0

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

Options:
  --framework <tfm>          Target framework (default: net8.0)
  --configuration <cfg>      Build configuration (default: Release)
  --artifacts-root <path>    Root folder for BenchmarkDotNet artifacts (default: Build/BenchmarkResults)
  --no-base                  Skip CodeGlyphX-only benchmarks
  --no-compare               Skip external comparisons
  --compare-zxing            Compare only ZXing (implies compare run)
  --compare-qrcoder          Compare only QRCoder (implies compare run)
  --compare-barcoder         Compare only Barcoder (implies compare run)
  --base-filter <filter>     Benchmark filter for baseline (default: *)
  --compare-filter <filter>  Benchmark filter for compare (default: *Compare*)
  --full                     Run full BenchmarkDotNet settings (default: quick)
  --allow-partial            Allow incomplete compare results in report
  --skip-preflight           Skip dependency preflight checks
  -h, --help                 Show this help
EOF
  return 0
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --framework) FRAMEWORK="$2"; shift 2 ;;
    --configuration) CONFIGURATION="$2"; shift 2 ;;
    --artifacts-root) ARTIFACTS_ROOT="$2"; shift 2 ;;
    --no-base) NO_BASE=1; shift ;;
    --no-compare) NO_COMPARE=1; shift ;;
    --compare-zxing) COMPARE_ZXING=1; shift ;;
    --compare-qrcoder) COMPARE_QRCODER=1; shift ;;
    --compare-barcoder) COMPARE_BARCODER=1; shift ;;
    --base-filter) BASE_FILTER="$2"; BASE_FILTER_SET=1; shift 2 ;;
    --compare-filter) COMPARE_FILTER="$2"; COMPARE_FILTER_SET=1; shift 2 ;;
    --full) BENCH_QUICK=0; shift ;;
    --allow-partial) ALLOW_PARTIAL=1; shift ;;
    --skip-preflight) SKIP_PREFLIGHT=1; shift ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown option: $1"; usage; exit 1 ;;
  esac
done

# Keep the default quick run focused on compare suites so it stays fast.
if [[ $BENCH_QUICK -eq 1 && $BASE_FILTER_SET -eq 0 ]]; then
  BASE_FILTER="*CompareBenchmarks*"
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_PATH="$SCRIPT_DIR/../CodeGlyphX.Benchmarks/CodeGlyphX.Benchmarks.csproj"

if [[ -z "$ARTIFACTS_ROOT" ]]; then
  ARTIFACTS_ROOT="$SCRIPT_DIR/BenchmarkResults"
fi

OS_NAME="unknown"
case "$(uname -s)" in
  Linux) OS_NAME="linux" ;;
  Darwin) OS_NAME="macos" ;;
  MINGW*|MSYS*|CYGWIN*) OS_NAME="windows" ;;
  *) OS_NAME="unknown" ;;
esac

TIMESTAMP="$(date +"%Y%m%d-%H%M%S")"
ARTIFACTS_PATH="$ARTIFACTS_ROOT/$OS_NAME-$TIMESTAMP"
mkdir -p "$ARTIFACTS_PATH"

run_bench() {
  local label="$1"
  local filter="$2"
  local env_prefix="$3"
  shift 3
  local props=("$@")

  echo ""
  echo "== $label =="
  local args=(run -c "$CONFIGURATION" --framework "$FRAMEWORK" --project "$PROJECT_PATH")
  if [[ ${#props[@]} -gt 0 ]]; then
    args+=("${props[@]}")
  fi
  args+=(-- --filter "$filter" --artifacts "$ARTIFACTS_PATH")
  if [[ -n "$env_prefix" ]]; then
    eval "$env_prefix dotnet \"\${args[@]}\""
  else
    dotnet "${args[@]}"
  fi
  return 0
}

run_pack_runner() {
  local label="$1"
  local env_prefix="$2"
  shift 2
  local props=("$@")

  local mode_arg="quick"
  if [[ $BENCH_QUICK -eq 0 ]]; then
    mode_arg="full"
  fi

  local reports_dir="$ARTIFACTS_PATH/pack-runner"
  mkdir -p "$reports_dir"

  echo ""
  echo "== $label =="
  local args=(run -c "$CONFIGURATION" --framework "$FRAMEWORK" --project "$PROJECT_PATH")
  if [[ ${#props[@]} -gt 0 ]]; then
    args+=("${props[@]}")
  fi
  args+=(-- --pack-runner --mode "$mode_arg" --format json --reports-dir "$reports_dir")

  local pack_env="$env_prefix"
  pack_env+="CODEGLYPHX_PACK_REPORTS_DIR=\"$reports_dir\" "
  pack_env+="BENCH_QUICK=$([[ $BENCH_QUICK -eq 1 ]] && echo true || echo false) "

  if [[ -n "$pack_env" ]]; then
    eval "$pack_env dotnet \"\${args[@]}\""
  else
    dotnet "${args[@]}"
  fi
  return 0
}

run_preflight() {
  local env_prefix="$1"
  shift
  local props=("$@")

  echo ""
  echo "== Preflight (Compare dependencies) =="
  local args=(run -c "$CONFIGURATION" --framework "$FRAMEWORK" --project "$PROJECT_PATH")
  if [[ ${#props[@]} -gt 0 ]]; then
    args+=("${props[@]}")
  fi
  args+=(-- --preflight)
  if [[ -n "$env_prefix" ]]; then
    eval "$env_prefix dotnet \"\${args[@]}\""
  else
    dotnet "${args[@]}"
  fi
  return 0
}

if [[ $NO_BASE -eq 0 ]]; then
  base_props=()
  base_env="BENCH_QUICK=$([[ $BENCH_QUICK -eq 1 ]] && echo true || echo false) "
  if [[ $BENCH_QUICK -eq 1 ]]; then
    base_props+=("/p:BenchQuick=true")
  fi
  run_bench "Baseline (CodeGlyphX only)" "$BASE_FILTER" "$base_env" "${base_props[@]}"
fi

PACK_PROPS=()
PACK_ENV_PREFIX=""

if [[ $NO_COMPARE -eq 0 ]]; then
  props=()
  env_prefix="BENCH_QUICK=$([[ $BENCH_QUICK -eq 1 ]] && echo true || echo false) "
  if [[ $BENCH_QUICK -eq 1 ]]; then
    props+=("/p:BenchQuick=true")
  fi
  if [[ $COMPARE_ZXING -eq 1 || $COMPARE_QRCODER -eq 1 || $COMPARE_BARCODER -eq 1 ]]; then
    [[ $COMPARE_ZXING -eq 1 ]] && props+=("/p:CompareZXing=true")
    [[ $COMPARE_QRCODER -eq 1 ]] && props+=("/p:CompareQRCoder=true")
    [[ $COMPARE_BARCODER -eq 1 ]] && props+=("/p:CompareBarcoder=true")
    [[ $COMPARE_ZXING -eq 1 ]] && env_prefix+="COMPARE_ZXING=true "
    [[ $COMPARE_QRCODER -eq 1 ]] && env_prefix+="COMPARE_QRCODER=true "
    [[ $COMPARE_BARCODER -eq 1 ]] && env_prefix+="COMPARE_BARCODER=true "
  else
    props+=("/p:CompareExternal=true")
    env_prefix="COMPARE_EXTERNAL=true "
  fi
  if [[ $SKIP_PREFLIGHT -eq 0 ]]; then
    run_preflight "$env_prefix" "${props[@]}"
  fi
  run_bench "External comparisons" "$COMPARE_FILTER" "$env_prefix" "${props[@]}"
  PACK_PROPS=("${props[@]}")
  PACK_ENV_PREFIX="$env_prefix"
elif [[ $NO_BASE -eq 0 ]]; then
  PACK_PROPS=("${base_props[@]}")
  PACK_ENV_PREFIX="$base_env"
fi

run_pack_runner "QR decode pack runner" "$PACK_ENV_PREFIX" "${PACK_PROPS[@]}"

REPORT_SCRIPT="$SCRIPT_DIR/generate-benchmark-report.py"
if command -v python3 >/dev/null 2>&1 && [[ -f "$REPORT_SCRIPT" ]]; then
  if [[ $BENCH_QUICK -eq 1 ]]; then
    if [[ $ALLOW_PARTIAL -eq 1 ]]; then
      python3 "$REPORT_SCRIPT" --artifacts-path "$ARTIFACTS_PATH" --framework "$FRAMEWORK" --configuration "$CONFIGURATION" --run-mode quick --allow-partial
    else
      python3 "$REPORT_SCRIPT" --artifacts-path "$ARTIFACTS_PATH" --framework "$FRAMEWORK" --configuration "$CONFIGURATION" --run-mode quick --fail-on-missing-compare
    fi
  else
    if [[ $ALLOW_PARTIAL -eq 1 ]]; then
      python3 "$REPORT_SCRIPT" --artifacts-path "$ARTIFACTS_PATH" --framework "$FRAMEWORK" --configuration "$CONFIGURATION" --run-mode full --allow-partial
    else
      python3 "$REPORT_SCRIPT" --artifacts-path "$ARTIFACTS_PATH" --framework "$FRAMEWORK" --configuration "$CONFIGURATION" --run-mode full --fail-on-missing-compare
    fi
  fi
else
  echo ""
  echo "Skipping BENCHMARK.md generation (python3 not found or script missing)."
fi
