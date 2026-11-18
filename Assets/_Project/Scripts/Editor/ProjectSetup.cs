using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ZvezdnoePero.EditorTools
{
    /// <summary>
    /// Меню «Звёздное перо» в редакторе Unity.
    /// 1 — настраивает проект под Android, URP и новую систему ввода.
    /// 2 — собирает стартовую сцену (см. WorldBuilder).
    /// 3 — собирает тестовый APK.
    /// </summary>
    public static class ProjectSetup
    {
        public const string ProductName = "Академия Звёздного Пера";
        public const string AndroidId = "com.aim9798.zvezdnoepero";
        public const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        const string UrpFolder = "Assets/_Project/Settings";
        const string ModelsFolder = "Assets/_Project/Art/Models";

        [MenuItem("Звёздное перо/1. Настроить проект (Android, URP, ввод)", priority = 1)]
        public static void ConfigureProject()
        {
            bool inputChanged = ApplySettings();

            if (BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }
            else
            {
                EditorUtility.DisplayDialog(ProductName,
                    "Модуль Android Build Support не установлен. Добавь его в Unity Hub: Installs → шестерёнка у версии → Add modules → Android Build Support (вместе с OpenJDK и SDK/NDK). Остальные настройки уже применены.",
                    "Понятно");
            }

            if (inputChanged &&
                EditorUtility.DisplayDialog(ProductName,
                    "Включена новая система ввода. Unity нужно перезапустить проект, чтобы она заработала.",
                    "Перезапустить", "Позже"))
            {
                EditorApplication.OpenProject(Environment.CurrentDirectory);
                return;
            }

            Debug.Log($"[{ProductName}] Проект настроен. Следующий шаг: меню «Звёздное перо → 2. Собрать стартовую сцену».");
        }

        /// <summary>
        /// Применяет настройки Android, URP и ввода без диалогов (используется и в облачной сборке).
        /// Возвращает true, если пришлось переключить систему ввода.
        /// </summary>
        public static bool ApplySettings()
        {
            PlayerSettings.companyName = "AIM9798";
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            var android = NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(android, AndroidId);
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.bundleVersionCode = Math.Max(1, PlayerSettings.Android.bundleVersionCode);

            EnsureUrp();
            ReimportModels();
            bool inputChanged = EnableNewInputSystem();
            AssetDatabase.SaveAssets();
            return inputChanged;
        }

        [MenuItem("Звёздное перо/2. Собрать стартовую сцену", priority = 2)]
        public static void BuildScene()
        {
            if (GraphicsSettings.defaultRenderPipeline == null) EnsureUrp();
            if (File.Exists(ScenePath) && !Application.isBatchMode &&
                !EditorUtility.DisplayDialog(ProductName,
                    "Сцена Main уже есть. Собрать её заново? Ручные правки в ней пропадут.", "Собрать заново", "Отмена"))
                return;

            WorldBuilder.Build(ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log($"[{ProductName}] Сцена собрана: {ScenePath}. Нажми Play: WASD — ходьба, F — метла, пробел/Ctrl — вверх/вниз, E — действие, правая кнопка мыши — камера.");
        }

        [MenuItem("Звёздное перо/3. Собрать тестовый APK", priority = 3)]
        public static void BuildTestApk()
        {
            if (!File.Exists(ScenePath)) BuildScene();
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                EditorUtility.DisplayDialog(ProductName, "Сначала установи модуль Android Build Support в Unity Hub.", "Понятно");
                return;
            }

            EditorUserBuildSettings.buildAppBundle = false;
            Directory.CreateDirectory("Builds/Android");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Android/zvezdnoe-pero-test.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[{ProductName}] APK готов: {options.locationPathName} ({report.summary.totalSize / (1024 * 1024)} МБ)");
                EditorUtility.RevealInFinder(options.locationPathName);
            }
            else
            {
                Debug.LogError($"[{ProductName}] Сборка не удалась: {report.summary.result}. Ошибки — в Console выше.");
            }
        }

        static void EnsureUrp()
        {
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset existing)
            {
                ApplyMobileQuality(existing);
                return;
            }

            Directory.CreateDirectory(UrpFolder);
            string rendererPath = UrpFolder + "/Mobile_Renderer.asset";
            string assetPath = UrpFolder + "/Mobile_URP.asset";

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, rendererPath);
            }

            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (urp == null)
            {
                urp = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(urp, assetPath);
            }

            ApplyMobileQuality(urp);
            GraphicsSettings.defaultRenderPipeline = urp;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = urp;
            }
            QualitySettings.SetQualityLevel(current, false);
            AssetDatabase.SaveAssets();
        }

        static void ApplyMobileQuality(UniversalRenderPipelineAsset urp)
        {
            urp.shadowDistance = 120f;
            urp.renderScale = 0.85f;
            urp.msaaSampleCount = 2;
            EditorUtility.SetDirty(urp);
        }

        /// <summary>Модели импортируются до включения URP и получают не те шейдеры — импортируем заново.</summary>
        static void ReimportModels()
        {
            if (!AssetDatabase.IsValidFolder(ModelsFolder)) return;
            foreach (var guid in AssetDatabase.FindAssets("", new[] { ModelsFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".glb", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase))
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        /// <summary>Переключает «Active Input Handling» на Input System. Возвращает true, если значение изменилось.</summary>
        static bool EnableNewInputSystem()
        {
            var settings = Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");
            if (settings == null) return false;
            var so = new SerializedObject(settings);
            var prop = so.FindProperty("activeInputHandler");
            if (prop == null || prop.intValue == 1) return false;
            prop.intValue = 1;
            so.ApplyModifiedProperties();
            return true;
        }
    }
}
