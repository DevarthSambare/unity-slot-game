using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SlotGame.EditorTools
{
    /// <summary>
    /// One click (or command line) WebGL build into Build/WebGL, which is the folder the
    /// assignment asks for. Command line:
    ///   Unity -batchmode -quit -projectPath . -executeMethod SlotGame.EditorTools.WebGLBuilder.Build
    /// </summary>
    public static class WebGLBuilder
    {
        private const string OutputPath = "Build/WebGL";

        [MenuItem("Tools/Slot Game/Build WebGL")]
        public static void Build()
        {
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
                Debug.Log($"[Build] WebGL OK: {summary.totalSize / (1024f * 1024f):0.0} MB in {summary.totalTime.TotalSeconds:0}s -> {OutputPath}");
            else
                Debug.LogError($"[Build] WebGL failed: {summary.result}, {summary.totalErrors} errors");

            // non zero exit code in batch mode so CI notices a failed build
            if (Application.isBatchMode && summary.result != BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }
    }
}
