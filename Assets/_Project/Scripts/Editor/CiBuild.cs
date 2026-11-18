using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ZvezdnoePero.EditorTools
{
    /// <summary>
    /// Точка входа для облачной сборки (GitHub Actions, game-ci/unity-builder).
    /// Настраивает проект, собирает стартовую сцену и тестовый APK без диалогов.
    /// Путь к APK берётся из аргумента -customBuildPath, который передаёт game-ci.
    /// </summary>
    public static class CiBuild
    {
        public static void BuildAndroid()
        {
            try
            {
                ProjectSetup.ApplySettings();
                ProjectSetup.BuildScene();

                string path = ArgValue("-customBuildPath") ?? "build/Android/zvezdnoe-pero-test.apk";
                if (!path.EndsWith(".apk", StringComparison.OrdinalIgnoreCase)) path = Path.Combine(path, "zvezdnoe-pero-test.apk");
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));

                int run;
                if (int.TryParse(Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER"), out run))
                    PlayerSettings.Android.bundleVersionCode = run;

                EditorUserBuildSettings.buildAppBundle = false;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ProjectSetup.ScenePath },
                    locationPathName = path,
                    target = BuildTarget.Android,
                    options = BuildOptions.None,
                });

                Debug.Log($"[CiBuild] Результат: {report.summary.result}, ошибок: {report.summary.totalErrors}, файл: {path}");
                EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 101);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(102);
            }
        }

        static string ArgValue(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
