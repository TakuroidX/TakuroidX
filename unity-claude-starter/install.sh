#!/usr/bin/env bash
# Copy this starter kit into an existing Unity project. Existing files are never overwritten.
# Usage: ./install.sh <path to Unity project> [--no-sample]
#   --no-sample : don't copy the sample code (Health*); only the asmdefs and folders

set -euo pipefail
SRC="$(cd "$(dirname "$0")" && pwd)"
DEST="${1:-}"
NO_SAMPLE=0
[[ "${2:-}" == "--no-sample" ]] && NO_SAMPLE=1

[[ -n "$DEST" ]] || { echo "Usage: $0 <path to Unity project> [--no-sample]" >&2; exit 1; }
[[ -f "$DEST/ProjectSettings/ProjectVersion.txt" ]] \
  || { echo "error: $DEST is not a Unity project (ProjectSettings/ProjectVersion.txt missing)" >&2; exit 1; }

copied=0; skipped=0
while IFS= read -r -d '' f; do
  rel="${f#"$SRC"/}"
  case "$rel" in
    install.sh|README.md) continue ;;
  esac
  if [[ $NO_SAMPLE -eq 1 && "$rel" == Assets/_Game/*Health*.cs ]]; then continue; fi
  if [[ -e "$DEST/$rel" ]]; then
    echo "skip (exists): $rel"; skipped=$((skipped + 1))
  else
    mkdir -p "$(dirname "$DEST/$rel")"
    cp -p "$f" "$DEST/$rel"
    copied=$((copied + 1))
  fi
done < <(find "$SRC" -type f -print0)

echo "done: copied=$copied skipped=$skipped"
[[ $skipped -gt 0 ]] && echo "For files that already exist (.gitignore etc.), merge the contents by hand as needed."
echo "Next: open the project in Unity once to generate the .meta files, then run ./scripts/unity-test.sh editmode."
