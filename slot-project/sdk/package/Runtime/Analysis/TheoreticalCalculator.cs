using System;
using System.Collections.Generic;
using System.Linq;

namespace SlotSdk.Analysis
{
    public sealed class LinePayRow
    {
        public string Symbol;
        public int Count;
        public double Probability;
        public int Pay;
        public double Contribution;
    }

    public sealed class TheoreticalResult
    {
        public List<LinePayRow> LineRows = new List<LinePayRow>();
        /// <summary>Probability of each reel showing a SCATTER in the window</summary>
        public double[] ScatterReelProbability;
        /// <summary>ScatterDistribution[k] = probability that exactly k SCATTERs are visible</summary>
        public double[] ScatterDistribution;
        public double LineRtp;
        public double ScatterRtp;
        public double BaseRtp => LineRtp + ScatterRtp;
        public double TriggerProbability;
        public double ExpectedFreeSpins;
        public double FreeSpinRtp;
        public double TotalRtp => BaseRtp + FreeSpinRtp;
    }

    /// <summary>
    /// Exact theoretical calculation (same formulas as the "line calc", "scatter calc", and "free spin calc" sheets in the Excel MATH spec).
    ///
    /// Premises (enforced by MathModel.Validate):
    ///   - No WILD on reel 1 → each line's paying symbol is reel 1's symbol, so there's no double counting
    ///   - WILD has no pay of its own (checked at compute time)
    ///   - SCATTERs are at least RowCount apart → at most 1 visible per reel, so P = rows × count / length
    ///   - Reels are independent and each stop is equally likely → every payline has the same symbol distribution
    ///   - Free spins use the same reels and pay × multiplier. Retriggers are unlimited (a branching process)
    /// </summary>
    public static class TheoreticalCalculator
    {
        public static TheoreticalResult Compute(MathModel m)
        {
            if (m.Paytable[m.WildSymbol].Any(p => p != 0))
                throw new NotSupportedException("Theoretical calculator assumes WILD has no own pays");

            var res = new TheoreticalResult();
            var reels = m.ReelCount;
            var len = m.Reels.Select(r => (double)r.Length).ToArray();
            var counts = m.Reels.Select(r =>
            {
                var c = new int[m.Symbols.Count];
                foreach (var s in r) c[s]++;
                return c;
            }).ToArray();

            // ---- Line pays (probability for one line; RTP is the same across the 20 lines) ----
            foreach (var sym in m.Symbols)
            {
                if (sym.Type != SymbolType.Normal) continue;
                var ge = new double[reels + 2];
                ge[0] = 1.0;
                for (var i = 0; i < reels; i++)
                {
                    var n = counts[i][sym.Index] + (i > 0 ? counts[i][m.WildSymbol] : 0);
                    ge[i + 1] = ge[i] * n / len[i];
                }
                for (var k = 1; k <= reels; k++)
                {
                    var pay = m.Paytable[sym.Index][k];
                    if (pay == 0) continue;
                    var p = ge[k] - (k < reels ? ge[k + 1] : 0.0);
                    res.LineRows.Add(new LinePayRow { Symbol = sym.Id, Count = k, Probability = p, Pay = pay, Contribution = p * pay });
                }
            }
            res.LineRtp = res.LineRows.Sum(r => r.Contribution);

            // ---- Scatter (Poisson binomial) ----
            var ps = new double[reels];
            for (var i = 0; i < reels; i++) ps[i] = m.RowCount * counts[i][m.ScatterSymbol] / len[i];
            var dist = new double[reels + 1];
            for (var mask = 0; mask < 1 << reels; mask++)
            {
                var pr = 1.0;
                var k = 0;
                for (var i = 0; i < reels; i++)
                {
                    if ((mask & (1 << i)) != 0) { pr *= ps[i]; k++; }
                    else pr *= 1 - ps[i];
                }
                dist[k] += pr;
            }
            res.ScatterReelProbability = ps;
            res.ScatterDistribution = dist;
            for (var k = 1; k <= reels; k++) res.ScatterRtp += dist[k] * m.ScatterPays[k];

            // ---- Free spins ----
            for (var k = m.ScatterTriggerCount; k <= reels; k++) res.TriggerProbability += dist[k];
            var branching = m.FreeSpins.RetriggerSpins * res.TriggerProbability;
            if (branching >= 1) throw new InvalidOperationException("Free spins are expected to never end (retrigger too frequent)");
            res.ExpectedFreeSpins = m.FreeSpins.AwardSpins / (1 - branching);
            res.FreeSpinRtp = res.TriggerProbability * res.ExpectedFreeSpins * m.FreeSpins.Multiplier * res.BaseRtp;
            return res;
        }
    }
}
