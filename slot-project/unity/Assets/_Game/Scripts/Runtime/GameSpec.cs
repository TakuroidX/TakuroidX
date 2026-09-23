using UnityEngine;

namespace Game
{
    public enum WinTier
    {
        None,
        Normal,
        Big,
        Mega,
        Epic,
    }

    /// <summary>Reel presentation timing (game spec "Reel Timing" sheet)</summary>
    public sealed class ReelTiming
    {
        public float SpeedSymbolsPerSec;
        public float SpinUpSeconds;
        public float MinSpinSeconds;
        public float StopIntervalSeconds;
        public float BounceSeconds;
        public float AnticipationSeconds;
        public float FreeSpinIntervalSeconds;
        public float AutoIntervalSeconds;
    }

    /// <summary>
    /// Constants defined in the game spec (docs/game_spec.xlsx).
    /// When you change one, keep it in sync with tools/build_game_spec.py.
    /// </summary>
    public static class GameSpec
    {
        public const string MathResource = "Math/neon_fortune";
        public const long StartingCredits = 10_000;

        public static readonly ReelTiming Normal = new ReelTiming
        {
            SpeedSymbolsPerSec = 22f, SpinUpSeconds = 0.12f, MinSpinSeconds = 0.60f, StopIntervalSeconds = 0.16f,
            BounceSeconds = 0.10f, AnticipationSeconds = 0.90f, FreeSpinIntervalSeconds = 0.40f, AutoIntervalSeconds = 0.30f,
        };

        public static readonly ReelTiming Turbo = new ReelTiming
        {
            SpeedSymbolsPerSec = 34f, SpinUpSeconds = 0.06f, MinSpinSeconds = 0.20f, StopIntervalSeconds = 0.05f,
            BounceSeconds = 0.06f, AnticipationSeconds = 0.60f, FreeSpinIntervalSeconds = 0.20f, AutoIntervalSeconds = 0.10f,
        };

        /// <summary>Stop bounce overshoot (fraction of a cell)</summary>
        public const float BounceOvershoot = 0.12f;

        // ---- Win presentation ("Win Presentation" sheet) ----
        public const double BigWinMultiple = 10;
        public const double MegaWinMultiple = 25;
        public const double EpicWinMultiple = 50;
        public const float NormalCountUpSeconds = 0.6f;
        public const float BigCountUpSeconds = 3.0f;
        public const float MegaCountUpSeconds = 4.5f;
        public const float EpicCountUpSeconds = 6.0f;
        public const float AllLinesSeconds = 1.0f;
        public const float EachLineSeconds = 0.8f;
        public const float DimAlpha = 0.45f;

        // ---- Free spins ("Free Spins" / "Game Flow" sheets) ----
        public const float FreeSpinIntroSeconds = 2.0f;
        public const float RetriggerBannerSeconds = 1.5f;
        public const float FreeSpinTotalSeconds = 3.0f;

        // ---- Layout ("Screen Layout" sheet, reference 1920x1080) ----
        public static readonly Vector2 ReferenceResolution = new Vector2(1920, 1080);
        public const float CellSize = 200f;
        public const float ReelSpacing = 16f;
        public static readonly Vector2 ReelAreaCenter = new Vector2(0, 40);

        public static WinTier TierOf(long win, long totalBet)
        {
            if (win <= 0 || totalBet <= 0) return WinTier.None;
            var multiple = (double)win / totalBet;
            if (multiple >= EpicWinMultiple) return WinTier.Epic;
            if (multiple >= MegaWinMultiple) return WinTier.Mega;
            if (multiple >= BigWinMultiple) return WinTier.Big;
            return WinTier.Normal;
        }

        public static float CountUpSeconds(WinTier tier)
        {
            switch (tier)
            {
                case WinTier.Big: return BigCountUpSeconds;
                case WinTier.Mega: return MegaCountUpSeconds;
                case WinTier.Epic: return EpicCountUpSeconds;
                case WinTier.Normal: return NormalCountUpSeconds;
                default: return 0f;
            }
        }

        // ---- Colors ("Symbol Visuals" sheet) ----
        public static readonly Color Background = Hex("0B1020");
        public static readonly Color FreeSpinBackground = Hex("1A0B2E");
        public static readonly Color Panel = Hex("1B2340");
        public static readonly Color PanelDark = Hex("11172B");
        public static readonly Color Button = Hex("303A5C");
        public static readonly Color ButtonActive = Hex("5B2A86");
        public static readonly Color Spin = Hex("FF3B5C");
        public static readonly Color TitlePink = Hex("FF4FD8");
        public static readonly Color Gold = Hex("FFC400");
        public static readonly Color Cyan = Hex("29D3FF");
        public static readonly Color WinGreen = Hex("4ADE80");
        public static readonly Color BetYellow = Hex("FFD54A");

        public static Color SymbolColor(string id)
        {
            switch (id)
            {
                case "WILD": return Hex("B04DFF");
                case "SCATTER": return Hex("FFC400");
                case "SEVEN": return Hex("FF3B5C");
                case "DIAMOND": return Hex("29D3FF");
                case "BELL": return Hex("FFD54A");
                case "CHERRY": return Hex("FF6F91");
                case "A": return Hex("4ADE80");
                case "K": return Hex("60A5FA");
                case "Q": return Hex("F472B6");
                case "J": return Hex("FB923C");
                default: return Color.white;
            }
        }

        /// <summary>Label shown on the symbol. null means shape only (star / diamond)</summary>
        public static string SymbolLabel(string id)
        {
            switch (id)
            {
                case "SCATTER":
                case "DIAMOND": return null;
                case "SEVEN": return "7";
                default: return id;
            }
        }

        public static Color LineColor(int lineIndex, int lineCount) =>
            Color.HSVToRGB((float)lineIndex / lineCount, 0.7f, 1f);

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}
