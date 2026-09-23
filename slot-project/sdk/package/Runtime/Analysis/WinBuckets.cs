namespace SlotSdk.Analysis
{
    /// <summary>Win distribution bucket boundaries (as multiples of total bet). Shared by the simulator and the Excel file.</summary>
    public static class WinBuckets
    {
        /// <summary>Lower bound of each bucket. Bucket i = [Bounds[i], Bounds[i+1]). Bucket 0 is exactly "no win".</summary>
        public static readonly double[] Bounds = { 0, 1e-9, 1, 2, 5, 10, 20, 50, 100, 500 };

        public static readonly string[] Labels =
        {
            "0 (no win)", "0-1x", "1-2x", "2-5x", "5-10x", "10-20x", "20-50x", "50-100x", "100-500x", "500x+",
        };

        public static int IndexOf(double multiple)
        {
            for (var i = Bounds.Length - 1; i >= 0; i--)
                if (multiple >= Bounds[i]) return i;
            return 0;
        }
    }
}
