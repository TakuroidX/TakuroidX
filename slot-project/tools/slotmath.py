"""Exact theoretical math for Neon Fortune (same formulas as the Excel spec and the SDK's TheoreticalCalculator).

Premises (validated by validate()):
  - Reel 1 has no WILD, so each line's paying symbol = the symbol on reel 1
  - WILD substitutes for everything except SCATTER and has no pay of its own
  - SCATTERs on each reel are at least 3 apart (cyclic) → at most 1 visible per reel
"""
import itertools
import json


def load(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def counts(m):
    return [{s: reel.count(s) for s in set(reel)} for reel in m["reels"]]


def validate(m):
    rows = m["grid"]["rows"]
    wild = m["wild"]["symbol"]
    sc = m["scatter"]["symbol"]
    assert wild not in m["reels"][0], "WILD on reel 1 breaks the calculation premise"
    for i, reel in enumerate(m["reels"]):
        pos = [j for j, s in enumerate(reel) if s == sc]
        n = len(reel)
        for a, b in zip(pos, pos[1:] + [pos[0] + n] if pos else []):
            if len(pos) > 1:
                assert b - a >= rows, f"reel {i+1}: SCATTER spacing {b-a} < {rows}"
    for line in m["paylines"]:
        assert len(line) == m["grid"]["reels"] and all(0 <= r < rows for r in line)


def line_table(m):
    """Returns [(symbol, count, probability, pay, contribution)]. Contribution is per line bet (= per total bet)."""
    c = counts(m)
    N = [len(r) for r in m["reels"]]
    wild = m["wild"]["symbol"]
    out = []
    for sym, pays in m["paytable"].items():
        ge = [1.0]
        for i in range(len(N)):
            n = c[i].get(sym, 0) + (c[i].get(wild, 0) if i > 0 else 0)
            ge.append(ge[-1] * n / N[i])
        for k in range(1, len(N) + 1):
            pay = pays[k - 1]
            if pay == 0:
                continue
            p = ge[k] - (ge[k + 1] if k < len(N) else 0.0)
            out.append((sym, k, p, pay, p * pay))
    return out


def scatter_dist(m):
    c = counts(m)
    rows = m["grid"]["rows"]
    sc = m["scatter"]["symbol"]
    ps = [rows * c[i].get(sc, 0) / len(r) for i, r in enumerate(m["reels"])]
    dist = [0.0] * (len(ps) + 1)
    for mask in itertools.product([0, 1], repeat=len(ps)):
        pr = 1.0
        for p, b in zip(ps, mask):
            pr *= p if b else 1 - p
        dist[sum(mask)] += pr
    return ps, dist


def summary(m):
    lines = line_table(m)
    line_rtp = sum(x[4] for x in lines)
    ps, dist = scatter_dist(m)
    sp = m["scatter"]["pays"]
    sc_rtp = sum(dist[k] * sp[k - 1] for k in range(1, len(dist)))
    trig = sum(dist[m["scatter"]["triggerCount"]:])
    fs = m["freeSpins"]
    base = line_rtp + sc_rtp
    # Branching process: each FS spin retriggers with probability trig and adds retriggerSpins
    exp_spins = fs["awardSpins"] / (1 - fs["retriggerSpins"] * trig)
    fs_rtp = trig * exp_spins * fs["multiplier"] * base
    return {
        "lineRtp": line_rtp, "scatterRtp": sc_rtp, "baseRtp": base,
        "triggerProbability": trig, "triggerOneIn": 1 / trig,
        "expectedFreeSpins": exp_spins, "freeSpinRtp": fs_rtp,
        "totalRtp": base + fs_rtp,
    }


if __name__ == "__main__":
    import sys
    m = load(sys.argv[1] if len(sys.argv) > 1 else "math/neon_fortune.json")
    validate(m)
    for k, v in summary(m).items():
        print(f"{k:20s} {v:.6f}")
