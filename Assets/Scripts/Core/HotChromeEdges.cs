using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace VCS.Core
{
    // Exploration (explore/hot-chrome): ink outlines as a Post Processing v2 custom effect. Only ever added to the
    // Hot Chrome volume (HotChromeLook), so the shipped look is untouched. Shader: Resources/Shaders/HotChromeEdges.
    [Serializable]
    [PostProcess(typeof(HotChromeEdgesRenderer), PostProcessEvent.BeforeStack, "VCS/Hot Chrome Edges")]
    public sealed class HotChromeEdges : PostProcessEffectSettings
    {
        public ColorParameter color = new ColorParameter { value = new Color(0.10f, 0f, 0.04f, 0.9f) };
        public FloatParameter thickness = new FloatParameter { value = 1.2f };
        public FloatParameter depthThreshold = new FloatParameter { value = 0.06f };
        public FloatParameter normalThreshold = new FloatParameter { value = 0.45f };
        public FloatParameter farFade = new FloatParameter { value = 1.6f };
    }

    public sealed class HotChromeEdgesRenderer : PostProcessEffectRenderer<HotChromeEdges>
    {
        static Shader shader;

        public override DepthTextureMode GetCameraFlags() => DepthTextureMode.DepthNormals;

        public override void Render(PostProcessRenderContext context)
        {
            if (shader == null) shader = Resources.Load<Shader>("Shaders/HotChromeEdges");
            if (shader == null) { context.command.BlitFullscreenTriangle(context.source, context.destination); return; }
            var sheet = context.propertySheets.Get(shader);
            sheet.properties.SetColor("_EdgeColor", settings.color);
            sheet.properties.SetFloat("_Thickness", settings.thickness);
            sheet.properties.SetFloat("_DepthThreshold", settings.depthThreshold);
            sheet.properties.SetFloat("_NormalThreshold", settings.normalThreshold);
            sheet.properties.SetFloat("_FarFade", settings.farFade);
            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
        }
    }
}
