using System.Globalization;
using SlotSdk;
using UnityEngine;

namespace Game
{
    /// <summary>Save data on PlayerPrefs (game spec "Save Data" sheet).</summary>
    public static class SaveStore
    {
        private const string Balance = "nf.balance";
        private const string BetLevel = "nf.betLevel";
        private const string FsRemaining = "nf.fsRemaining";
        private const string FsPlayed = "nf.fsPlayed";
        private const string FsTotalWin = "nf.fsTotalWin";
        private const string Turbo = "nf.turbo";
        private const string MathVersion = "nf.mathVersion";

        /// <summary>Tests set this to false so the real save data isn't touched</summary>
        public static bool Enabled = true;

        public static void Load(SlotGame game, out bool turbo)
        {
            turbo = false;
            if (!Enabled) return;
            turbo = PlayerPrefs.GetInt(Turbo, 0) == 1;
            if (!PlayerPrefs.HasKey(Balance)) return;

            var balance = ParseLong(PlayerPrefs.GetString(Balance), GameSpec.StartingCredits);
            var level = Mathf.Clamp(PlayerPrefs.GetInt(BetLevel, game.Model.Bet.DefaultLevel), 0, game.Model.Bet.LineBetLevels.Count - 1);

            // If the math version changed, drop the in-progress FS (it would resume under different math)
            var sameMath = PlayerPrefs.GetString(MathVersion, "") == game.Model.Version;
            var remaining = sameMath ? Mathf.Max(0, PlayerPrefs.GetInt(FsRemaining, 0)) : 0;
            var played = sameMath ? Mathf.Max(0, PlayerPrefs.GetInt(FsPlayed, 0)) : 0;
            var total = sameMath ? ParseLong(PlayerPrefs.GetString(FsTotalWin, "0"), 0) : 0;

            game.Restore(balance, level, remaining, played, total);
        }

        public static void Save(SlotGame game, bool turbo)
        {
            if (!Enabled) return;
            PlayerPrefs.SetString(Balance, game.Balance.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.SetInt(BetLevel, game.BetLevel);
            PlayerPrefs.SetInt(FsRemaining, game.FreeSpinsRemaining);
            PlayerPrefs.SetInt(FsPlayed, game.FreeSpinsPlayed);
            PlayerPrefs.SetString(FsTotalWin, game.FeatureTotalWin.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.SetInt(Turbo, turbo ? 1 : 0);
            PlayerPrefs.SetString(MathVersion, game.Model.Version);
            PlayerPrefs.Save();
        }

        private static long ParseLong(string s, long fallback) =>
            long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) && v >= 0 ? v : fallback;
    }
}
