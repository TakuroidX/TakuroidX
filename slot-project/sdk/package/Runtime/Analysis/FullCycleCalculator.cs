using System;
using System.Linq;
using System.Threading.Tasks;

namespace SlotSdk.Analysis
{
    public sealed class FullCycleResult
    {
        /// <summary>Total number of stop combinations (product of the reel lengths)</summary>
        public long Combinations;
        /// <summary>Total win in line-bet units (base game only: line + scatter)</summary>
        public long TotalUnits;
        public long HitCount;
        public long[] ScatterCountHistogram;
        public long MaxUnits;
        public long[] Buckets = new long[WinBuckets.Bounds.Length];
        public int Lines;
        public double ElapsedSeconds;

        public double BaseRtp => (double)TotalUnits / ((double)Combinations * Lines);
        public double HitFrequency => (double)HitCount / Combinations;
        public double MaxWinMultiple => (double)MaxUnits / Lines;
    }

    /// <summary>
    /// Enumerates every base game stop combination (full cycle) and computes the exact base RTP, hit frequency, and win distribution.
    /// Used to independently verify the TheoreticalCalculator formulas (= the Excel formulas) against the actual evaluator.
    /// Free spins aren't enumerated (they're infinitely recursive); they're covered by the formula and Monte Carlo.
    /// </summary>
    public static class FullCycleCalculator
    {
        public static FullCycleResult Run(MathModel m, Action<double> progress = null)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var len0 = m.Reels[0].Length;
            var parts = new FullCycleResult[len0];
            long done = 0;

            Parallel.For(0, len0, s0 =>
            {
                var ev = new SpinEvaluator(m);
                var stops = new int[m.ReelCount];
                stops[0] = s0;
                var part = new FullCycleResult { ScatterCountHistogram = new long[m.ReelCount * m.RowCount + 1] };
                Enumerate(m, ev, stops, 1, part);
                parts[s0] = part;
                var d = System.Threading.Interlocked.Increment(ref done);
                progress?.Invoke((double)d / len0);
            });

            var total = new FullCycleResult
            {
                ScatterCountHistogram = new long[m.ReelCount * m.RowCount + 1],
                Lines = m.Bet.Lines,
            };
            foreach (var p in parts)
            {
                total.Combinations += p.Combinations;
                total.TotalUnits += p.TotalUnits;
                total.HitCount += p.HitCount;
                total.MaxUnits = Math.Max(total.MaxUnits, p.MaxUnits);
                for (var i = 0; i < total.ScatterCountHistogram.Length; i++) total.ScatterCountHistogram[i] += p.ScatterCountHistogram[i];
                for (var i = 0; i < total.Buckets.Length; i++) total.Buckets[i] += p.Buckets[i];
            }
            total.ElapsedSeconds = sw.Elapsed.TotalSeconds;
            return total;
        }

        public static long CombinationCount(MathModel m) =>
            m.Reels.Aggregate(1L, (acc, r) => acc * r.Length);

        private static void Enumerate(MathModel m, SpinEvaluator ev, int[] stops, int reel, FullCycleResult part)
        {
            if (reel == m.ReelCount)
            {
                var units = ev.EvaluateUnits(stops, out var sc);
                part.Combinations++;
                part.TotalUnits += units;
                if (units > 0) part.HitCount++;
                if (units > part.MaxUnits) part.MaxUnits = units;
                part.ScatterCountHistogram[sc]++;
                part.Buckets[WinBuckets.IndexOf((double)units / m.Bet.Lines)]++;
                return;
            }
            var n = m.Reels[reel].Length;
            for (var s = 0; s < n; s++)
            {
                stops[reel] = s;
                Enumerate(m, ev, stops, reel + 1, part);
            }
        }
    }
}
