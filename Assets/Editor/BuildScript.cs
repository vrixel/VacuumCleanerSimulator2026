using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VCS.Editor
{
    /// <summary>
    /// Batch-mode entry points. Invoked by tools/build.ps1:
    ///   Unity.exe -batchmode -nographics -quit -projectPath . -executeMethod VCS.Editor.BuildScript.BuildWindows64
    /// </summary>
    public static class BuildScript
    {
        public const string OutputDir = "Builds/Win64";
        public const string ExeName = "VacuumCleanerSimulator2026.exe";

        [MenuItem("VCS/Build Windows 64")]
        public static void BuildWindows64()
        {
            ProjectSetup.Apply();
            Directory.CreateDirectory(OutputDir);
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.ScenePath },
                locationPathName = Path.Combine(OutputDir, ExeName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            Debug.Log("[VCS] Build " + s.result + ": " + (s.totalSize / (1024 * 1024)) + " MB, "
                      + s.totalErrors + " errors, " + s.totalWarnings + " warnings, " + s.totalTime.TotalSeconds.ToString("F0") + " s");
            if (s.result != BuildResult.Succeeded)
            {
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw new Exception("Build failed: " + s.result);
            }
        }
    }
}
