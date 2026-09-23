using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using SlotSdk;
using SlotSdk.Analysis;

namespace SlotSdk.Simulator
{
    /// <summary>
    /// MATH simulator CLI.
    ///   slotsim theory    <math.json>
    ///   slotsim mc        <math.json> [--rounds N] [--seed S]
    ///   slotsim fullcycle <math.json>
    ///   slotsim report    <math.json> [--rounds N] [--seed S] [--skip-fullcycle] [--out results.json]
    /// report runs all three, cross-checks them, and exits with code 1 on a mismatch.
    /// </summary>
    public static class Program
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("usage: slotsim <theory|mc|fullcycle|report> <math.json> [--rounds N] [--seed S] [--out FILE] [--skip-fullcycle]");
                return 2;
            }
            var cmd = args[0];
            var path = args[1];
            var rounds = long.Parse(Opt(args, "--rounds") ?? "10000000", Inv);
            var seed = ulong.Parse(Opt(args, "--seed") ?? "12345", Inv);
            var outPath = Opt(args, "--out");
            var skipFull = args.Contains("--skip-fullcycle");

            var model = MathModel.FromJson(File.ReadAllText(path));
            Console.WriteLine($"# {model.Name} v{model.Version}  ({model.ReelCount}x{model.RowCount}, {model.Bet.Lines} lines, reels={string.Join("/", model.Reels.Select(r => r.Length))})");

            switch (cmd)
            {
                case "theory":
                    PrintTheory(TheoreticalCalculator.Compute(model));
                    return 0;
                case "mc":
                    PrintMc(RunMc(model, rounds, seed));
                    return 0;
                case "fullcycle":
                    PrintFull(RunFull(model));
                    return 0;
                case "report":
                    return Report(model, rounds, seed, skipFull, outPath);
                default:
                    Console.Error.WriteLine($"unknown command '{cmd}'");
                    return 2;
            }
        }

        private static int Report(MathModel model, long rounds, ulong seed, bool skipFull, string outPath)
        {
            var th = TheoreticalCalculator.Compute(model);
            PrintTheory(th);
            var full = skipFull ? null : RunFull(model);
            if (full != null) PrintFull(full);
            var mc = RunMc(model, rounds, seed);
            PrintMc(mc);

            Console.WriteLine();
            Console.WriteLine("== Verification ==");
            var ok = true;
            if (full != null)
            {
                var diff = Math.Abs(full.BaseRtp - th.BaseRtp);
                var pass = diff < 1e-9;
                ok &= pass;
                Console.WriteLine($"[{(pass ? "PASS" : "FAIL")}] base RTP theory={Pct(th.BaseRtp, 6)} fullcycle={Pct(full.BaseRtp, 6)} diff={diff:E2}");
                var trigFull = full.ScatterCountHistogram.Skip(model.ScatterTriggerCount).Sum() / (double)full.Combinations;
                var diffT = Math.Abs(trigFull - th.TriggerProbability);
                pass = diffT < 1e-12;
                ok &= pass;
                Console.WriteLine($"[{(pass ? "PASS" : "FAIL")}] FS trigger theory={th.TriggerProbability:F8} fullcycle={trigFull:F8}");
            }
            var z = (mc.Rtp - th.TotalRtp) / (mc.StdDev / Math.Sqrt(mc.Rounds));
            var mcPass = Math.Abs(z) < 4;
            ok &= mcPass;
            Console.WriteLine($"[{(mcPass ? "PASS" : "FAIL")}] total RTP theory={Pct(th.TotalRtp, 4)} mc={Pct(mc.Rtp, 4)} ±{Pct(mc.Rtp95HalfWidth, 4)} (z={z:F2}, |z|<4)");

            if (outPath != null)
            {
                File.WriteAllText(outPath, ToJson(model, th, full, mc, ok));
                Console.WriteLine($"wrote {outPath}");
            }
            return ok ? 0 : 1;
        }

        private static MonteCarloResult RunMc(MathModel m, long rounds, ulong seed)
        {
            Console.Error.Write($"monte carlo {rounds:N0} rounds (seed={seed}) ");
            var r = MonteCarloSimulator.Run(m, rounds, seed, ProgressBar());
            Console.Error.WriteLine();
            return r;
        }

        private static FullCycleResult RunFull(MathModel m)
        {
            Console.Error.Write($"full cycle {FullCycleCalculator.CombinationCount(m):N0} combinations ");
            var r = FullCycleCalculator.Run(m, ProgressBar());
            Console.Error.WriteLine();
            return r;
        }

        private static Action<double> ProgressBar()
        {
            var last = 0;
            var gate = new object();
            return p =>
            {
                lock (gate)
                {
                    var n = (int)(p * 20);
                    while (last < n) { Console.Error.Write('.'); last++; }
                }
            };
        }

        private static void PrintTheory(TheoreticalResult t)
        {
            Console.WriteLine();
            Console.WriteLine("== Theory (exact) ==");
            Console.WriteLine($"line RTP            {Pct(t.LineRtp, 4)}");
            Console.WriteLine($"scatter RTP         {Pct(t.ScatterRtp, 4)}");
            Console.WriteLine($"base RTP            {Pct(t.BaseRtp, 4)}");
            Console.WriteLine($"FS trigger          {t.TriggerProbability:F6} (1 in {1 / t.TriggerProbability:F1})");
            Console.WriteLine($"expected FS spins   {t.ExpectedFreeSpins:F4}");
            Console.WriteLine($"free spin RTP       {Pct(t.FreeSpinRtp, 4)}");
            Console.WriteLine($"TOTAL RTP           {Pct(t.TotalRtp, 4)}");
        }

        private static void PrintFull(FullCycleResult f)
        {
            Console.WriteLine();
            Console.WriteLine($"== Full cycle base game ({f.Combinations:N0} combinations, {f.ElapsedSeconds:F1}s) ==");
            Console.WriteLine($"base RTP            {Pct(f.BaseRtp, 6)}");
            Console.WriteLine($"hit frequency       {Pct(f.HitFrequency, 3)} (1 in {1 / f.HitFrequency:F2})");
            Console.WriteLine($"max base win        {f.MaxWinMultiple:F2}x total bet");
        }

        private static void PrintMc(MonteCarloResult r)
        {
            Console.WriteLine();
            Console.WriteLine($"== Monte Carlo ({r.Rounds:N0} rounds, seed={r.Seed}, {r.ElapsedSeconds:F1}s) ==");
            Console.WriteLine($"total RTP           {Pct(r.Rtp, 4)} ± {Pct(r.Rtp95HalfWidth, 4)} (95% CI)");
            Console.WriteLine($"  base / FS         {Pct(r.BaseRtp, 4)} / {Pct(r.FreeSpinRtp, 4)}");
            Console.WriteLine($"hit frequency       {Pct(r.HitFrequency, 3)}");
            Console.WriteLine($"FS trigger          1 in {1 / r.TriggerFrequency:F1}");
            Console.WriteLine($"avg FS per trigger  {r.AvgFreeSpinsPerTrigger:F3}");
            Console.WriteLine($"avg feature win     {r.AvgFeatureWinMultiple:F2}x");
            Console.WriteLine($"std dev (volatility) {r.StdDev:F3}");
            Console.WriteLine($"max round win       {r.MaxRoundWinMultiple:F2}x");
            Console.WriteLine("win distribution (round win / total bet):");
            for (var i = 0; i < r.Buckets.Length; i++)
                Console.WriteLine($"  {WinBuckets.Labels[i],-12} {(double)r.Buckets[i] / r.Rounds,10:P4}  (1 in {(r.Buckets[i] == 0 ? "-" : ((double)r.Rounds / r.Buckets[i]).ToString("F1", Inv))})");
        }

        private static string Pct(double v, int digits) => (v * 100).ToString("F" + digits, Inv) + "%";

        private static string Opt(string[] args, string name)
        {
            var i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        // ---------- JSON output (read by the Excel generator) ----------
        private static string ToJson(MathModel m, TheoreticalResult th, FullCycleResult full, MonteCarloResult mc, bool ok)
        {
            var o = new Dictionary<string, object>
            {
                ["model"] = new Dictionary<string, object> { ["id"] = m.Id, ["version"] = m.Version },
                ["generatedAt"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", Inv),
                ["verificationPassed"] = ok,
                ["theory"] = new Dictionary<string, object>
                {
                    ["lineRtp"] = th.LineRtp, ["scatterRtp"] = th.ScatterRtp, ["baseRtp"] = th.BaseRtp,
                    ["triggerProbability"] = th.TriggerProbability, ["expectedFreeSpins"] = th.ExpectedFreeSpins,
                    ["freeSpinRtp"] = th.FreeSpinRtp, ["totalRtp"] = th.TotalRtp,
                },
                ["monteCarlo"] = new Dictionary<string, object>
                {
                    ["rounds"] = mc.Rounds, ["seed"] = mc.Seed.ToString(Inv), ["rtp"] = mc.Rtp, ["rtp95HalfWidth"] = mc.Rtp95HalfWidth,
                    ["baseRtp"] = mc.BaseRtp, ["freeSpinRtp"] = mc.FreeSpinRtp, ["hitFrequency"] = mc.HitFrequency,
                    ["triggerFrequency"] = mc.TriggerFrequency, ["avgFreeSpinsPerTrigger"] = mc.AvgFreeSpinsPerTrigger,
                    ["avgFeatureWinMultiple"] = mc.AvgFeatureWinMultiple, ["stdDev"] = mc.StdDev,
                    ["maxRoundWinMultiple"] = mc.MaxRoundWinMultiple, ["elapsedSeconds"] = mc.ElapsedSeconds,
                    ["buckets"] = Buckets(mc.Buckets, mc.Rounds),
                },
            };
            if (full != null)
            {
                o["fullCycle"] = new Dictionary<string, object>
                {
                    ["combinations"] = full.Combinations, ["baseRtp"] = full.BaseRtp, ["hitFrequency"] = full.HitFrequency,
                    ["maxWinMultiple"] = full.MaxWinMultiple, ["elapsedSeconds"] = full.ElapsedSeconds,
                    ["scatterHistogram"] = full.ScatterCountHistogram.Select(x => (object)x).ToList(),
                    ["buckets"] = Buckets(full.Buckets, full.Combinations),
                };
            }
            var sb = new StringBuilder();
            WriteJson(sb, o, 0);
            return sb.Append('\n').ToString();
        }

        private static List<object> Buckets(long[] counts, long total) =>
            counts.Select((c, i) => (object)new Dictionary<string, object>
            {
                ["label"] = WinBuckets.Labels[i], ["min"] = WinBuckets.Bounds[i], ["count"] = c, ["probability"] = (double)c / total,
            }).ToList();

        private static void WriteJson(StringBuilder sb, object v, int indent)
        {
            var pad = new string(' ', indent * 2);
            switch (v)
            {
                case null: sb.Append("null"); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case string s: sb.Append('"').Append(s.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"'); break;
                case double d: sb.Append(d.ToString("R", Inv)); break;
                case long l: sb.Append(l.ToString(Inv)); break;
                case int i: sb.Append(i.ToString(Inv)); break;
                case Dictionary<string, object> dict:
                    sb.Append("{\n");
                    var n = 0;
                    foreach (var kv in dict)
                    {
                        sb.Append(pad).Append("  \"").Append(kv.Key).Append("\": ");
                        WriteJson(sb, kv.Value, indent + 1);
                        sb.Append(++n < dict.Count ? ",\n" : "\n");
                    }
                    sb.Append(pad).Append('}');
                    break;
                case List<object> list:
                    sb.Append("[\n");
                    for (var k = 0; k < list.Count; k++)
                    {
                        sb.Append(pad).Append("  ");
                        WriteJson(sb, list[k], indent + 1);
                        sb.Append(k + 1 < list.Count ? ",\n" : "\n");
                    }
                    sb.Append(pad).Append(']');
                    break;
                default: throw new NotSupportedException(v.GetType().Name);
            }
        }
    }
}
