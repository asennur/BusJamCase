using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BusJam.EditorTools
{
    /// <summary>
    /// One-click Android APK build. Switching the build target reloads all scripts, so the request is remembered
    /// in SessionState and the build continues automatically after the reload. The result is written next to the APK.
    /// </summary>
    [InitializeOnLoad]
    public static class AndroidBuilder
    {
        private const string PendingKey = "BusJam.AndroidBuildPending";
        private const string OutputFolder = @"C:\Users\Kullanici\Documents\BusJamCase_Builds\Android";
        private static readonly string ApkPath = Path.Combine(OutputFolder, "BusJamCase.apk");
        private static readonly string ResultPath = Path.Combine(OutputFolder, "build_result.txt");

        static AndroidBuilder()
        {
            if (SessionState.GetBool(PendingKey, false))
                EditorApplication.delayCall += BuildNow;
        }

        [MenuItem("Bus Jam/Build/Android APK")]
        public static void Build()
        {
            Directory.CreateDirectory(OutputFolder);
            File.WriteAllText(ResultPath, "started");
            Configure();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                SessionState.SetBool(PendingKey, true);
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                return; // continues in BuildNow after the script reload
            }
            BuildNow();
        }

        private static void BuildNow()
        {
            SessionState.SetBool(PendingKey, false);
            Configure();
            EditorUserBuildSettings.buildAppBundle = false;
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/_BusJam/Scenes/Game.unity", "Assets/_BusJam/Scenes/LevelEditor.unity" },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            File.WriteAllText(ResultPath,
                $"{summary.result}\nsize: {summary.totalSize / (1024f * 1024f):0.0} MB\nerrors: {summary.totalErrors}\ntime: {summary.totalTime}");
            Debug.Log($"[AndroidBuilder] {summary.result} -> {ApkPath}");
        }

        private static void Configure()
        {
            PlayerSettings.companyName = "Asen";
            PlayerSettings.productName = "Bus Jam Case";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.asen.busjamcase");
            PlayerSettings.bundleVersion = "1.0";
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        }
    }
}
