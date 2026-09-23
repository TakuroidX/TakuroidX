#!/usr/bin/env bash
# Check that files and .meta files under Assets/ correspond one to one (no Unity needed).
#   - An asset with no .meta     → forgot to commit it, or never opened in Unity
#   - A .meta with no asset      → the asset was deleted or moved but its .meta was left behind
# Unity ignores hidden files (.*) and anything ending in ~, so they aren't checked.
# Usage: ./scripts/check-meta.sh [directory (default: Assets)]

set -euo pipefail
cd "$(dirname "$0")/.."
ROOT="${1:-Assets}"
[[ -d "$ROOT" ]] || { echo "error: $ROOT not found" >&2; exit 1; }

missing=0
orphan=0

while IFS= read -r -d '' p; do
  if [[ ! -e "$p.meta" ]]; then
    echo "missing .meta: $p"; missing=$((missing + 1))
  fi
done < <(find "$ROOT" -mindepth 1 \
  \( -name '.*' -o -name '*~' \) -prune -o \
  ! -name '*.meta' -print0)

while IFS= read -r -d '' m; do
  if [[ ! -e "${m%.meta}" ]]; then
    echo "orphan .meta:  $m"; orphan=$((orphan + 1))
  fi
done < <(find "$ROOT" \( -name '.*' -o -name '*~' \) -prune -o -name '*.meta' -print0)

if (( missing + orphan > 0 )); then
  echo "❌ missing=$missing orphan=$orphan"
  echo "Hint: open the project in Unity once to generate .meta files, then commit them together."
  exit 1
fi
echo "✅ .meta files are consistent ($ROOT)"
