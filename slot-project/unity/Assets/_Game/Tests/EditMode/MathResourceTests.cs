using System.IO;
using NUnit.Framework;
using SlotSdk;
using SlotSdk.Analysis;
using UnityEngine;

namespace Game.Tests
{
    public class MathResourceTests
    {
        private static string LoadJson()
        {
            var asset = Resources.Load<TextAsset>(GameSpec.MathResource);
            Assert.That(asset, Is.Not.Null, "Resources/Math/neon_fortune.json is missing (run tools/make_specs.sh)");
            return asset.text;
        }

        [Test]
        public void MathLoadsAndValidates()
        {
            var model = MathModel.FromJson(LoadJson());
            Assert.That(model.Validate(), Is.Empty);
            Assert.That(model.ReelCount, Is.EqualTo(5));
            Assert.That(model.RowCount, Is.EqualTo(3));
        }

        [Test]
        public void TheoreticalRtpIsInDesignRange()
        {
            var rtp = TheoreticalCalculator.Compute(MathModel.FromJson(LoadJson())).TotalRtp;
            Assert.That(rtp, Is.InRange(0.955, 0.965));
        }

        [Test]
        public void ResourceCopyMatchesSourceOfTruth()
        {
            // unity/ is inside slot-project/, so the source of truth is ../math/neon_fortune.json
            var source = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "math", "neon_fortune.json"));
            if (!File.Exists(source)) Assert.Ignore($"source math not found at {source}");
            Assert.That(LoadJson().Replace("\r\n", "\n"), Is.EqualTo(File.ReadAllText(source).Replace("\r\n", "\n")),
                "The Unity copy differs from math/neon_fortune.json. Run tools/make_specs.sh.");
        }

        [Test]
        public void EverySymbolHasAVisual()
        {
            var model = MathModel.FromJson(LoadJson());
            foreach (var s in model.Symbols)
                Assert.That(GameSpec.SymbolColor(s.Id), Is.Not.EqualTo(Color.white), $"no color for {s.Id}");
        }
    }
}
