"""Generate math/neon_fortune.json (reproducible).

Symbol counts were decided by a search for the targets (RTP≈96%, FS trigger≈1/140, FS share≈18%).
Arrangement is a seeded shuffle, retried until:
  - the same symbol is never adjacent (cyclic)
  - SCATTERs are at least 3 apart (cyclic)
To change counts or the paytable, edit this file and rerun → check with slotmath.py / the simulator.
"""
import json
import random
from pathlib import Path

SEED = 20260923

COUNTS = [
    {"SEVEN": 2, "DIAMOND": 4, "BELL": 4, "CHERRY": 5, "A": 9, "K": 5, "Q": 7, "J": 7, "SCATTER": 1},
    {"SEVEN": 1, "DIAMOND": 3, "BELL": 4, "CHERRY": 4, "A": 6, "K": 6, "Q": 7, "J": 7, "SCATTER": 2, "WILD": 3},
    {"SEVEN": 2, "DIAMOND": 2, "BELL": 3, "CHERRY": 4, "A": 6, "K": 6, "Q": 7, "J": 6, "SCATTER": 1, "WILD": 5},
    {"SEVEN": 2, "DIAMOND": 3, "BELL": 4, "CHERRY": 5, "A": 6, "K": 6, "Q": 6, "J": 7, "SCATTER": 2, "WILD": 4},
    {"SEVEN": 2, "DIAMOND": 3, "BELL": 4, "CHERRY": 5, "A": 7, "K": 6, "Q": 7, "J": 7, "SCATTER": 1},
]

SYMBOLS = [
    {"id": "WILD", "type": "wild", "name": "Wild"},
    {"id": "SCATTER", "type": "scatter", "name": "Star"},
    {"id": "SEVEN", "type": "normal", "name": "Seven"},
    {"id": "DIAMOND", "type": "normal", "name": "Diamond"},
    {"id": "BELL", "type": "normal", "name": "Bell"},
    {"id": "CHERRY", "type": "normal", "name": "Cherry"},
    {"id": "A", "type": "normal", "name": "A"},
    {"id": "K", "type": "normal", "name": "K"},
    {"id": "Q", "type": "normal", "name": "Q"},
    {"id": "J", "type": "normal", "name": "J"},
]

# Pays for 1..5 of a kind (x line bet)
PAYTABLE = {
    "SEVEN":   [0, 2, 50, 200, 1000],
    "DIAMOND": [0, 0, 30, 100, 500],
    "BELL":    [0, 0, 20, 60, 250],
    "CHERRY":  [0, 0, 15, 40, 150],
    "A":       [0, 0, 10, 25, 100],
    "K":       [0, 0, 8, 20, 80],
    "Q":       [0, 0, 5, 15, 60],
    "J":       [0, 0, 5, 10, 50],
}

PAYLINES = [
    [1, 1, 1, 1, 1], [0, 0, 0, 0, 0], [2, 2, 2, 2, 2], [0, 1, 2, 1, 0], [2, 1, 0, 1, 2],
    [0, 0, 1, 2, 2], [2, 2, 1, 0, 0], [1, 0, 0, 0, 1], [1, 2, 2, 2, 1], [1, 0, 1, 2, 1],
    [1, 2, 1, 0, 1], [0, 1, 1, 1, 0], [2, 1, 1, 1, 2], [0, 1, 0, 1, 0], [2, 1, 2, 1, 2],
    [1, 1, 0, 1, 1], [1, 1, 2, 1, 1], [0, 0, 2, 0, 0], [2, 2, 0, 2, 2], [0, 2, 2, 2, 0],
]


def ok(strip):
    n = len(strip)
    if any(strip[i] == strip[(i + 1) % n] for i in range(n)):
        return False
    pos = [i for i, s in enumerate(strip) if s == "SCATTER"]
    if len(pos) > 1:
        gaps = [(pos[(j + 1) % len(pos)] - pos[j]) % n for j in range(len(pos))]
        if min(gaps) < 3:
            return False
    return True


def arrange(counts, rng):
    pool = [s for s, c in counts.items() for _ in range(c)]
    for _ in range(100000):
        rng.shuffle(pool)
        if ok(pool):
            return list(pool)
    raise RuntimeError("no valid arrangement")


def main():
    rng = random.Random(SEED)
    model = {
        "id": "neon_fortune",
        "name": "Neon Fortune",
        "version": "1.0.0",
        "grid": {"reels": 5, "rows": 3},
        "symbols": SYMBOLS,
        "wild": {"symbol": "WILD"},
        "scatter": {"symbol": "SCATTER", "pays": [0, 0, 2, 10, 50], "triggerCount": 3},
        "freeSpins": {"awardSpins": 10, "retriggerSpins": 10, "multiplier": 3},
        "bet": {"lines": 20, "lineBetLevels": [1, 2, 5, 10], "defaultLevel": 0},
        "paytable": PAYTABLE,
        "paylines": PAYLINES,
        "reels": [arrange(c, rng) for c in COUNTS],
    }
    out = Path(__file__).resolve().parent.parent / "math" / "neon_fortune.json"
    # One reel strip per line so it stays readable
    text = json.dumps(model, indent=2, ensure_ascii=False)
    out.write_text(compact_arrays(text) + "\n", encoding="utf-8")
    print(f"wrote {out}")


def compact_arrays(text):
    """Collapse innermost arrays (lists of numbers or strings) onto one line."""
    import re
    return re.sub(r"\[\s*((?:(?:-?\d+|\"[^\"]*\"),\s*)*(?:-?\d+|\"[^\"]*\"))\s*\]",
                  lambda mm: "[" + ", ".join(x.strip() for x in mm.group(1).split(",")) + "]", text)


if __name__ == "__main__":
    main()
