using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Game.EditorTools
{
    /// <summary>
    /// Entry point called from scripts/unity-build.sh via -executeMethod.
    /// </summary>
    public static class BatchBuild
    {
        public static void BuildFromCommandLine()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var outDir = GetArg("-customBuildPath") ?? Path.Combine("Builds", target.ToString());

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Console.WriteLine("[BatchBuild] No enabled scenes in Build Settings");
                EditorApplication.Exit(1);
                return;
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                target = target,
                locationPathName = LocationFor(target, outDir),
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Console.WriteLine($"[BatchBuild] result={s.result} errors={s.totalErrors} size={s.totalSize} time={s.totalTime}");
            EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }

        private static string LocationFor(BuildTarget target, string outDir)
        {
            var name = PlayerSettings.productName;
            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64: return Path.Combine(outDir, name + ".exe");
                case BuildTarget.StandaloneOSX: return Path.Combine(outDir, name + ".app");
                case BuildTarget.StandaloneLinux64: return Path.Combine(outDir, name + ".x86_64");
                case BuildTarget.Android: return Path.Combine(outDir, name + ".apk");
                default: return outDir; // iOS / WebGL etc. output a folder
            }
        }

        private static string GetArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            var i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
    }
}
