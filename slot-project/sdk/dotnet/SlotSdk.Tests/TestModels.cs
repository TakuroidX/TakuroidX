using System;
using System.IO;
using System.Linq;
using SlotSdk;

namespace SlotSdk.Tests
{
    internal static class TestModels
    {
        public static string NeonJson => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "math", "neon_fortune.json"));

        public static MathModel Neon() => MathModel.FromJson(NeonJson);

        /// <summary>Takes the Neon Fortune definition and swaps only the reel strips (the "reels" key is last in the file).</summary>
        public static string WithReels(string[][] reels)
        {
            var json = NeonJson;
            var idx = json.LastIndexOf("\"reels\"", StringComparison.Ordinal);
            var arr = "[" + string.Join(",", reels.Select(r => "[" + string.Join(",", r.Select(s => "\"" + s + "\"")) + "]")) + "]";
            return json.Substring(0, idx) + "\"reels\": " + arr + "\n}";
        }

        /// <summary>A model where stops=0 shows the given window exactly. columns[reel] = {top, mid, bottom}</summary>
        public static MathModel FixedWindow(params string[][] columns) => MathModel.FromJson(WithReels(columns));

        public static int[] Zeros => new int[5];

        /// <summary>Returns the stop for each reel where the top row is SCATTER (for reels without one, returns a stop with no SCATTER in view)</summary>
        public static int[] ScatterStops(MathModel m, params int[] reelsWithScatter)
        {
            var stops = new int[m.ReelCount];
            for (var r = 0; r < m.ReelCount; r++)
            {
                var strip = m.Reels[r];
                bool Visible(int s) => Enumerable.Range(0, m.RowCount).Any(k => strip[(s + k) % strip.Length] == m.ScatterSymbol);
                stops[r] = reelsWithScatter.Contains(r)
                    ? Array.IndexOf(strip, m.ScatterSymbol)
                    : Enumerable.Range(0, strip.Length).First(s => !Visible(s));
            }
            return stops;
        }
    }
}
