#!/usr/bin/env bash
# Shared helpers for the unity-*.sh scripts. Source this file; don't run it directly.
#
# Unity executable lookup order:
#   1. The UNITY_PATH environment variable
#   2. The Unity Hub default install location + ProjectSettings/ProjectVersion.txt

set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOG_DIR="$PROJECT_ROOT/Logs/claude"
mkdir -p "$LOG_DIR"

die() { echo "error: $*" >&2; exit 1; }

unity_version() {
  local f="$PROJECT_ROOT/ProjectSettings/ProjectVersion.txt"
  [[ -f "$f" ]] || die "$f not found. Is this the root of a Unity project?"
  sed -n 's/^m_EditorVersion: *//p' "$f" | tr -d '\r'
}

find_unity() {
  if [[ -n "${UNITY_PATH:-}" ]]; then
    [[ -x "$UNITY_PATH" ]] || die "UNITY_PATH=$UNITY_PATH is not executable"
    echo "$UNITY_PATH"; return
  fi
  local v; v="$(unity_version)"
  local candidates=(
    "/Applications/Unity/Hub/Editor/$v/Unity.app/Contents/MacOS/Unity"
    "/c/Program Files/Unity/Hub/Editor/$v/Editor/Unity.exe"
    "$HOME/Unity/Hub/Editor/$v/Editor/Unity"
  )
  local c
  for c in "${candidates[@]}"; do
    [[ -x "$c" ]] && { echo "$c"; return; }
  done
  die "Unity $v not found. Install it via Unity Hub or set UNITY_PATH."
}

# Unity.exe on Windows (Git Bash) needs Windows-style paths
native_path() {
  if command -v cygpath >/dev/null 2>&1; then cygpath -w "$1"; else echo "$1"; fi
}

# Print compile errors from a log (up to 50 lines)
print_compile_errors() {
  grep -E "error CS[0-9]+|Scripts have compiler errors" "$1" | sort -u | head -50 || true
}
