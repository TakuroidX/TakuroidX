# Slot machine project plan (personal hobby / learning, no real money)

Status: **not started (design only)** — next session starts at step 1.

## Steps
1. **Specs (Excel)** — generated from Python (openpyxl)
   - `docs/math_spec.xlsx`: symbols, paytable, reel strips, paylines, line RTP (computed with Excel formulas),
     scatter probability (32-pattern enumeration), free spin expectation, RTP breakdown, simulation results
   - `docs/game_spec.xlsx`: overview, screen layout, game flow (state transitions), controls, win presentation tiers, FS spec, sounds, change log
   - Source of truth is `math/neon_fortune.json`; the Excel file is generated from it (plus an xlsx→json export script)
2. **SDK (C#, netstandard2.1, no dependencies)** — `sdk/package/` (Unity UPM package) + `sdk/dotnet/` (csproj/tests)
   - Config loading (built-in mini JSON), validation, RNG (xoshiro256**, Lemire range), evaluator, SlotGame (balance, bet, FS state)
   - **MATH simulator** (console): `theory` (exact formula calc = must match Excel) / `mc` (Monte Carlo + confidence interval) / `exact` (full-cycle base game enumeration)
3. **Sample game**: console slot (`samples/ConsoleSlot`) to check the SDK's behavior
4. **Full implementation**: Unity project (built on `unity-claude-starter`), reference the SDK as a local UPM package, generate UI procedurally in code

## Math draft (Neon Fortune, 5x3, 20 fixed lines)
- WILD: reels 2–4 only, substitutes for everything except SCATTER, no pay of its own → no WILD on reel 1, so each line's paying symbol = reel 1's symbol (exact calc is easy)
- SCATTER: anywhere, 3/4/5 = 2/10/50x total bet, 3+ triggers 10 FS (all wins x2 in FS, 3+ retriggers +10)
- Paying symbols (x line bet, 3/4/5): SEVEN 50/200/1000 (2-of-a-kind 2), DIAMOND 30/100/500, BELL 20/60/250, CHERRY 15/40/150, A 10/25/100, K 8/20/80, Q 5/15/60, J 5/10/50
- Place SCATTER at least 3 apart on each reel (at most 1 visible per reel)
- RTP = B + P_trig × 10/(1−10·P_trig) × 2B (B = base line + scatter RTP), target 96%
- Build notes: `apt-get install dotnet-sdk-8.0` works (dot.net downloads are blocked by the proxy); NuGet and pip are available
