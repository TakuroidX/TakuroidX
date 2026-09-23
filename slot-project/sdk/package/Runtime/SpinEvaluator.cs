using System;

namespace SlotSdk
{
    /// <summary>
    /// Evaluates stop positions into a window and line / scatter wins.
    /// Not thread-safe (holds internal buffers). Create one per thread.
    ///
    /// Line rules: consecutive matches from reel 1 (left to right). WILD substitutes for every symbol except SCATTER.
    /// Scatter rules: counted anywhere in the window; pays a multiple of total bet.
    /// </summary>
    public sealed class SpinEvaluator
    {
        private readonly MathModel _m;
        private readonly int[][] _window;

        public SpinEvaluator(MathModel model)
        {
            _m = model ?? throw new ArgumentNullException(nameof(model));
            _window = new int[model.ReelCount][];
            for (var r = 0; r < _window.Length; r++) _window[r] = new int[model.RowCount];
        }

        public MathModel Model => _m;

        /// <summary>Fills the window from stop positions (row = strip[(stop + row) % length])</summary>
        public void FillWindow(int[] stops, int[][] window)
        {
            for (var r = 0; r < _m.ReelCount; r++)
            {
                var strip = _m.Reels[r];
                for (var row = 0; row < _m.RowCount; row++)
                    window[r][row] = strip[(stops[r] + row) % strip.Length];
            }
        }

        /// <summary>
        /// Fast evaluation for the simulator. Win (no multiplier) in line-bet units, with scatter pays converted to line-bet units too.
        /// </summary>
        public long EvaluateUnits(int[] stops, out int scatterCount)
        {
            FillWindow(stops, _window);
            long units = 0;
            for (var l = 0; l < _m.Paylines.Length; l++)
            {
                LinePay(_window, _m.Paylines[l], out _, out _, out var pay);
                units += pay;
            }
            scatterCount = CountScatters(_window);
            units += (long)_m.ScatterPays[scatterCount] * _m.Bet.Lines;
            return units;
        }

        /// <summary>Detailed evaluation for display (allocates).</summary>
        public SpinResult Evaluate(int[] stops, long lineBet, int multiplier)
        {
            var window = new int[_m.ReelCount][];
            for (var r = 0; r < window.Length; r++) window[r] = new int[_m.RowCount];
            FillWindow(stops, window);

            var totalBet = lineBet * _m.Bet.Lines;
            var result = new SpinResult
            {
                Stops = (int[])stops.Clone(),
                Window = window,
                Multiplier = multiplier,
                LineBet = lineBet,
                TotalBet = totalBet,
            };

            long lineTotal = 0;
            for (var l = 0; l < _m.Paylines.Length; l++)
            {
                LinePay(window, _m.Paylines[l], out var sym, out var count, out var pay);
                if (pay <= 0) continue;
                var win = pay * lineBet * multiplier;
                result.LineWins.Add(new LineWin(l, sym, count, win));
                lineTotal += win;
            }
            result.LineWinTotal = lineTotal;

            for (var r = 0; r < _m.ReelCount; r++)
                for (var row = 0; row < _m.RowCount; row++)
                    if (window[r][row] == _m.ScatterSymbol) result.ScatterPositions.Add(new CellPos(r, row));
            result.ScatterWin = (long)_m.ScatterPays[result.ScatterCount] * totalBet * multiplier;
            return result;
        }

        private int CountScatters(int[][] window)
        {
            var n = 0;
            for (var r = 0; r < _m.ReelCount; r++)
                for (var row = 0; row < _m.RowCount; row++)
                    if (window[r][row] == _m.ScatterSymbol) n++;
            return n;
        }

        /// <summary>
        /// Evaluates one line. pay is a multiple of line bet (no multiplier).
        /// If the line leads with WILDs, compares the pay of the WILD run alone with the pay after substitution and takes the higher.
        /// </summary>
        private void LinePay(int[][] window, int[] line, out int symbol, out int count, out long pay)
        {
            var wild = _m.WildSymbol;
            var scatter = _m.ScatterSymbol;
            var target = -1;
            var n = 0;
            var leadingWilds = 0;
            for (var r = 0; r < _m.ReelCount; r++)
            {
                var s = window[r][line[r]];
                if (s == scatter) break;
                if (s == wild)
                {
                    n++;
                    if (target < 0) leadingWilds++;
                    continue;
                }
                if (target < 0) { target = s; n++; }
                else if (s == target) n++;
                else break;
            }

            long symPay = target >= 0 ? _m.Paytable[target][n] : 0;
            long wildPay = leadingWilds > 0 ? _m.Paytable[wild][leadingWilds] : 0;
            if (wildPay > symPay)
            {
                symbol = wild; count = leadingWilds; pay = wildPay;
            }
            else
            {
                symbol = target >= 0 ? target : wild; count = n; pay = symPay;
            }
        }
    }
}
