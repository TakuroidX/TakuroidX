#!/usr/bin/env bash
# Open the project in batchmode, compile the scripts, and exit.
# Usage: ./scripts/unity-compile.sh
# Exit code: 0 = no compile errors, nonzero = errors (details are printed)

source "$(dirname "$0")/unity-env.sh"

UNITY="$(find_unity)"
LOG="$LOG_DIR/compile.log"

echo "Compiling with: $UNITY"
set +e
"$UNITY" -batchmode -nographics -quit \
  -projectPath "$(native_path "$PROJECT_ROOT")" \
  -logFile "$(native_path "$LOG")"
code=$?
set -e

if [[ $code -ne 0 ]] || grep -q "error CS" "$LOG"; then
  echo "❌ Compile failed (exit $code). Log: $LOG"
  print_compile_errors "$LOG"
  if grep -q "another Unity instance is running" "$LOG"; then
    echo "→ The Unity Editor has this project open. Close the Editor and try again."
  fi
  exit 1
fi
echo "✅ No compile errors"
