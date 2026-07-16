using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace SnowbreakFan.Infrastructure.Editor
{
    public static class BuildScripts
    {
        public static void BuildWindows64()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length != 3)
                throw new InvalidOperationException($"Expected 3 enabled scenes, found {scenes.Length}.");

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Builds/Windows/Platformer2D.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Windows build failed: {report.summary.result}");
        }
    }
}
