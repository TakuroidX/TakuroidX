using System;
using System.Threading.Tasks;

namespace SlotSdk.Analysis
{
    public sealed class MonteCarloResult
    {
        public long Rounds;
        public ulong Seed;
        /// <summary>Total bet (line-bet units: 1 round = Lines)</summary>
        public double TotalBet;
        public double TotalWin;
        public double BaseWin;
        public double FreeSpinWin;
        public long BaseHits;
        public long Triggers;
        public long FreeSpinsPlayed;
        public double MaxRoundWinMultiple;
        /// <summary>Round win (base + FS it triggered) as a multiple of total bet: sum and sum of squares (for standard deviation)</summary>
        public double SumMultiple;
        public double SumMultipleSq;
        public long[] Buckets = new long[WinBuckets.Bounds.Length];
        public double ElapsedSeconds;

        public double Rtp => TotalWin / TotalBet;
        public double BaseRtp => BaseWin / TotalBet;
        public double FreeSpinRtp => FreeSpinWin / TotalBet;
        public double HitFrequency => (double)BaseHits / Rounds;
        public double TriggerFrequency => (double)Triggers / Rounds;
        public double AvgFreeSpinsPerTrigger => Triggers == 0 ? 0 : (double)FreeSpinsPlayed / Triggers;
        public double AvgFeatureWinMultiple => Triggers == 0 ? 0 : FreeSpinWin / TotalBet * Rounds / Triggers;

        /// <summary>Standard deviation of the per-round win multiple (volatility index)</summary>
        public double StdDev
        {
            get
            {
                var mean = SumMultiple / Rounds;
                return Math.Sqrt(Math.Max(0, SumMultipleSq / Rounds - mean * mean));
            }
        }

        /// <summary>Half-width of the 95% confidence interval for RTP</summary>
        public double Rtp95HalfWidth => 1.96 * StdDev / Math.Sqrt(Rounds);

        internal void Merge(MonteCarloResult o)
        {
            Rounds += o.Rounds; TotalBet += o.TotalBet; TotalWin += o.TotalWin; BaseWin += o.BaseWin;
            FreeSpinWin += o.FreeSpinWin; BaseHits += o.BaseHits; Triggers += o.Triggers;
            FreeSpinsPlayed += o.FreeSpinsPlayed; SumMultiple += o.SumMultiple; SumMultipleSq += o.SumMultipleSq;
            MaxRoundWinMultiple = Math.Max(MaxRoundWinMultiple, o.MaxRoundWinMultiple);
            for (var i = 0; i < Buckets.Length; i++) Buckets[i] += o.Buckets[i];
        }
    }

    /// <summary>
    /// Monte Carlo simulation. 1 round = 1 paid spin + the whole free spin feature it triggers.
    /// Results are reproducible from the seed regardless of thread count (split into fixed chunks, each with its own RNG).
    /// </summary>
    public static class MonteCarloSimulator
    {
        private const int Chunks = 64;
        /// <summary>Free spin limit per round (prevents an infinite loop from bad math)</summary>
        public const int MaxFreeSpinsPerRound = 100_000;

        public static MonteCarloResult Run(MathModel m, long rounds, ulong seed, Action<double> progress = null)
        {
            if (rounds <= 0) throw new ArgumentOutOfRangeException(nameof(rounds));
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var parts = new MonteCarloResult[Chunks];
            long done = 0;
            Parallel.For(0, Chunks, c =>
            {
                var n = rounds / Chunks + (c < rounds % Chunks ? 1 : 0);
                parts[c] = RunChunk(m, n, seed ^ (0x9E3779B97F4A7C15UL * (ulong)(c + 1)));
                var d = System.Threading.Interlocked.Increment(ref done);
                progress?.Invoke((double)d / Chunks);
            });

            var total = new MonteCarloResult { Seed = seed };
            foreach (var p in parts) total.Merge(p);
            total.ElapsedSeconds = sw.Elapsed.TotalSeconds;
            return total;
        }

        private static MonteCarloResult RunChunk(MathModel m, long rounds, ulong seed)
        {
            var rng = new Xoshiro256StarStar(seed);
            var ev = new SpinEvaluator(m);
            var stops = new int[m.ReelCount];
            var lines = m.Bet.Lines;
            var mult = m.FreeSpins.Multiplier;
            var r = new MonteCarloResult();

            for (long i = 0; i < rounds; i++)
            {
                Draw(m, rng, stops);
                var baseUnits = ev.EvaluateUnits(stops, out var scatters);
                long featureUnits = 0;
                if (baseUnits > 0) r.BaseHits++;

                if (scatters >= m.ScatterTriggerCount)
                {
                    r.Triggers++;
                    var remaining = m.FreeSpins.AwardSpins;
                    var played = 0;
                    while (remaining > 0)
                    {
                        remaining--;
                        r.FreeSpinsPlayed++;
                        if (++played > MaxFreeSpinsPerRound)
                            throw new InvalidOperationException("Free spins exceeded MaxFreeSpinsPerRound (retrigger too frequent?)");
                        Draw(m, rng, stops);
                        featureUnits += ev.EvaluateUnits(stops, out var fsScatters) * mult;
                        if (fsScatters >= m.ScatterTriggerCount) remaining += m.FreeSpins.RetriggerSpins;
                    }
                }

                var roundWin = baseUnits + featureUnits;
                var multiple = (double)roundWin / lines;
                r.Rounds++;
                r.TotalBet += lines;
                r.BaseWin += baseUnits;
                r.FreeSpinWin += featureUnits;
                r.TotalWin += roundWin;
                r.SumMultiple += multiple;
                r.SumMultipleSq += multiple * multiple;
                if (multiple > r.MaxRoundWinMultiple) r.MaxRoundWinMultiple = multiple;
                r.Buckets[WinBuckets.IndexOf(multiple)]++;
            }
            return r;
        }

        private static void Draw(MathModel m, IRng rng, int[] stops)
        {
            for (var k = 0; k < stops.Length; k++) stops[k] = rng.NextInt(m.Reels[k].Length);
        }
    }
}
