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

        [MenuItem("VCS/Build Android APK")]
        public static void BuildAndroidApk() => BuildAndroid(false);

        [MenuItem("VCS/Build Android AAB")]
        public static void BuildAndroidAab() => BuildAndroid(true);

        /// <summary>
        /// Android (2026-09-07): an APK for a phone over adb, an AAB for Google Play. Invoked by tools/build-android.ps1
        /// with "-buildTarget Android". The upload keystore is read from the environment: VCS_KEYSTORE (path),
        /// VCS_KEYSTORE_PASS, VCS_KEYALIAS, VCS_KEYALIAS_PASS; without it the build is debug-signed (fine for adb).
        /// </summary>
        static void BuildAndroid(bool aab)
        {
            ProjectSetup.Apply();
            string ks = Environment.GetEnvironmentVariable("VCS_KEYSTORE");
            if (!string.IsNullOrEmpty(ks) && File.Exists(ks))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = ks;
                PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable("VCS_KEYSTORE_PASS") ?? "";
                PlayerSettings.Android.keyaliasName = Environment.GetEnvironmentVariable("VCS_KEYALIAS") ?? "vacuum";
                PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable("VCS_KEYALIAS_PASS") ?? PlayerSettings.Android.keystorePass;
                Debug.Log("[VCS] Android: signing with " + ks);
            }
            else
            {
                PlayerSettings.Android.useCustomKeystore = false;
                Debug.Log("[VCS] Android: no VCS_KEYSTORE, debug signature");
            }
            EditorUserBuildSettings.buildAppBundle = aab;
            const string dir = "Builds/Android";
            Directory.CreateDirectory(dir);
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.ScenePath },
                locationPathName = Path.Combine(dir, "VacuumCleanerSimulator2026." + (aab ? "aab" : "apk")),
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            Debug.Log("[VCS] Build " + s.result + ": " + (s.totalSize / (1024 * 1024)) + " MB, "
                      + s.totalErrors + " errors, " + s.totalWarnings + " warnings, " + s.totalTime.TotalSeconds.ToString("F0") + " s -> " + opts.locationPathName);
            if (s.result != BuildResult.Succeeded)
            {
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw new Exception("Build failed: " + s.result);
            }
        }
    }
}
