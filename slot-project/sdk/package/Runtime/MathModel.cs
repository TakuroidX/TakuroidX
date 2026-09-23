using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SlotSdk
{
    public enum SymbolType
    {
        Normal,
        Wild,
        Scatter,
    }

    public sealed class SymbolDef
    {
        public int Index { get; }
        public string Id { get; }
        public string Name { get; }
        public SymbolType Type { get; }

        public SymbolDef(int index, string id, string name, SymbolType type)
        {
            Index = index;
            Id = id;
            Name = name;
            Type = type;
        }

        public override string ToString() => Id;
    }

    public sealed class FreeSpinConfig
    {
        public int AwardSpins { get; }
        public int RetriggerSpins { get; }
        public int Multiplier { get; }

        public FreeSpinConfig(int awardSpins, int retriggerSpins, int multiplier)
        {
            AwardSpins = awardSpins;
            RetriggerSpins = retriggerSpins;
            Multiplier = multiplier;
        }
    }

    public sealed class BetConfig
    {
        public int Lines { get; }
        public IReadOnlyList<int> LineBetLevels { get; }
        public int DefaultLevel { get; }

        public BetConfig(int lines, int[] lineBetLevels, int defaultLevel)
        {
            Lines = lines;
            LineBetLevels = lineBetLevels;
            DefaultLevel = defaultLevel;
        }
    }

    /// <summary>
    /// Slot math definition (PAR). Loaded from math/*.json and immutable.
    /// Symbols are handled internally by index (the order of the symbols array).
    /// </summary>
    public sealed class MathModel
    {
        public string Id { get; }
        public string Name { get; }
        public string Version { get; }
        public int ReelCount { get; }
        public int RowCount { get; }
        public IReadOnlyList<SymbolDef> Symbols { get; }
        public int WildSymbol { get; }
        public int ScatterSymbol { get; }
        /// <summary>ScatterPays[count] = pay as a multiple of total bet (index 0 is unused)</summary>
        public int[] ScatterPays { get; }
        public int ScatterTriggerCount { get; }
        public FreeSpinConfig FreeSpins { get; }
        public BetConfig Bet { get; }
        /// <summary>Paytable[symbol][count] = pay as a multiple of line bet (index 0 is unused)</summary>
        public int[][] Paytable { get; }
        /// <summary>Paylines[line][reel] = row (0 = top)</summary>
        public int[][] Paylines { get; }
        /// <summary>Reels[reel][stop] = symbol index</summary>
        public int[][] Reels { get; }

        private MathModel(string id, string name, string version, int reels, int rows, SymbolDef[] symbols,
            int wild, int scatter, int[] scatterPays, int triggerCount, FreeSpinConfig fs, BetConfig bet,
            int[][] paytable, int[][] paylines, int[][] reelStrips)
        {
            Id = id; Name = name; Version = version; ReelCount = reels; RowCount = rows; Symbols = symbols;
            WildSymbol = wild; ScatterSymbol = scatter; ScatterPays = scatterPays; ScatterTriggerCount = triggerCount;
            FreeSpins = fs; Bet = bet; Paytable = paytable; Paylines = paylines; Reels = reelStrips;
        }

        public int SymbolIndex(string id)
        {
            for (var i = 0; i < Symbols.Count; i++)
                if (Symbols[i].Id == id) return i;
            throw new KeyNotFoundException($"Unknown symbol '{id}'");
        }

        public static MathModel FromJson(string json)
        {
            var root = AsObject(MiniJson.Parse(json), "root");

            var grid = AsObject(root["grid"], "grid");
            var reels = AsInt(grid["reels"]);
            var rows = AsInt(grid["rows"]);

            var symbolList = AsList(root["symbols"], "symbols");
            var symbols = new SymbolDef[symbolList.Count];
            for (var i = 0; i < symbols.Length; i++)
            {
                var o = AsObject(symbolList[i], "symbols[]");
                var id = (string)o["id"];
                var name = o.TryGetValue("name", out var n) ? (string)n : id;
                symbols[i] = new SymbolDef(i, id, name, ParseType((string)o["type"]));
            }

            int Index(string id)
            {
                for (var i = 0; i < symbols.Length; i++)
                    if (symbols[i].Id == id) return i;
                throw new FormatException($"Unknown symbol '{id}'");
            }

            var wild = Index((string)AsObject(root["wild"], "wild")["symbol"]);
            var sc = AsObject(root["scatter"], "scatter");
            var scatter = Index((string)sc["symbol"]);
            var scatterPays = PrependZero(AsIntArray(sc["pays"]));
            var trigger = AsInt(sc["triggerCount"]);

            var fsObj = AsObject(root["freeSpins"], "freeSpins");
            var fs = new FreeSpinConfig(AsInt(fsObj["awardSpins"]), AsInt(fsObj["retriggerSpins"]), AsInt(fsObj["multiplier"]));

            var betObj = AsObject(root["bet"], "bet");
            var bet = new BetConfig(AsInt(betObj["lines"]), AsIntArray(betObj["lineBetLevels"]), AsInt(betObj["defaultLevel"]));

            var paytable = new int[symbols.Length][];
            for (var i = 0; i < paytable.Length; i++) paytable[i] = new int[reels + 1];
            foreach (var kv in AsObject(root["paytable"], "paytable"))
                paytable[Index(kv.Key)] = PrependZero(AsIntArray(kv.Value));

            var paylines = AsList(root["paylines"], "paylines").Select(AsIntArray).ToArray();
            var strips = AsList(root["reels"], "reels")
                .Select(r => AsList(r, "reels[]").Select(s => Index((string)s)).ToArray())
                .ToArray();

            var model = new MathModel(
                (string)root["id"], (string)root["name"], (string)root["version"], reels, rows, symbols,
                wild, scatter, scatterPays, trigger, fs, bet, paytable, paylines, strips);

            var errors = model.Validate();
            if (errors.Count > 0) throw new FormatException("Invalid math model:\n - " + string.Join("\n - ", errors));
            return model;
        }

        /// <summary>
        /// Checks the model's consistency and the premises of the theoretical calculation. Returns an empty list if there is no problem.
        /// </summary>
        public List<string> Validate()
        {
            var e = new List<string>();
            if (Reels.Length != ReelCount) e.Add($"reels: expected {ReelCount} strips, got {Reels.Length}");
            if (Symbols[WildSymbol].Type != SymbolType.Wild) e.Add("wild.symbol is not of type wild");
            if (Symbols[ScatterSymbol].Type != SymbolType.Scatter) e.Add("scatter.symbol is not of type scatter");
            if (Paylines.Length != Bet.Lines) e.Add($"bet.lines={Bet.Lines} but {Paylines.Length} paylines defined");
            if (Bet.LineBetLevels.Count == 0 || Bet.LineBetLevels.Any(x => x <= 0)) e.Add("bet.lineBetLevels must be positive");
            if (Bet.DefaultLevel < 0 || Bet.DefaultLevel >= Bet.LineBetLevels.Count) e.Add("bet.defaultLevel out of range");
            if (ScatterPays.Length != ReelCount + 1) e.Add("scatter.pays must have one entry per reel");
            if (ScatterTriggerCount < 1 || ScatterTriggerCount > ReelCount) e.Add("scatter.triggerCount out of range");

            for (var l = 0; l < Paylines.Length; l++)
            {
                if (Paylines[l].Length != ReelCount) e.Add($"payline {l + 1}: length {Paylines[l].Length}");
                else if (Paylines[l].Any(r => r < 0 || r >= RowCount)) e.Add($"payline {l + 1}: row out of range");
            }

            for (var s = 0; s < Paytable.Length; s++)
                if (Paytable[s].Length != ReelCount + 1) e.Add($"paytable {Symbols[s].Id}: must have {ReelCount} entries");

            for (var r = 0; r < Reels.Length; r++)
            {
                var strip = Reels[r];
                if (strip.Length < RowCount) { e.Add($"reel {r + 1}: shorter than row count"); continue; }
                // Theoretical-calculation premise: each line's paying symbol = reel 1's symbol
                if (r == 0 && strip.Contains(WildSymbol)) e.Add("reel 1 must not contain WILD");
                // Theoretical-calculation premise: at most 1 SCATTER visible per reel
                var pos = Enumerable.Range(0, strip.Length).Where(i => strip[i] == ScatterSymbol).ToList();
                for (var i = 0; i < pos.Count && pos.Count > 1; i++)
                {
                    var gap = (pos[(i + 1) % pos.Count] - pos[i] + strip.Length) % strip.Length;
                    if (gap < RowCount) e.Add($"reel {r + 1}: SCATTERs closer than {RowCount} stops");
                }
            }
            return e;
        }

        private static SymbolType ParseType(string t)
        {
            switch (t)
            {
                case "normal": return SymbolType.Normal;
                case "wild": return SymbolType.Wild;
                case "scatter": return SymbolType.Scatter;
                default: throw new FormatException($"Unknown symbol type '{t}'");
            }
        }

        private static int[] PrependZero(int[] a)
        {
            var r = new int[a.Length + 1];
            Array.Copy(a, 0, r, 1, a.Length);
            return r;
        }

        private static Dictionary<string, object> AsObject(object o, string what) =>
            o as Dictionary<string, object> ?? throw new FormatException($"{what}: expected object");

        private static List<object> AsList(object o, string what) =>
            o as List<object> ?? throw new FormatException($"{what}: expected array");

        private static int AsInt(object o)
        {
            if (!(o is double d) || d != Math.Floor(d))
                throw new FormatException($"Expected integer, got '{Convert.ToString(o, CultureInfo.InvariantCulture)}'");
            return (int)d;
        }

        private static int[] AsIntArray(object o) => AsList(o, "array").Select(AsInt).ToArray();
    }
}
