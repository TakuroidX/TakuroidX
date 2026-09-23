using System.Collections.Generic;

namespace SlotSdk
{
    public readonly struct LineWin
    {
        /// <summary>0-based payline number</summary>
        public int LineIndex { get; }
        /// <summary>Paying symbol index (WILD only if the whole run is WILDs)</summary>
        public int Symbol { get; }
        /// <summary>Number of consecutive matching symbols from reel 1</summary>
        public int Count { get; }
        /// <summary>Win in credits (multiplier applied)</summary>
        public long Win { get; }

        public LineWin(int lineIndex, int symbol, int count, long win)
        {
            LineIndex = lineIndex;
            Symbol = symbol;
            Count = count;
            Win = win;
        }
    }

    public readonly struct CellPos
    {
        public int Reel { get; }
        public int Row { get; }
        public CellPos(int reel, int row) { Reel = reel; Row = row; }
    }

    public sealed class SpinResult
    {
        /// <summary>Stop position of each reel (the strip index shown in the top row)</summary>
        public int[] Stops { get; internal set; }
        /// <summary>Window[reel][row] = symbol index</summary>
        public int[][] Window { get; internal set; }
        public List<LineWin> LineWins { get; } = new List<LineWin>();
        public List<CellPos> ScatterPositions { get; } = new List<CellPos>();
        public int ScatterCount => ScatterPositions.Count;
        public long ScatterWin { get; internal set; }
        public long LineWinTotal { get; internal set; }
        public long TotalWin => LineWinTotal + ScatterWin;
        public int Multiplier { get; internal set; } = 1;
        public long LineBet { get; internal set; }
        public long TotalBet { get; internal set; }

        // ---- Game state (filled in by SlotGame) ----
        /// <summary>Whether this spin was a free spin</summary>
        public bool IsFreeSpin { get; internal set; }
        /// <summary>Number of free spins awarded by this spin (initial trigger or retrigger)</summary>
        public int FreeSpinsAwarded { get; internal set; }
        /// <summary>Base game spin → entered free spins</summary>
        public bool FeatureTriggered { get; internal set; }
        /// <summary>This spin ended the free spins</summary>
        public bool FeatureEnded { get; internal set; }
        /// <summary>Total free spin win so far (final value when FeatureEnded)</summary>
        public long FeatureTotalWin { get; internal set; }
        public int FreeSpinsRemaining { get; internal set; }
        public long BalanceAfter { get; internal set; }
    }
}
