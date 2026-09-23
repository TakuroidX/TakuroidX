using System;

namespace SlotSdk
{
    /// <summary>
    /// A player's play session. Manages balance, bet, and free spin state.
    ///
    /// Base game: deducts total bet → evaluates → adds win. 3+ SCATTERs → awards free spins.
    /// Free spins: no bet deduction, all wins × multiplier, 3+ SCATTERs retrigger. Bet is fixed at the trigger value.
    /// </summary>
    public sealed class SlotGame
    {
        private readonly MathModel _m;
        private readonly IRng _rng;
        private readonly SpinEvaluator _evaluator;
        private readonly int[] _stops;

        public MathModel Model => _m;
        public long Balance { get; private set; }
        public int BetLevel { get; private set; }
        public long LineBet => _m.Bet.LineBetLevels[BetLevel];
        public long TotalBet => LineBet * _m.Bet.Lines;
        public bool IsInFreeSpins => FreeSpinsRemaining > 0;
        public int FreeSpinsRemaining { get; private set; }
        public int FreeSpinsPlayed { get; private set; }
        public long FeatureTotalWin { get; private set; }
        public bool CanSpin => IsInFreeSpins || Balance >= TotalBet;

        public SlotGame(MathModel model, IRng rng, long initialBalance)
        {
            _m = model ?? throw new ArgumentNullException(nameof(model));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            if (initialBalance < 0) throw new ArgumentOutOfRangeException(nameof(initialBalance));
            _evaluator = new SpinEvaluator(model);
            _stops = new int[model.ReelCount];
            Balance = initialBalance;
            BetLevel = model.Bet.DefaultLevel;
        }

        public void SetBetLevel(int level)
        {
            if (IsInFreeSpins) throw new InvalidOperationException("Cannot change bet during free spins");
            if (level < 0 || level >= _m.Bet.LineBetLevels.Count) throw new ArgumentOutOfRangeException(nameof(level));
            BetLevel = level;
        }

        public void AddCredits(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Balance += amount;
        }

        /// <summary>Spin with random stops.</summary>
        public SpinResult Spin()
        {
            for (var r = 0; r < _stops.Length; r++) _stops[r] = _rng.NextInt(_m.Reels[r].Length);
            return SpinWithStops(_stops);
        }

        /// <summary>Spin with fixed stops (for tests and debug; applies the same state transitions as Spin).</summary>
        public SpinResult SpinWithStops(int[] stops)
        {
            if (stops == null || stops.Length != _m.ReelCount) throw new ArgumentException("stops length mismatch", nameof(stops));
            for (var r = 0; r < stops.Length; r++)
                if (stops[r] < 0 || stops[r] >= _m.Reels[r].Length) throw new ArgumentOutOfRangeException(nameof(stops));
            if (!CanSpin) throw new InvalidOperationException("Insufficient balance");

            var isFree = IsInFreeSpins;
            if (!isFree) Balance -= TotalBet;

            var result = _evaluator.Evaluate(stops, LineBet, isFree ? _m.FreeSpins.Multiplier : 1);
            result.IsFreeSpin = isFree;
            Balance += result.TotalWin;

            var triggered = result.ScatterCount >= _m.ScatterTriggerCount;
            if (isFree)
            {
                FreeSpinsRemaining--;
                FreeSpinsPlayed++;
                FeatureTotalWin += result.TotalWin;
                if (triggered)
                {
                    FreeSpinsRemaining += _m.FreeSpins.RetriggerSpins;
                    result.FreeSpinsAwarded = _m.FreeSpins.RetriggerSpins;
                }
                result.FeatureTotalWin = FeatureTotalWin;
                result.FeatureEnded = FreeSpinsRemaining == 0;
            }
            else if (triggered)
            {
                FreeSpinsRemaining = _m.FreeSpins.AwardSpins;
                FreeSpinsPlayed = 0;
                FeatureTotalWin = 0;
                result.FreeSpinsAwarded = _m.FreeSpins.AwardSpins;
                result.FeatureTriggered = true;
            }

            result.FreeSpinsRemaining = FreeSpinsRemaining;
            result.BalanceAfter = Balance;
            return result;
        }

        /// <summary>Restores a saved state (balance, bet, FS in progress).</summary>
        public void Restore(long balance, int betLevel, int freeSpinsRemaining, int freeSpinsPlayed, long featureTotalWin)
        {
            if (balance < 0 || freeSpinsRemaining < 0) throw new ArgumentOutOfRangeException();
            if (betLevel < 0 || betLevel >= _m.Bet.LineBetLevels.Count) throw new ArgumentOutOfRangeException(nameof(betLevel));
            Balance = balance;
            BetLevel = betLevel;
            FreeSpinsRemaining = freeSpinsRemaining;
            FreeSpinsPlayed = freeSpinsPlayed;
            FeatureTotalWin = featureTotalWin;
        }
    }
}
