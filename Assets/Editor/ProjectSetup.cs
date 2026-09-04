using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace VCS.Editor
{
    /// <summary>
    /// Applies player settings and creates the few assets the runtime needs (scene, materials).
    /// Runs before every build and is available from the VCS menu.
    /// </summary>
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("VCS/Setup Project")]
        public static void Apply()
        {
            PlayerSettings.productName = "Vacuum Cleaner Simulator 2026";
            PlayerSettings.companyName = "Cosnuau";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Standard);
            EnsureMaterials();
            EnsureScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[VCS] Project setup applied.");
        }

        static void EnsureMaterials()
        {
            Directory.CreateDirectory("Assets/Resources/Materials");
            EnsureMaterial("Assets/Resources/Materials/Lit.mat", "Standard", m =>
            {
                m.SetFloat("_Glossiness", 0.25f);
                m.SetFloat("_Metallic", 0f);
            });
            EnsureMaterial("Assets/Resources/Materials/Particle.mat", "Legacy Shaders/Particles/Alpha Blended", m =>
            {
                m.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
            });
        }

        static void EnsureMaterial(string path, string shaderName, System.Action<Material> configure)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning("[VCS] Shader not found: " + shaderName);
                return;
            }
            var m = new Material(shader);
            configure(m);
            AssetDatabase.CreateAsset(m, path);
            Debug.Log("[VCS] Created " + path);
        }

        static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory("Assets/Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log("[VCS] Created " + ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}
