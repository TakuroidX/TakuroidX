# Neon Fortune — a slot machine built solo (learning project)

A 5-reel × 3-row, 20-line video slot, built end to end from **math design through the game**.
A personal hobby and learning project. **Not intended for real-money gambling** (the RNG isn't third-party certified either).

```
① Specs (Excel)          ② SDK + MATH simulator        ③ Sample game          ④ Full implementation
docs/math_spec.xlsx  →   sdk/ (C#, Unity-compatible) →  samples/ConsoleSlot →   unity/ (Unity 6)
docs/game_spec.xlsx      slotsim: theory/full cycle/MC   playable in terminal    procedural UI and presentation
```

## Math results (Neon Fortune v1.0.0)

| Item | Value | Source |
|---|---|---|
| Theoretical RTP | **95.9794%** | Excel formulas = SDK TheoreticalCalculator (exact match) |
| └ Line / scatter / FS | 75.996% / 1.752% / 18.232% | Same as above |
| Base RTP (full cycle) | 77.747880% | Enumerating all 150,186,960 combinations (diff from theory 1e-16) |
| Monte Carlo RTP | 96.020% ± 0.064% | 100 million rounds (z = 1.26, consistent with theory) |
| Hit frequency | 53.85% | Full cycle |
| FS trigger | 1 in 137.9 | Theory = full cycle |
| Volatility (per-round SD) | 3.25 | Monte Carlo. Lower-middle volatility |
| Max base game win | 76x total bet | Full cycle |

Details are in the "Summary", "Simulation Results", and "Verification" sheets of `docs/math_spec.xlsx`.

## Directory layout

```
math/            Math definition (neon_fortune.json = source of truth) and simulation results
docs/            math_spec.xlsx (MATH spec) / game_spec.xlsx (game spec)
tools/           Reel design, Excel generation, Excel→JSON export, pipeline
sdk/package/     Slot SDK (netstandard2.1, no dependencies. Doubles as a Unity UPM package)
sdk/dotnet/      SDK build, xUnit tests (26), slotsim simulator
samples/         ConsoleSlot (sample game)
unity/           Unity project (built on unity-claude-starter)
```

## How to run

Requirements: .NET 8 SDK, Python 3 + openpyxl, (for the full implementation) Unity 6

```bash
# SDK tests
dotnet test sdk/SlotSdk.sln

# MATH verification (theory / full cycle / 100M-round MC) → Excel generation → sync to Unity, all in one step
./tools/make_specs.sh            # ROUNDS=2000000 ./tools/make_specs.sh to shorten

# Simulator on its own
dotnet run -c Release --project sdk/dotnet/SlotSdk.Simulator -- report math/neon_fortune.json --rounds 10000000

# Sample game (terminal)
dotnet run --project samples/ConsoleSlot
dotnet run --project samples/ConsoleSlot -- --demo 50 --seed 1   # automated demo
```

### Changing the math

1. Edit the blue cells in the "Reel Strips", "Paytable", or "Base Parameters" sheet of `docs/math_spec.xlsx` (RTP recalculates immediately in Excel)
2. `python3 tools/export_math.py docs/math_spec.xlsx math/neon_fortune.json`
3. `./tools/make_specs.sh` (SDK verifies → Excel regenerated → copied into Unity)

Alternatively, change the symbol counts in `tools/design_reels.py` and rerun it (reproducible from its seed).

## Unity (full implementation)

1. Add `unity/` in Unity Hub via "Add project from disk" (made for Unity 6000.0 LTS. If your version differs, pick the one you have)
2. On first open, `Assets/_Game/Scenes/Main.unity` is created automatically and registered in Build Settings
3. Press Play. **Space** = spin/stop/skip, **↑↓** = bet, **A** = auto, **T** = turbo, **P** = paytable, **F9** = force FS (development builds only)

- The SDK is referenced as a local package via `Packages/manifest.json` (`file:../../sdk/package`)
- The UI, symbols, and sound effects are all generated in code (no image or audio assets)
- The math stays consistent with the SDK: results (stops and wins) are decided by `SlotGame` at spin start, and presentation doesn't change them

### Verification status (being honest)

| Item | Status |
|---|---|
| SDK / simulator / Excel | Ran and verified in this environment (tests 26/26, 4 cross-checks PASS) |
| Unity C# compile | Compile-checked with dotnet against third-party repackaged Unity reference DLLs (UnityEngine modules 2021.3 / UnityEngine.UI 2020.3 / UnityEditor 2021.1): Runtime, Editor, and Tests all have 0 errors and 0 warnings |
| Unity EditMode tests | Only the Unity-independent `ReelMotionTests` (4) ran with NUnit. The rest haven't run in Unity yet |
| Unity running / look and feel | **Not yet confirmed** (Unity can't run in this environment). Needs a check in the Unity Editor on your own PC |

## Future work

See the "Open Issues" sheet in `docs/game_spec.xlsx` (real art, BGM, FS-specific reels, max win cap, Japanese localization, portrait support).
