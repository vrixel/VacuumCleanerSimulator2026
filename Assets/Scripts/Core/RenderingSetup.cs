using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace VCS.Core
{
    /// <summary>
    /// The look of the picture, on top of the lights: ambient occlusion in the creases, a touch of bloom on the
    /// LEDs and highlights, ACES tonemapping, a soft vignette and a little grain. Post Processing v2 for the built-in
    /// pipeline; the PostProcessResources asset is copied into Resources by ProjectSetup so builds ship the shaders.
    /// Every camera that should get the look calls Attach; one global volume holds the settings.
    /// </summary>
    public static class RenderingSetup
    {
        public const int VolumeLayer = 9;

        static PostProcessResources resources;
        static PostProcessVolume volume;

        public static bool Available
        {
            get
            {
                if (resources == null) resources = Resources.Load<PostProcessResources>("PostProcessResources");
                return resources != null;
            }
        }

        public static void Attach(Camera cam)
        {
            if (cam == null || !Available) { if (cam != null) Debug.LogWarning("[VCS] No PostProcessResources: camera " + cam.name + " renders raw"); return; }
            if (cam.GetComponent<PostProcessLayer>() != null) return;
            var layer = cam.gameObject.AddComponent<PostProcessLayer>();
            layer.Init(resources);
            layer.volumeLayer = 1 << VolumeLayer;
            layer.volumeTrigger = cam.transform;
            layer.antialiasingMode = PostProcessLayer.Antialiasing.None;   // MSAA 4x is on in QualitySettings
            layer.fog.enabled = false;
            cam.allowHDR = true;
            EnsureVolume();
        }

        static void EnsureVolume()
        {
            if (volume != null) return;
            var ao = ScriptableObject.CreateInstance<AmbientOcclusion>();
            ao.enabled.Override(true);
            ao.mode.Override(AmbientOcclusionMode.MultiScaleVolumetricObscurance);
            ao.intensity.Override(0.85f);
            ao.thicknessModifier.Override(1.1f);
            ao.color.Override(new Color(0.02f, 0.02f, 0.03f));
            ao.ambientOnly.Override(false);

            var bloom = ScriptableObject.CreateInstance<Bloom>();
            bloom.enabled.Override(true);
            bloom.intensity.Override(0.9f);
            bloom.threshold.Override(1.05f);
            bloom.softKnee.Override(0.55f);
            bloom.diffusion.Override(6.5f);

            var grading = ScriptableObject.CreateInstance<ColorGrading>();
            grading.enabled.Override(true);
            grading.tonemapper.Override(Tonemapper.ACES);
            grading.postExposure.Override(0.35f);
            grading.contrast.Override(8f);
            grading.saturation.Override(4f);
            grading.temperature.Override(3f);

            var vignette = ScriptableObject.CreateInstance<Vignette>();
            vignette.enabled.Override(true);
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.45f);

            var grain = ScriptableObject.CreateInstance<Grain>();
            grain.enabled.Override(true);
            grain.intensity.Override(0.10f);
            grain.size.Override(1.2f);
            grain.colored.Override(false);

            volume = PostProcessManager.instance.QuickVolume(VolumeLayer, 100f, ao, bloom, grading, vignette, grain);
            volume.isGlobal = true;
            Object.DontDestroyOnLoad(volume.gameObject);
            Debug.Log("[VCS] Post-processing on: AO, bloom, ACES, vignette, grain");
        }
    }
}
