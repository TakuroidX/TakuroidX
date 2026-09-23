using System;
using System.IO;
using System.Linq;
using System.Text;
using SlotSdk;

namespace ConsoleSlot
{
    /// <summary>
    /// Sample game playable in the terminal. Covers the minimal way to use the SDK (SlotGame).
    ///   dotnet run --project samples/ConsoleSlot              … interactive
    ///   dotnet run --project samples/ConsoleSlot -- --demo 30 --seed 1   … automated demo (no input)
    /// </summary>
    public static class Program
    {
        private const long StartingCredits = 10_000;

        public static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            var mathPath = Opt(args, "--math") ?? Path.Combine(AppContext.BaseDirectory, "neon_fortune.json");
            var model = MathModel.FromJson(File.ReadAllText(mathPath));
            var seed = Opt(args, "--seed");
            IRng rng = seed != null ? new Xoshiro256StarStar(ulong.Parse(seed)) : (IRng)new CryptoRng();
            var game = new SlotGame(model, rng, StartingCredits);
            var ui = new Renderer(model, color: !args.Contains("--no-color"));

            var demo = Opt(args, "--demo");
            if (demo != null) return RunDemo(game, ui, int.Parse(demo));

            ui.Banner();
            while (true)
            {
                ui.Status(game);
                Console.Write("[Enter]=SPIN  +/-=BET  a=AUTO x10  p=PAYTABLE  q=QUIT > ");
                var input = (Console.ReadLine() ?? "q").Trim().ToLowerInvariant();
                switch (input)
                {
                    case "q": return 0;
                    case "p": ui.Paytable(game); continue;
                    case "+": TryBet(game, game.BetLevel + 1); continue;
                    case "-": TryBet(game, game.BetLevel - 1); continue;
                    case "a":
                        for (var i = 0; i < 10 && PlayRound(game, ui); i++) { }
                        continue;
                    case "":
                        PlayRound(game, ui);
                        continue;
                    default:
                        Console.WriteLine("?");
                        continue;
                }
            }
        }

        private static int RunDemo(SlotGame game, Renderer ui, int rounds)
        {
            var start = game.Balance;
            for (var i = 0; i < rounds; i++)
                if (!PlayRound(game, ui)) break;
            Console.WriteLine($"\nDEMO END: {rounds} rounds, balance {start:N0} -> {game.Balance:N0}");
            return 0;
        }

        /// <summary>One paid spin + the whole free spin feature it triggers. false if the balance is too low.</summary>
        private static bool PlayRound(SlotGame game, Renderer ui)
        {
            if (!game.CanSpin)
            {
                Console.WriteLine("NOT ENOUGH CREDIT → REFILL");
                game.AddCredits(StartingCredits);
                return false;
            }
            var r = game.Spin();
            ui.Spin(r);
            while (game.IsInFreeSpins)
            {
                var f = game.Spin();
                ui.Spin(f);
                if (f.FeatureEnded) ui.Say($"*** FREE SPINS TOTAL WIN {f.FeatureTotalWin:N0} ***", ConsoleColor.Yellow);
            }
            return true;
        }

        private static void TryBet(SlotGame game, int level)
        {
            if (level < 0 || level >= game.Model.Bet.LineBetLevels.Count) return;
            game.SetBetLevel(level);
        }

        private static string Opt(string[] args, string name)
        {
            var i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
    }

    internal sealed class Renderer
    {
        private readonly MathModel _m;
        private readonly bool _color;

        private static readonly (string Id, string Label, ConsoleColor Color)[] Look =
        {
            ("WILD", " WILD ", ConsoleColor.Magenta), ("SCATTER", " STAR ", ConsoleColor.Yellow),
            ("SEVEN", "  7   ", ConsoleColor.Red), ("DIAMOND", "DIAMND", ConsoleColor.Cyan),
            ("BELL", " BELL ", ConsoleColor.DarkYellow), ("CHERRY", "CHERRY", ConsoleColor.DarkRed),
            ("A", "  A   ", ConsoleColor.Green), ("K", "  K   ", ConsoleColor.Blue),
            ("Q", "  Q   ", ConsoleColor.DarkMagenta), ("J", "  J   ", ConsoleColor.DarkCyan),
        };

        public Renderer(MathModel m, bool color) { _m = m; _color = color; }

        public void Banner() => Say($"=== {_m.Name} (sample) — play credits only ===", ConsoleColor.Cyan);

        public void Status(SlotGame g) =>
            Console.WriteLine($"CREDIT {g.Balance:N0}   BET {g.TotalBet:N0} (line {g.LineBet} x {_m.Bet.Lines})");

        public void Spin(SpinResult r)
        {
            var head = r.IsFreeSpin ? $"FREE SPIN x{r.Multiplier}  (remaining {r.FreeSpinsRemaining})" : "SPIN";
            Console.WriteLine();
            Say(head, r.IsFreeSpin ? ConsoleColor.Magenta : ConsoleColor.Gray);
            var winCells = r.LineWins.SelectMany(w => Enumerable.Range(0, w.Count).Select(k => (k, _m.Paylines[w.LineIndex][k])))
                .Concat(r.ScatterPositions.Select(p => (p.Reel, p.Row))).ToHashSet();
            for (var row = 0; row < _m.RowCount; row++)
            {
                Console.Write("  ");
                for (var reel = 0; reel < _m.ReelCount; reel++)
                {
                    var look = Look.First(l => l.Id == _m.Symbols[r.Window[reel][row]].Id);
                    var hit = winCells.Contains((reel, row));
                    Write(hit ? $"[{look.Label}]" : $" {look.Label} ", look.Color, hit);
                }
                Console.WriteLine();
            }
            foreach (var w in r.LineWins)
                Console.WriteLine($"  LINE {w.LineIndex + 1,2}  {_m.Symbols[w.Symbol].Id} x{w.Count}  WIN {w.Win:N0}");
            if (r.ScatterWin > 0) Console.WriteLine($"  SCATTER x{r.ScatterCount}  WIN {r.ScatterWin:N0}");
            if (r.TotalWin > 0)
            {
                var mult = (double)r.TotalWin / r.TotalBet;
                var tier = mult >= 50 ? "EPIC WIN! " : mult >= 25 ? "MEGA WIN! " : mult >= 10 ? "BIG WIN! " : "";
                Say($"  {tier}WIN {r.TotalWin:N0}  ({mult:F1}x)", ConsoleColor.Green);
            }
            if (r.FeatureTriggered) Say($"*** FREE SPINS! {r.FreeSpinsAwarded} SPINS, ALL WINS x{_m.FreeSpins.Multiplier} ***", ConsoleColor.Yellow);
            else if (r.IsFreeSpin && r.FreeSpinsAwarded > 0) Say($"*** +{r.FreeSpinsAwarded} SPINS! ***", ConsoleColor.Yellow);
        }

        public void Paytable(SlotGame g)
        {
            Console.WriteLine($"PAYTABLE (credits at line bet {g.LineBet})");
            foreach (var s in _m.Symbols.Where(s => s.Type == SymbolType.Normal))
            {
                var pays = Enumerable.Range(1, _m.ReelCount).Where(k => _m.Paytable[s.Index][k] > 0)
                    .Select(k => $"{k}:{_m.Paytable[s.Index][k] * g.LineBet}");
                Console.WriteLine($"  {s.Id,-8} {string.Join("  ", pays)}");
            }
            var sc = Enumerable.Range(1, _m.ReelCount).Where(k => _m.ScatterPays[k] > 0).Select(k => $"{k}:{_m.ScatterPays[k] * g.TotalBet}");
            Console.WriteLine($"  SCATTER  {string.Join("  ", sc)}  (anywhere; {_m.ScatterTriggerCount}+ = FREE SPINS)");
            Console.WriteLine("  WILD substitutes for all except SCATTER (reels 2-4)");
        }

        public void Say(string text, ConsoleColor color)
        {
            Write(text, color, false);
            Console.WriteLine();
        }

        private void Write(string text, ConsoleColor color, bool bright)
        {
            if (!_color) { Console.Write(text); return; }
            var old = Console.ForegroundColor;
            Console.ForegroundColor = color;
            if (bright) Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.Write(text);
            Console.ResetColor();
            Console.ForegroundColor = old;
        }
    }
}
