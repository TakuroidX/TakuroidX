#!/usr/bin/env bash
# Math verification → Excel spec generation → sync to Unity, in one go.
#   1. Build the SDK simulator and verify theory / full cycle / Monte Carlo (stops on failure)
#   2. Generate docs/math_spec.xlsx and docs/game_spec.xlsx
#   3. Copy the math JSON into the Unity project's Resources
# Environment variables: ROUNDS (default 100,000,000), SEED (default 20260923)
set -euo pipefail
cd "$(dirname "$0")/.."

ROUNDS="${ROUNDS:-100000000}"
SEED="${SEED:-20260923}"
MATH=math/neon_fortune.json

python3 tools/slotmath.py "$MATH" > /dev/null   # premise check (WILD placement, SCATTER spacing)

dotnet build -c Release sdk/dotnet/SlotSdk.Simulator -v q -nologo
dotnet sdk/dotnet/SlotSdk.Simulator/bin/Release/net8.0/slotsim.dll report "$MATH" \
  --rounds "$ROUNDS" --seed "$SEED" --out math/neon_fortune.sim.json

python3 tools/build_math_spec.py
python3 tools/build_game_spec.py

UNITY_MATH=unity/Assets/_Game/Resources/Math/neon_fortune.json
if [[ -d unity/Assets ]]; then
  mkdir -p "$(dirname "$UNITY_MATH")"
  cp "$MATH" "$UNITY_MATH"
  echo "synced $UNITY_MATH"
fi
echo "done. Opening the .xlsx files in Excel recalculates every formula."
