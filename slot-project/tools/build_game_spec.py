"""Generate the Excel game spec (docs/game_spec.xlsx).

The values here (timings, colors, win presentation thresholds, save keys) are what the Unity implementation follows.
Their implementation counterpart is unity/Assets/_Game/Scripts/Runtime/GameSpec.cs. When you change one, change the other.

Usage: python3 tools/build_game_spec.py
"""
import json
from datetime import date
from pathlib import Path

from openpyxl import Workbook
from openpyxl.styles import Alignment, Font, PatternFill

import xlstyle as S
from xlstyle import cell, header, title, widths

ROOT = Path(__file__).resolve().parent.parent
MATH = ROOT / "math" / "neon_fortune.json"
OUT = ROOT / "docs" / "game_spec.xlsx"
VERSION = "1.0.0"


def table(ws, row, labels, data, col_widths=None, wrap_height=None):
    header(ws, row, 1, labels, col_widths)
    for i, rec in enumerate(data, start=row + 1):
        for j, v in enumerate(rec, start=1):
            cell(ws, i, j, v, S.BOLD if j == 1 else S.BODY, align=S.TOP_LEFT)
        if wrap_height:
            ws.row_dimensions[i].height = wrap_height
    return row + 1 + len(data)


def main():
    m = json.loads(MATH.read_text(encoding="utf-8"))
    wb = Workbook()
    wb.remove(wb.active)
    cover(wb, m)
    overview(wb, m)
    screen(wb)
    flow(wb)
    controls(wb)
    symbols(wb, m)
    reel_timing(wb)
    win_presentation(wb)
    free_spins(wb, m)
    texts(wb)
    sounds(wb)
    save_data(wb)
    nonfunctional(wb)
    open_issues(wb)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    wb.calculation.fullCalcOnLoad = True  # Excel recalculates every formula when opened
    wb.save(OUT)
    print(f"wrote {OUT}")


def cover(wb, m):
    ws = wb.create_sheet("Cover")
    title(ws, f"{m['name']}  Game Spec")
    widths(ws, {"A": 22, "B": 70, "C": 16, "D": 16})
    info = [("Document", "Game spec (presentation, screens, controls, flow)"), ("Version", VERSION),
            ("Created", date.today().isoformat()), ("Author", "TakuroidX (AI Director) / Claude Code"),
            ("Related docs", "math_spec.xlsx (MATH spec: pays, reel strips, RTP)"),
            ("Implementation", "Unity (unity/) + Slot SDK (sdk/package)")]
    for i, (k, v) in enumerate(info, start=3):
        cell(ws, i, 1, k, S.BOLD, fill=S.SECTION_FILL)
        cell(ws, i, 2, v)
    ws["A11"] = "Sheets"
    ws["A11"].font = S.SUBTITLE
    sheets = ["Overview", "Screen Layout", "Game Flow", "Controls", "Symbol Visuals", "Reel Timing", "Win Presentation",
              "Free Spins", "Screen Text", "Sound", "Save Data", "Non-Functional", "Open Issues"]
    for i, s in enumerate(sheets, start=12):
        cell(ws, i, 1, s)
    r = 12 + len(sheets) + 1
    ws.cell(row=r, column=1, value="Revision History").font = S.SUBTITLE
    table(ws, r + 1, ["Version", "Changes", "Date", "Author"],
          [(VERSION, "Initial version", date.today().isoformat(), "TakuroidX")])


def overview(wb, m):
    ws = wb.create_sheet("Overview")
    title(ws, "Game Overview")
    rows = [
        ("Title", m["name"]),
        ("Genre", "Video slot (5 reels x 3 rows, 20 fixed lines)"),
        ("Theme", "Neon signs of a night city. Dark background, glowing neon colors for the symbols."),
        ("Platforms", "PC (Windows / macOS) via Unity. 1920x1080 landscape (scales with CanvasScaler)"),
        ("Target", "Solo play (hobby / learning project)"),
        ("Currency", "Play-only credits. Can't be bought or cashed out. Starting balance 10,000 credits"),
        ("Refill", "When the balance drops below the minimum bet (20), a REFILL button appears → balance becomes 10,000"),
        ("Bet", f"Line bet {m['bet']['lineBetLevels']} x {m['bet']['lines']} lines = total bet "
                 f"{[x * m['bet']['lines'] for x in m['bet']['lineBetLevels']]}"),
        ("Main features", "WILD substitution / SCATTER pays / free spins (10 spins, x3, retriggers)"),
        ("Design RTP", "95.98% (see math_spec.xlsx)"),
        ("Disclaimer", "Not intended for real-money gambling. The RNG isn't certified by any third party."),
    ]
    widths(ws, {"A": 20, "B": 100})
    for i, (k, v) in enumerate(rows, start=3):
        cell(ws, i, 1, k, S.BOLD, fill=S.SECTION_FILL)
        cell(ws, i, 2, v)


def screen(wb):
    ws = wb.create_sheet("Screen Layout")
    title(ws, "Screen Layout (1920x1080)", "The mockup below is to scale-ish. Coordinates are Canvas coordinates (reference resolution 1920x1080, origin at center).")
    # ---- Mockup made of cells (columns B..U = 20 columns, rows 4..22) ----
    for c in range(2, 23):
        ws.column_dimensions[ws.cell(row=1, column=c).column_letter].width = 5.5
    for r in range(4, 23):
        ws.row_dimensions[r].height = 20
    bg = PatternFill("solid", fgColor="0B1020")
    for r in range(4, 23):
        for c in range(2, 23):
            ws.cell(row=r, column=c).fill = bg

    def block(r1, c1, r2, c2, text, color, font_color="FFFFFF", size=10):
        ws.merge_cells(start_row=r1, start_column=c1, end_row=r2, end_column=c2)
        x = ws.cell(row=r1, column=c1, value=text)
        x.fill = PatternFill("solid", fgColor=color)
        x.font = Font(name=S.FONT_NAME, size=size, bold=True, color=font_color)
        x.alignment = Alignment(horizontal="center", vertical="center", wrap_text=True)

    block(4, 2, 5, 7, "NEON FORTUNE", "1B2340", "FF4FD8", 14)
    block(4, 8, 5, 15, "FREE SPINS 3 / 10   x3\n(FS only)", "1B2340", "FFC400")
    block(4, 16, 5, 22, "CREDIT\n10,000", "1B2340", "29D3FF")
    reel_colors = ["2A1E4F", "241A45"]
    for reel in range(5):
        c1 = 3 + reel * 4
        for row in range(3):
            r1 = 7 + row * 3
            block(r1, c1, r1 + 2, c1 + 2, f"R{reel + 1}\nrow{row}", reel_colors[(reel + row) % 2], "B8B8D0", 9)
    block(7, 2, 15, 2, "L\nI\nN\nE", "11172B", "666A80", 8)
    block(7, 22, 15, 22, "L\nI\nN\nE", "11172B", "666A80", 8)
    block(17, 3, 17, 21, "LINE 5  SEVEN x3  WIN 250  (win info / messages)", "11172B", "FFFFFF")
    block(19, 2, 21, 4, "PAYTABLE", "303A5C")
    block(19, 5, 21, 6, "BET\n−", "303A5C")
    block(19, 7, 21, 9, "BET\n20", "1B2340", "FFD54A")
    block(19, 10, 21, 11, "BET\n+", "303A5C")
    block(19, 12, 21, 15, "WIN\n0", "1B2340", "4ADE80", 12)
    block(19, 16, 21, 17, "AUTO", "303A5C")
    block(19, 18, 21, 18, "TURBO", "303A5C", size=8)
    block(19, 19, 21, 22, "SPIN", "FF3B5C", size=14)

    # ---- Element definitions ----
    r = 25
    ws.cell(row=r - 1, column=2, value="UI elements").font = S.SUBTITLE
    data = [
        ("UI-01", "Title logo", "Top left (-760, 485)", "NEON FORTUNE. Pink neon", ""),
        ("UI-02", "Free spin indicator", "Top center (0, 485)", "FREE SPINS {played}/{total}  x{mult}", "FS only"),
        ("UI-03", "Credit display", "Top right (760, 485)", "CREDIT + balance (comma-separated)", "Counts up by the win amount"),
        ("UI-04", "Reel area", "Center (0, 40) 1164x600", "5 reels x 3 rows. Cell 200x200, reel spacing 16, frame glow", "Masked outside"),
        ("UI-05", "Line indicators", "Left and right of the reel area", "Line number markers 1–20. Winning lines light up", ""),
        ("UI-06", "Message bar", "(0, -300) 1164x56", "Idle: GOOD LUCK / on a win: line details / FS: messages", ""),
        ("UI-07", "PAYTABLE button", "Bottom left", "Opens the paytable overlay", "Only while idle"),
        ("UI-08", "BET −/+ buttons", "Bottom", "Moves through the 4 levels (no wraparound)", "Disabled during spins and FS"),
        ("UI-09", "Bet display", "Bottom", "Total bet (line bet x 20)", ""),
        ("UI-10", "WIN display", "Bottom center", "Win for this spin (FS: cumulative)", "Count-up"),
        ("UI-11", "AUTO button", "Bottom right", "Toggle auto play. Lit while ON", ""),
        ("UI-12", "TURBO button", "Bottom right", "Turbo on/off (shorter reel timing)", "Lit while ON"),
        ("UI-13", "SPIN button", "Bottom right (largest)", "Start a spin. During a spin: STOP (stop immediately)", "Space key also works"),
        ("UI-14", "Paytable overlay", "Full screen", "Current pays (x line bet already applied) and rule text. Close button", ""),
        ("UI-15", "Big win overlay", "Full screen", "BIG/MEGA/EPIC WIN + count-up", "Tap to skip"),
        ("UI-16", "FS banner", "Full screen", "FREE SPINS! / +10 SPINS / TOTAL WIN", "Tap to skip"),
        ("UI-17", "REFILL button", "Bottom center (replaces WIN)", "Shown when the balance is below the minimum bet", ""),
    ]
    spans = [(2, 2), (3, 5), (6, 9), (10, 17), (18, 21)]  # (first column, last column)
    for (c1, c2), label in zip(spans, ["ID", "Element", "Position / size", "Contents", "Notes"]):
        if c2 > c1:
            ws.merge_cells(start_row=r, start_column=c1, end_row=r, end_column=c2)
        h = ws.cell(row=r, column=c1, value=label)
        h.font = S.HEADER
        h.fill = S.HEADER_FILL
        h.alignment = S.CENTER
    for i, rec in enumerate(data, start=r + 1):
        ws.row_dimensions[i].height = 30
        for (c1, c2), v in zip(spans, rec):
            if c2 > c1:
                ws.merge_cells(start_row=i, start_column=c1, end_row=i, end_column=c2)
            cell(ws, i, c1, v, S.BOLD if c1 == 2 else S.BODY, align=S.TOP_LEFT)


def flow(wb):
    ws = wb.create_sheet("Game Flow")
    title(ws, "Game Flow (state transitions)", "Implementation: the SlotController state machine. Math decisions (stops, wins, FS state) are all delegated to the SDK's SlotGame.")
    data = [
        ("S0 Boot", "Load the math JSON, restore save data, build the screen", "Done → S1 (if resuming FS mid-feature → S6)"),
        ("S1 Idle", "Waiting for input. BET/PAYTABLE/AUTO/TURBO available", "SPIN (with enough balance) → S2 / AUTO ON → S2"),
        ("S2 Spin start", "Debit the bet (SDK) → decide the stops (SDK) → all reels start spinning", "Immediately → S3"),
        ("S3 Reel stop", "Stop reels 1→5 in order. Anticipation applies (see Reel Timing)", "SPIN pressed again: stop all immediately. All stopped → S4"),
        ("S4 Result", "Evaluate the win tier. Line / scatter presentation", "Win ≥ 10x → S5 / FS triggered → S6 / otherwise → S1 (auto: next spin)"),
        ("S5 Big win", "Big win overlay + count-up", "Done or tapped → FS triggered ? S6 : S1"),
        ("S6 FS intro", "FREE SPINS! banner (2.0s, tap to skip)", "→ S7"),
        ("S7 FS spin", "No bet. Auto spins at 0.4s intervals. Same reel stop / result presentation", "Retrigger → +10 banner (1.5s) / remaining 0 → S8"),
        ("S8 FS end", "TOTAL WIN banner (3.0s, tap to skip). Adds the total", "→ S1"),
    ]
    table(ws, 3, ["State", "What happens", "Transition condition → next"], data, [18, 70, 60], 32)
    ws["A14"] = "Auto play stop conditions: AUTO pressed again / insufficient balance / BIG WIN or above / free spins triggered (auto continues after FS ends)"
    ws["A14"].font = S.BOLD


def controls(wb):
    ws = wb.create_sheet("Controls")
    title(ws, "Controls")
    data = [
        ("SPIN", "Space / Enter", "Idle with enough balance", "Start a spin"),
        ("STOP", "Space / Enter", "Reels spinning", "Stop all reels immediately (at the already-decided stops; the result doesn't change)"),
        ("BET −", "↓ / -", "Idle and not in FS", "Lower the bet level by one"),
        ("BET +", "↑ / +", "Idle and not in FS", "Raise the bet level by one"),
        ("AUTO", "A", "Always", "Toggle auto play"),
        ("TURBO", "T", "Always", "Toggle turbo"),
        ("PAYTABLE", "P", "Idle", "Show / close the paytable"),
        ("Skip", "Space / click", "Big win / banners showing", "End the presentation immediately"),
        ("REFILL", "—", "Balance below minimum bet", "Set the balance to 10,000"),
        ("Debug: force FS", "F9", "Development builds / Editor only", "Next spin uses trigger stops (for testing)"),
    ]
    table(ws, 3, ["Control", "Key", "Enabled when", "Action"], data, [18, 18, 30, 70])


# Symbol colors (shared with GameSpec.cs)
SYMBOL_VISUALS = [
    ("WILD", "WILD", "#B04DFF", "Rounded rect + WILD text", "Purple neon. Pulses when part of a win"),
    ("SCATTER", "(star)", "#FFC400", "Star shape (drawn procedurally)", "Glows when it lands. Every star pulses on an FS trigger"),
    ("SEVEN", "7", "#FF3B5C", "Rounded rect + 7", "Top symbol"),
    ("DIAMOND", "(diamond)", "#29D3FF", "Diamond shape (drawn procedurally)", ""),
    ("BELL", "BELL", "#FFD54A", "Rounded rect + text", ""),
    ("CHERRY", "CHERRY", "#FF6F91", "Rounded rect + text", ""),
    ("A", "A", "#4ADE80", "Rounded rect + text", "Low symbol"),
    ("K", "K", "#60A5FA", "Rounded rect + text", "Low symbol"),
    ("Q", "Q", "#F472B6", "Rounded rect + text", "Low symbol"),
    ("J", "J", "#FB923C", "Rounded rect + text", "Low symbol"),
]


def symbols(wb, m):
    ws = wb.create_sheet("Symbol Visuals")
    title(ws, "Symbol Visuals", "v1 uses placeholder art drawn procedurally (no image assets needed). Colors are shared with the implementation (GameSpec.cs).")
    header(ws, 3, 1, ["ID", "Label", "Color", "Swatch", "Shape", "Presentation"], [12, 12, 12, 10, 34, 50])
    for i, (sid, label, color, shape, fx) in enumerate(SYMBOL_VISUALS, start=4):
        cell(ws, i, 1, sid, S.BOLD)
        cell(ws, i, 2, label)
        cell(ws, i, 3, color)
        cell(ws, i, 4, "", fill=PatternFill("solid", fgColor=color[1:]))
        cell(ws, i, 5, shape)
        cell(ws, i, 6, fx)
    ws.cell(row=15, column=1, value="Payline colors: 20 lines drawn cyclically from a hue circle (HSV, hue = index/20, S=0.7, V=1)").font = S.NOTE


REEL_TIMING = [
    ("Reel spin speed", "symbols/s", 22, 34, ""),
    ("Spin-up time", "s", 0.12, 0.06, "Slight back-pull then accelerate"),
    ("Minimum spin time (reel 1)", "s", 0.60, 0.20, "From spin start until reel 1 begins stopping"),
    ("Reel stop interval", "s", 0.16, 0.05, "Interval between reel i and reel i+1 stopping"),
    ("Stop bounce", "s", 0.10, 0.06, "Overshoot 12% of a cell, then settle"),
    ("Anticipation extension", "s", 0.90, 0.60, "Extra spin for each later reel when SCATTERs so far ≥ 2"),
    ("FS auto spin interval", "s", 0.40, 0.20, "Wait from the end of result presentation to the next FS spin"),
    ("Auto play interval", "s", 0.30, 0.10, "Wait until the next spin"),
]


def reel_timing(wb):
    ws = wb.create_sheet("Reel Timing")
    title(ws, "Reel Timing", "Implementation counterpart: GameSpec.cs / ReelTiming. Stops are decided by the SDK the moment the spin starts; presentation only changes the display.")
    header(ws, 3, 1, ["Item", "Unit", "Normal", "Turbo", "Notes"], [30, 12, 12, 12, 60])
    for i, rec in enumerate(REEL_TIMING, start=4):
        for j, v in enumerate(rec, start=1):
            cell(ws, i, j, v, S.INPUT if j in (3, 4) else (S.BOLD if j == 1 else S.BODY),
                 fill=S.INPUT_FILL if j in (3, 4) else None)
    ws["A14"] = "Anticipation condition: when a reel stops, if SCATTERs visible so far ≥ trigger count − 1 and reels remain, extend each remaining reel and highlight it."
    ws["A14"].font = S.BOLD


WIN_TIERS = [
    ("None", 0, "—", "No presentation", 0),
    ("Normal", 0.000001, "Line/scatter highlight + WIN count-up", "Count-up 0.6s", 0.6),
    ("BIG WIN", 10, "Full-screen overlay (gold)", "Count-up 3.0s", 3.0),
    ("MEGA WIN", 25, "Full-screen overlay (pink)", "Count-up 4.5s", 4.5),
    ("EPIC WIN", 50, "Full-screen overlay (cyan)", "Count-up 6.0s", 6.0),
]


def win_presentation(wb):
    ws = wb.create_sheet("Win Presentation")
    title(ws, "Win Presentation", "Tiers are decided by the spin's total win ÷ total bet (in FS, each spin's win compared to the bet at trigger).")
    header(ws, 3, 1, ["Tier", "Threshold (x total bet or more)", "Presentation", "Count-up", "Seconds"], [14, 22, 40, 20, 10])
    for i, rec in enumerate(WIN_TIERS, start=4):
        for j, v in enumerate(rec, start=1):
            cell(ws, i, j, v, S.INPUT if j in (2, 5) else (S.BOLD if j == 1 else S.BODY),
                 fill=S.INPUT_FILL if j in (2, 5) else None)
    ws.cell(row=10, column=1, value="Line win display").font = S.SUBTITLE
    data = [
        ("1. All lines at once", "Highlight every winning line's cells and line markers together (1.0s)"),
        ("2. Line by line", "Show each winning line in turn for 0.8s. Message: LINE n  SYMBOL xCount  WIN amount"),
        ("3. Scatter", "If there's a scatter win, pulse the SCATTER cells. Message: SCATTER xCount  WIN amount"),
        ("Loop", "While idle, keep cycling through step 2. Stops at the next spin"),
        ("Dimming", "Cells not part of a win are dimmed to 45% brightness"),
    ]
    table(ws, 11, ["Step", "Contents"], data, [22, 90])


def free_spins(wb, m):
    ws = wb.create_sheet("Free Spins")
    title(ws, "Free Spin Spec")
    fs = m["freeSpins"]
    data = [
        ("Trigger", f"{m['scatter']['triggerCount']}+ SCATTERs anywhere in the base game"),
        ("Spins awarded", f"{fs['awardSpins']} spins"),
        ("Multiplier", f"Every win x{fs['multiplier']} (line and scatter)"),
        ("Retrigger", f"{m['scatter']['triggerCount']}+ during FS → +{fs['retriggerSpins']} spins (no limit)"),
        ("Bet", "Fixed at the trigger bet. No bet deduction during FS"),
        ("Reel strips", "Same as the base game"),
        ("Presentation", "Background tint shifts toward purple. Top shows FREE SPINS played/total x3"),
        ("Controls", "Fully automatic. SPIN button acts as STOP only"),
        ("Suspend / resume", "Quitting mid-FS saves remaining spins and cumulative win, and resumes on next launch"),
        ("End", "TOTAL WIN banner (total FS win) → back to the base game"),
    ]
    table(ws, 3, ["Item", "Spec"], data, [20, 90])


def texts(wb):
    ws = wb.create_sheet("Screen Text")
    title(ws, "Screen Text", "v1 shows English on screen (to avoid font dependencies). Japanese is reference for future localization.")
    data = [
        ("title", "NEON FORTUNE", "ネオンフォーチュン"),
        ("credit", "CREDIT", "クレジット"),
        ("bet", "BET", "ベット"),
        ("win", "WIN", "獲得"),
        ("spin", "SPIN", "スピン"),
        ("stop", "STOP", "ストップ"),
        ("auto", "AUTO", "オート"),
        ("turbo", "TURBO", "ターボ"),
        ("paytable", "PAYTABLE", "配当表"),
        ("refill", "REFILL", "補充"),
        ("idle", "GOOD LUCK!", "幸運を！"),
        ("line_win", "LINE {n}  {symbol} x{count}  WIN {amount}", "ライン{n} {symbol}×{count} 獲得{amount}"),
        ("scatter_win", "SCATTER x{count}  WIN {amount}", "スキャッター×{count} 獲得{amount}"),
        ("fs_intro", "FREE SPINS!\n{spins} SPINS  ALL WINS x{mult}", "フリースピン！{spins}回 全配当×{mult}"),
        ("fs_retrigger", "+{spins} SPINS!", "+{spins}回！"),
        ("fs_counter", "FREE SPINS {played}/{total}  x{mult}", "フリースピン {played}/{total} ×{mult}"),
        ("fs_total", "TOTAL WIN\n{amount}", "合計獲得 {amount}"),
        ("big", "BIG WIN", "ビッグウィン"),
        ("mega", "MEGA WIN", "メガウィン"),
        ("epic", "EPIC WIN", "エピックウィン"),
        ("no_credit", "NOT ENOUGH CREDIT", "クレジット不足"),
    ]
    table(ws, 3, ["Key", "Display (EN)", "Japanese (reference)"], data, [16, 50, 50])


def sounds(wb):
    ws = wb.create_sheet("Sound")
    title(ws, "Sound List", "v1 plays short synthesized tones (generated in code). Swapping in real assets is future work.")
    data = [
        ("SE-01", "Spin start", "Short rising sweep", "Implemented (synth)"),
        ("SE-02", "Reel stop", "Short click (for each reel)", "Implemented (synth)"),
        ("SE-03", "Anticipation", "Rising tone during the extension", "Implemented (synth)"),
        ("SE-04", "SCATTER lands", "High chime", "Implemented (synth)"),
        ("SE-05", "Normal win", "Short arpeggio", "Implemented (synth)"),
        ("SE-06", "Big win", "Long fanfare", "Not implemented"),
        ("SE-07", "FS start / end", "Jingle", "Not implemented"),
        ("BGM-01", "Base game", "Loop", "Not implemented"),
        ("BGM-02", "Free spins", "Loop (higher tension)", "Not implemented"),
    ]
    table(ws, 3, ["ID", "Situation", "Image", "Status"], data, [10, 20, 40, 24])


def save_data(wb):
    ws = wb.create_sheet("Save Data")
    title(ws, "Save Data (PlayerPrefs)", "Saved after every spin ends. Keys are prefixed with nf.")
    data = [
        ("nf.balance", "string (long)", "Balance", "10000"),
        ("nf.betLevel", "int", "Bet level (0–3)", "0"),
        ("nf.fsRemaining", "int", "FS spins remaining", "0"),
        ("nf.fsPlayed", "int", "FS spins played", "0"),
        ("nf.fsTotalWin", "string (long)", "Cumulative FS win", "0"),
        ("nf.turbo", "int (0/1)", "Turbo setting", "0"),
        ("nf.mathVersion", "string", "Math version at save time. If different, reset FS state", "1.0.0"),
    ]
    table(ws, 3, ["Key", "Type", "Contents", "Initial"], data, [18, 16, 60, 12])


def nonfunctional(wb):
    ws = wb.create_sheet("Non-Functional")
    title(ws, "Non-Functional Requirements and Constraints")
    data = [
        ("Performance", "60fps target. No per-frame allocations during spins (reuse symbol cells)"),
        ("RNG", "Real play: SDK CryptoRng (OS cryptographic RNG). Tests: Xoshiro256** (seeded)"),
        ("Separating result from presentation", "Stops and wins are decided by the SDK at spin start. Presentation (STOP / turbo / skip) never affects the result"),
        ("Math consistency", "Unity loads math/neon_fortune.json (copied into Resources). Changes flow Excel → JSON → verification → copy"),
        ("Tests", "SDK: xUnit (theory, full cycle, MC). Unity: EditMode tests (math loads, GameSpec consistency)"),
        ("Legal / ethics", "No real-money gambling. Don't publish as a real-money game. Play credits can't be bought"),
    ]
    table(ws, 3, ["Item", "Contents"], data, [26, 100])


def open_issues(wb):
    ws = wb.create_sheet("Open Issues")
    title(ws, "Open Issues / Future Work")
    data = [
        ("1", "Real art (sprites) and animation", "v1 is procedural placeholders"),
        ("2", "BGM / real sound effects", "v1 is synthesized tones"),
        ("3", "Dedicated free spin reel strips (tuning volatility)", "Currently the same as the base game"),
        ("4", "Max win cap", "None today. Consider when designing high volatility"),
        ("5", "Localization (Japanese display)", "Needs a font strategy (TextMeshPro + Japanese font)"),
        ("6", "Mobile (portrait) layout", "Undecided"),
    ]
    table(ws, 3, ["No", "Topic", "Notes"], data, [6, 50, 50])


if __name__ == "__main__":
    main()
