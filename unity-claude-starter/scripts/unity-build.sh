#!/usr/bin/env bash
# Build in batchmode. Scenes are the ones enabled in Build Settings.
# Usage: ./scripts/unity-build.sh <BuildTarget> [output path]
#   BuildTarget e.g.: StandaloneWindows64 / StandaloneOSX / StandaloneLinux64 / Android / iOS / WebGL
#   Default output: Builds/<BuildTarget>/

source "$(dirname "$0")/unity-env.sh"

TARGET="${1:-}"
[[ -n "$TARGET" ]] || die "Usage: $0 <BuildTarget> [output path]"
OUT="${2:-$PROJECT_ROOT/Builds/$TARGET}"

UNITY="$(find_unity)"
LOG="$LOG_DIR/build-$TARGET.log"

echo "Building $TARGET → $OUT"
set +e
"$UNITY" -batchmode -nographics -quit \
  -projectPath "$(native_path "$PROJECT_ROOT")" \
  -buildTarget "$TARGET" \
  -executeMethod Game.EditorTools.BatchBuild.BuildFromCommandLine \
  -customBuildPath "$(native_path "$OUT")" \
  -logFile "$(native_path "$LOG")"
code=$?
set -e

if [[ $code -ne 0 ]]; then
  echo "❌ Build failed (exit $code). Log: $LOG"
  print_compile_errors "$LOG"
  grep -E "^\[BatchBuild\]" "$LOG" || true
  exit "$code"
fi
echo "✅ Build succeeded: $OUT"
