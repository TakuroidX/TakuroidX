#!/usr/bin/env bash
# Run the Unity Test Framework in batchmode and print a summary of the results.
# Usage: ./scripts/unity-test.sh [editmode|playmode] [test-filter]
#   e.g. ./scripts/unity-test.sh editmode
#        ./scripts/unity-test.sh editmode "Game.Tests.HealthModelTests"
# Exit code: 0 = all passed, 2 = some tests failed, other = run error

source "$(dirname "$0")/unity-env.sh"

MODE="${1:-editmode}"
FILTER="${2:-}"
case "$MODE" in
  editmode) PLATFORM=EditMode ;;
  playmode) PLATFORM=PlayMode ;;
  *) die "Specify editmode or playmode (got: $MODE)" ;;
esac

UNITY="$(find_unity)"
LOG="$LOG_DIR/test-$MODE.log"
RESULTS="$LOG_DIR/test-$MODE-results.xml"
rm -f "$RESULTS"

args=(-batchmode -nographics
  -projectPath "$(native_path "$PROJECT_ROOT")"
  -runTests -testPlatform "$PLATFORM"
  -testResults "$(native_path "$RESULTS")"
  -logFile "$(native_path "$LOG")")
[[ -n "$FILTER" ]] && args+=(-testFilter "$FILTER")

echo "Running $PLATFORM tests with: $UNITY"
set +e
"$UNITY" "${args[@]}"   # Don't pass -quit together with -runTests
code=$?
set -e

if [[ ! -f "$RESULTS" ]]; then
  echo "❌ No test results were produced (exit $code). Log: $LOG"
  print_compile_errors "$LOG"
  exit "${code/#0/3}"
fi

# Summary from the <test-run> element (NUnit3 format)
summary="$(grep -o '<test-run [^>]*>' "$RESULTS" | head -1)"
attr() { echo "$summary" | sed -n "s/.* $1=\"\([^\"]*\)\".*/\1/p"; }
echo "total=$(attr total) passed=$(attr passed) failed=$(attr failed) skipped=$(attr skipped) duration=$(attr duration)s"

if [[ "$(attr failed)" != "0" ]]; then
  echo "❌ Failed tests:"
  grep -o '<test-case [^>]*result="Failed"[^>]*>' "$RESULTS" \
    | sed -n 's/.*fullname="\([^"]*\)".*/  - \1/p'
  echo "Details (failure messages and stack traces): $RESULTS"
  exit 2
fi
echo "✅ All tests passed"
exit "$code"
